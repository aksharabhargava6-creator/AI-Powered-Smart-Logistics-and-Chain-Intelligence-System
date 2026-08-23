using SmartLogistics.API.Models;
using System.Text;
using System.Text.Json;

namespace SmartLogistics.API.Services;

public class AIAssistantService : IAIAssistantService
{
    private readonly IRouteOptimizationService _routeService;
    private readonly Dictionary<string, string> _operationalInsights;

    public AIAssistantService(IRouteOptimizationService routeService)
    {
        _routeService = routeService;
        _operationalInsights = new Dictionary<string, string>
        {
            ["efficiency"] = "Optimize delivery routes by grouping nearby destinations and avoiding peak traffic hours.",
            ["inventory"] = "Maintain safety stock levels at 20% above average demand for critical products.",
            ["cost"] = "Consider consolidating shipments to reduce fuel costs by up to 30%.",
            ["time"] = "Prioritize high-priority deliveries during morning hours for better success rates."
        };
    }

    public async Task<AIQueryResponse> ProcessQueryAsync(AIQueryRequest request)
    {
        // Simulate AI processing with context-aware responses
        var response = new AIQueryResponse
        {
            Confidence = 0.85,
            Sources = new List<string> { "Route Data", "Operational History" }
        };

        // Normalize query for better matching
        var normalizedQuery = request.Query.ToLower();
        var context = DetermineContext(normalizedQuery);

        // Generate response based on query type
        if (normalizedQuery.Contains("route") || normalizedQuery.Contains("delivery"))
        {
            response.Answer = await GetRouteResponseAsync(request);
            response.ContextUsed = "Route Optimization";
        }
        else if (normalizedQuery.Contains("optimize") || normalizedQuery.Contains("efficiency"))
        {
            response.Answer = _operationalInsights["efficiency"];
            response.ContextUsed = "Efficiency Optimization";
        }
        else if (normalizedQuery.Contains("inventory") || normalizedQuery.Contains("stock"))
        {
            response.Answer = _operationalInsights["inventory"];
            response.ContextUsed = "Inventory Management";
        }
        else if (normalizedQuery.Contains("cost") || normalizedQuery.Contains("fuel"))
        {
            response.Answer = _operationalInsights["cost"];
            response.ContextUsed = "Cost Optimization";
        }
        else if (normalizedQuery.Contains("priority") || normalizedQuery.Contains("high"))
        {
            response.Answer = _operationalInsights["time"];
            response.ContextUsed = "Priority Management";
        }
        else
        {
            response.Answer = GenerateGeneralResponse(normalizedQuery);
            response.ContextUsed = "General Operations";
            response.Confidence = 0.65;
        }

        // Add additional context data
        response.AdditionalData["timestamp"] = DateTime.UtcNow;
        response.AdditionalData["query_type"] = context;
        response.AdditionalData["role_context"] = request.UserRole;

        return await Task.FromResult(response);
    }

    private async Task<string> GetRouteResponseAsync(AIQueryRequest request)
    {
        // Create a sample route request for demonstration
        var routeRequest = new RouteRequest
        {
            StartingPoint = new DeliveryPoint
            {
                Latitude = 40.7128,
                Longitude = -74.0060,
                Address = "Origin Hub"
            },
            DeliveryPoints = new List<DeliveryPoint>
            {
                new DeliveryPoint { Latitude = 40.7484, Longitude = -73.9857, Address = "Stop 1", Priority = 1 },
                new DeliveryPoint { Latitude = 40.7580, Longitude = -73.9855, Address = "Stop 2", Priority = 2 },
                new DeliveryPoint { Latitude = 40.7128, Longitude = -74.0060, Address = "Stop 3", Priority = 3 }
            },
            Criteria = OptimizationCriteria.Distance
        };

        var result = _routeService.OptimizeRoute(routeRequest);
        var recommendation = GetRouteRecommendation(result);
        
        return $"🚚 Route Recommendation:\n{recommendation}\n" +
               $"📊 Summary: {result.RouteSummary}";
    }

    public string GetRouteRecommendation(RouteResponse route)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Optimal Delivery Sequence:");
        
        foreach (var segment in route.Segments.Take(10))
        {
            sb.AppendLine($"  Stop {segment.Sequence}: {segment.From.Address} → {segment.To.Address}");
            sb.AppendLine($"    Distance: {segment.DistanceKm:F1} km, ETA: {segment.EstimatedTimeMinutes:F0} min");
        }

        if (route.Segments.Count > 10)
        {
            sb.AppendLine($"  ... and {route.Segments.Count - 10} more stops");
        }

        return sb.ToString();
    }

    public string GetOperationalInsight(string context)
    {
        return _operationalInsights.GetValueOrDefault(
            context.ToLower(),
            "No specific insight available for this context.");
    }

    private string DetermineContext(string query)
    {
        if (query.Contains("route") || query.Contains("delivery") || query.Contains("distance"))
            return "route";
        if (query.Contains("cost") || query.Contains("fuel") || query.Contains("expense"))
            return "cost";
        if (query.Contains("time") || query.Contains("schedule") || query.Contains("delay"))
            return "time";
        if (query.Contains("inventory") || query.Contains("stock") || query.Contains("warehouse"))
            return "inventory";
        if (query.Contains("priority") || query.Contains("urgent") || query.Contains("high"))
            return "priority";
        return "general";
    }

    private string GenerateGeneralResponse(string query)
    {
        var responses = new List<string>
        {
            "I can help with route optimization, delivery scheduling, and operational insights.",
            "Based on our data, focusing on route efficiency can reduce costs by 15-20%.",
            "Consider prioritizing high-volume delivery zones for better resource allocation.",
            "Our analytics suggest that early morning deliveries have 30% higher success rates.",
            "Monitor fuel costs closely - they account for approximately 25% of operational expenses."
        };

        var random = new Random();
        return responses[random.Next(responses.Count)];
    }
}