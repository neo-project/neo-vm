// Copyright (C) 2015-2026 The Neo Project.
//
// OpCode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM;

/// <summary>
/// Represents the opcode of an <see cref="Instruction"/>.
/// </summary>
public enum OpCode : byte
{
    #region Constants

    /// <summary>
    /// Pushes a 1-byte signed integer onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    PUSHINT8 = 0x00,

    /// <summary>
    /// Pushes a 2-bytes signed integer onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 2)]
    PUSHINT16 = 0x01,

    /// <summary>
    /// Pushes a 4-bytes signed integer onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    PUSHINT32 = 0x02,

    /// <summary>
    /// Pushes an 8-bytes signed integer onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 8)]
    PUSHINT64 = 0x03,

    /// <summary>
    /// Pushes a 16-bytes signed integer onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 16)]
    PUSHINT128 = 0x04,

    /// <summary>
    /// Pushes a 32-bytes signed integer onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 32)]
    PUSHINT256 = 0x05,

    /// <summary>
    /// Pushes the boolean value <see langword="true"/> onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSHT = 0x08,

    /// <summary>
    /// Pushes the boolean value <see langword="false"/> onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSHF = 0x09,

    /// <summary>
    /// Converts the 4-bytes offset to an <see cref="Pointer"/>, and pushes it onto the stack.
    /// <para>
    /// The execution will be faulted if the current position + offset is out of script range[0, script.Length).
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    PUSHA = 0x0A,

    /// <summary>
    /// The item <see langword="null"/> is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSHNULL = 0x0B,

    /// <summary>
    /// The next byte contains the number of bytes to be pushed onto the stack.
    /// The data format: <c>|1-byte unsigned size|data|</c>.
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(SizePrefix = 1)]
    PUSHDATA1 = 0x0C,

    /// <summary>
    /// The next two bytes contain the number of bytes to be pushed onto the stack.
    /// The data format: <c>|2-byte little-endian unsigned size|data|</c>.
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(SizePrefix = 2)]
    PUSHDATA2 = 0x0D,

    /// <summary>
    /// The next four bytes contain the number of bytes to be pushed onto the stack.
    /// The data format: <c>|4-byte little-endian unsigned size|data|</c>.
    /// <para>
    /// The execution will be faulted if the datasize is out of MaxItemSize.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(SizePrefix = 4)]
    PUSHDATA4 = 0x0E,

    /// <summary>
    /// The number -1 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSHM1 = 0x0F,

    /// <summary>
    /// The number 0 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH0 = 0x10,

    /// <summary>
    /// The number 1 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH1 = 0x11,

    /// <summary>
    /// The number 2 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH2 = 0x12,

    /// <summary>
    /// The number 3 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH3 = 0x13,

    /// <summary>
    /// The number 4 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH4 = 0x14,

    /// <summary>
    /// The number 5 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH5 = 0x15,

    /// <summary>
    /// The number 6 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH6 = 0x16,

    /// <summary>
    /// The number 7 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH7 = 0x17,

    /// <summary>
    /// The number 8 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH8 = 0x18,

    /// <summary>
    /// The number 9 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH9 = 0x19,

    /// <summary>
    /// The number 10 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH10 = 0x1A,

    /// <summary>
    /// The number 11 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH11 = 0x1B,

    /// <summary>
    /// The number 12 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH12 = 0x1C,

    /// <summary>
    /// The number 13 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH13 = 0x1D,

    /// <summary>
    /// The number 14 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH14 = 0x1E,

    /// <summary>
    /// The number 15 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH15 = 0x1F,

    /// <summary>
    /// The number 16 is pushed onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PUSH16 = 0x20,

    #endregion

    #region Flow control

    /// <summary>
    /// The <see cref="NOP"/> operation does nothing. It is intended to fill in space if opcodes are patched.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    NOP = 0x21,

    /// <summary>
    /// Unconditionally transfers control to a target instruction.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if the target offset is out of script range[0, script.Length).
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMP = 0x22,

    /// <summary>
    /// Unconditionally transfers control to a target instruction.
    /// The target instruction is represented as a 4-bytes little-endian signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if the target offset is out of script range[0, script.Length).
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMP_L = 0x23,

    /// <summary>
    /// Transfers control to a target instruction if the value is true value (true, non-null, non-zero).
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if the target offset is out of script range[0, script.Length).
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPIF = 0x24,

    /// <summary>
    /// Transfers control to a target instruction if the value is true value (true, non-null, non-zero).
    /// The target instruction is represented as a 4-bytes little-endian signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if the target offset is out of script range[0, script.Length).
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPIF_L = 0x25,

    /// <summary>
    /// Transfers control to a target instruction if the value is false value (false, null, zero).
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// <para>
    /// If the target offset is out of script range[0, script.Length), the execution will be faulted.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPIFNOT = 0x26,

    /// <summary>
    /// Transfers control to a target instruction if the value is false value (false, null, zero).
    /// The target instruction is represented as a 4-bytes little-endian signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if the target offset is out of script range[0, script.Length).
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPIFNOT_L = 0x27,

    /// <summary>
    /// Transfers control to a target instruction if the top two items are equal.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot convert to integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPEQ = 0x28,

    /// <summary>
    /// Transfers control to a target instruction if the top two items are equal.
    /// The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot convert to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPEQ_L = 0x29,

    /// <summary>
    /// Transfers control to a target instruction when the top two items are not equal.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot convert to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPNE = 0x2A,

    /// <summary>
    /// Transfers control to a target instruction when the top two items are not equal.
    /// The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot convert to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPNE_L = 0x2B,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is greater than the second pushed item.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPGT = 0x2C,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is greater than the second pushed item.
    /// The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPGT_L = 0x2D,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is greater than or equal to the second pushed item.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPGE = 0x2E,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is greater than or equal to the second pushed item.
    /// The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPGE_L = 0x2F,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is less than the second pushed item.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPLT = 0x30,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is less than the second pushed item.
    /// The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPLT_L = 0x31,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is less than or equal to the second pushed item.
    /// The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    JMPLE = 0x32,

    /// <summary>
    /// Transfers control to a target instruction if first pushed item(the second in the stack) is less than or equal to the second pushed item.
    /// The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
    /// 
    /// If any item is not an integer, it will be converted to an integer then compared.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the target offset is out of script range[0, script.Length).
    ///  2. One or both of items cannot represent as an integer.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 4)]
    JMPLE_L = 0x33,

    /// <summary>
    /// Calls the function at the target address which is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// 
    /// <para>
    /// The execution will be faulted if the target address is out of script range[0, script.Length).
    /// </para>
    /// </summary>
    [OperandSize(Size = 1)]
    CALL = 0x34,

    /// <summary>
    /// Calls the function at the target address which is represented as a 4-bytes little-endian signed offset from the beginning of the current instruction.
    /// 
    /// <para>
    /// The execution will be faulted if the target address is out of script range[0, script.Length).
    /// </para>
    /// </summary>
    [OperandSize(Size = 4)]
    CALL_L = 0x35,

    /// <summary>
    /// Pop the pointer of a function from the stack, and call the function.
    /// 
    /// <para>
    /// The execution will be faulted if the pointer is not from the current script or not a valid pointer.
    /// </para>
    /// </summary>
    CALLA = 0x36,

    /// <summary>
    /// Calls the function which is described by the token.
    /// </summary>
    [OperandSize(Size = 2)]
    CALLT = 0x37,

    /// <summary>
    /// It turns the vm state to FAULT immediately, and cannot be caught.
    /// 
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    ABORT = 0x38,

    /// <summary>
    /// Pop the top value of the stack. If it's false value (false, null, zero), exit vm execution and set vm state to FAULT.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    ASSERT = 0x39,

    /// <summary>
    /// Pop the top value of the stack, and throw it.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    THROW = 0x3A,

    /// <summary>
    /// TRY CatchOffset(sbyte) FinallyOffset(sbyte). If there's no catch body, set CatchOffset 0. If there's no finally body, set FinallyOffset 0.
    /// <para>
    /// The execution will be faulted if:
    ///  1. `catch` and `finally` are not provided both.
    ///  2. the `try` can not be nested more than `MaxTryNestingDepth`.
    ///  3. the `catch` or `finally` offset is out of script range[0, script.Length).
    /// </para>
    /// </summary>
    [OperandSize(Size = 2)]
    TRY = 0x3B,

    /// <summary>
    /// TRY_L CatchOffset(int) FinallyOffset(int). If there's no catch body, set CatchOffset 0. If there's no finally body, set FinallyOffset 0.
    /// <para>
    /// The execution will be faulted if:
    ///  1. `catch` and `finally` are not provided both.
    ///  2. the `try` can not be nested more than `MaxTryNestingDepth`.
    ///  3. the `catch` or `finally` offset is out of script range[0, script.Length).
    /// </para>
    /// </summary>
    [OperandSize(Size = 8)]
    TRY_L = 0x3C,

    /// <summary>
    /// Ensures that the appropriate surrounding finally blocks are executed.
    /// And then unconditionally transfers control to the specific target instruction,
    /// which is represented as a 1-byte signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the corresponding `try` is not provided.
    ///  2. the end offset is out of script range[0, script.Length).
    /// </para>
    /// </summary>
    [OperandSize(Size = 1)]
    ENDTRY = 0x3D,

    /// <summary>
    /// Ensures that the appropriate surrounding finally blocks are executed.
    /// And then unconditionally transfers control to the specific target instruction,
    /// which is represented as a 4-byte little-endian signed offset from the beginning of the current instruction.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the corresponding `try` is not provided.
    ///  2. the end offset is out of script range[0, script.Length).
    /// </para>
    /// </summary>
    [OperandSize(Size = 4)]
    ENDTRY_L = 0x3E,

    /// <summary>
    /// End finally, If no exception happen or be catched, vm will jump to the target instruction of ENDTRY/ENDTRY_L.
    /// Otherwise, vm will rethrow the exception to upper layer.
    /// <para>
    /// The execution will be faulted if the corresponding `try` is not provided.
    /// </para>
    /// </summary>
    ENDFINALLY = 0x3F,

    /// <summary>
    /// Returns from the current method.
    /// </summary>
    RET = 0x40,

    /// <summary>
    /// Calls to an interop service.
    /// </summary>
    [OperandSize(Size = 4)]
    SYSCALL = 0x41,

    #endregion

    #region Stack

    /// <summary>
    /// Pushes the number of stack items onto the stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    DEPTH = 0x43,

    /// <summary>
    /// Removes the top stack item.
    ///
    /// <example> a b c -> a b </example>
    /// <para>
    /// The execution will be faulted if the stack is empty.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    DROP = 0x45,

    /// <summary>
    /// Removes the second-to-top stack item.
    ///
    /// <example> a b c -> a c </example>
    /// <para>
    /// The execution will be faulted if the stack has less than 2 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NIP = 0x46,

    /// <summary>
    /// The item n back in the main stack is removed. The top item indicates the number of items to be removed.
    /// If the top item is not an integer, it will be converted to an integer.
    /// 
    /// <para>
    /// The execution will be faulted if:
    ///  1. The top item cannot convert to an integer.
    ///  2. The stack has less than n+1 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: n+1 item(s)
    /// </remarks>
    /// </summary>
    XDROP = 0x48,

    /// <summary>
    /// Clear the stack
    /// </summary>
    CLEAR = 0x49,

    /// <summary>
    /// Duplicates the top stack item.
    ///
    /// <example> a b c -> a b c c </example>
    /// <para>
    /// The execution will be faulted if the stack is empty.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    DUP = 0x4A,

    /// <summary>
    /// Copies the second-to-top stack item to the top.
    ///
    /// <example> a b c -> a b c b </example>
    /// <para>
    /// The execution will be faulted if the stack has less than 2 items.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    OVER = 0x4B,

    /// <summary>
    /// The item n back in the stack is copied to the top. The top item indicates the index of the item to be copied.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <example> a b c d 2 -> a b c d b
    ///  index => 3[2]1 0
    /// </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The top item cannot convert to an integer.
    ///  2. The stack has less than n+1 items.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    PICK = 0x4D,

    /// <summary>
    /// The item at the top of the stack is copied and inserted before the second-to-top item.
    ///
    /// <example> a b c -> a c b c </example>
    /// <para>
    /// The execution will be faulted if the stack has less than 2 items.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    TUCK = 0x4E,

    /// <summary>
    /// The top two items on the stack are swapped.
    ///
    /// <example> a b -> b a</example>
    /// <para>
    /// The execution will be faulted if the stack has less than 2 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    SWAP = 0x50,

    /// <summary>
    /// The top three items on the stack are rotated to the left.
    ///
    /// <example> a b c -> b c a</example>
    /// <para>
    /// The execution will be faulted if the stack has less than 3 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    ROT = 0x51,

    /// <summary>
    /// The item n back in the stack is moved to the top. The top item indicates the index of the item to be moved.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <example>a b c d 2 -> a c d b
    /// index => 3[2]1 0
    /// </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The top item cannot convert to an integer.
    ///  2. The stack has less than n+1 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    ROLL = 0x52,

    /// <summary>
    /// Reverse the order of the top 3 items on the stack.
    ///
    /// <example> a b c -> c b a</example>
    /// <para>
    /// The execution will be faulted if the stack has less than 3 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    REVERSE3 = 0x53,

    /// <summary>
    /// Reverse the order of the top 4 items on the stack.
    ///
    /// <example> a b c d -> d c b a</example>
    /// <para>
    /// The execution will be faulted if the stack has less than 4 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    REVERSE4 = 0x54,

    /// <summary>
    /// Pop the number N on the stack, and reverse the order of the top N items on the stack.
    /// If the top item is not an integer, it will be converted to an integer.
    /// 
    /// <example> a b c d 3 -> a d c b </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The top item cannot convert to an integer.
    ///  2. The stack has less than n+1 items.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    REVERSEN = 0x55,

    #endregion

    #region Slot

    /// <summary>
    /// Initialize the static field list for the current execution context.
    /// 
    /// <para>
    /// The execution will be faulted if:
    ///  1. The static field list for the current execution context has been initialized.
    ///  2. The operand is 0.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    INITSSLOT = 0x56,

    /// <summary>
    /// Initialize the argument slot and/or the local variable list for the current execution context.
    /// It has two uint8 operands: The first is the number of local variables, and the second is the number of arguments.
    /// Two operands cannot both be 0.
    /// 
    /// <para>
    /// The execution will be faulted if:
    ///  1. The argument slot and/or the local variable list for the current execution context has been initialized.
    ///  2. Two operands are both 0.
    /// </para>
    /// </summary>
    [OperandSize(Size = 2)]
    INITSLOT = 0x57,

    /// <summary>
    /// Loads the static field at index 0 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD0 = 0x58,

    /// <summary>
    /// Loads the static field at index 1 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD1 = 0x59,

    /// <summary>
    /// Loads the static field at index 2 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD2 = 0x5A,

    /// <summary>
    /// Loads the static field at index 3 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD3 = 0x5B,

    /// <summary>
    /// Loads the static field at index 4 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD4 = 0x5C,

    /// <summary>
    /// Loads the static field at index 5 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD5 = 0x5D,

    /// <summary>
    /// Loads the static field at index 6 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDSFLD6 = 0x5E,

    /// <summary>
    /// Loads the static field at a specified index onto the evaluation stack.
    /// The index is represented as a 1-byte unsigned integer.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    LDSFLD = 0x5F,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 0.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD0 = 0x60,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 1.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD1 = 0x61,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 2.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD2 = 0x62,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 3.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD3 = 0x63,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 4.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD4 = 0x64,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 5.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD5 = 0x65,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at index 6.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STSFLD6 = 0x66,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the static field list at a specified index.
    /// The index is represented as a 1-byte unsigned integer.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    STSFLD = 0x67,

    /// <summary>
    /// Loads the local variable at index 0 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC0 = 0x68,

    /// <summary>
    /// Loads the local variable at index 1 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC1 = 0x69,

    /// <summary>
    /// Loads the local variable at index 2 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC2 = 0x6A,

    /// <summary>
    /// Loads the local variable at index 3 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC3 = 0x6B,

    /// <summary>
    /// Loads the local variable at index 4 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC4 = 0x6C,

    /// <summary>
    /// Loads the local variable at index 5 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC5 = 0x6D,

    /// <summary>
    /// Loads the local variable at index 6 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDLOC6 = 0x6E,

    /// <summary>
    /// Loads the local variable at a specified index onto the evaluation stack.
    /// The index is represented as a 1-byte unsigned integer.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    LDLOC = 0x6F,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 0.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC0 = 0x70,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 1.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC1 = 0x71,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 2.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC2 = 0x72,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 3.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC3 = 0x73,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 4.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC4 = 0x74,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 5.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC5 = 0x75,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at index 6.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STLOC6 = 0x76,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the local variable list at a specified index.
    /// The index is represented as a 1-byte unsigned integer.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    STLOC = 0x77,

    /// <summary>
    /// Loads the argument at index 0 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG0 = 0x78,

    /// <summary>
    /// Loads the argument at index 1 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG1 = 0x79,

    /// <summary>
    /// Loads the argument at index 2 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG2 = 0x7A,

    /// <summary>
    /// Loads the argument at index 3 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG3 = 0x7B,

    /// <summary>
    /// Loads the argument at index 4 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG4 = 0x7C,

    /// <summary>
    /// Loads the argument at index 5 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG5 = 0x7D,

    /// <summary>
    /// Loads the argument at index 6 onto the evaluation stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    LDARG6 = 0x7E,

    /// <summary>
    /// Loads the argument at a specified index onto the evaluation stack.
    /// The index is represented as a 1-byte unsigned integer.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    LDARG = 0x7F,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 0.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG0 = 0x80,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 1.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG1 = 0x81,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 2.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG2 = 0x82,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 3.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG3 = 0x83,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 4.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG4 = 0x84,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 5.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG5 = 0x85,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at index 6.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    STARG6 = 0x86,

    /// <summary>
    /// Stores the value on top of the evaluation stack in the argument slot at a specified index.
    /// The index is represented as a 1-byte unsigned integer.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    STARG = 0x87,

    #endregion

    #region Splice

    /// <summary>
    /// Creates a new <see cref="Buffer"/> and pushes it onto the stack, and the top item is the length of the buffer.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <example> new Buffer(a) </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The top item cannot be converted to an integer.
    ///  2. The length is negative or exceeds the maximum item size.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NEWBUFFER = 0x88,

    /// <summary>
    /// Copies a range of bytes from one <see cref="Buffer"/> to another.
    /// Using this opcode will require to dup the destination buffer.
    /// If the destination start index, source start index or count is not an integer, it will be converted to an integer.
    ///
    /// <example> c[d..e].CopyTo(a[b..]); </example>
    ///
    /// <para>
    /// The top 5 items in the stack are(The `count` item is the top item):
    /// <c> | destination buffer | destination start index | source buffer | source start index | count </c>.
    /// </para>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the destination start index, source start index or count cannot be converted to integer.
    ///  2. The destination start index, source start index or count is negative(or converted value is negative).
    ///  3. The destination start index + count is out of the destination buffer range.
    ///  4. The source start index + count is out of the source buffer range.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 5 item(s)
    /// </remarks>
    /// </summary>
    MEMCPY = 0x89,

    /// <summary>
    /// Concatenates two items as a buffer. The result is the first pushed item concatenated with the second pushed item(the top item).
    /// If item is not a buffer, it will be converted to a buffer(bytes) and then concatenated.
    ///
    /// <example> a.concat(b) </example>
    ///
    /// <para>
    /// The execution will be faulted if:
    ///  1. the total length exceeds the maximum item size.
    ///  2. One or both items cannot be converted to a buffer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    CAT = 0x8B,

    /// <summary>
    /// Pushes a sub-buffer from the source buffer onto the evaluation stack.
    /// The first pushed item is the source buffer, the second pushed item is the start index, the third pushed item is the count(the top item).
    /// If the start index or count is not an integer, it will be converted to an integer.
    /// If the source buffer is not a buffer, it will be converted to a buffer(bytes).
    ///
    /// <example> a[b..c] </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The start index or count cannot be converted to integer.
    ///  2. The source buffer cannot be converted to buffer(bytes).
    ///  3. The start index or count is negative(or converted value is negative).
    ///  4. The start index + count is out of the source buffer range.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 3 item(s)
    /// </remarks>
    /// </summary>
    SUBSTR = 0x8C,

    /// <summary>
    /// Keeps only characters left of the specified point in a buffer.
    /// The first pushed item is the source buffer, the second pushed item is the count(the top item).
    /// If the count is not an integer, it will be converted to an integer.
    /// If the source buffer is not a buffer, it will be converted to a buffer(bytes).
    ///
    /// <example> a[..b] </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The count cannot be converted to integer.
    ///  2. The source buffer cannot be converted to buffer(bytes).
    ///  3. The count is negative(or converted value is negative) or out of the source buffer range.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    LEFT = 0x8D,

    /// <summary>
    /// Keeps only characters right of the specified point in a buffer.
    /// The first pushed item is the source buffer, the second pushed item is the count(the top item).
    /// If the count is not an integer, it will be converted to an integer.
    /// If the source buffer is not a buffer, it will be converted to a buffer(bytes).
    ///
    /// <example> a[^b..^0] </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. The count cannot be converted to integer.
    ///  2. The source buffer cannot be converted to buffer(bytes).
    ///  3. The count is negative(or converted value is negative) or out of the source buffer range.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    RIGHT = 0x8E,

    #endregion

    #region Bitwise logic

    /// <summary>
    /// Pops the top stack item and pushes the result of flipping all the bits in the item.
    /// If the item is not an integer, it will be converted to an integer.
    ///
    /// <example> ~a </example>
    /// <para>
    /// The execution will be faulted if the item cannot be converted to integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    INVERT = 0x90,

    /// <summary>
    /// Pops the top two stack items and pushes the result of the boolean and between each bit in the items.
    /// If the items are not integers, they will be converted to integers.
    ///
    /// <example> a&amp;b </example>
    /// <para>
    /// The execution will be faulted if the items cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    AND = 0x91,

    /// <summary>
    /// Pops the top two stack items and pushes the result of the boolean or between each bit in the items.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a|b </example>
    /// <para>
    /// The execution will be faulted if the items cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    OR = 0x92,

    /// <summary>
    /// Pops the top two stack items and pushes the result of the boolean exclusive or between each bit in the items.
    /// If the items are not integers, they will be converted to integers.
    ///
    /// <example> a^b </example>
    /// <para>
    /// The execution will be faulted if the items cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    XOR = 0x93,

    /// <summary>
    /// Pops the top two stack items and pushes true if the items are exactly equal, false otherwise.
    ///
    /// <example> a.Equals(b) </example>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    EQUAL = 0x97,

    /// <summary>
    /// Pops the top two stack items and pushes true if the items are not equal, false otherwise.
    ///
    /// <example> !a.Equals(b) </example>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    NOTEQUAL = 0x98,

    #endregion

    #region Arithmetic

    /// <summary>
    /// Pops the top stack item and pushes the sign of the item.
    /// If the item is negative, push -1; if positive, push 1; if zero, push 0.
    /// If the item is not an integer, it will be converted to an integer.
    /// 
    /// <example> a.Sign </example>
    /// <para>
    /// The execution will be faulted if the item cannot be converted to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    SIGN = 0x99,

    /// <summary>
    /// Pops the top stack item and pushes the absolute value of the item.
    /// If the item is not an integer, it will be converted to an integer.
    ///
    /// <example> abs(a) </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the item cannot be converted to an integer.
    ///  2. the item is the minimum integer value.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    ABS = 0x9A,

    /// <summary>
    /// Pops the top stack item and pushes the negation of the item.
    /// If the input is not an integer, it will be converted to an integer.
    ///
    /// <example> -a </example>
    /// <para>
    /// The execution will be faulted if the item cannot be converted to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NEGATE = 0x9B,

    /// <summary>
    /// Pops the top stack item and pushes the result of adding 1 to the item.
    /// If the input is not an integer, it will be converted to an integer.
    ///
    /// <example> a+1 </example>
    /// <para>
    /// The execution will be faulted if the item cannot be converted to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    INC = 0x9C,

    /// <summary>
    /// Pops the top stack item and pushes the result of subtracting 1 from the item.
    /// If the input is not an integer, it will be converted to an integer.
    ///
    /// <example> a-1 </example>
    /// <para>
    /// The execution will be faulted if the item cannot be converted to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    DEC = 0x9D,

    /// <summary>
    /// Pops the top two stack items and pushes the result of adding the first pushed item to the second pushed item(the top item).
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a+b </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    ADD = 0x9E,

    /// <summary>
    /// Pops the top two stack items and pushes the result of subtracting the second pushed item(the top item) from the first pushed item.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a-b </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    SUB = 0x9F,

    /// <summary>
    /// Pops the top two stack items and pushes the result of multiplying the first pushed item by the second pushed item(the top item).
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a*b </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    MUL = 0xA0,

    /// <summary>
    /// Pops the top two stack items and pushes the result of dividing the first pushed item by the second pushed item(the top item).
    /// The first pushed item is the dividend, the second pushed item is the divisor(the top item).
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a/b </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the inputs cannot be converted to integers.
    ///  2. the divisor is zero.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    DIV = 0xA1,

    /// <summary>
    /// Pops the top two stack items and pushes the remainder after dividing a by b.
    /// The first pushed item is the dividend, the second pushed item is the divisor(the top item).
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a%b </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the inputs cannot be converted to integers.
    ///  2. the divisor is zero.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    MOD = 0xA2,

    /// <summary>
    /// Pops the top two stack items and pushes the result of raising value to the exponent power.
    /// The first pushed item is the exponent, the second pushed item is the value(the top item).
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a^b </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    POW = 0xA3,

    /// <summary>
    /// Pops the top stack item and pushes the square root of the item.
    /// If the input is not an integer, it will be converted to an integer.
    ///
    /// <example> sqrt(a) </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the input cannot be converted to an integer.
    ///  2. the input is negative.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    SQRT = 0xA4,

    /// <summary>
    /// Performs modulus division on a number multiplied by another number.
    /// The third pushed item is the modulus.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a*b%c </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the inputs cannot be converted to integers.
    ///  2. the modulus is zero.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 3 item(s)
    /// </remarks>
    /// </summary>
    MODMUL = 0xA5,

    /// <summary>
    /// Performs modulus division on a number raised to the power of another number.
    /// If the exponent is -1, it will have the calculation of the modular inverse.
    ///
    /// The third pushed item is the modulus, the second pushed item is the exponent, the first pushed item is the value(the top item).
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> modpow(a, b, c) </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the inputs cannot be converted to integers.
    ///  2. the modulus is zero.
    ///  3. the exponent is negative and not -1.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 3 item(s)
    /// </remarks>
    /// </summary>
    MODPOW = 0xA6,

    /// <summary>
    /// Pops the top two stack items and shifts the first pushed item left by the second pushed item(the top item) bits, preserving sign.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a&lt;&lt;b </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the inputs cannot be converted to integers.
    ///  2. the shift amount is negative or out of the limit.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    SHL = 0xA8,

    /// <summary>
    /// Pops the top two stack items and shifts the first pushed item right by the second pushed item(the top item) bits, preserving sign.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> a>>b </example>
    /// <para>
    /// The execution will be faulted if:
    ///  1. the inputs cannot be converted to integers.
    ///  2. the shift amount is negative or out of the limit.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    SHR = 0xA9,

    /// <summary>
    /// Pushes true if the input is false value (false, null, zero), false otherwise.
    ///
    /// <example> !a </example>
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NOT = 0xAA,

    /// <summary>
    /// Pops the top two stack items and pushes true if both items are true value (true, not null, not zero), false otherwise.
    ///
    /// <example> b &amp;&amp; a </example>
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    BOOLAND = 0xAB,

    /// <summary>
    /// Pops the top two stack items and pushes true if either item is true value (true, not null, not zero), false otherwise.
    ///
    /// <example> b || a </example>
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    BOOLOR = 0xAC,

    /// <summary>
    /// Pops the top stack item and pushes true if the item is not 0, false otherwise.
    /// If the input is not an integer, it will be converted to an integer.
    ///
    /// <example> a != 0 </example>
    /// <para>
    /// The execution will be faulted if the input cannot be converted to an integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NZ = 0xB1,

    /// <summary>
    /// Pops the top two stack items and pushes true if the items are equal in number, false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> b == a </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    NUMEQUAL = 0xB3,

    /// <summary>
    /// Pops the top two stack items and pushes true if the items are not equal in number, false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> b != a </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    NUMNOTEQUAL = 0xB4,

    /// <summary>
    /// Pops the top two stack items and pushes true if the first pushed item is less than the second pushed item(the top item), false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> b>a </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    LT = 0xB5,

    /// <summary>
    /// Pops the top two stack items and pushes true if the first pushed item is less than or equal to the second pushed item(the top item), false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> b>=a </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    LE = 0xB6,

    /// <summary>
    /// Pops the top two stack items and pushes true if the first pushed item is greater than the second pushed item(the top item), false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> b>a </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    GT = 0xB7,

    /// <summary>
    /// Pops the top two stack items and pushes true if the first pushed item is greater than or equal to the second pushed item(the top item), false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> b>=a </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    GE = 0xB8,

    /// <summary>
    /// Pops the top two stack items and pushes the minimum of the two items.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> min(a, b) </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    MIN = 0xB9,

    /// <summary>
    /// Pops the top two stack items and pushes the maximum of the two items.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> max(a, b) </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    MAX = 0xBA,

    /// <summary>
    /// Pops the top three stack items and pushes true if the first pushed item is within the specified range [left, right), false otherwise.
    /// If the inputs are not integers, they will be converted to integers.
    ///
    /// <example> x within [left, right) </example>
    /// <para>
    /// The execution will be faulted if the inputs cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 3 item(s)
    /// </remarks>
    /// </summary>
    WITHIN = 0xBB,

    #endregion

    #region Compound-type

    /// <summary>
    /// A value n is taken from top of main stack.
    /// The next n*2 items on main stack are removed, put inside n-sized map and this map is put on top of the main stack.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <example> | value2 | key2 | value1 | key1 | 2 | -> | map{key1 = value1, key2 = value2} | </example>
    /// The key should be primitive type and if there are the same key, the last one will be used.
    /// <para>
    /// The execution will be faulted if:
    ///  1. the top item cannot be converted to integers.
    ///  2. Any key is not a primitive type.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2n+1 item(s)
    /// </remarks>
    /// </summary>
    PACKMAP = 0xBE,

    /// <summary>
    /// A value n is taken from top of main stack.
    /// The next n items on main stack are removed, put inside n-sized struct and this struct is put on top of the main stack.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <para>
    /// The execution will be faulted if the top item cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: n+1 item(s)
    /// </remarks>
    /// </summary>
    PACKSTRUCT = 0xBF,

    /// <summary>
    /// A value n is taken from top of main stack.
    /// The next n items on main stack are removed, put inside n-sized array and this array is put on top of the main stack.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <para>
    /// The execution will be faulted if the top item cannot be converted to integers.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: n+1 item(s)
    /// </remarks>
    /// </summary>
    PACK = 0xC0,

    /// <summary>
    /// A collection is removed from top of the main stack.
    /// Its elements are put on top of the main stack (in reverse order) and the collection size is also put on the main stack.
    ///
    /// <remarks>
    ///     Push: 2n+1 or n+1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    UNPACK = 0xC1,

    /// <summary>
    /// An empty array (with size 0) is put on top of the main stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    NEWARRAY0 = 0xC2,

    /// <summary>
    /// A value n is taken from top of main stack. A null-filled array with size n is put on top of the main stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NEWARRAY = 0xC3,

    /// <summary>
    /// An array of type T with size n filled with the default value of type T(false, 0, empty string or null) is put on top of the main stack.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <para>
    /// The execution will be faulted if:
    ///  1. the top item cannot be converted to integer.
    ///  2. the type operand is not a valid stack item type.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    NEWARRAY_T = 0xC4,

    /// <summary>
    /// An empty struct (with size 0) is put on top of the main stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    NEWSTRUCT0 = 0xC5,

    /// <summary>
    /// A value n is taken from top of main stack. A null-filled struct with size n is put on top of the main stack.
    /// If the top item is not an integer, it will be converted to an integer.
    ///
    /// <para>
    /// The execution will be faulted if the top item cannot be converted to integer.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    NEWSTRUCT = 0xC6,

    /// <summary>
    /// A empty map is created and put on top of the main stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 0 item(s)
    /// </remarks>
    /// </summary>
    NEWMAP = 0xC8,

    /// <summary>
    /// Pop the top item and push its size. The top item should be an array, map, buffer or primitive type.
    /// If the top item is an array or map, push its count.
    /// If the top item is a buffer or primitive type, push its size(in bytes).
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    SIZE = 0xCA,

    /// <summary>
    /// An input index n (or key) and an array (map, buffer or string) are removed from the top of the main stack.
    /// Pushes true to the stack if array[n](map[key], or the n-th byte of buffer/string) exist, and false otherwise.
    ///
    /// If the target is an array, buffer or string, the index will be converted to an integer.
    /// The index is the second pushed item(the top item), and the tartget is the first pushed item.
    ///
    /// <para>
    /// The execution will be faulted if:
    ///  1. The target is an array, buffer or string and the index cannot be converted to integer or is out of range.
    ///  2. The target is a map and the key is not a primitive type.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    HASKEY = 0xCB,

    /// <summary>
    /// A map is taken from top of the main stack. The keys of this map are put on top of the main stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    KEYS = 0xCC,

    /// <summary>
    /// An array or map is taken from top of the main stack. The values of this array or map are put on top of the main stack.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    VALUES = 0xCD,

    /// <summary>
    /// An input index n (or key) and an array (map, buffer or primitive type) are taken from main stack.
    /// Element array[n], or map[n], or buffer[n], or the n-th byte of primitive type(converted to integer) is put on top of the main stack.
    ///
    /// If the target is an array, buffer or primitive type, the index will be converted to an integer.
    /// The index is the second pushed item(the top item), and the tartget is the first pushed item.
    ///
    /// <para>
    /// The execution will be faulted if:
    ///  1. The target is an array, buffer or primitive type and the index cannot be converted to integer or is out of range.
    ///  2. The target is a map and the key is not a primitive type.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    PICKITEM = 0xCE,

    /// <summary>
    /// The item on top of main stack is removed and appended to the second item on top of the main stack.
    /// When we use this opcode, we should dup the second item on top of the main stack before using it.
    ///
    /// <example> a a b -> a.concat(b)</example>
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    APPEND = 0xCF,

    /// <summary>
    /// A value, index n (or key) and an array (or buffer, map) are taken from main stack.
    /// Attribution array[n] = value (or buffer[n] = value, map[key] = value) is performed.
    /// 
    /// The `value` is the third pushed item(the top item), the `n` or `key` is the second pushed item, and target is the first pushed item.
    /// If the target is an array or buffer, the index will be converted to an integer.
    /// If the tartget is a buffer, the value should within [-128, 255].
    /// 
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 3 item(s)
    /// </remarks>
    /// </summary>
    SETITEM = 0xD0,

    /// <summary>
    /// An array or buffer is removed from the top of the main stack and its elements are reversed.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    REVERSEITEMS = 0xD1,

    /// <summary>
    /// An input index n (or key) and an array (or map)are removed from the top of the main stack.
    /// Element array[n] (or map[key]) is removed.
    ///
    /// The index or key is the second pushed item(the top item), and the array or map is the first pushed item.
    /// If the target is an array, the index will be converted to an integer.
    ///
    /// <para>
    /// The execution will be faulted if:
    ///  1. The target is an array and the index cannot be converted to integer or is out of range.
    ///  2. The target is a map and the key is not a primitive type.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    REMOVE = 0xD2,

    /// <summary>
    /// Remove all the items from the compound-type.
    /// Using this opcode will need to dup the compound-type before using it.
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    CLEARITEMS = 0xD3,

    /// <summary>
    /// Remove the last element from an array, and push it onto the stack.
    /// Using this opcode will need to dup the array before using it.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    POPITEM = 0xD4,

    #endregion

    #region Types

    /// <summary>
    /// Pop the top item and push a bool value indicating whether the item is null.
    ///
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    ISNULL = 0xD8,

    /// <summary>
    /// Pop the top item and push a bool value indicating whether the item is of the specified type.
    ///
    /// <para>
    /// The execution will be faulted if the type operand(uint8) is invalid.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    ISTYPE = 0xD9,

    /// <summary>
    /// Pop the top item and convert it to the specified type, then push the converted item.
    ///
    /// <para>
    /// The execution will be faulted if:
    ///  1. The top item cannot convert to the specified type.
    ///  2. The type operand(uint8) is invalid.
    /// </para>
    /// <remarks>
    ///     Push: 1 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    [OperandSize(Size = 1)]
    CONVERT = 0xDB,

    #endregion

    #region Extensions

    /// <summary>
    /// Pops the top stack item. Then, turns the vm state to FAULT immediately, and cannot be caught.
    /// The top stack value is used as reason. The top item should be string or can be converted to string.
    ///
    /// <example>new Exception(a)</example>
    ///
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 1 item(s)
    /// </remarks>
    /// </summary>
    ABORTMSG = 0xE0,

    /// <summary>
    /// Pops the top two stack items.
    /// If the second-to-top stack value is false value (false, null, zero), exits the vm execution and sets the vm state to FAULT.
    /// In this case, the top stack value is used as reason for the exit. Otherwise, it is ignored.
    ///
    /// The top item should be string or can be converted to string.
    ///
    /// <para>
    /// The execution will be faulted if the top item is not string or cannot be converted to string.
    /// </para>
    /// <remarks>
    ///     Push: 0 item(s)
    ///     Pop: 2 item(s)
    /// </remarks>
    /// </summary>
    ASSERTMSG = 0xE1

    #endregion
}
