namespace SmartLogistics.API.Models;

public class RouteRequest
{
    public DeliveryPoint StartingPoint { get; set; } = new DeliveryPoint();
    public List<DeliveryPoint> DeliveryPoints { get; set; } = new List<DeliveryPoint>();
    public OptimizationCriteria Criteria { get; set; } = OptimizationCriteria.Distance;
    public bool ReturnToStart { get; set; } = false;
}

public enum OptimizationCriteria
{
    Distance,
    Time,
    Priority
}