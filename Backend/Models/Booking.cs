using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        Rescheduled
    }

    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int LessonOfferingId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [StringLength(500)]
        public string? InstructorNotes { get; set; }

        [StringLength(500)]
        public string? StudentNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;

        [ForeignKey("LessonOfferingId")]
        public LessonOffering LessonOffering { get; set; } = null!;

        public Payment? Payment { get; set; }
        public Review? Review { get; set; }
    }
}
