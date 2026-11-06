using CommunityToolkit.Mvvm.ComponentModel;
using TripFlow.Models;

namespace TripFlow.ViewModels;

/// <summary>Одна річ у списку. Зміна галочки одразу викликає збереження в базу.</summary>
public class PackingItemViewModel : ObservableObject
{
    private readonly Func<PackingItemViewModel, Task> _onToggled;
    private bool _isPacked;

    public PackingItemViewModel(PackingItem item, Func<PackingItemViewModel, Task> onToggled)
    {
        Id = item.Id;
        Name = item.Name;
        Category = item.Category;
        _isPacked = item.IsPacked;
        _onToggled = onToggled;
    }

    public int Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string CategoryTitle => PackingCategories.GetTitle(Category);

    public bool IsPacked
    {
        get => _isPacked;
        set
        {
            if (SetProperty(ref _isPacked, value))
            {
                _ = _onToggled(this);
            }
        }
    }
}

/// <summary>Варіант категорії для вибору при додаванні речі.</summary>
public partial class PackingCategoryOption : ObservableObject
{
    public PackingCategoryOption(string value, string title, bool isSelected)
    {
        Value = value;
        Title = title;
        IsSelected = isSelected;
    }

    public string Value { get; }
    public string Title { get; }

    [ObservableProperty]
    private bool _isSelected;
}