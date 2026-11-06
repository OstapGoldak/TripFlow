namespace TripFlow.Models;

public class PackingItem
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public bool IsPacked { get; set; }
}

/// <summary>Допустимі категорії (збігаються з CHECK у таблиці PackingItems) та їхні назви для інтерфейсу.</summary>
public static class PackingCategories
{
    public static readonly (string Value, string Title)[] All =
    {
        ("Documents", "Документи"),
        ("Clothing", "Одяг"),
        ("Electronics", "Електроніка"),
        ("FirstAid", "Аптечка"),
        ("Other", "Інше")
    };

    public static bool IsValid(string? value) => All.Any(c => c.Value == value);

    public static string GetTitle(string value) =>
        All.FirstOrDefault(c => c.Value == value).Title ?? value;
}