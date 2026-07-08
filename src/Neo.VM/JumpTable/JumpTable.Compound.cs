// Copyright (C) 2015-2026 The Neo Project.
//
// JumpTable.Compound.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Array = System.Array;
using Buffer = Neo.VM.Types.Buffer;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM;

partial class JumpTable
{
    /// <summary>
    /// Packs a map from the evaluation stack.
    /// <see cref="OpCode.PACKMAP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 2n+1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void PackMap(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var size = (int)engine.Pop().GetInteger();
        if (size < 0 || size * 2 > engine.CurrentContext!.EvaluationStack.Count)
            throw new InvalidOperationException($"The map size is out of valid range, 2*{size}/[0, {engine.CurrentContext!.EvaluationStack.Count}].");
        Map map = new();
        var r = engine.ReferenceCounter.Count;
        for (var i = 0; i < size; i++)
        {
            var key = engine.PopNoRef<PrimitiveType>();
            var value = engine.PopNoRef();
            if (map.TryGetValue(key, out var oldValue))
            {
                engine.ReferenceCounter.RemoveStackReference(key);
                engine.ReferenceCounter.RemoveStackReference(oldValue);
            }
            map[key] = value;
        }
        map.StackReferences++;
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count, Length = size };
        engine.PushItemCounted(map, 1);
    }

    /// <summary>
    /// Packs a struct from the evaluation stack.
    /// <see cref="OpCode.PACKSTRUCT"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop n+1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void PackStruct(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var size = (int)engine.Pop().GetInteger();
        if (size < 0 || size > engine.CurrentContext!.EvaluationStack.Count)
            throw new InvalidOperationException($"The struct size is out of valid range, {size}/[0, {engine.CurrentContext!.EvaluationStack.Count}].");
        Struct @struct = new();
        for (var i = 0; i < size; i++)
        {
            var item = engine.PopNoRef();
            @struct.Add(item);
        }
        @struct.StackReferences++;
        engine.PushItemCounted(@struct, 1);
        runStats = new RunStats { Length = size };
    }

    /// <summary>
    /// Packs an array from the evaluation stack.
    /// <see cref="OpCode.PACK"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop n+1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Pack(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var size = (int)engine.Pop().GetInteger();
        if (size < 0 || size > engine.CurrentContext!.EvaluationStack.Count)
            throw new InvalidOperationException($"The array size is out of valid range, {size}/[0, {engine.CurrentContext!.EvaluationStack.Count}].");
        VMArray array = new();
        for (var i = 0; i < size; i++)
        {
            var item = engine.PopNoRef();
            array.Add(item);
        }
        array.StackReferences++;
        engine.PushItemCounted(array, 1);
        runStats = new RunStats { Length = size };
    }

    /// <summary>
    /// Unpacks a compound type from the evaluation stack.
    /// <see cref="OpCode.UNPACK"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 2n+1 or n+1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Unpack(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var compound = engine.PopNoRef<CompoundType>();
        compound.StackReferences--;
        // Decrease reference count by 1.
        engine.ReferenceCounter.RemoveStackReference(StackItem.Null);
        switch (compound)
        {
            case Map map:
                if (map.IsStackReferenced)
                {
                    foreach (var (key, value) in map.Reverse())
                    {
                        engine.Push(value);
                        engine.PushItemCounted(key, 1);
                    }
                }
                else
                {
                    foreach (var (key, value) in map.Reverse())
                    {
                        engine.PushItemCounted(value, 0);
                        engine.PushItemCounted(key, 0);
                    }
                }
                break;
            case VMArray array:
                if (array.IsStackReferenced)
                {
                    for (var i = array.Count - 1; i >= 0; i--)
                    {
                        engine.Push(array[i]);
                    }
                }
                else
                {
                    for (var i = array.Count - 1; i >= 0; i--)
                    {
                        engine.PushItemCounted(array[i], 0);
                    }
                }
                break;
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {compound.Type}");
        }
        engine.Push(compound.Count);
        runStats = new RunStats { Length = compound.Count };
    }

    /// <summary>
    /// Creates a new empty array with zero elements on the evaluation stack.
    /// <see cref="OpCode.NEWARRAY0"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>
    /// Pop 0, Push 1
    /// TODO: Change to NewNullArray method or add it?
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NewArray0(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        engine.Push(new VMArray());
        runStats = null;
    }

    /// <summary>
    /// Creates a new array with a specified number of elements on the evaluation stack.
    /// <see cref="OpCode.NEWARRAY"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NewArray(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var n = (int)engine.Pop().GetInteger();
        if (n < 0 || n > engine.Limits.MaxStackSize)
            throw new InvalidOperationException($"The array size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");
        var nullArray = new StackItem[n];
        Array.Fill(nullArray, StackItem.Null);
        var newArray = new VMArray(nullArray);
        newArray.StackReferences++;
        engine.PushItemCounted(newArray, n + 1);
        runStats = new RunStats { Type = StackItemType.Any, Length = n };
    }

    /// <summary>
    /// Creates a new array with a specified number of elements and a specified type on the evaluation stack.
    /// <see cref="OpCode.NEWARRAY_T"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NewArray_T(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var n = (int)engine.Pop().GetInteger();
        if (n < 0 || n > engine.Limits.MaxStackSize)
            throw new InvalidOperationException($"The array size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");

        var type = (StackItemType)instruction.TokenU8;
        if (!Enum.IsDefined(type))
            throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {instruction.TokenU8}");

        var item = instruction.TokenU8 switch
        {
            (byte)StackItemType.Boolean => StackItem.False,
            (byte)StackItemType.Integer => Integer.Zero,
            (byte)StackItemType.ByteString => ByteString.Empty,
            _ => StackItem.Null
        };
        var itemArray = new StackItem[n];
        Array.Fill(itemArray, item);
        var newArray = new VMArray(itemArray);
        newArray.StackReferences++;
        engine.PushItemCounted(newArray, n + 1);
        runStats = new RunStats { Type = type, Length = n };
    }

    /// <summary>
    /// Creates a new empty struct with zero elements on the evaluation stack.
    /// <see cref="OpCode.NEWSTRUCT0"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 0, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NewStruct0(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        engine.Push(new Struct());
        runStats = null;
    }

    /// <summary>
    /// Creates a new struct with a specified number of elements on the evaluation stack.
    /// <see cref="OpCode.NEWSTRUCT"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NewStruct(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var n = (int)engine.Pop().GetInteger();
        if (n < 0 || n > engine.Limits.MaxStackSize)
            throw new InvalidOperationException($"The struct size is out of valid range, {n}/[0, {engine.Limits.MaxStackSize}].");

        var nullArray = new StackItem[n];
        Array.Fill(nullArray, StackItem.Null);
        var newStruct = new Struct(nullArray);
        newStruct.StackReferences++;
        engine.PushItemCounted(newStruct, n + 1);
        runStats = new RunStats { Type = StackItemType.Any, Length = n };
    }

    /// <summary>
    /// Creates a new empty map on the evaluation stack.
    /// <see cref="OpCode.NEWMAP"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 0, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void NewMap(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        engine.Push(new Map());
        runStats = null;
    }

    /// <summary>
    /// Gets the size of the top item on the evaluation stack and pushes it onto the stack.
    /// <see cref="OpCode.SIZE"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Size(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        // TODO: we should be able to optimize by using peek instead of dup and pop
        var x = engine.Pop();
        switch (x)
        {
            case CompoundType compound:
                engine.Push(compound.Count);
                break;
            case PrimitiveType primitive:
                engine.Push(primitive.Size);
                break;
            case Buffer buffer:
                engine.Push(buffer.Size);
                break;
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Checks whether the top item on the evaluation stack has the specified key.
    /// <see cref="OpCode.HASKEY"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 2, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void HasKey(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var key = engine.Pop<PrimitiveType>();
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        // Check the type of the top item and perform the corresponding action.
        switch (x)
        {
            // For arrays, check if the index is within bounds and push the result onto the stack.
            case VMArray array:
                {
                    var index = key.GetInteger();
                    if (index < 0 || index >= engine.Limits.MaxItemSize)
                        throw new InvalidOperationException($"The index {index} is invalid for OpCode {instruction.OpCode}.");
                    engine.Push(index < array.Count);
                    break;
                }
            // For maps, check if the key exists and push the result onto the stack.
            case Map map:
                {
                    engine.Push(map.ContainsKey(key));
                    break;
                }
            // For buffers, check if the index is within bounds and push the result onto the stack.
            case Buffer buffer:
                {
                    var index = key.GetInteger();
                    if (index < 0 || index >= engine.Limits.MaxItemSize)
                        throw new InvalidOperationException($"The index {index} is invalid for OpCode {instruction.OpCode}.");
                    engine.Push(index < buffer.Size);
                    break;
                }
            // For byte strings, check if the index is within bounds and push the result onto the stack.
            case ByteString array:
                {
                    var index = key.GetInteger();
                    if (index < 0 || index >= engine.Limits.MaxItemSize)
                        throw new InvalidOperationException($"The index {index} is invalid for OpCode {instruction.OpCode}.");
                    engine.Push(index < array.Size);
                    break;
                }
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Retrieves the keys of a map and pushes them onto the evaluation stack as an array.
    /// <see cref="OpCode.KEYS"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Keys(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        var map = engine.Pop<Map>();
        var array = new VMArray(map.Keys);
        array.StackReferences++;
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count, Length = map.Count };
        engine.PushItemCounted(array, map.Count + 1);
    }

    /// <summary>
    /// Retrieves the values of a compound type and pushes them onto the evaluation stack as an array.
    /// <see cref="OpCode.VALUES"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Values(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var x = engine.PopNoRef();

        var nClonedItems = 0;
        var refsDelta = 0;
        IEnumerable<StackItem> values;
        bool isReferenced;

        switch (x)
        {
            case VMArray array:
                {
                    array.StackReferences--;
                    values = array;
                    isReferenced = array.IsStackReferenced;
                    break;
                }
            case Map map:
                {
                    map.StackReferences--;
                    values = map.Values;
                    isReferenced = map.IsStackReferenced;
                    if (!isReferenced)
                        // Decrease refcounter value by number of keys in map.
                        engine.ReferenceCounter.AddStackReference(StackItem.Null, -map.Count);
                    break;
                }
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        VMArray newArray = new();
        if (isReferenced)
        {
            var r = engine.ReferenceCounter.Count;
            foreach (var item in values)
            {
                var n = 0;
                var cpItem = item is Struct s ? s.Clone(engine.Limits, out n) : item;
                newArray.Add(cpItem);
                nClonedItems += n;
                engine.ReferenceCounter.AddStackReference(cpItem);
            }
            refsDelta = engine.ReferenceCounter.Count - r;
        }
        else
        {
            foreach (var item in values)
            {
                if (item is Struct s)
                {
                    var cpItem = s.Clone(engine.Limits, out int n);
                    var r = engine.ReferenceCounter.Count;
                    engine.ReferenceCounter.RemoveStackReference(item);
                    refsDelta += r - engine.ReferenceCounter.Count;
                    engine.ReferenceCounter.AddStackReference(cpItem);
                    newArray.Add(cpItem);
                    nClonedItems += n;
                    refsDelta += n;
                }
                else
                {
                    newArray.Add(item);
                }
            }
        }
        newArray.StackReferences++;
        engine.PushItemCounted(newArray, 0);
        runStats = new RunStats { RefsDelta = refsDelta, Length = newArray.Count, NClonedItems = nClonedItems };
    }

    /// <summary>
    /// Retrieves the item from an array, map, buffer, or byte string based on the specified key,
    /// and pushes it onto the evaluation stack.
    /// <see cref="OpCode.PICKITEM"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 2, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void PickItem(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var key = engine.Pop<PrimitiveType>();
        var r1 = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        StackItem item;
        switch (x)
        {
            case VMArray array:
                {
                    var index = key.GetInteger();
                    if (index < 0 || index >= array.Count)
                    {
                        var r3 = engine.ReferenceCounter.Count;
                        ExecuteThrow(engine, $"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).", out int refsDelta);
                        runStats = new RunStats { RefsDelta = r1 - r3 + refsDelta };
                        return;
                    }
                    item = array[(int)index];
                    break;
                }
            case Map map:
                {
                    if (!map.TryGetValue(key, out var value))
                    {
                        var r3 = engine.ReferenceCounter.Count;
                        ExecuteThrow(engine, $"Key {key} not found in {nameof(Map)}.", out int refsDelta);
                        runStats = new RunStats { RefsDelta = r1 - r3 + refsDelta };
                        return;
                    }
                    item = value;
                    break;
                }
            case PrimitiveType primitive:
                {
                    var byteArray = primitive.GetSpan();
                    var index = key.GetInteger();
                    if (index < 0 || index >= byteArray.Length)
                    {
                        var r3 = engine.ReferenceCounter.Count;
                        ExecuteThrow(engine, $"The index of {nameof(PrimitiveType)} is out of range, {index}/[0, {byteArray.Length}).", out int refsDelta);
                        runStats = new RunStats { RefsDelta = r1 - r3 + refsDelta };
                        return;
                    }
                    item = (BigInteger)byteArray[(int)index];
                    break;
                }
            case Buffer buffer:
                {
                    var index = key.GetInteger();
                    if (index < 0 || index >= buffer.Size)
                    {
                        var r3 = engine.ReferenceCounter.Count;
                        ExecuteThrow(engine, $"The index of {nameof(Buffer)} is out of range, {index}/[0, {buffer.Size}).", out int refsDelta);
                        runStats = new RunStats { RefsDelta = r1 - r3 + refsDelta };
                        return;
                    }
                    item = (BigInteger)buffer.InnerBuffer.Span[(int)index];
                    break;
                }
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        var r2 = engine.ReferenceCounter.Count;
        engine.Push(item);
        runStats = new RunStats { RefsDelta = r1 - r2 + engine.ReferenceCounter.Count - r2 };
    }

    /// <summary>
    /// Appends an item to the end of the specified array.
    /// <see cref="OpCode.APPEND"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 2, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Append(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r1 = engine.ReferenceCounter.Count;
        var newItem = engine.Pop();
        var array = engine.Pop<VMArray>();
        var nClonedItems = 0;
        if (newItem is Struct s) newItem = s.Clone(engine.Limits, out nClonedItems);
        array.Add(newItem);
        var r2 = engine.ReferenceCounter.Count;
        if (array.IsStackReferenced)
            engine.ReferenceCounter.AddStackReference(newItem);
        runStats = new RunStats { RefsDelta = r1 - r2 + engine.ReferenceCounter.Count - r2, NClonedItems = nClonedItems };
    }

    /// <summary>
    /// A value v, index n (or key) and an array (or map) are taken from main stack. Attribution array[n]=v (or map[n]=v) is performed.
    /// <see cref="OpCode.SETITEM"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 3, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetItem(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var value = engine.PopNoRef();
        var nClonedItems = 0;
        var (r1, r2) = (engine.ReferenceCounter.Count, engine.ReferenceCounter.Count);
        if (value is Struct s)
        {
            engine.ReferenceCounter.RemoveStackReference(value);
            value = s.Clone(engine.Limits, out nClonedItems);
            engine.ReferenceCounter.AddStackReference(value);
        }
        var r3 = engine.ReferenceCounter.Count;
        var key = engine.Pop<PrimitiveType>();
        var x = engine.Pop();
        switch (x)
        {
            case VMArray array:
                {
                    var index = key.GetInteger();
                    if (index < 0 || index >= array.Count)
                    {
                        engine.ReferenceCounter.RemoveStackReference(value);
                        var r4 = engine.ReferenceCounter.Count;
                        ExecuteThrow(engine, $"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).", out int refsDelta);
                        runStats = new RunStats { RefsDelta = (r1 - r2) + (r3 - r2) + (r3 - r4) + refsDelta, NClonedItems = nClonedItems };
                        return;
                    }
                    var i = (int)index;
                    if (array.IsStackReferenced)
                        engine.ReferenceCounter.RemoveStackReference(array[i]);
                    else
                        engine.ReferenceCounter.RemoveStackReference(value);
                    array[i] = value;
                    break;
                }
            case Map map:
                {
                    if (map.IsStackReferenced)
                    {
                        if (map.TryGetValue(key, out var value1))
                        {
                            engine.ReferenceCounter.RemoveStackReference(value1);
                        }
                        else
                        {
                            engine.ReferenceCounter.AddStackReference(key);
                        }
                    }
                    else
                        engine.ReferenceCounter.RemoveStackReference(value);
                    map[key] = value;
                    break;
                }
            case Buffer buffer:
                {
                    engine.ReferenceCounter.RemoveStackReference(value);
                    var index = key.GetInteger();
                    if (index < 0 || index >= buffer.Size)
                    {
                        var r4 = engine.ReferenceCounter.Count;
                        ExecuteThrow(engine, $"The index of {nameof(Buffer)} is out of range, {index}/[0, {buffer.Size}).", out int refsDelta);
                        runStats = new RunStats { RefsDelta = (r1 - r2) + (r3 - r2) + (r3 - r4) + refsDelta, NClonedItems = nClonedItems };
                        return;
                    }
                    if (value is not PrimitiveType p)
                        throw new InvalidOperationException($"Only primitive type values can be set in {nameof(Buffer)} in {instruction.OpCode}.");
                    var b = p.GetInteger();
                    if (b < sbyte.MinValue || b > byte.MaxValue)
                        throw new InvalidOperationException($"Overflow in {instruction.OpCode}, {b} is not a byte type.");
                    buffer.InnerBuffer.Span[(int)index] = (byte)b;
                    break;
                }
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        runStats = new RunStats { RefsDelta = (r1 - r2) + (r3 - r2) + (r3 - engine.ReferenceCounter.Count), NClonedItems = nClonedItems };
    }

    /// <summary>
    /// Reverses the order of items in the specified array or buffer.
    /// <see cref="OpCode.REVERSEITEMS"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void ReverseItems(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        int l;
        var x = engine.Pop();
        switch (x)
        {
            case VMArray array:
                array.Reverse();
                l = array.Count;
                break;
            case Buffer buffer:
                buffer.InnerBuffer.Span.Reverse();
                l = buffer.Size;
                break;
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        runStats = new RunStats { Type = x.Type, RefsDelta = r - engine.ReferenceCounter.Count, Length = l };
    }

    /// <summary>
    /// Removes the item at the specified index from the array or map.
    /// <see cref="OpCode.REMOVE"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 2, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Remove(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var key = engine.Pop<PrimitiveType>();
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop();
        var l = 0;
        switch (x)
        {
            case VMArray array:
                var index = key.GetInteger();
                if (index < 0 || index >= array.Count)
                    throw new InvalidOperationException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");

                var i = (int)index;
                var item = array[i];
                array.RemoveAt(i);
                l = array.Count - i;
                if (array.IsStackReferenced)
                    engine.ReferenceCounter.RemoveStackReference(item);
                break;
            case Map map:
                var old = map.Remove(key, out int idx);
                if (idx >= 0)
                    l = map.Count - idx;
                if (old is not null && map.IsStackReferenced)
                {
                    engine.ReferenceCounter.RemoveStackReference(key);
                    engine.ReferenceCounter.RemoveStackReference(old);
                }
                break;
            default:
                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
        }
        runStats = new RunStats { Type = x.Type, RefsDelta = r - engine.ReferenceCounter.Count, Length = l };
    }

    /// <summary>
    /// Clears all items from the compound type.
    /// <see cref="OpCode.CLEARITEMS"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 0</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void ClearItems(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r = engine.ReferenceCounter.Count;
        var x = engine.Pop<CompoundType>();
        var subItems = x.SubItems.ToList();
        x.Clear();
        if (x.IsStackReferenced)
        {
            foreach (var xSubItem in subItems)
            {
                engine.ReferenceCounter.RemoveStackReference(xSubItem);
            }
        }
        runStats = new RunStats { RefsDelta = r - engine.ReferenceCounter.Count };
    }

    /// <summary>
    /// Removes and returns the item at the top of the specified array.
    /// <see cref="OpCode.POPITEM"/>
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="instruction">The instruction being executed.</param>
    /// <param name="runStats">The opcode parameters for dynamic pricing.</param>
    /// <remarks>Pop 1, Push 1</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void PopItem(ExecutionEngine engine, Instruction instruction, out RunStats? runStats)
    {
        var r1 = engine.ReferenceCounter.Count;
        var x = engine.Pop<VMArray>();
        var index = x.Count - 1;
        var item = x[index];
        var r2 = engine.ReferenceCounter.Count;
        engine.Push(item);
        x.RemoveAt(index);
        if (x.IsStackReferenced)
            engine.ReferenceCounter.RemoveStackReference(item);
        runStats = new RunStats { RefsDelta = (r1 - r2) + (engine.ReferenceCounter.Count - r2) };
    }
}
