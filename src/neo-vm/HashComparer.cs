using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM
{
    internal class HashComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            return BitConverter.ToInt32(obj, 0);
        }
    }
}
