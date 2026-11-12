using TripFlow.Models;
using TripFlow.BLL;
namespace TripFlow.ViewModels;

/// <summary>Картка поїздки у списку (лише відображення). Статус обчислює TripService.</summary>
public sealed class TripCardViewModel
{
    public TripCardViewModel(Trip trip, TripStatus status, int packedCount = 0, int totalCount = 0)
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

        // Пакування рахується з чек-листа; витрати з'являться разом з бюджетом
        
        PackingPercent = PackingService.CalculateProgress(packedCount, totalCount).Percent;
        PackingText = totalCount == 0 ? "0%" : $"{packedCount} з {totalCount} ({PackingPercent:0}%)";
        BudgetText = $"0 ₴ / {trip.Budget:N0} ₴";
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
    public bool IsOverBudget => false;
}
