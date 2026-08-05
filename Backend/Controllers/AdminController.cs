using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetAdminId()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return null;
            return HttpContext.Session.GetInt32("UserId");
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var pending = await _context.Instructors
                .Where(i => i.Status == InstructorStatus.Pending)
                .OrderBy(i => i.ApplicationDate)
                .ToListAsync();

            var approved = await _context.Instructors
                .Where(i => i.Status == InstructorStatus.Approved)
                .OrderByDescending(i => i.ApprovalDate)
                .ToListAsync();

            var rejected = await _context.Instructors
                .Where(i => i.Status == InstructorStatus.Rejected)
                .OrderByDescending(i => i.ApprovalDate)
                .ToListAsync();

            ViewBag.Pending = pending;
            ViewBag.Approved = approved;
            ViewBag.Rejected = rejected;
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();

            return View();
        }

        // POST: /Admin/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int instructorId)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null) return NotFound();

            instructor.Status = InstructorStatus.Approved;
            instructor.IsVerified = true;
            instructor.ApprovedByAdminId = adminId.Value;
            instructor.ApprovalDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{instructor.FirstName} {instructor.LastName} has been approved.";
            return RedirectToAction("Index");
        }

        // POST: /Admin/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int instructorId, string? reason)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null) return NotFound();

            instructor.Status = InstructorStatus.Rejected;
            instructor.IsVerified = false;
            instructor.ApprovedByAdminId = adminId.Value;
            instructor.ApprovalDate = DateTime.UtcNow;
            instructor.RejectionReason = reason;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{instructor.FirstName} {instructor.LastName} has been rejected.";
            return RedirectToAction("Index");
        }

        // POST: /Admin/Suspend
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int instructorId)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null) return NotFound();

            instructor.Status = InstructorStatus.Suspended;
            instructor.IsVerified = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{instructor.FirstName} {instructor.LastName} has been suspended.";
            return RedirectToAction("Index");
        }

        // POST: /Admin/Reinstate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reinstate(int instructorId)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null) return NotFound();

            instructor.Status = InstructorStatus.Approved;
            instructor.IsVerified = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{instructor.FirstName} {instructor.LastName} has been reinstated.";
            return RedirectToAction("Index");
        }

        // GET: /Admin/Bookings
        public async Task<IActionResult> Bookings(string? status, string? search, DateTime? dateFrom, DateTime? dateTo)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var query = _context.Bookings
                .Include(b => b.Student)
                .Include(b => b.Instructor)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.Payment)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(b => b.Status == status);
            }

            // Filter by search (student or instructor name)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(b =>
                    b.Student.FirstName.ToLower().Contains(term) ||
                    b.Student.LastName.ToLower().Contains(term) ||
                    b.Instructor.FirstName.ToLower().Contains(term) ||
                    b.Instructor.LastName.ToLower().Contains(term));
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                query = query.Where(b => b.Schedule != null && b.Schedule.Date >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(b => b.Schedule != null && b.Schedule.Date <= dateTo.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;

            return View(bookings);
        }
    }
}
