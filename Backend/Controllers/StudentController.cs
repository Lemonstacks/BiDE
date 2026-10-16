using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int? GetStudentId()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Student") return null;
            return HttpContext.Session.GetInt32("UserId");
        }

        // GET: /Student/Bookings
        public async Task<IActionResult> Bookings()
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Instructor)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.Payment)
                .Where(b => b.StudentId == studentId.Value)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var active = bookings.Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted).ToList();
            var past = bookings.Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Rejected).ToList();

            ViewBag.Active = active;
            ViewBag.Past = past;
            return View();
        }

        // GET: /Student/CompletedLessons
        public async Task<IActionResult> CompletedLessons()
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Instructor)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.Review)
                .Include(b => b.LessonProgresses)
                .Where(b => b.StudentId == studentId.Value && b.Status == BookingStatus.Completed)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: /Student/LessonProgress
        public async Task<IActionResult> LessonProgress()
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Instructor)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.LessonProgresses)
                .Where(b => b.StudentId == studentId.Value &&
                       (b.Status == BookingStatus.Accepted || b.Status == BookingStatus.Completed))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: /Student/CancelBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId, string? reason)
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.StudentId == studentId.Value);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Accepted)
            {
                TempData["Error"] = "Only pending or accepted bookings can be cancelled.";
                return RedirectToAction("Bookings");
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.CancelledBy = CancelledBy.Student;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Free up the schedule slot
            if (booking.Schedule != null)
                booking.Schedule.AvailabilityStatus = AvailabilityStatus.Available;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully.";
            return RedirectToAction("Bookings");
        }

        // POST: /Student/LeaveReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveReview(int bookingId, int rating, string? comment)
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("CompletedLessons");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.StudentId == studentId.Value);

            if (booking == null || booking.Status != BookingStatus.Completed)
                return NotFound();

            // Check if already reviewed
            var existing = await _context.Reviews
                .AnyAsync(r => r.BookingId == bookingId);
            if (existing)
            {
                TempData["Error"] = "You have already reviewed this lesson.";
                return RedirectToAction("CompletedLessons");
            }

            var review = new Review
            {
                BookingId = bookingId,
                StudentId = studentId.Value,
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Review submitted. Thank you!";
            return RedirectToAction("CompletedLessons");
        }

        // POST: /Student/SubmitPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPayment(int bookingId, string paymentMethod, string? paymentReference, IFormFile proofOfPayment)
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            // Validate payment method
            if (string.IsNullOrWhiteSpace(paymentMethod) || !PaymentMethod.All.Contains(paymentMethod))
            {
                TempData["Error"] = "Please select a valid payment method.";
                return RedirectToAction("Bookings");
            }

            // EFT requires a payment reference so the instructor can match the bank deposit
            if (paymentMethod == PaymentMethod.Eft && string.IsNullOrWhiteSpace(paymentReference))
            {
                TempData["Error"] = "Please enter the payment reference used for your EFT.";
                return RedirectToAction("Bookings");
            }

            // Validate file upload (not required for Cash payments)
            if (paymentMethod != PaymentMethod.Cash)
            {
                if (proofOfPayment == null || proofOfPayment.Length == 0)
                {
                    TempData["Error"] = "Proof of payment file is required for EFT payments.";
                    return RedirectToAction("Bookings");
                }

                // File size limit: 5MB
                if (proofOfPayment.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction("Bookings");
                }

                // File type validation (extension allowlist).
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(proofOfPayment.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Only JPG, PNG, and PDF files are allowed.";
                    return RedirectToAction("Bookings");
                }

                // Content validation: inspect the actual file bytes (magic number) so a
                // disallowed file (e.g. a .vsdx or .exe) renamed to .pdf/.jpg cannot slip
                // past the extension check. A renamed Office/Visio file is really a ZIP
                // and starts with "PK", which matches none of the signatures below.
                if (!await IsAllowedFileSignatureAsync(proofOfPayment))
                {
                    TempData["Error"] = "The file does not look like a valid JPG, PNG, or PDF. Please upload a real image or PDF.";
                    return RedirectToAction("Bookings");
                }
            }

            var booking = await _context.Bookings
                .Include(b => b.LessonOffering)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.StudentId == studentId.Value);

            if (booking == null) return NotFound();

            // Don't allow payment if already paid
            if (booking.Payment != null && booking.Payment.PaymentStatus == PaymentStatus.Verified)
            {
                TempData["Error"] = "Payment already verified for this booking.";
                return RedirectToAction("Bookings");
            }

            // Save proof of payment file
            string? proofPath = null;
            if (proofOfPayment != null && proofOfPayment.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "payments");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"payment_{bookingId}_{Path.GetRandomFileName()}{Path.GetExtension(proofOfPayment.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await proofOfPayment.CopyToAsync(stream);
                }

                proofPath = $"/uploads/payments/{fileName}";
            }

            if (booking.Payment != null)
            {
                // Update existing payment record
                booking.Payment.PaymentMethod = paymentMethod;
                booking.Payment.PaymentReference = paymentReference;
                booking.Payment.ProofOfPayment = proofPath;
                booking.Payment.PaymentStatus = paymentMethod == PaymentMethod.Cash ? PaymentStatus.Verified : PaymentStatus.Pending;
                booking.Payment.PaymentDate = DateTime.UtcNow;
                booking.Payment.RejectionReason = null;
                if (paymentMethod == PaymentMethod.Cash) booking.Payment.VerificationDate = DateTime.UtcNow;
            }
            else
            {
                // Create new payment record
                var payment = new Payment
                {
                    BookingId = bookingId,
                    InstructorId = booking.InstructorId,
                    Amount = booking.AgreedPrice > 0 ? booking.AgreedPrice : (booking.LessonOffering?.Price ?? 0),
                    PaymentMethod = paymentMethod,
                    PaymentReference = paymentReference,
                    ProofOfPayment = proofPath,
                    PaymentStatus = paymentMethod == PaymentMethod.Cash ? PaymentStatus.Verified : PaymentStatus.Pending,
                    PaymentDate = DateTime.UtcNow
                };
                if (paymentMethod == PaymentMethod.Cash) payment.VerificationDate = DateTime.UtcNow;
                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            if (paymentMethod == PaymentMethod.Cash)
            {
                TempData["Success"] = "Cash payment confirmed. Your lesson is ready to go!";
            }
            else
            {
                TempData["Success"] = "Payment submitted. Your instructor will verify it shortly.";
            }
            return RedirectToAction("Bookings");
        }

        /// <summary>
        /// Verifies an uploaded file's actual leading bytes (its "magic number") match an
        /// allowed proof-of-payment type: JPEG, PNG, or PDF. This is content-based validation
        /// that cannot be fooled by simply renaming a disallowed file (e.g. a .vsdx, .docx, or
        /// .exe) to a .pdf/.jpg extension, because those files begin with different signatures
        /// (Office/Visio/ZIP files start with "PK", 0x50 0x4B).
        /// </summary>
        private static async Task<bool> IsAllowedFileSignatureAsync(IFormFile file)
        {
            try
            {
                // Read the first 8 bytes, enough for every signature we check.
                var header = new byte[8];
                using (var stream = file.OpenReadStream())
                {
                    int read = await stream.ReadAsync(header.AsMemory(0, header.Length));
                    if (read < 4) return false; // too short to be any valid image/pdf
                }

                // JPEG: FF D8 FF
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                    return true;

                // PNG: 89 50 4E 47 0D 0A 1A 0A
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                    return true;

                // PDF: 25 50 44 46  ("%PDF")
                if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                    return true;

                return false;
            }
            catch
            {
                // If the file can't be read, treat it as invalid rather than letting it through.
                return false;
            }
        }
    }
}
