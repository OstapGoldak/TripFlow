using System.Data;
using Microsoft.Data.SqlClient;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.DAL;

/// <summary>
/// Зберігає імпортовану поїздку разом з активностями, витратами й речами однією транзакцією.
/// Параметризовані запити, без бізнес-правил: усі перевірки вже виконав DataTransferService.
/// </summary>
public class TripImportRepository : ITripImportRepository
{
    private readonly SqlConnectionFactory _factory;

    public TripImportRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<int> ImportTripAsync(
        Trip trip,
        IReadOnlyList<Activity> activities,
        IReadOnlyList<Expense> expenses,
        IReadOnlyList<PackingItem> packingItems,
        CancellationToken ct = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(ct);

        // Якщо під час запису виникне виняток, Commit не виконається, а Dispose транзакції відкотить усе.
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        var tripId = await InsertTripAsync(connection, transaction, trip, ct);

        foreach (var activity in activities)
        {
            await InsertActivityAsync(connection, transaction, tripId, activity, ct);
        }

        foreach (var expense in expenses)
        {
            await InsertExpenseAsync(connection, transaction, tripId, expense, ct);
        }

        foreach (var item in packingItems)
        {
            await InsertPackingItemAsync(connection, transaction, tripId, item, ct);
        }

        await transaction.CommitAsync(ct);
        return tripId;
    }

    private static async Task<int> InsertTripAsync(
        SqlConnection connection, SqlTransaction transaction, Trip trip, CancellationToken ct)
    {
        const string sql = @"INSERT INTO dbo.Trips (Name, Destination, StartDate, EndDate, Budget)
                             OUTPUT INSERTED.Id
                             VALUES (@Name, @Destination, @StartDate, @EndDate, @Budget);";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = trip.Name;
        command.Parameters.Add("@Destination", SqlDbType.NVarChar, 255).Value = trip.Destination;
        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = trip.StartDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = trip.EndDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add(Money("@Budget", trip.Budget));

        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private static async Task InsertActivityAsync(
        SqlConnection connection, SqlTransaction transaction, int tripId, Activity activity, CancellationToken ct)
    {
        const string sql = @"INSERT INTO dbo.Activities (TripId, [Date], [Time], Title, Location, EstimatedCost, Note)
                             VALUES (@TripId, @Date, @Time, @Title, @Location, @EstimatedCost, @Note);";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
        command.Parameters.Add("@Date", SqlDbType.Date).Value = activity.Date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@Time", SqlDbType.Time).Value = activity.Time.ToTimeSpan();
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = activity.Title;
        command.Parameters.Add("@Location", SqlDbType.NVarChar, 255).Value = (object?)activity.Location ?? DBNull.Value;
        command.Parameters.Add(Money("@EstimatedCost", activity.EstimatedCost));
        command.Parameters.Add("@Note", SqlDbType.NVarChar, 1000).Value = (object?)activity.Note ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertExpenseAsync(
        SqlConnection connection, SqlTransaction transaction, int tripId, Expense expense, CancellationToken ct)
    {
        const string sql = @"INSERT INTO dbo.Expenses (TripId, [Date], Amount, Category, Description)
                             VALUES (@TripId, @Date, @Amount, @Category, @Description);";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
        command.Parameters.Add("@Date", SqlDbType.Date).Value = expense.Date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add(Money("@Amount", expense.Amount));
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = expense.Category.ToString();
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = (object?)expense.Description ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertPackingItemAsync(
        SqlConnection connection, SqlTransaction transaction, int tripId, PackingItem item, CancellationToken ct)
    {
        const string sql = @"INSERT INTO dbo.PackingItems (TripId, Name, Category, IsPacked)
                             VALUES (@TripId, @Name, @Category, @IsPacked);";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = item.Name;
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = item.Category;
        command.Parameters.Add("@IsPacked", SqlDbType.Bit).Value = item.IsPacked;

        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Параметр DECIMAL(18,2); null записується як NULL.</summary>
    private static SqlParameter Money(string name, decimal? value) => new(name, SqlDbType.Decimal)
    {
        Precision = 18,
        Scale = 2,
        Value = (object?)value ?? DBNull.Value
    };
}
