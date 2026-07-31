namespace BiDE.DTOs
{
    public class InstructorListDto
    {
        public int InstructorId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public string? Certification { get; set; }
        public int Experience { get; set; }
    }

    public class InstructorDetailDto
    {
        public int InstructorId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public string? Certification { get; set; }
        public int Experience { get; set; }
        public List<OfferingDto> Offerings { get; set; } = new();
        public List<AvailabilityDto> Availability { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    public class OfferingDto
    {
        public int OfferId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string LessonType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }

    public class AvailabilityDto
    {
        public int AvailabilityId { get; set; }
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }

    public class CreateOfferingRequest
    {
        public string Title { get; set; } = string.Empty;
        public string LessonType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }

    public class UpdateOfferingRequest
    {
        public string Title { get; set; } = string.Empty;
        public string LessonType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }

    public class AddAvailabilityRequest
    {
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = string.Empty; // "HH:mm"
        public string EndTime { get; set; } = string.Empty;   // "HH:mm"
    }
}
