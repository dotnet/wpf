// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics.Eventing;

// TraceEventSession defintions See code:#Introduction to get started.
namespace Microsoft.Test.EventTracing
{
    /// <summary>
    /// #Introduction A TraceEventSession represents a single ETW Tracing Session (something that logs a
    /// single output moduleFile). Every ETL output moduleFile has exactly one session assoicated with it,
    /// although you can have 'real time' sessions that have no output moduleFile and you can connect to
    /// 'directly' to get events without ever creating a moduleFile. You signify this simply by passing
    /// 'null' as the name of the moduleFile. You extract data from these 'real time' sources by specifying
    /// the session name to the constructor of code:ETWTraceEventSource). Sessions are MACHINE WIDE and can
    /// OUTLIVE the process that creates them. This it takes some care to insure that sessions are cleaned up
    /// in all cases.
    /// 
    /// Code that generated ETW events are called Providers. The Kernel has a provider (and it is often the
    /// most intersting) but other components are free to use public APIs (eg TraceEvent), to create
    /// user-mode providers. Each Provider is given a GUID that is used to identify it. You can get a list of
    /// all providers on the system as well as their GUIDs by typing the command
    /// 
    ///             logman query providers
    ///             
    /// The basic model is that you start a session (which creates a ETL moduleFile), and then you call
    /// code:TraceEventSession.EnableProvider on it to add all the providers (event sources), that you are
    /// interested in. A session is given a name (which is MACHINE WIDE), so that you can connect back up to
    /// it from another process (since it might outlive the process that created it), so you can modify it or
    /// (more commonly) close the session down later from another process.
    /// 
    /// For implementation reasons, this is only one Kernel provider and it can only be specified in a
    /// special 'Kernel Mode' session. There can be only one kernel mode session (MACHINE WIDE) and it is
    /// distinguished by a special name 'NT Kernel Logger'. The framework allows you to pass flags to the
    /// provider to control it and the Kernel provider uses these bits to indicate which particular events
    /// are of interest. Because of these restrictions, you often need two sessions, one for the kernel
    /// events and one for all user-mode events.
    /// 
    /// TraceEventSession has suport for simulating being able to run user mode and kernel mode session as
    /// one session. The support surfaces in the code:TraceEventSession.EnableKernelProvider. If this methodIndex
    /// is called on a session that is not named 'NT Kernel Session', AND the session is logging to a
    /// moduleFile, it will start up kernel session that logs to [basename].kernel.etl.
    /// 
    /// Sample use. Enabling the Kernel's DLL image logging to the moduleFile output.etl
    /// 
    ///  TraceEventSession session = new TraceEventSession("output.etl", KernelTraceEventParser.Keywords.ImageLoad); //
    ///  Run you scenario session.Close(); // Flush and close the output.etl moduleFile
    /// 
    /// Once the scenario is complete, you use the code:TraceEventSession.Close methodIndex to shut down a
    /// session. You can also use the code:TraceEventSession.GetActiveSessionNames to get a list of all
    /// currently running session on the machine (in case you forgot to close them!).
    /// 
    /// When the sesion is closed, you can use the code:ETWTraceEventSource to parse the events in the ETL
    /// moduleFile.  Alternatively, you can use code:TraceLog.CreateFromETL to convert the ETL file into an ETLX file. 
    /// Once it is an ETLX file you have a much richer set of processing options availabe from code:TraceLog. 
    /// </summary>
    [SecurityTreatAsSafe, SecurityCritical]
    public sealed class TraceEventSession : IDisposable
    {
        /// <summary>
        /// Create a new logging session.
        /// </summary>
        /// <param name="sessionName">
        /// The name of the session. Since session can exist beyond the lifetime of the process this name is
        /// used to refer to the session from other threads.
        /// </param>
        /// <param name="fileName">
        /// The output moduleFile (by convention .ETL) to put the event data. If this parameter is null, it means
        /// that the data is 'real time' (stored in the session memory itself)
        /// </param>
        
        public TraceEventSession(string sessionName, string fileName)
        {
            Init(sessionName);
            this.fileName = fileName;

            properties.FlushTimer = 1;              // flush every second;
            properties.BufferSize = 64;             // 64 KB buffer blockSize
            properties.MinimumBuffers = 128;        // 64K * 128 = 8Meg minimum
            properties.MaximumBuffers = properties.MinimumBuffers * 4;  // 32Meg maximum
            if (fileName == null)
            {
                properties.LogFileMode = TraceEventNativeMethods.EVENT_TRACE_REAL_TIME_MODE;
            }
            else
            {
                if (fileName.Length > MaxNameSize - 1)
                    throw new ArgumentException("File name too long", "fileName");
                properties.LogFileMode = TraceEventNativeMethods.EVENT_TRACE_FILE_MODE_SEQUENTIAL;
            }

            properties.Wnode.ClientContext = 1;  // set Timer resolution to 100ns.  
            this.create = true;
        }
        /// <summary>
        /// Open an existing Windows Event Tracing Session, with name 'sessionName'. To create a new session,
        /// use TraceEventSession(string, string)
        /// </summary>
        /// <param name="sessionName"> The name of the session to open (see GetActiveSessionNames)</param>
        
        public TraceEventSession(string sessionName) : this(sessionName, false) { }
        /// <summary>
        /// Open an existing Windows Event Tracing Session, with name 'sessionName'.
        /// 
        /// If you are opening a new session use TraceEventSession(string, string).
        ///  
        /// To support the illusion that you can have a session with both kernel and user events,
        /// TraceEventSession might start up both a kernel and a user session.   When you want to 'attach'
        /// to such a combined session, the constructor needs to know if you want to control the kernel
        /// session or not.  If attachKernelSession is true, then it opens both sessions (and thus 'Close'
        /// will operation on both sessions.
        /// </summary>
        public TraceEventSession(string sessionName, bool attachKernelSession)
        {
            Init(sessionName);
            int hr = TraceEventNativeMethods.ControlTrace(0UL, sessionName, ToUnmanagedBuffer(properties, null), TraceEventNativeMethods.EVENT_TRACE_CONTROL_QUERY);
            Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(hr));
            isActive = true;
            properties = (TraceEventNativeMethods.EVENT_TRACE_PROPERTIES)Marshal.PtrToStructure(unmanagedPropertiesBuffer, typeof(TraceEventNativeMethods.EVENT_TRACE_PROPERTIES));
            if (properties.LogFileNameOffset != 0)
                fileName = Marshal.PtrToStringUni((IntPtr)(unmanagedPropertiesBuffer.ToInt64() + properties.LogFileNameOffset));

            if (attachKernelSession)
            {
                bool success = false;
                try
                {
                    kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
                    success = true;
                }
                finally
                {
                    if (!success)
                        Stop();
                }
            }
        }

        [Obsolete("Use overload taking System.Diagnostics.Eventting.Parsers.Kernel.KernelTraceEventParser.KernelSessionName instead")]
        public static string KernelSessionName { get { return "NT Kernel Logger"; } }

        [Obsolete("Use overload taking System.Diagnostics.Eventting.Parsers.Kernel.KernelTraceEventParser.Keywords instead")]
        public bool EnableKernelProvider(TraceEventKernelFlags flags)
        {
            return EnableKernelProvider(flags, TraceEventKernelFlags.None);
        }
        [Obsolete("Use overload taking System.Diagnostics.Eventting.Parsers.Kernel.KernelTraceEventParser.Keywords instead")]
        public unsafe bool EnableKernelProvider(TraceEventKernelFlags flags, TraceEventKernelFlags stackCapture)
        {
            return EnableKernelProvider((KernelTraceEventParser.Keywords) flags, (KernelTraceEventParser.Keywords) stackCapture);
        }


        /// <summary>
        /// Shortcut that enables the kernel provider with no eventToStack trace capturing. 
        /// See code:#EnableKernelProvider (flags, stackCapture)
        /// </summary>
        [CLSCompliant(false)]
        public bool EnableKernelProvider(KernelTraceEventParser.Keywords flags)
        {
            return EnableKernelProvider(flags, KernelTraceEventParser.Keywords.None);
        }
        /// <summary>
        /// #EnableKernelProvider
        /// Enable the kernel provider for the session. If the session name is 'NT Kernel Session' then it
        /// operates on that.   This can be used to manipuate the kernel session.   If the name is not 'NT
        /// Kernel Session' AND it is a moduleFile based session, then it tries to approximate attaching the
        /// kernel session by creating another session logs to [basename].kernel.etl.  There is support in
        /// ETWTraceEventSource for looking for these files automatically, which give a good illusion that
        /// you can have a session that has both kernel and user events turned on.  
        /// <param name="flags">
        /// Specifies the particular kernel events of interest</param>
        /// <param name="stackCapture">
        /// Specifies which events should have their eventToStack traces captured too (VISTA only)</param>
        /// <returns>Returns true if the session had existed before and is now restarted</returns>
        /// </summary>
        [CLSCompliant(false)]
        public unsafe bool EnableKernelProvider(KernelTraceEventParser.Keywords flags, KernelTraceEventParser.Keywords stackCapture)
        {
            if (sessionName != KernelTraceEventParser.KernelSessionName)
            {
                if (kernelSession != null)
                    throw new Exception("A kernel session is already active.");
                if (string.IsNullOrEmpty(FileName))
                    throw new Exception("Cannot enable kernel events to a real time session unless it is named " + KernelTraceEventParser.KernelSessionName);
                string kernelFileName = System.IO.Path.ChangeExtension(FileName, ".kernel.etl");
                kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName, kernelFileName);
                return kernelSession.EnableKernelProvider(flags, stackCapture);
            }
            if (sessionHandle != TraceEventNativeMethods.INVALID_HANDLE_VALUE)
                throw new Exception("The kernel provider must be enabled as the only provider.");

            properties.Wnode.Guid = KernelTraceEventParser.ProviderGuid;
            if (Environment.OSVersion.Version.Major <= 5)
            {
                // TODO should we fail, or should we silently ignore?
                if (stackCapture != KernelTraceEventParser.Keywords.None)
                    throw new Exception("Stack trace capture only available on Windows 6 (VISTA) and above.");

                KernelTraceEventParser.Keywords vistaOnlyFlags =
                    KernelTraceEventParser.Keywords.ProcessCounters |
                    KernelTraceEventParser.Keywords.ContextSwitch |
                    KernelTraceEventParser.Keywords.Interrupt |
                    KernelTraceEventParser.Keywords.DiskIOInit |
                    KernelTraceEventParser.Keywords.Driver |
                    KernelTraceEventParser.Keywords.Profile |
                    KernelTraceEventParser.Keywords.FileIO |
                    KernelTraceEventParser.Keywords.FileIOInit |
                    KernelTraceEventParser.Keywords.Dispatcher |
                    KernelTraceEventParser.Keywords.VirtualAlloc;
                KernelTraceEventParser.Keywords setVistaFlags = flags & vistaOnlyFlags;
                if (setVistaFlags != KernelTraceEventParser.Keywords.None)
                    throw new Exception("A Kernel Event Flags {" + setVistaFlags.ToString() + "} specified that is not supported on Pre-VISTA OSes.");

                properties.EnableFlags = (uint) flags;
                return StartTrace();
            }

            // Initialize the stack collecting information
            const int stackTracingIdsMax = 96;
            int curID = 0;
            var stackTracingIds = stackalloc TraceEventNativeMethods.STACK_TRACING_EVENT_ID[stackTracingIdsMax];
#if DEBUG
            // Try setting all flags, if we overflow an assert in SetStackTraceIds will fire.  
            SetStackTraceIds((KernelTraceEventParser.Keywords)(-1), stackTracingIds, stackTracingIdsMax);
#endif
            if (stackCapture != KernelTraceEventParser.Keywords.None)
                curID = SetStackTraceIds(stackCapture, stackTracingIds, stackTracingIdsMax);

            // The Profile event requires the SeSystemProfilePrivilege to succeed, so set it.  
            if ((flags & KernelTraceEventParser.Keywords.Profile) != 0)
                TraceEventNativeMethods.SetSystemProfilePrivilege();

            bool ret = false;
            properties.EnableFlags = (uint)flags;
            int dwErr = TraceEventNativeMethods.StartKernelTrace(out sessionHandle, ToUnmanagedBuffer(properties, fileName), stackTracingIds, curID);
            if (dwErr == 0xB7) // STIERR_HANDLEEXISTS
            {
                ret = true;
                Stop();
                Thread.Sleep(100);  // Give it some time to stop. 
                dwErr = TraceEventNativeMethods.StartKernelTrace(out sessionHandle, ToUnmanagedBuffer(properties, fileName), stackTracingIds, curID);
            }
            if (dwErr == 5 && Environment.OSVersion.Version.Major > 5)      // On Vista and we get a 'Accessed Denied' message
                throw new UnauthorizedAccessException("Error Starting ETW:  Access Denied (Administrator rights required to start ETW)");
            Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(dwErr));

            return ret;
        }
        public bool EnableProvider(Guid providerGuid, TraceEventLevel providerLevel)
        {
            return EnableProvider(providerGuid, providerLevel, 0, 0, null);
        }
        [CLSCompliant(false)]
        public bool EnableProvider(Guid providerGuid, TraceEventLevel providerLevel, ulong matchAnyKeywords)
        {
            return EnableProvider(providerGuid, providerLevel, matchAnyKeywords, 0, null);
        }
        /// <summary>
        /// Add an additional USER MODE provider prepresented by 'providerGuid' (a list of
        /// providers is available by using 'logman query providers').
        /// </summary>
        /// <param name="providerGuid">
        /// The GUID that represents the event provider to turn on. Use 'logman query providers' or
        /// for a list of possible providers. Note that additional user mode (but not kernel mode)
        /// providers can be added to the session by using EnableProvider.</param>
        /// <param name="providerLevel">The verbosity to turn on</param>
        /// <param name="matchAnyKeywords">A bitvector representing the areas to turn on. Only the
        /// low 32 bits are used by classic providers and passed as the 'flags' value.  Zero
        /// is a special value which is a provider defined default, which is usuall 'everything'</param>
        /// <param name="matchAllKeywords">A bitvector representing keywords of an event that must
        /// be on for a particular event for the event to be logged.  A value of zero means
        /// that no keyword must be on, which effectively ignores this value.  </param>
        /// <param name="values">This is set of key-value strings that are passed to the provider
        /// for provider-specific interpretation. Can be null if no additional args are needed.</param>
        /// <returns>true if the session already existed and needed to be restarted.</returns>
        [CLSCompliant(false)]
        public bool EnableProvider(Guid providerGuid, TraceEventLevel providerLevel, ulong matchAnyKeywords, ulong matchAllKeywords, IEnumerable<KeyValuePair<string, string>> values)
        {
            byte[] valueData = null;
            int valueDataSize = 0;
            int valueDataType = 0;
            if (values != null)
            {
                valueDataType = 0; // ControllerCommands.Start   // TODO use enumeration
                valueData = new byte[1024];
                foreach (KeyValuePair<string, string> keyValue in values)
                {
                    valueDataSize += Encoding.UTF8.GetBytes(keyValue.Key, 0, keyValue.Key.Length, valueData, valueDataSize);
                    if (valueDataSize >= 1023)
                        throw new Exception("Too much provider data");  // TODO better message. 
                    valueData[valueDataSize++] = 0;
                    valueDataSize += Encoding.UTF8.GetBytes(keyValue.Value, 0, keyValue.Value.Length, valueData, valueDataSize);
                    if (valueDataSize >= 1023)
                        throw new Exception("Too much provider data");  // TODO better message. 
                    valueData[valueDataSize++] = 0;
                }
            }
            return EnableProvider(providerGuid, providerLevel, matchAnyKeywords, matchAllKeywords, valueDataType, valueData, valueDataSize);
        }

        /// <summary>
        /// Once started, event sessions will persist even after the process that created them dies. They are
        /// only stoped by this explicit Stop() API.  If you used both kernel and user events, consider
        /// using the code:StopUserAndKernelSession API instead. 
        /// </summary>
        public void Stop()
        {
            if (stopped)
                return;
            stopped = true;
            int hr = TraceEventNativeMethods.ControlTrace(0UL, sessionName,
                ToUnmanagedBuffer(properties, null), TraceEventNativeMethods.EVENT_TRACE_CONTROL_STOP);

            // TODO enumerate providers in session and turn them off
#if false
            string regKeyName = @"Software\Microsoft\Windows\CurrentVersion\Winevt\Publishers\{" + providerGuid + "}";
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regKeyName, true);
            regKey.DeleteValue("ControllerData", false);
            regKey.Close();
#endif

            if (hr != 4201)     // Instance name not found.  This means we did not start
                Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(hr));

            if (kernelSession != null)
            {
                kernelSession.Stop();
                kernelSession = null;
            }
        }
        /// <summary>
        /// TraceEventSessions may have both a kernel session and a user session turned on.  To simplify
        /// error handling, call code:StopSession to stop both.  This is equivalent it attaching to the
        /// combined session and calling Stop, but also works in all error cases (if it was possible to stop
        /// the sessions they are stopped), and is silent if the sessions are already stopped.  
        /// </summary>
        /// <param name="userSessionName">The name of the user session to stop</param>
        public static void StopUserAndKernelSession(string userSessionName)
        {
            Exception eToThow = null;
            try
            {
                TraceEventSession kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
                kernelSession.Stop();
            }
            catch (COMException e)
            {
                if ((uint)e.ErrorCode != 0x80071069)        // Could not find provider, that is OK. 
                    eToThow = e;
            }
            catch (Exception e)
            {
                eToThow = e;                                // we will throw this later.  
            }

            try
            {
                TraceEventSession userSession = new TraceEventSession(userSessionName);
                userSession.Stop();
            }
            catch (COMException e)
            {
                if ((uint)e.ErrorCode != 0x80071069)      // Could not find provider, that is OK 
                    throw;
            }

            // If we got an error closing down the kernel provider, throw it here.  
            if (eToThow != null)
                throw eToThow;
        }

        /// <summary>
        /// The name of the session that can be used by other threads to attach to the session. 
        /// </summary>
        public string SessionName
        {
            get { return sessionName; }
        }
        /// <summary>
        /// The name of the moduleFile that events are logged to.  Null means the session is real time. 
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
        }
        /// <summary>
        /// Creating a TraceEventSession does not actually interact with the operating system until a
        /// provider is enabled. At that point the session is considered active (OS state that survives a
        /// process exit has been modified). IsActive returns true if the session is active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return isActive;
            }
        }
        /// <summary>
        /// Returns true if the OS kernel provider is enabled.  
        /// </summary>
        public bool KernelProviderEnabled { get { return kernelSession != null; } }

        /// <summary>
        /// ETW trace sessions survive process shutdown. Thus you can attach to existing active sessions.
        /// GetActiveSessionNames() returns a list of currently existing session names.  These can be passed
        /// to the code:TraceEventSession constructor to control it.   
        /// </summary>
        /// <returns>A enumeration of strings, each of which is a name of a session</returns>
        public unsafe static IEnumerable<string> GetActiveSessionNames()
        {
            const int MAX_SESSIONS = 64;
            int sizeOfProperties = sizeof(TraceEventNativeMethods.EVENT_TRACE_PROPERTIES) +
                                   sizeof(char) * MaxNameSize +     // For log moduleFile name 
                                   sizeof(char) * MaxNameSize;      // For session name

            byte* sessionsArray = stackalloc byte[MAX_SESSIONS * sizeOfProperties];
            TraceEventNativeMethods.EVENT_TRACE_PROPERTIES** propetiesArray = stackalloc TraceEventNativeMethods.EVENT_TRACE_PROPERTIES*[MAX_SESSIONS];

            for (int i = 0; i < MAX_SESSIONS; i++)
            {
                TraceEventNativeMethods.EVENT_TRACE_PROPERTIES* properties = (TraceEventNativeMethods.EVENT_TRACE_PROPERTIES*)&sessionsArray[sizeOfProperties * i];
                properties->Wnode.BufferSize = (uint)sizeOfProperties;
                properties->LoggerNameOffset = (uint)sizeof(TraceEventNativeMethods.EVENT_TRACE_PROPERTIES);
                properties->LogFileNameOffset = (uint)sizeof(TraceEventNativeMethods.EVENT_TRACE_PROPERTIES) + sizeof(char) * MaxNameSize;
                propetiesArray[i] = properties;
            }
            int sessionCount = 0;
            int hr = TraceEventNativeMethods.QueryAllTraces((IntPtr)propetiesArray, MAX_SESSIONS, ref sessionCount);
            Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(hr));

            List<string> activeTraceNames = new List<string>();
            for (int i = 0; i < sessionCount; i++)
            {
                byte* propertiesBlob = (byte*)propetiesArray[i];
                string sessionName = new string((char*)(&propertiesBlob[propetiesArray[i]->LoggerNameOffset]));
                activeTraceNames.Add(sessionName);
            }
            return activeTraceNames;
        }

        #region Private
        private const int maxStackTraceProviders = 256;

        /// <summary>
        /// Do intialization common to the contructors.  
        /// </summary>
        
        private void Init(string sessionName)
        {
            this.sessionHandle = TraceEventNativeMethods.INVALID_HANDLE_VALUE;
            if (sessionName.Length > MaxNameSize - 1)
                throw new ArgumentException("File name too long", "sessionName");

            this.sessionName = sessionName;
            properties = new TraceEventNativeMethods.EVENT_TRACE_PROPERTIES();
            properties.Wnode.Flags = TraceEventNativeMethods.WNODE_FLAG_TRACED_GUID;
            properties.LoggerNameOffset = (uint)Marshal.SizeOf(typeof(TraceEventNativeMethods.EVENT_TRACE_PROPERTIES));
            properties.LogFileNameOffset = properties.LoggerNameOffset + MaxNameSize * sizeof(char);
            extentionSpaceOffset = properties.LogFileNameOffset + MaxNameSize * sizeof(char);
            // #sizeCalculation
            properties.Wnode.BufferSize = extentionSpaceOffset + 8 + 8 * 4 + 4 + maxStackTraceProviders * 4;
            unmanagedPropertiesBuffer = TraceEventNativeMethods.AllocHGlobal((int) properties.Wnode.BufferSize);
            TraceEventNativeMethods.ZeroMemory(unmanagedPropertiesBuffer, properties.Wnode.BufferSize);
        }

        private bool InsureSession()
        {
            bool ret = false;
            if (sessionHandle == TraceEventNativeMethods.INVALID_HANDLE_VALUE)
            {
                if (create)
                {
                    properties.Wnode.Guid = new Guid();
                    ret = StartTrace();
                }
                else
                {
                    // You should only get here if the constructor failed. 
                    throw new Exception("Invalid TraceEventSession");
                }
            }
            return ret;
        }

        private unsafe bool EnableProvider(Guid providerGuid, TraceEventLevel providerLevel, ulong matchAnyKeywords, ulong matchAllKeywords, int providerDataType, byte[] providerData, int providerDataSize)
        {
            bool ret = InsureSession();
            TraceEventNativeMethods.EVENT_FILTER_DESCRIPTOR* dataDescrPtr = null;
            fixed (byte* providerDataPtr = providerData)
            {
                string regKeyName = @"Software\Microsoft\Windows\CurrentVersion\Winevt\Publishers\{" + providerGuid + "}";
                byte[] registryData = null;
                if (providerData != null || providerDataType != 0)
                {
                    TraceEventNativeMethods.EVENT_FILTER_DESCRIPTOR dataDescr = new TraceEventNativeMethods.EVENT_FILTER_DESCRIPTOR();
                    dataDescr.Ptr = null;
                    dataDescr.Size = providerDataSize;
                    dataDescr.Type = providerDataType;
                    dataDescrPtr = &dataDescr;

                    if (providerData == null)
                        providerData = new byte[0];
                    else
                        dataDescr.Ptr = providerDataPtr;

                    // Set the registry key so providers get the information even if they are not active now
                    registryData = new byte[providerDataSize + 4];
                    registryData[0] = (byte)(providerDataType);
                    registryData[1] = (byte)(providerDataType >> 8);
                    registryData[2] = (byte)(providerDataType >> 16);
                    registryData[3] = (byte)(providerDataType >> 24);
                    Array.Copy(providerData, 0, registryData, 4, providerDataSize);
                }
                SetOrDelete(regKeyName, "ControllerData", registryData);
                int hr;
                try
                {
                    hr = TraceEventNativeMethods.EnableTraceEx(ref providerGuid, null, sessionHandle,
                    1, (byte)providerLevel, matchAnyKeywords, matchAllKeywords, 0, dataDescrPtr);
                }
                catch (EntryPointNotFoundException)
                {
                    // Try with the old pre-vista API
                    hr = TraceEventNativeMethods.EnableTrace(1, (int)matchAnyKeywords, (int)providerLevel, ref providerGuid, sessionHandle);
                }
                Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(hr));
                }
            isActive = true;
            return ret;
        }

        private void SetOrDelete(string regKeyName, string valueName, byte[] data)
        {
#if !Silverlight 
            if (System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)) == 8 &&
                regKeyName.StartsWith(@"Software\", StringComparison.OrdinalIgnoreCase))
                regKeyName = @"Software\Wow6432Node" + regKeyName.Substring(8);

            if (data == null)
            {
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regKeyName, true);
                if (regKey != null)
                {
                    regKey.DeleteValue(valueName, false);
                    regKey.Close();
                }
            }
            else
            {
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(regKeyName);
                regKey.SetValue(valueName, data, Microsoft.Win32.RegistryValueKind.Binary);
                regKey.Close();
            }
#endif
        }

        /// <summary>
        /// Given a mask of kernel flags, set the array stackTracingIds of size stackTracingIdsMax to match.
        /// It returns the number of entries in stackTracingIds that were filled in.
        /// </summary>
        private unsafe int SetStackTraceIds(KernelTraceEventParser.Keywords stackCapture, TraceEventNativeMethods.STACK_TRACING_EVENT_ID* stackTracingIds, int stackTracingIdsMax)
        {
            int curID = 0;

            // PerfInfo (sample profiling)
            if ((stackCapture & KernelTraceEventParser.Keywords.Profile) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PerfInfoTaskGuid;
                stackTracingIds[curID].Type = 0x2e;     // Sample Profile
                curID++;
            }

            if ((stackCapture & KernelTraceEventParser.Keywords.SystemCall) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PerfInfoTaskGuid;
                stackTracingIds[curID].Type = 0x33;     // SysCall
                curID++;
            }
            // TODO SysCall?

            // Thread
            if ((stackCapture & KernelTraceEventParser.Keywords.Thread) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.ThreadTaskGuid;
                stackTracingIds[curID].Type = 0x01;     // Thread Create
                curID++;
            }

            if ((stackCapture & KernelTraceEventParser.Keywords.ContextSwitch) != 0)
                {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.ThreadTaskGuid;
                if ((stackCapture & KernelTraceEventParser.Keywords.Thread) != 0)
                    stackTracingIds[curID].Type = 0x24;     // Context Switch
                curID++;
            }

            if ((stackCapture & KernelTraceEventParser.Keywords.Dispatcher) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.ThreadTaskGuid;
                stackTracingIds[curID].Type = 0x32;     // Ready Thread
                curID++;
            }

            // Image
            if ((stackCapture & KernelTraceEventParser.Keywords.ImageLoad) != 0)
            {
                // Confirm this is not ImageTaskGuid
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.ProcessTaskGuid;
                stackTracingIds[curID].Type = 0x0A;     // Image Load
                curID++;
            }

            // Process
            if ((stackCapture & KernelTraceEventParser.Keywords.Process) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.ProcessTaskGuid;
                stackTracingIds[curID].Type = 0x01;     // Process Create
                curID++;
            }

            // Disk
            if ((stackCapture & KernelTraceEventParser.Keywords.DiskIOInit) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.DiskIoTaskGuid;
                stackTracingIds[curID].Type = 0x0c;     // Read Init
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.DiskIoTaskGuid;
                stackTracingIds[curID].Type = 0x0d;     // Write Init
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.DiskIoTaskGuid;
                stackTracingIds[curID].Type = 0x0f;     // Flush Init
                curID++;
            }

            // Virtual Alloc
            if ((stackCapture & KernelTraceEventParser.Keywords.VirtualAlloc) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.VirtualAllocTaskGuid;
                stackTracingIds[curID].Type = 0x62;     // Flush Init
                curID++;
            }

            // Hard Faults
            if ((stackCapture & KernelTraceEventParser.Keywords.MemoryHardFaults) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PageFaultTaskGuid;
                stackTracingIds[curID].Type = 0x20;     // Hard Fault
                curID++;
            }

            // Page Faults 
            if ((stackCapture & KernelTraceEventParser.Keywords.MemoryPageFaults) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PageFaultTaskGuid;
                stackTracingIds[curID].Type = 0x0A;     // Transition Fault
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PageFaultTaskGuid;
                stackTracingIds[curID].Type = 0x0B;     // Demand zero Fault
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PageFaultTaskGuid;
                stackTracingIds[curID].Type = 0x0C;     // Copy on Write Fault
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PageFaultTaskGuid;
                stackTracingIds[curID].Type = 0x0D;     // Guard Page Fault
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.PageFaultTaskGuid;
                stackTracingIds[curID].Type = 0x0E;     // Hard Page Fault
                curID++;

                // ! %02 49 ! Pagefile Mapped Section Create
                // ! %02 69 ! Pagefile Backed Image Mapping
                // ! %02 71 ! Contiguous Memory Generation
            }

            if ((stackCapture & KernelTraceEventParser.Keywords.FileIOInit) != 0)
            {
                // TODO allow stacks only on open and close;
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x40;     // Create
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x41;     // Cleanup
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x42;     // Close
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x43;     // Read
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x44;     // Write
                curID++;

#if false
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x45;     // SetInformation
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x46;     // Delete
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x47;     // Rename
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x48;     // DirEnum
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x49;     // Flush
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x4A;     // QueryInformation
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x4B;     // FSControl
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.FileIoTaskGuid;
                stackTracingIds[curID].Type = 0x4D;     // DirNotify
                curID++;
#endif
            }

            if ((stackCapture & KernelTraceEventParser.Keywords.Registry) != 0)
            {
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x0A;     // NtCreateKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x0B;     // NtOpenKey
                curID++;
#if false
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x0C;     // NtDeleteKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x0D;     // NtQueryKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x0E;     // NtSetValueKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x0F;     // NtDeleteValueKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x10;     // NtQueryValueKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x11;     // NtEnumerateKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x12;     // NtEnumerateValueKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x13;     // NtQueryMultipleValueKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x14;     // NtSetInformationKey
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x15;     // NtFlushKey
                curID++;

                // TODO What are these?  
                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x16;     // KcbCreate
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x17;     // KcbDelete
                curID++;

                stackTracingIds[curID].EventGuid = KernelTraceEventParser.RegistryTaskGuid;
                stackTracingIds[curID].Type = 0x1A;     // VirtualizeKey
                curID++;
#endif
            }

            // Confirm we did not overflow.  
            Debug.Assert(curID <= stackTracingIdsMax);
            return curID;
        }

        /// <summary>
        /// The 'properties' field is only the header information.  There is 'tail' that is 
        /// required.  'ToUnmangedBuffer' fills in this tail properly. 
        /// </summary>
        ~TraceEventSession()
        {
            Dispose();
        }

        [SecurityTreatAsSafe, SecurityCritical]
        public void Dispose()
        {
            if (unmanagedPropertiesBuffer != IntPtr.Zero)
                TraceEventNativeMethods.FreeHGlobal(unmanagedPropertiesBuffer);                
            unmanagedPropertiesBuffer = IntPtr.Zero;

            if (sessionHandle != TraceEventNativeMethods.INVALID_HANDLE_VALUE)
                TraceEventNativeMethods.CloseTrace(sessionHandle);
            sessionHandle = TraceEventNativeMethods.INVALID_HANDLE_VALUE;

            GC.SuppressFinalize(this);
        }

        private IntPtr ToUnmanagedBuffer(TraceEventNativeMethods.EVENT_TRACE_PROPERTIES properties, string fileName)
        {

            if (fileName == null)
                properties.LogFileNameOffset = 0;
            else
                properties.LogFileNameOffset = properties.LoggerNameOffset + MaxNameSize * 2;

            Marshal.StructureToPtr(properties, unmanagedPropertiesBuffer, false);
            byte[] buffer = Encoding.Unicode.GetBytes(sessionName);
            Marshal.Copy(buffer, 0, (IntPtr)(unmanagedPropertiesBuffer.ToInt64() + properties.LoggerNameOffset), buffer.Length);
            Marshal.WriteInt16(unmanagedPropertiesBuffer, (int)properties.LoggerNameOffset + buffer.Length, 0);

            if (fileName != null)
            {
                buffer = Encoding.Unicode.GetBytes(fileName);
                Marshal.Copy(buffer, 0, (IntPtr)(unmanagedPropertiesBuffer.ToInt64() + properties.LogFileNameOffset), buffer.Length);
                Marshal.WriteInt16(unmanagedPropertiesBuffer, (int)properties.LogFileNameOffset + buffer.Length, 0);
            }
            return unmanagedPropertiesBuffer;
        }

        /// <summary>
        /// Actually starts the trace, with added logic to retry if the session already exists.  
        /// </summary>
        private bool StartTrace()
        {
            bool ret = false;
            int dwErr = TraceEventNativeMethods.StartTrace(out sessionHandle, sessionName, ToUnmanagedBuffer(properties, fileName));
            if (dwErr == 0xB7) // STIERR_HANDLEEXISTS
            {
                ret = true;
                Stop();
                Thread.Sleep(100);  // Give it some time to stop. 
                dwErr = TraceEventNativeMethods.StartTrace(out sessionHandle, sessionName, ToUnmanagedBuffer(properties, fileName));
            }
            if (dwErr == 5 && Environment.OSVersion.Version.Major > 5)      // On Vista and we get a 'Accessed Denied' message
                throw new UnauthorizedAccessException("Error Starting ETW:  Access Denied (Administrator rights required to start ETW)");

            Marshal.ThrowExceptionForHR(TraceEventNativeMethods.GetHRFromWin32(dwErr));
            return ret;
        }

        private const int MaxNameSize = 1024;

        // Data that is exposed through properties.  
        private string sessionName;
        private string fileName;

        // Things TraceEventSession generates
        private bool create;                    // Should create if it does not exist.
        private bool isActive;
        private bool stopped;
        private UInt64 sessionHandle;
        TraceEventNativeMethods.EVENT_TRACE_PROPERTIES properties;
        IntPtr unmanagedPropertiesBuffer;
        uint extentionSpaceOffset;
        private TraceEventSession kernelSession;        // Support to do a user and kernel session together. 
        #endregion
    }

    /// <summary>
    /// Indicates to a provider whether verbose events should be logged.  
    /// </summary>
    public enum TraceEventLevel
    {
        Always = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Informational = 4,
        Verbose = 5,
    };

    /// <summary>
    /// MatchAnyKeywords that indicate which kernel events to log.  See
    /// http://msdn2.microsoft.com/en-us/library/aa363784.aspx (EnableFlags) for more 
    /// </summary>
    [Flags, Obsolete("Use System.Diagnostics.Eventting.Parsers.Kernel.KernelTraceEventParser.Keywords instead")]
    public enum TraceEventKernelFlags
    {
        None = 0x00000000, // no tracing
        Process = 0x00000001,
        Thread = 0x00000002,
        ImageLoad = 0x00000004, // image load
        DiskIO = 0x00000100, // physical disk IO
        DiskFileIO = 0x00000200, // requires disk IO
        MemoryPageFaults = 0x00001000,
        MemoryHardFaults = 0x00002000,
        NetworkTCPIP = 0x00010000,
        Registry = 0x00020000, // registry calls
        ProcessCounters = 0x00000008,
        ContextSwitch = 0x00000010,
        DeferedProcedureCalls = 0x00000020,
        Interrupt = 0x00000040,
        SystemCall = 0x00000080,
        DiskIOInit = 0x00000400,
        SplitIO = 0x00200000,
        Driver = 0x00800000,
        Profile = 0x01000000,
        FileIO = 0x02000000,
        FileIOInit = 0x04000000,
        Dispatcher = 0x00000800,
        VirtualAlloc = 0x004000,
        Default = Process | Thread | ImageLoad | DiskIO | DiskFileIO | NetworkTCPIP | MemoryHardFaults | ProcessCounters | Profile,
        All = Default | DeferedProcedureCalls | Interrupt | SystemCall | DiskIOInit | SplitIO | Driver | FileIO | FileIOInit | ContextSwitch,
    };

    /// <summary>
    /// flags that can be passed to code:EnableClrProvider
    /// </summary>
    [Flags, Obsolete("Use System.Diagnostics.Eventting.Parsers.Clr.ClrTraceEventParser.Keywords instead")]
    public enum TraceEventClrFlags
    {
        None = 0x0,
        GC = 0x00001,
        Fusion = 0x00004,
        Loader = 0x00008,
        Jit = 0x00010,
        NGen = 0x00020,
        AttachRundown = 0x00040,
        DetachRundown = 0x00080,
        Security = 0x400,
        AppDomainResourceManagement = 0x800,
        JitTracing = 0x1000,
        Interop = 0x2000,
        Contention = 0x4000,
        Exception = 0x8000,
        Threading = 0x10000,
        Stack = 0x40000000,
        Default = GC | Fusion | Loader | Jit | NGen | DetachRundown | Security | AppDomainResourceManagement | Exception  | Threading,
        RundownFlags = AttachRundown | DetachRundown,
    }
}

