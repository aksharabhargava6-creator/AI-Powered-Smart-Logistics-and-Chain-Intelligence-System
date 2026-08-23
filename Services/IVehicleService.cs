using FleetTracking.DTOs;

namespace FleetTracking.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync(bool includeInactive = false);
        Task<VehicleDto?> GetVehicleByIdAsync(int id);
        Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto vehicleDto);
        Task<VehicleDto?> UpdateVehicleAsync(int id, UpdateVehicleDto vehicleDto);
        Task<bool> DeleteVehicleAsync(int id);
        Task<VehicleDto?> UpdateVehicleLocationAsync(int id, double latitude, double longitude, double speed);
        Task<IEnumerable<LocationPointDto>> GetVehicleLocationHistoryAsync(int id, DateTime? from = null, DateTime? to = null);
        Task<VehicleDto?> AssignDriverAsync(int vehicleId, int driverId);
        Task<VehicleDto?> UpdateVehicleStatusAsync(int id, string status);
        Task<IEnumerable<VehicleDto>> GetVehiclesByStatusAsync(string status);
    }
}