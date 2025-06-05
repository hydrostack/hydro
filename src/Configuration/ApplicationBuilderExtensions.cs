using Hydro.Utils;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace Hydro.Configuration;

/// <summary>
/// Hydro extensions to IApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        Converters = new JsonConverter[] { new Int32Converter() }.ToList()
    };

    /// <summary>
    /// Adds configuration for hydro
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="environment">Current environment</param>
    /// <param name="serializerSettingsMutator">Modification callback for custom serialization, in case of polymorphism within components</param>
    public static IApplicationBuilder UseHydro(this IApplicationBuilder builder, IWebHostEnvironment environment = null, Action<JsonSerializerSettings> serializerSettingsMutator = null) =>
        builder.UseHydro(environment, Assembly.GetCallingAssembly(), serializerSettingsMutator);

    /// <summary>
    /// Adds configuration for hydro
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="environment">Current environment</param>
    /// <param name="assembly">Assembly to scan for the Hydro components</param>
    /// <param name="serializerSettingsMutator">Modification callback for custom serialization, in case of polymorphism within components</param>
    /// <returns></returns>
    public static IApplicationBuilder UseHydro(this IApplicationBuilder builder, IWebHostEnvironment environment, Assembly assembly, Action<JsonSerializerSettings> serializerSettingsMutator = null)
    {
        var settings = JsonSerializerSettings;
        
        serializerSettingsMutator?.Invoke(settings);
        
        builder.UseEndpoints(endpoints =>
        {
            var types = assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(HydroComponent))).ToList();

            foreach (var type in types)
            {
                endpoints.MapHydroComponent(type, settings);
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
