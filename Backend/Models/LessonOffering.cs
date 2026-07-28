using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public enum TransmissionType
    {
        Manual,
        Automatic
    }

    public class LessonOffering
    {
        [Key]
        public int LessonOfferingId { get; set; }

        [Required]
        public int InstructorId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int DurationMinutes { get; set; } = 60;

        [Required]
        public TransmissionType Transmission { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("InstructorId")]
        public Instructor Instructor { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
