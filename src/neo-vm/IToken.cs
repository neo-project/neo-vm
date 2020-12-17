namespace Neo.VM
{
    public interface IToken
    {
        ushort ParametersCount { get; }
        ushort ReturnValuesCount { get; }
    }
}
