using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Логіка бюджету: валідація витрати, загальна сума, залишок і відсоток використання.
/// Дата витрати не обмежена датами поїздки, бо оплата можлива до виїзду (див. ARCHITECTURE.md).
/// </summary>
public class BudgetService : IBudgetService
{
    private const int MaxDescriptionLength = 500;

    private readonly IExpenseRepository _repository;

    public BudgetService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<BudgetOverview> GetOverviewAsync(Trip trip, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var expenses = await _repository.GetByTripAsync(trip.Id, ct);
        return new BudgetOverview(expenses, Calculate(trip.Budget, expenses));
    }

    public async Task<OperationResult<int>> AddExpenseAsync(Trip trip, Expense expense, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(expense);

        var error = Validate(expense);
        if (error is not null)
        {
            return OperationResult<int>.Failure(error);
        }

        expense.TripId = trip.Id;
        expense.Description = string.IsNullOrWhiteSpace(expense.Description) ? null : expense.Description.Trim();

        var id = await _repository.AddAsync(expense, ct);
        return OperationResult<int>.Success(id);
    }

    public Task DeleteExpenseAsync(int expenseId, CancellationToken ct = default) =>
        _repository.DeleteAsync(expenseId, ct);

    /// <summary>Підсумок за списком витрат (чиста функція, зручна для тестів).</summary>
    public static BudgetSummary Calculate(decimal planned, IEnumerable<Expense> expenses) =>
        new(planned, expenses.Sum(e => e.Amount));

    /// <summary>Повертає текст першої знайденої помилки або null, якщо дані коректні.</summary>
    private static string? Validate(Expense expense)
    {
        if (expense.Amount <= 0)
        {
            return "Сума витрати має бути більшою за нуль.";
        }

        if (!Enum.IsDefined(expense.Category))
        {
            return "Оберіть категорію витрати.";
        }

        if (expense.Date == default)
        {
            return "Вкажіть дату витрати.";
        }

        if (expense.Description is { Length: > MaxDescriptionLength })
        {
            return $"Опис не може бути довшим за {MaxDescriptionLength} символів.";
        }

        return null;
    }
}
