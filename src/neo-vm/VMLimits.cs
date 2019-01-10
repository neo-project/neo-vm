using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public class VMLimits
    {
        /// <summary>
        /// Default limits
        /// </summary>
        public static VMLimits Default { get; set; } = new VMLimits();

        /// <summary>
        /// Max value for SHL and SHR
        /// </summary>
        public int Max_SHL_SHR = ushort.MaxValue;
        /// <summary>
        /// Min value for SHL and SHR
        /// </summary>
        public int Min_SHL_SHR = -ushort.MaxValue;
        /// <summary>
        /// Set the max size allowed size for BigInteger
        /// </summary>
        public int MaxSizeForBigInteger = 32;
        /// <summary>
        /// Set the max Stack Size
        /// </summary>
        public uint MaxStackSize = 2 * 1024;
        /// <summary>
        /// Set Max Item Size
        /// </summary>
        public uint MaxItemSize = 1024 * 1024;
        /// <summary>
        /// Set Max Invocation Stack Size
        /// </summary>
        public uint MaxInvocationStackSize = 1024;
        /// <summary>
        /// Set Max Array Size
        /// </summary>
        public uint MaxArraySize = 1024;

        /// <summary>
        /// Check if the is possible to overflow the MaxArraySize
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckArraySize(int length) => length <= MaxArraySize;

        /// <summary>
        /// Check if the is possible to overflow the MaxItemSize
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckMaxItemSize(int length) => length <= MaxItemSize;

        /// <summary>
        /// Check if the is possible to overflow the MaxInvocationStack
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckMaxInvocationStack(ExecutionEngine stack) => stack.InvocationStack.Count < MaxInvocationStackSize;

        /// <summary>
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBigInteger(BigInteger value) => value.ToByteArray().Length <= MaxSizeForBigInteger;

        /// <summary>
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary>
        /// <param name="byteLength">Value</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckBigIntegerBitLength(int byteLength) => byteLength <= MaxSizeForBigInteger;

        /// <summary>
        /// Check if the number is allowed from SHL and SHR
        /// </summary>
        /// <param name="shift">Shift</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckShift(int shift) => shift <= Max_SHL_SHR && shift >= Min_SHL_SHR;
    }
}