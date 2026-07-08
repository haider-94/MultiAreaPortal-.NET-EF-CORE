using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiAreaPortal.Models;

namespace MultiAreaPortal.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    // Sales schema (Admin "Top Products" performance bonus).
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OrderItem>(e =>
        {
            e.HasOne(oi => oi.Order).WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oi => oi.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(oi => oi.ProductId);
            e.HasIndex(oi => oi.OrderId);
        });

        builder.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>().HasIndex(o => o.OrderDate);
    }
}
