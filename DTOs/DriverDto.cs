namespace FleetTracking.DTOs
{
    public class DriverDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiry { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? AssignedVehicleId { get; set; }
        public string? AssignedVehicleRegistration { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateDriverDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiry { get; set; }
    }

    public class UpdateDriverDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; }
        public DateTime? LicenseExpiry { get; set; }
    }
}