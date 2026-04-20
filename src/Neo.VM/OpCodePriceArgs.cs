// Copyright (C) 2015-2026 The Neo Project.
//
// OpCodePriceArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM;

/// <summary>
/// Contains opcode-specific parameters used to calculate dynamic price.
/// </summary>
public struct OpcodePriceParams
{
    public StackItemType Type { get; internal set; }
    public int Length { get; internal set; }
    public int RefsDelta { get; internal set; }
    public int NClonedItems { get; internal set; }
}
