namespace Neo.VM
{
    public enum OpCode : byte
    {
        // Constants
        PUSH0 = 0x00, // An empty array of bytes is pushed onto the stack.
        PUSHF = PUSH0,
        PUSHBYTES1 = 0x01, // 0x01-0x4B The next opcode bytes is data to be pushed onto the stack
        PUSHBYTES75 = 0x4B,
        PUSHDATA1 = 0x4C, // The next byte contains the number of bytes to be pushed onto the stack.
        PUSHDATA2 = 0x4D, // The next two bytes contain the number of bytes to be pushed onto the stack.
        PUSHDATA4 = 0x4E, // The next four bytes contain the number of bytes to be pushed onto the stack.
        PUSHM1 = 0x4F, // The number -1 is pushed onto the stack.
        PUSH1 = 0x51, // The number 1 is pushed onto the stack.
        PUSHT = PUSH1,
        PUSH2 = 0x52, // The number 2 is pushed onto the stack.
        PUSH3 = 0x53, // The number 3 is pushed onto the stack.
        PUSH4 = 0x54, // The number 4 is pushed onto the stack.
        PUSH5 = 0x55, // The number 5 is pushed onto the stack.
        PUSH6 = 0x56, // The number 6 is pushed onto the stack.
        PUSH7 = 0x57, // The number 7 is pushed onto the stack.
        PUSH8 = 0x58, // The number 8 is pushed onto the stack.
        PUSH9 = 0x59, // The number 9 is pushed onto the stack.
        PUSH10 = 0x5A, // The number 10 is pushed onto the stack.
        PUSH11 = 0x5B, // The number 11 is pushed onto the stack.
        PUSH12 = 0x5C, // The number 12 is pushed onto the stack.
        PUSH13 = 0x5D, // The number 13 is pushed onto the stack.
        PUSH14 = 0x5E, // The number 14 is pushed onto the stack.
        PUSH15 = 0x5F, // The number 15 is pushed onto the stack.
        PUSH16 = 0x60, // The number 16 is pushed onto the stack.


        // Flow control
        NOP = 0x61, // Does nothing.
        JMP = 0x62, // Reads a 2-byte value n and a jump is performed to relative position n-3.
        JMPIF = 0x63, // A boolean value b is taken from main stack and reads a 2-byte value n, if b is True then a jump is performed to relative position n-3.
        JMPIFNOT = 0x64, // A boolean value b is taken from main stack and reads a 2-byte value n, if b is False then a jump is performed to relative position n-3.
        CALL = 0x65, // Current context is copied to the invocation stack. Reads a 2-byte value n and a jump is performed to relative position n-3.
        RET = 0x66, // Stops the execution if invocation stack is empty.
        APPCALL = 0x67, // Reads a scripthash and executes the corresponding contract.
        SYSCALL = 0x68, // Reads a string and executes the corresponding operation.
        TAILCALL = 0x69, // Reads a scripthash and executes the corresponding contract. Disposes the top item on invocation stack.


        // Stack
        DUPFROMALTSTACK = 0x6A, // Duplicates the item on top of alt stack and put it on top of main stack.
        TOALTSTACK = 0x6B, // Puts the input onto the top of the alt stack. Removes it from the main stack.
        FROMALTSTACK = 0x6C, // Puts the input onto the top of the main stack. Removes it from the alt stack.
        XDROP = 0x6D, // The item n back in the main stack is removed.
        XSWAP = 0x72, // The item n back in the main stack in swapped with top stack item.
        XTUCK = 0x73, // The item on top of the main stack is copied and inserted to the position n in the main stack.
        DEPTH = 0x74, // Puts the number of stack items onto the stack.
        DROP = 0x75, // Removes the top stack item.
        DUP = 0x76, // Duplicates the top stack item.
        NIP = 0x77, // Removes the second-to-top stack item.
        OVER = 0x78, // Copies the second-to-top stack item to the top.
        PICK = 0x79, // The item n back in the stack is copied to the top.
        ROLL = 0x7A, // The item n back in the stack is moved to the top.
        ROT = 0x7B, // The top three items on the stack are rotated to the left.
        SWAP = 0x7C, // The top two items on the stack are swapped.
        TUCK = 0x7D, // The item at the top of the stack is copied and inserted before the second-to-top item.


        // Splice
        CAT = 0x7E, // Concatenates two strings.
        SUBSTR = 0x7F, // Returns a section of a string.
        LEFT = 0x80, // Keeps only characters left of the specified point in a string.
        RIGHT = 0x81, // Keeps only characters right of the specified point in a string.
        SIZE = 0x82, // Returns the length of the input string.


        // Bitwise logic
        INVERT = 0x83, // Flips all of the bits in the input.
        AND = 0x84, // Boolean and between each bit in the inputs.
        OR = 0x85, // Boolean or between each bit in the inputs.
        XOR = 0x86, // Boolean exclusive or between each bit in the inputs.
        EQUAL = 0x87, // Returns 1 if the inputs are exactly equal, 0 otherwise.
        //OP_EQUALVERIFY = 0x88, // Same as OP_EQUAL, but runs OP_VERIFY afterward.
        //OP_RESERVED1 = 0x89, // Transaction is invalid unless occuring in an unexecuted OP_IF branch
        //OP_RESERVED2 = 0x8A, // Transaction is invalid unless occuring in an unexecuted OP_IF branch


        // Arithmetic
        // Note: Arithmetic inputs are limited to signed 32-bit integers, but may overflow their output.
        INC = 0x8B, // 1 is added to the input.
        DEC = 0x8C, // 1 is subtracted from the input.
        SIGN = 0x8D, // Puts the sign of top stack item on top of the main stack. If value is negative, put -1; if positive, put 1; if value is zero, put 0.
        NEGATE = 0x8F, // The sign of the input is flipped.
        ABS = 0x90, // The input is made positive.
        NOT = 0x91, // If the input is 0 or 1, it is flipped. Otherwise the output will be 0.
        NZ = 0x92, // Returns 0 if the input is 0. 1 otherwise.
        ADD = 0x93, // a is added to b.
        SUB = 0x94, // b is subtracted from a.
        MUL = 0x95, // a is multiplied by b.
        DIV = 0x96, // a is divided by b.
        MOD = 0x97, // Returns the remainder after dividing a by b.
        SHL = 0x98, // Shifts a left b bits, preserving sign.
        SHR = 0x99, // Shifts a right b bits, preserving sign.
        BOOLAND = 0x9A, // If both a and b are not 0, the output is 1. Otherwise 0.
        BOOLOR = 0x9B, // If a or b is not 0, the output is 1. Otherwise 0.
        NUMEQUAL = 0x9C, // Returns 1 if the numbers are equal, 0 otherwise.
        NUMNOTEQUAL = 0x9E, // Returns 1 if the numbers are not equal, 0 otherwise.
        LT = 0x9F, // Returns 1 if a is less than b, 0 otherwise.
        GT = 0xA0, // Returns 1 if a is greater than b, 0 otherwise.
        LTE = 0xA1, // Returns 1 if a is less than or equal to b, 0 otherwise.
        GTE = 0xA2, // Returns 1 if a is greater than or equal to b, 0 otherwise.
        MIN = 0xA3, // Returns the smaller of a and b.
        MAX = 0xA4, // Returns the larger of a and b.
        WITHIN = 0xA5, // Returns 1 if x is within the specified range (left-inclusive), 0 otherwise.


        // Crypto
        //RIPEMD160 = 0xA6, // The input is hashed using RIPEMD-160.
        SHA1 = 0xA7, // The input is hashed using SHA-1.
        SHA256 = 0xA8, // The input is hashed using SHA-256.
        HASH160 = 0xA9, // The input is hashed using Hash160: first with SHA-256 and then with RIPEMD-160.
        HASH256 = 0xAA, // The input is hashed using Hash256: twice with SHA-256.
        CHECKSIG = 0xAC, // The publickey and signature are taken from main stack. Verifies if transaction was signed by given publickey and a boolean output is put on top of the main stack.
        VERIFY = 0xAD,  // The publickey, signature and message are taken from main stack. Verifies if given message was signed by given publickey and a boolean output is put on top of the main stack.
        CHECKMULTISIG = 0xAE, // A set of n public keys (an array or value n followed by n pubkeys) is validated against a set of m signatures (an array or value m followed by m signatures). Verify transaction as multisig and a boolean output is put on top of the main stack.


        // Array
        ARRAYSIZE = 0xC0, // An array is removed from top of the main stack. Its size is put on top of the main stack.
        PACK = 0xC1, // A value n is taken from top of main stack. The next n items on main stack are removed, put inside n-sized array and this array is put on top of the main stack.
        UNPACK = 0xC2, // An array is removed from top of the main stack. Its elements are put on top of the main stack (in reverse order) and the array size is also put on main stack.
        PICKITEM = 0xC3, // An input index n (or key) and an array (or map) are taken from main stack. Element array[n] (or map[n]) is put on top of the main stack.
        SETITEM = 0xC4, // A value v, index n (or key) and an array (or map) are taken from main stack. Attribution array[n]=v (or map[n]=v) is performed.
        NEWARRAY = 0xC5, //用作引用類型  en: A value n is taken from top of main stack. A zero-filled array type with size n is put on top of the main stack.
        NEWSTRUCT = 0xC6, //用作值類型 en: A value n is taken from top of main stack. A zero-filled struct type with size n is put on top of the main stack.
        NEWMAP = 0xC7, // A Map is created and put on top of the main stack.
        APPEND = 0xC8, // The item on top of main stack is removed and appended to the second item on top of the main stack.
        REVERSE = 0xC9, // An array is removed from the top of the main stack and its elements are reversed.
        REMOVE = 0xCA, // An input index n (or key) and an array (or map) are removed from the top of the main stack. Element array[n] (or map[n]) is removed.
        HASKEY = 0xCB, // An input index n (or key) and an array (or map) are removed from the top of the main stack. Puts True on top of main stack if array[n] (or map[n]) exist, and False otherwise.
        KEYS = 0xCC, // A map is taken from top of the main stack. The keys of this map are put on top of the main stack.
        VALUES = 0xCD, // A map is taken from top of the main stack. The values of this map are put on top of the main stack.

        // Exceptions
        THROW = 0xF0, // Halts the execution of the vm by setting VMState.FAULT.
        THROWIFNOT = 0xF1  // Removes top stack item n, and halts the execution of the vm by setting VMState.FAULT only if n is False.
    }
}
