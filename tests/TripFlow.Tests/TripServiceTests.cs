using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>
/// Модульні тести TripService: валідація поїздки, редагування та статус за датами.
/// Репозиторій замінено заглушкою (Moq), тому база даних не потрібна.
/// </summary>
public class TripServiceTests
{
    private readonly Mock<ITripRepository> _repository = new();
    private readonly Mock<IActivityRepository> _activities = new();
    private readonly TripService _service;

    public TripServiceTests()
    {
        _service = new TripService(_repository.Object, _activities.Object);
    }

    // Поїздка: 1–5 грудня 2026
    private static Trip ValidTrip() => new()
    {
        Name = "Карпати",
        Destination = "Яремче, Україна",
        StartDate = new DateOnly(2026, 12, 1),
        EndDate = new DateOnly(2026, 12, 5),
        Budget = 5000m
    };

    private void VerifyNothingSaved() =>
        _repository.Verify(r => r.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Never);

    // ---------- Створення: успішні випадки ----------

    [Fact]
    public async Task AddAsync_ValidTrip_SavesAndReturnsId()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(42);

        var result = await _service.AddAsync(ValidTrip());

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        _repository.Verify(r => r.AddAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_TrimsNameAndDestination()
    {
        var trip = ValidTrip();
        trip.Name = "  Карпати  ";
        trip.Destination = "  Яремче  ";

        await _service.AddAsync(trip);

        _repository.Verify(r => r.AddAsync(
            It.Is<Trip>(t => t.Name == "Карпати" && t.Destination == "Яремче"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Крайові випадки, які мають проходити ----------

    [Fact]
    public void Validate_ZeroBudget_IsAllowed()
    {
        var trip = ValidTrip();
        trip.Budget = 0m;

        Assert.True(_service.Validate(trip).IsSuccess);
    }

    [Fact]
    public void Validate_OneDayTrip_IsAllowed()
    {
        var trip = ValidTrip();
        trip.EndDate = trip.StartDate;

        Assert.True(_service.Validate(trip).IsSuccess);
    }

    [Fact]
    public void Validate_NameOfMaxLength_IsAllowed()
    {
        var trip = ValidTrip();
        trip.Name = new string('а', TripService.MaxTextLength);

        Assert.True(_service.Validate(trip).IsSuccess);
    }

    // ---------- Помилки валідації: нічого не зберігається ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_EmptyName_ReturnsFailureAndDoesNotSave(string name)
    {
        var trip = ValidTrip();
        trip.Name = name;

        var result = await _service.AddAsync(trip);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_EmptyDestination_ReturnsFailureAndDoesNotSave(string destination)
    {
        var trip = ValidTrip();
        trip.Destination = destination;

        var result = await _service.AddAsync(trip);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NameLongerThanMax_ReturnsFailureAndDoesNotSave()
    {
        var trip = ValidTrip();
        trip.Name = new string('а', TripService.MaxTextLength + 1);

        var result = await _service.AddAsync(trip);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_EndDateBeforeStartDate_ReturnsFailureAndDoesNotSave()
    {
        var trip = ValidTrip();
        trip.EndDate = trip.StartDate.AddDays(-1);

        var result = await _service.AddAsync(trip);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NegativeBudget_ReturnsFailureAndDoesNotSave()
    {
        var trip = ValidTrip();
        trip.Budget = -0.01m;

        var result = await _service.AddAsync(trip);

        Assert.False(result.IsSuccess);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task AddAsync_NullTrip_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddAsync(null!));
    }

    // ---------- Редагування ----------

    private static Trip SavedTrip()
    {
        var trip = ValidTrip();
        trip.Id = 7;
        return trip;
    }

    /// <summary>У поїздки є події з <paramref name="first"/> по <paramref name="last"/> грудня 2026.</summary>
    private void SetupSchedule(int first, int last) =>
        _activities.Setup(a => a.GetDateRangeAsync(7, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((new DateOnly(2026, 12, first), new DateOnly(2026, 12, last)));

    private void VerifyNothingUpdated() =>
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Trip>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task UpdateAsync_ValidTrip_SavesTrimmedTrip()
    {
        var trip = SavedTrip();
        trip.Name = "  Карпати взимку  ";
        trip.Budget = 8000m;

        var result = await _service.UpdateAsync(trip);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.UpdateAsync(
            It.Is<Trip>(t => t.Id == 7 && t.Name == "Карпати взимку" && t.Budget == 8000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidTrip_ReturnsFailureAndDoesNotSave()
    {
        var trip = SavedTrip();
        trip.Name = "   ";

        var result = await _service.UpdateAsync(trip);

        Assert.False(result.IsSuccess);
        VerifyNothingUpdated();
    }

    [Fact]
    public async Task UpdateAsync_NoActivities_AllowsAnyValidDates()
    {
        // Подій немає: репозиторій повертає null, дати можна міняти як завгодно
        var trip = SavedTrip();
        trip.StartDate = new DateOnly(2027, 3, 10);
        trip.EndDate = new DateOnly(2027, 3, 12);

        var result = await _service.UpdateAsync(trip);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_NewDatesExactlyCoverActivities_IsAllowed()
    {
        // Події 2–4 грудня, поїздку скорочено рівно до цих днів
        SetupSchedule(2, 4);
        var trip = SavedTrip();
        trip.StartDate = new DateOnly(2026, 12, 2);
        trip.EndDate = new DateOnly(2026, 12, 4);

        var result = await _service.UpdateAsync(trip);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(3, 5)] // перший день з подією (2 грудня) лишився б до початку поїздки
    [InlineData(1, 3)] // останній день з подією (4 грудня) лишився б після завершення
    public async Task UpdateAsync_ActivitiesOutsideNewDates_ReturnsFailureAndDoesNotSave(int startDay, int endDay)
    {
        SetupSchedule(2, 4);
        var trip = SavedTrip();
        trip.StartDate = new DateOnly(2026, 12, startDay);
        trip.EndDate = new DateOnly(2026, 12, endDay);

        var result = await _service.UpdateAsync(trip);

        Assert.False(result.IsSuccess);
        Assert.Contains("02.12.2026", result.ErrorMessage);
        VerifyNothingUpdated();
    }

    [Fact]
    public async Task UpdateAsync_TripWithoutId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.UpdateAsync(ValidTrip()));
    }

    // ---------- Статус за датами (поїздка 1–5 грудня 2026) ----------

    [Theory]
    [InlineData(2026, 11, 30, TripStatus.Planned)]    // день до початку
    [InlineData(2026, 12, 1, TripStatus.InProgress)]  // перший день
    [InlineData(2026, 12, 3, TripStatus.InProgress)]  // середина
    [InlineData(2026, 12, 5, TripStatus.InProgress)]  // останній день
    [InlineData(2026, 12, 6, TripStatus.Completed)]   // день після завершення
    public void GetStatus_ReturnsStatusByDates(int year, int month, int day, TripStatus expected)
    {
        var status = _service.GetStatus(ValidTrip(), new DateOnly(year, month, day));

        Assert.Equal(expected, status);
    }
}
