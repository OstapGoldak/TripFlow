using Microsoft.Data.SqlClient;
using TripFlow.DAL;
using TripFlow.Models;

namespace TripFlow.Tests.Integration;

/// <summary>
/// Інтеграційні тести DAL: справжні репозиторії, справжні SQL-запити й справжня база LocalDB.
/// Модульні тести перевіряють сервіси із заглушками; ці тести перевіряють те, чого заглушки не бачать:
/// SQL-запити, ручний мапінг рядків у сутності, сортування в базі, CHECK-обмеження та каскадне видалення.
/// Потребують встановленого SQL Server LocalDB (Windows).
/// Запуск лише цих тестів: dotnet test --filter Category=Integration
/// </summary>
[Collection(LocalDbCollection.Name)]
[Trait("Category", "Integration")]
public class RepositoryIntegrationTests
{
    private readonly LocalDbFixture _db;
    private readonly TripRepository _trips;
    private readonly ActivityRepository _activities;
    private readonly ExpenseRepository _expenses;
    private readonly PackingRepository _packing;

    public RepositoryIntegrationTests(LocalDbFixture db)
    {
        _db = db;
        _trips = new TripRepository(db.Factory);
        _activities = new ActivityRepository(db.Factory);
        _expenses = new ExpenseRepository(db.Factory);
        _packing = new PackingRepository(db.Factory);
    }

    // База спільна для всіх тестів, тому кожен тест створює власну поїздку: 1–3 грудня 2026
    private Task<int> AddTripAsync(string name = "Львів") => _trips.AddAsync(new Trip
    {
        Name = name,
        Destination = "Україна",
        StartDate = new DateOnly(2026, 12, 1),
        EndDate = new DateOnly(2026, 12, 3),
        Budget = 1500.50m
    });

    private Task<int> AddActivityAsync(int tripId, int day, int hour, int minute, string title) =>
        _activities.AddAsync(new Activity
        {
            TripId = tripId,
            Date = new DateOnly(2026, 12, day),
            Time = new TimeOnly(hour, minute),
            Title = title
        });

    private async Task<int> CountAsync(string table, int tripId)
    {
        // Назва таблиці береться лише з коду тестів, значення — параметром
        await using var connection = await _db.Factory.CreateOpenConnectionAsync();
        await using var command = new SqlCommand($"SELECT COUNT(*) FROM dbo.{table} WHERE TripId = @TripId;", connection);
        command.Parameters.AddWithValue("@TripId", tripId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    // ---------- Схема ----------

    [Fact]
    public async Task DatabaseInitializer_RunTwice_DoesNotFailAndKeepsAllTables()
    {
        // Застосунок виконує Schema.sql при кожному запуску, тож повторний запуск не повинен падати
        await new DatabaseInitializer(_db.Factory).InitializeAsync();

        await using var connection = await _db.Factory.CreateOpenConnectionAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM sys.tables WHERE name IN (N'Trips', N'Activities', N'Expenses', N'PackingItems');",
            connection);

        Assert.Equal(4, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    // ---------- Поїздки ----------

    [Fact]
    public async Task Trip_AddThenRead_RoundTripsAllFields()
    {
        var id = await AddTripAsync("Відпустка в Карпатах");

        var trip = (await _trips.GetAllAsync()).Single(t => t.Id == id);

        Assert.Equal("Відпустка в Карпатах", trip.Name);
        Assert.Equal("Україна", trip.Destination);
        Assert.Equal(new DateOnly(2026, 12, 1), trip.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 3), trip.EndDate);
        Assert.Equal(1500.50m, trip.Budget);
    }

    [Fact]
    public async Task Trip_NameWithSqlCharacters_IsStoredAsPlainText()
    {
        // Параметризовані запити: текст не виконується як SQL
        const string name = "O'Brien'); DROP TABLE dbo.Trips; --";

        var id = await AddTripAsync(name);

        var trip = (await _trips.GetAllAsync()).Single(t => t.Id == id);
        Assert.Equal(name, trip.Name);
    }

    [Fact]
    public async Task DeleteTrip_CascadesToActivitiesExpensesAndPackingItems()
    {
        var tripId = await AddTripAsync();
        await AddActivityAsync(tripId, 1, 10, 0, "Екскурсія");
        await _expenses.AddAsync(new Expense
        {
            TripId = tripId, Date = new DateOnly(2026, 12, 1), Amount = 300m, Category = ExpenseCategory.Food
        });
        await _packing.AddAsync(new PackingItem { TripId = tripId, Name = "Паспорт", Category = "Documents" });

        await _trips.DeleteAsync(tripId);

        Assert.Equal(0, await CountAsync("Activities", tripId));
        Assert.Equal(0, await CountAsync("Expenses", tripId));
        Assert.Equal(0, await CountAsync("PackingItems", tripId));
    }

    // ---------- Розклад ----------

    [Fact]
    public async Task Activities_GetByDay_ReturnsOnlyThatDaySortedByTime()
    {
        var tripId = await AddTripAsync();
        // Додаємо не за порядком і ще одну подію на інший день
        await AddActivityAsync(tripId, 2, 13, 0, "Обід");
        await AddActivityAsync(tripId, 2, 9, 0, "Сніданок");
        await AddActivityAsync(tripId, 2, 11, 30, "Музей");
        await AddActivityAsync(tripId, 3, 8, 0, "Інший день");

        var day = await _activities.GetByDayAsync(tripId, new DateOnly(2026, 12, 2));

        Assert.Equal(new[] { "Сніданок", "Музей", "Обід" }, day.Select(a => a.Title));
    }

    [Fact]
    public async Task Activity_OptionalFieldsAndUpdate_RoundTrip()
    {
        var tripId = await AddTripAsync();
        var id = await AddActivityAsync(tripId, 1, 10, 0, "Похід");

        // Порожні необов'язкові поля зберігаються як NULL і повертаються як null
        var created = await _activities.GetByIdAsync(id);
        Assert.NotNull(created);
        Assert.Null(created!.Location);
        Assert.Null(created.EstimatedCost);
        Assert.Null(created.Note);

        created.Title = "Похід на Говерлу";
        created.Time = new TimeOnly(7, 45);
        created.Location = "Говерла";
        created.EstimatedCost = 250.75m;
        created.Note = "Взяти воду";
        await _activities.UpdateAsync(created);

        var updated = await _activities.GetByIdAsync(id);
        Assert.Equal("Похід на Говерлу", updated!.Title);
        Assert.Equal(new TimeOnly(7, 45), updated.Time);
        Assert.Equal("Говерла", updated.Location);
        Assert.Equal(250.75m, updated.EstimatedCost);
        Assert.Equal("Взяти воду", updated.Note);
    }

    [Fact]
    public async Task Activity_Delete_RemovesIt()
    {
        var tripId = await AddTripAsync();
        var id = await AddActivityAsync(tripId, 1, 10, 0, "Видалити мене");

        await _activities.DeleteAsync(id);

        Assert.Null(await _activities.GetByIdAsync(id));
    }

    [Fact]
    public async Task Activities_GetDateRange_IsNullWhenEmptyAndMinMaxOtherwise()
    {
        var tripId = await AddTripAsync();
        Assert.Null(await _activities.GetDateRangeAsync(tripId));

        await AddActivityAsync(tripId, 3, 9, 0, "Останній день");
        await AddActivityAsync(tripId, 1, 9, 0, "Перший день");

        var range = await _activities.GetDateRangeAsync(tripId);

        Assert.NotNull(range);
        Assert.Equal(new DateOnly(2026, 12, 1), range!.Value.First);
        Assert.Equal(new DateOnly(2026, 12, 3), range.Value.Last);
    }

    // ---------- Бюджет і чек-лист ----------

    [Fact]
    public async Task Expense_ZeroAmount_IsRejectedByDatabaseCheck()
    {
        // Другий рубіж захисту: навіть якщо сервіс пропустить нуль, CHECK (Amount > 0) у базі не дасть зберегти
        var tripId = await AddTripAsync();

        await Assert.ThrowsAsync<SqlException>(() => _expenses.AddAsync(new Expense
        {
            TripId = tripId, Date = new DateOnly(2026, 12, 1), Amount = 0m, Category = ExpenseCategory.Food
        }));
        Assert.Equal(0, await CountAsync("Expenses", tripId));
    }

    [Fact]
    public async Task PackingItem_SetPacked_IsPersisted()
    {
        var tripId = await AddTripAsync();
        var id = await _packing.AddAsync(new PackingItem { TripId = tripId, Name = "Зарядка", Category = "Electronics" });

        await _packing.SetPackedAsync(id, true);

        var item = (await _packing.GetByTripAsync(tripId)).Single(i => i.Id == id);
        Assert.True(item.IsPacked);
        Assert.Equal("Electronics", item.Category);
    }
}
