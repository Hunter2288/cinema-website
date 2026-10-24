using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PokazTiney.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CorporateRequest> CorporateRequests => Set<CorporateRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.SessionId, t.SeatNumber })
            .IsUnique()
            .HasFilter("[Status] <> 2");

        modelBuilder.Entity<Session>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);
    }
}