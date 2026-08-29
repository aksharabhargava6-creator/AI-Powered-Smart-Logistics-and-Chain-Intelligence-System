```csharp
using Microsoft.EntityFrameworkCore;

using LogisticsPlatform.API.Data;
using LogisticsPlatform.API.Filters;
using LogisticsPlatform.API.Services;
using LogisticsPlatform.API.Hub;

using SmartLogisticsApp.Services;

using FleetTracking.Data;
using FleetTracking.Services;
using FleetTracking.Simulation;

using SmartLogistics.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE CONTEXTS
// ============================================================

// Main Smart Logistics database
builder.Services.AddDbContext<SmartLogisticsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Application / Identity database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Fleet Tracking database
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


// ============================================================
// APPLICATION SERVICES
// ============================================================

// AI Engine
builder.Services.AddScoped<IAiEngineService, AiEngineService>();

// Audit logging
builder.Services.AddScoped<AuditLogFilter>();

// Alerts & Notifications - Member 7
builder.Services.AddScoped<IMember7AlertService, Member7AlertService>();


// ============================================================
// FLEET TRACKING SERVICES
// ============================================================

builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IDriverService, DriverService>();

// GPS simulator
builder.Services.AddHostedService<GpsSimulatorService>();


// ============================================================
// SMART ROUTE / AI SERVICES
// ============================================================

builder.Services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
builder.Services.AddScoped<IAIAssistantService, AIAssistantService>();


// ============================================================
// MVC / API CONTROLLERS
// ============================================================

builder.Services.AddControllersWithViews(options =>
{
    // Apply AuditLogFilter globally
    options.Filters.Add<AuditLogFilter>();
});


// ============================================================
// SIGNALR
// ============================================================

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});


// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// ============================================================
// COOKIE AUTHENTICATION
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});


// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// HTTP PIPELINE
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();


// CORS
app.UseCors("ReactPolicy");


// ============================================================
// MVC ROUTING
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ============================================================
// SIGNALR HUBS
// ============================================================

// Main Logistics Tracking Hub
app.MapHub<TrackingHub>("/trackingHub");


// ============================================================
// DATABASE MIGRATION
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Fleet database migrations
        var fleetDb = services.GetRequiredService<LogisticsDbContext>();
        await fleetDb.Database.MigrateAsync();

        // Main Smart Logistics database migrations
        var smartDb = services.GetRequiredService<SmartLogisticsContext>();
        await smartDb.Database.MigrateAsync();

        // Application database migrations
        var applicationDb = services.GetRequiredService<ApplicationDbContext>();
        await applicationDb.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogError(
            ex,
            "An error occurred while migrating the databases.");
    }
}


// ============================================================
// START APPLICATION
// ============================================================

app.Run();
```
