namespace Neo.VM
{
    public enum VMState : byte
    {
        HALT = 1 << 0,
        FAULT = 1 << 1,
        BREAK = 1 << 2,
    }
}
