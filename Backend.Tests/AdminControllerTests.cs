using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Tests
{
    public class AdminControllerTests
    {
        private static Student SeededStudent(int id = 1) => new Student
        {
            StudentId = id,
            FirstName = "Test",
            LastName = "Student",
            Email = $"s{id}@test.com",
            Contact = "0722222222",
            Password = "pass"
        };

        private static Instructor SeededInstructor(int id = 1, InstructorStatus status = InstructorStatus.Approved) => new Instructor
        {
            InstructorId = id,
            FirstName = "Test",
            LastName = "Inst",
            Email = $"inst{id}@test.com",
            Contact = "0711111111",
            Password = "pass",
            Status = status,
            IsVerified = status == InstructorStatus.Approved
        };

        // ----- UC17_A Manage Users -----

        [Fact]
        public async Task UC17_DeactivateStudent_NoActiveBookings_Removes()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupSession(controller, 1, "Admin");

            db.Students.Add(SeededStudent(2));
            await db.SaveChangesAsync();

            var result = await controller.DeactivateStudent(2);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageUsers", redirect.ActionName);
            Assert.Equal("Test Student has been removed.", controller.TempData["Success"]);
            Assert.Null(await db.Students.FindAsync(2));
        }

        [Fact]
        public async Task UC17_DeactivateStudent_WithActiveBookings_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupSession(controller, 1, "Admin");

            var student = SeededStudent(2);
            db.Students.Add(student);
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 2, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Pending });
            await db.SaveChangesAsync();

            var result = await controller.DeactivateStudent(2);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ManageUsers", redirect.ActionName);
            Assert.Equal($"Cannot remove {student.FirstName} {student.LastName} — they have active bookings.", controller.TempData["Error"]);
            Assert.NotNull(await db.Students.FindAsync(2));
        }

        [Fact]
        public async Task UC17_Suspend_SetsStatusSuspended()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupSession(controller, 1, "Admin");

            db.Instructors.Add(SeededInstructor(2, InstructorStatus.Approved));
            await db.SaveChangesAsync();

            var result = await controller.Suspend(2);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            var instructor = await db.Instructors.FindAsync(2);
            Assert.Equal(InstructorStatus.Suspended, instructor!.Status);
            Assert.False(instructor.IsVerified);
        }

        [Fact]
        public async Task UC17_Reinstate_SetsStatusApproved()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupSession(controller, 1, "Admin");

            db.Instructors.Add(SeededInstructor(2, InstructorStatus.Suspended));
            await db.SaveChangesAsync();

            var result = await controller.Reinstate(2);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            var instructor = await db.Instructors.FindAsync(2);
            Assert.Equal(InstructorStatus.Approved, instructor!.Status);
            Assert.True(instructor.IsVerified);
        }

        [Fact]
        public async Task UC17_DeactivateStudent_NonAdmin_RedirectsToLogin()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupAnonymousSession(controller);

            db.Students.Add(SeededStudent(2));
            await db.SaveChangesAsync();

            var result = await controller.DeactivateStudent(2);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
            Assert.NotNull(await db.Students.FindAsync(2));
        }

        // Seeds a complete Student/Instructor/Offering/Booking/Payment graph. The
        // ViewPayments query eager-loads Booking.Student/Instructor/LessonOffering, and
        // the in-memory provider only materialises rows whose included relations exist.
        private static void SeedPaymentGraph(ApplicationDbContext db)
        {
            db.Students.Add(SeededStudent(2));
            db.Instructors.Add(SeededInstructor(1));
            db.LessonOfferings.Add(new LessonOffering { OfferId = 1, InstructorId = 1, Title = "Lesson", LessonType = "Manual", Price = 300 });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 2, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted });
            db.Bookings.Add(new Booking { BookingId = 2, InstructorId = 1, StudentId = 2, ScheduleId = 2, OfferId = 1, Status = BookingStatus.Completed });
            db.Payments.Add(new Payment { PaymentId = 1, BookingId = 1, InstructorId = 1, Amount = 100, PaymentMethod = PaymentMethod.Eft, PaymentStatus = PaymentStatus.Pending, PaymentDate = DateTime.UtcNow });
            db.Payments.Add(new Payment { PaymentId = 2, BookingId = 2, InstructorId = 1, Amount = 200, PaymentMethod = PaymentMethod.Cash, PaymentStatus = PaymentStatus.Verified, PaymentDate = DateTime.UtcNow });
        }

        // ----- UC18_A View Payments -----

        [Fact]
        public async Task UC18_ViewPayments_Admin_ReturnsViewWithPayments()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupSession(controller, 1, "Admin");

            SeedPaymentGraph(db);
            await db.SaveChangesAsync();

            var result = await controller.ViewPayments(null, null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var payments = Assert.IsType<List<Payment>>(view.Model);
            Assert.Equal(2, payments.Count);
        }

        [Fact]
        public async Task UC18_ViewPayments_StatusFilter_ReturnsOnlyMatching()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupSession(controller, 1, "Admin");

            SeedPaymentGraph(db);
            await db.SaveChangesAsync();

            var result = await controller.ViewPayments(PaymentStatus.Verified, null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var payments = Assert.IsType<List<Payment>>(view.Model);
            Assert.Single(payments);
            Assert.Equal(PaymentStatus.Verified, payments[0].PaymentStatus);
        }

        [Fact]
        public async Task UC18_ViewPayments_NonAdmin_RedirectsToLogin()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AdminController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.ViewPayments(null, null, null, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }
    }
}
