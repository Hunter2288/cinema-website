using System.ComponentModel.DataAnnotations;

namespace PokazTiney.Api.DTOs;

public class CreateSessionDto
{
    [Required]
    public int MovieId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required, MaxLength(50)]
    public string Hall { get; set; } = string.Empty;

    [Range(0, 10000)]
    public decimal Price { get; set; }
}