using System.ComponentModel.DataAnnotations.Schema;

namespace PokazTiney.Api.Models;

public enum RequestStatus
{
    New,
    UnderReview,
    Confirmed,
    Rejected
}

public class CorporateRequest
{
    public int Id { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int? MovieId { get; set; }
    public int? UserId { get; set; }
    public DateTime DesiredDate { get; set; }
    public int GuestsCount { get; set; }
    public string? Comment { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimatedPrice { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}