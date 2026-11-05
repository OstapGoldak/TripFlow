-- TripFlow: модель даних (T-SQL, SQL Server LocalDB).
-- Скрипт ідемпотентний: кожна таблиця створюється, лише якщо її ще немає.

IF OBJECT_ID(N'dbo.Trips', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Trips
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Trips PRIMARY KEY,
        Name        NVARCHAR(255)     NOT NULL,
        Destination NVARCHAR(255)     NOT NULL,
        StartDate   DATE              NOT NULL,
        EndDate     DATE              NOT NULL,
        Budget      DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Trips_Budget DEFAULT (0),
        CONSTRAINT CK_Trips_Budget CHECK (Budget >= 0),
        CONSTRAINT CK_Trips_Dates  CHECK (EndDate >= StartDate)
    );
END
GO

IF OBJECT_ID(N'dbo.Activities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Activities
    (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Activities PRIMARY KEY,
        TripId        INT               NOT NULL,
        [Date]        DATE              NOT NULL,
        [Time]        TIME(0)           NOT NULL,
        Title         NVARCHAR(255)     NOT NULL,
        Location      NVARCHAR(255)     NULL,
        EstimatedCost DECIMAL(18,2)     NULL,
        Note          NVARCHAR(1000)    NULL,
        CONSTRAINT FK_Activities_Trips FOREIGN KEY (TripId)
            REFERENCES dbo.Trips (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Activities_TripId_Date_Time
        ON dbo.Activities (TripId, [Date], [Time]);
END
GO

IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Expenses PRIMARY KEY,
        TripId      INT               NOT NULL,
        [Date]      DATE              NOT NULL,
        Amount      DECIMAL(18,2)     NOT NULL,
        Category    NVARCHAR(50)      NOT NULL,
        Description NVARCHAR(500)     NULL,
        CONSTRAINT FK_Expenses_Trips FOREIGN KEY (TripId)
            REFERENCES dbo.Trips (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Expenses_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Expenses_Category
            CHECK (Category IN (N'Housing', N'Transport', N'Food', N'Other'))
    );

    CREATE INDEX IX_Expenses_TripId ON dbo.Expenses (TripId);
END
GO

IF OBJECT_ID(N'dbo.PackingItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackingItems
    (
        Id       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PackingItems PRIMARY KEY,
        TripId   INT               NOT NULL,
        Name     NVARCHAR(255)     NOT NULL,
        Category NVARCHAR(50)      NOT NULL,
        IsPacked BIT               NOT NULL CONSTRAINT DF_PackingItems_IsPacked DEFAULT (0),
        CONSTRAINT FK_PackingItems_Trips FOREIGN KEY (TripId)
            REFERENCES dbo.Trips (Id) ON DELETE CASCADE,
        CONSTRAINT CK_PackingItems_Category
            CHECK (Category IN (N'Documents', N'Clothing', N'Electronics', N'FirstAid', N'Other'))
    );

    CREATE INDEX IX_PackingItems_TripId ON dbo.PackingItems (TripId);
END
GO
