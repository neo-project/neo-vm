using Neo.Test.Extensions;
using Neo.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

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
                        using (var script = new ScriptBuilder())
                        {
                            foreach (var entry in JArray.Load(reader))
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
                int ip = 0;
                var array = new JArray();

                try
                {
                    for (ip = 0; ip < data.Length;)
                    {
                        var instruction = new Instruction(data, ip);

                        array.Add(instruction.OpCode.ToString().ToUpperInvariant());

                        // Operand Size

                        if (instruction.Size - 1 - instruction.Operand.Length > 0)
                        {
                            array.Add(data.Skip(ip + 1).Take(instruction.Size - 1 - instruction.Operand.Length).ToArray().ToHexString());
                        }

                        if (!instruction.Operand.IsEmpty)
                        {    // Data

                            array.Add(instruction.Operand.ToArray().ToHexString());
                        }

                        ip += instruction.Size;
                    }
                }
                catch
                {
                    // Something was wrong, but maybe it's intentioned

                    array.Add(data[ip..].ToHexString());
                }

                // Write the script

                writer.WriteStartArray();
                foreach (var entry in array) writer.WriteValue(entry.Value<string>());
                writer.WriteEndArray();

                // Double check - Ensure that the format is exactly the same

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

                    if (script.ToArray().ToHexString() != data.ToHexString())
                    {
                        throw new FormatException();
                    }
                }
            }
            else
            {
                throw new FormatException();
            }
        }
    }
}
