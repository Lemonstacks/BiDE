namespace BiDE.DTOs
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public string LessonType { get; set; } = string.Empty;
        public DateTime ScheduleDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CancellationReason { get; set; }
        public ReviewDto? Review { get; set; }
        public List<ProgressDto> Progress { get; set; } = new();
    }

    public class CreateBookingRequest
    {
        public int InstructorId { get; set; }
        public int OfferId { get; set; }
        public int ScheduleId { get; set; }
    }

    public class CancelBookingRequest
    {
        public string? Reason { get; set; }
    }

    public class RejectBookingRequest
    {
        public string? Reason { get; set; }
    }

    public class LeaveReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class AddProgressRequest
    {
        public string? Feedback { get; set; }
        public string CompletionStatus { get; set; } = "In Progress";
        public int Duration { get; set; }
    }

    public class ProgressDto
    {
        public int ProgressId { get; set; }
        public DateTime ProgressDate { get; set; }
        public int Duration { get; set; }
        public string CompletionStatus { get; set; } = string.Empty;
        public string? Feedback { get; set; }
    }
}
