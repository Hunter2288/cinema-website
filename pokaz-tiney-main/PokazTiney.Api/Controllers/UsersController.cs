using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public record UpdateProfileRequest(string FirstName, string? LastName, string Email, string? Phone, string? BirthDate, string? AvatarUrl);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record DeleteAccountRequest(string Password);

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    private bool Verify(User u, string password)
    {
        try { return _hasher.VerifyHashedPassword(u, u.PasswordHash, password) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
    }

    // GET: api/users/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(UserShape.From(user));
    }

    // PUT: api/users/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProfileRequest dto, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound();

        var first = dto.FirstName?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        if (first.Length < 2) return BadRequest("Введіть ім'я (мінімум 2 символи).");
        if (!email.Contains('@') || email.Length < 5) return BadRequest("Введіть коректний email.");
        if (await _context.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken))
            return Conflict("Цей email уже використовується.");

        DateTime? birth = null;
        if (!string.IsNullOrWhiteSpace(dto.BirthDate))
        {
            if (!DateTime.TryParse(dto.BirthDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) || d > DateTime.Today)
                return BadRequest("Некоректна дата народження.");
            birth = d.Date;
        }

        if (dto.AvatarUrl is not null)
        {
            if (dto.AvatarUrl == "") user.AvatarUrl = null;
            else if (dto.AvatarUrl.StartsWith("data:image/") && dto.AvatarUrl.Length <= 300_000) user.AvatarUrl = dto.AvatarUrl;
            else return BadRequest("Некоректне зображення профілю.");
        }

        user.FirstName = first;
        user.LastName = dto.LastName?.Trim() ?? "";
        user.Email = email;
        user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        user.BirthDate = birth;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(UserShape.From(user));
    }

    // POST: api/users/5/password
    [HttpPost("{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, ChangePasswordRequest dto, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (!Verify(user, dto.OldPassword ?? "")) return Unauthorized("Поточний пароль невірний.");
        if ((dto.NewPassword ?? "").Length < 6) return BadRequest("Новий пароль має містити щонайменше 6 символів.");

        user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword!);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // POST: api/users/5/delete
    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteAccount(int id, DeleteAccountRequest dto, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (!Verify(user, dto.Password ?? "")) return Unauthorized("Пароль невірний.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}