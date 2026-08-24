using System.ComponentModel.DataAnnotations;

namespace LogisticsPlatform.API.Models;

public class AuditLog
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string UserId { get; set; } = "Anonymous User";

    [Required]
    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Details { get; set; } = string.Empty;
}
