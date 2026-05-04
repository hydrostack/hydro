using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hydro.Utils;

internal static class JsonPopulator
{
    public static void PopulateObject(string json, object target, JsonSerializerOptions options = null)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));

        using var document = JsonDocument.Parse(json);
        PopulateObject(document.RootElement, target, options ?? JsonSettings.SerializerSettings);
    }

    private static void PopulateObject(JsonElement element, object target, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object || target is null)
        {
            return;
        }

        var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var jsonProp in element.EnumerateObject())
        {
            var prop = FindProperty(properties, jsonProp.Name);
            if (prop is null || !prop.CanRead)
            {
                continue;
            }

            var propType = prop.PropertyType;
            var jsonValue = jsonProp.Value;

            // 1) Objeto anidado: reutilizar instancia existente (ObjectCreationHandling.Auto)
            if (jsonValue.ValueKind == JsonValueKind.Object && !IsSimpleType(propType))
            {
                var current = prop.GetValue(target);
                if (current is not null)
                {
                    PopulateObject(jsonValue, current, options);
                    continue;
                }
            }

            // 2) Colección existente: hacer Add (incluso si la propiedad no tiene setter)
            if (jsonValue.ValueKind == JsonValueKind.Array && typeof(IList).IsAssignableFrom(propType))
            {
                if (prop.GetValue(target) is IList existing && !existing.IsReadOnly && !existing.IsFixedSize)
                {
                    var elementType = GetCollectionElementType(propType);
                    foreach (var item in jsonValue.EnumerateArray())
                    {
                        existing.Add(DeserializeValue(item, elementType, options));
                    }
                    continue;
                }
            }

            // 3) Cualquier otra cosa: requiere setter
            if (!prop.CanWrite)
            {
                continue;
            }

            if (jsonValue.ValueKind == JsonValueKind.Null)
            {
                if (!propType.IsValueType || Nullable.GetUnderlyingType(propType) is not null)
                {
                    prop.SetValue(target, null);
                }
                continue;
            }

            prop.SetValue(target, DeserializeValue(jsonValue, propType, options));
        }
    }

    private static object DeserializeValue(JsonElement element, Type targetType, JsonSerializerOptions options)
    {
        // Newtonsoft acepta enums como string (case-insensitive) por default
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsEnum && element.ValueKind == JsonValueKind.String)
        {
            var name = element.GetString();
            return name is null ? null : Enum.Parse(underlying, name, ignoreCase: true);
        }

        return element.Deserialize(targetType, options);
    }

    private static PropertyInfo FindProperty(PropertyInfo[] properties, string jsonName)
    {
        PropertyInfo caseInsensitiveMatch = null;

        foreach (var p in properties)
        {
            var name = GetJsonName(p);
            if (name == jsonName)
            {
                return p; // match exacto gana
            }

            if (caseInsensitiveMatch is null &&
                string.Equals(name, jsonName, StringComparison.OrdinalIgnoreCase))
            {
                caseInsensitiveMatch = p;
            }
        }

        return caseInsensitiveMatch;
    }

    private static string GetJsonName(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attr?.Name ?? property.Name;
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType()!;
        }

        if (collectionType.IsGenericType)
        {
            var args = collectionType.GetGenericArguments();
            if (args.Length == 1)
            {
                return args[0];
            }
        }

        foreach (var iface in collectionType.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return typeof(object);
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }
}