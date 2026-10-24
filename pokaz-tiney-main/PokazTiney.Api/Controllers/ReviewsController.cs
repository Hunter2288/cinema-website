using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public record CreateReviewRequest(int MovieId, int UserId, int Rating, string Text);

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/reviews?movieId=3
    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] int? movieId, CancellationToken cancellationToken)
    {
        var query = _context.Reviews.AsNoTracking();

        if (movieId.HasValue)
            query = query.Where(r => r.MovieId == movieId.Value);

        var list = await query
            .OrderByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.MovieId,
                UserName = (r.User.FirstName + " " + r.User.LastName).Trim(),
                r.Rating,
                r.Text,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    // POST: api/reviews
    [HttpPost]
    public async Task<IActionResult> CreateReview(CreateReviewRequest dto, CancellationToken cancellationToken)
    {
        var text = dto.Text?.Trim() ?? "";

        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Оцінка має бути від 1 до 5.");
        if (text.Length < 3 || text.Length > 1000)
            return BadRequest("Відгук має містити від 3 до 1000 символів.");
        if (!await _context.Movies.AnyAsync(m => m.Id == dto.MovieId, cancellationToken))
            return BadRequest("Фільм не знайдено.");
        if (!await _context.Users.AnyAsync(u => u.Id == dto.UserId, cancellationToken))
            return BadRequest("Користувача не знайдено.");

        var review = new Review
        {
            MovieId = dto.MovieId,
            UserId = dto.UserId,
            Rating = dto.Rating,
            Text = text
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new { review.Id });
    }
}