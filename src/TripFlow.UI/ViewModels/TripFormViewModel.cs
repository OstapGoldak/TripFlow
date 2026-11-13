using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Дані, які користувач ввів у формі нової подорожі.</summary>
public sealed record TripFormData(string Name, string Destination, DateOnly StartDate, DateOnly EndDate, decimal Budget);

/// <summary>
/// Сценарій 1: створення нової подорожі, а також редагування наявної.
/// Форма перевіряє лише формат введення (обрано дати, бюджет є числом).
/// Бізнес-правила (порожні поля, порядок дат, від'ємний бюджет, дні з подіями) перевіряє TripService:
/// форма питає сервіс і лишається відкритою з його повідомленням (альтернативний потік 5a).
/// </summary>
public partial class TripFormViewModel : FormViewModelBase
{
    private readonly ITripService _tripService;
    private readonly bool _isEdit;

    /// <summary>Дні з подіями розкладу (лише під час редагування): нові дати мають їх охоплювати.</summary>
    private readonly (DateOnly First, DateOnly Last)? _scheduleRange;

    /// <summary>Нова подорож.</summary>
    public TripFormViewModel(ITripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>Редагування наявної подорожі: поля заповнюються її даними.</summary>
    public TripFormViewModel(ITripService tripService, Trip trip, (DateOnly First, DateOnly Last)? scheduleRange)
        : this(tripService)
    {
        _isEdit = true;
        _scheduleRange = scheduleRange;
        _name = trip.Name;
        _destination = trip.Destination;
        _startDate = trip.StartDate.ToDateTime(TimeOnly.MinValue);
        _endDate = trip.EndDate.ToDateTime(TimeOnly.MinValue);
        _budgetText = trip.Budget.ToString("0.##", CultureInfo.CurrentCulture);
    }

    public override string Title => _isEdit ? "Редагування подорожі" : "Нова подорож";

    /// <summary>Заповнюється після успішної перевірки.</summary>
    public TripFormData? Result { get; private set; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _destination = string.Empty;
    [ObservableProperty] private DateTime? _startDate = DateTime.Today;
    [ObservableProperty] private DateTime? _endDate = DateTime.Today.AddDays(7);
    [ObservableProperty] private string _budgetText = "0";

    protected override bool Validate()
    {
        // Формат введення: це відповідальність форми
        if (StartDate is null || EndDate is null)
        {
            ErrorMessage = "Оберіть дати початку й завершення.";
            return false;
        }

        if (!Parsing.TryDecimal(BudgetText, out var budget))
        {
            ErrorMessage = "Введіть коректне числове значення бюджету.";
            return false;
        }

        var data = new TripFormData(
            Name,
            Destination,
            DateOnly.FromDateTime(StartDate.Value),
            DateOnly.FromDateTime(EndDate.Value),
            budget);

        // Бізнес-правила: лише в сервісі
        var check = _tripService.Validate(new Trip
        {
            Name = data.Name,
            Destination = data.Destination,
            StartDate = data.StartDate,
            EndDate = data.EndDate,
            Budget = data.Budget
        }, _scheduleRange);

        if (!check.IsSuccess)
        {
            ErrorMessage = check.ErrorMessage;
            return false;
        }

        Result = data;
        return true;
    }
}
