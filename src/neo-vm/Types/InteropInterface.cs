using System;
using System.Diagnostics;

namespace Neo.VM.Types
{
    public abstract class InteropInterface : StackItem
    {
        public override byte[] GetByteArray()
        {
            throw new NotSupportedException();
        }

        public abstract T GetInterface<T>() where T : class;
    }

    [DebuggerDisplay("type=InteropInterface, value={_object}")]
    public class InteropInterface<T> : InteropInterface
        where T : class
    {
        private readonly T _object;

        public InteropInterface(T value)
        {
            _object = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (!(other is InteropInterface<T> i)) return false;
            return _object.Equals(i._object);
        }

        public override bool GetBoolean() => _object != null;

        public override I GetInterface<I>() => _object as I;

        public static implicit operator T(InteropInterface<T> @interface) => @interface._object;
    }
}
