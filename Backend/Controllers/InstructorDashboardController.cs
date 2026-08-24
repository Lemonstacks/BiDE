using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class InstructorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstructorDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetInstructorId()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Instructor") return null;
            return HttpContext.Session.GetInt32("UserId");
        }

        // GET: /InstructorDashboard
        public async Task<IActionResult> Index()
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(instructorId.Value);
            if (instructor == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Student)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.Payment)
                .Where(b => b.InstructorId == instructorId.Value)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.Instructor = instructor;
            ViewBag.Pending = bookings.Where(b => b.Status == "Pending").ToList();
            ViewBag.Accepted = bookings.Where(b => b.Status == "Accepted").ToList();
            ViewBag.Completed = bookings.Where(b => b.Status == "Completed").ToList();
            ViewBag.TotalBookings = bookings.Count;

            return View();
        }

        // POST: /InstructorDashboard/AcceptBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId.Value);
            if (booking == null) return NotFound();

            booking.Status = "Accepted";
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking accepted.";
            return RedirectToAction("Index");
        }

        // POST: /InstructorDashboard/RejectBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int bookingId, string? reason)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId.Value);
            if (booking == null) return NotFound();

            booking.Status = "Rejected";
            booking.CancellationReason = reason;
            booking.CancelledBy = "Instructor";
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.Schedule != null)
                booking.Schedule.AvailabilityStatus = "Available";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking rejected.";
            return RedirectToAction("Index");
        }

        // POST: /InstructorDashboard/CompleteBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBooking(int bookingId)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId.Value);
            if (booking == null) return NotFound();

            if (booking.Status != "Accepted")
            {
                TempData["Error"] = "Only accepted bookings can be marked as completed.";
                return RedirectToAction("Index");
            }

            booking.Status = "Completed";
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lesson marked as completed.";
            return RedirectToAction("Index");
        }

        // GET: /InstructorDashboard/Availability
        public async Task<IActionResult> Availability()
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var slots = await _context.Availabilities
                .Where(a => a.InstructorId == instructorId.Value)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return View(slots);
        }

        // POST: /InstructorDashboard/AddAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            // Validate: end time must be after start time
            if (endTime <= startTime)
            {
                TempData["Error"] = "End time must be after start time.";
                return RedirectToAction("Availability");
            }

            // Validate: cannot add slot in the past
            var slotDateTime = date.Date + startTime;
            if (slotDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Cannot add an availability slot in the past.";
                return RedirectToAction("Availability");
            }

            // Validate: no overlapping slots
            var overlapping = await _context.Availabilities
                .AnyAsync(a => a.InstructorId == instructorId.Value &&
                    a.Date == date.Date &&
                    a.StartTime < endTime &&
                    a.EndTime > startTime);
            if (overlapping)
            {
                TempData["Error"] = "This slot overlaps with an existing availability.";
                return RedirectToAction("Availability");
            }

            var slot = new Availability
            {
                InstructorId = instructorId.Value,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                AvailabilityStatus = "Available",
                CreatedAt = DateTime.UtcNow
            };

            _context.Availabilities.Add(slot);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Availability slot added.";
            return RedirectToAction("Availability");
        }

        // POST: /InstructorDashboard/DeleteAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var slot = await _context.Availabilities
                .FirstOrDefaultAsync(a => a.AvailabilityId == id && a.InstructorId == instructorId.Value);

            if (slot != null)
            {
                _context.Availabilities.Remove(slot);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Slot removed.";
            }

            return RedirectToAction("Availability");
        }

        // GET: /InstructorDashboard/Offerings
        public async Task<IActionResult> Offerings()
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(instructorId.Value);
            var offerings = await _context.LessonOfferings
                .Where(o => o.InstructorId == instructorId.Value)
                .ToListAsync();

            ViewBag.Instructor = instructor;
            return View(offerings);
        }

        // POST: /InstructorDashboard/CreateOffering
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOffering(string title, string lessonType, string? description, decimal price)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction("Offerings");
            }

            if (price < 0)
            {
                TempData["Error"] = "Price cannot be negative.";
                return RedirectToAction("Offerings");
            }

            var offering = new LessonOffering
            {
                InstructorId = instructorId.Value,
                Title = title,
                LessonType = lessonType,
                Description = description,
                Price = price,
                CreatedAt = DateTime.UtcNow
            };

            _context.LessonOfferings.Add(offering);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Offering created.";
            return RedirectToAction("Offerings");
        }

        // POST: /InstructorDashboard/UpdateOffering
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOffering(int offerId, string title, string lessonType, string? description, decimal price)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction("Offerings");
            }

            if (price < 0)
            {
                TempData["Error"] = "Price cannot be negative.";
                return RedirectToAction("Offerings");
            }

            var offering = await _context.LessonOfferings
                .FirstOrDefaultAsync(o => o.OfferId == offerId && o.InstructorId == instructorId.Value);
            if (offering == null) return NotFound();

            offering.Title = title;
            offering.LessonType = lessonType;
            offering.Description = description;
            offering.Price = price;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Offering updated.";
            return RedirectToAction("Offerings");
        }

        // POST: /InstructorDashboard/DeleteOffering
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOffering(int offerId)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var offering = await _context.LessonOfferings
                .FirstOrDefaultAsync(o => o.OfferId == offerId && o.InstructorId == instructorId.Value);

            if (offering != null)
            {
                _context.LessonOfferings.Remove(offering);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Offering deleted.";
            }

            return RedirectToAction("Offerings");
        }

        // GET: /InstructorDashboard/LessonProgress
        public async Task<IActionResult> LessonProgress()
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Student)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.LessonProgresses)
                .Where(b => b.InstructorId == instructorId.Value && b.Status == "Accepted")
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: /InstructorDashboard/AddProgress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProgress(int bookingId, string feedback, string completionStatus, int duration)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(feedback))
            {
                TempData["Error"] = "Feedback is required.";
                return RedirectToAction("LessonProgress");
            }

            if (duration <= 0)
            {
                TempData["Error"] = "Duration must be greater than 0.";
                return RedirectToAction("LessonProgress");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId.Value);
            if (booking == null) return NotFound();

            if (booking.Status != "Accepted")
            {
                TempData["Error"] = "Can only add progress to accepted bookings.";
                return RedirectToAction("LessonProgress");
            }

            var progress = new Models.LessonProgress
            {
                BookingId = bookingId,
                ProgressDate = DateTime.UtcNow,
                Duration = duration,
                StartTime = TimeSpan.Zero,
                CompletionStatus = completionStatus,
                Feedback = feedback
            };

            _context.LessonProgresses.Add(progress);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Progress recorded.";
            return RedirectToAction("LessonProgress");
        }

        // GET: /InstructorDashboard/Payments
        public async Task<IActionResult> Payments()
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Student)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.LessonOffering)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Schedule)
                .Where(p => p.InstructorId == instructorId.Value)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.Pending = payments.Where(p => p.PaymentStatus == "Pending").ToList();
            ViewBag.Verified = payments.Where(p => p.PaymentStatus == "Verified").ToList();
            ViewBag.Rejected = payments.Where(p => p.PaymentStatus == "Rejected").ToList();

            return View(payments);
        }

        // POST: /InstructorDashboard/VerifyPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int paymentId)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.InstructorId == instructorId.Value);
            if (payment == null) return NotFound();

            payment.PaymentStatus = "Verified";
            payment.VerificationDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment verified.";
            return RedirectToAction("Payments");
        }

        // POST: /InstructorDashboard/RejectPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int paymentId)
        {
            var instructorId = GetInstructorId();
            if (instructorId == null) return RedirectToAction("Login", "Account");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.InstructorId == instructorId.Value);
            if (payment == null) return NotFound();

            payment.PaymentStatus = "Rejected";
            payment.VerificationDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment rejected.";
            return RedirectToAction("Payments");
        }
    }
}
