using System;
using System.IO;
using System.Numerics;

namespace Neo.VM
{
    /// <summary>
    /// A helper class for building scripts.
    /// </summary>
    public class ScriptBuilder : IDisposable
    {
        private readonly MemoryStream ms = new();
        private readonly BinaryWriter writer;

        /// <summary>
        /// The length of the script.
        /// </summary>
        public int Length => (int)ms.Position;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptBuilder"/> class.
        /// </summary>
        public ScriptBuilder()
        {
            writer = new BinaryWriter(ms);
        }

        public void Dispose()
        {
            writer.Dispose();
            ms.Dispose();
        }

        /// <summary>
        /// Emits an <see cref="Instruction"/> with the specified <see cref="OpCode"/> and operand.
        /// </summary>
        /// <param name="opcode">The <see cref="OpCode"/> to be emitted.</param>
        /// <param name="operand">The operand to be emitted.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder Emit(OpCode opcode, byte[]? operand = null)
        {
            writer.Write((byte)opcode);
            if (operand != null)
                writer.Write(operand);
            return this;
        }

        /// <summary>
        /// Emits a call <see cref="Instruction"/> with the specified offset.
        /// </summary>
        /// <param name="offset">The offset to be called.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitCall(int offset)
        {
            if (offset < sbyte.MinValue || offset > sbyte.MaxValue)
                return Emit(OpCode.CALL_L, BitConverter.GetBytes(offset));
            else
                return Emit(OpCode.CALL, new[] { (byte)offset });
        }

        /// <summary>
        /// Emits a jump <see cref="Instruction"/> with the specified offset.
        /// </summary>
        /// <param name="opcode">The <see cref="OpCode"/> to be emitted. It must be a jump <see cref="OpCode"/></param>
        /// <param name="offset">The offset to jump.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitJump(OpCode opcode, int offset)
        {
            if (opcode < OpCode.JMP || opcode > OpCode.JMPLE_L)
                throw new ArgumentOutOfRangeException(nameof(opcode));
            if ((int)opcode % 2 == 0 && (offset < sbyte.MinValue || offset > sbyte.MaxValue))
                opcode += 1;
            if ((int)opcode % 2 == 0)
                return Emit(opcode, new[] { (byte)offset });
            else
                return Emit(opcode, BitConverter.GetBytes(offset));
        }

        /// <summary>
        /// Emits a push <see cref="Instruction"/> with the specified number.
        /// </summary>
        /// <param name="value">The number to be pushed.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitPush(BigInteger value)
        {
            if (value >= -1 && value <= 16) return Emit(OpCode.PUSH0 + (byte)(int)value);
            byte[] data = value.ToByteArray(isUnsigned: false, isBigEndian: false);
            if (data.Length == 1) return Emit(OpCode.PUSHINT8, data);
            if (data.Length == 2) return Emit(OpCode.PUSHINT16, data);
            if (data.Length <= 4) return Emit(OpCode.PUSHINT32, PadRight(data, 4, value.Sign < 0));
            if (data.Length <= 8) return Emit(OpCode.PUSHINT64, PadRight(data, 8, value.Sign < 0));
            if (data.Length <= 16) return Emit(OpCode.PUSHINT128, PadRight(data, 16, value.Sign < 0));
            if (data.Length <= 32) return Emit(OpCode.PUSHINT256, PadRight(data, 32, value.Sign < 0));
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>
        /// Emits a push <see cref="Instruction"/> with the specified boolean value.
        /// </summary>
        /// <param name="value">The value to be pushed.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitPush(bool value)
        {
            return Emit(value ? OpCode.PUSH1 : OpCode.PUSH0);
        }

        /// <summary>
        /// Emits a push <see cref="Instruction"/> with the specified data.
        /// </summary>
        /// <param name="data">The data to be pushed.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
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

        /// <summary>
        /// Emits a push <see cref="Instruction"/> with the specified <see cref="string"/>.
        /// </summary>
        /// <param name="data">The <see cref="string"/> to be pushed.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitPush(string data)
        {
            return EmitPush(Utility.StrictUTF8.GetBytes(data));
        }

        /// <summary>
        /// Emits raw script.
        /// </summary>
        /// <param name="script">The raw script to be emitted.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitRaw(byte[]? script = null)
        {
            if (script != null)
                writer.Write(script);
            return this;
        }

        /// <summary>
        /// Emits an <see cref="Instruction"/> with <see cref="OpCode.SYSCALL"/>.
        /// </summary>
        /// <param name="api">The operand of <see cref="OpCode.SYSCALL"/>.</param>
        /// <returns>A reference to this instance after the emit operation has completed.</returns>
        public ScriptBuilder EmitSysCall(uint api)
        {
            return Emit(OpCode.SYSCALL, BitConverter.GetBytes(api));
        }

        /// <summary>
        /// Converts the value of this instance to a byte array.
        /// </summary>
        /// <returns>A byte array contains the script.</returns>
        public byte[] ToArray()
        {
            writer.Flush();
            return ms.ToArray();
        }

        private static byte[] PadRight(byte[] data, int length, bool negative)
        {
            if (data.Length >= length) return data;
            byte[] buffer = new byte[length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            if (negative)
            {
                for (int x = data.Length; x < length; x++)
                {
                    buffer[x] = byte.MaxValue;
                }
            }
            return buffer;
        }
    }
}
