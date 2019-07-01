// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;
using System.Reflection;
using System.Security;
using System.Runtime.ConstrainedExecution;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Collections.Generic;

namespace Microsoft.Test.Diagnostics
{
    internal static class ProcessHelper
    {

        #region Public Members

        public const int DefaultRedirectionBufferSize = 4096;

        public static Process CreateProcessAsFirstActiveSessionUser(ProcessStartInfo startInfo) 
        {
            return CreateProcessAsFirstActiveSessionUser(startInfo, false);
        }

        public static Process CreateProcessAsFirstActiveSessionUser(ProcessStartInfo startInfo, bool runElevated)
        {
            int threadId;
            string stdOut, stdErr, stdIn;
            int processId = BeginCreateProcess(startInfo, true, null, false, runElevated, out threadId, out stdOut, out stdErr, out stdIn, DefaultRedirectionBufferSize);
            
            return EndCreateProcess(startInfo, processId, threadId, stdOut, stdErr, stdIn, DefaultRedirectionBufferSize, true);
        }        

        public static Process CreateProcessAsSystem(ProcessStartInfo startInfo, int sessionId)
        {
            int threadId;
            string stdOut, stdErr, stdIn;
            int processId = BeginCreateProcess(startInfo, false, sessionId, false, false, out threadId, out stdOut, out stdErr, out stdIn, DefaultRedirectionBufferSize);
            return EndCreateProcess(startInfo, processId, threadId, stdOut, stdErr, stdIn, DefaultRedirectionBufferSize, true);
        }


        #endregion


        #region Internal Members

        internal static int BeginCreateProcess(ProcessStartInfo startInfo, bool runAsUser, Nullable<int> sessionId, bool lowIntegrity, bool elevated, out int threadId, out string stdOutPipeName, out string stdErrPipeName, out string stdInPipeName, int redirectionBufferSize)
        {
            stdOutPipeName = null;
            stdErrPipeName = null;
            stdInPipeName = null;

            //HACK: use reflection to call the Process.BuildCommandLine executionService function
            //      This saves us from duplicating this code
            StringBuilder cmdLine = (StringBuilder)typeof(Process).InvokeMember("BuildCommandLine", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { startInfo.FileName, startInfo.Arguments });
            STARTUPINFO startupInfo = new STARTUPINFO();
            GCHandle EnvVarBlockGCPtr = new GCHandle();
            GCHandle SecurityDescriptorPtr = new GCHandle();
            SafeProcessHandle userToken = SafeProcessHandle.InvalidHandle;
            IntPtr sidPtr = IntPtr.Zero;
            IntPtr tokenMandatoryLabelPtr = IntPtr.Zero;

            bool processCreated = false;
            bool createProcessAsUser = runAsUser;  //Determine if we call CreateProcessAsUser instead of CreateProcess

            PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();

            try
            {
                //Create the pipes for redirected std handles
                if ((startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput) || startInfo.RedirectStandardError)
                {
                    if (startInfo.RedirectStandardInput)
                        stdInPipeName = CreateRandomNamedPipe(out startupInfo.hStdInput, redirectionBufferSize, FileAccess.Read);
                    else
                        startupInfo.hStdInput = new SafeFileHandle(GetStdHandle(STD_INPUT_HANDLE), false);

                    if (startInfo.RedirectStandardOutput)
                        stdOutPipeName = CreateRandomNamedPipe(out startupInfo.hStdOutput, redirectionBufferSize, FileAccess.Write);
                    else
                        startupInfo.hStdOutput = new SafeFileHandle(GetStdHandle(STD_OUTPUT_HANDLE), false);

                    if (startInfo.RedirectStandardError)
                        stdErrPipeName = CreateRandomNamedPipe(out startupInfo.hStdError, redirectionBufferSize, FileAccess.Write);
                    else
                        startupInfo.hStdError = new SafeFileHandle(GetStdHandle(STD_ERROR_HANDLE), false);
                    startupInfo.dwFlags = STARTF_USESTDHANDLES;
                }


                int creationFlags = CREATE_SUSPENDED;
                //Don't create a console window if not needed
                if (startInfo.CreateNoWindow)
                    creationFlags |= CREATE_NO_WINDOW;

                //get the environmentVaiableBlock
                IntPtr EnvVarBlockPtr = IntPtr.Zero;
                if (startInfo.EnvironmentVariables != null)
                {
                    bool unicode = false;
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        creationFlags |= CREATE_UNICODE_ENVIRONMENT;
                        unicode = true;
                    }
                    
                    //HACK: Get the evironment block array using the CLR private executionService function
                    //      This saves us from duplicating this code
                    Type ebType = typeof(Process).Assembly.GetType("System.Diagnostics.EnvironmentBlock");
                    byte[] envVarBuffer = (byte[])ebType.InvokeMember("ToByteArray", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { startInfo.EnvironmentVariables, unicode });
                    EnvVarBlockGCPtr = GCHandle.Alloc(envVarBuffer, GCHandleType.Pinned);
                    EnvVarBlockPtr = EnvVarBlockGCPtr.AddrOfPinnedObject();
                }

                //set the working directory
                string workDir = startInfo.WorkingDirectory;
                if (string.IsNullOrEmpty(workDir))
                    workDir = Environment.CurrentDirectory;



                //Get an active session
                if (runAsUser)
                {
                    if (!sessionId.HasValue)
                        sessionId = UserSessionHelper.GetFirstActiveUserSession();

                    //Change privileges so that we can query for the token info
                    ChangePrivilege("SeTcbPrivilege");

                    //Get the user token for the active session
                    if (!WTSQueryUserToken(sessionId.Value, ref userToken))
                    {
                        Win32Exception exception = new Win32Exception();
                        //HACK: If we are running in User Mode, this will fail - try to run BeginCreateProcess instead
                        if (exception.NativeErrorCode == 1300 || exception.NativeErrorCode == 1314)
                            runAsUser = false;
                        else
                            throw exception;
                    }
                    else if (Environment.OSVersion.Version.Major > 5) //Vista and greater wich supports LUA
                    {
                        string filename = GetExeFullPath(startInfo.FileName);
                        bool uiAccess = false; //Not currently used, but this can still be present in the Manifest.
                        ManifestHelper.GetManifestSecurityRequirements(filename, ref elevated, ref uiAccess);

                        if (elevated || lowIntegrity)
                        {
                            SafeProcessHandle elevatedToken;
                            int retVal = LUAGetElevatedToken(userToken, out elevatedToken);
                            if (retVal != 0)
                                throw new Win32Exception("GetElevatedToken returned error code " + retVal);
                            userToken.Close();
                            userToken = elevatedToken;
                        }

                        //You can only lower the IL of a full token
                        if (lowIntegrity)
                        {
                            if (!ConvertStringSidToSid(SDDL_ML_LOW, out sidPtr))
                                throw new Win32Exception("ConvertStringSidToSid failed");

                            TOKEN_MANDATORY_LABEL tokenMandatoryLabel = new TOKEN_MANDATORY_LABEL();
                            tokenMandatoryLabel.Label.Attributes = SE_GROUP_INTEGRITY;
                            tokenMandatoryLabel.Label.Sid = sidPtr;

                            int tokenMandatoryLabelSize = Marshal.SizeOf(tokenMandatoryLabel) + GetLengthSid(sidPtr);
                            tokenMandatoryLabelPtr = Marshal.AllocHGlobal(tokenMandatoryLabelSize);
                            Marshal.StructureToPtr(tokenMandatoryLabel, tokenMandatoryLabelPtr, true);

                            if (!SetTokenInformationStruct(userToken, TokenInformationClass.TokenIntegrityLevel, tokenMandatoryLabelPtr, tokenMandatoryLabelSize))
                                throw new Win32Exception("SetTokenInformation failed");
                        }
                    }
                }
                else if (!sessionId.HasValue) //Run as System in current session
                    sessionId = Process.GetCurrentProcess().SessionId;
                else if (sessionId != Process.GetCurrentProcess().SessionId)
                {
                    //Change privileges so that we can query for the token info
                    ChangePrivilege("SeTcbPrivilege");

                    //Run as System in a session other than the current session
                    SafeProcessHandle hToken;

                    if (!OpenProcessToken(GetCurrentProcess(), TokenAccessLevels.AllAccess, out hToken))
                        throw new Win32Exception();

                    if (!DuplicateTokenEx(hToken, TokenAccessLevels.MaximumAllowed, null, TokenImpersonationLevel.Anonymous, TokenType.TokenPrimary, out userToken))
                        throw new Win32Exception();

                    if (Environment.OSVersion.Version.Major > 5)
                    {
                        if (!SetTokenInformation(userToken, TokenInformationClass.TokenSessionId, BitConverter.GetBytes(sessionId.Value), BitConverter.GetBytes(sessionId.Value).Length))
                            throw new Win32Exception();
                    }

                    createProcessAsUser = true;
                }

                byte[] sdBuffer = CreateSecurityDescriptor(ref SecurityDescriptorPtr);

                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.nLength = Marshal.SizeOf(sa);
                sa.lpSecurityDescriptor = Marshal.UnsafeAddrOfPinnedArrayElement(sdBuffer, 0);

                if (createProcessAsUser)
                    processCreated = CreateProcessAsUser(userToken, null, cmdLine, sa, sa, true, creationFlags, EnvVarBlockPtr, workDir, startupInfo, processInfo);
                else
                    processCreated = CreateProcess(null, cmdLine, sa, sa, true, creationFlags, EnvVarBlockPtr, workDir, startupInfo, processInfo);

                if (!processCreated)
                {
                    int num1 = Marshal.GetLastWin32Error();
                    if (num1 == ERROR_BAD_EXE_FORMAT)
                        throw new Win32Exception(num1, "Invalid Application");
                    throw new Win32Exception(num1);
                }
                else
                {
                    threadId = processInfo.dwThreadId;
                    return processInfo.dwProcessId;
                }
            }
            finally
            {
                //free the environment block
                if (EnvVarBlockGCPtr.IsAllocated)
                    EnvVarBlockGCPtr.Free();

                //free the security decriptor
                if (SecurityDescriptorPtr.IsAllocated)
                    SecurityDescriptorPtr.Free();
                
                //Dispose of the startup info handles
                startupInfo.Dispose();

                if (!userToken.IsInvalid && !userToken.IsClosed)
                    userToken.Close();

                //Close the handled returned in the processInfo struct
                if (processInfo.hProcess != IntPtr.Zero)
                    CloseHandle(processInfo.hProcess);
                if (processInfo.hThread != IntPtr.Zero)
                    CloseHandle(processInfo.hThread);

                if (sidPtr != IntPtr.Zero)
                    CloseHandle(sidPtr);
                if (tokenMandatoryLabelPtr != IntPtr.Zero)
                    CloseHandle(tokenMandatoryLabelPtr);
            }
        }

        internal static Process EndCreateProcess(ProcessStartInfo startInfo, int processId,
            int threadId, string stdOut, string stdErr, string stdIn, int redirectionBufferSize, bool fullProcessAccess)
        {
            Process process = Process.GetProcessById(processId);

            //HACK: In a scenario where a LUA application starts an elevated process:
            //      Cant call waitfor exit unless you read the handle property otherwise you get an exception
            //      so we just read the process handle just in case someone uses WaitForExit
            //On downlevel (XP, Server), we always want full process access so we ignore the bool
            if (fullProcessAccess || Environment.OSVersion.Version.Major < 6)
            {
                IntPtr processHandle = process.Handle;
            }
            else
            {
                IntPtr processHandle = OpenProcess(SYNCHRONIZE, false, processId);

                if (processHandle == IntPtr.Zero)
                    throw new Win32Exception();

                Type safeProcessHandleType = process.GetType().GetField("m_processHandle", BindingFlags.NonPublic | BindingFlags.Instance).FieldType;
                object safeProcessHandle = Activator.CreateInstance(safeProcessHandleType, true);
                safeProcessHandle.GetType().GetMethod("InitialSetHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(safeProcessHandle, new object[] { processHandle });
                process.GetType().GetMethod("SetProcessHandle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(process, new object[] { safeProcessHandle });
            }

            //hook up the stream readers and writers for redirected standard io
            if (stdOut != null)
            {
                Encoding stdOutEncoding = (startInfo.StandardOutputEncoding != null) ? startInfo.StandardOutputEncoding : Encoding.GetEncoding(GetConsoleOutputCP());
                StreamReader standardOutput = new StreamReader(new FileStream(ProcessHelper.OpenPipeFile(stdOut, FileAccess.Read), FileAccess.Read, redirectionBufferSize, false), stdOutEncoding, true, redirectionBufferSize);
                typeof(Process).InvokeMember("standardOutput", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, process, new object[] { standardOutput });
            }
            if (stdErr != null)
            {
                Encoding stdErrEncoding = (startInfo.StandardErrorEncoding != null) ? startInfo.StandardErrorEncoding : Encoding.GetEncoding(GetConsoleOutputCP());
                StreamReader standardError = new StreamReader(new FileStream(ProcessHelper.OpenPipeFile(stdErr, FileAccess.Read), FileAccess.Read, redirectionBufferSize, false), stdErrEncoding, true, redirectionBufferSize);
                typeof(Process).InvokeMember("standardError", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, process, new object[] { standardError });
            }
            if (stdIn != null)
            {
                StreamWriter standardInput = new StreamWriter(new FileStream(ProcessHelper.OpenPipeFile(stdIn, FileAccess.Write), FileAccess.Write, redirectionBufferSize, false), Encoding.GetEncoding(GetConsoleCP()), redirectionBufferSize);
                standardInput.AutoFlush = true;
                typeof(Process).InvokeMember("standardInput", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, process, new object[] { standardInput });
            }

            if (!TryResumeThread(threadId))
            {
                ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();

                if (executionServiceClient == null)
                    throw new InvalidOperationException("Execution service is not installed to try to resume thread");

                executionServiceClient.ResumeThread(threadId);
            }

            return process;
        }

        internal static void ResumeThread(int threadId)
        {
            if (!TryResumeThread(threadId))
                throw new Win32Exception();
        }

        internal static bool TryResumeThread(int threadId)
        {
            IntPtr hThread = IntPtr.Zero;
            bool threadResumed = false;

            try
            {
                hThread = OpenThread(THREAD_SUSPEND_RESUME, false, threadId); // access

                //If we are returned an invalid pointer, it may be possible
                //to try again using ExecutionService
                if (hThread == IntPtr.Zero)
                    return false;

                if (ResumeThread(hThread) < 0)
                    return false;

                threadResumed = true;
            }
            finally
            {
                if (hThread != IntPtr.Zero)
                    CloseHandle(hThread);
            }

            return threadResumed;
        }

        internal static byte[] CreateSecurityDescriptor(ref GCHandle securityDescriptorPtr)
        {
            //Grant access to everyone
            SecurityIdentifier everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);
            dacl.AddAccess(AccessControlType.Allow, everyoneSid, -1, InheritanceFlags.None, PropagationFlags.None);
            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent | ControlFlags.OwnerDefaulted | ControlFlags.GroupDefaulted, null, null, null, dacl);

            //Create a pinned buffer for the Security Descriptor
            byte[] sdBuffer = new byte[sd.BinaryLength];
            sd.GetBinaryForm(sdBuffer, 0);
            securityDescriptorPtr = GCHandle.Alloc(sdBuffer, GCHandleType.Pinned);

            return sdBuffer;
        }


        //Returns the SID of the user the process is running as,
        //otherwise, null if the process has exited or access is denied to the process
        internal static string GetProcessUserSid(Process process)
        {
            //If this is a well known process (specifically the System and Idle kernal processes)
            //Assume you are running as system.  This is also special cased by the OS.
            if (process.Id == 0 || process.Id == 4)
                return new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Value;
            
            SafeProcessHandle processToken = SafeProcessHandle.InvalidHandle;
            IntPtr tokenPtr = IntPtr.Zero;
            TOKEN_USER userToken = new TOKEN_USER();
            string userSid = null;
            IntPtr hProcess = IntPtr.Zero;

            try
            {
                //      For Vista it is possible to get the user SID for any process when running with administrator privileges.
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

        //searches for the executable using the path following the same logic as BeginCreateProcess
        internal static string GetExeFullPath(string filename)
        {
            List<string> extensions = new List<string>();
            List<string> dirs = new List<string>();

            if (Path.IsPathRooted(filename))
            {
                //split the directory from the filename in case the extension is not specified
                dirs.Add(Path.GetDirectoryName(filename));
                filename = Path.GetFileName(filename);
            }
            else
            {
                //BeginCreateProcess search logic is:
                // - The current directory
                // - The windows directory
                // - The windows system directory
                // - The directories listed in the path environment variable
                dirs.Add(Environment.CurrentDirectory);
                dirs.Add(Environment.GetEnvironmentVariable("windir"));
                dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));
                string[] pathDirs = Environment.GetEnvironmentVariable("path").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                dirs.AddRange(pathDirs);
            }

            if (Path.GetExtension(filename) == string.Empty)
                extensions.AddRange(new string[] { ".exe", ".com", ".bat", ".cmd" });
            else
                extensions.Add(""); //since we always append the extension

            //search for the file
            foreach (string dir in dirs)
            {
                foreach (string extension in extensions)
                {
                    string fullPath = Path.Combine(dir, filename + extension);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            //if we cant find the file then throw the same exeption that would be thrown
            //if we tried to call createprocess on the file.  This gives callers a predictable
            //exception for this condition
            throw new Win32Exception(2); //File not found.
        }

        //Kills all the processes that are not part of the list passed as argument
        internal static List<string> EndNewUserProcesses(string userSid, List<int> processExclusionList)
        {
            // Used for sorting the process list.  Promote debuggers to the top of the list so that they 
            // must be killed before their debugees.
            List<string> debuggers = new List<string>((IEnumerable<string>)new string [] { "cdb", "ntsd", "windbg", "kd", "executionharnessdebugger" });

            // List of processes killed as part of cleanup.
            List<string> processesKilled = new List<string>();
            
            Process[] currentRunningProcesses = Process.GetProcesses();

            // Sort all debuggers to the beginning of the list... 
            int debuggerIndex = 0;
            for (int index = 0; index < currentRunningProcesses.Length; index++)
            {
                if (debuggers.Contains(currentRunningProcesses[index].ProcessName.ToLowerInvariant()))
                {
                    Process temp = currentRunningProcesses[debuggerIndex];
                    currentRunningProcesses[debuggerIndex] = currentRunningProcesses[index];
                    currentRunningProcesses[index] = temp;
                    debuggerIndex++;
                }
            }           

            for (int procIndex = 0; procIndex < currentRunningProcesses.Length; procIndex ++)
            {
                Process process = currentRunningProcesses[procIndex];

                //We check to see if the process has exited after verifying it is a user process because Process.HasExited may throw if we dont have access to the process handle
                if (!processExclusionList.Contains(process.Id) &&
                    userSid.Equals(ProcessHelper.GetProcessUserSid(process), StringComparison.InvariantCultureIgnoreCase) &&
                    !process.HasExited)
                {
                    try
                    {
                        string processName = process.ProcessName;
                        int processId = process.Id;
                        process.Kill();
                        process.WaitForExit();
                        processesKilled.Add(string.Format("{0}:{1}", processId, processName));
                    }
                    catch (InvalidOperationException)
                    {
                        //The process has already exited
                    }
                    catch (Win32Exception)
                    {
                        //The process has already exited or access is denied (Probably a Whidbey bug)
                    }
                }
            }

            return processesKilled;
        }

        internal static string CacheFile(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("File could not be found for registration", filename);

            //Cache the file, in case something happens to it
            string cachedFilename = Path.Combine(ProcessHelper.CreateTempFolder(), Path.GetFileName(filename));
            File.Copy(filename, cachedFilename, true);

            return cachedFilename;
        }

        internal static void DeleteCachedFile(string cachedFilename)
        {
            string cachedPath = Path.GetFullPath(cachedFilename);

            if (File.Exists(cachedFilename))
                File.Delete(cachedFilename);

            if (Directory.Exists(cachedPath))
            {
                try
                {
                    Directory.Delete(cachedPath);
                }
                catch (IOException)
                {
                    //Exception thrown if the folder is not empty
                    //This isn't really critical to the operation
                }
            }
        }

        internal static string CreateTempFolder()
        {
            string directory = Path.GetTempFileName();
            //Since GetTempFileName() creates a file we delete it and create a directory instead
            File.Delete(directory);
            Directory.CreateDirectory(directory);

            return directory;
        }

        #endregion


        #region Private Members
        

        private static void ChangePrivilege(string privilege)
        {
            //This method ensures that the process we are using allows us to query for a user token's info
            SafeProcessHandle processToken = SafeProcessHandle.InvalidHandle;
            LUID luid = new LUID();

            if (!OpenProcessToken(GetCurrentProcess(), TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges, out processToken))
            {
                throw new Win32Exception();
            }
            try
            {
                if (!LookupPrivilegeValue(null, privilege, out luid))
                {
                    throw new Win32Exception();
                }
                TOKEN_PRIVILEGE newState = new TOKEN_PRIVILEGE();
                newState.PrivilegeCount = 1;
                newState.Privilege.Luid = luid;
                newState.Privilege.Attributes = SE_PRIVILEGE_ENABLED;

                TOKEN_PRIVILEGE previousState = new TOKEN_PRIVILEGE();
                uint previousSize = 0;

                if (!AdjustTokenPrivileges(processToken, false, ref newState, (uint)Marshal.SizeOf(previousState), ref previousState, ref previousSize))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                //Close the token before exiting since we know it will be invalid
                if (!processToken.IsInvalid)
                    processToken.Close();
            }
        }

 
        private static string CreateRandomNamedPipe(out SafeFileHandle fileHandle, int bufferSize, FileAccess fileHandleMode)
        {
            string pipeName = @"\\.\pipe\" + Guid.NewGuid().ToString();

            GCHandle SecurityDescriptorPtr = new GCHandle();
            SECURITY_ATTRIBUTES attr = new SECURITY_ATTRIBUTES();
            
            //TODO: Implement proper ACLing of the pipe so that only the calling process can read it

            try
            {
                byte[] sdBuffer = CreateSecurityDescriptor(ref SecurityDescriptorPtr);
                attr.bInheritHandle = true;
                attr.nLength = (int)Marshal.SizeOf(attr);
                attr.lpSecurityDescriptor = Marshal.UnsafeAddrOfPinnedArrayElement(sdBuffer, 0);

                // Create the named pipe with the appropriate name
                fileHandle = CreateNamedPipe(pipeName,
                                          (fileHandleMode == FileAccess.Write ? PIPE_ACCESS_OUTBOUND : PIPE_ACCESS_INBOUND)
                                          | FILE_FLAG_FIRST_PIPE_INSTANCE,
                                          PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
                                          PIPE_UNLIMITED_INSTANCES,
                                          bufferSize,
                                          bufferSize,
                                          NMPWAIT_WAIT_FOREVER,
                                          attr);
            }
            finally
            {
                if (SecurityDescriptorPtr.IsAllocated)
                    SecurityDescriptorPtr.Free();
            }

            if (fileHandle.IsInvalid)
                throw new Win32Exception();

             return pipeName;
        }


        private static SafeFileHandle OpenPipeFile(string name, FileAccess accessMode)
        {
            // Invoke CreateFile with the pipeName to open a client side connection                
            SafeFileHandle fileHandle = CreateFile(name,
                                 (accessMode == FileAccess.Write) ? GENERIC_WRITE : GENERIC_READ,
                                  FILE_SHARE_READ | FILE_SHARE_WRITE,
                                 IntPtr.Zero,
                                 OPEN_EXISTING,
                                 FILE_ATTRIBUTE_NORMAL, //| FILE_FLAG_OVERLAPPED,
                                 IntPtr.Zero);

            if (fileHandle.IsInvalid)
                throw new Win32Exception();

            return fileHandle;
        }


        #endregion


        #region Unmanaged Interop

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
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetConsoleOutputCP();

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
        internal struct LUID
        {
            internal uint LowPart;
            internal uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_PRIVILEGE
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

        internal enum TokenType
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
