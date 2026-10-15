using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.Models;
using BiDE.ViewModels;

namespace BiDE.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                return View("StudentProfile", ToStudentViewModel(student));
            }
            else if (role == "Instructor")
            {
                var instructor = await _context.Instructors.FindAsync(userId.Value);
                if (instructor == null) return RedirectToAction("Login", "Account");
                return View("InstructorProfile", ToInstructorViewModel(instructor));
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
        public async Task<IActionResult> UpdateStudent(StudentProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var student = await _context.Students.FindAsync(userId.Value);
            if (student == null) return NotFound();

            // Server-only rules: file type and size
            ValidateProfilePicture(model.ProfilePicture);

            if (!ModelState.IsValid)
                return View("StudentProfile", WithDisplayValues(model, student));

            var firstName = model.FirstName;
            var lastName = model.LastName;
            var contact = model.Contact;
            var suburb = model.Suburb;
            var profilePicture = model.ProfilePicture;
            student.FirstName = firstName;
            student.LastName = lastName;
            student.Contact = contact;
            student.Suburb = suburb;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"student_{userId.Value}_{Path.GetRandomFileName()}{Path.GetExtension(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Delete old picture if exists
                if (!string.IsNullOrEmpty(student.ProfilePicture))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, student.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                student.ProfilePicture = $"/uploads/profiles/{fileName}";
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", $"{student.FirstName} {student.LastName}");
            if (!string.IsNullOrEmpty(student.ProfilePicture))
                HttpContext.Session.SetString("UserPicture", student.ProfilePicture);
            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Index");
        }

        // POST: /Profile/UpdateInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInstructor(InstructorProfileViewModel model)
        {
            // Note: Experience is intentionally not accepted here. It is a verified
            // credential managed by an administrator, so instructors cannot self-edit it.
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var instructor = await _context.Instructors.FindAsync(userId.Value);
            if (instructor == null) return NotFound();

            // Server-only rules: file type and size
            ValidateProfilePicture(model.ProfilePicture);

            if (!ModelState.IsValid)
                return View("InstructorProfile", WithDisplayValues(model, instructor));

            var firstName = model.FirstName;
            var lastName = model.LastName;
            var contact = model.Contact;
            var suburb = model.Suburb;
            var certification = model.Certification;
            var profilePicture = model.ProfilePicture;
            instructor.FirstName = firstName;
            instructor.LastName = lastName;
            instructor.Contact = contact;
            instructor.Suburb = suburb;
            instructor.Certification = certification;
            // Experience is admin-managed and deliberately not updated here.

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"instructor_{userId.Value}_{Path.GetRandomFileName()}{Path.GetExtension(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Delete old picture if exists
                if (!string.IsNullOrEmpty(instructor.ProfilePicture))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, instructor.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                instructor.ProfilePicture = $"/uploads/profiles/{fileName}";
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", $"{instructor.FirstName} {instructor.LastName}");
            if (!string.IsNullOrEmpty(instructor.ProfilePicture))
                HttpContext.Session.SetString("UserPicture", instructor.ProfilePicture);
            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Index");
        }

        // Form field name shared by both profile view models.
        private const string PictureField = "ProfilePicture";

        // Adds a ModelState error for a bad profile picture (type or size).
        private void ValidateProfilePicture(IFormFile? profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0) return;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(profilePicture.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(PictureField, "Profile picture must be a JPG, PNG, or GIF image.");
                return;
            }
            if (profilePicture.Length > 3 * 1024 * 1024)
                ModelState.AddModelError(PictureField, "Profile picture must be less than 3MB.");
        }

        private static string InitialsOf(string first, string last) =>
            $"{(first.Length > 0 ? first[0] : ' ')}{(last.Length > 0 ? last[0] : ' ')}".Trim();

        private static StudentProfileViewModel ToStudentViewModel(Student s) => new()
        {
            FirstName = s.FirstName,
            LastName = s.LastName,
            Contact = s.Contact,
            Suburb = s.Suburb,
            Email = s.Email,
            CurrentPictureUrl = s.ProfilePicture,
            Initials = InitialsOf(s.FirstName, s.LastName)
        };

        private static InstructorProfileViewModel ToInstructorViewModel(Instructor i) => new()
        {
            FirstName = i.FirstName,
            LastName = i.LastName,
            Contact = i.Contact,
            Suburb = i.Suburb,
            Certification = i.Certification,
            Email = i.Email,
            Experience = i.Experience,
            Status = i.Status,
            RejectionReason = i.RejectionReason,
            CurrentPictureUrl = i.ProfilePicture,
            Initials = InitialsOf(i.FirstName, i.LastName)
        };

        // Re-fills the read-only display fields so the form can be re-rendered with the user's input.
        private static StudentProfileViewModel WithDisplayValues(StudentProfileViewModel model, Student s)
        {
            model.Email = s.Email;
            model.CurrentPictureUrl = s.ProfilePicture;
            model.Initials = InitialsOf(s.FirstName, s.LastName);
            return model;
        }

        private static InstructorProfileViewModel WithDisplayValues(InstructorProfileViewModel model, Instructor i)
        {
            model.Email = i.Email;
            model.Experience = i.Experience;
            model.Status = i.Status;
            model.RejectionReason = i.RejectionReason;
            model.CurrentPictureUrl = i.ProfilePicture;
            model.Initials = InitialsOf(i.FirstName, i.LastName);
            return model;
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

            // Check for active bookings
            var activeBookings = await _context.Bookings
                .AnyAsync(b => b.InstructorId == userId.Value &&
                    (b.Status == "Pending" || b.Status == "Accepted"));
            if (activeBookings)
            {
                TempData["Error"] = "Cannot delete profile while you have active bookings. Please complete or reject them first.";
                return RedirectToAction("Index");
            }

            try
            {
                var availabilities = _context.Availabilities.Where(a => a.InstructorId == userId.Value);
                _context.Availabilities.RemoveRange(availabilities);

                var offerings = _context.LessonOfferings.Where(o => o.InstructorId == userId.Value);
                _context.LessonOfferings.RemoveRange(offerings);

                _context.Instructors.Remove(instructor);
                await _context.SaveChangesAsync();

                HttpContext.Session.Clear();
                TempData["Success"] = "Instructor profile deleted.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to delete profile. Please contact support.";
                return RedirectToAction("Index");
            }
        }
    }
}
