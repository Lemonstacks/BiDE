using System.ComponentModel.DataAnnotations;

namespace BiDE.Models
{
    public class Instructor
    {
        [Key]
        public int InstructorId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(15)]
        public string Contact { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        public int Experience { get; set; }

        public bool IsVerified { get; set; } = false;

        [StringLength(200)]
        public string? Certification { get; set; }

        [StringLength(100)]
        public string? Suburb { get; set; }

        // Navigation properties
        public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
        public ICollection<LessonOffering> LessonOfferings { get; set; } = new List<LessonOffering>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
