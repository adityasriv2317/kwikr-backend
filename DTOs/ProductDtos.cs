using System.ComponentModel.DataAnnotations;
namespace Ecommerce.Api.Dtos;

public record ProductDto(
    int Id, string Name, string Description, decimal Price,
    int StockQuantity, string ImageUrl, int CategoryId,
    string CategoryName
);

public record CreateProductDto(
    string Name, string Description, decimal Price,
    int StockQuantity, string ImageUrl, int CategoryId
);

public record UpdateProductDto(
    [Required, MaxLength(100)] string Name,
    string Description,
    [Range(0.01, 1000000)] decimal Price,
    [Range(0, int.MaxValue)] int StockQuantity,
    string ImageUrl,
    [Range(1, int.MaxValue)] int CategoryId
);