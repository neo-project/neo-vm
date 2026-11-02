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

namespace Neo.VM;

/// <summary>
/// Represents the opcode of the NEO-VM.
/// </summary>
/// <remarks>
/// <para>
/// Stack diagrams are written from bottom to top. The rightmost item is the stack top;
/// in <c>[..., x1, x2]</c>, x2 is on top. A fault may occur after some operands have already
/// been removed, so no rollback of the operand stack is guaranteed.
/// </para>
/// <para>
/// A value is integer-convertible when it is Boolean (0 or 1), Integer, or a ByteString
/// of at most 32 bytes interpreted as a signed little-endian integer. Buffer, Null, Pointer,
/// InteropInterface, and composite values are not directly integer-convertible. Use CONVERT
/// when an explicit conversion is required.
/// </para>
/// <para>
/// Boolean interpretation is defined as follows: Null is false; Boolean keeps its value;
/// Integer is true when non-zero; a ByteString of at most 32 bytes is true when it contains
/// a non-zero byte and false when empty or all zero; Buffer, Array, Struct, Map, Pointer, and
/// InteropInterface are always true, including empty values.
/// </para>
/// <para>
/// Byte-oriented operations accept the byte representation of Boolean, Integer, ByteString,
/// or Buffer. Boolean occupies one byte. Integer uses its minimal signed little-endian
/// representation, with zero represented by no bytes. Text interpretation requires strict
/// UTF-8; Null has no text and no byte representation.
/// </para>
/// <para>
/// EQUAL performs no numeric or textual coercion. Null equals only Null. Boolean, Integer,
/// and ByteString compare by value only with the same type. Pointer compares script identity
/// and position. Array, Buffer, and Map compare by identity. Struct compares recursively by
/// content. InteropInterface uses equality defined by the wrapped host value.
/// </para>
/// <para>
/// An uncatchable fault terminates execution immediately and does not enter a
/// try/catch/finally region. ABORT, a false ASSERT, invalid operands, and invalid types are
/// generally uncatchable. Exceptions raised by THROW, a pending exception re-raised by
/// ENDFINALLY, and documented index or missing-key errors from PICKITEM/SETITEM are
/// catchable by script exception handling.
/// </para>
/// <para>
/// Operations are subject to configured VM resource limits. Integer results must fit the
/// VM integer-size limit (the default value is 256-bit).
/// Strict script validation may reject invalid control-flow targets
/// and invalid type operands before execution; otherwise such conditions fault when reached.
/// </para>
/// </remarks>
public enum OpCode : byte
{
    #region Constants

    /// <summary>
    /// Pushes an 8-bit signed integer onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A fixed 1-byte two's-complement value.</para>
    /// <para>Type rules: The result is an Integer.</para>
    /// <para>Stack: <c>[] -> [value]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure after decoding.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    PUSHINT8 = 0x00,

    /// <summary>
    /// Pushes a 16-bit signed integer onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A fixed 2-byte little-endian two's-complement value.</para>
    /// <para>Type rules: The result is an Integer.</para>
    /// <para>Stack: <c>[] -> [value]</c>.</para>
    /// <para>Faults: An incomplete operand makes the instruction invalid.</para>
    /// </remarks>
    [OperandSize(Size = 2)]
    PUSHINT16 = 0x01,

    /// <summary>
    /// Pushes a 32-bit signed integer onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A fixed 4-byte little-endian two's-complement value.</para>
    /// <para>Type rules: The result is an Integer.</para>
    /// <para>Stack: <c>[] -> [value]</c>.</para>
    /// <para>Faults: An incomplete operand makes the instruction invalid.</para>
    /// </remarks>
    [OperandSize(Size = 4)]
    PUSHINT32 = 0x02,

    /// <summary>
    /// Pushes a 64-bit signed integer onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A fixed 8-byte little-endian two's-complement value.</para>
    /// <para>Type rules: The result is an Integer.</para>
    /// <para>Stack: <c>[] -> [value]</c>.</para>
    /// <para>Faults: An incomplete operand makes the instruction invalid.</para>
    /// </remarks>
    [OperandSize(Size = 8)]
    PUSHINT64 = 0x03,

    /// <summary>
    /// Pushes a 128-bit signed integer onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A fixed 16-byte little-endian two's-complement value.</para>
    /// <para>Type rules: The result is an Integer.</para>
    /// <para>Stack: <c>[] -> [value]</c>.</para>
    /// <para>Faults: An incomplete operand makes the instruction invalid.</para>
    /// </remarks>
    [OperandSize(Size = 16)]
    PUSHINT128 = 0x04,

    /// <summary>
    /// Pushes a 256-bit signed integer.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A fixed 32-byte little-endian two's-complement value.</para>
    /// <para>Type rules: The result is an Integer.</para>
    /// <para>Stack: <c>[] -> [value]</c>.</para>
    /// <para>Faults: An incomplete operand makes the instruction invalid.</para>
    /// </remarks>
    [OperandSize(Size = 32)]
    PUSHINT256 = 0x05,

    /// <summary>
    /// Pushes the Boolean value true onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is Boolean true, not the Integer value 1.</para>
    /// <para>Stack: <c>[] -> [true]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSHT = 0x08,

    /// <summary>
    /// Pushes the Boolean value false onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is Boolean false, not Integer 0 or Null.</para>
    /// <para>Stack: <c>[] -> [false]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSHF = 0x09,

    /// <summary>
    /// Pushes a code pointer to a location in the current script.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of PUSHA.</para>
    /// <para>Type rules: The result is a Pointer containing a script identity and an absolute byte position. It is not an integer address.</para>
    /// <para>Stack: <c>[] -> [pointer]</c>.</para>
    /// <para>The position at the end of the script is valid.</para>
    /// <para>
    /// Faults: The offset overflows, or the target lies outside the inclusive range from the beginning to the end of the script.
    /// With strict script validation, the target must also be the start of an instruction.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    PUSHA = 0x0A,

    /// <summary>
    /// Pushes the VM Null value onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Null is distinct from an empty ByteString, false, and zero.</para>
    /// <para>Stack: <c>[] -> [null]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSHNULL = 0x0B,

    /// <summary>
    /// Pushes raw data following a 1-byte length prefix as a ByteString onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: Reads a 1-byte length, followed by exactly that many data bytes.</para>
    /// <para>Type rules: The data is not decoded or converted. The length prefix is not part of the resulting ByteString.</para>
    /// <para>Stack: <c>[] -> [data]</c>.</para>
    /// <para>Faults: An incomplete length or payload makes the instruction invalid, or the payload exceeds the configured item-size limit.</para>
    /// </remarks>
    [OperandSize(SizePrefix = 1)]
    PUSHDATA1 = 0x0C,

    /// <summary>
    /// Pushes raw data following a 2-byte length prefix as a ByteString onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: Reads a 2-byte little-endian length, followed by exactly that many data bytes.</para>
    /// <para>Type rules: The data is not decoded or converted. The length prefix is not part of the resulting ByteString.</para>
    /// <para>Stack: <c>[] -> [data]</c>.</para>
    /// <para>Faults: An incomplete length or payload makes the instruction invalid, or the payload exceeds the configured item-size limit.</para>
    /// </remarks>
    [OperandSize(SizePrefix = 2)]
    PUSHDATA2 = 0x0D,

    /// <summary>
    /// Pushes raw data following a 4-byte length prefix as a ByteString onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Operand: Reads a 4-byte little-endian signed length, followed by exactly that many data bytes.</para>
    /// <para>Type rules: The data is not decoded or converted. The length prefix is not part of the resulting ByteString.</para>
    /// <para>Stack: <c>[] -> [data]</c>.</para>
    /// <para>
    /// Faults: An incomplete length or payload makes the instruction invalid;
    /// a negative length or a payload larger than the configured item-size limit also faults.
    /// </para>
    /// </remarks>
    [OperandSize(SizePrefix = 4)]
    PUSHDATA4 = 0x0E,

    /// <summary>
    /// Pushes the Integer value -1 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value -1.</para>
    /// <para>Stack: <c>[] -> [-1]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSHM1 = 0x0F,

    /// <summary>
    /// Pushes the Integer value 0 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 0.</para>
    /// <para>Stack: <c>[] -> [0]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH0 = 0x10,

    /// <summary>
    /// Pushes the Integer value 1 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 1.</para>
    /// <para>Stack: <c>[] -> [1]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH1 = 0x11,

    /// <summary>
    /// Pushes the Integer value 2 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 2.</para>
    /// <para>Stack: <c>[] -> [2]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH2 = 0x12,

    /// <summary>
    /// Pushes the Integer value 3 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 3.</para>
    /// <para>Stack: <c>[] -> [3]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH3 = 0x13,

    /// <summary>
    /// Pushes the Integer value 4 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 4.</para>
    /// <para>Stack: <c>[] -> [4]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH4 = 0x14,

    /// <summary>
    /// Pushes the Integer value 5 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 5.</para>
    /// <para>Stack: <c>[] -> [5]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH5 = 0x15,

    /// <summary>
    /// Pushes the Integer value 6 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 6.</para>
    /// <para>Stack: <c>[] -> [6]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH6 = 0x16,

    /// <summary>
    /// Pushes the Integer value 7 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 7.</para>
    /// <para>Stack: <c>[] -> [7]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH7 = 0x17,

    /// <summary>
    /// Pushes the Integer value 8 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 8.</para>
    /// <para>Stack: <c>[] -> [8]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH8 = 0x18,

    /// <summary>
    /// Pushes the Integer value 9 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 9.</para>
    /// <para>Stack: <c>[] -> [9]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH9 = 0x19,

    /// <summary>
    /// Pushes the Integer value 10 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 10.</para>
    /// <para>Stack: <c>[] -> [10]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH10 = 0x1A,

    /// <summary>
    /// Pushes the Integer value 11 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 11.</para>
    /// <para>Stack: <c>[] -> [11]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH11 = 0x1B,

    /// <summary>
    /// Pushes the Integer value 12 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 12.</para>
    /// <para>Stack: <c>[] -> [12]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH12 = 0x1C,

    /// <summary>
    /// Pushes the Integer value 13 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 13.</para>
    /// <para>Stack: <c>[] -> [13]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH13 = 0x1D,

    /// <summary>
    /// Pushes the Integer value 14 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 14.</para>
    /// <para>Stack: <c>[] -> [14]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH14 = 0x1E,

    /// <summary>
    /// Pushes the Integer value 15 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 15.</para>
    /// <para>Stack: <c>[] -> [15]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH15 = 0x1F,

    /// <summary>
    /// Pushes the Integer value 16 onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer with value 16.</para>
    /// <para>Stack: <c>[] -> [16]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    PUSH16 = 0x20,

    #endregion

    #region Flow control

    /// <summary>
    /// Performs no operation.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: No stack item is read or written.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    NOP = 0x21,

    /// <summary>
    /// Transfers control unconditionally to a relative address.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the JMP instruction.</para>
    /// <para>Type rules: No stack item is read.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// Faults: The address calculation overflows, or the target is not a byte position inside the script.
    /// With strict script validation, it must also be the start of an instruction.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMP = 0x22,

    /// <summary>
    /// Transfers control unconditionally to a relative address.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the JMP instruction.</para>
    /// <para>Type rules: No stack item is read.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// Faults: The address calculation overflows, or the target is not a byte position inside the script.
    /// With strict script validation, it must also be the start of an instruction.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMP_L = 0x23,

    /// <summary>
    /// Pops a condition and jumps when its Boolean value is true.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the current instruction.</para>
    /// <para>Type rules: The condition is interpreted as a Boolean. The jump occurs only when the result is true.</para>
    /// <para>Stack: <c>[..., condition] -> [...]</c>.</para>
    /// <para>
    /// Faults: The condition cannot be interpreted as a Boolean, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPIF = 0x24,

    /// <summary>
    /// Pops a condition and jumps when its Boolean value is true.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the current instruction.</para>
    /// <para>Type rules: The condition is interpreted as a Boolean. The jump occurs only when the result is true.</para>
    /// <para>Stack: <c>[..., condition] -> [...]</c>.</para>
    /// <para>
    /// Faults: The condition cannot be interpreted as a Boolean, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPIF_L = 0x25,

    /// <summary>
    /// Pops a condition and jumps when its Boolean value is false.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the current instruction.</para>
    /// <para>Type rules: The condition is interpreted as a Boolean. The jump occurs only when the result is false.</para>
    /// <para>Stack: <c>[..., condition] -> [...]</c>.</para>
    /// <para>
    /// Faults: The condition cannot be interpreted as a Boolean, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPIFNOT = 0x26,

    /// <summary>
    /// Pops a condition and jumps when its Boolean value is false.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the current instruction.</para>
    /// <para>Type rules: The condition is interpreted as a Boolean. The jump occurs only when the result is false.</para>
    /// <para>Stack: <c>[..., condition] -> [...]</c>.</para>
    /// <para>
    /// Faults: The condition cannot be interpreted as a Boolean, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPIFNOT_L = 0x27,

    /// <summary>
    /// Pops two integers and jumps when x1 is equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPEQ = 0x28,

    /// <summary>
    /// Pops two integers and jumps when x1 is equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPEQ_L = 0x29,

    /// <summary>
    /// Pops two integers and jumps when x1 is not equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is not equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPNE = 0x2A,

    /// <summary>
    /// Pops two integers and jumps when x1 is not equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is not equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPNE_L = 0x2B,

    /// <summary>
    /// Pops two integers and jumps when x1 is greater than x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is greater than x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPGT = 0x2C,

    /// <summary>
    /// Pops two integers and jumps when x1 is greater than x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is greater than x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPGT_L = 0x2D,

    /// <summary>
    /// Pops two integers and jumps when x1 is greater than or equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is greater than or equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPGE = 0x2E,

    /// <summary>
    /// Pops two integers and jumps when x1 is greater than or equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is greater than or equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPGE_L = 0x2F,

    /// <summary>
    /// Pops two integers and jumps when x1 is less than x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is less than x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPLT = 0x30,

    /// <summary>
    /// Pops two integers and jumps when x1 is less than x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is less than x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPLT_L = 0x31,

    /// <summary>
    /// Pops two integers and jumps when x1 is less than or equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is less than or equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    JMPLE = 0x32,

    /// <summary>
    /// Pops two integers and jumps when x1 is less than or equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the jump instruction.</para>
    /// <para>Type rules: x2 is the top item. Both items are integer-convertible. The jump condition is that x1 is less than or equal to x2.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [...]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, or a taken jump has an overflowing or out-of-range target.
    /// A target is not validated when the jump is not taken; strict script validation checks it independently.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    JMPLE_L = 0x33,

    /// <summary>
    /// Calls code in the current script.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of the CALL instruction.</para>
    /// <para>Type rules: The callee uses the caller's operand stack and shared static fields. A new call frame has its own arguments and local variables.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// Faults: The address calculation overflows, the target is outside the inclusive byte range of the script, or the call-depth limit is reached.
    /// With strict script validation, the target must be the start of an instruction.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    CALL = 0x34,

    /// <summary>
    /// Calls code in the current script.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of the CALL instruction.</para>
    /// <para>Type rules: The callee uses the caller's operand stack and shared static fields. A new call frame has its own arguments and local variables.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// Faults: The address calculation overflows, the target is outside the inclusive byte range of the script, or the call-depth limit is reached.
    /// With strict script validation, the target must be the start of an instruction.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    CALL_L = 0x35,

    /// <summary>
    /// Pops a Pointer and calls the referenced script location.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The top item must be a Pointer. No integer conversion is performed.</para>
    /// <para>Stack: <c>[..., pointer] -> [...]</c>.</para>
    /// <para>Faults: The top item is not a Pointer, the Pointer refers to another script, the target is invalid, or the call-depth limit is reached.</para>
    /// </remarks>
    CALLA = 0x36,

    /// <summary>
    /// Calls a host-defined function identified by a token.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 2-byte unsigned little-endian token.</para>
    /// <para>Type rules: The meaning of the token and the types used by the target function are supplied by the host.</para>
    /// <para>Stack: <c>Defined by the bound function</c>.</para>
    /// <para>Faults: No function is registered for the token, so the VM faults.</para>
    /// </remarks>
    [OperandSize(Size = 2)]
    CALLT = 0x37,

    /// <summary>
    /// Terminates execution immediately.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: No stack item is read.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>Faults: Always faults. The fault is not catchable by a try/catch/finally region.</para>
    /// </remarks>
    ABORT = 0x38,

    /// <summary>
    /// Pops a condition and continues only when it is true.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The condition is interpreted as a Boolean.</para>
    /// <para>Stack: <c>[..., condition] -> [...]</c>.</para>
    /// <para>
    /// Faults: The condition cannot be interpreted as a Boolean, or it is false.
    /// A false assertion is not catchable by a try/catch/finally region.
    /// </para>
    /// </remarks>
    ASSERT = 0x39,

    /// <summary>
    /// Pops a value and raises it as a script exception.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The exception value may be any stack item. It is not required to be a string.</para>
    /// <para>Stack: <c>[..., exception] -> [...]</c>.</para>
    /// <para>
    /// Faults: If a catch handler is reached, the exception is pushed at that handler and execution continues there.
    /// If only finally handlers are available, control enters finally. If no handler remains, execution faults.
    /// </para>
    /// </remarks>
    THROW = 0x3A,

    /// <summary>
    /// Registers catch and/or finally handlers for the current try region.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operand: Two consecutive 1-byte signed little-endian offsets for catch and finally, relative to the start of TRY.
    /// Zero means that the corresponding handler is absent.
    /// </para>
    /// <para>Type rules: No stack item is read or converted.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>Handler locations are checked when control actually transfers to them.</para>
    /// <para>Faults: Both offsets are zero, an offset calculation overflows, or the configured nesting limit is reached.</para>
    /// </remarks>
    [OperandSize(Size = 2)]
    TRY = 0x3B,

    /// <summary>
    /// Registers catch and/or finally handlers for the current try region.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operand: Two consecutive 4-byte signed little-endian offsets for catch and finally, relative to the start of TRY.
    /// Zero means that the corresponding handler is absent.
    /// </para>
    /// <para>Type rules: No stack item is read or converted.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>Handler locations are checked when control actually transfers to them.</para>
    /// <para>Faults: Both offsets are zero, an offset calculation overflows, or the configured nesting limit is reached.</para>
    /// </remarks>
    [OperandSize(Size = 8)]
    TRY_L = 0x3C,

    /// <summary>
    /// Ends the current try or catch region and selects the continuation point.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte signed little-endian offset relative to the start of ENDTRY.</para>
    /// <para>Type rules: No stack item is read or converted.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// If finally exists, the continuation is saved, the region enters finally, and validation of the continuation is deferred until finally completes.
    /// </para>
    /// <para>Otherwise the handler is removed and control transfers directly to the continuation.</para>
    /// <para>
    /// Faults: There is no active handler, the current region is already in finally, or the address calculation overflows.
    /// Without a finally handler, the continuation must also be a valid byte position in the script.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    ENDTRY = 0x3D,

    /// <summary>
    /// Ends the current try or catch region and selects the continuation point.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte signed little-endian offset relative to the start of ENDTRY.</para>
    /// <para>Type rules: No stack item is read or converted.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// If finally exists, the continuation is saved, the region enters finally, and validation of the continuation is deferred until finally completes.
    /// </para>
    /// <para>Otherwise the handler is removed and control transfers directly to the continuation.</para>
    /// <para>
    /// Faults: There is no active handler, the current region is already in finally, or the address calculation overflows.
    /// Without a finally handler, the continuation must also be a valid byte position in the script.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 4)]
    ENDTRY_L = 0x3E,

    /// <summary>
    /// Completes the current finally region.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: No stack item is read or converted.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>
    /// With no pending exception, control resumes at the continuation selected by ENDTRY.
    /// A pending exception is raised again in the enclosing context.
    /// </para>
    /// <para>Faults: There is no active handler, or there is no pending exception and the saved continuation is not a valid byte position in the script.</para>
    /// </remarks>
    ENDFINALLY = 0x3F,

    /// <summary>
    /// Returns from the current call frame.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Return values are not converted.</para>
    /// <para>Stack: <c>The items remaining on the current operand stack are returned</c>.</para>
    /// <para>Returning from the outer frame places its remaining stack items in the VM result and moves the VM to HALT.</para>
    /// <para>Faults: When a return-value count is specified for the frame, the number of remaining items must match it.</para>
    /// </remarks>
    RET = 0x40,

    /// <summary>
    /// Calls a host-defined system service.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 4-byte unsigned little-endian service identifier.</para>
    /// <para>Type rules: Argument types and stack effects are defined by the bound service.</para>
    /// <para>Stack: <c>Depends on the service</c>.</para>
    /// <para>Faults: No service is registered for the identifier, so the VM faults.</para>
    /// </remarks>
    [OperandSize(Size = 4)]
    SYSCALL = 0x41,

    #endregion

    #region Stack

    /// <summary>
    /// Pushes the number of items on the operand stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The result is an Integer. It counts the items present before this instruction runs and does not include the new result.</para>
    /// <para>Stack: <c>[...] -> [..., depth]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    DEPTH = 0x43,

    /// <summary>
    /// Removes the top item.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is not interpreted.</para>
    /// <para>Stack: <c>[..., x] -> [...]</c>.</para>
    /// <para>Faults: The stack is empty.</para>
    /// </remarks>
    DROP = 0x45,

    /// <summary>
    /// Removes the item immediately below the top item.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Stack items are not interpreted.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., x2]</c>.</para>
    /// <para>Faults: There are fewer than two items.</para>
    /// </remarks>
    NIP = 0x46,

    /// <summary>
    /// Removes the item at a given depth below the top.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: n is integer-convertible and is a zero-based depth after the depth operand itself is removed; depth 0 denotes the new top item.</para>
    /// <para>Stack: <c>[..., x[n], x[n-1], ..., x[0], n] -> [..., x[n-1], ..., x[0]]</c>.</para>
    /// <para>Faults: n cannot be interpreted as an integer, is negative, or is not a valid depth in the remaining stack.</para>
    /// </remarks>
    XDROP = 0x48,

    /// <summary>
    /// Removes all items from the operand stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Only the operand stack is cleared. Arguments, local variables, and static fields are unchanged.</para>
    /// <para>Stack: <c>[x1, ..., xn] -> []</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    CLEAR = 0x49,

    /// <summary>
    /// Duplicates the top item.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The duplicate shares the same underlying value. Composite values are not deep-copied.</para>
    /// <para>Stack: <c>[..., x] -> [..., x, x]</c>.</para>
    /// <para>Faults: The stack is empty.</para>
    /// </remarks>
    DUP = 0x4A,

    /// <summary>
    /// Copies the second item from the top to the top.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The copied item is not deep-copied.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., x1, x2, x1]</c>.</para>
    /// <para>Faults: There are fewer than two items.</para>
    /// </remarks>
    OVER = 0x4B,

    /// <summary>
    /// Copies the item at a given depth below the top.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: n is integer-convertible and identifies a depth after n itself is removed. Depth 0 is the new top item.</para>
    /// <para>Stack: <c>[..., x[n], ..., x[0], n] -> [..., x[n], ..., x[0], x[n]]</c>.</para>
    /// <para>Faults: n cannot be interpreted as an integer, is negative, or is not a valid depth in the remaining stack.</para>
    /// </remarks>
    PICK = 0x4D,

    /// <summary>
    /// Copies the top item beneath the former second item.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The duplicated item is not deep-copied.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., x2, x1, x2]</c>.</para>
    /// <para>Faults: There are fewer than two items.</para>
    /// </remarks>
    TUCK = 0x4E,

    /// <summary>
    /// Exchanges the top two items.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Stack items are not interpreted.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., x2, x1]</c>.</para>
    /// <para>Faults: There are fewer than two items.</para>
    /// </remarks>
    SWAP = 0x50,

    /// <summary>
    /// Moves the third item to the top.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Stack items are not interpreted.</para>
    /// <para>Stack: <c>[..., x1, x2, x3] -> [..., x2, x3, x1]</c>.</para>
    /// <para>Faults: There are fewer than three items.</para>
    /// </remarks>
    ROT = 0x51,

    /// <summary>
    /// Moves the item at a given depth to the top.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: n is integer-convertible and identifies a depth after n itself is removed. Depth 0 leaves the order unchanged.</para>
    /// <para>Stack: <c>[..., x[n], x[n-1], ..., x[0], n] -> [..., x[n-1], ..., x[0], x[n]]</c>.</para>
    /// <para>Faults: n cannot be interpreted as an integer, is negative, or is not a valid depth in the remaining stack.</para>
    /// </remarks>
    ROLL = 0x52,

    /// <summary>
    /// Reverses the top three items.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Stack items are not interpreted.</para>
    /// <para>Stack: <c>[..., x1, x2, x3] -> [..., x3, x2, x1]</c>.</para>
    /// <para>Faults: There are fewer than three items.</para>
    /// </remarks>
    REVERSE3 = 0x53,

    /// <summary>
    /// Reverses the top four items.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Stack items are not interpreted.</para>
    /// <para>Stack: <c>[..., x1, x2, x3, x4] -> [..., x4, x3, x2, x1]</c>.</para>
    /// <para>Faults: There are fewer than four items.</para>
    /// </remarks>
    REVERSE4 = 0x54,

    /// <summary>
    /// Reverses a given number of items at the top of the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: n is integer-convertible. Values 0 and 1 leave the order unchanged.</para>
    /// <para>Stack: <c>[..., x1, ..., xn, n] -> [..., xn, ..., x1]</c>.</para>
    /// <para>Faults: n is negative or exceeds the number of items remaining after n is removed.</para>
    /// </remarks>
    REVERSEN = 0x55,

    #endregion

    #region Slot

    /// <summary>
    /// Initializes the static-field area for the current script.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned field count.</para>
    /// <para>Type rules: Each field initially contains Null and may later hold any stack item.</para>
    /// <para>Stack: <c>Unchanged</c>.</para>
    /// <para>All call frames for the same script share the static-field area.</para>
    /// <para>Faults: The count is zero, static fields were already initialized for this script, or the new storage exceeds the configured stack limit.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    INITSSLOT = 0x56,

    /// <summary>
    /// Initializes local variables and optionally loads arguments for the current call frame.
    /// </summary>
    /// <remarks>
    /// <para>Operand: Two bytes: the number of local variables followed by the number of arguments.</para>
    /// <para>
    /// Type rules: Local variables initially contain Null. Arguments are removed from the stack top first, so the former top item becomes argument 0.
    /// </para>
    /// <para>Stack: <c>[..., argument[n-1], ..., argument[0]] -> [...]</c>.</para>
    /// <para>A count of zero leaves that kind of storage uninitialized.</para>
    /// <para>
    /// Faults: Both counts are zero, the slots were already initialized, there are too few arguments on the stack,
    /// or the new storage exceeds the configured stack limit.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 2)]
    INITSLOT = 0x57,

    /// <summary>
    /// Pushes static field[0].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 0.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD0 = 0x58,

    /// <summary>
    /// Pushes static field[1].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 1.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD1 = 0x59,

    /// <summary>
    /// Pushes static field[2].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 2.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD2 = 0x5A,

    /// <summary>
    /// Pushes static field[3].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 3.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD3 = 0x5B,

    /// <summary>
    /// Pushes static field[4].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 4.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD4 = 0x5C,

    /// <summary>
    /// Pushes static field[5].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 5.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD5 = 0x5D,

    /// <summary>
    /// Pushes static field[6].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The static field area must be initialized and contain index 6.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDSFLD6 = 0x5E,

    /// <summary>
    /// Pushes the static field selected by an unsigned byte index.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned index.</para>
    /// <para>Type rules: The value is not converted or copied.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The static field area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    LDSFLD = 0x5F,

    /// <summary>
    /// Stores the top item in static field[0].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD0 = 0x60,

    /// <summary>
    /// Stores the top item in static field[1].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD1 = 0x61,

    /// <summary>
    /// Stores the top item in static field[2].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD2 = 0x62,

    /// <summary>
    /// Stores the top item in static field[3].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD3 = 0x63,

    /// <summary>
    /// Stores the top item in static field[4].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD4 = 0x64,

    /// <summary>
    /// Stores the top item in static field[5].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD5 = 0x65,

    /// <summary>
    /// Stores the top item in static field[6].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STSFLD6 = 0x66,

    /// <summary>
    /// Stores the top item in static field at an unsigned byte index.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned index.</para>
    /// <para>Type rules: Any stack item may be stored without conversion or copying.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The static field area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    STSFLD = 0x67,

    /// <summary>
    /// Pushes local variable[0].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 0.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC0 = 0x68,

    /// <summary>
    /// Pushes local variable[1].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 1.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC1 = 0x69,

    /// <summary>
    /// Pushes local variable[2].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 2.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC2 = 0x6A,

    /// <summary>
    /// Pushes local variable[3].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 3.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC3 = 0x6B,

    /// <summary>
    /// Pushes local variable[4].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 4.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC4 = 0x6C,

    /// <summary>
    /// Pushes local variable[5].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 5.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC5 = 0x6D,

    /// <summary>
    /// Pushes local variable[6].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The local variable area must be initialized and contain index 6.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDLOC6 = 0x6E,

    /// <summary>
    /// Pushes the local variable selected by an unsigned byte index.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned index.</para>
    /// <para>Type rules: The value is not converted or copied.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The local variable area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    LDLOC = 0x6F,

    /// <summary>
    /// Stores the top item in local variable[0].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC0 = 0x70,

    /// <summary>
    /// Stores the top item in local variable[1].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC1 = 0x71,

    /// <summary>
    /// Stores the top item in local variable[2].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC2 = 0x72,

    /// <summary>
    /// Stores the top item in local variable[3].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC3 = 0x73,

    /// <summary>
    /// Stores the top item in local variable[4].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC4 = 0x74,

    /// <summary>
    /// Stores the top item in local variable[5].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC5 = 0x75,

    /// <summary>
    /// Stores the top item in local variable[6].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STLOC6 = 0x76,

    /// <summary>
    /// Stores the top item in local variable at an unsigned byte index.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned index.</para>
    /// <para>Type rules: Any stack item may be stored without conversion or copying.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The local variable area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    STLOC = 0x77,

    /// <summary>
    /// Pushes argument[0].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 0.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG0 = 0x78,

    /// <summary>
    /// Pushes argument[1].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 1.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG1 = 0x79,

    /// <summary>
    /// Pushes argument[2].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 2.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG2 = 0x7A,

    /// <summary>
    /// Pushes argument[3].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 3.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG3 = 0x7B,

    /// <summary>
    /// Pushes argument[4].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 4.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG4 = 0x7C,

    /// <summary>
    /// Pushes argument[5].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 5.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG5 = 0x7D,

    /// <summary>
    /// Pushes argument[6].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The value is not converted or copied. The argument area must be initialized and contain index 6.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    LDARG6 = 0x7E,

    /// <summary>
    /// Pushes the argument selected by an unsigned byte index.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned index.</para>
    /// <para>Type rules: The value is not converted or copied.</para>
    /// <para>Stack: <c>[...] -> [..., value]</c>.</para>
    /// <para>Faults: The argument area is not initialized or the index is outside its declared size.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    LDARG = 0x7F,

    /// <summary>
    /// Stores the top item in argument[0].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG0 = 0x80,

    /// <summary>
    /// Stores the top item in argument[1].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG1 = 0x81,

    /// <summary>
    /// Stores the top item in argument[2].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG2 = 0x82,

    /// <summary>
    /// Stores the top item in argument[3].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG3 = 0x83,

    /// <summary>
    /// Stores the top item in argument[4].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG4 = 0x84,

    /// <summary>
    /// Stores the top item in argument[5].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG5 = 0x85,

    /// <summary>
    /// Stores the top item in argument[6].
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Any stack item may be stored without conversion or copying. The previous value is replaced.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    STARG6 = 0x86,

    /// <summary>
    /// Stores the top item in argument at an unsigned byte index.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte unsigned index.</para>
    /// <para>Type rules: Any stack item may be stored without conversion or copying.</para>
    /// <para>Stack: <c>[..., value] -> [...]</c>.</para>
    /// <para>Faults: The argument area is not initialized, the index is outside its declared size, or the stack is empty.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    STARG = 0x87,

    #endregion

    #region Splice

    /// <summary>
    /// Creates a zero-filled Buffer of a requested length.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: length is integer-convertible. The result is a mutable Buffer containing only zero bytes.</para>
    /// <para>Stack: <c>[..., length] -> [..., buffer]</c>.</para>
    /// <para>Faults: length cannot be interpreted as an integer, is negative, or exceeds the configured item-size limit.</para>
    /// </remarks>
    NEWBUFFER = 0x88,

    /// <summary>
    /// Copies bytes from one memory value into a Buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: The destination is a Buffer. The source is a Boolean, Integer, ByteString, or Buffer.
    /// Both indices and count are integer-convertible.
    /// </para>
    /// <para>Stack: <c>[..., destination, destinationIndex, source, sourceIndex, count] -> [...]</c>.</para>
    /// <para>The operation overwrites count bytes in place and does not resize either value. The ranges may overlap.</para>
    /// <para>
    /// Faults: The destination or source has an unsupported type, count or an index cannot be interpreted as an integer,
    /// any value is negative, or the source or destination range is out of bounds.
    /// </para>
    /// </remarks>
    MEMCPY = 0x89,

    /// <summary>
    /// Concatenates two byte sequences.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x1 and x2 are Boolean, Integer, ByteString, or Buffer values. The result is a new Buffer.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., buffer]</c>.</para>
    /// <para>Faults: Either operand has an unsupported type, or the combined size exceeds the configured item-size limit.</para>
    /// </remarks>
    CAT = 0x8B,

    /// <summary>
    /// Extracts a byte range.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: index and count are integer-convertible. bytes is a Boolean, Integer, ByteString, or Buffer. The result is a new Buffer.</para>
    /// <para>Stack: <c>[..., bytes, index, count] -> [..., buffer]</c>.</para>
    /// <para>Faults: index or count cannot be interpreted as an integer, is negative, or the range [index, index + count) is outside the input.</para>
    /// </remarks>
    SUBSTR = 0x8C,

    /// <summary>
    /// Copies bytes from the beginning of a value.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: count is integer-convertible. bytes is a Boolean, Integer, ByteString, or Buffer. The result is a new Buffer.</para>
    /// <para>Stack: <c>[..., bytes, count] -> [..., buffer]</c>.</para>
    /// <para>Faults: count cannot be interpreted as an integer, is negative, greater than the input size, or bytes has an unsupported type.</para>
    /// </remarks>
    LEFT = 0x8D,

    /// <summary>
    /// Copies bytes from the end of a value.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: count is integer-convertible. bytes is a Boolean, Integer, ByteString, or Buffer. The result is a new Buffer.</para>
    /// <para>Stack: <c>[..., bytes, count] -> [..., buffer]</c>.</para>
    /// <para>Faults: count cannot be interpreted as an integer, is negative, greater than the input size, or bytes has an unsupported type.</para>
    /// </remarks>
    RIGHT = 0x8E,

    #endregion

    #region Bitwise logic

    /// <summary>
    /// Inverts every bit of an integer.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible. The operation uses signed arbitrary-precision bitwise complement semantics.</para>
    /// <para>Stack: <c>[..., x] -> [..., ~x]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    INVERT = 0x90,

    /// <summary>
    /// Computes the bitwise AND of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible and are treated as signed arbitrary-precision integers.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., bitwise-and(x1, x2)]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    AND = 0x91,

    /// <summary>
    /// Computes the bitwise OR of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible and are treated as signed arbitrary-precision integers.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., bitwise-or(x1, x2)]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    OR = 0x92,

    /// <summary>
    /// Computes the bitwise XOR of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible and are treated as signed arbitrary-precision integers.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., bitwise-xor(x1, x2)]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    XOR = 0x93,

    /// <summary>
    /// Compares two values using VM equality rules.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: No numeric or textual coercion is performed.
    /// Null equals only Null. Boolean, Integer, and ByteString compare by value only with the same type.
    /// Pointer compares script identity and position.
    /// Array, Buffer, and Map compare by identity.
    /// Struct compares recursively by content.
    /// InteropInterface uses equality defined by the wrapped host value.
    /// </para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., result]</c>.</para>
    /// <para>Faults: A comparison exceeds the configured comparison limit.</para>
    /// </remarks>
    EQUAL = 0x97,

    /// <summary>
    /// Compares two values using VM equality rules and negates the result.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The equality rules are the same as for EQUAL. No cross-type coercion is performed.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., not-equal]</c>.</para>
    /// <para>Faults: A comparison exceeds the configured comparison limit.</para>
    /// </remarks>
    NOTEQUAL = 0x98,

    #endregion

    #region Arithmetic

    /// <summary>
    /// Returns the sign of an integer.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible. The result is -1, 0, or 1 according to the sign of x.</para>
    /// <para>Stack: <c>[..., x] -> [..., sign]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer.</para>
    /// </remarks>
    SIGN = 0x99,

    /// <summary>
    /// Returns the absolute value of an integer.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible.</para>
    /// <para>Stack: <c>[..., x] -> [..., absolute value]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    ABS = 0x9A,

    /// <summary>
    /// Returns the additive inverse of an integer.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible.</para>
    /// <para>Stack: <c>[..., x] -> [..., -x]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    NEGATE = 0x9B,

    /// <summary>
    /// Adds one to an integer.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible.</para>
    /// <para>Stack: <c>[..., x] -> [..., x + 1]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    INC = 0x9C,

    /// <summary>
    /// Subtracts one from an integer.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible.</para>
    /// <para>Stack: <c>[..., x] -> [..., x - 1]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    DEC = 0x9D,

    /// <summary>
    /// Computes the sum of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible. x2 is the top item.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., result]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    ADD = 0x9E,

    /// <summary>
    /// Computes the difference of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible. x2 is the top item.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., result]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    SUB = 0x9F,

    /// <summary>
    /// Computes the product of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible. x2 is the top item.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., result]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    MUL = 0xA0,

    /// <summary>
    /// Divides two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible. The quotient is truncated toward zero.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., quotient]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, the divisor is zero, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    DIV = 0xA1,

    /// <summary>
    /// Computes the remainder of integer division.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible. The remainder has the sign of the dividend x1.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., remainder]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, the divisor is zero, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    MOD = 0xA2,

    /// <summary>
    /// Raises an integer to a non-negative integer power.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., value, exponent] -> [..., power]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, the exponent is negative or exceeds the configured exponent limit,
    /// or the result exceeds the VM integer-size limit.
    /// </para>
    /// </remarks>
    POW = 0xA3,

    /// <summary>
    /// Computes the integer square root.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible. The result is the greatest integer whose square is not greater than x.</para>
    /// <para>Stack: <c>[..., x] -> [..., floor(sqrt(x))]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer, or x is negative.</para>
    /// </remarks>
    SQRT = 0xA4,

    /// <summary>
    /// Computes multiplication followed by modular reduction.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: All items are integer-convertible. The result is (x1 * x2) reduced modulo modulus with signed integer remainder semantics.</para>
    /// <para>Stack: <c>[..., x1, x2, modulus] -> [..., result]</c>.</para>
    /// <para>Faults: Any item cannot be interpreted as an integer, modulus is zero, or the result exceeds the VM integer-size limit.</para>
    /// </remarks>
    MODMUL = 0xA5,

    /// <summary>
    /// Computes modular exponentiation or a modular inverse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: All items are integer-convertible. A normal non-negative exponent computes modular exponentiation.
    /// An exponent of -1 computes the modular inverse of value.
    /// </para>
    /// <para>Stack: <c>[..., value, exponent, modulus] -> [..., result]</c>.</para>
    /// <para>
    /// Faults: Any item cannot be interpreted as an integer, the normal exponent is negative,
    /// the modulus is zero for exponentiation, the inverse does not exist, or the result exceeds the VM integer-size limit.
    /// </para>
    /// </remarks>
    MODPOW = 0xA6,

    /// <summary>
    /// Shifts an integer left.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x and shift are integer-convertible.</para>
    /// <para>Stack: <c>[..., x, shift] -> [..., x &lt;&lt; shift]</c>.</para>
    /// <para>
    /// Faults: Either item cannot be interpreted as an integer, shift is negative or exceeds the configured shift limit,
    /// or the result exceeds the VM integer-size limit.
    /// </para>
    /// </remarks>
    SHL = 0xA8,

    /// <summary>
    /// Shifts an integer right.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x and shift are integer-convertible. A right shift preserves the sign of x.</para>
    /// <para>Stack: <c>[..., x, shift] -> [..., x &gt;&gt; shift]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer, or shift is negative or exceeds the configured shift limit.</para>
    /// </remarks>
    SHR = 0xA9,

    /// <summary>
    /// Negates a Boolean value.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is interpreted as a Boolean. This is logical negation, not bitwise inversion.</para>
    /// <para>Stack: <c>[..., x] -> [..., not x]</c>.</para>
    /// <para>Faults: x cannot be interpreted as a Boolean.</para>
    /// </remarks>
    NOT = 0xAA,

    /// <summary>
    /// Computes the logical AND of two values.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are interpreted as Booleans. There is no short-circuit evaluation.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., logical-and]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as a Boolean.</para>
    /// </remarks>
    BOOLAND = 0xAB,

    /// <summary>
    /// Computes the logical OR of two values.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are interpreted as Booleans. There is no short-circuit evaluation.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., logical-or]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as a Boolean.</para>
    /// </remarks>
    BOOLOR = 0xAC,

    /// <summary>
    /// Tests whether an integer is non-zero.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: x is integer-convertible. The result is Boolean.</para>
    /// <para>Stack: <c>[..., x] -> [..., x != 0]</c>.</para>
    /// <para>Faults: x cannot be interpreted as an integer.</para>
    /// </remarks>
    NZ = 0xB1,

    /// <summary>
    /// Tests two integers for equality.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible, so numeric Boolean, Integer, and small ByteString values may compare equal.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., x1 == x2]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer. Null is not integer-convertible.</para>
    /// </remarks>
    NUMEQUAL = 0xB3,

    /// <summary>
    /// Tests two integers for inequality.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., x1 != x2]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer. Null is not integer-convertible.</para>
    /// </remarks>
    NUMNOTEQUAL = 0xB4,

    /// <summary>
    /// Tests whether x1 is less than x2.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: If either item is Null, the result is false. Otherwise both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., less than(x1, x2)]</c>.</para>
    /// <para>Faults: A non-Null item cannot be interpreted as an integer.</para>
    /// </remarks>
    LT = 0xB5,

    /// <summary>
    /// Tests whether x1 is less than or equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: If either item is Null, the result is false. Otherwise both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., less than or equal(x1, x2)]</c>.</para>
    /// <para>Faults: A non-Null item cannot be interpreted as an integer.</para>
    /// </remarks>
    LE = 0xB6,

    /// <summary>
    /// Tests whether x1 is greater than x2.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: If either item is Null, the result is false. Otherwise both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., greater than(x1, x2)]</c>.</para>
    /// <para>Faults: A non-Null item cannot be interpreted as an integer.</para>
    /// </remarks>
    GT = 0xB7,

    /// <summary>
    /// Tests whether x1 is greater than or equal to x2.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: If either item is Null, the result is false. Otherwise both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., greater than or equal(x1, x2)]</c>.</para>
    /// <para>Faults: A non-Null item cannot be interpreted as an integer.</para>
    /// </remarks>
    GE = 0xB8,

    /// <summary>
    /// Returns the smaller of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., minimum]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer.</para>
    /// </remarks>
    MIN = 0xB9,

    /// <summary>
    /// Returns the larger of two integers.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Both items are integer-convertible.</para>
    /// <para>Stack: <c>[..., x1, x2] -> [..., maximum]</c>.</para>
    /// <para>Faults: Either item cannot be interpreted as an integer.</para>
    /// </remarks>
    MAX = 0xBA,

    /// <summary>
    /// Tests whether a value lies in a half-open interval.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: All three items are integer-convertible. The result is true when lower is less than or equal to x and x is less than upper.</para>
    /// <para>Stack: <c>[..., x, lower, upper] -> [..., within]</c>.</para>
    /// <para>Faults: Any item cannot be interpreted as an integer.</para>
    /// </remarks>
    WITHIN = 0xBB,

    #endregion

    #region Compound-type

    /// <summary>
    /// Builds a Map from key-value pairs on the stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: n is integer-convertible. Each key must be a Boolean, Integer, or ByteString;
    /// each value may be any stack item. Pairs are consumed key first, then value, from the top downward.
    /// </para>
    /// <para>Stack: <c>[..., value1, key1, ..., valuen, keyn, n] -> [..., map]</c>.</para>
    /// <para>A repeated key replaces its earlier value while preserving the position of its first insertion.</para>
    /// <para>
    /// Faults: n cannot be interpreted as an integer, is negative, 2*n exceeds the available stack items,
    /// a key has an unsupported type, or a key exceeds the Map key-size limit.
    /// </para>
    /// </remarks>
    PACKMAP = 0xBE,

    /// <summary>
    /// Builds a Struct from stack items.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: n is integer-convertible. Items are removed from the top and become fields in that order, so the top item becomes field 0.</para>
    /// <para>Stack: <c>[..., x1, ..., xn, n] -> [..., struct]</c>.</para>
    /// <para>Faults: n cannot be interpreted as an integer, is negative, or exceeds the available stack items.</para>
    /// </remarks>
    PACKSTRUCT = 0xBF,

    /// <summary>
    /// Builds an Array from stack items.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: n is integer-convertible. Items are removed from the top and become elements in that order, so the top item becomes element 0.</para>
    /// <para>Stack: <c>[..., x1, ..., xn, n] -> [..., array]</c>.</para>
    /// <para>Faults: n cannot be interpreted as an integer, is negative, or exceeds the available stack items.</para>
    /// </remarks>
    PACK = 0xC0,

    /// <summary>
    /// Pushes the contents of an Array, Struct, or Map onto the stack.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The input must be an Array, Struct, or Map. Elements are not converted or deep-copied.</para>
    /// <para>Stack: <c>[..., compound] -> [..., item[count-1], ..., item[0], count]</c>.</para>
    /// <para>
    /// An Array or Struct is pushed in reverse index order, followed by its count.
    /// A Map is traversed in reverse enumeration order, pushing each value followed by its key, and then the pair count.
    /// </para>
    /// <para>Faults: The top item is not an Array, Struct, or Map, or the expanded result exceeds the configured stack limit.</para>
    /// </remarks>
    UNPACK = 0xC1,

    /// <summary>
    /// Creates an empty Array.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: No stack item is read.</para>
    /// <para>Stack: <c>[...] -> [..., array]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    NEWARRAY0 = 0xC2,

    /// <summary>
    /// Creates an Array with a requested number of Null elements.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: length is integer-convertible. Every element initially contains Null.</para>
    /// <para>Stack: <c>[..., length] -> [..., array]</c>.</para>
    /// <para>Faults: length cannot be interpreted as an integer, is negative, or exceeds the configured stack limit.</para>
    /// </remarks>
    NEWARRAY = 0xC3,

    /// <summary>
    /// Creates an Array whose elements have a default value selected by type.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte VM type code.</para>
    /// <para>
    /// Type rules: length is integer-convertible. Boolean elements default to false, Integer elements to 0, and ByteString elements to an empty ByteString.
    /// All other recognized type codes produce Null elements, including Any, Buffer, Array, Struct, Map, Pointer, and InteropInterface.
    /// </para>
    /// <para>Stack: <c>[..., length] -> [..., array]</c>.</para>
    /// <para>Faults: length cannot be interpreted as an integer, is negative, or exceeds the configured stack limit, or the type code is not recognized.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    NEWARRAY_T = 0xC4,

    /// <summary>
    /// Creates an empty Struct.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: No stack item is read.</para>
    /// <para>Stack: <c>[...] -> [..., struct]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    NEWSTRUCT0 = 0xC5,

    /// <summary>
    /// Creates a Struct with a requested number of Null fields.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: length is integer-convertible. Every field initially contains Null.</para>
    /// <para>Stack: <c>[..., length] -> [..., struct]</c>.</para>
    /// <para>Faults: length cannot be interpreted as an integer, is negative, or exceeds the configured stack limit.</para>
    /// </remarks>
    NEWSTRUCT = 0xC6,

    /// <summary>
    /// Creates an empty Map.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: No stack item is read.</para>
    /// <para>Stack: <c>[...] -> [..., map]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    NEWMAP = 0xC8,

    /// <summary>
    /// Returns the byte length or element count of a value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: Boolean has size 1. Integer reports its signed byte representation length.
    /// ByteString and Buffer report byte length.
    /// Array and Struct report element count.
    /// Map reports key-value pair count.
    /// </para>
    /// <para>Stack: <c>[..., value] -> [..., size]</c>.</para>
    /// <para>Faults: The value is Null, Pointer, InteropInterface, or another unsupported type.</para>
    /// </remarks>
    SIZE = 0xCA,

    /// <summary>
    /// Tests whether an index exists or a Map contains a key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: key must be a Boolean, Integer, or ByteString. For Array, ByteString, and Buffer, the key is integer-convertible and denotes an index.
    /// For Map, the key is compared as a primitive key.
    /// </para>
    /// <para>Stack: <c>[..., collection, key] -> [..., result]</c>.</para>
    /// <para>A non-negative index within the accepted index range but beyond the collection size produces false. A missing Map key also produces false.</para>
    /// <para>
    /// Faults: key or collection has an unsupported type, an index cannot be interpreted as an integer or is outside the accepted index range,
    /// or a Map key exceeds the key-size limit.
    /// </para>
    /// </remarks>
    HASKEY = 0xCB,

    /// <summary>
    /// Collects the keys of a Map into a new Array.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The input must be a Map. The result preserves Map enumeration order and contains the original key values.</para>
    /// <para>Stack: <c>[..., map] -> [..., array]</c>.</para>
    /// <para>Faults: The top item is not a Map, or the result exceeds the configured stack limit.</para>
    /// </remarks>
    KEYS = 0xCC,

    /// <summary>
    /// Collects the values of an Array or Map into a new Array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: The input must be an Array, Struct, or Map. Result order matches the input enumeration order.
    /// Non-Struct values are shared; Struct values are copied by value.
    /// </para>
    /// <para>Stack: <c>[..., collection] -> [..., array]</c>.</para>
    /// <para>Faults: The input has an unsupported type, or copying a Struct or creating the result exceeds configured limits.</para>
    /// </remarks>
    VALUES = 0xCD,

    /// <summary>
    /// Reads an indexed element or Map value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: key must be a Boolean, Integer, or ByteString. Array and Struct use an integer index, Map uses the key itself,
    /// and ByteString, Integer, Boolean, and Buffer use an integer byte index.
    /// A byte read produces an Integer from 0 through 255. Array, Struct, and Map values are returned without deep copying.
    /// </para>
    /// <para>Stack: <c>[..., collection, key] -> [..., item]</c>.</para>
    /// <para>
    /// Faults: The collection or key has an unsupported type, a key or index cannot be interpreted as required, or a Map key exceeds the key-size limit.
    /// An index outside the collection or a missing Map key raises a script-catchable exception.
    /// </para>
    /// </remarks>
    PICKITEM = 0xCE,

    /// <summary>
    /// Appends an item to an Array or Struct.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The collection must be an Array or Struct. Any item may be appended. A Struct item is copied by value before insertion.</para>
    /// <para>Stack: <c>[..., array, item] -> [...]</c>.</para>
    /// <para>Faults: The collection has an unsupported type or is read-only, or copying a Struct exceeds configured limits.</para>
    /// </remarks>
    APPEND = 0xCF,

    /// <summary>
    /// Replaces an Array or Struct element, a Map value, or a Buffer byte.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type rules: key must be a Boolean, Integer, or ByteString. Array and Struct use an integer index,
    /// Map uses the key itself, and Buffer uses an integer index. Struct values are copied by value.
    /// A Buffer value must be integer-convertible and between -128 and 255.
    /// </para>
    /// <para>Stack: <c>[..., collection, key, value] -> [...]</c>.</para>
    /// <para>
    /// Faults: The collection or key has an unsupported type, an index cannot be interpreted as required,a Map key is too large,
    /// a value has an invalid type or range, a Struct copy exceeds limits, or the collection is read-only.
    /// An out-of-range Array, Struct, or Buffer index raises a script-catchable exception.
    /// </para>
    /// </remarks>
    SETITEM = 0xD0,

    /// <summary>
    /// Reverses the elements of an Array or Struct, or the bytes of a Buffer, in place.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Only Array, Struct, and Buffer are accepted. No element conversion occurs.</para>
    /// <para>Stack: <c>[..., collection] -> [...]</c>.</para>
    /// <para>Faults: The collection has an unsupported type or is read-only.</para>
    /// </remarks>
    REVERSEITEMS = 0xD1,

    /// <summary>
    /// Removes an Array or Struct element, or a Map entry.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: key must be a Boolean, Integer, or ByteString. Array and Struct use an integer index; Map uses the key itself.</para>
    /// <para>Stack: <c>[..., collection, key] -> [...]</c>.</para>
    /// <para>Removing a key that is absent from a Map leaves the Map unchanged.</para>
    /// <para>
    /// Faults: The collection or key has an unsupported type, an index is invalid or non-integer, a Map key is too large, or the collection is read-only.
    /// </para>
    /// </remarks>
    REMOVE = 0xD2,

    /// <summary>
    /// Removes all contents from an Array, Struct, or Map.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The container itself is retained. Only its elements or entries are removed.</para>
    /// <para>Stack: <c>[..., collection] -> [...]</c>.</para>
    /// <para>Faults: The top item is not an Array, Struct, or Map, or the container is read-only.</para>
    /// </remarks>
    CLEARITEMS = 0xD3,

    /// <summary>
    /// Removes and returns the last element of an Array or Struct.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The input must be an Array or Struct. The returned value is not converted.</para>
    /// <para>Stack: <c>[..., array] -> [..., item]</c>.</para>
    /// <para>Faults: The input has an unsupported type, the container is empty, or it is read-only.</para>
    /// </remarks>
    POPITEM = 0xD4,

    #endregion

    #region Types

    /// <summary>
    /// Tests whether a value is Null.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: Only Null produces true. Empty ByteString, false, and zero are not Null.</para>
    /// <para>Stack: <c>[..., x] -> [..., result]</c>.</para>
    /// <para>Faults: No instruction-specific runtime failure.</para>
    /// </remarks>
    ISNULL = 0xD8,

    /// <summary>
    /// Tests a value for an exact VM type.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte VM type code.</para>
    /// <para>
    /// Type rules: The check compares the value's type code exactly and does not convert it.
    /// For example, Array is not Struct, and Integer is not Boolean.
    /// </para>
    /// <para>Stack: <c>[..., x] -> [..., result]</c>.</para>
    /// <para>Faults: The type code is Any or is not a recognized VM type.</para>
    /// </remarks>
    [OperandSize(Size = 1)]
    ISTYPE = 0xD9,

    /// <summary>
    /// Converts a value to a requested VM type.
    /// </summary>
    /// <remarks>
    /// <para>Operand: A 1-byte VM type code.</para>
    /// <para>
    /// Type rules: Conversion to the same type preserves the value.
    /// Boolean converts to Integer 0 or 1, to a one-byte ByteString, or to a Buffer.
    /// Integer converts to Boolean, ByteString, or Buffer.
    /// ByteString converts to Buffer at any size; it converts to Boolean or Integer only when it is at most 32 bytes.
    /// Buffer converts to Boolean or ByteString at any size, and to Integer only when it is at most 32 bytes.
    /// Array and Struct convert to each other by shallow conversion.
    /// Null remains Null for any recognized non-Any target.
    /// Array, Struct, Map, Buffer, Pointer, and InteropInterface convert to Boolean true.
    /// </para>
    /// <para>Stack: <c>[..., x] -> [..., converted]</c>.</para>
    /// <para>ByteString and Buffer conversions copy the bytes. Array and Struct conversions copy only the outer container; contained values are shared.</para>
    /// <para>
    /// Faults: The target type is Any or unrecognized, the conversion is not supported,
    /// a ByteString is too long for the requested Boolean or Integer conversion,
    /// a Buffer is too long for Integer, or a related configured limit is exceeded.
    /// </para>
    /// </remarks>
    [OperandSize(Size = 1)]
    CONVERT = 0xDB,

    #endregion

    #region Extensions

    /// <summary>
    /// Terminates execution with a supplied reason.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: The message bytes are decoded as strict UTF-8. Null is accepted as an empty reason because execution terminates immediately.</para>
    /// <para>Stack: <c>[..., message] -> [...]</c>.</para>
    /// <para>
    /// Faults: The message cannot be represented as a valid UTF-8 byte sequence. Otherwise the instruction always faults,
    /// and that fault is not catchable by a try/catch/finally region.
    /// </para>
    /// </remarks>
    ABORTMSG = 0xE0,

    /// <summary>
    /// Continues only when a condition is true and otherwise faults with a reason.
    /// </summary>
    /// <remarks>
    /// <para>Type rules: message must be a value with a byte representation and is decoded as strict UTF-8. condition is interpreted as a Boolean.</para>
    /// <para>Stack: <c>[..., condition, message] -> [...]</c>.</para>
    /// <para>
    /// Faults: The message is invalid UTF-8, condition cannot be interpreted as a Boolean, condition is false,
    /// or message is Null even when the condition is true. A false assertion is not catchable by a try/catch/finally region.
    /// </para>
    /// </remarks>
    ASSERTMSG = 0xE1

    #endregion
}
