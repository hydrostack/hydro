using System.Collections.Concurrent;
using System.Reflection;

namespace Hydro;

internal static class PropertyExtractor
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    public static Dictionary<string, object> GetPropertiesFromObject(object source) =>
        source == null
            ? new()
            : PropertiesCache.GetOrAdd(source.GetType(), type => type.GetProperties())
                .ToDictionary(p => p.Name, p => p.GetValue(source));
}