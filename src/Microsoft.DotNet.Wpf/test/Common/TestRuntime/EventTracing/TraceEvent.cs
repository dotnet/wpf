// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using Microsoft.Test.EventTracing.FastSerialization;
using System.Diagnostics.Eventing;

// #Introduction
// 
// The the heart of the ETW reader are two important classes.
// 
//     * code:TraceEventSource which is an abstract represents the stream of events as a whole. Thus it
//         has holds things like session start and stop times, number of lost events etc.
//         
//     * code:TraceEvent is a base class that represents an individual event payload. Because different
//         events have different fields, this is actually the base of a class hierarchy. code:TraceEvent
//         itself holds all properties that are common to all events (like TimeStamp, ProcessID,
//         ThreadID, etc). Subclasses then add properties that know how to parse that specific event
//         type.
//         
// However these two classes are not enough. ETW has a model where there can be many independent
// providers each of which contributed to the event stream. Since the number of providers is unknown,
// code:TraceEventSource can not know the details of decoding all possible events. Instead we introduce
// a class
// 
//    code:TraceEventParser
//  
// one for each provider. This class knows the details of taking a binary blob representing a event and
// turning it into a code:TraceEvent. Since each provider has different details of how to do this,
// code:TraceEventParser is actually just a base class and specific subclasses of code:TraceEventParser
// like code:KernelTraceEventParser or code:ClrTraceEventParser do the real work.
// 
// TraceEventParsers have a very ridged layout that closely parallels the data in the providers's ETW
// manifest (in fact there is a tool for creating TraceEventParser's from a ETW manifest). A
// TraceEventParser has a C# event (callback) for each different event the provider can generate. These
// events callbacks have an argument that is the specific subclass of code:TraceEvent that represents
// the payload for that event. This allows client code to 'subscribe' to events in a strongly typed way.
// For example:
// 
// * ETWTraceEventSource source = new ETWTraceEventSource("output.etl"); // open an ETL file
// * KernelTraceEventParser kernelEvents = new KernelTraceEventParser(source); // Attach Kernel Parser.
// *
// * // Subscribe to the ImageLoad event that the KernelTraceEventParser knows about.
// * kernelEvents.ImageLoad += delegate(ImageLoadTraceData data) {
//      * Console.WriteLine("Got Image Base {0} ModuleFile {1} ", data.BaseAddress, data.FileName);
// * };
// *
// * // Attach more parsers, and subscribe to more events.
// * source.Process(); // Read through the stream, calling all the callbacks in one pass.
// 
// In the example above, ETWTraceEventSource (a specific subclass of TraceEventSource that understand
// ETL files) is created by opening the 'output.etl' file. Then the KernelTraceEventParser is 'attached'
// to the source so that kernel events can be decoded. Finally a callback is registered with the
// KernelTraceEventParser, to call user code when the 'ImageLoad' event is found in the stream. The user
// code has access to an code:ImageLoadTraceData which is a subclass of code:TraceEvent and has properties
// like 'BaseAddress' and 'FileName' which are specific to that particular event. The user can subscribe
// to many such events (each having differnet event-specific data), and then finaly call Process() which
// causes the source to enumerate the event stream, calling the appropriate callbacks.
// 
// This model has the important attribute that new TraceEventParsers (ETW provviders), can be crated and
// used by user code WITHOUT changing the code associated with code:TraceEventSource. Unfortunatley, it
// has a discoverability drawback. Given a TraceEventSource (like ETWTraceEventSource), it is difficult
// discover that you need classes like code:KernelTraceEventParser to do anything useful with the
// source. As a concession to discoverabilty, TraceEventSource provides properties ('Kernel' and CLR)
// for two 'well known' parsers. Thus the above example can be written
// 
// * ETWTraceEventSource source = new ETWTraceEventSource("output.etl"); // open an ETL file
// * source.Kernel.ImageLoad += delegate(ImageLoadTraceData data) {
//      * Console.WriteLine("Got Image Base {0} ModuleFile {1} ", data.BaseAddress, data.FileName);
// * };
// * source.Process(); // Read through the stream, calling all the callbacks in one pass.
// 
// To keep efficiently high, this basic decode in Process() does NOT allocate new event every time a
// callback is made. Instead code:TraceEvent passed to the callback is reused on later events, so
// clients must copy the data out if they need to persist it past the time the callback returns. The
// code:TraceEvent.Clone methodIndex can be used to form a copy of a code:TraceEvent that will not be reused
// by the code:TraceEventSource.
// 
// Another important attribute of the system is that decoding of the fields of code:TraceEvent is done
// lazily. For example code:ImageLoadTraceData does not actually contain fields for things like
// 'BaseAddress' or 'FileName', but simply a pointer to the raw bits from the file. It is only when a
// property like code:ImageLoadTraceData.FileName it invoked that the raw bits are actually read converted
// to a string. The rationale for this approach is that it is common that substantial parts of an
// event's payload may be ignored by any particular client. A concequence of this approach is that for
// properites that do non-trivial work (like create a string from the raw data) it is better not to call
// the property mulitple times (instead cache it locally in a local variable).
// 
// Supporting Sources that don't implement a callback model
// 
// In the previous example code:ETWTraceEventSource supported the subscription model where the client
// regisgters a set of callbacks and then calls Process() to cause the callbacks to happen. This model
// is very efficient and allows alot of logically distinct processing to be done in 'one pass'. However
// we also want to support sources that do not wish to support the callback model (opting instead for a
// iteration model). To support this code:TraceEventSource that knows how to do this dispatch (as well
// as the Process()) methodIndex), is actually put in a subclass of code:TraceEventSource called
// code:TraceEventDispatcher. Those sources that support the subscription model inherit from
// code:TraceEventSource, and those that do not inherit directly from code:TraceEventSource.
// 
// The Protocol between code:TraceEventParser and code:TraceEventSource
// 
// What is common among all TraceEventSources (even if they do not support callbacks), is that parsers
// need to be registered with the source so that the source can decode the events. This is the purpose
// of the code:TraceEventSource.RegisterParser and code:TraceEventSource.RegisterEventTemplate methods.
// The expectation is that when a subclass of code:TraceEventParser is constructed, it will be passed a
// code:TraceEventSource. The parser should call the RegisterParser methodIndex, so that the source knows
// about this new parser. Also any time a user subscribes to a particular event in the parser, the
// source needs to know about so that its (shared) event dispatch table can be updated this is what
// RegisterEventTemplate is for.
// 
// * See also
//     * code:ETWTraceEventSource a trace event source for a .ETL file or a 'real time' ETW stream.
//     * code:ETLXTraceEventSource a trace event source for a ETLX file (post-porcesses ETL file).
//     * code:TraceEventParser is the base class for all event parsers for TraceEvents.
//     * code:TraceEventDispatcher contains logic for dispatching events in the callback model
//         * The heart of the callback logic is code:TraceEventDispatcher.Dispatch
namespace Microsoft.Test.EventTracing
{
    /// <summary>
    /// represents an address for the machine where the log was created.  
    /// </summary>
    public enum Address : long { Null = 0 }

    /// <summary>
    /// code:TraceEventSource represents a list of events (eg a ETL file or ETLX file or a real time stream).
    /// There are two basic models for acessing such a list, either a callback model (where clients register
    /// their desire to know about particular events), and the iterator model (where you can use a 'foreach'
    /// on the list to get each event in turn. code:TraceEventSource represnts all those aspects of the list
    /// of event that is INDEPENDENT of which model you use. Thus code:TraceEventSource does not actually
    /// have the most interesting methods (Because the interesting methods deal with getting at the events)
    /// those actually are on sublasses
    /// 
    ///     * code:TraceEventDispatcher - is a subclass of code:TraceEventSource that supports the callback
    ///         model for accessing events. This interface can be used with 'real time' streams.
    ///     * code:System.Diagnostics.Eventing.TraceLog.TraceLog - is also a subclass of
    ///         code:TraceEventSource that supports the iteration model (through its
    ///         code:System.Diagnostics.Eventing.TraceLog.TraceLog.Events property. This mechanism can only
    ///         be used on files, because it supports a much broader variety of access methods (eg moving
    ///         backwards, annotating events ...)
    /// 
    /// Regardless of the model used to access the events, an important aspect of the system is that
    /// code:TraceEventSource does not know about the event-specific layout of an event (which allows new
    /// events to be added easily). Instead there needs to be a way for event specific
    /// parsers to register themselves (this is needed regarless of whether the callback or
    /// iterator model is used).  Providing the interface to do this is the primary purpose
    /// of code:TraceEventSource and is defined by the code:ITraceParserServices 
    ///      
    /// * see code:#Introduction for details
    /// </summary>
    [CLSCompliant(false)]
    abstract unsafe public class TraceEventSource : ITraceParserServices, IDisposable
    {
        // Properties to subscribe to find important parsers (these are convinience routines). 
        /// <summary>
        /// For convinience, we provide a property that will instantiate an object that knows how to parse
        /// all the Kernel events into callbacks.  See code:KernelTraceEventParser for more 
        /// </summary>
        public KernelTraceEventParser Kernel
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                if (_Kernel == null)
                    _Kernel = new KernelTraceEventParser(this);
                return _Kernel;
            }
        }
        public ClrTraceEventParser Clr
        {
            get
            {
                if (_CLR == null)
                    _CLR = new ClrTraceEventParser(this);
                return _CLR;
            }
        }

        /// <summary>
        /// For convinience, we provide a property that will instantiate an object that knows how to parse
        /// all providers that dump their manifests into the event stream.  See code:DynamicTraceEventParser for more 
        /// </summary>
        public DynamicTraceEventParser Dynamic
        {
            get
            {
                if (_Dynamic == null)
                    _Dynamic = new DynamicTraceEventParser(this);
                return _Dynamic;
            }
        }
        /// <summary>
        /// The time when session started logging. 
        /// </summary>
        public DateTime SessionStartTime { get { return DateTime.FromFileTime(SessionStartTime100ns); } }
        /// <summary>
        /// The time is expressed as a windows moduleFile time (100ns ticks since 1601). This is very
        /// efficient and useful for finding deltas between events quickly.
        /// </summary>
        public long SessionStartTime100ns { get { return sessionStartTime100ns; } }
        /// <summary>
        /// The time that the session stopped logging.
        /// </summary>
        public DateTime SessionEndTime { get { return DateTime.FromFileTime(SessionEndTime100ns); } }
        /// <summary>
        /// The end time expresses as a windows moduleFile time (100ns ticks since 1601).  This is very efficient
        /// and useful for finding deltas between events quickly.  
        /// </summary>
        public long SessionEndTime100ns { get { return sessionEndTime100ns; } }
        /// <summary>
        /// The differnet between SessionEndTime and SessionStartTime;
        /// </summary>
        public TimeSpan SessionDuration { get { return new TimeSpan(SessionEndTime100ns - SessionStartTime100ns); } }
        /// <summary>
        /// Returns a double representing the number of milliseconds 'time100ns' is from the offset of the log 
        /// </summary>
        /// <param name="time100ns">The time to convert to relative form</param>
        /// <returns>number of milliseconds from the begining of the log</returns>
        public double RelativeTimeMSec(long time100ns)
        {
            if (time100ns == MaxTime100ns)
                return double.PositiveInfinity;
            double msec = (time100ns - SessionStartTime100ns) / 10000.0;
            if (msec < 0)
                msec = 0;
            return msec;
        }
        /// <summary>
        /// Returns the size of a pointer on the machine where events were collected. 
        /// </summary>
        public int PointerSize { get { return pointerSize; } }
        /// <summary>
        /// The number of events that were dropped (event rate was too fast)
        /// </summary>
        public int EventsLost { get { return eventsLost; } }
        /// <summary>
        /// The number of processors on the machine doing the logging. 
        /// </summary>
        public int NumberOfProcessors { get { return numberOfProcessors; } }
        /// <summary>
        /// Cpu speed of the processor doing the logging. 
        /// </summary>
        public int CpuSpeedMHz { get { return cpuSpeedMHz; } }
        /// <summary>
        /// Should be called when you are done with the source.  
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        public virtual void Dispose() { }

        /// <summary>
        /// code:TraceEventSource support attaching arbitary user data to the source.  One convetion that
        /// has been established is that parsers that need additional state to parse their events should
        /// store them in 'parsers\(ParserName)'.  
        /// </summary>
        public IDictionary<string, object> UserData { get { return userData; } }

        #region protected
        internal protected static long MaxTime100ns = DateTime.MaxValue.ToFileTime();
        protected TraceEventSource()
        {
            userData = new Dictionary<string, object>();
        }

        [SecurityTreatAsSafe, SecurityCritical]
        abstract protected void RegisterEventTemplateImpl(TraceEvent template);
        [SecurityTreatAsSafe, SecurityCritical]
        abstract protected void RegisterParserImpl(TraceEventParser parser);
        [SecurityTreatAsSafe, SecurityCritical]
        abstract protected void RegisterUnhandledEventImpl(Action<TraceEvent> callback);
        [SecurityTreatAsSafe, SecurityCritical]
        virtual protected string TaskNameForGuidImpl(Guid guid) { return null; }
        [SecurityTreatAsSafe, SecurityCritical]
        virtual protected string ProviderNameForGuidImpl(Guid taskOrProviderGuid) { return null; }

        #region ITraceParserServices Members
        [SecurityTreatAsSafe, SecurityCritical]
        void ITraceParserServices.RegisterEventTemplate(TraceEvent template)
        {
            RegisterEventTemplateImpl(template);
        }
        [SecurityTreatAsSafe, SecurityCritical]
        void ITraceParserServices.RegisterParser(TraceEventParser parser)
        {
            RegisterParserImpl(parser);
        }
        [SecurityTreatAsSafe, SecurityCritical]
        void ITraceParserServices.RegisterUnhandledEvent(Action<TraceEvent> callback)
        {
            RegisterUnhandledEventImpl(callback);
        }
        string ITraceParserServices.TaskNameForGuid(Guid guid)
        {
            return TaskNameForGuidImpl(guid);
        }
        string ITraceParserServices.ProviderNameForGuid(Guid taskOrProviderGuid)
        {
            return ProviderNameForGuidImpl(taskOrProviderGuid);
        }
        #endregion
        [CLSCompliant(false)]
        protected IDictionary<string, object> userData;
        [CLSCompliant(false)]
        protected long sessionStartTime100ns;
        [CLSCompliant(false)]
        protected long sessionEndTime100ns;
        [CLSCompliant(false)]
        protected int pointerSize;
        [CLSCompliant(false)]
        protected int eventsLost;
        [CLSCompliant(false)]
        protected int numberOfProcessors;
        [CLSCompliant(false)]
        protected int cpuSpeedMHz;

        protected bool useClassicETW;
        [CLSCompliant(false)]
        protected KernelTraceEventParser _Kernel;
        [CLSCompliant(false)]
        protected ClrTraceEventParser _CLR;
        [CLSCompliant(false)]
        protected DynamicTraceEventParser _Dynamic;
        #endregion
        #region private
        protected internal virtual string ProcessName(int processID, long time100ns)
        {
            return "(" + processID.ToString() + ")";
        }
        #endregion
    }

    /// <summary>
    /// code:TraceEvent represents the data from one event. Logically a TraceEventSource is mostly just a
    /// stream of TraceEvent objects. An event is identified by a GUID (128 bit unique ID) of the event
    /// provider that generated it PLUS a small integer (must fit in a ushort) code:TraceEventID that
    /// distinguishes it among other events from the same provider.
    /// 
    /// The provider GUID and the Event ID together uniquely identify the data format of the event. Event
    /// providers can provide a description of events and their payloads in a manifest that is traditionally
    /// defined by an XML file (in Windows Vista). For Pre-vista ETW, the descriptions are done with MOF
    /// files. There are tools that can convert a XML manifest description to C# that defines a
    /// code:TraceEventParser which can be used by this infrastructure.
    ///
    /// There are operations (Start, Stop), that are common to a broad set of differnet events (from
    /// different providers), that should be processed in simmiar ways. To make identifing these common kinds
    /// of events easier, events can be given an code:TraceEventOpcode which indentified these common
    /// patterns.
    ///
    /// While Event data can have arbitrary data in it, there is a header that every event must have.
    /// TraceEvent provides an interface to this header data and other operations that can be done without
    /// needing to know that exact layout of the event data. In particular every event knows its Provider
    /// GUID (but not necessarily its name), opcode number (but not necessarily its opocode name), the time
    /// it happened, the code:TraceEventLevel (how important the event is) the thread it happened on (not all
    /// events have an associated thread), and the size of the event-specific data.
    /// 
    /// The basic architecture is that specific events (eg. the 'Process' event with opcode 'Start') define a
    /// new subclass of code:TraceEvent (eg. code:ProcessTraceData) that define properties that know how to
    /// parse the raw data into its various 'fields'.
    /// 
    /// In addition a code:TraceEvent instance has support for the subscription model in the form a a the
    /// code:TraceEvent.Dispatch virtual function. Events can remember a user-defined callback and dispatch
    /// to that callback when the Dispatch' virtual methodIndex is called.
    /// 
    /// An important restriction is that a TraceEvent becomes invalid after the callback is complete (it is
    /// reused for the next event of that type in the stream). Thus callers should NOT cache instances of the
    /// subclass of TraceEvent in their own data structures, but copy out the data they need or call
    /// code:TraceEvent.Clone if they need a permanent copy.
    /// </summary>

    public unsafe abstract class TraceEvent
    {
        /// <summary>
        /// The GUID that uniquely identifies the Provider for this event.  This can return Guid.Empty for
        /// pre-VISTA ETW providers.  
        /// </summary>        
        public Guid ProviderGuid { get { return providerGuid; } }

        public Guid TaskGuid { get { return taskGuid; } }

        public string ProviderName
        {
            get
            {
                if (providerName == null)
                {
                    Guid guid = providerGuid;
                    if (guid == Guid.Empty)
                        guid = taskGuid;
                    ITraceParserServices asParserServces = this.Source as ITraceParserServices;
                    if (asParserServces != null)
                        providerName = asParserServces.ProviderNameForGuid(guid);
                    if (providerName == null)
                    {
                        if (providerGuid == Guid.Empty)
                            providerName = "UnknownProvider";
                        else
                            providerName = "Provider(" + providerGuid.ToString() + ")";
                    }
                }
                return providerName;
            }
        }

        /// <summary>
        /// A name for the event.  This is simply the concatination of the task and opcode names. 
        /// </summary>
        public string EventName { get { return TaskName + OpcodeName; } }
        /// <summary>
        /// Returns the provider-specific integer value that uniquely identifies event within the scope of
        /// the provider. (Currently returns 0 for pre-VISTA ETW providers).
        /// 
        /// The strong convention (but is is only a convention) is that every (Task x Opcode) pair is given
        /// a unique ID.   
        /// 
        /// TODO: Synthesize something for pre-Vista?
        /// </summary>
        public TraceEventID ID
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                Debug.Assert(ClassicProvider || eventID == (TraceEventID)eventRecord->EventHeader.Id);
                return eventID;
            }
        }
        /// <summary>
        /// Events for a given provider can be given a group identifier called a Task that indicates the
        /// broad area within the provider that the event pertains to (for example the Kernel provider has
        /// Tasks for Process, Threads, etc).  
        /// </summary>
        public TraceEventTask Task { get { return task; } }
        /// <summary>
        /// The human readable name for the event's task (group of related events) (eg. process, thread,
        /// image, GC, ...).  May return a string Task(GUID) or Task(TASK_NUM) if no good symbolic name is
        /// available. 
        /// </summary>
        public string TaskName
        {
            get
            {
                if (taskName == null)
                {
                    if (taskGuid != Guid.Empty)
                    {
                        ITraceParserServices asParserServces = this.Source as ITraceParserServices;
                        if (asParserServces != null)
                            taskName = asParserServces.TaskNameForGuid(taskGuid);
                        if (taskName == null)
                            taskName = "Task(" + taskGuid + ")";
                    }
                    else if (Task != 0)
                        taskName = "Task" + Task;
                    else
                        taskName = "";
                }
                return taskName;
            }
        }
        /// <summary>
        /// Each event has a Type identifier that indicates what kind of an event is being logged. Note that
        /// providers are free to extend this set, so the id may not be just the value in code:TraceEventOpcode
        /// </summary>
        public TraceEventOpcode Opcode { get { return opcode; } }
        /// <summary>
        /// Returns the human-readable string name for the code:Opcode property. 
        /// </summary>
        public string OpcodeName
        {
            get
            {
                if (opcodeName == null)
                    opcodeName = ToString(Opcode);
                return opcodeName;
            }
        }

        /// <summary>
        /// The verbosity of the event (Fatal, Error, ..., Info, Verbose)
        /// </summary>
        public TraceEventLevel Level
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                // Debug.Assert(eventRecord->EventHeader.Level < 6, "Level out of range");
                return (TraceEventLevel)eventRecord->EventHeader.Level;
            }
        }
        /// <summary>
        /// The version number for this event.  
        /// </summary>
        public int Version
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get { return eventRecord->EventHeader.Version; }
        }
        /// <summary>
        /// When an entry is logged it can specify a bitfield TraceEventKeyword that identifies
        /// provider-specific 'areas' that the event is relevent to.  Return this bitfield for the event. 
        /// Returns TraceEventKeyword.None for pre-VISTA ETW providers. 
        /// </summary>
        public TraceEventKeyword Keyword
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get { return (TraceEventKeyword)eventRecord->EventHeader.Keyword; }
        }
        /// <summary>
        /// A Channel is a provider defined 'audience' for the event.  It is TraceEventChannel.Default for
        /// Pre-Vista providers.  
        /// </summary>
        public TraceEventChannel Channel
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get { return (TraceEventChannel)eventRecord->EventHeader.Channel; }
        }

        /// <summary>
        /// The thread ID for the event
        /// </summary>
        public int ThreadID
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                return eventRecord->EventHeader.ThreadId;
            }
        }
        /// <summary>
        /// The process ID of the process which caused the event. 
        /// </summary>
        public int ProcessID
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                return eventRecord->EventHeader.ProcessId;
            }
        }
        /// <summary>
        /// The time of the event, represented in 100ns units from the year 1601.  See also code:TimeStamp
        /// </summary>
        public long TimeStamp100ns
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                return eventRecord->EventHeader.TimeStamp;
            }
        }
        /// <summary>
        /// The time of the event. The overhead of creating a DateTime object can be avoided using
        /// code:TimeStamp100ns
        /// </summary>
        public DateTime TimeStamp
        {
            get { return DateTime.FromFileTime(TimeStamp100ns); }
        }
        /// <summary>
        /// Returns a short name for the process. This the image file name (without the path or extension),
        /// or if that is not present, then the string "(" ProcessID + ")" 
        /// </summary>
        public string ProcessName
        {
            get
            {
                string name = source.ProcessName(ProcessID, TimeStamp100ns);
                if (name == null)
                    name = "(" + ProcessID + ")";
                return name;
            }
        }
        /// <summary>
        /// Returns a double representing the number of milliseconds since the beining of the trace.     
        /// </summary>
        public double TimeStampRelativeMSec
        {
            get
            {
                return source.RelativeTimeMSec(TimeStamp100ns);
            }
        }
        /// <summary>
        /// The processor Number (from 0 to code:TraceEventSource.NumberOfProcessors) that generated this
        /// event. 
        /// </summary>
        public int ProcessorNumber
        {
            get
            {
                int ret = eventRecord->BufferContext.ProcessorNumber;
                Debug.Assert(0 <= ret && ret < source.NumberOfProcessors);
                return ret;
            }
        }
        /// <summary>
        /// Get the size of a pointer associated with the event.  
        /// 
        /// Note that for providers that run in the WOW this might return 4 on a 64 bit machines. 
        /// TODO: Figure out what bit in the event is used to disginguish this!
        /// </summary>
        public int PointerSize
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get
            {
                Debug.Assert((eventRecord->EventHeader.Flags & TraceEventNativeMethods.EVENT_HEADER_FLAG_64_BIT_HEADER) != 0 ||
                             (eventRecord->EventHeader.Flags & TraceEventNativeMethods.EVENT_HEADER_FLAG_32_BIT_HEADER) != 0);
                return (eventRecord->EventHeader.Flags & TraceEventNativeMethods.EVENT_HEADER_FLAG_64_BIT_HEADER) != 0 ? 8 : 4;
            }
        }

        /// <summary>
        /// An EventIndex is a integer that is guarenteed to be unique for this even over the entire log.  Its
        /// primary purpose is to act as a key that allows side tables to be built up that allow value added
        /// processing to 'attach' additional data to this particular event unambiguously.  
        /// 
        /// EventIndex is currently a 4 byte quantity.  This does limit log sizes to 4Gig of events, but
        /// that is a LOT of events.  (realistically > 100 GIG ETL files)   
        /// </summary>
        public EventIndex EventIndex { get { return eventIndex; } }

        /// <summary>
        /// The TraceEventSource associated with this event
        /// </summary>
        [CLSCompliant(false)] public TraceEventSource Source { get { return source; } }

        /* TODO figure out what Ticks are (100ns units?), and when the fields are valid
        public int CPUTimeTicks { get { return KernelTimeTicks + UserTimeTicks; } }
        public int KernelTimeTicks { get { return eventRecord->EventHeader.KernelTime; } }
        public int UserTimeTicks { get { return eventRecord->EventHeader.UserTime; } }
         ****/
        // TODO expose ActivityID?
        // public int ActivityID { get { return eventRecord->EventHeader.ActivityId; } }

        /// <summary>
        /// The size of the Event-specific data payload.  (see code:EventData)
        /// </summary>
        public int EventDataLength
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get { return eventRecord->UserDataLength; }
        }
        /// <summary>
        /// Returns an array of bytes reprsenting the Event-specific payload associted with the event.  
        /// </summary>
        /// <returns></returns>
        public byte[] EventData()
        {
            return EventData(null, 0, 0, EventDataLength);
        }
        /// <summary>
        /// Gets the event data and puts it in 'targetBuffer' at 'targetStartIndex' and returns the resulting buffer.
        /// If 'targetBuffer is null, it will allocate a buffer of the correct size.  Note that normally you
        /// don't need to use this routine as some subclass of EventData that does proper parsing will work
        /// for you instead.  
        /// </summary>
        public byte[] EventData(byte[] targetBuffer, int targetStartIndex, int sourceStartIndex, int length)
        {
            if (targetBuffer == null)
            {
                Debug.Assert(targetStartIndex == 0);
                targetBuffer = new byte[length + targetStartIndex];
            }
            // TODO overflow
            if (sourceStartIndex + length > EventDataLength)
                throw new IndexOutOfRangeException();

            IntPtr start = (IntPtr)((byte*)DataStart.ToPointer() + sourceStartIndex);
            if (length > 0)
                Marshal.Copy(start, targetBuffer, targetStartIndex, length);
            return targetBuffer;
        }

        /// <summary>
        /// The events passed to the callback functions only last as long as the callback, so if you need to
        /// keep the information around after that you need to copy it.  If it is convinient to store it as
        /// the original event, you can do so using this Clone functionality.  Note that this operation is
        /// not really cheap, so you should avoid calling it if you can. 
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        public unsafe virtual TraceEvent Clone()
        {
            TraceEvent ret = (TraceEvent)MemberwiseClone();     // Clone myself. 
            if (eventRecord != null)
            {

                int userDataLength = (EventDataLength + 3) / 4 * 4;            // DWORD align
                int totalLength = sizeof(TraceEventNativeMethods.EVENT_RECORD) + userDataLength;

                IntPtr eventRecordBuffer = TraceEventNativeMethods.AllocHGlobal(totalLength);

                IntPtr userDataBuffer = (IntPtr)(((byte*)eventRecordBuffer) + sizeof(TraceEventNativeMethods.EVENT_RECORD));

                CopyBlob((IntPtr)eventRecord, eventRecordBuffer, sizeof(TraceEventNativeMethods.EVENT_RECORD));
                CopyBlob(userData, userDataBuffer, userDataLength);

                ret.eventRecord = (TraceEventNativeMethods.EVENT_RECORD*)eventRecordBuffer;
                ret.userData = userDataBuffer;
                ret.myBuffer = eventRecordBuffer;
            }
            return ret;
        }
        /// <summary>
        /// Pretty print the event.  It uses XML syntax so you can make XML with this routine too. 
        /// </summary>
        public override string ToString()
        {
            return ToXml(new StringBuilder()).ToString();
        }
        /// <summary>
        /// Dumps a very verbose description of the event, including a dump of they payload bytes. It is in
        /// XML format. This is very useful in debugging (put it in a watch window) when parsers are not
        /// interpreting payloads properly.
        /// </summary>
        public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            Prefix(sb);
            sb.AppendLine().Append(' ');
            sb.XmlAttrib("ID", ID);
            sb.XmlAttrib("OpcodeNum", (int)Opcode);
            sb.XmlAttrib("Version", Version);
            sb.XmlAttrib("Level", Level);
            sb.XmlAttrib("PointerSize", PointerSize);
            if (ProviderGuid != Guid.Empty)
                sb.AppendLine().Append(' ').XmlAttrib("ProviderGuid", ProviderGuid);
            if (taskGuid != Guid.Empty)
                sb.AppendLine().Append(' ').XmlAttrib("TaskGuid", taskGuid);
            sb.Append('>').AppendLine();
            byte[] data = EventData();
            sb.Append("  <Payload").XmlAttrib("Length", EventDataLength).Append(">").AppendLine();

            StringWriter dumpSw = new StringWriter();
            DumpBytes(data, dumpSw, "    ");
            sb.Append(XmlUtilities.XmlEscape(dumpSw.ToString(), false));

            sb.AppendLine("  </Payload>");
            sb.Append("</Event>");
            return sb.ToString();
        }

        public virtual StringBuilder ToXml(StringBuilder sb)
        {
            Prefix(sb);
            if (ProviderGuid != Guid.Empty)
                sb.XmlAttrib("ProviderName", ProviderName);
            string[] payloadNames = PayloadNames;
            for (int i = 0; i < payloadNames.Length; i++)
                sb.XmlAttrib(payloadNames[i], PayloadValue(i));
            sb.Append("/>");
            return sb;
        }

        /// <summary>
        /// returns the names of all the manifest declared field names for the event.  
        /// </summary>
        public abstract string[] PayloadNames { get; }

        [CLSCompliant(false)]
        protected internal string[] payloadNames;

        /// <summary>
        /// Given an index from 0 to PayloadNames.Length-1, return the value for that payload item
        /// as an object (boxed if necessary).  
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public abstract object PayloadValue(int index);

        /// <summary>
        /// Only use this if you don't care about performance.  It fetches a field by name.  Will return
        /// null if the name is not found.
        /// </summary>
        /// <param name="fieldName"></param>
        public object PayloadByName(string fieldName)
        {
            string[] fieldNames = PayloadNames;
            for (int i = 0; i < fieldNames.Length; i++)
                if (fieldName == fieldNames[i])
                    return PayloadValue(i);
            return null;
        }


        /// <summary>
        /// Used for binary searching of event IDs.    Abstracts the size (currently a int, could go to long) 
        /// </summary>
        [CLSCompliant(false)]
        public static int Compare(EventIndex id1, EventIndex id2)
        {
            return (int)id1 - (int)id2;
        }
        #region Protected
        protected TraceEvent(int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
        {
            Debug.Assert((ushort)eventID == eventID);
            this.eventID = (TraceEventID)eventID;
            this.task = (TraceEventTask)task;
            this.taskName = taskName;
            this.taskGuid = taskGuid;
            Debug.Assert((byte)opcode == opcode);
            this.opcode = (TraceEventOpcode)opcode;
            this.opcodeName = opcodeName;
            this.providerGuid = providerGuid;
            this.providerName = providerName;
            this.ParentThread = -1;
        }

        /// <summary>
        /// A standard way for events to are that certain addresses are addresses in code and ideally have
        /// symbolic information associated with them. 
        /// </summary>
        internal virtual protected void LogCodeAddresses(Action<TraceEvent, Address> callBack)
        {
        }

        /// <summary>
        /// Is this a Pre-Vista (classic) provider?
        /// </summary>
        internal protected bool ClassicProvider
        {
            [SecurityTreatAsSafe, SecurityCritical]
            get { return (eventRecord->EventHeader.Flags & TraceEventNativeMethods.EVENT_HEADER_FLAG_CLASSIC_HEADER) != 0; }
        }
        /// <summary>
        /// Returns the raw IntPtr pointer to the data blob associated with the event.  This is the way the
        /// subclasses of TraceEvent get at the data to display it in a reasonable fashion.
        /// </summary>
        protected IntPtr DataStart { get { return userData; } }
        /// <summary>
        /// Assume that  'Offset' bytes into the 'mofData' is a ascii 
        /// string.  Return the Offset after it is skipped.  This is intended
        /// to be used by subclasses trying to parse mofData 
        /// </summary>
        /// <param name="offset">the starting Offset</param>
        /// <returns>Offset just after the string</returns>
        protected int SkipAsciiString(int offset)
        {
            IntPtr mofData = DataStart;
            while (TraceEventRawReaders.ReadByte(mofData, offset) != 0)
                offset++;
            offset++;
            return offset;
        }
        /// <summary>
        /// Assume that  'offset' bytes into the 'mofData' is a unicode 
        /// string.  Return the Offset after it is skipped.  This is intended
        /// to be used by subclasses trying to parse mofData 
        /// </summary>
        /// <param name="offset">the starting Offset</param>
        /// <returns>Offset just after the string</returns>
        protected int SkipUnicodeString(int offset)
        {
            IntPtr mofData = DataStart;
            while (TraceEventRawReaders.ReadInt16(mofData, offset) != 0)
                offset += 2;
            offset += 2;
            return offset;
        }
        /// <summary>
        /// Assume that  'offset' bytes into the 'mofData' is SID.
        /// Return the Offset after it is skipped.  This is intended
        /// to be used by subclasses trying to parse mofData 
        /// </summary>
        /// <param name="offset">the starting Offset</param>
        /// <returns>Offset just after the string</returns>
        protected int SkipSID(int offset)
        {
            IntPtr mofData = DataStart;
            // This is a Security Token.  Either it is null, which takes 4 bytes, 
            // Otherwise it is an 8 byte structure (TOKEN_USER) followed by SID, which is variable
            // size (sigh) depending on the 2nd byte in the SID
            int sid = TraceEventRawReaders.ReadInt32(mofData, offset);
            if (sid == 0)
                return offset + 4;      // TODO confirm 
            else
            {
                int tokenSize = HostOffset(8, 2);
                int numAuthorities = TraceEventRawReaders.ReadByte(mofData, offset + (tokenSize + 1));
                return offset + tokenSize + 8 + 4 * numAuthorities;
            }
        }

        /// <summary>
        /// Trivial helper that allows you to get the Offset of a field independent of 32 vs 64 bit pointer
        /// size.
        /// </summary>
        /// <param name="offset">The Offset as it would be on a 32 bit system</param>
        /// <param name="numPointers">The number of pointer-sized fields that came before this field.
        /// </param>
        protected int HostOffset(int offset, int numPointers)
        {
            return offset + (PointerSize - 4) * numPointers;
        }
        /// <summary>
        /// Given an Offset to a null terminated ASCII string in an event blob, return the string that is
        /// held there.   
        /// </summary>
        protected string GetAsciiStringAt(int offset)
        {
            if (offset >= EventDataLength)
                throw new Exception("Reading past end of event");
            else
                return TraceEventRawReaders.ReadAsciiString(DataStart, offset);
        }

        /// <summary>
        /// Given an Offset to a fixed sized string at 'offset', whose buffer size is 'charCount'
        /// return the string value.  A null in the string will terminate the string before the
        /// end of the buffer. 
        /// </summary>        
        protected string GetFixedUnicodeStringAt(int charCount, int offset)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charCount; i++)
            {
                char c = (char)GetInt16At(offset + i * 2);
                if (c == 0)
                    break;
                // TODO review. 
                if ((c < ' ' || c > '~') && !char.IsWhiteSpace(c))
                {
                    Console.WriteLine("Warning: Found unprintable chars in string truncating to " + sb.ToString());
                    break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
        protected System.Net.IPAddress GetIPAddrV6At(int offset)
        {

            byte[] addrBytes = new byte[16];
            for (int i = 0; i < addrBytes.Length; i++)
                addrBytes[i] = TraceEventRawReaders.ReadByte(DataStart, offset + i);
            return new System.Net.IPAddress(addrBytes);
        }
        protected Guid GetGuidAt(int offset)
        {
            return new Guid(GetInt32At(offset),
                (short)GetInt16At(offset + 4),
                (short)GetInt16At(offset + 6),
                (byte)GetByteAt(offset + 8),
                (byte)GetByteAt(offset + 9),
                (byte)GetByteAt(offset + 10),
                (byte)GetByteAt(offset + 11),
                (byte)GetByteAt(offset + 12),
                (byte)GetByteAt(offset + 13),
                (byte)GetByteAt(offset + 14),
                (byte)GetByteAt(offset + 15));
        }
        /// <summary>
        /// Given an Offset to a null terminated unicode string in an event blob, return the string that is
        /// held there.   
        /// </summary>
        protected internal string GetUnicodeStringAt(int offset)
        {
            if (offset >= EventDataLength)
                throw new Exception("Reading past end of event");
            else
                return TraceEventRawReaders.ReadUnicodeString(DataStart, offset);
        }
        protected internal int GetByteAt(int offset)
        {
            return TraceEventRawReaders.ReadByte(DataStart, offset);
        }
        protected internal int GetInt16At(int offset)
        {
            return TraceEventRawReaders.ReadInt16(DataStart, offset);
        }
        protected internal int GetInt32At(int offset)
        {
            return TraceEventRawReaders.ReadInt32(DataStart, offset);
        }
        protected long GetInt64At(int offset)
        {
            return TraceEventRawReaders.ReadInt64(DataStart, offset);
        }
        /// <summary>
        /// Get something that is machine word sized for the provider that collected the data, but is an
        /// integer (and not an address)
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        protected long GetIntPtrAt(int offset)
        {
            Debug.Assert(PointerSize == 4 || PointerSize == 8);
            if (PointerSize == 4)
                return (long)GetInt32At(offset);
            else
                return GetInt64At(offset);
        }
        /// <summary>
        /// Gets something that is pointer sized for the provider that collected the data.  
        /// TODO rename to GetPointerAt()
        /// </summary>
        [CLSCompliant(false)]
        protected Address GetHostPointer(int offset)
        {
            Debug.Assert(PointerSize == 4 || PointerSize == 8);
            if (PointerSize == 4)
                return (Address)(uint)GetInt32At(offset);
            else
                return (Address)GetInt64At(offset);
        }

        protected float GetSingleAt(int offset)
        {
            return TraceEventRawReaders.ReadSingle(DataStart, offset);
        }

        protected double GetDoubleAt(int offset)
        {
            return TraceEventRawReaders.ReadDouble(DataStart, offset);
        }

        /// <summary>
        /// Prints a standard prefix for a event (includes the time of the event, the process ID and the
        /// thread ID.  
        /// </summary>
        internal protected StringBuilder Prefix(StringBuilder sb)
        {
            sb.Append("<Event MSec="); QuotePadLeft(sb, TimeStampRelativeMSec.ToString("f4"), 13);
            sb.Append(" PID="); QuotePadLeft(sb, ProcessID.ToString(), 6);
            sb.Append(" PName="); QuotePadLeft(sb, ProcessName, 10);
            sb.Append(" TID="); QuotePadLeft(sb, ThreadID.ToString(), 6);
            sb.Append(" EventName=\"").Append(EventName).Append('"');
            return sb;
        }

        // If non-null, when reading from ETL files, call this routine to fix poorly formed Event headers.  
        // Ideally this would not be needed, and is never used on ETLX files.
        protected internal Action FixupETLData;
        protected internal int ParentThread;
        #endregion
        #region Private
        // #TraceEventRecordLayout
        // Constants for picking apart the header of the event payload.  See  code:TraceEventNativeMethods.EVENT_TRACE
        [SecurityTreatAsSafe, SecurityCritical]
        ~TraceEvent()
        {
            // Most Data does not own its data, so this is usually a no-op. 

            if (myBuffer != IntPtr.Zero)
                TraceEventNativeMethods.FreeHGlobal(myBuffer);

        }
        private static void DumpBytes(byte[] bytes, TextWriter output, string indent)
        {
            int row = 0;
            while (row < bytes.Length)
            {
                output.Write(indent);
                output.Write("{0,4:x}:  ", row);
                for (int i = 0; i < 16; i++)
                {
                    if (i == 8)
                        output.Write("| ");
                    if (i + row < bytes.Length)
                        output.Write("{0,2:x} ", bytes[i + row]);
                    else
                        output.Write("   ");
                }
                output.Write("  ");
                for (int i = 0; i < 16; i++)
                {
                    if (i == 8)
                        output.Write(" ");
                    if (i + row >= bytes.Length)
                        break;
                    byte val = bytes[i + row];
                    if (32 <= val && val < 128)
                        output.Write((Char)val);
                    else
                        output.Write(".");
                }
                output.WriteLine();
                row += 16;
            }
        }
        unsafe private static void CopyBlob(IntPtr source, IntPtr destination, int byteCount)
        {
            Debug.Assert((int)source % 4 == 0);
            Debug.Assert((int)destination % 4 == 0);
            Debug.Assert(byteCount % 4 == 0);
            int* sourcePtr = (int*)source;
            int* destationPtr = (int*)destination;
            int intCount = byteCount >> 2;
            while (intCount > 0)
            {
                *destationPtr++ = *sourcePtr++;
                --intCount;
            }
        }

        internal static void QuotePadLeft(StringBuilder sb, string str, int totalSize)
        {
            int spaces = totalSize - 2 - str.Length;
            if (spaces > 0)
                sb.Append(' ', spaces);
            sb.Append('"').Append(str).Append('"');
        }
        private static string ToString(TraceEventOpcode opcode)
        {
            switch (opcode)
            {
                case TraceEventOpcode.Info: return "";
                case TraceEventOpcode.Start: return "Start";
                case TraceEventOpcode.Stop: return "End";
                case TraceEventOpcode.DataCollectionStart: return "DCStart";
                case TraceEventOpcode.DataCollectionStop: return "DCEnd";
                case TraceEventOpcode.Extension: return "Extension";
                case TraceEventOpcode.Reply: return "Reply";
                case TraceEventOpcode.Resume: return "Resume";
                case TraceEventOpcode.Suspend: return "Suspend";
                case TraceEventOpcode.Transfer: return "Send";
                default: return "Opcode" + ((int)opcode).ToString();
            }
        }

        /// <summary>
        ///  If the event data looks like a unicode string, then return it.  This is heuristic.  
        /// </summary>
        /// <returns></returns>
        internal unsafe string EventDataAsString()
        {
            if (EventDataLength % 2 != 0)
                return null;

            int numChars = EventDataLength / 2;
            if (numChars < 2)
                return null;

            char* ptr = (char*)DataStart;
            if (ptr[numChars - 1] != '\0')          // Needs to be null terminated. 
                return null;

            for (int i = 0; i < numChars - 1; i++)  // Rest need to be printable unicode chars.  
            {
                char c = ptr[i];
                if (!((' ' <= c && c <= '~') || c == '\n' || c == '\r'))
                    return null;
            }

            return TraceEventRawReaders.ReadUnicodeString(DataStart, 0);
        }

        /// <summary>
        /// Each code:TraceEvent items knows where it should Dispatch to.
        /// code:ETWTraceEventSource.Dispatch calls this function to go to the right placed. By default we
        /// do nothing. Typically a subclass just dispatches to another callback that passes itself to a
        /// type-specific event callback.
        /// </summary>
        protected internal virtual void Dispatch()
        {
            Debug.Assert(false, "Dispatching through base class!");
        }

        /// <summary>
        /// This is a DEBUG-ONLY routine that allows a routine to do consistancy checking in a debug build.  
        /// </summary>
        protected internal virtual void Validate()
        {
        }

        [Conditional("DEBUG")]
        protected internal void DebugValidate()
        {
            this.Validate();
        }

        // Note that you can't use the ExtendedData, UserData or UserContext fields, they are not set
        // properly in all cases.  
        internal TraceEventNativeMethods.EVENT_RECORD* eventRecord; // points at the record data itself.  (fixed size)
        internal IntPtr userData;                                   // The user payload

        /// <summary>
        /// TraceEvent knows where to dispatch to. To support many subscriptions to the same event we chain
        /// them.
        /// </summary>
        internal TraceEvent next;
        internal bool indexInDispatcherAsClassic;       // Did we put ourselves in the table using Classic conventions

        // These are constant over the TraceEvent's lifetime (after setup) (except for the UnhandledTraceEvent
        internal TraceEventID eventID;          // The ID you should switch on.  
        [CLSCompliant(false)]
        protected internal TraceEventOpcode opcode;
        [CLSCompliant(false)]
        protected internal string opcodeName;
        [CLSCompliant(false)]
        protected internal TraceEventTask task;
        [CLSCompliant(false)]
        protected internal string taskName;
        [CLSCompliant(false)]
        protected internal Guid taskGuid;
        [CLSCompliant(false)]
        protected internal Guid providerGuid;
        [CLSCompliant(false)]
        protected internal string providerName;
        internal TraceEventSource source;
        internal EventIndex eventIndex;               // something that uniquely identifies this event in the stream.  
        internal IntPtr myBuffer;                     // If the raw data is owned by this instance, this points at it.  Normally null.
        #endregion
    }

    /// <summary>
    /// Individual event providers can supply many different types of events.  These are distinguished from each
    /// other by a TraceEventID, which is just a 16 bit number.  Its meaning is provider-specific.  
    /// </summary>
    public enum TraceEventID : short { Illegal = -1, /* This is not mandated by Windows. */ }

    /// <summary>
    /// Providers can define different audiences or Channels for an event (eg Admin, Developer ...) Its
    /// meaning is provider 
    /// </summary>
    public enum TraceEventChannel : byte { Default = 0 }

    /// <summary>
    /// There are certain classes of events (like start and stop) which are common across a broad variety of
    /// event providers for which it is useful to treat uniformly (for example, determing the elapsed time
    /// between a start and stop event).  To facilitate this, event can have opcode which defines these
    /// common operations.  Below are the standard ones but proivders can define additional ones about 10.
    /// </summary>
    public enum TraceEventOpcode : byte
    {
        /// <summary>
        /// Generic opcode that does not have specific semantics associted with it. 
        /// </summary>
        Info = 0,
        /// <summary>
        /// The entity (process, thread, ...) is starting
        /// </summary>
        Start = 1,
        /// <summary>
        /// The entity (process, thread, ...) is stoping (ending)
        /// </summary>
        Stop = 2,
        /// <summary>
        /// The entity (process, thread, ...) did not terminate before data collection ended, so indicate
        /// this at data collection termination time.
        /// </summary>
        DataCollectionStart = 3,
        /// <summary>
        /// The entity (process, thread, ...) did not terminate before data collection ended, so indicate
        /// this at data collection termination time. This is mostly for 'flight recorder' scenarios where
        /// you only have the 'tail' of the data and would like to know about everything that existed. 
        /// </summary>
        DataCollectionStop = 4,
        /// <summary>
        /// TODO document these. 
        /// </summary>
        Extension = 5,
        Reply = 6,
        Resume = 7,
        Suspend = 8,
        Transfer = 9,
        // Recieve = 240,
    };

    /// <summary>
    /// VISTA ETW defines the concept of a Keyword, which is a 64 bit bitfield. Each bit in the bitfield
    /// represents some proider defined 'area' that is useful for filtering. When processing the events, it
    /// is then possible to filter based on whether various bits in the bitfield are set.  There are some
    /// standard keywords, but most are provider specific. 
    /// </summary>
    [Flags]
    public enum TraceEventKeyword : long
    {
        None = 0L,
        AuditFailure = 0x10000000000000L,
        AuditSuccess = 0x20000000000000L,
        CorrelationHint = 0x10000000000000L,
        EventLogClassic = 0x80000000000000L,
        Sqm = 0x8000000000000L,
        WdiContext = 0x2000000000000L,
        WdiDiagnostic = 0x4000000000000L
    }

    /// <summary>
    /// Tasks are groups of related events for a given provider (for example Process, or Thread, or Registry
    /// for the Kernel Provider).  They are defined by the provider.  
    /// </summary>
    public enum TraceEventTask : short { Default = 0 }

    /// <summary>
    /// code:EventIdex is a unsigned integer that is unique to a particular event. Like code:ProcessIndex and
    /// code:ThreadIndex, code:EventIndex is guarenteed to be unique over the whole log.  
    /// 
    /// The fact that EventIndex is a 32 bit number limits us to 4Gig events in a log.  Sample based profiling
    /// takes 1K samples per CPU per second.  Context switches and page faults can happen at about the same
    /// rate.  Thus 3K-6K is not uncommon and 10K /sec.  At that rate it will take 4E5 seconds == 111 hours
    /// == 4.6 days to exceed the limit.  Even at 100K / sec, it would be 11 hours of trace (Keep in mind
    /// we don't give StackTrace events IDs).   The file size would be greater than 100Gig which would make
    /// it REALLY painful to work with.  
    /// 
    /// We choose as the event ID simply the index in the log file of the event.  Thus the IDs are dense as
    /// they can be.  We don't however guarentee ordering, as we probably want to be able to add new
    /// events to the stream, and these will be addded at the end even if they occur elsewhere in the time
    /// stream. 
    /// </summary>
    public enum EventIndex : int { Invalid = -1 };

    /// <summary>
    /// code:TraceEventSource has two roles.  The first is the obvious one of providing some properties
    /// like 'SessionStartTime' for clients.  The other role is provide an anchor for code:TraceEventParser
    /// to 'hook' to so that events can be decoded.  code:ITraceParserServices is the API service for this
    /// second role.  It provides the methods that parsers use attach themselves to sources and register the
    /// fact that they undertand how to decode certain events.  
    /// </summary>
    public interface ITraceParserServices
    {
        /// <summary>
        /// RegisterEventTemplate is the mechanism a particular event payload description 'template' (a
        /// subclass of code:TraceEvent) is injected into the event processing stream. Once registered, an
        /// event is 'parsed' simply by setting the 'rawData' field in the event. It is up to the template
        /// then to take this raw data an present it in a useful way to the user (via properties). Note that
        /// parsing is thus 'lazy' in no processing of the raw data is not done at event dispatch time but
        /// only when the properties of an event are accessed.
        /// 
        /// Another important aspect is that templates are reused by code:TraceEventSource agressively. The
        /// expectation is that no memory needs to be allocated during a normal dispatch (in fact only one
        /// field in the code:TraceEvent is set).
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        void RegisterEventTemplate(TraceEvent template);
        /// <summary>
        /// It is expected that when a subclass of code:TraceEventParser is created, it calls this
        /// methodIndex on the source.  This allows the source to do any Parser-specific initialization.  
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        void RegisterParser(TraceEventParser parser);
        [SecurityTreatAsSafe, SecurityCritical]
        void RegisterUnhandledEvent(Action<TraceEvent> callback);
        /// <summary>
        /// Looks if any provider has registered an event with task with 'taskGuid'. Will return null if
        /// there is no registered event.
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical] 
        string TaskNameForGuid(Guid taskGuid);
        /// <summary>
        /// Looks if any provider has registered with the given GUID OR has registered any task that mathces
        /// the GUID. Will return null if there is no registered event.
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        string ProviderNameForGuid(Guid taskOrProviderGuid);
    }

    /// <summary>
    /// code:TraceEventParser Represents a class that knows how to decode particular set of events (typcially
    /// grouped by provider). It is the embodyment of all the type information that is typically stored in a
    /// ETW MOF file (Windows XP events) or an ETW manifest (Vista events). It is expected that a
    /// TraceEventParser can be generated completely mechanically from a MOF or ETW manifest (it really is
    /// just a decoder of that type information).
    /// 
    /// There is no static interface associated with a TraceEventParser, but there is a dynamic one. It is
    /// expected that TraceEventsParsers have a set of public event APIs of the form
    /// 
    ///     public event Action[SubclassOfTraceEvent] EventName
    /// 
    /// which allows users of the parser to subscribe to callbacks (in the case about called 'EventName'. The
    /// callback take a single argument (in this case SubclassOfTraceEvent) which is passed to the callback.
    /// 
    /// TraceEventParsers typically are constructed with a constructor that takes a code:TraceEventSource.
    /// The parser remembers the source, and when users subscribe to events on the code:TraceEventParser, the
    /// parser in turn calls code:TraceEventSource.RegisterEventTemplate with the correct subclass of
    /// code:TraceEvent that knows how to decode all the fields a a paraticular event.
    /// 
    /// Thus a code:TraceEventParser has built int support for a 'callback' model for subscribing to events.
    /// Parsers also support interacting with sources that support an iterator model. The code:TraceLog class
    /// is an example of this. In this model the user still 'registers' a parser with the source. When this
    /// registration happens, the source in turn calls back to the code:TraceEventParser.All event on the
    /// parser registering a 'null' callback. This causes the parser to register all events with the source
    /// with null callbacks. The callbacks are are never used (afer all, they are null), but the source
    /// needed the templates to be registered so the event payload data can be decoded.
    /// 
    /// * See code:ClrTraceEventParser
    /// * See code:KernelTraceEventParser
    /// </summary>
    public abstract class TraceEventParser
    {
        /// <summary>
        /// Subscribe to all the events this parser can parse.  Note that it will only add to
        /// events that are compatible with the delegate that is passed.  This is useful because
        /// it allows you to match all events that a certain delegate understands.  
        /// </summary>
        public virtual event Action<TraceEvent> All
        {
            add
            {
                AddToAllMatching(value);
            }
            remove
            {
                throw new Exception("Not supported");
            }
        }
        /// <summary>
        /// Subscribe to all events compatible with 'callback' 
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        public virtual void AddToAllMatching<T>(Action<T> callback)
        {
            // Use reflectin to add the callback to each of the events this class defines.  
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                // TODO tighten this up for wierd error conditions. 
                if (!method.Name.StartsWith("add_") || !method.IsSpecialName)
                    continue;
                if (method.Name == "add_All")
                    continue;
                // Convention: methods that end with 'Group' are groups of other events and
                // thus should not be considered part of an 'all' group.   
                if (method.Name.EndsWith("Group"))
                    continue;
                ParameterInfo[] paramInfos = method.GetParameters();
                if (paramInfos.Length != 1)
                    continue;
                Type paramType = paramInfos[0].ParameterType;
                if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof(Action<>))
                    continue;
                Type paramActionTypeParam = paramType.GetGenericArguments()[0];
                if (!typeof(T).IsAssignableFrom(paramActionTypeParam))
                    continue;

                // We have to make a new delegate because the type of the delegate argument
                // may not exactly match the type of the parameter of the method and delegate
                // casting logic is not smart enough at present. 

                object newDelegate = null;
                if (callback != null)
                    newDelegate = Delegate.CreateDelegate(paramType, callback.Target, callback.Method);
                method.Invoke(this, new object[] { newDelegate });
            }
        }
        #region protected
        [SecurityTreatAsSafe, SecurityCritical]
        [CLSCompliant(false)]
        protected TraceEventParser(TraceEventSource source)
        {
            this.source = source;
            this.stateKey = @"parsers\" + this.GetType().FullName;
            if (source != null)
                this.source.RegisterParser(this);
        }

        protected object StateObject
        {
            get
            {
                object ret;
                ((TraceEventSource)source).UserData.TryGetValue(stateKey, out ret);
                return ret;
            }
            set
            {
                ((TraceEventSource)source).UserData[stateKey] = value;
            }
        }

        void ForEachEvent(Action<MethodInfo> body)
        {

        }

        /// <summary>
        /// The source that this parser is connected to.  
        /// </summary>
        internal protected ITraceParserServices source;
        private string stateKey;
        #endregion
    }

    /// <summary>
    /// A code:TraceEventDispatcher is a code:TraceEventSource that supports a callback model for dispatching
    /// events. Like all code:TraceEventSource, it represents a list of code:TraceEvent however a
    /// code:TracEventDispatcher in addition has a hash table (from event GUI and EventID to code:TraceEvent)
    /// that is filled in when RegisterEventTemplate is called. Once registration is complete, calling
    /// code:TraceEventDispatcher.Process() will cause the callbacks to be triggered (in order)
    /// 
    /// See also code:ETWTraceEventSource a dispatcher tailored for reading ETL files.
    /// See also code:ETLXTraceEventSource a dispatcher tailored for reading ETLX files. 
    /// </summary>

    [CLSCompliant(false)]
    abstract unsafe public class TraceEventDispatcher : TraceEventSource
    {
        // Normally you subscribe to events using parsers that 'attach' themselves to the source. However
        // there are a couple of events that TraceEventDispatcher can handle directly.
        /// <summary>
        /// This event is called if no other hander has processed the event. Generally it is best not to use
        /// this if possible as it means that no filtering can be done by ETWTraceEventSource.
        /// </summary>
        public event Action<TraceEvent> UnhandledEvent
        {
            add
            {
                unhandledEventTemplate.Action += value;
            }
            remove
            {
                unhandledEventTemplate.Action -= value;
            }
        }
        /// <summary>
        /// This event is called on every event in the trace.  Generally you should be picking off just he
        /// events you want by using subclasses of code:ETWTraceEventSource like code:Kernel and code:CLR to
        /// subscribe to specific events, but sometimes you want to uniformly process every event.  
        /// 
        /// This is called AFTER any event-specific handlers.
        /// </summary>
        public event Action<TraceEvent> EveryEvent;
        /// <summary>
        /// Once a client has subscribed to the events of interest, calling Process actually causes
        /// the callbacks to happen.   
        /// </summary>
        /// <returns>false If StopProcesing was called</returns>
        public abstract bool Process();
        /// <summary>
        /// Calling this function in a callback when 'Process' is running will indicate that processing
        /// should be stopped immediately. 
        /// </summary>
        public virtual void StopProcessing()
        {
            stopProcessing = true;
        }

        #region private
        protected TraceEventDispatcher()
        {
            // Initialize our data structures. 
            unhandledEventTemplate = new UnhandledTraceEvent();
            unhandledEventTemplate.source = this;
            ReHash();       // Allocates the hash table
        }
        [SecurityTreatAsSafe, SecurityCritical]
        protected override void RegisterUnhandledEventImpl(Action<TraceEvent> callback)
        {
            UnhandledEvent += callback;
        }

        /// <summary>
        /// This is the routine that is called back when any event arrives.  Basically it looks up the GUID
        /// and the opcode associated with the event and finds right subclass of code:TraceEvent that
        /// knows how to decode the packet, and calls its virtual code:TraceEvent.Dispatch methodIndex.  Note
        /// that code:TraceEvent does NOT have a copy of hte data, but rather just a pointer to it. 
        /// This data is ONLY valid during the callback. 
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        internal protected void Dispatch(TraceEvent anEvent)
        {
#if DEBUG
            try
            {
#endif
            anEvent.Dispatch();
            if (anEvent.next != null)
            {
                TraceEvent nextEvent = anEvent;
                for (; ; )
                {
                    nextEvent = nextEvent.next;
                    if (nextEvent == null)
                        break;
                    nextEvent.eventRecord = anEvent.eventRecord;
                    nextEvent.userData = anEvent.userData;
                    nextEvent.eventIndex = anEvent.eventIndex;
                    nextEvent.Dispatch();
                    nextEvent.eventRecord = null;      // Technically not needed but detects user errors sooner. 
                }
            }
            if (EveryEvent != null)
            {
                if (unhandledEventTemplate == anEvent)
                    unhandledEventTemplate.PrepForCallback();
                EveryEvent(anEvent);
            }
            anEvent.eventRecord = null;      // Technically not needed but detects user errors sooner. 
#if DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: exception thrown during callback.  Will be swallowed!");
                Console.WriteLine("Exception: " + e.Message);
            }
#endif
        }

        /// <summary>
        /// Lookup up the event based on its ProviderID (GUID) and EventId (Classic use the TaskId and the
        /// Opcode field for lookup, but use these same fields (see code:ETWTraceEventSource.RawDispatchClassic)
        /// </summary>
        [SecurityTreatAsSafe, SecurityCritical]
        internal TraceEvent Lookup(TraceEventNativeMethods.EVENT_RECORD* eventRecord)
        {
            ushort eventID = eventRecord->EventHeader.Id;

            // Classic events use the opcode field as the discrimator instead of the event ID
            if ((eventRecord->EventHeader.Flags & TraceEventNativeMethods.EVENT_HEADER_FLAG_CLASSIC_HEADER) != 0)
            {
                Debug.Assert(eventID == 0);     // old style events don't use the eventIndex field.  
                eventID = eventRecord->EventHeader.Opcode;
            }

            // calculate the hash, and look it up in the table please note that this was hand
            // inlined, and is replicated in code:TraceEventDispatcher.Insert
            int* guidPtr = (int*)&eventRecord->EventHeader.ProviderId;   // This is the taskGuid for Classic events.  
            int hash = (*guidPtr + eventID * 9) & templatesLengthMask;
            for (; ; )
            {
                int* tableGuidPtr = (int*)&templatesInfo[hash].eventGuid;
                if (tableGuidPtr[0] == guidPtr[0] && tableGuidPtr[1] == guidPtr[1] &&
                    tableGuidPtr[2] == guidPtr[2] && tableGuidPtr[3] == guidPtr[3])
                {
                    TraceEvent ret = templates[hash];
                    if (eventID == templatesInfo[hash].eventID)
                    {
                        if (ret != null)
                        {
                            // Since provider and task guids can not overlap, we can only match if
                            // we are using the correct format.  
                            ret.eventRecord = eventRecord;
                            ret.userData = eventRecord->UserData;
                            ret.eventIndex = currentID;
                            currentID = currentID + 1;      // TODO overflow. 
                            // We expect to have matched either the opcode (for classic ETW), or
                            // eventID (for Vista)
                            Debug.Assert((ret.ClassicProvider && (byte)ret.opcode == eventRecord->EventHeader.Opcode) ||
                                         (!ret.ClassicProvider && (ushort)ret.eventID == eventRecord->EventHeader.Id));
                            Debug.Assert((ret.ClassicProvider && ret.taskGuid == eventRecord->EventHeader.ProviderId) ||
                                         (!ret.ClassicProvider && ret.ProviderGuid == eventRecord->EventHeader.ProviderId));
                            return ret;
                        }
                    }
                }
                if (templates[hash] == null)
                    break;

                // Console.Write("Colision " + *asGuid + " opcode " + opcode + " and " + templatesInfo[hash].providerGuid + " opcode " + templatesInfo[hash].opcode);
                hash = (hash + (int)eventID * 2 + 1) & templatesLengthMask;
            }

            unhandledEventTemplate.eventRecord = eventRecord;
            unhandledEventTemplate.userData = eventRecord->UserData;

            unhandledEventTemplate.eventIndex = currentID;
            currentID = currentID + 1;      // TODO overflow.
            unhandledEventTemplate.opcode = unchecked((TraceEventOpcode)(-1));      // Marks it as unhandledEvent;
#if DEBUG
            // Set some illegal values to highlight missed PrepForCallback() calls
            unhandledEventTemplate.task = unchecked((TraceEventTask)(-1));
            unhandledEventTemplate.providerName = "ERRORPROVIDER";
            unhandledEventTemplate.taskName = "ERRORTASK";
            unhandledEventTemplate.opcodeName = "ERROROPCODE";
            unhandledEventTemplate.eventID = TraceEventID.Illegal;
            unhandledEventTemplate.taskGuid = Guid.Empty;
#endif
            return unhandledEventTemplate;
        }

        [SecurityTreatAsSafe, SecurityCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (templatesInfo != null)
            {

                TraceEventNativeMethods.FreeHGlobal((IntPtr)templatesInfo);

                templatesInfo = null;
            }
        }
        [SecurityTreatAsSafe, SecurityCritical]
        ~TraceEventDispatcher()
        {
            Dispose(false);
        }

        [SecurityTreatAsSafe, SecurityCritical]
        private void ReHash()
        {
            TemplateEntry* oldTemplatesInfo = templatesInfo;
            TraceEvent[] oldTemplates = templates;

            // BatchCount must be a power of 2 because we use a mask to do modulus
            int newLength = templates != null ? (templates.Length * 2) : 256;
            templatesLengthMask = newLength - 1;

            templates = new TraceEvent[newLength];

            templatesInfo = (TemplateEntry*)TraceEventNativeMethods.AllocHGlobal(sizeof(TemplateEntry) * newLength);

            for (int i = 0; i < newLength; i++)
                templatesInfo[i].eventGuid = Guid.Empty;

            if (oldTemplates != null)
            {
                for (int i = 0; i < oldTemplates.Length; i++)
                {
                    if (oldTemplates[i] != null)
                        Insert(oldTemplates[i]);
                }

                TraceEventNativeMethods.FreeHGlobal((IntPtr)oldTemplatesInfo);

            }
        }

        /// <summary>
        /// Inserts 'template' into the hash table, using 'providerGuid' and and 'eventID' as the key. 
        /// For Vista ETW events 'providerGuid' must match the provider GUID and the 'eventID' the ID filed.
        /// For PreVist ETW events 'providerGuid must match the task GUID the 'eventID' is the Opcode
        /// </summary>
        private void Insert(TraceEvent template)
        {
            if (numTemplates * 4 > templates.Length * 3)    // Are we over 3/4 full?
                ReHash();

            // We need the size to be a power of two since we use a mask to do the modulus. 
            Debug.Assert(((templates.Length - 1) & templates.Length) == 0, "array length must be a power of 2");

            // Which conventions are we using?
            ushort eventID = (ushort)template.eventID;
            Guid eventGuid = template.providerGuid;
            if (template.indexInDispatcherAsClassic)
            {
                eventID = (ushort)template.Opcode;
                eventGuid = template.taskGuid;
            }
            Debug.Assert(eventGuid != Guid.Empty);

            // compute the hash, and look it up in the table please note that this was
            // hand inlined, and is replicated in code:TraceEventDispatcher.Lookup
            int* guidPtr = (int*)&eventGuid;
            int hash = (*guidPtr + (int)eventID * 9) & templatesLengthMask;
            for (; ; )
            {
                int* tableGuidPtr = (int*)&templatesInfo[hash].eventGuid;
                if (tableGuidPtr[0] == guidPtr[0] && tableGuidPtr[1] == guidPtr[1] &&
                    tableGuidPtr[2] == guidPtr[2] && tableGuidPtr[3] == guidPtr[3])
                {
                    if (eventID == templatesInfo[hash].eventID)
                    {
                        TraceEvent existingTemplate = templates[hash];
                        if (existingTemplate != null)
                        {
                            if (existingTemplate is DynamicTraceEventData)
                            {
                                // We treat Dynamic templates specially, allowing this one to take precidence.
                                // TODO think about this more carefully
                                template.next = existingTemplate.next;
                                templates[hash] = template;
                                return;
                            }
                            // Goto the end of the list (preserve order of adding events).
                            while (existingTemplate.next != null)
                            {
                                existingTemplate = existingTemplate.next;
                            }
                            existingTemplate.next = template;
                            template.next = null;
                            return;
                        }
                    }
                }
                if (templates[hash] == null)
                    break;
                hash = (hash + (int)eventID * 2 + 1) & templatesLengthMask;
            }
            templates[hash] = template;
            TemplateEntry* entry = &templatesInfo[hash];
            entry->eventID = eventID;
            entry->eventGuid = eventGuid;
            numTemplates++;
        }

        #region TemplateHashTable
        struct TemplateEntry
        {
            public Guid eventGuid;
            public ushort eventID; // Event ID for Vista events, Opcode for Classic events.  
        }

        TemplateEntry* templatesInfo;   // unmanaged array, this is the hash able.  

        TraceEvent[] templates;         // Logically a field in code:TemplateEntry 
        private struct NamesEntry
        {
            public NamesEntry(string taskName, string providerName) { this.taskName = taskName; this.providerName = providerName; }
            public string taskName;
            public string providerName;
        }
        Dictionary<Guid, NamesEntry> guidToNames; // Used to find Provider and Task names from their Guids.  Only rarely used
        int templatesLengthMask;
        int numTemplates;
        protected UnhandledTraceEvent unhandledEventTemplate;
        protected IEnumerable<TraceEvent> Templates
        {
            get
            {
                for (int i = 0; i < templates.Length; i++)
                {
                    if (templates[i] != null)
                        yield return templates[i];
                }
            }
        }
        #endregion

        #region ITraceParserServices Members
        [SecurityTreatAsSafe, SecurityCritical]
        protected override void RegisterEventTemplateImpl(TraceEvent template)
        {
            if (template.source == null)
                template.source = this;
            Debug.Assert(template.eventRecord == null);

            // Use the old style exclusive if we are using old ETW apis, or the provider does not
            // support it (This currently includes the Kernel Events)
            Debug.Assert(!(template.ProviderGuid == KernelTraceEventParser.ProviderGuid && template.eventID != TraceEventID.Illegal));
            if (useClassicETW || template.eventID == TraceEventID.Illegal)
            {
                template.indexInDispatcherAsClassic = true;
                Insert(template);
            }
            else
            {
                template.indexInDispatcherAsClassic = false;
                Insert(template);

                // If the provider supports both pre-vista events (Guid non-empty), (The CLR does this)
                // Because the template is chained, we need to clone the template to insert it
                // again.  
                if (template.taskGuid != Guid.Empty)
                {
                    template = template.Clone();
                    template.indexInDispatcherAsClassic = true;
                    Insert(template);
                }
            }
        }
        [SecurityTreatAsSafe, SecurityCritical]
        protected override void RegisterParserImpl(TraceEventParser parser)
        {
            if (_Kernel == null)
                _Kernel = parser as KernelTraceEventParser;
            if (_CLR == null)
                _CLR = parser as ClrTraceEventParser;
            if (_Dynamic == null)
                _Dynamic = parser as DynamicTraceEventParser;
        }

        [SecurityTreatAsSafe, SecurityCritical]
        protected override string TaskNameForGuidImpl(Guid guid)
        {
            NamesEntry entry;
            LookupGuid(guid, out entry);
            return entry.taskName;
        }
        protected override string ProviderNameForGuidImpl(Guid taskOrProviderName)
        {
            NamesEntry entry;
            LookupGuid(taskOrProviderName, out entry);
            return entry.providerName;
        }

        private void LookupGuid(Guid guid, out NamesEntry ret)
        {
            ret.providerName = null;
            ret.taskName = null;
            if (guidToNames == null)
            {
                if (templates == null)
                    return;
                // Populate the map
                guidToNames = new Dictionary<Guid, NamesEntry>();
                foreach (TraceEvent template in templates)
                {
                    if (template != null)
                    {
                        if (template.providerName != null && template.providerGuid != Guid.Empty && !guidToNames.ContainsKey(template.providerGuid))
                            guidToNames[template.providerGuid] = new NamesEntry(null, template.providerName);
                        if (template.taskName != null && template.taskGuid != Guid.Empty)
                            guidToNames[template.taskGuid] = new NamesEntry(template.taskName, template.providerName);
                    }
                }
            }
            guidToNames.TryGetValue(guid, out ret);
        }
        #endregion
        [CLSCompliant(false)]
        protected bool stopProcessing;
        internal EventIndex currentID;
        #endregion
    }

    // Generic events for very simple cases (no payload, one value)
    /// <summary>
    /// When the event has no interesting data associated with it, you can use this shared event current
    /// rather than making an event-specific class.
    /// </summary>
    public sealed class EmptyTraceData : TraceEvent
    {
        public EmptyTraceData(Action<EmptyTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        #region Private
        public override StringBuilder ToXml(StringBuilder sb)
        {
            return Prefix(sb).Append("/>");
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
            Debug.Assert(false);
            return null;
        }

        private event Action<EmptyTraceData> Action;
        protected internal override void Dispatch()
        {
            Action(this);
        }
        #endregion
    }

    /// <summary>
    /// When the event has just a single int value associated with it, you can use this shared event current
    /// rather than making an event-specific class.
    /// </summary>
    public sealed class Int32TraceData : TraceEvent
    {
        public int Value { get { return GetInt32At(0); } }

        public Int32TraceData(Action<Int32TraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        #region Private
        public override StringBuilder ToXml(StringBuilder sb)
        {
            return Prefix(sb).XmlAttrib("Value", Value).Append("/>");
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Value" };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            Debug.Assert(index < 1);
            switch (index)
            {
                case 0:
                    return Value;
                default:
                    return null;
            }
        }

        private event Action<Int32TraceData> Action;
        protected internal override void Dispatch()
        {
            Action(this);
        }
        #endregion
    }

    /// <summary>
    /// When the event has just a single int value associated with it, you can use this shared event current
    /// rather than making an event-specific class.
    /// </summary>
    public sealed class Int64TraceData : TraceEvent
    {
        public long Value { get { return GetInt64At(0); } }

        public Int64TraceData(Action<Int64TraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
        }
        #region Private
        public override StringBuilder ToXml(StringBuilder sb)
        {
            return Prefix(sb).XmlAttrib("Value", Value).Append("/>");
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Value" };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            Debug.Assert(index < 1);
            switch (index)
            {
                case 0:
                    return Value;
                default:
                    return null;
            }
        }

        private event Action<Int64TraceData> Action;
        protected internal override void Dispatch()
        {
            Action(this);
        }
        #endregion
    }

    /// <summary>
    /// When the event has just a single string value associated with it, you can use this shared event
    /// template rather than making an event-specific class.
    /// </summary>
    public sealed class StringTraceData : TraceEvent
    {
        public string Value { get { if (isUnicode) return GetUnicodeStringAt(0); else return GetAsciiStringAt(0); } }

        public StringTraceData(Action<StringTraceData> action, int eventID, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName, bool isUnicode)
            : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
        {
            this.Action = action;
            this.isUnicode = isUnicode;
        }
        #region Private
        public override StringBuilder ToXml(StringBuilder sb)
        {
            return Prefix(sb).XmlAttrib("Value", Value).Append("/>");
        }
        public override string[] PayloadNames
        {
            get
            {
                if (payloadNames == null)
                    payloadNames = new string[] { "Value" };
                return payloadNames;
            }
        }
        public override object PayloadValue(int index)
        {
            Debug.Assert(index < 1);
            switch (index)
            {
                case 0:
                    return Value;
                default:
                    return null;
            }
        }

        private event Action<StringTraceData> Action;
        protected internal override void Dispatch()
        {
            Action(this);
        }
        bool isUnicode;
        #endregion
    }

    public unsafe sealed class UnhandledTraceEvent : TraceEvent
    {
        public static StringBuilder ToXmlAsUnknown(TraceEvent anEvent, StringBuilder sb)
        {
            sb.Append("<Event MSec="); QuotePadLeft(sb, anEvent.TimeStampRelativeMSec.ToString("f4"), 13);
            sb.Append(" PID="); QuotePadLeft(sb, anEvent.ProcessID.ToString(), 6);
            sb.Append(" PName="); QuotePadLeft(sb, anEvent.ProcessName, 10);
            sb.Append(" TID="); QuotePadLeft(sb, anEvent.ThreadID.ToString(), 6);
            sb.XmlAttrib("IsClassic", anEvent.ClassicProvider);
            if (anEvent.ClassicProvider)
            {
                var providerName = anEvent.ProviderName;
                if (providerName != "UnknownProvider")
                    sb.XmlAttrib("ProviderName", providerName);
                var taskName = anEvent.TaskName;
                sb.XmlAttrib("TaskName", taskName);
                if (!taskName.StartsWith("Task"))
                    sb.XmlAttrib("TaskGuid", anEvent.taskGuid);
            }
            else
            {
                var providerName = anEvent.ProviderName;
                sb.XmlAttrib("ProviderName", providerName);
                if (!providerName.StartsWith("Provider("))
                    sb.XmlAttrib("ProviderGuid", anEvent.providerGuid);
                sb.XmlAttrib("eventID", (int)anEvent.ID);

                var taskName = anEvent.TaskName;
                sb.XmlAttrib("TaskName", taskName);
                if (!taskName.StartsWith("Task"))
                    sb.XmlAttrib("TaskNum", (int)anEvent.Task);
            }
            sb.XmlAttrib("OpcodeNum", (int)anEvent.Opcode);
            sb.XmlAttrib("Version", anEvent.Version);
            sb.XmlAttrib("Level", (int)anEvent.Level);
            sb.XmlAttrib("PointerSize", anEvent.PointerSize);
            sb.XmlAttrib("EventDataLength", anEvent.EventDataLength);
            string dataAsString = anEvent.EventDataAsString();
            if (dataAsString != null)
                sb.XmlAttrib("EventDataString", dataAsString);

            // Is this a manifest event?
            if (anEvent.Opcode == (TraceEventOpcode)0xFE && (anEvent.ID == 0 || (byte)anEvent.ID == 0xFE))
            {
                sb.AppendLine(">");
                try
                {
                    byte[] rawData = anEvent.EventData();
                    if (rawData.Length > sizeof(ManifestEnvelope) && (ManifestEnvelope.ManifestFormats)rawData[0] == ManifestEnvelope.ManifestFormats.SimpleXmlFormat)
                    {
                        string str = System.Text.Encoding.UTF8.GetString(rawData, sizeof(ManifestEnvelope), rawData.Length - sizeof(ManifestEnvelope));
                        if (str.StartsWith("<provider"))
                            sb.Append(str);
                    }
                }
                catch (Exception) { }
                sb.Append("</Event>");
            }
            else
                sb.Append("/>");
            return sb;
        }

        #region private
        public override StringBuilder ToXml(StringBuilder sb)
        {
            return ToXmlAsUnknown(this, sb);
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
            Debug.Assert(false);
            return null;
        }

        internal event Action<TraceEvent> Action;
        internal UnhandledTraceEvent() : base(0, 0, null, Guid.Empty, 0, null, Guid.Empty, null) { }

        protected internal override void Dispatch()
        {
            if (Action != null)
            {
                PrepForCallback();
                Action(this);
            }
        }

        public override string ToString()
        {
            // This is only needed so that when we print when debugging we get sane results.  
            if (eventID == TraceEventID.Illegal)
                PrepForCallback();
            return base.ToString();
        }

        /// <summary>
        /// There is some work needed to prepare the generic unhandledTraceEvent that we defer
        /// late (since we often don't care about unhandled events)  This is 
        /// </summary>
        internal void PrepForCallback()
        {
            // Could not find the event, populate the shared 'unhandled event' information.   
            if (ClassicProvider)
            {
                providerGuid = Guid.Empty;
                taskGuid = eventRecord->EventHeader.ProviderId;
                eventID = 0;
            }
            else
            {
                taskGuid = Guid.Empty;
                providerGuid = eventRecord->EventHeader.ProviderId;
                eventID = (TraceEventID)eventRecord->EventHeader.Id;
            }
            opcode = (TraceEventOpcode)eventRecord->EventHeader.Opcode;
            task = (TraceEventTask)eventRecord->EventHeader.Task;
            taskName = null;        // Null them out so that they get repopulated with this data's
            providerName = null;
            opcodeName = null;
        }
        #endregion
    }

    #region Private Classes

    internal sealed class TraceEventRawReaders
    {
        unsafe internal static IntPtr Add(IntPtr pointer, int offset)
        {
            return (IntPtr)(((byte*)pointer) + offset);
        }
        unsafe internal static Guid ReadGuid(IntPtr pointer, int offset)
        {
            return *((Guid*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static double ReadDouble(IntPtr pointer, int offset)
        {
            return *((double*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static float ReadSingle(IntPtr pointer, int offset)
        {
            return *((float*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static long ReadInt64(IntPtr pointer, int offset)
        {
            return *((long*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static int ReadInt32(IntPtr pointer, int offset)
        {
            return *((int*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static short ReadInt16(IntPtr pointer, int offset)
        {
            return *((short*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static IntPtr ReadIntPtr(IntPtr pointer, int offset)
        {
            return *((IntPtr*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static byte ReadByte(IntPtr pointer, int offset)
        {
            return *((byte*)((byte*)pointer.ToPointer() + offset));
        }
        unsafe internal static string ReadUnicodeString(IntPtr pointer, int offset)
        {
            // TODO in debug mode, insure string are inside buffer. 
            string str = new string((char*)((byte*)pointer.ToPointer() + offset));

            // TODO investigate this why does this happen?
#if true
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if ((c < ' ' || c > '~') && !char.IsWhiteSpace(c))
                {
                    str = str.Substring(0, i);
                    Console.WriteLine("Warning: truncating " + (str.Length - i).ToString() + " non-ascii name characters: resulting string: " + str);
                    break;
                }
            }
#endif
            return str;
        }
        unsafe internal static string ReadAsciiString(IntPtr pointer, int offset)
        {
            // TODO in debug mode, insure string are inside buffer. 
            string ret = Marshal.PtrToStringAnsi((IntPtr)(pointer.ToInt64() + offset));
            return ret;
        }
    }

    public static class StringBuilderExtensions
    {
        public static StringBuilder XmlAttrib(this StringBuilder sb, string attribName, string value)
        {
            return sb.XmlAttribPrefix(attribName).Append(XmlUtilities.XmlEscape(value, false)).Append('"');
        }
        public static StringBuilder XmlAttrib(this StringBuilder sb, string attribName, int value)
        {
            return sb.XmlAttribPrefix(attribName).Append(value.ToString()).Append('"');
        }
        public static StringBuilder XmlAttrib(this StringBuilder sb, string attribName, long value)
        {
            return sb.XmlAttribPrefix(attribName).Append(value.ToString()).Append('"');
        }
        [CLSCompliant(false)]
        public static StringBuilder XmlAttribHex(this StringBuilder sb, string attribName, ulong value)
        {
            sb.XmlAttribPrefix(attribName);
            sb.Append('0').Append('x');
            uint intValue = (uint)(value >> 32);
            for (int i = 0; i < 2; i++)
            {
                if (i != 0 || intValue != 0)
                {
                    for (int j = 28; j >= 0; j -= 4)
                    {
                        uint digit = (intValue >> j) & 0xF;
                        uint charDigit = ('0' + digit);
                        if (charDigit > '9')
                            charDigit += ('A' - '9' - 1);
                        sb.Append((char)charDigit);
                    }
                }
                intValue = (uint)value;
            }
            sb.Append('"');
            return sb;
        }
        [CLSCompliant(false)]
        public static StringBuilder XmlAttribHex(this StringBuilder sb, string attribName, Address value)
        {
            return sb.XmlAttribHex(attribName, (ulong)value);
        }
        public static StringBuilder XmlAttribHex(this StringBuilder sb, string attribName, long value)
        {
            return sb.XmlAttribHex(attribName, (ulong)value);
        }
        [CLSCompliant(false)]
        public static StringBuilder XmlAttribHex(this StringBuilder sb, string attribName, uint value)
        {
            return sb.XmlAttribHex(attribName, (ulong)value);
        }
        public static StringBuilder XmlAttribHex(this StringBuilder sb, string attribName, int value)
        {
            return sb.XmlAttribHex(attribName, (ulong)value);
        }
        public static StringBuilder XmlAttrib(this StringBuilder sb, string attribName, object value)
        {
            if (value is Address)
                return sb.XmlAttribHex(attribName, (Address)value);
            return sb.XmlAttrib(attribName, value.ToString());
        }

        #region private
        private static StringBuilder XmlAttribPrefix(this StringBuilder sb, string attribName)
        {
            sb.Append(' ').Append(attribName).Append('=').Append('"');
            return sb;
        }
        #endregion
    }
    #endregion
}

