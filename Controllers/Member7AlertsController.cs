using Microsoft.AspNetCore.Mvc;
using SmartLogisticsApp.Services;

namespace LogisticsPlatform.API.Controllers;

[ApiController]
[Route("api/member7/alerts")]
public class Member7AlertsController : ControllerBase
{
    private readonly IMember7AlertService _alertService;

    public Member7AlertsController(IMember7AlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<ActionResult> GetAlerts()
    {
        var alerts = await _alertService.GetOperationalAlertsAsync();

        return Ok(alerts);
    }
}