using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Дані, які користувач ввів у формі нової подорожі.</summary>
public sealed record TripFormData(string Name, string Destination, DateOnly StartDate, DateOnly EndDate, decimal Budget);

/// <summary>
/// Сценарій 1: створення нової подорожі.
/// Форма перевіряє лише формат введення (обрано дати, бюджет є числом).
/// Бізнес-правила (порожні поля, порядок дат, від'ємний бюджет) перевіряє TripService:
/// форма питає сервіс і лишається відкритою з його повідомленням (альтернативний потік 5a).
/// </summary>
public partial class TripFormViewModel : FormViewModelBase
{
    private readonly ITripService _tripService;

    public TripFormViewModel(ITripService tripService)
    {
        _tripService = tripService;
    }

    public override string Title => "Нова подорож";

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
        });

        if (!check.IsSuccess)
        {
            ErrorMessage = check.ErrorMessage;
            return false;
        }

        Result = data;
        return true;
    }
}
