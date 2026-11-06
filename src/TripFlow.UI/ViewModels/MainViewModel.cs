using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Оболонка вікна: тримає поточний екран і перемикає екрани.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Func<BudgetViewModel> _createBudget;
    private readonly Func<ItineraryViewModel> _createItinerary;

    public MainViewModel(TripsOverviewViewModel overview,
        Func<BudgetViewModel> createBudget,
        Func<ItineraryViewModel> createItinerary)
    {
        Overview = overview;
        _createBudget = createBudget;
        _createItinerary = createItinerary;
        Overview.OpenTripRequested += OpenTrip;
        CurrentViewModel = overview;
    }

    public TripsOverviewViewModel Overview { get; }

    [ObservableProperty]
    private object? _currentViewModel;

    /// <summary>Список → вікно поїздки. Вкладки модулів створюються тут і передаються у вікно.</summary>
    private void OpenTrip(Trip trip) =>
        CurrentViewModel = new TripDetailsViewModel(trip, ShowOverview, _createBudget(), _createItinerary());

    /// <summary>Вікно поїздки → список (з оновленням даних).</summary>
    private async void ShowOverview()
    {
        CurrentViewModel = Overview;
        await Overview.LoadAsync();
    }
}
