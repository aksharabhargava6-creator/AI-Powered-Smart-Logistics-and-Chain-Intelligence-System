namespace SmartLogistics.API.Services;

public interface IRouteOptimizationService
{
    RouteResponse OptimizeRoute(RouteRequest request);
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
    double CalculateETA(double distanceKm);
    RouteResponse OptimizeRoutesWithConstraints(RouteRequest request, int maxStopsPerRoute = 20);
}