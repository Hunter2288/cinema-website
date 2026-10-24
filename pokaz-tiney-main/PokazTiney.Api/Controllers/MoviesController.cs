using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.DTOs;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/movies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(CancellationToken cancellationToken)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Select(m => new MovieDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PosterUrl = m.PosterUrl,
                BackdropUrl = m.BackdropUrl,
                TrailerUrl = m.TrailerUrl,
                Rating = m.Rating,
                ReleaseDate = m.ReleaseDate,
                Director = m.Director,
                Cast = m.Cast,
                Language = m.Language,
                Format = m.Format,
                AgeRating = m.AgeRating,
                Country = m.Country,
                Year = m.Year,
                Stills = m.Stills
            })
            .ToListAsync(cancellationToken);

        return Ok(movies);
    }

    // GET: api/movies/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDto>> GetMovie(int id, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MovieDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Genre = m.Genre,
                DurationMinutes = m.DurationMinutes,
                PosterUrl = m.PosterUrl,
                BackdropUrl = m.BackdropUrl,
                TrailerUrl = m.TrailerUrl,
                Rating = m.Rating,
                ReleaseDate = m.ReleaseDate,
                Director = m.Director,
                Cast = m.Cast,
                Language = m.Language,
                Format = m.Format,
                AgeRating = m.AgeRating,
                Country = m.Country,
                Year = m.Year,
                Stills = m.Stills
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (movie is null)
            return NotFound();

        return Ok(movie);
    }

    // POST: api/movies
    [HttpPost]
    public async Task<ActionResult<MovieDto>> CreateMovie(CreateMovieDto dto, CancellationToken cancellationToken)
    {
        var movie = new Movie
        {
            Title = dto.Title,
            Description = dto.Description,
            Genre = dto.Genre,
            DurationMinutes = dto.DurationMinutes,
            PosterUrl = dto.PosterUrl
        };

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync(cancellationToken);

        var result = new MovieDto
        {
            Id = movie.Id,
            Title = movie.Title,
            Description = movie.Description,
            Genre = movie.Genre,
            DurationMinutes = movie.DurationMinutes,
            PosterUrl = movie.PosterUrl
        };

        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, result);
    }

    // PUT: api/movies/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMovie(int id, UpdateMovieDto dto, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (movie is null)
            return NotFound();

        movie.Title = dto.Title;
        movie.Description = dto.Description;
        movie.Genre = dto.Genre;
        movie.DurationMinutes = dto.DurationMinutes;
        movie.PosterUrl = dto.PosterUrl;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE: api/movies/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (movie is null)
            return NotFound();

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}