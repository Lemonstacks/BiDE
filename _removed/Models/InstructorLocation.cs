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
    }
}
