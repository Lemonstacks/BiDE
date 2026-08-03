using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
