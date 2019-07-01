// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security;

// code:System.Diagnostics.ETWTraceEventSource defintion.
namespace Microsoft.Test.EventTracing
{
    /// <summary>
    /// A code:ETWTraceEventSource represents the stream of events that was collected from a
    /// code:TraceEventSession (eg the ETL moduleFile, or the live session event stream). Like all
    /// code:TraceEventSource, it logically represents a stream of code:TraceEvent s. Like all
    /// code:TraceEventDispather s it supports a callback model where Parsers attach themselves to this
    /// soures, and user callbacks defined on the parsers are called when the 'Process' methodIndex is called.
    /// 
    /// * See also code:TraceEventDispatcher
    /// * See also code:TraceEvent
    /// * See also code:#ETWTraceEventSourceInternals
    /// * See also code:#ETWTraceEventSourceFields
    /// </summary>    
    [CLSCompliant(false)]
    public unsafe sealed class ETWTraceEventSource : TraceEventDispatcher, IDisposable
    {
        /// <summary>
        /// Open a ETW event trace moduleFile (ETL moduleFile) for processing.  
        /// </summary>
        /// <param name="fileName">The ETL data moduleFile to open</param>
        public ETWTraceEventSource(string fileName)
            : this(fileName, TraceEventSourceType.UserAndKernelFile)
        {
        }
        /// <summary>
        /// Open a ETW event source for processing.  This can either be a moduleFile or a real time ETW session
        /// </summary>
        /// <param name="fileOrSessionName">
        /// If type == ModuleFile this is the name of the moduleFile to open.
        /// If type == Session this is the name of real time sessing to open.</param>
        /// <param name="type"></param>
        [SecurityTreatAsSafe, SecurityCritical]
        public ETWTraceEventSource(string fileOrSessionName, TraceEventSourceType type)
        {
            long now = DateTime.Now.ToFileTime() - 100000;     // subtract 10ms to avoid negative times. 

            primaryLogFile = new TraceEventNativeMethods.EVENT_TRACE_LOGFILEW();
            primaryLogFile.BufferCallback = this.TraceEventBufferCallback;

            useClassicETW = Environment.OSVersion.Version.Major < 6;
            if (useClassicETW)
            {
                IntPtr mem = TraceEventNativeMethods.AllocHGlobal(sizeof(TraceEventNativeMethods.EVENT_RECORD));      
                TraceEventNativeMethods.ZeroMemory(mem, (uint)sizeof(TraceEventNativeMethods.EVENT_RECORD));
                convertedHeader = (TraceEventNativeMethods.EVENT_RECORD*)mem;
                primaryLogFile.EventCallback = RawDispatchClassic;

            }
            else 
            {
                primaryLogFile.LogFileMode = TraceEventNativeMethods.PROCESS_TRACE_MODE_EVENT_RECORD;
                primaryLogFile.EventCallback = RawDispatch;
            }

            if (type == TraceEventSourceType.Session)
            {
                primaryLogFile.LoggerName = fileOrSessionName;
                primaryLogFile.LogFileMode |= TraceEventNativeMethods.EVENT_TRACE_REAL_TIME_MODE;
            }
            else
            {
                if (type == TraceEventSourceType.UserAndKernelFile)
                {
                    // See if we have also have kernel log moduleFile. 
                    if (fileOrSessionName.Length > 4 && string.Compare(fileOrSessionName, fileOrSessionName.Length - 4, ".etl", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string kernelFileName = fileOrSessionName.Substring(0, fileOrSessionName.Length - 4) + ".kernel.etl";
                        if (File.Exists(kernelFileName))
                        {
                            if (File.Exists(fileOrSessionName))
                            {
                                handles = new ulong[2];
                                handles[0] = TraceEventNativeMethods.INVALID_HANDLE_VALUE;

                                kernelModeLogFile = new TraceEventNativeMethods.EVENT_TRACE_LOGFILEW();
                                kernelModeLogFile.BufferCallback = primaryLogFile.BufferCallback;
                                kernelModeLogFile.EventCallback = primaryLogFile.EventCallback;
                                kernelModeLogFile.LogFileName = kernelFileName;
                                kernelModeLogFile.LogFileMode = primaryLogFile.LogFileMode;

                                handles[1] = TraceEventNativeMethods.OpenTrace(ref kernelModeLogFile);

                                if (TraceEventNativeMethods.INVALID_HANDLE_VALUE == handles[1])
                                    Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRForLastWin32Error());

                            }
                            else
                            {
                                // we have ONLY a *.kernel.etl moduleFile, treat it as the primary moduleFile. 
                                fileOrSessionName = kernelFileName;
                            }
                        }
                    }
                    if (!File.Exists(fileOrSessionName))
                        throw new FileNotFoundException("Unable to find the file " + fileOrSessionName);
                }
                primaryLogFile.LogFileName = fileOrSessionName;
            }

            // Open the main data source
            if (handles == null)
                handles = new ulong[1];
            
            handles[0] = TraceEventNativeMethods.OpenTrace(ref primaryLogFile);

            if (TraceEventNativeMethods.INVALID_HANDLE_VALUE == handles[0])
                Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRForLastWin32Error());

            // Session offset time is the minimum of all times.  
            sessionStartTime100ns = primaryLogFile.LogfileHeader.StartTime;
            if (handles.Length == 2 && kernelModeLogFile.LogfileHeader.StartTime < sessionStartTime100ns)
                sessionStartTime100ns = kernelModeLogFile.LogfileHeader.StartTime;

            // Real time providers don't set this to something useful
            if (sessionStartTime100ns == 0)
                sessionStartTime100ns = now;

            sessionEndTime100ns = primaryLogFile.LogfileHeader.EndTime;
            if (handles.Length == 2 && sessionEndTime100ns < kernelModeLogFile.LogfileHeader.EndTime)
                sessionEndTime100ns = kernelModeLogFile.LogfileHeader.EndTime;

            if (sessionEndTime100ns == 0)
                sessionEndTime100ns = sessionStartTime100ns;

            // TODO remove when we do per-event stuff.  
            pointerSize = (int)primaryLogFile.LogfileHeader.PointerSize;
            if (handles.Length == 2)
                pointerSize = (int) kernelModeLogFile.LogfileHeader.PointerSize;

            if (pointerSize == 0)
            {
                pointerSize = sizeof(IntPtr);
                Debug.Assert((primaryLogFile.LogFileMode & TraceEventNativeMethods.EVENT_TRACE_REAL_TIME_MODE) != 0);
            }
            Debug.Assert(pointerSize == 4 || pointerSize == 8);

            eventsLost = (int)primaryLogFile.LogfileHeader.EventsLost; 
            cpuSpeedMHz = (int)primaryLogFile.LogfileHeader.CpuSpeedInMHz;
            numberOfProcessors = (int)primaryLogFile.LogfileHeader.NumberOfProcessors;

            // Logic for looking up process names
            processNameForID = new Dictionary<int, string>();
            Kernel.ProcessStartGroup += delegate(ProcessTraceData data) {
                // Get just the file name without the extension.  Can't use the 'Path' class because
                // it tests to make certain it does not have illegal chars etc.  Since KernelImageFileName
                // is not a true user mode path, we can get failures. 
                string path = data.KernelImageFileName;
                int startIdx = path.LastIndexOf('\\');
                if (0 <= startIdx)
                    startIdx++;
                else
                    startIdx = 0;
                int endIdx = path.LastIndexOf('.', startIdx);
                if (endIdx < 0)
                    endIdx = path.Length;
                processNameForID[data.ProcessID] = path.Substring(startIdx, endIdx - startIdx);
            };
        }
        
        // Process is called after all desired subscriptions have been registered.  
        /// <summary>
        /// Processes all the events in the data soruce, issuing callbacks that were subscribed to.  See
        /// code:#Introduction for more
        /// </summary>
        /// <returns>false If StopProcesing was called</returns>
        [SecurityTreatAsSafe, SecurityCritical]
        public override bool Process()
        {
            if (processTraceCalled)
                Reset();
            processTraceCalled = true;
            stopProcessing = false;
            int dwErr = TraceEventNativeMethods.ProcessTrace(handles, (uint)handles.Length, (IntPtr)0, (IntPtr)0);
            Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(dwErr));
            return !stopProcessing;
        }

        /// <summary>
        /// Closes the ETL moduleFile or detaches from the session.  
        /// </summary>  
        
        public void Close()
        {
            Dispose(true);
        }
        /// <summary>
        /// The log moduleFile that is being processed (if present)
        /// TODO: what does this do for Real time sessions?
        /// </summary>
        public string LogFileName { get { return primaryLogFile.LogFileName; } }
        /// <summary>
        /// The name of the session that generated the data. 
        /// </summary>
        public string SessionName { get { return primaryLogFile.LoggerName; } }
        /// <summary>
        /// Returns true if the code:Process can be called mulitple times (if the Data source is from a
        /// moduleFile, not a real time stream.
        /// </summary>
        public bool CanReset { get { return (primaryLogFile.LogFileMode & TraceEventNativeMethods.EVENT_TRACE_REAL_TIME_MODE) == 0; } }

        [SecurityTreatAsSafe, SecurityCritical]
        public override void Dispose()
        {
            Dispose(true);
        }

        #region Private
        internal bool HasKernelEvents { get { return kernelModeLogFile.LogFileName != null; } }

        // #ETWTraceEventSourceInternals
        // 
        // ETWTraceEventSource is a wrapper around the Windows API code:TraceEventNativeMethods.OpenTrace
        // methodIndex (see http://msdn2.microsoft.com/en-us/library/aa364089.aspx) We set it up so that we call
        // back to code:ETWTraceEventSource.Dispatch which is the heart of the event callback logic.
        [SecurityTreatAsSafe, SecurityCritical]
        private void RawDispatchClassic(TraceEventNativeMethods.EVENT_RECORD* eventData)
        {
            // TODO not really a EVENT_RECORD on input, but it is a pain to be type-correct.  
            TraceEventNativeMethods.EVENT_TRACE* oldStyleHeader = (TraceEventNativeMethods.EVENT_TRACE*) eventData;
            eventData = convertedHeader;

            eventData->EventHeader.Size = (ushort) sizeof(TraceEventNativeMethods.EVENT_TRACE_HEADER);
            // HeaderType
            eventData->EventHeader.Flags = TraceEventNativeMethods.EVENT_HEADER_FLAG_CLASSIC_HEADER;

            // TODO Figure out if there is a marker that is used in the WOW for the classic providers 
            // right now I assume they are all the same as the machine.  
            if (pointerSize == 8)
                eventData->EventHeader.Flags |= TraceEventNativeMethods.EVENT_HEADER_FLAG_64_BIT_HEADER;
            else
                eventData->EventHeader.Flags |= TraceEventNativeMethods.EVENT_HEADER_FLAG_32_BIT_HEADER;

            // EventProperty
            eventData->EventHeader.ThreadId = oldStyleHeader->Header.ThreadId;
            eventData->EventHeader.ProcessId = oldStyleHeader->Header.ProcessId;
            eventData->EventHeader.TimeStamp = oldStyleHeader->Header.TimeStamp;
            eventData->EventHeader.ProviderId = oldStyleHeader->Header.Guid;            // ProviderId = TaskId
            // ID left 0
            eventData->EventHeader.Version = (byte) oldStyleHeader->Header.Version;  
            // Channel
            eventData->EventHeader.Level = oldStyleHeader->Header.Level;
            eventData->EventHeader.Opcode = oldStyleHeader->Header.Type;
            // Task
            // Keyword
            eventData->EventHeader.KernelTime = oldStyleHeader->Header.KernelTime;
            eventData->EventHeader.UserTime = oldStyleHeader->Header.UserTime;
            // ActivityID
            
            eventData->BufferContext = oldStyleHeader->BufferContext;
            // ExtendedDataCount
            eventData->UserDataLength = (ushort) oldStyleHeader->MofLength;
            // ExtendedData
            eventData->UserData = oldStyleHeader->MofData;
            // UserContext 

            RawDispatch(eventData);
        }

        [SecurityTreatAsSafe, SecurityCritical]
        private void RawDispatch(TraceEventNativeMethods.EVENT_RECORD* rawData)
        {
            Debug.Assert(rawData->EventHeader.HeaderType == 0);     // if non-zero probably old-style ETW header
            TraceEvent anEvent = Lookup(rawData);
            // Keep in mind that for UnhandledTraceEvent 'PrepForCallback' has NOT been called, which means the
            // opcode, guid and eventIds are not correct at this point.  The ToString() routine WILL call
            // this so if that is in your debug window, it will have this side effect (which is good and bad)
            // Looking at rawData will give you the truth however. 
            anEvent.DebugValidate();

            if (anEvent.FixupETLData != null)
                anEvent.FixupETLData();
            Dispatch(anEvent);
        }

        [SecurityTreatAsSafe, SecurityCritical]
        protected override void Dispose(bool disposing)
        {
            if (handles != null)
            {
                foreach (ulong handle in handles)
                    if (handle != TraceEventNativeMethods.INVALID_HANDLE_VALUE)
                        TraceEventNativeMethods.CloseTrace(handle);
                handles = null;
            }
            base.Dispose(disposing);
            GC.SuppressFinalize(this);
        }

        
        ~ETWTraceEventSource()
        {
            Dispose(false);
        }
        
        
        private void Reset()
        {
            if (!CanReset)
                throw new InvalidOperationException("Event stream is not resetable (eg Real time)");

            // Annoying.  The OS resets the LogFileMode field, so I have to set it up again.   
            if (!useClassicETW)
                primaryLogFile.LogFileMode = TraceEventNativeMethods.PROCESS_TRACE_MODE_EVENT_RECORD;

            if (handles.Length > 1)
            {
                if (handles[1] != TraceEventNativeMethods.INVALID_HANDLE_VALUE)
                    TraceEventNativeMethods.CloseTrace(handles[1]);
                kernelModeLogFile.LogFileMode = primaryLogFile.LogFileMode;
                handles[1] = TraceEventNativeMethods.OpenTrace(ref kernelModeLogFile);

                if (TraceEventNativeMethods.INVALID_HANDLE_VALUE == handles[1])
                    Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRForLastWin32Error());

            }

            if (handles[0] != TraceEventNativeMethods.INVALID_HANDLE_VALUE)
                TraceEventNativeMethods.CloseTrace(handles[0]);
            handles[0] = TraceEventNativeMethods.OpenTrace(ref primaryLogFile);

            if (TraceEventNativeMethods.INVALID_HANDLE_VALUE == handles[0])
                Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRForLastWin32Error());
    
        }

        // Private data / methods 
        [SecurityTreatAsSafe, SecurityCritical]
        private bool TraceEventBufferCallback(IntPtr rawLogFile)
        {
            return !stopProcessing;
        }

        // #ETWTraceEventSourceFields
        private bool processTraceCalled;
        private TraceEventNativeMethods.EVENT_TRACE_LOGFILEW primaryLogFile;
        private TraceEventNativeMethods.EVENT_TRACE_LOGFILEW kernelModeLogFile;
        private TraceEventNativeMethods.EVENT_RECORD* convertedHeader;
        private UInt64[] handles;

        // We do minimal processing to keep track of process names (since they are REALLY handy). 
        private Dictionary<int, string> processNameForID;
        
        protected internal override string ProcessName(int processID, long time100ns)
        {
            string ret;
            if (!processNameForID.TryGetValue(processID, out ret))
                ret = "";
            return ret;
        }
        #endregion
    }

    /// <summary>
    /// The kinds of data sources that can be opened (see code:ETWTraceEventSource)
    /// </summary>
    [CLSCompliant(false)]
    public enum TraceEventSourceType
    {
        /// <summary>
        /// Look for a ModuleFile *.etl (for user events) and a moduleFile *.kernel.etl (for kernel events) as the event
        /// data source
        /// </summary>
        UserAndKernelFile,
        /// <summary>
        /// Look for a ETL moduleFile *.etl as the event data source 
        /// </summary>
        FileOnly,
        /// <summary>
        /// Use a real time session as the event data source.
        /// </summary>
        Session,
    };
}
