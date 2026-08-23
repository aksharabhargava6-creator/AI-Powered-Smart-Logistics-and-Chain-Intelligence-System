using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetTracking.Models
{
    public class VehicleLocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; } = null!;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double Speed { get; set; } // Speed in km/h

        public DateTime Timestamp { get; set; }

        public double? Heading { get; set; } // Optional: direction in degrees

        public string? Address { get; set; } // Optional: reverse geocoded address
    }
}