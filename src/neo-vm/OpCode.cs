namespace Neo.VM
{
    public enum OpCode : byte
    {
        // Constants
        /// <summary>
        /// An empty array of bytes is pushed onto the stack.
        /// </summary>
        PUSH0 = 0x00,
        PUSHF = PUSH0,
        /// <summary>
        /// 0x01-0x4B The next opcode bytes is data to be pushed onto the stack
        /// </summary>
        PUSHBYTES1 = 0x01,
        PUSHBYTES75 = 0x4B,
        /// <summary>
        /// The next byte contains the number of bytes to be pushed onto the stack.
        /// </summary>
        PUSHDATA1 = 0x4C,
        /// <summary>
        /// The next two bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        PUSHDATA2 = 0x4D,
        /// <summary>
        /// The next four bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        PUSHDATA4 = 0x4E,
        /// <summary>
        /// The number -1 is pushed onto the stack.
        /// </summary>
        PUSHM1 = 0x4F,
        /// <summary>
        /// The number 1 is pushed onto the stack.
        /// </summary>
        PUSH1 = 0x51,
        PUSHT = PUSH1,
        /// <summary>
        /// The number 2 is pushed onto the stack.
        /// </summary>
        PUSH2 = 0x52,
        /// <summary>
        /// The number 3 is pushed onto the stack.
        /// </summary>
        PUSH3 = 0x53,
        /// <summary>
        /// The number 4 is pushed onto the stack.
        /// </summary>
        PUSH4 = 0x54,
        /// <summary>
        /// The number 5 is pushed onto the stack.
        /// </summary>
        PUSH5 = 0x55,
        /// <summary>
        /// The number 6 is pushed onto the stack.
        /// </summary>
        PUSH6 = 0x56,
        /// <summary>
        /// The number 7 is pushed onto the stack.
        /// </summary>
        PUSH7 = 0x57,
        /// <summary>
        /// The number 8 is pushed onto the stack.
        /// </summary>
        PUSH8 = 0x58,
        /// <summary>
        /// The number 9 is pushed onto the stack.
        /// </summary>
        PUSH9 = 0x59,
        /// <summary>
        /// The number 10 is pushed onto the stack.
        /// </summary>
        PUSH10 = 0x5A,
        /// <summary>
        /// The number 11 is pushed onto the stack.
        /// </summary>
        PUSH11 = 0x5B,
        /// <summary>
        /// The number 12 is pushed onto the stack.
        /// </summary>
        PUSH12 = 0x5C,
        /// <summary>
        /// The number 13 is pushed onto the stack.
        /// </summary>
        PUSH13 = 0x5D,
        /// <summary>
        /// The number 14 is pushed onto the stack.
        /// </summary>
        PUSH14 = 0x5E,
        /// <summary>
        /// The number 15 is pushed onto the stack.
        /// </summary>
        PUSH15 = 0x5F,
        /// <summary>
        /// The number 16 is pushed onto the stack.
        /// </summary>
        PUSH16 = 0x60,

        // Flow control
        /// <summary>
        ///  Does nothing.
        /// </summary>
        NOP = 0x61,
        /// <summary>
        /// Reads a 2-byte value n and a jump is performed to relative position n-3.
        /// </summary>
        JMP = 0x62,
        /// <summary>
        /// A boolean value b is taken from main stack and reads a 2-byte value n, if b is True then a jump is performed to relative position n-3.
        /// </summary>
        JMPIF = 0x63,
        /// <summary>
        /// A boolean value b is taken from main stack and reads a 2-byte value n, if b is False then a jump is performed to relative position n-3.
        /// </summary>
        JMPIFNOT = 0x64,
        /// <summary>
        /// Current context is copied to the invocation stack. Reads a 2-byte value n and a jump is performed to relative position n-3.
        /// </summary>
        CALL = 0x65,
        /// <summary>
        /// Stops the execution if invocation stack is empty.
        /// </summary>
        RET = 0x66,
        /// <summary>
        /// Reads a scripthash and executes the corresponding contract.
        /// </summary>
        APPCALL = 0x67,
        /// <summary>
        /// Reads a string and executes the corresponding operation.
        /// </summary>
        SYSCALL = 0x68,
        /// <summary>
        /// Reads a scripthash and executes the corresponding contract. Disposes the top item on invocation stack.
        /// </summary>
        TAILCALL = 0x69,


        // Stack
        /// <summary>
        /// Duplicates the item on top of alt stack and put it on top of main stack.
        /// </summary>
        DUPFROMALTSTACK = 0x6A,
        /// <summary>
        /// Puts the input onto the top of the alt stack. Removes it from the main stack.
        /// </summary>
        TOALTSTACK = 0x6B,
        /// <summary>
        /// Puts the input onto the top of the main stack. Removes it from the alt stack.
        /// </summary>
        FROMALTSTACK = 0x6C,
        /// <summary>
        /// The item n back in the main stack is removed.
        /// </summary>
        XDROP = 0x6D,
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
        //OP_EQUALVERIFY = 0x88, // Same as OP_EQUAL, but runs OP_VERIFY afterward.
        //OP_RESERVED1 = 0x89, // Transaction is invalid unless occuring in an unexecuted OP_IF branch
        //OP_RESERVED2 = 0x8A, // Transaction is invalid unless occuring in an unexecuted OP_IF branch


        // Arithmetic
        // Note: Arithmetic inputs are limited to signed 32-bit integers, but may overflow their output.
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


        // Crypto
        //RIPEMD160 = 0xA6, // The input is hashed using RIPEMD-160.
        /// <summary>
        /// The input is hashed using SHA-1.
        /// </summary>
        SHA1 = 0xA7,
        /// <summary>
        /// The input is hashed using SHA-256.
        /// </summary>
        SHA256 = 0xA8,
        /// <summary>
        /// The input is hashed using Hash160: first with SHA-256 and then with RIPEMD-160.
        /// </summary>
        HASH160 = 0xA9,
        /// <summary>
        /// The input is hashed using Hash256: twice with SHA-256.
        /// </summary>
        HASH256 = 0xAA,
        /// <summary>
        /// The publickey and signature are taken from main stack. Verifies if transaction was signed by given publickey and a boolean output is put on top of the main stack.
        /// </summary>
        CHECKSIG = 0xAC,
        /// <summary>
        /// The publickey, signature and message are taken from main stack. Verifies if given message was signed by given publickey and a boolean output is put on top of the main stack.
        /// </summary>
        VERIFY = 0xAD,
        /// <summary>
        /// A set of n public keys (an array or value n followed by n pubkeys) is validated against a set of m signatures (an array or value m followed by m signatures). Verify transaction as multisig and a boolean output is put on top of the main stack.
        /// </summary>
        CHECKMULTISIG = 0xAE,


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
        ///用作引用類型  en: A value n is taken from top of main stack. A zero-filled array type with size n is put on top of the main stack.
        /// </summary>
        NEWARRAY = 0xC5,
        /// <summary>
        ///用作值類型 en: A value n is taken from top of main stack. A zero-filled struct type with size n is put on top of the main stack.
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


        // Stack isolation
        CALL_I = 0xE0,
        CALL_E = 0xE1,
        CALL_ED = 0xE2,
        CALL_ET = 0xE3,
        CALL_EDT = 0xE4,


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
