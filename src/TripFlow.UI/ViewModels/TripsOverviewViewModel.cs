using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>
/// Головний екран: список поїздок із бази (UI → BLL → DAL → LocalDB),
/// створення поїздки через форму та видалення.
/// </summary>
public partial class TripsOverviewViewModel : ViewModelBase
{
    private readonly ITripService _tripService;
    private readonly IDialogService _dialogs;

    public TripsOverviewViewModel(ITripService tripService, IDialogService dialogs)
    {
        _tripService = tripService;
        _dialogs = dialogs;
    }

    public ObservableCollection<TripCardViewModel> Trips { get; } = new();

    public bool IsEmpty => Trips.Count == 0;

    public async Task LoadAsync()
    {
        try
        {
            var trips = await _tripService.GetAllAsync();

            Trips.Clear();
            foreach (var trip in trips)
            {
                Trips.Add(new TripCardViewModel(trip));
            }

            OnPropertyChanged(nameof(IsEmpty));
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
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
            await LoadAsync();
        }
        catch (Exception ex)
        {
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
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не вдалося видалити поїздку: " + ex.Message;
        }
    }
}
