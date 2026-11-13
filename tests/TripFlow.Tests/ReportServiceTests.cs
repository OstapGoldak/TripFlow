using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>Модульні тести ReportService: дані PDF-звіту збираються з сервісів, база не потрібна.</summary>
public class ReportServiceTests
{
    private readonly Mock<IItineraryService> _itinerary = new();
    private readonly Mock<IBudgetService> _budget = new();
    private readonly Mock<IPackingService> _packing = new();
    private readonly ReportService _service;

    private static readonly DateOnly Day1 = new(2026, 7, 15);
    private static readonly DateOnly Day2 = new(2026, 7, 16);

    private readonly Trip _trip = new()
    {
        Id = 3,
        Name = "Карпати",
        Destination = "Яремче",
        StartDate = Day1,
        EndDate = Day2,
        Budget = 15000m
    };

    public ReportServiceTests()
    {
        _service = new ReportService(_itinerary.Object, _budget.Object, _packing.Object);

        _itinerary.Setup(i => i.GetDays(_trip)).Returns(new[] { Day1, Day2 });
        _itinerary.Setup(i => i.GetActivitiesAsync(3, Day1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { new Activity { Title = "Говерла", Date = Day1, Time = new TimeOnly(9, 0) } });
        _itinerary.Setup(i => i.GetActivitiesAsync(3, Day2, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Array.Empty<Activity>());

        var expenses = new[] { new Expense { Amount = 12000m, Category = ExpenseCategory.Housing } };
        _budget.Setup(b => b.GetOverviewAsync(_trip, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new BudgetOverview(expenses, BudgetService.Calculate(15000m, expenses)));
    }

    private void SetupPacking(params PackingItem[] items) =>
        _packing.Setup(p => p.GetByTripAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(items);

    [Fact]
    public async Task Build_ContainsEveryTripDayNumberedInOrder()
    {
        SetupPacking();

        var report = await _service.BuildAsync(_trip);

        Assert.Equal(2, report.Days.Count);
        Assert.Equal((1, Day1), (report.Days[0].Number, report.Days[0].Date));
        Assert.Equal((2, Day2), (report.Days[1].Number, report.Days[1].Date));
        Assert.Single(report.Days[0].Activities);
        Assert.Empty(report.Days[1].Activities);
    }

    [Fact]
    public async Task Build_UsesBudgetSummaryFromBudgetService()
    {
        SetupPacking();

        var report = await _service.BuildAsync(_trip);

        Assert.Equal(12000m, report.Budget.Summary.Spent);
        Assert.Equal(3000m, report.Budget.Summary.Remaining);
        Assert.Equal(BudgetLevel.Warning, report.Budget.Summary.Level);
    }

    [Fact]
    public async Task Build_GroupsPackingInCategoryOrderAndSkipsEmptyGroups()
    {
        SetupPacking(
            new PackingItem { Name = "Дощовик", Category = "Clothing", IsPacked = true },
            new PackingItem { Name = "Паспорт", Category = "Documents", IsPacked = true },
            new PackingItem { Name = "Квитки", Category = "Documents", IsPacked = false });

        var report = await _service.BuildAsync(_trip);

        Assert.Equal(new[] { "Документи", "Одяг" }, report.PackingGroups.Select(g => g.Title));
        Assert.Equal(2, report.PackingGroups[0].Items.Count);
        Assert.Equal(2, report.Packing.Packed);
        Assert.Equal(3, report.Packing.Total);
    }

    [Fact]
    public async Task Build_NoPackingItems_ZeroProgressAndNoGroups()
    {
        SetupPacking();

        var report = await _service.BuildAsync(_trip);

        Assert.Empty(report.PackingGroups);
        Assert.Equal(0, report.Packing.Percent);
    }

    [Fact]
    public async Task Build_NullTrip_Throws() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.BuildAsync(null!));
}
