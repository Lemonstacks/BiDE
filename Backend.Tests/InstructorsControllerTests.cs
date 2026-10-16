using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Tests
{
    public class InstructorsControllerTests
    {
        private static Instructor ApprovedInstructor(int id = 1) => new Instructor
        {
            InstructorId = id,
            FirstName = "Test",
            LastName = "Inst",
            Email = $"inst{id}@test.com",
            Contact = "0711111111",
            Password = "pass",
            Status = InstructorStatus.Approved,
            IsVerified = true
        };

        // UC06_S Make Booking Request -> InstructorsController.Book
        [Fact]
        public async Task UC06_Book_ValidSlot_CreatesPendingBooking()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorsController(db);
            TestHelper.SetupSession(controller, 5, "Student");

            db.Instructors.Add(ApprovedInstructor(1));
            db.LessonOfferings.Add(new LessonOffering { OfferId = 10, InstructorId = 1, Title = "Highway", LessonType = "Manual", Price = 350 });
            db.Availabilities.Add(new Availability { AvailabilityId = 20, InstructorId = 1, Date = DateTime.Today.AddDays(1), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = AvailabilityStatus.Available });
            await db.SaveChangesAsync();

            var result = await controller.Book(1, 10, 20);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("Student", redirect.ControllerName);
            Assert.Equal("Booking submitted successfully! The instructor will review your request.", controller.TempData["Success"]);

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.ScheduleId == 20);
            Assert.NotNull(booking);
            Assert.Equal(BookingStatus.Pending, booking!.Status);
            Assert.Equal(5, booking.StudentId);
            Assert.Equal(350, booking.AgreedPrice);

            var slot = await db.Availabilities.FindAsync(20);
            Assert.Equal(AvailabilityStatus.Booked, slot!.AvailabilityStatus);
        }

        // UC06_S: slot not Available -> error, no booking
        [Fact]
        public async Task UC06_Book_SlotNotAvailable_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorsController(db);
            TestHelper.SetupSession(controller, 5, "Student");

            db.Instructors.Add(ApprovedInstructor(1));
            db.LessonOfferings.Add(new LessonOffering { OfferId = 10, InstructorId = 1, Title = "Highway", LessonType = "Manual", Price = 350 });
            db.Availabilities.Add(new Availability { AvailabilityId = 20, InstructorId = 1, Date = DateTime.Today.AddDays(1), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = AvailabilityStatus.Booked });
            await db.SaveChangesAsync();

            var result = await controller.Book(1, 10, 20);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Detail", redirect.ActionName);
            Assert.Equal("This time slot is no longer available. Please choose another.", controller.TempData["Error"]);
            Assert.Equal(0, await db.Bookings.CountAsync());
        }

        // UC06_S: duplicate booking for the same slot -> error
        [Fact]
        public async Task UC06_Book_DuplicateSlot_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorsController(db);
            TestHelper.SetupSession(controller, 5, "Student");

            db.Instructors.Add(ApprovedInstructor(1));
            db.LessonOfferings.Add(new LessonOffering { OfferId = 10, InstructorId = 1, Title = "Highway", LessonType = "Manual", Price = 350 });
            db.Availabilities.Add(new Availability { AvailabilityId = 20, InstructorId = 1, Date = DateTime.Today.AddDays(1), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = AvailabilityStatus.Available });
            db.Bookings.Add(new Booking { BookingId = 99, InstructorId = 1, StudentId = 7, ScheduleId = 20, OfferId = 10, Status = BookingStatus.Pending });
            await db.SaveChangesAsync();

            var result = await controller.Book(1, 10, 20);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Detail", redirect.ActionName);
            Assert.Equal("This time slot has already been booked.", controller.TempData["Error"]);
            Assert.Equal(1, await db.Bookings.CountAsync());
        }

        // UC06_S: offering/schedule not belonging to the instructor -> error
        [Fact]
        public async Task UC06_Book_OfferingNotOwnedByInstructor_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorsController(db);
            TestHelper.SetupSession(controller, 5, "Student");

            db.Instructors.Add(ApprovedInstructor(1));
            db.Instructors.Add(ApprovedInstructor(2));
            // Offering belongs to instructor 2, booking attempted against instructor 1
            db.LessonOfferings.Add(new LessonOffering { OfferId = 10, InstructorId = 2, Title = "Highway", LessonType = "Manual", Price = 350 });
            db.Availabilities.Add(new Availability { AvailabilityId = 20, InstructorId = 1, Date = DateTime.Today.AddDays(1), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = AvailabilityStatus.Available });
            await db.SaveChangesAsync();

            var result = await controller.Book(1, 10, 20);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Detail", redirect.ActionName);
            Assert.Equal("Offering and schedule must belong to the selected instructor.", controller.TempData["Error"]);
            Assert.Equal(0, await db.Bookings.CountAsync());
        }
    }
}
