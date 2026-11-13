using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>

/// </summary>
public interface IActivityRepository
{
    /// <summary>Активності одного дня поїздки, відсортовані за часом.</summary>
    Task<IReadOnlyList<Activity>> GetByDayAsync(int tripId, DateOnly date, CancellationToken ct = default);

    Task<Activity?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Додає активність і повертає її новий Id.</summary>
    Task<int> AddAsync(Activity activity, CancellationToken ct = default);

    Task UpdateAsync(Activity activity, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Найраніша й найпізніша дата подій поїздки або null, якщо подій немає.
    /// Потрібна, щоб під час редагування дат поїздки події не опинилися поза нею.
    /// </summary>
    Task<(DateOnly First, DateOnly Last)?> GetDateRangeAsync(int tripId, CancellationToken ct = default);
}