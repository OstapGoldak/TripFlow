using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;
using Serilog;

namespace TripFlow.ViewModels;

/// <summary>
/// Головний екран: список поїздок із бази (UI → BLL → DAL → LocalDB),
/// створення поїздки через форму та видалення.
/// </summary>
public partial class TripsOverviewViewModel : ViewModelBase
{
    private readonly ITripService _tripService;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;
    private readonly IPackingService _packingService;

    public TripsOverviewViewModel(ITripService tripService, IPackingService packingService,
    IDialogService dialogs, ILogger log)
{
    _tripService = tripService;
    _packingService = packingService;
    _dialogs = dialogs;
    _log = log;
}
    public ObservableCollection<TripCardViewModel> Trips { get; } = new();

    public bool IsEmpty => Trips.Count == 0;

    /// <summary>Повідомляє MainViewModel, що користувач натиснув «Відкрити» на картці поїздки.</summary>
    public event Action<TripCardViewModel>? OpenRequested;

    [RelayCommand]
    private void OpenTrip(TripCardViewModel card) => OpenRequested?.Invoke(card);

    public async Task LoadAsync()
    {
        try
        {
            var trips = await _tripService.GetAllAsync();

            Trips.Clear();
            foreach (var trip in trips)
{
    var items = await _packingService.GetByTripAsync(trip.Id);
    Trips.Add(new TripCardViewModel(trip, items.Count(i => i.IsPacked), items.Count));
}
            OnPropertyChanged(nameof(IsEmpty));
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося завантажити поїздки");
            ErrorMessage = "Не вдалося завантажити поїздки: " + ex.Message;
        }
    }

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
