using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto(c.Id, c.Name, c.Slug, c.Products.Count))
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
    {
        var category = await _db.Categories
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponseDto(c.Id, c.Name, c.Slug, c.Products.Count))
            .FirstOrDefaultAsync();
        return category == null ? NotFound() : Ok(category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create(CreateCategoryDto dto)
    {
        var slug = await EnsureUniqueSlugAsync(dto.Name, dto.Slug);
        if (slug == null) return Conflict(new { message = "A category with this slug already exists." });

        var category = new Category { Name = dto.Name.Trim(), Slug = slug };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new CategoryResponseDto(category.Id, category.Name, category.Slug, 0));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(int id, UpdateCategoryDto dto)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        var slug = await EnsureUniqueSlugAsync(dto.Name, dto.Slug, id);
        if (slug == null) return Conflict(new { message = "A category with this slug already exists." });

        category.Name = dto.Name.Trim();
        category.Slug = slug;
        await _db.SaveChangesAsync();
        var count = await _db.Products.CountAsync(p => p.CategoryId == id);
        return Ok(new CategoryResponseDto(category.Id, category.Name, category.Slug, count));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            return Conflict(new { message = "Cannot delete a category that contains products." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> EnsureUniqueSlugAsync(string name, string? requestedSlug, int? excludeId = null)
    {
        var provided = !string.IsNullOrWhiteSpace(requestedSlug);
        var baseSlug = CreateSlug(provided ? requestedSlug! : name);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "category";

        var slug = baseSlug;
        if (provided && await _db.Categories.AnyAsync(c =>
                c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value)))
            return null;

        var suffix = 2;
        while (await _db.Categories.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value)))
            slug = $"{baseSlug}-{suffix++}";
        return slug;
    }

    private static string CreateSlug(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }
        var ascii = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        return Regex.Replace(ascii, @"[^a-z0-9]+", "-").Trim('-');
    }
}
