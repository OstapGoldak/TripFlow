namespace TripFlow.ViewModels;

/// <summary>Один день поїздки в лівому списку вкладки «Розклад» (лише відображення).</summary>
public sealed class DayItemViewModel
{
    public DayItemViewModel(DateOnly date, int number)
    {
        Date = date;
        Number = number;
    }

    public DateOnly Date { get; }

    /// <summary>Порядковий номер дня, починаючи з 1.</summary>
    public int Number { get; }

    /// <summary>У списку днів: «День 1: 15.07».</summary>
    public string Text => $"День {Number}: {Date:dd.MM}";

    /// <summary>Заголовок над подіями: «День 1 · 15.07.2026».</summary>
    public string Title => $"День {Number} · {Date:dd.MM.yyyy}";
}
