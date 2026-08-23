namespace FleetTracking.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public double Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? DriverId { get; set; }
        public string? DriverName { get; set; }
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public double? CurrentSpeed { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateVehicleDto
    {
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public double Capacity { get; set; }
        public int? DriverId { get; set; }
    }

    public class UpdateVehicleDto
    {
        public string? VehicleType { get; set; }
        public double? Capacity { get; set; }
        public string? Status { get; set; }
        public int? DriverId { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
    }
}