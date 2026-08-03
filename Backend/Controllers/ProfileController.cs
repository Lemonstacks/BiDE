using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (role == "Student")
            {
                var student = await _context.Students.FindAsync(userId.Value);
                if (student == null) return RedirectToAction("Login", "Account");
                return View("StudentProfile", student);
            }
            else if (role == "Instructor")
            {
                var instructor = await _context.Instructors.FindAsync(userId.Value);
                if (instructor == null) return RedirectToAction("Login", "Account");
                return View("InstructorProfile", instructor);
            }
            else if (role == "Admin")
            {
                var admin = await _context.Admins.FindAsync(userId.Value);
                if (admin == null) return RedirectToAction("Login", "Account");
                return View("AdminProfile", admin);
            }

            return RedirectToAction("Login", "Account");
        }

        // POST: /Profile/UpdateStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStudent(string firstName, string lastName, string contact, string? suburb)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FindAsync(userId.Value);
            if (student == null) return NotFound();

            student.FirstName = firstName;
            student.LastName = lastName;
            student.Contact = contact;
            student.Suburb = suburb;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", $"{student.FirstName} {student.LastName}");
            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Index");
        }

        // POST: /Profile/UpdateInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInstructor(
            string firstName, string lastName, string contact,
            string? suburb, string? certification, int experience)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(userId.Value);
            if (instructor == null) return NotFound();

            instructor.FirstName = firstName;
            instructor.LastName = lastName;
            instructor.Contact = contact;
            instructor.Suburb = suburb;
            instructor.Certification = certification;
            instructor.Experience = experience;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", $"{instructor.FirstName} {instructor.LastName}");
            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Index");
        }

        // POST: /Profile/DeleteInstructorProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInstructorProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null || role != "Instructor")
                return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(userId.Value);
            if (instructor == null) return NotFound();

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();
            TempData["Success"] = "Instructor profile deleted.";
            return RedirectToAction("Login", "Account");
        }
    }
}
