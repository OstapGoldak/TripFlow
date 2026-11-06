namespace TripFlow.Models;

/// <summary>Витрата в поїздці (рядок таблиці dbo.Expenses).</summary>
public class Expense
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? Description { get; set; }
}
