// Copyright (C) 2016-2022 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.StronglyConnectedComponents
{
    class Vertex<T>
    {
        public readonly T Value;
        internal int Index = -1;
        internal int LowLink = 0;

        public Vertex(T value)
        {
            Value = value;
        }
    }
}
