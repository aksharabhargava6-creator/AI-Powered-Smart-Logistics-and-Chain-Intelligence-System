// IDriverService.cs
using FleetTracking.DTOs;

namespace FleetTracking.Services
{
    public interface IDriverService
    {
        Task<IEnumerable<DriverDto>> GetAllDriversAsync(bool includeInactive = false);
        Task<DriverDto?> GetDriverByIdAsync(int id);
        Task<DriverDto> CreateDriverAsync(CreateDriverDto driverDto);
        Task<DriverDto?> UpdateDriverAsync(int id, UpdateDriverDto driverDto);
        Task<bool> DeleteDriverAsync(int id);
        Task<IEnumerable<DriverDto>> GetAvailableDriversAsync();
        Task<DriverDto?> UpdateDriverStatusAsync(int id, string status);
    }
}

// DriverService.cs
using Microsoft.EntityFrameworkCore;
using FleetTracking.Data;
using FleetTracking.DTOs;
using FleetTracking.Models;

namespace FleetTracking.Services
{
    public class DriverService : IDriverService
    {
        private readonly LogisticsDbContext _context;

        public DriverService(LogisticsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DriverDto>> GetAllDriversAsync(bool includeInactive = false)
        {
            var query = _context.Drivers
                .Include(d => d.Vehicle)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(d => d.IsActive);
            }

            var drivers = await query.ToListAsync();
            return drivers.Select(MapToDto);
        }

        public async Task<DriverDto?> GetDriverByIdAsync(int id)
        {
            var driver = await _context.Drivers
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            return driver == null ? null : MapToDto(driver);
        }

        public async Task<DriverDto> CreateDriverAsync(CreateDriverDto driverDto)
        {
            // Check if license number already exists
            var existing = await _context.Drivers
                .FirstOrDefaultAsync(d => d.LicenseNumber == driverDto.LicenseNumber);

            if (existing != null)
            {
                throw new InvalidOperationException($"Driver with license {driverDto.LicenseNumber} already exists.");
            }

            var driver = new Driver
            {
                Name = driverDto.Name,
                Phone = driverDto.Phone,
                LicenseNumber = driverDto.LicenseNumber,
                LicenseExpiry = driverDto.LicenseExpiry,
                Status = "Available",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            var created = await _context.Drivers
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == driver.Id);

            return MapToDto(created!);
        }

        public async Task<DriverDto?> UpdateDriverAsync(int id, UpdateDriverDto driverDto)
        {
            var driver = await _context.Drivers
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (driver == null) return null;

            if (!string.IsNullOrEmpty(driverDto.Name))
                driver.Name = driverDto.Name;

            if (!string.IsNullOrEmpty(driverDto.Phone))
                driver.Phone = driverDto.Phone;

            if (!string.IsNullOrEmpty(driverDto.Status))
                driver.Status = driverDto.Status;

            if (driverDto.LicenseExpiry.HasValue)
                driver.LicenseExpiry = driverDto.LicenseExpiry.Value;

            await _context.SaveChangesAsync();

            var updated = await _context.Drivers
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == id);

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteDriverAsync(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return false;

            // Check if driver is assigned to a vehicle
            var assigned = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DriverId == id && v.IsActive);

            if (assigned != null)
            {
                throw new InvalidOperationException($"Cannot delete driver {driver.Name} - currently assigned to vehicle {assigned.RegistrationNumber}");
            }

            driver.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DriverDto>> GetAvailableDriversAsync()
        {
            var drivers = await _context.Drivers
                .Include(d => d.Vehicle)
                .Where(d => d.IsActive && d.Status == "Available" && d.Vehicle == null)
                .ToListAsync();

            return drivers.Select(MapToDto);
        }

        public async Task<DriverDto?> UpdateDriverStatusAsync(int id, string status)
        {
            var driver = await _context.Drivers
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (driver == null) return null;

            driver.Status = status;
            await _context.SaveChangesAsync();

            return MapToDto(driver);
        }

        private DriverDto MapToDto(Driver driver)
        {
            return new DriverDto
            {
                Id = driver.Id,
                Name = driver.Name,
                Phone = driver.Phone,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiry = driver.LicenseExpiry,
                Status = driver.Status,
                AssignedVehicleId = driver.Vehicle?.Id,
                AssignedVehicleRegistration = driver.Vehicle?.RegistrationNumber,
                IsActive = driver.IsActive
            };
        }
    }
}