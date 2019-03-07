using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Test.Extensions;
using Neo.VM;

namespace Neo.Test.Types
{
    public class ScriptTable : IScriptTable
    {
        class ByteArrayComparable : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                if (obj.Length < 4) return 0;

                return BitConverter.ToInt32(obj);
            }
        }

        private Dictionary<byte[], int> _scriptCounter = new Dictionary<byte[], int>(new ByteArrayComparable());
        private Dictionary<string, byte[]> _data = new Dictionary<string, byte[]>();

        public byte[] GetScript(byte[] script_hash)
        {
            if (!_data.TryGetValue(script_hash.ToHexString(), out var ret))
            {
                return null;
            }

            return ret;
        }

        public void Add(byte[] script)
        {
            _data.Add(Crypto.Default.Hash160(script).ToHexString(), script);
        }

        public void IncrementInvocationCounter(byte[] script_hash)
        {
            _scriptCounter[script_hash]++;
        }

        public int GetInvocationCounter(byte[] script_hash)
        {
            if (!_scriptCounter.TryGetValue(script_hash, out var value))
            {
                return 0;
            }

            return value;
        }
    }
}