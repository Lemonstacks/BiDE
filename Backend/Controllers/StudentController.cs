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

            var active = bookings.Where(b => b.Status == "Pending" || b.Status == "Accepted").ToList();
            var past = bookings.Where(b => b.Status == "Completed" || b.Status == "Cancelled" || b.Status == "Rejected").ToList();

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
                .Where(b => b.StudentId == studentId.Value && b.Status == "Completed")
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
                       (b.Status == "Accepted" || b.Status == "Completed"))
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

            if (booking.Status != "Pending" && booking.Status != "Accepted")
            {
                TempData["Error"] = "Only pending or accepted bookings can be cancelled.";
                return RedirectToAction("Bookings");
            }

            booking.Status = "Cancelled";
            booking.CancellationReason = reason;
            booking.CancelledBy = "Student";
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Free up the schedule slot
            if (booking.Schedule != null)
                booking.Schedule.AvailabilityStatus = "Available";

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

            if (booking == null || booking.Status != "Completed")
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
        public async Task<IActionResult> SubmitPayment(int bookingId, string paymentMethod, IFormFile proofOfPayment)
        {
            var studentId = GetStudentId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            // Validate payment method
            var validMethods = new[] { "EFT", "Cash", "Card" };
            if (string.IsNullOrWhiteSpace(paymentMethod) || !validMethods.Contains(paymentMethod))
            {
                TempData["Error"] = "Please select a valid payment method.";
                return RedirectToAction("Bookings");
            }

            // Validate file upload (not required for Cash payments)
            if (paymentMethod != "Cash")
            {
                if (proofOfPayment == null || proofOfPayment.Length == 0)
                {
                    TempData["Error"] = "Proof of payment file is required for EFT and Card payments.";
                    return RedirectToAction("Bookings");
                }

                // File size limit: 5MB
                if (proofOfPayment.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction("Bookings");
                }

                // File type validation
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(proofOfPayment.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Only JPG, PNG, and PDF files are allowed.";
                    return RedirectToAction("Bookings");
                }
            }

            var booking = await _context.Bookings
                .Include(b => b.LessonOffering)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.StudentId == studentId.Value);

            if (booking == null) return NotFound();

            // Don't allow payment if already paid
            if (booking.Payment != null && booking.Payment.PaymentStatus == "Verified")
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
                booking.Payment.ProofOfPayment = proofPath;
                booking.Payment.PaymentStatus = paymentMethod == "Cash" ? "Verified" : "Pending";
                booking.Payment.PaymentDate = DateTime.UtcNow;
                booking.Payment.RejectionReason = null;
                if (paymentMethod == "Cash") booking.Payment.VerificationDate = DateTime.UtcNow;
            }
            else
            {
                // Create new payment record
                var payment = new Payment
                {
                    BookingId = bookingId,
                    InstructorId = booking.InstructorId,
                    Amount = booking.LessonOffering?.Price ?? 0,
                    PaymentMethod = paymentMethod,
                    ProofOfPayment = proofPath,
                    PaymentStatus = paymentMethod == "Cash" ? "Verified" : "Pending",
                    PaymentDate = DateTime.UtcNow
                };
                if (paymentMethod == "Cash") payment.VerificationDate = DateTime.UtcNow;
                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            if (paymentMethod == "Cash")
            {
                TempData["Success"] = "Cash payment confirmed. Your lesson is ready to go!";
            }
            else
            {
                TempData["Success"] = "Payment submitted. Your instructor will verify it shortly.";
            }
            return RedirectToAction("Bookings");
        }
    }
}
