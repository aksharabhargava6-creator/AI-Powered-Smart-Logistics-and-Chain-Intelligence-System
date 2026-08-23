using Microsoft.EntityFrameworkCore;
using FleetTracking.Data;
using FleetTracking.DTOs;
using FleetTracking.Models;
using Microsoft.Extensions.Logging;

namespace FleetTracking.Services
{
    public class DriverService : IDriverService
    {
        private readonly LogisticsDbContext _context;
        private readonly ILogger<DriverService> _logger;

        public DriverService(LogisticsDbContext context, ILogger<DriverService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<DriverDto>> GetAllDriversAsync(bool includeInactive = false)
        {
            try
            {
                var query = _context.Drivers
                    .Include(d => d.Vehicle)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(d => d.IsActive);
                }

                var drivers = await query
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return drivers.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all drivers");
                throw;
            }
        }

        public async Task<DriverDto?> GetDriverByIdAsync(int id)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

                return driver == null ? null : MapToDto(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting driver with ID {id}");
                throw;
            }
        }

        public async Task<DriverDto> CreateDriverAsync(CreateDriverDto driverDto)
        {
            try
            {
                // Validate license number
                if (string.IsNullOrWhiteSpace(driverDto.LicenseNumber))
                {
                    throw new ArgumentException("License number is required");
                }

                // Check if license number already exists
                var existing = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.LicenseNumber == driverDto.LicenseNumber);

                if (existing != null)
                {
                    throw new InvalidOperationException($"Driver with license number {driverDto.LicenseNumber} already exists.");
                }

                // Check if phone number already exists
                var existingPhone = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.Phone == driverDto.Phone);

                if (existingPhone != null)
                {
                    throw new InvalidOperationException($"Driver with phone number {driverDto.Phone} already exists.");
                }

                // Validate license expiry
                if (driverDto.LicenseExpiry <= DateTime.UtcNow)
                {
                    throw new ArgumentException("License expiry date must be in the future");
                }

                var driver = new Driver
                {
                    Name = driverDto.Name.Trim(),
                    Phone = driverDto.Phone.Trim(),
                    LicenseNumber = driverDto.LicenseNumber.Trim().ToUpper(),
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating driver: {driverDto.Name}");
                throw;
            }
        }

        public async Task<DriverDto?> UpdateDriverAsync(int id, UpdateDriverDto driverDto)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

                if (driver == null) return null;

                if (!string.IsNullOrWhiteSpace(driverDto.Name))
                {
                    driver.Name = driverDto.Name.Trim();
                }

                if (!string.IsNullOrWhiteSpace(driverDto.Phone))
                {
                    // Check if phone number is being used by another driver
                    var existingPhone = await _context.Drivers
                        .FirstOrDefaultAsync(d => d.Phone == driverDto.Phone && d.Id != id);

                    if (existingPhone != null)
                    {
                        throw new InvalidOperationException($"Phone number {driverDto.Phone} is already used by another driver.");
                    }
                    driver.Phone = driverDto.Phone.Trim();
                }

                if (!string.IsNullOrWhiteSpace(driverDto.Status))
                {
                    // Validate status
                    var validStatuses = new[] { "Available", "OnDuty", "OffDuty" };
                    if (!validStatuses.Contains(driverDto.Status))
                    {
                        throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
                    }

                    // If driver is being set to OnDuty, they must have a vehicle assigned
                    if (driverDto.Status == "OnDuty" && driver.Vehicle == null)
                    {
                        throw new InvalidOperationException("Cannot set driver to OnDuty without an assigned vehicle.");
                    }

                    driver.Status = driverDto.Status;
                }

                if (driverDto.LicenseExpiry.HasValue)
                {
                    if (driverDto.LicenseExpiry.Value <= DateTime.UtcNow)
                    {
                        throw new ArgumentException("License expiry date must be in the future");
                    }
                    driver.LicenseExpiry = driverDto.LicenseExpiry.Value;
                }

                await _context.SaveChangesAsync();

                var updated = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id);

                return MapToDto(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver with ID {id}");
                throw;
            }
        }

        public async Task<bool> DeleteDriverAsync(int id)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (driver == null) return false;

                // Check if driver is assigned to an active vehicle
                if (driver.Vehicle != null && driver.Vehicle.IsActive)
                {
                    throw new InvalidOperationException(
                        $"Cannot delete driver {driver.Name} - currently assigned to vehicle {driver.Vehicle.RegistrationNumber}.\n" +
                        "Please unassign the driver from the vehicle first.");
                }

                // Soft delete - just mark as inactive
                driver.IsActive = false;
                
                // If driver is assigned to a vehicle, unassign them
                if (driver.Vehicle != null)
                {
                    driver.Vehicle.DriverId = null;
                    driver.Vehicle.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting driver with ID {id}");
                throw;
            }
        }

        public async Task<IEnumerable<DriverDto>> GetAvailableDriversAsync()
        {
            try
            {
                var drivers = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .Where(d => d.IsActive && 
                               d.Status == "Available" && 
                               d.Vehicle == null)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return drivers.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available drivers");
                throw;
            }
        }

        public async Task<DriverDto?> UpdateDriverStatusAsync(int id, string status)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

                if (driver == null) return null;

                // Validate status
                var validStatuses = new[] { "Available", "OnDuty", "OffDuty" };
                if (!validStatuses.Contains(status))
                {
                    throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
                }

                // If setting to OnDuty, driver must have a vehicle
                if (status == "OnDuty" && driver.Vehicle == null)
                {
                    throw new InvalidOperationException("Cannot set driver to OnDuty without an assigned vehicle.");
                }

                // If setting to Available, ensure driver is not on duty
                if (status == "Available" && driver.Vehicle != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot set driver to Available while assigned to vehicle {driver.Vehicle.RegistrationNumber}.\n" +
                        "Please unassign the driver from the vehicle first.");
                }

                driver.Status = status;
                await _context.SaveChangesAsync();

                var updated = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id);

                return MapToDto(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver status for ID {id}");
                throw;
            }
        }

        public async Task<DriverDto?> GetDriverByLicenseNumberAsync(string licenseNumber)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.LicenseNumber == licenseNumber.ToUpper() && d.IsActive);

                return driver == null ? null : MapToDto(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting driver by license number {licenseNumber}");
                throw;
            }
        }

        public async Task<DriverDto?> GetDriverByPhoneAsync(string phone)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Phone == phone && d.IsActive);

                return driver == null ? null : MapToDto(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting driver by phone {phone}");
                throw;
            }
        }

        public async Task<IEnumerable<DriverDto>> GetDriversWithExpiringLicensesAsync(int daysThreshold = 30)
        {
            try
            {
                var threshold = DateTime.UtcNow.AddDays(daysThreshold);
                var drivers = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .Where(d => d.IsActive && 
                               d.LicenseExpiry <= threshold && 
                               d.LicenseExpiry > DateTime.UtcNow)
                    .OrderBy(d => d.LicenseExpiry)
                    .ToListAsync();

                return drivers.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting drivers with expiring licenses (threshold: {daysThreshold} days)");
                throw;
            }
        }

        public async Task<DriverDto?> UnassignDriverFromVehicleAsync(int driverId)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == driverId && d.IsActive);

                if (driver == null) return null;

                if (driver.Vehicle == null)
                {
                    throw new InvalidOperationException($"Driver {driver.Name} is not assigned to any vehicle.");
                }

                var vehicle = driver.Vehicle;
                driver.Vehicle = null;
                vehicle.DriverId = null;
                vehicle.LastUpdated = DateTime.UtcNow;

                // If driver was OnDuty, set them to Available
                if (driver.Status == "OnDuty")
                {
                    driver.Status = "Available";
                }

                await _context.SaveChangesAsync();

                var updated = await _context.Drivers
                    .Include(d => d.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == driverId);

                return MapToDto(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unassigning driver {driverId} from vehicle");
                throw;
            }
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
                IsActive = driver.IsActive,
                CreatedAt = driver.CreatedAt,
                DaysUntilLicenseExpiry = (int)(driver.LicenseExpiry - DateTime.UtcNow).TotalDays
            };
        }
    }
}