using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Dtos;

public record CategoryResponseDto(int Id, string Name, string Slug, int ProductCount);

public record CreateCategoryDto(
    [param: Required, MaxLength(100)] string Name,
    [param: MaxLength(100)] string? Slug
);

public record UpdateCategoryDto(
    [param: Required, MaxLength(100)] string Name,
    [param: MaxLength(100)] string? Slug
);
