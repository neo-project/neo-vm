using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Neo.VM.Types
{
    public abstract class StackItem : IEquatable<StackItem>
    {
        public bool IsNull => this is Null;

        public static StackItem Null { get; } = new Null();

        public abstract bool Equals(StackItem other);

        public sealed override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj is StackItem other)
                return Equals(other);
            return false;
        }

        public static StackItem FromInterface<T>(T value)
            where T : class
        {
            if (value is null) return Null;
            return new InteropInterface<T>(value);
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        public abstract bool ToBoolean();

        public static implicit operator StackItem(int value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(uint value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(long value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(ulong value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(BigInteger value)
        {
            return new Integer(value);
        }

        public static implicit operator StackItem(bool value)
        {
            return new Boolean(value);
        }

        public static implicit operator StackItem(byte[] value)
        {
            return new ByteArray(value);
        }

        public static implicit operator StackItem(ReadOnlyMemory<byte> value)
        {
            return new ByteArray(value);
        }

        public static implicit operator StackItem(string value)
        {
            return new ByteArray(Encoding.UTF8.GetBytes(value));
        }

        public static implicit operator StackItem(StackItem[] value)
        {
            return new Array(value);
        }

        public static implicit operator StackItem(List<StackItem> value)
        {
            return new Array(value);
        }
    }
}
