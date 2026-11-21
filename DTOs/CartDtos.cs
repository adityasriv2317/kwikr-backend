using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Dtos;

public record CartItemResponseDto(
    int ProductId,
    string Name,
    decimal Price,
    string ImageUrl,
    int StockQuantity,
    int Quantity,
    decimal LineTotal
);

public record CartResponseDto(
    List<CartItemResponseDto> Items,
    int TotalItems,
    decimal Subtotal
);

public record AddToCartDto(
    [param: Range(1, int.MaxValue)] int ProductId,
    [param: Range(1, int.MaxValue)] int Quantity
);

public record UpdateCartItemDto(
    [param: Range(0, int.MaxValue)] int Quantity
);
