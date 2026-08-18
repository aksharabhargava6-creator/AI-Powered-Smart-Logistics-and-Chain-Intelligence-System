using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLogisticsApp.Data;
using SmartLogisticsApp.Services;

namespace SmartLogisticsApp.Controllers;

public class HomeController : Controller
{
	private readonly SmartLogisticsContext _db;
	private readonly IAiEngineService _aiService;

	public HomeController(SmartLogisticsContext db, IAiEngineService aiService)
	{
		_db = db;
		_aiService = aiService;
	}

	public async Task<IActionResult> Index()
	{
		ViewBag.TotalWarehouses = await _db.Warehouses.CountAsync();
		ViewBag.TotalOrders = await _db.Orders.CountAsync();
		ViewBag.ActiveDeliveries = await _db.DeliveryAssignments.CountAsync(d => d.Status == "IN_TRANSIT" || d.Status == "ASSIGNED");

		var inventory = await _db.Inventory
			.Include(i => i.Product)
			.Include(i => i.Warehouse)
			.Take(10)
			.ToListAsync();

		var forecasts = await _db.DemandForecasts
			.Include(f => f.Product)
			.OrderByDescending(f => f.GeneratedAt)
			.Take(5)
			.ToListAsync();

		ViewBag.Forecasts = forecasts;
		return View(inventory);
	}

	[HttpPost]
	public async Task<IActionResult> RunAiForecast()
	{
		await _aiService.GenerateDemandForecastsAsync();
		return RedirectToAction("Index");
	}
}