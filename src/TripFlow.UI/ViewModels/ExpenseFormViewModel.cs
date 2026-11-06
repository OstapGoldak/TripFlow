using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Дані, які користувач ввів у формі нової витрати.</summary>
public sealed record ExpenseFormData(DateOnly Date, decimal Amount, ExpenseCategory Category, string Description);

/// <summary>Сценарій 4: внесення витрати.</summary>
public partial class ExpenseFormViewModel : FormViewModelBase
{
    public ExpenseFormViewModel()
    {
        _selectedCategory = Categories[0];
    }

    public override string Title => "Нова витрата";

    public IReadOnlyList<CategoryOption> Categories { get; } = ExpenseCategoryNames.Options;

    /// <summary>Заповнюється після успішного збереження.</summary>
    public ExpenseFormData? Result { get; private set; }

    [ObservableProperty] private DateTime? _date = DateTime.Today;
    [ObservableProperty] private string _amountText = string.Empty;
    [ObservableProperty] private CategoryOption? _selectedCategory;
    [ObservableProperty] private string _description = string.Empty;

    protected override bool Validate()
    {
        if (Date is null)
        {
            ErrorMessage = "Оберіть дату витрати.";
            return false;
        }

        if (!Parsing.TryDecimal(AmountText, out var amount))
        {
            ErrorMessage = "Введіть суму числом.";
            return false;
        }

        if (amount <= 0)
        {
            ErrorMessage = "Сума має бути більшою за нуль.";
            return false;
        }

        if (SelectedCategory is null)
        {
            ErrorMessage = "Оберіть категорію.";
            return false;
        }

        Result = new ExpenseFormData(
            DateOnly.FromDateTime(Date.Value), amount, SelectedCategory.Value, Description.Trim());
        return true;
    }
}
