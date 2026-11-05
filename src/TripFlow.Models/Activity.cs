namespace TripFlow.Models;

/// <summary></summary>
public class Activity
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? Note { get; set; }
}