using Microsoft.AspNetCore.Mvc;
using FleetTracking.DTOs;
using FleetTracking.Services;

namespace FleetTracking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly ILogger<DriversController> _logger;

        public DriversController(IDriverService driverService, ILogger<DriversController> logger)
        {
            _driverService = driverService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DriverDto>>> GetDrivers([FromQuery] bool includeInactive = false)
        {
            try
            {
                var drivers = await _driverService.GetAllDriversAsync(includeInactive);
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drivers");
                return StatusCode(500, "An error occurred while retrieving drivers");
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<DriverDto>>> GetAvailableDrivers()
        {
            try
            {
                var drivers = await _driverService.GetAvailableDriversAsync();
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available drivers");
                return StatusCode(500, "An error occurred while retrieving available drivers");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DriverDto>> GetDriver(int id)
        {
            try
            {
                var driver = await _driverService.GetDriverByIdAsync(id);
                if (driver == null)
                    return NotFound($"Driver with ID {id} not found");

                return Ok(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting driver {id}");
                return StatusCode(500, "An error occurred while retrieving the driver");
            }
        }

        [HttpPost]
        public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] CreateDriverDto driverDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var driver = await _driverService.CreateDriverAsync(driverDto);
                return CreatedAtAction(nameof(GetDriver), new { id = driver.Id }, driver);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating driver");
                return StatusCode(500, "An error occurred while creating the driver");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DriverDto>> UpdateDriver(int id, [FromBody] UpdateDriverDto driverDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var driver = await _driverService.UpdateDriverAsync(id, driverDto);
                if (driver == null)
                    return NotFound($"Driver with ID {id} not found");

                return Ok(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver {id}");
                return StatusCode(500, "An error occurred while updating the driver");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<DriverDto>> UpdateDriverStatus(int id, [FromBody] string status)
        {
            try
            {
                var driver = await _driverService.UpdateDriverStatusAsync(id, status);
                if (driver == null)
                    return NotFound($"Driver with ID {id} not found");

                return Ok(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating driver status {id}");
                return StatusCode(500, "An error occurred while updating driver status");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDriver(int id)
        {
            try
            {
                var result = await _driverService.DeleteDriverAsync(id);
                if (!result)
                    return NotFound($"Driver with ID {id} not found");

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting driver {id}");
                return StatusCode(500, "An error occurred while deleting the driver");
            }
        }
    }
}