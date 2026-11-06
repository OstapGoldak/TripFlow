using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Оболонка вікна: тримає поточний екран і перемикає екрани.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Func<BudgetViewModel> _createBudget;

    public MainViewModel(TripsOverviewViewModel overview, Func<BudgetViewModel> createBudget)
    {
        Overview = overview;
        _createBudget = createBudget;
        Overview.OpenTripRequested += OpenTrip;
        CurrentViewModel = overview;
    }

    public TripsOverviewViewModel Overview { get; }

    [ObservableProperty]
    private object? _currentViewModel;

    /// <summary>Список → вікно поїздки. Вкладки модулів створюються тут і передаються у вікно.</summary>
    private void OpenTrip(Trip trip) =>
        CurrentViewModel = new TripDetailsViewModel(trip, ShowOverview, _createBudget());

    /// <summary>Вікно поїздки → список (з оновленням даних).</summary>
    private async void ShowOverview()
    {
        CurrentViewModel = Overview;
        await Overview.LoadAsync();
    }
}