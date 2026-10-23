// Copyright (C) 2015-2026 The Neo Project.
//
// RunStats.cs file belongs to the neo project and is free
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
public struct RunStats
{
    /// <summary>
    /// Typ specifies type of <see cref="StackItemType"/> which in most of the cases serves
    /// as an operand of the given opcode.
    /// </summary>
    public StackItemType Type { get; private set; }
    /// <summary>
    /// Length denotes one of the following:
    /// - the number of elements in the compound type (that is, the length of Array
    ///   or Struct, the number of key-value pairs in Map);
    /// - the length of Buffer or ByteArray;
    /// - the number of VM slot cells or stack elements involved in opcode handling;
    /// - the number of stack elements that the opcode processes.
    /// </summary>
    public int Length { get; private set; }
    /// <summary>
    /// RefsDelta is total change of refCounter value performed by opcode.
    /// </summary>
    public int RefsDelta { get; private set; }
    /// <summary>
    /// NClonedItems is number of items cloned by opcode.
    /// </summary>
    public int NClonedItems { get; private set; }

    /// <summary>
    /// Records the <see cref="StackItemType"/> operand of the executed opcode.
    /// </summary>
    public void CollectType(StackItemType type)
    {
        Type = type;
    }

    /// <summary>
    /// Records the length-related statistic of the executed opcode, see <see cref="Length"/>.
    /// </summary>
    public void CollectOpLength(int length)
    {
        Length = length;
    }

    /// <summary>
    /// Records the reference counter delta caused by the executed opcode.
    /// </summary>
    public void CollectRefDelta(int refsDelta)
    {
        RefsDelta = refsDelta;
    }

    /// <summary>
    /// Records the number of items cloned by the executed opcode.
    /// </summary>
    public void CollectClonedItems(int nClonedItems)
    {
        NClonedItems = nClonedItems;
    }
}
