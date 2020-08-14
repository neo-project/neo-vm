using Neo.VM.Types;
using System;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.VM
{
    public class VMUnhandledException : Exception
    {
        public StackItem ExceptionObject { get; }

        public VMUnhandledException(StackItem e) : base(GetExceptionMessage(e))
        {
            ExceptionObject = e;
        }

        private static string GetExceptionMessage(StackItem e)
        {
            StringBuilder sb = new StringBuilder("An unhandled exception was thrown.");
            ByteString s = e as ByteString;
            if (s is null && e is Array array && array.Count > 0)
                s = array[0] as ByteString;
            if (s != null)
            {
                sb.Append(' ');
                sb.Append(Encoding.UTF8.GetString(s.GetSpan()));
            }
            return sb.ToString();
        }
    }
}
