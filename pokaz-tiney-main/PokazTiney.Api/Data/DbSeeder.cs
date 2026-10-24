using PokazTiney.Api.Models;

namespace PokazTiney.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Movies.Any()) return;

        var movies = new List<Movie>
        {
            new Movie
            {
                Title = "Дюна: Частина друга",
                Description = "Продовження історії Пола Атріда на планеті Арракіс.",
                Genre = "Фантастика",
                DurationMinutes = 166
            },
            new Movie
            {
                Title = "Опівнічний блюз",
                Description = "Романтична драма про випадкову зустріч у нічному місті.",
                Genre = "Драма",
                DurationMinutes = 104
            },
            new Movie
            {
                Title = "Код мовчання",
                Description = "Трилер про хакера, який розкриває змову.",
                Genre = "Трилер",
                DurationMinutes = 118
            }
        };
        context.Movies.AddRange(movies);
        context.SaveChanges();

        var sessions = new List<Session>
        {
            new Session
            {
                MovieId = movies[0].Id,
                StartTime = DateTime.UtcNow.AddDays(1).AddHours(19),
                Hall = "Зал 1",
                Price = 180
            },
            new Session
            {
                MovieId = movies[0].Id,
                StartTime = DateTime.UtcNow.AddDays(2).AddHours(21),
                Hall = "Зал 2",
                Price = 180
            },
            new Session
            {
                MovieId = movies[1].Id,
                StartTime = DateTime.UtcNow.AddDays(1).AddHours(17),
                Hall = "Зал 1",
                Price = 150
            },
            new Session
            {
                MovieId = movies[2].Id,
                StartTime = DateTime.UtcNow.AddDays(3).AddHours(20),
                Hall = "Зал 3",
                Price = 160
            }
        };
        context.Sessions.AddRange(sessions);
        context.SaveChanges();

        var users = new List<User>
        {
            new User
            {
                FirstName = "Анна",
                LastName = "Ковальчук",
                Email = "anna@example.com",
                PasswordHash = "placeholder", // замінимо на реальний хеш, коли зробимо реєстрацію
                Role = UserRole.Viewer
            },
            new User
            {
                FirstName = "Андрій",
                LastName = "Петренко",
                Email = "andriy.manager@example.com",
                PasswordHash = "placeholder",
                Role = UserRole.Manager
            },
            new User
            {
                FirstName = "Олена",
                LastName = "Адмінська",
                Email = "admin@example.com",
                PasswordHash = "placeholder",
                Role = UserRole.Admin
            }
        };
        context.Users.AddRange(users);
        context.SaveChanges();

        context.Tickets.Add(new Ticket
        {
            SessionId = sessions[0].Id,
            UserId = users[0].Id,
            SeatNumber = "A5",
            Status = TicketStatus.Paid
        });
        context.SaveChanges();

        context.CorporateRequests.Add(new CorporateRequest
        {
            ContactName = "Олег Кравченко",
            ContactPhone = "+380501234567",
            Location = "Кав'ярня \"Весна\", вул. Шевченка 12",
            DesiredDate = DateTime.UtcNow.AddDays(10),
            GuestsCount = 20,
            Comment = "Потрібен проєктор і колонки, бажано вечірній сеанс",
            Status = RequestStatus.New
        });
        context.SaveChanges();
    }
}