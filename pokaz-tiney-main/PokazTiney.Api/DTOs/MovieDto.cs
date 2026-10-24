namespace PokazTiney.Api.DTOs;

public class MovieDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public double? Rating { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? Director { get; set; }
    public string? Cast { get; set; }
    public string? Language { get; set; }
    public string? Format { get; set; }
    public string? AgeRating { get; set; }
    public string? Country { get; set; }
    public int? Year { get; set; }
    public string? Stills { get; set; }
}