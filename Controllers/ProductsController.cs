using LogisticsPlatform.API.Data;
using LogisticsPlatform.API.DTOs;
using LogisticsPlatform.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsPlatform.API.Controllers;

/// <summary>
/// Basic CRUD for products. Implements the product side of FR-02
/// (Product & Inventory Management). Stock movement/transfer endpoints
/// belong to a separate InventoryController in Sprint 2.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db) => _db = db;

    /// <summary>Lists products with optional search, category filter, and pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        return Ok(ToDto(product));
    }

    /// <summary>Returns products whose stock across all warehouses is at or below their reorder level (feeds FR-10 alerts).</summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStock()
    {
        var lowStock = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.InventoryBalances.Sum(ib => (int?)ib.QuantityOnHand ?? 0) <= p.ReorderLevel)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(lowStock);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager},{AppRoles.WarehouseManager}")]
    public async Task<ActionResult<ProductDto>> Create(ProductCreateDto dto)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists) return BadRequest(new { message = "CategoryId does not exist." });

        var skuTaken = await _db.Products.AnyAsync(p => p.Sku == dto.Sku);
        if (skuTaken) return Conflict(new { message = "A product with this SKU already exists." });

        var product = new Product
        {
            Sku = dto.Sku,
            Name = dto.Name,
            Description = dto.Description,
            UnitPrice = dto.UnitPrice,
            CategoryId = dto.CategoryId,
            ReorderLevel = dto.ReorderLevel
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        await _db.Entry(product).Reference(p => p.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager},{AppRoles.WarehouseManager}")]
    public async Task<IActionResult> Update(int id, ProductUpdateDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists) return BadRequest(new { message = "CategoryId does not exist." });

        var skuTaken = await _db.Products.AnyAsync(p => p.Sku == dto.Sku && p.Id != id);
        if (skuTaken) return Conflict(new { message = "Another product already uses this SKU." });

        product.Sku = dto.Sku;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.UnitPrice = dto.UnitPrice;
        product.CategoryId = dto.CategoryId;
        product.ReorderLevel = dto.ReorderLevel;
        product.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Soft-deletes a product (sets IsActive = false) to preserve historical inventory/order references.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Description = p.Description,
        UnitPrice = p.UnitPrice,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        ReorderLevel = p.ReorderLevel,
        IsActive = p.IsActive
    };
}
