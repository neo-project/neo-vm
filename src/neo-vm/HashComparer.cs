using System.Collections.Generic;

namespace Neo.VM
{
    internal class HashComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return Unsafe.MemoryEquals(x, y);
        }

        public int GetHashCode(byte[] obj)
        {
            return Unsafe.ToInt32(obj, 0);
        }
    }
}
