using Microsoft.EntityFrameworkCore;
using SmartLogisticsApp.Data;
using SmartLogisticsApp.Models;

namespace SmartLogisticsApp.Services;

public interface IMember7AlertService
{
    Task<List<OperationalAlert>> GetOperationalAlertsAsync();
}

public class Member7AlertService : IMember7AlertService
{
    private readonly SmartLogisticsContext _db;

    public Member7AlertService(SmartLogisticsContext db)
    {
        _db = db;
    }

    public async Task<List<OperationalAlert>> GetOperationalAlertsAsync()
    {
        var alerts = new List<OperationalAlert>();

        // ---------------------------------------------------------
        // FR-10.1 - LOW STOCK ALERTS
        // Available stock = QuantityOnHand - QuantityReserved
        // ---------------------------------------------------------

        var inventory = await _db.Inventory
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .ToListAsync();

        foreach (var item in inventory)
        {
            if (item.Product == null)
                continue;

            int availableStock =
                item.QuantityOnHand - item.QuantityReserved;

            if (availableStock <= item.Product.ReorderLevel)
            {
                string severity =
                    availableStock <= 0 ? "CRITICAL" : "WARNING";

                string message;

                if (availableStock <= 0)
                {
                    message =
                        $"{item.Product.ProductName} is out of stock " +
                        $"at {item.Warehouse?.WarehouseName ?? "the warehouse"}.";
                }
                else
                {
                    message =
                        $"{item.Product.ProductName} has only " +
                        $"{availableStock} available units at " +
                        $"{item.Warehouse?.WarehouseName ?? "the warehouse"}. " +
                        $"Reorder level is {item.Product.ReorderLevel}.";
                }

                alerts.Add(new OperationalAlert
                {
                    Code = "LOW_STOCK",
                    Severity = severity,
                    Title = availableStock <= 0
                        ? "Critical: Out of Stock"
                        : "Low Stock Alert",
                    Message = message
                });
            }
        }

        // ---------------------------------------------------------
        // FR-10.2 - DELAYED DELIVERY ALERTS
        // ---------------------------------------------------------

        var now = DateTime.Now;

        var deliveries = await _db.DeliveryAssignments
            .ToListAsync();

        foreach (var delivery in deliveries)
        {
            if (!delivery.EstimatedArrival.HasValue)
                continue;

            string status =
                delivery.Status?.Trim().ToUpperInvariant() ?? string.Empty;

            bool completed =
                status == "DELIVERED" ||
                status == "COMPLETED" ||
                status == "CANCELLED";

            if (!completed && delivery.EstimatedArrival.Value < now)
            {
                alerts.Add(new OperationalAlert
                {
                    Code = "DELAYED_DELIVERY",
                    Severity = "WARNING",
                    Title = "Delayed Delivery",
                    Message =
                        $"Delivery #{delivery.DeliveryId} for order " +
                        $"#{delivery.OrderId} has passed its estimated " +
                        $"arrival time."
                });
            }
        }

        // ---------------------------------------------------------
// FR-10.3 - WAREHOUSE CAPACITY ALERTS
// ---------------------------------------------------------

var warehouses = await _db.Warehouses.ToListAsync();

var inventoryByWarehouse = await _db.Inventory
    .GroupBy(i => i.WarehouseId)
    .Select(g => new
    {
        WarehouseId = g.Key,
        StoredUnits = g.Sum(i => i.QuantityOnHand)
    })
    .ToListAsync();

foreach (var warehouse in warehouses)
{
    if (warehouse.CapacityUnits <= 0)
        continue;

    int storedUnits = inventoryByWarehouse
        .FirstOrDefault(x => x.WarehouseId == warehouse.WarehouseId)
        ?.StoredUnits ?? 0;

    double utilization =
        (double)storedUnits / warehouse.CapacityUnits * 100;

    if (utilization >= 90)
    {
        string severity =
            utilization >= 100 ? "CRITICAL" : "WARNING";

        alerts.Add(new OperationalAlert
        {
            Code = "WAREHOUSE_CAPACITY",
            Severity = severity,
            Title = utilization >= 100
                ? "Warehouse Capacity Exceeded"
                : "High Warehouse Utilization",
            Message =
                $"{warehouse.WarehouseName} is " +
                $"{utilization:F1}% utilized " +
                $"({storedUnits}/{warehouse.CapacityUnits} units)."
        });
    }
}
        return alerts
            .OrderByDescending(a => a.Severity == "CRITICAL")
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToList();
    }
}