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
    private readonly IReportService _report;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    public TripDetailsViewModel(Trip trip, Action goBack,
        BudgetViewModel budget, ItineraryViewModel itinerary, PackingViewModel packing,
        IDataTransferService transfer, IReportService report, IDialogService dialogs, ILogger log)
    {
        Trip = trip;
        _goBack = goBack;
        _transfer = transfer;
        _report = report;
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

    /// <summary>
    /// PDF-звіт поїздки (розклад, бюджет, чек-лист) через принтер Windows «Microsoft Print to PDF».
    /// Куди зберегти файл, Windows запитує у власному вікні.
    /// </summary>
    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        try
        {
            var report = await _report.BuildAsync(Trip);
            if (!PdfReportWriter.Write(report))
            {
                _dialogs.ShowError("Не знайдено принтер «Microsoft Print to PDF». Увімкніть його: " +
                                   "Панель керування → Програми → Увімкнення або вимкнення компонентів Windows → " +
                                   "«Microsoft Print to PDF».");
                return;
            }

            _log.Information("PDF-звіт поїздки {TripId} надіслано на принтер PDF", Trip.Id);
        }
        catch (Exception ex) when (ex.GetType().Name == "PrintingCanceledException")
        {
            // Користувач закрив вікно збереження PDF: це не помилка
            _log.Information("Збереження PDF-звіту поїздки {TripId} скасовано", Trip.Id);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося створити PDF-звіт поїздки {TripId}", Trip.Id);
            _dialogs.ShowError("Не вдалося створити PDF-звіт: " + ex.Message);
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