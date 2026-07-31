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
    [Route("api/instructor-dashboard")]
    [Authorize(Roles = "Instructor")]
    public class InstructorDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InstructorDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetInstructorId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/instructor-dashboard
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var instructorId = GetInstructorId();

            var bookings = await _context.Bookings
                .Include(b => b.Student)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Where(b => b.InstructorId == instructorId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var pending = bookings.Where(b => b.Status == "Pending").Select(MapBookingDto).ToList();
            var accepted = bookings.Where(b => b.Status == "Accepted").Select(MapBookingDto).ToList();
            var completed = bookings.Where(b => b.Status == "Completed").Select(MapBookingDto).ToList();

            return Ok(new
            {
                totalBookings = bookings.Count,
                pending,
                accepted,
                completed
            });
        }

        // POST: api/instructor-dashboard/bookings/{bookingId}/accept
        [HttpPost("bookings/{bookingId}/accept")]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            var instructorId = GetInstructorId();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId);
            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            booking.Status = "Accepted";
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking accepted." });
        }

        // POST: api/instructor-dashboard/bookings/{bookingId}/reject
        [HttpPost("bookings/{bookingId}/reject")]
        public async Task<IActionResult> RejectBooking(int bookingId, [FromBody] RejectBookingRequest request)
        {
            var instructorId = GetInstructorId();

            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId);
            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            booking.Status = "Rejected";
            booking.CancellationReason = request.Reason;
            booking.CancelledBy = "Instructor";
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.Schedule != null)
                booking.Schedule.AvailabilityStatus = "Available";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking rejected." });
        }

        // POST: api/instructor-dashboard/bookings/{bookingId}/complete
        [HttpPost("bookings/{bookingId}/complete")]
        public async Task<IActionResult> CompleteBooking(int bookingId)
        {
            var instructorId = GetInstructorId();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId);
            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            if (booking.Status != "Accepted")
                return BadRequest(new { message = "Only accepted bookings can be marked as completed." });

            booking.Status = "Completed";
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lesson marked as completed." });
        }

        // --- Availability ---

        // GET: api/instructor-dashboard/availability
        [HttpGet("availability")]
        public async Task<ActionResult<List<AvailabilityDto>>> GetAvailability()
        {
            var instructorId = GetInstructorId();

            var slots = await _context.Availabilities
                .Where(a => a.InstructorId == instructorId)
                .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
                .Select(a => new AvailabilityDto
                {
                    AvailabilityId = a.AvailabilityId,
                    Date = a.Date,
                    StartTime = a.StartTime.ToString(@"hh\:mm"),
                    EndTime = a.EndTime.ToString(@"hh\:mm"),
                    Status = a.AvailabilityStatus
                })
                .ToListAsync();

            return Ok(slots);
        }

        // POST: api/instructor-dashboard/availability
        [HttpPost("availability")]
        public async Task<IActionResult> AddAvailability([FromBody] AddAvailabilityRequest request)
        {
            var instructorId = GetInstructorId();

            if (request.Date.Date < DateTime.UtcNow.Date)
                return BadRequest(new { message = "Cannot add availability for a past date." });

            if (!TimeSpan.TryParse(request.StartTime, out var startTime) ||
                !TimeSpan.TryParse(request.EndTime, out var endTime))
                return BadRequest(new { message = "Invalid time format. Use HH:mm." });

            if (endTime <= startTime)
                return BadRequest(new { message = "End time must be after start time." });

            var slot = new Availability
            {
                InstructorId = instructorId,
                Date = request.Date,
                StartTime = startTime,
                EndTime = endTime,
                AvailabilityStatus = "Available",
                CreatedAt = DateTime.UtcNow
            };

            _context.Availabilities.Add(slot);
            await _context.SaveChangesAsync();

            return Ok(new AvailabilityDto
            {
                AvailabilityId = slot.AvailabilityId,
                Date = slot.Date,
                StartTime = slot.StartTime.ToString(@"hh\:mm"),
                EndTime = slot.EndTime.ToString(@"hh\:mm"),
                Status = slot.AvailabilityStatus
            });
        }

        // DELETE: api/instructor-dashboard/availability/{id}
        [HttpDelete("availability/{id}")]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            var instructorId = GetInstructorId();

            var slot = await _context.Availabilities
                .FirstOrDefaultAsync(a => a.AvailabilityId == id && a.InstructorId == instructorId);

            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            if (slot.AvailabilityStatus == "Booked")
                return BadRequest(new { message = "Cannot delete a slot that is already booked." });

            _context.Availabilities.Remove(slot);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Slot removed." });
        }

        // --- Offerings ---

        // GET: api/instructor-dashboard/offerings
        [HttpGet("offerings")]
        public async Task<ActionResult<List<OfferingDto>>> GetOfferings()
        {
            var instructorId = GetInstructorId();

            var offerings = await _context.LessonOfferings
                .Where(o => o.InstructorId == instructorId)
                .Select(o => new OfferingDto
                {
                    OfferId = o.OfferId,
                    Title = o.Title,
                    LessonType = o.LessonType,
                    Description = o.Description,
                    Price = o.Price
                })
                .ToListAsync();

            return Ok(offerings);
        }

        // POST: api/instructor-dashboard/offerings
        [HttpPost("offerings")]
        public async Task<IActionResult> CreateOffering([FromBody] CreateOfferingRequest request)
        {
            var instructorId = GetInstructorId();

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.LessonType))
                return BadRequest(new { message = "Title and lesson type are required." });

            if (request.Price <= 0)
                return BadRequest(new { message = "Price must be greater than zero." });

            var offering = new LessonOffering
            {
                InstructorId = instructorId,
                Title = request.Title,
                LessonType = request.LessonType,
                Description = request.Description,
                Price = request.Price,
                CreatedAt = DateTime.UtcNow
            };

            _context.LessonOfferings.Add(offering);
            await _context.SaveChangesAsync();

            return Ok(new OfferingDto
            {
                OfferId = offering.OfferId,
                Title = offering.Title,
                LessonType = offering.LessonType,
                Description = offering.Description,
                Price = offering.Price
            });
        }

        // PUT: api/instructor-dashboard/offerings/{offerId}
        [HttpPut("offerings/{offerId}")]
        public async Task<IActionResult> UpdateOffering(int offerId, [FromBody] UpdateOfferingRequest request)
        {
            var instructorId = GetInstructorId();

            var offering = await _context.LessonOfferings
                .FirstOrDefaultAsync(o => o.OfferId == offerId && o.InstructorId == instructorId);
            if (offering == null)
                return NotFound(new { message = "Offering not found." });

            offering.Title = request.Title;
            offering.LessonType = request.LessonType;
            offering.Description = request.Description;
            offering.Price = request.Price;
            await _context.SaveChangesAsync();

            return Ok(new OfferingDto
            {
                OfferId = offering.OfferId,
                Title = offering.Title,
                LessonType = offering.LessonType,
                Description = offering.Description,
                Price = offering.Price
            });
        }

        // DELETE: api/instructor-dashboard/offerings/{offerId}
        [HttpDelete("offerings/{offerId}")]
        public async Task<IActionResult> DeleteOffering(int offerId)
        {
            var instructorId = GetInstructorId();

            var offering = await _context.LessonOfferings
                .FirstOrDefaultAsync(o => o.OfferId == offerId && o.InstructorId == instructorId);

            if (offering == null)
                return NotFound(new { message = "Offering not found." });

            _context.LessonOfferings.Remove(offering);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Offering deleted." });
        }

        // --- Lesson Progress ---

        // GET: api/instructor-dashboard/progress
        [HttpGet("progress")]
        public async Task<IActionResult> GetLessonProgress()
        {
            var instructorId = GetInstructorId();

            var bookings = await _context.Bookings
                .Include(b => b.Student)
                .Include(b => b.LessonOffering)
                .Include(b => b.Schedule)
                .Include(b => b.LessonProgresses)
                .Where(b => b.InstructorId == instructorId && b.Status == "Accepted")
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var result = bookings.Select(MapBookingDto).ToList();
            return Ok(result);
        }

        // POST: api/instructor-dashboard/bookings/{bookingId}/progress
        [HttpPost("bookings/{bookingId}/progress")]
        public async Task<IActionResult> AddProgress(int bookingId, [FromBody] AddProgressRequest request)
        {
            var instructorId = GetInstructorId();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.InstructorId == instructorId);
            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            var progress = new LessonProgress
            {
                BookingId = bookingId,
                ProgressDate = DateTime.UtcNow,
                Duration = request.Duration,
                StartTime = TimeSpan.Zero,
                CompletionStatus = request.CompletionStatus,
                Feedback = request.Feedback
            };

            _context.LessonProgresses.Add(progress);
            await _context.SaveChangesAsync();

            return Ok(new ProgressDto
            {
                ProgressId = progress.ProgressId,
                ProgressDate = progress.ProgressDate,
                Duration = progress.Duration,
                CompletionStatus = progress.CompletionStatus,
                Feedback = progress.Feedback
            });
        }

        private static BookingDto MapBookingDto(Booking b) => new()
        {
            BookingId = b.BookingId,
            InstructorName = "",
            StudentName = $"{b.Student.FirstName} {b.Student.LastName}",
            LessonTitle = b.LessonOffering.Title,
            LessonType = b.LessonOffering.LessonType,
            ScheduleDate = b.Schedule.Date,
            StartTime = b.Schedule.StartTime.ToString(@"hh\:mm"),
            EndTime = b.Schedule.EndTime.ToString(@"hh\:mm"),
            Status = b.Status,
            CreatedAt = b.CreatedAt,
            CancellationReason = b.CancellationReason,
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
