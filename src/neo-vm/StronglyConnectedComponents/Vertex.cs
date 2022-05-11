// Copyright (C) 2016-2022 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.VM.StronglyConnectedComponents
{
    abstract class Vertex<T> where T : Vertex<T>
    {
        internal int Index = -1;
        internal int LowLink = 0;
        internal protected abstract IEnumerable<T> Successors { get; }
        public void Reset() => (Index, LowLink) = (-1, 0);
    }
}
