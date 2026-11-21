using System.Security.Claims;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet]
    public async Task<ActionResult<CartResponseDto>> GetCart()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await BuildCartAsync(userId));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponseDto>> AddItem(AddToCartDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product == null) return NotFound(new { message = "Product not found." });

        var item = await _db.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == userId && c.ProductId == dto.ProductId);
        var resultingQuantity = dto.Quantity + (item?.Quantity ?? 0);
        if (resultingQuantity > product.StockQuantity)
            return BadRequest(new { message = $"Insufficient stock for {product.Name}." });

        if (item == null)
        {
            _db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            });
        }
        else
        {
            item.Quantity = resultingQuantity;
        }

        await _db.SaveChangesAsync();
        return Ok(await BuildCartAsync(userId));
    }

    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<CartResponseDto>> UpdateItem(int productId, UpdateCartItemDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var item = await _db.CartItems
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item == null) return NotFound();

        if (dto.Quantity == 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            if (item.Product == null || dto.Quantity > item.Product.StockQuantity)
                return BadRequest(new { message = $"Insufficient stock for {item.Product?.Name ?? "product"}." });
            item.Quantity = dto.Quantity;
        }

        await _db.SaveChangesAsync();
        return Ok(await BuildCartAsync(userId));
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<CartResponseDto>> RemoveItem(int productId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return Ok(await BuildCartAsync(userId));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var items = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<CartResponseDto> BuildCartAsync(int userId)
    {
        var rows = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var items = rows.Select(c =>
        {
            var product = c.Product!;
            return new CartItemResponseDto(
                product.Id,
                product.Name,
                product.Price,
                product.ImageUrl,
                product.StockQuantity,
                c.Quantity,
                product.Price * c.Quantity);
        }).ToList();

        return new CartResponseDto(
            items,
            items.Sum(i => i.Quantity),
            items.Sum(i => i.LineTotal));
    }
}
