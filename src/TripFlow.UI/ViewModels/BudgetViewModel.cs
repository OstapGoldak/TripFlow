using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>Пункт фільтра за категорією у вкладці «Бюджет». Value == null означає «Усі категорії».</summary>
public sealed record CategoryFilterOption(ExpenseCategory? Value, string Text);

/// <summary>Вкладка «Бюджет»: підсумок, ProgressBar, список витрат, додавання й видалення.</summary>
public partial class BudgetViewModel : ViewModelBase
{
    private readonly IBudgetService _budget;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    private Trip? _trip;
    private List<ExpenseRowViewModel> _all = new();

    public BudgetViewModel(IBudgetService budget, IDialogService dialogs, ILogger log)
    {
        _budget = budget;
        _dialogs = dialogs;
        _log = log;

        Filters = new[] { new CategoryFilterOption(null, "Усі категорії") }
            .Concat(ExpenseCategoryNames.Options.Select(o => new CategoryFilterOption(o.Value, o.Text)))
            .ToList();
        _selectedFilter = Filters[0];
    }

    public IReadOnlyList<CategoryFilterOption> Filters { get; }

    public ObservableCollection<ExpenseRowViewModel> Expenses { get; } = new();

    public bool IsEmpty => Expenses.Count == 0;

    [ObservableProperty] private CategoryFilterOption _selectedFilter;
    [ObservableProperty] private string _plannedText = MoneyFormat.Format(0);
    [ObservableProperty] private string _spentText = MoneyFormat.Format(0);
    [ObservableProperty] private string _remainingText = MoneyFormat.Format(0);
    [ObservableProperty] private string _usedPercentText = "0%";

    /// <summary>Значення для ProgressBar (0–100).</summary>
    [ObservableProperty] private double _usedPercent;

    /// <summary>Стан бюджету; за ним конвертер вибирає колір ProgressBar (червоний при перевитраті).</summary>
    [ObservableProperty] private BudgetLevel _level;

    partial void OnSelectedFilterChanged(CategoryFilterOption value) => ApplyFilter();

    /// <summary>Завантажує витрати вказаної поїздки. Викликається вікном поїздки при відкритті вкладки.</summary>
    public async Task LoadAsync(Trip trip)
    {
        _trip = trip;
        try
        {
            var overview = await _budget.GetOverviewAsync(trip);

            _all = overview.Expenses.Select(e => new ExpenseRowViewModel(e)).ToList();

            var summary = overview.Summary;
            PlannedText = MoneyFormat.Format(summary.Planned);
            SpentText = MoneyFormat.Format(summary.Spent);
            RemainingText = MoneyFormat.Format(summary.Remaining);
            UsedPercent = Math.Min(summary.UsedPercent, 100);
            UsedPercentText = $"{Math.Round(summary.UsedPercent):0}%";
            Level = summary.Level;

            ApplyFilter();
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося завантажити бюджет поїздки {TripId}", trip.Id);
            ErrorMessage = "Не вдалося завантажити витрати: " + ex.Message;
        }
    }

    private void ApplyFilter()
    {
        var category = SelectedFilter.Value;

        Expenses.Clear();
        foreach (var row in _all.Where(r => category is null || r.Category == category))
        {
            Expenses.Add(row);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (_trip is null)
        {
            return;
        }

        var form = new ExpenseFormViewModel();
        if (!_dialogs.ShowForm(form))
        {
            return;
        }

        var data = form.Result!;
        try
        {
            var result = await _budget.AddExpenseAsync(_trip, new Expense
            {
                Date = data.Date,
                Amount = data.Amount,
                Category = data.Category,
                Description = data.Description
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            _log.Information("Витрату додано до поїздки {TripId}: {Amount}", _trip.Id, data.Amount);
            await LoadAsync(_trip);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося зберегти витрату поїздки {TripId}", _trip.Id);
            ErrorMessage = "Не вдалося зберегти витрату: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(ExpenseRowViewModel row)
    {
        if (_trip is null || !_dialogs.Confirm("Видалити цю витрату?"))
        {
            return;
        }

        try
        {
            await _budget.DeleteExpenseAsync(row.Id);
            _log.Information("Витрату {ExpenseId} видалено", row.Id);
            await LoadAsync(_trip);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося видалити витрату {ExpenseId}", row.Id);
            ErrorMessage = "Не вдалося видалити витрату: " + ex.Message;
        }
    }
}
