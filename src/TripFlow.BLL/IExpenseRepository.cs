using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Інтерфейс оголошує BLL, а реалізує DAL, тому BudgetService тестується без бази даних.</summary>
public interface IExpenseRepository
{
    /// <summary>Усі витрати поїздки: спершу найновіші.</summary>
    Task<IReadOnlyList<Expense>> GetByTripAsync(int tripId, CancellationToken ct = default);

    /// <summary>Додає витрату й повертає її новий Id.</summary>
    Task<int> AddAsync(Expense expense, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
