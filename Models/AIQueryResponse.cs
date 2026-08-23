namespace SmartLogistics.API.Models;

public class AIQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public string UserRole { get; set; } = "Manager";
    public bool IncludeRouteData { get; set; } = true;
    public bool IncludeForecastData { get; set; } = true;
}