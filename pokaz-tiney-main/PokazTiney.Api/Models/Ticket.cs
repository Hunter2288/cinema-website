using System.ComponentModel.DataAnnotations.Schema;

namespace PokazTiney.Api.Models;

public enum TicketStatus
{
    Pending,
    Paid,
    Cancelled
}

public class Ticket
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string SeatNumber { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? OrderNumber { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Discount { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}