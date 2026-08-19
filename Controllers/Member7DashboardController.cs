using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLogisticsApp.Data;
using SmartLogisticsApp.Models;
using SmartLogisticsApp.Services;

namespace SmartLogisticsApp.Controllers;

public class Member7DashboardController : Controller
{
    private readonly SmartLogisticsContext _db;
    private readonly IMember7AlertService _alertService;

    public Member7DashboardController(
        SmartLogisticsContext db,
        IMember7AlertService alertService)
    {
        _db = db;
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products
            .ToListAsync();

        var warehouses = await _db.Warehouses
            .ToListAsync();

        var inventory = await _db.Inventory
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .ToListAsync();

        var orders = await _db.Orders
            .ToListAsync();

        var deliveries = await _db.DeliveryAssignments
            .ToListAsync();

        var forecasts = await _db.DemandForecasts
            .Include(f => f.Product)
            .OrderByDescending(f => f.GeneratedAt)
            .Take(10)
            .ToListAsync();

        // -------------------------------------------------
        // Inventory calculations
        // -------------------------------------------------

        int totalInventoryUnits = inventory.Sum(i =>
            Math.Max(0, i.QuantityOnHand - i.QuantityReserved));

        int lowStockProducts = products.Count(product =>
        {
            var availableStock = inventory
                .Where(i => i.ProductId == product.ProductId)
                .Sum(i => i.QuantityOnHand - i.QuantityReserved);

            return availableStock <= product.ReorderLevel;
        });

        // -------------------------------------------------
        // Order calculations
        // -------------------------------------------------

        int deliveredOrders = orders.Count(o =>
            o.OrderStatus != null &&
            (
                o.OrderStatus.Equals("DELIVERED",
                    StringComparison.OrdinalIgnoreCase)
                ||
                o.OrderStatus.Equals("COMPLETED",
                    StringComparison.OrdinalIgnoreCase)
            ));

        int activeDeliveries = deliveries.Count(d =>
            d.Status != null &&
            (
                d.Status.Equals("ASSIGNED",
                    StringComparison.OrdinalIgnoreCase)
                ||
                d.Status.Equals("IN_TRANSIT",
                    StringComparison.OrdinalIgnoreCase)
            ));

        int delayedDeliveries = deliveries.Count(d =>
            d.EstimatedArrival.HasValue &&
            d.EstimatedArrival.Value < DateTime.Now &&
            d.Status != null &&
            !d.Status.Equals("DELIVERED",
                StringComparison.OrdinalIgnoreCase) &&
            !d.Status.Equals("COMPLETED",
                StringComparison.OrdinalIgnoreCase) &&
            !d.Status.Equals("CANCELLED",
                StringComparison.OrdinalIgnoreCase));

        double deliveryPerformance =
            deliveries.Count == 0
                ? 100
                : (double)deliveredOrders /
                  Math.Max(orders.Count, 1) * 100;

        // -------------------------------------------------
        // Dashboard summary
        // -------------------------------------------------

        var summary = new DashboardSummary
        {
            TotalProducts = products.Count,
            TotalWarehouses = warehouses.Count,
            TotalInventoryUnits = totalInventoryUnits,
            LowStockProducts = lowStockProducts,
            TotalOrders = orders.Count,
            DeliveredOrders = deliveredOrders,
            DelayedDeliveries = delayedDeliveries,
            ActiveDeliveries = activeDeliveries,
            DeliveryPerformancePercentage =
                Math.Round(deliveryPerformance, 2)
        };

        // -------------------------------------------------
        // Warehouse analytics
        // -------------------------------------------------

        var warehouseAnalytics = warehouses
            .Select(w =>
            {
                int storedUnits = inventory
                    .Where(i => i.WarehouseId == w.WarehouseId)
                    .Sum(i => i.QuantityOnHand);

                double utilization =
                    w.CapacityUnits <= 0
                        ? 0
                        : (double)storedUnits /
                          w.CapacityUnits * 100;

                return new WarehouseAnalytics
                {
                    WarehouseId = w.WarehouseId,
                    WarehouseName = w.WarehouseName,
                    CapacityUnits = w.CapacityUnits,
                    StoredUnits = storedUnits,
                    UtilizationPercentage =
                        Math.Round(utilization, 2)
                };
            })
            .ToList();

        // -------------------------------------------------
        // Order status analytics
        // -------------------------------------------------

        var orderStatuses = orders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.OrderStatus)
                ? "UNKNOWN"
                : o.OrderStatus.ToUpper())
            .Select(g => new OrderStatusAnalytics
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // -------------------------------------------------
        // Forecast analytics
        // -------------------------------------------------

        var forecastAnalytics = forecasts
            .Select(f => new ForecastAnalytics
            {
                ProductId = f.ProductId,
                ProductName = f.Product?.ProductName ?? "Unknown Product",
                ForecastDate = f.ForecastDate,
                PredictedDemand = f.PredictedDemand,
                ConfidenceScore = f.ConfidenceScore
            })
            .ToList();

        // -------------------------------------------------
        // FR-11 Alerts
        // -------------------------------------------------

        var alerts = await _alertService.GetOperationalAlertsAsync();

        var dashboard = new AnalyticsDashboard
        {
            Summary = summary,
            Warehouses = warehouseAnalytics,
            OrderStatuses = orderStatuses,
            Forecasts = forecastAnalytics,
            Alerts = alerts
        };

        return View(dashboard);
    }
}