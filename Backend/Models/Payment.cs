using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiDE.Models
{
    public enum PaymentStatus
    {
        Pending,
        Uploaded,
        Verified,
        Rejected
    }

    public enum PaymentMethod
    {
        EFT,
        Cash
    }

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(500)]
        public string? ProofOfPaymentPath { get; set; }

        public DateTime? UploadedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        [StringLength(300)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;
    }
}
