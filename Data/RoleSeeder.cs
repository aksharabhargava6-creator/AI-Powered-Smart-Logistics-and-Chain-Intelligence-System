using LogisticsPlatform.API.Models;
using Microsoft.AspNetCore.Identity;

namespace LogisticsPlatform.API.Data;

/// <summary>
/// Ensures the six roles from Section 5 (Stakeholders and User Roles) exist in the database.
/// Runs once at application startup.
/// </summary>
public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}
