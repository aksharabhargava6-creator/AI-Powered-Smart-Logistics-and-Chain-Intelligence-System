using LogisticsPlatform.API.Data;
using LogisticsPlatform.API.DTOs;
using LogisticsPlatform.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsPlatform.API.Controllers;

/// <summary>
/// Basic CRUD for warehouses. Implements FR-03 (Warehouse Management) at the
/// schema level; capacity/transfer logic against InventoryBalance arrives in Sprint 2.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public WarehousesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Warehouses.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(w => w.IsActive);
        }

        var warehouses = await query.OrderBy(w => w.Name).Select(w => ToDto(w)).ToListAsync();
        return Ok(warehouses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WarehouseDto>> GetById(int id)
    {
        var warehouse = await _db.Warehouses.FindAsync(id);
        if (warehouse is null) return NotFound();

        return Ok(ToDto(warehouse));
    }

    /// <summary>Current total units stored vs. capacity — a quick check before assigning inbound stock.</summary>
    [HttpGet("{id:int}/utilization")]
    public async Task<ActionResult> GetUtilization(int id)
    {
        var warehouse = await _db.Warehouses.FindAsync(id);
        if (warehouse is null) return NotFound();

        var unitsStored = await _db.InventoryBalances
            .Where(ib => ib.WarehouseId == id)
            .SumAsync(ib => (int?)ib.QuantityOnHand) ?? 0;

        return Ok(new
        {
            warehouseId = id,
            capacityUnits = warehouse.CapacityUnits,
            unitsStored,
            utilizationPercent = warehouse.CapacityUnits == 0
                ? 0
                : Math.Round(unitsStored * 100.0 / warehouse.CapacityUnits, 1)
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager}")]
    public async Task<ActionResult<WarehouseDto>> Create(WarehouseCreateDto dto)
    {
        var warehouse = new Warehouse
        {
            Name = dto.Name,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CapacityUnits = dto.CapacityUnits
        };

        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, ToDto(warehouse));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager},{AppRoles.WarehouseManager}")]
    public async Task<IActionResult> Update(int id, WarehouseUpdateDto dto)
    {
        var warehouse = await _db.Warehouses.FindAsync(id);
        if (warehouse is null) return NotFound();

        warehouse.Name = dto.Name;
        warehouse.Address = dto.Address;
        warehouse.Latitude = dto.Latitude;
        warehouse.Longitude = dto.Longitude;
        warehouse.CapacityUnits = dto.CapacityUnits;
        warehouse.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Soft-deletes a warehouse (sets IsActive = false) to preserve historical inventory references.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.SystemAdministrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var warehouse = await _db.Warehouses.FindAsync(id);
        if (warehouse is null) return NotFound();

        var hasStock = await _db.InventoryBalances.AnyAsync(ib => ib.WarehouseId == id && ib.QuantityOnHand > 0);
        if (hasStock)
        {
            return BadRequest(new { message = "Cannot delete a warehouse that still holds inventory." });
        }

        warehouse.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static WarehouseDto ToDto(Warehouse w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Address = w.Address,
        Latitude = w.Latitude,
        Longitude = w.Longitude,
        CapacityUnits = w.CapacityUnits,
        IsActive = w.IsActive
    };
}
