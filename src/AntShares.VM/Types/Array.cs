using System.IO;
using System.Linq;
using System.Numerics;

namespace AntShares.VM.Types
{
    internal class Array : StackItem
    {
        private StackItem[] _array;

        public override int ArraySize => _array.Length;

        public override bool IsArray => true;

        public Array(StackItem[] value)
        {
            this._array = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            Array a = other as Array;
            if (a == null)
                return false;
            else
                return _array.SequenceEqual(a._array);
        }

        public override StackItem[] GetArray()
        {
            return _array;
        }

        public override BigInteger GetBigInteger()
        {
            return _array.Length > 0 ? _array[0].GetBigInteger() : BigInteger.Zero;
        }

        public override bool GetBoolean()
        {
            return _array.Length > 0 ? _array[0].GetBoolean() : false;
        }

        public override byte[] GetByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.WriteVarInt(_array.Length);
                foreach (StackItem item in _array)
                    w.Write(item.GetByteArray());
                w.Flush();
                return ms.ToArray();
            }
        }

        public override T GetInterface<T>()
        {
            return _array.Length > 0 ? _array[0].GetInterface<T>() : null;
        }
    }
}
