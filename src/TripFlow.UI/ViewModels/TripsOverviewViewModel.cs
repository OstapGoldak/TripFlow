using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;
using Serilog;

namespace TripFlow.ViewModels;

/// <summary>
/// Головний екран: список поїздок із бази (UI → BLL → DAL → LocalDB),
/// фільтр за статусом, створення поїздки через форму, відкриття та видалення.
/// </summary>
public partial class TripsOverviewViewModel : ViewModelBase
{
    private readonly ITripService _tripService;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    /// <summary>Усі поїздки з бази (без фільтра).</summary>
    private List<TripCardViewModel> _allTrips = new();

    public TripsOverviewViewModel(ITripService tripService, IDialogService dialogs, ILogger log)
    {
        _tripService = tripService;
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

            _allTrips = trips.Select(t => new TripCardViewModel(t)).ToList();
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

    [RelayCommand]
    private async Task CreateTripAsync()
    {
        var form = new TripFormViewModel();
        if (!_dialogs.ShowForm(form))
        {
            return;
        }

        var data = form.Result!;
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
