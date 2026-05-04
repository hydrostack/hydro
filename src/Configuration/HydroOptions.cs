using System.Text.Json;

namespace Hydro.Configuration;

/// <summary>
/// Hydro options
/// </summary>
public class HydroOptions
{
    /// <summary>
    /// The default base path all hydro requests are based of
    /// </summary>
    public const string DefaultBasePath = "/hydro";
    
    private IEnumerable<IHydroValueMapper> _valueMappers;
    private string _basePath = DefaultBasePath;

    internal Dictionary<Type, IHydroValueMapper> ValueMappersDictionary { get; set; } = new();

    /// <summary>
    /// Serializer settings
    /// </summary>
    public JsonSerializerOptions JsonSerializerSettings => HydroComponent.JsonSerializerSettings;
    
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

    /// <summary>
    /// The base path all hydro requests are based of
    /// The path is must always start with a slash '/' and any slashes in the end are trimmed
    /// </summary>
    public string BasePath
    {
        get => _basePath;
        set => _basePath = string.IsNullOrWhiteSpace(value) ? DefaultBasePath : value.TrimEnd('/');
    }
}