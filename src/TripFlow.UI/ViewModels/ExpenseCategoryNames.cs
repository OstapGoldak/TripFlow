using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Пункт списку категорій у формі витрати.</summary>
public sealed record CategoryOption(ExpenseCategory Value, string Text);

/// <summary>Підписи категорій витрат українською та ключі для стилю CategoryTag.</summary>
public static class ExpenseCategoryNames
{
    public static string Text(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Food => "Харчування",
        ExpenseCategory.Housing => "Проживання",
        ExpenseCategory.Transport => "Транспорт",
        _ => "Інше"
    };

    /// <summary>Ключ для DataTrigger у стилі CategoryTag (Resources/Styles.xaml).</summary>
    public static string Key(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Food => "Food",
        ExpenseCategory.Housing => "Accommodation",
        ExpenseCategory.Transport => "Transport",
        _ => "Other"
    };

    public static IReadOnlyList<CategoryOption> Options { get; } = new[]
    {
        ExpenseCategory.Food, ExpenseCategory.Housing, ExpenseCategory.Transport, ExpenseCategory.Other
    }.Select(c => new CategoryOption(c, Text(c))).ToList();
}
