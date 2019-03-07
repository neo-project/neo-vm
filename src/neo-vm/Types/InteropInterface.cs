using System;

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

    public class InteropInterface<T> : InteropInterface
        where T : class
    {
        private T _object;

        public InteropInterface(T value)
        {
            this._object = value;
        }

        public override bool Equals(StackItem other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            if (!(other is InteropInterface<T> i)) return false;
            return _object.Equals(i._object);
        }

        public override bool GetBoolean()
        {
            return _object != null;
        }

        public override I GetInterface<I>()
        {
            return _object as I;
        }

        public static implicit operator T(InteropInterface<T> @interface)
        {
            return @interface._object;
        }
    }
}
