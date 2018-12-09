using System;
using System.IO;

namespace Neo.VM
{
    internal static class Helper
    {
        public static byte[] SafeReadBytes(this BinaryReader reader, int max = 0x1000000)
        {
            if((max > 0x1000000) || (!reader.BaseStream.CanSeek) || (reader.BaseStream.Length - reader.BaseStream.Position) < max))
                throw new FormatException;
            return reader.reader.ReadBytes(max);
        }
    }
}
