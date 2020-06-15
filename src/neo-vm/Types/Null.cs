using System;

namespace Neo.VM.Types
{
    public class Null : StackItem
    {
        public override StackItemType Type => StackItemType.Any;

        internal Null() { }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidCastException();
            return this;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other is Null;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool ToBoolean()
        {
            return false;
        }
    }
}
