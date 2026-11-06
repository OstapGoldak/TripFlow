using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>
/// Вікно поїздки з вкладками «Розклад», «Бюджет», «Чек-лист».
/// Тримає обрану поїздку; вміст вкладок додають окремі модулі.
/// </summary>
public partial class TripDetailsViewModel : ViewModelBase
{
    private readonly Action _goBack;

    public TripDetailsViewModel(Trip trip, PackingViewModel packing, Action goBack)
{
    Trip = trip;
    Packing = packing;
    _goBack = goBack;
    _ = Packing.LoadAsync(trip);
}

/// <summary>Вкладка «Чек-лист».</summary>
public PackingViewModel Packing { get; }

    /// <summary>Обрана поїздка. Інші модулі беруть її звідси (наприклад, new ItineraryViewModel(Trip, ...)).</summary>
    public Trip Trip { get; }

    public string Name => Trip.Name;

    /// <summary>"15.07 – 22.07.2025" (рік показується один раз, якщо він однаковий).</summary>
    public string DatesText => Trip.StartDate.Year == Trip.EndDate.Year
        ? $"{Trip.StartDate:dd.MM} – {Trip.EndDate:dd.MM.yyyy}"
        : $"{Trip.StartDate:dd.MM.yyyy} – {Trip.EndDate:dd.MM.yyyy}";

    // Активна вкладка (кнопки-перемикачі у шапці). За замовчуванням відкривається «Розклад».
    [ObservableProperty] private bool _isScheduleTab = true;
    [ObservableProperty] private bool _isBudgetTab;
    [ObservableProperty] private bool _isChecklistTab;

    [RelayCommand]
    private void Back() => _goBack();
}
