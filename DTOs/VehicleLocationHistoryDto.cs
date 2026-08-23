namespace FleetTracking.DTOs
{
    /// <summary>
    /// DTO for vehicle location history with detailed trip information
    /// </summary>
    public class VehicleLocationHistoryDto
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public List<LocationPointDto> Locations { get; set; } = new();
        public double TotalDistance { get; set; } // Total distance in kilometers
        public double AverageSpeed { get; set; } // Average speed in km/h
        public int TotalTrips { get; set; }
        public DateTime? EarliestLocation { get; set; }
        public DateTime? LatestLocation { get; set; }
        public int LocationCount { get; set; }
        public Dictionary<string, object>? Statistics { get; set; }
    }

    /// <summary>
    /// DTO for individual location point with movement data
    /// </summary>
    public class LocationPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime Timestamp { get; set; }
        public double? DistanceSinceLast { get; set; } // Distance since previous point in km
        public bool IsMoving { get; set; }
        public string? Address { get; set; } // Optional: Reverse geocoded address
        public double? Elevation { get; set; } // Optional: Elevation in meters
        public int? OdometerReading { get; set; } // Optional: Odometer reading in km
    }

    /// <summary>
    /// DTO for detailed route information including stops
    /// </summary>
    public class RouteInfoDto
    {
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public List<LocationPointDto> Points { get; set; } = new();
        public List<StopDto> Stops { get; set; } = new();
        public double TotalDistance { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double AverageSpeed { get; set; }
        public double? MaxSpeed { get; set; }
        public double? MinSpeed { get; set; }
        public int StopCount { get; set; }
        public TimeSpan TotalStopDuration { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// DTO for stops during a route
    /// </summary>
    public class StopDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsOngoing { get; set; }
        public string? LocationName { get; set; } // Optional: Name of the stop location
        public double? DistanceFromPrevious { get; set; } // Distance from previous stop
        public double? DistanceToNext { get; set; } // Distance to next stop
    }

    /// <summary>
    /// DTO for vehicle trip summaries
    /// </summary>
    public class TripSummaryDto
    {
        public int TripId { get; set; }
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double? EndLatitude { get; set; }
        public double? EndLongitude { get; set; }
        public double TotalDistance { get; set; }
        public double AverageSpeed { get; set; }
        public double MaxSpeed { get; set; }
        public int StopsCount { get; set; }
        public TimeSpan? TotalStopDuration { get; set; }
        public string? TripStatus { get; set; } // InProgress, Completed
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// DTO for vehicle alerts and notifications
    /// </summary>
    public class AlertDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Type { get; set; } = string.Empty; // Speed, Geofence, Maintenance, etc.
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
        public DateTime Timestamp { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? SpeedAtAlert { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// DTO for vehicle status information
    /// </summary>
    public class VehicleStatusDto
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? DriverName { get; set; }
        public double Speed { get; set; }
        public bool IsMoving { get; set; }
        public double? Heading { get; set; }
        public string? LastAddress { get; set; }
        public double? DistanceToday { get; set; }
        public double? BatteryLevel { get; set; } // Optional: For electric vehicles
        public string? FuelLevel { get; set; } // Optional: Fuel level percentage
    }

    /// <summary>
    /// DTO for real-time tracking updates
    /// </summary>
    public class RealTimeUpdateDto
    {
        public int VehicleId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsMoving { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// DTO for vehicle location statistics
    /// </summary>
    public class LocationStatisticsDto
    {
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public double TotalDistance { get; set; }
        public double AverageSpeed { get; set; }
        public double MaxSpeed { get; set; }
        public double MinSpeed { get; set; }
        public TimeSpan TotalDrivingTime { get; set; }
        public TimeSpan TotalIdleTime { get; set; }
        public int StopCount { get; set; }
        public double FuelConsumption { get; set; } // Optional: Fuel consumption in liters
        public Dictionary<int, double> SpeedDistribution { get; set; } = new(); // Speed distribution by hour
        public List<LocationPointDto> PeakLocations { get; set; } = new(); // Most visited locations
    }

    /// <summary>
    /// DTO for geofence alerts
    /// </summary>
    public class GeofenceAlertDto
    {
        public int AlertId { get; set; }
        public int VehicleId { get; set; }
        public string GeofenceName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // Entry, Exit, Stay
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsResolved { get; set; }
        public double? DurationInside { get; set; } // Duration inside geofence in minutes
    }

    /// <summary>
    /// DTO for vehicle maintenance alerts
    /// </summary>
    public class MaintenanceAlertDto
    {
        public int AlertId { get; set; }
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string MaintenanceType { get; set; } = string.Empty; // Oil Change, Tire Rotation, etc.
        public DateTime DueDate { get; set; }
        public int MilesSinceLastService { get; set; }
        public int MilesUntilDue { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// DTO for vehicle movement analysis
    /// </summary>
    public class MovementAnalysisDto
    {
        public int VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalDistance { get; set; }
        public double TotalDrivingHours { get; set; }
        public double AverageDailyDistance { get; set; }
        public double MaxDailyDistance { get; set; }
        public double AverageTripDistance { get; set; }
        public int TotalTrips { get; set; }
        public Dictionary<DateTime, double> DailyDistances { get; set; } = new();
        public Dictionary<string, double> RouteHeatmap { get; set; } = new(); // Key: route segment, Value: frequency
        public List<LocationPointDto> FrequentLocations { get; set; } = new();
        public Dictionary<string, double> SpeedDistribution { get; set; } = new(); // Speed ranges and percentages
    }
}