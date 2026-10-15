using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;
using BiDE.ViewModels;

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
        private async Task SignInUser(int userId, string role, string fullName, string email, string? pictureUrl = null)
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
            if (!string.IsNullOrEmpty(pictureUrl))
                HttpContext.Session.SetString("UserPicture", pictureUrl);
            else
                HttpContext.Session.Remove("UserPicture");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? redirect)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View(new LoginViewModel { Redirect = redirect });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email;
            var password = model.Password;

            // Check Student
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);
            if (student != null && password == student.Password)
            {
                await SignInUser(student.StudentId, "Student", $"{student.FirstName} {student.LastName}", student.Email, student.ProfilePicture);
                // Always take student to Find Instructors after login
                return RedirectToAction("Index", "Instructors");
            }

            // Check Instructor
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Email == email);
            if (instructor != null && password == instructor.Password)
            {
                await SignInUser(instructor.InstructorId, "Instructor", $"{instructor.FirstName} {instructor.LastName}", instructor.Email, instructor.ProfilePicture);
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

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            model.Password = string.Empty;
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var firstName = model.FirstName;
            var lastName = model.LastName;
            var email = model.Email;
            var contact = model.Contact;
            var password = model.Password;
            var role = model.Role;
            var suburb = model.Suburb;
            var securityQuestion = model.SecurityQuestion;
            var securityAnswer = model.SecurityAnswer;

            // Server-only rule: email must be unique across all account types
            var existingStudent = await _context.Students.AnyAsync(s => s.Email == email);
            var existingInstructor = await _context.Instructors.AnyAsync(i => i.Email == email);
            var existingAdmin = await _context.Admins.AnyAsync(a => a.Email == email);

            if (existingStudent || existingInstructor || existingAdmin)
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "An account with this email already exists.");
                return View(model);
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
            return View("ResetPassword", new ResetPasswordViewModel { Email = email, Question = question });
        }

        // POST: /Account/ResetPassword  (step 2: verify answer -> set new password)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var email = model.Email;
            var (found, question) = await LookupSecurityQuestion(email);
            if (!found)
            {
                ViewBag.Error = "No account found with that email address.";
                return View("ForgotPassword");
            }

            model.Question = question;

            if (!ModelState.IsValid)
                return View("ResetPassword", model);

            var newPassword = model.NewPassword;
            var normalizedAnswer = model.Answer.Trim().ToLower();

            // Find the account and verify the answer
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student != null)
            {
                if (student.SecurityAnswer != normalizedAnswer)
                    return ResetError(model, nameof(ResetPasswordViewModel.Answer), "Incorrect answer to the security question.");
                if (student.Password == newPassword)
                    return ResetError(model, nameof(ResetPasswordViewModel.NewPassword), "Your new password cannot be the same as your old password.");
                student.Password = newPassword;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Email == email);
            if (instructor != null)
            {
                if (instructor.SecurityAnswer != normalizedAnswer)
                    return ResetError(model, nameof(ResetPasswordViewModel.Answer), "Incorrect answer to the security question.");
                if (instructor.Password == newPassword)
                    return ResetError(model, nameof(ResetPasswordViewModel.NewPassword), "Your new password cannot be the same as your old password.");
                instructor.Password = newPassword;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null)
            {
                if (admin.SecurityAnswer != normalizedAnswer)
                    return ResetError(model, nameof(ResetPasswordViewModel.Answer), "Incorrect answer to the security question.");
                if (admin.Password == newPassword)
                    return ResetError(model, nameof(ResetPasswordViewModel.NewPassword), "Your new password cannot be the same as your old password.");
                admin.Password = newPassword;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "No account found with that email address.";
            return View("ForgotPassword");
        }

        private IActionResult ResetError(ResetPasswordViewModel model, string field, string message)
        {
            ModelState.AddModelError(field, message);
            return View("ResetPassword", model);
        }
    }
}