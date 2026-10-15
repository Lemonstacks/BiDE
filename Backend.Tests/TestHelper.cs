using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BiDE.Controllers;
using BiDE.Data;
using BiDE.Models;
using Moq;

namespace BiDE.Tests
{
    public class TestHelper
    {
        public static ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        public static void SetupSession(Controller controller, int userId, string role, string name = "Test User")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetInt32("UserId", userId);
            httpContext.Session.SetString("UserRole", role);
            httpContext.Session.SetString("UserName", name);
            httpContext.RequestServices = BuildRequestServices();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        public static void SetupAnonymousSession(Controller controller)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.RequestServices = BuildRequestServices();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // Unit tests call actions directly, so model binding never runs. This mirrors the
        // framework: it runs the model's data annotations and records failures in ModelState.
        public static void ValidateModel(Controller controller, object model)
        {
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            foreach (var r in results)
            {
                var members = r.MemberNames.Any() ? r.MemberNames : new[] { string.Empty };
                foreach (var member in members)
                    controller.ModelState.AddModelError(member, r.ErrorMessage ?? string.Empty);
            }
        }
        // Provides the services HttpContext.SignInAsync/SignOutAsync resolve at runtime.
        // The auth service is a no-op stub so cookie sign-in during Register/Login
        // succeeds without a full authentication middleware pipeline.
        private static IServiceProvider BuildRequestServices()
        {
            var authService = new Mock<IAuthenticationService>();
            authService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
                    It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
            authService
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton(authService.Object);
            // RedirectToAction results resolve IUrlHelperFactory eagerly; provide a stub.
            services.AddSingleton(Mock.Of<IUrlHelperFactory>());
            return services.BuildServiceProvider();
        }
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
    }
}
