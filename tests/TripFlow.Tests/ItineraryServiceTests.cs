using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>
/// Модульні тести ItineraryService. Репозиторій замінено заглушкою (Moq),
/// тому тести працюють без бази даних.
/// </summary>
public class ItineraryServiceTests
{
    private readonly Mock<IActivityRepository> _repository = new();
    private readonly ItineraryService _service;

    // Поїздка: 1–3 грудня 2026
    private readonly Trip _trip = new()
    {
        Id = 7,
        Name = "Львів",
        Destination = "Львів, Україна",
        StartDate = new DateOnly(2026, 12, 1),
        EndDate = new DateOnly(2026, 12, 3),
        Budget = 3000m
    };

    public ItineraryServiceTests()
    {
        _service = new ItineraryService(_repository.Object);
    }

    private Activity ValidActivity() => new()
    {
        Date = new DateOnly(2026, 12, 2),
        Time = new TimeOnly(10, 30),
        Title = "Екскурсія центром",
        Location = "Площа Ринок",
        EstimatedCost = 200m,
        Note = "Зустріч біля ратуші"
    };

    // ---------- Генерація днів ----------

    [Fact]
    public void GetDays_ThreeDayTrip_ReturnsAllDaysInOrder()
    {
        var days = _service.GetDays(_trip);

        Assert.Equal(
            new[] { new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2), new DateOnly(2026, 12, 3) },
            days);
    }

    [Fact]
    public void GetDays_OneDayTrip_ReturnsSingleDay()
    {
        var trip = new Trip { StartDate = new DateOnly(2026, 12, 1), EndDate = new DateOnly(2026, 12, 1) };

        var days = _service.GetDays(trip);

        Assert.Single(days);
    }

    // ---------- Додавання: успішні випадки ----------

    [Fact]
    public async Task AddAsync_ValidActivity_SavesWithTripIdAndReturnsId()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(15);

        var result = await _service.AddAsync(_trip, ValidActivity());

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value);
        _repository.Verify(r => r.AddAsync(
            It.Is<Activity>(a => a.TripId == _trip.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1)] // перший день поїздки
    [InlineData(3)] // останній день поїздки
    public async Task AddAsync_DateOnTripBoundary_IsAllowed(int day)
    {
        var activity = ValidActivity();
        activity.Date = new DateOnly(2026, 12, day);

        var result = await _service.AddAsync(_trip, activity);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddAsync_NoCostAndZeroCost_AreAllowed()
    {
        var withoutCost = ValidActivity();
        withoutCost.EstimatedCost = null;
        var zeroCost = ValidActivity();
        zeroCost.EstimatedCost = 0m;

        Assert.True((await _service.AddAsync(_trip, withoutCost)).IsSuccess);
        Assert.True((await _service.AddAsync(_trip, zeroCost)).IsSuccess);
    }

    [Fact]
    public async Task AddAsync_TrimsTitleAndStoresEmptyOptionalFieldsAsNull()
    {
        var activity = ValidActivity();
        activity.Title = "  Музей  ";
        activity.Location = "   ";
        activity.Note = "";

        await _service.AddAsync(_trip, activity);

        _repository.Verify(r => r.AddAsync(
            It.Is<Activity>(a => a.Title == "Музей" && a.Location == null && a.Note == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Додавання: помилки валідації ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_EmptyTitle_ReturnsFailureAndDoesNotSave(string title)
    {
        var activity = ValidActivity();
        activity.Title = title;

        var result = await _service.AddAsync(_trip, activity);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData(11, 30)] // день до початку поїздки
    [InlineData(12, 4)]  // день після завершення поїздки
    public async Task AddAsync_DateOutsideTrip_ReturnsFailureAndDoesNotSave(int month, int day)
    {
        var activity = ValidActivity();
        activity.Date = new DateOnly(2026, month, day);

        var result = await _service.AddAsync(_trip, activity);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NegativeCost_ReturnsFailureAndDoesNotSave()
    {
        var activity = ValidActivity();
        activity.EstimatedCost = -0.01m;

        var result = await _service.AddAsync(_trip, activity);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    // ---------- Редагування ----------

    [Fact]
    public async Task UpdateAsync_ValidActivity_Saves()
    {
        var activity = ValidActivity();
        activity.Id = 3;

        var result = await _service.UpdateAsync(_trip, activity);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.UpdateAsync(activity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidActivity_ReturnsFailureAndDoesNotSave()
    {
        var activity = ValidActivity();
        activity.Title = "";

        var result = await _service.UpdateAsync(_trip, activity);

        Assert.False(result.IsSuccess);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyNothingSaved() =>
        _repository.Verify(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Never);
}