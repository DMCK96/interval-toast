using IntervalToast.Application.Common;
using IntervalToast.Infrastructure.Configuration;
using IntervalToast.Presentation.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace IntervalToast;

/// <summary>
/// Modern WPF application with dependency injection and clean architecture
/// </summary>
public partial class App : System.Windows.Application
{
    private IApplicationHost? _applicationHost;
    private ILogger<App>? _logger;

    /// <summary>
    /// Gets the current application host
    /// </summary>
    public static new IApplicationHost? Current => ((App)System.Windows.Application.Current)?._applicationHost;

    /// <summary>
    /// Application startup with dependency injection initialization
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Setup global exception handling first
            SetupExceptionHandling();

            // Create and configure the host
            var host = CreateHost(e.Args);
            _applicationHost = new ApplicationHost(host);
            _logger = _applicationHost.GetRequiredService<ILogger<App>>();

            // Start the host
            await _applicationHost.StartAsync();

            _logger.LogInformation("IntervalToast application started successfully");

            // Continue with WPF startup
            base.OnStartup(e);

            // Create and show main window using DI
            var mainWindow = _applicationHost.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;

            // Handle startup arguments
            HandleStartupArguments(e.Args);
        }
        catch (Exception ex)
        {
            HandleStartupError(ex);
        }
    }

    /// <summary>
    /// Application shutdown with proper cleanup
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("IntervalToast application shutting down");

            if (_applicationHost != null)
            {
                await _applicationHost.StopAsync();
                _applicationHost.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during application shutdown");
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Creates and configures the application host
    /// </summary>
    private static IHost CreateHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add application services
                services.AddIntervalToastServices();
                services.AddPresentationServices();

                // Add hosted services for background tasks
                services.AddHostedService<NotificationSchedulerService>();
            })
            .UseConsoleLifetime() // For hosted services
            .Build();
    }

    /// <summary>
    /// Handles command line arguments
    /// </summary>
    private void HandleStartupArguments(string[] args)
    {
        var shouldMinimize = args.Contains("--minimized") || args.Contains("/m");
        var shouldValidate = args.Contains("--validate-hotkeys") || args.Contains("/validate");

        if (shouldValidate)
        {
            // Run validation mode (implementation needed)
            _logger?.LogInformation("Running in validation mode");
            // TODO: Implement validation mode
            return;
        }

        if (shouldMinimize)
        {
            MainWindow.WindowState = WindowState.Minimized;
        }
        else
        {
            MainWindow.Show();
        }
    }

    /// <summary>
    /// Handles startup errors
    /// </summary>
    private void HandleStartupError(Exception ex)
    {
        var errorMessage = $"Failed to start IntervalToast:\n\n{ex.Message}\n\nThe application will now exit.";

        System.Windows.MessageBox.Show(
            errorMessage,
            "IntervalToast Startup Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Shutdown(1);
    }

    /// <summary>
    /// Global exception handling setup
    /// </summary>
    private void SetupExceptionHandling()
    {
        // Handle unhandled exceptions on the UI thread
        DispatcherUnhandledException += (sender, e) =>
        {
            _logger?.LogError(e.Exception, "Unhandled UI thread exception");
            e.Handled = true; // Prevent application crash
        };

        // Handle unhandled exceptions on background threads
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            _logger?.LogError((Exception)e.ExceptionObject, "Unhandled background thread exception");
        };

        // Handle task exceptions
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            _logger?.LogError(e.Exception, "Unhandled task exception");
            e.SetObserved(); // Prevent application crash
        };
    }
}

/// <summary>
/// Background service for notification scheduling
/// </summary>
internal sealed class NotificationSchedulerService : BackgroundService
{
    private readonly ILogger<NotificationSchedulerService> _logger;

    public NotificationSchedulerService(ILogger<NotificationSchedulerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification scheduler service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process scheduled notifications
                // TODO: Implement notification processing

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification scheduler service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Notification scheduler service stopped");
    }
}