using System;
using System.Linq;
using System.Numerics;

namespace AntShares.VM
{
    public class StackItem : IEquatable<StackItem>
    {
        private byte[] data;
        private IApiInterface _object;

        private StackItem() { }

        public StackItem(byte[] value)
        {
            this.data = value;
            this._object = null;
        }

        public StackItem(IApiInterface value)
        {
            this.data = null;
            this._object = value;
        }

        public T GetInterface<T>() where T : class, IApiInterface
        {
            return _object as T;
        }

        internal static bool IsEqual(object x, object y)
        {
            if (x.GetType() != y.GetType()) return false;
            if (x is byte[])
                return ((byte[])x).SequenceEqual((byte[])y);
            else
                return x.Equals(y);
        }

        public bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            if (_object != null && other._object != null)
                return _object.Equals(other._object);
            if (_object == null && data == null && other._object == null && other.data == null)
                return true;
            if ((_object == null && data == null) || (other._object == null && other.data == null))
                return false;
            byte[] x = data ?? _object.ToArray();
            byte[] y = other.data ?? other._object.ToArray();
            return x.SequenceEqual(y);
        }

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
            return new StackItem(value.ToByteArray());
        }

        public static implicit operator StackItem(bool value)
        {
            return new StackItem(value ? new[] { (byte)1 } : new byte[0]);
        }

        public static implicit operator StackItem(byte[] value)
        {
            return new StackItem(value);
        }

        public static explicit operator BigInteger(StackItem value)
        {
            return new BigInteger(value.data);
        }

        public static implicit operator bool(StackItem value)
        {
            if (value.data != null) return value.data.Any(p => p != 0);
            return value._object != null;
        }

        public static explicit operator byte[] (StackItem value)
        {
            if (value.data != null) return value.data;
            if (value._object != null) return value._object.ToArray();
            return null;
        }
    }
}
