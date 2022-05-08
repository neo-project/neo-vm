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

namespace Neo.VM.StronglyConnectedComponents
{
    class Tarjan<T>
    {
        private readonly IEnumerable<Vertex<T>> vertexs;
        private readonly Func<Vertex<T>, IEnumerable<Vertex<T>>> successors;
        private readonly LinkedList<LinkedList<Vertex<T>>> components = new();
        private readonly Stack<Vertex<T>> stack = new();
        private int index = 0;

        public Tarjan(IEnumerable<Vertex<T>> vertexs, Func<Vertex<T>, IEnumerable<Vertex<T>>> successors)
        {
            this.vertexs = vertexs;
            this.successors = successors;
        }

        public IReadOnlyCollection<IReadOnlyCollection<Vertex<T>>> Invoke()
        {
            foreach (var v in vertexs)
            {
                if (v.Index < 0)
                {
                    StrongConnect(v);
                }
            }
            return components;
        }

        private void StrongConnect(Vertex<T> v)
        {
            v.Index = index;
            v.LowLink = index;
            index++;
            stack.Push(v);

            foreach (Vertex<T> w in successors(v))
            {
                if (w.Index < 0)
                {
                    StrongConnect(w);
                    v.LowLink = Math.Min(v.LowLink, w.LowLink);
                }
                else if (stack.Contains(w))
                {
                    v.LowLink = Math.Min(v.LowLink, w.Index);
                }
            }

            if (v.LowLink == v.Index)
            {
                LinkedList<Vertex<T>> scc = new();
                Vertex<T> w;
                do
                {
                    w = stack.Pop();
                    scc.AddLast(w);
                } while (v != w);
                components.AddLast(scc);
            }
        }
    }
}
