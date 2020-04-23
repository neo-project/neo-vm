using System;

namespace Neo.VM
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OperandSizeAttribute : Attribute
    {
        public int Size { get; set; }
        public int SizePrefix { get; set; }
    }
}
