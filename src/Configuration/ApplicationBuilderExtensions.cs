using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Hydro.Configuration;

/// <summary>
/// Hydro extensions to IApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds configuration for hydro
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="environment">Current environment</param>
    public static IApplicationBuilder UseHydro(this IApplicationBuilder builder, IWebHostEnvironment environment = null) =>
        builder.UseHydro(environment, Assembly.GetCallingAssembly());

    /// <summary>
    /// Adds configuration for hydro
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="environment">Current environment</param>
    /// <param name="assembly">Assembly to scan for the Hydro components</param>
    /// <returns></returns>
    public static IApplicationBuilder UseHydro(this IApplicationBuilder builder, IWebHostEnvironment environment, Assembly assembly)
    {
        builder.UseEndpoints(endpoints =>
        {
            var types = assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(HydroComponent))).ToList();

            foreach (var type in types)
            {
                endpoints.MapHydroComponent(type);
            }
        });

        environment ??= (IWebHostEnvironment)builder.ApplicationServices.GetService(typeof(IWebHostEnvironment))!;
        
        var existingProvider = environment.WebRootFileProvider; 

        var scriptsFileProvider = new ScriptsFileProvider(typeof(ApplicationBuilderExtensions).Assembly);
        var compositeProvider = new CompositeFileProvider(existingProvider, scriptsFileProvider);
        environment.WebRootFileProvider = compositeProvider;
        
        return builder;
    }

}
