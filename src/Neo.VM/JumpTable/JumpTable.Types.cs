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
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? IsNull(ExecutionEngine engine, Instruction instruction)
    {
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        engine.Push(x.IsNull);
        return new OpCodePriceParams { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Determines whether the item on top of the evaluation stack has a specified type.
    /// <see cref="OpCode.ISTYPE"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? IsType(ExecutionEngine engine, Instruction instruction)
    {
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        var type = (StackItemType)instruction.TokenU8;
#if NET5_0_OR_GREATER
        if (type == StackItemType.Any || !Enum.IsDefined(type))
#else
        if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
#endif
            throw new InvalidOperationException($"Invalid type: {type}");
        engine.Push(x.Type == type);
        return new OpCodePriceParams { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Converts the item on top of the evaluation stack to a specified type.
    /// <see cref="OpCode.CONVERT"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Convert(ExecutionEngine engine, Instruction instruction)
    {
        var x = engine.Pop();
        var fromType = x.Type;
        var toType = (StackItemType)instruction.TokenU8;
        engine.Push(x.ConvertTo(toType));
        if (fromType == StackItemType.Array && toType == StackItemType.Struct || fromType == StackItemType.Struct && toType == StackItemType.Array)
        {
            if (fromType == StackItemType.Array)
                return new OpCodePriceParams { Type = StackItemType.Array, Length = ((Types.Array)x).Count };
            return new OpCodePriceParams { Type = StackItemType.Struct, Length = ((Struct)x).Count };
        }
        if (fromType == StackItemType.ByteString && toType == StackItemType.Buffer || fromType == StackItemType.Buffer && toType == StackItemType.ByteString)
        {
            if (fromType == StackItemType.ByteString)
                return new OpCodePriceParams { Type = StackItemType.ByteString, Length = ((ByteString)x).GetSpan().Length };
            return new OpCodePriceParams { Type = StackItemType.Buffer, Length = ((Types.Buffer)x).GetSpan().Length };
        }
        return null;
    }

    /// <summary>
    /// Aborts execution with a specified message.
    /// <see cref="OpCode.ABORTMSG"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? AbortMsg(ExecutionEngine engine, Instruction instruction)
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
    /// <remarks>Pop 2, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? AssertMsg(ExecutionEngine engine, Instruction instruction)
    {
        var msg = engine.Pop().GetString();
        var x = engine.Pop().GetBoolean();
        if (!x)
            throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
        return null;
    }
}
