using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Hydro;

internal static class PropertyInjector
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CachedPropertyInfos = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo> PropertyCache = new();

    public static string SerializeDeclaredProperties(Type type, object instance)
    {
        var regularProperties = GetRegularProperties(type, instance);
        return JsonConvert.SerializeObject(regularProperties, HydroComponent.JsonSerializerSettings);
    }

    private static IDictionary<string, object> GetRegularProperties(Type type, object instance) =>
        GetPropertyInfos(type).ToDictionary(p => p.Name, p => p.GetValue(instance));

    private static IEnumerable<PropertyInfo> GetPropertyInfos(Type type)
    {
        if (CachedPropertyInfos.TryGetValue(type, out var properties))
        {
            return properties;
        }

        var viewComponentType = typeof(TagHelper);
        var hydroComponentType = typeof(HydroComponent);

        var baseProps = new[] { "Key", "IsModelTouched", "TouchedProperties" };
        
        var propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => (baseProps.Contains(p.Name) && p.DeclaringType == hydroComponentType)
                        || (p.DeclaringType != viewComponentType && p.DeclaringType != hydroComponentType
                            && p.GetGetMethod()?.IsPublic == true
                            && p.GetSetMethod()?.IsPublic == true
                            && !p.GetCustomAttributes<TransientAttribute>().Any())
            )
            .ToArray();

        CachedPropertyInfos.TryAdd(type, propertyInfos);

        return propertyInfos;
    }

    public static void SetPropertyValue(object target, string propertyPath, object value)
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

        if (currentObject == null)
        {
            return;
        }

        var propName = properties[^1];

        if (string.IsNullOrWhiteSpace(propName))
        {
            throw new InvalidOperationException("Wrong property path");
        }

        if (propName.Contains('['))
        {
            throw new NotSupportedException();
        }

        var propertyInfo = currentObject.GetType().GetProperty(propName);
        if (propertyInfo == null)
        {
            return;
        }

        var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
        var convertedValue = value == null || value.GetType() == propertyInfo.PropertyType
            ? value
            : converter.ConvertFrom(value);

        propertyInfo.SetValue(currentObject, convertedValue);
    }

    public static (object Value, Action<object> Setter, Type PropertyType)? GetPropertySetter(object target, string propertyPath, object value)
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

        return SetValueOnObject(currentObject, properties[^1], value);
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

    private static (object, Action<object>, Type)? SetValueOnObject(object obj, string propName, object valueToSet)
    {
        if (obj == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(propName))
        {
            throw new InvalidOperationException("Wrong property path");
        }

        if (propName.Contains('['))
        {
            return SetIndexedValue(obj, propName, valueToSet);
        }

        var propertyInfo = obj.GetType().GetProperty(propName);
        if (propertyInfo == null)
        {
            return null;
        }

        var convertedValue = ConvertValue(valueToSet, propertyInfo.PropertyType);
        propertyInfo.SetValue(obj, convertedValue);
        return (convertedValue, val => propertyInfo.SetValue(obj, val), propertyInfo.PropertyType);
    }

    private static (object, Action<object>, Type)? SetIndexedValue(object obj, string propName, object valueToSet)
    {
        var (index, cleanedPropName) = GetIndexAndCleanedPropertyName(propName);
        var propertyInfo = obj.GetType().GetProperty(cleanedPropName);
        var convertedValue = ConvertValue(valueToSet, propertyInfo!.PropertyType);

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

            return (convertedValue, val => array.SetValue(val, index), propertyInfo!.PropertyType);
        }

        if (typeof(IList).IsAssignableFrom(propertyInfo.PropertyType))
        {
            if (value is not IList list)
            {
                throw new InvalidOperationException("Wrong type");
            }

            return (convertedValue, val => list[index] = val, propertyInfo!.PropertyType);
        }

        throw new InvalidOperationException($"Indexed access for property '{cleanedPropName}' is not supported.");
    }

    private static object ConvertValue(object valueToConvert, Type destinationType)
    {
        if (valueToConvert is not StringValues stringValues)
        {
            return valueToConvert;
        }

        if (typeof(IFormFile).IsAssignableFrom(destinationType) && StringValues.IsNullOrEmpty(stringValues))
        {
            return null;
        }
        
        if (typeof(IFormFile[]).IsAssignableFrom(destinationType) && StringValues.IsNullOrEmpty(stringValues))
        {
            return Array.Empty<IFormFile>();
        }
        
        if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(destinationType) && StringValues.IsNullOrEmpty(stringValues))
        {
            return new List<IFormFile>();
        }

        var converter = TypeDescriptor.GetConverter(destinationType!);

        if (!converter.CanConvertFrom(typeof(string)))
        {
            throw new InvalidOperationException($"Cannot convert StringValues to '{destinationType}'.");
        }

        if (!destinationType.IsArray || stringValues is { Count: <= 1 })
        {
            try
            {
                return converter.ConvertFromString(valueToConvert.ToString());
            }
            catch
            {
                return Activator.CreateInstance(destinationType);
            }
        }

        var elementType = destinationType.GetElementType();
        var array = Array.CreateInstance(elementType, stringValues.Count);
        for (var i = 0; i < stringValues.Count; i++)
        {
            try
            {
                array.SetValue(converter.ConvertFromString(stringValues[i]), i);
            }
            catch
            {
                array.SetValue(Activator.CreateInstance(elementType), i);
            }
        }

        return array;
    }
}