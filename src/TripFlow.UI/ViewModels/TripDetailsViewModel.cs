using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>
/// Вікно поїздки з вкладками «Розклад», «Бюджет», «Чек-лист».
/// Тримає обрану поїздку та ViewModel кожної вкладки.
/// </summary>
public partial class TripDetailsViewModel : ViewModelBase
{
    private readonly Action _goBack;

    public TripDetailsViewModel(Trip trip, Action goBack, BudgetViewModel budget)
    {
        Trip = trip;
        _goBack = goBack;

        Budget = budget;
        // LoadAsync сам перехоплює помилки й показує їх у вкладці, тому результат можна не чекати
        _ = Budget.LoadAsync(trip);
    }

    /// <summary>Обрана поїздка.</summary>
    public Trip Trip { get; }

    /// <summary>Вкладка «Бюджет».</summary>
    public BudgetViewModel Budget { get; }

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