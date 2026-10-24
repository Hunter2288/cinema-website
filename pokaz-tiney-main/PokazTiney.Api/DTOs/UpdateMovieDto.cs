using System.ComponentModel.DataAnnotations;

namespace PokazTiney.Api.DTOs;

public class UpdateMovieDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Genre { get; set; } = string.Empty;

    [Range(1, 600)]
    public int DurationMinutes { get; set; }

    public string? PosterUrl { get; set; }
}