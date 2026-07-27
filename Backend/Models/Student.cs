using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(100)]
        public string? LearnerPermitNumber { get; set; }

        public int CompletedLessons { get; set; } = 0;

        public DateTime EnrolledDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
