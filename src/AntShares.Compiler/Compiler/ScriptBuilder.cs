using AntShares.VM;
using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace AntShares.Compiler
{
    public class ScriptBuilder : IDisposable
    {
        private MemoryStream ms = new MemoryStream();

        public int Offset => (int)ms.Position;

        public void Dispose()
        {
            ms.Dispose();
        }

        public ScriptBuilder Emit(OpCode op, byte[] arg = null)
        {
            ms.WriteByte((byte)op);
            if (arg != null)
                ms.Write(arg, 0, arg.Length);
            return this;
        }

        public ScriptBuilder EmitAppCall(byte[] scriptHash)
        {
            if (scriptHash.Length != 20)
                throw new ArgumentException();
            return Emit(OpCode.OP_APPCALL, scriptHash);
        }

        public ScriptBuilder EmitJump(OpCode op, short offset)
        {
            if (op != OpCode.OP_JMP && op != OpCode.OP_JMPIF && op != OpCode.OP_JMPIFNOT && op != OpCode.OP_CALL)
                throw new ArgumentException();
            return Emit(op, BitConverter.GetBytes(offset));
        }

        public ScriptBuilder EmitPush(BigInteger number)
        {
            if (number == -1) return Emit(OpCode.OP_1NEGATE);
            if (number == 0) return Emit(OpCode.OP_0);
            if (number > 0 && number <= 16) return Emit(OpCode.OP_1 - 1 + (byte)number);
            return EmitPush(number.ToByteArray());
        }

        public ScriptBuilder EmitPush(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException();
            if (data.Length <= (int)OpCode.OP_PUSHBYTES75)
            {
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x100)
            {
                Emit(OpCode.OP_PUSHDATA1);
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x10000)
            {
                Emit(OpCode.OP_PUSHDATA2);
                ms.Write(BitConverter.GetBytes((ushort)data.Length), 0, 2);
                ms.Write(data, 0, data.Length);
            }
            else// if (data.Length < 0x100000000L)
            {
                Emit(OpCode.OP_PUSHDATA4);
                ms.Write(BitConverter.GetBytes((uint)data.Length), 0, 4);
                ms.Write(data, 0, data.Length);
            }
            return this;
        }

        public ScriptBuilder EmitSysCall(string api)
        {
            if (api == null)
                throw new ArgumentNullException();
            byte[] api_bytes = Encoding.ASCII.GetBytes(api);
            if (api_bytes.Length == 0 || api_bytes.Length > 252)
                throw new ArgumentException();
            byte[] arg = new byte[api_bytes.Length + 1];
            arg[0] = (byte)api_bytes.Length;
            Buffer.BlockCopy(api_bytes, 0, arg, 1, api_bytes.Length);
            return Emit(OpCode.OP_SYSCALL, arg);
        }

        public byte[] ToArray()
        {
            return ms.ToArray();
        }
    }
}
