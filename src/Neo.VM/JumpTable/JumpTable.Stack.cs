// Copyright (C) 2015-2026 The Neo Project.
//
// JumpTable.Stack.cs file belongs to the neo project and is free
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
    /// Pushes the number of stack items in the evaluation stack onto the stack.
    /// <see cref="OpCode.DEPTH"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 0, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Depth(ExecutionEngine engine, Instruction instruction)
    {
        engine.Push(engine.CurrentContext!.EvaluationStack.Count);
        return null;
    }

    /// <summary>
    /// Removes the top item from the evaluation stack.
    /// <see cref="OpCode.DROP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Drop(ExecutionEngine engine, Instruction instruction)
    {
        var r = engine.ReferenceCounter.Count;
        engine.Pop();
        return new OpCodePriceParams { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Removes the second-to-top stack item.
    /// <see cref="OpCode.NIP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Nip(ExecutionEngine engine, Instruction instruction)
    {
        var r = engine.ReferenceCounter.Count;
        engine.CurrentContext!.EvaluationStack.Remove<StackItem>(1);
        return new OpCodePriceParams { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Removes the n-th item from the top of the evaluation stack.
    /// <see cref="OpCode.XDROP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? XDrop(ExecutionEngine engine, Instruction instruction)
    {
        var n = (int)engine.Pop().GetInteger();
        if (n < 0)
            throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
        var r = engine.ReferenceCounter.Count;
        engine.CurrentContext!.EvaluationStack.Remove<StackItem>(n);
        return new OpCodePriceParams { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Clears all items from the evaluation stack.
    /// <see cref="OpCode.CLEAR"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Clear(ExecutionEngine engine, Instruction instruction)
    {
        var r = engine.ReferenceCounter.Count;
        var l = engine.CurrentContext!.EvaluationStack.Count;
        engine.CurrentContext!.EvaluationStack.Clear();
        return new OpCodePriceParams { RefsDelta = r - engine.ReferenceCounter.Count, Length = l };
    }

    /// <summary>
    /// Duplicates the item on the top of the evaluation stack.
    /// <see cref="OpCode.DUP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 0, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Dup(ExecutionEngine engine, Instruction instruction)
    {
        var item = engine.Peek();
        engine.Push(item);
        if (item.Type == StackItemType.ByteString)
            return new OpCodePriceParams { Length = item.GetSpan().Length };
        return null;
    }

    /// <summary>
    /// Copies the second item from the top of the evaluation stack and pushes the copy onto the stack.
    /// <see cref="OpCode.OVER"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 0, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Over(ExecutionEngine engine, Instruction instruction)
    {
        var item = engine.Peek(1);
        engine.Push(item);
        if (item.Type == StackItemType.ByteString)
            return new OpCodePriceParams { Length = item.GetSpan().Length };
        return null;
    }

    /// <summary>
    /// Copies the nth item from the top of the evaluation stack and pushes the copy onto the stack.
    /// <see cref="OpCode.PICK"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Pick(ExecutionEngine engine, Instruction instruction)
    {
        var n = (int)engine.Pop().GetInteger();
        if (n < 0)
            throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
        var item = engine.Peek(n);
        engine.Push(item);
        if (item.Type == StackItemType.ByteString)
            return new OpCodePriceParams { Length = item.GetSpan().Length };
        return null;
    }

    /// <summary>
    /// Copies the top item on the evaluation stack and inserts the copy between the first and second items.
    /// <see cref="OpCode.TUCK"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Tuck(ExecutionEngine engine, Instruction instruction)
    {
        var item = engine.Peek();
        engine.CurrentContext!.EvaluationStack.Insert(2, item);
        if (item.Type == StackItemType.ByteString)
            return new OpCodePriceParams { Length = item.GetSpan().Length };
        return null;
    }

    /// <summary>
    /// Swaps the top two items on the evaluation stack.
    /// <see cref="OpCode.SWAP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 0, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Swap(ExecutionEngine engine, Instruction instruction)
    {
        var stack = engine.CurrentContext!.EvaluationStack;
        if (stack.Count < 2)
            throw new ArgumentOutOfRangeException($"Swap index is out of stack bounds: 1/{stack.Count}");
        stack.Swap(0, 1);
        return null;
    }

    /// <summary>
    /// Left rotates the top three items on the evaluation stack.
    /// <see cref="OpCode.ROT"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 0, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Rot(ExecutionEngine engine, Instruction instruction)
    {
        // ROT: [a, b, c] -> [b, c, a] (c is top)
        // Equivalent to: swap(1,2), swap(0,1)
        var stack = engine.CurrentContext!.EvaluationStack;
        if (stack.Count < 3)
            throw new ArgumentOutOfRangeException($"Swap index is out of stack bounds: 2/{stack.Count}");
        stack.Swap(1, 2);
        stack.Swap(0, 1);
        return new OpCodePriceParams { Length = 2 };
    }

    /// <summary>
    /// The item n back in the stack is moved to the top.
    /// <see cref="OpCode.ROLL"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Roll(ExecutionEngine engine, Instruction instruction)
    {
        var n = (int)engine.Pop().GetInteger();
        if (n < 0)
            throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
        if (n == 0) return null;
        var x = engine.CurrentContext!.EvaluationStack.Remove<StackItem>(n);
        engine.Push(x);
        return new OpCodePriceParams { Length = n };
    }

    /// <summary>
    /// Reverses the order of the top 3 items on the evaluation stack.
    /// <see cref="OpCode.REVERSE3"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Reverse3(ExecutionEngine engine, Instruction instruction)
    {
        engine.CurrentContext!.EvaluationStack.Reverse(3);
        return new OpCodePriceParams { Length = 3 };
    }

    /// <summary>
    /// Reverses the order of the top 4 items on the evaluation stack.
    /// <see cref="OpCode.REVERSE4"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? Reverse4(ExecutionEngine engine, Instruction instruction)
    {
        engine.CurrentContext!.EvaluationStack.Reverse(4);
        return new OpCodePriceParams { Length = 4 };
    }

    /// <summary>
    /// Reverses the order of the top n items on the evaluation stack.
    /// <see cref="OpCode.REVERSEN"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual OpCodePriceParams? ReverseN(ExecutionEngine engine, Instruction instruction)
    {
        var n = (int)engine.Pop().GetInteger();
        engine.CurrentContext!.EvaluationStack.Reverse(n);
        return new OpCodePriceParams { Length = n };
    }
}
