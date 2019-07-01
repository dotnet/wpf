// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//	
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// #define DEBUG_SERIALIZE
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel; // For Win32Excption;
using System.Runtime.InteropServices;
using Microsoft.Test.EventTracing.FastSerialization;
using System.Reflection;
using System.Diagnostics.Eventing;

/* TODO current interesting places */
/* code:#UniqueAddress code:TraceCodeAddresses.LookupSymbols */
/* code:TraceLog.CopyRawEvents code:TraceLog.ToStream */

// See code:#Introduction
namespace Microsoft.Test.EventTracing
{
    /// <summary>
    /// #Introduction
    /// 
    /// While the raw ETW events are valuable, they really need additional processing to be really useful.
    /// Things like symbolic names for addresses, the ability to randomly access events, and having various
    /// links between threads, threads, modules, and eventToStack traces are really needed. This is what
    /// code:TraceLog provides.
    /// 
    /// In addition the format of an ETL file is private (it can only be accessed through OS APIs), and the
    /// only access is as stream of records. This makes it very difficult to do processing on the data
    /// without reading all the data into memory or reading over the file more than once. Because the data is
    /// very large, this is quite undesireable. There is also no good place to put digested information like
    /// symbols, or indexes. code:TraceLog also defines a new file format for trace data, that is public and
    /// seekable, extendable, and versionable. This is a key piece of added value.
    /// 
    /// code:TraceLog is really the entry point for a true object model for event data that are cross linked
    /// to each other as well as the raw events. Here are some of the players
    /// 
    /// * code:TraceLog - represents the event log as a whole. It holds 'global' things, like a list of
    ///     code:TraceProcesss, and the list of code:TraceModuleFiles
    ///     * code:TraceProcesses - represents a list of code:TraceProcess s, that can be looked up by
    ///         (PID,time)
    ///     * code:TraceProcess - represents a single process.
    ///     * code:TraceThread - reprsents a thread within a process.
    ///     * code:TraceLoadedModules - represents a list of code:TraceLoadedModule s that can be looked up
    ///         by (address, time) or (filename, time)
    ///     * code:TraceLoadedModule - represents a loaded DLL or EXE (it knows its image base, and time
    ///         loaded)
    ///     * code:TraceModuleFile - represents a DLL or EXE on the disk (it only contains information that
    ///         is common to all threads that use it (eg its name). In particular it holds all the symbolic
    ///         address to name mappings (extracted from PDBs).  New TraceModuleFiles are generated if a
    ///         files is loaded in another locations (either later in the same process or a different
    ///         process).   Thus the image base becomes a attribute of the ModuleFile
    ///     * code:TraceCallStack - represents a call stack associated with the event (on VISTA). It is
    ///         logically a list of code addresses (from callee to caller).    
    ///     * code:TraceCodeAddress - represents instruction pointer into the code. This can be decorated
    ///         with symbolic informaition, (methodIndex, source line, source file) information.
    ///     * code:TraceMethod - represents a particular method.  This class allows information that is
    ///         common to many samples (it method name and source file), to be shared.  
    ///     
    /// * See also code:TraceLog.CopyRawEvents for the routine that scans over the events during TraceLog
    ///     creation.
    /// * See also code:#ProcessHandlersCalledFromTraceLog for callbacks made from code:TraceLog.CopyRawEvents
    /// * See also code:#ModuleHandlersCalledFromTraceLog for callbacks made from code:TraceLog.CopyRawEvents
    /// </summary>
    [CLSCompliant(false)]
    public class TraceLog : TraceEventSource, IDisposable, IFastSerializable
    {
        /// <summary>
        /// If etlxFilePath exists, it simply calls the constuctor.  However it it does not exist and a
        /// cooresponding ETL file exists, generate the etlx file from the ETL file.  options indicate
        /// conversion options (can be null). 
        /// </summary>
        public static TraceLog OpenOrConvert(string etlxFilePath, TraceLogOptions options)
        {
            if (etlxFilePath.EndsWith(".etl", StringComparison.OrdinalIgnoreCase))
                etlxFilePath = Path.ChangeExtension(etlxFilePath, ".etlx");
            if (!File.Exists(etlxFilePath))
            {
                string etlFilePath = Path.ChangeExtension(etlxFilePath, ".etl");
                if (File.Exists(etlFilePath))
                    CreateFromETL(etlFilePath, etlxFilePath, options);
            }
            return new TraceLog(etlxFilePath);
        }
        public static TraceLog OpenOrConvert(string etlxFilePath)
        {
            return OpenOrConvert(etlxFilePath, null);
        }
        /// <summary>
        /// Opens a existing Trace Event log file (and ETLX file).  If you need to create a new log file
        /// from other data see 
        /// </summary>
        public TraceLog(string etlxFilePath)
            : this()
        {
            InitializeFromFile(etlxFilePath);
        }

        /// <summary>
        /// Generates the cooresponding ETLX file from the raw ETL files.  Returns the name of ETLX file. 
        /// </summary>
        public static string CreateFromETL(string etlFilePath)
        {
            return CreateFromETL(etlFilePath, null, null);
        }
        /// <summary>
        /// Given 'etlFilePath' create a etlxFile for the profile data. Options can be null.
        /// </summary>
        public static string CreateFromETL(string etlFilePath, string etlxFilePath, TraceLogOptions options)
        {
            if (etlxFilePath == null)
                etlxFilePath = Path.ChangeExtension(etlFilePath, ".etlx");
            using (ETWTraceEventSource source = new ETWTraceEventSource(etlFilePath))
                CreateFromSource(source, etlxFilePath, options);
            return etlxFilePath;
        }
        /// <summary>
        /// Given a source of events 'source' generated a ETLX file representing these events from them. This
        /// file can then be opened with the code:TraceLog constructor. 'options' can be null.
        /// </summary>
        public static void CreateFromSource(TraceEventDispatcher source, string etlxFilePath, TraceLogOptions options)
        {
            CreateFromSourceTESTONLY(source, etlxFilePath, options);
        }
        /// <summary>
        /// TODO: only used for testing, will be removed eventually.  Use CreateFromSource
        /// 
        /// Because the code path when reading from the file (and thus uses the deserializers), is very
        /// different from when the data structures are in memory, and we don't want to have to test both
        /// permutations, we don't allow getting a TraceLog that did NOT come from a file.  
        /// 
        /// However for testing this is useful, because we can see the 'before serialization' and 'after
        /// serialization' behavior and if they are differnet we know we hav a bug.  This routine should be
        /// removed eventually, after we have high confidence that the log file works well.  
        /// </summary>
        //TODO remove
        // [Obsolete("Only for testing, please use CreateFromSource and open the resultint TraceLog instead")]
        public static TraceLog CreateFromSourceTESTONLY(TraceEventDispatcher source, string etlxFilePath, TraceLogOptions options)
        {
            if (options == null)
                options = new TraceLogOptions();

            // TODO copy the additional data from a ETLX file if the source is ETLX 

            // TODO handle this for real.  
            Debug.Assert(source.EventsLost == 0);

            TraceLog newLog = new TraceLog();
            newLog.rawEventSourceToConvert = source;
            newLog.options = options;

            // Copy over all the users data from the source.  
            foreach (string key in source.UserData.Keys)
                newLog.UserData[key] = source.UserData[key];

            // We always create these parsers that the TraceLog knows about.
            new KernelTraceEventParser(newLog);
            new ClrTraceEventParser(newLog);
            new ClrPrivateTraceEventParser(newLog);
            new ClrStressTraceEventParser(newLog);
            new ClrRundownTraceEventParser(newLog);
            new DynamicTraceEventParser(newLog);
            new WPFTraceEventParser(newLog);
            new SymbolTraceEventParser(newLog);

            // Avoid partially written files by writing to a temp and moving atomically to the
            // final destination.  
            string etlxTempPath = etlxFilePath + ".tmp";
            try
            {
                // This calls code:TraceLog.ToStream operation on TraceLog which does the real work.  
                using (Serializer serializer = new Serializer(etlxTempPath, newLog)) { }
                if (File.Exists(etlxFilePath))
                    File.Delete(etlxFilePath);
                File.Move(etlxTempPath, etlxFilePath);
            }
            finally
            {
                if (File.Exists(etlxTempPath))
                    File.Delete(etlxTempPath);
            }

            return newLog;      // TODO should return void.  
        }

        /// <summary>
        /// All the events in the stream.  A code:TraceEvent can be used with foreach
        /// directly but it can also be used to filter in arbitrary ways to form other
        /// logical streams of data.  
        /// </summary>
        public TraceEvents Events { get { return events; } }
        /// <summary>
        /// Enumerate all the threads that occured in the trace log. 
        /// </summary>
        public TraceProcesses Processes { get { return processes; } }
        /// <summary>
        /// A list of all the files that are loaded by some process during the logging. 
        /// </summary>
        public TraceModuleFiles ModuleFiles { get { return moduleFiles; } }
        /// <summary>
        /// Get the collection of all callstacks.  
        /// </summary>
        public TraceCallStacks CallStacks { get { return callStacks; } }
        /// <summary>
        /// Get the collection of all symbolic code addresses. 
        /// </summary>
        public TraceCodeAddresses CodeAddresses { get { return codeAddresses; } }

        /// <summary>
        /// If the event has a call eventToStack associated with it, retrieve it. 
        /// </summary>
        public TraceCallStack GetCallStackForEvent(TraceEvent anEvent)
        {
            return callStacks[GetCallStackIndexForEvent(anEvent)];
        }
        /// <summary>
        /// If an event has fields of type 'Address' the address can be converted to a symblic value (a
        /// code:TraceCodeAddress) by calling this function.
        /// </summary>
        [CLSCompliant(false)]
        public TraceCodeAddress GetCodeAddressAtEvent(Address address, TraceEvent context)
        {
            CodeAddressIndex codeAddressIndex = GetCodeAddressIndexAtEvent(address, context);
            if (codeAddressIndex == CodeAddressIndex.Invalid)
                return null;
            return codeAddresses[codeAddressIndex];
        }

        public CallStackIndex GetCallStackIndexForEvent(TraceEvent anEvent)
        {
            // TODO optimize for sequential access.  
            lazyEventsToStacks.FinishRead();
            int index;
            if (eventsToStacks.BinarySearch(anEvent.EventIndex, out index, stackComparer))
                return eventsToStacks[index].CallStackIndex;
            return CallStackIndex.Invalid;
        }
        [CLSCompliant(false)]
        public CodeAddressIndex GetCodeAddressIndexAtEvent(Address address, TraceEvent context)
        {
            // TODO optimize for sequential access.  
            EventIndex eventIndex = context.EventIndex;
            int index;
            if (!eventsToCodeAddresses.BinarySearch(eventIndex, out index, CodeAddressComparer))
                return CodeAddressIndex.Invalid;
            do
            {
                Debug.Assert(eventsToCodeAddresses[index].EventIndex == eventIndex);
                if (eventsToCodeAddresses[index].Address == address)
                    return eventsToCodeAddresses[index].CodeAddressIndex;
                index++;
            } while (index < eventsToCodeAddresses.Count && eventsToCodeAddresses[index].EventIndex == eventIndex);
            return CodeAddressIndex.Invalid;
        }

        /// <summary>
        /// Events are given an Index (ID) that are unique across the whole code:TraceLog.   They are not guarenteed
        /// to be sequential, but they are guarenteed to be between 0 and MaxEventIndex.  Ids can be used to
        /// allow clients to associate additional information with event (with a side lookup table).   See
        /// code:TraceEvent.EventIndex and code:EventIndex for more 
        /// </summary>
        public EventIndex MaxEventIndex { get { return (EventIndex)eventCount; } }
        /// <summary>
        /// Given an eventIndex, get the event.  This is relatively expensive because we need to create a
        /// copy of the event that will not be reused by the TraceLog.   Ideally you woudld not use this API
        /// but rather use iterate over event using code:TraceEvents
        /// </summary>
        [CLSCompliant(false)]
        public TraceEvent GetEvent(EventIndex eventIndex)
        {
            // TODO NOT Done. 
            Debug.Assert(false, "NOT Done");
            TraceEvent anEvent = null;
            return anEvent;
        }

        /// <summary>
        /// The total number of events in the log.  
        /// </summary>
        public int EventCount { get { return eventCount; } }
        /// <summary>
        /// The file path name for the ETLX file associated with this log.  
        /// </summary>
        public string FilePath { get { return etlxFilePath; } }
        public long FirstEventTime100ns { get { return firstEventTime100ns; } }
        public DateTime FirstEventTime { get { return DateTime.FromFileTime(FirstEventTime100ns); } }
        /// <summary>
        /// The machine one which the log was collected.  Returns empty string if unknown. 
        /// </summary>
        public string MachineName { get { return machineName; } }
        /// <summary>
        /// The size of the main memory (RAM) on the collection machine.  Will return 0 if memory size is unknown 
        /// </summary>
        public int MemorySizeMeg { get { return memorySizeMeg; } }
        /// <summary>
        /// Are there any events with stack traces in them?
        /// </summary>
        public bool HasCallStacks { get { return CallStacks.MaxCallStackIndex > 0; } }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" <TraceLogHeader ");
            sb.AppendLine("   MachineName=" + XmlUtilities.XmlQuote(MachineName));
            sb.AppendLine("   EventCount=" + XmlUtilities.XmlQuote(EventCount));
            sb.AppendLine("   LogFileName=" + XmlUtilities.XmlQuote(FilePath));
            sb.AppendLine("   EventsLost=" + XmlUtilities.XmlQuote(EventsLost));
            sb.AppendLine("   SessionStartTime=" + XmlUtilities.XmlQuote(SessionStartTime));
            sb.AppendLine("   SessionEndTime=" + XmlUtilities.XmlQuote(SessionEndTime));
            sb.AppendLine("   SessionDuration=" + XmlUtilities.XmlQuote((SessionDuration).ToString()));
            sb.AppendLine("   NumberOfProcessors=" + XmlUtilities.XmlQuote(NumberOfProcessors));
            sb.AppendLine("   CpuSpeedMHz=" + XmlUtilities.XmlQuote(CpuSpeedMHz));
            sb.AppendLine("   MemorySizeMeg=" + XmlUtilities.XmlQuote(MemorySizeMeg));
            sb.AppendLine("   PointerSize=" + XmlUtilities.XmlQuote(PointerSize));
            sb.AppendLine(" />");
            return sb.ToString();
        }

        /// <summary>
        /// Agressively releases resources associated with the log. 
        /// </summary>
        public void Close()
        {
            lazyRawEvents.Dispose();
        }

        #region ITraceParserServices Members

        protected override void RegisterEventTemplateImpl(TraceEvent template)
        {
            ((ITraceParserServices)sourceWithRegisteredParsers).RegisterEventTemplate(template);

            // If we are converting from raw input, send the callbacks to them during that phase too.   
            if (rawEventSourceToConvert != null)
                ((ITraceParserServices)rawEventSourceToConvert).RegisterEventTemplate(template);
        }

        protected override void RegisterParserImpl(TraceEventParser parser)
        {
            parsers.Add(parser);
            // cause all the events in the parser to be registered with me.
            // Converting raw input is a special case, and we don't do the registration in that case. 
            if (rawEventSourceToConvert == null)
                parser.All += null;
        }

        protected override void RegisterUnhandledEventImpl(Action<TraceEvent> callback)
        {
            ((ITraceParserServices)sourceWithRegisteredParsers).RegisterUnhandledEvent(callback);

            // If we are converting from raw input, send the callbacks to them during that phase too.   
            if (rawEventSourceToConvert != null)
                ((ITraceParserServices)rawEventSourceToConvert).RegisterUnhandledEvent(callback);
        }

        protected override string  TaskNameForGuidImpl(Guid guid)
        {
            return ((ITraceParserServices)sourceWithRegisteredParsers).TaskNameForGuid(guid);
        }
        protected override string ProviderNameForGuidImpl(Guid taskOrProviderGuid)
        {
            return ((ITraceParserServices)sourceWithRegisteredParsers).ProviderNameForGuid(taskOrProviderGuid);
        }
        #endregion
        #region Private
        private TraceLog()
        {
            // TODO: All the IFastSerializable parts of this are discareded, which is unfortunate. 
            this.processes = new TraceProcesses(this);
            this.events = new TraceEvents(this);
            this.moduleFiles = new TraceModuleFiles(this);
            this.codeAddresses = new TraceCodeAddresses(this, this.moduleFiles);
            this.callStacks = new TraceCallStacks(this, this.codeAddresses);
            this.parsers = new List<TraceEventParser>();
            this.sourceWithRegisteredParsers = new ETLXTraceEventSource(new TraceEvents(this));
            this.machineName = "";
        }

        /// <summary>
        ///  Copies the events from the 'rawEvents' dispatcher to the output stream 'IStreamWriter'.  It
        ///  also creates auxillery data structures associated with the raw events (eg, processes, threads,
        ///  modules, address lookup maps...  Basically any information that needs to be determined by
        ///  scanning over the events during TraceLog creation should hook in here.  
        /// </summary>
        private void CopyRawEvents(TraceEventDispatcher rawEvents, IStreamWriter writer)
        {
            bool removeEventFromStream = true;
            bool inEpilog = false;              // Epilogs are when the kernel starts dumping DCEnd messages for images. 
            bool inProlog = false;               // Prologs are before the kernel DCStarts. 
            int numberOnPage = eventsPerPage;
            PastEventInfo pastEventInfo = new PastEventInfo(0);
            long lastDCStart100ns = rawEvents.SessionStartTime100ns;
            eventCount = 0;

            if (!(rawEvents is ETLXTraceEventSource))
            {
                // We only do the prolog thing if we have a combined log with Kernel events. 
                inProlog = true;
                ETWTraceEventSource asETW = rawEvents as ETWTraceEventSource;
                if (asETW != null)
                    inProlog = asETW.HasKernelEvents;

                // If this is a ETL file, we also need to compute all the normal TraceLog stuff the raw
                // stream.  

                // TODO fail if you merge logs of varying pointer size.  
                this.pointerSize = rawEvents.PointerSize;
                this.sessionEndTime100ns = rawEvents.SessionEndTime100ns;
                this.sessionStartTime100ns = rawEvents.SessionStartTime100ns;
                this.cpuSpeedMHz = rawEvents.CpuSpeedMHz;
                this.numberOfProcessors = rawEvents.NumberOfProcessors;
                this.cpuSpeedMHz = rawEvents.CpuSpeedMHz;
                this.eventsLost = rawEvents.EventsLost;

                // TODO need all events that might have Addresses in them, can we be more efficient.  
                rawEvents.Kernel.All += delegate(TraceEvent data) { };

                Debug.Assert(((eventsPerPage - 1) & eventsPerPage) == 0, "eventsPerPage must be a power of 2");

                // TODO, these are put first, but the user mode header has a time-stamp that is out of order
                // which messes up our binary search.   For now just remove them, as the data is really
                // accessed from the log, not the event.  
                rawEvents.Kernel.EventTraceHeader += delegate(EventTraceHeaderTraceData data)
                {
                    removeEventFromStream = true;
                };

                rawEvents.Kernel.SystemConfigCPU += delegate(SystemConfigCPUTraceData data)
                {
                    this.memorySizeMeg = data.MemSize;
                    if (data.DomainName.Length > 0)
                        this.machineName = data.ComputerName + "." + data.DomainName;
                    else
                        this.machineName = data.ComputerName;
                };

                // Process level events. 
                rawEvents.Kernel.ProcessStartGroup += delegate(ProcessTraceData data)
                {
                    this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).ProcessStart(data);
                    if (data.Opcode == TraceEventOpcode.DataCollectionStart)
                    {
                        DebugWarn(inProlog, "DCStart for thread outside prolog", data);
                        removeEventFromStream = true;
                        lastDCStart100ns = data.TimeStamp100ns;
                    }
                };

                rawEvents.Kernel.ProcessEndGroup += delegate(ProcessTraceData data)
                {
                    this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).ProcessEnd(data);
                    if (data.Opcode == TraceEventOpcode.DataCollectionStart)
                    {
                        this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).ProcessEnd(data);
                        removeEventFromStream = true;
                        inEpilog = true;
                    }
                };
                // Thread level events
                rawEvents.Kernel.ThreadStartGroup += delegate(ThreadTraceData data)
                {
                    TraceThread thread = this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).Threads.GetOrCreateThread(data.ThreadID, data.TimeStamp100ns);
                    DebugWarn(thread.startTime100ns == 0 || data.ThreadID == 0 || inProlog || inEpilog, "Thread start on an existing Thread ID " + data.ThreadID, data);
                    DebugWarn(thread.Process.EndTime100ns == ETWTraceEventSource.MaxTime100ns || inProlog || inEpilog, "Thread starting on ended process", data);
                    thread.startTime100ns = data.TimeStamp100ns;
                    if (data.Opcode == TraceEventOpcode.DataCollectionStart)
                    {
                        DebugWarn(inProlog, "DCStart for thread outside prolog", data);
                        removeEventFromStream = true;
                        thread.startTime100ns = sessionStartTime100ns;
                        lastDCStart100ns = data.TimeStamp100ns;
                    }
                };
                rawEvents.Kernel.ThreadEndGroup += delegate(ThreadTraceData data)
                {
                    TraceThread thread = this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).Threads.GetOrCreateThread(data.ThreadID, data.TimeStamp100ns);
                    DebugWarn(thread.startTime100ns != 0 || inProlog || inEpilog, "Thread end that was not started " + data.ThreadID, data);
                    DebugWarn(thread.endTime100ns == ETWTraceEventSource.MaxTime100ns || inProlog || inEpilog, "Thread end on a terminated thread " + data.ThreadID + " that ended at " + RelativeTimeMSec(thread.endTime100ns), data);
                    DebugWarn(thread.Process.EndTime100ns == ETWTraceEventSource.MaxTime100ns || inProlog || inEpilog, "Thread ending on ended process", data);
                    thread.endTime100ns = data.TimeStamp100ns;
                    if (data.Opcode == TraceEventOpcode.DataCollectionStop)
                    {
                        thread.endTime100ns = sessionEndTime100ns;
                        removeEventFromStream = true;
                        inEpilog = true;
                    }
                };

                // ModuleFile level events
                rawEvents.Kernel.ImageLoadGroup += delegate(ImageLoadTraceData data)
                {
                    this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).LoadedModules.ImageLoadOrUnload(data, true);
                    if (data.Opcode == TraceEventOpcode.DataCollectionStop)
                    {
                        DebugWarn(inProlog, "DCStart for image outside prolog", data);
                        removeEventFromStream = true;
                        lastDCStart100ns = data.TimeStamp100ns;
                    }
                };
                rawEvents.Kernel.ImageUnloadGroup += delegate(ImageLoadTraceData data)
                {
                    this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).LoadedModules.ImageLoadOrUnload(data, false);
                    if (data.Opcode == TraceEventOpcode.DataCollectionStop)
                    {
                        removeEventFromStream = true;
                        inEpilog = true;
                    }
                };

                rawEvents.Kernel.FileIoName += delegate(FileIoNameTraceData data)
                {
                    removeEventFromStream = true;
                };

                rawEvents.Clr.LoaderModuleLoad += delegate(ModuleLoadUnloadTraceData data)
                {
                    this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).LoadedModules.ManagedModuleLoadOrUnload(data, true);
                };
                rawEvents.Clr.LoaderModuleUnload += delegate(ModuleLoadUnloadTraceData data)
                {
                    this.processes.GetOrCreateProcess(data.ProcessID, data.TimeStamp100ns).LoadedModules.ManagedModuleLoadOrUnload(data, false);
                };

                // Method level events
                rawEvents.Clr.MethodLoadVerbose += delegate(MethodLoadUnloadVerboseTraceData data)
                {
                    // WE only caputure data on unload, because we collect the addresses first. 
                    if (!data.IsDynamic && !data.IsJitted)
                        removeEventFromStream = true;
                };
                rawEvents.Clr.MethodUnloadVerbose += delegate(MethodLoadUnloadVerboseTraceData data)
                {
                    codeAddresses.AddMethod(data);
                    removeEventFromStream = true;   // TODO remove unconditionally?
                };

                int warningCount = 0;
                int largeDeltas = 0;
                QPCInfo[] clockInfos = new QPCInfo[rawEvents.NumberOfProcessors];
                for (int i = 0; i < clockInfos.Length; i++)
                    clockInfos[i] = new QPCInfo(i, rawEvents);


                rawEvents.Clr.ClrStackWalk += delegate(ClrStackWalkTraceData data)
                {
                    if (!inProlog && !inEpilog)
                    {
                        PastEventInfoIndex prevEventIndex = pastEventInfo.GetPreviousEventIndex(data);
                        if (prevEventIndex != PastEventInfoIndex.Invalid)
                        {
                            // TODO really need to only consider CLR events on the same thread. 
                            CallStackIndex callStackIndex = callStacks.GetStackIndexForStackEvent(data, data.FrameCount);
                            Debug.Assert(callStacks.Depth(callStackIndex) == data.FrameCount);
                            DebugWarn(pastEventInfo.GetThreadID(prevEventIndex) == data.ThreadID, "Mismatched thread for CLR Stack Trace", data);

                            // Get the previous event on the same thread. 
                            EventIndex eventIndex = pastEventInfo.GetEventIndex(prevEventIndex);
                            eventsToStacks.Add(new EventsToStackIndex(eventIndex, callStackIndex));
                        }
                        else
                            DebugWarn(false, "Could not find a previous event for a CLR stack trace.", data);
                    }
                };

                rawEvents.Kernel.StackWalk += delegate(StackWalkTraceData data)
                {
                    removeEventFromStream = true;
                    long curTimeQPC = data.EventTimeStampQPC;
                    int procNum = data.ProcessorNumber;
                    long expectedTime100ns = clockInfos[procNum].ExpectedTime100ns(curTimeQPC);

                    PastEventInfoIndex prevEventIndex;
                    bool isInitialized = clockInfos[procNum].Initialized;
                    if (isInitialized)
                    {
                        /*
                        Console.WriteLine("Stack at {0,10:f4} Expected target event: {1,10:f4}",
                            data.TimeStampRelativeMSec, rawEvents.RelativeTimeMSec(expectedTime100ns));
                         */
                        prevEventIndex = pastEventInfo.GetEventForTime(expectedTime100ns);
                    }
                    else
                    {
                        // We have not figured out the relationship between QPC and time, so we
                        // just look for the 'last' event on the same thread.  
                        // Console.WriteLine("QPC ratio not found, using last event on same thread");
                        prevEventIndex = pastEventInfo.GetPreviousEventIndex(data);
                    }

                    if (prevEventIndex != PastEventInfoIndex.Invalid)
                    {
                        long actualTime100ns = pastEventInfo.GetTimeStamp100ns(prevEventIndex);
                        long delta = clockInfos[procNum].Update(actualTime100ns, curTimeQPC, expectedTime100ns);
                        // Console.WriteLine("Found target at {0,10:f4} delta {1,4:f1},", rawEvents.RelativeTimeMSec(actualTime100ns), delta);

                        // Consistancy check.  Do the thread IDs match?
                        int threadID = pastEventInfo.GetThreadID(prevEventIndex);
                        if (threadID >= 0 && threadID != data.ThreadID)
                        {
                            DebugWarn(warningCount > 50, "The expected event time " +
                                rawEvents.RelativeTimeMSec(expectedTime100ns).ToString("f4") +
                                " found an event at " +
                                rawEvents.RelativeTimeMSec(actualTime100ns).ToString("f4") +
                                " but the Thread IDs don't match, dropping stack.", data);
                            clockInfos[procNum].Reset();
                            warningCount++;
                            return;
                        }

                        if (!inProlog && !inEpilog)
                        {
                            if (delta > 500)
                            {
                                if (isInitialized)
                                {
                                    DebugWarn(warningCount > 50, "Delta between expected " +
                                        rawEvents.RelativeTimeMSec(expectedTime100ns).ToString("f4") +
                                        " and actual event time " +
                                        rawEvents.RelativeTimeMSec(actualTime100ns).ToString("f4") +
                                        " is > 50usec (" + (delta / 10) + "), dropping stack.", data);
                                    clockInfos[procNum].Reset();
                                    warningCount++;
                                }
                                return;
                            }
                            if (delta > 50 && isInitialized)
                            {
                                DebugWarn(warningCount > 50, "Delta between expected " +
                                    rawEvents.RelativeTimeMSec(expectedTime100ns).ToString("f4") +
                                    " and actual event time " +
                                    rawEvents.RelativeTimeMSec(actualTime100ns).ToString("f4") +
                                    " is > 5usec (" + (delta / 10) + ").", data);
                                largeDeltas++;
                                if (largeDeltas >= 3)
                                    clockInfos[procNum].Reset();
                                warningCount++;
                            }
                            else
                                largeDeltas = 0;

                            // Log the stack
                            CallStackIndex callStackIndex = callStacks.GetStackIndexForStackEvent(data, data.FrameCount);
                            Debug.Assert(callStacks.Depth(callStackIndex) == data.FrameCount);

                            int prevEventsToStack = pastEventInfo.GetCallStackIndex(prevEventIndex, curTimeQPC);
                            if (prevEventsToStack >= 0)
                            {
                                // There was already a stack, so we append to it
                                EventsToStackIndex eventToStackEntry = eventsToStacks[prevEventsToStack];
                                eventToStackEntry.CallStackIndex = callStacks.Combine(eventToStackEntry.CallStackIndex, callStackIndex);
                                eventsToStacks[prevEventsToStack] = eventToStackEntry;
                            }
                            else
                            {
                                EventIndex eventIndex = pastEventInfo.GetEventIndex(prevEventIndex);
                                // Make a new stack, but also remember the index for it so we
                                // can find it again quickly if we need to add to it.  
                                pastEventInfo.SetCallStackIndex(prevEventIndex, eventsToStacks.Count, curTimeQPC);
                                eventsToStacks.Add(new EventsToStackIndex(eventIndex, callStackIndex));
                            }
                        }
                    }
                    else
                    {
                        if (expectedTime100ns > lastDCStart100ns)
                        {
                            double delayMsec = (data.TimeStamp100ns - expectedTime100ns) / 10000.0;
                            if (delayMsec < 5)
                            {
                                DebugWarn(warningCount > 50, "Stack points to time " +
                                    rawEvents.RelativeTimeMSec(expectedTime100ns).ToString("f4") + " but no event found.", data);
                            }
                            else
                            {
                                DebugWarn(warningCount > 50, "Lost part of stack (event happened at " + rawEvents.RelativeTimeMSec(expectedTime100ns).ToString("f4") + ").", data);
                            }
                            warningCount++;
                        }
                    }
                };
            }

            // This callback will fire on every event that has an address that needs to be associted with
            // symbolic information (methodIndex, and source file line / name information.  
            Action<TraceEvent, Address> addToCodeAddressMap = delegate(TraceEvent data, Address address)
            {
                CodeAddressIndex codeAddressIndex = codeAddresses.GetCodeAddressIndex(data, address);

                // I require that the list be sorted by event ID.  
                Debug.Assert(eventsToCodeAddresses.Count == 0 || eventsToCodeAddresses[eventsToCodeAddresses.Count - 1].EventIndex <= (EventIndex)eventCount);
                eventsToCodeAddresses.Add(new EventsToCodeAddressIndex(MaxEventIndex, address, codeAddressIndex));
            };

            // TODO remove
            bool shownEpilog = false;

            // While scanning over the stream, copy all data to the file. 
            rawEvents.EveryEvent += delegate(TraceEvent data)
            {
#if DEBUG
                if (data is UnhandledTraceEvent)
                {
                    Debug.Assert((byte)data.opcode != unchecked((byte)-1));        // Means PrepForCallback not done. 
                    Debug.Assert(data.TaskName != "ERRORTASK");
                    Debug.Assert(data.OpcodeName != "ERROROPCODE");
                }
#endif
                pastEventInfo.LogEvent(data, (EventIndex)eventCount);
                Debug.Assert(!((inEpilog || inProlog) && !removeEventFromStream));
                if (removeEventFromStream)
                {
                    bool keepEvent = false;
                    if (inProlog)
                    {
                        // And we expect no more than 250MSec between DCStarts.   
                        if (data.TimeStamp100ns - lastDCStart100ns > 250 * 10000)
                            inProlog = false;

                        // The PerfInfoCollectionStart marks the end of the prolog for sure.
                        // Because XP does not have this event, we also will start on the first
                        // ProcessStart event
                        // * TODO: add additional condition that trace classicETW when using process start.
                        if ((data is SampledProfileIntervalTraceData && data.Opcode == (TraceEventOpcode)73) ||
                            (data is ProcessTraceData && data.Opcode == TraceEventOpcode.Start))
                        {
                            keepEvent = true;
                            inProlog = false;
                        }

                        // Never strip image loads or thread loads (process loads are handled
                        // above)
                        if (data.Opcode == TraceEventOpcode.Start &&
                            (data is ThreadTraceData || data is ImageLoadTraceData))
                            keepEvent = true;

                        if (!inProlog)
                            Console.WriteLine("Prolog Ended at " + data.TimeStampRelativeMSec);
                    }
                    if (!inProlog && !inEpilog)
                        removeEventFromStream = false;

                    if (keepEvent)
                        goto WRITE_EVENT;

                    // Don't trim system configuration events even if they are in the prolog or
                    // epilog.  
                    if (data.TaskName == "SystemConfig")
                        goto WRITE_EVENT;

                    if (inEpilog && !shownEpilog)
                    {
                        shownEpilog = true;
                        Console.WriteLine("Epilog started at " + data.TimeStampRelativeMSec);
                    }
                    return;
                }

            WRITE_EVENT:
                // Console.WriteLine("Event at " + data.TimeStampRelativeMSec + " ID 0x" + ((int)eventIndex).ToString("x"));

                data.LogCodeAddresses(addToCodeAddressMap);       // set up Event x Address -> TraceCodeAddress table. 
                if (numberOnPage >= eventsPerPage)
                {
                    // Console.WriteLine("Writing page " + this.eventPages.BatchCount, " Start " + writer.GetLabel());
                    this.eventPages.Add(new EventPageEntry(data.TimeStamp100ns, writer.GetLabel()));
                    numberOnPage = 0;
                }
#if DEBUG
                double relativeTime = data.TimeStampRelativeMSec;
#endif
                unsafe
                {
                    WriteBlob((IntPtr)data.eventRecord, writer, headerSize);
                    WriteBlob(data.userData, writer, (data.EventDataLength + 3 & ~3));
                }
                if (eventCount == 0)
                    firstEventTime100ns = data.TimeStamp100ns;
                numberOnPage++;
                eventCount++;
            };

            rawEvents.Process();                  // Run over the data. 
            Debug.Assert(eventCount % eventsPerPage == numberOnPage || eventCount == 0);
            Console.WriteLine("Got " + processes.MaxProcessIndex + " distinct processes.");
            foreach (TraceProcess process in processes)
            {
                if (process.StartTime100ns > sessionStartTime100ns && process.ExitStatus.HasValue)
                    Console.WriteLine("Process " + process.Name +
                        " started at " + process.StartTimeRelativeMsec.ToString("f3") +
                        " and ended at " + process.EndTimeRelativeMsec.ToString("f3"));
            }
            Console.WriteLine("Got " + eventCount + " events.");
            Console.WriteLine("Got " + eventsToStacks.Count + " events with stack traces.");
            Console.WriteLine("Got " + eventsToCodeAddresses.Count + " events with code addresses in them.");
            Console.WriteLine("Got " + codeAddresses.MaxCodeAddressIndex + " unique code addresses.");
            Console.WriteLine("Got " + callStacks.MaxCallStackIndex + " unique stacks.");
            Console.WriteLine("Got " + codeAddresses.Methods.MaxMethodIndex + " unique managed methods parsed.");
            Console.WriteLine("From " + codeAddresses.ManagedMethodRecordCount + " CLR method event records.");
        }

        protected override internal string ProcessName(int processID, long time100ns)
        {
            TraceProcess process = Processes.GetProcess(processID, time100ns);
            if (process == null)
                return base.ProcessName(processID, time100ns);
            return process.Name;
        }

        public override void Dispose()
        {
            Close();
        }
        unsafe private static void WriteBlob(IntPtr source, IStreamWriter writer, int byteCount)
        {
            Debug.Assert((int)source % 4 == 0);
            Debug.Assert(byteCount % 4 == 0);
            int* sourcePtr = (int*)source;
            int intCount = byteCount >> 2;
            while (intCount > 0)
            {
                writer.Write(*sourcePtr++);
                --intCount;
            }
        }

        internal static void Warn(string message)
        {
            Console.WriteLine(message);
        }
        // [Conditional("DEBUG")]
        internal void DebugWarn(bool condition, string message, TraceEvent data)
        {
            if (!condition)
            {
                Console.Write("WARNING: ");
                if (data != null)
                    Console.Write("Time: " + data.TimeStampRelativeMSec.ToString("f4").PadLeft(12) + " PID: " + data.ProcessID.ToString().PadLeft(4) + ": ");
                Console.WriteLine(message);

                ImageLoadTraceData asImageLoad = data as ImageLoadTraceData;
                if (asImageLoad != null)
                {
                    Console.WriteLine("    FILE: " + asImageLoad.FileName);
                    Console.WriteLine("    BASE: 0x" + asImageLoad.ImageBase.ToString("x"));
                    Console.WriteLine("    SIZE: 0x" + asImageLoad.ImageSize.ToString("x"));
                }
                ModuleLoadUnloadTraceData asModuleLoad = data as ModuleLoadUnloadTraceData;
                if (asModuleLoad != null)
                {
                    Console.WriteLine("    NGEN:     " + asModuleLoad.ModuleNativePath);
                    Console.WriteLine("    ILFILE:   " + asModuleLoad.ModuleILPath);
                    Console.WriteLine("    MODULEID: 0x" + ((ulong)asModuleLoad.ModuleID).ToString("x"));
                }
                MethodLoadUnloadVerboseTraceData asMethodLoad = data as MethodLoadUnloadVerboseTraceData;
                if (asMethodLoad != null)
                {
                    Console.WriteLine("    METHOD:   " + GetFullName(asMethodLoad));
                    Console.WriteLine("    MODULEID: " + ((ulong)asMethodLoad.ModuleID).ToString("x"));
                    Console.WriteLine("    START:    " + ((ulong)asMethodLoad.MethodStartAddress).ToString("x"));
                    Console.WriteLine("    LENGTH:   " + asMethodLoad.MethodSize.ToString("x"));
                }
            }
        }
        internal static string GetFullName(MethodLoadUnloadVerboseTraceData data)
        {
            string sig = data.MethodSignature;
            int parens = sig.IndexOf('(');
            string args;
            if (parens >= 0)
                args = sig.Substring(parens);
            else
                args = "";
            string fullName = data.MethodNamespace + "." + data.MethodName + args;
            return fullName;
        }
        // [Conditional("DEBUG")]
        internal static void DebugWarn(string message)
        {
            Console.WriteLine(message);
        }

        internal int FindPageIndex(long time100ns)
        {
            int pageIndex;
            // TODO error conditions. 
            // TODO? extra copy of EventPageEntry during search.  
            eventPages.BinarySearch(time100ns, out pageIndex, delegate(long targetTime100ns, EventPageEntry entry)
            {
                return targetTime100ns.CompareTo(entry.Time100ns);
            });
            // TODO completely empty logs.  
            if (pageIndex < 0)
                pageIndex = 0;
            return pageIndex;
        }

        /// <summary>
        /// Advance 'reader' until it point at a event that occurs on or after 'time100ns'.  on page
        /// 'pageIndex'.  If 'positions' is non-null, fill in that array.  Also return the index in
        /// 'positions' for the entry that was found.  
        /// </summary>
        internal unsafe void SeekToTimeOnPage(PinnedStreamReader reader, long time100ns, int pageIndex, out int indexOnPage, StreamLabel[] positions)
        {
            reader.Goto(eventPages[pageIndex].Position);
            int i = -1;
            while (i < TraceLog.eventsPerPage - 1)
            {
                i++;
                if (positions != null)
                    positions[i] = reader.Current;
                TraceEventNativeMethods.EVENT_RECORD* ptr = reader.GetPointer(headerSize);

                Debug.Assert(ptr->EventHeader.Level <= 6);
                Debug.Assert(ptr->EventHeader.Version <= 2);

                long eventTime100ns = ptr->EventHeader.TimeStamp;
                Debug.Assert(sessionStartTime100ns <= eventTime100ns && eventTime100ns < DateTime.Now.Ticks || eventTime100ns == long.MaxValue);

                if (eventTime100ns >= time100ns)
                    break;

                int eventDataLength = ptr->UserDataLength;
                Debug.Assert(eventDataLength < 0x20000);
                reader.Skip(headerSize + ((eventDataLength + 3) & ~3));
            }
            indexOnPage = i;
        }

        internal unsafe PinnedStreamReader AllocReader()
        {
            if (freeReader == null)
                freeReader = ((PinnedStreamReader)lazyRawEvents.Deserializer.Reader).Clone();
            PinnedStreamReader ret = freeReader;
            freeReader = null;
            return ret;
        }
        internal unsafe void FreeReader(PinnedStreamReader reader)
        {
            if (freeReader == null)
                freeReader = reader;
        }
        internal unsafe TraceEventDispatcher AllocLookup()
        {
            if (freeLookup == null)
                freeLookup = sourceWithRegisteredParsers.Clone();
            TraceEventDispatcher ret = freeLookup;
            freeLookup = null;
            return ret;
        }
        internal unsafe void FreeLookup(TraceEventDispatcher lookup)
        {
            if (freeLookup == null)
                freeLookup = lookup;
        }

        private unsafe void InitializeFromFile(string etlxFilePath)
        {
            // If this Assert files, fix the declaration of code:headerSize to match
            Debug.Assert(sizeof(TraceEventNativeMethods.EVENT_HEADER) == 0x50 && sizeof(TraceEventNativeMethods.ETW_BUFFER_CONTEXT) == 4);

            Deserializer deserializer = new Deserializer(new PinnedStreamReader(etlxFilePath), etlxFilePath);

            // when the deserializer needs a TraceLog we return the current instance.  We also assert that
            // we only do this once.  
            deserializer.RegisterFactory(typeof(TraceLog), delegate
            {
                Debug.Assert(sessionStartTime100ns == 0 && sessionEndTime100ns == 0);
                return this;
            });
            deserializer.RegisterFactory(typeof(TraceProcess), delegate { return new TraceProcess(0, null, 0); });
            deserializer.RegisterFactory(typeof(TraceProcesses), delegate { return new TraceProcesses(null); });
            deserializer.RegisterFactory(typeof(TraceThreads), delegate { return new TraceThreads(null); });
            deserializer.RegisterFactory(typeof(TraceThread), delegate { return new TraceThread(0, null, (ThreadIndex)0); });
            deserializer.RegisterFactory(typeof(TraceModuleFiles), delegate { return new TraceModuleFiles(null); });
            deserializer.RegisterFactory(typeof(TraceModuleFile), delegate { return new TraceModuleFile(null, Address.Null, 0); });
            deserializer.RegisterFactory(typeof(TraceMethods), delegate { return new TraceMethods(); });
            deserializer.RegisterFactory(typeof(TraceCodeAddresses), delegate { return new TraceCodeAddresses(null, null); });
            deserializer.RegisterFactory(typeof(TraceCallStacks), delegate { return new TraceCallStacks(null, null); });

            deserializer.RegisterFactory(typeof(TraceLoadedModules), delegate { return new TraceLoadedModules(null); });
            deserializer.RegisterFactory(typeof(TraceLoadedModule), delegate { return new TraceLoadedModule(null, null); });
            deserializer.RegisterFactory(typeof(TraceManagedModule), delegate { return new TraceManagedModule(0, 0, false, null, null, null); });

            deserializer.RegisterFactory(typeof(ProviderManifest), delegate { return new ProviderManifest(null, ManifestEnvelope.ManifestFormats.SimpleXmlFormat, 0, 0); });

            // when the serserializer needs any TraceEventParser class, we assume that its constructor
            // takes an argument of type TraceEventSource and that you can pass null to make an
            // 'empty' parser to fill in with FromStream.  
            deserializer.RegisterDefaultFactory(delegate(Type typeToMake)
            {
                if (typeToMake.IsSubclassOf(typeof(TraceEventParser)))
                    return (IFastSerializable)Activator.CreateInstance(typeToMake, new object[] { null });
                return null;
            });

            IFastSerializable entry = deserializer.GetEntryObject();
            // TODO this needs to be a runtime error, not an assert.  
            Debug.Assert(entry == this);
            // Our deserializer is now attached to our defered events.  
            Debug.Assert(lazyRawEvents.Deserializer == deserializer);

            this.etlxFilePath = etlxFilePath;

            // Sanity checking.  
            Debug.Assert(pointerSize == 4 || pointerSize == 8, "Bad pointer size");
            Debug.Assert(100 <= cpuSpeedMHz && cpuSpeedMHz <= 10000, "Bad cpu speed");
            Debug.Assert(0 < numberOfProcessors && numberOfProcessors < 1024, "Bad number of processors");
            Debug.Assert(0 < MaxEventIndex);
            // TODO remove
            if (eventsLost > 0)
                Console.WriteLine("Warning: " + eventsLost + " events were lost");
        }

#if DEBUG
        /// <summary>
        /// Returns true if 'str' has only normal ascii (printable) characters.
        /// </summary>
        static internal bool NormalChars(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                Char c = str[i];
                if (c < ' ' && !Char.IsWhiteSpace(c) || '~' < c)
                    return false;
            }
            return true;
        }
#endif
        void IFastSerializable.ToStream(Serializer serializer)
        {
            // Write out the events themselves, Before we do this we write a reference past the end of the
            // events so we can skip them without actually reading them. 
            // The real work is done in code:CopyRawEvents

            // Align to 8 bytes
            StreamLabel pos = serializer.Writer.GetLabel();
            int align = ((int)pos + 1) & 7;          // +1 take into acount we always write the count
            if (align > 0)
                align = 8 - align;
            serializer.Write((byte)align);
            for (int i = 0; i < align; i++)
                serializer.Write((byte)0);
            Debug.Assert((int)serializer.Writer.GetLabel() % 8 == 0);

            serializer.Log("<Marker name=\"RawEvents\"/>");
            lazyRawEvents.Write(serializer, delegate
            {
                // Get the events from a given raw stream
                TraceEventDispatcher dispatcher = rawEventSourceToConvert;
                if (dispatcher == null)
                    dispatcher = events.GetSource();
                CopyRawEvents(dispatcher, serializer.Writer);
                // Write sentinal event with a long.MaxValue timestamp mark the end of the data. 
                for (int i = 0; i < 11; i++)
                {
                    if (i == 2)
                        serializer.Write(long.MaxValue);    // This is the important field, the timestamp. 
                    else
                        serializer.Write((long)0);          // The important field here is the EventDataSize field 
                }

                if (HasCallStacks || options.AlwaysResolveSymbols)
                    codeAddresses.LookupSymbols(options);
            });

            serializer.Log("<Marker name=\"sessionStartTime100ns\"/>");
            serializer.Write(sessionStartTime100ns);
            serializer.Write(firstEventTime100ns);
            serializer.Write(sessionEndTime100ns);
            serializer.Write(pointerSize);
            serializer.Write(numberOfProcessors);
            serializer.Write(cpuSpeedMHz);
            serializer.Write(eventsLost);
            serializer.Write(machineName);
            serializer.Write(memorySizeMeg);

            serializer.Write(processes);
            serializer.Write(codeAddresses);
            serializer.Write(callStacks);
            serializer.Write(moduleFiles);

            serializer.Log("<WriteColection name=\"eventPages\" count=\"" + eventPages.Count + "\">\r\n");
            serializer.Write(eventPages.Count);
            for (int i = 0; i < eventPages.Count; i++)
            {
                serializer.Write(eventPages[i].Time100ns);
                serializer.Write(eventPages[i].Position);
            }
            serializer.Write(eventPages.Count);                 // redundant as a checksum
            serializer.Log("</WriteColection>\r\n");
            serializer.Write(eventCount);

            serializer.Log("<Marker Name=\"eventsToStacks\"/>");
            lazyEventsToStacks.Write(serializer, delegate
            {
                serializer.Log("<WriteColection name=\"eventsToStacks\" count=\"" + eventsToStacks.Count + "\">\r\n");
                serializer.Write(eventsToStacks.Count);
                foreach (EventsToStackIndex eventToStack in eventsToStacks)
                {
                    serializer.Write((int)eventToStack.EventIndex);
                    serializer.Write((int)eventToStack.CallStackIndex);
                }
                serializer.Write(eventsToStacks.Count);             // Redundant as a checksum
                serializer.Log("</WriteColection>\r\n");
            });

            serializer.Log("<Marker Name=\"eventsToCodeAddresses\"/>");
            lazyEventsToCodeAddresses.Write(serializer, delegate
            {
                serializer.Log("<WriteColection name=\"eventsToCodeAddresses\" count=\"" + eventsToCodeAddresses.Count + "\">\r\n");
                serializer.Write(eventsToCodeAddresses.Count);
                foreach (EventsToCodeAddressIndex eventsToCodeAddress in eventsToCodeAddresses)
                {
                    serializer.Write((int)eventsToCodeAddress.EventIndex);
                    serializer.Write((long)eventsToCodeAddress.Address);
                    serializer.Write((int)eventsToCodeAddress.CodeAddressIndex);
                }
                serializer.Write(eventsToCodeAddresses.Count);       // Redundant as a checksum
                serializer.Log("</WriteColection>\r\n");
            });

            serializer.Log("<WriteColection name=\"userData\" count=\"" + userData.Count + "\">\r\n");
            serializer.Write(userData.Count);
            foreach (KeyValuePair<string, object> pair in UserData)
            {
                serializer.Write(pair.Key);
                IFastSerializable asFastSerializable = (IFastSerializable)pair.Value;
                serializer.Write(asFastSerializable);
            }
            serializer.Write(userData.Count);                   // Redundant as a checksum
            serializer.Log("</WriteColection>\r\n");

            serializer.Log("<WriteColection name=\"parsers\" count=\"" + parsers.Count + "\">\r\n");
            serializer.Write(parsers.Count);
            for (int i = 0; i < parsers.Count; i++)
                serializer.Write(parsers[i].GetType().FullName);
            serializer.Write(parsers.Count);                    // redundant as a checksum
            serializer.Log("</WriteColection>\r\n");

            string shortName = MachineName;
            int dot = shortName.IndexOf('.');
            if (dot >= 0)
                shortName = shortName.Substring(0, dot);
            if (shortName.Length > 0 && string.Compare(Environment.MachineName, shortName, StringComparison.OrdinalIgnoreCase) != 0)
                DebugWarn("ERROR! Collection Machine: " + shortName +
                    " differs from ETL conversion machine: " + Environment.MachineName + "\r\n" +
                    "File names and symbols will not be correct!");
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Log("<Marker Name=\"RawEvents\"/>");
            byte align;
            deserializer.Read(out align);
            while (align > 0)
            {
                byte zero;
                deserializer.Read(out zero);
                --align;
            }
            Debug.Assert((int)deserializer.reader.Current % 8 == 0);    // We expect alignment. 

            // Skip all the raw events.  
            lazyRawEvents.Read(deserializer, null);

            deserializer.Log("<Marker Name=\"sessionStartTime100ns\"/>");
            deserializer.Read(out sessionStartTime100ns);
            deserializer.Read(out firstEventTime100ns);
            deserializer.Read(out sessionEndTime100ns);
            deserializer.Read(out pointerSize);
            deserializer.Read(out numberOfProcessors);
            deserializer.Read(out cpuSpeedMHz);
            deserializer.Read(out eventsLost);
            deserializer.Read(out machineName);
            deserializer.Read(out memorySizeMeg);

            deserializer.Read(out processes);
            deserializer.Read(out codeAddresses);
            deserializer.Read(out callStacks);
            deserializer.Read(out moduleFiles);

            deserializer.Log("<Marker Name=\"eventPages\"/>");
            int count = deserializer.ReadInt();
            eventPages = new GrowableArray<EventPageEntry>(count + 1);
            EventPageEntry entry = new EventPageEntry();
            for (int i = 0; i < count; i++)
            {
                deserializer.Read(out entry.Time100ns);
                deserializer.Read(out entry.Position);
                eventPages.Add(entry);
            }
            int checkCount = deserializer.ReadInt();
            if (count != checkCount)
                throw new SerializationException("Redundant count check fail.");
            deserializer.Read(out eventCount);

            lazyEventsToStacks.Read(deserializer, delegate
            {
                int stackCount = deserializer.ReadInt();
                deserializer.Log("<Marker name=\"eventToStackIndex\" count=\"" + stackCount + "\"/>");
                eventsToStacks = new GrowableArray<EventsToStackIndex>(stackCount + 1);
                EventsToStackIndex eventToStackIndex = new EventsToStackIndex();
                for (int i = 0; i < stackCount; i++)
                {
                    eventToStackIndex.EventIndex = (EventIndex)deserializer.ReadInt();
                    Debug.Assert((int)eventToStackIndex.EventIndex < eventCount);
                    eventToStackIndex.CallStackIndex = (CallStackIndex)deserializer.ReadInt();
                    eventsToStacks.Add(eventToStackIndex);
                }
                int stackCheckCount = deserializer.ReadInt();
                if (stackCount != stackCheckCount)
                    throw new SerializationException("Redundant count check fail.");

            });
            lazyEventsToStacks.FinishRead();        // TODO REMOVE

            lazyEventsToCodeAddresses.Read(deserializer, delegate
            {
                int codeAddressCount = deserializer.ReadInt();
                deserializer.Log("<Marker Name=\"eventToCodeAddressIndex\" count=\"" + codeAddressCount + "\"/>");
                eventsToCodeAddresses = new GrowableArray<EventsToCodeAddressIndex>(codeAddressCount + 1);
                EventsToCodeAddressIndex eventToCodeAddressIndex = new EventsToCodeAddressIndex();
                for (int i = 0; i < codeAddressCount; i++)
                {
                    eventToCodeAddressIndex.EventIndex = (EventIndex)deserializer.ReadInt();
                    deserializer.ReadAddress(out eventToCodeAddressIndex.Address);
                    eventToCodeAddressIndex.CodeAddressIndex = (CodeAddressIndex)deserializer.ReadInt();
                    eventsToCodeAddresses.Add(eventToCodeAddressIndex);
                }
                int codeAddressCheckCount = deserializer.ReadInt();
                if (codeAddressCount != codeAddressCheckCount)
                    throw new SerializationException("Redundant count check fail.");
            });
            lazyEventsToCodeAddresses.FinishRead();        // TODO REMOVE

            count = deserializer.ReadInt();
            deserializer.Log("<Marker Name=\"userData\" count=\"" + count + "\"/>");
            for (int i = 0; i < count; i++)
            {
                string key;
                deserializer.Read(out key);
                IFastSerializable value = deserializer.ReadObject();
                userData[key] = value;
            }
            checkCount = deserializer.ReadInt();
            if (count != checkCount)
                throw new SerializationException("Redundant count check fail.");

            deserializer.Log("<Marker Name=\"parsers\"/>");
            count = deserializer.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string fullTypeName = deserializer.ReadString();
                Type type = Type.GetType(fullTypeName, true);
                ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(TraceEventSource) });
                if (constructor == null)
                    throw new SerializationException("Type: " + fullTypeName + " does not have a constructor taking a TraceSource");
                TraceEventParser parser = (TraceEventParser)constructor.Invoke(new object[] { this });
                parsers.Add(parser);
            }
            checkCount = deserializer.ReadInt();    // TODO make this checksumming automatic. 
            if (count != checkCount)
                throw new SerializationException("Redundant count check fail.");
        }

        // headerSize is the size we persist of code:TraceEventNativeMethods.EVENT_RECORD which is up to and
        // including the UserDataLength field (after this field the fields are architecture dependent in
        // size. 
        // TODO: we add 16 just to keep compatibility with the size we used before.  This is a complete
        // waste at the moment.  When we decide to break compatibility we should reclaim this.  
        internal const int headerSize = 0x50 /* EVENT_HEADER */ + 4 /* ETW_BUFFER_CONTEXT */ + 4 /* 2 shorts */ + 16;

        // #TraceLogVars
        // see code:#TraceEventVars
        private string etlxFilePath;
        [CLSCompliant(false)]
        protected long firstEventTime100ns;
        private int memorySizeMeg;
        private string machineName;
        private TraceProcesses processes;
        private TraceCallStacks callStacks;
        private TraceCodeAddresses codeAddresses;

        private DeferedRegion lazyRawEvents;
        private DeferedRegion lazyEventsToStacks;
        private DeferedRegion lazyEventsToCodeAddresses;
        private TraceEvents events;
        private GrowableArray<EventPageEntry> eventPages;   // The offset offset of a page
        private int eventCount;                             // Total number of events
        private TraceModuleFiles moduleFiles;
        private GrowableArray<EventsToStackIndex> eventsToStacks;
        private GrowableArray<EventsToCodeAddressIndex> eventsToCodeAddresses;


        private TraceEventDispatcher freeLookup;    // Try to reused old ones. 
        private PinnedStreamReader freeReader;

        private List<TraceEventParser> parsers;
        internal ETLXTraceEventSource sourceWithRegisteredParsers;

        #region EventPages
        internal const int eventsPerPage = 1024;    // We keep track of  where events are in 'pages' of this size.
        private struct EventPageEntry
        {
            public EventPageEntry(long Time100ns, StreamLabel Position)
            {
                this.Time100ns = Time100ns;
                this.Position = Position;
            }
            public long Time100ns;                      // Time for the first items in this page. 
            public StreamLabel Position;                // Offset to this page. 
        }
        #endregion

        // These clases are only used during conversion from ETL files 
        // They are not needed for ETLX consumption.  
        #region PastEventInfo
        enum PastEventInfoIndex { Invalid = -1 };

        /// <summary>
        /// We need to remember the the EventIndexes of the events that were 'just before' this event so we can
        /// associate eventToStack traces with the event that actually caused them.  PastEventInfo does this.  
        /// </summary>
        struct PastEventInfo
        {
            public PastEventInfo(int dummy)
            {
                pastEventInfo = new PastEventInfoEntry[historySize];
                curPastEventInfo = 0;
            }
            public void LogEvent(TraceEvent data, EventIndex eventIndex)
            {
                int threadID = data.ThreadID;
                // Thread an process events need to be munged slightly.  
                if (data.ParentThread >= 0)
                {
                    Debug.Assert(data is ProcessTraceData || data is ThreadTraceData);
                    threadID = data.ParentThread;
                }
                pastEventInfo[curPastEventInfo].ThreadID = threadID;
                pastEventInfo[curPastEventInfo].TimeStamp100ns = data.TimeStamp100ns;
                pastEventInfo[curPastEventInfo].EventIndex = eventIndex;
                pastEventInfo[curPastEventInfo].StackIndex = -1;
                curPastEventInfo = (curPastEventInfo + 1) & (historySize - 1);
            }

            public PastEventInfoIndex GetPreviousEventIndex(TraceEvent anEvent)
            {
                int idx = curPastEventInfo;
                for (int i = 0; i < historySize; i++)
                {
                    --idx;
                    if (idx < 0)
                        idx = historySize - 1;
                    if (pastEventInfo[idx].ThreadID == anEvent.ThreadID)
                        return (PastEventInfoIndex)idx;
                    // Don't go back more than 2msec. 
                    if (anEvent.TimeStamp100ns - pastEventInfo[idx].TimeStamp100ns > 20000)
                        break;
                }
                return PastEventInfoIndex.Invalid;
            }

            public PastEventInfoIndex GetEventForTime(long timeStamp100ns)
            {
                PastEventInfoIndex retIdx = PastEventInfoIndex.Invalid;
                long lastTimeStamp100ns = long.MaxValue;
                int idx = curPastEventInfo;
                for (int i = 0; i < historySize; i++)
                {
                    --idx;
                    if (idx < 0)
                        idx = historySize - 1;
                    long curTimeStamp100ns = pastEventInfo[idx].TimeStamp100ns;
                    if (((ulong)curTimeStamp100ns + (ulong)lastTimeStamp100ns) / 2 < (ulong)timeStamp100ns)
                        return retIdx;
                    lastTimeStamp100ns = curTimeStamp100ns;
                    retIdx = (PastEventInfoIndex)idx;
                }
                return PastEventInfoIndex.Invalid;
            }

            public int GetThreadID(PastEventInfoIndex index) { return pastEventInfo[(int)index].ThreadID; }
            public EventIndex GetEventIndex(PastEventInfoIndex index) { return pastEventInfo[(int)index].EventIndex; }
            public long GetTimeStamp100ns(PastEventInfoIndex index) { return pastEventInfo[(int)index].TimeStamp100ns; }
            public int GetCallStackIndex(PastEventInfoIndex index, long QPCTime)
            {
                int ret = pastEventInfo[(int)index].StackIndex;
                if (ret >= 0)
                {
                    if (QPCTime != pastEventInfo[(int)index].QPCTime)
                    {
                        TraceLog.DebugWarn("Event lookup for stack does not match existing stack fragment.");
                        ret = -1;
                    }
                }
                return ret;
            }
            public void SetCallStackIndex(PastEventInfoIndex index, int stackIndex, long QPCTime)
            {
                pastEventInfo[(int)index].StackIndex = stackIndex;
                pastEventInfo[(int)index].QPCTime = QPCTime;
            }

            #region private
            // Stuff we remember about past events 
            private struct PastEventInfoEntry
            {
                public long TimeStamp100ns;
                public long QPCTime;
                public int ThreadID;
                public int StackIndex;
                public EventIndex EventIndex;
            }

            const int historySize = 1024;          // Must be a power of 2
            PastEventInfoEntry[] pastEventInfo;
            int curPastEventInfo;
            #endregion
        }
        #endregion


        /// <summary>
        /// Stack traces have a tick count that indicats the event associated with that stack.  We
        /// need to convert these tick counts to normal time (adjusting for clock skew) That is
        /// what these variables are for. 
        /// </summary>
        struct QPCInfo
        {
            public QPCInfo(int procNum, TraceEventSource source)
            {
                this.procNum = procNum;
                this.source = source;
                this.goodEstimates = 0;
                this.baseTimeQPC = 0;
                this.baseTime100ns = 0;
                this.firstTimeQPC = 0;
                this.firstTime100ns = 0;
                this.QPCsPer100ns = 0;
            }
            public bool Initialized { get { return goodEstimates >= 4; } }
            public void Reset() { goodEstimates = 0; }
            public long ExpectedTime100ns(long curTimeQPC) { return (long)(((curTimeQPC - baseTimeQPC) / QPCsPer100ns) + .5) + baseTime100ns; }

            public long Update(long actualTime100ns, long actualTimeQPC, long expectedTime100ns)
            {
                long delta100ns = actualTime100ns - firstTime100ns;
                if (QPCsPer100ns == 0)
                {
                    if (firstTime100ns == 0)
                    {
                        firstTimeQPC = baseTimeQPC = actualTimeQPC;
                        firstTime100ns = baseTime100ns = actualTime100ns;
                    }
                    else if (delta100ns != 0)
                        QPCsPer100ns = ((double)(actualTimeQPC - firstTimeQPC)) / delta100ns;
                    return long.MaxValue;
                }

                long delta = Math.Abs(actualTime100ns - expectedTime100ns);
                if (!Initialized)
                {
                    QPCsPer100ns = ((double)(actualTimeQPC - firstTimeQPC)) / delta100ns;
                    baseTimeQPC = actualTimeQPC;
                    baseTime100ns = actualTime100ns;

                    // Are we pretty certain that our estimate hit the 'right' event.  
                    if (delta < 5)
                    {
                        if (goodEstimates == 0)
                        {
                            firstTime100ns = actualTime100ns;
                            firstTimeQPC = actualTimeQPC;
                        }
                        goodEstimates++;
                    }
                    else
                        goodEstimates = 0;

                    if (Initialized)
                    {
                        long startOfYear = new DateTime(DateTime.Now.Year, 1, 1).ToFileTime();
                        double QPCStartTime = (firstTime100ns - startOfYear) - (firstTimeQPC / QPCsPer100ns);
                        /* Console.WriteLine("Got QPC params. proc:{0} QPCsPerf100ns:{1:f4} QPCStart:{2:f7} sec.",
                            procNum, QPCsPer100ns, QPCStartTime / 10000000.0);
                         */
                    }
                }
                else if (delta <= 50)
                {
                    if (delta < 5)
                        QPCsPer100ns = ((double)(actualTimeQPC - firstTimeQPC)) / delta100ns;
                    baseTimeQPC = actualTimeQPC;
                    baseTime100ns = actualTime100ns;
                }
                return delta;
            }

            long baseTimeQPC;                           // The time in QueryPerformanceCounter ticks
            long baseTime100ns;                         // The time in standard 100ns ticks
            long firstTimeQPC;
            long firstTime100ns;
            double QPCsPer100ns;                        // Number of QueryPerformanceCounter ticks per 100ns
            int goodEstimates;
            int procNum;                                // Processor number (used for diagnostics)
            TraceEventSource source;                    // Used for diagnostics
        };

        #region EventsToStackIndex
        internal struct EventsToStackIndex
        {
            public EventsToStackIndex(EventIndex eventIndex, CallStackIndex stackIndex)
            {
                EventIndex = eventIndex;
                CallStackIndex = stackIndex;
            }
            public EventIndex EventIndex;
            public CallStackIndex CallStackIndex;
        }

        private GrowableArray<EventsToStackIndex>.Comparison<EventIndex> stackComparer = delegate(EventIndex eventID, EventsToStackIndex elem)
            { return TraceEvent.Compare(eventID, elem.EventIndex); };

        #endregion

        #region EventsToCodeAddressIndex

        struct EventsToCodeAddressIndex
        {
            public EventsToCodeAddressIndex(EventIndex eventIndex, Address address, CodeAddressIndex codeAddressIndex)
            {
                EventIndex = eventIndex;
                Address = address;
                CodeAddressIndex = codeAddressIndex;
            }
            public EventIndex EventIndex;
            public Address Address;
            public CodeAddressIndex CodeAddressIndex;
        }
        private GrowableArray<EventsToCodeAddressIndex>.Comparison<EventIndex> CodeAddressComparer = delegate(EventIndex eventIndex, EventsToCodeAddressIndex elem)
            { return TraceEvent.Compare(eventIndex, elem.EventIndex); };

        #endregion


        // These are only used when converting from ETL
        private TraceEventDispatcher rawEventSourceToConvert;      // used to convert from raw format only.  Null for ETLX files.
        internal TraceLogOptions options;
        #endregion
    }

    public class TraceEvents : IEnumerable<TraceEvent>
    {
        IEnumerator<TraceEvent> IEnumerable<TraceEvent>.GetEnumerator()
        {
            if (this.backwards)
                return new TraceEvents.BackwardEventEnumerator(this);
            else
                return new TraceEvents.ForwardEventEnumerator(this);
        }
        public IEnumerable<T> ByEventType<T>() where T : TraceEvent
        {
            foreach (TraceEvent anEvent in this)
            {
                T asTypedEvent = anEvent as T;
                if (asTypedEvent != null)
                    yield return asTypedEvent;
            }
        }
        [CLSCompliant(false)] public TraceEventDispatcher GetSource() { return new ETLXTraceEventSource(this); }
        public TraceEvents Backwards()
        {
            return new TraceEvents(log, startTime100ns, endTime100ns, predicate, true);
        }
        /// <summary>
        /// Filter the events by time.  Startime is INCLUSIVE. 
        /// </summary>
        public TraceEvents FilterByTime(DateTime startTime)
        {
            return FilterByTime(startTime.ToFileTime());
        }
        /// <summary>
        /// Filter the events by time.  both startime and endTime are INCLUSIVE. 
        /// </summary>
        public TraceEvents FilterByTime(DateTime startTime, DateTime endTime)
        {
            return FilterByTime(startTime.ToFileTime(), endTime.ToFileTime());
        }
        /// <summary>
        /// Filter the events by time.  Startime is INCLUSIVE. 
        /// </summary>
        public TraceEvents FilterByTime(long startTime100ns)
        {
            return Filter(startTime100ns, long.MaxValue, null);
        }
        /// <summary>
        /// Filter the events by time.  both startTime100ns and endTime100ns are INCLUSIVE. 
        /// </summary>
        public TraceEvents FilterByTime(long startTime100ns, long endTime100ns)
        {
            return Filter(startTime100ns, endTime100ns, null);
        }
        public TraceEvents Filter(Predicate<TraceEvent> predicate)
        {
            return Filter(0, TraceEventDispatcher.MaxTime100ns, predicate);
        }

        [CLSCompliant(false)] public TraceLog Log { get { return log; } }

        /// <summary>
        /// StartTime100ns for a code:TraceEvents is defined to be any time of the first event (or any time
        /// before it and after any event in the whole log that is before the first event in the
        /// TraceEvents).   
        /// </summary>
        public long StartTime100ns { get { return startTime100ns; } }
        public DateTime StartTime { get { return DateTime.FromFileTime(startTime100ns); } }
        public double RelativeStartTime { get { return log.RelativeTimeMSec(startTime100ns); } }
        public long EndTime100ns { get { return endTime100ns; } }
        public DateTime EndTime { get { return DateTime.FromFileTime(endTime100ns); } }
        public double RelativeEndTime { get { return log.RelativeTimeMSec(endTime100ns); } }

        #region private
        internal TraceEvents(TraceLog log)
        {
            this.log = log;
            this.endTime100ns = long.MaxValue;
        }
        internal TraceEvents(TraceLog log, long startTime100ns, long endTime100ns, Predicate<TraceEvent> predicate, bool backwards)
        {
            this.log = log;
            this.startTime100ns = startTime100ns;
            this.endTime100ns = endTime100ns;
            this.predicate = predicate;
            this.backwards = backwards;
        }

        internal TraceEvents Filter(long startTime100ns, long endTime100ns, Predicate<TraceEvent> predicate)
        {
            // merge the two predicates
            if (predicate == null)
                predicate = this.predicate;
            else if (this.predicate != null)
            {
                Predicate<TraceEvent> predicate1 = this.predicate;
                Predicate<TraceEvent> predicate2 = predicate;
                predicate = delegate(TraceEvent anEvent)
                {
                    return predicate1(anEvent) && predicate2(anEvent);
                };
            }
            return new TraceEvents(log,
                Math.Max(startTime100ns, this.startTime100ns),
                Math.Min(endTime100ns, this.endTime100ns),
                predicate, this.backwards);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }

        internal abstract class EventEnumeratorBase
        {
            protected EventEnumeratorBase(TraceEvents events)
            {
                this.events = events;
                reader = events.Log.AllocReader();
                lookup = events.Log.AllocLookup();
            }
            public TraceEvent Current { get { return current; } }
            public void Dispose()
            {
                events.Log.FreeReader(reader);
                events.Log.FreeLookup(lookup);
            }
            public void Reset()
            {
                throw new Exception("The method or operation is not implemented.");
            }
            protected unsafe TraceEvent GetNext()
            {
                TraceEventNativeMethods.EVENT_RECORD* ptr = reader.GetPointer(TraceLog.headerSize);
                TraceEvent ret = lookup.Lookup(ptr);

                // This first check is just a perf optimization so in the common case we don't to
                // the extra logic 
                if (ret.opcode == unchecked((TraceEventOpcode)(-1)))
                {
                    UnhandledTraceEvent unhandled = ret as UnhandledTraceEvent;
                    if (unhandled != null)
                        unhandled.PrepForCallback();
                }
                Debug.Assert(ret.source == events.log);

                // Confirm we have a half-way sane event, to catch obvious loss of sync.  
                Debug.Assert(ret.Level <= (TraceEventLevel)64);
                Debug.Assert(ret.Version <= 4);
                Debug.Assert(ret.TimeStamp100ns == long.MaxValue ||
                    events.Log.SessionStartTime100ns <= ret.TimeStamp100ns && ret.TimeStamp100ns <= events.Log.SessionEndTime100ns);

                // We have to insure we have a pointer to the whole blob, not just the header.  
                int totalLength = TraceLog.headerSize + (ret.EventDataLength + 3 & ~3);
                Debug.Assert(totalLength < 0x20000);
                ret.eventRecord = reader.GetPointer(totalLength);
                ret.userData = TraceEventRawReaders.Add((IntPtr)ret.eventRecord, TraceLog.headerSize);
                reader.Skip(totalLength);
                return ret;
            }

            protected TraceEvent current;
            protected TraceEvents events;
            protected internal PinnedStreamReader reader;
            protected internal TraceEventDispatcher lookup;
            protected StreamLabel[] positions;
            protected int indexOnPage;
            protected int pageIndex;
        }

        internal sealed class ForwardEventEnumerator : EventEnumeratorBase, IEnumerator<TraceEvent>
        {
            public ForwardEventEnumerator(TraceEvents events)
                : base(events)
            {
                pageIndex = events.Log.FindPageIndex(events.startTime100ns);
                events.Log.SeekToTimeOnPage(reader, events.startTime100ns, pageIndex, out indexOnPage, positions);
                lookup.currentID = (EventIndex)(pageIndex * TraceLog.eventsPerPage + indexOnPage);
            }
            public bool MoveNext()
            {
                for (; ; )
                {
                    current = GetNext();
                    if (current.TimeStamp100ns > events.endTime100ns || current.TimeStamp100ns == long.MaxValue)
                        return false;

                    // TODO confirm this works with nested predicates
                    if (events.predicate == null || events.predicate(current))
                        return true;
                }
            }
            public new object Current { get { return current; } }
        }

        internal sealed class BackwardEventEnumerator : EventEnumeratorBase, IEnumerator<TraceEvent>
        {
            public BackwardEventEnumerator(TraceEvents events)
                : base(events)
            {
                long endTime = events.endTime100ns;
                if (endTime != long.MaxValue)
                    endTime++;
                pageIndex = events.Log.FindPageIndex(endTime);
                positions = new StreamLabel[TraceLog.eventsPerPage];
                events.Log.SeekToTimeOnPage(reader, endTime, pageIndex, out indexOnPage, positions);
            }
            public bool MoveNext()
            {
                for (; ; )
                {
                    if (indexOnPage == 0)
                    {
                        if (pageIndex == 0)
                            return false;
                        --pageIndex;
                        events.Log.SeekToTimeOnPage(reader, long.MaxValue, pageIndex, out indexOnPage, positions);
                    }
                    else
                        --indexOnPage;
                    reader.Goto(positions[indexOnPage]);
                    lookup.currentID = (EventIndex)(pageIndex * TraceLog.eventsPerPage + indexOnPage);
                    current = GetNext();

                    if (current.TimeStamp100ns < events.startTime100ns)
                        return false;

                    // TODO confirm this works with nested predicates
                    if (events.predicate == null || events.predicate(current))
                        return true;
                }
            }
            public new object Current { get { return current; } }
        }

        private TraceEvent GetTemplateAtStreamLabel(StreamLabel label)
        {
            return null;
        }

        // #TraceEventVars
        // see code:#TraceLogVars
        internal TraceLog log;
        internal long startTime100ns;
        internal long endTime100ns;
        internal Predicate<TraceEvent> predicate;
        internal bool backwards;
        #endregion
    }

    /// <summary>
    /// We give each process a unique index from 0 to code:TraceProcesses.MaxProcessIndex. Thus it is unique
    /// within the whole code:TraceLog. You are explictly allowed take advantage of the fact that this number
    /// is in the range from 0 to code:TracesProcesses.BatchCount (you can create arrays indexed by
    /// code:ProcessIndex). We create the Enum because the strong typing avoids a class of user errors.
    /// </summary>
    public enum ProcessIndex { Invalid = -1 };

    /// <summary>
    /// A code:TraceProcesses represents the list of procsses in the Event log.  
    /// 
    /// TraceProcesses are IEnumerable, and will return the processes in order of time created.   
    /// </summary>
    public sealed class TraceProcesses : IEnumerable<TraceProcess>, IFastSerializable
    {
        /// <summary>
        /// Enumerate all the threads that occured in the trace log.  It does so in order of their process
        /// offset events in the log.  
        /// </summary> 
        IEnumerator<TraceProcess> IEnumerable<TraceProcess>.GetEnumerator()
        {
            for (int i = 0; i < processes.Count; i++)
                yield return processes[i];
        }
        /// <summary>
        /// The log associated with this collection of threads. 
        /// </summary>
        [CLSCompliant(false)] public TraceLog Log { get { return log; } }
        /// <summary>
        /// The count of the number of code:TraceProcess s in the trace log. 
        /// </summary>
        public int MaxProcessIndex { get { return processes.Count; } }
        /// <summary>
        /// Each process that occurs in the log is given a unique index (which unlike the PID is unique), that
        /// ranges from 0 to code:BatchCount - 1.   Return the code:TraceProcess for the given index.  
        /// </summary>
        public TraceProcess this[ProcessIndex processIndex]
        {
            get
            {
                if (processIndex == ProcessIndex.Invalid)
                    return null;
                return processes[(int)processIndex];
            }
        }
        /// <summary>
        /// Given a OS process ID and a time, return the last code:TraceProcess that has the same process index,
        /// and whose offset time is less than 'time100ns'. If 'time100ns' is during the threads lifetime this
        /// is guarenteed to be the correct process. Using time100ns = code:TraceLog.SessionEndTime100ns will return the
        /// last process with the given PID, even if it had died.
        /// 
        /// Generally using code:TraceLog.GetProcessForEvent is a more convinient way to get a code:TraceProcess
        /// associated with an event.  
        /// </summary>
        public TraceProcess GetProcess(int processID, long time100ns)
        {
            int index;
            return FindProcessAndIndex(processID, time100ns, out index);
        }
        /// <summary>
        /// Given a thread ID and a time, find the process associated with the thread.  
        /// </summary>
        public TraceProcess GetProcessForThreadID(int threadID, long time100ns)
        {
            if (threadIDToProcess == null)
            {
                threadIDToProcess = new HistoryDictionary<TraceProcess>(200);
                foreach (TraceProcess process in processes)
                    foreach (TraceThread thread in process.Threads)
                        threadIDToProcess.Add((Address)thread.ThreadID, thread.startTime100ns, process);
            }
            TraceProcess ret;
            threadIDToProcess.TryGetValue((Address)threadID, time100ns, out ret);
            return ret;
        }

        /// <summary>
        /// Return the last process in the log with the given process ID.  Useful when the logging session
        /// was stopped just after the processes completed (a common scenario).  
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public TraceProcess LastProcessWithID(int processID)
        {
            return GetProcess(processID, Log.SessionEndTime100ns);
        }

        /// <summary>
        /// Gets the first process (in time) that has the name 'processName'. The name of a process is the file
        /// name (not full path), without its extension. Returns null on failure
        /// </summary>
        public TraceProcess FirstProcessWithName(string processName)
        {
            return FirstProcessWithName(processName, 0);
        }
        /// <summary>
        /// Gets the first process (in time) that has the name 'processName' that started after 'afterTime'
        /// (inclusive). The name of a process is the file name (not full path), without its extension. Returns
        /// null on failure
        /// </summary>
        public TraceProcess FirstProcessWithName(string processName, long afterTime100ns)
        {
            for (int i = 0; i < MaxProcessIndex; i++)
            {
                TraceProcess process = processes[i];
                if (afterTime100ns <= process.StartTime100ns && string.Compare(process.Name, processName, StringComparison.OrdinalIgnoreCase) == 0)
                    return process;
            }
            return null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TraceProcesses Count=").Append(XmlUtilities.XmlQuote(MaxProcessIndex)).AppendLine(">");
            foreach (TraceProcess process in this)
                sb.Append("  ").Append(process.ToString()).AppendLine();
            sb.AppendLine("</TraceProcesses>");
            return sb.ToString();
        }
        #region Private
        /// <summary>
        /// TraceProcesses represents the entire ETL moduleFile log.   At the node level it is organized by threads.  
        /// 
        /// The TraceProcesses also is where we put various caches that are independent of the process involved. 
        /// These include a cache for code:TraceModuleFile that represent native images that can be loaded into a
        /// process, as well as the process lookup tables and a cache that remembers the last calls to
        /// GetNameForAddress(). 
        /// </summary>
        internal TraceProcesses(TraceLog log)
        {
            this.log = log;
            this.processes = new GrowableArray<TraceProcess>(64);
            this.processesByPID = new GrowableArray<TraceProcess>(64);
        }
        internal TraceProcess GetOrCreateProcess(int processID, long time100ns)
        {
            Debug.Assert(processes.Count == processesByPID.Count);
            int index;
            TraceProcess newProcess = FindProcessAndIndex(processID, time100ns, out index);
            if (newProcess == null)
            {
                newProcess = new TraceProcess(processID, log, (ProcessIndex)processes.Count);
                processes.Add(newProcess);
                processesByPID.Insert(index + 1, newProcess);
            }
            return newProcess;
        }
        internal TraceProcess FindProcessAndIndex(int processID, long time100ns, out int index)
        {
            if (processesByPID.BinarySearch(processID, out index, compareByProcessID))
            {
                for (int candidateIndex = index; candidateIndex >= 0; --candidateIndex)
                {
                    TraceProcess candidate = processesByPID[candidateIndex];
                    if (candidate.ProcessID != processID)
                        break;

                    // Note that I take the last process that has a offset time that preceeds this one. We do this
                    // because some events associated with the process (eg ImageUnload, can seem to occur after
                    // the process has indicated it ended)
                    if (candidate.StartTime100ns <= time100ns)
                    {
                        index = candidateIndex;
                        return candidate;
                    }
                    else
                    {
                        // Early in the log, before all the Process DCStart have happened, you can get this.
                        // TODO This is mostly for my debugging, as it should rarely happen 
                        if (Log.RelativeTimeMSec(time100ns) > 1000)
                            Log.DebugWarn(false, "Event at " + Log.RelativeTimeMSec(time100ns) + " used process ID " + processID + " before process start.", null);
                    }
                }
            }
            return null;
        }

        // State variables.  
        private GrowableArray<TraceProcess> processes;          // The threads ordered in time. 
        private GrowableArray<TraceProcess> processesByPID;     // The threads ordered by processID.  
        private TraceLog log;

        // This is lazily created.  It holds a reverse map from thread IDs to the process that contains them.
        public HistoryDictionary<TraceProcess> threadIDToProcess;  // TODO hack

        static public GrowableArray<TraceProcess>.Comparison<int> compareByProcessID = delegate(int processID, TraceProcess process)
        {
            return (processID - process.ProcessID);
        };
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(log);
            serializer.Log("<WriteColection name=\"Processes\" count=\"" + processes.Count + "\">\r\n");
            serializer.Write(processes.Count);
            for (int i = 0; i < processes.Count; i++)
                serializer.Write(processes[i]);
            serializer.Log("</WriteColection>\r\n");

            serializer.Log("<WriteColection name=\"ProcessesByPID\" count=\"" + processesByPID.Count + "\">\r\n");
            serializer.Write(processesByPID.Count);
            for (int i = 0; i < processesByPID.Count; i++)
                serializer.Write(processesByPID[i]);
            serializer.Log("</WriteColection>\r\n");
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out log);

            Debug.Assert(processes.Count == 0);
            int count = deserializer.ReadInt();
            processes = new GrowableArray<TraceProcess>(count + 1);
            for (int i = 0; i < count; i++)
            {
                TraceProcess elem; deserializer.Read(out elem);
                processes.Add(elem);
            }

            count = deserializer.ReadInt();
            processesByPID = new GrowableArray<TraceProcess>(count + 1);
            for (int i = 0; i < count; i++)
            {
                TraceProcess elem; deserializer.Read(out elem);
                processesByPID.Add(elem);
            }
        }

        #endregion
    }

    /// <summary>
    /// A code:TraceProcess represents a process.  
    /// 
    /// </summary>
    public sealed class TraceProcess : IFastSerializable
    {
        /// <summary>
        /// The OS process ID associated with the process.   It is NOT unique across the whole log.  Use
        /// code:ProcessIndex for if you need that. 
        /// </summary>
        [Obsolete("Use ProcessID")]
        public int ProcessId { get { return processID; } }
        [CLSCompliant(false)]
        public int ProcessID { get { return processID; } }
        /// <summary>
        /// The index into the logical array of code:TraceProcesses for this process.  Unlike ProcessIndex (which
        /// may be reused after the process dies, the process index is unique over the log.  
        /// </summary>
        public ProcessIndex ProcessIndex { get { return processIndex; } }
        /// <summary>
        /// The log file associated with the process. 
        /// </summary>
        [CLSCompliant(false)] public TraceLog Log { get { return log; } }

        public string CommandLine { get { return commandLine; } }
        public string ImageFileName { get { return imageFileName; } }

        /// <summary>
        /// This is a short name for the process.  It is the image file name without the path or suffix.  
        /// </summary>
        public string Name
        {
            get
            {
                if (name == null)
                    name = Path.GetFileNameWithoutExtension(ImageFileName);
                return name;
            }
        }
        public DateTime StartTime { get { return DateTime.FromFileTime(StartTime100ns); } }
        public double StartTimeRelativeMsec { get { return Log.RelativeTimeMSec(StartTime100ns); } }
        public long StartTime100ns { get { return startTime100ns; } }
        public DateTime EndTime { get { return DateTime.FromFileTime(EndTime100ns); } }
        public double EndTimeRelativeMsec { get { return Log.RelativeTimeMSec(EndTime100ns); } }
        public long EndTime100ns { get { return endTime100ns; } }
        [Obsolete("Use ParentID")]
        public int ParentId { get { return parentID; } }
        [CLSCompliant(false)]
        public int ParentID { get { return parentID; } }
        public TraceProcess Parent { get { return parent; } }
        public int? ExitStatus { get { return exitStatus; } }

        /// <summary>
        /// Filters events to only those for a particular process. 
        /// </summary>
        public TraceEvents EventsInProcess
        {
            get
            {
                return log.Events.Filter(StartTime100ns, EndTime100ns, delegate(TraceEvent anEvent)
                {
                    // FIX Virtual allocs
                    if (anEvent.ProcessID == processID)
                        return true;
                    // FIX Virtual alloc's Process ID? 
                    if (anEvent.ProcessID == -1)
                        return true;
                    return false;
                });
            }
        }
        /// <summary>
        /// Filters events to only that occured during the time a the process was alive. 
        /// </summary>
        /// 
        public TraceEvents EventsDuringProcess
        {
            get
            {
                return log.Events.FilterByTime(StartTime100ns, EndTime100ns);
            }
        }

        public TraceLoadedModules LoadedModules { get { return loadedModules; } }
        public TraceThreads Threads { get { return threads; } }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TraceProcess ");
            sb.Append("PID=").Append(XmlUtilities.XmlQuote(ProcessID)).Append(" ");
            // TODO null parent pointers should be impossible
            if (Parent != null)
                sb.Append("ParentPID=").Append(XmlUtilities.XmlQuote(Parent.ProcessID)).Append(" ");
            sb.Append("Exe=").Append(XmlUtilities.XmlQuote(Path.GetFileNameWithoutExtension(ImageFileName))).Append(" ");
            sb.Append("Start=").Append(XmlUtilities.XmlQuote(StartTimeRelativeMsec)).Append(" ");
            sb.Append("End=").Append(XmlUtilities.XmlQuote(EndTimeRelativeMsec)).Append(" ");
            if (ExitStatus.HasValue)
                sb.Append("ExitStatus=").Append(XmlUtilities.XmlQuote(ExitStatus.Value)).Append(" ");
            sb.Append("CommandLine=").Append(XmlUtilities.XmlQuote(CommandLine)).Append(" ");
            sb.Append("/>");
            return sb.ToString();
        }

        #region Private
        #region EventHandlersCalledFromTraceLog
        // #ProcessHandlersCalledFromTraceLog
        // 
        // called from code:TraceLog.CopyRawEvents
        internal void ProcessStart(ProcessTraceData data)
        {
            Log.DebugWarn(StartTime100ns == 0, "Events for process happen before process start.  PrevEventTime: " + Log.RelativeTimeMSec(StartTime100ns), data);
            Debug.Assert(EndTime100ns == ETWTraceEventSource.MaxTime100ns); // We would create a new Process record otherwise 

            if (data.Opcode == TraceEventOpcode.DataCollectionStart)
                this.startTime100ns = log.SessionStartTime100ns;
            else
            {
                Debug.Assert(data.Opcode == TraceEventOpcode.Start);
                this.startTime100ns = data.TimeStamp100ns;
            }
            this.commandLine = data.CommandLine;
            this.imageFileName = data.ImageFileName;
            this.parentID = data.ParentID;
            this.parent = log.Processes.GetProcess(data.ParentID, data.TimeStamp100ns);
        }
        internal void ProcessEnd(ProcessTraceData data)
        {
            Log.DebugWarn(EndTime100ns == ETWTraceEventSource.MaxTime100ns, "Multiple Ends for process. PrevEndTime: " + Log.RelativeTimeMSec(EndTime100ns), data);
            Log.DebugWarn(StartTime100ns != 0, "Process End without a start.", data);

            if (data.Opcode == TraceEventOpcode.DataCollectionStop)
                this.endTime100ns = log.SessionEndTime100ns;
            else
            {
                Debug.Assert(data.Opcode == TraceEventOpcode.Stop);
                // Only set the exit code if it really is a process exit (not a DCEnd). 
                if (data.Opcode == TraceEventOpcode.Stop)
                    this.exitStatus = data.ExitStatus;
                this.endTime100ns = data.TimeStamp100ns;
            }
            Log.DebugWarn(StartTime100ns <= EndTime100ns, "Process Ends before it starts! StartTime: " + Log.RelativeTimeMSec(StartTime100ns), data);
        }
        #endregion

        /// <summary>
        /// Create a new code:TraceProcess.  It should only be done by code:log.CreateTraceProcess because
        /// only code:TraceLog is responsible for generating a new ProcessIndex which we need.   'processIndex'
        /// is a index that is unique for the whole log file (where as processID can be reused).  
        /// </summary>
        internal TraceProcess(int processID, TraceLog log, ProcessIndex processIndex)
        {
            this.log = log;
            this.processID = processID;
            this.processIndex = processIndex;
            this.endTime100ns = ETWTraceEventSource.MaxTime100ns;
            this.commandLine = "";
            this.imageFileName = "";
            this.loadedModules = new TraceLoadedModules(this);
            this.threads = new TraceThreads(this);
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(processID);
            serializer.Write((int)processIndex);
            serializer.Write(log);
            serializer.Write(commandLine);
            serializer.Write(imageFileName);
            serializer.Write(startTime100ns);
            serializer.Write(endTime100ns);
            serializer.Write(exitStatus);
            serializer.Write(parentID);
            serializer.Write(parent);
            serializer.Write(threads);
            serializer.Write(loadedModules);
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out processID);
            int processIndex; deserializer.Read(out processIndex); this.processIndex = (ProcessIndex)processIndex;
            deserializer.Read(out log);
            deserializer.Read(out commandLine);
            deserializer.Read(out imageFileName);
            deserializer.Read(out startTime100ns);
            deserializer.Read(out endTime100ns);
            deserializer.Read(out exitStatus);
            deserializer.Read(out parentID);
            deserializer.Read(out parent);
            deserializer.Read(out threads);
            deserializer.Read(out loadedModules);
        }

        private int processID;
        internal ProcessIndex processIndex;
        private TraceLog log;

        private string commandLine;
        private string imageFileName;
        private string name;
        private long startTime100ns;
        private long endTime100ns;
        private int? exitStatus;
        private int parentID;
        private TraceProcess parent;

        private TraceLoadedModules loadedModules;
        private TraceThreads threads;
        #endregion
    }

    /// <summary>
    /// We give each process a unique index from 0 to code:TraceThreads.MaxThreadIndex. Thus it is unique
    /// within the whole code:TraceProcess. You are explictly allowed take advantage of the fact that this
    /// number is in the range from 0 to code:TracesThreads.BatchCount (you can create arrays indexed by
    /// code:ThreadIndex). We create the Enum because the strong typing avoids a class of user errors.
    /// </summary>
    public enum ThreadIndex { Invalid = -1 };

    /// <summary>
    /// A code:TraceThreads represents the list of threads in a process. 
    /// </summary>
    public sealed class TraceThreads : IEnumerable<TraceThread>, IFastSerializable
    {
        /// <summary>
        /// Enumerate all the threads that occured in the trace log.  It does so in order of their thread
        /// offset events in the log.  
        /// </summary> 
        IEnumerator<TraceThread> IEnumerable<TraceThread>.GetEnumerator()
        {
            for (int i = 0; i < threads.Count; i++)
                yield return threads[i];
        }
        /// <summary>
        /// The process associated with this collection of threads. 
        /// </summary>
        public TraceProcess Process { get { return process; } }
        /// <summary>
        /// The count of the number of code:TraceThread s in the trace log. 
        /// </summary>
        public int MaxThreadIndex { get { return threads.Count; } }
        /// <summary>
        /// Each thread that occurs in the log is given a unique index (which unlike the PID is unique), that
        /// ranges from 0 to code:BatchCount - 1.   Return the code:TraceThread for the given index.  
        /// </summary>
        public TraceThread this[ThreadIndex threadIndex]
        {
            get
            {
                if (threadIndex == ThreadIndex.Invalid)
                    return null;
                return threads[(int)threadIndex];
            }
        }
        /// <summary>
        /// Given a OS thread ID and a time, return the last code:TraceThread that has the same thread index,
        /// and whose offset time is less than 'time100ns'. If 'time100ns' is during the threads lifetime this
        /// is guarenteed to be the correct thread. 
        /// </summary>
        public TraceThread GetThread(int threadID, long time100ns)
        {
            for (int i = threads.Count - 1; i >= 0; --i)
            {
                TraceThread thread = threads[i];
                if (thread.StartTime100ns <= time100ns && thread.ThreadID == threadID)
                    return thread;
            }
            return null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TraceThreads Count=").Append(XmlUtilities.XmlQuote(MaxThreadIndex)).AppendLine(">");
            foreach (TraceThread thread in this)
                sb.Append("  ").Append(thread.ToString()).AppendLine();
            sb.AppendLine("</TraceThreads>");
            return sb.ToString();
        }
        #region Private
        /// <summary>
        /// TraceThreads   represents the collection of threads in a process. 
        /// 
        /// </summary>
        internal TraceThreads(TraceProcess process)
        {
            this.process = process;
        }
        internal TraceThread GetOrCreateThread(int threadID, long time100ns)
        {
            Debug.Assert(threads.Count == threads.Count);
            TraceThread newThread = GetThread(threadID, time100ns);
            if (newThread == null)
            {
                newThread = new TraceThread(threadID, process, (ThreadIndex)threads.Count);
                threads.Add(newThread);
            }
            return newThread;
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(process);

            serializer.Log("<WriteColection name=\"threads\" count=\"" + threads.Count + "\">\r\n");
            serializer.Write(threads.Count);
            for (int i = 0; i < threads.Count; i++)
                serializer.Write(threads[i]);
            serializer.Log("</WriteColection>\r\n");
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out process);
            Debug.Assert(threads.Count == 0);
            int count = deserializer.ReadInt();
            threads = new GrowableArray<TraceThread>(count + 1);

            for (int i = 0; i < count; i++)
            {
                TraceThread elem; deserializer.Read(out elem);
                threads.Add(elem);
            }
        }
        // State variables.  
        private GrowableArray<TraceThread> threads;          // The threads ordered in time. 
        private TraceProcess process;

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }
        #endregion
    }

    /// <summary>
    /// A code:TraceThread represents a tread of execution in a process.  
    /// </summary>
    public sealed class TraceThread : IFastSerializable
    {
        /// <summary>
        /// The OS process ID associated with the process. 
        /// </summary>
        [Obsolete("Used ThreadId")]
        public int ThreadId { get { return threadID; } }
        [CLSCompliant(false)]
        public int ThreadID { get { return threadID; } }
        /// <summary>
        /// The index into the logical array of code:TraceProcesses for this process.  Unlike ProcessIndex (which
        /// may be reused after the process dies, the process index is unique over the log.  
        /// </summary>
        public ThreadIndex ThreadIndex { get { return threadIndex; } }
        /// <summary>
        /// The process associated with the thread. 
        /// </summary>
        public TraceProcess Process { get { return process; } }
        public DateTime StartTime { get { return DateTime.FromFileTime(StartTime100ns); } }
        public double StartTimeRelative { get { return process.Log.RelativeTimeMSec(StartTime100ns); } }
        public long StartTime100ns { get { return startTime100ns; } }
        public DateTime EndTime { get { return DateTime.FromFileTime(EndTime100ns); } }
        public double EndTimeRelative { get { return process.Log.RelativeTimeMSec(EndTime100ns); } }
        public long EndTime100ns { get { return endTime100ns; } }
        /// <summary>
        /// Filters events to only those for a particular thread. 
        /// </summary>
        public TraceEvents EventsInThread
        {
            get
            {
                return Process.Log.Events.Filter(StartTime100ns, EndTime100ns, delegate(TraceEvent anEvent)
                {
                    return anEvent.ThreadID == ThreadID;
                });
            }
        }
        /// <summary>
        /// Filters events to only that occured during the time a the thread was alive. 
        /// </summary>
        /// 
        public TraceEvents EventsDuringThread
        {
            get
            {
                return Process.Log.Events.FilterByTime(StartTime100ns, EndTime100ns);
            }
        }
        public override string ToString()
        {
            return "<TraceThread " +
                    "TID=" + XmlUtilities.XmlQuote(ThreadID).PadRight(5) + " " +
                    "StartTimeRelative=" + XmlUtilities.XmlQuote(StartTimeRelative).PadRight(8) + " " +
                    "EndTimeRelative=" + XmlUtilities.XmlQuote(EndTimeRelative).PadRight(8) + " " +
                   "/>";
        }

        #region Private
        #region EventHandlersCalledFromTraceLog
        // #EventHandlersCalledFromTraceLog
        internal void ThreadStart(ThreadTraceData data)
        {
            process.Log.DebugWarn(StartTime100ns == 0, "Events for process happen before process start.  PrevEventTime: " + process.Log.RelativeTimeMSec(StartTime100ns), data);
            Debug.Assert(EndTime100ns == ETWTraceEventSource.MaxTime100ns); // We would create a new Thread record otherwise 

            this.startTime100ns = data.TimeStamp100ns;
        }
        internal void ThreadEnd(ThreadTraceData data)
        {
            process.Log.DebugWarn(EndTime100ns == ETWTraceEventSource.MaxTime100ns, "Multiple Ends for process. PrevEndTime: " + process.Log.RelativeTimeMSec(EndTime100ns), data);
            process.Log.DebugWarn(StartTime100ns != 0, "Thread End without a start.", data);

            this.endTime100ns = data.TimeStamp100ns;
            process.Log.DebugWarn(StartTime100ns <= EndTime100ns, "Thread Ends before it starts! StartTime: " + process.Log.RelativeTimeMSec(StartTime100ns), data);
        }
        #endregion

        /// <summary>
        /// Create a new code:TraceProcess.  It should only be done by code:log.CreateTraceProcess because
        /// only code:TraceLog is responsible for generating a new ProcessIndex which we need.   'processIndex'
        /// is a index that is unique for the whole log file (where as processID can be reused).  
        /// </summary>
        internal TraceThread(int threadID, TraceProcess process, ThreadIndex threadIndex)
        {
            this.threadID = threadID;
            this.threadIndex = threadIndex;
            this.process = process;
            this.endTime100ns = ETWTraceEventSource.MaxTime100ns;
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(threadID);
            serializer.Write((int)threadIndex);
            serializer.Write(process);
            serializer.Write(startTime100ns);
            serializer.Write(endTime100ns);
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out threadID);
            int threadIndex; deserializer.Read(out threadIndex); this.threadIndex = (ThreadIndex)threadIndex;
            deserializer.Read(out process);
            deserializer.Read(out startTime100ns);
            deserializer.Read(out endTime100ns);
        }

        private int threadID;
        private ThreadIndex threadIndex;
        private TraceProcess process;
        internal long startTime100ns;
        internal long endTime100ns;

        #endregion
    }

    /// <summary>
    /// code:TraceLoadedModules represents the collection of static modules (loaded DLLs or EXEs that
    /// directly runnable) in a particular process.  
    /// </summary>
    public sealed class TraceLoadedModules : IEnumerable<TraceLoadedModule>, IFastSerializable
    {
        // TODO do we want a LoadedModuleIndex?
        public TraceProcess Process { get { return process; } }
        public IEnumerator<TraceLoadedModule> GetEnumerator()
        {
            for (int i = 0; i < modules.Count; i++)
                yield return modules[i];
        }
        /// <summary>
        /// Returns the managedModule with the given moduleID.  For native images the managedModule ID is the image base.  For
        /// managed images the managedModule returned is always the IL managedModule. 
        /// TODO should managedModuleID be given an opaque type?
        /// </summary>
        public TraceManagedModule GetManagedModule(long managedModuleID, long time100ns)
        {
            // We put managed modules in by module ID. 
            int index;
            TraceLoadedModule module = FindModuleAndIndex((Address)managedModuleID, time100ns, out index);
            if (module == null)
                return null;

            // We may have found the NGEN image (since the module ID lives in the NGEN image).  We make both
            // the IL an NGEN image point to the IL image, so this should work in all cases. 
            return module.ManagedModule;
        }
        /// <summary>
        /// This function will find the module assocated with 'address' at 'time100ns' however it will only
        /// find modules that are mapped in memory (module assocated with JIT compiled methods will not be found).  
        /// </summary>
        [CLSCompliant(false)]
        public TraceLoadedModule GetModuleContainingAddress(Address address, long time100ns)
        {
            int index;
            TraceLoadedModule module = FindModuleAndIndex(address, time100ns, out index);
            return module;
        }

        /// <summary>
        /// Returns the module representing the unmanaged load of a file.  The code:TraceManagedModule can be
        /// fetched from the code:TraceLoadedModule.ManagedModule property.
        /// </summary>
        public TraceLoadedModule GetModule(string fileName, long time100ns)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                TraceLoadedModule module = modules[i];
                if (module.FileName == fileName && module.loadTime100ns <= time100ns && time100ns < module.unloadTime100ns)
                    return module;
            }
            return null;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TraceLoadedeModules Count=").Append(XmlUtilities.XmlQuote(modules.Count)).AppendLine(">");
            foreach (TraceLoadedModule module in this)
                sb.Append("  ").Append(module.ToString()).AppendLine();
            sb.AppendLine("</TraceLoadedModules>");
            return sb.ToString();
        }
        #region Private
        // #ModuleHandlersCalledFromTraceLog
        internal void ImageLoadOrUnload(ImageLoadTraceData data, bool isLoad)
        {
            TraceLoadedModule module = GetOrCreateModule(data.FileName, data.TimeStamp100ns, data.ImageBase);

            TraceModuleFile moduleFile = module.ModuleFile;
            Debug.Assert(moduleFile != null);
            Debug.Assert(module.ImageBase == data.ImageBase);

            process.Log.DebugWarn(string.Compare(module.ModuleFile.FileName, data.FileName, StringComparison.OrdinalIgnoreCase) == 0,
                "FileName Load/Unload mismatch.\r\n    FILE1: " + module.ModuleFile.FileName, data);
            process.Log.DebugWarn(module.ModuleFile.ImageSize == 0 || module.ModuleFile.ImageSize == data.ImageSize,
                "ImageSize not consistant over all Loads Size 0x" + module.ModuleFile.ImageSize.ToString("x"), data);
            /* TODO this one fails.  decide what to do about it. 
            process.Log.DebugWarn(module.ModuleFile.DefaultBase == 0 || module.ModuleFile.DefaultBase == data.DefaultBase,
                "DefaultBase not consistant over all Loads Size 0x" + module.ModuleFile.DefaultBase.ToString("x"), data);
             ***/

            moduleFile.imageSize = data.ImageSize;
            moduleFile.defaultBase = data.DefaultBase;
            if (isLoad)
            {
                process.Log.DebugWarn(module.loadTime100ns == 0 || data.Opcode == TraceEventOpcode.DataCollectionStart, "Events for module happened before load.  PrevEventTime: " + process.Log.RelativeTimeMSec(module.loadTime100ns), data);
                process.Log.DebugWarn(data.TimeStamp100ns < module.unloadTime100ns, "Unload time < load time!", data);

                module.loadTime100ns = data.TimeStamp100ns;
                if (data.Opcode == TraceEventOpcode.DataCollectionStart)
                    module.loadTime100ns = process.Log.SessionStartTime100ns;
            }
            else
            {
                process.Log.DebugWarn(module.loadTime100ns != 0, "Unloading image not loaded.", data);
                process.Log.DebugWarn(module.loadTime100ns < data.TimeStamp100ns, "Unload time < load time!", data);
                process.Log.DebugWarn(module.unloadTime100ns == ETWTraceEventSource.MaxTime100ns, "Unloading a image twice PrevUnloadTime: " + process.Log.RelativeTimeMSec(module.unloadTime100ns), data);
                module.unloadTime100ns = data.TimeStamp100ns;
                if (data.Opcode == TraceEventOpcode.DataCollectionStop)
                    module.unloadTime100ns = process.Log.SessionEndTime100ns;
            }
            CheckClassInvarients();
        }
        internal void ManagedModuleLoadOrUnload(ModuleLoadUnloadTraceData data, bool isLoad)
        {
#if false
            int index;

            // Try to look up the managed module by its module ID, remember the index you found.   
            long moduleID = data.ModuleIdentifier;
            TraceLoadedModule module = FindModuleAndIndex((Address)moduleID, data.TimeStamp100ns, out index);
            TraceManagedModule managedModule = null;
            TraceLoadedModule nativeModule = null;
            if (module != null)
            {
                // the moduleID lookup is just looking for an address range that matches. It might find the NGEN
                // image (in which case it is just a TraceLoadedModule), and it might find the IL Module (in which
                // case it is a TraceManagedModule.   If it is the NGEN image, it will hold a pointer to the IL
                // module (if present)  
                managedModule = module as TraceManagedModule;
                if (managedModule == null)
                {
                    nativeModule = module;
                    managedModule = nativeModule.ManagedModule;
                    if (managedModule != null && managedModule.ModuleID != moduleID)
                    {
                        process.Log.DebugWarn(false, "Module ID found in a non-matching NGEN image", data);
                        nativeModule = null;
                        managedModule = null;
                    }
                }
            }

            // If there is an NGEN image, get it. 
            string nativePath = data.ModuleNativePath;
            if (nativePath.Length > 0)
            {
                if (nativeModule == null)
                {
                    // Look it up by name. 
                    nativeModule = GetModule(nativePath, data.TimeStamp100ns);
                    if (nativeModule == null)
                    {
                        // This is an unusual condition.  Normally native images should be loaded before the
                        // CLR claims that the module is loaded.  However for data colllection offset events,
                        // the CLR and the OS can be out of sync, so ignore the warning for this case.  
                        process.Log.DebugWarn(data.OpcodeName == "ModuleDCStart", "No Load event for native image.", data);

                        // ulong.MaxValue is used as an 'Not yet defined' value.  see code:GetOrCreateModule for
                        // more 
                        nativeModule = GetOrCreateModule(nativePath, data.TimeStamp100ns, (Address)ulong.MaxValue);
                    }
                }
                else
                    process.Log.DebugWarn(string.Compare(nativePath, nativeModule.FileName, StringComparison.OrdinalIgnoreCase) == 0,
                         "Inconsistant native images for managed module: " + nativeModule.FileName, data);

                Debug.Assert(nativeModule != null);     // At this point we have made one one way or the other. 
            }

            // If there was no managed module (expected for Load events), create it.  
            if (managedModule == null)
            {
                process.Log.DebugWarn(isLoad, " Unloading image not loaded.", data);

                string ilPath = data.ModuleILPath;
                TraceModuleFile moduleFile = null;

                TraceLoadedModule ilModule = null;              // non-null if there is a LoadLibary of the IL image
                if (ilPath.Length > 0)
                {
                    ilModule = GetOrCreateModule(ilPath, data.TimeStamp100ns, (Address)ulong.MaxValue);
                    if (ilModule != null)
                        moduleFile = ilModule.ModuleFile;
                    if (moduleFile == null)
                        moduleFile = process.Log.ModuleFiles.GetOrCreateModuleFile(data.ModuleILPath, Address.Null);

                    // Data Collection offset events can be out ot sync, so don't warn on DCStarts.  
                    process.Log.DebugWarn(moduleFile.ImageSize == 0 || data.OpcodeName == "ModuleDCStart", "No Load event for IL image.", data);
                }
                managedModule = new TraceManagedModule(moduleID, data.AssemblyIdentifier, data.IsDomainNeutral, moduleFile, nativeModule, process);
                modules.Insert(index + 1, managedModule);

                // Link up the back pointers from the unmanaged modules to the managed counterparts.  
                if (ilModule != null)
                {
                    ilModule.managedModule = managedModule;
                    managedModule.imageBase = ilModule.imageBase;
                }
                if (nativeModule != null)
                    nativeModule.managedModule = managedModule;
            }
            else
                process.Log.DebugWarn(!isLoad, "Loading image twice.", data);

            Debug.Assert(managedModule.ModuleID == moduleID);
            process.Log.DebugWarn(nativeModule == null || string.Compare(nativeModule.Name, 0, managedModule.Name, 0, managedModule.Name.Length, StringComparison.OrdinalIgnoreCase) == 0,
                "NGEN and IL module names do not match as expected.", data);

            if (isLoad)
                managedModule.loadTime100ns = data.TimeStamp100ns;
            else
                managedModule.unloadTime100ns = data.TimeStamp100ns;
            CheckClassInvarients();
#endif
        }

        /// <summary>
        /// Looks up a native modules by imageBase in the current process.  There is an unusual case where you
        /// may not know the image base, in which case ulong.MaxInt should be used.  Before failing we will
        /// check for these 'not yet assigned' images, and return them if the file names match.  
        /// </summary>
        private TraceLoadedModule GetOrCreateModule(string fileName, long timeCreated100ns, Address imageBase)
        {
            int index;
            TraceLoadedModule module = FindModuleAndIndex(imageBase, timeCreated100ns, out index);
            if (module == null)
            {
#if false 
                // Search for modules without a base address (that is ImageBase == ulong.MaxInt)
                for (int unassignedIndex = modules.Count - 1; unassignedIndex >= 0; --unassignedIndex)
                {
                    module = modules[unassignedIndex];
                    if (module != null)
                    {
                        if (module.ImageBase != (Address)ulong.MaxValue)
                            break;
                        Debug.Assert(false, "Test me");
                        if (string.Compare(module.FileName, fileName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Note that this code works even if Address is ulong.MaxValue

                            // FillBlock in the real base address. 
                            module.imageBase = imageBase;

                            // Put it in the right place in the table. 
                            Debug.Assert(imageBase == (Address)ulong.MaxValue || unassignedIndex > index);
                            modules.Remove(unassignedIndex, 1);         // Remove it from its old Location.
                            modules.Insert(index + 1, module);          // Put it in its new Location. 
                            return module;                              // Success!
                        }
                    }
                }
#endif
                // We need to make a new module 
                TraceModuleFile newModuleFile = process.Log.ModuleFiles.GetOrCreateModuleFile(fileName, imageBase);
                module = new TraceLoadedModule(process, newModuleFile);
                modules.Insert(index + 1, module);
            }
            return module;
        }

        private TraceLoadedModule FindModuleAndIndex(Address address, long time100ns, out int index)
        {
            modules.BinarySearch((ulong)address, out index, compareByAddress);
            if (index >= 0)
            {
                for (int candidateIndex = index; candidateIndex >= 0; --candidateIndex)
                {
                    TraceLoadedModule canidateModule = modules[candidateIndex];
                    if ((ulong)address < (ulong)canidateModule.ImageBase + (uint)canidateModule.ModuleFile.ImageSize)
                    {
                        if (canidateModule.LoadTime100ns <= time100ns && time100ns < canidateModule.UnloadTime100ns)
                        {
                            index = candidateIndex;
                            return canidateModule;
                        }
                    }
                }
            }
            return null;
        }

        static internal GrowableArray<TraceLoadedModule>.Comparison<ulong> compareByAddress = delegate(ulong x, TraceLoadedModule y)
        {
            if (x > (ulong)y.ImageBase)
                return 1;
            if (x < (ulong)y.ImageBase)
                return -1;
            return 0;
        };

        [Conditional("DEBUG")]
        private void CheckClassInvarients()
        {
            // Modules better be sorted
            long lastAddress = 0;
            for (int i = 0; i < modules.Count; i++)
            {
                TraceLoadedModule module = modules[i];

                Debug.Assert((ulong)module.ModuleID >= (ulong)lastAddress, "regions not sorted!");
                lastAddress = module.ModuleID;
            }
        }

        internal TraceLoadedModules(TraceProcess process)
        {
            this.process = process;
        }
        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(process);
            serializer.Log("<WriteColection count=\"" + modules.Count + "\">\r\n");
            serializer.Write(modules.Count);
            for (int i = 0; i < modules.Count; i++)
                serializer.Write(modules[i]);
            serializer.Log("</WriteColection>\r\n");
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out process);
            Debug.Assert(modules.Count == 0);
            int count; deserializer.Read(out count);
            for (int i = 0; i < count; i++)
            {
                TraceLoadedModule elem; deserializer.Read(out elem);
                modules.Add(elem);
            }
        }

        TraceProcess process;
        GrowableArray<TraceLoadedModule> modules;               // sorted by ModuleID (or ImageBase)

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }
        #endregion
    }

    /// <summary>
    /// A code:TraceLoadedModule represents a collection of code that is ready to run (it is loaded into a
    /// process. 
    /// </summary>
    public class TraceLoadedModule : IFastSerializable
    {
        // TODO do we want loadedModuleIndex?
        /// <summary>
        /// 0 for managed modules without NGEN images.  
        /// </summary>
        public Address ImageBase { get { if (moduleFile == null) return Address.Null; else return moduleFile.ImageBase; } }
        /// <summary>
        /// The load time is the time the LoadLibrary was done if it was loaded from a file, otherwise is the
        /// time the CLR loaded the module. 
        /// </summary>
        public DateTime LoadTime { get { return DateTime.FromFileTime(LoadTime100ns); } }
        public double LoadTimeRelative { get { return Process.Log.RelativeTimeMSec(LoadTime100ns); } }
        public long LoadTime100ns { get { return loadTime100ns; } }
        public DateTime UnloadTime { get { return DateTime.FromFileTime(UnloadTime100ns); } }
        public double UnloadTimeRelative { get { return Process.Log.RelativeTimeMSec(UnloadTime100ns); } }
        public long UnloadTime100ns { get { return unloadTime100ns; } }
        public TraceProcess Process { get { return process; } }
        virtual public long ModuleID { get { return (long)ImageBase; } }
        /// <summary>
        /// If this managedModule was a file that was mapped into memory (eg LoadLibary), then ModuleFile points at
        /// it.  If a managed module does not have a file associated with it, this can be null.  
        /// </summary>
        public TraceModuleFile ModuleFile { get { return moduleFile; } }
        public string FileName { get { if (ModuleFile == null) return ""; else return ModuleFile.FileName; } }
        public string Name { get { if (ModuleFile == null) return ""; else return ModuleFile.Name; } }
        public override string ToString()
        {
            string moduleFileRef = "";
            return "<TraceLoadedModule " +
                    "Name=" + XmlUtilities.XmlQuote(Name).PadRight(24) + " " +
                    moduleFileRef +
                    "ImageBase=" + XmlUtilities.XmlQuoteHex((ulong)ImageBase) + " " +
                    "ImageSize=" + XmlUtilities.XmlQuoteHex((ModuleFile != null) ? ModuleFile.ImageSize : 0) + " " +
                    "LoadTimeRelative=" + XmlUtilities.XmlQuote(LoadTimeRelative) + " " +
                    "UnloadTimeRelative=" + XmlUtilities.XmlQuote(UnloadTimeRelative) + " " +
                    "FileName=" + XmlUtilities.XmlQuote(FileName) + " " +
                   "/>";
        }
        /// <summary>
        /// If this module is an NGEN (or IL) image, return the first instance that this module was loaded as a
        /// managed module (note that there may be more than one (if the code is Appdomain specific and loaded
        /// in several appdomains).  
        /// 
        /// TODO: provide a way of getting at all the loaded images.  
        /// </summary>
        public TraceManagedModule ManagedModule { get { return managedModule; } }
        #region Private
        internal TraceLoadedModule(TraceProcess process, TraceModuleFile moduleFile)
        {
            this.process = process;
            this.moduleFile = moduleFile;
            this.unloadTime100ns = ETWTraceEventSource.MaxTime100ns;
        }


        public void ToStream(Serializer serializer)
        {
            serializer.Write(loadTime100ns);
            serializer.Write(unloadTime100ns);
            serializer.Write(managedModule);
            serializer.Write(process);
            serializer.Write(moduleFile);
        }

        public void FromStream(Deserializer deserializer)
        {
            deserializer.Read(out loadTime100ns);
            deserializer.Read(out unloadTime100ns);
            deserializer.Read(out managedModule);
            deserializer.Read(out process);
            deserializer.Read(out moduleFile);
        }

        internal long loadTime100ns;
        internal long unloadTime100ns;
        internal TraceManagedModule managedModule;
        private TraceProcess process;
        private TraceModuleFile moduleFile;         // Can be null (modules with files)
        #endregion
    }

    /// <summary>
    /// A code:TraceManagedModule is a .NET runtime loaded managedModule.  
    /// TODO explain more
    /// </summary>
    public sealed class TraceManagedModule : TraceLoadedModule, IFastSerializable
    {
        override public long ModuleID { get { return moduleID; } }
        public long AssmeblyID { get { return assmeblyID; } }
        public bool IsAppDomainNeutral { get { return isAppDomainNeutral; } }
        /// <summary>
        /// If the managed managedModule is an IL managedModule that has has an NGEN image, return it. 
        /// </summary>
        public TraceLoadedModule NativeModule { get { return nativeModule; } }
        public override string ToString()
        {
            string nativeInfo = "";
            if (NativeModule != null)
                nativeInfo = "<NativeModule>\r\n  " + NativeModule.ToString() + "\r\n</NativeModule>\r\n";

            return "<TraceManagedModule " +
                   "ModuleID=" + XmlUtilities.XmlQuoteHex((ulong)ModuleID) + " " +
                   "AssmeblyID=" + XmlUtilities.XmlQuoteHex((ulong)AssmeblyID) + ">\r\n" +
                   "  " + base.ToString() + "\r\n" +
                   nativeInfo +
                   "</TraceManagedModule>";
        }
        #region Private
        internal TraceManagedModule(long moduleID, long assemblyID, bool isAppdomainNeutral, TraceModuleFile moduleFile, TraceLoadedModule nativeModule, TraceProcess process)
            : base(process, moduleFile)
        {
            this.nativeModule = nativeModule;
            this.moduleID = moduleID;
            this.assmeblyID = assemblyID;
            this.isAppDomainNeutral = isAppdomainNeutral;
            this.managedModule = this;
        }

        private TraceLoadedModule nativeModule;      // non-null for IL managed modules
        private long moduleID;
        private long assmeblyID;
        private bool isAppDomainNeutral;

        void IFastSerializable.ToStream(Serializer serializer)
        {
            base.ToStream(serializer);
            serializer.Write((long)moduleID);
            serializer.Write((long)assmeblyID);
            serializer.Write(nativeModule);
            serializer.Write(isAppDomainNeutral);
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            base.FromStream(deserializer);
            long address;
            deserializer.Read(out address); moduleID = address;
            deserializer.Read(out address); assmeblyID = address;
            deserializer.Read(out nativeModule);
            deserializer.Read(out isAppDomainNeutral);
        }
        #endregion
    }

    /// <summary>
    /// code:CallStackIndex uniquely identifies a callstack within the log.  Valid values are between 0 and
    /// code:TraceCallStacks.MaxCallStackIndex, Thus an array can be used to 'attach' data to a callstack.   
    /// </summary>
    public enum CallStackIndex { Invalid = -1 };

    public class TraceCallStacks : IFastSerializable, IEnumerable<TraceCallStack>
    {
        public int MaxCallStackIndex { get { return callStacks.Count; } }
        public CodeAddressIndex CodeAddressIndex(CallStackIndex stackIndex) { return callStacks[(int)stackIndex].codeAddressIndex; }
        public CallStackIndex Caller(CallStackIndex stackIndex)
        {
            CallStackIndex ret = callStacks[(int)stackIndex].callerIndex;
            Debug.Assert(ret < stackIndex);         // Stacks should be getting 'smaller'
            return ret;
        }
        public int Depth(CallStackIndex stackIndex)
        {
            int ret = 0;
            while (stackIndex != CallStackIndex.Invalid)
            {
                Debug.Assert(ret < 1000000);       // Catches infinite recursion 
                ret++;
                stackIndex = Caller(stackIndex);
            }
            return ret;
        }
        public TraceCallStack this[CallStackIndex callStackIndex]
        {
            get
            {
                // We don't bother interning. 
                if (callStackIndex == CallStackIndex.Invalid)
                    return null;
                return new TraceCallStack(this, callStackIndex);
            }
        }
        public TraceCodeAddresses CodeAddresses { get { return codeAddresses; } }
        public IEnumerator<TraceCallStack> GetEnumerator()
        {
            for (int i = 0; i < MaxCallStackIndex; i++)
                yield return this[(CallStackIndex)i];
        }
        public IEnumerable<CallStackIndex> GetAllIndexes
        {
            get
            {
                for (int i = 0; i < MaxCallStackIndex; i++)
                    yield return (CallStackIndex)i;
            }
        }

        #region private
        internal TraceCallStacks(TraceLog log, TraceCodeAddresses codeAddresses)
        {
            this.log = log;
            this.codeAddresses = codeAddresses;
        }

        /// <summary>
        /// Used to 'undo' the effects of adding a eventToStack that you no longer want.  This happens when we find
        /// out that a eventToStack is actually got more callers in it (when a eventToStack is split).  
        /// </summary>
        /// <param name="origSize"></param>
        internal void SetSize(int origSize)
        {
            callStacks.RemoveRange(origSize, callStacks.Count - origSize);
        }

        internal CallStackIndex Combine(CallStackIndex part1, CallStackIndex part2)
        {
            if (part1 == CallStackIndex.Invalid)
                return part2;

            CallStackIndex caller = Combine(Caller(part1), part2);
            return InternCallStackIndex(CodeAddressIndex(part1), caller);
        }

        internal CallStackIndex GetStackIndexForStackEvent(StackWalkTraceData stackData, int depth)
        {
            Debug.Assert(depth >= 0);
            Debug.Assert(depth <= stackData.FrameCount);

            CallStackIndex ret = CallStackIndex.Invalid;
            if (depth > 0)
            {
                CodeAddressIndex codeAddress = codeAddresses.GetCodeAddressIndex(stackData, stackData.InstructionPointer(stackData.FrameCount - depth));
                CallStackIndex caller = GetStackIndexForStackEvent(stackData, depth - 1);
                ret = InternCallStackIndex(codeAddress, caller);
            }
            Debug.Assert(depth == Depth(ret));  // TODO comment out. 
            return ret;
        }

        internal CallStackIndex GetStackIndexForStackEvent(ClrStackWalkTraceData stackData, int depth)
        {
            Debug.Assert(depth >= 0);
            Debug.Assert(depth <= stackData.FrameCount);

            CallStackIndex ret = CallStackIndex.Invalid;
            if (depth > 0)
            {
                CodeAddressIndex codeAddress = codeAddresses.GetCodeAddressIndex(stackData, stackData.InstructionPointer(stackData.FrameCount - depth));
                CallStackIndex caller = GetStackIndexForStackEvent(stackData, depth - 1);
                ret = InternCallStackIndex(codeAddress, caller);
            }
            Debug.Assert(depth == Depth(ret));  // TODO comment out. 
            return ret;
        }

        private CallStackIndex InternCallStackIndex(CodeAddressIndex codeAddressIndex, CallStackIndex callerIndex)
        {
            List<CallStackIndex> frameCallees;
            if (callerIndex == CallStackIndex.Invalid)
            {
                if (top == null)
                {
                    if (callStacks.Count == 0)
                        callStacks = new GrowableArray<CallStackInfo>(10000);
                    callees = new GrowableArray<List<CallStackIndex>>(10000);
                    top = new List<CallStackIndex>();
                }
                frameCallees = top;
            }
            else
            {
                frameCallees = callees[(int)callerIndex];
                if (frameCallees == null)
                {
                    frameCallees = new List<CallStackIndex>(4);
                    callees[(int)callerIndex] = frameCallees;
                }
            }

            // Search backwards, assuming that most reciently added is the most likely hit.  
            for (int i = frameCallees.Count - 1; i >= 0; --i)
            {
                CallStackIndex calleeIndex = frameCallees[i];
                if (callStacks[(int)calleeIndex].codeAddressIndex == codeAddressIndex)
                {
                    Debug.Assert(calleeIndex > callerIndex);
                    return calleeIndex;
                }
            }
            CallStackIndex ret = (CallStackIndex)callStacks.Count;
            callStacks.Add(new CallStackInfo(codeAddressIndex, callerIndex));
            frameCallees.Add(ret);
            callees.Add(null);
            Debug.Assert(callees.Count == callStacks.Count);
            return ret;
        }

        private struct CallStackInfo
        {
            internal CallStackInfo(CodeAddressIndex codeAddressIndex, CallStackIndex callerIndex)
            {
                this.codeAddressIndex = codeAddressIndex;
                this.callerIndex = callerIndex;
            }

            internal CodeAddressIndex codeAddressIndex;
            internal CallStackIndex callerIndex;
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(log);
            serializer.Write(codeAddresses);
            lazyCallStacks.Write(serializer, delegate
            {
                serializer.Log("<WriteColection name=\"callStacks\" count=\"" + callStacks.Count + "\">\r\n");
                serializer.Write(callStacks.Count);
                for (int i = 0; i < callStacks.Count; i++)
                {
                    serializer.Write((int)callStacks[i].codeAddressIndex);
                    serializer.Write((int)callStacks[i].callerIndex);
                }
                serializer.Log("</WriteColection>\r\n");
            });
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out log);
            deserializer.Read(out codeAddresses);

            lazyCallStacks.Read(deserializer, delegate
            {
                deserializer.Log("<Marker Name=\"callStacks\"/>");
                int count = deserializer.ReadInt();
                callStacks = new GrowableArray<CallStackInfo>(count + 1);
                CallStackInfo callStackInfo = new CallStackInfo();
                for (int i = 0; i < count; i++)
                {
                    callStackInfo.codeAddressIndex = (CodeAddressIndex)deserializer.ReadInt();
                    callStackInfo.callerIndex = (CallStackIndex)deserializer.ReadInt();
                    callStacks.Add(callStackInfo);
                }
            });
            lazyCallStacks.FinishRead();        // TODO REMOVE 
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }

        private GrowableArray<List<CallStackIndex>> callees;    // This is only used when converted but is logically
        private List<CallStackIndex> top;                       // callees for top of stacks.  
        // a field on CallStackInfo
        private GrowableArray<CallStackInfo> callStacks;
        private DeferedRegion lazyCallStacks;
        private TraceCodeAddresses codeAddresses;
        private TraceLog log;
        #endregion
    }

    /// <summary>
    /// A TraceCallStack is a structure that represents a call eventToStack as a linked list.  It contains the
    /// Address in the current frame, and the pointer to the caller's eventToStack.  
    /// </summary>
    public class TraceCallStack
    {
        public CallStackIndex CallStackIndex { get { return stackIndex; } }
        public TraceCodeAddress CodeAddress { get { return callStacks.CodeAddresses[callStacks.CodeAddressIndex(stackIndex)]; } }
        public TraceCallStack Caller { get { return callStacks[callStacks.Caller(stackIndex)]; } }
        public int Depth { get { return callStacks.Depth(stackIndex); } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(4096);
            return ToString(sb).ToString();
        }
        public StringBuilder ToString(StringBuilder sb)
        {
            TraceCallStack cur = this;
            while (cur != null)
            {
                cur.CodeAddress.ToString(sb).AppendLine();
                cur = cur.Caller;
            }
            return sb;
        }
        #region private
        internal TraceCallStack(TraceCallStacks stacks, CallStackIndex stackIndex)
        {
            this.callStacks = stacks;
            this.stackIndex = stackIndex;
        }

        private TraceCallStacks callStacks;
        private CallStackIndex stackIndex;
        #endregion
    }

    /// <summary>
    /// code:MethodIndex uniquely identifies a method within the log.  Valid values are between 0 and
    /// code:TraceMethods.MaxMethodIndex, Thus an array can be used to 'attach' data to a method.   
    /// </summary>
    public enum MethodIndex { Invalid = -1 };
    public class TraceMethods : IFastSerializable, IEnumerable<TraceMethod>
    {
        public int MaxMethodIndex { get { return methods.Count; } }
        public string FullMethodName(MethodIndex methodIndex)
        {
            if (methodIndex == MethodIndex.Invalid)
                return "";
            else
                return methods[(int)methodIndex].fullMethodName;
        }
        public string SourceFileName(MethodIndex methodIndex)
        {
            if (methodIndex == MethodIndex.Invalid)
                return "";
            else
                return methods[(int)methodIndex].sourceFileName;
        }
        public TraceMethod this[MethodIndex methodIndex]
        {
            get
            {
                if (methodObjects == null || (int)methodIndex >= methodObjects.Length)
                    methodObjects = new TraceMethod[(int)methodIndex + 16];

                if (methodIndex == MethodIndex.Invalid)
                    return null;

                TraceMethod ret = methodObjects[(int)methodIndex];
                if (ret == null)
                {
                    ret = new TraceMethod(this, methodIndex);
                    methodObjects[(int)methodIndex] = ret;
                }
                return ret;
            }
        }

        public IEnumerator<TraceMethod> GetEnumerator()
        {
            for (int i = 0; i < MaxMethodIndex; i++)
                yield return this[(MethodIndex)i];
        }
        public IEnumerable<MethodIndex> GetAllIndexes
        {
            get
            {
                for (int i = 0; i < MaxMethodIndex; i++)
                    yield return (MethodIndex)i;
            }
        }
        #region private
        internal TraceMethods() { }

        internal MethodIndex NewMethod(string fullMethodName, string sourceFileName)
        {
            MethodIndex ret = (MethodIndex)methods.Count;
            methods.Add(new MethodInfo(fullMethodName, sourceFileName));
            return ret;
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            lazyMethods.Write(serializer, delegate
            {
                serializer.Write(methods.Count);
                serializer.Log("<WriteColection name=\"methods\" count=\"" + methods.Count + "\">\r\n");
                for (int i = 0; i < methods.Count; i++)
                {
                    serializer.Write(methods[i].fullMethodName);
                    // TODO string interning on currentSourceFileName
                    serializer.Write(methods[i].sourceFileName);
                }
                serializer.Log("</WriteColection>\r\n");
            });
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            lazyMethods.Read(deserializer, delegate
            {
                int count = deserializer.ReadInt();
                deserializer.Log("<Marker name=\"methods\" count=\"" + count + "\"/>");
                MethodInfo methodInfo = new MethodInfo();
                methods = new GrowableArray<MethodInfo>(count + 1);

                for (int i = 0; i < count; i++)
                {
                    deserializer.Read(out methodInfo.fullMethodName);
                    deserializer.Read(out methodInfo.sourceFileName);
                    // TODO remove when we have real interning
                    if (methodInfo.sourceFileName.Length == 0)
                        methodInfo.sourceFileName = string.Empty;
                    methods.Add(methodInfo);
                }
            });
            lazyMethods.FinishRead();        // TODO REMOVE 
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }

        private struct MethodInfo
        {
            internal MethodInfo(string fullMethodName, string sourceFileName)
            {
                this.fullMethodName = fullMethodName;
                this.sourceFileName = sourceFileName;
            }
            internal string fullMethodName;
            internal string sourceFileName;
        }

        private DeferedRegion lazyMethods;
        private GrowableArray<MethodInfo> methods;
        private TraceMethod[] methodObjects;
        #endregion
    }

    public class TraceMethod
    {
        public MethodIndex MethodIndex { get { return methodIndex; } }
        public string SourceFileName { get { return methods.SourceFileName(methodIndex); } }
        public string FullMethodName { get { return methods.FullMethodName(methodIndex); } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            return ToString(sb).ToString();
        }
        public StringBuilder ToString(StringBuilder sb)
        {
            sb.Append("  <TraceMethod ");
            if (FullMethodName.Length > 0)
                sb.Append(" FullMethodName=\"").Append(XmlUtilities.XmlEscape(FullMethodName, false)).Append("\"");
            if (SourceFileName.Length > 0)
                sb.Append(" SourceFileName=\"").Append(XmlUtilities.XmlEscape(SourceFileName, false)).Append("\"");
            sb.Append("/>");
            return sb;
        }
        #region private
        internal TraceMethod(TraceMethods methods, MethodIndex methodIndex)
        {
            this.methods = methods;
            this.methodIndex = methodIndex;
        }

        TraceMethods methods;
        MethodIndex methodIndex;
        #endregion
    }

    /// <summary>
    /// code:CodeAddressIndex uniquely identifies a symbolic codeAddress within the log (note that the SAME
    /// physical addresses can have a different symbolic codeAddress because they are in different
    /// processes). Valid values are between 0 and code:TraceCodeAddresses.MaxCodeAddressIndex, Thus an array
    /// can be used to 'attach' data to a method.
    /// </summary>
    public enum CodeAddressIndex { Invalid = -1 };
    public class TraceCodeAddresses : IFastSerializable, IEnumerable<TraceCodeAddress>
    {
        public int MaxCodeAddressIndex { get { return codeAddresses.Count; } }
        [CLSCompliant(false)]
        public Address Address(CodeAddressIndex codeAddressIndex) { return codeAddresses[(int)codeAddressIndex].address; }
        public int SourceLineNumber(CodeAddressIndex codeAddressIndex) { return codeAddresses[(int)codeAddressIndex].sourceLineNumber; }
        public ModuleFileIndex ModuleFileIndex(CodeAddressIndex codeAddressIndex) { return codeAddresses[(int)codeAddressIndex].moduleFileIndex; }
        public MethodIndex MethodIndex(CodeAddressIndex codeAddressIndex) { return codeAddresses[(int)codeAddressIndex].methodIndex; }
        public TraceModuleFile ModuleFile(CodeAddressIndex codeAddressIndex) { return ModuleFiles[ModuleFileIndex(codeAddressIndex)]; }
        public TraceCodeAddress this[CodeAddressIndex codeAddressIndex]
        {
            get
            {
                if (codeAddressObjects == null || (int)codeAddressIndex >= codeAddressObjects.Length)
                    codeAddressObjects = new TraceCodeAddress[(int)codeAddressIndex + 16];

                if (codeAddressIndex == CodeAddressIndex.Invalid)
                    return null;

                TraceCodeAddress ret = codeAddressObjects[(int)codeAddressIndex];
                if (ret == null)
                {
                    ret = new TraceCodeAddress(this, codeAddressIndex);
                    codeAddressObjects[(int)codeAddressIndex] = ret;
                }
                return ret;
            }
        }
        public IEnumerator<TraceCodeAddress> GetEnumerator()
        {
            for (int i = 0; i < MaxCodeAddressIndex; i++)
                yield return this[(CodeAddressIndex)i];
        }
        public IEnumerable<CodeAddressIndex> GetAllIndexes
        {
            get
            {
                for (int i = 0; i < MaxCodeAddressIndex; i++)
                    yield return (CodeAddressIndex)i;
            }
        }
        public TraceMethods Methods { get { return methods; } }
        public TraceModuleFiles ModuleFiles { get { return moduleFiles; } }

        /// <summary>
        /// Indicates the number of managed method record that were encountered.
        /// </summary>
        public int ManagedMethodRecordCount { get { return managedMethodRecordCount; } }
        #region private
        internal TraceCodeAddresses(TraceLog log, TraceModuleFiles moduleFiles)
        {
            this.log = log;
            this.moduleFiles = moduleFiles;
            this.methods = new TraceMethods();
        }

        internal void AddMethod(MethodLoadUnloadVerboseTraceData data)
        {
            managedMethodRecordCount++;
            if (codeAddressBuckets == null)
            {
                codeAddressBuckets = new Dictionary<long, CodeAddressBucketEntry>(5000);
                if (codeAddresses.Count == 0)
                    codeAddresses = new GrowableArray<CodeAddressInfo>(10000);
            }

            Address address = data.MethodStartAddress;
            Address endAddressInclusive = (Address)((long)address + data.MethodSize - 1);
            long curBucket = RoundToBucket((long)address);
            long endBucket = RoundToBucket((long)endAddressInclusive);
            MethodIndex methodIndex = Microsoft.Test.EventTracing.MethodIndex.Invalid;
            for (; ; )
            {
                if (curBucket > endBucket)
                    return;

                CodeAddressBucketEntry codeAddressEntry;
                if (codeAddressBuckets.TryGetValue(curBucket, out codeAddressEntry))
                {
                    do
                    {
                        Address entryAddress = codeAddressEntry.address;
                        if (address <= entryAddress && entryAddress <= endAddressInclusive)
                        {
                            if (MethodIndex(codeAddressEntry.codeAddressIndex) == Microsoft.Test.EventTracing.MethodIndex.Invalid)
                            {
                                if (methodIndex == Microsoft.Test.EventTracing.MethodIndex.Invalid)
                                {
                                    string fullName = TraceLog.GetFullName(data);
#if DEBUG
                                    Debug.Assert(TraceLog.NormalChars(fullName));
#endif
                                    methodIndex = methods.NewMethod(fullName, "");

                                    // Console.WriteLine("CREATED methodIndex " + methodIndex + " for address 0x" + ((long)codeAddressEntry.address).ToString("x") + " name " + fullName);
                                }

                                CodeAddressInfo info = codeAddresses[(int)codeAddressEntry.codeAddressIndex];
                                info.methodIndex = methodIndex;
                                codeAddresses[(int)codeAddressEntry.codeAddressIndex] = info;
                                // Console.WriteLine("FOUND methodIndex " + methodIndex + " for address 0x" + ((long)codeAddressEntry.address).ToString("x") + " name " + methods.FullMethodName(MethodIndex(codeAddressEntry.codeAddressIndex)));
                            }
                            else
                            {
                                // TODO remove after a while.  Just here because it is true most of the
                                // time.  
                                log.DebugWarn(Methods.FullMethodName(MethodIndex(codeAddressEntry.codeAddressIndex)) == TraceLog.GetFullName(data), "Different methods for the same address", data);
                            }
                        }
                        codeAddressEntry = codeAddressEntry.next;
                    } while (codeAddressEntry != null);
                }
                curBucket += bucketSize;
            }
        }

        internal CodeAddressIndex GetCodeAddressIndex(TraceEvent context, Address address)
        {
            return GetCodeAddressEntry(context, address).codeAddressIndex;
        }

        // This supports the lookup of CodeAddresses by range.  
        const long bucketSize = 64;
        static long RoundToBucket(long value)
        {
            Debug.Assert((bucketSize & (bucketSize - 1)) == 0);       // bucketSize must be a power of 2
            return value & (~(bucketSize - 1));
        }

        /// <summary>
        /// Gets the symbolic information entry for 'address' which can be any address.  If it falls in the
        /// range of a symbol, then that symbolic information is returned.  Regarless of whether symbolic
        /// information is found, however, an entry is created for it, so every unique address has an etnry
        /// in this table.  
        /// </summary>
        private CodeAddressBucketEntry GetCodeAddressEntry(TraceEvent context, Address address)
        {
            CodeAddressBucketEntry codeAddressInfo;
            long roundedAddress = RoundToBucket((long)address);
            if (codeAddressBuckets == null)
            {
                codeAddressBuckets = new Dictionary<long, CodeAddressBucketEntry>(5000);
                if (codeAddresses.Count == 0)
                    codeAddresses = new GrowableArray<CodeAddressInfo>(10000);
            }
            if (!codeAddressBuckets.TryGetValue(roundedAddress, out codeAddressInfo))
            {
                codeAddressInfo = NewEntry(context, address, null);
                codeAddressBuckets.Add(roundedAddress, codeAddressInfo);
            }
            else if (address < codeAddressInfo.address)
            {
                codeAddressInfo = NewEntry(context, address, codeAddressInfo);
                codeAddressBuckets[roundedAddress] = codeAddressInfo;
            }
            else
            {
                for (; ; )
                {
                    if (codeAddressInfo.address == address && MethodIndex(codeAddressInfo.codeAddressIndex) == Microsoft.Test.EventTracing.MethodIndex.Invalid)
                        break;

                    CodeAddressBucketEntry nextEntry = codeAddressInfo.next;
                    if (nextEntry == null || address < nextEntry.address)
                    {
                        codeAddressInfo = NewEntry(context, address, nextEntry);
                        codeAddressInfo.next = codeAddressInfo;
                        break;
                    }
                    codeAddressInfo = codeAddressInfo.next;
                }
            }
            return codeAddressInfo;
        }

        private CodeAddressBucketEntry NewEntry(TraceEvent context, Address address, CodeAddressBucketEntry next)
        {
            TraceProcess process = log.Processes.GetOrCreateProcess(context.ProcessID, context.TimeStamp100ns);
            TraceLoadedModule module = process.LoadedModules.GetModuleContainingAddress(address, context.TimeStamp100ns);
            if (module == null)
            {
                // Not mapped in process address space, maybe it is a kernel address.  
                // Uniformly, kernel modules are above this (even on 64 bit and even with 3Gig etc). 
                if ((ulong)address >= (ulong)0x80000000)
                {
                    // The kernel is repsented as the process with id 0,  It is logically mappped into
                    // every process.  
                    // We pick a time in the middle arbitrarily.  
                    long midTime100ns = (log.SessionStartTime100ns + log.SessionEndTime100ns) / 2;
                    if (kernelProcess == null)
                        kernelProcess = log.Processes.GetProcess(0, midTime100ns);
                    if (kernelProcess != null)
                        module = kernelProcess.LoadedModules.GetModuleContainingAddress(address, midTime100ns);
                }
            }

            Microsoft.Test.EventTracing.ModuleFileIndex moduleFileIndex = Microsoft.Test.EventTracing.ModuleFileIndex.Invalid;
            if (module != null)
                moduleFileIndex = module.ModuleFile.ModuleFileIndex;

            CodeAddressIndex codeAddressIndex = (CodeAddressIndex)codeAddresses.Count;
            codeAddresses.Add(new CodeAddressInfo(address, moduleFileIndex));
            CodeAddressBucketEntry codeAddressInfo = new CodeAddressBucketEntry(address, codeAddressIndex, next);

            return codeAddressInfo;
        }

        /// <summary>
        /// Code ranges need to be looked up by arbirary address. There are two basic ways of doing this
        /// efficiently. First a binary search, second create 'buckets' (fixed sized ranges, see
        /// code:bucketSize and code:RoundToBucket) and round any address to these buckets and look them up
        /// in a hash table. This latter option is what we use. What this means is that when a entry is added
        /// to the table (see code:AddMethod) it must be added to every bucket over its range. Each entry in
        /// the table is a code:CodeAddressBucketEntry which is simply a linked list.
        /// </summary>
        class CodeAddressBucketEntry
        {
            public CodeAddressBucketEntry(Address address, CodeAddressIndex codeAddress, CodeAddressBucketEntry next)
            {
                this.address = address;
                this.codeAddressIndex = codeAddress;
                this.next = next;
            }
            public Address address;
            public CodeAddressIndex codeAddressIndex;
            public CodeAddressBucketEntry next;
        }

        // TODO do we need this?
        /// <summary>
        /// Sort from lowest address to highest address. 
        /// </summary>
        IEnumerable<CodeAddressIndex> GetSortedCodeAddressIndexes()
        {
            List<CodeAddressIndex> list = new List<CodeAddressIndex>(GetAllIndexes);
            list.Sort(delegate(CodeAddressIndex x, CodeAddressIndex y)
            {
                ulong addrX = (ulong)Address(x);
                ulong addrY = (ulong)Address(y);
                if (addrX > addrY)
                    return 1;
                if (addrX < addrY)
                    return -1;
                return 0;
            });
            return list;
        }

        /// <summary>
        /// Do symbol resolution for all addresses in the log file. 
        /// </summary>
        internal void LookupSymbols(TraceLogOptions options)
        {
            if (options.SourceLineNumbers)
                Console.WriteLine("Looking up symbolic information for line number and methods.");
            else
                Console.WriteLine("Looking up symbolic information for just methods.");

            TracePdbReader reader = null;
            int totalAddressCount = 0;
            int noModuleAddressCount = 0;
            IEnumerator<CodeAddressIndex> codeAddressIndexCursor = GetSortedCodeAddressIndexes().GetEnumerator();
            bool notDone = codeAddressIndexCursor.MoveNext();
            while (notDone)
            {
                TraceModuleFile moduleFile = moduleFiles[ModuleFileIndex(codeAddressIndexCursor.Current)];
                if (moduleFile != null)
                {
                    if (options.ShouldResolveSymbols(moduleFile.FileName))
                    {
                        if (reader == null)
                            reader = new TracePdbReader();
                        int moduleAddressCount = 0;
                        try
                        {
                            notDone = LookupSymbolsForModule(reader, moduleFile, codeAddressIndexCursor, options, out moduleAddressCount);
                        }
                        catch (Exception e)
                        {
                            // TODO too strong. 
                            Console.WriteLine("An exception occurred during symbol lookup.  Continuing...");
                            Console.WriteLine("Exception: " + e.Message);
                        }
                        totalAddressCount += moduleAddressCount;
                    }

                    // Skip the rest of the addresses for that module.  
                    while ((moduleFiles[ModuleFileIndex(codeAddressIndexCursor.Current)] == moduleFile))
                    {
                        notDone = codeAddressIndexCursor.MoveNext();
                        if (!notDone)
                            break;
                        totalAddressCount++;
                    }
                }
                else
                {
                    // TraceLog.DebugWarn("Could not find a module for address " + ("0x" + Address(codeAddressIndexCursor.Current).ToString("x")).PadLeft(10));
                    notDone = codeAddressIndexCursor.MoveNext();
                    noModuleAddressCount++;
                    totalAddressCount++;
                }
            }

            if (reader != null)
                reader.Dispose();

            double noModulePercent = 0;
            if (totalAddressCount > 0)
                noModulePercent = noModuleAddressCount * 100.0 / totalAddressCount;
            Console.WriteLine("A total of " + totalAddressCount + " symbolic addresses were looked up.");
            Console.WriteLine("Addresses outside any module: " + noModuleAddressCount + " out of " + totalAddressCount + " (" + noModulePercent.ToString("f1") + "%)");
            Console.WriteLine("Done with symbolic lookup");
        }

        /// <summary>
        /// LookupSymbolsForModule takes a IEumerator respresenting the 'cursor' to a list of
        /// sorted code addresses, and this IEnumerator's 'Current' property is the first code
        /// address in the module 'moduleFile'.  LookupSymbolsForModule should resolve all the
        /// symbols that are in 'moduleFile' updating the cursor (which now points to the first
        /// code address outside that module).  It should also increment 'totalAddressCount' for
        /// each address it finds.   It will return 'true' if the updated cursor is not at the end
        /// of the enumeration (that is Current is valid), and false otherwise.
        /// </summary>
        private bool LookupSymbolsForModule(TracePdbReader reader, TraceModuleFile moduleFile, IEnumerator<CodeAddressIndex> codeAddressIndexCursor, TraceLogOptions options, out int totalAddressCount)
        {
            Debug.Assert(moduleFiles[ModuleFileIndex(codeAddressIndexCursor.Current)] == moduleFile);
            totalAddressCount = 0;
            int existingSymbols = 0;
            int distinctSymbols = 0;
            int unmatchedSymbols = 0;
            int repeats = 0;

            Console.WriteLine("Trying to Load symbols for " + moduleFile.FileName);
            using (TracePdbModuleReader moduleReader = reader.LoadSymbolsForModule(moduleFile.FileName, moduleFile.ImageBase))
            {
                Console.WriteLine("Loaded, resolving symbols");
                string currentMethodName = "";
                string currentSourceFileName = "";
                Address currentMethodEnd = Microsoft.Test.EventTracing.Address.Null;
                Address endModule = moduleFile.ImageEnd;
                for (; ; )
                {
                    // Console.WriteLine("Code address = " + Address(codeAddressIndexCursor.Current).ToString("x"));
                    totalAddressCount++;
                    Address address = Address(codeAddressIndexCursor.Current);
                    if (address >= endModule)
                        break;
                    MethodIndex methodIndex = MethodIndex(codeAddressIndexCursor.Current);
                    if (methodIndex == Microsoft.Test.EventTracing.MethodIndex.Invalid)
                    {
                        if (address < currentMethodEnd)
                        {
                            repeats++;
                            // Console.WriteLine("Repeat of " + currentMethodName + " at " + address.ToString("x"));  
                        }
                        else
                        {
                            currentMethodName = moduleReader.FindMethodForAddress(address, out currentMethodEnd);
                            if (currentMethodName.Length > 0)
                                distinctSymbols++;
                            else
                            {
                                unmatchedSymbols++;
                            }
                        }
                        int line = 0;
                        if (options.SourceLineNumbers)
                            moduleReader.FindSourceLineForAddress(address, out line, ref currentSourceFileName);

                        if (currentMethodName.Length > 0 || currentSourceFileName.Length > 0)
                        {
                            CodeAddressInfo codeAddressInfo = codeAddresses[(int)codeAddressIndexCursor.Current];
                            codeAddressInfo.methodIndex = methods.NewMethod(currentMethodName, currentSourceFileName);
                            codeAddressInfo.sourceLineNumber = line;
                            codeAddresses[(int)codeAddressIndexCursor.Current] = codeAddressInfo;
                        }
                    }
                    else
                    {
                        // Console.WriteLine("Found existing method " + Methods[methodIndex].FullMethodName);
                        existingSymbols++;
                    }

                    if (!codeAddressIndexCursor.MoveNext())
                        break;
                }
                Console.WriteLine("    Addresses to look up       " + totalAddressCount);
                if (existingSymbols != 0)
                    Console.WriteLine("        Existing Symbols       " + existingSymbols);
                Console.WriteLine("        Found Symbols          " + (distinctSymbols + repeats));
                Console.WriteLine("        Distinct Found Symbols " + distinctSymbols);
                Console.WriteLine("        Unmatched Symbols " + (totalAddressCount - (distinctSymbols + repeats)));
            }
            return true;
        }
        
        void IFastSerializable.ToStream(Serializer serializer)
        {
            lazyCodeAddresses.Write(serializer, delegate
            {
                serializer.Write(log);
                serializer.Write(moduleFiles);
                serializer.Write(methods);

                serializer.Write(codeAddresses.Count);
                serializer.Log("<WriteColection name=\"codeAddresses\" count=\"" + codeAddresses.Count + "\">\r\n");
                for (int i = 0; i < codeAddresses.Count; i++)
                {
                    serializer.WriteAddress(codeAddresses[i].address);
                    serializer.Write(codeAddresses[i].sourceLineNumber);
                    serializer.Write((int)codeAddresses[i].moduleFileIndex);
                    serializer.Write((int)codeAddresses[i].methodIndex);
                }
                serializer.Log("</WriteColection>\r\n");
            });
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            lazyCodeAddresses.Read(deserializer, delegate
            {
                deserializer.Read(out log);
                deserializer.Read(out moduleFiles);
                deserializer.Read(out methods);

                int count = deserializer.ReadInt();
                deserializer.Log("<Marker name=\"codeAddresses\" count=\"" + count + "\"/>");
                CodeAddressInfo codeAddressInfo = new CodeAddressInfo();
                codeAddresses = new GrowableArray<CodeAddressInfo>(count + 1);
                for (int i = 0; i < count; i++)
                {
                    deserializer.ReadAddress(out codeAddressInfo.address);
                    deserializer.Read(out codeAddressInfo.sourceLineNumber);
                    int index;
                    deserializer.Read(out index); codeAddressInfo.moduleFileIndex = (ModuleFileIndex)index;
                    deserializer.Read(out index); codeAddressInfo.methodIndex = (MethodIndex)index;
                    codeAddresses.Add(codeAddressInfo);
                }
            });
            lazyCodeAddresses.FinishRead();        // TODO REMOVE 
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }

        private struct CodeAddressInfo
        {
            internal CodeAddressInfo(Address address, ModuleFileIndex moduleFileIndex)
            {
                this.address = address;
                this.sourceLineNumber = 0;
                this.moduleFileIndex = moduleFileIndex;
                this.methodIndex = Microsoft.Test.EventTracing.MethodIndex.Invalid;
            }
            internal Address address;
            internal int sourceLineNumber;
            internal ModuleFileIndex moduleFileIndex;
            internal MethodIndex methodIndex;
        }

        // Only used during conversion. 
        private TraceProcess kernelProcess;     // kernel dlls are mapped to PID 0.  
        private Dictionary<long, CodeAddressBucketEntry> codeAddressBuckets;

        private TraceLog log;
        private TraceModuleFiles moduleFiles;
        private TraceMethods methods;
        private DeferedRegion lazyCodeAddresses;
        private GrowableArray<CodeAddressInfo> codeAddresses;
        private TraceCodeAddress[] codeAddressObjects;
        private int managedMethodRecordCount;
        #endregion
    }

    /// <summary>
    /// A TraceCodeAddress represents a address of code (where an instruction pointer might point). Unlike a
    /// raw pointer, TraceCodeAddresses will be distinct if they come from different ModuleFiles (thus at
    /// different times (or different processes) different modules were loaded and had the same virtual
    /// address they would NOT have the same TraceCodeAddress because the load file (and thus the symbolic
    /// information) would be different.
    /// 
    /// TraceCodeAddresses hold the symbolic information associated with the address.
    /// </summary>
    public class TraceCodeAddress
    {
        public CodeAddressIndex CodeAddressIndex { get { return codeAddressIndex; } }

        public Address Address { get { return codeAddresses.Address(codeAddressIndex); } }
        public int SourceLineNumber { get { return codeAddresses.SourceLineNumber(codeAddressIndex); } }
        public string SourceFileName
        {
            get
            {
                MethodIndex methodIndex = codeAddresses.MethodIndex(codeAddressIndex);
                if (methodIndex == MethodIndex.Invalid)
                    return "";
                return codeAddresses.Methods.SourceFileName(methodIndex);
            }
        }
        public string FullMethodName
        {
            get
            {
                MethodIndex methodIndex = codeAddresses.MethodIndex(codeAddressIndex);
                if (methodIndex == MethodIndex.Invalid)
                    return "";
                return codeAddresses.Methods.FullMethodName(methodIndex);
            }
        }
        public TraceMethod Method
        {
            get
            {
                MethodIndex methodIndex = codeAddresses.MethodIndex(codeAddressIndex);
                if (methodIndex == MethodIndex.Invalid)
                    return null;
                else
                    return codeAddresses.Methods[methodIndex];
            }
        }
        public TraceModuleFile ModuleFile
        {
            get
            {
                ModuleFileIndex moduleFileIndex = codeAddresses.ModuleFileIndex(codeAddressIndex);
                if (moduleFileIndex == ModuleFileIndex.Invalid)
                    return null;
                else
                    return codeAddresses.ModuleFiles[moduleFileIndex];
            }
        }
        public string ModuleName
        {
            get
            {
                TraceModuleFile moduleFile = ModuleFile;
                if (moduleFile == null)
                    return "";
                return moduleFile.Name;
            }
        }
        public string ModuleFileName
        {
            get
            {
                TraceModuleFile moduleFile = ModuleFile;
                if (moduleFile == null)
                    return "";
                return moduleFile.FileName;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            return ToString(sb).ToString();
        }
        public StringBuilder ToString(StringBuilder sb)
        {
            sb.Append("  <CodeAddress Address=\"0x").Append(((long)Address).ToString("x")).Append("\"");
            if (FullMethodName.Length > 0)
                sb.Append(" FullMethodName=\"").Append(XmlUtilities.XmlEscape(FullMethodName, false)).Append("\"");
            if (ModuleName.Length != 0)
                sb.Append(" ModuleName=\"").Append(XmlUtilities.XmlEscape(ModuleName, false)).Append("\"");
            if (SourceLineNumber != 0)
                sb.Append(" SourceLineNumber=\"").Append(SourceLineNumber).Append("\"");
            if (SourceFileName.Length > 0)
                sb.Append(" SourceFileName=\"").Append(XmlUtilities.XmlEscape(SourceFileName, false)).Append("\"");
            sb.Append("/>");
            return sb;
        }
        #region private
        internal TraceCodeAddress(TraceCodeAddresses codeAddresses, CodeAddressIndex codeAddressIndex)
        {
            this.codeAddresses = codeAddresses;
            this.codeAddressIndex = codeAddressIndex;
        }

        TraceCodeAddresses codeAddresses;
        CodeAddressIndex codeAddressIndex;
        #endregion
    }

    public enum ModuleFileIndex { Invalid = -1 };
    public class TraceModuleFiles : IFastSerializable, IEnumerable<TraceModuleFile>
    {
        /// <summary>
        /// Enumerate all the threads that occured in the trace log.  It does so in order of their process
        /// offset events in the log.  
        /// </summary> 
        IEnumerator<TraceModuleFile> IEnumerable<TraceModuleFile>.GetEnumerator()
        {
            for (int i = 0; i < moduleFiles.Count; i++)
                yield return moduleFiles[i];
        }
        /// <summary>
        /// The log associated with this collection of threads. 
        /// </summary>

        public int MaxModuleFileIndex { get { return moduleFiles.Count; } }
        public TraceModuleFile this[ModuleFileIndex moduleFileIndex]
        {
            get
            {
                if (moduleFileIndex == ModuleFileIndex.Invalid)
                    return null;
                return moduleFiles[(int)moduleFileIndex];
            }
        }

        [CLSCompliant(false)] public TraceLog Log { get { return log; } }

        /// <summary>
        /// For a given file name, get the code:TraceModuleFile associated with it.  
        /// </summary>
        [CLSCompliant(false)]
        public TraceModuleFile GetModuleFile(string fileName, Address imageBase)
        {
            TraceModuleFile moduleFile;
            if (moduleFilesByName == null)
            {
                moduleFilesByName = new Dictionary<string, TraceModuleFile>(Math.Max(256, moduleFiles.Count + 4), StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < moduleFiles.Count; i++)
                {
                    moduleFile = moduleFiles[i];
                    Debug.Assert(moduleFile.next == null);
                    TraceModuleFile collision;
                    if (moduleFilesByName.TryGetValue(moduleFile.FileName, out collision))
                        moduleFile.next = collision;
                    else
                        moduleFilesByName.Add(moduleFile.FileName, moduleFile);
                }
            }
            if (moduleFilesByName.TryGetValue(fileName, out moduleFile))
            {
                do
                {
                    if (moduleFile.imageBase == imageBase)
                        return moduleFile;
                    //                    Console.WriteLine("WARNING: " + fileName + " loaded with two base addresses 0x" + imageBase.ToString("x") + " and 0x" + moduleFile.imageBase.ToString("x"));
                    moduleFile = moduleFile.next;
                } while (moduleFile != null);
            }
            return moduleFile;
        }

        #region private

        /// <summary>
        /// We cache information about a native image load in a code:TraceModuleFile.  Retrieve or create a new
        /// cache entry associated with 'nativePath'.  
        /// </summary>
        internal TraceModuleFile GetOrCreateModuleFile(string nativePath, Address imageBase)
        {
            TraceModuleFile moduleFile = GetModuleFile(nativePath, imageBase);
            if (moduleFile == null)
            {
                moduleFile = new TraceModuleFile(nativePath, imageBase, (ModuleFileIndex)moduleFiles.Count);
                moduleFiles.Add(moduleFile);
                if (moduleFilesByName != null)
                {
                    TraceModuleFile prevValue;
                    if (moduleFilesByName.TryGetValue(nativePath, out prevValue))
                        moduleFile.next = prevValue;
                    moduleFilesByName[nativePath] = moduleFile;
                }
            }

            Debug.Assert(moduleFilesByName == null || moduleFiles.Count >= moduleFilesByName.Count);
            return moduleFile;
        }
        internal TraceModuleFiles(TraceLog log)
        {
            this.log = log;
        }

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(log);
            serializer.Write(moduleFiles.Count);
            for (int i = 0; i < moduleFiles.Count; i++)
                serializer.Write(moduleFiles[i]);
        }

        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out log);
            int count = deserializer.ReadInt();
            moduleFiles = new GrowableArray<TraceModuleFile>(count + 1);
            for (int i = 0; i < count; i++)
            {
                TraceModuleFile elem;
                deserializer.Read(out elem);
                moduleFiles.Add(elem);
            }
            moduleFilesByName = null;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException(); // GetEnumerator
        }

        private TraceLog log;
        private Dictionary<string, TraceModuleFile> moduleFilesByName;
        private GrowableArray<TraceModuleFile> moduleFiles;
        #endregion
    }

    /// <summary>
    /// The TraceModuleFile reprsents a executable file that can be loaded into memory (either an EXE or a
    /// DLL).  It only represents the data file as well as the location in mememory where it was loaded, but
    /// NOT the load or unload time etc.  Thus it is good for sharing symbolic information.  
    /// </summary>
    public sealed class TraceModuleFile : IFastSerializable
    {
        public ModuleFileIndex ModuleFileIndex { get { return moduleFileIndex; } }
        /// <summary>
        /// The moduleFile name associted with the moduleFile.  May be the empty string if the moduleFile has no moduleFile
        /// (dynamically generated).  For managed code, this is the IL moduleFile name.  
        /// </summary>
        public string FileName { get { return fileName; } }
        /// <summary>
        /// This is the short name of the moduleFile (moduleFile name without exention). 
        /// </summary>
        public string Name
        {
            get
            {
                if (name == null)
                    name = Path.GetFileNameWithoutExtension(FileName);
                return name;
            }
        }
        public Address DefaultBase { get { return defaultBase; } }
        public Address ImageBase { get { return imageBase; } }
        public int ImageSize { get { return imageSize; } }
        public Address ImageEnd { get { return (Address)((ulong)imageBase + (uint)imageSize); } }

        public override string ToString()
        {
            return "<TraceModuleFile " +
                    "Name=" + XmlUtilities.XmlQuote(Name) + " " +
                    "ImageSize=" + XmlUtilities.XmlQuoteHex(ImageSize) + " " +
                    "FileName=" + XmlUtilities.XmlQuote(FileName) + " " +
                    "ImageBase=" + XmlUtilities.XmlQuoteHex((ulong)ImageBase) + " " +
                   "/>";
        }

        #region Private
        internal TraceModuleFile(string fileName, Address imageBase, ModuleFileIndex moduleFileIndex)
        {
            this.fileName = fileName;
            this.imageBase = imageBase;
            this.moduleFileIndex = moduleFileIndex;
        }

        private string fileName;
        internal int imageSize;
        internal Address imageBase;
        internal Address defaultBase;
        internal string name;
        private ModuleFileIndex moduleFileIndex;
        internal TraceModuleFile next;          // Chain of modules that have the same name (But different image bases)

        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(fileName);
            serializer.Write(imageSize);
            serializer.WriteAddress(imageBase);
            serializer.WriteAddress(defaultBase);
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out fileName);
            deserializer.Read(out imageSize);
            deserializer.ReadAddress(out imageBase);
            deserializer.ReadAddress(out defaultBase);
        }
        #endregion
    }

    /// <summary>
    /// TraceLogOptions control the generation of a TraceLog.  
    /// </summary>
    public class TraceLogOptions
    {
        public TraceLogOptions()
        {
            // These are the default modules to look up symbolically.  
            ShouldResolveSymbols = delegate(string moduleFilePath)
            {
                string moduleName = Path.GetFileNameWithoutExtension(moduleFilePath);
                if (string.Compare(moduleName, "ntdll") == 0 ||
                    string.Compare(moduleName, "kernel32") == 0 ||
                    string.Compare(moduleName, "ntkrnlpa") == 0 ||
                    string.Compare(moduleName, "ntoskrnl") == 0)
                    return true;
                return false;
            };
        }
        public Predicate<string> ShouldResolveSymbols;

        /// <summary>
        /// Resolving symbols from a symbol server can take a long time. If
        /// there is a DLL that always fails, it can be quite anoying because
        /// it will always cause delays, By specifying only local symbols it
        /// will only resolve the symbols if it can do so without delay.
        /// Symbols that have been reviously locally cached from a symbol
        /// server count as local symobls.
        ///    
        /// TODO NOT IMPLEMENTED.
        /// </summary>
        public bool LocalSymbolsOnly;
        /// <summary>
        /// If set, will resolve addresses to line numbers, not just names.  Default is not to have line
        /// numbers.  
        /// </summary>
        public bool SourceLineNumbers;
        /// <summary>
        /// print detailed symbolic information (TODO where?)
        /// </summary>
        public bool SymbolDebug;
        /// <summary>
        /// By default symbols are only resolve if there are stacks assocated with the trace. 
        /// Setting this option forces resolution even if there are no stacks. 
        /// </summary>
        public bool AlwaysResolveSymbols;
    }

    public static class TraceLogExtensionsMethods
    {
        public static TraceProcess Process(this TraceEvent anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.Processes.GetProcess(anEvent.ProcessID, anEvent.TimeStamp100ns);
        }
        public static TraceThread Thread(this TraceEvent anEvent)
        {
            TraceProcess process = Process(anEvent);
            return process.Threads.GetThread(anEvent.ThreadID, anEvent.TimeStamp100ns);
        }

        [CLSCompliant(false)]
        public static TraceLog Log(this TraceEvent anEvent)
        {
            return anEvent.Source as TraceLog;
        }
        public static TraceCallStack CallStack(this TraceEvent anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.GetCallStackForEvent(anEvent);
        }
        public static CallStackIndex CallStackIndex(this TraceEvent anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            if (log == null)
                return Microsoft.Test.EventTracing.CallStackIndex.Invalid;
            return log.GetCallStackIndexForEvent(anEvent);
        }
        public static TraceCallStacks CallStacks(this TraceEvent anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.CallStacks;
        }

        public static string ProgramCounterAddressString(this PageFaultTraceData anEvent)
        {
            TraceCodeAddress codeAddress = anEvent.ProgramCounterAddress();
            if (codeAddress != null)
                return codeAddress.ToString();
            return "";
        }
        public static TraceCodeAddress ProgramCounterAddress(this PageFaultTraceData anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.GetCodeAddressAtEvent(anEvent.ProgramCounter, anEvent);
        }
        public static CodeAddressIndex ProgramCounterAddressIndex(this PageFaultTraceData anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.GetCodeAddressIndexAtEvent(anEvent.ProgramCounter, anEvent);
        }

        public static string IntructionPointerCodeAddressString(this SampledProfileTraceData anEvent)
        {
            TraceCodeAddress codeAddress = anEvent.IntructionPointerCodeAddress();
            if (codeAddress != null)
                return codeAddress.ToString();
            return "";
        }
        // Only really useful when SampleProfile does not have callStacks turned on, since it is in the eventToStack. 
        public static TraceCodeAddress IntructionPointerCodeAddress(this SampledProfileTraceData anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.GetCodeAddressAtEvent(anEvent.InstructionPointer, anEvent);
        }
        public static CodeAddressIndex IntructionPointerCodeAddressIndex(this SampledProfileTraceData anEvent)
        {
            TraceLog log = anEvent.Source as TraceLog;
            return log.GetCodeAddressIndexAtEvent(anEvent.InstructionPointer, anEvent);
        }
        [CLSCompliant(false)]
        public static TraceLoadedModule ModuleForAddress(this TraceEvent anEvent, Address address)
        {
            return Process(anEvent).LoadedModules.GetModuleContainingAddress(address, anEvent.TimeStamp100ns);
        }
    }

    #region Private Classes

    internal static class SerializerExtentions
    {
        public static void WriteAddress(this Serializer serializer, Address address)
        {
            serializer.Write((long)address);
        }
        public static void ReadAddress(this Deserializer deserializer, out Address address)
        {
            long longAddress;
            deserializer.Read(out longAddress);
            address = (Address)longAddress;
        }
    }

    /// <summary>
    /// Default Implementation of symbol resolver.
    /// This implementation assume it the symbol resolution is happening on the same machine the trace is taken on.
    /// this symbol resolve relies on the image information in the image to resolve the symbols.
    /// </summary>
    public struct SymbolResolverContextInfo
    {
        public IntPtr currentProcessHandle;
        internal TraceEventNativeMethods.IMAGEHLP_LINE64 lineInfo;
    }
    internal interface ISymbolResolver
    {
        bool InitSymbolResolver(SymbolResolverContextInfo context);
        bool GetLineFromAddr(Address address);
        void CleanUp();
        ulong LoadSymModule(string moduleName, ulong moduleBase);
        IntPtr CurrentProcessHandle { get; }
    }
    internal interface ISymbolReader
    {
        bool GetLineFromAddr(Address address, ref TraceEventNativeMethods.IMAGEHLP_LINE64 lineInfo);
    }

    internal class DefaultSymbolReader : ISymbolReader
    {
        public DefaultSymbolReader(SymbolResolverContextInfo contextInfo)
        {
            context = contextInfo;
        }
        public bool GetLineFromAddr(Address address, ref TraceEventNativeMethods.IMAGEHLP_LINE64 lineInfo)
        {
            int displacement = 0;
            return (!TraceEventNativeMethods.SymGetLineFromAddrW64(context.currentProcessHandle, (ulong)address,
                ref displacement, ref lineInfo));
        }

        private SymbolResolverContextInfo context;
    }

    internal unsafe class TracePdbReader : IDisposable
    {
        public TracePdbReader()
        {
            TraceEventNativeMethods.SymOptions options = TraceEventNativeMethods.SymGetOptions();
            TraceEventNativeMethods.SymSetOptions(
                TraceEventNativeMethods.SymOptions.SYMOPT_DEBUG |
                // TraceEventNativeMethods.SymOptions.SYMOPT_DEFERRED_LOADS |
                TraceEventNativeMethods.SymOptions.SYMOPT_LOAD_LINES |
                TraceEventNativeMethods.SymOptions.SYMOPT_EXACT_SYMBOLS |
                TraceEventNativeMethods.SymOptions.SYMOPT_UNDNAME
                );
            TraceEventNativeMethods.SymOptions options1 = TraceEventNativeMethods.SymGetOptions();

            currentProcess = Process.GetCurrentProcess();  // Only here to insure processHandle does not die.  TODO get on safeHandles. 
            currentProcessHandle = currentProcess.Handle;
            string symPath = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
            if (symPath == null)
            {
                string temp = Environment.GetEnvironmentVariable("TEMP");
                if (temp == null)
                    temp = ".";
                string symCache = Path.Combine(temp, "symbols");
                string symSrv = "SRV*" + symCache + "*http://msdl.microsoft.com/download/symbols";
                Environment.SetEnvironmentVariable("_NT_SYMBOL_PATH", symSrv);
                Console.WriteLine("No symbol path set. Setting to:" + symSrv);
            }

            bool success = TraceEventNativeMethods.SymInitializeW(currentProcessHandle, null, false);
            if (!success)
            {
                // This captures the GetLastEvent (and has to happen before calling CloseHandle()
                currentProcessHandle = IntPtr.Zero;
                throw new Win32Exception();
            }
            callback = new TraceEventNativeMethods.SymRegisterCallbackProc(this.StatusCallback);
            success = TraceEventNativeMethods.SymRegisterCallbackW64(currentProcessHandle, callback, 0);

            Debug.Assert(success);

            // TODO remove when we are sure we won't use SymFromAddr
            // const int maxNameLen = 512;
            // int bufferSize = sizeof(TraceEventNativeMethods.SYMBOL_INFO) + maxNameLen*2;
            // buffer = (byte*)Marshal.AllocHGlobal(bufferSize);
            // TraceEventNativeMethods.ZeroMemory((IntPtr)buffer, (uint)bufferSize);
            // symbolInfo = (TraceEventNativeMethods.SYMBOL_INFO*)buffer;
            // symbolInfo->SizeOfStruct = (uint)sizeof(TraceEventNativeMethods.SYMBOL_INFO);
            // symbolInfo->MaxNameLen = maxNameLen - 1;

            lineInfo.SizeOfStruct = (uint)sizeof(TraceEventNativeMethods.IMAGEHLP_LINE64);
            messages = new StringBuilder();
        }
        public TracePdbModuleReader LoadSymbolsForModule(string moduleFilepath, Address moduleImageBase)
        {
            return new TracePdbModuleReader(this, moduleFilepath, moduleImageBase);
        }
        public void Dispose()
        {
            if (currentProcessHandle != IntPtr.Zero)
            {
                // Can't do this in the finalizer as the handle may not be valid then.  
                TraceEventNativeMethods.SymCleanup(currentProcessHandle);
                currentProcessHandle = IntPtr.Zero;
                currentProcess.Close();
                currentProcess = null;
                callback = null;
                messages = null;
            }
        }

        #region private
        ~TracePdbReader()
        {
            /*** TODO remove
            if (buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)buffer);
                buffer = null;
            }
             * ***/
        }

        private bool StatusCallback(
            IntPtr hProcess,
            TraceEventNativeMethods.SymCallbackActions ActionCode,
            ulong UserData,
            ulong UserContext)
        {
            bool ret = false;
            switch (ActionCode)
            {
                case TraceEventNativeMethods.SymCallbackActions.CBA_DEBUG_INFO:
                    lastLine = new String((char*)UserData);
                    messages.Append(lastLine);
                    ret = true;
                    break;
                default:
                    // Console.WriteLine("In status callback Code = " + ActionCode);
                    break;
            }
            return ret;
        }

        internal Process currentProcess;      // keep to insure currentProcessHandle stays alive
        internal IntPtr currentProcessHandle; // TODO really need to get on safe handle plan 
        internal string lastLine;
        internal StringBuilder messages;
        internal TraceEventNativeMethods.SymRegisterCallbackProc callback;

        // TODO put these in TracePdbModuleReader
        // internal TraceEventNativeMethods.SYMBOL_INFO* symbolInfo;
        // internal byte* buffer;
        internal TraceEventNativeMethods.IMAGEHLP_LINE64 lineInfo;
        #endregion
    }

    internal unsafe class TracePdbModuleReader : IDisposable
    {
        public string FindMethodForAddress(Address address, out Address endOfSymbol)
        {
            int amountLeft;
            int rva = MapToOriginalRva(address, out amountLeft);

            if (syms.Count > 0 && syms[0].StartRVA <= rva)
            {
                int high = syms.Count;
                int low = 0;
                for (; ; )
                {
                    int mid = (low + high) / 2;
                    if (mid == low)
                        break;
                    if (syms[mid].StartRVA <= rva)
                        low = mid;
                    else
                        high = mid;
                }
                Debug.Assert(low < syms.Count);
                Debug.Assert(low + 1 == high);
                Debug.Assert(syms[low].StartRVA <= rva);
                Debug.Assert(low >= syms.Count - 1 || rva < syms[low + 1].StartRVA);
                Sym sym = syms[low];
                if (sym.Size == 0)
                {
                    endOfSymbol = (Address)((long)address + amountLeft);
                    return sym.MethodName;
                }
                if (rva < sym.StartRVA + sym.Size)
                {
                    int symbolLeft = (int)sym.Size - (rva - sym.StartRVA);
                    amountLeft = Math.Min(symbolLeft, amountLeft);
                    Debug.Assert(0 <= amountLeft && amountLeft < 0x10000);      // Not true but useful for unit testing
                    endOfSymbol = (Address)((long)address + amountLeft);
                    return sym.MethodName;
                }
                // Console.WriteLine("No match");
            }
            endOfSymbol = Address.Null;
            return "";
        }
        public void FindSourceLineForAddress(Address address, out int lineNum, ref string sourceFile)
        {
            int displacement = 0;
            if (!TraceEventNativeMethods.SymGetLineFromAddrW64(reader.currentProcessHandle, (ulong)address, ref displacement, ref reader.lineInfo))
            {
                lineNum = 0;
                sourceFile = "";
                return;
            }
            lineNum = (int)reader.lineInfo.LineNumber;

            // Try to reuse the source file name as much as we can.  Don't create a new string unless we
            // have to. 
            for (int i = 0; ; i++)
            {
                if (reader.lineInfo.FileName[i] == 0 && i == sourceFile.Length)
                    return;
                if (i >= sourceFile.Length)
                    break;
                if (reader.lineInfo.FileName[i] != sourceFile[i])
                    break;
            }
            sourceFile = new String((char*)reader.lineInfo.FileName);
        }
        public void Dispose()
        {
            if (!TraceEventNativeMethods.SymUnloadModule64(reader.currentProcessHandle, (ulong)moduleImageBase))
                Console.WriteLine("Error unloading module with image base " + ((ulong)moduleImageBase).ToString("x"));
        }

        #region private
        internal TracePdbModuleReader(TracePdbReader reader, string moduleFilepath, Address imageBase)
        {
            this.reader = reader;
            reader.messages.Length = 0;
            reader.lastLine = "";
            ulong imageBaseRet = TraceEventNativeMethods.SymLoadModuleExW(reader.currentProcessHandle, IntPtr.Zero,
                moduleFilepath, null, (ulong)imageBase, 0, null, 0);

            if (imageBaseRet == 0)
                throw new Exception("Fatal error loading symbols for " + moduleFilepath);
            this.moduleImageBase = (Address)imageBaseRet;
            Debug.Assert(moduleImageBase == imageBase);

            if (reader.lastLine.IndexOf(" no symbols") >= 0 || reader.lastLine.IndexOf(" export symbols") >= 0)
                throw new Exception(
                    "   Could not find PDB file for " + moduleFilepath + "\r\n" +
                    "   Detailed Diagnostic information.\r\n" +
                    "      " + reader.messages.ToString().Replace("\n", "\r\n      "));
            if (reader.lastLine.IndexOf(" public symbols") >= 0)
                Console.WriteLine("Loaded only public symbols.");

            // See if we have an object file map (created by BBT)
            TraceEventNativeMethods.OMAP* fromMap = null;
            toMap = null;
            ulong toMapCount = 0;
            ulong fromMapCount = 0;
            if (!TraceEventNativeMethods.SymGetOmaps(reader.currentProcessHandle, (ulong)moduleImageBase, ref toMap, ref toMapCount, ref fromMap, ref fromMapCount))
                Console.WriteLine("No Object maps found");
            this.toMapCount = (int)toMapCount;
            if (toMapCount <= 0)
                toMap = null;

            /*
            Console.WriteLine("Got ToMap");
            for (int i = 0; i < (int) toMapCount; i++)
                Console.WriteLine("Rva {0:x} -> {1:x}", toMap[i].rva, toMap[i].rvaTo);
            Console.WriteLine("Got FromMap");
            for (int i = 0; i < (int) toMapCount; i++)
                Console.WriteLine("Rva {0:x} -> {1:x}", toMap[i].rva, toMap[i].rvaTo);
            */

            syms = new List<Sym>(5000);
            TraceEventNativeMethods.SymEnumSymbolsW(reader.currentProcessHandle, (ulong)moduleImageBase, "*",
                delegate(TraceEventNativeMethods.SYMBOL_INFO* symbolInfo, uint SymbolSize, IntPtr UserContext)
                {
                    int amountLeft;
                    int mappedRVA = MapToOriginalRva((Address)symbolInfo->Address, out amountLeft);
                    if (mappedRVA == 0)
                        return true;
                    Sym sym = new Sym();
                    sym.MethodName = new String((char*)(&symbolInfo->Name));
                    sym.StartRVA = mappedRVA;
                    sym.Size = (int)symbolInfo->Size;
                    syms.Add(sym);
                    return true;
                }, IntPtr.Zero);

            Console.WriteLine("Got {0} symbols and {1} mappings ", syms.Count, toMapCount);
            syms.Sort(delegate(Sym x, Sym y) { return x.StartRVA - y.StartRVA; });
        }

        /// <summary>
        /// BBT splits up methods into many chunks.  Map the final RVA of a symbol back into its
        /// pre-BBTed RVA.  
        /// </summary>
        private int MapToOriginalRva(Address finalAddress, out int amountLeft)
        {
            int rva = (int)(finalAddress - moduleImageBase);
            if (toMap == null || rva < toMap[0].rva)
            {
                amountLeft = 0;
                return rva;
            };
            Debug.Assert(toMapCount > 0);
            int high = toMapCount;
            int low = 0;

            // Invarient toMap[low]rva <= rva < toMap[high].rva (or high == toMapCount)
            for (; ; )
            {
                int mid = (low + high) / 2;
                if (mid == low)
                    break;
                if (toMap[mid].rva <= rva)
                    low = mid;
                else
                    high = mid;
            }
            Debug.Assert(toMap[low].rva <= rva);
            Debug.Assert(low < toMapCount);
            Debug.Assert(low + 1 == high);
            Debug.Assert(toMap[low].rva <= rva && (low >= toMapCount - 1 || rva < toMap[low + 1].rva));

            if (low + 1 < toMapCount)
                amountLeft = toMap[low + 1].rva - rva;
            else
                amountLeft = 0;
            int diff = rva - toMap[low].rva;

            int ret = toMap[low].rvaTo + diff;
#if false
            int slowAmountLeft;
            int slowRet = MapToOriginalRvaSlow(finalAddress, out slowAmountLeft);
            Debug.Assert(slowRet == ret);
            Debug.Assert(slowAmountLeft == amountLeft);
#endif

            return ret;
        }

#if DEBUG
        private int MapToOriginalRvaSlow(Address finalAddress, out int amountLeft)
        {
            int rva = (int)(finalAddress - moduleImageBase);
            Debug.Assert(toMapCount > 0);
            if (toMap == null || rva < toMap[0].rva)
            {
                amountLeft = 0;
                return rva;
            };

            int i = 0;
            for (; ; )
            {
                if (i >= toMapCount)
                {
                    amountLeft = 0;
                    return toMap[i - 1].rvaTo + (rva - toMap[i - 1].rva);
                }
                if (rva < toMap[i].rva)
                {
                    amountLeft = toMap[i].rva - rva;
                    if (i != 0)
                    {
                        --i;
                        rva = toMap[i].rvaTo + (rva - toMap[i].rva);
                    }
                    return rva;
                }
                i++;
            }
        }
#endif
        class Sym
        {
            public int StartRVA;
            public int Size;
            public string MethodName;
        };

        TracePdbReader reader;
        Address moduleImageBase;
        TraceEventNativeMethods.OMAP* toMap;
        int toMapCount;
        List<Sym> syms;
        #endregion
    }

    /// <summary>
    /// Represents a source for an ETLX file.  This is the class returned by the code:TraceEvents.GetSource
    /// methodIndex 
    /// </summary>
    class ETLXTraceEventSource : TraceEventDispatcher
    {
        public override bool Process()
        {

            // This basically a foreach loop, however we cheat and substitute our own dispatcher 
            // to do the lookup.  TODO: is there a better way?
            IEnumerator<TraceEvent> enumerator = ((IEnumerable<TraceEvent>)events).GetEnumerator();
            TraceEvents.EventEnumeratorBase asBase = (TraceEvents.EventEnumeratorBase)enumerator;
            events.log.FreeLookup(asBase.lookup);
            asBase.lookup = this;
            try
            {
                while (enumerator.MoveNext())
                {
                    Dispatch(enumerator.Current);
                    if (stopProcessing)
                        return false;
                }
            }
            finally
            {
                events.log.FreeReader(asBase.reader);
            }
            return true;
        }
        public TraceLog Log { get { return events.log; } }

        public unsafe ETLXTraceEventSource Clone()
        {
            ETLXTraceEventSource ret = new ETLXTraceEventSource(events);
            foreach (TraceEvent template in Templates)
            {
                // TODO: it would be better if we cleaned up this potentially dangling pointer
                template.eventRecord = null;
                template.userData = IntPtr.Zero;
                ((ITraceParserServices)ret).RegisterEventTemplate(template.Clone());
            }
            return ret;
        }
        public override void Dispose()
        {
        }
        #region private
        protected override void RegisterEventTemplateImpl(TraceEvent template)
        {
            template.source = Log;
            base.RegisterEventTemplateImpl(template);
        }
        internal ETLXTraceEventSource(TraceEvents events)
        {
            this.events = events;
            this.unhandledEventTemplate.source = Log;
            this.userData = Log.UserData;
        }

        TraceEvents events;
        #endregion
    }

#if MAC_SUPPORT
    class MacTraceEventSource : TraceEventDispatcher
    {
        public MacTraceEventSource(string fileName)
        {
            this.fileName = fileName;
        }
        public override bool Process()
        {
            using (PinnedStreamReader reader = new PinnedStreamReader(fileName))
            {
                // Read in the header.
                int signature = reader.ReadInt32();     // Just a number that identifies the file. 
                Debug.Assert(signature == 0x3AD34349);
                int version = reader.ReadInt32();
                sessionStartTime100ns = reader.ReadInt64();
                sessionEndTime100ns = reader.ReadInt64();
                pointerSize = reader.ReadInt32();
                eventsLost = reader.ReadInt32();
                numberOfProcessors = reader.ReadInt32();
                cpuSpeedInMHz = reader.ReadInt32();

                while (reader.Current < (StreamLabel) reader.Length)
                {
                    // TODO fold in with logic in GetNext
                    IntPtr ptr = reader.GetPointer(TraceEvent.HeaderLength);

                    // The events are expected to have a header that looks like code:TraceEventNativeMethods.EVENT_TRACE
                    // the important data is in code:TraceEvent#TraceEventRecordLayout
                    TraceEvent ret = Lookup(ptr);

                    // Confirm we have a half-way sane event, to catch obvious loss of sync.  
                    Debug.Assert(ret.Level <= TraceEventLevel.Verbose);
                    Debug.Assert(ret.Version <= 2);
                    Debug.Assert(ret.TimeCreated100ns == long.MaxValue ||
                        SessionStartTime100ns <= ret.TimeCreated100ns && ret.TimeCreated100ns <= SessionEndTime100ns);

                    // We have to insure we have a pointer to the whole blob, not just the header.  
                    int totalLength = TraceEvent.HeaderLength + (ret.EventDataLength + 3 & ~3);
                    Debug.Assert(totalLength < 0x20000);
                    ret.rawData = reader.GetPointer(totalLength);
                    ret.mofData = TraceEventRawReaders.Add(ret.rawData, TraceEvent.HeaderLength);

                    Dispatch(ret);
                    if (stopProcessing)
                        return false;

                    reader.Skip(totalLength);
                }
            }
            return true;
        }
    #region private
        private string fileName;
    #endregion

#if true
        /// <summary>
        /// Test code for using a source.  This assumes you dumped the data to test.etlm and it will print
        /// it out as text in a verbose dumping mode.  
        /// </summary>
        static void Test()
        {
            MacTraceEventSource source = new MacTraceEventSource("test.etlm");
            source.EveryEvent += delegate(TraceEvent anEvent)
                         if (anEvent is UnhandledTraceEvent)
                    Console.Write(anEvent.Dump());
                else 
                    Console.WriteLine(anEvent.ToString());
            };
            source.Process();
        }
#endif
    }
#endif
    #endregion
}
