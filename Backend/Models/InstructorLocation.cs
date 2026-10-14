namespace BiDE.Models
{
    public class InstructorLocation
    {
        public int InstructorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string VehicleType { get; set; } = "Manual";
        public bool IsAvailable { get; set; } = true;

        // Enriched display fields for the live map UI
        public string? ProfilePicture { get; set; }
        public double Rating { get; set; }
        public int LessonsCount { get; set; }
        public decimal Price { get; set; }
    }
}
