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

        // GET: /Admin/ViewPayments
        public async Task<IActionResult> ViewPayments(string? status, string? search)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var query = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Student)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.LessonOffering)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Instructor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(p => p.PaymentStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(p =>
                    p.Booking.Student.FirstName.ToLower().Contains(term) ||
                    p.Booking.Student.LastName.ToLower().Contains(term) ||
                    p.Booking.Student.Email.ToLower().Contains(term) ||
                    p.Booking.Instructor.FirstName.ToLower().Contains(term) ||
                    p.Booking.Instructor.LastName.ToLower().Contains(term));
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.StatusFilter = status ?? "all";
            ViewBag.SearchFilter = search ?? "";
            return View(payments);
        }

        // GET: /Admin/MonitorBookings
        public async Task<IActionResult> MonitorBookings(string? status, string? search)
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

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(b =>
                    b.Student.FirstName.ToLower().Contains(term) ||
                    b.Student.LastName.ToLower().Contains(term) ||
                    b.Instructor.FirstName.ToLower().Contains(term) ||
                    b.Instructor.LastName.ToLower().Contains(term) ||
                    (b.LessonOffering != null && b.LessonOffering.Title.ToLower().Contains(term)));
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status ?? "all";
            ViewBag.SearchFilter = search ?? "";
            return View(bookings);
        }

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers(string? role, string? search)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var students = await _context.Students.ToListAsync();
            var instructors = await _context.Instructors.ToListAsync();
            var admins = await _context.Admins.ToListAsync();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                students = students.Where(s =>
                    s.FirstName.ToLower().Contains(term) ||
                    s.LastName.ToLower().Contains(term) ||
                    s.Email.ToLower().Contains(term)).ToList();
                instructors = instructors.Where(i =>
                    i.FirstName.ToLower().Contains(term) ||
                    i.LastName.ToLower().Contains(term) ||
                    i.Email.ToLower().Contains(term)).ToList();
                admins = admins.Where(a =>
                    a.FirstName.ToLower().Contains(term) ||
                    a.LastName.ToLower().Contains(term) ||
                    a.Email.ToLower().Contains(term)).ToList();
            }

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(role) && role != "all")
            {
                if (role == "Student") { instructors = new List<Instructor>(); admins = new List<Admin>(); }
                else if (role == "Instructor") { students = new List<Student>(); admins = new List<Admin>(); }
                else if (role == "Admin") { students = new List<Student>(); instructors = new List<Instructor>(); }
            }

            ViewBag.Students = students;
            ViewBag.Instructors = instructors;
            ViewBag.Admins = admins;
            ViewBag.RoleFilter = role ?? "all";
            ViewBag.SearchFilter = search ?? "";
            return View();
        }

        // POST: /Admin/DeactivateStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateStudent(int studentId)
        {
            var adminId = GetAdminId();
            if (adminId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Check for active bookings
            var activeBookings = await _context.Bookings
                .AnyAsync(b => b.StudentId == studentId &&
                    (b.Status == "Pending" || b.Status == "Accepted"));
            if (activeBookings)
            {
                TempData["Error"] = $"Cannot remove {student.FirstName} {student.LastName} — they have active bookings.";
                return RedirectToAction("ManageUsers");
            }

            try
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{student.FirstName} {student.LastName} has been removed.";
            }
            catch (Exception)
            {
                TempData["Error"] = $"Unable to remove {student.FirstName} {student.LastName}. They may have existing bookings or reviews.";
            }

            return RedirectToAction("ManageUsers");
        }
    }
}
