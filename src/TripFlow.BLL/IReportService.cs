using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>Збирає дані поїздки для PDF-звіту: розклад, бюджет і чек-лист.</summary>
public interface IReportService
{
    Task<TripReport> BuildAsync(Trip trip, CancellationToken ct = default);
}
