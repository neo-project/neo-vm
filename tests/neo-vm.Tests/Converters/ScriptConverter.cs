using System;
using Neo.Test.Extensions;
using Newtonsoft.Json;

namespace Neo.Test.Converters
{
    internal class ScriptConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]) || objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is byte[] data) return data;
            if (!(reader.Value is string str)) throw new FormatException();

            if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                data = str.FromHexString();
            }
            else
            {
                data = Convert.FromBase64String(str);
            }

            return data;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is byte[] data)
            {
                writer.WriteValue(data.ToHexString());
            }
            else
            {
                throw new FormatException();
            }
        }
    }
}