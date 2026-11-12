using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>
/// Модульні тести DataTransferService (експорт і імпорт JSON).
/// Усі залежності замінено заглушками (Moq), тому база даних не потрібна.
/// </summary>
public class DataTransferServiceTests
{
    private readonly Mock<IItineraryService> _itinerary = new();
    private readonly Mock<IBudgetService> _budget = new();
    private readonly Mock<IPackingService> _packing = new();
    private readonly Mock<ITripImportRepository> _importRepository = new();
    private readonly DataTransferService _service;

    private readonly Trip _trip = new()
    {
        Id = 7,
        Name = "Карпати",
        Destination = "Буковель",
        StartDate = new DateOnly(2026, 12, 1),
        EndDate = new DateOnly(2026, 12, 3),
        Budget = 15000m
    };

    public DataTransferServiceTests()
    {
        _service = new DataTransferService(
            _itinerary.Object, _budget.Object, _packing.Object, _importRepository.Object);

        _importRepository
            .Setup(r => r.ImportTripAsync(
                It.IsAny<Trip>(),
                It.IsAny<IReadOnlyList<Activity>>(),
                It.IsAny<IReadOnlyList<Expense>>(),
                It.IsAny<IReadOnlyList<PackingItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
    }

    /// <summary>Поїздка з однією подією на другий день, двома витратами та двома речами.</summary>
    private void SetupTripData()
    {
        var days = new[] { new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2), new DateOnly(2026, 12, 3) };
        _itinerary.Setup(s => s.GetDays(_trip)).Returns(days);
        _itinerary.Setup(s => s.GetActivitiesAsync(_trip.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Activity>());
        _itinerary.Setup(s => s.GetActivitiesAsync(_trip.Id, days[1], It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Activity>
                  {
                      new()
                      {
                          Id = 11, TripId = 7, Date = days[1], Time = new TimeOnly(9, 30),
                          Title = "Підйом на гору", Location = "Говерла", EstimatedCost = 250m, Note = "Взяти воду"
                      }
                  });

        var expenses = new List<Expense>
        {
            new() { Id = 2, TripId = 7, Date = new DateOnly(2026, 12, 2), Amount = 620m, Category = ExpenseCategory.Food, Description = "Обід" },
            new() { Id = 1, TripId = 7, Date = new DateOnly(2026, 11, 28), Amount = 3000m, Category = ExpenseCategory.Housing, Description = null }
        };
        _budget.Setup(s => s.GetOverviewAsync(_trip, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new BudgetOverview(expenses, BudgetService.Calculate(_trip.Budget, expenses)));

        _packing.Setup(s => s.GetByTripAsync(_trip.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PackingItem>
                {
                    new() { Id = 5, TripId = 7, Name = "Паспорт", Category = "Documents", IsPacked = true },
                    new() { Id = 6, TripId = 7, Name = "Пластир", Category = "FirstAid", IsPacked = false }
                });
    }

    // ---------- Експорт ----------

    [Fact]
    public async Task Export_ContainsTripActivitiesExpensesAndPackingItems()
    {
        SetupTripData();

        var json = await _service.ExportAsync(_trip);

        Assert.Contains("\"Name\": \"Карпати\"", json);
        Assert.Contains("Підйом на гору", json);
        Assert.Contains("Говерла", json);
        Assert.Contains("\"Category\": \"Food\"", json);      // категорія назвою, а не числом
        Assert.Contains("\"Category\": \"Housing\"", json);
        Assert.Contains("Пластир", json);
        Assert.Contains("\"IsPacked\": true", json);
    }

    [Fact]
    public async Task Export_WritesReadableCyrillicAndNoIds()
    {
        SetupTripData();

        var json = await _service.ExportAsync(_trip);

        Assert.DoesNotContain("\\u04", json);   // кирилиця не екранується
        Assert.DoesNotContain("\"Id\"", json);  // Id не потрапляють у файл
        Assert.DoesNotContain("\"TripId\"", json);
    }

    [Fact]
    public async Task Export_TripWithoutData_ProducesFileWithEmptyLists()
    {
        _itinerary.Setup(s => s.GetDays(_trip)).Returns(new[] { new DateOnly(2026, 12, 1) });
        _itinerary.Setup(s => s.GetActivitiesAsync(_trip.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Activity>());
        _budget.Setup(s => s.GetOverviewAsync(_trip, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new BudgetOverview(new List<Expense>(), new BudgetSummary(_trip.Budget, 0m)));
        _packing.Setup(s => s.GetByTripAsync(_trip.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PackingItem>());

        var json = await _service.ExportAsync(_trip);

        // Такий файл має імпортуватися без помилок
        var result = await _service.ImportAsync(json);
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    // ---------- Експорт → імпорт ----------

    [Fact]
    public async Task ExportThenImport_RestoresAllData()
    {
        SetupTripData();
        var json = await _service.ExportAsync(_trip);

        Trip? savedTrip = null;
        IReadOnlyList<Activity>? savedActivities = null;
        IReadOnlyList<Expense>? savedExpenses = null;
        IReadOnlyList<PackingItem>? savedItems = null;
        _importRepository
            .Setup(r => r.ImportTripAsync(
                It.IsAny<Trip>(),
                It.IsAny<IReadOnlyList<Activity>>(),
                It.IsAny<IReadOnlyList<Expense>>(),
                It.IsAny<IReadOnlyList<PackingItem>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Trip, IReadOnlyList<Activity>, IReadOnlyList<Expense>, IReadOnlyList<PackingItem>, CancellationToken>(
                (t, a, e, p, _) => { savedTrip = t; savedActivities = a; savedExpenses = e; savedItems = p; })
            .ReturnsAsync(42);

        var result = await _service.ImportAsync(json);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(42, result.Value);

        Assert.NotNull(savedTrip);
        Assert.Equal("Карпати", savedTrip!.Name);
        Assert.Equal("Буковель", savedTrip.Destination);
        Assert.Equal(new DateOnly(2026, 12, 1), savedTrip.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 3), savedTrip.EndDate);
        Assert.Equal(15000m, savedTrip.Budget);

        var activity = Assert.Single(savedActivities!);
        Assert.Equal("Підйом на гору", activity.Title);
        Assert.Equal(new TimeOnly(9, 30), activity.Time);
        Assert.Equal(250m, activity.EstimatedCost);

        Assert.Equal(2, savedExpenses!.Count);
        Assert.Equal(ExpenseCategory.Housing, savedExpenses[0].Category); // експорт упорядковує за датою
        Assert.Null(savedExpenses[0].Description);

        Assert.Equal(2, savedItems!.Count);
        Assert.True(savedItems[0].IsPacked);
        Assert.Equal("FirstAid", savedItems[1].Category);
    }

    // ---------- Імпорт: некоректні файли ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("це не json")]
    [InlineData("{ \"Trip\": ")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task Import_EmptyOrBrokenFile_ReturnsErrorAndDoesNotSave(string json)
    {
        var result = await _service.ImportAsync(json);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Import_UnsupportedFormatVersion_ReturnsError()
    {
        var result = await _service.ImportAsync(Json(version: 99));

        Assert.False(result.IsSuccess);
        Assert.Contains("версі", result.ErrorMessage);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Import_MissingTrip_ReturnsError()
    {
        var result = await _service.ImportAsync("{ \"FormatVersion\": 1 }");

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData("""{ "Name": "", "Destination": "Рим", "StartDate": "2026-12-01", "EndDate": "2026-12-03", "Budget": 100 }""")]
    [InlineData("""{ "Name": "Рим", "Destination": "  ", "StartDate": "2026-12-01", "EndDate": "2026-12-03", "Budget": 100 }""")]
    [InlineData("""{ "Name": "Рим", "Destination": "Рим", "StartDate": "2026-12-05", "EndDate": "2026-12-03", "Budget": 100 }""")]
    [InlineData("""{ "Name": "Рим", "Destination": "Рим", "StartDate": "2026-12-01", "EndDate": "2026-12-03", "Budget": -1 }""")]
    [InlineData("""{ "Name": "Рим", "Destination": "Рим", "Budget": 100 }""")]
    public async Task Import_InvalidTrip_ReturnsErrorAndDoesNotSave(string trip)
    {
        var result = await _service.ImportAsync(Json(trip: trip));

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Theory]
    // дата поза межами поїздки
    [InlineData("""[{ "Date": "2026-12-10", "Time": "10:00:00", "Title": "Екскурсія" }]""")]
    // порожня назва
    [InlineData("""[{ "Date": "2026-12-02", "Time": "10:00:00", "Title": " " }]""")]
    // від'ємна вартість
    [InlineData("""[{ "Date": "2026-12-02", "Time": "10:00:00", "Title": "Екскурсія", "EstimatedCost": -5 }]""")]
    // порожній елемент масиву
    [InlineData("[null]")]
    public async Task Import_InvalidActivity_ReturnsErrorAndDoesNotSave(string activities)
    {
        var result = await _service.ImportAsync(Json(activities: activities));

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData("""[{ "Date": "2026-12-02", "Amount": 0, "Category": "Food" }]""")]
    [InlineData("""[{ "Date": "2026-12-02", "Amount": -10, "Category": "Food" }]""")]
    [InlineData("""[{ "Date": "2026-12-02", "Amount": 10, "Category": "Beer" }]""")]  // невідома категорія
    [InlineData("""[{ "Date": "2026-12-02", "Amount": 10, "Category": 2 }]""")]       // число замість назви
    [InlineData("""[{ "Amount": 10, "Category": "Food" }]""")]                        // немає дати
    public async Task Import_InvalidExpense_ReturnsErrorAndDoesNotSave(string expenses)
    {
        var result = await _service.ImportAsync(Json(expenses: expenses));

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData("""[{ "Name": "Паспорт", "Category": "Weapons", "IsPacked": false }]""")]
    [InlineData("""[{ "Name": "Паспорт", "IsPacked": false }]""")]
    [InlineData("""[{ "Name": "", "Category": "Documents", "IsPacked": false }]""")]
    public async Task Import_InvalidPackingItem_ReturnsErrorAndDoesNotSave(string items)
    {
        var result = await _service.ImportAsync(Json(packing: items));

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Import_ErrorMessageNamesTheBrokenRecord()
    {
        const string expenses = """
            [
              { "Date": "2026-12-02", "Amount": 10, "Category": "Food" },
              { "Date": "2026-12-02", "Amount": 0,  "Category": "Food" }
            ]
            """;

        var result = await _service.ImportAsync(Json(expenses: expenses));

        Assert.False(result.IsSuccess);
        Assert.Contains("№2", result.ErrorMessage);
    }

    // ---------- Імпорт: допустимі файли ----------

    [Fact]
    public async Task Import_TrimsTextAndTurnsBlankOptionalFieldsIntoNull()
    {
        const string trip = """{ "Name": "  Рим  ", "Destination": " Італія ", "StartDate": "2026-12-01", "EndDate": "2026-12-03", "Budget": 0 }""";
        const string activities = """[{ "Date": "2026-12-01", "Time": "08:00:00", "Title": "  Колізей ", "Location": "   ", "Note": "" }]""";

        IReadOnlyList<Activity>? saved = null;
        Trip? savedTrip = null;
        _importRepository
            .Setup(r => r.ImportTripAsync(
                It.IsAny<Trip>(),
                It.IsAny<IReadOnlyList<Activity>>(),
                It.IsAny<IReadOnlyList<Expense>>(),
                It.IsAny<IReadOnlyList<PackingItem>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Trip, IReadOnlyList<Activity>, IReadOnlyList<Expense>, IReadOnlyList<PackingItem>, CancellationToken>(
                (t, a, _, _, _) => { savedTrip = t; saved = a; })
            .ReturnsAsync(1);

        var result = await _service.ImportAsync(Json(trip: trip, activities: activities));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("Рим", savedTrip!.Name);
        Assert.Equal("Італія", savedTrip.Destination);
        var activity = Assert.Single(saved!);
        Assert.Equal("Колізей", activity.Title);
        Assert.Null(activity.Location);
        Assert.Null(activity.Note);
    }

    [Fact]
    public async Task Import_MissingLists_AreTreatedAsEmpty()
    {
        const string json = """
            { "FormatVersion": 1,
              "Trip": { "Name": "Рим", "Destination": "Італія", "StartDate": "2026-12-01", "EndDate": "2026-12-03", "Budget": 100 } }
            """;

        var result = await _service.ImportAsync(json);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Import_ExpenseBeforeTripStart_IsAllowed()
    {
        // Оплата можлива до виїзду: дата витрати не обмежена датами поїздки (ARCHITECTURE.md)
        const string expenses = """[{ "Date": "2026-10-01", "Amount": 3000, "Category": "Housing" }]""";

        var result = await _service.ImportAsync(Json(expenses: expenses));

        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    // ---------- Допоміжне ----------

    private const string DefaultTrip =
        """{ "Name": "Карпати", "Destination": "Буковель", "StartDate": "2026-12-01", "EndDate": "2026-12-03", "Budget": 15000 }""";

    /// <summary>Збирає JSON-файл: підставляє потрібну частину, решта залишається коректною.</summary>
    private static string Json(
        string trip = DefaultTrip,
        string activities = "[]",
        string expenses = "[]",
        string packing = "[]",
        int version = 1) =>
        $$"""
        {
          "FormatVersion": {{version}},
          "ExportedAt": "2026-11-12T20:00:00Z",
          "Trip": {{trip}},
          "Activities": {{activities}},
          "Expenses": {{expenses}},
          "PackingItems": {{packing}}
        }
        """;

    private void VerifyNothingSaved() =>
        _importRepository.Verify(r => r.ImportTripAsync(
            It.IsAny<Trip>(),
            It.IsAny<IReadOnlyList<Activity>>(),
            It.IsAny<IReadOnlyList<Expense>>(),
            It.IsAny<IReadOnlyList<PackingItem>>(),
            It.IsAny<CancellationToken>()), Times.Never);
}
