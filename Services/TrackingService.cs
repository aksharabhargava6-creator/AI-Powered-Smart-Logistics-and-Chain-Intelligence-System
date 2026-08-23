using Microsoft.EntityFrameworkCore;
using FleetTracking.Data;
using FleetTracking.DTOs;
using FleetTracking.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FleetTracking.Services
{
    public class TrackingService : ITrackingService
    {
        private readonly LogisticsDbContext _context;
        private readonly ILogger<TrackingService> _logger;
        private readonly ConcurrentDictionary<int, VehicleStatus> _vehicleStatusCache;
        private readonly SemaphoreSlim _cacheLock;

        public TrackingService(LogisticsDbContext context, ILogger<TrackingService> logger)
        {
            _context = context;
            _logger = logger;
            _vehicleStatusCache = new ConcurrentDictionary<int, VehicleStatus>();
            _cacheLock = new SemaphoreSlim(1, 1);
            
            // Initialize cache
            InitializeCacheAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task InitializeCacheAsync()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.IsActive)
                    .Select(v => new VehicleStatus
                    {
                        VehicleId = v.Id,
                        Status = v.Status,
                        LastUpdated = v.LastUpdated
                    })
                    .ToListAsync();

                foreach (var vehicle in vehicles)
                {
                    _vehicleStatusCache[vehicle.VehicleId] = vehicle;
                }

                _logger.LogInformation($"Initialized vehicle status cache with {_vehicleStatusCache.Count} vehicles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing vehicle status cache");
            }
        }

        public async Task<LocationUpdateDto> UpdateVehicleLocationAsync(int vehicleId, double latitude, double longitude, double speed, double? heading = null)
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

                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");
                }

                if (!vehicle.IsActive)
                {
                    throw new InvalidOperationException($"Vehicle with ID {vehicleId} is inactive.");
                }

                // Update vehicle location
                var oldLatitude = vehicle.CurrentLatitude;
                var oldLongitude = vehicle.CurrentLongitude;
                var distance = CalculateDistance(oldLatitude, oldLongitude, latitude, longitude);

                vehicle.CurrentLatitude = latitude;
                vehicle.CurrentLongitude = longitude;
                vehicle.LastUpdated = DateTime.UtcNow;

                // Create location history entry
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

                // Calculate trip statistics if vehicle is in transit
                if (vehicle.Status == "InTransit")
                {
                    await UpdateTripStatisticsAsync(vehicleId, distance, speed);
                }

                await _context.SaveChangesAsync();

                // Update cache
                _vehicleStatusCache[vehicleId] = new VehicleStatus
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

        public async Task<VehicleLocationHistoryDto> GetVehicleLocationHistoryAsync(int vehicleId, DateTime? from = null, DateTime? to = null, int limit = 1000)
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
                {
                    query = query.Where(vl => vl.Timestamp >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(vl => vl.Timestamp <= to.Value);
                }

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
                    TotalTrips = await GetTripCountAsync(vehicleId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting location history for vehicle {vehicleId}");
                throw;
            }
        }

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

                // Calculate stops (locations where speed < 1 km/h for more than 5 minutes)
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
                    AverageSpeed = totalDistance / (endTime - startTime).TotalHours
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting route for vehicle {vehicleId} on {date:d}");
                throw;
            }
        }

        public async Task<IEnumerable<AlertDto>> GetVehicleAlertsAsync(int vehicleId, DateTime? from = null)
        {
            try
            {
                var query = _context.VehicleAlerts
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

        public async Task<AlertDto> CreateAlertAsync(int vehicleId, string type, string message, string severity, double? latitude = null, double? longitude = null)
        {
            try
            {
                // Validate severity
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

                _context.VehicleAlerts.Add(alert);
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
                var alert = await _context.VehicleAlerts.FindAsync(alertId);
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

        public async Task<Dictionary<int, VehicleStatusDto>> GetVehiclesInRegionAsync(double minLat, double maxLat, double minLng, double maxLng)
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

        // Helper methods
        private bool IsValidCoordinate(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 &&
                   longitude >= -180 && longitude <= 180;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for distance calculation
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
            const double speedThreshold = 1.0; // km/h
            const int minStopDuration = 5; // minutes

            foreach (var location in locations)
            {
                if (location.Speed < speedThreshold)
                {
                    currentStop.Add(location);
                }
                else if (currentStop.Any())
                {
                    // Check if stop lasted more than minimum duration
                    var duration = currentStop.Last().Timestamp - currentStop.First().Timestamp;
                    if (duration.TotalMinutes >= minStopDuration)
                    {
                        var avgLat = currentStop.Average(l => l.Latitude);
                        var avgLng = currentStop.Average(l => l.Longitude);
                        stops.Add(new StopDto
                        {
                            Latitude = avgLat,
                            Longitude = avgLng,
                            StartTime = currentStop.First().Timestamp,
                            EndTime = currentStop.Last().Timestamp,
                            Duration = duration
                        });
                    }
                    currentStop.Clear();
                }
            }

            // Check for ongoing stop at the end
            if (currentStop.Any())
            {
                var duration = currentStop.Last().Timestamp - currentStop.First().Timestamp;
                if (duration.TotalMinutes >= minStopDuration)
                {
                    var avgLat = currentStop.Average(l => l.Latitude);
                    var avgLng = currentStop.Average(l => l.Longitude);
                    stops.Add(new StopDto
                    {
                        Latitude = avgLat,
                        Longitude = avgLng,
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
            // Find current trip or create new one
            var currentTrip = await _context.VehicleTrips
                .FirstOrDefaultAsync(t => t.VehicleId == vehicleId && 
                                         t.EndTime == null);

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
                _context.VehicleTrips.Add(currentTrip);
            }
            else
            {
                currentTrip.Distance += distance;
                currentTrip.MaxSpeed = Math.Max(currentTrip.MaxSpeed, speed);
                currentTrip.AvgSpeed = (currentTrip.AvgSpeed + speed) / 2;
                currentTrip.LastLatitude = _context.Vehicles.Find(vehicleId)?.CurrentLatitude ?? 0;
                currentTrip.LastLongitude = _context.Vehicles.Find(vehicleId)?.CurrentLongitude ?? 0;
            }
        }

        private async Task<int> GetTripCountAsync(int vehicleId)
        {
            return await _context.VehicleTrips
                .CountAsync(t => t.VehicleId == vehicleId);
        }

        // Internal class for cache
        private class VehicleStatus
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