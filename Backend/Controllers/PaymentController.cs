using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiDE.Data;

namespace BiDE.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        // Logical prefix for proofs stored outside wwwroot (resolved against ContentRootPath)
        public const string PrivateProofPrefix = "private-uploads/payments/";
        // Legacy prefix for proofs that were stored under wwwroot (resolved against WebRootPath)
        public const string LegacyProofPrefix = "/uploads/payments/";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PaymentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Payment/Proof/{paymentId}
        // Streams the proof only to the owning student, the booking's instructor, or an admin.
        [HttpGet]
        public async Task<IActionResult> Proof(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null || string.IsNullOrEmpty(role)) return Forbid();

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null || payment.Booking == null || string.IsNullOrEmpty(payment.ProofOfPayment))
                return NotFound();

            var allowed = role == "Admin"
                || (role == "Student" && payment.Booking.StudentId == userId.Value)
                || (role == "Instructor" && payment.Booking.InstructorId == userId.Value);
            if (!allowed) return NotFound();

            var physicalPath = ResolveProofPath(payment.ProofOfPayment);
            if (physicalPath == null || !System.IO.File.Exists(physicalPath)) return NotFound();

            var contentType = Path.GetExtension(physicalPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            Response.Headers["Cache-Control"] = "private,no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(physicalPath, contentType);
        }

        // Maps a stored reference to a physical file, confined to the expected directory.
        private string? ResolveProofPath(string stored)
        {
            string baseDir;
            string prefix;
            if (stored.StartsWith(PrivateProofPrefix, StringComparison.Ordinal))
            {
                baseDir = Path.Combine(_env.ContentRootPath, "private-uploads", "payments");
                prefix = PrivateProofPrefix;
            }
            else if (stored.StartsWith(LegacyProofPrefix, StringComparison.Ordinal))
            {
                baseDir = Path.Combine(_env.WebRootPath, "uploads", "payments");
                prefix = LegacyProofPrefix;
            }
            else
            {
                return null;
            }

            // Strip any directory parts so a crafted value cannot escape baseDir
            var fileName = Path.GetFileName(stored.Substring(prefix.Length));
            if (string.IsNullOrEmpty(fileName)) return null;

            var full = Path.GetFullPath(Path.Combine(baseDir, fileName));
            var root = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
    }
}
