namespace Neo.VM
{
    public enum OpCode : byte
    {
        // Constants

        [OperandSize(Size = 1)]
        PUSHINT8 = 0x00,
        [OperandSize(Size = 2)]
        PUSHINT16 = 0x01,
        [OperandSize(Size = 4)]
        PUSHINT32 = 0x02,
        [OperandSize(Size = 8)]
        PUSHINT64 = 0x03,
        [OperandSize(Size = 16)]
        PUSHINT128 = 0x04,
        [OperandSize(Size = 32)]
        PUSHINT256 = 0x05,
        /// <summary>
        /// Convert the next four bytes to an address, and push the address onto the stack.
        /// </summary>
        [OperandSize(Size = 4)]
        PUSHA = 0x0A,
        /// <summary>
        /// The item null is pushed onto the stack.
        /// </summary>
        PUSHNULL = 0x0B,
        /// <summary>
        /// The next byte contains the number of bytes to be pushed onto the stack.
        /// </summary>
        [OperandSize(SizePrefix = 1)]
        PUSHDATA1 = 0x0C,
        /// <summary>
        /// The next two bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        [OperandSize(SizePrefix = 2)]
        PUSHDATA2 = 0x0D,
        /// <summary>
        /// The next four bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        [OperandSize(SizePrefix = 4)]
        PUSHDATA4 = 0x0E,
        /// <summary>
        /// The number -1 is pushed onto the stack.
        /// </summary>
        PUSHM1 = 0x0F,
        /// <summary>
        /// The number 0 is pushed onto the stack.
        /// </summary>
        PUSH0 = 0x10,
        /// <summary>
        /// The number 1 is pushed onto the stack.
        /// </summary>
        PUSH1 = 0x11,
        /// <summary>
        /// The number 2 is pushed onto the stack.
        /// </summary>
        PUSH2 = 0x12,
        /// <summary>
        /// The number 3 is pushed onto the stack.
        /// </summary>
        PUSH3 = 0x13,
        /// <summary>
        /// The number 4 is pushed onto the stack.
        /// </summary>
        PUSH4 = 0x14,
        /// <summary>
        /// The number 5 is pushed onto the stack.
        /// </summary>
        PUSH5 = 0x15,
        /// <summary>
        /// The number 6 is pushed onto the stack.
        /// </summary>
        PUSH6 = 0x16,
        /// <summary>
        /// The number 7 is pushed onto the stack.
        /// </summary>
        PUSH7 = 0x17,
        /// <summary>
        /// The number 8 is pushed onto the stack.
        /// </summary>
        PUSH8 = 0x18,
        /// <summary>
        /// The number 9 is pushed onto the stack.
        /// </summary>
        PUSH9 = 0x19,
        /// <summary>
        /// The number 10 is pushed onto the stack.
        /// </summary>
        PUSH10 = 0x1A,
        /// <summary>
        /// The number 11 is pushed onto the stack.
        /// </summary>
        PUSH11 = 0x1B,
        /// <summary>
        /// The number 12 is pushed onto the stack.
        /// </summary>
        PUSH12 = 0x1C,
        /// <summary>
        /// The number 13 is pushed onto the stack.
        /// </summary>
        PUSH13 = 0x1D,
        /// <summary>
        /// The number 14 is pushed onto the stack.
        /// </summary>
        PUSH14 = 0x1E,
        /// <summary>
        /// The number 15 is pushed onto the stack.
        /// </summary>
        PUSH15 = 0x1F,
        /// <summary>
        /// The number 16 is pushed onto the stack.
        /// </summary>
        PUSH16 = 0x20,

        // Flow control

        /// <summary>
        /// Pop the address of a function from the stack, and call the function.
        /// </summary>
        CALLA = 0x3A,
        /// <summary>
        ///  Does nothing.
        /// </summary>
        NOP = 0x61,
        /// <summary>
        /// Reads a 2-byte value n and a jump is performed to relative position n (counting from opcode JMP address).
        /// </summary>
        [OperandSize(Size = 2)]
        JMP = 0x62,
        /// <summary>
        /// A boolean value b is taken from main stack and reads a 2-byte value n, if b is True then a jump is performed to relative position n (counting from opcode JMPIF address).
        /// </summary>
        [OperandSize(Size = 2)]
        JMPIF = 0x63,
        /// <summary>
        /// A boolean value b is taken from main stack and reads a 2-byte value n, if b is False then a jump is performed to relative position n (counting from opcode JMPIFNOT address).
        /// </summary>
        [OperandSize(Size = 2)]
        JMPIFNOT = 0x64,
        /// <summary>
        /// Current context is copied to the invocation stack. Reads a 2-byte value n and a jump is performed to relative position n.
        /// </summary>
        [OperandSize(Size = 2)]
        CALL = 0x65,
        /// <summary>
        /// Stops the execution if invocation stack is empty.
        /// </summary>
        RET = 0x66,
        /// <summary>
        /// Reads a string and executes the corresponding operation.
        /// </summary>
        [OperandSize(Size = 4)]
        SYSCALL = 0x68,


        // Stack
        /// <summary>
        /// Puts the input onto the top of the alt stack. Removes it from the main stack.
        /// </summary>
        TOALTSTACK = 0x6B,
        /// <summary>
        /// Puts the input onto the top of the main stack. Removes it from the alt stack.
        /// </summary>
        FROMALTSTACK = 0x6C,
        /// <summary>
        /// Duplicates the item on top of alt stack and put it on top of main stack.
        /// </summary>
        DUPFROMALTSTACK = 0x6D,
        /// <summary>
        /// Copies the bottom of alt stack and put it on top of main stack.
        /// </summary> 
        DUPFROMALTSTACKBOTTOM = 0x6E,
        /// <summary>
        /// Returns true if the input is null. Returns false otherwise.
        /// </summary>
        ISNULL = 0x70,
        /// <summary>
        /// The item n back in the main stack is removed.
        /// </summary>
        XDROP = 0x71,
        /// <summary>
        /// The item n back in the main stack in swapped with top stack item.
        /// </summary>
        XSWAP = 0x72,
        /// <summary>
        /// The item on top of the main stack is copied and inserted to the position n in the main stack.
        /// </summary>
        XTUCK = 0x73,
        /// <summary>
        /// Puts the number of stack items onto the stack.
        /// </summary>
        DEPTH = 0x74,
        /// <summary>
        /// Removes the top stack item.
        /// </summary>
        DROP = 0x75,
        /// <summary>
        /// Duplicates the top stack item.
        /// </summary>
        DUP = 0x76,
        /// <summary>
        /// Removes the second-to-top stack item.
        /// </summary>
        NIP = 0x77,
        /// <summary>
        /// Copies the second-to-top stack item to the top.
        /// </summary>
        OVER = 0x78,
        /// <summary>
        /// The item n back in the stack is copied to the top.
        /// </summary>
        PICK = 0x79,
        /// <summary>
        /// The item n back in the stack is moved to the top.
        /// </summary>
        ROLL = 0x7A,
        /// <summary>
        /// The top three items on the stack are rotated to the left.
        /// </summary>
        ROT = 0x7B,
        /// <summary>
        /// The top two items on the stack are swapped.
        /// </summary>
        SWAP = 0x7C,
        /// <summary>
        /// The item at the top of the stack is copied and inserted before the second-to-top item.
        /// </summary>
        TUCK = 0x7D,


        // Splice
        /// <summary>
        /// Concatenates two strings.
        /// </summary>
        CAT = 0x7E,
        /// <summary>
        /// Returns a section of a string.
        /// </summary>
        SUBSTR = 0x7F,
        /// <summary>
        /// Keeps only characters left of the specified point in a string.
        /// </summary>
        LEFT = 0x80,
        /// <summary>
        /// Keeps only characters right of the specified point in a string.
        /// </summary>
        RIGHT = 0x81,
        /// <summary>
        /// Returns the length of the input string.
        /// </summary>
        SIZE = 0x82,


        // Bitwise logic
        /// <summary>
        /// Flips all of the bits in the input.
        /// </summary>
        INVERT = 0x83,
        /// <summary>
        /// Boolean and between each bit in the inputs.
        /// </summary>
        AND = 0x84,
        /// <summary>
        /// Boolean or between each bit in the inputs.
        /// </summary>
        OR = 0x85,
        /// <summary>
        /// Boolean exclusive or between each bit in the inputs.
        /// </summary>
        XOR = 0x86,
        /// <summary>
        /// Returns 1 if the inputs are exactly equal, 0 otherwise.
        /// </summary>
        EQUAL = 0x87,


        // Arithmetic
        /// <summary>
        /// 1 is added to the input.
        /// </summary>
        INC = 0x8B,
        /// <summary>
        /// 1 is subtracted from the input.
        /// </summary>
        DEC = 0x8C,
        /// <summary>
        /// Puts the sign of top stack item on top of the main stack. If value is negative, put -1; if positive, put 1; if value is zero, put 0.
        /// </summary>
        SIGN = 0x8D,
        /// <summary>
        /// The sign of the input is flipped.
        /// </summary>
        NEGATE = 0x8F,
        /// <summary>
        /// The input is made positive.
        /// </summary>
        ABS = 0x90,
        /// <summary>
        /// If the input is 0 or 1, it is flipped. Otherwise the output will be 0.
        /// </summary>
        NOT = 0x91,
        /// <summary>
        /// Returns 0 if the input is 0. 1 otherwise.
        /// </summary>
        NZ = 0x92,
        /// <summary>
        /// a is added to b.
        /// </summary>
        ADD = 0x93,
        /// <summary>
        /// b is subtracted from a.
        /// </summary>
        SUB = 0x94,
        /// <summary>
        /// a is multiplied by b.
        /// </summary>
        MUL = 0x95,
        /// <summary>
        /// a is divided by b.
        /// </summary>
        DIV = 0x96,
        /// <summary>
        /// Returns the remainder after dividing a by b.
        /// </summary>
        MOD = 0x97,
        /// <summary>
        /// Shifts a left b bits, preserving sign.
        /// </summary>
        SHL = 0x98,
        /// <summary>
        /// Shifts a right b bits, preserving sign.
        /// </summary>
        SHR = 0x99,
        /// <summary>
        /// If both a and b are not 0, the output is 1. Otherwise 0.
        /// </summary>
        BOOLAND = 0x9A,
        /// <summary>
        /// If a or b is not 0, the output is 1. Otherwise 0.
        /// </summary>
        BOOLOR = 0x9B,
        /// <summary>
        /// Returns 1 if the numbers are equal, 0 otherwise.
        /// </summary>
        NUMEQUAL = 0x9C,
        /// <summary>
        /// Returns 1 if the numbers are not equal, 0 otherwise.
        /// </summary>
        NUMNOTEQUAL = 0x9E,
        /// <summary>
        /// Returns 1 if a is less than b, 0 otherwise.
        /// </summary>
        LT = 0x9F,
        /// <summary>
        /// Returns 1 if a is greater than b, 0 otherwise.
        /// </summary>
        GT = 0xA0,
        /// <summary>
        /// Returns 1 if a is less than or equal to b, 0 otherwise.
        /// </summary>
        LTE = 0xA1,
        /// <summary>
        /// Returns 1 if a is greater than or equal to b, 0 otherwise.
        /// </summary>
        GTE = 0xA2,
        /// <summary>
        /// Returns the smaller of a and b.
        /// </summary>
        MIN = 0xA3,
        /// <summary>
        /// Returns the larger of a and b.
        /// </summary>
        MAX = 0xA4,
        /// <summary>
        /// Returns 1 if x is within the specified range (left-inclusive), 0 otherwise.
        /// </summary>
        WITHIN = 0xA5,

        //Reserved = 0xAC,
        //Reserved = 0xAE,

        // Array
        /// <summary>
        /// An array is removed from top of the main stack. Its size is put on top of the main stack.
        /// </summary>
        ARRAYSIZE = 0xC0,
        /// <summary>
        /// A value n is taken from top of main stack. The next n items on main stack are removed, put inside n-sized array and this array is put on top of the main stack.
        /// </summary>
        PACK = 0xC1,
        /// <summary>
        /// An array is removed from top of the main stack. Its elements are put on top of the main stack (in reverse order) and the array size is also put on main stack.
        /// </summary>
        UNPACK = 0xC2,
        /// <summary>
        /// An input index n (or key) and an array (or map) are taken from main stack. Element array[n] (or map[n]) is put on top of the main stack.
        /// </summary>
        PICKITEM = 0xC3,
        /// <summary>
        /// A value v, index n (or key) and an array (or map) are taken from main stack. Attribution array[n]=v (or map[n]=v) is performed.
        /// </summary>
        SETITEM = 0xC4,
        /// <summary>
        /// A value n is taken from top of main stack. A zero-filled array type with size n is put on top of the main stack.
        /// OR a struct is taken from top of main stack and is converted to an array.
        /// </summary>
        NEWARRAY = 0xC5,
        /// <summary>
        /// A value n is taken from top of main stack. A zero-filled struct type with size n is put on top of the main stack.
        /// OR an array is taken from top of main stack and is converted to a struct.
        /// </summary>
        NEWSTRUCT = 0xC6,
        /// <summary>
        /// A Map is created and put on top of the main stack.
        /// </summary>
        NEWMAP = 0xC7,
        /// <summary>
        /// The item on top of main stack is removed and appended to the second item on top of the main stack.
        /// </summary>
        APPEND = 0xC8,
        /// <summary>
        /// An array is removed from the top of the main stack and its elements are reversed.
        /// </summary>
        REVERSE = 0xC9,
        /// <summary>
        /// An input index n (or key) and an array (or map) are removed from the top of the main stack. Element array[n] (or map[n]) is removed.
        /// </summary>
        REMOVE = 0xCA,
        /// <summary>
        /// An input index n (or key) and an array (or map) are removed from the top of the main stack. Puts True on top of main stack if array[n] (or map[n]) exist, and False otherwise.
        /// </summary>
        HASKEY = 0xCB,
        /// <summary>
        /// A map is taken from top of the main stack. The keys of this map are put on top of the main stack.
        /// </summary>
        KEYS = 0xCC,
        /// <summary>
        /// A map is taken from top of the main stack. The values of this map are put on top of the main stack.
        /// </summary>
        VALUES = 0xCD,


        // Exceptions
        /// <summary>
        /// Halts the execution of the vm by setting VMState.FAULT.
        /// </summary>
        THROW = 0xF0,
        /// <summary>
        /// Removes top stack item n, and halts the execution of the vm by setting VMState.FAULT only if n is False.
        /// </summary>
        THROWIFNOT = 0xF1
    }
}
