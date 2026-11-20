// Copyright (C) 2015-2026 The Neo Project.
//
// VMUTEntry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Test.Converters;
using System.Text.Json.Serialization;

namespace Neo.Test.Types;

public class VMUTEntry
{
    [JsonPropertyOrder(1)]
    public string Name { get; set; }

    [JsonPropertyOrder(2), JsonConverter(typeof(ScriptConverter))]
    public byte[] Script { get; set; }

    [JsonPropertyOrder(3)]
    public VMUTStep[] Steps { get; set; }
}
