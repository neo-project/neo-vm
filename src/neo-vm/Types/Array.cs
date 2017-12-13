using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Types
{
    internal class Array : StackItem
    {
        protected readonly List<StackItem> _array;

        public override bool IsArray => true;

        public Array(IEnumerable<StackItem> value)
        {
            this._array = value as List<StackItem> ?? value.ToList();
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

        public override IList<StackItem> GetArray()
        {
            return _array;
        }

        public override bool GetBoolean()
        {
            return _array.Count > 0;
        }

        public override byte[] GetByteArray()
        {
            throw new NotSupportedException();
        }

        public void Reverse()
        {
            _array.Reverse();
        }
    }
}
