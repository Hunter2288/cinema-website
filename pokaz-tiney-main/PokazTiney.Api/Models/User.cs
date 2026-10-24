namespace PokazTiney.Api.Models;

public enum UserRole
{
    Viewer,
    Manager,
    Admin
}

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}