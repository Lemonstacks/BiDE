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
    }
}
