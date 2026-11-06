using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TripFlow.ViewModels;

/// <summary>Група речей однієї категорії з власним прогресом.</summary>
public class PackingGroupViewModel : ObservableObject
{
    public PackingGroupViewModel(string title, IEnumerable<PackingItemViewModel> items)
    {
        Title = title;
        Items = new ObservableCollection<PackingItemViewModel>(items);
    }

    public string Title { get; }

    public ObservableCollection<PackingItemViewModel> Items { get; }

    public int PackedCount => Items.Count(i => i.IsPacked);
    public double ProgressPercent => Items.Count == 0 ? 0 : (double)PackedCount / Items.Count * 100;
    public string ProgressText => $"{PackedCount} з {Items.Count}";

    public void Refresh()
    {
        OnPropertyChanged(nameof(PackedCount));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
    }
}