using AntShares.VM.Types;
using System;
using System.Numerics;
using Array = AntShares.VM.Types.Array;
using Boolean = AntShares.VM.Types.Boolean;

namespace AntShares.VM
{
    public abstract class StackItem : IEquatable<StackItem>
    {
        public virtual int ArraySize => 1;

        public virtual bool IsArray => false;

        public abstract bool Equals(StackItem other);

        public static StackItem FromInterface(IInteropInterface value)
        {
            return new InteropInterface(value);
        }

        public virtual StackItem[] GetArray()
        {
            return new[] { this };
        }

        public virtual BigInteger GetBigInteger()
        {
            return new BigInteger(GetByteArray());
        }

        public abstract bool GetBoolean();

        public abstract byte[] GetByteArray();

        public abstract T GetInterface<T>() where T : class, IInteropInterface;

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

        public static implicit operator StackItem(StackItem[] value)
        {
            return new Array(value);
        }
    }
}
