using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
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
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
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
