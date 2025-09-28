using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntervalToast.Application.Common;

/// <summary>
/// Interface for application host management
/// </summary>
public interface IApplicationHost : IDisposable
{
    /// <summary>
    /// Gets the service provider
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets a service of the specified type
    /// </summary>
    T GetService<T>() where T : notnull;

    /// <summary>
    /// Gets a required service of the specified type
    /// </summary>
    T GetRequiredService<T>() where T : notnull;

    /// <summary>
    /// Starts the application host
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the application host
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of application host
/// </summary>
public sealed class ApplicationHost : IApplicationHost, IDisposable
{
    private readonly IHost _host;

    public ApplicationHost(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <inheritdoc />
    public IServiceProvider Services => _host.Services;

    /// <inheritdoc />
    public T GetService<T>() where T : notnull
    {
        return Services.GetService<T>()!;
    }

    /// <inheritdoc />
    public T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _host.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _host.StopAsync(cancellationToken);
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}