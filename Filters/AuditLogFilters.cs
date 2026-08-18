using Microsoft.AspNetCore.Mvc.Filters;
using LogisticsPlatform.API.Data;
using LogisticsPlatform.API.Models;

namespace LogisticsPlatform.API.Filters;

public class AuditLogFilter : IAsyncActionFilter
{
    private readonly SmartLogisticsContext _context;

    public AuditLogFilter(SmartLogisticsContext context)
    {
        _context = context;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User.Identity?.Name ?? "Anonymous User";
        var path = httpContext.Request.Path.Value ?? "/";
        var method = httpContext.Request.Method;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

        // Execute the targeted action
        var resultContext = await next();

        // Skip logging GET requests to AuditLogs view itself to avoid noise
        if (controller == "AuditLogs" && method == "GET") return;

        try
        {
            var auditEntry = new AuditLog
            {
                UserId = user,
                Action = $"{method} {controller}/{action}",
                EntityName = controller,
                IpAddress = ip,
                Timestamp = DateTime.UtcNow,
                Details = $"Path: {path} | Status: {httpContext.Response.StatusCode}"
            };

            _context.AuditLogs.Add(auditEntry);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Fallthrough silently so audit failure doesn't break primary user requests
        }
    }
}