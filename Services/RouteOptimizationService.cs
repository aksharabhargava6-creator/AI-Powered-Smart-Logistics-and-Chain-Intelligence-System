using SmartLogistics.API.Models;

namespace SmartLogistics.API.Services;

public class RouteOptimizationService : IRouteOptimizationService
{
    private const double EARTH_RADIUS_KM = 6371;
    private const double AVERAGE_DRIVING_SPEED_KMPH = 40;
    private const double AVERAGE_STOP_TIME_MINUTES = 5;
    private const double FUEL_COST_PER_KM = 1.2; // USD

    public RouteResponse OptimizeRoute(RouteRequest request)
    {
        if (request.DeliveryPoints == null || !request.DeliveryPoints.Any())
        {
            return new RouteResponse
            {
                Segments = new List<RouteSegment>(),
                TotalDistance = 0,
                TotalEstimatedTime = 0,
                TotalFuelCost = 0,
                RouteSummary = "No delivery points provided"
            };
        }

        // Get all points including start
        var allPoints = new List<DeliveryPoint> { request.StartingPoint };
        allPoints.AddRange(request.DeliveryPoints);

        // Apply optimization algorithm
        var optimizedOrder = OptimizeRouteOrder(allPoints, request.Criteria, request.ReturnToStart);

        // Build segments
        var segments = new List<RouteSegment>();
        double totalDistance = 0;
        double totalTime = 0;

        for (int i = 0; i < optimizedOrder.Count - 1; i++)
        {
            var from = optimizedOrder[i];
            var to = optimizedOrder[i + 1];
            
            var distance = CalculateDistance(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
            var time = CalculateETA(distance);
            
            segments.Add(new RouteSegment
            {
                Sequence = i + 1,
                From = from,
                To = to,
                DistanceKm = distance,
                EstimatedTimeMinutes = time
            });

            totalDistance += distance;
            totalTime += time;
        }

        return new RouteResponse
        {
            Segments = segments,
            TotalDistance = totalDistance,
            TotalEstimatedTime = totalTime,
            TotalFuelCost = totalDistance * FUEL_COST_PER_KM,
            RouteSummary = GenerateRouteSummary(segments, totalDistance, totalTime)
        };
    }

    private List<DeliveryPoint> OptimizeRouteOrder(
        List<DeliveryPoint> points, 
        OptimizationCriteria criteria,
        bool returnToStart)
    {
        if (points.Count <= 2)
            return points;

        // Greedy Nearest Neighbor with criteria support
        var unvisited = new List<DeliveryPoint>(points);
        var route = new List<DeliveryPoint>();
        
        // Start with origin
        var current = unvisited.First();
        route.Add(current);
        unvisited.Remove(current);

        while (unvisited.Any())
        {
            DeliveryPoint next;
            
            switch (criteria)
            {
                case OptimizationCriteria.Priority:
                    next = unvisited.OrderBy(p => p.Priority)
                                    .ThenBy(p => CalculateDistance(
                                        current.Latitude, current.Longitude,
                                        p.Latitude, p.Longitude))
                                    .First();
                    break;
                case OptimizationCriteria.Distance:
                default:
                    next = unvisited.OrderBy(p => CalculateDistance(
                        current.Latitude, current.Longitude,
                        p.Latitude, p.Longitude))
                        .First();
                    break;
            }
            
            route.Add(next);
            unvisited.Remove(next);
            current = next;
        }

        // Return to start if requested
        if (returnToStart && points.Any())
        {
            route.Add(points.First());
        }

        return route;
    }

    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var lat1Rad = ToRadians(lat1);
        var lat2Rad = ToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EARTH_RADIUS_KM * c;
    }

    public double CalculateETA(double distanceKm)
    {
        var timeHours = distanceKm / AVERAGE_DRIVING_SPEED_KMPH;
        var timeMinutes = timeHours * 60;
        // Add stop time for each stop
        return timeMinutes + AVERAGE_STOP_TIME_MINUTES;
    }

    public RouteResponse OptimizeRoutesWithConstraints(RouteRequest request, int maxStopsPerRoute = 20)
    {
        // Split deliveries into multiple routes if needed
        var routeResponse = OptimizeRoute(request);
        
        if (request.DeliveryPoints.Count <= maxStopsPerRoute)
            return routeResponse;

        // Split into multiple routes (simplified implementation)
        var allPoints = request.DeliveryPoints.ToList();
        var routes = new List<RouteResponse>();
        
        for (int i = 0; i < allPoints.Count; i += maxStopsPerRoute)
        {
            var chunk = allPoints.Skip(i).Take(maxStopsPerRoute).ToList();
            var subRequest = new RouteRequest
            {
                StartingPoint = request.StartingPoint,
                DeliveryPoints = chunk,
                Criteria = request.Criteria,
                ReturnToStart = request.ReturnToStart
            };
            routes.Add(OptimizeRoute(subRequest));
        }

        // Combine responses
        var combined = new RouteResponse
        {
            Segments = routes.SelectMany(r => r.Segments).ToList(),
            TotalDistance = routes.Sum(r => r.TotalDistance),
            TotalEstimatedTime = routes.Sum(r => r.TotalEstimatedTime),
            TotalFuelCost = routes.Sum(r => r.TotalFuelCost),
            RouteSummary = $"Route split into {routes.Count} optimal routes"
        };

        return combined;
    }

    private string GenerateRouteSummary(List<RouteSegment> segments, double totalDistance, double totalTime)
    {
        var totalStops = segments.Count;
        return $"📦 Route with {totalStops} stops | Total Distance: {totalDistance:F1} km | " +
               $"Total Time: {totalTime:F0} min | Fuel Cost: ${totalDistance * FUEL_COST_PER_KM:F2}";
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}