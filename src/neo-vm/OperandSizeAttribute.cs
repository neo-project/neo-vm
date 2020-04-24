using System;

namespace Neo.VM
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OperandSizeAttribute : Attribute
    {
        public int Size { get; set; }
        public int SizePrefix { get; set; }

        public int GetOperandSize(byte[] script, int ip)
        {
            return SizePrefix switch
            {
                0 => Size,
                1 => script[ip],
                2 => BitConverter.ToUInt16(script, ip),
                4 => BitConverter.ToInt32(script, ip),
                _ => throw new FormatException(),
            };
        }
    }
}
