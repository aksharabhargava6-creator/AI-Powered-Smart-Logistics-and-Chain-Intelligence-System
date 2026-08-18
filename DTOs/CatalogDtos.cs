using System.ComponentModel.DataAnnotations;

namespace LogisticsPlatform.API.DTOs;

// ---------- Category ----------
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CategoryCreateDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ---------- Product ----------
public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
}

public class ProductCreateDto
{
    [Required, MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public int CategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; } = 10;
}

public class ProductUpdateDto : ProductCreateDto
{
    public bool IsActive { get; set; } = true;
}

// ---------- Warehouse ----------
public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int CapacityUnits { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseCreateDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Range(0, int.MaxValue)]
    public int CapacityUnits { get; set; }
}

public class WarehouseUpdateDto : WarehouseCreateDto
{
    public bool IsActive { get; set; } = true;
}
