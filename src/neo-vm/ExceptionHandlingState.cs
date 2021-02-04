namespace Neo.VM
{
    /// <summary>
    /// Indicates the state of the <see cref="ExceptionHandlingContext"/>.
    /// </summary>
    public enum ExceptionHandlingState : byte
    {
        /// <summary>
        /// Indicates that the <see langword="try"/> block is being executed.
        /// </summary>
        Try,

        /// <summary>
        /// Indicates that the <see langword="catch"/> block is being executed.
        /// </summary>
        Catch,

        /// <summary>
        /// Indicates that the <see langword="finally"/> block is being executed.
        /// </summary>
        Finally
    }
}
