namespace TickerScout.Backend.Services;

public sealed class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public static T GetService<T>() where T : notnull
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");
        }

        return _serviceProvider.GetRequiredService<T>();
    }

    public static T? GetServiceOrDefault<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }

    public static object GetService(Type serviceType)
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize() first.");
        }

        return _serviceProvider.GetRequiredService(serviceType);
    }

    // For testing purposes - reset the service provider
    internal static void Reset()
    {
        _serviceProvider = null;
    }
}
