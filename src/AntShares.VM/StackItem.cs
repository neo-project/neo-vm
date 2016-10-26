using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AntShares.VM
{
    public class StackItem
    {
        private object[] _array;

        public int Count => _array.Length;

        private StackItem() { }

        public StackItem(byte[] value)
        {
            this._array = new[] { value };
        }

        public StackItem(byte[][] value)
        {
            this._array = value;
        }

        public StackItem(IApiInterface value)
        {
            this._array = new[] { value };
        }

        public StackItem(IApiInterface[] value)
        {
            this._array = value;
        }

        internal StackItem(StackItem[] value)
        {
            this._array = value.Select(p => p._array[0]).ToArray();
        }

        internal StackItem Concat(StackItem item)
        {
            return new StackItem { _array = _array.Concat(item._array).ToArray() };
        }

        internal StackItem Distinct()
        {
            if (_array.Length <= 1) return this;
            List<object> list = new List<object>();
            for (int i = 0; i < _array.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < list.Count; j++)
                {
                    if (IsEqual(_array[i], list[j]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) list.Add(_array[i]);
            }
            return new StackItem { _array = list.ToArray() };
        }

        internal StackItem ElementAt(int index)
        {
            if (index == 0 && _array.Length == 1) return this;
            return new StackItem { _array = new[] { _array[index] } };
        }

        internal StackItem Except(StackItem item)
        {
            if (_array.Length == 0) return this;
            if (item._array.Length == 0) return this;
            List<object> list = new List<object>();
            for (int i = 0; i < _array.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < item._array.Length; j++)
                {
                    if (IsEqual(_array[i], item._array[j]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) list.Add(_array[i]);
            }
            return new StackItem { _array = list.ToArray() };
        }

        public T[] GetArray<T>() where T : IApiInterface
        {
            return _array.Cast<T>().ToArray();
        }

        internal StackItem[] GetArray()
        {
            return _array.Select(p => new StackItem { _array = new[] { p } }).ToArray();
        }

        public bool[] GetBooleanArray()
        {
            return _array.Select(p => ToBoolean(p)).ToArray();
        }

        public byte[][] GetBytesArray()
        {
            return _array.Cast<byte[]>().ToArray();
        }

        public BigInteger[] GetIntArray()
        {
            return _array.Cast<byte[]>().Select(p => new BigInteger(p)).ToArray();
        }

        public T GetInterface<T>() where T : IApiInterface
        {
            return (T)_array.FirstOrDefault();
        }

        internal StackItem Intersect(StackItem item)
        {
            if (_array.Length == 0) return this;
            if (item._array.Length == 0) return item;
            List<object> list = new List<object>();
            for (int i = 0; i < _array.Length; i++)
            {
                for (int j = 0; j < item._array.Length; j++)
                {
                    if (IsEqual(_array[i], item._array[j]))
                    {
                        list.Add(_array[i]);
                        break;
                    }
                }
            }
            return new StackItem { _array = list.ToArray() };
        }

        private static bool IsEqual(object x, object y)
        {
            if (x.GetType() != y.GetType()) return false;
            if (x is byte[])
                return ((byte[])x).SequenceEqual((byte[])y);
            else
                return x.Equals(y);
        }

        internal StackItem Reverse()
        {
            return new StackItem { _array = _array.Reverse().ToArray() };
        }

        internal StackItem Skip(int count)
        {
            return new StackItem { _array = _array.Skip(count).ToArray() };
        }

        internal StackItem Take(int count)
        {
            if (_array.Length <= count) return this;
            return new StackItem { _array = _array.Take(count).ToArray() };
        }

        private static bool ToBoolean(object value)
        {
            if (value is byte[])
                return ((byte[])value).Any(p => p != 0);
            else
                return value != null;
        }

        public static implicit operator StackItem(int value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(int[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(uint value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(uint[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(long value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(long[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(ulong value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(ulong[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(BigInteger value)
        {
            return new StackItem(value.ToByteArray());
        }

        public static implicit operator StackItem(BigInteger[] value)
        {
            return value.Select(p => p.ToByteArray()).ToArray();
        }

        public static implicit operator StackItem(bool value)
        {
            return new StackItem(value ? new[] { (byte)1 } : new byte[0]);
        }

        public static implicit operator StackItem(bool[] value)
        {
            return value.Select(p => p ? new[] { (byte)1 } : new byte[0]).ToArray();
        }

        public static implicit operator StackItem(byte[] value)
        {
            return new StackItem(value);
        }

        public static implicit operator StackItem(byte[][] value)
        {
            return new StackItem(value);
        }

        public static explicit operator BigInteger(StackItem value)
        {
            return new BigInteger((byte[])value._array[0]);
        }

        public static implicit operator bool(StackItem value)
        {
            return ToBoolean(value._array[0]);
        }

        public static explicit operator byte[] (StackItem value)
        {
            return (byte[])value._array[0];
        }
    }
}
