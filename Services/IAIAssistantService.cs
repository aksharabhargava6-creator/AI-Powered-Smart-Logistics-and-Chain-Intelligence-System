namespace SmartLogistics.API.Services;

public interface IAIAssistantService
{
    Task<AIQueryResponse> ProcessQueryAsync(AIQueryRequest request);
    string GetRouteRecommendation(RouteResponse route);
    string GetOperationalInsight(string context);
}