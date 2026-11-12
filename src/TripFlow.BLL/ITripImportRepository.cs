using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Збереження імпортованої поїздки разом з усіма дочірніми записами.
/// Інтерфейс оголошує BLL, а реалізує DAL, тому DataTransferService тестується без бази даних.
/// </summary>
public interface ITripImportRepository
{
    /// <summary>
    /// Зберігає поїздку, її активності, витрати та речі в одній транзакції:
    /// або записується все, або нічого (див. ARCHITECTURE.md). Повертає Id нової поїздки.
    /// TripId у переданих записах ігнорується, репозиторій підставляє Id нової поїздки.
    /// </summary>
    Task<int> ImportTripAsync(
        Trip trip,
        IReadOnlyList<Activity> activities,
        IReadOnlyList<Expense> expenses,
        IReadOnlyList<PackingItem> packingItems,
        CancellationToken ct = default);
}
