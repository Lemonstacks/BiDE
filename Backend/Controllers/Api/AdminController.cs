using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;
using BiDE.DTOs;
using BiDE.Models;

namespace BiDE.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetAdminId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
        {
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

            var dto = new AdminDashboardDto
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                Pending = pending.Select(MapInstructorDto).ToList(),
                Approved = approved.Select(MapInstructorDto).ToList(),
                Rejected = rejected.Select(MapInstructorDto).ToList()
            };

            return Ok(dto);
        }

        // POST: api/admin/instructors/{instructorId}/approve
        [HttpPost("instructors/{instructorId}/approve")]
        public async Task<IActionResult> Approve(int instructorId)
        {
            var adminId = GetAdminId();
            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null)
                return NotFound(new { message = "Instructor not found." });

            instructor.Status = InstructorStatus.Approved;
            instructor.IsVerified = true;
            instructor.ApprovedByAdminId = adminId;
            instructor.ApprovalDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{instructor.FirstName} {instructor.LastName} approved." });
        }

        // POST: api/admin/instructors/{instructorId}/reject
        [HttpPost("instructors/{instructorId}/reject")]
        public async Task<IActionResult> Reject(int instructorId, [FromBody] RejectInstructorRequest request)
        {
            var adminId = GetAdminId();
            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null)
                return NotFound(new { message = "Instructor not found." });

            instructor.Status = InstructorStatus.Rejected;
            instructor.IsVerified = false;
            instructor.ApprovedByAdminId = adminId;
            instructor.ApprovalDate = DateTime.UtcNow;
            instructor.RejectionReason = request.Reason;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{instructor.FirstName} {instructor.LastName} rejected." });
        }

        // POST: api/admin/instructors/{instructorId}/suspend
        [HttpPost("instructors/{instructorId}/suspend")]
        public async Task<IActionResult> Suspend(int instructorId)
        {
            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null)
                return NotFound(new { message = "Instructor not found." });

            instructor.Status = InstructorStatus.Suspended;
            instructor.IsVerified = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{instructor.FirstName} {instructor.LastName} suspended." });
        }

        // POST: api/admin/instructors/{instructorId}/reinstate
        [HttpPost("instructors/{instructorId}/reinstate")]
        public async Task<IActionResult> Reinstate(int instructorId)
        {
            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null)
                return NotFound(new { message = "Instructor not found." });

            instructor.Status = InstructorStatus.Approved;
            instructor.IsVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{instructor.FirstName} {instructor.LastName} reinstated." });
        }

        private static InstructorApplicationDto MapInstructorDto(Instructor i) => new()
        {
            InstructorId = i.InstructorId,
            FirstName = i.FirstName,
            LastName = i.LastName,
            Email = i.Email,
            Contact = i.Contact,
            Suburb = i.Suburb,
            Certification = i.Certification,
            Experience = i.Experience,
            Status = i.Status.ToString(),
            ApplicationDate = i.ApplicationDate,
            ApprovalDate = i.ApprovalDate,
            RejectionReason = i.RejectionReason
        };
    }
}
