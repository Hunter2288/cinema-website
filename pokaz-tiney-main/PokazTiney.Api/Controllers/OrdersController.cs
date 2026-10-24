using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public static class TicketHolds
{
    public const int Minutes = 10;
    public const decimal VipExtra = 50m;
    public const decimal FeeRate = 0.05m;
    public const int Rows = 5, SeatsPerRow = 8, VipRow = 5;

    // неоплачені квитки без способу оплати звільняються через 10 хвилин
    public static Task<int> ExpireAsync(AppDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-Minutes);
        return db.Tickets
            .Where(t => t.Status == TicketStatus.Pending && t.PaymentMethod == null && t.CreatedAt < cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TicketStatus.Cancelled), ct);
    }

    public static bool TryParseSeat(string? code, out int row, out int seat)
    {
        row = 0;
        seat = 0;
        var parts = (code ?? "").Split('-');
        return parts.Length == 2
            && int.TryParse(parts[0], out row) && int.TryParse(parts[1], out seat)
            && row >= 1 && row <= Rows && seat >= 1 && seat <= SeatsPerRow;
    }
}

public record CreateOrderRequest(int SessionId, List<string> Seats, int? UserId, string Email, string Phone);
public record PayRequest(string Method, string? Email);
public record PromoRequest(string Code);

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static object? Shape(List<Ticket> tickets)
    {
        if (tickets.Count == 0) return null;
        var first = tickets[0];
        var s = first.Session;
        var m = s.Movie;

        var active = tickets.Where(t => t.Status != TicketStatus.Cancelled).ToList();
        var shown = active.Count > 0 ? active : tickets;

        var sub = shown.Sum(t => t.Price);
        var disc = shown.Sum(t => t.Discount);
        var fee = Math.Round((sub - disc) * TicketHolds.FeeRate, 0);

        var onSite = shown.Any(t => t.PaymentMethod == "onsite");
        var status = active.Count == 0 ? "Cancelled"
            : active.All(t => t.Status == TicketStatus.Paid) ? "Paid"
            : onSite ? "OnSite" : "Pending";

        DateTime? expires = status == "Pending" ? Utc(first.CreatedAt.AddMinutes(TicketHolds.Minutes)) : null;

        return new
        {
            number = first.OrderNumber,
            status,
            createdAt = Utc(first.CreatedAt),
            expiresAt = expires,
            paymentMethod = first.PaymentMethod,
            paidAt = first.PaidAt.HasValue ? Utc(first.PaidAt.Value) : (DateTime?)null,
            email = first.ContactEmail,
            phone = first.ContactPhone,
            userId = first.UserId,
            session = new { id = s.Id, startTime = s.StartTime, hall = s.Hall, location = s.Location, price = s.Price },
            movie = new
            {
                id = m.Id,
                title = m.Title,
                genre = m.Genre,
                durationMinutes = m.DurationMinutes,
                ageRating = m.AgeRating,
                posterUrl = m.PosterUrl,
                trailerUrl = m.TrailerUrl,
                rating = m.Rating
            },
            seats = shown.Select(t => new { id = t.Id, code = t.SeatNumber, price = t.Price, status = t.Status.ToString() }).ToList(),
            subtotal = sub,
            discount = disc,
            fee,
            total = sub - disc + fee
        };
    }

    private async Task<object?> BuildAsync(string number, CancellationToken ct)
    {
        var tickets = await _context.Tickets.AsNoTracking()
            .Include(t => t.Session).ThenInclude(s => s.Movie)
            .Where(t => t.OrderNumber == number)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);
        return Shape(tickets);
    }

    // POST: api/orders  — утримує місця на 10 хвилин
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest dto, CancellationToken ct)
    {
        await TicketHolds.ExpireAsync(_context, ct);

        var seats = (dto.Seats ?? new List<string>()).Select(s => s.Trim()).Distinct().ToList();
        if (seats.Count == 0) return BadRequest("Оберіть хоча б одне місце.");
        if (seats.Count > 10) return BadRequest("За одне замовлення можна обрати не більше 10 місць.");
        if (seats.Any(s => !TicketHolds.TryParseSeat(s, out _, out _))) return BadRequest("Некоректний номер місця.");

        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var phone = dto.Phone?.Trim() ?? "";
        if (!email.Contains('@') || email.Length < 5) return BadRequest("Введіть коректний email.");
        if (phone.Length < 10) return BadRequest("Введіть коректний номер телефону.");

        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == dto.SessionId, ct);
        if (session is null || session.StartTime <= DateTime.Now)
            return BadRequest("Сеанс недоступний для бронювання.");

        var taken = await _context.Tickets
            .Where(t => t.SessionId == session.Id && t.Status != TicketStatus.Cancelled && seats.Contains(t.SeatNumber))
            .Select(t => t.SeatNumber)
            .ToListAsync(ct);
        if (taken.Count > 0) return Conflict("Місце вже зайняте: " + string.Join(", ", taken) + ".");

        User? user = null;
        if (dto.UserId.HasValue)
            user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId.Value, ct);

        if (user is null)
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is not null && user.PasswordHash != "")
                return Conflict("Цей email уже зареєстровано. Увійдіть в акаунт.");

            if (user is null)
            {
                user = new User
                {
                    FirstName = email.Split('@')[0],
                    LastName = "",
                    Email = email,
                    PasswordHash = "",
                    Phone = phone,
                    Role = UserRole.Viewer
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync(ct);
            }
        }

        string number;
        do { number = "PT-" + Random.Shared.Next(100000, 1000000); }
        while (await _context.Tickets.AnyAsync(t => t.OrderNumber == number, ct));

        foreach (var code in seats)
        {
            TicketHolds.TryParseSeat(code, out var row, out _);
            _context.Tickets.Add(new Ticket
            {
                SessionId = session.Id,
                UserId = user.Id,
                SeatNumber = code,
                Status = TicketStatus.Pending,
                OrderNumber = number,
                Price = session.Price + (row == TicketHolds.VipRow ? TicketHolds.VipExtra : 0m),
                ContactEmail = email,
                ContactPhone = phone
            });
        }

        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict("Місце щойно забронював інший користувач. Оберіть інше."); }

        return Ok(await BuildAsync(number, ct));
    }

    // GET: api/orders/PT-123456
    [HttpGet("{number}")]
    public async Task<IActionResult> Get(string number, CancellationToken ct)
    {
        await TicketHolds.ExpireAsync(_context, ct);
        var order = await BuildAsync(number, ct);
        return order is null ? NotFound() : Ok(order);
    }

    // GET: api/orders?userId=1
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int userId, CancellationToken ct)
    {
        await TicketHolds.ExpireAsync(_context, ct);
        var tickets = await _context.Tickets.AsNoTracking()
            .Include(t => t.Session).ThenInclude(s => s.Movie)
            .Where(t => t.UserId == userId && t.OrderNumber != null)
            .OrderByDescending(t => t.Id)
            .ToListAsync(ct);

        var orders = tickets
            .GroupBy(t => t.OrderNumber!)
            .Select(g => Shape(g.OrderBy(t => t.Id).ToList()))
            .ToList();
        return Ok(orders);
    }

    // POST: api/orders/PT-123456/promo
    [HttpPost("{number}/promo")]
    public async Task<IActionResult> Promo(string number, PromoRequest dto, CancellationToken ct)
    {
        await TicketHolds.ExpireAsync(_context, ct);
        if ((dto.Code ?? "").Trim().ToUpperInvariant() != "TINI10")
            return BadRequest("Невірний промокод.");

        var tickets = await _context.Tickets
            .Where(t => t.OrderNumber == number && t.Status == TicketStatus.Pending)
            .ToListAsync(ct);
        if (tickets.Count == 0) return NotFound();

        foreach (var t in tickets) t.Discount = Math.Round(t.Price * 0.10m, 0);
        await _context.SaveChangesAsync(ct);
        return Ok(await BuildAsync(number, ct));
    }

    // POST: api/orders/PT-123456/pay  (оплата імітується, дані картки на сервер не надходять)
    [HttpPost("{number}/pay")]
    public async Task<IActionResult> Pay(string number, PayRequest dto, CancellationToken ct)
    {
        await TicketHolds.ExpireAsync(_context, ct);

        var tickets = await _context.Tickets.Where(t => t.OrderNumber == number).ToListAsync(ct);
        if (tickets.Count == 0) return NotFound();
        if (tickets.All(t => t.Status == TicketStatus.Cancelled))
            return Conflict("Час утримання місць минув. Оберіть місця ще раз.");

        var method = (dto.Method ?? "").Trim().ToLowerInvariant();
        if (method != "card" && method != "wallet" && method != "onsite")
            return BadRequest("Оберіть спосіб оплати.");

        var email = dto.Email?.Trim().ToLowerInvariant();
        foreach (var t in tickets.Where(t => t.Status != TicketStatus.Cancelled))
        {
            t.PaymentMethod = method;
            if (!string.IsNullOrEmpty(email) && email.Contains('@')) t.ContactEmail = email;
            if (method != "onsite")
            {
                t.Status = TicketStatus.Paid;
                t.PaidAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);
        return Ok(await BuildAsync(number, ct));
    }

    // POST: api/orders/PT-123456/cancel
    [HttpPost("{number}/cancel")]
    public async Task<IActionResult> Cancel(string number, CancellationToken ct)
    {
        var tickets = await _context.Tickets.Where(t => t.OrderNumber == number).ToListAsync(ct);
        if (tickets.Count == 0) return NotFound();

        foreach (var t in tickets) t.Status = TicketStatus.Cancelled;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}