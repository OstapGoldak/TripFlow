using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Логіка поїздок: валідація, редагування, статус за датами, виклики репозиторію.
/// Про WPF і SQL сервіс нічого не знає.
/// </summary>
public class TripService : ITripService
{
    /// <summary>Максимальна довжина назви й розташування (NVARCHAR(255) у таблиці Trips).</summary>
    public const int MaxTextLength = 255;

    private readonly ITripRepository _repository;
    private readonly IActivityRepository _activities;

    public TripService(ITripRepository repository, IActivityRepository activities)
    {
        _repository = repository;
        _activities = activities;
    }

    public Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default) =>
        _repository.GetAllAsync(ct);

    public OperationResult Validate(Trip trip, (DateOnly First, DateOnly Last)? scheduleRange = null)
    {
        ArgumentNullException.ThrowIfNull(trip);

        if (string.IsNullOrWhiteSpace(trip.Name))
        {
            return OperationResult.Failure("Введіть назву подорожі.");
        }

        if (trip.Name.Trim().Length > MaxTextLength)
        {
            return OperationResult.Failure($"Назва подорожі задовга (максимум {MaxTextLength} символів).");
        }

        if (string.IsNullOrWhiteSpace(trip.Destination))
        {
            return OperationResult.Failure("Введіть розташування (місто або країну).");
        }

        if (trip.Destination.Trim().Length > MaxTextLength)
        {
            return OperationResult.Failure($"Розташування задовге (максимум {MaxTextLength} символів).");
        }

        if (trip.EndDate < trip.StartDate)
        {
            return OperationResult.Failure("Дата завершення не може бути раніше за дату початку.");
        }

        if (trip.Budget < 0)
        {
            return OperationResult.Failure("Бюджет не може бути від'ємним.");
        }

        // Під час редагування: дні з подіями мають лишитися всередині поїздки
        if (scheduleRange is { } range && (range.First < trip.StartDate || range.Last > trip.EndDate))
        {
            return OperationResult.Failure(
                $"У розкладі є події з {range.First:dd.MM.yyyy} по {range.Last:dd.MM.yyyy}. " +
                "Нові дати поїздки мають охоплювати ці дні, або спершу видаліть події поза новими датами.");
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult<int>> AddAsync(Trip trip, CancellationToken ct = default)
    {
        var validation = Validate(trip);
        if (!validation.IsSuccess)
        {
            return OperationResult<int>.Failure(validation.ErrorMessage!);
        }

        trip.Name = trip.Name.Trim();
        trip.Destination = trip.Destination.Trim();

        var id = await _repository.AddAsync(trip, ct);
        return OperationResult<int>.Success(id);
    }

    public Task<(DateOnly First, DateOnly Last)?> GetScheduleRangeAsync(int tripId, CancellationToken ct = default) =>
        _activities.GetDateRangeAsync(tripId, ct);

    public async Task<OperationResult> UpdateAsync(Trip trip, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(trip.Id);

        // Діапазон подій беремо з бази, а не з форми: так правило діє за будь-якого виклику
        var range = await _activities.GetDateRangeAsync(trip.Id, ct);
        var validation = Validate(trip, range);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        trip.Name = trip.Name.Trim();
        trip.Destination = trip.Destination.Trim();

        await _repository.UpdateAsync(trip, ct);
        return OperationResult.Success();
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        _repository.DeleteAsync(id, ct);

    public TripStatus GetStatus(Trip trip, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(trip);

        if (today < trip.StartDate)
        {
            return TripStatus.Planned;
        }

        return today > trip.EndDate ? TripStatus.Completed : TripStatus.InProgress;
    }
}
