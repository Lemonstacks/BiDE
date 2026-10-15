using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;
using BiDE.ViewModels;

namespace BiDE.Tests
{
    public class AccountControllerTests
    {
        private static RegisterViewModel ValidRegister(string email = "john@test.com") => new()
        {
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            Contact = "0711111111",
            Password = "password123",
            ConfirmPassword = "password123",
            Role = "Student",
            SecurityQuestion = "What was your first pet?",
            SecurityAnswer = "Rex"
        };

        private static string[] ErrorsFor(Controller controller, string key) =>
            controller.ModelState.TryGetValue(key, out var entry)
                ? entry.Errors.Select(e => e.ErrorMessage).ToArray()
                : Array.Empty<string>();

        [Fact]
        public async Task Register_PasswordTooShort_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = ValidRegister();
            model.Password = "12345";
            model.ConfirmPassword = "12345";
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Contains("Password must be at least 6 characters.", ErrorsFor(controller, nameof(RegisterViewModel.Password)));
            Assert.Equal(0, await db.Students.CountAsync());
        }

        [Fact]
        public async Task Register_InvalidEmail_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = ValidRegister("notanemail");
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Contains("Please enter a valid email address.", ErrorsFor(controller, nameof(RegisterViewModel.Email)));
        }

        [Fact]
        public async Task Register_PasswordMismatch_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = ValidRegister();
            model.Password = "password1";
            model.ConfirmPassword = "password2";
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Contains("Passwords do not match.", ErrorsFor(controller, nameof(RegisterViewModel.ConfirmPassword)));
        }

        [Fact]
        public async Task Register_MultipleInvalidFields_ReportsAllErrorsAtOnce()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = ValidRegister("notanemail");
            model.FirstName = "";
            model.Password = "123";
            model.ConfirmPassword = "456";
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Register(model);

            Assert.IsType<ViewResult>(result);
            Assert.NotEmpty(ErrorsFor(controller, nameof(RegisterViewModel.FirstName)));
            Assert.NotEmpty(ErrorsFor(controller, nameof(RegisterViewModel.Email)));
            Assert.NotEmpty(ErrorsFor(controller, nameof(RegisterViewModel.Password)));
            Assert.NotEmpty(ErrorsFor(controller, nameof(RegisterViewModel.ConfirmPassword)));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Existing", LastName = "User", Email = "john@test.com", Contact = "0711111111", Password = "pass123" });
            await db.SaveChangesAsync();

            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = ValidRegister();
            model.Contact = "0722222222";
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Contains("An account with this email already exists.", ErrorsFor(controller, nameof(RegisterViewModel.Email)));
            Assert.Equal(1, await db.Students.CountAsync());
        }

        [Fact]
        public async Task Register_ValidStudent_CreatesAccountAndRedirects()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = new RegisterViewModel
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@test.com",
                Contact = "0733333333",
                Password = "password123",
                ConfirmPassword = "password123",
                Role = "Student",
                Suburb = "Sandton",
                SecurityQuestion = "What was your first pet?",
                SecurityAnswer = "Rex"
            };
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Register(model);

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

            var model = new LoginViewModel { Email = "test@test.com", Password = "wrong" };
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Contains("Invalid email or password.", ErrorsFor(controller, string.Empty));
        }

        [Fact]
        public async Task Login_EmptyFields_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = new LoginViewModel { Email = "", Password = "" };
            TestHelper.ValidateModel(controller, model);

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.NotEmpty(ErrorsFor(controller, nameof(LoginViewModel.Email)));
            Assert.NotEmpty(ErrorsFor(controller, nameof(LoginViewModel.Password)));
        }

        [Fact]
        public async Task ResetPassword_SameAsOldPassword_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", Contact = "0711111111", Password = "samepass", SecurityQuestion = "Q?", SecurityAnswer = "rex" });
            await db.SaveChangesAsync();

            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = new ResetPasswordViewModel { Email = "test@test.com", Answer = " Rex ", NewPassword = "samepass", ConfirmPassword = "samepass" };
            TestHelper.ValidateModel(controller, model);

            var result = await controller.ResetPassword(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ResetPassword", viewResult.ViewName);
            Assert.Contains("Your new password cannot be the same as your old password.", ErrorsFor(controller, nameof(ResetPasswordViewModel.NewPassword)));
        }

        [Fact]
        public async Task ResetPassword_WrongAnswer_ReturnsError()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", Contact = "0711111111", Password = "oldpass1", SecurityQuestion = "Q?", SecurityAnswer = "rex" });
            await db.SaveChangesAsync();

            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = new ResetPasswordViewModel { Email = "test@test.com", Answer = "wrong", NewPassword = "newpass1", ConfirmPassword = "newpass1" };
            TestHelper.ValidateModel(controller, model);

            var result = await controller.ResetPassword(model);

            Assert.IsType<ViewResult>(result);
            Assert.Contains("Incorrect answer to the security question.", ErrorsFor(controller, nameof(ResetPasswordViewModel.Answer)));
            Assert.Equal("oldpass1", (await db.Students.FirstAsync()).Password);
        }

        [Fact]
        public async Task ResetPassword_ValidAnswer_UpdatesPasswordAndRedirectsToLogin()
        {
            var db = TestHelper.GetInMemoryDbContext();
            db.Students.Add(new Student { StudentId = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", Contact = "0711111111", Password = "oldpass1", SecurityQuestion = "Q?", SecurityAnswer = "rex" });
            await db.SaveChangesAsync();

            var controller = new AccountController(db);
            TestHelper.SetupAnonymousSession(controller);

            var model = new ResetPasswordViewModel { Email = "test@test.com", Answer = "Rex", NewPassword = "newpass1", ConfirmPassword = "newpass1" };
            TestHelper.ValidateModel(controller, model);

            var result = await controller.ResetPassword(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("newpass1", (await db.Students.FirstAsync()).Password);
        }
    }
}
