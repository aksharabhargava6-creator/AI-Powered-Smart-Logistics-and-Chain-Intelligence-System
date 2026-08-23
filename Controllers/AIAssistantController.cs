using Microsoft.AspNetCore.Mvc;
using SmartLogistics.API.Models;
using SmartLogistics.API.Services;

namespace SmartLogistics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIAssistantController : ControllerBase
{
    private readonly IAIAssistantService _aiService;
    private readonly ILogger<AIAssistantController> _logger;

    public AIAssistantController(
        IAIAssistantService aiService,
        ILogger<AIAssistantController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Ask the AI Assistant operational questions
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> ProcessQuery([FromBody] AIQueryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query cannot be empty.");
            }

            _logger.LogInformation("Processing AI query: {Query}", request.Query);

            var response = await _aiService.ProcessQueryAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI query");
            return StatusCode(500, "An error occurred processing your query.");
        }
    }

    /// <summary>
    /// Get route recommendation
    /// </summary>
    [HttpGet("route-recommendation")]
    public IActionResult GetRouteRecommendation()
    {
        try
        {
            // Create a sample route for demonstration
            var routeService = HttpContext.RequestServices.GetService<IRouteOptimizationService>();
            if (routeService == null)
            {
                return StatusCode(500, "Route service not available");
            }

            var request = new RouteRequest
            {
                StartingPoint = new DeliveryPoint
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Address = "Origin Hub"
                },
                DeliveryPoints = new List<DeliveryPoint>
                {
                    new DeliveryPoint { Latitude = 40.7484, Longitude = -73.9857, Address = "Stop 1" },
                    new DeliveryPoint { Latitude = 40.7580, Longitude = -73.9855, Address = "Stop 2" }
                }
            };

            var result = routeService.OptimizeRoute(request);
            var recommendation = _aiService.GetRouteRecommendation(result);
            
            return Ok(new { recommendation, routeDetails = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route recommendation");
            return StatusCode(500, "An error occurred getting recommendation.");
        }
    }
}