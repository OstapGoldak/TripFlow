using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Те, що ViewModel знає про поїздки. Про базу даних і SQL ViewModel не знає нічого.</summary>
public interface ITripService
{
    Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Перевіряє бізнес-правила поїздки без збереження.
    /// Форма викликає його перед закриттям, щоб показати помилку й не втратити введені дані.
    /// <paramref name="scheduleRange"/> — дні, на які вже є події розкладу (під час редагування):
    /// нові дати поїздки мають їх охоплювати.
    /// </summary>
    OperationResult Validate(Trip trip, (DateOnly First, DateOnly Last)? scheduleRange = null);

    /// <summary>Перевіряє й зберігає поїздку. Якщо дані некоректні, нічого не зберігає й повертає помилку.</summary>
    Task<OperationResult<int>> AddAsync(Trip trip, CancellationToken ct = default);

    /// <summary>Дні, на які в поїздки є події розкладу, або null, якщо подій немає.</summary>
    Task<(DateOnly First, DateOnly Last)?> GetScheduleRangeAsync(int tripId, CancellationToken ct = default);

    /// <summary>
    /// Перевіряє й оновлює поїздку (Id обов'язковий).
    /// Відмовляє, якщо нові дати не охоплюють наявні події розкладу; тоді нічого не зберігається.
    /// </summary>
    Task<OperationResult> UpdateAsync(Trip trip, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Статус поїздки відносно вказаної дати (зазвичай сьогоднішньої).</summary>
    TripStatus GetStatus(Trip trip, DateOnly today);
}
