using Microsoft.Data.SqlClient;
using TripFlow.DAL;

namespace TripFlow.Tests.Integration;

/// <summary>
/// Окрема тимчасова база LocalDB для інтеграційних тестів.
/// Перед тестами створює базу з унікальною назвою і застосовує ту саму схему, що й застосунок
/// (DatabaseInitializer + Schema.sql); після тестів видаляє її. Робоча база TripFlow не зачіпається.
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    /// <summary>Сервер за замовчуванням. Інший можна задати змінною середовища TRIPFLOW_TEST_SQL_SERVER.</summary>
    private const string DefaultServer = @"(localdb)\MSSQLLocalDB";

    public LocalDbFixture()
    {
        var server = Environment.GetEnvironmentVariable("TRIPFLOW_TEST_SQL_SERVER") ?? DefaultServer;

        // Назву генеруємо самі (лише латиниця й цифри), тому її безпечно підставляти в DROP DATABASE нижче
        DatabaseName = "TripFlow_Tests_" + Guid.NewGuid().ToString("N")[..8];

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        };
        Factory = new SqlConnectionFactory(builder.ConnectionString);
    }

    public string DatabaseName { get; }

    public SqlConnectionFactory Factory { get; }

    /// <summary>Створює базу й таблиці тим самим кодом, що й застосунок під час запуску.</summary>
    public Task InitializeAsync() => new DatabaseInitializer(Factory).InitializeAsync();

    /// <summary>Видаляє тимчасову базу разом з її файлами.</summary>
    public async Task DisposeAsync()
    {
        // Пул тримає відкриті підключення до тестової бази, без цього DROP не виконається
        SqlConnection.ClearAllPools();

        var builder = new SqlConnectionStringBuilder(Factory.ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"DROP DATABASE [{DatabaseName}];";
        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>Усі інтеграційні тести використовують одну тимчасову базу й виконуються послідовно.</summary>
[CollectionDefinition(Name)]
public sealed class LocalDbCollection : ICollectionFixture<LocalDbFixture>
{
    public const string Name = "LocalDB";
}
