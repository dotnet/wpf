// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security;

// This moduleFile contains Internal PINVOKE declarations and has no public API surface. 
namespace Microsoft.Test.EventTracing
{
    #region Private Classes

    /// <summary>
    /// TraceEventNativeMethods contians the PINVOKE declarations needed
    /// to get at the Win32 TraceEvent infrastructure.  It is effectively
    /// a port of evntrace.h to C# declarations.  
    /// </summary>
    [SecurityTreatAsSafe, SecurityCritical]
    internal unsafe static class TraceEventNativeMethods
    {
        #region symbol lookup
        /**
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetCurrentProcessId();

        [DllImport("kernel32.dll",  SetLastError = true)]
        public static extern IntPtr OpenProcess(int access, bool inherit, int processID);
        **/

        internal const int SSRVOPT_DWORD = 0x0002;
        internal const int SSRVOPT_DWORDPTR = 0x004;
        internal const int SSRVOPT_GUIDPTR = 0x0008;

        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymFindFileInPathW(
            IntPtr hProcess, 
            string searchPath, 
            [MarshalAs(UnmanagedType.LPWStr)]
            [In]string fileName, 
            IntPtr id, //void*
            int two, 
            int three, 
            int flags,             
            [Out]System.Text.StringBuilder filepath, 
            SymFindFileInPathProc findCallback, 
            IntPtr context // void*
            );

        [DllImport("dbghelp.dll", CharSet = CharSet.Ansi, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymFindFileInPath(
            IntPtr hProcess,
            string searchPath,
            string fileName,
            IntPtr id, //void*
            int two,
            int three,
            int flags,
            out string filepath,
            SymFindFileInPathProc findCallback,
            IntPtr context // void*
            );


        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymInitializeW(
            IntPtr hProcess,
            string UserSearchPath,
            [MarshalAs(UnmanagedType.Bool)] bool fInvadeProcess);

        [DllImport("dbghelp.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymCleanup(
            IntPtr hProcess);


        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymEnumSymbolsW(
            IntPtr hProcess,
            ulong BaseOfDll,
            string Mask,
            SymEnumSymbolsProc EnumSymbolsCallback,
            IntPtr UserContext);

        // TODO: unicode version of this does not work
        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern ulong SymLoadModuleExW(
            IntPtr hProcess,
            IntPtr hFile,
            string ImageName,
            string ModuleName,
            ulong BaseOfDll,
            uint DllSize,
            void* Data,
            uint Flags
         );

        [DllImport("dbghelp.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymUnloadModule64(
            IntPtr hProcess,
            ulong BaseOfDll);

        /***
        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymFromAddrW(
            IntPtr hProcess,
            ulong Address,
            ref ulong Displacement,
            SYMBOL_INFO* Symbol
        );
         ****/

        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymGetLineFromAddrW64(
            IntPtr hProcess,
            ulong Address,
            ref Int32 Displacement,
            ref IMAGEHLP_LINE64 Line
        );

        // Some structures used by the callback 
        internal struct IMAGEHLP_CBA_EVENT
        {
            public int Severity;
            public char* pStrDesc;
            public void* pData;

        }        
        internal struct IMAGEHLP_DEFERRED_SYMBOL_LOAD64
        {
            public int SizeOfStruct;
            public Int64 BaseOfImage;
            public int CheckSum;
            public int TimeDateStamp;            
            public fixed sbyte FileName[MAX_PATH];
            public bool Reparse;
            public void* hFile;
            public int Flags;
        }

        [DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymRegisterCallbackW64(
            IntPtr hProcess,
            SymRegisterCallbackProc callBack,
            ulong UserContext);

        internal delegate bool SymRegisterCallbackProc(
            IntPtr hProcess,
            SymCallbackActions ActionCode,
            ulong UserData,
            ulong UserContext);


        [Flags]
        public enum SymCallbackActions
        {
            CBA_DEBUG_INFO = 0x10000000,
            CBA_DEFERRED_SYMBOL_LOAD_CANCEL = 0x00000007,
            CBA_DEFERRED_SYMBOL_LOAD_COMPLETE = 0x00000002,
            CBA_DEFERRED_SYMBOL_LOAD_FAILURE = 0x00000003,
            CBA_DEFERRED_SYMBOL_LOAD_PARTIAL = 0x00000020,
            CBA_DEFERRED_SYMBOL_LOAD_START = 0x00000001,
            CBA_DUPLICATE_SYMBOL = 0x00000005,
            CBA_EVENT = 0x00000010,
            CBA_READ_MEMORY = 0x00000006,
            CBA_SET_OPTIONS = 0x00000008,
            CBA_SRCSRV_EVENT = 0x40000000,
            CBA_SRCSRV_INFO = 0x20000000,
            CBA_SYMBOLS_UNLOADED = 0x00000004,
        }

        [Flags]
        public enum SymOptions : uint
        {
            SYMOPT_ALLOW_ABSOLUTE_SYMBOLS = 0x00000800,
            SYMOPT_ALLOW_ZERO_ADDRESS = 0x01000000,
            SYMOPT_AUTO_PUBLICS = 0x00010000,
            SYMOPT_CASE_INSENSITIVE = 0x00000001,
            SYMOPT_DEBUG = 0x80000000,
            SYMOPT_DEFERRED_LOADS = 0x00000004,
            SYMOPT_DISABLE_SYMSRV_AUTODETECT = 0x02000000,
            SYMOPT_EXACT_SYMBOLS = 0x00000400,
            SYMOPT_FAIL_CRITICAL_ERRORS = 0x00000200,
            SYMOPT_FAVOR_COMPRESSED = 0x00800000,
            SYMOPT_FLAT_DIRECTORY = 0x00400000,
            SYMOPT_IGNORE_CVREC = 0x00000080,
            SYMOPT_IGNORE_IMAGEDIR = 0x00200000,
            SYMOPT_IGNORE_NT_SYMPATH = 0x00001000,
            SYMOPT_INCLUDE_32BIT_MODULES = 0x00002000,
            SYMOPT_LOAD_ANYTHING = 0x00000040,
            SYMOPT_LOAD_LINES = 0x00000010,
            SYMOPT_NO_CPP = 0x00000008,
            SYMOPT_NO_IMAGE_SEARCH = 0x00020000,
            SYMOPT_NO_PROMPTS = 0x00080000,
            SYMOPT_NO_PUBLICS = 0x00008000,
            SYMOPT_NO_UNQUALIFIED_LOADS = 0x00000100,
            SYMOPT_OVERWRITE = 0x00100000,
            SYMOPT_PUBLICS_ONLY = 0x00004000,
            SYMOPT_SECURE = 0x00040000,
            SYMOPT_UNDNAME = 0x00000002,
        };

        [DllImport("dbghelp.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern SymOptions SymSetOptions(
            SymOptions SymOptions
            );

        [DllImport("dbghelp.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern SymOptions SymGetOptions();

        internal delegate bool SymEnumSymbolsProc(
            SYMBOL_INFO* pSymInfo,
            uint SymbolSize,
            IntPtr UserContext);
        internal delegate  bool SymFindFileInPathProc( string fileName,
                                                       IntPtr context);
        internal delegate bool SymEnumLinesProc(
            SRCCODEINFO* LineInfo,
            IntPtr UserContext);

        internal struct SYMBOL_INFO
        {
            public UInt32 SizeOfStruct;
            public UInt32 TypeIndex;
            public UInt64 Reserved1;
            public UInt64 Reserved2;
            public UInt32 Index;
            public UInt32 Size;
            public UInt64 ModBase;
            public UInt32 Flags;
            public UInt64 Value;
            public UInt64 Address;
            public UInt32 Register;
            public UInt32 Scope;
            public UInt32 Tag;
            public UInt32 NameLen;
            public UInt32 MaxNameLen;
            public byte Name;           // Actually of variable size Unicode string
        };

        internal struct IMAGEHLP_LINE64
        {
            public UInt32 SizeOfStruct;
            public void* Key;
            public UInt32 LineNumber;
            public byte* FileName;             // pointer to character string. 
            public UInt64 Address;
        };

        internal const int MAX_PATH = 260;
        internal const int DSLFLAG_MISMATCHED_DBG = 0x2;
        internal const int DSLFLAG_MISMATCHED_PDB = 0x1;

        internal struct SRCCODEINFO
        {
            public UInt32 SizeOfStruct;
            public void* Key;
            public UInt64 ModBase;
            public InlineAsciiString Obj;
            public InlineAsciiString FileName;
            public UInt32 LineNumber;
            public UInt64 Address;
        };

        // Tools like BBT rearrange the image but not the PDB instead they simply append a mapping
        // structure at the end that allows the reader of the PDB to map old address to the new address
        [DllImport("dbghelp.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SymGetOmaps(
            IntPtr hProcess,
            ulong BaseOfDll,
            ref OMAP* OmapTo,
            ref ulong cOmapTo,
            ref OMAP* OmapFrom,
            ref ulong cOmapFrom);

        internal struct OMAP
        {
            public int rva;
            public int rvaTo;
        };

        [StructLayout(LayoutKind.Explicit, Size = MAX_PATH + 1)]
        internal struct InlineAsciiString { }

        #endregion

        #region TimeZone type from winbase.h

        /// <summary>
        ///	Time zone info.  Used as one field of TRACE_EVENT_LOGFILE, below.
        ///	Total struct size is 0xac.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 0xac, CharSet = CharSet.Unicode)]
        internal struct TIME_ZONE_INFORMATION
        {
            public uint bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string standardName;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 8)]
            public UInt16[] standardDate;
            public uint standardBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string daylightName;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 8)]
            public UInt16[] daylightDate;
            public uint daylightBias;
        }

        #endregion TimeZone type from winbase.h

        #region ETW tracing types from evntrace.h

        //	Delegates for use with ETW EVENT_TRACE_LOGFILEW struct.
        //	These are the callbacks that ETW will call while processing a moduleFile
        //	so that we can process each line of the trace moduleFile.
        internal delegate bool EventTraceBufferCallback(
            [In] IntPtr logfile); // Really a EVENT_TRACE_LOGFILEW, but more efficient to marshal manually);
        internal delegate void EventTraceEventCallback(
            [In] EVENT_RECORD* rawData);

        internal const ulong INVALID_HANDLE_VALUE = unchecked((ulong)(-1));

        internal const uint EVENT_TRACE_REAL_TIME_MODE = 0x00000100;

        //  EVENT_TRACE_LOGFILE.LogFileMode should be set to PROCESS_TRACE_MODE_EVENT_RECORD 
        //  to consume events using EventRecordCallback
        internal const uint PROCESS_TRACE_MODE_EVENT_RECORD = 0x10000000;

        internal const uint EVENT_TRACE_FILE_MODE_NONE = 0x00000000;
        internal const uint EVENT_TRACE_FILE_MODE_SEQUENTIAL = 0x00000001;
        internal const uint EVENT_TRACE_FILE_MODE_CIRCULAR = 0x00000002;
        internal const uint EVENT_TRACE_FILE_MODE_APPEND = 0x00000004;
        internal const uint EVENT_TRACE_FILE_MODE_NEWFILE = 0x00000008;


        internal const uint EVENT_TRACE_CONTROL_QUERY = 0;
        internal const uint EVENT_TRACE_CONTROL_STOP = 1;
        internal const uint EVENT_TRACE_CONTROL_UPDATE = 2;
        internal const uint EVENT_TRACE_CONTROL_FLUSH = 3;

        internal const uint WNODE_FLAG_TRACED_GUID = 0x00020000;

        /// <summary>
        /// EventTraceHeader structure used by EVENT_TRACE_PROPERTIES
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct WNODE_HEADER
        {
            public UInt32 BufferSize;
            public UInt32 ProviderId;
            public UInt64 HistoricalContext;
            public UInt64 TimeStamp;
            public Guid Guid;
            public UInt32 ClientContext;
            public UInt32 Flags;
        }

        /// <summary>
        /// EVENT_TRACE_PROPERTIES is a structure used by StartTrace, ControlTrace
        /// however it can not be used directly in the defintion of these functions
        /// because extra information has to be attached to the end of the structure
        /// before being passed.  (LofFileNameOffset, LoggerNameOffset)
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENT_TRACE_PROPERTIES
        {
            public WNODE_HEADER Wnode;
            public UInt32 BufferSize;
            public UInt32 MinimumBuffers;
            public UInt32 MaximumBuffers;
            public UInt32 MaximumFileSize;
            public UInt32 LogFileMode;
            public UInt32 FlushTimer;
            public UInt32 EnableFlags;
            public Int32 AgeLimit;
            public UInt32 NumberOfBuffers;
            public UInt32 FreeBuffers;
            public UInt32 EventsLost;
            public UInt32 BuffersWritten;
            public UInt32 LogBuffersLost;
            public UInt32 RealTimeBuffersLost;
            public IntPtr LoggerThreadId;
            public UInt32 LogFileNameOffset;
            public UInt32 LoggerNameOffset;
        }

        //	TraceMessage flags
        //	These flags are overlaid into the node USHORT in the EVENT_TRACE.header.version field.
        //	These items are packed in order in the packet (MofBuffer), as indicated by the flags.
        //	I don't know what PerfTimestamp is (size?) or holds.
        internal enum TraceMessageFlags : int
        {
            Sequence = 0x01,
            Guid = 0x02,
            ComponentId = 0x04,
            Timestamp = 0x08,
            PerformanceTimestamp = 0x10,
            SystemInfo = 0x20,
            FlagMask = 0xffff,
        }

        /// <summary>
        ///	EventTraceHeader and structure used to defined EVENT_TRACE (the main packet)
        ///	I have simplified from the original struct definitions.  I have
        ///	omitted alternate union-fields which we don't use.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENT_TRACE_HEADER
        {
            public ushort Size;
            public ushort FieldTypeFlags;	// holds our MarkerFlags too
            public byte Type;
            public byte Level;
            public ushort Version;
            public int ThreadId;
            public int ProcessId;
            public long TimeStamp;          // Offset 0x10 
            public Guid Guid;
            //	no access to GuidPtr, union'd with guid field
            //	no access to ClientContext & MatchAnyKeywords, ProcessorTime, 
            //	union'd with kernelTime,userTime
            public int KernelTime;         // Offset 0x28
            public int UserTime;
        }

        /// <summary>
        /// EVENT_TRACE is the structure that represents a single 'packet'
        /// of data repesenting a single event.  
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENT_TRACE
        {
            public EVENT_TRACE_HEADER Header;
            public uint InstanceId;
            public uint ParentInstanceId;
            public Guid ParentGuid;
            public IntPtr MofData; // PVOID
            public int MofLength;
            public ETW_BUFFER_CONTEXT BufferContext;
        }

        /// <summary>
        /// TRACE_LOGFILE_HEADER is a header used to define EVENT_TRACE_LOGFILEW.
        ///	Total struct size is 0x110.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TRACE_LOGFILE_HEADER
        {
            public uint BufferSize;
            public uint Version;
            public uint ProviderVersion;
            public uint NumberOfProcessors;
            public long EndTime;            // 0x10
            public uint TimerResolution;
            public uint MaximumFileSize;
            public uint LogFileMode;        // 0x20
            public uint BuffersWritten;
            public uint StartBuffers;
            public uint PointerSize;
            public uint EventsLost;         // 0x30
            public uint CpuSpeedInMHz;
            public IntPtr LoggerName;	// string, but not CoTaskMemAlloc'd
            public IntPtr LogFileName;	// string, but not CoTaskMemAlloc'd
            public TIME_ZONE_INFORMATION TimeZone;   // 0x40         0xac size
            public long BootTime;
            public long PerfFreq;
            public long StartTime;
            public uint ReservedFlags;
            public uint BuffersLost;        // 0x10C?        
        }

        /// <summary>
        ///	EVENT_TRACE_LOGFILEW Main struct passed to OpenTrace() to be filled in.
        /// It represents the collection of ETW events as a whole.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct EVENT_TRACE_LOGFILEW
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string LogFileName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string LoggerName;
            public Int64 CurrentTime;
            public uint BuffersRead;
            public uint LogFileMode;
            // EVENT_TRACE for the current event.  Nulled-out when we are opening files.
            // [FieldOffset(0x18)] 
            public EVENT_TRACE CurrentEvent;
            // [FieldOffset(0x70)]
            public TRACE_LOGFILE_HEADER LogfileHeader;
            // callback before each buffer is read
            // [FieldOffset(0x180)]
            public EventTraceBufferCallback BufferCallback;
            public Int32 BufferSize;
            public Int32 Filled;
            public Int32 EventsLost;
            // callback for every 'event', each line of the trace moduleFile
            // [FieldOffset(0x190)]
            public EventTraceEventCallback EventCallback;
            public Int32 IsKernelTrace;     // TRUE for kernel logfile
            public IntPtr Context;	        // reserved for internal use
        }
        #endregion // ETW tracing types

        #region ETW tracing types from evntcons.h

        internal const ushort EVENT_HEADER_FLAG_32_BIT_HEADER = 0x0020;
        internal const ushort EVENT_HEADER_FLAG_64_BIT_HEADER = 0x0040;
        internal const ushort EVENT_HEADER_FLAG_CLASSIC_HEADER = 0x0100;

        /// <summary>
        ///	EventTraceHeader and structure used to define EVENT_TRACE_LOGFILE (the main packet on Vista and above)
        ///	I have simplified from the original struct definitions.  I have
        ///	omitted alternate union-fields which we don't use.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENT_HEADER
        {
            public ushort Size;
            public ushort HeaderType;
            public ushort Flags;            // offset: 0x4
            public ushort EventProperty;
            public int ThreadId;            // offset: 0x8
            public int ProcessId;           // offset: 0xc
            public long TimeStamp;          // offset: 0x10
            public Guid ProviderId;         // offset: 0x18
            public ushort Id;               // offset: 0x28
            public byte Version;            // offset: 0x2a
            public byte Channel;
            public byte Level;              // offset: 0x2c
            public byte Opcode;
            public ushort Task;
            public ulong Keyword;
            public int KernelTime;         // offset: 0x38
            public int UserTime;           // offset: 0x3C
            public Guid ActivityId;
        }

        /// <summary>
        ///	Provides context information about the event
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct ETW_BUFFER_CONTEXT
        {
            public byte ProcessorNumber;
            public byte Alignment;
            public ushort LoggerId;
        }

        /// <summary>
        ///	Defines the layout of an event that ETW delivers
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENT_RECORD
        {
            public EVENT_HEADER EventHeader;            //  size: 80
            public ETW_BUFFER_CONTEXT BufferContext;    //  size: 4
            public ushort ExtendedDataCount;
            public ushort UserDataLength;               //  offset: 86
            public IntPtr ExtendedData;
            public IntPtr UserData;
            public IntPtr UserContext;
        }

        [StructLayout(LayoutKind.Explicit)]
        unsafe internal struct EVENT_FILTER_DESCRIPTOR
        {
            [FieldOffset(0)]
            public byte* Ptr;          // Data
            [FieldOffset(8)]
            public int Size;
            [FieldOffset(12)]
            public int Type;
        };

        #endregion

        #region ETW tracing functions
        //	TRACEHANDLE handle type is a ULONG64 in evntrace.h.  Use UInt64 here.
        [DllImport("advapi32.dll",
            EntryPoint = "OpenTraceW",
            CharSet = CharSet.Unicode,
            SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static UInt64 OpenTrace(
            [In][Out] ref EVENT_TRACE_LOGFILEW logfile);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static int ProcessTrace(
            [In] UInt64[] handleArray,
            [In] uint handleCount,
            [In] IntPtr StartTime,
            [In] IntPtr EndTime);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static int CloseTrace(
            [In] UInt64 traceHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static int QueryAllTraces(
            [In] IntPtr propertyArray,
            [In] int propertyArrayCount,
            [In][Out] ref int sessionCount);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static int StartTrace(
            [Out] out UInt64 sessionHandle,
            [In] string sessionName,
            [In][Out] IntPtr properties);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern int EnableTrace(
            [In] uint enable,
            [In] int enableFlag,
            [In] int enableLevel,
            [In] ref Guid controlGuid,
            [In] ulong sessionHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern int EnableTraceEx(
            [In] ref Guid ProviderId,
            [In] Guid* SourceId,
            [In] ulong TraceHandle,
            [In] int IsEnabled,
            [In] byte Level,
            [In] ulong MatchAnyKeyword,
            [In] ulong MatchAllKeyword,
            [In] uint EnableProperty,
            [In] EVENT_FILTER_DESCRIPTOR* filterData);


        [DllImport("advapi32.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern int ControlTrace(
            [In] UInt64 sessionHandle,
            [In] string sessionName,
            [In][Out] IntPtr properties,
            [In] uint controlCode);

        #endregion // ETW tracing functions

        #region ETW Tracing from KernelTraceControl.h (from XPERF distribution)

        /// <summary>
        /// Used in code:StartKernelTrace to indicate the kernel events that should have stack traces
        /// collected for them.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct STACK_TRACING_EVENT_ID
        {
            public Guid EventGuid;
            public byte Type;
            byte Reserved1;
            byte Reserved2;
            byte Reserved3;
            byte Reserved4;
            byte Reserved5;
            byte Reserved6;
            byte Reserved7;
        }

        [DllImport("KernelTraceControl.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static int StartKernelTrace(
            out UInt64 TraceHandle,
            IntPtr Properties,                                  // Actually a pointer to code:EVENT_TRACE_PROPERTIES
            STACK_TRACING_EVENT_ID* StackTracingEventIds,       // Actually an array of  code:STACK_TRACING_EVENT_ID
            int cStackTracingEventIds);

        [DllImport("KernelTraceControl.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurityAttribute]
        internal extern static int CreateMergedTraceFile(
            string wszMergedFileName,
            string[] wszTraceFileNames,
            int cTraceFileNames,
            EVENT_TRACE_MERGE_EXTENDED_DATA dwExtendedDataFlags);

        // Flags to save extended information to the ETW trace file
        [Flags]
        public enum EVENT_TRACE_MERGE_EXTENDED_DATA
        {   
            NONE            = 0x00,
            IMAGEID         = 0x01,
            BUILDINFO       = 0x02,
            VOLUME_MAPPING  = 0x04,
            WINSAT          = 0x08,
            EVENT_METADATA  = 0x10,
        }
        #endregion

        #region Security Entry Points

        internal static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        internal static uint STANDARD_RIGHTS_READ = 0x00020000;
        internal static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        internal static uint TOKEN_DUPLICATE = 0x0002;
        internal static uint TOKEN_IMPERSONATE = 0x0004;
        internal static uint TOKEN_QUERY = 0x0008;
        internal static uint TOKEN_QUERY_SOURCE = 0x0010;
        internal static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        internal static uint TOKEN_ADJUST_GROUPS = 0x0040;
        internal static uint TOKEN_ADJUST_SESSIONID = 0x0100;
        internal static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        [DllImport("advapi32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(
            [In] IntPtr ProcessHandle,
            [In] UInt32 DesiredAccess,
            [Out] out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(
           [In] IntPtr TokenHandle,
           [In, MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
           [In] ref TOKEN_PRIVILEGES NewState,
           [In] UInt32 BufferLength,
            // [Out] out TOKEN_PRIVILEGES PreviousState,
           [In] IntPtr NullParam,
           [In] IntPtr ReturnLength);

        // I explicitly DONT caputure GetLastError information on this call because it is often used to
        // clean up and it is cleaner if GetLastError still points at the orginal error, and not the failure
        // in CloseHandle.  If we ever care about exact errors of CloseHandle, we can make another entry
        // point 
        [DllImport("kernel32.dll"), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern bool CloseHandle([In] IntPtr hHandle);

        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_PRIVILEGES      // taylored for the case where you only have 1. 
        {
            public UInt32 PrivilegeCount;
            public LUID Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        // Constants for the Attributes field
        internal const UInt32 SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        internal const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;
        internal const UInt32 SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        // Constants for the Luid field 
        internal const uint SE_SYSTEM_PROFILE_PRIVILEGE = 11;

        #endregion

        [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern void ZeroMemory(IntPtr handle, uint length);

        [DllImport("kernel32.dll", EntryPoint = "LocalAlloc"), SuppressUnmanagedCodeSecurityAttribute]
        private static extern IntPtr LocalAlloc(int uFlags, IntPtr sizeBytes);

        [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        private static extern IntPtr LocalFree(IntPtr handle);

        // TODO what is this for?
        internal static int GetHRForLastWin32Error()
        {
            int dwLastError = Marshal.GetLastWin32Error();
            if ((dwLastError & 0x80000000) == 0x80000000)
                return dwLastError;
            else
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        }

        internal static IntPtr AllocHGlobal(int sizeBytes)
        {
            IntPtr ret = LocalAlloc(0, (IntPtr)sizeBytes);
            return ret;
        }

        internal static void FreeHGlobal(IntPtr hglobal)
        {
            if (IntPtr.Zero != LocalFree(hglobal))
                Marshal.ThrowExceptionForHR(GetHRForLastWin32Error());
        }

        /// <summary>
        /// The Sample based profiling requires the SystemProfilePrivilege, This code turns it on.   
        /// </summary>
        internal static void SetSystemProfilePrivilege()
        {
#if !Silverlight
            Process process = Process.GetCurrentProcess();
            IntPtr tokenHandle = IntPtr.Zero;
            bool success = OpenProcessToken(process.Handle, TOKEN_ADJUST_PRIVILEGES, out tokenHandle);
            if (!success)
                throw new Win32Exception();
            GC.KeepAlive(process);                      // TODO get on SafeHandles. 

            TOKEN_PRIVILEGES privileges = new TOKEN_PRIVILEGES();
            privileges.PrivilegeCount = 1;
            privileges.Luid.LowPart = SE_SYSTEM_PROFILE_PRIVILEGE;
            privileges.Attributes = SE_PRIVILEGE_ENABLED;

            success = AdjustTokenPrivileges(tokenHandle, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero);
            CloseHandle(tokenHandle);
            if (!success)
                throw new Win32Exception();
#endif
        }

        // TODO why do we need this? 
        internal static int GetHRFromWin32(int dwErr)
        {
            return (int)((0 != dwErr) ? (0x80070000 | ((uint)dwErr & 0xffff)) : 0);
        }

    } // end class
    #endregion
}
