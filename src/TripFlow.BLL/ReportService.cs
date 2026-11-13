using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>
/// Дані для PDF-звіту поїздки. Збирає їх через наявні сервіси розкладу, бюджету й чек-листа,
/// тому всі підрахунки (залишок, відсоток, прогрес зборів) ті самі, що й у вкладках.
/// </summary>
public class ReportService : IReportService
{
    private readonly IItineraryService _itinerary;
    private readonly IBudgetService _budget;
    private readonly IPackingService _packing;

    public ReportService(IItineraryService itinerary, IBudgetService budget, IPackingService packing)
    {
        _itinerary = itinerary;
        _budget = budget;
        _packing = packing;
    }

    public async Task<TripReport> BuildAsync(Trip trip, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var days = new List<ReportDay>();
        var number = 1;
        foreach (var date in _itinerary.GetDays(trip))
        {
            var activities = await _itinerary.GetActivitiesAsync(trip.Id, date, ct);
            days.Add(new ReportDay(number++, date, activities));
        }

        var budget = await _budget.GetOverviewAsync(trip, ct);

        var items = await _packing.GetByTripAsync(trip.Id, ct);
        var groups = PackingCategories.All
            .Select(c => new ReportPackingGroup(c.Title, items.Where(i => i.Category == c.Value).ToList()))
            .Where(g => g.Items.Count > 0)
            .ToList();

        return new TripReport(trip, days, budget, groups, PackingService.CalculateProgress(items));
    }
}
