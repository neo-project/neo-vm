using System;

namespace Neo.VM
{
    /// <summary>
    /// Indicates the operand length of an <see cref="OpCode"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OperandSizeAttribute : Attribute
    {
        /// <summary>
        /// When it is greater than 0, indicates the size of the operand.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// When it is greater than 0, indicates the size prefix of the operand.
        /// </summary>
        public int SizePrefix { get; set; }
    }
}
