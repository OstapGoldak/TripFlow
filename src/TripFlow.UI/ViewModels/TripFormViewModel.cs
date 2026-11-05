using CommunityToolkit.Mvvm.ComponentModel;

namespace TripFlow.ViewModels;

/// <summary>Дані, які користувач ввів у формі нової подорожі.</summary>
public sealed record TripFormData(string Name, string Destination, DateOnly StartDate, DateOnly EndDate, decimal Budget);

/// <summary>Сценарій 1: створення нової подорожі.</summary>
public partial class TripFormViewModel : FormViewModelBase
{
    public override string Title => "Нова подорож";

    /// <summary>Заповнюється після успішного збереження.</summary>
    public TripFormData? Result { get; private set; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _destination = string.Empty;
    [ObservableProperty] private DateTime? _startDate = DateTime.Today;
    [ObservableProperty] private DateTime? _endDate = DateTime.Today.AddDays(7);
    [ObservableProperty] private string _budgetText = "0";

    protected override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Введіть назву подорожі.";
            return false;
        }

        if (StartDate is null || EndDate is null)
        {
            ErrorMessage = "Оберіть дати початку й завершення.";
            return false;
        }

        if (EndDate < StartDate)
        {
            ErrorMessage = "Дата завершення не може бути раніше за дату початку.";
            return false;
        }

        if (!Parsing.TryDecimal(BudgetText, out var budget))
        {
            ErrorMessage = "Введіть коректне числове значення бюджету.";
            return false;
        }

        if (budget < 0)
        {
            ErrorMessage = "Бюджет не може бути від'ємним.";
            return false;
        }

        Result = new TripFormData(
            Name.Trim(),
            Destination.Trim(),
            DateOnly.FromDateTime(StartDate.Value),
            DateOnly.FromDateTime(EndDate.Value),
            budget);
        return true;
    }
}
