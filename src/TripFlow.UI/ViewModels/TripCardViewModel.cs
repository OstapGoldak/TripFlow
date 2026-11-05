using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Картка поїздки у списку (лише відображення).</summary>
public sealed class TripCardViewModel
{
    public TripCardViewModel(Trip trip, int packedCount = 0, int totalCount = 0)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        Id = trip.Id;
        Name = trip.Name;
        Status = today < trip.StartDate ? "Planned" : today > trip.EndDate ? "Completed" : "InProgress";
        StatusText = Status switch
        {
            "Completed" => "ЗАВЕРШЕНО",
            "InProgress" => "У ПРОЦЕСІ",
            _ => "ЗАПЛАНОВАНО"
        };
        DatesText = $"{trip.StartDate:dd.MM.yyyy} – {trip.EndDate:dd.MM.yyyy}";

        // Пакування рахується з чек-листа; витрати з'являться разом з бюджетом
        PackingPercent = totalCount == 0 ? 0 : (double)packedCount / totalCount * 100;
        PackingText = totalCount == 0 ? "0%" : $"{packedCount} з {totalCount} ({PackingPercent:0}%)";
        BudgetText = $"0 ₴ / {trip.Budget:N0} ₴";
    }

    public int Id { get; }
    public string Name { get; }

    /// <summary>"Planned", "InProgress" або "Completed" (використовується стилем бейджа).</summary>
    public string Status { get; }
    public string StatusText { get; }
    public string DatesText { get; }
    public double PackingPercent { get; }
    public string PackingText { get; }
    public string BudgetText { get; }
    public bool IsOverBudget => false;
}
