using System.IO;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class Script
    {
        private byte[] _scriptHash = null;

        private readonly byte[] _value;
        private readonly ICrypto _crypto;

        /// <summary>
        /// Cached script hash
        /// </summary>
        public byte[] ScriptHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_scriptHash == null) _scriptHash = _crypto.Hash160(_value);
                return _scriptHash;
            }
        }

        /// <summary>
        /// Script length
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _value.Length;
            }
        }

        /// <summary>
        /// Get opcode
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Returns the opcode</returns>
        public OpCode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (OpCode)_value[index];
            }
        }

        /// <summary>
        /// Get Binary reader
        /// </summary>
        /// <returns>Returns the binary reader of the script</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryReader GetBinaryReader()
        {
            return new BinaryReader(new MemoryStream(_value, false));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="crypto">Crypto</param>
        /// <param name="script">Script</param>
        public Script(ICrypto crypto, byte[] script)
        {
            _crypto = crypto;
            _value = script;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hash">Hash</param>
        /// <param name="script">Script</param>
        internal Script(byte[] hash, byte[] script)
        {
            _scriptHash = hash;
            _value = script;
        }

        public static implicit operator byte[](Script script)
        {
            return script._value;
        }
    }
}