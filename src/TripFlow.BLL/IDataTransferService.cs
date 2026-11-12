using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Експорт і імпорт поїздки у JSON. Сервіс працює з текстом, а вибір файлу
/// та читання й запис на диск виконує інтерфейс (див. ARCHITECTURE.md).
/// </summary>
public interface IDataTransferService
{
    /// <summary>Збирає поїздку з розкладом, витратами та чек-листом у JSON-рядок.</summary>
    Task<string> ExportAsync(Trip trip, CancellationToken ct = default);

    /// <summary>
    /// Перевіряє JSON і зберігає поїздку в базу як нову. Повертає Id нової поїздки
    /// або зрозуміле користувачеві повідомлення, чому файл не прийнято.
    /// </summary>
    Task<OperationResult<int>> ImportAsync(string json, CancellationToken ct = default);
}
