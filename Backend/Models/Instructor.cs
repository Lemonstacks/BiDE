using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public enum InstructorStatus
    {
        Pending,
        Approved,
        Rejected,
        Suspended
    }

    public class Instructor
    {
        [Key]
        public int InstructorId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        public int YearsOfExperience { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Required]
        public InstructorStatus Status { get; set; } = InstructorStatus.Pending;

        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovalDate { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<LessonOffering> LessonOfferings { get; set; } = new List<LessonOffering>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
