namespace FleetTracking.DTOs
{
    public class LocationUpdateDto
    {
        public int VehicleId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VehicleLocationHistoryDto
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public List<LocationPointDto> Locations { get; set; } = new();
    }

    public class LocationPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public DateTime Timestamp { get; set; }
    }
}