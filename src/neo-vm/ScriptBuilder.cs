using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace Neo.VM
{
    public class ScriptBuilder : IDisposable
    {
        private readonly MemoryStream ms = new MemoryStream();
        private readonly BinaryWriter writer;

        public int Offset => (int)ms.Position;

        public ScriptBuilder()
        {
            writer = new BinaryWriter(ms);
        }

        public void Dispose()
        {
            writer.Dispose();
            ms.Dispose();
        }

        public ScriptBuilder Emit(OpCode op, byte[] arg = null)
        {
            writer.Write((byte)op);
            if (arg != null)
                writer.Write(arg);
            return this;
        }

        public ScriptBuilder EmitCall(int offset)
        {
            if (offset < sbyte.MinValue || offset > sbyte.MaxValue)
                return Emit(OpCode.CALL_L, BitConverter.GetBytes(offset));
            else
                return Emit(OpCode.CALL, new[] { (byte)offset });
        }

        public ScriptBuilder EmitJump(OpCode op, int offset)
        {
            if (op < OpCode.JMP || op > OpCode.JMPLE_L)
                throw new ArgumentOutOfRangeException(nameof(op));
            if ((int)op % 2 == 0 && (offset < sbyte.MinValue || offset > sbyte.MaxValue))
                op += 1;
            if ((int)op % 2 == 0)
                return Emit(op, new[] { (byte)offset });
            else
                return Emit(op, BitConverter.GetBytes(offset));
        }

        public ScriptBuilder EmitPush(BigInteger number)
        {
            if (number >= -1 && number <= 16) return Emit(OpCode.PUSH0 + (byte)(int)number);
            byte[] data = number.ToByteArray(isUnsigned: false, isBigEndian: false);
            if (data.Length == 1) return Emit(OpCode.PUSHINT8, data);
            if (data.Length == 2) return Emit(OpCode.PUSHINT16, data);
            if (data.Length <= 4) return Emit(OpCode.PUSHINT32, PadRight(data, 4));
            if (data.Length <= 8) return Emit(OpCode.PUSHINT64, PadRight(data, 8));
            if (data.Length <= 16) return Emit(OpCode.PUSHINT128, PadRight(data, 16));
            if (data.Length <= 32) return Emit(OpCode.PUSHINT256, PadRight(data, 32));
            throw new ArgumentOutOfRangeException(nameof(number));
        }

        public ScriptBuilder EmitPush(bool data)
        {
            return Emit(data ? OpCode.PUSH1 : OpCode.PUSH0);
        }

        public ScriptBuilder EmitPush(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length < 0x100)
            {
                Emit(OpCode.PUSHDATA1);
                writer.Write((byte)data.Length);
                writer.Write(data);
            }
            else if (data.Length < 0x10000)
            {
                Emit(OpCode.PUSHDATA2);
                writer.Write((ushort)data.Length);
                writer.Write(data);
            }
            else// if (data.Length < 0x100000000L)
            {
                Emit(OpCode.PUSHDATA4);
                writer.Write(data.Length);
                writer.Write(data);
            }
            return this;
        }

        public ScriptBuilder EmitPush(string data)
        {
            return EmitPush(Encoding.UTF8.GetBytes(data));
        }

        public ScriptBuilder EmitRaw(byte[] arg = null)
        {
            if (arg != null)
                writer.Write(arg);
            return this;
        }

        public ScriptBuilder EmitSysCall(uint api)
        {
            return Emit(OpCode.SYSCALL, BitConverter.GetBytes(api));
        }

        public byte[] ToArray()
        {
            writer.Flush();
            return ms.ToArray();
        }

        private static byte[] PadRight(byte[] data, int length)
        {
            if (data.Length >= length) return data;
            byte[] buffer = new byte[length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            return buffer;
        }
    }
}
