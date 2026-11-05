using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TripFlow.BLL;
using TripFlow.DAL;
using TripFlow.Services;
using TripFlow.ViewModels;

namespace TripFlow;

public partial class App : Application
{
    private ServiceProvider? _provider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _provider = ConfigureServices();

            // 1. Створити базу й таблицю Trips, якщо їх ще немає
            await _provider.GetRequiredService<DatabaseInitializer>().InitializeAsync();

            // 2. Показати вікно й завантажити поїздки: UI → BLL → DAL → LocalDB
            var window = _provider.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();

            await _provider.GetRequiredService<MainViewModel>().Overview.LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Не вдалося запустити TripFlow.\n\n" + ex.Message +
                "\n\nПеревірте, що встановлено SQL Server LocalDB, та рядок підключення в appsettings.json.",
                "TripFlow", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Єдине місце створення об'єктів (Dependency Injection).</summary>
    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // DAL
        services.AddSingleton(new SqlConnectionFactory(ReadConnectionString()));
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<ITripRepository, TripRepository>();

        // BLL
        services.AddSingleton<ITripService, TripService>();

        // UI
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<TripsOverviewViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private static string ReadConnectionString()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("ConnectionStrings").GetProperty("TripFlow").GetString()!;
    }
}
