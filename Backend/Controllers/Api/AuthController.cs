using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BiDE.Data;
using BiDE.DTOs;
using BiDE.Helpers;
using BiDE.Models;

namespace BiDE.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required." });

            // Check Student
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == request.Email);
            if (student != null && PasswordHasher.Verify(request.Password, student.Password))
            {
                if (!PasswordHasher.IsBCryptHash(student.Password))
                {
                    student.Password = PasswordHasher.Hash(request.Password);
                    await _context.SaveChangesAsync();
                }

                return Ok(GenerateAuthResponse(student.StudentId, "Student",
                    $"{student.FirstName} {student.LastName}", student.Email));
            }

            // Check Instructor
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Email == request.Email);
            if (instructor != null && PasswordHasher.Verify(request.Password, instructor.Password))
            {
                if (!PasswordHasher.IsBCryptHash(instructor.Password))
                {
                    instructor.Password = PasswordHasher.Hash(request.Password);
                    await _context.SaveChangesAsync();
                }

                return Ok(GenerateAuthResponse(instructor.InstructorId, "Instructor",
                    $"{instructor.FirstName} {instructor.LastName}", instructor.Email));
            }

            // Check Admin
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);
            if (admin != null && PasswordHasher.Verify(request.Password, admin.Password))
            {
                if (!PasswordHasher.IsBCryptHash(admin.Password))
                {
                    admin.Password = PasswordHasher.Hash(request.Password);
                    await _context.SaveChangesAsync();
                }

                return Ok(GenerateAuthResponse(admin.AdminId, "Admin",
                    $"{admin.FirstName} {admin.LastName}", admin.Email));
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Contact))
                return BadRequest(new { message = "All required fields must be provided." });

            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            if (request.Password.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters." });

            if (request.Role != "Student" && request.Role != "Instructor")
                return BadRequest(new { message = "Role must be 'Student' or 'Instructor'." });

            // Check if email already exists
            var emailExists = await _context.Students.AnyAsync(s => s.Email == request.Email)
                || await _context.Instructors.AnyAsync(i => i.Email == request.Email)
                || await _context.Admins.AnyAsync(a => a.Email == request.Email);

            if (emailExists)
                return Conflict(new { message = "An account with this email already exists." });

            if (request.Role == "Student")
            {
                var student = new Student
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Contact = request.Contact,
                    Password = PasswordHasher.Hash(request.Password),
                    Suburb = request.Suburb,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Ok(GenerateAuthResponse(student.StudentId, "Student",
                    $"{student.FirstName} {student.LastName}", student.Email));
            }
            else
            {
                var instructor = new Instructor
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Contact = request.Contact,
                    Password = PasswordHasher.Hash(request.Password),
                    Suburb = request.Suburb,
                    Status = InstructorStatus.Pending,
                    IsVerified = false,
                    ApplicationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();

                return Ok(GenerateAuthResponse(instructor.InstructorId, "Instructor",
                    $"{instructor.FirstName} {instructor.LastName}", instructor.Email));
            }
        }

        private AuthResponse GenerateAuthResponse(int userId, string role, string name, string email)
        {
            var token = GenerateJwtToken(userId, role, email);
            return new AuthResponse
            {
                Token = token,
                Role = role,
                UserId = userId,
                Name = name,
                Email = email
            };
        }

        private string GenerateJwtToken(int userId, string role, string email)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email)
            };

            var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "1440");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
