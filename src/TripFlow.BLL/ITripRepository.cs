using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Інтерфейс репозиторію оголошує BLL, а реалізує DAL.
/// Завдяки цьому TripService можна тестувати із заглушкою, без бази даних.
/// </summary>
public interface ITripRepository
{
    Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Додає поїздку й повертає її новий Id.</summary>
    Task<int> AddAsync(Trip trip, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
