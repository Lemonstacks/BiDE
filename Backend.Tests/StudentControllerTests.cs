using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;
using Moq;

namespace BiDE.Tests
{
    public class StudentControllerTests
    {
        [Fact]
        public async Task LeaveReview_InvalidRating_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = "Completed" });
            await db.SaveChangesAsync();

            var result = await controller.LeaveReview(1, 0, "Bad");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CompletedLessons", redirect.ActionName);
            Assert.Equal("Rating must be between 1 and 5.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task LeaveReview_RatingAbove5_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = "Completed" });
            await db.SaveChangesAsync();

            var result = await controller.LeaveReview(1, 6, "Good");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Rating must be between 1 and 5.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task CancelBooking_CompletedBooking_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            var avail = new Availability { AvailabilityId = 99, InstructorId = 1, Date = DateTime.Today, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = "Booked" };
            db.Availabilities.Add(avail);
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 99, OfferId = 1, Status = "Completed" });
            await db.SaveChangesAsync();

            var result = await controller.CancelBooking(1, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Only pending or accepted bookings can be cancelled.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task CancelBooking_PendingBooking_Succeeds()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            var avail = new Availability { AvailabilityId = 1, InstructorId = 1, Date = DateTime.Today, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = "Booked" };
            db.Availabilities.Add(avail);
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = "Pending" });
            await db.SaveChangesAsync();

            var result = await controller.CancelBooking(1, "Changed my mind");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Booking cancelled successfully.", controller.TempData["Success"]);
            var booking = await db.Bookings.FindAsync(1);
            Assert.Equal("Cancelled", booking!.Status);
            Assert.Equal("Available", avail.AvailabilityStatus);
        }

        [Fact]
        public async Task Bookings_NotLoggedIn_RedirectsToLogin()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Bookings();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }
    }
}
