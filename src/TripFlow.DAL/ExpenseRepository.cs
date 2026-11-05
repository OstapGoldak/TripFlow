using System.Data;
using Microsoft.Data.SqlClient;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.DAL;

/// <summary>Репозиторій витрат: параметризовані SQL-запити через ADO.NET, без бізнес-правил.</summary>
public class ExpenseRepository : IExpenseRepository
{
    private readonly SqlConnectionFactory _factory;

    public ExpenseRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Expense>> GetByTripAsync(int tripId, CancellationToken ct = default)
    {
        const string sql = @"SELECT Id, TripId, [Date], Amount, Category, Description
                             FROM dbo.Expenses
                             WHERE TripId = @TripId
                             ORDER BY [Date] DESC, Id DESC;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;

        await using var reader = await command.ExecuteReaderAsync(ct);
        var expenses = new List<Expense>();
        while (await reader.ReadAsync(ct))
        {
            expenses.Add(new Expense
            {
                Id = reader.GetInt32(0),
                TripId = reader.GetInt32(1),
                Date = DateOnly.FromDateTime(reader.GetDateTime(2)),
                Amount = reader.GetDecimal(3),
                Category = Enum.Parse<ExpenseCategory>(reader.GetString(4)),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return expenses;
    }

    public async Task<int> AddAsync(Expense expense, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.Expenses (TripId, [Date], Amount, Category, Description)
                             OUTPUT INSERTED.Id
                             VALUES (@TripId, @Date, @Amount, @Category, @Description);";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = expense.TripId;
        command.Parameters.Add("@Date", SqlDbType.Date).Value = expense.Date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add(new SqlParameter("@Amount", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = expense.Amount
        });
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = expense.Category.ToString();
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = (object?)expense.Description ?? DBNull.Value;

        var newId = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(newId);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.Expenses WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync(ct);
    }
}
