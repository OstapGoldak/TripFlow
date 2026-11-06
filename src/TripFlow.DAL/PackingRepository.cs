using System.Data;
using Microsoft.Data.SqlClient;
using TripFlow.BLL;
using TripFlow.Models;

namespace TripFlow.DAL;

/// <summary>Репозиторій чек-листа: параметризовані SQL-запити через ADO.NET.</summary>
public class PackingRepository : IPackingRepository
{
    private readonly SqlConnectionFactory _factory;

    public PackingRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<PackingItem>> GetByTripAsync(int tripId, CancellationToken ct = default)
    {
        const string sql = @"SELECT Id, TripId, Name, Category, IsPacked
                             FROM dbo.PackingItems
                             WHERE TripId = @TripId
                             ORDER BY Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = tripId;
        await using var reader = await command.ExecuteReaderAsync(ct);

        var items = new List<PackingItem>();
        while (await reader.ReadAsync(ct))
        {
            items.Add(new PackingItem
            {
                Id = reader.GetInt32(0),
                TripId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Category = reader.GetString(3),
                IsPacked = reader.GetBoolean(4)
            });
        }

        return items;
    }

    public async Task<int> AddAsync(PackingItem item, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.PackingItems (TripId, Name, Category, IsPacked)
                             OUTPUT INSERTED.Id
                             VALUES (@TripId, @Name, @Category, @IsPacked);";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TripId", SqlDbType.Int).Value = item.TripId;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = item.Name;
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = item.Category;
        command.Parameters.Add("@IsPacked", SqlDbType.Bit).Value = item.IsPacked;

        var newId = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(newId);
    }

    public async Task SetPackedAsync(int id, bool isPacked, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.PackingItems SET IsPacked = @IsPacked WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IsPacked", SqlDbType.Bit).Value = isPacked;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.PackingItems WHERE Id = @Id;";

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync(ct);
    }
}