using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MultiAreaPortal.Data;

// Applies migrations and seeds the Admin/Customer roles plus one test user each.
public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in new[] { AdminRole, CustomerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUserAsync(userManager,
            config["SeedData:AdminEmail"]!, config["SeedData:AdminPassword"]!, AdminRole);

        await EnsureUserAsync(userManager,
            config["SeedData:CustomerEmail"]!, config["SeedData:CustomerPassword"]!, CustomerRole);
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager, string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
