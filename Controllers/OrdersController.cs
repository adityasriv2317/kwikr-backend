using System.Security.Claims;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponseDto>> Checkout([FromBody] CheckoutDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var cart = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (cart.Count == 0)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "Your cart is empty." });
        }

        foreach (var item in cart)
        {
            if (item.Product == null || item.Quantity > item.Product.StockQuantity)
            {
                await transaction.RollbackAsync();
                var name = item.Product?.Name ?? "A product";
                return BadRequest(new { message = $"Insufficient stock for {name}." });
            }
        }

        var order = new Order
        {
            UserId = userId,
            Status = "Pending",
            ShippingAddress = $"{dto.ShippingAddress}, {dto.City}, {dto.PostalCode}".Trim(' ', ','),
            TotalAmount = cart.Sum(c => c.Product!.Price * c.Quantity),
            CreatedAt = DateTime.UtcNow
        };

        foreach (var cartItem in cart)
        {
            var product = cartItem.Product!;
            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price,
                Product = product
            });
            product.StockQuantity -= cartItem.Quantity;
        }

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(order));
    }

    [HttpGet("my-orders")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> MyOrders()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var orders = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(orders.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.UserId != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(ToDto(order));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAll([FromQuery] string? status)
    {
        var query = _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(ToDto).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderResponseDto>> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusDto dto)
    {
        var allowed = new[] { "Pending", "Paid", "Shipped", "Delivered", "Cancelled" };
        if (!allowed.Contains(dto.Status))
            return BadRequest(new { message = "Invalid order status." });

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            await transaction.RollbackAsync();
            return NotFound();
        }

        if (dto.Status == "Cancelled" && order.Status != "Cancelled")
        {
            foreach (var item in order.OrderItems)
                item.Product!.StockQuantity += item.Quantity;
        }

        order.Status = dto.Status;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(ToDto(order));
    }

    private static OrderResponseDto ToDto(Order order) => new(
        order.Id,
        order.TotalAmount,
        order.Status,
        order.ShippingAddress,
        order.CreatedAt,
        order.OrderItems.Select(i => new OrderItemResponseDto(
            i.ProductId,
            i.Product?.Name ?? "Product",
            i.Product?.ImageUrl ?? "",
            i.UnitPrice,
            i.Quantity)).ToList(),
        order.User?.FullName,
        order.User?.Email);
}
