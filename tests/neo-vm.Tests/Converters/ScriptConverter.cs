using System;
using Neo.Test.Extensions;
using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    {
                        if (reader.Value is string str) return str.FromHexString();
                        break;
                    }
                case JsonToken.Bytes:
                    {
                        if (reader.Value is byte[] data) return data;
                        break;
                    }
                case JsonToken.StartArray:
                    {
                        var array = JArray.Load(reader);

                        using (var script = new ScriptBuilder())
                        {
                            foreach (var entry in array)
                            {
                                if (Enum.TryParse<OpCode>(entry.Value<string>(), out var opCode))
                                {
                                    script.Emit(opCode);
                                }
                                else
                                {
                                    script.EmitRaw(entry.Value<string>().FromHexString());
                                }
                            }

                            return script.ToArray();
                        }
                    }
            }

            throw new FormatException();
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
