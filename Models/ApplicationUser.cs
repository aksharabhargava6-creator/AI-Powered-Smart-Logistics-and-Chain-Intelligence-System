using Microsoft.AspNetCore.Identity;

namespace LogisticsPlatform.API.Models;

/// <summary>
/// Extends the built-in Identity user with fields needed for FR-01 (Authentication & Authorization)
/// and FR-13 (Audit & Activity Logs).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }
}

/// <summary>
/// Role names matching the Stakeholders and User Roles table in the requirement document (Section 5).
/// Kept as constants so controllers/policies never rely on magic strings.
/// </summary>
public static class AppRoles
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string SupplyChainManager = "SupplyChainManager";
    public const string WarehouseManager = "WarehouseManager";
    public const string FleetManager = "FleetManager";
    public const string OperationsStaff = "OperationsStaff";
    public const string Analyst = "Analyst";

    public static readonly string[] All =
    {
        SystemAdministrator,
        SupplyChainManager,
        WarehouseManager,
        FleetManager,
        OperationsStaff,
        Analyst
    };
}
