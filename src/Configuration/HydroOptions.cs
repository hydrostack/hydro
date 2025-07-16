using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Hydro.Configuration;

/// <summary>
/// Hydro options
/// </summary>
public class HydroOptions
{
    private IEnumerable<IHydroValueMapper> _valueMappers;

    internal Dictionary<Type, IHydroValueMapper> ValueMappersDictionary { get; set; } = new();
    
    /// <summary>
    /// Serializer settings for add Custom Convertors
    /// </summary>
    [CanBeNull]
    public Action<JsonSerializerSettings> ModifyJsonSerializerSettings { get; set; }
    
    /// <summary>
    /// Indicates if antiforgery token should be exchanged during the communication
    /// </summary>
    public bool AntiforgeryTokenEnabled { get; set; }

    /// <summary>
    /// Performs mapping of each value that goes through binding mechanism in all the components
    /// </summary>
    public IEnumerable<IHydroValueMapper> ValueMappers
    {
        get => _valueMappers;
        set
        {
            _valueMappers = value;

            if (value != null)
            {
                ValueMappersDictionary = value.ToDictionary(mapper => mapper.MappedType, mapper => mapper);
            }
        }
    }
}
