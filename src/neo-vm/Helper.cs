#if !NETCOREAPP
using System.Numerics;

namespace Neo.VM
{
    internal static class Helper
    {
        public static int GetByteCount(this BigInteger source)
        {
            return source.ToByteArray().Length;
        }
    }
}
#endif
