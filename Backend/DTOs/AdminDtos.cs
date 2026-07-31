namespace BiDE.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalStudents { get; set; }
        public int TotalBookings { get; set; }
        public List<InstructorApplicationDto> Pending { get; set; } = new();
        public List<InstructorApplicationDto> Approved { get; set; } = new();
        public List<InstructorApplicationDto> Rejected { get; set; } = new();
    }

    public class InstructorApplicationDto
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
        public DateTime ApplicationDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class RejectInstructorRequest
    {
        public string? Reason { get; set; }
    }
}
