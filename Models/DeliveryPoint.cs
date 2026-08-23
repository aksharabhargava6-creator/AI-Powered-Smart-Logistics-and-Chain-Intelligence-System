namespace SmartLogistics.API.Models;

public class DeliveryPoint
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? EstimatedDeliveryTimeMinutes { get; set; }
    public int Priority { get; set; } = 1; // 1 = highest priority
    public string? OrderReference { get; set; }
}