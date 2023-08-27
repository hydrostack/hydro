using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace Hydro;

internal static class PropertyInjector
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CachedPropertyInfos = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo> PropertyCache = new();

    public static string SerializeDeclaredProperties(Type type, object instance)
    {
        var regularProperties = GetRegularProperties(type, instance);
        return JsonSerializer.Serialize(regularProperties);
    }

    private static IDictionary<string, object> GetRegularProperties(Type type, object instance) =>
        GetPropertyInfos(type).ToDictionary(p => p.Name, p => p.GetValue(instance));

    private static IEnumerable<PropertyInfo> GetPropertyInfos(Type type)
    {
        if (CachedPropertyInfos.TryGetValue(type, out var properties))
        {
            return properties;
        }

        var propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.DeclaringType == type || p.DeclaringType == typeof(HydroComponent))
            .ToArray();

        CachedPropertyInfos.TryAdd(type, propertyInfos);

        return propertyInfos;
    }

    public static void SetPropertyValue(object target, string propertyPath, StringValues value)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            throw new ArgumentException("Property path cannot be empty.", nameof(propertyPath));
        }

        var properties = propertyPath.Split('.');
        var currentObject = target;

        for (var i = 0; i < properties.Length - 1; i++)
        {
            currentObject = GetObjectOrIndexedValue(currentObject, properties[i]);
        }

        SetValueOnObject(currentObject, properties[^1], value);
    }

    private static object GetObjectOrIndexedValue(object obj, string propName)
    {
        if (obj == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(propName))
        {
            throw new InvalidOperationException("Wrong property path");
        }

        return propName.Contains('[')
            ? GetIndexedValue(obj, propName)
            : obj.GetType().GetProperty(propName)?.GetValue(obj);
    }


    private static object GetIndexedValue(object obj, string propName)
    {
        if (obj == null)
        {
            return null;
        }

        var (index, cleanedPropName) = GetIndexAndCleanedPropertyName(propName);
        var propertyInfo = obj.GetType().GetProperty(cleanedPropName);

        if (propertyInfo == null)
        {
            return null;
        }

        var value = propertyInfo.GetValue(obj);
        
        if (value == null)
        {
            return null;
        }

        if (propertyInfo.PropertyType.IsArray)
        {
            return value is Array array && index < array.Length
                ? array.GetValue(index)
                : throw new InvalidOperationException("Wrong value type");
        }

        if (typeof(IList).IsAssignableFrom(propertyInfo.PropertyType))
        {
            return value is IList list && index < list.Count
                ? list[index]
                : throw new InvalidOperationException("Wrong value type");
        }

        throw new InvalidOperationException("Wrong indexer property");
    }

    private static (int, string) GetIndexAndCleanedPropertyName(string propName)
    {
        var iteratorStart = propName.IndexOf('[');
        var iteratorEnd = propName.IndexOf(']');
        var iteratorValue = propName.Substring(iteratorStart + 1, iteratorEnd - iteratorStart - 1);
        var cleanedPropName = propName[..iteratorStart];
        return (Convert.ToInt32(iteratorValue), cleanedPropName);
    }

    private static void SetValueOnObject(object obj, string propName, StringValues valueToSet)
    {
        if (obj == null)
        {
            return;
        }
        
        if (string.IsNullOrWhiteSpace(propName))
        {
            throw new InvalidOperationException("Wrong property path");
        }

        if (propName.Contains('['))
        {
            SetIndexedValue(obj, propName, valueToSet);
            return;
        }

        var propertyInfo = obj.GetType().GetProperty(propName);
        if (propertyInfo == null)
        {
            return;
        }

        var convertedValue = ConvertValue(valueToSet, propertyInfo.PropertyType);
        propertyInfo.SetValue(obj, convertedValue);
    }

    private static void SetIndexedValue(object obj, string propName, StringValues valueToSet)
    {
        var (index, cleanedPropName) = GetIndexAndCleanedPropertyName(propName);
        var propertyInfo = obj.GetType().GetProperty(cleanedPropName);
        var convertedValue = ConvertValue(valueToSet, propertyInfo.PropertyType);

        var value = propertyInfo.GetValue(obj);

        if (value == null)
        {
            throw new InvalidOperationException("Cannot set value to null");
        }
        
        if (propertyInfo.PropertyType.IsArray)
        {
            if (value is not Array array)
            {
                throw new InvalidOperationException("Wrong type");
            }

            array.SetValue(convertedValue, index);
        }
        else if (typeof(IList).IsAssignableFrom(propertyInfo.PropertyType))
        {
            if (value is not IList list)
            {
                throw new InvalidOperationException("Wrong type");
            }

            list[index] = convertedValue;
        }
        else
        {
            throw new InvalidOperationException($"Indexed access for property '{cleanedPropName}' is not supported.");
        }
    }

    private static object ConvertValue(StringValues valueToConvert, Type destinationType)
    {
        var converter = TypeDescriptor.GetConverter(destinationType);
        if (!converter.CanConvertFrom(typeof(string)))
        {
            throw new InvalidOperationException($"Cannot convert StringValues to '{destinationType}'.");
        }

        if (!destinationType.IsArray || valueToConvert.Count <= 1)
        {
            return converter.ConvertFromString(valueToConvert.ToString());
        }

        var elementType = destinationType.GetElementType();
        var array = Array.CreateInstance(elementType, valueToConvert.Count);
        for (var i = 0; i < valueToConvert.Count; i++)
        {
            array.SetValue(converter.ConvertFromString(valueToConvert[i]), i);
        }

        return array;
    }
}