using Neo.VM.Types;
using System;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.VM
{
    public class UnhandledException : Exception
    {
        public StackItem ExceptionObject { get; }

        public UnhandledException(StackItem e) : base(GetExceptionMessage(e))
        {
            ExceptionObject = e;
        }

        private static string GetExceptionMessage(StackItem e)
        {
            StringBuilder sb = new StringBuilder("An unhandled exception was thrown.");
            if (e is Array array && array.Count > 0 && array[0] is ByteString s)
            {
                sb.Append(' ');
                sb.Append(Encoding.UTF8.GetString(s.GetSpan()));
            }
            return sb.ToString();
        }
    }
}
