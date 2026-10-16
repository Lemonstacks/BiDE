using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

        // Builds an IWebHostEnvironment whose WebRootPath points at a temp dir so the
        // SubmitPayment file-save path has somewhere to write.
        private static IWebHostEnvironment TempWebRootEnv()
        {
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());
            return env.Object;
        }

        private static IFormFile FakeProofFile(string fileName = "proof.png")
        {
            // Valid PNG magic-number header (89 50 4E 47 0D 0A 1A 0A) so the controller's
            // content-signature validation accepts it; the rest is filler.
            var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "proofOfPayment", fileName);
        }

        // ----- UC07_S View Booking Status -> StudentController.Bookings -----
        // (The not-logged-in case is already covered by Bookings_NotLoggedIn_RedirectsToLogin above.)

        [Fact]
        public async Task UC07_Bookings_LoggedInWithBookings_ReturnsViewSplitActiveAndPast()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Instructors.Add(new Instructor { InstructorId = 1, FirstName = "Test", LastName = "Inst", Email = "inst@test.com", Contact = "0711111111", Password = "pass" });
            db.LessonOfferings.Add(new LessonOffering { OfferId = 1, InstructorId = 1, Title = "Lesson", LessonType = "Manual", Price = 400 });
            for (int i = 1; i <= 4; i++)
                db.Availabilities.Add(new Availability { AvailabilityId = i, InstructorId = 1, Date = DateTime.Today.AddDays(i), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10), AvailabilityStatus = AvailabilityStatus.Booked });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Pending });
            db.Bookings.Add(new Booking { BookingId = 2, InstructorId = 1, StudentId = 1, ScheduleId = 2, OfferId = 1, Status = BookingStatus.Accepted });
            db.Bookings.Add(new Booking { BookingId = 3, InstructorId = 1, StudentId = 1, ScheduleId = 3, OfferId = 1, Status = BookingStatus.Completed });
            db.Bookings.Add(new Booking { BookingId = 4, InstructorId = 1, StudentId = 1, ScheduleId = 4, OfferId = 1, Status = BookingStatus.Cancelled });
            await db.SaveChangesAsync();

            var result = await controller.Bookings();

            Assert.IsType<ViewResult>(result);
            var active = (List<Booking>)controller.ViewBag.Active;
            var past = (List<Booking>)controller.ViewBag.Past;
            Assert.Equal(2, active.Count);
            Assert.Equal(2, past.Count);
            Assert.All(active, b => Assert.True(b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted));
            Assert.All(past, b => Assert.True(b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.Rejected));
        }

        // ----- UC08_S Upload Proof of Payment -> StudentController.SubmitPayment -----

        [Fact]
        public async Task UC08_SubmitPayment_ValidEftWithReferenceAndProof_CreatesPendingPayment()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new StudentController(db, TempWebRootEnv());
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.LessonOfferings.Add(new LessonOffering { OfferId = 1, InstructorId = 1, Title = "Lesson", LessonType = "Manual", Price = 400 });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted, AgreedPrice = 400 });
            await db.SaveChangesAsync();

            var result = await controller.SubmitPayment(1, PaymentMethod.Eft, "REF123", FakeProofFile());

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("Payment submitted. Your instructor will verify it shortly.", controller.TempData["Success"]);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == 1);
            Assert.NotNull(payment);
            Assert.Equal(PaymentStatus.Pending, payment!.PaymentStatus);
            Assert.Equal(PaymentMethod.Eft, payment.PaymentMethod);
            Assert.Equal("REF123", payment.PaymentReference);
            Assert.Equal(400, payment.Amount);
        }

        [Fact]
        public async Task UC08_SubmitPayment_DisguisedFileWrongSignature_ReturnsError()
        {
            // Security: a non-image/PDF (e.g. a .vsdx, which is a ZIP starting with "PK")
            // renamed to proof.png must be rejected by the content-signature check even
            // though its extension is allowed.
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new StudentController(db, TempWebRootEnv());
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted });
            await db.SaveChangesAsync();

            // ZIP/Office/Visio signature "PK" (0x50 0x4B) with a .png extension.
            var zipBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
            var disguised = new FormFile(new MemoryStream(zipBytes), 0, zipBytes.Length, "proofOfPayment", "proof.png");

            var result = await controller.SubmitPayment(1, PaymentMethod.Eft, "REF123", disguised);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("The file does not look like a valid JPG, PNG, or PDF. Please upload a real image or PDF.", controller.TempData["Error"]);
            Assert.Equal(0, await db.Payments.CountAsync());
        }

        [Fact]
        public async Task UC08_SubmitPayment_EftWithoutReference_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new StudentController(db, TempWebRootEnv());
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted });
            await db.SaveChangesAsync();

            var result = await controller.SubmitPayment(1, PaymentMethod.Eft, null, FakeProofFile());

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("Please enter the payment reference used for your EFT.", controller.TempData["Error"]);
            Assert.Equal(0, await db.Payments.CountAsync());
        }

        [Fact]
        public async Task UC08_SubmitPayment_InvalidPaymentMethod_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new StudentController(db, TempWebRootEnv());
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted });
            await db.SaveChangesAsync();

            var result = await controller.SubmitPayment(1, "", null, FakeProofFile());

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("Please select a valid payment method.", controller.TempData["Error"]);
            Assert.Equal(0, await db.Payments.CountAsync());
        }

        [Fact]
        public async Task UC08_SubmitPayment_Cash_NoFile_AutoVerified()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new StudentController(db, TempWebRootEnv());
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.LessonOfferings.Add(new LessonOffering { OfferId = 1, InstructorId = 1, Title = "Lesson", LessonType = "Manual", Price = 400 });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted, AgreedPrice = 400 });
            await db.SaveChangesAsync();

            var result = await controller.SubmitPayment(1, PaymentMethod.Cash, null, null!);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("Cash payment confirmed. Your lesson is ready to go!", controller.TempData["Success"]);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == 1);
            Assert.NotNull(payment);
            Assert.Equal(PaymentStatus.Verified, payment!.PaymentStatus);
            Assert.Equal(PaymentMethod.Cash, payment.PaymentMethod);
        }

        [Fact]
        public async Task UC08_SubmitPayment_EftWithoutProofFile_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new StudentController(db, TempWebRootEnv());
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted });
            await db.SaveChangesAsync();

            var result = await controller.SubmitPayment(1, PaymentMethod.Eft, "REF123", null!);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Equal("Proof of payment file is required for EFT payments.", controller.TempData["Error"]);
            Assert.Equal(0, await db.Payments.CountAsync());
        }

        // ----- UC10_S Leave Review -> StudentController.LeaveReview -----
        // (The invalid-rating and rating-above-5 cases are covered by the two existing tests above.)

        [Fact]
        public async Task UC10_LeaveReview_ValidOnCompletedBooking_CreatesReview()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Completed });
            await db.SaveChangesAsync();

            var result = await controller.LeaveReview(1, 5, "Great lesson");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CompletedLessons", redirect.ActionName);
            Assert.Equal("Review submitted. Thank you!", controller.TempData["Success"]);

            var review = await db.Reviews.FirstOrDefaultAsync(r => r.BookingId == 1);
            Assert.NotNull(review);
            Assert.Equal(5, review!.Rating);
            Assert.Equal("Great lesson", review.Comment);
        }

        [Fact]
        public async Task UC10_LeaveReview_DuplicateReview_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Completed });
            db.Reviews.Add(new Review { ReviewId = 1, BookingId = 1, StudentId = 1, Rating = 4, Comment = "First" });
            await db.SaveChangesAsync();

            var result = await controller.LeaveReview(1, 5, "Second");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CompletedLessons", redirect.ActionName);
            Assert.Equal("You have already reviewed this lesson.", controller.TempData["Error"]);
            Assert.Equal(1, await db.Reviews.CountAsync());
        }

        [Fact]
        public async Task UC10_LeaveReview_BookingNotCompleted_ReturnsNotFound()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var env = Mock.Of<IWebHostEnvironment>();
            var controller = new StudentController(db, env);
            TestHelper.SetupSession(controller, 1, "Student");

            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "Student", Email = "s@test.com", Contact = "0722222222", Password = "pass" });
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 1, StudentId = 1, ScheduleId = 1, OfferId = 1, Status = BookingStatus.Accepted });
            await db.SaveChangesAsync();

            var result = await controller.LeaveReview(1, 5, "Too early");

            Assert.IsType<NotFoundResult>(result);
            Assert.Equal(0, await db.Reviews.CountAsync());
        }
    }
}
