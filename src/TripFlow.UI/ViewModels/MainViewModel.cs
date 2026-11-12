using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>Оболонка вікна: тримає поточний екран і перемикає екрани.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Func<BudgetViewModel> _createBudget;
    private readonly Func<ItineraryViewModel> _createItinerary;
    private readonly Func<PackingViewModel> _createPacking;
    private readonly IDataTransferService _transfer;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    public MainViewModel(TripsOverviewViewModel overview,
        Func<BudgetViewModel> createBudget,
        Func<ItineraryViewModel> createItinerary,
        Func<PackingViewModel> createPacking,
        IDataTransferService transfer,
        IDialogService dialogs,
        ILogger log)
    {
        Overview = overview;
        _createBudget = createBudget;
        _createItinerary = createItinerary;
        _createPacking = createPacking;
        _transfer = transfer;
        _dialogs = dialogs;
        _log = log;
        Overview.OpenTripRequested += OpenTrip;
        CurrentViewModel = overview;
    }

    public TripsOverviewViewModel Overview { get; }

    [ObservableProperty]
    private object? _currentViewModel;

    /// <summary>Список → вікно поїздки. Вкладки модулів створюються тут і передаються у вікно.</summary>
    private void OpenTrip(Trip trip) =>
        CurrentViewModel = new TripDetailsViewModel(trip, ShowOverview,
            _createBudget(), _createItinerary(), _createPacking(),
            _transfer, _dialogs, _log);

    /// <summary>Вікно поїздки → список (з оновленням даних).</summary>
    private async void ShowOverview()
    {
        CurrentViewModel = Overview;
        await Overview.LoadAsync();
    }
}