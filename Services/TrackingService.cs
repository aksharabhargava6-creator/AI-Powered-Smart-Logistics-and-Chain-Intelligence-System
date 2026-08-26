using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FleetTracking.Data;
using FleetTracking.DTOs;
using FleetTracking.Models;
using System.Collections.Concurrent;

namespace FleetTracking.Services
{
    public class TrackingService : ITrackingService
    {
        private readonly LogisticsDbContext _context;
        private readonly ILogger<TrackingService> _logger;
        private readonly ConcurrentDictionary<int, VehicleStatusCache> _vehicleStatusCache;

        public TrackingService(
            LogisticsDbContext context, 
            ILogger<TrackingService> logger)
        {
            _context = context;
            _logger = logger;
            _vehicleStatusCache = new ConcurrentDictionary<int, VehicleStatusCache>();
        }

        // ===================== LOCATION UPDATES =====================

        public async Task<LocationUpdateDto> UpdateVehicleLocationAsync(
            int vehicleId, 
            double latitude, 
            double longitude, 
            double speed, 
            double? heading = null)
        {
            try
            {
                // Validate coordinates
                if (!IsValidCoordinate(latitude, longitude))
                {
                    throw new ArgumentException("Invalid coordinates. Latitude must be between -90 and 90, Longitude between -180 and 180.");
                }

                // Validate speed
                if (speed < 0 || speed > 200)
                {
                    throw new ArgumentException("Invalid speed. Speed must be between 0 and 200 km/h.");
                }

                // Get vehicle
                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");
                }

                if (!vehicle.IsActive)
                {
                    throw new InvalidOperationException($"Vehicle with ID {vehicleId} is inactive.");
                }

                // Calculate distance since last update
                var oldLat = vehicle.CurrentLatitude;
                var oldLng = vehicle.CurrentLongitude;
                var distance = CalculateDistance(oldLat, oldLng, latitude, longitude);

                // Update vehicle
                vehicle.CurrentLatitude = latitude;
                vehicle.CurrentLongitude = longitude;
                vehicle.LastUpdated = DateTime.UtcNow;

                // Create location history
                var location = new VehicleLocation
                {
                    VehicleId = vehicleId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Speed = Math.Round(speed, 1),
                    Heading = heading,
                    Timestamp = DateTime.UtcNow,
                    DistanceSinceLast = distance,
                    IsMoving = speed > 1
                };

                _context.VehicleLocations.Add(location);

                // Update trip statistics if in transit
                if (vehicle.Status == "InTransit")
                {
                    await UpdateTripStatisticsAsync(vehicleId, distance, speed);
                }

                await _context.SaveChangesAsync();

                // Update cache
                _vehicleStatusCache[vehicleId] = new VehicleStatusCache
                {
                    VehicleId = vehicleId,
                    Status = vehicle.Status,
                    LastUpdated = vehicle.LastUpdated,
                    CurrentLatitude = latitude,
                    CurrentLongitude = longitude,
                    Speed = speed
                };

                _logger.LogDebug($"Updated location for vehicle {vehicleId}: ({latitude:F6}, {longitude:F6}) - {speed} km/h");

                return new LocationUpdateDto
                {
                    VehicleId = vehicleId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Speed = Math.Round(speed, 1),
                    Heading = heading,
                    Timestamp = DateTime.UtcNow,
                    DistanceSinceLast = distance,
                    IsMoving = speed > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle location for vehicle {vehicleId}");
                throw;
            }
        }

        // ===================== HISTORY & ROUTES =====================

        public async Task<VehicleLocationHistoryDto> GetVehicleLocationHistoryAsync(
            int vehicleId, 
            DateTime? from = null, 
            DateTime? to = null, 
            int limit = 1000)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.Id == vehicleId);

                if (vehicle == null)
                {
                    throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");
                }

                var query = _context.VehicleLocations
                    .Where(vl => vl.VehicleId == vehicleId)
                    .OrderByDescending(vl => vl.Timestamp)
                    .AsQueryable();

                if (from.HasValue)
                    query = query.Where(vl => vl.Timestamp >= from.Value);

                if (to.HasValue)
                    query = query.Where(vl => vl.Timestamp <= to.Value);

                var locations = await query
                    .Take(limit)
                    .OrderBy(vl => vl.Timestamp)
                    .ToListAsync();

                var locationDtos = locations.Select(l => new LocationPointDto
                {
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Speed = l.Speed,
                    Heading = l.Heading,
                    Timestamp = l.Timestamp,
                    DistanceSinceLast = l.DistanceSinceLast,
                    IsMoving = l.IsMoving
                }).ToList();

                return new VehicleLocationHistoryDto
                {
                    VehicleId = vehicleId,
                    RegistrationNumber = vehicle.RegistrationNumber,
                    VehicleType = vehicle.VehicleType,
                    Locations = locationDtos,
                    TotalDistance = locationDtos.Sum(l => l.DistanceSinceLast ?? 0),
                    AverageSpeed = locationDtos.Any() ? locationDtos.Average(l => l.Speed) : 0,
                    LocationCount = locationDtos.Count,
                    EarliestLocation = locationDtos.FirstOrDefault()?.Timestamp,
                    LatestLocation = locationDtos.LastOrDefault()?.Timestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting location history for vehicle {vehicleId}");
                throw;
            }
        }

        public async Task<RouteInfoDto> GetVehicleRouteAsync(int vehicleId, DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1);

                var locations = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == vehicleId &&
                                 vl.Timestamp >= startOfDay &&
                                 vl.Timestamp < endOfDay)
                    .OrderBy(vl => vl.Timestamp)
                    .ToListAsync();

                if (!locations.Any())
                {
                    return new RouteInfoDto
                    {
                        VehicleId = vehicleId,
                        Date = date,
                        TotalDistance = 0,
                        Points = new List<LocationPointDto>(),
                        Stops = new List<StopDto>(),
                        TotalDuration = TimeSpan.Zero
                    };
                }

                var points = locations.Select(l => new LocationPointDto
                {
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Speed = l.Speed,
                    Heading = l.Heading,
                    Timestamp = l.Timestamp,
                    IsMoving = l.IsMoving
                }).ToList();

                var stops = CalculateStops(locations);
                var totalDistance = locations.Sum(l => l.DistanceSinceLast ?? 0);
                var startTime = locations.First().Timestamp;
                var endTime = locations.Last().Timestamp;

                return new RouteInfoDto
                {
                    VehicleId = vehicleId,
                    Date = date,
                    Points = points,
                    Stops = stops,
                    TotalDistance = Math.Round(totalDistance, 2),
                    TotalDuration = endTime - startTime,
                    StartTime = startTime,
                    EndTime = endTime,
                    AverageSpeed = totalDistance / (endTime - startTime).TotalHours,
                    StopCount = stops.Count,
                    TotalStopDuration = TimeSpan.FromTicks(stops.Sum(s => s.Duration.Ticks))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting route for vehicle {vehicleId} on {date:d}");
                throw;
            }
        }

        // ===================== STATUS & MONITORING =====================

        public async Task<IEnumerable<VehicleStatusDto>> GetAllVehicleStatusAsync()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Include(v => v.Driver)
                    .Where(v => v.IsActive)
                    .Select(v => new VehicleStatusDto
                    {
                        VehicleId = v.Id,
                        RegistrationNumber = v.RegistrationNumber,
                        Status = v.Status,
                        CurrentLatitude = v.CurrentLatitude,
                        CurrentLongitude = v.CurrentLongitude,
                        LastUpdated = v.LastUpdated,
                        DriverName = v.Driver != null ? v.Driver.Name : null,
                        Speed = _context.VehicleLocations
                            .Where(vl => vl.VehicleId == v.Id)
                            .OrderByDescending(vl => vl.Timestamp)
                            .Select(vl => vl.Speed)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return vehicles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vehicle statuses");
                throw;
            }
        }

        public async Task<VehicleStatusDto?> GetVehicleStatusAsync(int vehicleId)
        {
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Driver)
                    .FirstOrDefaultAsync(v => v.Id == vehicleId && v.IsActive);

                if (vehicle == null) return null;

                var latestLocation = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == vehicleId)
                    .OrderByDescending(vl => vl.Timestamp)
                    .FirstOrDefaultAsync();

                return new VehicleStatusDto
                {
                    VehicleId = vehicle.Id,
                    RegistrationNumber = vehicle.RegistrationNumber,
                    Status = vehicle.Status,
                    CurrentLatitude = vehicle.CurrentLatitude,
                    CurrentLongitude = vehicle.CurrentLongitude,
                    LastUpdated = vehicle.LastUpdated,
                    DriverName = vehicle.Driver?.Name,
                    Speed = latestLocation?.Speed ?? 0,
                    IsMoving = latestLocation?.IsMoving ?? false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vehicle status for vehicle {vehicleId}");
                throw;
            }
        }

        public async Task<Dictionary<int, VehicleStatusDto>> GetVehiclesInRegionAsync(
            double minLat, 
            double maxLat, 
            double minLng, 
            double maxLng)
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Include(v => v.Driver)
                    .Where(v => v.IsActive &&
                               v.CurrentLatitude >= minLat &&
                               v.CurrentLatitude <= maxLat &&
                               v.CurrentLongitude >= minLng &&
                               v.CurrentLongitude <= maxLng)
                    .ToListAsync();

                var result = new Dictionary<int, VehicleStatusDto>();

                foreach (var vehicle in vehicles)
                {
                    var latestLocation = await _context.VehicleLocations
                        .Where(vl => vl.VehicleId == vehicle.Id)
                        .OrderByDescending(vl => vl.Timestamp)
                        .FirstOrDefaultAsync();

                    result[vehicle.Id] = new VehicleStatusDto
                    {
                        VehicleId = vehicle.Id,
                        RegistrationNumber = vehicle.RegistrationNumber,
                        Status = vehicle.Status,
                        CurrentLatitude = vehicle.CurrentLatitude,
                        CurrentLongitude = vehicle.CurrentLongitude,
                        LastUpdated = vehicle.LastUpdated,
                        DriverName = vehicle.Driver?.Name,
                        Speed = latestLocation?.Speed ?? 0
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles in region");
                throw;
            }
        }

        // ===================== ALERTS =====================

        public async Task<IEnumerable<AlertDto>> GetVehicleAlertsAsync(int vehicleId, DateTime? from = null)
        {
            try
            {
                var query = _context.Set<VehicleAlert>()
                    .Where(a => a.VehicleId == vehicleId)
                    .OrderByDescending(a => a.Timestamp)
                    .AsQueryable();

                if (from.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= from.Value);
                }

                var alerts = await query
                    .Take(100)
                    .ToListAsync();

                return alerts.Select(a => new AlertDto
                {
                    Id = a.Id,
                    VehicleId = a.VehicleId,
                    Type = a.Type,
                    Message = a.Message,
                    Severity = a.Severity,
                    Timestamp = a.Timestamp,
                    IsResolved = a.IsResolved,
                    ResolvedAt = a.ResolvedAt,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting alerts for vehicle {vehicleId}");
                throw;
            }
        }

        public async Task<AlertDto> CreateAlertAsync(
            int vehicleId, 
            string type, 
            string message, 
            string severity, 
            double? latitude = null, 
            double? longitude = null)
        {
            try
            {
                var validSeverities = new[] { "Info", "Warning", "Critical" };
                if (!validSeverities.Contains(severity))
                {
                    throw new ArgumentException($"Invalid severity. Must be one of: {string.Join(", ", validSeverities)}");
                }

                var alert = new VehicleAlert
                {
                    VehicleId = vehicleId,
                    Type = type,
                    Message = message,
                    Severity = severity,
                    Timestamp = DateTime.UtcNow,
                    IsResolved = false,
                    Latitude = latitude,
                    Longitude = longitude
                };

                _context.Set<VehicleAlert>().Add(alert);
                await _context.SaveChangesAsync();

                return new AlertDto
                {
                    Id = alert.Id,
                    VehicleId = alert.VehicleId,
                    Type = alert.Type,
                    Message = alert.Message,
                    Severity = alert.Severity,
                    Timestamp = alert.Timestamp,
                    IsResolved = alert.IsResolved,
                    Latitude = alert.Latitude,
                    Longitude = alert.Longitude
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating alert for vehicle {vehicleId}");
                throw;
            }
        }

        public async Task<bool> ResolveAlertAsync(int alertId)
        {
            try
            {
                var alert = await _context.Set<VehicleAlert>().FindAsync(alertId);
                if (alert == null) return false;

                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving alert {alertId}");
                throw;
            }
        }

        // ===================== ANALYTICS =====================

        public async Task<TripSummaryDto> GetTripSummaryAsync(int vehicleId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.Set<VehicleTrip>()
                    .Where(t => t.VehicleId == vehicleId)
                    .AsQueryable();

                if (from.HasValue)
                    query = query.Where(t => t.StartTime >= from.Value);

                if (to.HasValue)
                    query = query.Where(t => t.StartTime <= to.Value);

                var trips = await query
                    .OrderByDescending(t => t.StartTime)
                    .ToListAsync();

                if (!trips.Any())
                {
                    return new TripSummaryDto
                    {
                        VehicleId = vehicleId,
                        TotalDistance = 0,
                        AverageSpeed = 0,
                        MaxSpeed = 0,
                        TotalTrips = 0
                    };
                }

                return new TripSummaryDto
                {
                    VehicleId = vehicleId,
                    TotalDistance = trips.Sum(t => t.Distance),
                    AverageSpeed = trips.Average(t => t.AvgSpeed),
                    MaxSpeed = trips.Max(t => t.MaxSpeed),
                    TotalTrips = trips.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting trip summary for vehicle {vehicleId}");
                throw;
            }
        }

        public async Task<MovementAnalysisDto> GetMovementAnalysisAsync(
            int vehicleId, 
            DateTime startDate, 
            DateTime endDate)
        {
            try
            {
                var locations = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == vehicleId &&
                                 vl.Timestamp >= startDate &&
                                 vl.Timestamp <= endDate)
                    .OrderBy(vl => vl.Timestamp)
                    .ToListAsync();

                var analysis = new MovementAnalysisDto
                {
                    VehicleId = vehicleId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDistance = locations.Sum(l => l.DistanceSinceLast ?? 0),
                    TotalTrips = await _context.Set<VehicleTrip>()
                        .CountAsync(t => t.VehicleId == vehicleId &&
                                        t.StartTime >= startDate &&
                                        t.StartTime <= endDate)
                };

                analysis.TotalDrivingHours = locations
                    .Where(l => l.IsMoving)
                    .GroupBy(l => l.Timestamp.Date)
                    .Select(g => g.Count() * 2 / 3600.0) // 2 seconds per point
                    .Sum();

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting movement analysis for vehicle {vehicleId}");
                throw;
            }
        }

        public async Task<LocationStatisticsDto> GetLocationStatisticsAsync(int vehicleId, DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1);

                var locations = await _context.VehicleLocations
                    .Where(vl => vl.VehicleId == vehicleId &&
                                 vl.Timestamp >= startOfDay &&
                                 vl.Timestamp < endOfDay)
                    .ToListAsync();

                return new LocationStatisticsDto
                {
                    VehicleId = vehicleId,
                    Date = date,
                    TotalDistance = locations.Sum(l => l.DistanceSinceLast ?? 0),
                    AverageSpeed = locations.Any() ? locations.Average(l => l.Speed) : 0,
                    MaxSpeed = locations.Any() ? locations.Max(l => l.Speed) : 0,
                    MinSpeed = locations.Any() ? locations.Min(l => l.Speed) : 0,
                    StopCount = locations.Count(l => !l.IsMoving && l.Speed < 1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting location statistics for vehicle {vehicleId}");
                throw;
            }
        }

        // ===================== HELPER METHODS =====================

        private bool IsValidCoordinate(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 &&
                   longitude >= -180 && longitude <= 180;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Earth's radius in kilometers
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private List<StopDto> CalculateStops(List<VehicleLocation> locations)
        {
            var stops = new List<StopDto>();
            var currentStop = new List<VehicleLocation>();
            const double speedThreshold = 1.0;
            const int minStopDuration = 5;

            foreach (var location in locations)
            {
                if (location.Speed < speedThreshold)
                {
                    currentStop.Add(location);
                }
                else if (currentStop.Any())
                {
                    var duration = currentStop.Last().Timestamp - currentStop.First().Timestamp;
                    if (duration.TotalMinutes >= minStopDuration)
                    {
                        stops.Add(new StopDto
                        {
                            Latitude = currentStop.Average(l => l.Latitude),
                            Longitude = currentStop.Average(l => l.Longitude),
                            StartTime = currentStop.First().Timestamp,
                            EndTime = currentStop.Last().Timestamp,
                            Duration = duration
                        });
                    }
                    currentStop.Clear();
                }
            }

            if (currentStop.Any())
            {
                var duration = currentStop.Last().Timestamp - currentStop.First().Timestamp;
                if (duration.TotalMinutes >= minStopDuration)
                {
                    stops.Add(new StopDto
                    {
                        Latitude = currentStop.Average(l => l.Latitude),
                        Longitude = currentStop.Average(l => l.Longitude),
                        StartTime = currentStop.First().Timestamp,
                        EndTime = currentStop.Last().Timestamp,
                        Duration = duration,
                        IsOngoing = true
                    });
                }
            }

            return stops;
        }

        private async Task UpdateTripStatisticsAsync(int vehicleId, double distance, double speed)
        {
            var currentTrip = await _context.Set<VehicleTrip>()
                .FirstOrDefaultAsync(t => t.VehicleId == vehicleId && t.EndTime == null);

            if (currentTrip == null)
            {
                currentTrip = new VehicleTrip
                {
                    VehicleId = vehicleId,
                    StartTime = DateTime.UtcNow,
                    StartLatitude = _context.Vehicles.Find(vehicleId)?.CurrentLatitude ?? 0,
                    StartLongitude = _context.Vehicles.Find(vehicleId)?.CurrentLongitude ?? 0,
                    MaxSpeed = speed,
                    AvgSpeed = speed,
                    Distance = distance
                };
                _context.Set<VehicleTrip>().Add(currentTrip);
            }
            else
            {
                currentTrip.Distance += distance;
                currentTrip.MaxSpeed = Math.Max(currentTrip.MaxSpeed, speed);
                currentTrip.AvgSpeed = (currentTrip.AvgSpeed + speed) / 2;
            }
        }

        private class VehicleStatusCache
        {
            public int VehicleId { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime LastUpdated { get; set; }
            public double CurrentLatitude { get; set; }
            public double CurrentLongitude { get; set; }
            public double Speed { get; set; }
        }
    }
}
