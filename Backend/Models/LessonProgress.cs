using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public class LessonProgress
    {
        [Key]
        public int ProgressId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public DateTime ProgressDate { get; set; }

        public int Duration { get; set; }

        public TimeSpan StartTime { get; set; }

        [Required]
        [StringLength(50)]
        public string CompletionStatus { get; set; } = "In Progress";

        [StringLength(1000)]
        public string? Feedback { get; set; }

        // Navigation properties
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;
    }
}
