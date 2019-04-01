using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    unsafe internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemoryCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            if (count == 0) return;
            fixed (byte* sp = &src[srcOffset], dp = &dst[dstOffset])
            {
                Buffer.MemoryCopy(sp, dp, dst.Length - dstOffset, count);
            }
        }

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
        public static bool SpanEquals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            if (x == y) return true;
            int len = x.Length;
            if (len != y.Length) return false;
            fixed (byte* xp = x, yp = y)
            {
                long* xlp = (long*)xp, ylp = (long*)yp;
                for (; len >= 8; len -= 8)
                {
                    if (*xlp != *ylp) return false;
                    xlp++;
                    ylp++;
                }
                byte* xbp = (byte*)xlp, ybp = (byte*)ylp;
                for (; len > 0; len--)
                {
                    if (*xbp != *ybp) return false;
                    xbp++;
                    ybp++;
                }
            }
            return true;
        }

        /// <summary>
        /// Convert byte array to int32
        /// </summary>
        /// <param name="value">Value (must be checked before this call)</param>
        /// <param name="startIndex">Start index</param>
        /// <returns>Integer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *(int*)pbyte;
            }
        }
    }
}
