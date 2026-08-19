using Microsoft.EntityFrameworkCore;
using LogisticsPlatform.API.Data;
using LogisticsPlatform.API.Filters;
using LogisticsPlatform.API.Services;
using SmartLogisticsApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Database Contexts
builder.Services.AddDbContext<SmartLogisticsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services & Filters
builder.Services.AddScoped<IAiEngineService, AiEngineService>();
builder.Services.AddScoped<AuditLogFilter>();

// Member 7 - FR-10 Alerts & Notifications
builder.Services.AddScoped<IMember7AlertService, Member7AlertService>();

// Register Controllers & Apply AuditLogFilter Globally (FR-13)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

builder.Services.AddSignalR();

// Add Cookie Authentication for MVC Views (FR-01)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable Authentication & Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

// Route Endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<LogisticsPlatform.API.Hub.TrackingHub>("/trackingHub");

app.Run();