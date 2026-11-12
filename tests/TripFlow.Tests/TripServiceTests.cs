using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>
/// Модульні тести TripService: валідація поїздки та статус за датами.
/// Репозиторій замінено заглушкою (Moq), тому база даних не потрібна.
/// </summary>
public class TripServiceTests
{
    private readonly Mock<ITripRepository> _repository = new();
    private readonly TripService _service;

    public TripServiceTests()
    {
        _service = new TripService(_repository.Object);
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
