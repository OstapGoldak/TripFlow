using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Те, що ViewModel бюджету знає про витрати. Про SQL ViewModel не знає нічого.</summary>
public interface IBudgetService
{
    /// <summary>Витрати поїздки та підрахунок: загальна сума, залишок, відсоток використання.</summary>
    Task<BudgetOverview> GetOverviewAsync(Trip trip, CancellationToken ct = default);

    /// <summary>Перевіряє й додає витрату до поїздки. Повертає Id нової витрати.</summary>
    Task<OperationResult<int>> AddExpenseAsync(Trip trip, Expense expense, CancellationToken ct = default);

    Task DeleteExpenseAsync(int expenseId, CancellationToken ct = default);
}
