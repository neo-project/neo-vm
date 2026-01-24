// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ReferenceCounterEquivalence.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.Test;

/// <summary>
/// Ensures that the legacy Tarjan-based <see cref="ReferenceCounter"/> and the
/// mark-sweep <see cref="MarkSweepReferenceCounter"/> behave identically.
/// </summary>
[TestClass]
public class UT_ReferenceCounterEquivalence
{
    private static void AssertEquivalent(Func<IReferenceCounter, IReadOnlyList<int>> scenario)
    {
        var legacy = scenario(new ReferenceCounter()).ToArray();
        var markSweep = scenario(new MarkSweepReferenceCounter()).ToArray();

        CollectionAssert.AreEqual(legacy, markSweep);
    }

    #region Core RC API equivalence

    [TestMethod]
    public void TestEquivalence_BasicStackReferences()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var array = new Array(rc);

            counts.Add(rc.Count); // 0

            rc.AddStackReference(array);
            counts.Add(rc.Count); // 1

            rc.AddStackReference(array, 2);
            counts.Add(rc.Count); // 3

            rc.RemoveStackReference(array);
            counts.Add(rc.Count); // 2

            counts.Add(rc.CheckZeroReferred()); // still 2

            rc.RemoveStackReference(array);
            rc.RemoveStackReference(array);
            counts.Add(rc.Count); // 0

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_ParentChildLifecycle()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var parent = new Array(rc);
            var child = new Array(rc);

            rc.AddStackReference(parent);
            counts.Add(rc.Count); // 1

            parent.Add(child);
            counts.Add(rc.Count); // 2

            rc.RemoveStackReference(parent);
            counts.Add(rc.Count); // 1

            counts.Add(rc.CheckZeroReferred()); // 0 after sweep

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_MultipleParents()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var p1 = new Array(rc);
            var p2 = new Array(rc);
            var child = new Array(rc);

            rc.AddStackReference(p1);
            rc.AddStackReference(p2);
            counts.Add(rc.Count); // 2

            p1.Add(child);
            counts.Add(rc.Count); // 3

            p2.Add(child);
            counts.Add(rc.Count); // 4

            rc.RemoveStackReference(p1);
            counts.Add(rc.Count); // 3

            counts.Add(rc.CheckZeroReferred()); // child still reachable via p2

            rc.RemoveStackReference(p2);
            counts.Add(rc.Count); // 2

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_CircularReferences()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var a = new Array(rc);
            var b = new Array(rc);

            rc.AddStackReference(a);
            rc.AddStackReference(b);
            counts.Add(rc.Count); // 2

            a.Add(b);
            b.Add(a);
            counts.Add(rc.Count); // 4

            rc.RemoveStackReference(a);
            rc.RemoveStackReference(b);
            counts.Add(rc.Count); // 2 (object refs only)

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_MixedStackAndObjectReferences()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var parent = new Array(rc);
            var child = new Array(rc);

            rc.AddStackReference(parent);
            rc.AddStackReference(child);
            parent.Add(child);
            counts.Add(rc.Count); // parent + child stack refs + object ref = 3

            rc.RemoveStackReference(child);
            counts.Add(rc.Count); // 2

            counts.Add(rc.CheckZeroReferred()); // child still reachable from parent

            rc.RemoveStackReference(parent);
            counts.Add(rc.Count); // 1

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_AddReferenceWithoutSubItem()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var parent = new Array(rc);
            var child = new Array(rc);

            rc.AddStackReference(parent);
            rc.AddReference(child, parent);
            counts.Add(rc.CheckZeroReferred()); // child reachable via parent

            rc.RemoveReference(child, parent);
            counts.Add(rc.Count); // parent stack ref only

            rc.RemoveStackReference(parent);
            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_ExplicitMultiParentReferences()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var p1 = new Array(rc);
            var p2 = new Array(rc);
            var child = new Array(rc);

            rc.AddStackReference(p1);
            rc.AddStackReference(p2);
            counts.Add(rc.Count); // 2

            rc.AddReference(child, p1);
            rc.AddReference(child, p2);
            counts.Add(rc.Count); // 4

            rc.RemoveReference(child, p1);
            counts.Add(rc.Count); // 3

            rc.RemoveStackReference(p1);
            counts.Add(rc.Count); // 2

            counts.Add(rc.CheckZeroReferred()); // child still reachable via p2

            rc.RemoveReference(child, p2);
            counts.Add(rc.Count); // 1

            rc.RemoveStackReference(p2);
            counts.Add(rc.Count); // 0

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_BufferTracking()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var root = new Array(rc);
            var buffer = new Buffer(8);

            rc.AddStackReference(root);
            counts.Add(rc.Count); // 1

            root.Add(buffer);
            counts.Add(rc.Count); // 2 (buffer is tracked)

            rc.RemoveStackReference(root);
            counts.Add(rc.Count); // 1

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_MapAddRemove()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var map = new Map(rc);
            rc.AddStackReference(map);
            counts.Add(rc.Count); // 1

            map[(ByteString)"k1"] = 1;
            counts.Add(rc.Count); // 3 (map + key + value)

            map[(ByteString)"k2"] = new Array(rc);
            counts.Add(rc.Count); // 5 (map + 2 keys + 2 values)

            map.Remove((ByteString)"k1");
            counts.Add(rc.Count); // 3

            rc.RemoveStackReference(map);
            counts.Add(rc.Count); // 2

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_DeepNestingCleanup()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var root = new Array(rc);
            rc.AddStackReference(root);

            var current = root;
            for (int i = 0; i < 20; i++)
            {
                var next = new Array(rc);
                current.Add(next);
                current = next;
            }

            counts.Add(rc.Count); // root + 20 nested

            rc.RemoveStackReference(root);
            counts.Add(rc.Count);

            counts.Add(rc.CheckZeroReferred()); // 0 after cleanup

            return counts;
        });
    }

    [TestMethod]
    public void TestEquivalence_ZeroReferredCacheLikeScenario()
    {
        AssertEquivalent(rc =>
        {
            List<int> counts = new();
            var parent = new Array(rc);
            var child = new Array(rc);
            rc.AddStackReference(parent);
            rc.AddStackReference(child);

            // First sweep builds Tarjan cache in legacy RC.
            counts.Add(rc.CheckZeroReferred());

            parent.Add(child);
            counts.Add(rc.Count);

            parent.RemoveAt(0);
            counts.Add(rc.Count);

            rc.RemoveStackReference(parent);
            rc.RemoveStackReference(child);
            counts.Add(rc.Count);

            counts.Add(rc.CheckZeroReferred()); // 0

            return counts;
        });
    }

    #endregion

    #region Deterministic fuzzing

    [TestMethod]
    public void TestEquivalence_DeterministicFuzz()
    {
        AssertEquivalent(ScenarioDeterministicFuzz);
    }

    [TestMethod]
    public void TestEquivalence_RandomFuzzSeeds()
    {
        int[] seeds = { 1, 2, 3, 5, 8, 13, 42 };
        const int steps = 600;
        foreach (int seed in seeds)
        {
            var legacy = ScenarioRandomFuzzRoots(new ReferenceCounter(), seed, steps).ToArray();
            var markSweep = ScenarioRandomFuzzRoots(new MarkSweepReferenceCounter(), seed, steps).ToArray();
            CollectionAssert.AreEqual(legacy, markSweep, $"Seed {seed} mismatch.");
        }
    }

    [TestMethod]
    public void TestEquivalence_RandomFuzzSeeds_Extended()
    {
        int[] seeds = { 21, 34, 55, 89, 144, 233, 377, 610 };
        const int steps = 1200;
        const int checkInterval = 29;
        foreach (int seed in seeds)
        {
            var legacy = ScenarioRandomFuzzRoots(new ReferenceCounter(), seed, steps, checkInterval).ToArray();
            var markSweep = ScenarioRandomFuzzRoots(new MarkSweepReferenceCounter(), seed, steps, checkInterval).ToArray();
            CollectionAssert.AreEqual(legacy, markSweep, $"Seed {seed} mismatch.");
        }
    }

    [TestMethod]
    [TestCategory("ComplexFuzz")]
    public void TestEquivalence_ComplexFuzz()
    {
        int[] seeds = { 17, 73, 2027 };
        const int steps = 20000;
        const int checkInterval = 23;
        foreach (int seed in seeds)
        {
            var legacy = ScenarioComplexFuzz(new ReferenceCounter(), seed, steps, checkInterval).ToArray();
            var markSweep = ScenarioComplexFuzz(new MarkSweepReferenceCounter(), seed, steps, checkInterval).ToArray();
            CollectionAssert.AreEqual(legacy, markSweep, $"Seed {seed} mismatch.");
        }
    }

    [TestMethod]
    public void TestEquivalence_LongRunFuzz()
    {
        int[] seeds = { 101, 404, 808 };
        const int steps = 3000;
        const int checkInterval = 31;
        foreach (int seed in seeds)
        {
            var legacy = ScenarioRandomFuzzRoots(new ReferenceCounter(), seed, steps, checkInterval).ToArray();
            var markSweep = ScenarioRandomFuzzRoots(new MarkSweepReferenceCounter(), seed, steps, checkInterval).ToArray();
            CollectionAssert.AreEqual(legacy, markSweep, $"Seed {seed} mismatch.");
        }
    }

    [TestMethod]
    [TestCategory("LongFuzz")]
    public void TestEquivalence_VeryLongRunFuzz()
    {
        int[] seeds = { 2021, 9091 };
        int steps = GetFuzzSteps("NEO_VM_LONG_FUZZ_STEPS", 200000, 10000);
        const int checkInterval = 47;
        foreach (int seed in seeds)
        {
            var legacy = ScenarioRandomFuzzRoots(new ReferenceCounter(), seed, steps, checkInterval).ToArray();
            var markSweep = ScenarioRandomFuzzRoots(new MarkSweepReferenceCounter(), seed, steps, checkInterval).ToArray();
            CollectionAssert.AreEqual(legacy, markSweep, $"Seed {seed} mismatch.");
        }
    }

    [TestMethod]
    [TestCategory("StressFuzz")]
    public void TestEquivalence_StressFuzz()
    {
        int[] seeds = { 424242 };
        int steps = GetFuzzSteps("NEO_VM_STRESS_FUZZ_STEPS", 500000, 50000);
        const int checkInterval = 61;
        foreach (int seed in seeds)
        {
            var legacy = ScenarioRandomFuzzRoots(new ReferenceCounter(), seed, steps, checkInterval).ToArray();
            var markSweep = ScenarioRandomFuzzRoots(new MarkSweepReferenceCounter(), seed, steps, checkInterval).ToArray();
            CollectionAssert.AreEqual(legacy, markSweep, $"Seed {seed} mismatch.");
        }
    }

    [TestMethod]
    public void TestEquivalence_ExplicitReferenceChurn()
    {
        AssertEquivalent(ScenarioExplicitReferenceChurn);
    }

    private static IReadOnlyList<int> ScenarioDeterministicFuzz(IReferenceCounter rc)
    {
        const int steps = 200;
        var rng = new Random(12345);
        List<int> counts = new();

        List<Array> objects = new();
        List<StackItem> stack = new();

        for (int i = 0; i < steps; i++)
        {
            int op = rng.Next(6);
            switch (op)
            {
                case 0:
                    {
                        var a = new Array(rc);
                        objects.Add(a);
                        rc.AddStackReference(a);
                        stack.Add(a);
                        break;
                    }
                case 1:
                    if (stack.Count > 0)
                    {
                        var item = stack[^1];
                        stack.RemoveAt(stack.Count - 1);
                        rc.RemoveStackReference(item);
                    }
                    break;
                case 2:
                    {
                        var live = stack.OfType<Array>().Distinct().ToArray();
                        if (live.Length >= 2)
                        {
                            var parent = live[rng.Next(live.Length)];
                            var child = live[rng.Next(live.Length)];
                            if (!ReferenceEquals(parent, child))
                                parent.Add(child);
                        }
                        break;
                    }
                case 3:
                    {
                        var live = stack.OfType<Array>().Distinct().ToArray();
                        if (live.Length > 0)
                        {
                            var parent = live[rng.Next(live.Length)];
                            if (parent.Count > 0)
                                parent.RemoveAt(rng.Next(parent.Count));
                        }
                        break;
                    }
                case 4:
                    {
                        var live = stack.OfType<Array>().Distinct().ToArray();
                        if (live.Length > 0)
                        {
                            var parent = live[rng.Next(live.Length)];
                            parent.Add((StackItem)rng.Next(1000));
                        }
                        break;
                    }
                case 5:
                    {
                        var live = stack.OfType<Array>().Distinct().ToArray();
                        if (live.Length > 0)
                        {
                            var parent = live[rng.Next(live.Length)];
                            parent.Add(new Buffer(1));
                        }
                        break;
                    }
            }

            if (i % 10 == 0)
                rc.CheckZeroReferred();

            counts.Add(rc.Count);
        }

        counts.Add(rc.CheckZeroReferred());
        return counts;
    }

    private static IReadOnlyList<int> ScenarioComplexFuzz(IReferenceCounter rc, int seed, int steps, int checkInterval = 23)
    {
        var rng = new Random(seed);
        List<int> counts = new(steps + steps / checkInterval + 8);

        var stackCounts = new Dictionary<StackItem, int>(ReferenceEqualityComparer.Instance);
        var stackItems = new List<StackItem>();
        var stackIndex = new Dictionary<StackItem, int>(ReferenceEqualityComparer.Instance);

        var edgeComparer = new EdgeKeyComparer();
        var edgeCounts = new Dictionary<EdgeKey, int>(edgeComparer);
        var activeEdges = new List<EdgeKey>();

        void AddStackReference(StackItem item, int count = 1)
        {
            rc.AddStackReference(item, count);
            if (stackCounts.TryGetValue(item, out var existing))
            {
                stackCounts[item] = existing + count;
                return;
            }
            stackCounts[item] = count;
            stackIndex[item] = stackItems.Count;
            stackItems.Add(item);
        }

        void RemoveExplicitEdgesFor(StackItem item)
        {
            for (int i = activeEdges.Count - 1; i >= 0; i--)
            {
                var edge = activeEdges[i];
                if (!ReferenceEquals(edge.Parent, item) && !ReferenceEquals(edge.Child, item))
                    continue;

                int count = edgeCounts[edge];
                for (int j = 0; j < count; j++)
                    rc.RemoveReference(edge.Child, edge.Parent);

                edgeCounts.Remove(edge);
                int lastIndex = activeEdges.Count - 1;
                if (i != lastIndex)
                    activeEdges[i] = activeEdges[lastIndex];
                activeEdges.RemoveAt(lastIndex);
            }
        }

        void RemoveStackReference()
        {
            if (stackItems.Count == 0) return;

            int index = rng.Next(stackItems.Count);
            StackItem item = stackItems[index];
            rc.RemoveStackReference(item);

            int count = stackCounts[item] - 1;
            if (count > 0)
            {
                stackCounts[item] = count;
                return;
            }

            stackCounts.Remove(item);
            stackIndex.Remove(item);

            int lastIndex = stackItems.Count - 1;
            if (index != lastIndex)
            {
                var lastItem = stackItems[lastIndex];
                stackItems[index] = lastItem;
                stackIndex[lastItem] = index;
            }
            stackItems.RemoveAt(lastIndex);

            RemoveExplicitEdgesFor(item);
        }

        Array TryPickStackArray()
        {
            if (stackItems.Count == 0) return null;
            for (int i = 0; i < 4; i++)
            {
                var candidate = stackItems[rng.Next(stackItems.Count)];
                if (candidate is Array array)
                    return array;
            }
            foreach (var item in stackItems)
            {
                if (item is Array array)
                    return array;
            }
            return null;
        }

        Map TryPickStackMap()
        {
            if (stackItems.Count == 0) return null;
            for (int i = 0; i < 4; i++)
            {
                var candidate = stackItems[rng.Next(stackItems.Count)];
                if (candidate is Map map)
                    return map;
            }
            foreach (var item in stackItems)
            {
                if (item is Map map)
                    return map;
            }
            return null;
        }

        CompoundType TryPickStackParent()
        {
            if (stackItems.Count == 0) return null;
            for (int i = 0; i < 4; i++)
            {
                var candidate = stackItems[rng.Next(stackItems.Count)];
                if (candidate is CompoundType compound)
                    return compound;
            }
            foreach (var item in stackItems)
            {
                if (item is CompoundType compound)
                    return compound;
            }
            return null;
        }

        StackItem TryPickStackItem()
        {
            if (stackItems.Count == 0) return null;
            return stackItems[rng.Next(stackItems.Count)];
        }

        CompoundType TryPickNestedCompound()
        {
            var parent = TryPickStackParent();
            if (parent == null) return null;

            if (parent is Array array)
            {
                if (array.Count == 0) return null;
                return array[rng.Next(array.Count)] as CompoundType;
            }
            if (parent is Map map)
            {
                if (map.Count == 0) return null;
                int index = rng.Next(map.Count);
                int i = 0;
                foreach (var value in map.Values)
                {
                    if (i++ == index)
                        return value as CompoundType;
                }
            }
            return null;
        }

        PrimitiveType CreateKey()
        {
            if (rng.Next(100) < 60)
                return (ByteString)$"k{seed}_{rng.Next(16384)}";
            return new Integer(rng.Next(1, 1_000_000));
        }

        StackItem CreateValue()
        {
            int roll = rng.Next(100);
            if (stackItems.Count > 0 && roll < 35)
                return stackItems[rng.Next(stackItems.Count)];
            if (roll < 50)
                return (StackItem)rng.Next(1000);
            if (roll < 65)
                return (StackItem)(ByteString)$"v{seed}_{rng.Next(4096)}";
            if (roll < 75)
                return new Buffer(rng.Next(1, 64));
            return rng.Next(4) switch
            {
                0 => new Array(rc),
                1 => new Map(rc),
                2 => new Struct(rc),
                _ => new Buffer(rng.Next(1, 32))
            };
        }

        void AddExplicitEdge(StackItem child, CompoundType parent)
        {
            rc.AddReference(child, parent);
            var key = new EdgeKey(child, parent);
            if (edgeCounts.TryGetValue(key, out var count))
            {
                edgeCounts[key] = count + 1;
                return;
            }
            edgeCounts[key] = 1;
            activeEdges.Add(key);
        }

        void RemoveExplicitEdge()
        {
            if (activeEdges.Count == 0) return;
            int index = rng.Next(activeEdges.Count);
            var key = activeEdges[index];
            rc.RemoveReference(key.Child, key.Parent);

            int count = edgeCounts[key] - 1;
            if (count > 0)
            {
                edgeCounts[key] = count;
                return;
            }

            edgeCounts.Remove(key);
            int lastIndex = activeEdges.Count - 1;
            if (index != lastIndex)
                activeEdges[index] = activeEdges[lastIndex];
            activeEdges.RemoveAt(lastIndex);
        }

        for (int step = 0; step < steps; step++)
        {
            switch (rng.Next(18))
            {
                case 0:
                    AddStackReference(new Array(rc));
                    break;
                case 1:
                    AddStackReference(new Map(rc));
                    break;
                case 2:
                    AddStackReference(new Struct(rc));
                    break;
                case 3:
                    AddStackReference(new Buffer(rng.Next(1, 64)));
                    break;
                case 4:
                    {
                        var item = TryPickStackItem();
                        if (item != null)
                            AddStackReference(item, rng.Next(1, 3));
                        break;
                    }
                case 5:
                    RemoveStackReference();
                    break;
                case 6:
                    {
                        var array = TryPickStackArray();
                        if (array != null)
                            array.Add(CreateValue());
                        break;
                    }
                case 7:
                    {
                        var array = TryPickStackArray();
                        if (array != null && array.Count > 0)
                            array.RemoveAt(rng.Next(array.Count));
                        break;
                    }
                case 8:
                    {
                        var array = TryPickStackArray();
                        if (array != null && array.Count > 0)
                            array[rng.Next(array.Count)] = CreateValue();
                        break;
                    }
                case 9:
                    {
                        var array = TryPickStackArray();
                        if (array != null)
                        {
                            if (array.Count > 1 && rng.Next(100) < 60)
                                array.Reverse();
                            else if (array.Count > 0 && rng.Next(100) < 30)
                                array.Clear();
                        }
                        break;
                    }
                case 10:
                    {
                        var map = TryPickStackMap();
                        if (map != null)
                            map[CreateKey()] = CreateValue();
                        break;
                    }
                case 11:
                    {
                        var map = TryPickStackMap();
                        if (map != null && map.Count > 0)
                        {
                            var keys = map.Keys.ToArray();
                            map.Remove(keys[rng.Next(keys.Length)]);
                        }
                        break;
                    }
                case 12:
                    {
                        var map = TryPickStackMap();
                        if (map != null && map.Count > 0 && rng.Next(100) < 40)
                            map.Clear();
                        break;
                    }
                case 13:
                    {
                        var nested = TryPickNestedCompound();
                        if (nested is Array array)
                        {
                            array.Add(CreateValue());
                        }
                        else if (nested is Map map)
                        {
                            map[CreateKey()] = CreateValue();
                        }
                        break;
                    }
                case 14:
                    {
                        var source = TryPickStackArray();
                        var destination = TryPickStackArray();
                        if (source != null && destination != null && source.Count > 0 && !ReferenceEquals(source, destination))
                        {
                            int index = rng.Next(source.Count);
                            var item = source[index];
                            destination.Add(item);
                            source.RemoveAt(index);
                        }
                        break;
                    }
                case 15:
                    {
                        var map = TryPickStackMap();
                        var array = TryPickStackArray();
                        if (map != null && array != null && !ReferenceEquals(map, array))
                        {
                            map[CreateKey()] = array;
                            array.Add(map);
                        }
                        break;
                    }
                case 16:
                    {
                        var parent = TryPickStackParent();
                        var child = TryPickStackItem();
                        if (parent != null && child != null)
                            AddExplicitEdge(child, parent);
                        break;
                    }
                case 17:
                    RemoveExplicitEdge();
                    break;
            }

            if (step % checkInterval == 0)
                counts.Add(rc.CheckZeroReferred());

            counts.Add(rc.Count);
        }

        counts.Add(rc.CheckZeroReferred());
        return counts;
    }

    private static IReadOnlyList<int> ScenarioRandomFuzzRoots(IReferenceCounter rc, int seed, int steps, int checkInterval = 17)
    {
        var rng = new Random(seed);
        List<int> counts = new(steps + steps / checkInterval + 8);

        var stackCounts = new Dictionary<StackItem, int>(ReferenceEqualityComparer.Instance);
        var stackItems = new List<StackItem>();
        var stackIndex = new Dictionary<StackItem, int>(ReferenceEqualityComparer.Instance);

        var edgeComparer = new EdgeKeyComparer();
        var edgeCounts = new Dictionary<EdgeKey, int>(edgeComparer);
        var activeEdges = new List<EdgeKey>();

        void AddStackReference(StackItem item)
        {
            rc.AddStackReference(item);
            if (stackCounts.TryGetValue(item, out var count))
            {
                stackCounts[item] = count + 1;
                return;
            }
            stackCounts[item] = 1;
            stackIndex[item] = stackItems.Count;
            stackItems.Add(item);
        }

        void RemoveExplicitEdgesFor(StackItem item)
        {
            for (int i = activeEdges.Count - 1; i >= 0; i--)
            {
                var edge = activeEdges[i];
                if (!ReferenceEquals(edge.Parent, item) && !ReferenceEquals(edge.Child, item))
                    continue;

                int count = edgeCounts[edge];
                for (int j = 0; j < count; j++)
                    rc.RemoveReference(edge.Child, edge.Parent);

                edgeCounts.Remove(edge);
                int lastIndex = activeEdges.Count - 1;
                if (i != lastIndex)
                    activeEdges[i] = activeEdges[lastIndex];
                activeEdges.RemoveAt(lastIndex);
            }
        }

        void RemoveStackReference()
        {
            if (stackItems.Count == 0) return;

            int index = rng.Next(stackItems.Count);
            StackItem item = stackItems[index];
            rc.RemoveStackReference(item);

            int count = stackCounts[item] - 1;
            if (count > 0)
            {
                stackCounts[item] = count;
                return;
            }

            stackCounts.Remove(item);
            stackIndex.Remove(item);

            int lastIndex = stackItems.Count - 1;
            if (index != lastIndex)
            {
                var lastItem = stackItems[lastIndex];
                stackItems[index] = lastItem;
                stackIndex[lastItem] = index;
            }
            stackItems.RemoveAt(lastIndex);

            RemoveExplicitEdgesFor(item);
        }

        Array TryPickStackArray()
        {
            if (stackItems.Count == 0) return null;
            for (int i = 0; i < 4; i++)
            {
                var candidate = stackItems[rng.Next(stackItems.Count)];
                if (candidate is Array array)
                    return array;
            }
            foreach (var item in stackItems)
            {
                if (item is Array array)
                    return array;
            }
            return null;
        }

        Map TryPickStackMap()
        {
            if (stackItems.Count == 0) return null;
            for (int i = 0; i < 4; i++)
            {
                var candidate = stackItems[rng.Next(stackItems.Count)];
                if (candidate is Map map)
                    return map;
            }
            foreach (var item in stackItems)
            {
                if (item is Map map)
                    return map;
            }
            return null;
        }

        CompoundType TryPickStackParent()
        {
            if (stackItems.Count == 0) return null;
            for (int i = 0; i < 4; i++)
            {
                var candidate = stackItems[rng.Next(stackItems.Count)];
                if (candidate is CompoundType compound)
                    return compound;
            }
            foreach (var item in stackItems)
            {
                if (item is CompoundType compound)
                    return compound;
            }
            return null;
        }

        StackItem TryPickStackItem()
        {
            if (stackItems.Count == 0) return null;
            return stackItems[rng.Next(stackItems.Count)];
        }

        StackItem CreateValue()
        {
            int roll = rng.Next(100);
            if (stackItems.Count > 0 && roll < 45)
                return stackItems[rng.Next(stackItems.Count)];
            if (roll < 60)
                return (StackItem)rng.Next(1000);
            if (roll < 75)
                return (StackItem)(ByteString)$"v{seed}_{rng.Next(2048)}";
            if (roll < 85)
                return new Buffer(rng.Next(1, 32));
            return rng.Next(3) switch
            {
                0 => new Array(rc),
                1 => new Map(rc),
                _ => new Struct(rc)
            };
        }

        void AddExplicitEdge(StackItem child, CompoundType parent)
        {
            rc.AddReference(child, parent);
            var key = new EdgeKey(child, parent);
            if (edgeCounts.TryGetValue(key, out var count))
            {
                edgeCounts[key] = count + 1;
                return;
            }
            edgeCounts[key] = 1;
            activeEdges.Add(key);
        }

        void RemoveExplicitEdge()
        {
            if (activeEdges.Count == 0) return;
            int index = rng.Next(activeEdges.Count);
            var key = activeEdges[index];
            rc.RemoveReference(key.Child, key.Parent);

            int count = edgeCounts[key] - 1;
            if (count > 0)
            {
                edgeCounts[key] = count;
                return;
            }

            edgeCounts.Remove(key);
            int lastIndex = activeEdges.Count - 1;
            if (index != lastIndex)
                activeEdges[index] = activeEdges[lastIndex];
            activeEdges.RemoveAt(lastIndex);
        }

        for (int step = 0; step < steps; step++)
        {
            switch (rng.Next(12))
            {
                case 0:
                    AddStackReference(new Array(rc));
                    break;
                case 1:
                    AddStackReference(new Map(rc));
                    break;
                case 2:
                    AddStackReference(new Struct(rc));
                    break;
                case 3:
                    AddStackReference(new Buffer(rng.Next(1, 32)));
                    break;
                case 4:
                    RemoveStackReference();
                    break;
                case 5:
                    {
                        var array = TryPickStackArray();
                        if (array != null)
                            array.Add(CreateValue());
                        break;
                    }
                case 6:
                    {
                        var array = TryPickStackArray();
                        if (array != null && array.Count > 0)
                            array.RemoveAt(rng.Next(array.Count));
                        break;
                    }
                case 7:
                    {
                        var array = TryPickStackArray();
                        if (array != null && array.Count > 0)
                            array[rng.Next(array.Count)] = CreateValue();
                        break;
                    }
                case 8:
                    {
                        var map = TryPickStackMap();
                        if (map != null)
                            map[(ByteString)$"k{seed}_{rng.Next(2048)}"] = CreateValue();
                        break;
                    }
                case 9:
                    {
                        var map = TryPickStackMap();
                        if (map != null && map.Count > 0)
                        {
                            var keys = map.Keys.ToArray();
                            map.Remove(keys[rng.Next(keys.Length)]);
                        }
                        break;
                    }
                case 10:
                    {
                        var parent = TryPickStackParent();
                        var child = TryPickStackItem();
                        if (parent != null && child != null)
                            AddExplicitEdge(child, parent);
                        break;
                    }
                case 11:
                    RemoveExplicitEdge();
                    break;
            }

            if (step % checkInterval == 0)
                counts.Add(rc.CheckZeroReferred());

            counts.Add(rc.Count);
        }

        counts.Add(rc.CheckZeroReferred());
        return counts;
    }

    private static IReadOnlyList<int> ScenarioExplicitReferenceChurn(IReferenceCounter rc)
    {
        const int steps = 2000;
        var rng = new Random(202401);
        List<int> counts = new(steps + 32);

        var parent = new Array(rc);
        var child = new Array(rc);
        rc.AddStackReference(parent);
        rc.AddStackReference(child);

        int outstanding = 0;
        for (int i = 0; i < steps; i++)
        {
            if (rng.Next(100) < 60)
            {
                rc.AddReference(child, parent);
                outstanding++;
            }
            else if (outstanding > 0)
            {
                rc.RemoveReference(child, parent);
                outstanding--;
            }

            if (i % 23 == 0)
                counts.Add(rc.CheckZeroReferred());

            counts.Add(rc.Count);
        }

        while (outstanding-- > 0)
            rc.RemoveReference(child, parent);

        rc.RemoveStackReference(child);
        rc.RemoveStackReference(parent);
        counts.Add(rc.CheckZeroReferred());

        return counts;
    }

    private static int GetFuzzSteps(string envName, int defaultSteps, int minSteps)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int parsed) && parsed >= minSteps)
            return parsed;
        return defaultSteps;
    }

    private readonly struct EdgeKey
    {
        public EdgeKey(StackItem child, CompoundType parent)
        {
            Child = child;
            Parent = parent;
        }

        public StackItem Child { get; }
        public CompoundType Parent { get; }
    }

    private sealed class EdgeKeyComparer : IEqualityComparer<EdgeKey>
    {
        public bool Equals(EdgeKey x, EdgeKey y)
            => ReferenceEquals(x.Child, y.Child) && ReferenceEquals(x.Parent, y.Parent);

        public int GetHashCode(EdgeKey obj)
            => HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Child), RuntimeHelpers.GetHashCode(obj.Parent));
    }

    #endregion

    #region VM integration equivalence

    [TestMethod]
    public void TestEquivalence_VMIntegration_Scripts()
    {
        foreach (var script in BuildIntegrationScripts())
            AssertScriptEquivalent(script);
    }

    private static IEnumerable<byte[]> BuildIntegrationScripts()
    {
        // Script 1: create array, drop, ret
        using (ScriptBuilder sb = new())
        {
            sb.EmitPush(0);
            sb.Emit(OpCode.NEWARRAY);
            sb.Emit(OpCode.DROP);
            sb.Emit(OpCode.RET);
            yield return sb.ToArray();
        }

        // Script 2: nested arrays (outer[0] = inner)
        using (ScriptBuilder sb = new())
        {
            sb.EmitPush(1);
            sb.Emit(OpCode.NEWARRAY);
            sb.Emit(OpCode.DUP);
            sb.EmitPush(0);
            sb.EmitPush(0);
            sb.Emit(OpCode.NEWARRAY);
            sb.Emit(OpCode.SETITEM);
            sb.Emit(OpCode.RET);
            yield return sb.ToArray();
        }

        // Script 3: map with two entries, one value is array
        using (ScriptBuilder sb = new())
        {
            sb.Emit(OpCode.NEWMAP);
            sb.Emit(OpCode.DUP);
            sb.EmitPush("k1");
            sb.EmitPush(1);
            sb.Emit(OpCode.SETITEM);
            sb.Emit(OpCode.DUP);
            sb.EmitPush("k2");
            sb.EmitPush(0);
            sb.Emit(OpCode.NEWARRAY);
            sb.Emit(OpCode.SETITEM);
            sb.Emit(OpCode.RET);
            yield return sb.ToArray();
        }
    }

    private static void AssertScriptEquivalent(byte[] script)
    {
        var legacy = ExecuteWithCounter(new ReferenceCounter(), script);
        var markSweep = ExecuteWithCounter(new MarkSweepReferenceCounter(), script);

        Assert.AreEqual(legacy.state, markSweep.state);
        Assert.AreEqual(legacy.count, markSweep.count);
    }

    private static (VMState state, int count) ExecuteWithCounter(IReferenceCounter counter, byte[] script)
    {
        using var engine = new RcEngine(counter);
        engine.LoadScript(script);
        var state = engine.Execute();
        return (state, engine.ReferenceCounter.Count);
    }

    private sealed class RcEngine : ExecutionEngine
    {
        public RcEngine(IReferenceCounter counter) : base(null, counter, ExecutionEngineLimits.Default) { }
    }

    #endregion
}
