using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public record CreateCorporateRequest(
    int? MovieId, int? UserId, string LocationType, string? Address, int Guests, string EventType,
    string Name, string Phone, string Email, string DesiredDate, string? Comment);

[ApiController]
[Route("api/[controller]")]
public class CorporateRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CorporateRequestsController(AppDbContext context)
    {
        _context = context;
    }

    private static decimal Estimate(string type, int guests)
    {
        var basePrice = type == "cinema" ? 4500m : type == "restaurant" ? 6000m : 7500m;
        return basePrice + Math.Max(0, guests - 20) * 50m;
    }

    private static object Shape(CorporateRequest r, string? movieTitle) => new
    {
        id = r.Id,
        number = $"ЗП-{r.Id:D4}",
        contactName = r.ContactName,
        contactPhone = r.ContactPhone,
        contactEmail = r.ContactEmail,
        location = r.Location,
        locationType = r.LocationType,
        eventType = r.EventType,
        desiredDate = r.DesiredDate,
        guestsCount = r.GuestsCount,
        comment = r.Comment,
        status = r.Status.ToString(),
        movieId = r.MovieId,
        movieTitle,
        estimatedPrice = r.EstimatedPrice,
        createdAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
    };

    // POST: api/corporaterequests
    [HttpPost]
    public async Task<IActionResult> Create(CreateCorporateRequest dto, CancellationToken ct)
    {
        var name = dto.Name?.Trim() ?? "";
        var phone = dto.Phone?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var type = (dto.LocationType ?? "").Trim().ToLowerInvariant();

        if (name.Length < 2) return BadRequest("Введіть ім'я.");
        if (phone.Length < 10) return BadRequest("Введіть коректний номер телефону.");
        if (!email.Contains('@') || email.Length < 5) return BadRequest("Введіть коректний email.");
        if (dto.Guests < 1 || dto.Guests > 500) return BadRequest("Кількість гостей має бути від 1 до 500.");
        if (type != "cinema" && type != "restaurant" && type != "custom") return BadRequest("Оберіть локацію.");
        if (type == "custom" && string.IsNullOrWhiteSpace(dto.Address)) return BadRequest("Вкажіть адресу проведення.");
        if (!DateTime.TryParse(dto.DesiredDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var when) || when <= DateTime.Now)
            return BadRequest("Оберіть дату й час у майбутньому.");

        string? movieTitle = null;
        if (dto.MovieId.HasValue)
        {
            movieTitle = await _context.Movies.Where(m => m.Id == dto.MovieId.Value).Select(m => m.Title).FirstOrDefaultAsync(ct);
            if (movieTitle is null) return BadRequest("Фільм не знайдено.");
        }

        var label = type == "cinema" ? "Кінозал" : type == "restaurant" ? "Ресторан" : dto.Address!.Trim();

        var request = new CorporateRequest
        {
            ContactName = name,
            ContactPhone = phone,
            ContactEmail = email,
            Location = label,
            LocationType = type,
            EventType = dto.EventType?.Trim() ?? "",
            MovieId = dto.MovieId,
            UserId = dto.UserId,
            DesiredDate = when,
            GuestsCount = dto.Guests,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            EstimatedPrice = Estimate(type, dto.Guests)
        };

        _context.CorporateRequests.Add(request);
        await _context.SaveChangesAsync(ct);
        return StatusCode(201, Shape(request, movieTitle));
    }

    // GET: api/corporaterequests/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var r = await _context.CorporateRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        var title = r.MovieId.HasValue
            ? await _context.Movies.Where(m => m.Id == r.MovieId.Value).Select(m => m.Title).FirstOrDefaultAsync(ct)
            : null;
        return Ok(Shape(r, title));
    }

    // GET: api/corporaterequests?userId=1
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int userId, CancellationToken ct)
    {
        var list = await _context.CorporateRequests.AsNoTracking()
            .Where(r => r.UserId == userId).OrderByDescending(r => r.Id).ToListAsync(ct);
        var ids = list.Where(r => r.MovieId.HasValue).Select(r => r.MovieId!.Value).Distinct().ToList();
        var titles = await _context.Movies.Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Title, ct);
        return Ok(list.Select(r => Shape(r, r.MovieId.HasValue && titles.TryGetValue(r.MovieId.Value, out var t) ? t : null)));
    }
}