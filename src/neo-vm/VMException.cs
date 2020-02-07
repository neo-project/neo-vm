using System;

namespace Neo.VM
{
    public class VMException : ApplicationException
    {
        public VMException() { }
        public VMException(string msg) : base(msg)
        {
        }
    }
}
