using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    [DebuggerDisplay("Reserved={Reserved}, Used={Used}, Free={Free}")]
    public class ReservedMemory
    {
        [DebuggerDisplay("Item={Item}, Count={Count}")]
        class Entry
        {
            public readonly IMemoryItem Item;
            public int Count;

            public Entry(IMemoryItem item)
            {
                Item = item;
                Count = 1;
            }
        }

        class ReferenceEqualsComparer : IEqualityComparer<Entry>
        {
            public bool Equals(Entry x, Entry y) => ReferenceEquals(x.Item, y.Item);
            public int GetHashCode(Entry obj) => obj.Item.GetMemoryHashCode();
        }

        /// <summary>
        /// Entries
        /// </summary>
        private readonly HashSet<Entry> _entries = new HashSet<Entry>(new ReferenceEqualsComparer());

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
            var entry = new Entry(item);

            if (_entries.TryGetValue(entry, out var actual))
            {
                actual.Count++;
            }
            else
            {
                _entries.Add(entry);
                item.OnAddMemory(this);
            }
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="item">Item</param>
        public void Remove(IMemoryItem item)
        {
            var entry = new Entry(item);

            if (_entries.TryGetValue(entry, out var actual))
            {
                actual.Count--;

                if (actual.Count <= 0)
                {
                    _entries.Remove(actual);
                    item.OnRemoveFromMemory(this);
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
