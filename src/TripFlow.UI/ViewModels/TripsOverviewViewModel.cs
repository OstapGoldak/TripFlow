using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;
using Serilog;

namespace TripFlow.ViewModels;

/// <summary>
/// Головний екран: список поїздок із бази (UI → BLL → DAL → LocalDB),
/// фільтр за статусом, прогрес пакування на картках, створення, відкриття та видалення.
/// </summary>
public partial class TripsOverviewViewModel : ViewModelBase
{
    private readonly ITripService _tripService;
    private readonly IPackingService _packingService;
    private readonly IDataTransferService _transfer;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    /// <summary>Файл імпорту більший за це значення вважаємо помилковим (звичайна поїздка займає кілька КБ).</summary>
    private const long MaxImportFileBytes = 5 * 1024 * 1024;

    /// <summary>Усі поїздки з бази (без фільтра).</summary>
    private List<TripCardViewModel> _allTrips = new();

    public TripsOverviewViewModel(ITripService tripService, IPackingService packingService,
        IDataTransferService transfer, IDialogService dialogs, ILogger log)
    {
        _tripService = tripService;
        _packingService = packingService;
        _transfer = transfer;
        _dialogs = dialogs;
        _log = log;
    }

    /// <summary>Користувач натиснув «Відкрити» на картці (на неї підписаний MainViewModel).</summary>
    public event Action<Trip>? OpenTripRequested;

    /// <summary>Картки, які зараз показуються (після фільтра).</summary>
    public ObservableCollection<TripCardViewModel> Trips { get; } = new();

    public bool IsEmpty => Trips.Count == 0;

    // Фільтр за статусом: кнопки «Усі» / «Заплановані» / «Завершені».
    // Перемикаються як радіокнопки, тому під час зміни стан може оновлюватися двічі, це нормально.
    [ObservableProperty] private bool _isAllFilter = true;
    [ObservableProperty] private bool _isPlannedFilter;
    [ObservableProperty] private bool _isCompletedFilter;

    partial void OnIsAllFilterChanged(bool value) => ApplyFilter();
    partial void OnIsPlannedFilterChanged(bool value) => ApplyFilter();
    partial void OnIsCompletedFilterChanged(bool value) => ApplyFilter();

    /// <summary>
    /// «Завершені» — лише завершені.
    /// «Заплановані» — ще не завершені (заплановані та ті, що вже тривають).
    /// </summary>
    private void ApplyFilter()
    {
        IEnumerable<TripCardViewModel> filtered = _allTrips;

        if (IsCompletedFilter)
        {
            filtered = _allTrips.Where(t => t.Status == "Completed");
        }
        else if (IsPlannedFilter)
        {
            filtered = _allTrips.Where(t => t.Status != "Completed");
        }

        Trips.Clear();
        foreach (var card in filtered)
        {
            Trips.Add(card);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public async Task LoadAsync()
    {
        try
        {
            var trips = await _tripService.GetAllAsync();

            var cards = new List<TripCardViewModel>();
            foreach (var trip in trips)
            {
                // Прогрес пакування на картці: скільки речей спаковано з усіх
                var items = await _packingService.GetByTripAsync(trip.Id);
                cards.Add(new TripCardViewModel(trip, items.Count(i => i.IsPacked), items.Count));
            }

            _allTrips = cards;
            ApplyFilter();
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося завантажити поїздки");
            ErrorMessage = "Не вдалося завантажити поїздки: " + ex.Message;
        }
    }


    [RelayCommand]
    private void OpenTrip(TripCardViewModel card) => OpenTripRequested?.Invoke(card.Trip);

    /// <summary>Імпорт поїздки з файлу .json: створює нову поїздку з розкладом, витратами й чек-листом.</summary>
    [RelayCommand]
    private async Task ImportJsonAsync()
    {
        var path = _dialogs.PickOpenFile("JSON (*.json)|*.json");
        if (path is null)
        {
            return; // користувач скасував вибір файлу
        }

        try
        {
            if (new FileInfo(path).Length > MaxImportFileBytes)
            {
                ErrorMessage = "Файл завеликий для імпорту поїздки.";
                return;
            }

            var json = await File.ReadAllTextAsync(path);
            var result = await _transfer.ImportAsync(json);
            if (!result.IsSuccess)
            {
                _log.Warning("Імпорт файлу {Path} відхилено: {Reason}", path, result.ErrorMessage);
                ErrorMessage = "Імпорт не виконано. " + result.ErrorMessage;
                return;
            }

            _log.Information("Поїздку імпортовано з файлу {Path}, новий Id {TripId}", path, result.Value);
            await LoadAsync();
            _dialogs.ShowInfo("Поїздку імпортовано.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося імпортувати поїздку з файлу {Path}", path);
            ErrorMessage = "Не вдалося імпортувати файл: " + ex.Message;
        }
    }


    [RelayCommand]
    private async Task CreateTripAsync()
    {
        var form = new TripFormViewModel();
        if (!_dialogs.ShowForm(form))
        {
            return;
        }var data = form.Result!;
        try
        {
            await _tripService.AddAsync(new Trip
            {
                Name = data.Name,
                Destination = data.Destination,
                StartDate = data.StartDate,
                EndDate = data.EndDate,
                Budget = data.Budget
            });
            _log.Information("Поїздку створено: {Name}", data.Name);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося зберегти поїздку {Name}", data.Name);
            ErrorMessage = "Не вдалося зберегти поїздку: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteTripAsync(TripCardViewModel card)
    {
        if (!_dialogs.Confirm($"Видалити подорож «{card.Name}»?"))
        {
            return;
        }

        try
        {
            await _tripService.DeleteAsync(card.Id);
            _log.Information("Поїздку видалено: {TripId}", card.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося видалити поїздку {TripId}", card.Id);
            ErrorMessage = "Не вдалося видалити поїздку: " + ex.Message;
        }
    }
}
