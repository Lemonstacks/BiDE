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
    public class InstructorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InstructorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/instructors?search=term&specialization=cert
        [HttpGet]
        public async Task<ActionResult<List<InstructorListDto>>> GetInstructors(
            [FromQuery] string? search, [FromQuery] string? specialization)
        {
            var query = _context.Instructors
                .Where(i => i.Status == InstructorStatus.Approved && i.IsVerified);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(i =>
                    i.FirstName.ToLower().Contains(term) ||
                    i.LastName.ToLower().Contains(term) ||
                    (i.Suburb != null && i.Suburb.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(specialization) && specialization != "all")
            {
                query = query.Where(i =>
                    i.Certification != null && i.Certification.Contains(specialization));
            }

            var instructors = await query
                .OrderByDescending(i => i.Experience)
                .Select(i => new InstructorListDto
                {
                    InstructorId = i.InstructorId,
                    FirstName = i.FirstName,
                    LastName = i.LastName,
                    Suburb = i.Suburb,
                    Certification = i.Certification,
                    Experience = i.Experience
                })
                .ToListAsync();

            return Ok(instructors);
        }

        // GET: api/instructors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InstructorDetailDto>> GetInstructor(int id)
        {
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.InstructorId == id
                    && i.Status == InstructorStatus.Approved && i.IsVerified);

            if (instructor == null)
                return NotFound(new { message = "Instructor not found." });

            var offerings = await _context.LessonOfferings
                .Where(o => o.InstructorId == id)
                .Select(o => new OfferingDto
                {
                    OfferId = o.OfferId,
                    Title = o.Title,
                    LessonType = o.LessonType,
                    Description = o.Description,
                    Price = o.Price
                })
                .ToListAsync();

            var availability = await _context.Availabilities
                .Where(a => a.InstructorId == id && a.AvailabilityStatus == "Available")
                .OrderBy(a => a.Date)
                .Select(a => new AvailabilityDto
                {
                    AvailabilityId = a.AvailabilityId,
                    Date = a.Date,
                    StartTime = a.StartTime.ToString(@"hh\:mm"),
                    EndTime = a.EndTime.ToString(@"hh\:mm"),
                    Status = a.AvailabilityStatus
                })
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.Student)
                .Where(r => r.Booking.InstructorId == id)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    StudentName = $"{r.Student.FirstName} {r.Student.LastName}",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate
                })
                .ToListAsync();

            var dto = new InstructorDetailDto
            {
                InstructorId = instructor.InstructorId,
                FirstName = instructor.FirstName,
                LastName = instructor.LastName,
                Contact = instructor.Contact,
                Email = instructor.Email,
                Suburb = instructor.Suburb,
                Certification = instructor.Certification,
                Experience = instructor.Experience,
                Offerings = offerings,
                Availability = availability,
                Reviews = reviews
            };

            return Ok(dto);
        }

        // POST: api/instructors/book
        [Authorize(Roles = "Student")]
        [HttpPost("book")]
        public async Task<ActionResult<BookingDto>> Book([FromBody] CreateBookingRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var offering = await _context.LessonOfferings.FindAsync(request.OfferId);
            var schedule = await _context.Availabilities.FindAsync(request.ScheduleId);

            if (offering == null || schedule == null ||
                offering.InstructorId != request.InstructorId ||
                schedule.InstructorId != request.InstructorId)
                return BadRequest(new { message = "Invalid offering or schedule selection." });

            if (schedule.AvailabilityStatus != "Available")
                return Conflict(new { message = "This time slot is no longer available." });

            var booking = new Booking
            {
                InstructorId = request.InstructorId,
                StudentId = userId,
                ScheduleId = request.ScheduleId,
                OfferId = request.OfferId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            schedule.AvailabilityStatus = "Booked";
            await _context.SaveChangesAsync();

            var instructor = await _context.Instructors.FindAsync(request.InstructorId);

            return Ok(new BookingDto
            {
                BookingId = booking.BookingId,
                InstructorName = $"{instructor!.FirstName} {instructor.LastName}",
                StudentName = "",
                LessonTitle = offering.Title,
                LessonType = offering.LessonType,
                ScheduleDate = schedule.Date,
                StartTime = schedule.StartTime.ToString(@"hh\:mm"),
                EndTime = schedule.EndTime.ToString(@"hh\:mm"),
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            });
        }
    }
}
