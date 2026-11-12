using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Те, що ViewModel знає про поїздки. Про базу даних і SQL ViewModel не знає нічого.</summary>
public interface ITripService
{
    Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Перевіряє бізнес-правила поїздки без збереження.
    /// Форма викликає його перед закриттям, щоб показати помилку й не втратити введені дані.
    /// </summary>
    OperationResult Validate(Trip trip);

    /// <summary>Перевіряє й зберігає поїздку. Якщо дані некоректні, нічого не зберігає й повертає помилку.</summary>
    Task<OperationResult<int>> AddAsync(Trip trip, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Статус поїздки відносно вказаної дати (зазвичай сьогоднішньої).</summary>
    TripStatus GetStatus(Trip trip, DateOnly today);
}
