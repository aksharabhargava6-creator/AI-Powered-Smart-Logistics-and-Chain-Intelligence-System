using Microsoft.EntityFrameworkCore;
using FleetTracking.Data;
using FleetTracking.DTOs;
using FleetTracking.Models;

namespace FleetTracking.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly LogisticsDbContext _context;

        public VehicleService(LogisticsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync(bool includeInactive = false)
        {
            var query = _context.Vehicles
                .Include(v => v.Driver)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(v => v.IsActive);
            }

            var vehicles = await query.ToListAsync();
            return vehicles.Select(MapToDto);
        }

        public async Task<VehicleDto?> GetVehicleByIdAsync(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

            return vehicle == null ? null : MapToDto(vehicle);
        }

        public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto vehicleDto)
        {
            // Check if registration number already exists
            var existing = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.RegistrationNumber == vehicleDto.RegistrationNumber);

            if (existing != null)
            {
                throw new InvalidOperationException($"Vehicle with registration {vehicleDto.RegistrationNumber} already exists.");
            }

            var vehicle = new Vehicle
            {
                RegistrationNumber = vehicleDto.RegistrationNumber,
                VehicleType = vehicleDto.VehicleType,
                Capacity = vehicleDto.Capacity,
                DriverId = vehicleDto.DriverId,
                Status = "Available",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            var created = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == vehicle.Id);

            return MapToDto(created!);
        }

        public async Task<VehicleDto?> UpdateVehicleAsync(int id, UpdateVehicleDto vehicleDto)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

            if (vehicle == null) return null;

            if (!string.IsNullOrEmpty(vehicleDto.VehicleType))
                vehicle.VehicleType = vehicleDto.VehicleType;

            if (vehicleDto.Capacity.HasValue)
                vehicle.Capacity = vehicleDto.Capacity.Value;

            if (!string.IsNullOrEmpty(vehicleDto.Status))
                vehicle.Status = vehicleDto.Status;

            if (vehicleDto.DriverId.HasValue)
                vehicle.DriverId = vehicleDto.DriverId;

            if (vehicleDto.CurrentLatitude.HasValue)
                vehicle.CurrentLatitude = vehicleDto.CurrentLatitude.Value;

            if (vehicleDto.CurrentLongitude.HasValue)
                vehicle.CurrentLongitude = vehicleDto.CurrentLongitude.Value;

            vehicle.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload to get updated driver info
            var updated = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == id);

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteVehicleAsync(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return false;

            // Soft delete
            vehicle.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VehicleDto?> UpdateVehicleLocationAsync(int id, double latitude, double longitude, double speed)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return null;

            vehicle.CurrentLatitude = latitude;
            vehicle.CurrentLongitude = longitude;
            vehicle.LastUpdated = DateTime.UtcNow;

            // Save location history
            var location = new VehicleLocation
            {
                VehicleId = id,
                Latitude = latitude,
                Longitude = longitude,
                Speed = speed,
                Timestamp = DateTime.UtcNow
            };

            _context.VehicleLocations.Add(location);
            await _context.SaveChangesAsync();

            // Get updated vehicle with driver info
            var updated = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == id);

            return MapToDto(updated!);
        }

        public async Task<IEnumerable<LocationPointDto>> GetVehicleLocationHistoryAsync(int id, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.VehicleLocations
                .Where(vl => vl.VehicleId == id)
                .OrderByDescending(vl => vl.Timestamp);

            if (from.HasValue)
                query = query.Where(vl => vl.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(vl => vl.Timestamp <= to.Value);

            var locations = await query
                .Take(1000)
                .ToListAsync();

            return locations.Select(l => new LocationPointDto
            {
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Speed = l.Speed,
                Timestamp = l.Timestamp
            });
        }

        public async Task<VehicleDto?> AssignDriverAsync(int vehicleId, int driverId)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == vehicleId && v.IsActive);

            if (vehicle == null) return null;

            // Check if driver exists and is available
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == driverId && d.IsActive);

            if (driver == null) return null;

            // Check if driver is already assigned to another vehicle
            var existingAssignment = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DriverId == driverId && v.IsActive && v.Id != vehicleId);

            if (existingAssignment != null)
            {
                throw new InvalidOperationException($"Driver {driver.Name} is already assigned to vehicle {existingAssignment.RegistrationNumber}");
            }

            vehicle.DriverId = driverId;
            vehicle.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updated = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            return MapToDto(updated!);
        }

        public async Task<VehicleDto?> UpdateVehicleStatusAsync(int id, string status)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

            if (vehicle == null) return null;

            vehicle.Status = status;
            vehicle.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(vehicle);
        }

        public async Task<IEnumerable<VehicleDto>> GetVehiclesByStatusAsync(string status)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Driver)
                .Where(v => v.IsActive && v.Status == status)
                .ToListAsync();

            return vehicles.Select(MapToDto);
        }

        private VehicleDto MapToDto(Vehicle vehicle)
        {
            // Get latest speed from location history
            var latestLocation = _context.VehicleLocations
                .Where(vl => vl.VehicleId == vehicle.Id)
                .OrderByDescending(vl => vl.Timestamp)
                .FirstOrDefault();

            return new VehicleDto
            {
                Id = vehicle.Id,
                RegistrationNumber = vehicle.RegistrationNumber,
                VehicleType = vehicle.VehicleType,
                Capacity = vehicle.Capacity,
                Status = vehicle.Status,
                DriverId = vehicle.DriverId,
                DriverName = vehicle.Driver?.Name,
                CurrentLatitude = vehicle.CurrentLatitude,
                CurrentLongitude = vehicle.CurrentLongitude,
                LastUpdated = vehicle.LastUpdated,
                CurrentSpeed = latestLocation?.Speed,
                IsActive = vehicle.IsActive
            };
        }
    }
}