// Copyright (C) 2015-2026 The Neo Project.
//
// ReferenceCounter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;

namespace Neo.VM;

/// <summary>
/// Counts VM stack items the same way neo-go does: a single integer for the
/// total number of references, plus a per-compound <see cref="CompoundType.StackReferences"/>
/// count. Compound children are walked only when the compound becomes
/// referenced (0 → 1) or unreferenced (1 → 0). Unreferenced compounds are not
/// decremented, so cyclic graphs cannot drive the counter negative.
/// </summary>
public sealed class ReferenceCounter : IReferenceCounter
{
    private readonly ExecutionEngineLimits _limits;

    // Keeps the total count of references.
    private int _referencesCount = 0;

    /// <inheritdoc/>
    public int Count => _referencesCount;

    public ReferenceCounter(ExecutionEngineLimits? limits = null)
    {
        _limits = limits ?? ExecutionEngineLimits.Default;
    }

    /// <inheritdoc/>
    public void Inc(int count = 1)
    {
        _referencesCount += count;
    }

    /// <inheritdoc/>
    public void Dec(int count = 1)
    {
        _referencesCount -= count;
    }

    /// <inheritdoc/>
    public void AddStackReference(StackItem item, int count = 1)
    {
        _referencesCount += count;

        if (item is CompoundType compoundType)
        {
            compoundType.StackReferences += count;

            // First reference: count children (array/struct items, map keys and values).
            if (compoundType.StackReferences == count)
            {
                foreach (var subItem in compoundType.SubItems)
                {
                    AddStackReference(subItem);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void PostExecuteInstruction()
    {
        if (Count > _limits.MaxStackSize)
            throw new InvalidOperationException($"MaxStackSize exceed: {Count}/{_limits.MaxStackSize}");
    }

    /// <inheritdoc/>
    public void RemoveStackReference(StackItem item)
    {
        if (item is CompoundType compoundType)
        {
            // Skip compounds that are already unreferenced so cyclic graphs
            // cannot underflow the counter and bypass MaxStackSize.
            if (compoundType.IsStackReferenced)
            {
                _referencesCount--;
                compoundType.StackReferences--;

                if (compoundType.StackReferences == 0)
                {
                    foreach (var subItem in compoundType.SubItems)
                    {
                        RemoveStackReference(subItem);
                    }
                }
            }
        }
        else
        {
            _referencesCount--;
        }
    }
}
