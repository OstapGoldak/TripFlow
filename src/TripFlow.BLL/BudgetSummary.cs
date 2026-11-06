using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Стан бюджету для індикатора: звичайний, майже вичерпаний (від 80%) або перевитрата.</summary>
public enum BudgetLevel
{
    Normal,
    Warning,
    Over
}

/// <summary>Підсумок бюджету поїздки: заплановано, витрачено, залишок і відсоток використання.</summary>
public sealed record BudgetSummary(decimal Planned, decimal Spent)
{
    public decimal Remaining => Planned - Spent;

    /// <summary>Відсоток використання (може перевищувати 100 при перевитраті).</summary>
    public double UsedPercent =>
        Planned > 0 ? (double)(Spent / Planned * 100m) : (Spent > 0 ? 100d : 0d);

    public BudgetLevel Level =>
        Remaining < 0 ? BudgetLevel.Over :
        UsedPercent >= 80 ? BudgetLevel.Warning :
        BudgetLevel.Normal;
}

/// <summary>Усе, що потрібно вкладці «Бюджет»: список витрат і підсумок.</summary>
public sealed record BudgetOverview(IReadOnlyList<Expense> Expenses, BudgetSummary Summary);
