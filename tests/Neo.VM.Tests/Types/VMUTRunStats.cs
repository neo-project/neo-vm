// Copyright (C) 2015-2026 The Neo Project.
//
// VMUTRunStats.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.Test.Types;

/// <summary>
/// Expected <see cref="Neo.VM.RunStats"/> of the last instruction executed in a step.
/// Every field is optional: only the ones present in the JSON are checked.
/// </summary>
public class VMUTRunStats
{
    public StackItemType? Type { get; set; }

    public int? Length { get; set; }

    public int? RefsDelta { get; set; }

    public int? NClonedItems { get; set; }
}
