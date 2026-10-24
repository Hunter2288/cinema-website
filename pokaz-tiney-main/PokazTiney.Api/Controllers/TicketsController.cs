using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;

namespace PokazTiney.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/tickets?sessionId=3
    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] int? sessionId, [FromQuery] int? userId, CancellationToken cancellationToken)
    {
        await TicketHolds.ExpireAsync(_context, cancellationToken);

        var query = _context.Tickets.AsNoTracking();
        if (sessionId.HasValue) query = query.Where(t => t.SessionId == sessionId.Value);
        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

        var list = await query
            .OrderByDescending(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.SessionId,
                t.UserId,
                t.SeatNumber,
                Status = t.Status.ToString(),
                t.OrderNumber,
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(list);
    }
}