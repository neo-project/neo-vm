using System;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents <see langword="null"/> in the VM.
    /// </summary>
    public class Null : StackItem
    {
        public override StackItemType Type => StackItemType.Any;

        internal Null() { }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidCastException($"Type can't be converted to StackItemType: {type}");
            return this;
        }

        public override bool Equals(StackItem? other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other is Null;
        }

        public override bool GetBoolean()
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override T? GetInterface<T>() where T : class
        {
            return null;
        }

        public override string? GetString()
        {
            return null;
        }
    }
}
