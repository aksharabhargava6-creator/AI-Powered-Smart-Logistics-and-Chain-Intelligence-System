using Microsoft.AspNetCore.Mvc;
using FleetTracking.DTOs;
using FleetTracking.Services;

namespace FleetTracking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles([FromQuery] bool includeInactive = false)
        {
            try
            {
                var vehicles = await _vehicleService.GetAllVehiclesAsync(includeInactive);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicles");
                return StatusCode(500, "An error occurred while retrieving vehicles");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDto>> GetVehicle(int id)
        {
            try
            {
                var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
                if (vehicle == null)
                    return NotFound($"Vehicle with ID {id} not found");

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vehicle {id}");
                return StatusCode(500, "An error occurred while retrieving the vehicle");
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehiclesByStatus(string status)
        {
            try
            {
                var vehicles = await _vehicleService.GetVehiclesByStatusAsync(status);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vehicles by status {status}");
                return StatusCode(500, "An error occurred while retrieving vehicles");
            }
        }

        [HttpGet("{id}/location")]
        public async Task<ActionResult<LocationUpdateDto>> GetVehicleLocation(int id)
        {
            try
            {
                var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
                if (vehicle == null)
                    return NotFound($"Vehicle with ID {id} not found");

                var location = new LocationUpdateDto
                {
                    VehicleId = id,
                    Latitude = vehicle.CurrentLatitude,
                    Longitude = vehicle.CurrentLongitude,
                    Speed = vehicle.CurrentSpeed ?? 0,
                    Timestamp = vehicle.LastUpdated
                };

                return Ok(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting location for vehicle {id}");
                return StatusCode(500, "An error occurred while retrieving vehicle location");
            }
        }

        [HttpGet("{id}/location-history")]
        public async Task<ActionResult<IEnumerable<LocationPointDto>>> GetVehicleLocationHistory(
            int id, 
            [FromQuery] DateTime? from, 
            [FromQuery] DateTime? to,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _vehicleService.GetVehicleLocationHistoryAsync(id, from, to);
                return Ok(history.Take(limit));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting location history for vehicle {id}");
                return StatusCode(500, "An error occurred while retrieving location history");
            }
        }

        [HttpPost]
        public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] CreateVehicleDto vehicleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var vehicle = await _vehicleService.CreateVehicleAsync(vehicleDto);
                return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vehicle");
                return StatusCode(500, "An error occurred while creating the vehicle");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<VehicleDto>> UpdateVehicle(int id, [FromBody] UpdateVehicleDto vehicleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var vehicle = await _vehicleService.UpdateVehicleAsync(id, vehicleDto);
                if (vehicle == null)
                    return NotFound($"Vehicle with ID {id} not found");

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle {id}");
                return StatusCode(500, "An error occurred while updating the vehicle");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<VehicleDto>> UpdateVehicleStatus(int id, [FromBody] string status)
        {
            try
            {
                var vehicle = await _vehicleService.UpdateVehicleStatusAsync(id, status);
                if (vehicle == null)
                    return NotFound($"Vehicle with ID {id} not found");

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vehicle status {id}");
                return StatusCode(500, "An error occurred while updating vehicle status");
            }
        }

        [HttpPut("{vehicleId}/assign-driver/{driverId}")]
        public async Task<ActionResult<VehicleDto>> AssignDriver(int vehicleId, int driverId)
        {
            try
            {
                var vehicle = await _vehicleService.AssignDriverAsync(vehicleId, driverId);
                if (vehicle == null)
                    return NotFound($"Vehicle with ID {vehicleId} or Driver with ID {driverId} not found");

                return Ok(vehicle);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning driver {driverId} to vehicle {vehicleId}");
                return StatusCode(500, "An error occurred while assigning driver to vehicle");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVehicle(int id)
        {
            try
            {
                var result = await _vehicleService.DeleteVehicleAsync(id);
                if (!result)
                    return NotFound($"Vehicle with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vehicle {id}");
                return StatusCode(500, "An error occurred while deleting the vehicle");
            }
        }
    }
}