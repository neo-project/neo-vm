using System;

namespace Neo.VM
{
    /// <summary>
    /// Represents the exception thrown when the bad script is parsed.
    /// </summary>
    public class BadScriptException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadScriptException"/> class.
        /// </summary>
        public BadScriptException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadScriptException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BadScriptException(string message) : base(message) { }
    }
}
