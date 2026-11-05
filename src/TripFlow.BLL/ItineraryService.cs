using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Логіка розкладу: генерація днів за датами поїздки, валідація й збереження активностей.
/// Дні не зберігаються в базі, а обчислюються (див. ARCHITECTURE.md).
/// </summary>
public class ItineraryService : IItineraryService
{
    private readonly IActivityRepository _repository;

    public ItineraryService(IActivityRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<DateOnly> GetDays(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var days = new List<DateOnly>();
        for (var day = trip.StartDate; day <= trip.EndDate; day = day.AddDays(1))
        {
            days.Add(day);
        }

        return days;
    }

    public Task<IReadOnlyList<Activity>> GetActivitiesAsync(int tripId, DateOnly date, CancellationToken ct = default) =>
        _repository.GetByDayAsync(tripId, date, ct);

    public async Task<OperationResult<int>> AddAsync(Trip trip, Activity activity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(activity);

        var error = Validate(trip, activity);
        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        activity.TripId = trip.Id;
        Normalize(activity);

        var id = await _repository.AddAsync(activity, ct);
        return OperationResult<int>.Success(id);
    }

    public async Task<OperationResult> UpdateAsync(Trip trip, Activity activity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(activity);

        var error = Validate(trip, activity);
        if (error is not null)
        {
            return OperationResult.Failure(error);
        }

        Normalize(activity);

        await _repository.UpdateAsync(activity, ct);
        return OperationResult.Success();
    }

    public Task DeleteAsync(int activityId, CancellationToken ct = default) =>
        _repository.DeleteAsync(activityId, ct);

    /// <summary>Повертає текст першої знайденої помилки або null, якщо дані коректні.</summary>
    private static string? Validate(Trip trip, Activity activity)
    {
        if (string.IsNullOrWhiteSpace(activity.Title))
        {
            return "Введіть назву події.";
        }

        if (activity.Date < trip.StartDate || activity.Date > trip.EndDate)
        {
            return "Дата події має бути в межах дат поїздки.";
        }

        if (activity.EstimatedCost < 0)
        {
            return "Орієнтовна вартість не може бути від'ємною.";
        }

        return null;
    }

    /// <summary>Обрізає пробіли, а порожні необов'язкові поля зберігає як null.</summary>
    private static void Normalize(Activity activity)
    {
        activity.Title = activity.Title.Trim();
        activity.Location = string.IsNullOrWhiteSpace(activity.Location) ? null : activity.Location.Trim();
        activity.Note = string.IsNullOrWhiteSpace(activity.Note) ? null : activity.Note.Trim();
    }
}