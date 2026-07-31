using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BiDE.Data;
using BiDE.DTOs;

namespace BiDE.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetRole() =>
            User.FindFirstValue(ClaimTypes.Role)!;

        // GET: api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var role = GetRole();

            if (role == "Student")
            {
                var student = await _context.Students.FindAsync(userId);
                if (student == null) return NotFound();
                return Ok(new StudentProfileDto
                {
                    StudentId = student.StudentId,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    Contact = student.Contact,
                    Suburb = student.Suburb,
                    CreatedAt = student.CreatedAt
                });
            }

            if (role == "Instructor")
            {
                var instructor = await _context.Instructors.FindAsync(userId);
                if (instructor == null) return NotFound();
                return Ok(new InstructorProfileDto
                {
                    InstructorId = instructor.InstructorId,
                    FirstName = instructor.FirstName,
                    LastName = instructor.LastName,
                    Email = instructor.Email,
                    Contact = instructor.Contact,
                    Suburb = instructor.Suburb,
                    Certification = instructor.Certification,
                    Experience = instructor.Experience,
                    Status = instructor.Status.ToString(),
                    IsVerified = instructor.IsVerified,
                    CreatedAt = instructor.CreatedAt
                });
            }

            return NotFound(new { message = "Profile not found." });
        }

        // PUT: api/profile/student
        [HttpPut("student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateStudentProfile(
            [FromBody] UpdateStudentProfileRequest request)
        {
            var userId = GetUserId();
            var student = await _context.Students.FindAsync(userId);
            if (student == null) return NotFound();

            student.FirstName = request.FirstName;
            student.LastName = request.LastName;
            student.Contact = request.Contact;
            student.Suburb = request.Suburb;
            await _context.SaveChangesAsync();

            return Ok(new StudentProfileDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                Contact = student.Contact,
                Suburb = student.Suburb,
                CreatedAt = student.CreatedAt
            });
        }

        // PUT: api/profile/instructor
        [HttpPut("instructor")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> UpdateInstructorProfile(
            [FromBody] UpdateInstructorProfileRequest request)
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FindAsync(userId);
            if (instructor == null) return NotFound();

            instructor.FirstName = request.FirstName;
            instructor.LastName = request.LastName;
            instructor.Contact = request.Contact;
            instructor.Suburb = request.Suburb;
            instructor.Certification = request.Certification;
            instructor.Experience = request.Experience;
            await _context.SaveChangesAsync();

            return Ok(new InstructorProfileDto
            {
                InstructorId = instructor.InstructorId,
                FirstName = instructor.FirstName,
                LastName = instructor.LastName,
                Email = instructor.Email,
                Contact = instructor.Contact,
                Suburb = instructor.Suburb,
                Certification = instructor.Certification,
                Experience = instructor.Experience,
                Status = instructor.Status.ToString(),
                IsVerified = instructor.IsVerified,
                CreatedAt = instructor.CreatedAt
            });
        }

        // DELETE: api/profile
        [HttpDelete]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteProfile()
        {
            var userId = GetUserId();
            var instructor = await _context.Instructors.FindAsync(userId);
            if (instructor == null) return NotFound();

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile deleted." });
        }
    }
}
