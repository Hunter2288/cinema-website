/* =============================================================
   Показ Тіней — генерація розкладу на 7 днів уперед.
   Скрипт можна запускати повторно (наприклад, раз на тиждень):
   майбутні сеанси без проданих квитків перестворюються, сеанси
   з квитками не чіпаються, дублі не створюються.
   ============================================================= */
USE PokazTiney;
SET NOCOUNT ON;

DECLARE @today DATETIME2 = CAST(CAST(SYSDATETIME() AS DATE) AS DATETIME2);
DECLARE @days  INT = 7;

DELETE s
FROM dbo.Sessions s
WHERE s.StartTime > SYSDATETIME()
  AND NOT EXISTS (SELECT 1 FROM dbo.Tickets t WHERE t.SessionId = s.Id);

CREATE TABLE #days (n INT NOT NULL);
INSERT INTO #days (n)
SELECT TOP (@days) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) - 1 FROM sys.all_objects;

/* ---- кінотеатр: 4 сеанси щодня (12:00, 15:00, 18:30, 21:15) для фільмів, які вже вийшли ---- */
INSERT INTO dbo.Sessions (MovieId, StartTime, Hall, [Location], Price)
SELECT m.Id,
       DATEADD(MINUTE, t.mins, DATEADD(DAY, d.n, @today)),
       CASE WHEN m.Id % 2 = 0 THEN N'Зал 2' ELSE N'Зал 1' END,
       N'Кінотеатр', 140
FROM dbo.Movies m
CROSS JOIN #days d
CROSS JOIN (VALUES (720), (900), (1110), (1275)) t(mins)
WHERE (m.ReleaseDate IS NULL OR m.ReleaseDate <= SYSDATETIME())
  AND DATEADD(MINUTE, t.mins, DATEADD(DAY, d.n, @today)) > SYSDATETIME()
  AND NOT EXISTS (SELECT 1 FROM dbo.Sessions x
                  WHERE x.MovieId = m.Id AND x.[Location] = N'Кінотеатр'
                    AND x.StartTime = DATEADD(MINUTE, t.mins, DATEADD(DAY, d.n, @today)));

/* ---- покази просто неба о 20:30 ---- */
CREATE TABLE #open (Title NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                    [Location] NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                    Price DECIMAL(18,2) NOT NULL);
INSERT INTO #open (Title, [Location], Price) VALUES
(N'Вічність',               N'Жовтневий парк',      160),
(N'Чарлі та Різдвяне диво', N'Жовтневий парк',      160),
(N'Великий Пивний Забіг',   N'Жовтневий парк',      160),
(N'Оппенгеймер',            N'Жовтневий парк',      160),
(N'Три мудреці і хлопчик',  N'Набережна річки Прут', 160),
(N'Підвищення',             N'Набережна річки Прут', 160),
(N'Чародійка',              N'Набережна річки Прут', 160),
(N'Діва проти біди',        N'Набережна річки Прут', 160),
(N'Бджоляр',                N'Дах', 180),
(N'Твоя провина',           N'Дах', 180),
(N'Покинь, якщо кохаєш',    N'Дах', 180),
(N'Наша маленька таємниця', N'Дах', 180);

INSERT INTO dbo.Sessions (MovieId, StartTime, Hall, [Location], Price)
SELECT m.Id, DATEADD(MINUTE, 1230, DATEADD(DAY, d.n, @today)), N'Відкритий майданчик', o.[Location], o.Price
FROM #open o
JOIN dbo.Movies m ON m.Title = o.Title
CROSS JOIN #days d
WHERE DATEADD(MINUTE, 1230, DATEADD(DAY, d.n, @today)) > SYSDATETIME()
  AND NOT EXISTS (SELECT 1 FROM dbo.Sessions x
                  WHERE x.MovieId = m.Id AND x.[Location] = o.[Location]
                    AND x.StartTime = DATEADD(MINUTE, 1230, DATEADD(DAY, d.n, @today)));

DROP TABLE #open;
DROP TABLE #days;

SELECT [Location], COUNT(*) AS Sessions, MIN(StartTime) AS FirstSession, MAX(StartTime) AS LastSession
FROM dbo.Sessions GROUP BY [Location] ORDER BY [Location];