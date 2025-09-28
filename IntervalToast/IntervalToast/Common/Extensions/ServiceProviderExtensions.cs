using Microsoft.Extensions.DependencyInjection;

namespace IntervalToast.Common.Extensions;

/// <summary>
/// Extension methods for IServiceProvider
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets a service safely, returning null if not found
    /// </summary>
    public static T? GetServiceSafely<T>(this IServiceProvider serviceProvider) where T : class
    {
        try
        {
            return serviceProvider.GetService<T>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a required service with better error messaging
    /// </summary>
    public static T GetRequiredServiceSafely<T>(this IServiceProvider serviceProvider) where T : class
    {
        try
        {
            return serviceProvider.GetRequiredService<T>();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Service of type '{typeof(T).Name}' is not registered. " +
                $"Ensure it's added to the service collection during startup. " +
                $"Original error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tries to get a service and returns success status
    /// </summary>
    public static bool TryGetService<T>(this IServiceProvider serviceProvider, out T? service) where T : class
    {
        try
        {
            service = serviceProvider.GetService<T>();
            return service != null;
        }
        catch
        {
            service = null;
            return false;
        }
    }
}