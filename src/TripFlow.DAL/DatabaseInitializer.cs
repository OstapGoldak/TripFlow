using Microsoft.Data.SqlClient;

namespace TripFlow.DAL;

/// <summary>Створює базу LocalDB і таблиці при першому запуску.</summary>
public class DatabaseInitializer
{
    private readonly SqlConnectionFactory _factory;

    public DatabaseInitializer(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await EnsureDatabaseAsync(ct);
        await ApplySchemaAsync(ct);
    }

    private async Task EnsureDatabaseAsync(CancellationToken ct)
    {
        // База ще може не існувати, тому спершу підключаємось до master
        var builder = new SqlConnectionStringBuilder(_factory.ConnectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        // QUOTENAME не можна викликати всередині EXEC(...), тому команду збираємо у змінну
        command.CommandText = @"DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@name);
                                IF DB_ID(@name) IS NULL EXEC(@sql);";
        command.Parameters.AddWithValue("@name", databaseName);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ApplySchemaAsync(CancellationToken ct)
    {
        var script = ReadEmbeddedScript("Schema.sql");

        await using var connection = await _factory.CreateOpenConnectionAsync(ct);
        await using var command = new SqlCommand(script, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Читає SQL-скрипт, вбудований у збірку DAL (Scripts/Schema.sql).</summary>
    private static string ReadEmbeddedScript(string fileName)
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
