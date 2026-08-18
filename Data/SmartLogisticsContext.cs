using Microsoft.EntityFrameworkCore;
using SmartLogisticsApp.Models;

namespace SmartLogisticsApp.Data;

public class SmartLogisticsContext : DbContext
{
    public SmartLogisticsContext(DbContextOptions<SmartLogisticsContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<DeliveryAssignment> DeliveryAssignments => Set<DeliveryAssignment>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>().ToTable("Inventory");
        modelBuilder.Entity<DeliveryAssignment>().ToTable("DeliveryAssignments");
        base.OnModelCreating(modelBuilder);
    }
}