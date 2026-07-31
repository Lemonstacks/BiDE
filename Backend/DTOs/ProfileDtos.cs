namespace BiDE.DTOs
{
    public class StudentProfileDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InstructorProfileDto
    {
        public int InstructorId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public string? Certification { get; set; }
        public int Experience { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateStudentProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string? Suburb { get; set; }
    }

    public class UpdateInstructorProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public string? Certification { get; set; }
        public int Experience { get; set; }
    }
}
