using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Dtos;

public record RegisterDto(
    [param: Required, MaxLength(120)] string FullName,
    [param: Required, EmailAddress, MaxLength(180)] string Email,
    [param: Required, MinLength(6), MaxLength(100)] string Password
);

public record LoginDto(
    [param: Required, EmailAddress, MaxLength(180)] string Email,
    [param: Required] string Password
);

public record AuthResponseDto(
    string Token,
    int UserId,
    string FullName,
    string Email,
    string Role
);

public record UserProfileDto(int UserId, string FullName, string Email, string Role);
