using System;
using System.Diagnostics;
using System.Numerics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={System.BitConverter.ToString(InnerBuffer).Replace(\"-\", string.Empty)}")]
    public class Buffer : StackItem
    {
        public readonly byte[] InnerBuffer;

        public int Size => InnerBuffer.Length;
        public override StackItemType Type => StackItemType.Buffer;

        public Buffer(int size)
        {
            InnerBuffer = new byte[size];
        }

        public Buffer(ReadOnlySpan<byte> data)
            : this(data.Length)
        {
            if (!data.IsEmpty) data.CopyTo(InnerBuffer);
        }

        public override StackItem ConvertTo(StackItemType type)
        {
            switch (type)
            {
                case StackItemType.Integer:
                    if (InnerBuffer.Length > Integer.MaxSize)
                        throw new InvalidCastException();
                    return new BigInteger(InnerBuffer);
                case StackItemType.ByteArray:
                    byte[] clone = new byte[InnerBuffer.Length];
                    InnerBuffer.CopyTo(clone.AsSpan());
                    return clone;
                default:
                    return base.ConvertTo(type);
            }
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
