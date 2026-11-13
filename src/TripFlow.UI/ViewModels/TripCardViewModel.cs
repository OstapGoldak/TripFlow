using TripFlow.Models;
using TripFlow.BLL;
namespace TripFlow.ViewModels;

/// <summary>
/// Картка поїздки у списку (лише відображення).
/// Статус обчислює TripService, прогрес зборів — PackingService, витрачену суму — BudgetService.
/// </summary>
public sealed class TripCardViewModel
{
    public TripCardViewModel(Trip trip, TripStatus status, int packedCount = 0, int totalCount = 0,
        BudgetSummary? budget = null)
    {
        Trip = trip;
        Id = trip.Id;
        Name = trip.Name;

        StatusValue = status;
        Status = status.ToString();
        StatusText = status switch
        {
            TripStatus.Completed => "ЗАВЕРШЕНО",
            TripStatus.InProgress => "У ПРОЦЕСІ",
            _ => "ЗАПЛАНОВАНО"
        };
        DatesText = $"{trip.StartDate:dd.MM.yyyy} – {trip.EndDate:dd.MM.yyyy}";

        // Пакування рахується з чек-листа
        PackingPercent = PackingService.CalculateProgress(packedCount, totalCount).Percent;
        PackingText = totalCount == 0 ? "0%" : $"{packedCount} з {totalCount} ({PackingPercent:0}%)";

        // Бюджет: витрачено / заплановано; без даних про витрати вважаємо, що витрат немає
        var summary = budget ?? new BudgetSummary(trip.Budget, 0m);
        // Копійки показуємо лише тоді, коли вони є: 10 000 ₴, але 20 000,01 ₴
        BudgetText = $"{summary.Spent:#,0.##} ₴ / {summary.Planned:#,0.##} ₴";
        IsOverBudget = summary.Level == BudgetLevel.Over;
    }

    /// <summary>Поїздка, яку відкриває кнопка «Відкрити».</summary>
    public Trip Trip { get; }

    public int Id { get; }
    public string Name { get; }

    /// <summary>Статус для логіки (фільтр).</summary>
    public TripStatus StatusValue { get; }

    /// <summary>"Planned", "InProgress" або "Completed": за цим рядком стиль бейджа обирає колір (Styles.xaml).</summary>
    public string Status { get; }
    public string StatusText { get; }
    public string DatesText { get; }
    public double PackingPercent { get; }
    public string PackingText { get; }
    public string BudgetText { get; }

    /// <summary>Перевитрата: сума бюджету на картці стає червоною.</summary>
    public bool IsOverBudget { get; }
}
