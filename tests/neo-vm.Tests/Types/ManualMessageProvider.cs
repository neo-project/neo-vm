using Neo.VM;

namespace Neo.Test.Types
{
    public class ManualMessageProvider : IScriptContainer
    {
        private readonly byte[] _messageData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageData">Message</param>
        public ManualMessageProvider(byte[] messageData)
        {
            _messageData = messageData;
        }

        public byte[] GetMessage() => _messageData;
    }
}