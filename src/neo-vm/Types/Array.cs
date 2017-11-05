using System;
using System.Linq;

namespace Neo.VM.Types
{
    public class Array : StackItem
    {
        protected StackItem[] _array;

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

        public override bool GetBoolean()
        {
            return _array.Length > 0;
        }

        public override byte[] GetByteArray()
        {
            throw new NotSupportedException();
        }
    }
}
