/* =============================================================
   Показ Тіней — схема бази даних (Microsoft SQL Server 2019+)
   Скрипт можна запускати повторно: наявні таблиці не чіпаються.
   ============================================================= */

IF DB_ID(N'PokazTiney') IS NULL
    CREATE DATABASE PokazTiney;
GO

USE PokazTiney;
GO

/* ---------- Users ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        FirstName    NVARCHAR(100)  NOT NULL,
        LastName     NVARCHAR(100)  NOT NULL CONSTRAINT DF_Users_LastName DEFAULT N'',
        Email        NVARCHAR(256)  NOT NULL,
        PasswordHash NVARCHAR(MAX)  NOT NULL CONSTRAINT DF_Users_PasswordHash DEFAULT N'',  -- '' = гостьовий запис без пароля
        [Role]       INT            NOT NULL CONSTRAINT DF_Users_Role DEFAULT 0,             -- 0 Viewer, 1 Manager, 2 Admin
        CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        Phone        NVARCHAR(30)   NULL,
        BirthDate    DATETIME2      NULL,
        AvatarUrl    NVARCHAR(MAX)  NULL,
        CONSTRAINT CK_Users_Role CHECK ([Role] IN (0, 1, 2))
    );
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
END;
GO

/* ---------- Movies ---------- */
IF OBJECT_ID(N'dbo.Movies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Movies (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Movies PRIMARY KEY,
        Title           NVARCHAR(200)  NOT NULL,
        [Description]   NVARCHAR(MAX)  NOT NULL CONSTRAINT DF_Movies_Description DEFAULT N'',
        Genre           NVARCHAR(200)  NOT NULL CONSTRAINT DF_Movies_Genre DEFAULT N'',
        DurationMinutes INT            NOT NULL,
        PosterUrl       NVARCHAR(500)  NULL,
        BackdropUrl     NVARCHAR(500)  NULL,
        TrailerUrl      NVARCHAR(500)  NULL,
        Rating          FLOAT          NULL,
        ReleaseDate     DATETIME2      NULL,        -- NULL або дата в минулому = уже в прокаті
        Director        NVARCHAR(200)  NULL,
        [Cast]          NVARCHAR(500)  NULL,
        [Language]      NVARCHAR(100)  NULL,
        [Format]        NVARCHAR(100)  NULL,
        AgeRating       NVARCHAR(10)   NULL,
        Country         NVARCHAR(100)  NULL,
        [Year]          INT            NULL,
        Stills          NVARCHAR(MAX)  NULL,        -- адреси кадрів через «;»
        CONSTRAINT CK_Movies_Duration CHECK (DurationMinutes > 0),
        CONSTRAINT CK_Movies_Rating   CHECK (Rating IS NULL OR (Rating >= 0 AND Rating <= 10))
    );
END;
GO

/* ---------- Sessions ---------- */
IF OBJECT_ID(N'dbo.Sessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sessions PRIMARY KEY,
        MovieId   INT            NOT NULL,
        StartTime DATETIME2      NOT NULL,
        Hall      NVARCHAR(100)  NOT NULL CONSTRAINT DF_Sessions_Hall DEFAULT N'',
        [Location] NVARCHAR(200) NOT NULL CONSTRAINT DF_Sessions_Location DEFAULT N'',
        Price     DECIMAL(18,2)  NOT NULL,
        CONSTRAINT FK_Sessions_Movies FOREIGN KEY (MovieId) REFERENCES dbo.Movies (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Sessions_Price CHECK (Price >= 0)
    );
    CREATE INDEX IX_Sessions_MovieId   ON dbo.Sessions (MovieId);
    CREATE INDEX IX_Sessions_StartTime ON dbo.Sessions (StartTime);
END;
GO

/* ---------- Tickets ---------- */
IF OBJECT_ID(N'dbo.Tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tickets (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tickets PRIMARY KEY,
        SessionId     INT           NOT NULL,
        UserId        INT           NOT NULL,
        SeatNumber    NVARCHAR(20)  NOT NULL,                  -- «ряд-місце», наприклад 3-5
        [Status]      INT           NOT NULL CONSTRAINT DF_Tickets_Status DEFAULT 0,   -- 0 Pending, 1 Paid, 2 Cancelled
        CreatedAt     DATETIME2     NOT NULL CONSTRAINT DF_Tickets_CreatedAt DEFAULT SYSUTCDATETIME(),
        OrderNumber   NVARCHAR(20)  NULL,                      -- PT-123456, спільний для квитків одного замовлення
        Price         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Tickets_Price DEFAULT 0,
        Discount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_Tickets_Discount DEFAULT 0,
        PaymentMethod NVARCHAR(20)  NULL,                      -- card / wallet / onsite
        PaidAt        DATETIME2     NULL,
        ContactEmail  NVARCHAR(256) NULL,
        ContactPhone  NVARCHAR(30)  NULL,
        CONSTRAINT FK_Tickets_Sessions FOREIGN KEY (SessionId) REFERENCES dbo.Sessions (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Tickets_Users    FOREIGN KEY (UserId)    REFERENCES dbo.Users (Id)    ON DELETE CASCADE,
        CONSTRAINT CK_Tickets_Status   CHECK ([Status] IN (0, 1, 2))
    );
    CREATE INDEX IX_Tickets_UserId      ON dbo.Tickets (UserId);
    CREATE INDEX IX_Tickets_OrderNumber ON dbo.Tickets (OrderNumber);
    -- одне місце на сеанс; скасовані квитки (Status = 2) не враховуються
    CREATE UNIQUE INDEX UX_Tickets_Session_Seat ON dbo.Tickets (SessionId, SeatNumber) WHERE [Status] <> 2;
END;
GO

/* ---------- Reviews ---------- */
IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reviews (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
        MovieId   INT            NOT NULL,
        UserId    INT            NOT NULL,
        Rating    INT            NOT NULL,
        [Text]    NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME2      NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Reviews_Movies FOREIGN KEY (MovieId) REFERENCES dbo.Movies (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Reviews_Users  FOREIGN KEY (UserId)  REFERENCES dbo.Users (Id)  ON DELETE CASCADE,
        CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
    );
    CREATE INDEX IX_Reviews_MovieId ON dbo.Reviews (MovieId);
END;
GO

/* ---------- CorporateRequests (заявки на приватний показ) ---------- */
IF OBJECT_ID(N'dbo.CorporateRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorporateRequests (
        Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorporateRequests PRIMARY KEY,
        ContactName    NVARCHAR(200)  NOT NULL,
        ContactPhone   NVARCHAR(30)   NOT NULL,
        ContactEmail   NVARCHAR(256)  NOT NULL CONSTRAINT DF_CR_Email DEFAULT N'',
        [Location]     NVARCHAR(300)  NOT NULL,
        LocationType   NVARCHAR(20)   NOT NULL CONSTRAINT DF_CR_LocType DEFAULT N'',   -- cinema / restaurant / custom
        EventType      NVARCHAR(100)  NOT NULL CONSTRAINT DF_CR_Event DEFAULT N'',
        MovieId        INT            NULL,
        UserId         INT            NULL,
        DesiredDate    DATETIME2      NOT NULL,
        GuestsCount    INT            NOT NULL,
        Comment        NVARCHAR(500)  NULL,
        EstimatedPrice DECIMAL(18,2)  NOT NULL CONSTRAINT DF_CR_Price DEFAULT 0,
        [Status]       INT            NOT NULL CONSTRAINT DF_CR_Status DEFAULT 0,      -- 0 New, 1 UnderReview, 2 Confirmed, 3 Rejected
        CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_CR_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CR_Movies FOREIGN KEY (MovieId) REFERENCES dbo.Movies (Id) ON DELETE SET NULL,
        CONSTRAINT FK_CR_Users  FOREIGN KEY (UserId)  REFERENCES dbo.Users (Id)  ON DELETE SET NULL,
        CONSTRAINT CK_CR_Guests CHECK (GuestsCount BETWEEN 1 AND 500),
        CONSTRAINT CK_CR_Status CHECK ([Status] IN (0, 1, 2, 3))
    );
END;
GO

PRINT N'Схему бази PokazTiney створено.';