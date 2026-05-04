using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hydro.Utils;

internal class Int32Converter : JsonConverter<object>
{
    // Cacheamos las opciones "sin este converter" para no romper el cache de metadata
    // de STJ ni reconstruirlas en cada Read/Write.
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _withoutSelfCache = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(int) || typeToConvert == typeof(long) || typeToConvert == typeof(object);

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        return JsonSerializer.Deserialize(ref reader, typeToConvert, WithoutSelf(options));
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        // Equivalente a CanWrite => false en Newtonsoft:
        // no participamos en la escritura, delegamos al serializer por defecto.
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), WithoutSelf(options));
    }

    private static JsonSerializerOptions WithoutSelf(JsonSerializerOptions options) =>
        _withoutSelfCache.GetValue(options, static opts =>
        {
            var copy = new JsonSerializerOptions(opts);
            for (int i = copy.Converters.Count - 1; i >= 0; i--)
            {
                if (copy.Converters[i] is Int32Converter)
                {
                    copy.Converters.RemoveAt(i);
                }
            }
            return copy;
        });
}