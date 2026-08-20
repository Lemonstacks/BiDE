using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Tests
{
    public class InstructorDashboardTests
    {
        [Fact]
        public async Task AddAvailability_EndBeforeStart_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            await db.SaveChangesAsync();

            var result = await controller.AddAvailability(DateTime.Now.AddDays(1), new TimeSpan(17, 0, 0), new TimeSpan(9, 0, 0));

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Availability", redirect.ActionName);
            Assert.Equal("End time must be after start time.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task AddAvailability_PastDate_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            await db.SaveChangesAsync();

            var result = await controller.AddAvailability(DateTime.Now.AddDays(-1), new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0));

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Availability", redirect.ActionName);
            Assert.Equal("Cannot add an availability slot in the past.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task AddAvailability_ValidSlot_Succeeds()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            await db.SaveChangesAsync();

            var result = await controller.AddAvailability(DateTime.Now.AddDays(2), new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0));

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Availability", redirect.ActionName);
            Assert.Equal("Availability slot added.", controller.TempData["Success"]);
            Assert.Equal(1, await db.Availabilities.CountAsync());
        }

        [Fact]
        public async Task CreateOffering_EmptyTitle_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            await db.SaveChangesAsync();

            var result = await controller.CreateOffering("", "Manual", null, 350);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Title is required.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task CreateOffering_NegativePrice_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            await db.SaveChangesAsync();

            var result = await controller.CreateOffering("Highway Lesson", "Manual", null, -100);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Price cannot be negative.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task CompleteBooking_NotAccepted_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = "Pending" });
            await db.SaveChangesAsync();

            var result = await controller.CompleteBooking(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Only accepted bookings can be marked as completed.", controller.TempData["Error"]);
        }
    }
}
