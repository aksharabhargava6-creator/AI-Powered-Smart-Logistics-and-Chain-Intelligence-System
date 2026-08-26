using FleetTracking.DTOs;

namespace FleetTracking.Services
{
    public interface ITrackingService
    {
        Task<LocationUpdateDto> UpdateVehicleLocationAsync(
            int vehicleId, 
            double latitude, 
            double longitude, 
            double speed, 
            double? heading = null);

        Task<VehicleLocationHistoryDto> GetVehicleLocationHistoryAsync(
            int vehicleId, 
            DateTime? from = null, 
            DateTime? to = null, 
            int limit = 1000);

        Task<RouteInfoDto> GetVehicleRouteAsync(int vehicleId, DateTime date);

        Task<IEnumerable<VehicleStatusDto>> GetAllVehicleStatusAsync();
        Task<VehicleStatusDto?> GetVehicleStatusAsync(int vehicleId);
        Task<Dictionary<int, VehicleStatusDto>> GetVehiclesInRegionAsync(
            double minLat, 
            double maxLat, 
            double minLng, 
            double maxLng);

        Task<IEnumerable<AlertDto>> GetVehicleAlertsAsync(
            int vehicleId, 
            DateTime? from = null);
        Task<AlertDto> CreateAlertAsync(
            int vehicleId, 
            string type, 
            string message, 
            string severity, 
            double? latitude = null, 
            double? longitude = null);
        Task<bool> ResolveAlertAsync(int alertId);

        Task<TripSummaryDto> GetTripSummaryAsync(
            int vehicleId, 
            DateTime? from = null, 
            DateTime? to = null);
        Task<MovementAnalysisDto> GetMovementAnalysisAsync(
            int vehicleId, 
            DateTime startDate, 
            DateTime endDate);
        Task<LocationStatisticsDto> GetLocationStatisticsAsync(
            int vehicleId, 
            DateTime date);
    }
}
