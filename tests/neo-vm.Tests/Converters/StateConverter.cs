using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Neo.Test.Converters
{
    internal class StateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String) throw new FormatException();

            return Enum.Parse<VMState>(JToken.ReadFrom(reader).Value<string>().Trim().ToUpperInvariant());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is VMState data)
            {
                writer.WriteValue(data.ToString());
            }
            else
            {
                throw new FormatException();
            }
        }
    }
}