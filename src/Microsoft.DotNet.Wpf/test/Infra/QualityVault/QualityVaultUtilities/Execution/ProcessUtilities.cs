// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Text;

namespace Microsoft.Test.Execution
{
    internal static class ProcessUtilities
    {

        /// <summary>
        /// Runs the specified command - Blocks until complete.
        /// Output goes to the logging system.
        /// Returns the exit code.
        /// </summary>
        internal static int Run(string command, string arguments)
        {
            return Run(command, arguments, ProcessWindowStyle.Hidden, true);
        }

        /// <summary>
        /// Runs the specified command - Blocks until complete.
        /// When using Execution Log, output goes to logging system. Otherwise, we pipe to console.
        /// Returns the exit code.
        /// </summary>
        internal static int Run(string command, string arguments, ProcessWindowStyle windowStyle, bool useExecutionLog)
        {
            ProcessStartInfo p = CreateStartInfo(command, arguments, windowStyle);
            Process proc = Process.Start(p);
            while (!proc.StandardOutput.EndOfStream)
            {
                if (useExecutionLog)
                {
                    ExecutionEventLog.RecordVerboseStatus(proc.StandardOutput.ReadLine());
                }
                else
                {
                    Console.WriteLine(proc.StandardOutput.ReadLine());
                }
            }
            return proc.ExitCode;
        }

        /// <summary>
        /// Creates a stock Process StartInfo
        /// </summary>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <param name="windowStyle"></param>
        /// <returns></returns>
        internal static ProcessStartInfo CreateStartInfo(string command, string arguments, ProcessWindowStyle windowStyle)
        {
            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = command;
            p.Arguments = arguments;
            p.WindowStyle = windowStyle;
            p.RedirectStandardOutput = true;
            p.UseShellExecute = false;
            return p;
        }

        internal static bool IsCriticalProcess(Process process)
        {
            return (process.Id == 4 || //System
                   process.Id == 0 || //Idle
                   process.ProcessName == "audiodg"); //DRM
        }

        internal static bool IsIDE(Process process)
        {
            return process.ProcessName == "devenv" || process.ProcessName == "code";
        }

        internal static bool IsKnownTestProcess(Process process)
        {
            return (process.ProcessName == "sti"); //sometimes it doesnt get picked up or killed the first time...
        }

        //Returns the SID of the user the process is running as,
        //otherwise, null if the process has exited or access is denied to the process
        internal static string GetProcessUserSid(Process process)
        {
            //If this is a well known process (specifically the System and Idle kernel processes)
            //Assume you are running as system.  This is also special cased by the OS.
            if (process.Id == 0 || process.Id == 4)
            {
                return new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Value;
            }

            SafeProcessHandle processToken = SafeProcessHandle.InvalidHandle;
            IntPtr tokenPtr = IntPtr.Zero;
            TOKEN_USER userToken = new TOKEN_USER();
            string userSid = null;
            IntPtr hProcess = IntPtr.Zero;

            try
            {
                //HACK: For Vista it is possible to get the user SID for any process when running with administrator privileges.
                //      however downlevel you have to be running as system to get the sid for processes that are running as
                //      LocalService or NetworkService.
                //      The correct thing to do here would be to delegate the call on downlevel systems to ExecutionService.
                //      Alternatively we can use the undocumented WinStationGetProcessSid API in winsta.dll which is used by
                //      task manager which does an RPC call to a system process.  Because it is undocumented I was not successful in
                //      getting the pinvoke interop working.
                //      As a workaround for downlevel OSes we assume that processes that we can not get the access token to are running
                //      as LocalService.  This should still allow application logic to differentiate between user, system, and service
                //      processes which is the primary goal of this API.

                int processAccessLevel = Environment.OSVersion.Version.Major > 6 ? PROCESS_QUERY_LIMITED_INFORMATION : PROCESS_QUERY_INFORMATION;

                hProcess = OpenProcess(processAccessLevel, false, process.Id);
                if (hProcess == IntPtr.Zero)
                {
                    //the process has exited or access to the process is denied
                    //(running less then admin or locked down process such as audiodg on Vista)
                    return null;
                }

                if (!OpenProcessToken(hProcess, TokenAccessLevels.Query, out processToken))
                {
                    //Access Denied - assume the process is running as LocalService (see hack comment above)
                    return new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null).Value;
                }

                //Get the size of the structure
                uint userTokenSize = 0;
                GetTokenInformation(processToken, TokenInformationClass.TokenUser, IntPtr.Zero, 0, out userTokenSize);

                //The call above will fail with the error "Data Size too small", but will return the right size
                if (userTokenSize == 0)
                    throw new Win32Exception();

                //Retrieve the token data
                tokenPtr = Marshal.AllocHGlobal((int)userTokenSize);
                if (!GetTokenInformation(processToken, TokenInformationClass.TokenUser, tokenPtr, userTokenSize, out userTokenSize))
                    throw new Win32Exception();

                //Marshall the pointer to the structure
                Marshal.PtrToStructure(tokenPtr, userToken);
                //Retrieve the sid
                ConvertSidToStringSid(userToken.User.Sid, out userSid);
            }
            finally
            {
                //Free used resources
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);

                Marshal.FreeHGlobal(tokenPtr);
                processToken.Close();
            }

            return userSid;
        }


        #region Unmanaged Interop wrappers from old infra - TODO: Most of this can safely be pruned out as unused code.

        [DllImport("LuaInterop.dll", EntryPoint = "LUA_GetElevatedToken", SetLastError = true)]
        private static extern int LUAGetElevatedToken(SafeProcessHandle hToken, out SafeProcessHandle phElevatedToken);

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(
                                      SafeProcessHandle hExistingToken,
                                      TokenAccessLevels dwDesiredAccess,
                                      SECURITY_ATTRIBUTES lpTokenAttributes,
                                      TokenImpersonationLevel ImpersonationLevel,
                                      TokenType TokenType,
                                      out SafeProcessHandle phNewToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(
            SafeProcessHandle TokenHandle,
            TokenInformationClass TokenInformationClass,
            IntPtr TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern bool SetTokenInformation(
                                    SafeProcessHandle TokenHandle,
                                    TokenInformationClass TokenInformationClass,
                                    byte[] TokenInformation,
                                    int TokenInformationLength);

        [DllImport("Advapi32.dll", EntryPoint = "SetTokenInformation", SetLastError = true)]
        private static extern bool SetTokenInformationStruct(
                                    SafeProcessHandle TokenHandle,
                                    TokenInformationClass TokenInformationClass,
                                    IntPtr TokenInformation,
                                    int TokenInformationLength);

        [DllImport("Advapi32.dll", EntryPoint = "ConvertStringSidToSidW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ConvertStringSidToSid(string sid, out IntPtr psid);

        [DllImport("Advapi32.dll", EntryPoint = "ConvertSidToStringSidW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ConvertSidToStringSid(IntPtr psid, out string sid);

        [DllImport("Advapi32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern int GetLengthSid(IntPtr pSid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr attr, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateNamedPipe(string lpName, uint dwOpenMode, uint dwPipeMode, uint nMaxInstances, int nOutBufferSize, int nInBufferSize, uint nDefaultTimeOut, SECURITY_ATTRIBUTES pipeSecurityDescriptor);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetStdHandle(int whichHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateProcess([MarshalAs(UnmanagedType.LPTStr)] string lpApplicationName, StringBuilder lpCommandLine, SECURITY_ATTRIBUTES lpProcessAttributes, SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, [MarshalAs(UnmanagedType.LPTStr)] string lpCurrentDirectory, STARTUPINFO lpStartupInfo, PROCESS_INFORMATION lpProcessInformation);

        [SuppressUnmanagedCodeSecurity, DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateProcessAsUser(SafeProcessHandle hToken, string lpApplicationName, StringBuilder lpCommandLine, SECURITY_ATTRIBUTES lpProcessAttributes, SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, STARTUPINFO lpStartupInfo, PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, TokenAccessLevels DesiredAccess, out SafeProcessHandle TokenHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern bool AdjustTokenPrivileges(
            [In]     SafeProcessHandle TokenHandle,
            [In]     bool DisableAllPrivileges,
            [In]     ref TOKEN_PRIVILEGE NewState,
            [In]     uint BufferLength,
            [In, Out] ref TOKEN_PRIVILEGE PreviousState,
            [In, Out] ref uint ReturnLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WTSQueryUserToken(int sessionId, ref SafeProcessHandle userToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetConsoleOutputCP();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetConsoleCP();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr OpenThread(int desiredAccess, bool inheritHandle, int threadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int ResumeThread(IntPtr hThread);

        private sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            internal SafeProcessHandle()
                : base(true)
            {
            }

            internal SafeProcessHandle(IntPtr h)
                : base(true)
            {
                base.handle = h;
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(base.handle);
            }

            internal static SafeProcessHandle InvalidHandle
            {
                get { return new SafeProcessHandle(IntPtr.Zero); }
            }

        }

        private enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass  // MaxTokenInfoClass should always be the last enum
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LUID
        {
            internal uint LowPart;
            internal uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TOKEN_PRIVILEGE
        {
            internal uint PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SECURITY_ATTRIBUTES
        {
            public SECURITY_ATTRIBUTES()
            {
                this.nLength = 12;
                this.lpSecurityDescriptor = IntPtr.Zero;
            }

            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;

        }

        [StructLayout(LayoutKind.Sequential)]
        private class SID_AND_ATTRIBUTES
        {
            public SID_AND_ATTRIBUTES()
            {
                this.Sid = IntPtr.Zero;
            }

            public IntPtr Sid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class TOKEN_USER
        {
            public TOKEN_USER()
            {
                this.User = new SID_AND_ATTRIBUTES();
            }
            public SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class TOKEN_MANDATORY_LABEL
        {
            public TOKEN_MANDATORY_LABEL()
            {
                this.Label = new SID_AND_ATTRIBUTES();
            }
            public SID_AND_ATTRIBUTES Label;
        }

        private enum TokenType
        {
            // Fields
            TokenImpersonation = 2,
            TokenPrimary = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        private class STARTUPINFO : IDisposable
        {
            public STARTUPINFO()
            {
                this.lpReserved = IntPtr.Zero;
                this.lpDesktop = null;
                this.lpTitle = IntPtr.Zero;
                this.lpReserved2 = IntPtr.Zero;
                this.hStdInput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdError = new SafeFileHandle(IntPtr.Zero, false);
                this.cb = Marshal.SizeOf(this);
            }

            public int cb;
            public IntPtr lpReserved;
            public string lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public SafeFileHandle hStdInput;
            public SafeFileHandle hStdOutput;
            public SafeFileHandle hStdError;

            public void Dispose()
            {
                if ((this.hStdInput != null) && !this.hStdInput.IsInvalid)
                {
                    this.hStdInput.Close();
                    this.hStdInput = null;
                }
                if ((this.hStdOutput != null) && !this.hStdOutput.IsInvalid)
                {
                    this.hStdOutput.Close();
                    this.hStdOutput = null;
                }
                if ((this.hStdError != null) && !this.hStdError.IsInvalid)
                {
                    this.hStdError.Close();
                    this.hStdError = null;
                }
            }


        }

        [StructLayout(LayoutKind.Sequential)]
        private class PROCESS_INFORMATION
        {
            public PROCESS_INFORMATION()
            {
                this.hProcess = IntPtr.Zero;
                this.hThread = IntPtr.Zero;
            }

            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;

        }

        [Flags]
        private enum TokenAccessLevels
        {
            AssignPrimary = 0x00000001,
            Duplicate = 0x00000002,
            Impersonate = 0x00000004,
            Query = 0x00000008,
            QuerySource = 0x00000010,
            AdjustPrivileges = 0x00000020,
            AdjustGroups = 0x00000040,
            AdjustDefault = 0x00000080,
            AdjustSessionId = 0x00000100,

            Read = 0x00020000 | Query,

            Write = 0x00020000 | AdjustPrivileges | AdjustGroups | AdjustDefault,

            AllAccess = 0x000F0000 |
                AssignPrimary |
                Duplicate |
                Impersonate |
                Query |
                QuerySource |
                AdjustPrivileges |
                AdjustGroups |
                AdjustDefault |
                AdjustSessionId,

            MaximumAllowed = 0x02000000
        }

        private const uint SE_PRIVILEGE_DISABLED = 0x00000000;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int SE_GROUP_INTEGRITY = 0x00000020;

        private const uint CREATE_ALWAYS = 2;
        private const uint CREATE_NEW = 1;
        private const long ERROR_BROKEN_PIPE = 0x6d;
        private const long ERROR_IO_PENDING = 0x3e5;
        private const long ERROR_NO_DATA = 0xe8;
        private const long ERROR_PIPE_BUSY = 0xe7;
        private const long ERROR_PIPE_CONNECTED = 0x217;
        private const long ERROR_PIPE_LISTENING = 0x218;
        private const long ERROR_PIPE_NOT_CONNECTED = 0xe9;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x80000;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x2000;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
        private const uint GENERIC_ALL = 0x10000000;
        private const uint GENERIC_EXECUTE = 0x20000000;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const int INVALID_HANDLE_VALUE = -1;
        private const string Kernel32 = "kernel32.dll";
        private const uint NMPWAIT_NOWAIT = 1;
        private const uint NMPWAIT_USE_DEFAULT_WAIT = 0;
        private const uint NMPWAIT_WAIT_FOREVER = uint.MaxValue;
        private const uint OPEN_ALWAYS = 4;
        private const uint OPEN_EXISTING = 3;
        private const uint PIPE_ACCESS_DUPLEX = 3;
        private const uint PIPE_ACCESS_INBOUND = 1;
        private const uint PIPE_ACCESS_OUTBOUND = 2;
        private const uint PIPE_CLIENT_END = 0;
        private const uint PIPE_NOWAIT = 1;
        private const uint PIPE_READMODE_BYTE = 0;
        private const uint PIPE_READMODE_MESSAGE = 2;
        private const uint PIPE_SERVER_END = 1;
        private const uint PIPE_TYPE_BYTE = 0;
        private const uint PIPE_TYPE_MESSAGE = 4;
        private const uint PIPE_UNLIMITED_INSTANCES = 0xff;
        private const uint PIPE_WAIT = 0;
        private const uint SECURITY_ANONYMOUS = 0;
        private const uint SECURITY_DELEGATION = 0x30000;
        private const uint SECURITY_IDENTIFICATION = 0x10000;
        private const uint SECURITY_IMPERSONATION = 0x20000;
        private const uint SECURITY_SQOS_PRESENT = 0x100000;
        private const uint TRUNCATE_EXISTING = 5;
        private const int HANDLE_FLAG_INHERIT = 0x00000001;

        private const int CREATE_SUSPENDED = 0x00000004;
        private const int CREATE_NO_WINDOW = 0x08000000;
        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

        private const int DUPLICATE_SAME_ACCESS = 2;
        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const int STARTF_USESTDHANDLES = 0x100;
        private const int THREAD_SUSPEND_RESUME = 0x0002;
        private const int ERROR_BAD_EXE_FORMAT = 0xc1;

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int SYNCHRONIZE = 0x00100000;

        private const string SDDL_ML_LOW = "LW";          // Low mandatory level
        private const string SDDL_ML_MEDIUM = "ME";       // Medium mandatory level
        private const string SDDL_ML_HIGH = "HI";         // High mandatory level
        private const string SDDL_ML_SYSTEM = "SI";       // System mandatory level

        #endregion
    }
}