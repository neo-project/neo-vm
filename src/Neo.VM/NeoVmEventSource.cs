// Copyright (C) 2016-2022 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.Tracing;
using Neo.VM.Types;

namespace Neo.VM
{
    [EventSource(Name = "Neo.VM")]
    class NeoVmEventSource : EventSource
    {
        public static NeoVmEventSource Log = new NeoVmEventSource();

        public static class Keywords
        {
            public const EventKeywords ExecutionEngine = (EventKeywords)0x0001;
            public const EventKeywords ReferenceCounter = (EventKeywords)0x0002;
        }

        public static class Tasks
        {
            public const EventTask InstructionExecution = (EventTask)1;
            public const EventTask CheckZeroReferred = (EventTask)2;
        }

        [Event(1, Keywords = Keywords.ExecutionEngine, Opcode = EventOpcode.Start, Task = Tasks.InstructionExecution, Level = EventLevel.Informational)]
        public void InstructionExecutionStart(OpCode opCode) { WriteEvent(1, (int)opCode); }

        [Event(2, Keywords = Keywords.ExecutionEngine, Opcode = EventOpcode.Stop, Task = Tasks.InstructionExecution, Level = EventLevel.Informational)]
        public void InstructionExecutionStop() { WriteEvent(2); }


        [Event(101, Keywords = Keywords.ReferenceCounter, Level = EventLevel.Informational)]
        public void AddReference(StackItemType itemType, StackItemType parentType, int referencesCount, int itemRefCount)
        { WriteEvent(101, (int)itemType, (int)parentType, referencesCount, itemRefCount); }

        [Event(102, Keywords = Keywords.ReferenceCounter, Level = EventLevel.Informational)]
        public void AddStackReference(StackItemType itemType, int count, int referencesCount, int itemStackRefCount, int zeroReferredCount)
        { WriteEvent(102, (int)itemType, count, referencesCount, itemStackRefCount, zeroReferredCount); }

        [Event(103, Keywords = Keywords.ReferenceCounter, Level = EventLevel.Informational)]
        public void AddZeroReferred(StackItemType itemType, int zeroReferredCount)
        { WriteEvent(103, (int)itemType, zeroReferredCount); }

        [Event(104, Keywords = Keywords.ReferenceCounter, Level = EventLevel.Informational)]
        public void RemoveReference(StackItemType itemType, StackItemType parentType, int referencesCount, int itemStackRefCount, int itemRefCount, int zeroReferredCount)
        { WriteEvent(104, (int)itemType, (int)parentType, referencesCount, itemStackRefCount, itemRefCount, zeroReferredCount); }

        [Event(105, Keywords = Keywords.ReferenceCounter, Level = EventLevel.Informational)]
        public void RemoveStackReference(StackItemType itemType, int referencesCount, int itemStackRefCount, int zeroReferredCount)
        { WriteEvent(105, (int)itemType, referencesCount, itemStackRefCount, zeroReferredCount); }

        [Event(106, Keywords = Keywords.ReferenceCounter, Opcode = EventOpcode.Start, Task = Tasks.CheckZeroReferred, Level = EventLevel.Informational)]
        public void CheckZeroReferredStart(int zeroReferredCount) { WriteEvent(106, zeroReferredCount); }

        [Event(107, Keywords = Keywords.ReferenceCounter, Opcode = EventOpcode.Stop, Task = Tasks.CheckZeroReferred, Level = EventLevel.Informational)]
        public void CheckZeroReferredStop(int count) { WriteEvent(107, count); }


        // custom WriteEvent overloads to avoid array allocation as per 
        // https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-instrumentation#optimizing-performance-for-high-volume-events

        [NonEvent]
        public unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3, int arg4)
        {
            EventData* data = stackalloc EventSource.EventData[4];

            data[0].DataPointer = (IntPtr)(&arg1);
            data[0].Size = 4;
            data[1].DataPointer = (IntPtr)(&arg2);
            data[1].Size = 4;
            data[2].DataPointer = (IntPtr)(&arg3);
            data[2].Size = 4;
            data[3].DataPointer = (IntPtr)(&arg4);
            data[3].Size = 4;

            WriteEventCore(eventId, 4, data);
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3, int arg4, int arg5)
        {
            EventData* data = stackalloc EventSource.EventData[5];

            data[0].DataPointer = (IntPtr)(&arg1);
            data[0].Size = 4;
            data[1].DataPointer = (IntPtr)(&arg2);
            data[1].Size = 4;
            data[2].DataPointer = (IntPtr)(&arg3);
            data[2].Size = 4;
            data[3].DataPointer = (IntPtr)(&arg4);
            data[3].Size = 4;
            data[4].DataPointer = (IntPtr)(&arg5);
            data[4].Size = 4;

            WriteEventCore(eventId, 5, data);
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6)
        {
            EventData* data = stackalloc EventSource.EventData[6];

            data[0].DataPointer = (IntPtr)(&arg1);
            data[0].Size = 4;
            data[1].DataPointer = (IntPtr)(&arg2);
            data[1].Size = 4;
            data[2].DataPointer = (IntPtr)(&arg3);
            data[2].Size = 4;
            data[3].DataPointer = (IntPtr)(&arg4);
            data[3].Size = 4;
            data[4].DataPointer = (IntPtr)(&arg5);
            data[4].Size = 4;
            data[5].DataPointer = (IntPtr)(&arg6);
            data[5].Size = 4;

            WriteEventCore(eventId, 6, data);
        }
    }
}
