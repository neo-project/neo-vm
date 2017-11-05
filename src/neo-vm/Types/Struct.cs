using System.Linq;

namespace Neo.VM.Types
{
    public class Struct : Array
    {
        public override bool IsStruct => true;

        public Struct(StackItem[] value) : base(value)
        {
        }

        public StackItem Clone()
        {
            StackItem[] newArray = new StackItem[this._array.Length];
            for (var i = 0; i < _array.Length; i++)
            {
                if (_array[i].IsStruct)
                {
                    newArray[i] = (_array[i] as Struct).Clone();
                }
                else
                {
                    newArray[i] = _array[i]; //array = 是引用
                                             //其他的由于是固定值类型，不会改内部值，所以虽然需要复制，直接= 就行
                }
            }
            return new Struct(newArray);
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            Struct a = other as Struct;
            if (a == null)
                return false;
            else
                return _array.SequenceEqual(a._array);
        }
    }
}
