// Copyright (C) 2015-2026 The Neo Project.
//
// TestArray.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections;

namespace Neo.VM.Benchmarks;

public class TestArray : CompoundType, IReadOnlyList<StackItem>
{
    protected readonly List<StackItem> InnerList;

    /// <summary>
    /// Get or set item in the array.
    /// </summary>
    /// <param name="index">The index of the item in the array.</param>
    /// <returns>The item at the specified index.</returns>
    public StackItem this[int index]
    {
        get => InnerList[index];
        set
        {
            if (IsReadOnly) throw new InvalidOperationException("The object is readonly.");
            ReferenceCounter?.RemoveReference(InnerList[index], this);
            InnerList[index] = value;
            ReferenceCounter?.AddReference(value, this);
        }
    }

    /// <summary>
    /// The number of items in the array.
    /// </summary>
    public override int Count => InnerList.Count;
    public override IEnumerable<StackItem> SubItems => InnerList;
    public override int SubItemsCount => InnerList.Count;
    public override StackItemType Type => StackItemType.Array;

    /// <summary>
    /// Create an array containing the specified items.
    /// </summary>
    /// <param name="items">The items to be included in the array.</param>
    public TestArray(IEnumerable<StackItem>? items = null)
        : this(null, items)
    {
    }

    /// <summary>
    /// Create an array containing the specified items. And make the array use the specified <see cref="IReferenceCounter"/>.
    /// </summary>
    /// <param name="referenceCounter">The <see cref="IReferenceCounter"/> to be used by this array.</param>
    /// <param name="items">The items to be included in the array.</param>
    public TestArray(IReferenceCounter? referenceCounter, IEnumerable<StackItem>? items = null)
        : base(referenceCounter)
    {
        InnerList = items switch
        {
            null => new List<StackItem>(),
            List<StackItem> list => list,
            _ => new List<StackItem>(items)
        };
        if (referenceCounter != null)
            foreach (StackItem item in InnerList)
                referenceCounter.AddReference(item, this);
    }

    /// <summary>
    /// Add a new item at the end of the array.
    /// </summary>
    /// <param name="item">The item to be added.</param>
    public void Add(StackItem item)
    {
        if (IsReadOnly) throw new InvalidOperationException("The object is readonly.");
        InnerList.Add(item);
        ReferenceCounter?.AddReference(item, this);
    }

    public override void Clear()
    {
        if (IsReadOnly) throw new InvalidOperationException("The object is readonly.");
        if (ReferenceCounter != null)
            foreach (StackItem item in InnerList)
                ReferenceCounter.RemoveReference(item, this);
        InnerList.Clear();
    }

    public override StackItem ConvertTo(StackItemType type)
    {
        if (Type == StackItemType.Array && type == StackItemType.Struct)
            return new Struct(ReferenceCounter, new List<StackItem>(InnerList));
        return base.ConvertTo(type);
    }

    internal sealed override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
    {
        if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
        var result = this is TestStruct ? new TestStruct(ReferenceCounter) : new TestArray(ReferenceCounter);
        refMap.Add(this, result);
        foreach (StackItem item in InnerList)
            result.Add(item.DeepCopy(refMap, asImmutable));
        result.IsReadOnly = true;
        return result;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<StackItem> GetEnumerator()
    {
        return InnerList.GetEnumerator();
    }

    /// <summary>
    /// Remove the item at the specified index.
    /// </summary>
    /// <param name="index">The index of the item to be removed.</param>
    public void RemoveAt(int index)
    {
        if (IsReadOnly) throw new InvalidOperationException("The object is readonly.");
        ReferenceCounter?.RemoveReference(InnerList[index], this);
        InnerList.RemoveAt(index);
    }

    /// <summary>
    /// Reverse all items in the array.
    /// </summary>
    public void Reverse()
    {
        if (IsReadOnly) throw new InvalidOperationException("The object is readonly.");
        InnerList.Reverse();
    }
}
