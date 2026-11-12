using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>
/// Вікно поїздки з вкладками «Розклад», «Бюджет», «Чек-лист».
/// Тримає обрану поїздку та ViewModel кожної вкладки.
/// </summary>
public partial class TripDetailsViewModel : ViewModelBase
{
    private readonly Action _goBack;
    private readonly IDataTransferService _transfer;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    public TripDetailsViewModel(Trip trip, Action goBack,
        BudgetViewModel budget, ItineraryViewModel itinerary, PackingViewModel packing,
        IDataTransferService transfer, IDialogService dialogs, ILogger log)
    {
        Trip = trip;
        _goBack = goBack;
        _transfer = transfer;
        _dialogs = dialogs;
        _log = log;

        // LoadAsync вкладок сам перехоплює помилки й показує їх у вкладці, тому результат можна не чекати
        Budget = budget;
        _ = Budget.LoadAsync(trip);

        Itinerary = itinerary;
        _ = Itinerary.LoadAsync(trip);

        Packing = packing;
        _ = Packing.LoadAsync(trip);
    }

    /// <summary>Обрана поїздка.</summary>
    public Trip Trip { get; }

    /// <summary>Вкладка «Бюджет».</summary>
    public BudgetViewModel Budget { get; }

    /// <summary>Вкладка «Розклад».</summary>
    public ItineraryViewModel Itinerary { get; }

    /// <summary>Вкладка «Чек-лист».</summary>
    public PackingViewModel Packing { get; }

    public string Name => Trip.Name;

    /// <summary>"15.07 – 22.07.2025" (рік показується один раз, якщо він однаковий).</summary>
    public string DatesText => Trip.StartDate.Year == Trip.EndDate.Year
        ? $"{Trip.StartDate:dd.MM} – {Trip.EndDate:dd.MM.yyyy}"
        : $"{Trip.StartDate:dd.MM.yyyy} – {Trip.EndDate:dd.MM.yyyy}";

    // Активна вкладка (кнопки-перемикачі у шапці). За замовчуванням відкривається «Розклад».
    [ObservableProperty] private bool _isScheduleTab = true;
    [ObservableProperty] private bool _isBudgetTab;
    [ObservableProperty] private bool _isChecklistTab;

    [RelayCommand]
    private void Back() => _goBack();

    /// <summary>Експорт поїздки з розкладом, витратами й чек-листом у файл .json.</summary>
    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        var path = _dialogs.PickSaveFile(SafeFileName(Trip.Name) + ".json", "JSON (*.json)|*.json");
        if (path is null)
        {
            return; // користувач скасував вибір файлу
        }

        try
        {
            var json = await _transfer.ExportAsync(Trip);
            await File.WriteAllTextAsync(path, json, new UTF8Encoding(false));

            _log.Information("Поїздку {TripId} експортовано у файл {Path}", Trip.Id, path);
            _dialogs.ShowInfo("Поїздку експортовано: " + path);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося експортувати поїздку {TripId}", Trip.Id);
            _dialogs.ShowError("Не вдалося експортувати поїздку: " + ex.Message);
        }
    }

    /// <summary>Замінює символи, недопустимі в назві файлу.</summary>
    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return cleaned.Length == 0 ? "trip" : cleaned;
    }
}