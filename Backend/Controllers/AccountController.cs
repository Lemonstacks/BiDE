using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Helpers;
using BiDE.Models;

namespace BiDE.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please fill in all fields.";
                return View();
            }

            // Check Student
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);
            if (student != null && PasswordHasher.Verify(password, student.Password))
            {
                // Upgrade legacy plain-text password to BCrypt hash
                if (!PasswordHasher.IsBCryptHash(student.Password))
                {
                    student.Password = PasswordHasher.Hash(password);
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.SetInt32("UserId", student.StudentId);
                HttpContext.Session.SetString("UserRole", "Student");
                HttpContext.Session.SetString("UserName", $"{student.FirstName} {student.LastName}");
                HttpContext.Session.SetString("UserEmail", student.Email);
                return RedirectToAction("Index", "Home");
            }

            // Check Instructor
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Email == email);
            if (instructor != null && PasswordHasher.Verify(password, instructor.Password))
            {
                // Upgrade legacy plain-text password to BCrypt hash
                if (!PasswordHasher.IsBCryptHash(instructor.Password))
                {
                    instructor.Password = PasswordHasher.Hash(password);
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.SetInt32("UserId", instructor.InstructorId);
                HttpContext.Session.SetString("UserRole", "Instructor");
                HttpContext.Session.SetString("UserName", $"{instructor.FirstName} {instructor.LastName}");
                HttpContext.Session.SetString("UserEmail", instructor.Email);
                return RedirectToAction("Index", "InstructorDashboard");
            }

            // Check Admin
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null && PasswordHasher.Verify(password, admin.Password))
            {
                // Upgrade legacy plain-text password to BCrypt hash
                if (!PasswordHasher.IsBCryptHash(admin.Password))
                {
                    admin.Password = PasswordHasher.Hash(password);
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.SetInt32("UserId", admin.AdminId);
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", $"{admin.FirstName} {admin.LastName}");
                HttpContext.Session.SetString("UserEmail", admin.Email);
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.Error = "Invalid email or password.";
            ViewBag.Email = email;
            return View();
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string firstName, string lastName, string email,
            string contact, string password, string confirmPassword,
            string role, string? suburb)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(contact))
            {
                ViewBag.Error = "Please fill in all required fields.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (password.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters.";
                return View();
            }

            if (role != "Student" && role != "Instructor")
            {
                ViewBag.Error = "Please select a valid role.";
                return View();
            }

            // Check if email already exists
            var existingStudent = await _context.Students.AnyAsync(s => s.Email == email);
            var existingInstructor = await _context.Instructors.AnyAsync(i => i.Email == email);
            var existingAdmin = await _context.Admins.AnyAsync(a => a.Email == email);

            if (existingStudent || existingInstructor || existingAdmin)
            {
                ViewBag.Error = "An account with this email already exists.";
                return View();
            }

            if (role == "Student")
            {
                var student = new Student
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Contact = contact,
                    Password = PasswordHasher.Hash(password),
                    Suburb = suburb,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Auto-login
                HttpContext.Session.SetInt32("UserId", student.StudentId);
                HttpContext.Session.SetString("UserRole", "Student");
                HttpContext.Session.SetString("UserName", $"{student.FirstName} {student.LastName}");
                HttpContext.Session.SetString("UserEmail", student.Email);
                return RedirectToAction("Index", "Home");
            }
            else // Instructor
            {
                var instructor = new Instructor
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Contact = contact,
                    Password = PasswordHasher.Hash(password),
                    Suburb = suburb,
                    Status = InstructorStatus.Pending,
                    IsVerified = false,
                    ApplicationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();

                // Auto-login
                HttpContext.Session.SetInt32("UserId", instructor.InstructorId);
                HttpContext.Session.SetString("UserRole", "Instructor");
                HttpContext.Session.SetString("UserName", $"{instructor.FirstName} {instructor.LastName}");
                HttpContext.Session.SetString("UserEmail", instructor.Email);
                return RedirectToAction("Index", "InstructorDashboard");
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
