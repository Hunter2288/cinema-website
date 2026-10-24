using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public record RegisterRequest(string Name, string Email, string Password, string? Phone);
public record LoginRequest(string Email, string Password);

public static class UserShape
{
    public static object From(User u) => new
    {
        id = u.Id,
        name = (u.FirstName + " " + u.LastName).Trim(),
        firstName = u.FirstName,
        lastName = u.LastName,
        email = u.Email,
        phone = u.Phone,
        birthDate = u.BirthDate,
        avatarUrl = u.AvatarUrl,
        role = u.Role.ToString()
    };
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest dto, CancellationToken cancellationToken)
    {
        var name = dto.Name?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var password = dto.Password ?? "";
        var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();

        if (name.Length < 2)
            return BadRequest("Введіть ім'я (мінімум 2 символи).");
        if (!email.Contains('@') || email.Length < 5)
            return BadRequest("Введіть коректний email.");
        if (password.Length < 6)
            return BadRequest("Пароль має містити щонайменше 6 символів.");

        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var first = parts[0];
        var last = parts.Length > 1 ? parts[1] : "";

        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existing is not null && existing.PasswordHash != "")
            return Conflict("Користувач з таким email уже існує.");

        if (existing is not null)
        {
            // гостьовий обліковий запис (квитки без реєстрації) перетворюється на повноцінний
            existing.FirstName = first;
            existing.LastName = last;
            existing.Phone = phone ?? existing.Phone;
            existing.PasswordHash = _hasher.HashPassword(existing, password);
            await _context.SaveChangesAsync(cancellationToken);
            return StatusCode(201, UserShape.From(existing));
        }

        var user = new User
        {
            FirstName = first,
            LastName = last,
            Email = email,
            Phone = phone,
            Role = UserRole.Viewer
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, UserShape.From(user));
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest dto, CancellationToken cancellationToken)
    {
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var password = dto.Password ?? "";

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
            return Unauthorized("Невірний email або пароль.");

        try
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Невірний email або пароль.");
        }
        catch (FormatException)
        {
            return Unauthorized("Невірний email або пароль.");
        }

        return Ok(UserShape.From(user));
    }
}