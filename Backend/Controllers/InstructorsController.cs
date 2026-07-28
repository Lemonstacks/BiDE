using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstructorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Instructors
        public async Task<IActionResult> Index(string? search, string? specialization)
        {
            var query = _context.Instructors
                .Where(i => i.Status == InstructorStatus.Approved && i.IsVerified);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(i =>
                    i.FirstName.ToLower().Contains(term) ||
                    i.LastName.ToLower().Contains(term) ||
                    (i.Suburb != null && i.Suburb.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(specialization) && specialization != "all")
            {
                query = query.Where(i =>
                    i.Certification != null && i.Certification.Contains(specialization));
            }

            var instructors = await query
                .OrderByDescending(i => i.Experience)
                .ToListAsync();

            // Get all unique certifications for the filter dropdown
            var allCertifications = await _context.Instructors
                .Where(i => i.Status == InstructorStatus.Approved && i.Certification != null)
                .Select(i => i.Certification!)
                .Distinct()
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Specialization = specialization;
            ViewBag.Certifications = allCertifications;

            return View(instructors);
        }

        // GET: /Instructors/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.InstructorId == id);

            if (instructor == null)
                return NotFound();

            var offerings = await _context.LessonOfferings
                .Where(o => o.InstructorId == id)
                .ToListAsync();

            var availability = await _context.Availabilities
                .Where(a => a.InstructorId == id && a.AvailabilityStatus == "Available")
                .OrderBy(a => a.Date)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.Booking)
                .Include(r => r.Student)
                .Where(r => r.Booking.InstructorId == id)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            ViewBag.Offerings = offerings;
            ViewBag.Availability = availability;
            ViewBag.Reviews = reviews;

            return View(instructor);
        }

        // POST: /Instructors/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int instructorId, int offerId, int scheduleId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || role != "Student")
            {
                TempData["Error"] = "Please log in as a student to book a lesson.";
                return RedirectToAction("Login", "Account");
            }

            // Verify entities exist
            var offering = await _context.LessonOfferings.FindAsync(offerId);
            var schedule = await _context.Availabilities.FindAsync(scheduleId);

            if (offering == null || schedule == null)
            {
                TempData["Error"] = "Invalid offering or schedule selection.";
                return RedirectToAction("Detail", new { id = instructorId });
            }

            var booking = new Booking
            {
                InstructorId = instructorId,
                StudentId = userId.Value,
                ScheduleId = scheduleId,
                OfferId = offerId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // Mark schedule as booked
            schedule.AvailabilityStatus = "Booked";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking submitted successfully! The instructor will review your request.";
            return RedirectToAction("Bookings", "Student");
        }
    }
}
