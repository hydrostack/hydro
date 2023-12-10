using System.ComponentModel;
using System.Globalization;

namespace Hydro;

/// <summary>
/// Model for select components
/// </summary>
/// <typeparam name="TKey">Key's type</typeparam>
[TypeConverter(typeof(SelectValueConverter))]
public class SelectValue<TKey> : ISelectValue
{
    /// <inheritdoc />
    public string Id { get; set; }
    
    /// <summary />
    public static implicit operator string(SelectValue<TKey> value) => value?.Id;
    
    /// <summary />
    public static implicit operator SelectValue<TKey>(string value) => value == null ? null : new() { Id = value };
    
    /// <summary />
    public override string ToString() => Id;
}

/// <summary />
public interface ISelectValue
{
    /// <summary>
    /// Represents id/key of selection
    /// </summary>
    string Id { get; set; }
}

/// <inheritdoc />
public class SelectValueConverter : TypeConverter
{
    private readonly Type _destinationType;

    /// <inheritdoc />
    public SelectValueConverter(Type destinationType)
    {
        _destinationType = destinationType;
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string id)
        {
            var selectValue = (ISelectValue)Activator.CreateInstance(_destinationType)!;
            selectValue.Id = id;
            return selectValue;
        }

        return base.ConvertFrom(context, culture, value);
    }
}