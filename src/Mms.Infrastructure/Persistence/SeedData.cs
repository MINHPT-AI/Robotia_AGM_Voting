using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Mms.Infrastructure.Identity;

namespace Mms.Infrastructure.Persistence;

public static class SeedData
{
    private static readonly string[] DefaultRoles = ["admin", "operator", "viewer", "checkin"];

    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed roles — idempotent
        foreach (var role in DefaultRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        // Seed admin user — idempotent
        var adminUsername = Environment.GetEnvironmentVariable("SEED_ADMIN_USERNAME") ?? "admin";
        if (await userManager.FindByNameAsync(adminUsername) is null)
        {
            var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "Admin@2026!";
            var admin = new ApplicationUser
            {
                UserName = adminUsername,
                Email = $"{adminUsername}@local.mms",
                FullName = "System Administrator",
                MustChangePassword = true,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "admin");
            }
        }
    }
}
