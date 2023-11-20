using Newtonsoft.Json;

namespace Hydro.Utils;

internal class Int32Converter : JsonConverter
{
    public override bool CanConvert(Type objectType) => 
        objectType == typeof(int) || objectType == typeof(long) || objectType == typeof(object);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
        reader.TokenType == JsonToken.Integer
            ? Convert.ToInt32(reader.Value)
            : serializer.Deserialize(reader);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new InvalidOperationException();
    
    public override bool CanWrite => false;
}