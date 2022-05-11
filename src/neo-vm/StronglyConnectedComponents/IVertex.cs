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
    interface IVertex<T> where T : IVertex<T>
    {
        int Index { get; set; }
        int LowLink { get; set; }
        IEnumerable<T> Successors { get; }
        public void Reset() => (Index, LowLink) = (-1, 0);
    }
}
