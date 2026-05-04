using Hydro.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hydro;

internal static class JsonSettings
{
    public static readonly JsonSerializerOptions SerializerSettings = new()
    {
        Converters = { new Int32Converter() },
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}