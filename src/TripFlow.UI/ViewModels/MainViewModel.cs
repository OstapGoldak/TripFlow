using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Оболонка вікна: тримає поточний екран і перемикає екрани.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Func<PackingViewModel> _createPacking;

    public MainViewModel(TripsOverviewViewModel overview, Func<PackingViewModel> createPacking)
    {
        Overview = overview;
        _createPacking = createPacking;
        Overview.OpenTripRequested += OpenTrip;
        CurrentViewModel = overview;
    }

    public TripsOverviewViewModel Overview { get; }

    [ObservableProperty]
    private object? _currentViewModel;

    /// <summary>Список → вікно поїздки.</summary>
    private void OpenTrip(Trip trip) =>
        CurrentViewModel = new TripDetailsViewModel(trip, _createPacking(), ShowOverview);

    /// <summary>Вікно поїздки → список (з оновленням даних).</summary>
    private async void ShowOverview()
    {
        CurrentViewModel = Overview;
        await Overview.LoadAsync();
    }
}