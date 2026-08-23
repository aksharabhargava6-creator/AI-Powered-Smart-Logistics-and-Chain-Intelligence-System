using Microsoft.AspNetCore.Mvc;
using SmartLogistics.API.Models;
using SmartLogistics.API.Services;

namespace SmartLogistics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteOptimizationController : ControllerBase
{
    private readonly IRouteOptimizationService _routeService;
    private readonly ILogger<RouteOptimizationController> _logger;

    public RouteOptimizationController(
        IRouteOptimizationService routeService,
        ILogger<RouteOptimizationController> logger)
    {
        _routeService = routeService;
        _logger = logger;
    }

    /// <summary>
    /// Optimize delivery routes based on provided delivery points
    /// </summary>
    [HttpPost("optimize")]
    public IActionResult OptimizeRoute([FromBody] RouteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.DeliveryPoints == null || !request.DeliveryPoints.Any())
            {
                return BadRequest("No delivery points provided.");
            }

            _logger.LogInformation("Optimizing route with {Count} delivery points", 
                request.DeliveryPoints.Count);

            var result = _routeService.OptimizeRoute(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing route");
            return StatusCode(500, "An error occurred while optimizing the route.");
        }
    }

    /// <summary>
    /// Calculate distance between two coordinates using Haversine formula
    /// </summary>
    [HttpPost("distance")]
    public IActionResult CalculateDistance(
        [FromBody] DistanceRequest request)
    {
        try
        {
            var distance = _routeService.CalculateDistance(
                request.Lat1, request.Lon1,
                request.Lat2, request.Lon2);
            
            return Ok(new { distanceKm = distance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance");
            return StatusCode(500, "An error occurred calculating distance.");
        }
    }

    /// <summary>
    /// Optimize routes with constraints (max stops per route)
    /// </summary>
    [HttpPost("optimize-with-constraints")]
    public IActionResult OptimizeWithConstraints(
        [FromBody] RouteRequest request,
        [FromQuery] int maxStopsPerRoute = 20)
    {
        try
        {
            var result = _routeService.OptimizeRoutesWithConstraints(request, maxStopsPerRoute);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing routes with constraints");
            return StatusCode(500, "An error occurred optimizing routes.");
        }
    }
}

public class DistanceRequest
{
    public double Lat1 { get; set; }
    public double Lon1 { get; set; }
    public double Lat2 { get; set; }
    public double Lon2 { get; set; }
}