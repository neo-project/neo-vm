// Copyright (C) 2015-2026 The Neo Project.
//
// ScriptConverter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Test.Converters;

internal class ScriptConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    var str = reader.GetString();
                    Assert.StartsWith("0x", str, $"'0x' prefix required for value: '{str}'");
                    return str.FromHexString();
                }
            case JsonTokenType.StartArray:
                {
                    using var script = new ScriptBuilder();

                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        var mul = 1;
                        var value = reader.GetString();

                        if (Enum.IsDefined(typeof(OpCode), value) && Enum.TryParse<OpCode>(value, out var opCode))
                        {
                            for (int x = 0; x < mul; x++)
                            {
                                script.Emit(opCode);
                            }
                        }
                        else
                        {
                            for (int x = 0; x < mul; x++)
                            {
                                Assert.StartsWith("0x", value, $"'0x' prefix required for value: '{value}'");
                                script.EmitRaw(value.FromHexString());
                            }
                        }
                    }

                    return script.ToArray();
                }
        }

        throw new FormatException();
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value is byte[] data)
        {
            int ip = 0;
            var array = new List<string>();

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
                    {
                        // Data

                        array.Add(instruction.Operand.ToArray().ToHexString());
                    }

                    ip += instruction.Size;
                }
            }
            catch
            {
                // Something was wrong, but maybe it's intentioned

                if (Enum.IsDefined(typeof(OpCode), data[ip]))
                {
                    // Check if it was the content and not the opcode

                    array.Add(((OpCode)data[ip]).ToString().ToUpperInvariant());
                    array.Add(data[(ip + 1)..].ToHexString());
                }
                else
                {
                    array.Add(data[ip..].ToHexString());
                }
            }

            // Write the script

            writer.WriteStartArray();
            foreach (var entry in array) writer.WriteStringValue(entry);
            writer.WriteEndArray();

            // Double check - Ensure that the format is exactly the same

            using var script = new ScriptBuilder();

            foreach (var entry in array)
            {
                if (Enum.TryParse<OpCode>(entry, out var opCode))
                {
                    script.Emit(opCode);
                }
                else
                {
                    script.EmitRaw(entry.FromHexString());
                }
            }

            if (script.ToArray().ToHexString() != data.ToHexString())
            {
                throw new FormatException();
            }
        }
        else
        {
            throw new FormatException();
        }
    }
}
