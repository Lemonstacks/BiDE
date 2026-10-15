using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;
using Moq;

namespace BiDE.Tests
{
    public class PaymentControllerTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "bide-proof-" + Guid.NewGuid());

        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private async Task<(PaymentController controller, ApplicationDbContext db)> Arrange(int userId, string role)
        {
            var privateDir = Path.Combine(_root, "content", "private-uploads", "payments");
            Directory.CreateDirectory(privateDir);
            await File.WriteAllBytesAsync(Path.Combine(privateDir, "proof.png"), new byte[] { 1, 2, 3 });

            var legacyDir = Path.Combine(_root, "web", "uploads", "payments");
            Directory.CreateDirectory(legacyDir);
            await File.WriteAllBytesAsync(Path.Combine(legacyDir, "old.pdf"), new byte[] { 4, 5, 6 });

            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.ContentRootPath).Returns(Path.Combine(_root, "content"));
            env.Setup(e => e.WebRootPath).Returns(Path.Combine(_root, "web"));

            var db = TestHelper.GetInMemoryDbContext();
            db.Bookings.Add(new Booking { BookingId = 1, InstructorId = 10, StudentId = 20, ScheduleId = 1, OfferId = 1, Status = "Accepted" });
            db.Payments.Add(new Payment { PaymentId = 1, BookingId = 1, InstructorId = 10, Amount = 100, PaymentMethod = "EFT", ProofOfPayment = "private-uploads/payments/proof.png" });
            db.Bookings.Add(new Booking { BookingId = 2, InstructorId = 10, StudentId = 20, ScheduleId = 2, OfferId = 1, Status = "Accepted" });
            db.Payments.Add(new Payment { PaymentId = 2, BookingId = 2, InstructorId = 10, Amount = 100, PaymentMethod = "EFT", ProofOfPayment = "/uploads/payments/old.pdf" });
            await db.SaveChangesAsync();

            var controller = new PaymentController(db, env.Object);
            TestHelper.SetupSession(controller, userId, role);
            return (controller, db);
        }

        [Fact]
        public async Task Proof_OwningStudent_ReturnsFile()
        {
            var (controller, _) = await Arrange(20, "Student");
            var result = await controller.Proof(1);
            var file = Assert.IsType<PhysicalFileResult>(result);
            Assert.Equal("image/png", file.ContentType);
        }

        [Fact]
        public async Task Proof_BookingInstructor_ReturnsFile()
        {
            var (controller, _) = await Arrange(10, "Instructor");
            Assert.IsType<PhysicalFileResult>(await controller.Proof(1));
        }

        [Fact]
        public async Task Proof_Admin_ReturnsFile()
        {
            var (controller, _) = await Arrange(99, "Admin");
            Assert.IsType<PhysicalFileResult>(await controller.Proof(1));
        }

        [Fact]
        public async Task Proof_UnrelatedStudent_Denied()
        {
            var (controller, _) = await Arrange(21, "Student");
            Assert.IsType<NotFoundResult>(await controller.Proof(1));
        }

        [Fact]
        public async Task Proof_UnrelatedInstructor_Denied()
        {
            var (controller, _) = await Arrange(11, "Instructor");
            Assert.IsType<NotFoundResult>(await controller.Proof(1));
        }

        [Fact]
        public async Task Proof_LegacyWwwrootPath_StillResolves()
        {
            var (controller, _) = await Arrange(20, "Student");
            var result = await controller.Proof(2);
            var file = Assert.IsType<PhysicalFileResult>(result);
            Assert.Equal("application/pdf", file.ContentType);
        }
    }
}
