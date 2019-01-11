using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class Script
    {
        private readonly ICrypto _crypto;
        private byte[] _scriptHash = null;

        /// <summary>
        /// Script
        /// </summary>
        public readonly byte[] Value;

        /// <summary>
        /// Cached script hash
        /// </summary>
        public byte[] ScriptHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_scriptHash == null) _scriptHash = _crypto.Hash160(Value);
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
                return Value.Length;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="crypto">Crypto</param>
        /// <param name="script">Script</param>
        public Script(ICrypto crypto, byte[] script)
        {
            _crypto = crypto;
            Value = script;
        }

        internal Script(byte[] hash, byte[] script)
        {
            _scriptHash = hash;
            Value = script;
        }
    }
}