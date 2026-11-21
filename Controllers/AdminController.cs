using Ecommerce.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> Dashboard()
    {
        var totalRevenue = await _db.Orders
            .Where(o => o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var result = new AdminDashboardDto(
            await _db.Products.CountAsync(),
            await _db.Orders.CountAsync(),
            await _db.Users.CountAsync(u => u.Role == "Customer"),
            await _db.Categories.CountAsync(),
            totalRevenue,
            await _db.Products.CountAsync(p => p.StockQuantity <= 5),
            await _db.Orders.CountAsync(o => o.Status == "Pending"));

        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto(u.Id, u.FullName, u.Email, u.Role, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    [HttpPatch("users/{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
    {
        if (dto.Role != "Customer" && dto.Role != "Admin")
            return BadRequest(new { message = "Role must be Customer or Admin." });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Email.Equals("admin@kwikr.local", StringComparison.OrdinalIgnoreCase) &&
            dto.Role != "Admin")
            return BadRequest(new { message = "The main administrator cannot be changed to a customer." });

        user.Role = dto.Role;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record AdminDashboardDto(
    int ProductCount,
    int OrderCount,
    int CustomerCount,
    int CategoryCount,
    decimal TotalRevenue,
    int LowStockCount,
    int PendingOrderCount);

public record AdminUserDto(int Id, string FullName, string Email, string Role, DateTime CreatedAt);
public record UpdateUserRoleDto(string Role);
