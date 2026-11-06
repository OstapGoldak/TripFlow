using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TripFlow.BLL;
using TripFlow.DAL;
using TripFlow.Services;
using TripFlow.ViewModels;

namespace TripFlow;

public partial class App : Application
{
    private static readonly string LogsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private ServiceProvider? _provider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureLogging();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Необроблена помилка у фоновому потоці");

        try
        {
            Log.Information("Запуск TripFlow");
            _provider = ConfigureServices();

            // 1. Створити базу й таблицю Trips, якщо їх ще немає
            await _provider.GetRequiredService<DatabaseInitializer>().InitializeAsync();

            // 2. Показати вікно й завантажити поїздки: UI → BLL → DAL → LocalDB
            var window = _provider.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();

            await _provider.GetRequiredService<MainViewModel>().Overview.LoadAsync();
            Log.Information("TripFlow запущено");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Не вдалося запустити TripFlow");
            MessageBox.Show(
                "Не вдалося запустити TripFlow.\n\n" + ex.Message +
                "\n\nПеревірте, що встановлено SQL Server LocalDB, та рядок підключення в appsettings.json.",
                "TripFlow", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Завершення роботи TripFlow");
        _provider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>Глобальний обробник: записує помилку в журнал і не дає програмі закритися.</summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Необроблена помилка інтерфейсу");
        MessageBox.Show(
            "Сталася неочікувана помилка. Програма продовжить роботу.\n\nПодробиці записано в журнал:\n" + LogsDirectory,
            "TripFlow", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(LogsDirectory, "tripflow-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>Єдине місце створення об'єктів (Dependency Injection).</summary>
    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Логер
        services.AddSingleton<ILogger>(Log.Logger);

        // DAL
        services.AddSingleton(new SqlConnectionFactory(ReadConnectionString()));
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<ITripRepository, TripRepository>();
        services.AddSingleton<IPackingRepository, PackingRepository>();
        services.AddSingleton<IActivityRepository, ActivityRepository>();

        // BLL
        services.AddSingleton<ITripService, TripService>();
        services.AddSingleton<IPackingService, PackingService>();
        services.AddSingleton<IItineraryService, ItineraryService>();

        // UI
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<PackingViewModel>();
        services.AddSingleton<Func<PackingViewModel>>(sp => () => sp.GetRequiredService<PackingViewModel>());
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