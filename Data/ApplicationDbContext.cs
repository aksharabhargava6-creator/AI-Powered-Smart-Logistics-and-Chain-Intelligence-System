using LogisticsPlatform.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogisticsPlatform.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        builder.Entity<InventoryBalance>()
            .HasIndex(ib => new { ib.ProductId, ib.WarehouseId })
            .IsUnique();

        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InventoryBalance>()
            .HasOne(ib => ib.Product)
            .WithMany(p => p.InventoryBalances)
            .HasForeignKey(ib => ib.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InventoryBalance>()
            .HasOne(ib => ib.Warehouse)
            .WithMany(w => w.InventoryBalances)
            .HasForeignKey(ib => ib.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
