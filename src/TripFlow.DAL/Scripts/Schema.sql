-- TripFlow: схема бази LocalDB (T-SQL).
-- Walking Skeleton: лише таблиця Trips. Activities, Expenses і PackingItems додаються пізніше.
-- Скрипт можна виконувати при кожному запуску: таблиця створюється, лише якщо її ще немає.

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
            REFERENCES dbo.Trips (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Activities_EstimatedCost CHECK (EstimatedCost IS NULL OR EstimatedCost >= 0)
    );

    CREATE INDEX IX_Activities_TripId_Date_Time
        ON dbo.Activities (TripId, [Date], [Time]);
END