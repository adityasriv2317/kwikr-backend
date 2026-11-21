using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Dtos;

public record OrderItemResponseDto(
    int ProductId,
    string ProductName,
    string ImageUrl,
    decimal UnitPrice,
    int Quantity
);

public record OrderResponseDto(
    int Id,
    decimal TotalAmount,
    string Status,
    string ShippingAddress,
    DateTime CreatedAt,
    List<OrderItemResponseDto> Items,
    string? CustomerName = null,
    string? CustomerEmail = null
);

public record UpdateOrderStatusDto(
    [param: Required] string Status
);

public record CheckoutDto(
    [param: Required] string FullName,
    [param: Required] string ShippingAddress,
    [param: Required] string City,
    [param: Required] string PostalCode
);

