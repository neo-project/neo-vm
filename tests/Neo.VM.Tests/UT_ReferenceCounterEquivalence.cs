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
