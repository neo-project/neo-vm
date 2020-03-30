using System;
using System.Diagnostics;

namespace Neo.VM.Types
{
    [DebuggerDisplay("Type={GetType().Name}, Value={_object}")]
    public class InteropInterface : StackItem
    {
        private readonly object _object;

        public override StackItemType Type => StackItemType.InteropInterface;

        public InteropInterface(object value)
        {
            _object = value ?? throw new ArgumentException();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is InteropInterface i) return _object.Equals(i._object);
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        public T GetInterface<T>()
        {
            if (_object is T t) return t;
            throw new InvalidCastException();
        }

        public override bool ToBoolean()
        {
            return true;
        }

        public bool TryGetInterface<T>(out T result)
        {
            if (_object is T t)
            {
                result = t;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
