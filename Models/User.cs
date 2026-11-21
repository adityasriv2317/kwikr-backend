using System.ComponentModel.DataAnnotations;
namespace Ecommerce.Api.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(180)]
    public string Email { get; set; } = string.Empty;
    [Required, MaxLength(300)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string Role { get; set; } = "Customer"; // Customer or Admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}