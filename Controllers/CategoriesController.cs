using LogisticsPlatform.API.Data;
using LogisticsPlatform.API.DTOs;
using LogisticsPlatform.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsPlatform.API.Controllers;

/// <summary>
/// Basic CRUD for product categories. Supports FR-02 (Product & Inventory Management).
/// Read access: any authenticated user. Write access: Admin / Supply Chain / Warehouse managers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await _db.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        return Ok(new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description });
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager}")]
    public async Task<ActionResult<CategoryDto>> Create(CategoryCreateDto dto)
    {
        var category = new Category { Name = dto.Name, Description = dto.Description };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var result = new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description };
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.SystemAdministrator},{AppRoles.SupplyChainManager}")]
    public async Task<IActionResult> Update(int id, CategoryCreateDto dto)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = dto.Name;
        category.Description = dto.Description;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.SystemAdministrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            return BadRequest(new { message = "Cannot delete a category that still has products assigned to it." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
