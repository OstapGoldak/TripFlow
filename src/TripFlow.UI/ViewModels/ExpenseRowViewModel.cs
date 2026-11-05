using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Рядок таблиці витрат (лише відображення).</summary>
public sealed class ExpenseRowViewModel
{
    public ExpenseRowViewModel(Expense expense)
    {
        Id = expense.Id;
        Category = expense.Category;
        DateText = expense.Date.ToString("dd.MM.yyyy");
        CategoryText = ExpenseCategoryNames.Text(expense.Category).ToUpperInvariant();
        CategoryKey = ExpenseCategoryNames.Key(expense.Category);
        Description = expense.Description ?? string.Empty;
        AmountText = MoneyFormat.Format(expense.Amount);
    }

    public int Id { get; }
    public ExpenseCategory Category { get; }
    public string DateText { get; }
    public string CategoryText { get; }

    /// <summary>Використовується стилем CategoryTag.</summary>
    public string CategoryKey { get; }
    public string Description { get; }
    public string AmountText { get; }
}
