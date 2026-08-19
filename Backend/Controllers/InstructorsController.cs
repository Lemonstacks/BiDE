using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using BiDE.Data;
using BiDE.Models;
using BiDE.Hubs;

namespace BiDE.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<InstructorHub> _hubContext;

        public InstructorsController(ApplicationDbContext context, IHubContext<InstructorHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: /Instructors
        public async Task<IActionResult> Index(string? search, string? specialization)
        {
            // Only registered students can search for instructors (FSSB: student must be logged in)
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null)
            {
                TempData["Error"] = "Please log in to find instructors.";
                return RedirectToAction("Login", "Account");
            }

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

            // Validate offering and schedule belong to the same instructor
            if (offering.InstructorId != instructorId || schedule.InstructorId != instructorId)
            {
                TempData["Error"] = "Offering and schedule must belong to the selected instructor.";
                return RedirectToAction("Detail", new { id = instructorId });
            }

            // Verify the slot is still available
            if (schedule.AvailabilityStatus != "Available")
            {
                TempData["Error"] = "This time slot is no longer available. Please choose another.";
                return RedirectToAction("Detail", new { id = instructorId });
            }

            // Verify the instructor is still approved
            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null || instructor.Status != InstructorStatus.Approved || !instructor.IsVerified)
            {
                TempData["Error"] = "This instructor is no longer available for bookings.";
                return RedirectToAction("Index");
            }

            // Prevent duplicate booking for same slot
            var existingBooking = await _context.Bookings
                .AnyAsync(b => b.ScheduleId == scheduleId && b.Status != "Cancelled" && b.Status != "Rejected");
            if (existingBooking)
            {
                TempData["Error"] = "This time slot has already been booked.";
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

        // GET: /Instructors/LiveMap
        public IActionResult LiveMap()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null || role != "Student")
            {
                TempData["Error"] = "Please log in as a student to view the live map.";
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // POST: /Instructors/BookRealTime
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookRealTime(int instructorId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null || role != "Student")
            {
                TempData["Error"] = "Please log in as a student to book.";
                return RedirectToAction("Login", "Account");
            }

            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null || instructor.Status != InstructorStatus.Approved)
            {
                TempData["Error"] = "Instructor is not available.";
                return RedirectToAction("LiveMap");
            }

            // Find the next available slot for this instructor
            var nextSlot = await _context.Availabilities
                .Where(a => a.InstructorId == instructorId && a.AvailabilityStatus == "Available" && a.Date >= DateTime.Today)
                .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
                .FirstOrDefaultAsync();

            // Find any offering from this instructor
            var offering = await _context.LessonOfferings
                .Where(o => o.InstructorId == instructorId)
                .FirstOrDefaultAsync();

            if (nextSlot == null || offering == null)
            {
                TempData["Error"] = "This instructor has no available slots or offerings. Try the standard booking instead.";
                return RedirectToAction("LiveMap");
            }

            var booking = new Booking
            {
                InstructorId = instructorId,
                StudentId = userId.Value,
                ScheduleId = nextSlot.AvailabilityId,
                OfferId = offering.OfferId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            nextSlot.AvailabilityStatus = "Booked";
            await _context.SaveChangesAsync();

            // Remove instructor from all live maps via SignalR
            await _hubContext.Clients.All.SendAsync("InstructorRemoved", instructorId);

            TempData["Success"] = "Booking request sent! The instructor will be notified.";
            return RedirectToAction("Bookings", "Student");
        }
    }
}
