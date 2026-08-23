using System.ComponentModel.DataAnnotations;

namespace FleetTracking.Models
{
    public class Driver
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        public DateTime LicenseExpiry { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, OnDuty, OffDuty

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual Vehicle? Vehicle { get; set; }
    }
}
