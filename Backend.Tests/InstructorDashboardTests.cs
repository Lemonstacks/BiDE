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

        private static Instructor SeededInstructor(int id = 1) => new Instructor
        {
            InstructorId = id,
            FirstName = "Test",
            LastName = "Inst",
            Email = $"inst{id}@test.com",
            Contact = "0711111111",
            Password = "pass"
        };

        // ----- UC12_I Manage Lesson Offerings -----
        // (EmptyTitle and NegativePrice for Create are covered by existing tests above.)

        [Fact]
        public async Task UC12_CreateOffering_Valid_AddsOffering()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            await db.SaveChangesAsync();

            var result = await controller.CreateOffering("Highway Lesson", "Manual", "Freeway driving", 350);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Offerings", redirect.ActionName);
            Assert.Equal("Offering created.", controller.TempData["Success"]);

            var offering = await db.LessonOfferings.FirstOrDefaultAsync();
            Assert.NotNull(offering);
            Assert.Equal("Highway Lesson", offering!.Title);
            Assert.Equal(1, offering.InstructorId);
            Assert.Equal(350, offering.Price);
        }

        [Fact]
        public async Task UC12_UpdateOffering_OwnedByInstructor_ChangesFields()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            db.LessonOfferings.Add(new LessonOffering { OfferId = 5, InstructorId = 1, Title = "Old", LessonType = "Manual", Description = "old", Price = 100 });
            await db.SaveChangesAsync();

            var result = await controller.UpdateOffering(5, "New Title", "Automatic", "new desc", 250);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Offerings", redirect.ActionName);
            Assert.Equal("Offering updated.", controller.TempData["Success"]);

            var offering = await db.LessonOfferings.FindAsync(5);
            Assert.Equal("New Title", offering!.Title);
            Assert.Equal("Automatic", offering.LessonType);
            Assert.Equal("new desc", offering.Description);
            Assert.Equal(250, offering.Price);
        }

        [Fact]
        public async Task UC12_DeleteOffering_OwnedByInstructor_RemovesOffering()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            db.LessonOfferings.Add(new LessonOffering { OfferId = 5, InstructorId = 1, Title = "ToDelete", LessonType = "Manual", Price = 100 });
            await db.SaveChangesAsync();

            var result = await controller.DeleteOffering(5);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Offerings", redirect.ActionName);
            Assert.Equal("Offering deleted.", controller.TempData["Success"]);
            Assert.Equal(0, await db.LessonOfferings.CountAsync());
        }

        [Fact]
        public async Task UC12_UpdateOffering_NotOwnedByInstructor_ReturnsNotFound()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            db.Instructors.Add(SeededInstructor(2));
            // Offering belongs to instructor 2
            db.LessonOfferings.Add(new LessonOffering { OfferId = 5, InstructorId = 2, Title = "Theirs", LessonType = "Manual", Price = 100 });
            await db.SaveChangesAsync();

            var result = await controller.UpdateOffering(5, "Hijacked", "Manual", null, 999);

            Assert.IsType<NotFoundResult>(result);
            var offering = await db.LessonOfferings.FindAsync(5);
            Assert.Equal("Theirs", offering!.Title);
        }

        // ----- UC14_I Respond to Booking Request -----
        // (The CompleteBooking_NotAccepted case remains above.)

        [Fact]
        public async Task UC14_AcceptBooking_PendingOwnedByInstructor_BecomesAccepted()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 2, ScheduleId = 3, OfferId = 4, Status = BookingStatus.Pending });
            await db.SaveChangesAsync();

            var result = await controller.AcceptBooking(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Booking accepted.", controller.TempData["Success"]);

            var booking = await db.Bookings.FindAsync(1);
            Assert.Equal(BookingStatus.Accepted, booking!.Status);
        }

        [Fact]
        public async Task UC14_RejectBooking_PendingOwnedByInstructor_RejectedAndSlotFreed()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            var avail = new Availability { AvailabilityId = 3, InstructorId = 1, Date = DateTime.Today.AddDays(1), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = AvailabilityStatus.Booked };
            db.Availabilities.Add(avail);
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 2, ScheduleId = 3, OfferId = 4, Status = BookingStatus.Pending });
            await db.SaveChangesAsync();

            var result = await controller.RejectBooking(1, "Not available");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Booking rejected.", controller.TempData["Success"]);

            var booking = await db.Bookings.FindAsync(1);
            Assert.Equal(BookingStatus.Rejected, booking!.Status);
            Assert.Equal(CancelledBy.Instructor, booking.CancelledBy);

            var slot = await db.Availabilities.FindAsync(3);
            Assert.Equal(AvailabilityStatus.Available, slot!.AvailabilityStatus);
        }

        [Fact]
        public async Task UC14_AcceptBooking_NotOwnedByInstructor_ReturnsNotFound()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new InstructorDashboardController(db);
            TestHelper.SetupSession(controller, 1, "Instructor");

            db.Instructors.Add(SeededInstructor(1));
            db.Instructors.Add(SeededInstructor(2));
            // Booking belongs to instructor 2
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 2, StudentId = 3, ScheduleId = 4, OfferId = 5, Status = BookingStatus.Pending });
            await db.SaveChangesAsync();

            var result = await controller.AcceptBooking(1);

            Assert.IsType<NotFoundResult>(result);
            var booking = await db.Bookings.FindAsync(1);
            Assert.Equal(BookingStatus.Pending, booking!.Status);
        }
    }
}
