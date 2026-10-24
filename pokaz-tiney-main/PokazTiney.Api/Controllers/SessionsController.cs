using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.DTOs;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SessionsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/sessions
    // GET: api/sessions?movieId=3
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions(
        [FromQuery] int? movieId,
        CancellationToken cancellationToken)
    {
        var query = _context.Sessions.AsNoTracking();

        if (movieId.HasValue)
            query = query.Where(s => s.MovieId == movieId.Value);

        var sessions = await query
            .OrderBy(s => s.StartTime)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                MovieId = s.MovieId,
                MovieTitle = s.Movie.Title,
                StartTime = s.StartTime,
                Hall = s.Hall,
                Price = s.Price
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    // GET: api/sessions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionDto>> GetSession(int id, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                MovieId = s.MovieId,
                MovieTitle = s.Movie.Title,
                StartTime = s.StartTime,
                Hall = s.Hall,
                Price = s.Price
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            return NotFound();

        return Ok(session);
    }

    // POST: api/sessions
    [HttpPost]
    public async Task<ActionResult<SessionDto>> CreateSession(CreateSessionDto dto, CancellationToken cancellationToken)
    {
        var movieExists = await _context.Movies.AnyAsync(m => m.Id == dto.MovieId, cancellationToken);
        if (!movieExists)
            return BadRequest($"Movie with id {dto.MovieId} does not exist.");

        var session = new Session
        {
            MovieId = dto.MovieId,
            StartTime = dto.StartTime,
            Hall = dto.Hall,
            Price = dto.Price
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        var movie = await _context.Movies.FirstAsync(m => m.Id == dto.MovieId, cancellationToken);

        var result = new SessionDto
        {
            Id = session.Id,
            MovieId = session.MovieId,
            MovieTitle = movie.Title,
            StartTime = session.StartTime,
            Hall = session.Hall,
            Price = session.Price
        };

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, result);
    }

    // PUT: api/sessions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSession(int id, UpdateSessionDto dto, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
            return NotFound();

        var movieExists = await _context.Movies.AnyAsync(m => m.Id == dto.MovieId, cancellationToken);
        if (!movieExists)
            return BadRequest($"Movie with id {dto.MovieId} does not exist.");

        session.MovieId = dto.MovieId;
        session.StartTime = dto.StartTime;
        session.Hall = dto.Hall;
        session.Price = dto.Price;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE: api/sessions/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSession(int id, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
            return NotFound();

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}