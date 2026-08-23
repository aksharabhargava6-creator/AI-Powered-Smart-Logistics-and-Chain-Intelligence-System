namespace SmartLogistics.API.Models;

public class RouteResponse
{
    public List<RouteSegment> Segments { get; set; } = new List<RouteSegment>();
    public double TotalDistance { get; set; }
    public double TotalEstimatedTime { get; set; }
    public double TotalFuelCost { get; set; }
    public string? RouteSummary { get; set; }
}

public class RouteSegment
{
    public int Sequence { get; set; }
    public DeliveryPoint From { get; set; } = new DeliveryPoint();
    public DeliveryPoint To { get; set; } = new DeliveryPoint();
    public double DistanceKm { get; set; }
    public double EstimatedTimeMinutes { get; set; }
}