using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;

namespace BiDE.Tests
{
    public class AccountControllerTests
    {
        [Fact]
        public async Task Register_PasswordTooShort_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Register("John", "Doe", "john@test.com", "0711111111", "12345", "12345", "Student", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Password must be at least 6 characters.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Register_InvalidEmail_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Register("John", "Doe", "notanemail", "0711111111", "password123", "password123", "Student", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Please enter a valid email address.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Register_PasswordMismatch_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Register("John", "Doe", "john@test.com", "0711111111", "password1", "password2", "Student", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Passwords do not match.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Existing", LastName = "User", Email = "john@test.com", Contact = "0711111111", Password = "pass123" });
            await db.SaveChangesAsync();

            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Register("John", "Doe", "john@test.com", "0722222222", "password123", "password123", "Student", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("An account with this email already exists.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Register_ValidStudent_CreatesAccountAndRedirects()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Register("Jane", "Smith", "jane@test.com", "0733333333", "password123", "password123", "Student", "Sandton");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(1, await db.Students.CountAsync());
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", Contact = "0711111111", Password = "correct" });
            await db.SaveChangesAsync();

            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Login("test@test.com", "wrong", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Invalid email or password.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Login_EmptyFields_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var result = await controller.Login("", "", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Please fill in all fields.", controller.ViewBag.Error);
        }
    }
}
