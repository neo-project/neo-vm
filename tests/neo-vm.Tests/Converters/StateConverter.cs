using System;
using Neo.VM;
using Newtonsoft.Json;

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
            if (!(reader.Value is string str)) throw new FormatException();

            VMState ret = VMState.NONE;

            foreach (var split in str.Split("|", StringSplitOptions.RemoveEmptyEntries))
            {
                ret |= Enum.Parse<VMState>(split.Trim().ToUpperInvariant());
            }

            return ret;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is VMState data)
            {
                writer.WriteValue(data.ToString().Replace(" ", ""));
            }
            else
            {
                throw new FormatException();
            }
        }
    }
}