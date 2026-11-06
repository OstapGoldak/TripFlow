using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>
/// Вкладка «Розклад»: дні поїздки ліворуч, події обраного дня праворуч (сценарій 3).
/// Дні генерує ItineraryService за датами поїздки, події приходять відсортованими за часом.
/// </summary>
public partial class ItineraryViewModel : ViewModelBase
{
    private readonly IItineraryService _itinerary;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    private Trip? _trip;
    private Task _activitiesLoad = Task.CompletedTask;

    public ItineraryViewModel(IItineraryService itinerary, IDialogService dialogs, ILogger log)
    {
        _itinerary = itinerary;
        _dialogs = dialogs;
        _log = log;
    }

    /// <summary>Дні поїздки (ліва колонка).</summary>
    public ObservableCollection<DayItemViewModel> Days { get; } = new();

    /// <summary>Події обраного дня (права частина), уже відсортовані за часом.</summary>
    public ObservableCollection<ActivityRowViewModel> Activities { get; } = new();

    public bool IsEmpty => Activities.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDayTitle))]
    private DayItemViewModel? _selectedDay;

    /// <summary>«День 1 · 15.07.2026» над списком подій.</summary>
    public string SelectedDayTitle => SelectedDay?.Title ?? string.Empty;

    /// <summary>Вибір іншого дня одразу завантажує його події.</summary>
    partial void OnSelectedDayChanged(DayItemViewModel? value) => _activitiesLoad = LoadActivitiesAsync();

    /// <summary>Генерує дні поїздки й показує перший. Викликається вікном поїздки при відкритті.</summary>
    public async Task LoadAsync(Trip trip)
    {
        _trip = trip;

        Days.Clear();
        var number = 1;
        foreach (var date in _itinerary.GetDays(trip))
        {
            Days.Add(new DayItemViewModel(date, number++));
        }

        SelectedDay = Days.FirstOrDefault();
        await _activitiesLoad;
    }

    private async Task LoadActivitiesAsync()
    {
        Activities.Clear();
        OnPropertyChanged(nameof(IsEmpty));

        if (_trip is null || SelectedDay is null)
        {
            return;
        }

        try
        {
            var activities = await _itinerary.GetActivitiesAsync(_trip.Id, SelectedDay.Date);
            foreach (var activity in activities)
            {
                Activities.Add(new ActivityRowViewModel(activity));
            }

            OnPropertyChanged(nameof(IsEmpty));
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося завантажити розклад поїздки {TripId} на {Date}", _trip.Id, SelectedDay.Date);
            ErrorMessage = "Не вдалося завантажити розклад: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddActivityAsync()
    {
        if (_trip is null || SelectedDay is null)
        {
            return;
        }

        var form = new ActivityFormViewModel(SelectedDay.Date);
        if (!_dialogs.ShowForm(form))
        {
            return;
        }

        var data = form.Result!;
        try
        {
            var result = await _itinerary.AddAsync(_trip, new Activity
            {
                Date = SelectedDay.Date,
                Time = data.Time,
                Title = data.Title,
                Location = data.Location,
                EstimatedCost = data.EstimatedCost,
                Note = data.Note
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            _log.Information("Подію додано до поїздки {TripId}: {Title}", _trip.Id, data.Title);
            await LoadActivitiesAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося зберегти подію поїздки {TripId}", _trip.Id);
            ErrorMessage = "Не вдалося зберегти подію: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task EditActivityAsync(ActivityRowViewModel row)
    {
        if (_trip is null)
        {
            return;
        }

        var form = new ActivityFormViewModel(row.Activity);
        if (!_dialogs.ShowForm(form))
        {
            return;
        }

        var data = form.Result!;
        try
        {
            var result = await _itinerary.UpdateAsync(_trip, new Activity
            {
                Id = row.Id,
                TripId = row.Activity.TripId,
                Date = row.Activity.Date,
                Time = data.Time,
                Title = data.Title,
                Location = data.Location,
                EstimatedCost = data.EstimatedCost,
                Note = data.Note
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            _log.Information("Подію {ActivityId} змінено", row.Id);
            await LoadActivitiesAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося змінити подію {ActivityId}", row.Id);
            ErrorMessage = "Не вдалося змінити подію: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteActivityAsync(ActivityRowViewModel row)
    {
        if (!_dialogs.Confirm($"Видалити подію «{row.Title}»?"))
        {
            return;
        }

        try
        {
            await _itinerary.DeleteAsync(row.Id);
            _log.Information("Подію {ActivityId} видалено", row.Id);
            await LoadActivitiesAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося видалити подію {ActivityId}", row.Id);
            ErrorMessage = "Не вдалося видалити подію: " + ex.Message;
        }
    }
}