using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Дані, які користувач ввів у формі події.</summary>
public sealed record ActivityFormData(TimeOnly Time, string Title, string? Location, decimal? EstimatedCost, string? Note);

/// <summary>
/// Сценарій 3: додавання або редагування події дня.
/// Форма перевіряє заповненість і формат; дату в межах поїздки й вартість ≥ 0 перевіряє ItineraryService.
/// </summary>
public partial class ActivityFormViewModel : FormViewModelBase
{
    private static readonly string[] TimeFormats = { "H:mm", "HH:mm" };

    private readonly bool _isEdit;

    /// <summary>Нова подія на обраний день.</summary>
    public ActivityFormViewModel(DateOnly date)
    {
        DateText = date.ToString("dd.MM.yyyy");
    }

    /// <summary>Редагування наявної події: поля заповнюються її даними.</summary>
    public ActivityFormViewModel(Activity activity) : this(activity.Date)
    {
        _isEdit = true;
        _timeText = activity.Time.ToString("HH:mm");
        _activityTitle = activity.Title;
        _location = activity.Location ?? string.Empty;
        _costText = activity.EstimatedCost?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        _note = activity.Note ?? string.Empty;
    }

    /// <summary>Заголовок вікна форми.</summary>
    public override string Title => _isEdit ? "Редагування події" : "Нова подія";

    /// <summary>День, до якого належить подія (лише для показу).</summary>
    public string DateText { get; }

    /// <summary>Заповнюється після успішного збереження.</summary>
    public ActivityFormData? Result { get; private set; }

    [ObservableProperty] private string _timeText = "09:00";
    [ObservableProperty] private string _activityTitle = string.Empty;
    [ObservableProperty] private string _location = string.Empty;
    [ObservableProperty] private string _costText = string.Empty;
    [ObservableProperty] private string _note = string.Empty;

    protected override bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ActivityTitle))
        {
            ErrorMessage = "Введіть назву події.";
            return false;
        }

        if (!TimeOnly.TryParseExact(TimeText.Trim(), TimeFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            ErrorMessage = "Введіть час у форматі ГГ:ХХ, наприклад 09:30.";
            return false;
        }

        decimal? cost = null;
        if (!string.IsNullOrWhiteSpace(CostText))
        {
            if (!Parsing.TryDecimal(CostText, out var value))
            {
                ErrorMessage = "Введіть вартість числом або залиште поле порожнім.";
                return false;
            }
            cost = value;
        }

        Result = new ActivityFormData(time, ActivityTitle, Location, cost, Note);
        return true;
    }
}
