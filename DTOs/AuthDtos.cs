using System.ComponentModel.DataAnnotations;

namespace LogisticsPlatform.API.DTOs;

public class RegisterDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Must be one of AppRoles (SystemAdministrator, SupplyChainManager, WarehouseManager,
    /// FleetManager, OperationsStaff, Analyst). In production, only a SystemAdministrator
    /// should be able to create users with elevated roles — see AuthController for the check.
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
