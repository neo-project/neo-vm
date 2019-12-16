using System;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(InnerBuffer).Replace(\"-\", string.Empty)}")]
    public class Buffer : StackItem
    {
        internal readonly byte[] InnerBuffer;

        public int Size => InnerBuffer.Length;

        public Buffer(int size)
        {
            InnerBuffer = new byte[size];
        }

        public Buffer(ReadOnlySpan<byte> data)
            : this(data.Length)
        {
            if (!data.IsEmpty) data.CopyTo(InnerBuffer);
        }

        public override bool Equals(object obj)
        {
            return this == obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (byte element in InnerBuffer)
                    hash = hash * 31 + element;
                return hash;
            }
        }

        public override bool ToBoolean()
        {
            return true;
        }
    }
}
