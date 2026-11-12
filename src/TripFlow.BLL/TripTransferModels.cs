using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Файл експорту та імпорту поїздки (JSON).
/// Без Id і TripId: при імпорті база сама створює нові записи, тому файл не конфліктує з наявними даними.
/// Поля, що можуть бути відсутні у пошкодженому файлі, оголошені як nullable, а перевіряє їх DataTransferService.
/// </summary>
public sealed class TripExportFile
{
    /// <summary>Версія формату. Збільшується, якщо структура файлу зміниться несумісно.</summary>
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;

    /// <summary>Коли файл створено (UTC).</summary>
    public DateTime ExportedAt { get; set; }

    public TripData? Trip { get; set; }

    public List<ActivityData?>? Activities { get; set; }

    public List<ExpenseData?>? Expenses { get; set; }

    public List<PackingItemData?>? PackingItems { get; set; }
}

public sealed class TripData
{
    public string? Name { get; set; }
    public string? Destination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Budget { get; set; }
}

public sealed class ActivityData
{
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? Note { get; set; }
}

public sealed class ExpenseData
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }

    /// <summary>У файлі записується назвою (Food), а не числом.</summary>
    public ExpenseCategory Category { get; set; }

    public string? Description { get; set; }
}

public sealed class PackingItemData
{
    public string? Name { get; set; }

    /// <summary>Documents, Clothing, Electronics, FirstAid або Other.</summary>
    public string? Category { get; set; }

    public bool IsPacked { get; set; }
}
