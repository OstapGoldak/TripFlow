using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Те, що ViewModel розкладу знає про активності. Про SQL ViewModel не знає нічого.</summary>
public interface IItineraryService
{
    /// <summary>Генерує дні поїздки за датами початку й завершення (включно).</summary>
    IReadOnlyList<DateOnly> GetDays(Trip trip);

    /// <summary>Активності дня, відсортовані за часом.</summary>
    Task<IReadOnlyList<Activity>> GetActivitiesAsync(int tripId, DateOnly date, CancellationToken ct = default);

    /// <summary>Перевіряє й додає активність до поїздки. Повертає Id нової активності.</summary>
    Task<OperationResult<int>> AddAsync(Trip trip, Activity activity, CancellationToken ct = default);

    /// <summary>Перевіряє й зберігає зміни активності.</summary>
    Task<OperationResult> UpdateAsync(Trip trip, Activity activity, CancellationToken ct = default);

    Task DeleteAsync(int activityId, CancellationToken ct = default);
}