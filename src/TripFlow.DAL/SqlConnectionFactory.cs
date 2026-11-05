using Microsoft.Data.SqlClient;

namespace TripFlow.DAL;

/// <summary>Єдине місце, де створюються підключення до бази.</summary>
public class SqlConnectionFactory
{
    public SqlConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
