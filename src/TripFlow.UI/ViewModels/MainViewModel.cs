using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using TripFlow.BLL;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>Оболонка вікна: тримає поточний екран і перемикає між списком поїздок та чек-листом.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IPackingService _packingService;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    public MainViewModel(TripsOverviewViewModel overview, IPackingService packingService,
        IDialogService dialogs, ILogger log)
    {
        Overview = overview;
        _packingService = packingService;
        _dialogs = dialogs;
        _log = log;

        CurrentViewModel = overview;
        overview.OpenRequested += OpenPacking;
    }

    public TripsOverviewViewModel Overview { get; }

    [ObservableProperty]
    private object? _currentViewModel;

    private void OpenPacking(TripCardViewModel card)
    {
        var packing = new PackingViewModel(card.Id, card.Name, _packingService, _dialogs, _log, BackToOverviewAsync);
        CurrentViewModel = packing;
        _ = packing.LoadAsync();
    }

    private async Task BackToOverviewAsync()
    {
        CurrentViewModel = Overview;
        await Overview.LoadAsync();
    }
}