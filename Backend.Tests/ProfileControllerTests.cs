using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;
using BiDE.ViewModels;
using Moq;

namespace BiDE.Tests
{
    public class ProfileControllerTests
    {
        private static ProfileController CreateController(ApplicationDbContext db, int userId, string role)
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "bide-tests-" + Guid.NewGuid()));
            var controller = new ProfileController(db, env.Object);
            TestHelper.SetupSession(controller, userId, role);
            return controller;
        }

        private static IFormFile FakeFile(string name, long length)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(name);
            file.Setup(f => f.Length).Returns(length);
            return file.Object;
        }

        private static async Task<ApplicationDbContext> DbWithInstructor()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Instructors.Add(new Instructor
            {
                InstructorId = 1, FirstName = "Ins", LastName = "Tructor", Email = "ins@test.com",
                Contact = "0711111111", Password = "pass123", Experience = 5, Certification = "Old cert"
            });
            await db.SaveChangesAsync();
            return db;
        }

        private static InstructorProfileViewModel ValidInstructorModel() => new()
        {
            FirstName = "New", LastName = "Name", Contact = "0722222222", Suburb = "Sandton", Certification = "New cert"
        };

        [Fact]
        public void InstructorProfileViewModel_Experience_IsNotBindable()
        {
            var prop = typeof(InstructorProfileViewModel).GetProperty(nameof(InstructorProfileViewModel.Experience))!;
            Assert.NotNull(prop.GetCustomAttribute<BindNeverAttribute>());
        }

        [Fact]
        public async Task UpdateInstructor_PostedExperience_IsIgnored()
        {
            var db = await DbWithInstructor();
            var controller = CreateController(db, 1, "Instructor");
            var model = ValidInstructorModel();
            model.Experience = 99;

            var result = await controller.UpdateInstructor(model);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = await db.Instructors.FirstAsync();
            Assert.Equal(5, saved.Experience);
            Assert.Equal("New", saved.FirstName);
            Assert.Equal("New cert", saved.Certification);
        }

        [Fact]
        public async Task UpdateInstructor_BadFileType_ReturnsViewWithError()
        {
            var db = await DbWithInstructor();
            var controller = CreateController(db, 1, "Instructor");
            var model = ValidInstructorModel();
            model.ProfilePicture = FakeFile("malware.exe", 1024);

            var result = await controller.UpdateInstructor(model);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("InstructorProfile", view.ViewName);
            Assert.Contains("Profile picture must be a JPG, PNG, or GIF image.",
                controller.ModelState["ProfilePicture"]!.Errors.Select(e => e.ErrorMessage));
            Assert.Equal("Ins", (await db.Instructors.FirstAsync()).FirstName);
        }

        [Fact]
        public async Task UpdateInstructor_OversizedFile_ReturnsViewWithError()
        {
            var db = await DbWithInstructor();
            var controller = CreateController(db, 1, "Instructor");
            var model = ValidInstructorModel();
            model.ProfilePicture = FakeFile("big.png", 4 * 1024 * 1024);

            var result = await controller.UpdateInstructor(model);

            Assert.IsType<ViewResult>(result);
            Assert.Contains("Profile picture must be less than 3MB.",
                controller.ModelState["ProfilePicture"]!.Errors.Select(e => e.ErrorMessage));
        }

        [Fact]
        public async Task UpdateStudent_BadFileType_ReturnsViewWithError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Stu", LastName = "Dent", Email = "stu@test.com", Contact = "0711111111", Password = "pass123" });
            await db.SaveChangesAsync();
            var controller = CreateController(db, 1, "Student");
            var model = new StudentProfileViewModel
            {
                FirstName = "New", LastName = "Name", Contact = "0722222222",
                ProfilePicture = FakeFile("notes.txt", 10)
            };

            var result = await controller.UpdateStudent(model);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("StudentProfile", view.ViewName);
            Assert.Contains("Profile picture must be a JPG, PNG, or GIF image.",
                controller.ModelState["ProfilePicture"]!.Errors.Select(e => e.ErrorMessage));
            Assert.Equal("Stu", (await db.Students.FirstAsync()).FirstName);
        }
    }
}
