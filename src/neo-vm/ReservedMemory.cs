using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Reserved={Reserved}, Used={Used}, Free={Free}")]
    public class ReservedMemory
    {
        class ReferenceEqualsComparer : IEqualityComparer<IMemoryItem>
        {
            public bool Equals(IMemoryItem x, IMemoryItem y) => ReferenceEquals(x, y);
            public int GetHashCode(IMemoryItem obj) => obj.GetMemoryHashCode();
        }

        /// <summary>
        /// Entries
        /// </summary>
        private readonly IDictionary<IMemoryItem, int> _entries = new Dictionary<IMemoryItem, int>(new ReferenceEqualsComparer());

        /// <summary>
        /// Free memory
        /// </summary>
        public int Free => Reserved - Used;

        /// <summary>
        /// Used
        /// </summary>
        public int Used { get; private set; }

        /// <summary>
        /// Reserved
        /// </summary>
        public int Reserved { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reserved">Reserved</param>
        public ReservedMemory(int reserved = 1024)
        {
            Reserved = reserved;
        }

        /// <summary>
        /// Allocate memory
        /// </summary>
        /// <param name="memory">Memory</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AllocateMemory(int memory = 1)
        {
            Used = checked(Used + memory);

            if (Used > Reserved)
            {
                throw new OutOfMemoryException();
            }
        }

        /// <summary>
        /// Free memory
        /// </summary>
        /// <param name="memory">Memory</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FreeMemory(int memory = 1)
        {
            Used = checked(Used - memory);
        }

        /// <summary>
        /// Add item
        /// </summary>
        /// <param name="item">Item</param>
        public void Add(IMemoryItem item)
        {
            if (_entries.TryGetValue(item, out var actual))
            {
                _entries[item] = actual + 1;
            }
            else
            {
                _entries.Add(item, 1);
                item.OnAddMemory(this);
            }
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="item">Item</param>
        public void Remove(IMemoryItem item)
        {
            if (_entries.TryGetValue(item, out var actual))
            {
                if (actual <= 1)
                {
                    _entries.Remove(item);
                    item.OnRemoveFromMemory(this);
                }
                else
                {
                    _entries[item] = actual - 1;
                }
            }
            else
            {
                // This never should happend
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Add range
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="items">Items</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange<T>(IEnumerable<T> items) where T : IMemoryItem
        {
            foreach (var item in items) Add(item);
        }

        /// <summary>
        /// Remove Range
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="items">Items</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveRange<T>(IEnumerable<T> items) where T : IMemoryItem
        {
            foreach (var item in items) Remove(item);
        }
    }
}
