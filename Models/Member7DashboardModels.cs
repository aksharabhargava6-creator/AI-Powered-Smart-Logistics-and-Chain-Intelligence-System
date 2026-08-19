namespace SmartLogisticsApp.Models;

public class OperationalAlert
{
    public string Code { get; set; } = string.Empty;

    public string Severity { get; set; } = "WARNING";

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DashboardSummary
{
    public int TotalProducts { get; set; }

    public int TotalWarehouses { get; set; }

    public int TotalInventoryUnits { get; set; }

    public int LowStockProducts { get; set; }

    public int TotalOrders { get; set; }

    public int DeliveredOrders { get; set; }

    public int DelayedDeliveries { get; set; }

    public int ActiveDeliveries { get; set; }

    public double DeliveryPerformancePercentage { get; set; }
}

public class WarehouseAnalytics
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = string.Empty;

    public int CapacityUnits { get; set; }

    public int StoredUnits { get; set; }

    public double UtilizationPercentage { get; set; }
}

public class OrderStatusAnalytics
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class ForecastAnalytics
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public DateTime ForecastDate { get; set; }

    public decimal PredictedDemand { get; set; }

    public decimal ConfidenceScore { get; set; }
}

public class AnalyticsDashboard
{
    public DashboardSummary Summary { get; set; } = new();

    public List<WarehouseAnalytics> Warehouses { get; set; } = new();

    public List<OrderStatusAnalytics> OrderStatuses { get; set; } = new();

    public List<ForecastAnalytics> Forecasts { get; set; } = new();

    public List<OperationalAlert> Alerts { get; set; } = new();
}