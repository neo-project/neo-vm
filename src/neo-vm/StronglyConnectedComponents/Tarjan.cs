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
    class Tarjan<TVertex> where TVertex : Vertex<TVertex>
    {
        private readonly IEnumerable<TVertex> vertexs;
        private readonly LinkedList<LinkedList<TVertex>> components = new();
        private readonly Stack<TVertex> stack = new();
        private int index = 0;

        public Tarjan(IEnumerable<TVertex> vertexs)
        {
            this.vertexs = vertexs;
        }

        public IReadOnlyCollection<IReadOnlyCollection<TVertex>> Invoke()
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

        private void StrongConnect(TVertex v)
        {
            v.Index = index;
            v.LowLink = index;
            index++;
            stack.Push(v);

            foreach (TVertex w in v.Successors)
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
                LinkedList<TVertex> scc = new();
                TVertex w;
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
