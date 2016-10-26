using System;

namespace AntShares.VM
{
    [Flags]
    internal enum VMState : byte
    {
        NONE = 0,
        HALT = 1 << 0,
        FAULT = 1 << 1
    }
}
