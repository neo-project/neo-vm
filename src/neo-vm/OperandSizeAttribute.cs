using System;

namespace Neo.VM
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class OperandSizeAttribute : Attribute
    {
        public int Size { get; set; }
        public int SizePrefix { get; set; }
    }
}
