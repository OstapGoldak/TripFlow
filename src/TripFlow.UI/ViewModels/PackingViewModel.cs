using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TripFlow.BLL;
using TripFlow.Models;
using TripFlow.Services;

namespace TripFlow.ViewModels;

/// <summary>Вкладка «Чек-лист» поїздки: додавання, галочка «спаковано», прогрес, видалення.</summary>
public partial class PackingViewModel : ViewModelBase
{
    private int _tripId;
    private readonly IPackingService _service;
    private readonly IDialogService _dialogs;
    private readonly ILogger _log;

    public PackingViewModel(IPackingService service, IDialogService dialogs, ILogger log)
    {
        _service = service;
        _dialogs = dialogs;
        _log = log;

        foreach (var (value, title) in PackingCategories.All)
        {
            Categories.Add(new PackingCategoryOption(value, title, Categories.Count == 0));
        }
    }

    public ObservableCollection<PackingItemViewModel> Items { get; } = new();
    public ObservableCollection<PackingCategoryOption> Categories { get; } = new();
    public ObservableCollection<PackingGroupViewModel> Groups { get; } = new();

    [ObservableProperty]
    private string _newItemName = string.Empty;

    public int TotalCount => Items.Count;
    public int PackedCount => Items.Count(i => i.IsPacked);
    public double ProgressPercent => TotalCount == 0 ? 0 : (double)PackedCount / TotalCount * 100;
    public string ProgressText => $"Спаковано {PackedCount} з {TotalCount}";
    public bool IsEmpty => Items.Count == 0;

    private void RefreshStats()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PackedCount));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(IsEmpty));

        foreach (var group in Groups)
        {
            group.Refresh();
        }
    }

    /// <summary>Викликається, коли відкрито поїздку: запам'ятовує її й завантажує список речей.</summary>
    public async Task LoadAsync(Trip trip)
    {
        _tripId = trip.Id;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            var items = await _service.GetByTripAsync(_tripId);

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new PackingItemViewModel(item, OnItemToggledAsync));
            }

            Groups.Clear();
            foreach (var (value, title) in PackingCategories.All)
            {
                var inCategory = Items.Where(i => i.Category == value).ToList();
                if (inCategory.Count > 0)
                {
                    Groups.Add(new PackingGroupViewModel(title, inCategory));
                }
            }

            RefreshStats();
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося завантажити чек-лист поїздки {TripId}", _tripId);
            ErrorMessage = "Не вдалося завантажити список речей: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var name = NewItemName.Trim();
        if (name.Length == 0)
        {
            ErrorMessage = "Введіть назву речі.";
            return;
        }

        var category = Categories.FirstOrDefault(c => c.IsSelected)?.Value ?? "Other";

        try
        {
            await _service.AddAsync(new PackingItem
            {
                TripId = _tripId,
                Name = name,
                Category = category
            });
            _log.Information("Додано річ «{Name}» до поїздки {TripId}", name, _tripId);
            NewItemName = string.Empty;
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося додати річ {Name}", name);
            ErrorMessage = "Не вдалося додати річ: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(PackingItemViewModel item)
    {
        if (!_dialogs.Confirm($"Видалити «{item.Name}» зі списку?"))
        {
            return;
        }

        try
        {
            await _service.DeleteAsync(item.Id);
            _log.Information("Видалено річ {ItemId}", item.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося видалити річ {ItemId}", item.Id);
            ErrorMessage = "Не вдалося видалити річ: " + ex.Message;
        }
    }

    /// <summary>Викликається при зміні галочки: оновлює прогрес і зберігає в базу.</summary>
    private async Task OnItemToggledAsync(PackingItemViewModel item)
    {
        RefreshStats();
        try
        {
            await _service.SetPackedAsync(item.Id, item.IsPacked);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не вдалося зберегти стан речі {ItemId}", item.Id);
            await ReloadAsync();
            ErrorMessage = "Не вдалося зберегти зміну: " + ex.Message;
        }
    }
}