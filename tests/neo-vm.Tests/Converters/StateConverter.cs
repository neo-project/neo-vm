using System;
using System.Collections.Generic;
using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            if (reader.TokenType != JsonToken.StartArray) throw new FormatException();

            VMState ret = VMState.NONE;

            foreach (var split in JArray.ReadFrom(reader))
            {
                ret |= Enum.Parse<VMState>(split.Value<string>().Trim().ToUpperInvariant());
            }

            return ret;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is VMState data)
            {
                var list = new List<string>();

                foreach(VMState item in Enum.GetValues(typeof(VMState)))
                {
                    if (!data.HasFlag(item)) continue;

                    list.Add(item.ToString());
                }

                writer.WriteValue(list.ToArray());
            }
            else
            {
                throw new FormatException();
            }
        }
    }
}