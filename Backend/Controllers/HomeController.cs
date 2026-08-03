using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;

namespace BiDE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var topInstructors = await _context.Instructors
                .Where(i => i.Status == Models.InstructorStatus.Approved && i.IsVerified)
                .OrderByDescending(i => i.Experience)
                .Take(3)
                .ToListAsync();

            ViewBag.TopInstructors = topInstructors;
            ViewBag.TotalInstructors = await _context.Instructors
                .CountAsync(i => i.Status == Models.InstructorStatus.Approved);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalStudents = await _context.Students.CountAsync();

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
