using Microsoft.AspNetCore.SignalR;
using FleetTracking.DTOs;
using FleetTracking.Hubs;
using FleetTracking.Services;

namespace FleetTracking.Simulation
{
    public class GpsSimulatorService : BackgroundService
    {
        private readonly IHubContext<TrackingHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GpsSimulatorService> _logger;
        private readonly Random _random = new();

        private readonly List<SimulatedVehicle> _vehicles = new()
        {
            new SimulatedVehicle 
            { 
                VehicleId = 1, 
                Latitude = 23.2599, 
                Longitude = 77.4126,
                Speed = 40,
                Direction = 0 
            },
            new SimulatedVehicle 
            { 
                VehicleId = 2, 
                Latitude = 23.2500, 
                Longitude = 77.4200,
                Speed = 35,
                Direction = 45
            },
            new SimulatedVehicle 
            { 
                VehicleId = 3, 
                Latitude = 23.2700, 
                Longitude = 77.4000,
                Speed = 50,
                Direction = 90
            },
            new SimulatedVehicle 
            { 
                VehicleId = 4, 
                Latitude = 23.2400, 
                Longitude = 77.4300,
                Speed = 30,
                Direction = 135
            }
        };

        public GpsSimulatorService(
            IHubContext<TrackingHub> hubContext,
            IServiceProvider serviceProvider,
            ILogger<GpsSimulatorService> logger)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GPS Simulator Service started.");

            try
            {
                await InitializeVehiclePositionsAsync();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var tasks = _vehicles.Select(vehicle => 
                        UpdateVehiclePositionAsync(vehicle, stoppingToken));
                    
                    await Task.WhenAll(tasks);

                    await Task.Delay(2000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GPS Simulator Service stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GPS Simulator Service");
            }
        }

        private async Task UpdateVehiclePositionAsync(SimulatedVehicle vehicle, CancellationToken cancellationToken)
        {
            try
            {
                var deltaLat = _random.NextDouble() * 0.001 - 0.0005;
                var deltaLng = _random.NextDouble() * 0.001 - 0.0005;

                if (_random.NextDouble() < 0.1) 
                {
                    vehicle.Direction = (vehicle.Direction + (_random.NextDouble() * 60 - 30)) % 360;
                }

                var speedKmh = vehicle.Speed + (_random.NextDouble() * 10 - 5);
                speedKmh = Math.Max(0, speedKmh); 

                const double kmPerDegree = 111.0;
                var speedDegPerSecond = speedKmh / kmPerDegree / 3600; 

                var headingRad = vehicle.Direction * Math.PI / 180;
                var distance = speedDegPerSecond * 2;

                vehicle.Latitude += distance * Math.Cos(headingRad);
                vehicle.Longitude += distance * Math.Sin(headingRad);

                vehicle.Latitude += (_random.NextDouble() - 0.5) * 0.00005;
                vehicle.Longitude += (_random.NextDouble() - 0.5) * 0.00005;

                vehicle.Latitude = Math.Clamp(vehicle.Latitude, 23.20, 23.30);
                vehicle.Longitude = Math.Clamp(vehicle.Longitude, 77.30, 77.50);

                var location = new LocationUpdateDto
                {
                    VehicleId = vehicle.VehicleId,
                    Latitude = vehicle.Latitude,
                    Longitude = vehicle.Longitude,
                    Speed = Math.Round(speedKmh, 1),
                    Heading = vehicle.Direction,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients
                    .Group($"vehicle-{vehicle.VehicleId}")
                    .SendAsync("VehicleLocationUpdated", location, cancellationToken);

                await _hubContext.Clients
                    .Group("all-vehicles")
                    .SendAsync("AllVehiclesLocationUpdated", location, cancellationToken);

                await SaveLocationToDatabaseAsync(vehicle.VehicleId, 
                    vehicle.Latitude, vehicle.Longitude, speedKmh);

                _logger.LogDebug($"Vehicle {vehicle.VehicleId} updated: " +
                    $"({vehicle.Latitude:F6}, {vehicle.Longitude:F6}) - {speedKmh:F1} km/h");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle {vehicle.VehicleId}");
            }
        }

        private async Task SaveLocationToDatabaseAsync(int vehicleId, double latitude, double longitude, double speed)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

                await vehicleService.UpdateVehicleLocationAsync(vehicleId, latitude, longitude, speed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving location for vehicle {vehicleId}");
            }
        }

        private async Task InitializeVehiclePositionsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

                foreach (var vehicle in _vehicles)
                {
                    await vehicleService.UpdateVehicleLocationAsync(
                        vehicle.VehicleId, 
                        vehicle.Latitude, 
                        vehicle.Longitude, 
                        vehicle.Speed);
                }
                
                _logger.LogInformation("Initialized vehicle positions in database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing vehicle positions");
            }
        }

        private class SimulatedVehicle
        {
            public int VehicleId { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Speed { get; set; }
            public double Direction { get; set; }
        }
    }
}
