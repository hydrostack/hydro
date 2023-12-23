using Hydro.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hydro;

internal static class JsonSettings
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Converters = new JsonConverter[] { new Int32Converter() }.ToList(),
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };
}