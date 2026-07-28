using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int InstructorId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [Required]
        public int OfferId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        // Navigation properties
        [ForeignKey("InstructorId")]
        public Instructor Instructor { get; set; } = null!;

        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;

        [ForeignKey("ScheduleId")]
        public Availability Schedule { get; set; } = null!;

        [ForeignKey("OfferId")]
        public LessonOffering LessonOffering { get; set; } = null!;

        public Payment? Payment { get; set; }
        public Review? Review { get; set; }
        public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }
}
