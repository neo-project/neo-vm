namespace Neo.VM.Types
{
    public enum StackItemType : byte
    {
        Any = 0x00,
        Pointer = 0x10,
        Boolean = 0x20,
        Integer = 0x21,
        ByteString = 0x28,
        Buffer = 0x30,
        Array = 0x40,
        Struct = 0x41,
        Map = 0x48,
        InteropInterface = 0x60,
    }
}
