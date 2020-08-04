using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    unsafe internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotZero(ReadOnlySpan<byte> x)
        {
            int len = x.Length;
            if (len == 0) return false;
            fixed (byte* xp = x)
            {
                long* xlp = (long*)xp;
                for (; len >= 8; len -= 8)
                {
                    if (*xlp != 0) return true;
                    xlp++;
                }
                byte* xbp = (byte*)xlp;
                for (; len > 0; len--)
                {
                    if (*xbp != 0) return true;
                    xbp++;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<byte> value)
        {
            int hashCode = 17;

            unchecked
            {
                int len = value.Length;
                if (len > 0)
                {
                    fixed (byte* xp = value)
                    {
                        int* xlp = (int*)xp;
                        for (; len >= 4; len -= 4)
                        {
                            hashCode = hashCode * 31 + *xlp;
                            xlp++;
                        }
                        byte* xbp = (byte*)xlp;
                        for (; len > 0; len--)
                        {
                            hashCode = hashCode * 31 + *xbp;
                            xbp++;
                        }
                    }

                    if (hashCode == -1) hashCode = 0;
                }
            }

            return hashCode;
        }
    }
}
