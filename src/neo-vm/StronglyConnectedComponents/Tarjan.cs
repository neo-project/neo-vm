// Copyright (C) 2016-2022 The Neo Project.
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
using System.Linq;

namespace Neo.VM.StronglyConnectedComponents
{
    class Tarjan<T> where T : class, IVertex<T>
    {
        private readonly IEnumerable<T> vertexs;
        private readonly LinkedList<HashSet<T>> components = new();
        private readonly Stack<T> stack = new();
        private int index = 0;

        public Tarjan(IEnumerable<T> vertexs)
        {
            this.vertexs = vertexs;
        }

        public LinkedList<HashSet<T>> Invoke()
        {
            foreach (var v in vertexs)
            {
                if (v.Index < 0)
                {
                    StrongConnectNonRecursive(v);
                }
            }
            return components;
        }

        private void StrongConnect(T v)
        {
            v.Index = v.LowLink = ++index;
            stack.Push(v);

            foreach (T w in v.Successors)
            {
                if (w.Index < 0)
                {
                    StrongConnect(w);
                    v.LowLink = Math.Min(v.LowLink, w.LowLink);
                }
                else if (stack.Any(p => p == w))
                {
                    v.LowLink = Math.Min(v.LowLink, w.Index);
                }
            }

            if (v.LowLink == v.Index)
            {
                HashSet<T> scc = new(ReferenceEqualityComparer.Instance);
                T w;
                do
                {
                    w = stack.Pop();
                    scc.Add(w);
                } while (v != w);
                components.AddLast(scc);
            }
        }

        private void StrongConnectNonRecursive(T v)
        {
            Stack<(T, T?, IEnumerator<T>?, int)> sstack = new();
            sstack.Push((v, null, null, 0));
            while (sstack.TryPop(out var state))
            {
                (v, T? w, IEnumerator<T>? s, int n) = state;
                switch (n)
                {
                    case 0:
                        v.Index = v.LowLink = ++index;
                        stack.Push(v);
                        s = v.Successors.GetEnumerator();
                        goto case 2;
                    case 1:
                        v.LowLink = Math.Min(v.LowLink, w!.LowLink);
                        goto case 2;
                    case 2:
                        while (s!.MoveNext())
                        {
                            w = s.Current;
                            if (w.Index < 0)
                            {
                                sstack.Push((v, w, s, 1));
                                v = w;
                                goto case 0;
                            }
                            else if (stack.Any(p => p == w))
                            {
                                v.LowLink = Math.Min(v.LowLink, w.Index);
                            }
                        }
                        if (v.LowLink == v.Index)
                        {
                            HashSet<T> scc = new(ReferenceEqualityComparer.Instance);
                            do
                            {
                                w = stack.Pop();
                                scc.Add(w);
                            } while (v != w);
                            components.AddLast(scc);
                        }
                        break;
                }
            }
        }
    }
}
