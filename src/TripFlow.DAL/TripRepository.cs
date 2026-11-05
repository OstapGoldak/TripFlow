using System.Data;
using Microsoft.Data.SqlClient;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.DAL;

/// <summary>Репозиторій поїздок: параметризовані SQL-запити через ADO.NET, без бізнес-правил.</summary>
public class TripRepository : ITripRepository
{
    private readonly SqlConnectionFactory _factory;

    public TripRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT Id, Name, Destination, StartDate, EndDate, Budget
                             FROM dbo.Trips
                             ORDER BY StartDate, Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var trips = new List<Trip>();
        while (await reader.ReadAsync(ct))
        {
            trips.Add(new Trip
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Destination = reader.GetString(2),
                StartDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
                EndDate = DateOnly.FromDateTime(reader.GetDateTime(4)),
                Budget = reader.GetDecimal(5)
            });
        }

        return trips;
    }

    public async Task<int> AddAsync(Trip trip, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.Trips (Name, Destination, StartDate, EndDate, Budget)
                             OUTPUT INSERTED.Id
                             VALUES (@Name, @Destination, @StartDate, @EndDate, @Budget);";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = trip.Name;
        command.Parameters.Add("@Destination", SqlDbType.NVarChar, 255).Value = trip.Destination;
        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = trip.StartDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = trip.EndDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add(new SqlParameter("@Budget", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = trip.Budget
        });

        var newId = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(newId);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.Trips WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync(ct);
    }
}
