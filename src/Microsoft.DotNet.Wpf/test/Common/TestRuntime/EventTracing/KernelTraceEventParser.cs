// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using Microsoft.Test.EventTracing.FastSerialization;
using System.Runtime.InteropServices;
using System.Security;

/* This file was generated with the command */
// traceParserGen /needsState /merge /renameFile KernelTraceEventParser.renames /mof KernelTraceEventParser.mof KernelTraceEventParser.cs
/* And then modified by hand to add functionality (handle to name lookup, fixup of events ...) */
// The version before any hand modifications is kept as KernelTraceEventParser.base.cs, and a 3
// way diff is done when traceParserGen is rerun.  This allows the 'by-hand' modifications to be
// applied again if the mof or the traceParserGen transformation changes. 
// 
// See traceParserGen /usersGuide for more on the /merge option 
namespace Microsoft.Test.EventTracing
{
    /// <summary>
    /// The code:KernelTraceEventParser is a class that knows how to decode the 'standard' kernel events.
    /// It exposes an event for each event of interest that users can subscribe to.
    /// 
    /// see code:TraceEventParser for more 
    /// </summary>
    [SecurityTreatAsSafe, SecurityCritical]
    [CLSCompliant(false)]
    public sealed class KernelTraceEventParser : TraceEventParser
    {
        /// <summary>
        /// The special name for the Kernel session
        /// </summary>
        public static string KernelSessionName { get { return "NT Kernel Logger"; } }

        public static string ProviderName = "Windows Kernel";
        public static Guid ProviderGuid = new Guid(unchecked((int)0x9e814aad), unchecked((short)0x3204), unchecked((short)0x11d2), 0x9a, 0x82, 0x00, 0x60, 0x08, 0xa8, 0x69, 0x39);
        /// <summary>
        /// This is passed to code:TraceEventSession.EnableKernelProvider to enable particular sets of
        /// events. 
        /// </summary>
        [Flags]
        public enum Keywords
        {
            // These are available on XP and above 
            /// <summary>
            /// Logs nothing
            /// </summary>
            None = 0x00000000, // no tracing
            /// <summary>
            /// Logs process starts and stops.
            /// </summary>
            Process = 0x00000001,
            /// <summary>
            /// Logs threads starts and stops
            /// </summary>
            Thread = 0x00000002,
            /// <summary>
            /// Logs native modules loads (LoadLibrary), and unloads
            /// </summary>
            ImageLoad = 0x00000004, // image load
            /* For 0x8 - 0x080 See code:#vistaOnly below */

            /// <summary>
            /// Loads the completion of Physical disk activity. 
            /// </summary>
            DiskIO = 0x00000100, // physical disk IO
            /// <summary>
            /// Logs the mapping of file IDs to actual (kernel) file names. 
            /// </summary>
            DiskFileIO = 0x00000200, // requires disk IO
            /// <summary>
            /// Logs all page faults (hard or soft)
            /// </summary>
            MemoryPageFaults = 0x00001000,
            /// <summary>
            /// Logs all page faults that must fetch the data from the disk (hard faults)
            /// </summary>
            MemoryHardFaults = 0x00002000,
            /// <summary>
            /// Logs TCP/IP network send and recieve events. 
            /// </summary>
            NetworkTCPIP = 0x00010000,
            /// <summary>
            /// Logs activity to the windows registry. 
            /// </summary>
            Registry = 0x00020000, // registry calls

            // #vistaOnly These are only available on Vista an above
            /// <summary>
            /// Logs process performance counters (TODO When?)
            /// see code:KernelTraceEventParser.ProcessPerfCtr, code:ProcessPerfCtrTraceData
            /// </summary>
            ProcessCounters = 0x00000008,
            /// <summary>
            /// log thread context switches 
            /// </summary>
            ContextSwitch = 0x00000010,
            /// <summary>
            /// log defered procedure calls (an Kernel mechanism for having work done asynchronously)
            /// </summary>
            DeferedProcedureCalls = 0x00000020,
            /// <summary>
            /// log hardware interrupts. 
            /// </summary>
            Interrupt = 0x00000040,
            /// <summary>
            /// log calls to the OS
            /// </summary>
            SystemCall = 0x00000080,
            /// <summary>
            /// log Disk operations
            /// </summary>
            DiskIOInit = 0x00000400,
            /// <summary>
            /// Disk I/O that was split (eg because of mirroring requirements)
            /// </summary>
            SplitIO = 0x00200000,
            Driver = 0x00800000,
            /// <summary>
            /// Sampled based profiling (every msec)
            /// </summary>
            Profile = 0x01000000,
            /// <summary>
            /// log file operations (even ones that do not actually cause Disk I/O).  
            /// </summary>
            FileIO = 0x02000000,
            /// <summary>
            /// log the start of the File I/O operation as well as the end. 
            /// </summary>
            FileIOInit = 0x04000000,

            // Thes only work on Vista using the code:TraceEventNativeMethods.StartKernelTrace method
            /// <summary>
            /// Thread Dispatcher (ReadyThread)
            /// </summary>
            Dispatcher = 0x00000800,
            /// <summary>
            /// Log Virutal Alloc calls and VirtualFree.  
            /// </summary>
            VirtualAlloc = 0x004000,

            /// <summary>
            /// Good default kernel flags.  (TODO more detail)
            /// </summary>  
            Default = Process | Thread | ImageLoad | DiskIO | DiskFileIO | NetworkTCPIP | MemoryHardFaults | ProcessCounters | Profile,
            All = Default | DeferedProcedureCalls | Interrupt | SystemCall | DiskIOInit | SplitIO | Driver | FileIO | FileIOInit | ContextSwitch,
        };
        
        public KernelTraceEventParser(TraceEventSource source)
            : base(source)
        {
            if (source == null)         // Happens during deserialization.  
                return;

            KernelTraceEventParserState state = State;
            if (state.threadIDtoProcessID.Count == 0 && !state.callBacksSet)
            {
                state.callBacksSet = true;
                // logic to initialize state
                AddToAllMatching(delegate(RegistryTraceData data)
                {
                    if (RegistryTraceData.NameIsKeyName(data.Opcode))
                        State.fileIDToName.Add(data.KeyHandle, data.TimeStamp100ns, data.KeyName);
                });
                AddToAllMatching(delegate(FileIoNameTraceData data)
                {
                    Debug.Assert(data.FileName.Length != 0);
                    State.fileIDToName.Add(data.FileObject, data.TimeStamp100ns, data.FileName);
                });
                ThreadStartGroup += delegate(ThreadTraceData data)
                {
                    Debug.Assert(data.ThreadID >= 0);
                    Debug.Assert(data.ProcessID >= 0);
                    State.threadIDtoProcessID.Add((Address)data.ThreadID, data.TimeStamp100ns, data.ProcessID);
                };
            }
        }

        public event Action<EventTraceHeaderTraceData> EventTraceHeader
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EventTraceHeaderTraceData(value, 0xFFFF, 0, "EventTrace", EventTraceTaskGuid, 0, "Header", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeaderExtensionTraceData> EventTraceExtension
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeaderExtensionTraceData(value, 0xFFFF, 0, "EventTrace", EventTraceTaskGuid, 5, "Extension", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeaderExtensionTraceData> EventTraceEndExtension
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeaderExtensionTraceData(value, 0xFFFF, 0, "EventTrace", EventTraceTaskGuid, 32, "EndExtension", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> EventTraceRundownComplete
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 0, "EventTrace", EventTraceTaskGuid, 8, "RundownComplete", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessTraceData> ProcessStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 1, "Start", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Registers both ProcessStart and ProcessDCStart
        /// </summary>
        public event Action<ProcessTraceData> ProcessStartGroup
        {
            add
            {
                ProcessStart += value;
                ProcessDCStart += value;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessTraceData> ProcessEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 2, "End", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Registers both ProcessEnd and ProcessDCEnd
        /// </summary>
        public event Action<ProcessTraceData> ProcessEndGroup
        {
            add
            {
                ProcessEnd += value;
                ProcessDCEnd += value;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessTraceData> ProcessDCStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 3, "DCStart", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessTraceData> ProcessDCEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 4, "DCEnd", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessTraceData> ProcessDefunct
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 39, "Defunct", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessCtrTraceData> ProcessPerfCtr
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessCtrTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 32, "PerfCtr", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ProcessCtrTraceData> ProcessPerfCtrRundown
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ProcessCtrTraceData(value, 0xFFFF, 1, "Process", ProcessTaskGuid, 33, "PerfCtrRundown", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadTraceData> ThreadStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 1, "Start", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Registers both ThreadStart and ThreadDCStart
        /// </summary>
        public event Action<ThreadTraceData> ThreadStartGroup
        {
            add
            {
                ThreadStart += value;
                ThreadDCStart += value;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        public event Action<ThreadTraceData> ThreadEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 2, "End", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Registers both ThreadEnd and ThreadDCEnd
        /// </summary>
        public event Action<ThreadTraceData> ThreadEndGroup
        {
            add
            {
                ThreadEnd += value;
                ThreadDCEnd += value;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadTraceData> ThreadDCStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 3, "DCStart", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ThreadTraceData> ThreadDCEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 4, "DCEnd", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<CSwitchTraceData> ThreadCSwitch
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new CSwitchTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 36, "CSwitch", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> ThreadCompCS
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 37, "CompCS", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<WorkerThreadTraceData> ThreadWorkerThread
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new WorkerThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 57, "WorkerThread", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveCreateTraceData> ThreadReserveCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveCreateTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 48, "ReserveCreate", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveDeleteTraceData> ThreadReserveDelete
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveDeleteTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 49, "ReserveDelete", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveJoinThreadTraceData> ThreadReserveJoinThread
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveJoinThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 52, "ReserveJoinThread", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveDisjoinThreadTraceData> ThreadReserveDisjoinThread
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveDisjoinThreadTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 53, "ReserveDisjoinThread", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveStateTraceData> ThreadReserveState
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveStateTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 54, "ReserveState", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveBandwidthTraceData> ThreadReserveBandwidth
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveBandwidthTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 55, "ReserveBandwidth", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReserveLateCountTraceData> ThreadReserveLateCount
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ReserveLateCountTraceData(value, 0xFFFF, 2, "Thread", ThreadTaskGuid, 56, "ReserveLateCount", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DiskIoTraceData> DiskIoRead
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DiskIoTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 10, "Read", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DiskIoTraceData> DiskIoWrite
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DiskIoTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 11, "Write", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DiskIoInitTraceData> DiskIoReadInit
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DiskIoInitTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 12, "ReadInit", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DiskIoInitTraceData> DiskIoWriteInit
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DiskIoInitTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 13, "WriteInit", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DiskIoInitTraceData> DiskIoFlushInit
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DiskIoInitTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 15, "FlushInit", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DiskIoFlushBuffersTraceData> DiskIoFlushBuffers
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DiskIoFlushBuffersTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 14, "FlushBuffers", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DriverMajorFunctionCallTraceData> DiskIoDriverMajorFunctionCall
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DriverMajorFunctionCallTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 34, "DriverMajorFunctionCall", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DriverMajorFunctionReturnTraceData> DiskIoDriverMajorFunctionReturn
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DriverMajorFunctionReturnTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 35, "DriverMajorFunctionReturn", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DriverCompletionRoutineTraceData> DiskIoDriverCompletionRoutine
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DriverCompletionRoutineTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 37, "DriverCompletionRoutine", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DriverCompleteRequestTraceData> DiskIoDriverCompleteRequest
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DriverCompleteRequestTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 52, "DriverCompleteRequest", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DriverCompleteRequestReturnTraceData> DiskIoDriverCompleteRequestReturn
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DriverCompleteRequestReturnTraceData(value, 0xFFFF, 3, "DiskIo", DiskIoTaskGuid, 53, "DriverCompleteRequestReturn", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 10, "Create", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryOpen
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 11, "Open", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryDelete
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 12, "Delete", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryQuery
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 13, "Query", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistrySetValue
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 14, "SetValue", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryDeleteValue
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 15, "DeleteValue", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryQueryValue
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 16, "QueryValue", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryEnumerateKey
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 17, "EnumerateKey", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryEnumerateValueKey
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 18, "EnumerateValueKey", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryQueryMultipleValue
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 19, "QueryMultipleValue", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistrySetInformation
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 20, "SetInformation", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryFlush
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 21, "Flush", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryRunDown
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 22, "RunDown", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryKCBCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 22, "KCBCreate", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryKCBDelete
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 23, "KCBDelete", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryKCBRundownBegin
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 24, "KCBRundownBegin", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryKCBRundownEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 25, "KCBRundownEnd", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryVirtualize
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 26, "Virtualize", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<RegistryTraceData> RegistryClose
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new RegistryTraceData(value, 0xFFFF, 4, "Registry", RegistryTaskGuid, 27, "Close", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SplitIoInfoTraceData> SplitIoVolMgr
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SplitIoInfoTraceData(value, 0xFFFF, 5, "SplitIo", SplitIoTaskGuid, 32, "VolMgr", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoNameTraceData> FileIoName
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoNameTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 0, "Name", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoNameTraceData> FileIoFileCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoNameTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 32, "FileCreate", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoNameTraceData> FileIoFileDelete
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoNameTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 35, "FileDelete", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoNameTraceData> FileIoFileRundown
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoNameTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 36, "FileRundown", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoCreateTraceData> FileIoCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoCreateTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 64, "Create", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoSimpleOpTraceData> FileIoCleanup
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoSimpleOpTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 65, "Cleanup", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoSimpleOpTraceData> FileIoClose
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoSimpleOpTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 66, "Close", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoSimpleOpTraceData> FileIoFlush
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoSimpleOpTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 73, "Flush", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoReadWriteTraceData> FileIoRead
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoReadWriteTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 67, "Read", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoReadWriteTraceData> FileIoWrite
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoReadWriteTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 68, "Write", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoInfoTraceData> FileIoSetInfo
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoInfoTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 69, "SetInfo", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoInfoTraceData> FileIoDelete
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoInfoTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 70, "Delete", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoInfoTraceData> FileIoRename
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoInfoTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 71, "Rename", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoInfoTraceData> FileIoQueryInfo
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoInfoTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 74, "QueryInfo", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoInfoTraceData> FileIoFSControl
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoInfoTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 75, "FSControl", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoDirEnumTraceData> FileIoDirEnum
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoDirEnumTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 72, "DirEnum", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoDirEnumTraceData> FileIoDirNotify
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoDirEnumTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 77, "DirNotify", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<FileIoOpEndTraceData> FileIoOperationEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new FileIoOpEndTraceData(value, 0xFFFF, 6, "FileIo", FileIoTaskGuid, 76, "OperationEnd", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpSendTraceData> TcpIpSend
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpSendTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 10, "Send", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpRecv
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 11, "Recv", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpConnectTraceData> TcpIpConnect
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpConnectTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 12, "Connect", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpDisconnect
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 13, "Disconnect", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpRetransmit
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 14, "Retransmit", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpConnectTraceData> TcpIpAccept
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpConnectTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 15, "Accept", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpReconnect
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 16, "Reconnect", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpFailTraceData> TcpIpFail
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpFailTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 17, "Fail", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpTCPCopy
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 18, "TCPCopy", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpARPCopy
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 19, "ARPCopy", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpFullACK
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 20, "FullACK", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpPartACK
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 21, "PartACK", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpTraceData> TcpIpDupACK
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 22, "DupACK", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6SendTraceData> TcpIpSendIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6SendTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 26, "SendIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6TraceData> TcpIpRecvIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6TraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 27, "RecvIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6TraceData> TcpIpDisconnectIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6TraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 29, "DisconnectIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6TraceData> TcpIpRetransmitIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6TraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 30, "RetransmitIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6TraceData> TcpIpReconnectIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6TraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 32, "ReconnectIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6TraceData> TcpIpTCPCopyIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6TraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 34, "TCPCopyIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6ConnectTraceData> TcpIpConnectIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6ConnectTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 28, "ConnectIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TcpIpV6ConnectTraceData> TcpIpAcceptIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TcpIpV6ConnectTraceData(value, 0xFFFF, 7, "TcpIp", TcpIpTaskGuid, 31, "AcceptIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<UdpIpTraceData> UdpIpSend
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new UdpIpTraceData(value, 0xFFFF, 8, "UdpIp", UdpIpTaskGuid, 10, "Send", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<UdpIpTraceData> UdpIpRecv
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new UdpIpTraceData(value, 0xFFFF, 8, "UdpIp", UdpIpTaskGuid, 11, "Recv", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<UdpIpFailTraceData> UdpIpFail
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new UdpIpFailTraceData(value, 0xFFFF, 8, "UdpIp", UdpIpTaskGuid, 17, "Fail", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<UpdIpV6TraceData> UdpIpSendIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new UpdIpV6TraceData(value, 0xFFFF, 8, "UdpIp", UdpIpTaskGuid, 26, "SendIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<UpdIpV6TraceData> UdpIpRecvIPV6
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new UpdIpV6TraceData(value, 0xFFFF, 8, "UdpIp", UdpIpTaskGuid, 27, "RecvIPV6", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ImageLoadTraceData> ImageLoad
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ImageLoadTraceData(value, 0xFFFF, 9, "Image", ImageTaskGuid, 10, "Load", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Registers both ImageLoad and ImageDCStart
        /// </summary>
        public event Action<ImageLoadTraceData> ImageLoadGroup
        {
            add
            {
                ImageLoad += value;
                ImageDCStart += value;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ImageLoadTraceData> ImageUnload
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ImageLoadTraceData(value, 0xFFFF, 9, "Image", ImageTaskGuid, 2, "Unload", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Registers both ImageUnload and ImageDCEnd
        /// </summary>
        public event Action<ImageLoadTraceData> ImageUnloadGroup
        {
            add
            {
                ImageUnload += value;
                ImageDCEnd += value;
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ImageLoadTraceData> ImageDCStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ImageLoadTraceData(value, 0xFFFF, 9, "Image", ImageTaskGuid, 3, "DCStart", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ImageLoadTraceData> ImageDCEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ImageLoadTraceData(value, 0xFFFF, 9, "Image", ImageTaskGuid, 4, "DCEnd", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultTraceData> PageFaultTransitionFault
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 10, "TransitionFault", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultTraceData> PageFaultDemandZeroFault
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 11, "DemandZeroFault", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultTraceData> PageFaultCopyOnWrite
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 12, "CopyOnWrite", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultTraceData> PageFaultGuardPageFault
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 13, "GuardPageFault", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultTraceData> PageFaultHardPageFault
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 14, "HardPageFault", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultTraceData> PageFaultAccessViolation
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 15, "AccessViolation", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultHardFaultTraceData> PageFaultHardFault
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultHardFaultTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 32, "HardFault", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultHeapRangeRundownTraceData> PageFaultHeapRangeRundown
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultHeapRangeRundownTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 100, "HRRundown", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultHeapRangeCreateTraceData> PageFaultHeapRangeCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultHeapRangeCreateTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 101, "HRCreate", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultHeapRangeTraceData> PageFaultHeapRangeReserve
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultHeapRangeTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 102, "HRReserve", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultHeapRangeTraceData> PageFaultHeapRangeRelease
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultHeapRangeTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 103, "HRRelease", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultHeapRangeDestroyTraceData> PageFaultHeapRangeDestroy
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultHeapRangeDestroyTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 104, "HRDestroy", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<PageFaultImageLoadBackedTraceData> PageFaultImageLoadBacked
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new PageFaultImageLoadBackedTraceData(value, 0xFFFF, 10, "PageFault", PageFaultTaskGuid, 105, "ImageLoadBacked", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SampledProfileTraceData> PerfInfoSampleProf
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SampledProfileTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 46, "SampleProf", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<BatchedSampledProfileTraceData> PerfInfoBatchedSampleProf
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new BatchedSampledProfileTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 55, "SampleProfBatched", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SampledProfileIntervalTraceData> PerfInfoSetInterval
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SampledProfileIntervalTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 72, "SetInterval", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SampledProfileIntervalTraceData> PerfInfoCollectionStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SampledProfileIntervalTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 73, "CollectionStart", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SampledProfileIntervalTraceData> PerfInfoCollectionEnd
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SampledProfileIntervalTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 74, "CollectionEnd", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SysCallEnterTraceData> PerfInfoSysClEnter
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SysCallEnterTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 51, "SysClEnter", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SysCallExitTraceData> PerfInfoSysClExit
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SysCallExitTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 52, "SysClExit", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ISRTraceData> PerfInfoISR
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ISRTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 67, "ISR", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DPCTraceData> PerfInfoThreadedDPC
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DPCTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 66, "ThreadedDPC", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DPCTraceData> PerfInfoDPC
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DPCTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 68, "DPC", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<DPCTraceData> PerfInfoTimerDPC
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new DPCTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 69, "TimerDPC", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> PerfInfoDebuggerEnabled
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 11, "PerfInfo", PerfInfoTaskGuid, 58, "DebuggerEnabled", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<StackWalkTraceData> StackWalk
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StackWalkTraceData(value, 0xFFFF, 12, "StackWalk", StackWalkTaskGuid, 32, "Stack", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ALPCSendMessageTraceData> ALPCSendMessage
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ALPCSendMessageTraceData(value, 0xFFFF, 13, "ALPC", ALPCTaskGuid, 33, "ALPCSendMessage", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ALPCReceiveMessageTraceData> ALPCReceiveMessage
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ALPCReceiveMessageTraceData(value, 0xFFFF, 13, "ALPC", ALPCTaskGuid, 34, "ALPCReceiveMessage", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ALPCWaitForReplyTraceData> ALPCWaitForReply
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ALPCWaitForReplyTraceData(value, 0xFFFF, 13, "ALPC", ALPCTaskGuid, 35, "ALPCWaitForReply", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ALPCWaitForNewMessageTraceData> ALPCWaitForNewMessage
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ALPCWaitForNewMessageTraceData(value, 0xFFFF, 13, "ALPC", ALPCTaskGuid, 36, "ALPCWaitForNewMessage", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ALPCUnwaitTraceData> ALPCUnwait
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new ALPCUnwaitTraceData(value, 0xFFFF, 13, "ALPC", ALPCTaskGuid, 37, "ALPCUnwait", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<EmptyTraceData> LostEvent
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new EmptyTraceData(value, 0xFFFF, 14, "Lost_Event", Lost_EventTaskGuid, 32, "LostEvent", ProviderGuid, ProviderName));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigCPUTraceData> SystemConfigCPU
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigCPUTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 10, "CPU", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigPhyDiskTraceData> SystemConfigPhyDisk
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigPhyDiskTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 11, "PhyDisk", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigLogDiskTraceData> SystemConfigLogDisk
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigLogDiskTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 12, "LogDisk", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigNICTraceData> SystemConfigNIC
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigNICTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 13, "NIC", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigVideoTraceData> SystemConfigVideo
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigVideoTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 14, "Video", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigServicesTraceData> SystemConfigServices
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigServicesTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 15, "Services", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigPowerTraceData> SystemConfigPower
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigPowerTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 16, "Power", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigIRQTraceData> SystemConfigIRQ
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigIRQTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 21, "IRQ", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigPnPTraceData> SystemConfigPnP
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigPnPTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 22, "PnP", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigNetworkTraceData> SystemConfigNetwork
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigNetworkTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 17, "Network", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<SystemConfigIDEChannelTraceData> SystemConfigIDEChannel
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new SystemConfigIDEChannelTraceData(value, 0xFFFF, 15, "SystemConfig", SystemConfigTaskGuid, 23, "IDEChannel", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        // Added by hand. 
        public event Action<VirtualAllocTraceData> VirtualAlloc
        {
            add
            {
                source.RegisterEventTemplate(new VirtualAllocTraceData(value, 0xFFFF, 0, "VirtualAlloc", VirtualAllocTaskGuid, 98, "VirtualAlloc", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<VirtualAllocTraceData> VirtualFree
        {
            add
            {
                source.RegisterEventTemplate(new VirtualAllocTraceData(value, 0xFFFF, 0, "VirtualAlloc", VirtualAllocTaskGuid, 99, "VirtualFree", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<ReadyThreadTraceData> ReadyThread
        {
            add
            {
                source.RegisterEventTemplate(new ReadyThreadTraceData(value, 0xFFFF, 0, "Dispatcher", ReadyThreadTaskGuid, 50, "ReadyThread", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        public event Action<StringTraceData> Mark
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new StringTraceData(value, 0xFFFF, 0, "PerfInfo", PerfInfoTaskGuid, 34, "Mark", ProviderGuid, ProviderName, false));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        #region private
        KernelTraceEventParserState State
        {
            get
            {
                KernelTraceEventParserState ret = (KernelTraceEventParserState) StateObject;
                if (ret == null)
                {
                    ret = new KernelTraceEventParserState();
                    StateObject = ret;
                }
                return ret;
            }
        }
        internal static Guid EventTraceTaskGuid = new Guid(unchecked((int)0x68fdd900), unchecked((short)0x4a3e), unchecked((short)0x11d1), 0x84, 0xf4, 0x00, 0x00, 0xf8, 0x04, 0x64, 0xe3);
        internal static Guid ProcessTaskGuid = new Guid(unchecked((int)0x3d6fa8d0), unchecked((short)0xfe05), unchecked((short)0x11d0), 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);
        internal static Guid ThreadTaskGuid = new Guid(unchecked((int)0x3d6fa8d1), unchecked((short)0xfe05), unchecked((short)0x11d0), 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);
        internal static Guid DiskIoTaskGuid = new Guid(unchecked((int)0x3d6fa8d4), unchecked((short)0xfe05), unchecked((short)0x11d0), 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);
        internal static Guid RegistryTaskGuid = new Guid(unchecked((int)0xae53722e), unchecked((short)0xc863), unchecked((short)0x11d2), 0x86, 0x59, 0x00, 0xc0, 0x4f, 0xa3, 0x21, 0xa1);
        internal static Guid SplitIoTaskGuid = new Guid(unchecked((int)0xd837ca92), unchecked((short)0x12b9), unchecked((short)0x44a5), 0xad, 0x6a, 0x3a, 0x65, 0xb3, 0x57, 0x8a, 0xa8);
        internal static Guid FileIoTaskGuid = new Guid(unchecked((int)0x90cbdc39), unchecked((short)0x4a3e), unchecked((short)0x11d1), 0x84, 0xf4, 0x00, 0x00, 0xf8, 0x04, 0x64, 0xe3);
        internal static Guid TcpIpTaskGuid = new Guid(unchecked((int)0x9a280ac0), unchecked((short)0xc8e0), unchecked((short)0x11d1), 0x84, 0xe2, 0x00, 0xc0, 0x4f, 0xb9, 0x98, 0xa2);
        internal static Guid UdpIpTaskGuid = new Guid(unchecked((int)0xbf3a50c5), unchecked((short)0xa9c9), unchecked((short)0x4988), 0xa0, 0x05, 0x2d, 0xf0, 0xb7, 0xc8, 0x0f, 0x80);
        internal static Guid ImageTaskGuid = new Guid(unchecked((int)0x2cb15d1d), unchecked((short)0x5fc1), unchecked((short)0x11d2), 0xab, 0xe1, 0x00, 0xa0, 0xc9, 0x11, 0xf5, 0x18);
        internal static Guid PageFaultTaskGuid = new Guid(unchecked((int)0x3d6fa8d3), unchecked((short)0xfe05), unchecked((short)0x11d0), 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);
        internal static Guid PerfInfoTaskGuid = new Guid(unchecked((int)0xce1dbfb4), unchecked((short)0x137e), unchecked((short)0x4da6), 0x87, 0xb0, 0x3f, 0x59, 0xaa, 0x10, 0x2c, 0xbc);
        internal static Guid StackWalkTaskGuid = new Guid(unchecked((int)0xdef2fe46), unchecked((short)0x7bd6), unchecked((short)0x4b80), 0xbd, 0x94, 0xf5, 0x7f, 0xe2, 0x0d, 0x0c, 0xe3);
        internal static Guid ALPCTaskGuid = new Guid(unchecked((int)0x45d8cccd), unchecked((short)0x539f), unchecked((short)0x4b72), 0xa8, 0xb7, 0x5c, 0x68, 0x31, 0x42, 0x60, 0x9a);
        internal static Guid Lost_EventTaskGuid = new Guid(unchecked((int)0x6a399ae0), unchecked((short)0x4bc6), unchecked((short)0x4de9), 0x87, 0x0b, 0x36, 0x57, 0xf8, 0x94, 0x7e, 0x7e);
        internal static Guid SystemConfigTaskGuid = new Guid(unchecked((int)0x01853a65), unchecked((short)0x418f), unchecked((short)0x4f36), 0xae, 0xfc, 0xdc, 0x0f, 0x1d, 0x2f, 0xd2, 0x35);
        internal static Guid VirtualAllocTaskGuid = new Guid(unchecked((int)0x3d6fa8d3), unchecked((short)0xfe05), unchecked((short)0x11d0), 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);
        internal static Guid ReadyThreadTaskGuid = new Guid(unchecked((int)0x3d6fa8d1), unchecked((short)0xfe05), unchecked((short)0x11d0), 0x9d, 0xda, 0x00, 0xc0, 0x4f, 0xd7, 0xba, 0x7c);
        #endregion

    }
    #region private types
    /// <summary>
    /// code:KernelTraceEventParserState holds all information that is shared among all events that is
    /// needed to decode kernel events.   This class is registered with the source so that it will be
    /// persisted.  Things in here include
    /// 
    ///     * FileID to FileName mapping, 
    ///     * ThreadID to ProcessID mapping
    ///     * Kernel file name to user file name mapping 
    /// </summary>
    [SecurityTreatAsSafe, SecurityCritical]
    internal class KernelTraceEventParserState : IFastSerializable
    {
        public KernelTraceEventParserState()
        {
            fileIDToName = new HistoryDictionary<string>(500);
            threadIDtoProcessID = new HistoryDictionary<int>(50);
        }

        internal string FileIDToName(Address fileHandle, long time100ns)
        {
            string ret;
            if (!fileIDToName.TryGetValue(fileHandle, time100ns, out ret))
                return "";
            return ret;
        }
        internal int ThreadIDToProcessID(int threadID, long time100ns)
        {
            int ret;
            if (!threadIDtoProcessID.TryGetValue((Address)threadID, time100ns, out ret))
                ret = -1;
            return ret;
        }
        internal string KernelToUser(string kernelName)
        {
            if (driveNames == null)
                InitializeKernelNameMap();

            for (int i = 0; i < kernelNameForDrives.Length; i++)
            {
                string kernelPrefix = kernelNameForDrives[i];
                if (kernelName.Length > kernelPrefix.Length && kernelName[kernelPrefix.Length] == '\\' &&
                    string.Compare(kernelName, 0, kernelPrefix, 0, kernelPrefix.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return driveNames[i] + kernelName.Substring(kernelPrefix.Length);
            }
            string kernelSystemRoot = @"\SystemRoot\";
            if (string.Compare(kernelName, 0, kernelSystemRoot, 0, kernelSystemRoot.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return systemRoot + kernelName.Substring(kernelSystemRoot.Length - 1);

            string kernelWindows = @"\Windows\";
            if (string.Compare(kernelName, 0, kernelWindows, 0, kernelWindows.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return windows + kernelName.Substring(kernelWindows.Length - 1);

            string kernelMup = @"\Device\Mup\";
            if (string.Compare(kernelName, 0, kernelMup, 0, kernelMup.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return @"\\" + kernelName.Substring(kernelMup.Length - 1);

            if (kernelName.Length > 1 && kernelName[0] == '\\' && kernelName[1] != '?')
            {
                // TODO this is likely a hack
                string trialName = systemRoot.Substring(0, 2) + kernelName;
                if (trialName.IndexOfAny(System.IO.Path.GetInvalidPathChars()) < 0 && trialName.IndexOf(':', 2) < 0 && System.IO.File.Exists(trialName))
                    return trialName;
                return @"\??" + kernelName;
            }
            return kernelName;
        }

        #region private
        #region KernelToUserFileMapping

        [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
        [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        private static extern int GetLogicalDrives();
 
        private void InitializeKernelNameMap()
        {
#if !NEW
#if !Silverlight
            driveNames = Environment.GetLogicalDrives();
            kernelNameForDrives = new string[driveNames.Length];

            StringBuilder kernelNameBuff = new StringBuilder(2048);
            for (int i = 0; i < driveNames.Length; i++)
            {
                string drive = driveNames[i].Substring(0, 2);
                driveNames[i] = drive;
                string kernelName = "";
                kernelNameBuff.Length = 0;
                if (QueryDosDevice(drive, kernelNameBuff, 2048) != 0)
                    kernelName = kernelNameBuff.ToString();
                kernelNameForDrives[i] = kernelName;
            }
            systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            windows = Environment.GetEnvironmentVariable("windir");
#endif
#else
            List<string> kernelNameForDriveList = new List<string>(8);
            List<string> driveNameList = new List<string>(8);

            int logicalDriveBitVector = GetLogicalDrives();
            int bit = 1;
            StringBuilder kernelNameBuff = new StringBuilder(2048);
            StringBuilder driveNameBuff = new StringBuilder(8);
            for (int bitNum = 0; bitNum < kernelNameForDrives.Length; bitNum++)
            {
                if ((bit & logicalDriveBitVector) != 0)
                {
                    driveNameBuff.Length = 0;
                    driveNameBuff.Append('A' + bitNum).Append(':');
                    string driveName = driveNameBuff.ToString();

                    string kernelName = "";
                    kernelNameBuff.Length = 0;
                    if (QueryDosDevice(driveName, kernelNameBuff, 2048) != 0)
                        kernelName = kernelNameBuff.ToString();

                    driveNameList.Add(driveName);
                    kernelNameForDriveList.Add(kernelName);
                }
                bit >>= 1;
            }
            kernelNameForDrives = kernelNameForDriveList.ToArray();
            driveNames = driveNameList.ToArray();
            systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            windows = Environment.GetEnvironmentVariable("windir");
#endif
        }

        string[] driveNames;
        string[] kernelNameForDrives;
        string systemRoot;
        string windows;

        #endregion

        void IFastSerializable.ToStream(Serializer serializer)
        {
            if (driveNames == null)
                InitializeKernelNameMap();
            // We mark the end of this set of variables so we can add to the end of it, since there is a
            // good chance we will have to change it.  
            ForwardReference endOfKernelNameMap = serializer.GetForwardReference();
            serializer.Write(endOfKernelNameMap);
            serializer.Write(1);        // This is a version number.  
            serializer.Write(driveNames.Length);
            serializer.Log("<WriteColection name=\"driveNames\" count=\"" + driveNames.Length + "\">\r\n");
            for (int i = 0; i < driveNames.Length; i++)
            {
                serializer.Write(driveNames[i]);
                serializer.Write(kernelNameForDrives[i]);
            }
            serializer.Log("</WriteColection>\r\n");
            serializer.Write(windows);
            serializer.Write(systemRoot);
            serializer.DefineForwardReference(endOfKernelNameMap);

            serializer.Write(threadIDtoProcessID.Count);
            serializer.Log("<WriteColection name=\"ProcessIDForThread\" count=\"" + threadIDtoProcessID.Count + "\">\r\n");
            foreach (HistoryDictionary<int>.HistoryValue entry in threadIDtoProcessID.Entries)
            {
                serializer.Write((long)entry.Key);
                serializer.Write(entry.StartTime100ns);
                serializer.Write(entry.Value);
            }
            serializer.Log("</WriteColection>\r\n");

            serializer.Log("<WriteColection name=\"fileIDToName\" count=\"" + fileIDToName.Count + "\">\r\n");
            serializer.Write(fileIDToName.Count);
            foreach (HistoryDictionary<string>.HistoryValue entry in fileIDToName.Entries)
            {
                serializer.Write((long)entry.Key);
                serializer.Write(entry.StartTime100ns);
                serializer.Write(entry.Value);
            }
            serializer.Log("</WriteColection>\r\n");
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            // We mark the end of this set of variables so we can add to the end of it, since there is a
            // good chance we will have to change it.  
            ForwardReference endOfKernelNameMap = deserializer.ReadForwardReference();
            int version; deserializer.Read(out version);        // Not used (yet).  
            int numDrives; deserializer.Read(out numDrives);
            driveNames = new string[numDrives];
            kernelNameForDrives = new string[numDrives];
            for (int i = 0; i < numDrives; i++)
            {
                deserializer.Read(out driveNames[i]);
                deserializer.Read(out kernelNameForDrives[i]);
            }
            deserializer.Read(out windows);
            deserializer.Read(out systemRoot);
            deserializer.Goto(endOfKernelNameMap);      // Skip any fields added in later versions.  

            int count; deserializer.Read(out count);
            Debug.Assert(count >= 0);
            deserializer.Log("<Marker name=\"ProcessIDForThread\"/ count=\"" + count + "\">");
            for (int i = 0; i < count; i++)
            {
                long key; deserializer.Read(out key);
                long startTime100ns; deserializer.Read(out startTime100ns);
                int value; deserializer.Read(out value);
                threadIDtoProcessID.Add((Address)key, startTime100ns, value);
            }

            deserializer.Read(out count);
            Debug.Assert(count >= 0);
            deserializer.Log("<Marker name=\"fileIDToName\"/ count=\"" + count + "\">");
            for (int i = 0; i < count; i++)
            {
                long key; deserializer.Read(out key);
                long startTime100ns; deserializer.Read(out startTime100ns);
                string value; deserializer.Read(out value);
                fileIDToName.Add((Address)key, startTime100ns, value);
            }
        }
        internal HistoryDictionary<string> fileIDToName;
        internal HistoryDictionary<int> threadIDtoProcessID;
        internal bool callBacksSet;
        #endregion
    }
    #endregion

    [SecurityTreatAsSafe, SecurityCritical]
    [CLSCompliant(false)] 
    public sealed class EventTraceHeaderTraceData : TraceEvent
    {
        public int BufferSize { get { return GetInt32At(0); } }
        public new int Version { get { return GetInt32At(4); } }
        public int ProviderVersion { get { return GetInt32At(8); } }
        public int NumberOfProcessors { get { return GetInt32At(12); } }
        public long EndTime100ns { get { return GetInt64At(16); } }
        public DateTime EndTime { get { return DateTime.FromFileTime(EndTime100ns); } }
        public int TimerResolution { get { return GetInt32At(24); } }
        public int MaxFileSize { get { return GetInt32At(28); } }
        public int LogFileMode { get { return GetInt32At(32); } }
        public int BuffersWritten { get { return GetInt32At(36); } }
        public int StartBuffers { get { return GetInt32At(40); } }
        public new int PointerSize { get { return GetInt32At(44); } }
        public int EventsLost { get { return GetInt32At(48); } }
        public int CPUSpeed { get { return GetInt32At(52); } }
        //  Skipping SessionName (pointer)
        //  Skipping LogFileName (pointer) 
        // Skipping  TimeZoneInformation
        public long BootTime100ns { get { return GetInt64At(HostOffset(240, 2)); } }
        public DateTime BootTime { get { return DateTime.FromFileTime(BootTime100ns); } }
        public long PerfFreq { get { return GetInt64At(HostOffset(248, 2)); } }
        public long StartTime100ns { get { return GetInt64At(HostOffset(256, 2)); } }
        public DateTime StartTime { get { return DateTime.FromFileTime(StartTime100ns); } }
        public int ReservedFlags { get { return GetInt32At(HostOffset(264, 2)); } }
        public int BuffersLost { get { return GetInt32At(HostOffset(268, 2)); } }
        public string SessionName { get { return GetUnicodeStringAt(HostOffset(272, 2)); } }
        public string LogFileName { get { return GetUnicodeStringAt(SkipUnicodeString(HostOffset(272, 2))); } }

        #region Private
        internal EventTraceHeaderTraceData(Action<EventTraceHeaderTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(HostOffset(272, 2)))));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipUnicodeString(SkipUnicodeString(HostOffset(272, 2)))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("BufferSize", BufferSize);
            sb.XmlAttrib("Version", Version);
            sb.XmlAttrib("ProviderVersion", ProviderVersion);
            sb.XmlAttrib("NumberOfProcessors", NumberOfProcessors);
            sb.XmlAttrib("EndTime", EndTime);
            sb.XmlAttrib("TimerResolution", TimerResolution);
            sb.XmlAttrib("MaxFileSize", MaxFileSize);
            sb.XmlAttribHex("LogFileMode", LogFileMode);
            sb.XmlAttrib("BuffersWritten", BuffersWritten);
            sb.XmlAttrib("StartBuffers", StartBuffers);
            sb.XmlAttrib("PointerSize", PointerSize);
            sb.XmlAttrib("EventsLost", EventsLost);
            sb.XmlAttrib("CPUSpeed", CPUSpeed);
            sb.XmlAttrib("BootTime", BootTime);
            sb.XmlAttrib("PerfFreq", PerfFreq);
            sb.XmlAttrib("StartTime", StartTime);
            sb.XmlAttribHex("ReservedFlags", ReservedFlags);
            sb.XmlAttrib("BuffersLost", BuffersLost);
            sb.XmlAttrib("SessionName", SessionName);
            sb.XmlAttrib("LogFileName", LogFileName);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "BufferSize", "Version", "ProviderVersion", "NumberOfProcessors", "EndTime", "TimerResolution", "MaxFileSize", "LogFileMode", "BuffersWritten", "StartBuffers", "PointerSize", "EventsLost", "CPUSpeed", "BootTime", "PerfFreq", "StartTime", "ReservedFlags", "BuffersLost", "SessionName", "LogFileName" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return BufferSize;
                case 1:
                    return Version;
                case 2:
                    return ProviderVersion;
                case 3:
                    return NumberOfProcessors;
                case 4:
                    return EndTime;
                case 5:
                    return TimerResolution;
                case 6:
                    return MaxFileSize;
                case 7:
                    return LogFileMode;
                case 8:
                    return BuffersWritten;
                case 9:
                    return StartBuffers;
                case 10:
                    return PointerSize;
                case 11:
                    return EventsLost;
                case 12:
                    return CPUSpeed;
                case 13:
                    return BootTime;
                case 14:
                    return PerfFreq;
                case 15:
                    return StartTime;
                case 16:
                    return ReservedFlags;
                case 17:
                    return BuffersLost;
                case 18:
                    return SessionName;
                case 19:
                    return LogFileName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<EventTraceHeaderTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)] 
    public sealed class HeaderExtensionTraceData : TraceEvent
    {
        public int GroupMask1 { get { return GetInt32At(0); } }
        public int GroupMask2 { get { return GetInt32At(4); } }
        public int GroupMask3 { get { return GetInt32At(8); } }
        public int GroupMask4 { get { return GetInt32At(12); } }
        public int GroupMask5 { get { return GetInt32At(16); } }
        public int GroupMask6 { get { return GetInt32At(20); } }
        public int GroupMask7 { get { return GetInt32At(24); } }
        public int GroupMask8 { get { return GetInt32At(28); } }
        public int KernelEventVersion { get { if (Version >= 2) return GetInt32At(32); return 0; } }

        #region Private
        internal HeaderExtensionTraceData(Action<HeaderExtensionTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            // TODO fix for 
            Debug.Assert(!(Version == 0 && EventDataLength != 32));
            Debug.Assert(!(Version == 1 && EventDataLength != 32));
            Debug.Assert(!(Version == 2 && EventDataLength != 36));
            Debug.Assert(!(Version > 2 && EventDataLength < 36));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("GroupMask1", GroupMask1);
            sb.XmlAttribHex("GroupMask2", GroupMask2);
            sb.XmlAttribHex("GroupMask3", GroupMask3);
            sb.XmlAttribHex("GroupMask4", GroupMask4);
            sb.XmlAttribHex("GroupMask5", GroupMask5);
            sb.XmlAttribHex("GroupMask6", GroupMask6);
            sb.XmlAttribHex("GroupMask7", GroupMask7);
            sb.XmlAttribHex("GroupMask8", GroupMask8);
            sb.XmlAttribHex("KernelEventVersion", KernelEventVersion);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "GroupMask1", "GroupMask2", "GroupMask3", "GroupMask4", "GroupMask5", "GroupMask6", "GroupMask7", "GroupMask8", "KernelEventVersion" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return GroupMask1;
                case 1:
                    return GroupMask2;
                case 2:
                    return GroupMask3;
                case 3:
                    return GroupMask4;
                case 4:
                    return GroupMask5;
                case 5:
                    return GroupMask6;
                case 6:
                    return GroupMask7;
                case 7:
                    return GroupMask8;
                case 8:
                    return KernelEventVersion;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeaderExtensionTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)] 
    public sealed class ProcessTraceData : TraceEvent
    {
        // public int ProcessID { get { if (Version >= 1) return GetInt32At(HostOffset(4, 1)); return (int) GetHostPointer(0); } }
        [Obsolete("Use ParentID")]
        public int ParentId { get { return ParentID;  } }
        public int ParentID { get { if (Version >= 1) return GetInt32At(HostOffset(8, 1)); return (int)GetHostPointer(HostOffset(4, 1)); } }
        // Skipping UserSID
        public string KernelImageFileName { get { if (Version >= 1) return GetAsciiStringAt(SkipSID(HostOffset(20, 1))); return GetAsciiStringAt(SkipSID(HostOffset(8, 2))); } }
        public string ImageFileName { get { return state.KernelToUser(KernelImageFileName); } }

        public Address PageDirectoryBase { get { if (Version >= 1) return GetHostPointer(0); return 0; } }
        public int SessionID { get { if (Version >= 1) return GetInt32At(HostOffset(12, 1)); return 0; } }
        public int ExitStatus { get { if (Version >= 1) return GetInt32At(HostOffset(16, 1)); return 0; } }
        public Address UniqueProcessKey { get { if (Version >= 2) return GetHostPointer(0); return 0; } }
        public string CommandLine { get { if (Version >= 2) return GetUnicodeStringAt(SkipAsciiString(SkipSID(HostOffset(20, 1)))); return ""; } }

        #region Private
        internal ProcessTraceData(Action<ProcessTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < SkipAsciiString(SkipSID(HostOffset(8, 2)))));  // TODO fixed by hand
            Debug.Assert(!(Version == 1 && EventDataLength < SkipAsciiString(SkipSID(HostOffset(20, 1))))); // TODO fixed by hand
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(SkipAsciiString(SkipSID(HostOffset(20, 1))))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(SkipAsciiString(SkipSID(HostOffset(20, 1))))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("ProcessID", ProcessID);
            sb.XmlAttrib("ParentID", ParentID);
            sb.XmlAttrib("ImageFileName", ImageFileName);
            sb.XmlAttribHex("PageDirectoryBase", PageDirectoryBase);
            sb.XmlAttrib("SessionID", SessionID);
            sb.XmlAttribHex("ExitStatus", ExitStatus);
            sb.XmlAttribHex("UniqueProcessKey", UniqueProcessKey);
            sb.XmlAttrib("CommandLine", CommandLine);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ProcessID", "ParentID", "ImageFileName", "PageDirectoryBase", "SessionID", "ExitStatus", "UniqueProcessKey", "CommandLine" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ProcessID;
                case 1:
                    return ParentID;
                case 2:
                    return ImageFileName;
                case 3:
                    return PageDirectoryBase;
                case 4:
                    return SessionID;
                case 5:
                    return ExitStatus;
                case 6:
                    return UniqueProcessKey;
                case 7:
                    return CommandLine;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ProcessTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            // We wish to create the illusion that the events are reported by the process being started.   
            eventRecord->EventHeader.ProcessId = GetInt32At(HostOffset(4, 1));
            if (Opcode != TraceEventOpcode.Stop)             // Stop events do have the correct ThreadID, so keep it
            {
                ParentThread = eventRecord->EventHeader.ThreadId;
                eventRecord->EventHeader.ThreadId = -1;
            }
        }
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class ProcessCtrTraceData : TraceEvent
    {
        // public int ProcessID { get { return GetInt32At(0); } }
        public int PageFaultCount { get { return GetInt32At(4); } }
        public int HandleCount { get { return GetInt32At(8); } }
        // Skipping Reserved
        public Address PeakVirtualSize { get { return GetHostPointer(16); } }
        public Address PeakWorkingSetSize { get { return GetHostPointer(HostOffset(20, 1)); } }
        public Address PeakPagefileUsage { get { return GetHostPointer(HostOffset(24, 2)); } }
        public Address QuotaPeakPagedPoolUsage { get { return GetHostPointer(HostOffset(28, 3)); } }
        public Address QuotaPeakNonPagedPoolUsage { get { return GetHostPointer(HostOffset(32, 4)); } }
        public Address VirtualSize { get { return GetHostPointer(HostOffset(36, 5)); } }
        public Address WorkingSetSize { get { return GetHostPointer(HostOffset(40, 6)); } }
        public Address PagefileUsage { get { return GetHostPointer(HostOffset(44, 7)); } }
        public Address QuotaPagedPoolUsage { get { return GetHostPointer(HostOffset(48, 8)); } }
        public Address QuotaNonPagedPoolUsage { get { return GetHostPointer(HostOffset(52, 9)); } }
        public Address PrivatePageCount { get { return GetHostPointer(HostOffset(56, 10)); } }

        #region Private
        internal ProcessCtrTraceData(Action<ProcessCtrTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(60, 11)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(60, 11)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("ProcessID", ProcessID);
            sb.XmlAttrib("PageFaultCount", PageFaultCount);
            sb.XmlAttrib("HandleCount", HandleCount);
            sb.XmlAttribHex("PeakVirtualSize", PeakVirtualSize);
            sb.XmlAttribHex("PeakWorkingSetSize", PeakWorkingSetSize);
            sb.XmlAttribHex("PeakPagefileUsage", PeakPagefileUsage);
            sb.XmlAttribHex("QuotaPeakPagedPoolUsage", QuotaPeakPagedPoolUsage);
            sb.XmlAttribHex("QuotaPeakNonPagedPoolUsage", QuotaPeakNonPagedPoolUsage);
            sb.XmlAttribHex("VirtualSize", VirtualSize);
            sb.XmlAttribHex("WorkingSetSize", WorkingSetSize);
            sb.XmlAttribHex("PagefileUsage", PagefileUsage);
            sb.XmlAttribHex("QuotaPagedPoolUsage", QuotaPagedPoolUsage);
            sb.XmlAttribHex("QuotaNonPagedPoolUsage", QuotaNonPagedPoolUsage);
            sb.XmlAttribHex("PrivatePageCount", PrivatePageCount);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ProcessID", "PageFaultCount", "HandleCount", "PeakVirtualSize", "PeakWorkingSetSize", "PeakPagefileUsage", "QuotaPeakPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "VirtualSize", "WorkingSetSize", "PagefileUsage", "QuotaPagedPoolUsage", "QuotaNonPagedPoolUsage", "PrivatePageCount" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ProcessID;
                case 1:
                    return PageFaultCount;
                case 2:
                    return HandleCount;
                case 3:
                    return PeakVirtualSize;
                case 4:
                    return PeakWorkingSetSize;
                case 5:
                    return PeakPagefileUsage;
                case 6:
                    return QuotaPeakPagedPoolUsage;
                case 7:
                    return QuotaPeakNonPagedPoolUsage;
                case 8:
                    return VirtualSize;
                case 9:
                    return WorkingSetSize;
                case 10:
                    return PagefileUsage;
                case 11:
                    return QuotaPagedPoolUsage;
                case 12:
                    return QuotaNonPagedPoolUsage;
                case 13:
                    return PrivatePageCount;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ProcessCtrTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }

        #endregion
    }
    [CLSCompliant(false)]
    public sealed class ThreadTraceData : TraceEvent
    {
        // public int ThreadID { get { if (Version >= 1) return GetInt32At(4); return GetInt32At(0); } }
        // public int ProcessID { get { if (Version >= 1) return GetInt32At(0); return GetInt32At(4); } }
        public Address StackBase { get { if (Version >= 2) return GetHostPointer(8); return 0; } }
        public Address StackLimit { get { if (Version >= 2) return GetHostPointer(HostOffset(12, 1)); return 0; } }
        public Address UserStackBase { get { if (Version >= 2) return GetHostPointer(HostOffset(16, 2)); return 0; } }
        public Address UserStackLimit { get { if (Version >= 2) return GetHostPointer(HostOffset(20, 3)); return 0; } }
        public Address StartAddr { get { if (Version >= 2) return GetHostPointer(HostOffset(24, 4)); return 0; } }
        public Address Win32StartAddr { get { if (Version >= 2) return GetHostPointer(HostOffset(28, 5)); return 0; } }
        // Not present in V2 public int WaitMode { get { if (Version >= 1) return GetByteAt(HostOffset(32, 6)); return 0; } }
        public Address TebBase { get { if (Version >= 2) return GetHostPointer(HostOffset(32, 6)); return 0; } }
        public int SubProcessTag { get { if (Version >= 2) return GetInt32At(HostOffset(36, 7)); return 0; } }

        #region Private
        internal ThreadTraceData(Action<ThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength < 8));        // TODO fixed by hand (can be better)
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(40, 7)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(40, 7)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("StackBase", StackBase);
            sb.XmlAttribHex("StackLimit", StackLimit);
            sb.XmlAttribHex("UserStackBase", UserStackBase);
            sb.XmlAttribHex("UserStackLimit", UserStackLimit);
            sb.XmlAttribHex("StartAddr", StartAddr);
            sb.XmlAttribHex("Win32StartAddr", Win32StartAddr);
            sb.XmlAttribHex("TebBase", TebBase);
            sb.XmlAttribHex("SubProcessTag", SubProcessTag);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "StackBase", "StackLimit", "UserStackBase", "UserStackLimit", "StartAddr", "Win32StartAddr", "TebBase", "SubProcessTag" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return StackBase;
                case 1:
                    return StackLimit;
                case 2:
                    return UserStackBase;
                case 3:
                    return UserStackLimit;
                case 4:
                    return StartAddr;
                case 5:
                    return Win32StartAddr;
                case 6:
                    return TebBase;
                case 7:
                    return SubProcessTag;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ThreadTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            // We wish to create the illusion that the events are reported by the thread being started.   
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
            if (Opcode != TraceEventOpcode.Stop)             // Stop events do have the correct ThreadID, so keep it
            {
                ParentThread = eventRecord->EventHeader.ThreadId;
                if (Version >= 1)
                    eventRecord->EventHeader.ThreadId = GetInt32At(4);
                else
                    eventRecord->EventHeader.ThreadId = GetInt32At(0);
            }
        }

        /// <summary>
        /// Indicate that StartAddr and Win32StartAddr are a code addresses that needs symbolic information
        /// </summary>
        protected internal override void LogCodeAddresses(Action<TraceEvent, Address> callBack)
        {
            callBack(this, StartAddr);
            // TODO is this one worth resolving?
            // callBack(this, Win32StartAddr);
        }
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class CSwitchTraceData : TraceEvent
    {
        public enum ThreadWaitMode
        {
            NonSwap = 0,
            Swappable = 1,
        };

        /// <summary>
        /// We report a context switch from from the new thread.  Thus NewThreadID == ThreadID.  
        /// </summary>
        public int NewThreadID { get { return ThreadID; } }
        public int NewProcessID { get { return ProcessID; } }
        public int OldThreadID { get { return GetInt32At(4); } }
        public int NewThreadPriority { get { return GetByteAt(8); } }
        public int OldThreadPriority { get { return GetByteAt(9); } }
        public int OldProcessID { get { return state.ThreadIDToProcessID(OldThreadID, TimeStamp100ns); } }
        public string OldProcessName { get { return source.ProcessName(OldProcessID, TimeStamp100ns); } }
        // TODO figure out which one of these are right
        public int NewThreadQuantum { get { return GetByteAt(10); } }
        public int OldThreadQuantum { get { return GetByteAt(11); } }

        // public int PreviousCState { get { return GetByteAt(10); } }
        // public int SpareByte { get { return GetByteAt(11); } }

        public ThreadWaitReason OldThreadWaitReason { get { return (ThreadWaitReason)GetByteAt(0xc); } }
        public ThreadWaitMode OldThreadWaitMode { get { return (ThreadWaitMode)GetByteAt(0xd); } }
        public ThreadState OldThreadState { get { return (ThreadState)GetByteAt(0xe); } }
        public int OldThreadWaitIdealProcessor { get { return GetByteAt(15); } }
        public int NewThreadWaitTime { get { return GetInt32At(16); } }
        // Skipping Reserved

        #region Private
        internal CSwitchTraceData(Action<CSwitchTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != 24));
            Debug.Assert(!(Version > 2 && EventDataLength < 24));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("OldThreadID", OldThreadID);
            sb.XmlAttrib("OldProcessID", OldProcessID);
            sb.XmlAttrib("OldProcessName", OldProcessName);
            sb.XmlAttrib("NewThreadPriority", NewThreadPriority);
            sb.XmlAttrib("OldThreadPriority", OldThreadPriority);
            sb.XmlAttrib("NewThreadQuantum", NewThreadQuantum);
            sb.XmlAttrib("OldThreadQuantum", OldThreadQuantum);
            sb.XmlAttrib("OldThreadWaitReason", OldThreadWaitReason);
            sb.XmlAttrib("OldThreadWaitMode", OldThreadWaitMode);
            sb.XmlAttrib("OldThreadState", OldThreadState);
            sb.XmlAttrib("OldThreadWaitIdealProcessor", OldThreadWaitIdealProcessor);
            sb.XmlAttribHex("NewThreadWaitTime", NewThreadWaitTime);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "OldThreadID", "OldPRocessID", "OldProcessName",
                        "NewThreadPriority", "OldThreadPriority", "NewThreadQuantum", "OldThreadQuantum", 
                        "OldThreadWaitReason", "OldThreadWaitMode", "OldThreadState", "OldThreadWaitIdealProcessor", 
                        "NewThreadWaitTime" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {

                case 0:
                    return OldThreadID;
                case 1:
                    return OldProcessID;
                case 2:
                    return OldProcessName;
                case 3:
                    return NewThreadPriority;
                case 4:
                    return OldThreadPriority;
                case 5:
                    return NewThreadQuantum;
                case 6:
                    return OldThreadQuantum;
                case 7:
                    return OldThreadWaitReason;
                case 8:
                    return OldThreadWaitMode;
                case 9:
                    return OldThreadState;
                case 10:
                    return OldThreadWaitIdealProcessor;
                case 11:
                    return NewThreadWaitTime;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<CSwitchTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ThreadId == -1);
            eventRecord->EventHeader.ThreadId = GetInt32At(0);
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = state.ThreadIDToProcessID(ThreadID, TimeStamp100ns);
        }
        private string ToString(ThreadState state)
        {
            switch (state)
            {
                case ThreadState.Initialized: return "Initialized";
                case ThreadState.Ready: return "Ready";
                case ThreadState.Running: return "Running";
                case ThreadState.Standby: return "Standby";
                case ThreadState.Terminated: return "Terminated";
                case ThreadState.Wait: return "Wait";
                case ThreadState.Transition: return "Transition";
                case ThreadState.Unknown: return "Unknown";
                default: return ((int)state).ToString();
            }
        }
        private object ToString(ThreadWaitMode mode)
        {
            switch (mode)
            {
                case ThreadWaitMode.NonSwap: return "NonSwap";
                case ThreadWaitMode.Swappable: return "Swappable";
                default: return ((int)mode).ToString();
            }
        }
        private object ToString(ThreadWaitReason reason)
        {
            switch (reason)
            {
                case ThreadWaitReason.Executive: return "Executive";
                case ThreadWaitReason.FreePage: return "FreePage";
                case ThreadWaitReason.PageIn: return "PageIn";
                case ThreadWaitReason.SystemAllocation: return "SystemAllocation";
                case ThreadWaitReason.ExecutionDelay: return "ExecutionDelay";
                case ThreadWaitReason.Suspended: return "Suspended";
                case ThreadWaitReason.UserRequest: return "UserRequest";
                case ThreadWaitReason.EventPairHigh: return "EventPairHigh";
                case ThreadWaitReason.EventPairLow: return "EventPairLow";
                case ThreadWaitReason.LpcReceive: return "LpcReceive";
                case ThreadWaitReason.LpcReply: return "LpcReply";
                case ThreadWaitReason.VirtualMemory: return "VirtualMemory";
                case ThreadWaitReason.PageOut: return "PageOut";
                case ThreadWaitReason.Unknown: return "Unknown";
                default: return ((int)reason).ToString();
            }
        }
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class WorkerThreadTraceData : TraceEvent
    {
        public int TThreadID { get { return GetInt32At(0); } }
        public long StartTime100ns { get { return GetInt64At(4); } }
        public DateTime StartTime { get { return DateTime.FromFileTime(StartTime100ns); } }
        public Address ThreadRoutine { get { return GetHostPointer(12); } }

        #region Private
        internal WorkerThreadTraceData(Action<WorkerThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(16, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(16, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("TThreadID", TThreadID);
            sb.XmlAttrib("StartTime", StartTime);
            sb.XmlAttribHex("ThreadRoutine", ThreadRoutine);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "TThreadID", "StartTime", "ThreadRoutine" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return TThreadID;
                case 1:
                    return StartTime;
                case 2:
                    return ThreadRoutine;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<WorkerThreadTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class ReserveCreateTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }
        public int Period { get { return GetInt32At(HostOffset(4, 1)); } }
        public int Budget { get { return GetInt32At(HostOffset(8, 1)); } }
        public int ObjectFlags { get { return GetInt32At(HostOffset(12, 1)); } }
        public int Processor { get { return GetByteAt(HostOffset(16, 1)); } }

        #region Private
        internal ReserveCreateTraceData(Action<ReserveCreateTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(17, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(17, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.XmlAttrib("Period", Period);
            sb.XmlAttrib("Budget", Budget);
            sb.XmlAttrib("ObjectFlags", ObjectFlags);
            sb.XmlAttrib("Processor", Processor);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve", "Period", "Budget", "ObjectFlags", "Processor" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                case 1:
                    return Period;
                case 2:
                    return Budget;
                case 3:
                    return ObjectFlags;
                case 4:
                    return Processor;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveCreateTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class ReserveDeleteTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }

        #region Private
        internal ReserveDeleteTraceData(Action<ReserveDeleteTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveDeleteTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class ReserveJoinThreadTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }
        public int TThreadID { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal ReserveJoinThreadTraceData(Action<ReserveJoinThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.XmlAttrib("TThreadID", TThreadID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve", "TThreadID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                case 1:
                    return TThreadID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveJoinThreadTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ReserveDisjoinThreadTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }
        public int TThreadID { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal ReserveDisjoinThreadTraceData(Action<ReserveDisjoinThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.XmlAttrib("TThreadID", TThreadID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve", "TThreadID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                case 1:
                    return TThreadID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveDisjoinThreadTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ReserveStateTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }
        public int DispatchState { get { return GetByteAt(HostOffset(4, 1)); } }
        public bool Replenished { get { return GetByteAt(HostOffset(5, 1)) != 0; } }

        #region Private
        internal ReserveStateTraceData(Action<ReserveStateTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(6, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(6, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.XmlAttrib("DispatchState", DispatchState);
            sb.XmlAttrib("Replenished", Replenished);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve", "DispatchState", "Replenished" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                case 1:
                    return DispatchState;
                case 2:
                    return Replenished;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveStateTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ReserveBandwidthTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }
        public int Period { get { return GetInt32At(HostOffset(4, 1)); } }
        public int Budget { get { return GetInt32At(HostOffset(8, 1)); } }

        #region Private
        internal ReserveBandwidthTraceData(Action<ReserveBandwidthTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(12, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(12, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.XmlAttrib("Period", Period);
            sb.XmlAttrib("Budget", Budget);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve", "Period", "Budget" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                case 1:
                    return Period;
                case 2:
                    return Budget;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveBandwidthTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ReserveLateCountTraceData : TraceEvent
    {
        public Address Reserve { get { return GetHostPointer(0); } }
        public int LateCountIncrement { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal ReserveLateCountTraceData(Action<ReserveLateCountTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Reserve", Reserve);
            sb.XmlAttrib("LateCountIncrement", LateCountIncrement);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Reserve", "LateCountIncrement" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Reserve;
                case 1:
                    return LateCountIncrement;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ReserveLateCountTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DiskIoTraceData : TraceEvent
    {
        public int DiskNumber { get { return GetInt32At(0); } }
        public int IrpFlags { get { return GetInt32At(4); } }
        public int TransferSize { get { return GetInt32At(8); } }
        // Skipping Reserved
        public long ByteOffset { get { return GetInt64At(16); } }
        public Address FileObject { get { return GetHostPointer(24); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }

        public Address Irp { get { return GetHostPointer(HostOffset(28, 1)); } }
        public long HighResResponseTime100ns { get { return GetInt64At(HostOffset(32, 2)); } }
        public DateTime HighResResponseTime { get { return DateTime.FromFileTime(HighResResponseTime100ns); } }

        #region Private
        internal DiskIoTraceData(Action<DiskIoTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(40, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(40, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("DiskNumber", DiskNumber);
            sb.XmlAttribHex("IrpFlags", IrpFlags);
            sb.XmlAttrib("TransferSize", TransferSize);
            sb.XmlAttrib("ByteOffset", ByteOffset);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("Irp", Irp);
            sb.XmlAttrib("HighResResponseTime", HighResResponseTime);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "DiskNumber", "IrpFlags", "TransferSize", "ByteOffset", "FileObject", "Irp", "HighResResponseTime" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return DiskNumber;
                case 1:
                    return IrpFlags;
                case 2:
                    return TransferSize;
                case 3:
                    return ByteOffset;
                case 4:
                    return FileObject;
                case 5:
                    return Irp;
                case 6:
                    return HighResResponseTime;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DiskIoTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DiskIoInitTraceData : TraceEvent
    {
        public Address Irp { get { return GetHostPointer(0); } }

        #region Private
        internal DiskIoInitTraceData(Action<DiskIoInitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Irp", Irp);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Irp" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Irp;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DiskIoInitTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DiskIoFlushBuffersTraceData : TraceEvent
    {
        public int DiskNumber { get { return GetInt32At(0); } }
        public int IrpFlags { get { return GetInt32At(4); } }
        public long HighResResponseTime100ns { get { return GetInt64At(8); } }
        public DateTime HighResResponseTime { get { return DateTime.FromFileTime(HighResResponseTime100ns); } }
        public Address Irp { get { return GetHostPointer(16); } }

        #region Private
        internal DiskIoFlushBuffersTraceData(Action<DiskIoFlushBuffersTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(20, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(20, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("DiskNumber", DiskNumber);
            sb.XmlAttribHex("IrpFlags", IrpFlags);
            sb.XmlAttrib("HighResResponseTime", HighResResponseTime);
            sb.XmlAttribHex("Irp", Irp);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "DiskNumber", "IrpFlags", "HighResResponseTime", "Irp" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return DiskNumber;
                case 1:
                    return IrpFlags;
                case 2:
                    return HighResResponseTime;
                case 3:
                    return Irp;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DiskIoFlushBuffersTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DriverMajorFunctionCallTraceData : TraceEvent
    {
        public int MajorFunction { get { return GetInt32At(0); } }
        public int MinorFunction { get { return GetInt32At(4); } }
        public Address RoutineAddr { get { return GetHostPointer(8); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(12, 1)); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        public Address Irp { get { return GetHostPointer(HostOffset(16, 2)); } }
        public int UniqMatchID { get { return GetInt32At(HostOffset(20, 3)); } }

        #region Private
        internal DriverMajorFunctionCallTraceData(Action<DriverMajorFunctionCallTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(24, 3)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(24, 3)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("MajorFunction", MajorFunction);
            sb.XmlAttrib("MinorFunction", MinorFunction);
            sb.XmlAttribHex("RoutineAddr", RoutineAddr);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("Irp", Irp);
            sb.XmlAttrib("UniqMatchID", UniqMatchID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MajorFunction", "MinorFunction", "RoutineAddr", "FileObject", "Irp", "UniqMatchID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MajorFunction;
                case 1:
                    return MinorFunction;
                case 2:
                    return RoutineAddr;
                case 3:
                    return FileObject;
                case 4:
                    return Irp;
                case 5:
                    return UniqMatchID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DriverMajorFunctionCallTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DriverMajorFunctionReturnTraceData : TraceEvent
    {
        public Address Irp { get { return GetHostPointer(0); } }
        public int UniqMatchID { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal DriverMajorFunctionReturnTraceData(Action<DriverMajorFunctionReturnTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Irp", Irp);
            sb.XmlAttrib("UniqMatchID", UniqMatchID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Irp", "UniqMatchID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Irp;
                case 1:
                    return UniqMatchID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DriverMajorFunctionReturnTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DriverCompletionRoutineTraceData : TraceEvent
    {
        public Address Routine { get { return GetHostPointer(0); } }
        public Address IrpPtr { get { return GetHostPointer(HostOffset(4, 1)); } }
        public int UniqMatchID { get { return GetInt32At(HostOffset(8, 2)); } }

        #region Private
        internal DriverCompletionRoutineTraceData(Action<DriverCompletionRoutineTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(12, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(12, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Routine", Routine);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttrib("UniqMatchID", UniqMatchID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Routine", "IrpPtr", "UniqMatchID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Routine;
                case 1:
                    return IrpPtr;
                case 2:
                    return UniqMatchID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DriverCompletionRoutineTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DriverCompleteRequestTraceData : TraceEvent
    {
        public Address RoutineAddr { get { return GetHostPointer(0); } }
        public Address Irp { get { return GetHostPointer(HostOffset(4, 1)); } }
        public int UniqMatchID { get { return GetInt32At(HostOffset(8, 2)); } }

        #region Private
        internal DriverCompleteRequestTraceData(Action<DriverCompleteRequestTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(12, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(12, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("RoutineAddr", RoutineAddr);
            sb.XmlAttribHex("Irp", Irp);
            sb.XmlAttrib("UniqMatchID", UniqMatchID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "RoutineAddr", "Irp", "UniqMatchID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return RoutineAddr;
                case 1:
                    return Irp;
                case 2:
                    return UniqMatchID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DriverCompleteRequestTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DriverCompleteRequestReturnTraceData : TraceEvent
    {
        public Address Irp { get { return GetHostPointer(0); } }
        public int UniqMatchID { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal DriverCompleteRequestReturnTraceData(Action<DriverCompleteRequestReturnTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Irp", Irp);
            sb.XmlAttrib("UniqMatchID", UniqMatchID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Irp", "UniqMatchID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Irp;
                case 1:
                    return UniqMatchID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DriverCompleteRequestReturnTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class RegistryTraceData : TraceEvent
    {
        public int Status { get { if (Version >= 2) return GetInt32At(8); return (int)GetHostPointer(0); } }
        public Address KeyHandle { get { if (Version >= 2) return GetHostPointer(16); return GetHostPointer(HostOffset(4, 1)); } }
        public long ElapsedTime { get { return GetInt64At(HostOffset(8, 2)); } }

        public string KeyName
        {
            // TODO: I not certain I have things working properly on the Handle lookup. 
            get
            {
                if (NameIsKeyName(Opcode))
                {
                    if (Version >= 2) return GetUnicodeStringAt(HostOffset(20, 1));
                    if (Version >= 1) return GetUnicodeStringAt(HostOffset(20, 2));
                    return GetUnicodeStringAt(HostOffset(16, 2));
                }
                else
                    return state.FileIDToName(KeyHandle, TimeStamp100ns);
            }
        }
        public string ValueName
        {
            get
            {
                if (NameIsKeyName(Opcode))
                    return "";
                else
                    return GetUnicodeStringAt((Version < 2 ? HostOffset(0x14, 2) : HostOffset(0x14, 1)));
            }
        }
        public int Index { get { if (Version >= 2) return GetInt32At(12); if (Version >= 1) return GetInt32At(HostOffset(16, 2)); return 0; } }
        public long InitialTime100ns { get { if (Version >= 2) return GetInt64At(0); return 0; } }
        public DateTime InitialTime { get { return DateTime.FromFileTime(InitialTime100ns); } }

        #region Private
        internal RegistryTraceData(Action<RegistryTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(HostOffset(16, 2))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(HostOffset(20, 2))));
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(HostOffset(20, 1))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(HostOffset(20, 1))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("Status", Status);
            sb.XmlAttribHex("KeyHandle", KeyHandle);
            sb.XmlAttrib("ElapsedTime", ElapsedTime);
            sb.XmlAttrib("KeyName", KeyName);
            sb.XmlAttrib("Index", Index);
            sb.XmlAttrib("InitialTime", InitialTime);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Status", "KeyHandle", "ElapsedTime", "KeyName", "Index", "InitialTime" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Status;
                case 1:
                    return KeyHandle;
                case 2:
                    return ElapsedTime;
                case 3:
                    return KeyName;
                case 4:
                    return Index;
                case 5:
                    return InitialTime;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<RegistryTraceData> Action;
        private KernelTraceEventParserState state;

        static internal bool NameIsKeyName(TraceEventOpcode code)
        {
            // TODO confirm this is true
            switch ((int)code)
            {
                case 10: // Create
                case 11: // Open
                case 12: // Delete 
                    return true;
                case 13:    // Query
                case 14:    // SetValue
                case 15:    // DeleteValue
                case 16:    // QueryValue
                    return false;
                case 17:    // EnumerateKey
                    return true;
                case 18:    // EnumerateValueKey
                case 19:    // QueryMultipleValue
                    return false;
                case 20:    // SetInformation
                case 21:    // Flush
                case 22:    // KCBCreate
                case 23:    // KCBDelete
                case 24:    // KCBRundownBegin
                case 25:    // KCBRundownEnd
                case 26:    // Virtualize
                case 27:    // Close
                    return true;
                default:
                    Debug.Assert(false, "Unexpected Opcode");
                    return true;    // Seems the lesser of evils
            }
        }
        #endregion
    }
    public sealed class SplitIoInfoTraceData : TraceEvent
    {
        public Address ParentIrp { get { return GetHostPointer(0); } }
        public Address ChildIrp { get { return GetHostPointer(HostOffset(4, 1)); } }

        #region Private
        internal SplitIoInfoTraceData(Action<SplitIoInfoTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("ParentIrp", ParentIrp);
            sb.XmlAttribHex("ChildIrp", ChildIrp);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ParentIrp", "ChildIrp" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ParentIrp;
                case 1:
                    return ChildIrp;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SplitIoInfoTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoNameTraceData : TraceEvent
    {
        public Address FileObject { get { return GetHostPointer(0); } }
        public string KernelFileName { get { return GetUnicodeStringAt(HostOffset(4, 1)); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }

        #region Private
        internal FileIoNameTraceData(Action<FileIoNameTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(HostOffset(4, 1))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(HostOffset(4, 1))));
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(HostOffset(4, 1))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(HostOffset(4, 1))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttrib("FileName", FileName);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "FileObject", "FileName" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return FileObject;
                case 1:
                    return FileName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoNameTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoCreateTraceData : TraceEvent
    {
        public Address IrpPtr { get { return GetHostPointer(0); } }
        // public Address TTID { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(8, 2)); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        // TODO proper enums for these
        public int CreateOptions { get { return GetInt32At(HostOffset(12, 3)); } }
        public int FileAttributes { get { return GetInt32At(HostOffset(16, 3)); } }
        public int ShareAccess { get { return GetInt32At(HostOffset(20, 3)); } }
        public string KernelOpenPath { get { return GetUnicodeStringAt(HostOffset(24, 3)); } }
        public string OpenPath { get { return state.KernelToUser(KernelOpenPath); } }

        #region Private
        internal FileIoCreateTraceData(Action<FileIoCreateTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(GetInt32At(HostOffset(4, 1)) == ThreadID);
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(HostOffset(24, 3))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(HostOffset(24, 3))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttrib("CreateOptions", CreateOptions);
            sb.XmlAttrib("FileAttributes", FileAttributes);
            sb.XmlAttrib("ShareAccess", ShareAccess);
            sb.XmlAttrib("OpenPath", OpenPath);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IrpPtr", "FileObject", "CreateOptions", "FileAttributes", "ShareAccess", "OpenPath" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IrpPtr;
                case 1:
                    return FileObject;
                case 2:
                    return CreateOptions;
                case 3:
                    return FileAttributes;
                case 4:
                    return ShareAccess;
                case 5:
                    return OpenPath;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoCreateTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoSimpleOpTraceData : TraceEvent
    {
        public Address IrpPtr { get { return GetHostPointer(0); } }
        // public Address TTID { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(8, 2)); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        public Address FileKey { get { return GetHostPointer(HostOffset(12, 3)); } }

        #region Private
        internal FileIoSimpleOpTraceData(Action<FileIoSimpleOpTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(GetInt32At(HostOffset(4, 1)) == ThreadID);
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(16, 4)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(16, 4)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("FileKey", FileKey);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IrpPtr", "FileObject", "FileKey" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IrpPtr;
                case 1:
                    return FileObject;
                case 2:
                    return FileKey;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoSimpleOpTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoReadWriteTraceData : TraceEvent
    {
        public long Offset { get { return GetInt64At(0); } }
        public Address IrpPtr { get { return GetHostPointer(8); } }
        // public Address TTID { get { return GetHostPointer(HostOffset(12, 1)); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(16, 2)); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        public Address FileKey { get { return GetHostPointer(HostOffset(20, 3)); } }
        public int IoSize { get { return GetInt32At(HostOffset(24, 4)); } }
        public int IoFlags { get { return GetInt32At(HostOffset(28, 4)); } }

        #region Private
        internal FileIoReadWriteTraceData(Action<FileIoReadWriteTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(GetInt32At(HostOffset(4, 1)) == ThreadID);
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(32, 4)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(32, 4)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("Offset", Offset);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("FileKey", FileKey);
            sb.XmlAttrib("IoSize", IoSize);
            sb.XmlAttrib("IoFlags", IoFlags);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Offset", "IrpPtr", "FileObject", "FileKey", "IoSize", "IoFlags" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Offset;
                case 1:
                    return IrpPtr;
                case 2:
                    return FileObject;
                case 3:
                    return FileKey;
                case 4:
                    return IoSize;
                case 5:
                    return IoFlags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoReadWriteTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoInfoTraceData : TraceEvent
    {
        public Address IrpPtr { get { return GetHostPointer(0); } }
        // public Address TTID { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(8, 2)); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        public Address FileKey { get { return GetHostPointer(HostOffset(12, 3)); } }
        public Address ExtraInfo { get { return GetHostPointer(HostOffset(16, 4)); } }
        public int InfoClass { get { return GetInt32At(HostOffset(20, 5)); } }

        #region Private
        internal FileIoInfoTraceData(Action<FileIoInfoTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(GetInt32At(HostOffset(4, 1)) == ThreadID);
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(24, 5)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(24, 5)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("FileKey", FileKey);
            sb.XmlAttribHex("ExtraInfo", ExtraInfo);
            sb.XmlAttrib("InfoClass", InfoClass);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IrpPtr", "FileObject", "FileKey", "ExtraInfo", "InfoClass" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IrpPtr;
                case 1:
                    return FileObject;
                case 2:
                    return FileKey;
                case 3:
                    return ExtraInfo;
                case 4:
                    return InfoClass;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoInfoTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoDirEnumTraceData : TraceEvent
    {
        public Address IrpPtr { get { return GetHostPointer(0); } }
        // public Address TTID { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(8, 2)); } }
        public Address FileKey { get { return GetHostPointer(HostOffset(12, 3)); } }
        public int Length { get { return GetInt32At(HostOffset(16, 4)); } }
        public int InfoClass { get { return GetInt32At(HostOffset(20, 4)); } }
        public int FileIndex { get { return GetInt32At(HostOffset(24, 4)); } }
        public string KernelFileName { get { return GetUnicodeStringAt(HostOffset(28, 4)); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }

        #region Private
        internal FileIoDirEnumTraceData(Action<FileIoDirEnumTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(GetInt32At(HostOffset(4, 1)) == ThreadID);
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(HostOffset(28, 4))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(HostOffset(28, 4))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("FileKey", FileKey);
            sb.XmlAttrib("Length", Length);
            sb.XmlAttrib("InfoClass", InfoClass);
            sb.XmlAttrib("FileIndex", FileIndex);
            sb.XmlAttrib("FileName", FileName);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IrpPtr", "FileObject", "FileKey", "Length", "InfoClass", "FileIndex", "FileName" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IrpPtr;
                case 1:
                    return FileObject;
                case 2:
                    return FileKey;
                case 3:
                    return Length;
                case 4:
                    return InfoClass;
                case 5:
                    return FileIndex;
                case 6:
                    return FileName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoDirEnumTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class FileIoOpEndTraceData : TraceEvent
    {
        public Address IrpPtr { get { return GetHostPointer(0); } }
        public Address ExtraInfo { get { return GetHostPointer(HostOffset(4, 1)); } }
        public int NtStatus { get { return GetInt32At(HostOffset(8, 2)); } }

        #region Private
        internal FileIoOpEndTraceData(Action<FileIoOpEndTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(12, 2)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(12, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("IrpPtr", IrpPtr);
            sb.XmlAttribHex("ExtraInfo", ExtraInfo);
            sb.XmlAttrib("NtStatus", NtStatus);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IrpPtr", "ExtraInfo", "NtStatus" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IrpPtr;
                case 1:
                    return ExtraInfo;
                case 2:
                    return NtStatus;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<FileIoOpEndTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class TcpIpTraceData : TraceEvent
    {
        // PID
        public int size { get { if (Version >= 1) return GetInt32At(4); return GetInt32At(12); } }
        public int daddr { get { if (Version >= 1) return GetInt32At(8); return GetInt32At(0); } }
        public int saddr { get { if (Version >= 1) return GetInt32At(12); return GetInt32At(4); } }
        public int dport { get { if (Version >= 1) return GetInt16At(16); return GetInt16At(8); } }
        public int sport { get { if (Version >= 1) return GetInt16At(18); return GetInt16At(10); } }
        public Address connid { get { if (Version >= 1) return GetHostPointer(HostOffset(20, 1)); return 0; } }
        public int seqnum { get { if (Version >= 1) return GetInt32At(HostOffset(24, 1)); return 0; } }

        #region Private
        internal TcpIpTraceData(Action<TcpIpTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 20));
            Debug.Assert(!(Version == 1 && EventDataLength < HostOffset(28, 1)));   // TODO fixed by hand
            Debug.Assert(!(Version > 1 && EventDataLength < HostOffset(28, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("daddr", daddr);
            sb.XmlAttrib("saddr", saddr);
            sb.XmlAttrib("dport", dport);
            sb.XmlAttrib("sport", sport);
            sb.XmlAttrib("size", size);
            sb.XmlAttrib("connid", connid);
            sb.XmlAttrib("seqnum", seqnum);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "daddr", "saddr", "dport", "sport", "size", "connid", "seqnum" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return daddr;
                case 1:
                    return saddr;
                case 2:
                    return dport;
                case 3:
                    return sport;
                case 4:
                    return size;
                case 5:
                    return connid;
                case 6:
                    return seqnum;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TcpIpTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            if (Version >= 1)
                eventRecord->EventHeader.ProcessId = GetInt32At(0);
            else
                eventRecord->EventHeader.ProcessId = GetInt32At(16);
        }
        #endregion
    }
    public sealed class TcpIpFailTraceData : TraceEvent
    {
        public int Proto { get { if (Version >= 2) return GetInt16At(0); return GetInt32At(0); } }
        public int FailureCode { get { if (Version >= 2) return GetInt16At(2); return 0; } }

        #region Private
        internal TcpIpFailTraceData(Action<TcpIpFailTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 1 && EventDataLength != 4));
            Debug.Assert(!(Version == 2 && EventDataLength != 4));
            Debug.Assert(!(Version > 2 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("Proto", Proto);
            sb.XmlAttrib("FailureCode", FailureCode);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Proto", "FailureCode" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Proto;
                case 1:
                    return FailureCode;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TcpIpFailTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }

    public sealed class TcpIpSendTraceData : TraceEvent
    {
        // TODO not quite right for V0 TcpIP (does anyone care?)

        // PID
        public int size { get { return GetInt32At(4); } }
        public int daddr { get { return GetInt32At(8); } }
        public int saddr { get { return GetInt32At(12); } }
        public int dport { get { return (ushort)GetInt16At(16); } }
        public int sport { get { return (ushort)GetInt16At(18); } }
        public int startime { get { return GetInt32At(20); } }
        public int endtime { get { return GetInt32At(24); } }
        public int seqnum { get { return GetInt32At(28); } }
        public Address connid { get { return GetHostPointer(32); } }

        #region Private
        internal TcpIpSendTraceData(Action<TcpIpSendTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(36, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(36, 1)));
            Action(this);
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("size", size);
            sb.XmlAttrib("daddr", daddr);
            sb.XmlAttrib("saddr", saddr);
            sb.XmlAttrib("dport", dport);
            sb.XmlAttrib("sport", sport);
            sb.XmlAttrib("startime", startime);
            sb.XmlAttrib("endtime", endtime);
            sb.XmlAttrib("seqnum", seqnum);
            sb.XmlAttrib("connid", connid);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "size", "daddr", "saddr", "dport", "sport", "startime", "endtime", "seqnum", "connid" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return size;
                case 1:
                    return daddr;
                case 2:
                    return saddr;
                case 3:
                    return dport;
                case 4:
                    return sport;
                case 5:
                    return startime;
                case 6:
                    return endtime;
                case 7:
                    return seqnum;
                case 8:
                    return connid;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TcpIpSendTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class TcpIpConnectTraceData : TraceEvent
    {
        // TODO not quite right for V0 TcpIP (does anyone care?)

        // PID
        public int size { get { return GetInt32At(4); } }
        public int daddr { get { return GetInt32At(8); } }
        public int saddr { get { return GetInt32At(12); } }
        public int dport { get { return (ushort)GetInt16At(16); } }
        public int sport { get { return (ushort)GetInt16At(18); } }
        public int mss { get { return GetInt16At(20); } }
        public int sackopt { get { return GetInt16At(22); } }
        public int tsopt { get { return GetInt16At(24); } }
        public int wsopt { get { return GetInt16At(26); } }
        public int rcvwin { get { return GetInt32At(28); } }
        public int rcvwinscale { get { return GetInt16At(32); } }
        public int sndwinscale { get { return GetInt16At(34); } }
        public int seqnum { get { return GetInt32At(36); } }
        public Address connid { get { return GetHostPointer(40); } }

        #region Private
        internal TcpIpConnectTraceData(Action<TcpIpConnectTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(44, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(44, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("size", size);
            sb.XmlAttrib("daddr", daddr);
            sb.XmlAttrib("saddr", saddr);
            sb.XmlAttrib("dport", dport);
            sb.XmlAttrib("sport", sport);
            sb.XmlAttrib("mss", mss);
            sb.XmlAttrib("sackopt", sackopt);
            sb.XmlAttrib("tsopt", tsopt);
            sb.XmlAttrib("wsopt", wsopt);
            sb.XmlAttrib("rcvwin", rcvwin);
            sb.XmlAttrib("rcvwinscale", rcvwinscale);
            sb.XmlAttrib("sndwinscale", sndwinscale);
            sb.XmlAttrib("seqnum", seqnum);
            sb.XmlAttrib("connid", connid);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "size", "daddr", "saddr", "dport", "sport", "mss", "sackopt", "tsopt", "wsopt", "rcvwin", "rcvwinscale", "sndwinscale", "seqnum", "connid" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return size;
                case 1:
                    return daddr;
                case 2:
                    return saddr;
                case 3:
                    return dport;
                case 4:
                    return sport;
                case 5:
                    return mss;
                case 6:
                    return sackopt;
                case 7:
                    return tsopt;
                case 8:
                    return wsopt;
                case 9:
                    return rcvwin;
                case 10:
                    return rcvwinscale;
                case 11:
                    return sndwinscale;
                case 12:
                    return seqnum;
                case 13:
                    return connid;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TcpIpConnectTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class TcpIpV6TraceData : TraceEvent
    {
        // PID
        public int size { get { return GetInt32At(4); } }
        public System.Net.IPAddress daddr { get { return GetIPAddrV6At(8); } }
        public System.Net.IPAddress saddr { get { return GetIPAddrV6At(24); } }
        public int dport { get { return (ushort)GetInt16At(40); } }
        public int sport { get { return (ushort)GetInt16At(42); } }
        public Address connid { get { return GetHostPointer(44); } }
        public int seqnum { get { return GetInt32At(HostOffset(48, 1)); } }

        #region Private
        internal TcpIpV6TraceData(Action<TcpIpV6TraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version >= 1 && EventDataLength < HostOffset(52, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("size", size);
            sb.XmlAttrib("daddr", daddr);
            sb.XmlAttrib("saddr", saddr);
            sb.XmlAttribHex("dport", dport);
            sb.XmlAttribHex("sport", sport);
            sb.XmlAttrib("connid", connid);
            sb.XmlAttrib("seqnum", seqnum);
            sb.Append("/>");
            return sb;
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "size", "daddr", "saddr", "dport", "sport", "connid", "seqnum" };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return size;
                case 1:
                    return daddr;
                case 2:
                    return saddr;
                case 3:
                    return dport;
                case 4:
                    return sport;
                case 5:
                    return connid;
                case 6:
                    return seqnum;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }
        private event Action<TcpIpV6TraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class TcpIpV6SendTraceData : TraceEvent
    {
        // PID
        public int size { get { return GetInt32At(4); } }
        public System.Net.IPAddress daddr { get { return GetIPAddrV6At(8); } }
        public System.Net.IPAddress saddr { get { return GetIPAddrV6At(24); } }
        public int dport { get { return (ushort)GetInt16At(40); } }
        public int sport { get { return (ushort)GetInt16At(42); } }
        public int startime { get { return GetInt32At(44); } }
        public int endtime { get { return GetInt32At(48); } }
        public int seqnum { get { return GetInt32At(52); } }
        public Address connid { get { return GetHostPointer(56); } }

        #region Private
        internal TcpIpV6SendTraceData(Action<TcpIpV6SendTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(60, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(60, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("size", size);
            sb.XmlAttrib("daddr", daddr);
            sb.XmlAttrib("saddr", saddr);
            sb.XmlAttribHex("dport", dport);
            sb.XmlAttribHex("sport", sport);
            sb.XmlAttribHex("startime", startime);
            sb.XmlAttribHex("endtime", endtime);
            sb.XmlAttrib("seqnum", seqnum);
            sb.XmlAttrib("connid", connid);
            sb.Append("/>");
            return sb;
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "size", "daddr", "saddr", "dport", "sport", "startime", "endtime", "seqnum", "connid", };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return size;
                case 1:
                    return daddr;
                case 2:
                    return saddr;
                case 3:
                    return dport;
                case 4:
                    return sport;
                case 5:
                    return startime;
                case 6:
                    return endtime;
                case 7:
                    return seqnum;
                case 8:
                    return connid;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }
        private event Action<TcpIpV6SendTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class TcpIpV6ConnectTraceData : TraceEvent
    {
        // PID
        public int size { get { return GetInt32At(4); } }
        public System.Net.IPAddress daddr { get { return GetIPAddrV6At(8); } }
        public System.Net.IPAddress saddr { get { return GetIPAddrV6At(24); } }
        public int dport { get { return (ushort)GetInt16At(40); } }
        public int sport { get { return (ushort)GetInt16At(42); } }
        public int mss { get { return GetInt16At(44); } }
        public int sackopt { get { return GetInt16At(46); } }
        public int tsopt { get { return GetInt16At(48); } }
        public int wsopt { get { return GetInt16At(50); } }
        public int rcvwin { get { return GetInt32At(52); } }
        public int rcvwinscale { get { return GetInt16At(56); } }
        public int sndwinscale { get { return GetInt16At(58); } }
        public int seqnum { get { return GetInt32At(60); } }
        public Address connid { get { return GetHostPointer(64); } }

        #region Private
        internal TcpIpV6ConnectTraceData(Action<TcpIpV6ConnectTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(68, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(68, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("size", size);
            sb.XmlAttrib("dport", dport);
            sb.XmlAttrib("sport", sport);
            sb.XmlAttrib("mss", mss);
            sb.XmlAttrib("sackopt", sackopt);
            sb.XmlAttrib("tsopt", tsopt);
            sb.XmlAttrib("wsopt", wsopt);
            sb.XmlAttrib("rcvwin", rcvwin);
            sb.XmlAttrib("rcvwinscale", rcvwinscale);
            sb.XmlAttrib("sndwinscale", sndwinscale);
            sb.XmlAttrib("seqnum", seqnum);
            sb.XmlAttrib("connid", connid);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "size", "dport", "sport", "mss", "sackopt", "tsopt", "wsopt", "rcvwin", "rcvwinscale", "sndwinscale", "seqnum", "connid" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return size;
                case 1:
                    return dport;
                case 2:
                    return sport;
                case 3:
                    return mss;
                case 4:
                    return sackopt;
                case 5:
                    return tsopt;
                case 6:
                    return wsopt;
                case 7:
                    return rcvwin;
                case 8:
                    return rcvwinscale;
                case 9:
                    return sndwinscale;
                case 10:
                    return seqnum;
                case 11:
                    return connid;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TcpIpV6ConnectTraceData> Action;
        private KernelTraceEventParserState state;
        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class UdpIpTraceData : TraceEvent
    {
        public Address context { get { return GetHostPointer(0); } }
        public int saddr { get { if (Version >= 1) return GetInt32At(12); return GetInt32At(HostOffset(4, 1)); } }
        public int sport { get { if (Version >= 1) return GetInt16At(18); return GetInt16At(HostOffset(8, 1)); } }
        public int size { get { if (Version >= 1) return GetInt32At(4); return GetInt16At(HostOffset(10, 1)); } }
        public int daddr { get { if (Version >= 1) return GetInt32At(8); return GetInt32At(HostOffset(12, 1)); } }
        public int dport { get { if (Version >= 1) return GetInt16At(16); return GetInt16At(HostOffset(16, 1)); } }
        public int dsize { get { return GetInt16At(HostOffset(18, 1)); } }
        // PID
        #region Private
        internal UdpIpTraceData(Action<UdpIpTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < HostOffset(20, 1)));   // TODO fixed by hand
            Debug.Assert(!(Version == 1 && EventDataLength < 20));                  // TODO fixed by hand
            Debug.Assert(!(Version > 1 && EventDataLength < 20));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("context", context);
            sb.XmlAttrib("saddr", saddr);
            sb.XmlAttrib("sport", sport);
            sb.XmlAttrib("size", size);
            sb.XmlAttrib("daddr", daddr);
            sb.XmlAttrib("dport", dport);
            sb.XmlAttrib("dsize", dsize);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "context", "saddr", "sport", "size", "daddr", "dport", "dsize" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return context;
                case 1:
                    return saddr;
                case 2:
                    return sport;
                case 3:
                    return size;
                case 4:
                    return daddr;
                case 5:
                    return dport;
                case 6:
                    return dsize;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<UdpIpTraceData> Action;
        private KernelTraceEventParserState state;
        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            if (Version >= 1)
                eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class UdpIpFailTraceData : TraceEvent
    {
        public int Proto { get { return GetInt16At(0); } }
        public int FailureCode { get { return GetInt16At(2); } }

        #region Private
        internal UdpIpFailTraceData(Action<UdpIpFailTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != 4));
            Debug.Assert(!(Version > 2 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("Proto", Proto);
            sb.XmlAttrib("FailureCode", FailureCode);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Proto", "FailureCode" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Proto;
                case 1:
                    return FailureCode;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<UdpIpFailTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class UpdIpV6TraceData : TraceEvent
    {
        // PID
        public int size { get { return GetInt32At(4); } }
        public System.Net.IPAddress daddr { get { return GetIPAddrV6At(8); } }
        public System.Net.IPAddress saddr { get { return GetIPAddrV6At(24); } }
        public int dport { get { return (ushort)GetInt16At(40); } }
        public int sport { get { return (ushort)GetInt16At(42); } }
        public int seqnum { get { return GetInt32At(44); } }
        public Address connid { get { return GetHostPointer(48); } }

        #region Private
        internal UpdIpV6TraceData(Action<UpdIpV6TraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(52, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(52, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("size", size);
            sb.XmlAttrib("dport", dport);
            sb.XmlAttrib("sport", sport);
            sb.XmlAttrib("seqnum", seqnum);
            sb.XmlAttrib("connid", connid);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "size", "dport", "sport", "seqnum", "connid" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return size;
                case 1:
                    return dport;
                case 2:
                    return sport;
                case 3:
                    return seqnum;
                case 4:
                    return connid;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<UpdIpV6TraceData> Action;
        private KernelTraceEventParserState state;
        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }
        #endregion
    }
    public sealed class ImageLoadTraceData : TraceEvent
    {
        public Address ImageBase { get { return GetHostPointer(0); } }
        public int ImageSize { get { return (int)GetHostPointer(HostOffset(4, 1)); } }
        // public int ProcessID { get { if (Version >= 1) return GetInt32At(HostOffset(8, 2)); return 0; } }
        public int ImageChecksum { get { if (Version >= 2) return GetInt32At(HostOffset(12, 2)); return 0; } }
        public int TimeDateStamp { get { if (Version >= 2) return GetInt32At(HostOffset(16, 2)); return 0; } }
        // Skipping Reserved0
        public Address DefaultBase { get { if (Version >= 2) return GetHostPointer(HostOffset(24, 2)); return 0; } }
        // Skipping Reserved1
        // Skipping Reserved2
        // Skipping Reserved3
        // Skipping Reserved4
        public string KernelFileName { get { if (Version >= 2) return GetUnicodeStringAt(HostOffset(44, 3)); if (Version >= 1) return GetUnicodeStringAt(HostOffset(12, 2)); return ""; } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }

        #region Private
        internal ImageLoadTraceData(Action<ImageLoadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < SkipUnicodeString(HostOffset(8, 1))));
            Debug.Assert(!(Version == 1 && EventDataLength < SkipUnicodeString(HostOffset(12, 2))));
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(HostOffset(44, 3))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(HostOffset(44, 3))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("ImageBase", ImageBase);
            sb.XmlAttribHex("ImageSize", ImageSize);
            sb.XmlAttrib("ImageChecksum", ImageChecksum);
            sb.XmlAttrib("TimeDateStamp", TimeDateStamp);
            sb.XmlAttribHex("DefaultBase", DefaultBase);
            sb.XmlAttrib("FileName", FileName);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ImageBase", "ImageSize", "ImageChecksum", "TimeDateStamp", "DefaultBase", "FileName" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ImageBase;
                case 1:
                    return ImageSize;
                case 2:
                    return ImageChecksum;
                case 3:
                    return TimeDateStamp;
                case 4:
                    return DefaultBase;
                case 5:
                    return FileName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ImageLoadTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            // We wish to create the illusion that the events are reported by the process where it is loaded. 
            // This it not actually true for DCStart and DCEnd, and Stop events, so we fix it up.  
            if (Opcode == TraceEventOpcode.DataCollectionStart ||
                Opcode == TraceEventOpcode.DataCollectionStop || Opcode == TraceEventOpcode.Stop)
            {
                eventRecord->EventHeader.ThreadId = -1;     // DCStarts and DCEnds have no useful thread.
                if (eventRecord->EventHeader.Version >= 1)
                    eventRecord->EventHeader.ProcessId = GetInt32At(HostOffset(8, 2));
            }
            Debug.Assert(eventRecord->EventHeader.Version == 0 || eventRecord->EventHeader.ProcessId == GetInt32At(HostOffset(8, 2)));
        }
        #endregion
    }
    public sealed class PageFaultTraceData : TraceEvent
    {
        public Address VirtualAddress { get { return GetHostPointer(0); } }
        public Address ProgramCounter { get { return GetHostPointer(HostOffset(4, 1)); } }

        #region Private
        internal PageFaultTraceData(Action<PageFaultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("VirtualAddress", VirtualAddress);
            sb.XmlAttribHex("ProgramCounter", ProgramCounter);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "VirtualAddress", "ProgramCounter" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return VirtualAddress;
                case 1:
                    return ProgramCounter;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultTraceData> Action;
        private KernelTraceEventParserState state;

        /// <summary>
        /// Indicate that ProgramCounter is a code address that needs symbolic information
        /// </summary>
        protected internal override void LogCodeAddresses(Action<TraceEvent, Address> callBack)
        {
            callBack(this, ProgramCounter);
        }
        #endregion
    }
    public sealed class PageFaultHardFaultTraceData : TraceEvent
    {
        public long InitialTime100ns { get { return GetInt64At(0); } }
        public DateTime InitialTime { get { return DateTime.FromFileTime(InitialTime100ns); } }
        public long ReadOffset { get { return GetInt64At(8); } }
        public Address VirtualAddress { get { return GetHostPointer(16); } }
        public Address FileObject { get { return GetHostPointer(HostOffset(20, 1)); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        // public int TThreadID { get { return GetInt32At(HostOffset(24, 2)); } }
        public int ByteCount { get { return GetInt32At(HostOffset(28, 2)); } }

        #region Private
        internal PageFaultHardFaultTraceData(Action<PageFaultHardFaultTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(GetInt32At(HostOffset(24, 2)) == ThreadID);    // TThreadID == ThreadID
            Debug.Assert(!(Version >= 0 && EventDataLength < HostOffset(32, 2)));
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(32, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(32, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("InitialTime", InitialTime);
            sb.XmlAttribHex("ReadOffset", ReadOffset);
            sb.XmlAttribHex("VirtualAddress", VirtualAddress);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttrib("ByteCount", ByteCount);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "InitialTime", "ReadOffset", "VirtualAddress", "FileObject", "ByteCount" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return InitialTime;
                case 1:
                    return ReadOffset;
                case 2:
                    return VirtualAddress;
                case 3:
                    return FileObject;
                case 4:
                    return ByteCount;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultHardFaultTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ThreadId == -1);
            eventRecord->EventHeader.ThreadId = GetInt32At(HostOffset(0x18, 2));
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = state.ThreadIDToProcessID(ThreadID, TimeStamp100ns);
        }

        #endregion
    }
    public sealed class PageFaultHeapRangeRundownTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public int HRFlags { get { return GetInt32At(HostOffset(4, 1)); } }
        // HRPid 
        public int HRRangeCount { get { return GetInt32At(HostOffset(12, 1)); } }

        #region Private
        internal PageFaultHeapRangeRundownTraceData(Action<PageFaultHeapRangeRundownTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(16, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(16, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("HRFlags", HRFlags);
            sb.XmlAttrib("HRRangeCount", HRRangeCount);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "HRFlags", "HRRangeCount" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return HRFlags;
                case 2:
                    return HRRangeCount;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultHeapRangeRundownTraceData> Action;
        private KernelTraceEventParserState state;

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(HostOffset(8, 1));
        }
        #endregion
    }
    public sealed class PageFaultHeapRangeCreateTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address FirstRangeSize { get { return GetHostPointer(HostOffset(4, 1)); } }
        public int HRCreateFlags { get { return GetInt32At(HostOffset(8, 2)); } }

        #region Private
        internal PageFaultHeapRangeCreateTraceData(Action<PageFaultHeapRangeCreateTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(12, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(12, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("FirstRangeSize", FirstRangeSize);
            sb.XmlAttribHex("HRCreateFlags", HRCreateFlags);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "FirstRangeSize", "HRCreateFlags" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return FirstRangeSize;
                case 2:
                    return HRCreateFlags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultHeapRangeCreateTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class PageFaultHeapRangeTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address HRAddress { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address HRSize { get { return GetHostPointer(HostOffset(8, 2)); } }

        #region Private
        internal PageFaultHeapRangeTraceData(Action<PageFaultHeapRangeTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(12, 3)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(12, 3)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("HRAddress", HRAddress);
            sb.XmlAttribHex("HRSize", HRSize);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "HRAddress", "HRSize" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return HRAddress;
                case 2:
                    return HRSize;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultHeapRangeTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class PageFaultHeapRangeDestroyTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }

        #region Private
        internal PageFaultHeapRangeDestroyTraceData(Action<PageFaultHeapRangeDestroyTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultHeapRangeDestroyTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class PageFaultImageLoadBackedTraceData : TraceEvent
    {
        public Address FileObject { get { return GetHostPointer(0); } }
        public string KernelFileName { get { return state.FileIDToName(FileObject, TimeStamp100ns); } }
        public string FileName { get { return state.KernelToUser(KernelFileName); } }
        public int DeviceChar { get { return GetInt32At(HostOffset(4, 1)); } }
        public int FileChar { get { return GetInt16At(HostOffset(8, 1)); } }
        public int LoadFlags { get { return GetInt16At(HostOffset(10, 1)); } }

        #region Private
        internal PageFaultImageLoadBackedTraceData(Action<PageFaultImageLoadBackedTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(12, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(12, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("FileObject", FileObject);
            sb.XmlAttribHex("DeviceChar", DeviceChar);
            sb.XmlAttribHex("FileChar", FileChar);
            sb.XmlAttribHex("LoadFlags", LoadFlags);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "FileObject", "DeviceChar", "FileChar", "LoadFlags" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return FileObject;
                case 1:
                    return DeviceChar;
                case 2:
                    return FileChar;
                case 3:
                    return LoadFlags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<PageFaultImageLoadBackedTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SampledProfileTraceData : TraceEvent
    {
        public Address InstructionPointer { get { return GetHostPointer(0); } }
        // public int ThreadID { get { return GetInt32At(HostOffset(4, 1)); } }
        public int Count { get { return GetInt32At(HostOffset(8, 1)); } }

        #region Private
        internal SampledProfileTraceData(Action<SampledProfileTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(12, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(12, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("InstructionPointer", InstructionPointer);
            sb.XmlAttrib("ThreadID", ThreadID);
            sb.XmlAttrib("Count", Count);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "InstructionPointer", "ThreadID", "Count" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return InstructionPointer;
                case 1:
                    return ThreadID;
                case 2:
                    return Count;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SampledProfileTraceData> Action;
        private KernelTraceEventParserState state;

        /// <summary>
        /// Indicate that SystemCallAddress is a code address that needs symbolic information
        /// </summary>
        protected internal override void LogCodeAddresses(Action<TraceEvent, Address> callBack)
        {
            callBack(this, InstructionPointer);
        }

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ThreadId == -1);
            eventRecord->EventHeader.ThreadId = GetInt32At(HostOffset(4, 1));
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = state.ThreadIDToProcessID(ThreadID, TimeStamp100ns);
        }
        #endregion
    }

    public sealed class BatchedSampledProfileTraceData : TraceEvent
    {
        /// <summary>
        /// A BatchedSampleProfile contains many samples in a single payload.  The batchCount
        /// indicates the number of samples in this payload.  Each sample has a
        /// InstructionPointer, ThreadID and InstanceCount
        /// </summary>
        public int BatchCount { get { return GetInt32At(0); } }
        /// <summary>
        /// The instruction pointer assocaited with this sample 
        /// </summary>
        [CLSCompliant(false)]
        public Address InstructionPointer(int i)
        {
            Debug.Assert(0 <= i && i < BatchCount);
            int ptrSize = PointerSize;
            return GetHostPointer(4 + i * (ptrSize + 8));
        }
        /// <summary>
        /// The thread ID associatd with the sample 
        /// </summary>
        public int InstanceThreadID(int i)
        {
            Debug.Assert(0 <= i && i < BatchCount);
            int ptrSize = PointerSize;
            return GetInt32At(4 + ptrSize + i * (ptrSize + 8));
        }

        /// <summary>
        /// Each sample may represent mulitiple instances of samples with the same Instruction
        /// Pointer and ThreadID.  
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int InstanceCount(int i)
        {
            Debug.Assert(0 <= i && i < BatchCount);
            int ptrSize = PointerSize;
            return GetInt32At(4 + 4 + ptrSize + i * (ptrSize + 8));
        }

        #region Private
        internal BatchedSampledProfileTraceData(Action<BatchedSampledProfileTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(EventDataLength == BatchCount * HostOffset(12, 1) + 4);
            Action(this);
        }

        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb).XmlAttrib("BatchCount", BatchCount).Append(">").AppendLine();
            for (int i = 0; i < BatchCount; i++)
            {
                sb.Append("   <Sample");
                sb.XmlAttribHex("InstructionPointer", InstructionPointer(i));
                sb.XmlAttrib("InstanceThreadID", InstanceThreadID(i));
                sb.XmlAttrib("InstanceCount", InstanceCount(i));
                sb.Append("/>").AppendLine();
            }
            sb.Append("</Event>");
            return sb;
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[0];
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            return null;
        }

        private event Action<BatchedSampledProfileTraceData> Action;
        private KernelTraceEventParserState state;

        /// <summary>
        /// Indicate that SystemCallAddress is a code address that needs symbolic information
        /// </summary>
        protected internal override void LogCodeAddresses(Action<TraceEvent, Address> callBack)
        {
            for (int i = 0; i < BatchCount; i++)
                callBack(this, InstructionPointer(i));
        }
        #endregion
    }

    public sealed class SampledProfileIntervalTraceData : TraceEvent
    {
        public int SampleSource { get { return GetInt32At(0); } }
        public int NewInterval { get { return GetInt32At(4); } }
        public int OldInterval { get { return GetInt32At(8); } }

        #region Private
        internal SampledProfileIntervalTraceData(Action<SampledProfileIntervalTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != 12));
            Debug.Assert(!(Version > 2 && EventDataLength < 12));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("SampleSource", SampleSource);
            sb.XmlAttrib("NewInterval", NewInterval);
            sb.XmlAttrib("OldInterval", OldInterval);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "SampleSource", "NewInterval", "OldInterval" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return SampleSource;
                case 1:
                    return NewInterval;
                case 2:
                    return OldInterval;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SampledProfileIntervalTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SysCallEnterTraceData : TraceEvent
    {
        public Address SysCallAddress { get { return GetHostPointer(0); } }

        #region Private
        internal SysCallEnterTraceData(Action<SysCallEnterTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("SysCallAddress", SysCallAddress);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "SysCallAddress" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return SysCallAddress;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SysCallEnterTraceData> Action;
        private KernelTraceEventParserState state;

        /// <summary>
        /// Indicate that SystemCallAddress is a code address that needs symbolic information
        /// </summary>
        protected internal override void LogCodeAddresses(Action<TraceEvent, Address> callBack)
        {
            callBack(this, SysCallAddress);
        }
        #endregion
    }
    public sealed class SysCallExitTraceData : TraceEvent
    {
        public int SysCallNtStatus { get { return GetInt32At(0); } }

        #region Private
        internal SysCallExitTraceData(Action<SysCallExitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != 4));
            Debug.Assert(!(Version > 2 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("SysCallNtStatus", SysCallNtStatus);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "SysCallNtStatus" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return SysCallNtStatus;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SysCallExitTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ISRTraceData : TraceEvent
    {
        public long InitialTime100ns { get { return GetInt64At(0); } }
        public DateTime InitialTime { get { return DateTime.FromFileTime(InitialTime100ns); } }
        public Address Routine { get { return GetHostPointer(8); } }
        public int ReturnValue { get { return GetByteAt(HostOffset(12, 1)); } }
        public int Vector { get { return GetByteAt(HostOffset(13, 1)); } }
        // Skipping Reserved

        #region Private
        internal ISRTraceData(Action<ISRTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(16, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(16, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("InitialTime", InitialTime);
            sb.XmlAttribHex("Routine", Routine);
            sb.XmlAttrib("ReturnValue", ReturnValue);
            sb.XmlAttrib("Vector", Vector);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "InitialTime", "Routine", "ReturnValue", "Vector" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return InitialTime;
                case 1:
                    return Routine;
                case 2:
                    return ReturnValue;
                case 3:
                    return Vector;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ISRTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class DPCTraceData : TraceEvent
    {
        public long InitialTime100ns { get { return GetInt64At(0); } }
        public DateTime InitialTime { get { return DateTime.FromFileTime(InitialTime100ns); } }
        public Address Routine { get { return GetHostPointer(8); } }

        #region Private
        internal DPCTraceData(Action<DPCTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(12, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(12, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("InitialTime", InitialTime);
            sb.XmlAttribHex("Routine", Routine);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "InitialTime", "Routine" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return InitialTime;
                case 1:
                    return Routine;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<DPCTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }

    /// <summary>
    /// Collects the call callStacks for some other event.  
    /// 
    /// (TODO: always for the event that preceeded it on the same thread)?  
    /// </summary>
    public sealed class StackWalkTraceData : TraceEvent
    {
        /// <summary>
        /// The timestamp of the event which caused this stack walk using QueryPerformaceCounter
        /// cycles as the tick.
        /// </summary>
        public long EventTimeStampQPC { get { return GetInt64At(0); } }

        /// <summary>
        /// The total number of eventToStack frames collected.  The Windows OS currently has a maximum of 96 frames. 
        /// </summary>
        public int FrameCount { get { return (EventDataLength - 0x10) / PointerSize; } }
        /// <summary>
        /// Fetches the instruction pointer of a eventToStack frame 0 is the deepest frame, and the maximum should
        /// be a thread offset routine (if you get a complete eventToStack).  
        /// </summary>
        /// <param name="i">The index of the frame to fetch.  0 is the CPU EIP, 1 is the Caller of that
        /// routine ...</param>
        /// <returns>The instruction pointer of the specified frame.</returns>
        [CLSCompliant(false)]
        public Address InstructionPointer(int i)
        {
            Debug.Assert(0 <= i && i < FrameCount);
            return GetHostPointer(16 + i * PointerSize);
        }
        #region Private
        internal StackWalkTraceData(Action<StackWalkTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(FrameCount >= 0);
            Action(this);
        }

        /// <summary>
        /// StackWalkTraceData does not set Thread and process ID fields properly.  if that.  
        /// </summary>
        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ThreadId == -1);
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ThreadId = GetInt32At(0xC);
            eventRecord->EventHeader.ProcessId = GetInt32At(8);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb).XmlAttrib("EventTimeStampQPC", EventTimeStampQPC).XmlAttrib("FrameCount", FrameCount).AppendLine(">");
            for (int i = 0; i < FrameCount; i++)
            {
                sb.Append("  ");
                sb.Append("0x").Append(((ulong)InstructionPointer(i)).ToString("x"));
            }
            sb.AppendLine();
            sb.Append("</Event>");
            return sb;
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[0];
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            return null;
        }
        public event Action<StackWalkTraceData> Action;

        private KernelTraceEventParserState state;
        #endregion
    }

    public sealed class ALPCSendMessageTraceData : TraceEvent
    {
        public int MessageID { get { return GetInt32At(0); } }

        #region Private
        internal ALPCSendMessageTraceData(Action<ALPCSendMessageTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version > 0 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("MessageID", MessageID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MessageID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MessageID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ALPCSendMessageTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ALPCReceiveMessageTraceData : TraceEvent
    {
        public int MessageID { get { return GetInt32At(0); } }

        #region Private
        internal ALPCReceiveMessageTraceData(Action<ALPCReceiveMessageTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version > 0 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("MessageID", MessageID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MessageID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MessageID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ALPCReceiveMessageTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ALPCWaitForReplyTraceData : TraceEvent
    {
        public int MessageID { get { return GetInt32At(0); } }

        #region Private
        internal ALPCWaitForReplyTraceData(Action<ALPCWaitForReplyTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version > 0 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("MessageID", MessageID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MessageID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MessageID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ALPCWaitForReplyTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ALPCWaitForNewMessageTraceData : TraceEvent
    {
        public int IsServerPort { get { return GetInt32At(0); } }
        public string PortName { get { return GetAsciiStringAt(4); } }

        #region Private
        internal ALPCWaitForNewMessageTraceData(Action<ALPCWaitForNewMessageTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipAsciiString(4)));
            Debug.Assert(!(Version > 0 && EventDataLength < SkipAsciiString(4)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("IsServerPort", IsServerPort);
            sb.XmlAttrib("PortName", PortName);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IsServerPort", "PortName" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IsServerPort;
                case 1:
                    return PortName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ALPCWaitForNewMessageTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class ALPCUnwaitTraceData : TraceEvent
    {
        public int Status { get { return GetInt32At(0); } }

        #region Private
        internal ALPCUnwaitTraceData(Action<ALPCUnwaitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 4));
            Debug.Assert(!(Version > 0 && EventDataLength < 4));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("Status", Status);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Status" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return Status;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<ALPCUnwaitTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigCPUTraceData : TraceEvent
    {
        public int MHz { get { return GetInt32At(0); } }
        public int NumberOfProcessors { get { return GetInt32At(4); } }
        public int MemSize { get { return GetInt32At(8); } }
        public int PageSize { get { return GetInt32At(12); } }
        public int AllocationGranularity { get { return GetInt32At(16); } }
        public string ComputerName { get { return GetFixedUnicodeStringAt(256, (20)); } }
        public string DomainName { get { if (Version >= 2) return GetFixedUnicodeStringAt(134, (532)); return GetFixedUnicodeStringAt(132, (532)); } }
        public Address HyperThreadingFlag { get { if (Version >= 2) return GetHostPointer(800); return GetHostPointer(796); } }

        #region Private
        internal SystemConfigCPUTraceData(Action<SystemConfigCPUTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < HostOffset(800, 1)));     // TODO hand changed
            Debug.Assert(!(Version == 1 && EventDataLength < HostOffset(800, 1)));     // TODO hand changed
            Debug.Assert(!(Version == 2 && EventDataLength != HostOffset(804, 1)));
            Debug.Assert(!(Version > 2 && EventDataLength < HostOffset(804, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("MHz", MHz);
            sb.XmlAttrib("NumberOfProcessors", NumberOfProcessors);
            sb.XmlAttrib("MemSize", MemSize);
            sb.XmlAttrib("PageSize", PageSize);
            sb.XmlAttrib("AllocationGranularity", AllocationGranularity);
            sb.XmlAttrib("ComputerName", ComputerName);
            sb.XmlAttrib("DomainName", DomainName);
            sb.XmlAttribHex("HyperThreadingFlag", HyperThreadingFlag);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MHz", "NumberOfProcessors", "MemSize", "PageSize", "AllocationGranularity", "ComputerName", "DomainName", "HyperThreadingFlag" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MHz;
                case 1:
                    return NumberOfProcessors;
                case 2:
                    return MemSize;
                case 3:
                    return PageSize;
                case 4:
                    return AllocationGranularity;
                case 5:
                    return ComputerName;
                case 6:
                    return DomainName;
                case 7:
                    return HyperThreadingFlag;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigCPUTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigPhyDiskTraceData : TraceEvent
    {
        public int DiskNumber { get { return GetInt32At(0); } }
        public int BytesPerSector { get { return GetInt32At(4); } }
        public int SectorsPerTrack { get { return GetInt32At(8); } }
        public int TracksPerCylinder { get { return GetInt32At(12); } }
        public long Cylinders { get { return GetInt64At(16); } }
        public int SCSIPort { get { return GetInt32At(24); } }
        public int SCSIPath { get { return GetInt32At(28); } }
        public int SCSITarget { get { return GetInt32At(32); } }
        public int SCSILun { get { return GetInt32At(36); } }
        public string Manufacturer { get { return GetFixedUnicodeStringAt(256, (40)); } }
        public int PartitionCount { get { return GetInt32At(552); } }
        public int WriteCacheEnabled { get { return GetByteAt(556); } }
        // Skipping Pad
        public string BootDriveLetter { get { return GetFixedUnicodeStringAt(3, (558)); } }
        public string Spare { get { return GetFixedUnicodeStringAt(2, (564)); } }

        #region Private
        internal SystemConfigPhyDiskTraceData(Action<SystemConfigPhyDiskTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 568));    // TODO changed by hand
            Debug.Assert(!(Version == 1 && EventDataLength < 568));    // TODO changed by hand
            Debug.Assert(!(Version == 2 && EventDataLength != 568));
            Debug.Assert(!(Version > 2 && EventDataLength < 568));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("DiskNumber", DiskNumber);
            sb.XmlAttrib("BytesPerSector", BytesPerSector);
            sb.XmlAttrib("SectorsPerTrack", SectorsPerTrack);
            sb.XmlAttrib("TracksPerCylinder", TracksPerCylinder);
            sb.XmlAttrib("Cylinders", Cylinders);
            sb.XmlAttrib("SCSIPort", SCSIPort);
            sb.XmlAttrib("SCSIPath", SCSIPath);
            sb.XmlAttrib("SCSITarget", SCSITarget);
            sb.XmlAttrib("SCSILun", SCSILun);
            sb.XmlAttrib("Manufacturer", Manufacturer);
            sb.XmlAttrib("PartitionCount", PartitionCount);
            sb.XmlAttrib("WriteCacheEnabled", WriteCacheEnabled);
            sb.XmlAttrib("BootDriveLetter", BootDriveLetter);
            sb.XmlAttrib("Spare", Spare);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "DiskNumber", "BytesPerSector", "SectorsPerTrack", "TracksPerCylinder", "Cylinders", "SCSIPort", "SCSIPath", "SCSITarget", "SCSILun", "Manufacturer", "PartitionCount", "WriteCacheEnabled", "BootDriveLetter", "Spare" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return DiskNumber;
                case 1:
                    return BytesPerSector;
                case 2:
                    return SectorsPerTrack;
                case 3:
                    return TracksPerCylinder;
                case 4:
                    return Cylinders;
                case 5:
                    return SCSIPort;
                case 6:
                    return SCSIPath;
                case 7:
                    return SCSITarget;
                case 8:
                    return SCSILun;
                case 9:
                    return Manufacturer;
                case 10:
                    return PartitionCount;
                case 11:
                    return WriteCacheEnabled;
                case 12:
                    return BootDriveLetter;
                case 13:
                    return Spare;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigPhyDiskTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigLogDiskTraceData : TraceEvent
    {
        public long StartOffset { get { return GetInt64At(0); } }
        public long PartitionSize { get { return GetInt64At(8); } }
        public int DiskNumber { get { return GetInt32At(16); } }
        public int Size { get { return GetInt32At(20); } }
        public int DriveType { get { return GetInt32At(24); } }
        public string DriveLetterString { get { return GetFixedUnicodeStringAt(4, (28)); } }
        // Skipping Pad1
        public int PartitionNumber { get { return GetInt32At(40); } }
        public int SectorsPerCluster { get { return GetInt32At(44); } }
        public int BytesPerSector { get { return GetInt32At(48); } }
        // Skipping Pad2
        public long NumberOfFreeClusters { get { return GetInt64At(56); } }
        public long TotalNumberOfClusters { get { return GetInt64At(64); } }
        public string FileSystem { get { return GetFixedUnicodeStringAt(16, (72)); } }
        public int VolumeExt { get { return GetInt32At(104); } }
        // Skipping Pad3

        #region Private
        internal SystemConfigLogDiskTraceData(Action<SystemConfigLogDiskTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 108));        // TODO fixed by hand
            Debug.Assert(!(Version == 1 && EventDataLength < 108));        // TODO fixed by hand
            Debug.Assert(!(Version == 2 && EventDataLength != 112));
            Debug.Assert(!(Version > 2 && EventDataLength < 112));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("StartOffset", StartOffset);
            sb.XmlAttrib("PartitionSize", PartitionSize);
            sb.XmlAttrib("DiskNumber", DiskNumber);
            sb.XmlAttrib("Size", Size);
            sb.XmlAttrib("DriveType", DriveType);
            sb.XmlAttrib("DriveLetterString", DriveLetterString);
            sb.XmlAttrib("PartitionNumber", PartitionNumber);
            sb.XmlAttrib("SectorsPerCluster", SectorsPerCluster);
            sb.XmlAttrib("BytesPerSector", BytesPerSector);
            sb.XmlAttrib("NumberOfFreeClusters", NumberOfFreeClusters);
            sb.XmlAttrib("TotalNumberOfClusters", TotalNumberOfClusters);
            sb.XmlAttrib("FileSystem", FileSystem);
            sb.XmlAttrib("VolumeExt", VolumeExt);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "StartOffset", "PartitionSize", "DiskNumber", "Size", "DriveType", "DriveLetterString", "PartitionNumber", "SectorsPerCluster", "BytesPerSector", "NumberOfFreeClusters", "TotalNumberOfClusters", "FileSystem", "VolumeExt" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return StartOffset;
                case 1:
                    return PartitionSize;
                case 2:
                    return DiskNumber;
                case 3:
                    return Size;
                case 4:
                    return DriveType;
                case 5:
                    return DriveLetterString;
                case 6:
                    return PartitionNumber;
                case 7:
                    return SectorsPerCluster;
                case 8:
                    return BytesPerSector;
                case 9:
                    return NumberOfFreeClusters;
                case 10:
                    return TotalNumberOfClusters;
                case 11:
                    return FileSystem;
                case 12:
                    return VolumeExt;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigLogDiskTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class SystemConfigNICTraceData : TraceEvent
    {
        public int PhysicalAddrLen { get { if (Version >= 2) return GetInt32At(8); return GetInt32At(516); } }
        public long PhysicalAddr { get { if (Version >= 2) return GetInt64At(0); return 0; } }
        public int Ipv4Index { get { if (Version >= 2) return GetInt32At(12); return GetInt32At(512); ; } }
        public int Ipv6Index { get { if (Version >= 2) return GetInt32At(16); return 0; } }
        public string NICDescription { get { if (Version >= 2) return GetUnicodeStringAt(20); return GetFixedUnicodeStringAt(256, (0)); } }
        public string IpAddresses { get { if (Version >= 2) return GetUnicodeStringAt(SkipUnicodeString(20)); return ""; } }
        public string DnsServerAddresses { get { if (Version >= 2) return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(20))); return ""; } }

        #region Private
        internal SystemConfigNICTraceData(Action<SystemConfigNICTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 584));    // TODO changed by hand
            Debug.Assert(!(Version == 1 && EventDataLength < 584));    // TODO changed by  hand
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(20)))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(20)))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("PhysicalAddrLen", PhysicalAddrLen);
            sb.XmlAttrib("PhysicalAddr", PhysicalAddr);
            sb.XmlAttrib("Ipv4Index", Ipv4Index);
            sb.XmlAttrib("Ipv6Index", Ipv6Index);
            sb.XmlAttrib("NICDescription", NICDescription);
            sb.XmlAttrib("IpAddresses", IpAddresses);
            sb.XmlAttrib("DnsServerAddresses", DnsServerAddresses);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "PhysicalAddrLen", "PhysicalAddr", "Ipv4Index", "Ipv6Index", "NICDescription", "IpAddresses", "DnsServerAddresses" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return PhysicalAddrLen;
                case 1:
                    return PhysicalAddr;
                case 2:
                    return Ipv4Index;
                case 3:
                    return Ipv6Index;
                case 4:
                    return NICDescription;
                case 5:
                    return IpAddresses;
                case 6:
                    return DnsServerAddresses;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigNICTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    [CLSCompliant(false)]
    public sealed class SystemConfigVideoTraceData : TraceEvent
    {
        public int MemorySize { get { return GetInt32At(0); } }
        public int XResolution { get { return GetInt32At(4); } }
        public int YResolution { get { return GetInt32At(8); } }
        public int BitsPerPixel { get { return GetInt32At(12); } }
        public int VRefresh { get { return GetInt32At(16); } }
        public string ChipType { get { return GetFixedUnicodeStringAt(256, (20)); } }
        public string DACType { get { return GetFixedUnicodeStringAt(256, (532)); } }
        public string AdapterString { get { return GetFixedUnicodeStringAt(256, (1044)); } }
        public string BiosString { get { return GetFixedUnicodeStringAt(256, (1556)); } }
        public string DeviceID { get { return GetFixedUnicodeStringAt(256, (2068)); } }
        public int StateFlags { get { return GetInt32At(2580); } }

        #region Private
        internal SystemConfigVideoTraceData(Action<SystemConfigVideoTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 2584));
            Debug.Assert(!(Version == 1 && EventDataLength != 2584));
            Debug.Assert(!(Version == 2 && EventDataLength != 2584));
            Debug.Assert(!(Version > 2 && EventDataLength < 2584));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("MemorySize", MemorySize);
            sb.XmlAttrib("XResolution", XResolution);
            sb.XmlAttrib("YResolution", YResolution);
            sb.XmlAttrib("BitsPerPixel", BitsPerPixel);
            sb.XmlAttrib("VRefresh", VRefresh);
            sb.XmlAttrib("ChipType", ChipType);
            sb.XmlAttrib("DACType", DACType);
            sb.XmlAttrib("AdapterString", AdapterString);
            sb.XmlAttrib("BiosString", BiosString);
            sb.XmlAttrib("DeviceID", DeviceID);
            sb.XmlAttribHex("StateFlags", StateFlags);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "MemorySize", "XResolution", "YResolution", "BitsPerPixel", "VRefresh", "ChipType", "DACType", "AdapterString", "BiosString", "DeviceID", "StateFlags" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return MemorySize;
                case 1:
                    return XResolution;
                case 2:
                    return YResolution;
                case 3:
                    return BitsPerPixel;
                case 4:
                    return VRefresh;
                case 5:
                    return ChipType;
                case 6:
                    return DACType;
                case 7:
                    return AdapterString;
                case 8:
                    return BiosString;
                case 9:
                    return DeviceID;
                case 10:
                    return StateFlags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigVideoTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigServicesTraceData : TraceEvent
    {
        public string ServiceName { get { if (Version >= 2) return GetUnicodeStringAt(12); return GetFixedUnicodeStringAt(34, (0)); } }
        public string DisplayName { get { if (Version >= 2) return GetUnicodeStringAt(SkipUnicodeString(12)); return GetFixedUnicodeStringAt(256, (68)); } }
        // public new string ProcessName { get { if (Version >= 2) return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(12))); return GetFixedUnicodeStringAt(34, (580)); } }
        // public int ProcessID { get { if (Version >= 2) return GetInt32At(0); return GetInt32At(648); } }
        // TODO does this need FixupData?
        public int ServiceState { get { if (Version >= 2) return GetInt32At(4); return 0; } }
        public int SubProcessTag { get { if (Version >= 2) return GetInt32At(8); return 0; } }

        #region Private
        internal SystemConfigServicesTraceData(Action<SystemConfigServicesTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength < 652));     // TODO fixed by hand
            Debug.Assert(!(Version == 1 && EventDataLength < 652));     // TODO fixed by hand
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(12)))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(12)))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("ServiceName", ServiceName);
            sb.XmlAttrib("DisplayName", DisplayName);
            sb.XmlAttrib("ProcessName", ProcessName);
            sb.XmlAttrib("ProcessID", ProcessID);
            sb.XmlAttribHex("ServiceState", ServiceState);
            sb.XmlAttribHex("SubProcessTag", SubProcessTag);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "ServiceName", "DisplayName", "ProcessName", "ProcessID", "ServiceState", "SubProcessTag" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return ServiceName;
                case 1:
                    return DisplayName;
                case 2:
                    return ProcessName;
                case 3:
                    return ProcessID;
                case 4:
                    return ServiceState;
                case 5:
                    return SubProcessTag;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigServicesTraceData> Action;
        private KernelTraceEventParserState state;


        private unsafe void FixupData()
        {
            // Preserve the illusion that this event comes from the service it is for.
            // public int ProcessID { get { if (Version >= 2) return GetInt32At(0); return GetInt32At(648); } }
            // TODO does this need FixupData?
            if (Version >= 2)
                eventRecord->EventHeader.ProcessId = GetInt32At(0);
        }

        #endregion
    }
    public sealed class SystemConfigPowerTraceData : TraceEvent
    {
        public int S1 { get { return GetByteAt(0); } }
        public int S2 { get { return GetByteAt(1); } }
        public int S3 { get { return GetByteAt(2); } }
        public int S4 { get { return GetByteAt(3); } }
        public int S5 { get { return GetByteAt(4); } }
        // Skipping Pad1
        // Skipping Pad2
        // Skipping Pad3

        #region Private
        internal SystemConfigPowerTraceData(Action<SystemConfigPowerTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != 8));
            Debug.Assert(!(Version == 1 && EventDataLength != 8));
            Debug.Assert(!(Version == 2 && EventDataLength != 8));
            Debug.Assert(!(Version > 2 && EventDataLength < 8));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("S1", S1);
            sb.XmlAttrib("S2", S2);
            sb.XmlAttrib("S3", S3);
            sb.XmlAttrib("S4", S4);
            sb.XmlAttrib("S5", S5);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "S1", "S2", "S3", "S4", "S5" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return S1;
                case 1:
                    return S2;
                case 2:
                    return S3;
                case 3:
                    return S4;
                case 4:
                    return S5;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigPowerTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigIRQTraceData : TraceEvent
    {
        public long IRQAffinity { get { return GetInt64At(0); } }
        public int IRQNum { get { return GetInt32At(8); } }
        // TODO hand modified.   Fix for real 
        public int DeviceDescriptionLen { get { if (Version >= 3) return GetInt32At(16); else return GetInt32At(12); } }
        public string DeviceDescription { get { if (Version >= 3) return GetUnicodeStringAt(20); else return GetUnicodeStringAt(16); } }

        #region Private
        internal SystemConfigIRQTraceData(Action<SystemConfigIRQTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(16)));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(16)));
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(16)));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(16)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("IRQAffinity", IRQAffinity);
            sb.XmlAttrib("IRQNum", IRQNum);
            sb.XmlAttrib("DeviceDescriptionLen", DeviceDescriptionLen);
            sb.XmlAttrib("DeviceDescription", DeviceDescription);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IRQAffinity", "IRQNum", "DeviceDescriptionLen", "DeviceDescription" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IRQAffinity;
                case 1:
                    return IRQNum;
                case 2:
                    return DeviceDescriptionLen;
                case 3:
                    return DeviceDescription;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigIRQTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigPnPTraceData : TraceEvent
    {
        public int IDLength { get { return GetInt32At(0); } }
        public int DescriptionLength { get { return GetInt32At(4); } }
        public int FriendlyNameLength { get { return GetInt32At(8); } }
        public string DeviceID { get { return GetUnicodeStringAt(12); } }
        public string DeviceDescription { get { return GetUnicodeStringAt(SkipUnicodeString(12)); } }
        public string FriendlyName { get { return GetUnicodeStringAt(SkipUnicodeString(SkipUnicodeString(12))); } }

        #region Private
        internal SystemConfigPnPTraceData(Action<SystemConfigPnPTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(12)))));
            Debug.Assert(!(Version == 1 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(12)))));
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(12)))));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(SkipUnicodeString(SkipUnicodeString(12)))));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("IDLength", IDLength);
            sb.XmlAttrib("DescriptionLength", DescriptionLength);
            sb.XmlAttrib("FriendlyNameLength", FriendlyNameLength);
            sb.XmlAttrib("DeviceID", DeviceID);
            sb.XmlAttrib("DeviceDescription", DeviceDescription);
            sb.XmlAttrib("FriendlyName", FriendlyName);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "IDLength", "DescriptionLength", "FriendlyNameLength", "DeviceID", "DeviceDescription", "FriendlyName" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return IDLength;
                case 1:
                    return DescriptionLength;
                case 2:
                    return FriendlyNameLength;
                case 3:
                    return DeviceID;
                case 4:
                    return DeviceDescription;
                case 5:
                    return FriendlyName;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigPnPTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigNetworkTraceData : TraceEvent
    {
        public int TcbTablePartitions { get { return GetInt32At(0); } }
        public int MaxHashTableSize { get { return GetInt32At(4); } }
        public int MaxUserPort { get { return GetInt32At(8); } }
        public int TcpTimedWaitDelay { get { return GetInt32At(12); } }

        #region Private
        internal SystemConfigNetworkTraceData(Action<SystemConfigNetworkTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != 16));
            Debug.Assert(!(Version > 2 && EventDataLength < 16));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("TcbTablePartitions", TcbTablePartitions);
            sb.XmlAttrib("MaxHashTableSize", MaxHashTableSize);
            sb.XmlAttrib("MaxUserPort", MaxUserPort);
            sb.XmlAttrib("TcpTimedWaitDelay", TcpTimedWaitDelay);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "TcbTablePartitions", "MaxHashTableSize", "MaxUserPort", "TcpTimedWaitDelay" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return TcbTablePartitions;
                case 1:
                    return MaxHashTableSize;
                case 2:
                    return MaxUserPort;
                case 3:
                    return TcpTimedWaitDelay;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigNetworkTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }
    public sealed class SystemConfigIDEChannelTraceData : TraceEvent
    {
        public int TargetID { get { return GetInt32At(0); } }
        public int DeviceType { get { return GetInt32At(4); } }
        public int DeviceTimingMode { get { return GetInt32At(8); } }
        public int LocationInformationLen { get { return GetInt32At(12); } }
        public string LocationInformation { get { return GetUnicodeStringAt(16); } }

        #region Private
        internal SystemConfigIDEChannelTraceData(Action<SystemConfigIDEChannelTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 2 && EventDataLength != SkipUnicodeString(16)));
            Debug.Assert(!(Version > 2 && EventDataLength < SkipUnicodeString(16)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("TargetID", TargetID);
            sb.XmlAttribHex("DeviceType", DeviceType);
            sb.XmlAttribHex("DeviceTimingMode", DeviceTimingMode);
            sb.XmlAttrib("LocationInformationLen", LocationInformationLen);
            sb.XmlAttrib("LocationInformation", LocationInformation);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "TargetID", "DeviceType", "DeviceTimingMode", "LocationInformationLen", "LocationInformation" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return TargetID;
                case 1:
                    return DeviceType;
                case 2:
                    return DeviceTimingMode;
                case 3:
                    return LocationInformationLen;
                case 4:
                    return LocationInformation;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<SystemConfigIDEChannelTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }

    public sealed class VirtualAllocTraceData : TraceEvent
    {
        [Flags]
        public enum VirtualAllocFlags
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000,
            /*
            MEM_RESET = 0x80000,
            MEM_LARGE_PAGES = 0x20000000,
            MEM_PHYSICAL = 0x400000,
            MEM_TOP_DOWN = 0x100000,
            MEM_WRITE_WATCH = 0x200000,
            */
        };

        public Address BaseAddr { get { return GetHostPointer(0); } }
        public int Length { get { return GetInt32At(HostOffset(4, 1)); } }
        // Process ID is next (we fix it up). 
        public VirtualAllocFlags Flags { get { return (VirtualAllocFlags)GetInt32At(HostOffset(0xC, 1)); } }

        #region Private
        internal VirtualAllocTraceData(Action<VirtualAllocTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(EventDataLength == HostOffset(0x10, 1), "Unexpected data length");
            Action(this);
        }

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = GetInt32At(HostOffset(8, 1));
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("BaseAddr", BaseAddr);
            sb.XmlAttribHex("Length", Length);
            sb.XmlAttrib("Flags", Flags);
            sb.Append("/>");
            return sb;
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "BaseAddr", "Length", "Flags" };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return BaseAddr;
                case 1:
                    return Length;
                case 2:
                    return Flags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }
        public event Action<VirtualAllocTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }

    public sealed class ReadyThreadTraceData : TraceEvent
    {
        public enum AdjustReasonEnum
        {
            None = 0,
            Unwait = 1,
            Boost = 2,
        };

        // Thread ID is at offset 0 (we fix it up
        public AdjustReasonEnum AdjustReason { get { return (AdjustReasonEnum)GetByteAt(4); } }
        public int AdjustIncrement { get { return GetByteAt(5); } }
        public bool ExecutingDpc { get { return GetByteAt(6) != 0; } }
        // There is a spare byte after ExecutingDpc

        #region Private
        internal ReadyThreadTraceData(Action<ReadyThreadTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, KernelTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.FixupETLData = FixupData;
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(EventDataLength == 8, "Unexpected data length");
            Action(this);
        }

        private unsafe void FixupData()
        {
            Debug.Assert(eventRecord->EventHeader.ThreadId == -1);
            eventRecord->EventHeader.ThreadId = GetInt32At(0);
            Debug.Assert(eventRecord->EventHeader.ProcessId == -1);
            eventRecord->EventHeader.ProcessId = state.ThreadIDToProcessID(ThreadID, TimeStamp100ns);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("AdjustReason", AdjustReason);
            sb.XmlAttrib("AdjustIncrement", AdjustIncrement);
            sb.XmlAttrib("ExecutingDpc", ExecutingDpc);
            sb.Append("/>");
            return sb;
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "AdjustReason", "AdjustIncrement", "ExecutingDpc" };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return AdjustReason;
                case 1:
                    return AdjustIncrement;
                case 2:
                    return ExecutingDpc;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }
        public event Action<ReadyThreadTraceData> Action;
        private KernelTraceEventParserState state;
        #endregion
    }

    [SecurityTreatAsSafe, SecurityCritical]
    [CLSCompliant(false)]
    public sealed class ThreadPoolTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "ThreadPool";
        public static Guid ProviderGuid = new Guid(unchecked((int)0xc861d0e2), unchecked((short)0xa2c1), unchecked((short)0x4d36), 0x9f, 0x9c, 0x97, 0x0b, 0xab, 0x94, 0x3a, 0x12);
        public ThreadPoolTraceEventParser(TraceEventSource source) :base(source) {}

        public event Action<TPCBEnqueueTraceData> ThreadPoolTraceCBEnqueue
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPCBEnqueueTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 32, "CBEnqueue", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPCBEnqueueTraceData> ThreadPoolTraceCBStart
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPCBEnqueueTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 34, "CBStart", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPCBDequeueTraceData> ThreadPoolTraceCBDequeue
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPCBDequeueTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 33, "CBDequeue", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPCBDequeueTraceData> ThreadPoolTraceCBStop
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPCBDequeueTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 35, "CBStop", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPCBCancelTraceData> ThreadPoolTraceCBCancel
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPCBCancelTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 36, "CBCancel", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPPoolCreateCloseTraceData> ThreadPoolTracePoolCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPPoolCreateCloseTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 37, "PoolCreate", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPPoolCreateCloseTraceData> ThreadPoolTracePoolClose
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPPoolCreateCloseTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 38, "PoolClose", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPThreadSetTraceData> ThreadPoolTraceThreadMinSet
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPThreadSetTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 39, "ThreadMinSet", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<TPThreadSetTraceData> ThreadPoolTraceThreadMaxSet
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new TPThreadSetTraceData(value, 0xFFFF, 0, "ThreadPoolTrace", ThreadPoolTraceTaskGuid, 40, "ThreadMaxSet", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        #region private
        ThreadPoolTraceEventParserState State
        {
            get
            {
                ThreadPoolTraceEventParserState ret = (ThreadPoolTraceEventParserState)StateObject;
                if (ret == null)
                {
                    ret = new ThreadPoolTraceEventParserState();
                    StateObject = ret;
                }
                return ret;
            }
        }
        private static Guid ThreadPoolTraceTaskGuid = new Guid(unchecked((int)0xc861d0e2), unchecked((short)0xa2c1), unchecked((short)0x4d36), 0x9f, 0x9c, 0x97, 0x0b, 0xab, 0x94, 0x3a, 0x12);
        #endregion
    }
    #region private types
    internal class ThreadPoolTraceEventParserState : IFastSerializable
    {
        //TODO: Fill in
        void IFastSerializable.ToStream(Serializer serializer)
        {
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
        }
    }
    #endregion

    public sealed class TPCBEnqueueTraceData : TraceEvent
    {
        public Address PoolID { get { return GetHostPointer(0); } }
        public Address TaskID { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address CallbackFunction { get { return GetHostPointer(HostOffset(8, 2)); } }
        public Address CallbackContext { get { return GetHostPointer(HostOffset(12, 3)); } }
        public Address SubProcessTag { get { return GetHostPointer(HostOffset(16, 4)); } }

        #region Private
        internal TPCBEnqueueTraceData(Action<TPCBEnqueueTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, ThreadPoolTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(20, 5)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(20, 5)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("PoolID", PoolID);
            sb.XmlAttribHex("TaskID", TaskID);
            sb.XmlAttribHex("CallbackFunction", CallbackFunction);
            sb.XmlAttribHex("CallbackContext", CallbackContext);
            sb.XmlAttribHex("SubProcessTag", SubProcessTag);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "PoolID", "TaskID", "CallbackFunction", "CallbackContext", "SubProcessTag" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return PoolID;
                case 1:
                    return TaskID;
                case 2:
                    return CallbackFunction;
                case 3:
                    return CallbackContext;
                case 4:
                    return SubProcessTag;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TPCBEnqueueTraceData> Action;
        private ThreadPoolTraceEventParserState state;
        #endregion
    }
    public sealed class TPCBDequeueTraceData : TraceEvent
    {
        public Address TaskID { get { return GetHostPointer(0); } }

        #region Private
        internal TPCBDequeueTraceData(Action<TPCBDequeueTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, ThreadPoolTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("TaskID", TaskID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "TaskID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return TaskID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TPCBDequeueTraceData> Action;
        private ThreadPoolTraceEventParserState state;
        #endregion
    }
    public sealed class TPCBCancelTraceData : TraceEvent
    {
        public Address TaskID { get { return GetHostPointer(0); } }
        public int CancelCount { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal TPCBCancelTraceData(Action<TPCBCancelTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, ThreadPoolTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("TaskID", TaskID);
            sb.XmlAttrib("CancelCount", CancelCount);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "TaskID", "CancelCount" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return TaskID;
                case 1:
                    return CancelCount;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TPCBCancelTraceData> Action;
        private ThreadPoolTraceEventParserState state;
        #endregion
    }
    public sealed class TPPoolCreateCloseTraceData : TraceEvent
    {
        public Address PoolID { get { return GetHostPointer(0); } }

        #region Private
        internal TPPoolCreateCloseTraceData(Action<TPPoolCreateCloseTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, ThreadPoolTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("PoolID", PoolID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "PoolID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return PoolID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TPPoolCreateCloseTraceData> Action;
        private ThreadPoolTraceEventParserState state;
        #endregion
    }
    public sealed class TPThreadSetTraceData : TraceEvent
    {
        public Address PoolID { get { return GetHostPointer(0); } }
        public int ThreadNum { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal TPThreadSetTraceData(Action<TPThreadSetTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, ThreadPoolTraceEventParserState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("PoolID", PoolID);
            sb.XmlAttrib("ThreadNum", ThreadNum);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "PoolID", "ThreadNum" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return PoolID;
                case 1:
                    return ThreadNum;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<TPThreadSetTraceData> Action;
        private ThreadPoolTraceEventParserState state;
        #endregion
    }

    [SecurityTreatAsSafe, SecurityCritical]
    [CLSCompliant(false)]
    public sealed class HeapTraceProviderTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "HeapTraceProvider";
        public static Guid ProviderGuid = new Guid(unchecked((int)0x222962ab), unchecked((short)0x6180), unchecked((short)0x4b88), 0xa8, 0x25, 0x34, 0x6b, 0x75, 0xf2, 0xa2, 0x4a);
        public HeapTraceProviderTraceEventParser(TraceEventSource source) : base(source) {}

        public event Action<HeapCreateTraceData> HeapTraceCreate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapCreateTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 32, "Create", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapAllocTraceData> HeapTraceAlloc
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapAllocTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 33, "Alloc", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapReallocTraceData> HeapTraceReAlloc
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapReallocTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 34, "ReAlloc", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapFreeTraceData> HeapTraceFree
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapFreeTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 36, "Free", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapExpandTraceData> HeapTraceExpand
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapExpandTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 37, "Expand", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapSnapShotTraceData> HeapTraceSnapShot
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapSnapShotTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 38, "SnapShot", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapContractTraceData> HeapTraceContract
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapContractTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 42, "Contract", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapTraceData> HeapTraceDestroy
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 35, "Destroy", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapTraceData> HeapTraceLock
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 43, "Lock", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapTraceData> HeapTraceUnlock
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 44, "Unlock", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapTraceData> HeapTraceValidate
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 45, "Validate", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<HeapTraceData> HeapTraceWalk
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new HeapTraceData(value, 0xFFFF, 0, "HeapTrace", HeapTraceTaskGuid, 46, "Walk", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        #region private
        HeapTraceProviderState State
        {
            get
            {
                HeapTraceProviderState ret = (HeapTraceProviderState)StateObject;
                if (ret == null)
                {
                    ret = new HeapTraceProviderState();
                    StateObject = ret;
                }
                return ret;
            }
        }
        private static Guid HeapTraceTaskGuid = new Guid(unchecked((int)0x222962ab), unchecked((short)0x6180), unchecked((short)0x4b88), 0xa8, 0x25, 0x34, 0x6b, 0x75, 0xf2, 0xa2, 0x4a);
        #endregion
    }
    #region private types
    internal class HeapTraceProviderState : IFastSerializable
    {
        //TODO: Fill in
        void IFastSerializable.ToStream(Serializer serializer)
        {
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
        }
    }
    #endregion

    public sealed class HeapCreateTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public int HeapFlags { get { return GetInt32At(HostOffset(4, 1)); } }

        #region Private
        internal HeapCreateTraceData(Action<HeapCreateTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttrib("HeapFlags", HeapFlags);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "HeapFlags" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return HeapFlags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapCreateTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapAllocTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address AllocSize { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address AllocAddress { get { return GetHostPointer(HostOffset(8, 2)); } }
        public int SourceID { get { return GetInt32At(HostOffset(12, 3)); } }

        #region Private
        internal HeapAllocTraceData(Action<HeapAllocTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(16, 3)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(16, 3)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("AllocSize", AllocSize);
            sb.XmlAttribHex("AllocAddress", AllocAddress);
            sb.XmlAttrib("SourceID", SourceID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "AllocSize", "AllocAddress", "SourceID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return AllocSize;
                case 2:
                    return AllocAddress;
                case 3:
                    return SourceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapAllocTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapReallocTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address NewAllocAddress { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address OldAllocAddress { get { return GetHostPointer(HostOffset(8, 2)); } }
        public Address NewAllocSize { get { return GetHostPointer(HostOffset(12, 3)); } }
        public Address OldAllocSize { get { return GetHostPointer(HostOffset(16, 4)); } }
        public int SourceID { get { return GetInt32At(HostOffset(20, 5)); } }

        #region Private
        internal HeapReallocTraceData(Action<HeapReallocTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(24, 5)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(24, 5)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("NewAllocAddress", NewAllocAddress);
            sb.XmlAttribHex("OldAllocAddress", OldAllocAddress);
            sb.XmlAttribHex("NewAllocSize", NewAllocSize);
            sb.XmlAttribHex("OldAllocSize", OldAllocSize);
            sb.XmlAttrib("SourceID", SourceID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "NewAllocAddress", "OldAllocAddress", "NewAllocSize", "OldAllocSize", "SourceID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return NewAllocAddress;
                case 2:
                    return OldAllocAddress;
                case 3:
                    return NewAllocSize;
                case 4:
                    return OldAllocSize;
                case 5:
                    return SourceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapReallocTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapFreeTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address FreeAddress { get { return GetHostPointer(HostOffset(4, 1)); } }
        public int SourceID { get { return GetInt32At(HostOffset(8, 2)); } }

        #region Private
        internal HeapFreeTraceData(Action<HeapFreeTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(12, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(12, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("FreeAddress", FreeAddress);
            sb.XmlAttrib("SourceID", SourceID);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "FreeAddress", "SourceID" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return FreeAddress;
                case 2:
                    return SourceID;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapFreeTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapExpandTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address CommittedSize { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address CommitAddress { get { return GetHostPointer(HostOffset(8, 2)); } }
        public Address FreeSpace { get { return GetHostPointer(HostOffset(12, 3)); } }
        public Address CommittedSpace { get { return GetHostPointer(HostOffset(16, 4)); } }
        public Address ReservedSpace { get { return GetHostPointer(HostOffset(20, 5)); } }
        public int NoOfUCRs { get { return GetInt32At(HostOffset(24, 6)); } }

        #region Private
        internal HeapExpandTraceData(Action<HeapExpandTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(28, 6)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(28, 6)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("CommittedSize", CommittedSize);
            sb.XmlAttribHex("CommitAddress", CommitAddress);
            sb.XmlAttribHex("FreeSpace", FreeSpace);
            sb.XmlAttribHex("CommittedSpace", CommittedSpace);
            sb.XmlAttribHex("ReservedSpace", ReservedSpace);
            sb.XmlAttrib("NoOfUCRs", NoOfUCRs);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "CommittedSize", "CommitAddress", "FreeSpace", "CommittedSpace", "ReservedSpace", "NoOfUCRs" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return CommittedSize;
                case 2:
                    return CommitAddress;
                case 3:
                    return FreeSpace;
                case 4:
                    return CommittedSpace;
                case 5:
                    return ReservedSpace;
                case 6:
                    return NoOfUCRs;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapExpandTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapSnapShotTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address FreeSpace { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address CommittedSpace { get { return GetHostPointer(HostOffset(8, 2)); } }
        public Address ReservedSpace { get { return GetHostPointer(HostOffset(12, 3)); } }
        public int HeapFlags { get { return GetInt32At(HostOffset(16, 4)); } }

        #region Private
        internal HeapSnapShotTraceData(Action<HeapSnapShotTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(20, 4)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(20, 4)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("FreeSpace", FreeSpace);
            sb.XmlAttribHex("CommittedSpace", CommittedSpace);
            sb.XmlAttribHex("ReservedSpace", ReservedSpace);
            sb.XmlAttrib("HeapFlags", HeapFlags);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "FreeSpace", "CommittedSpace", "ReservedSpace", "HeapFlags" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return FreeSpace;
                case 2:
                    return CommittedSpace;
                case 3:
                    return ReservedSpace;
                case 4:
                    return HeapFlags;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapSnapShotTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapContractTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }
        public Address DeCommittedSize { get { return GetHostPointer(HostOffset(4, 1)); } }
        public Address DeCommitAddress { get { return GetHostPointer(HostOffset(8, 2)); } }
        public Address FreeSpace { get { return GetHostPointer(HostOffset(12, 3)); } }
        public Address CommittedSpace { get { return GetHostPointer(HostOffset(16, 4)); } }
        public Address ReservedSpace { get { return GetHostPointer(HostOffset(20, 5)); } }
        public int NoOfUCRs { get { return GetInt32At(HostOffset(24, 6)); } }

        #region Private
        internal HeapContractTraceData(Action<HeapContractTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(28, 6)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(28, 6)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.XmlAttribHex("DeCommittedSize", DeCommittedSize);
            sb.XmlAttribHex("DeCommitAddress", DeCommitAddress);
            sb.XmlAttribHex("FreeSpace", FreeSpace);
            sb.XmlAttribHex("CommittedSpace", CommittedSpace);
            sb.XmlAttribHex("ReservedSpace", ReservedSpace);
            sb.XmlAttrib("NoOfUCRs", NoOfUCRs);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle", "DeCommittedSize", "DeCommitAddress", "FreeSpace", "CommittedSpace", "ReservedSpace", "NoOfUCRs" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                case 1:
                    return DeCommittedSize;
                case 2:
                    return DeCommitAddress;
                case 3:
                    return FreeSpace;
                case 4:
                    return CommittedSpace;
                case 5:
                    return ReservedSpace;
                case 6:
                    return NoOfUCRs;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapContractTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }
    public sealed class HeapTraceData : TraceEvent
    {
        public Address HeapHandle { get { return GetHostPointer(0); } }

        #region Private
        internal HeapTraceData(Action<HeapTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, HeapTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(4, 1)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(4, 1)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("HeapHandle", HeapHandle);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "HeapHandle" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return HeapHandle;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<HeapTraceData> Action;
        private HeapTraceProviderState state;
        #endregion
    }

    [SecurityTreatAsSafe, SecurityCritical]
    [CLSCompliant(false)]
    public sealed class CritSecTraceProviderTraceEventParser : TraceEventParser
    {
        public static string ProviderName = "CritSecTraceProvider";
        public static Guid ProviderGuid = new Guid(unchecked((int)0x3ac66736), unchecked((short)0xcc59), unchecked((short)0x4cff), 0x81, 0x15, 0x8d, 0xf5, 0x0e, 0x39, 0x81, 0x6b);
        public CritSecTraceProviderTraceEventParser(TraceEventSource source) : base(source) {}

        public event Action<CritSecCollisionTraceData> CritSecTraceCollision
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new CritSecCollisionTraceData(value, 0xFFFF, 0, "CritSecTrace", CritSecTraceTaskGuid, 34, "Collision", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        public event Action<CritSecInitTraceData> CritSecTraceInitialize
        {
            add
            {
                // action, eventid, taskid, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName
                source.RegisterEventTemplate(new CritSecInitTraceData(value, 0xFFFF, 0, "CritSecTrace", CritSecTraceTaskGuid, 35, "Initialize", ProviderGuid, ProviderName, State));
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }

        #region private
        CritSecTraceProviderState State
        {
            get
            {
                CritSecTraceProviderState ret = (CritSecTraceProviderState)StateObject;
                if (ret == null)
                {
                    ret = new CritSecTraceProviderState();
                    StateObject = ret;
                }
                return ret;
            }
        }
        private static Guid CritSecTraceTaskGuid = new Guid(unchecked((int)0x3ac66736), unchecked((short)0xcc59), unchecked((short)0x4cff), 0x81, 0x15, 0x8d, 0xf5, 0x0e, 0x39, 0x81, 0x6b);
        #endregion
    }
    #region private types
    internal class CritSecTraceProviderState : IFastSerializable
    {
        //TODO: Fill in
        void IFastSerializable.ToStream(Serializer serializer)
        {
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
        }
    }
    #endregion

    public sealed class CritSecCollisionTraceData : TraceEvent
    {
        public int LockCount { get { return GetInt32At(0); } }
        public int SpinCount { get { return GetInt32At(4); } }
        public Address OwningThread { get { return GetHostPointer(8); } }
        public Address CritSecAddr { get { return GetHostPointer(HostOffset(12, 1)); } }

        #region Private
        internal CritSecCollisionTraceData(Action<CritSecCollisionTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, CritSecTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(16, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(16, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttrib("LockCount", LockCount);
            sb.XmlAttrib("SpinCount", SpinCount);
            sb.XmlAttribHex("OwningThread", OwningThread);
            sb.XmlAttribHex("CritSecAddr", CritSecAddr);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "LockCount", "SpinCount", "OwningThread", "CritSecAddr" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return LockCount;
                case 1:
                    return SpinCount;
                case 2:
                    return OwningThread;
                case 3:
                    return CritSecAddr;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<CritSecCollisionTraceData> Action;
        private CritSecTraceProviderState state;
        #endregion
    }
    public sealed class CritSecInitTraceData : TraceEvent
    {
        public Address SpinCount { get { return GetHostPointer(0); } }
        public Address CritSecAddr { get { return GetHostPointer(HostOffset(4, 1)); } }

        #region Private
        internal CritSecInitTraceData(Action<CritSecInitTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, CritSecTraceProviderState state)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.state = state;
        }
        protected internal override void Dispatch()
        {
            Debug.Assert(!(Version == 0 && EventDataLength != HostOffset(8, 2)));
            Debug.Assert(!(Version > 0 && EventDataLength < HostOffset(8, 2)));
            Action(this);
        }
        public override StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            sb.XmlAttribHex("SpinCount", SpinCount);
            sb.XmlAttribHex("CritSecAddr", CritSecAddr);
            sb.Append("/>");
            return sb;
        }

        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "SpinCount", "CritSecAddr" };
                return payloadNames;
            }
        }

        public override object PayloadValue(int index)
        {
            switch (index)
            {
                case 0:
                    return SpinCount;
                case 1:
                    return CritSecAddr;
                default:
                    Debug.Assert(false, "Bad field index");
                    return null;
            }
        }

        private event Action<CritSecInitTraceData> Action;
        private CritSecTraceProviderState state;
        #endregion
    }
}
