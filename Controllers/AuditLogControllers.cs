using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsPlatform.API.Data;

namespace LogisticsPlatform.API.Controllers;

[Authorize]
public class AuditLogsController : Controller
{
    private readonly SmartLogisticsContext _context;

    public AuditLogsController(SmartLogisticsContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchUser, string? searchAction)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchUser))
        {
            query = query.Where(a => a.UserId.Contains(searchUser));
        }

        if (!string.IsNullOrWhiteSpace(searchAction))
        {
            query = query.Where(a => a.Action.Contains(searchAction));
        }

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
            .ToListAsync();

        ViewData["SearchUser"] = searchUser;
        ViewData["SearchAction"] = searchAction;

        return View(logs);
    }
}