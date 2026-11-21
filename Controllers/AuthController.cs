using System.Security.Claims;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos;
using Ecommerce.Api.Models;
using Ecommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    public AuthController(AppDbContext db, ITokenService tokens) { _db = db; _tokens = tokens; }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict("An account with this email already exists.");
        if (string.IsNullOrWhiteSpace(dto.FullName) || dto.Password.Length < 6)
            return BadRequest("Full name is required and password must be at least 6 characters.");
        var user = new User { FullName = dto.FullName.Trim(), Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), Role = "Customer" };
       
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
  
        return Ok(new AuthResponseDto(_tokens.CreateToken(user), user.Id, user.FullName, user.Email, user.Role));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");
        return Ok(new AuthResponseDto(_tokens.CreateToken(user), user.Id, user.FullName, user.Email, user.Role));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idText, out var id)) return Unauthorized();
        var user = await _db.Users.FindAsync(id);
        return user == null ? Unauthorized() : Ok(new UserProfileDto(user.Id, user.FullName, user.Email, user.Role));
    }
}