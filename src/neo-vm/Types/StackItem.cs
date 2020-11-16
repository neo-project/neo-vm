#pragma warning disable CS0659

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    public abstract class StackItem : IEquatable<StackItem>
    {
        public static StackItem False { get; } = new Boolean(false);
        public bool IsNull => this is Null;
        public static StackItem Null { get; } = new Null();
        public static StackItem True { get; } = new Boolean(true);
        public abstract StackItemType Type { get; }

        public virtual StackItem ConvertTo(StackItemType type)
        {
            if (type == Type) return this;
            if (type == StackItemType.Boolean) return GetBoolean();
            throw new InvalidCastException();
        }

        public StackItem DeepCopy()
        {
            return DeepCopy(new Dictionary<StackItem, StackItem>(ReferenceEqualityComparer.Instance));
        }

        internal virtual StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap)
        {
            return this;
        }

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is StackItem item) return Equals(item);
            return false;
        }

        public virtual bool Equals(StackItem other)
        {
            return ReferenceEquals(this, other);
        }

        public static StackItem FromInterface(object value)
        {
            if (value is null) return Null;
            return new InteropInterface(value);
        }

        public abstract bool GetBoolean();

        public virtual BigInteger GetInteger()
        {
            throw new InvalidCastException();
        }

        public virtual T GetInterface<T>() where T : class
        {
            throw new InvalidCastException();
        }

        public virtual ReadOnlySpan<byte> GetSpan()
        {
            throw new InvalidCastException();
        }

        public virtual string GetString()
        {
            return Utility.StrictUTF8.GetString(GetSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(sbyte value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(byte value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(short value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(ushort value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(int value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(uint value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(long value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(ulong value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(BigInteger value)
        {
            return (Integer)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(bool value)
        {
            return value ? True : False;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(byte[] value)
        {
            return (ByteString)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(ReadOnlyMemory<byte> value)
        {
            return (ByteString)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StackItem(string value)
        {
            return (ByteString)value;
        }
    }
}
