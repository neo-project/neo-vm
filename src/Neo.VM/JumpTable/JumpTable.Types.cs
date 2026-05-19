// Copyright (C) 2015-2026 The Neo Project.
//
// JumpTable.Types.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Runtime.CompilerServices;

namespace Neo.VM;

partial class JumpTable
{
    /// <summary>
    /// Determines whether the item on top of the evaluation stack is null.
    /// <see cref="OpCode.ISNULL"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void IsNull(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        engine.Push(x.IsNull);
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Determines whether the item on top of the evaluation stack has a specified type.
    /// <see cref="OpCode.ISTYPE"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void IsType(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        var type = (StackItemType)instruction.TokenU8;
        if (type == StackItemType.Any || !Enum.IsDefined(type))
            throw new InvalidOperationException($"Invalid type: {type}");
        engine.Push(x.Type == type);
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Converts the item on top of the evaluation stack to a specified type.
    /// <see cref="OpCode.CONVERT"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Convert(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r1 = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        var fromType = x.Type;
        var toType = (StackItemType)instruction.TokenU8;
        var r2 = engine.ReferenceCounter.Count;
        engine.Push(x.ConvertTo(toType));
        var (type, length) = (StackItemType.Any, 0);
        if (fromType == StackItemType.Array && toType == StackItemType.Struct || fromType == StackItemType.Struct && toType == StackItemType.Array)
        {
            type = StackItemType.Array;
            length = ((CompoundType)x).Count;
        }
        else if (fromType == StackItemType.ByteString && toType == StackItemType.Buffer || fromType == StackItemType.Buffer && toType == StackItemType.ByteString)
        {
            type = StackItemType.ByteString;
            length = fromType == StackItemType.ByteString ? ((ByteString)x).Size : ((Types.Buffer)x).Size;
        }
        runStats = new RunStats { RefsDelta = (r1 - r2) + (engine.ReferenceCounter.Count - r2), Type = type, Length = length };
    }

    /// <summary>
    /// Aborts execution with a specified message.
    /// <see cref="OpCode.ABORTMSG"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void AbortMsg(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var msg = engine.Pop().GetString();
        throw new Exception($"{OpCode.ABORTMSG} is executed. Reason: {msg}");
    }

    /// <summary>
    /// Asserts a condition with a specified message, throwing an exception if the condition is false.
    /// <see cref="OpCode.ASSERTMSG"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 2, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void AssertMsg(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        var msg = engine.Pop().GetString();
        var x = engine.Pop().GetBoolean();
        if (!x)
            throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count, Length = msg!.Length };
    }
}
