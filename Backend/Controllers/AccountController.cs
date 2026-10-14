using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
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

        // Signs the user in with a claims cookie AND mirrors values into session
        // (existing controllers/_Layout still read Session for lightweight UI state).
        private async Task SignInUser(int userId, string role, string fullName, string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserName", fullName);
            HttpContext.Session.SetString("UserEmail", email);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? redirect)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            ViewBag.Redirect = redirect;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? redirect)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please fill in all fields.";
                return View();
            }

            // Check Student
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);
            if (student != null && password == student.Password)
            {
                await SignInUser(student.StudentId, "Student", $"{student.FirstName} {student.LastName}", student.Email);
                // Always take student to Find Instructors after login
                return RedirectToAction("Index", "Instructors");
            }

            // Check Instructor
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Email == email);
            if (instructor != null && password == instructor.Password)
            {
                await SignInUser(instructor.InstructorId, "Instructor", $"{instructor.FirstName} {instructor.LastName}", instructor.Email);
                return RedirectToAction("Index", "InstructorDashboard");
            }

            // Check Admin
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null && password == admin.Password)
            {
                await SignInUser(admin.AdminId, "Admin", $"{admin.FirstName} {admin.LastName}", admin.Email);
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.Error = "Invalid email or password.";
            ViewBag.Email = email;
            ViewBag.Redirect = redirect;
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
            string role, string? suburb, string securityQuestion, string securityAnswer)
        {
            // Preserve entered values so the form isn't cleared on validation error
            void PreserveInput()
            {
                ViewBag.FirstName = firstName;
                ViewBag.LastName = lastName;
                ViewBag.Email = email;
                ViewBag.Contact = contact;
                ViewBag.Suburb = suburb;
                ViewBag.Role = role;
                ViewBag.SecurityQuestion = securityQuestion;
            }

            // Validation
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(contact))
            {
                ViewBag.Error = "Please fill in all required fields.";
                PreserveInput();
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                PreserveInput();
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                PreserveInput();
                return View();
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                ViewBag.Error = "Please enter a valid email address.";
                PreserveInput();
                return View();
            }

            if (role != "Student" && role != "Instructor")
            {
                ViewBag.Error = "Please select a valid role.";
                PreserveInput();
                return View();
            }

            if (string.IsNullOrWhiteSpace(securityQuestion) || string.IsNullOrWhiteSpace(securityAnswer))
            {
                ViewBag.Error = "Please choose a security question and provide an answer.";
                PreserveInput();
                return View();
            }

            // Check if email already exists
            var existingStudent = await _context.Students.AnyAsync(s => s.Email == email);
            var existingInstructor = await _context.Instructors.AnyAsync(i => i.Email == email);
            var existingAdmin = await _context.Admins.AnyAsync(a => a.Email == email);

            if (existingStudent || existingInstructor || existingAdmin)
            {
                ViewBag.Error = "An account with this email already exists.";
                PreserveInput();
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
                    Password = password,
                    Suburb = suburb,
                    SecurityQuestion = securityQuestion,
                    SecurityAnswer = securityAnswer.Trim().ToLower(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Auto-login
                await SignInUser(student.StudentId, "Student", $"{student.FirstName} {student.LastName}", student.Email);
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
                    Password = password,
                    Suburb = suburb,
                    SecurityQuestion = securityQuestion,
                    SecurityAnswer = securityAnswer.Trim().ToLower(),
                    Status = InstructorStatus.Pending,
                    IsVerified = false,
                    ApplicationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();

                // Auto-login
                await SignInUser(instructor.InstructorId, "Instructor", $"{instructor.FirstName} {instructor.LastName}", instructor.Email);
                return RedirectToAction("Index", "InstructorDashboard");
            }
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Looks up a user's security question across all three account types.
        private async Task<(bool found, string? question)> LookupSecurityQuestion(string email)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student != null) return (true, student.SecurityQuestion);

            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Email == email);
            if (instructor != null) return (true, instructor.SecurityQuestion);

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null) return (true, admin.SecurityQuestion);

            return (false, null);
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword  (step 1: email -> show security question)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            var (found, question) = await LookupSecurityQuestion(email);

            if (!found)
            {
                ViewBag.Error = "No account found with that email address.";
                ViewBag.Email = email;
                return View();
            }

            if (string.IsNullOrEmpty(question))
            {
                ViewBag.Error = "This account has no security question set. Please contact an administrator to reset your password.";
                ViewBag.Email = email;
                return View();
            }

            // Move to step 2
            ViewBag.Email = email;
            ViewBag.Question = question;
            return View("ResetPassword");
        }

        // POST: /Account/ResetPassword  (step 2: verify answer -> set new password)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string answer, string newPassword, string confirmPassword)
        {
            var (found, question) = await LookupSecurityQuestion(email);
            if (!found)
            {
                ViewBag.Error = "No account found with that email address.";
                return View("ForgotPassword");
            }

            ViewBag.Email = email;
            ViewBag.Question = question;

            if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = "Please answer the question and enter a new password.";
                return View("ResetPassword");
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View("ResetPassword");
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View("ResetPassword");
            }

            var normalizedAnswer = answer.Trim().ToLower();

            // Find the account and verify the answer
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student != null)
            {
                if (student.SecurityAnswer != normalizedAnswer)
                {
                    ViewBag.Error = "Incorrect answer to the security question.";
                    return View("ResetPassword");
                }
                student.Password = newPassword;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Email == email);
            if (instructor != null)
            {
                if (instructor.SecurityAnswer != normalizedAnswer)
                {
                    ViewBag.Error = "Incorrect answer to the security question.";
                    return View("ResetPassword");
                }
                instructor.Password = newPassword;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null)
            {
                if (admin.SecurityAnswer != normalizedAnswer)
                {
                    ViewBag.Error = "Incorrect answer to the security question.";
                    return View("ResetPassword");
                }
                admin.Password = newPassword;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "No account found with that email address.";
            return View("ForgotPassword");
        }
    }
}
