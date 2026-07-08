using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiAreaPortal.Models;

namespace MultiAreaPortal.Data;

// Applies migrations, seeds roles/users, installs the reporting stored procedure,
// and seeds a sales dataset used by the Admin "Top Products" performance bonus.
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

        var env = sp.GetRequiredService<IHostEnvironment>();
        await InstallStoredProcedureAsync(db, env);
        await SeedSalesAsync(db);
    }

    // Executes Scripts/usp_GetTopProductsByQuantity.sql (CREATE OR ALTER, so it is idempotent).
    private static async Task InstallStoredProcedureAsync(ApplicationDbContext db, IHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Scripts", "usp_GetTopProductsByQuantity.sql");
        if (!File.Exists(path))
            return;

        var sql = await File.ReadAllTextAsync(path);
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    // Seeds categories, products, orders and order items (deterministic) for the report.
    private static async Task SeedSalesAsync(ApplicationDbContext db)
    {
        if (await db.Products.AnyAsync())
            return;

        var categoryNames = new[]
        {
            "Electronics", "Books", "Home & Kitchen", "Sports",
            "Toys", "Clothing", "Beauty", "Office"
        };
        var categories = categoryNames.Select(n => new Category { Name = n }).ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        // 5 products per category (40 products).
        var products = new List<Product>();
        foreach (var category in categories)
            for (var i = 1; i <= 5; i++)
                products.Add(new Product { Name = $"{category.Name} Item {i}", CategoryId = category.Id });
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        // Deterministic pseudo-random data so results/timings are reproducible.
        var rng = new Random(20260708);
        var today = DateTime.Today;

        const int orderCount = 800;
        var orders = new List<Order>(orderCount);
        for (var i = 0; i < orderCount; i++)
        {
            orders.Add(new Order
            {
                OrderDate = today.AddDays(-rng.Next(0, 180)).AddHours(rng.Next(0, 24)),
                CustomerName = $"Customer {rng.Next(1, 200)}"
            });
        }
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();

        // ~4 line items per order (~3,200 order items).
        var items = new List<OrderItem>();
        foreach (var order in orders)
        {
            var lineCount = rng.Next(1, 7);
            var used = new HashSet<int>();
            for (var l = 0; l < lineCount; l++)
            {
                var product = products[rng.Next(products.Count)];
                if (!used.Add(product.Id))
                    continue;

                items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = rng.Next(1, 11),
                    UnitPrice = Math.Round((decimal)(rng.NextDouble() * 495 + 5), 2)
                });
            }
        }
        db.OrderItems.AddRange(items);
        await db.SaveChangesAsync();
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
