// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-vm is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using T = Neo.VM.Types.StackItem;

namespace Neo.VM.StronglyConnectedComponents
{
    class Tarjan
    {
        private readonly IList<T> vertexs;
        private readonly LinkedList<HashSet<T>> components = new();
        private readonly Stack<T> stack = new();
        private readonly HashSet<T> onStack = new();
        private int index = 0;

        public Tarjan(IEnumerable<T> vertexs)
        {
            this.vertexs = new List<T>(vertexs);
        }

        public LinkedList<HashSet<T>> Invoke()
        {
            for (int i = 0; i < vertexs.Count; i++)
            {
                var v = vertexs[i];
                if (v.DFN < 0)
                {
                    StrongConnectNonRecursive(v);
                }
            }
            return components;
        }

        private void StrongConnectNonRecursive(T v)
        {
            var executionStack = new Stack<(T node, T? lastNode, IEnumerator<T> successors, int stage)>();
            executionStack.Push((v, null, v.Successors.GetEnumerator(), 0));

            while (executionStack.Count > 0)
            {
                var state = executionStack.Pop();
                v = state.node;
                var dfn = v.DFN;
                var lowLink = v.LowLink;
                var stage = state.stage;

                if (stage == 0)
                {
                    dfn = lowLink = ++index;
                    stack.Push(v);
                    onStack.Add(v);
                }

                var successors = state.successors;
                while (successors.MoveNext())
                {
                    var successor = successors.Current;
                    if (successor.DFN < 0)
                    {
                        executionStack.Push((v, successor, successors, 1));
                        executionStack.Push((successor, null, successor.Successors.GetEnumerator(), 0));
                        break;
                    }
                    else if (onStack.Contains(successor))
                    {
                        lowLink = Math.Min(lowLink, successor.DFN);
                    }
                }

                if (stage == 1)
                {
                    lowLink = Math.Min(lowLink, state.lastNode!.LowLink);
                }

                if (lowLink == dfn)
                {
                    var scc = new HashSet<T>(ReferenceEqualityComparer.Instance);
                    T node;
                    do
                    {
                        node = stack.Pop();
                        onStack.Remove(node);
                        scc.Add(node);
                    } while (!ReferenceEquals(v, node));
                    components.AddLast(scc);
                }

                v.DFN = dfn;
                v.LowLink = lowLink;
            }
        }
    }
}
