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
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetStudentId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/student/bookings
        [HttpGet("bookings")]
        public async Task<ActionResult<object>> GetBookings()
        {
            var studentId = GetStudentId();

            var bookings = await _context.Bookings
                .Include(b => b.Instructor)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.Review)
                .Include(b => b.LessonProgresses)
                .Where(b => b.StudentId == studentId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var active = bookings
                .Where(b => b.Status == "Pending" || b.Status == "Accepted")
                .Select(MapBookingDto).ToList();

            var past = bookings
                .Where(b => b.Status == "Completed" || b.Status == "Cancelled" || b.Status == "Rejected")
                .Select(MapBookingDto).ToList();

            return Ok(new { active, past });
        }

        // POST: api/student/bookings/{bookingId}/cancel
        [HttpPost("bookings/{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId, [FromBody] CancelBookingRequest request)
        {
            var studentId = GetStudentId();

            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.StudentId == studentId);

            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            if (booking.Status != "Pending" && booking.Status != "Accepted")
                return BadRequest(new { message = "This booking can no longer be cancelled." });

            booking.Status = "Cancelled";
            booking.CancellationReason = request.Reason;
            booking.CancelledBy = "Student";
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.Schedule != null)
                booking.Schedule.AvailabilityStatus = "Available";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking cancelled successfully." });
        }

        // POST: api/student/bookings/{bookingId}/review
        [HttpPost("bookings/{bookingId}/review")]
        public async Task<IActionResult> LeaveReview(int bookingId, [FromBody] LeaveReviewRequest request)
        {
            var studentId = GetStudentId();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.StudentId == studentId);

            if (booking == null || booking.Status != "Completed")
                return NotFound(new { message = "Completed booking not found." });

            var existing = await _context.Reviews.AnyAsync(r => r.BookingId == bookingId);
            if (existing)
                return Conflict(new { message = "You have already reviewed this lesson." });

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            var review = new Review
            {
                BookingId = bookingId,
                StudentId = studentId,
                Rating = request.Rating,
                Comment = request.Comment,
                ReviewDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review submitted successfully." });
        }

        private static BookingDto MapBookingDto(Booking b) => new()
        {
            BookingId = b.BookingId,
            InstructorName = $"{b.Instructor.FirstName} {b.Instructor.LastName}",
            StudentName = "",
            LessonTitle = b.LessonOffering.Title,
            LessonType = b.LessonOffering.LessonType,
            ScheduleDate = b.Schedule.Date,
            StartTime = b.Schedule.StartTime.ToString(@"hh\:mm"),
            EndTime = b.Schedule.EndTime.ToString(@"hh\:mm"),
            Status = b.Status,
            CreatedAt = b.CreatedAt,
            CancellationReason = b.CancellationReason,
            Review = b.Review != null ? new ReviewDto
            {
                ReviewId = b.Review.ReviewId,
                StudentName = "",
                Rating = b.Review.Rating,
                Comment = b.Review.Comment,
                ReviewDate = b.Review.ReviewDate
            } : null,
            Progress = b.LessonProgresses.Select(p => new ProgressDto
            {
                ProgressId = p.ProgressId,
                ProgressDate = p.ProgressDate,
                Duration = p.Duration,
                CompletionStatus = p.CompletionStatus,
                Feedback = p.Feedback
            }).ToList()
        };
    }
}
