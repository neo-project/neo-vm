using System;

namespace AntShares.Compiler
{
    public class CompilerException : Exception
    {
        public CompilerException(uint lineNumber, string message)
            : base($"ERROR: {message} in line {lineNumber}.")
        {
        }
    }
}
