namespace Neo.VM
{
    public interface IMemoryItem
    {
        /// <summary>
        /// Is invoked when the item will be added to the memory
        /// </summary>
        /// <param name="memory">Memory</param>
        void OnAddMemory(ReservedMemory memory);

        /// <summary>
        /// Is invoked when the item will be removed from the memory
        /// </summary>
        /// <param name="memory">Memory</param>
        void OnRemoveFromMemory(ReservedMemory memory);
    }
}
