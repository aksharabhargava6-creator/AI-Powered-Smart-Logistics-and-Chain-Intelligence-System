using Microsoft.EntityFrameworkCore;
using FleetTracking.Models;

namespace FleetTracking.Data
{
    public class LogisticsDbContext : DbContext
    {
        public LogisticsDbContext(DbContextOptions<LogisticsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<VehicleLocation> VehicleLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Vehicle configuration
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.RegistrationNumber)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Driver)
                .WithOne(d => d.Vehicle)
                .HasForeignKey<Vehicle>(v => v.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            // Driver configuration
            modelBuilder.Entity<Driver>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique();

            // VehicleLocation configuration
            modelBuilder.Entity<VehicleLocation>()
                .HasIndex(vl => vl.VehicleId)
                .HasDatabaseName("IX_VehicleLocations_VehicleId");

            modelBuilder.Entity<VehicleLocation>()
                .HasIndex(vl => vl.Timestamp)
                .HasDatabaseName("IX_VehicleLocations_Timestamp");
        }
    }
}