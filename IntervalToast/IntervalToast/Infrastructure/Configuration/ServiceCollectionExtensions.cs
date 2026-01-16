using IntervalToast.Application.Interfaces;
using IntervalToast.Application.Services;
using IntervalToast.Domain.Interfaces;
using IntervalToast.Infrastructure.Persistence;
using IntervalToast.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static IntervalToast.Application.Interfaces.IHotkeyService;

namespace IntervalToast.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring services in the DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the service collection
    /// </summary>
    public static IServiceCollection AddIntervalToastServices(this IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register repositories
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();

        // Register application services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, Infrastructure.Services.NotificationService>();
        services.AddSingleton<NotificationOrchestrationService>();

        // Register infrastructure services (placeholders for now)
        // services.AddTransient<IHotkeyService, HotkeyService>();
        // services.AddTransient<IAnimationService, AnimationService>();
        // services.AddTransient<IWindowPositionService, WindowPositionService>();

        return services;
    }

    /// <summary>
    /// Adds presentation layer services to the service collection
    /// </summary>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Register view models
        services.AddTransient<Presentation.ViewModels.MainWindowViewModel>();

        // Register windows
        services.AddTransient<Presentation.Windows.MainWindow>();

        // Register other presentation services as needed
        // services.AddTransient<IDialogService, DialogService>();
        // services.AddTransient<ISystemTrayService, SystemTrayService>();

        return services;
    }
}

// Placeholder implementations will be added later after we get the basic structure working