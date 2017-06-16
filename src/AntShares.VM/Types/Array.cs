using System;
using System.Linq;
using System.Numerics;

namespace AntShares.VM.Types
{
    internal class Array : StackItem
    {
        protected StackItem[] _array;


        public override bool IsArray => true;

        public Array(StackItem[] value)
        {
            this._array = value;
        }

        //public virtual StackItem Clone()
        //{
        //    StackItem[] newArray = new StackItem[this._array.Length];
        //    for (var i = 0; i < _array.Length; i++)
        //    {
        //        if(_array[i].IsStruct)
        //        {
        //            newArray[i] = (_array[i] as Struct).Clone();
        //        }
        //        else
        //        {
        //            newArray[i] = _array[i]; //array = 是引用
        //                                     //其他的由于是固定值类型，不会改内部值，所以虽然需要复制，直接= 就行

        //        }
        //    }
        //    return new Array(newArray);
        //}

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
            throw new NotSupportedException();
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
