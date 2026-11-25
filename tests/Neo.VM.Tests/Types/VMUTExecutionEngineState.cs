// Copyright (C) 2015-2026 The Neo Project.
//
// VMUTExecutionEngineState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Test.Converters;
using Neo.VM;
using System.Text.Json.Serialization;

namespace Neo.Test.Types;

public class VMUTExecutionEngineState
{
    [JsonConverter(typeof(UppercaseEnum))]
    public VMState State { get; set; }

    public VMUTStackItem[] ResultStack { get; set; }

    public VMUTExecutionContextState[] InvocationStack { get; set; }

    public string ExceptionMessage { get; set; }

    public int? Refs { get; set; }

    public VMUTRunStats RunStats { get; set; }
}
