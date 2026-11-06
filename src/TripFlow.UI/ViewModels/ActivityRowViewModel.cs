using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Картка події в розкладі дня (лише відображення).</summary>
public sealed class ActivityRowViewModel
{
    public ActivityRowViewModel(Activity activity)
    {
        Activity = activity;
        TimeText = activity.Time.ToString("HH:mm");
        Title = activity.Title;

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(activity.Location))
        {
            details.Add("Локація: " + activity.Location);
        }
        if (!string.IsNullOrWhiteSpace(activity.Note))
        {
            details.Add(activity.Note);
        }
        DetailsText = string.Join(" · ", details);

        CostText = activity.EstimatedCost is { } cost ? MoneyFormat.Format(cost) : string.Empty;
    }

    /// <summary>Сама подія — потрібна, щоб відкрити її у формі редагування.</summary>
    public Activity Activity { get; }

    public int Id => Activity.Id;
    public string TimeText { get; }
    public string Title { get; }

    /// <summary>«Локація: Яремче · Бронь підтверджено» (порожньо, якщо нічого не вказано).</summary>
    public string DetailsText { get; }
    public bool HasDetails => DetailsText.Length > 0;

    /// <summary>«850 ₴» або порожньо, якщо вартість не вказана.</summary>
    public string CostText { get; }
}