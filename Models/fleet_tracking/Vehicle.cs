using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetTracking.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string VehicleType { get; set; } = string.Empty;

        [Required]
        public double Capacity { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, InTransit, Maintenance, Offline

        public int? DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver? Driver { get; set; }

        public double CurrentLatitude { get; set; }

        public double CurrentLongitude { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<VehicleLocation> LocationHistory { get; set; } = new List<VehicleLocation>();
    }
}