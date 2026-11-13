using TripFlow.Models;

namespace TripFlow.BLL;

/// <summary>День розкладу у звіті: номер, дата й події, відсортовані за часом.</summary>
public sealed record ReportDay(int Number, DateOnly Date, IReadOnlyList<Activity> Activities);

/// <summary>Категорія чек-листа у звіті: назва для людини й речі цієї категорії.</summary>
public sealed record ReportPackingGroup(string Title, IReadOnlyList<PackingItem> Items);

/// <summary>Усе, що потрібно для PDF-звіту поїздки. Як намалювати звіт, вирішує інтерфейс.</summary>
public sealed record TripReport(
    Trip Trip,
    IReadOnlyList<ReportDay> Days,
    BudgetOverview Budget,
    IReadOnlyList<ReportPackingGroup> PackingGroups,
    PackingProgress Packing);
