using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Експорт і імпорт поїздки у JSON.
/// Експорт збирає дані через наявні сервіси розкладу, бюджету й чек-листа.
/// Імпорт спершу перевіряє структуру й значення файлу, а потім зберігає все однією транзакцією.
/// </summary>
public class DataTransferService : IDataTransferService
{
    // Обмеження збігаються з типами колонок у базі (див. Schema.sql).
    private const int MaxNameLength = 255;
    private const int MaxNoteLength = 1000;
    private const int MaxDescriptionLength = 500;
    private const decimal MaxMoney = 9_999_999_999_999_999.99m; // DECIMAL(18,2)

    private const string BrokenFileMessage = "Файл пошкоджений або не є експортом TripFlow.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        // Без цього кирилиця в файлі записується як Ка..., і файл неможливо прочитати очима.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // Категорії записуються назвою (Food), а числа замість назв файл не приймає.
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly IItineraryService _itinerary;
    private readonly IBudgetService _budget;
    private readonly IPackingService _packing;
    private readonly ITripImportRepository _importRepository;

    public DataTransferService(
        IItineraryService itinerary,
        IBudgetService budget,
        IPackingService packing,
        ITripImportRepository importRepository)
    {
        _itinerary = itinerary;
        _budget = budget;
        _packing = packing;
        _importRepository = importRepository;
    }

    // ---------- Експорт ----------

    public async Task<string> ExportAsync(Trip trip, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var activities = new List<ActivityData?>();
        foreach (var day in _itinerary.GetDays(trip))
        {
            foreach (var a in await _itinerary.GetActivitiesAsync(trip.Id, day, ct))
            {
                activities.Add(new ActivityData
                {
                    Date = a.Date,
                    Time = a.Time,
                    Title = a.Title,
                    Location = a.Location,
                    EstimatedCost = a.EstimatedCost,
                    Note = a.Note
                });
            }
        }

        var overview = await _budget.GetOverviewAsync(trip, ct);
        var expenses = overview.Expenses
            .OrderBy(e => e.Date).ThenBy(e => e.Id)
            .Select(e => (ExpenseData?)new ExpenseData
            {
                Date = e.Date,
                Amount = e.Amount,
                Category = e.Category,
                Description = e.Description
            })
            .ToList();

        var packingItems = (await _packing.GetByTripAsync(trip.Id, ct))
            .OrderBy(i => i.Id)
            .Select(i => (PackingItemData?)new PackingItemData
            {
                Name = i.Name,
                Category = i.Category,
                IsPacked = i.IsPacked
            })
            .ToList();

        var file = new TripExportFile
        {
            ExportedAt = DateTime.UtcNow,
            Trip = new TripData
            {
                Name = trip.Name,
                Destination = trip.Destination,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Budget = trip.Budget
            },
            Activities = activities,
            Expenses = expenses,
            PackingItems = packingItems
        };

        return JsonSerializer.Serialize(file, JsonOptions);
    }

    // ---------- Імпорт ----------

    public async Task<OperationResult<int>> ImportAsync(string json, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return OperationResult<int>.Failure("Файл порожній.");
        }

        TripExportFile? file;
        try
        {
            file = JsonSerializer.Deserialize<TripExportFile>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return OperationResult<int>.Failure(BrokenFileMessage);
        }

        if (file is null)
        {
            return OperationResult<int>.Failure(BrokenFileMessage);
        }

        var (data, error) = Build(file);
        if (data is null)
        {
            return OperationResult<int>.Failure(error!);
        }

        var id = await _importRepository.ImportTripAsync(
            data.Trip, data.Activities, data.Expenses, data.PackingItems, ct);
        return OperationResult<int>.Success(id);
    }

    /// <summary>Усе, що збережемо в базі після успішної перевірки файлу.</summary>
    private sealed record ImportData(
        Trip Trip,
        List<Activity> Activities,
        List<Expense> Expenses,
        List<PackingItem> PackingItems);

    /// <summary>
    /// Перевіряє файл і перетворює його на сутності. Повертає або дані, або текст першої знайденої помилки.
    /// Правила збігаються з тими, що діють при ручному введенні (ItineraryService, BudgetService, PackingService).
    /// </summary>
    private static (ImportData? Data, string? Error) Build(TripExportFile file)
    {
        if (file.FormatVersion != TripExportFile.CurrentFormatVersion)
        {
            return (null, $"Непідтримувана версія формату файлу: {file.FormatVersion}.");
        }

        var t = file.Trip;
        if (t is null)
        {
            return (null, "У файлі немає даних поїздки.");
        }

        var name = (t.Name ?? string.Empty).Trim();
        var destination = (t.Destination ?? string.Empty).Trim();
        if (name.Length == 0 || name.Length > MaxNameLength)
        {
            return (null, $"Назва поїздки має містити від 1 до {MaxNameLength} символів.");
        }
        if (destination.Length == 0 || destination.Length > MaxNameLength)
        {
            return (null, $"Напрямок поїздки має містити від 1 до {MaxNameLength} символів.");
        }
        if (t.StartDate == default || t.EndDate == default)
        {
            return (null, "У файлі не вказано дати поїздки.");
        }
        if (t.EndDate < t.StartDate)
        {
            return (null, "Дата завершення поїздки не може бути раніше дати початку.");
        }
        if (t.Budget < 0 || t.Budget > MaxMoney)
        {
            return (null, "Бюджет поїздки має бути невід'ємним числом.");
        }

        var trip = new Trip
        {
            Name = name,
            Destination = destination,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            Budget = t.Budget
        };

        var activities = new List<Activity>();
        var number = 0;
        foreach (var a in file.Activities ?? new List<ActivityData?>())
        {
            number++;
            if (a is null)
            {
                return (null, $"Подія №{number}: порожній запис.");
            }

            var title = (a.Title ?? string.Empty).Trim();
            var location = Optional(a.Location);
            var note = Optional(a.Note);
            if (title.Length == 0 || title.Length > MaxNameLength)
            {
                return (null, $"Подія №{number}: назва має містити від 1 до {MaxNameLength} символів.");
            }
            if (a.Date < trip.StartDate || a.Date > trip.EndDate)
            {
                return (null, $"Подія №{number}: дата має бути в межах дат поїздки.");
            }
            if (a.EstimatedCost is < 0 or > MaxMoney)
            {
                return (null, $"Подія №{number}: орієнтовна вартість має бути невід'ємним числом.");
            }
            if (location is { Length: > MaxNameLength } || note is { Length: > MaxNoteLength })
            {
                return (null, $"Подія №{number}: локація або нотатка задовга.");
            }

            activities.Add(new Activity
            {
                Date = a.Date,
                Time = a.Time,
                Title = title,
                Location = location,
                EstimatedCost = a.EstimatedCost,
                Note = note
            });
        }

        var expenses = new List<Expense>();
        number = 0;
        foreach (var e in file.Expenses ?? new List<ExpenseData?>())
        {
            number++;
            if (e is null)
            {
                return (null, $"Витрата №{number}: порожній запис.");
            }

            var description = Optional(e.Description);
            if (e.Amount <= 0 || e.Amount > MaxMoney)
            {
                return (null, $"Витрата №{number}: сума має бути більшою за нуль.");
            }
            if (!Enum.IsDefined(e.Category))
            {
                return (null, $"Витрата №{number}: невідома категорія.");
            }
            if (e.Date == default)
            {
                return (null, $"Витрата №{number}: не вказано дату.");
            }
            if (description is { Length: > MaxDescriptionLength })
            {
                return (null, $"Витрата №{number}: опис задовгий (максимум {MaxDescriptionLength} символів).");
            }

            expenses.Add(new Expense
            {
                Date = e.Date,
                Amount = e.Amount,
                Category = e.Category,
                Description = description
            });
        }

        var items = new List<PackingItem>();
        number = 0;
        foreach (var i in file.PackingItems ?? new List<PackingItemData?>())
        {
            number++;
            if (i is null)
            {
                return (null, $"Річ №{number}: порожній запис.");
            }

            var itemName = (i.Name ?? string.Empty).Trim();
            if (itemName.Length == 0 || itemName.Length > MaxNameLength)
            {
                return (null, $"Річ №{number}: назва має містити від 1 до {MaxNameLength} символів.");
            }
            if (!PackingCategories.IsValid(i.Category))
            {
                return (null, $"Річ №{number}: невідома категорія.");
            }

            items.Add(new PackingItem
            {
                Name = itemName,
                Category = i.Category!,
                IsPacked = i.IsPacked
            });
        }

        return (new ImportData(trip, activities, expenses, items), null);
    }

    /// <summary>Обрізає пробіли, а порожній необов'язковий текст перетворює на null.</summary>
    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
