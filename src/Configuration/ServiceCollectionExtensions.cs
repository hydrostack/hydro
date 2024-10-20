using Hydro.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hydro.Configuration;

/// <summary>
/// Hydro extensions to IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures services required by 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddHydro(this IServiceCollection services, Action<HydroOptions> options = null)
    {
        var hydroOptions = new HydroOptions();
        options?.Invoke(hydroOptions);
        services.AddSingleton(hydroOptions);
        services.TryAddSingleton<IPersistentState, PersistentState>();

        return services;
    }
}
