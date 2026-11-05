using CommunityToolkit.Mvvm.ComponentModel;

namespace TripFlow.ViewModels;

/// <summary>Оболонка вікна: тримає поточний екран.</summary>
public partial class MainViewModel : ObservableObject
{
    public MainViewModel(TripsOverviewViewModel overview)
    {
        Overview = overview;
        CurrentViewModel = overview;
    }

    public TripsOverviewViewModel Overview { get; }

    [ObservableProperty]
    private object? _currentViewModel;
}
