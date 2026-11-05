using System.Data;
using Microsoft.Data.SqlClient;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.DAL;

/// <summary>Репозиторій активностей: параметризовані SQL-запити через ADO.NET, без бізнес-правил.</summary>
public class ActivityRepository : IActivityRepository
{
    private const string SelectColumns =
        "SELECT Id, TripId, [Date], [Time], Title, Location, EstimatedCost, Note FROM dbo.Activities";

    private readonly SqlConnectionFactory _factory;

    public ActivityRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Activity>> GetByDayAsync(int tripId, DateOnly date, CancellationToken ct = default)
    {
        const string sql = SelectColumns + @"
                             WHERE TripId = @TripId AND [Date] = @Date
                             ORDER BY [Time], Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
        command.Parameters.Add("@Date", SqlDbType.Date).Value = date.ToDateTime(TimeOnly.MinValue);

        await using var reader = await command.ExecuteReaderAsync(ct);
        var activities = new List<Activity>();
        while (await reader.ReadAsync(ct))
        {
            activities.Add(Map(reader));
        }

        return activities;
    }

    public async Task<Activity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = SelectColumns + " WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<int> AddAsync(Activity activity, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.Activities (TripId, [Date], [Time], Title, Location, EstimatedCost, Note)
                             OUTPUT INSERTED.Id
                             VALUES (@TripId, @Date, @Time, @Title, @Location, @EstimatedCost, @Note);";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = activity.TripId;
        AddValueParameters(command, activity);

        var newId = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(newId);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken ct = default)
    {
        const string sql = @"UPDATE dbo.Activities
                             SET [Date] = @Date, [Time] = @Time, Title = @Title,
                                 Location = @Location, EstimatedCost = @EstimatedCost, Note = @Note
                             WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = activity.Id;
        AddValueParameters(command, activity);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.Activities WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<(DateOnly First, DateOnly Last)?> GetDateRangeAsync(int tripId, CancellationToken ct = default)
    {
        const string sql = "SELECT MIN([Date]), MAX([Date]) FROM dbo.Activities WHERE TripId = @TripId;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || reader.IsDBNull(0))
        {
            return null; // у поїздки немає активностей
        }

        return (DateOnly.FromDateTime(reader.GetDateTime(0)), DateOnly.FromDateTime(reader.GetDateTime(1)));
    }

    /// <summary>Спільні параметри для INSERT і UPDATE.</summary>
    private static void AddValueParameters(SqlCommand command, Activity activity)
    {
        command.Parameters.Add("@Date", SqlDbType.Date).Value = activity.Date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@Time", SqlDbType.Time).Value = activity.Time.ToTimeSpan();
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = activity.Title;
        command.Parameters.Add("@Location", SqlDbType.NVarChar, 255).Value = (object?)activity.Location ?? DBNull.Value;
        command.Parameters.Add(new SqlParameter("@EstimatedCost", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = (object?)activity.EstimatedCost ?? DBNull.Value
        });
        command.Parameters.Add("@Note", SqlDbType.NVarChar, 1000).Value = (object?)activity.Note ?? DBNull.Value;
    }

    /// <summary>Перетворює рядок таблиці на об'єкт Activity.</summary>
    private static Activity Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        TripId = reader.GetInt32(1),
        Date = DateOnly.FromDateTime(reader.GetDateTime(2)),
        Time = TimeOnly.FromTimeSpan(reader.GetTimeSpan(3)),
        Title = reader.GetString(4),
        Location = reader.IsDBNull(5) ? null : reader.GetString(5),
        EstimatedCost = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
        Note = reader.IsDBNull(7) ? null : reader.GetString(7)
    };
}