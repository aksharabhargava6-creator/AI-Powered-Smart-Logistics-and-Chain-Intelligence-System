using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLogisticsApp.Models;

public class User
{
    [Key]
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Product
{
    [Key]
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Warehouse
{
    [Key]
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int CapacityUnits { get; set; }
}

public class Inventory
{
    [Key]
    public int InventoryId { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}

public class Order
{
    [Key]
    public long OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public string OrderStatus { get; set; } = "CREATED";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DeliveryAssignment
{
    [Key]
    public long DeliveryId { get; set; }
    public long OrderId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public string Status { get; set; } = "ASSIGNED";
    public decimal? DestinationLatitude { get; set; }
    public decimal? DestinationLongitude { get; set; }
    public DateTime? EstimatedArrival { get; set; }
}

public class DemandForecast
{
    [Key]
    public long ForecastId { get; set; }
    public int ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public DateTime ForecastDate { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ModelVersion { get; set; } = "NET10-ML-v1.0";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}