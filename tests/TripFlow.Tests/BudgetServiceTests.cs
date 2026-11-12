using Moq;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.Tests;

/// <summary>Модульні тести BudgetService. Репозиторій замінено заглушкою (Moq), тому база не потрібна.</summary>
public class BudgetServiceTests
{
    private readonly Mock<IExpenseRepository> _repository = new();
    private readonly BudgetService _service;

    private readonly Trip _trip = new()
    {
        Id = 7,
        Name = "Карпати",
        Destination = "Буковель",
        StartDate = new DateOnly(2026, 12, 1),
        EndDate = new DateOnly(2026, 12, 8),
        Budget = 15000m
    };

    public BudgetServiceTests()
    {
        _service = new BudgetService(_repository.Object);
    }

    private static Expense ValidExpense() => new()
    {
        Date = new DateOnly(2026, 11, 28), // до виїзду: дозволено
        Amount = 620m,
        Category = ExpenseCategory.Food,
        Description = "  Обід у ресторані  "
    };

    private void SetupExpenses(params decimal[] amounts) =>
        _repository.Setup(r => r.GetByTripAsync(_trip.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(amounts.Select(a => new Expense { Amount = a }).ToList());

    // ---------- Додавання ----------

    [Fact]
    public async Task AddExpense_ValidData_SavesTrimmedExpenseForTrip()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);

        var result = await _service.AddExpenseAsync(_trip, ValidExpense());

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
        _repository.Verify(r => r.AddAsync(
            It.Is<Expense>(e => e.TripId == 7 && e.Description == "Обід у ресторані"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task AddExpense_AmountNotPositive_ReturnsErrorAndDoesNotSave(decimal amount)
    {
        var expense = ValidExpense();
        expense.Amount = amount;

        var result = await _service.AddExpenseAsync(_trip, expense);

        Assert.False(result.IsSuccess);
        Assert.Equal("Сума витрати має бути більшою за нуль.", result.ErrorMessage);
        _repository.Verify(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddExpense_UnknownCategory_ReturnsError()
    {
        var expense = ValidExpense();
        expense.Category = (ExpenseCategory)99;

        var result = await _service.AddExpenseAsync(_trip, expense);

        Assert.False(result.IsSuccess);
        Assert.Equal("Оберіть категорію витрати.", result.ErrorMessage);
    }

    [Fact]
    public async Task AddExpense_EmptyDescription_IsStoredAsNull()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var expense = ValidExpense();
        expense.Description = "   ";

        await _service.AddExpenseAsync(_trip, expense);

        _repository.Verify(r => r.AddAsync(It.Is<Expense>(e => e.Description == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Підрахунок ----------

    [Fact]
    public async Task GetOverview_CalculatesSpentRemainingAndPercent()
    {
        SetupExpenses(620m, 1400m, 10000m); // 12 020 з 15 000

        var overview = await _service.GetOverviewAsync(_trip);

        Assert.Equal(12020m, overview.Summary.Spent);
        Assert.Equal(2980m, overview.Summary.Remaining);
        Assert.Equal(80.13, overview.Summary.UsedPercent, 2);
        Assert.Equal(BudgetLevel.Warning, overview.Summary.Level);
    }

    [Fact]
    public async Task GetOverview_NoExpenses_IsNormalWithZeroSpent()
    {
        SetupExpenses();

        var overview = await _service.GetOverviewAsync(_trip);

        Assert.Equal(0m, overview.Summary.Spent);
        Assert.Equal(15000m, overview.Summary.Remaining);
        Assert.Equal(BudgetLevel.Normal, overview.Summary.Level);
    }

    [Fact]
    public async Task GetOverview_SpentMoreThanPlanned_IsOverBudget()
    {
        SetupExpenses(16000m, 2500m);

        var overview = await _service.GetOverviewAsync(_trip);

        Assert.Equal(-3500m, overview.Summary.Remaining);
        Assert.Equal(BudgetLevel.Over, overview.Summary.Level);
    }

    [Fact]
    public void Summary_ZeroPlannedBudgetWithExpenses_IsOverBudget()
    {
        var summary = BudgetService.Calculate(0m, new[] { new Expense { Amount = 100m } });

        Assert.Equal(100, summary.UsedPercent);
        Assert.Equal(BudgetLevel.Over, summary.Level);
    }

    [Fact]
    public async Task DeleteExpense_CallsRepository()
    {
        await _service.DeleteExpenseAsync(3);

        _repository.Verify(r => r.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Крайові випадки ----------

    [Fact]
    public void Summary_SpentExactlyWholeBudget_IsNotOverBudget()
    {
        // Витрачено рівно 100%: залишок 0, це ще не перевитрата
        var summary = BudgetService.Calculate(15000m, new[] { new Expense { Amount = 15000m } });

        Assert.Equal(0m, summary.Remaining);
        Assert.Equal(100, summary.UsedPercent);
        Assert.Equal(BudgetLevel.Warning, summary.Level);
    }

    [Fact]
    public void Summary_OneCentOverBudget_IsOverBudget()
    {
        var summary = BudgetService.Calculate(15000m, new[] { new Expense { Amount = 15000.01m } });

        Assert.Equal(-0.01m, summary.Remaining);
        Assert.Equal(BudgetLevel.Over, summary.Level);
    }

    [Theory]
    [InlineData(11999.99, BudgetLevel.Normal)]  // 79,99993% — ще зелений
    [InlineData(12000.00, BudgetLevel.Warning)] // рівно 80% — вже жовтий
    [InlineData(12000.01, BudgetLevel.Warning)]
    public void Summary_EightyPercentThreshold(double spent, BudgetLevel expected)
    {
        var summary = BudgetService.Calculate(15000m, new[] { new Expense { Amount = (decimal)spent } });

        Assert.Equal(expected, summary.Level);
    }

    [Fact]
    public async Task AddExpense_SmallestAmount_IsAccepted()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var expense = ValidExpense();
        expense.Amount = 0.01m;

        var result = await _service.AddExpenseAsync(_trip, expense);

        Assert.True(result.IsSuccess);
        Assert.Equal(14999.99m, BudgetService.Calculate(_trip.Budget, new[] { expense }).Remaining);
    }

    [Fact]
    public async Task AddExpense_VeryLargeAmount_IsAcceptedAndCalculatedWithoutOverflow()
    {
        // Найбільше значення, яке вміщує колонка DECIMAL(18,2) у таблиці Expenses
        const decimal max = 9_999_999_999_999_999.99m;
        _repository.Setup(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var expense = ValidExpense();
        expense.Amount = max;

        var result = await _service.AddExpenseAsync(_trip, expense);
        var summary = BudgetService.Calculate(_trip.Budget, new[] { expense, new Expense { Amount = max } });

        Assert.True(result.IsSuccess);
        Assert.Equal(2 * max, summary.Spent);
        Assert.Equal(BudgetLevel.Over, summary.Level);
    }

    [Fact]
    public async Task DeleteLastExpense_RemainingReturnsToFullBudget()
    {
        // «База» в пам'яті: одна витрата, яку видаляємо
        var expenses = new List<Expense> { new() { Id = 1, Amount = 620m } };
        _repository.Setup(r => r.GetByTripAsync(_trip.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(() => expenses.ToList());
        _repository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
                   .Callback(() => expenses.RemoveAll(e => e.Id == 1))
                   .Returns(Task.CompletedTask);

        var before = await _service.GetOverviewAsync(_trip);
        await _service.DeleteExpenseAsync(1);
        var after = await _service.GetOverviewAsync(_trip);

        Assert.Equal(14380m, before.Summary.Remaining);
        Assert.Empty(after.Expenses);
        Assert.Equal(0m, after.Summary.Spent);
        Assert.Equal(_trip.Budget, after.Summary.Remaining);
        Assert.Equal(BudgetLevel.Normal, after.Summary.Level);
    }
}
