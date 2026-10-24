using System.Net.Sockets;

namespace PokazTiney.Api.Models;

public class Session
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public string Hall { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}