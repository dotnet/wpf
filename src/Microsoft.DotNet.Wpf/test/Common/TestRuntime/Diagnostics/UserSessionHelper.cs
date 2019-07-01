// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;

namespace Microsoft.Test.Diagnostics
{
    internal static class UserSessionHelper
    {
        #region Public Members

        public static bool ConnectInternal(int sessionId)
        {
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection("TestRuntime", true);

            return connection.UserSessionConnect(sessionId, "Console");
        }

        public static bool DisconnectInternal(int sessionId)
        {
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection("TestRuntime", true);

            return connection.UserSessionDisconnect(sessionId);
        }

        public static int GetConsoleSessionIdInternal()
        {
            ExecutionServiceClient executionServiceClient = ExecutionServiceClient.Connect();
            ExecutionServiceClientConnection connection = executionServiceClient.BeginConnection("TestRuntime", true);

            return connection.UserSessionGetConsoleSessionId();
        }

        #endregion

        #region Internal Members

        internal static bool TryUserSessionConnect(int userSessionId, string targetUserSessionName)
        {
            int targetUserSessionId;

            if (!LogonIdFromWinStationName(IntPtr.Zero, targetUserSessionName, out targetUserSessionId))
                throw new Win32Exception();

            return TryUserSessionConnect(userSessionId, targetUserSessionId);
        }

        internal static bool TryUserSessionConnect(int sessionId, int targetSessionId)
        {
            if (!IsValidSession(sessionId))
                return false;

            if (!IsValidSession(targetSessionId))
                return false;

            if (!WaitForSessionState(targetSessionId, false, new TimeSpan(0,0,5), new TimeSpan(0,5,0), WTS_CONNECTSTATE_CLASS.WTSActive, WTS_CONNECTSTATE_CLASS.WTSConnected))
                return false;

            if (!WinStationConnect(IntPtr.Zero, sessionId, targetSessionId, string.Empty, true))
                throw new Win32Exception();

            return true;
            
        }

        internal static bool TryUserSessionDisconnect(int sessionId)
        {
            if (!IsValidSession(sessionId))
                return false;
                
            if (!WinStationDisconnect(IntPtr.Zero, sessionId, true))
                throw new Win32Exception();

            //It's possible for the disconnect to take at least 30 seconds and up to 2 minutes
            //This is especially true when disconnecting the console
            if (!WaitForSessionState(sessionId, false, new TimeSpan(0, 0, 5), new TimeSpan(0, 2, 0), WTS_CONNECTSTATE_CLASS.WTSDisconnected))
                return false;

            return true;
        }

        internal static bool TryUserSessionLogon(int sessionId, string username, string domain, string password)
        {
            string loginAgentFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LoginAgent.exe");
            string loginAgentArgument = domain + @"\" + username + " " + password;

            if (!File.Exists(loginAgentFilename))
                return false;

            if (!IsValidSession(sessionId))
                return false;

            //We need a login prompt in order to log in
            if (!IsLoginSession(sessionId))
                return false;

            Process loginProcess = ProcessHelper.CreateProcessAsSystem(new ProcessStartInfo(loginAgentFilename, loginAgentArgument), sessionId);
            if (!loginProcess.WaitForExit(20000))
                return false;

            //Need to wait for session to update itself while we log in
            //It's possible the session id used to log in will disappear when we log in, as
            //the system re-connects automatically a disconnected session. In that case, the disconnected
            //session becomes the active session id
            for (int tryCount = 0; tryCount < 5; tryCount++)
            {
                WTS_SESSION_INFO loginSession = GetUserSession(GetUserConsoleSession());

                if (loginSession.SessionId < 0)
                    continue;

                if (!WaitForSessionState(loginSession.SessionId, false, new TimeSpan(0, 0, 5), new TimeSpan(0, 1, 0), WTS_CONNECTSTATE_CLASS.WTSActive))
                    continue;

                UserInfo userInfo = new UserInfo();
                if (GetUserSessionUserInfo(loginSession.SessionId, out userInfo) &&
                    string.Equals(userInfo.Username, username, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(userInfo.Domain, domain, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        internal static bool TryUserSessionLogoff(int sessionId)
        {
            if (!IsValidSession(sessionId))
                return false;

            if (!WTSLogoffSession(IntPtr.Zero, sessionId, true))
                throw new Win32Exception();

            return true;
        }

        internal static bool TryUserSessionLogonToConsole(string username, string domain, string password)
        {
            int consoleId = GetUserConsoleSession();
            UserInfo currentUserInfo = new UserInfo();
            currentUserInfo.Username = username;
            currentUserInfo.Domain = domain;
            currentUserInfo.Password = password;

            if (!IsLoginSession(consoleId))
            {
                //If the currently logged in user is the same as the one we are trying to log into, we don't need to log in
                UserInfo consoleUserInfo;

                if (GetUserSessionUserInfo(consoleId, out consoleUserInfo))
                    currentUserInfo.LLUName = consoleUserInfo.LLUName;

                if (currentUserInfo.Equals(consoleUserInfo))
                    return true;

                TryUserSessionDisconnect(consoleId);
                consoleId = GetUserConsoleSession();
            }

            return TryUserSessionLogon(consoleId, username, domain, password);
        }

        internal static bool TryUserSessionActivateConsole(int consoleSessionId)
        {
            //This will need to be modified for fast user switching
            //since you may want to connect to the login ui session, which you
            //can't currently do
            bool consoleSessionActive = true;

            if (consoleSessionId < 0)
                throw new ArgumentException("Invalid SesssionId", "consoleSessionId");

            //If the session is locked then detach the session
            //Connecting from A to A requires us to disconnect first
            if (GetUserConsoleSession() == consoleSessionId && UserConsoleLocked)
                consoleSessionActive |= TryUserSessionDisconnect(GetUserConsoleSession());

            if (GetUserConsoleSession() != consoleSessionId)
                consoleSessionActive |= TryUserSessionConnect(consoleSessionId, "Console");

            return consoleSessionActive;
        }

        internal static void UserSessionLockConsole()
        {
            if (!LockWorkStation())
                throw new Win32Exception();

            //Ensure the UI is locked before proceeding - this is important because the unlocking code could
            //fail due to timing problems if it runs before the locking operation completed
            if (!WaitForSessionState(GetUserConsoleSession(), true, new TimeSpan(0, 0, 5), new TimeSpan(0, 2, 0), WTS_CONNECTSTATE_CLASS.WTSActive))
                throw new InvalidOperationException("Could not lock the UI");

        }

        internal static int GetUserConsoleSession()
        {
            int userConsoleSessionId = -1;

            //TODO: WE should poll or use events in order to know if the new logon
            //session is connected
            int tries = 5;
            while (tries > 0)
            {
                userConsoleSessionId = WTSGetActiveConsoleSessionId();

                if (userConsoleSessionId >= 0)
                    break;
                
                Thread.Sleep(2000);
                tries--;
            }

            return userConsoleSessionId;
        }

        internal static int GetFirstActiveUserSession()
        {
            // This returns the session Id of the first active user session
            // It should therefore fail if no user is currently logged in
            int sessionId = -1;
            List<WTS_SESSION_INFO> sessions = GetUserSessions();

            foreach (WTS_SESSION_INFO session in sessions)
            {
                if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                {
                    sessionId = session.SessionId;
                    break;
                }
            }

            return sessionId;
        }

        internal static bool UserConsoleLocked
        {
            get { return IsLoginSession(GetUserConsoleSession()); }
        }

        internal static bool IsLoginSession(int sessionId)
        {
            //In Vista: If the 'LogonUI.exe' process is running in the console session,
            //we can assume the console is locked
            //In Xp: the process is called "WinLogon.exe"
            string loginProcessName = "LogonUI";
            Process[] loginProcesses = Process.GetProcessesByName(loginProcessName);

            foreach (Process loginProcess in loginProcesses)
            {
                if (loginProcess.SessionId == sessionId)
                    return true;
            }

            return false;
        }

        internal static bool IsValidSession(int sessionId)
        {
            if (sessionId < 0)
                return false;

            WTS_SESSION_INFO session = GetUserSession(sessionId);
            if (session.SessionId == sessionId)
                return true;

            return false;
        }

        internal static string GetUserSessionQueryInfo(int sessionId, WTS_INFO_CLASS info)
        {
            IntPtr buffer = IntPtr.Zero;
            uint bytesReturned;
            string queryInfo = null;

            if (sessionId < 0)
                throw new ArgumentOutOfRangeException("sessionId");

            try
            {
                if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, info, out buffer, out bytesReturned))
                    throw new Win32Exception();

                queryInfo = Marshal.PtrToStringAuto(buffer);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    WTSFreeMemory(buffer);
                    buffer = IntPtr.Zero;
                }
            }

            return queryInfo;
        }

        internal static bool GetUserSessionUserInfo(int sessionId, out UserInfo userInfo)
        {
            userInfo = new UserInfo();

            string username = GetUserSessionQueryInfo(sessionId, WTS_INFO_CLASS.WTSUserName);
            if (String.IsNullOrEmpty(username))
                return false;

            string domain = GetUserSessionQueryInfo(sessionId, WTS_INFO_CLASS.WTSDomainName);

            //if (WttHelper.TryGetUserInfo(username, domain, out userInfo))
            //    return true;

            //We failed to get the account info through WTT
            //This should work when running with Piper. Need to clarify What happens if the auto Admin
            //account is not the current account. We currently assume all other accounts have the same password.
            string password = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "DefaultPassword", null) as string;
            if (!String.IsNullOrEmpty(password))
            {
                userInfo.Username = username;
                userInfo.Domain = domain;
                userInfo.Password = password;
                userInfo.LLUName = null;
                return true;
            }

            return false;
        }        

        internal static List<WTS_SESSION_INFO> GetUserSessions(params WTS_CONNECTSTATE_CLASS[] connectionClasses)
        {
            IntPtr buffer = IntPtr.Zero;
            WTS_SESSION_INFO[] sessionInfo = null;

            List<WTS_SESSION_INFO> sessions = new List<WTS_SESSION_INFO>();
            Dictionary<WTS_CONNECTSTATE_CLASS, object> connectionClassesHash = new Dictionary<WTS_CONNECTSTATE_CLASS, object>();

            int count = 0; //number of sessions

            // Fill buffer and termine count of sessions
            if (WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref buffer, ref count))
            {
                // Marshal to a structure array here. Create the array first.
                sessionInfo = new WTS_SESSION_INFO[count];

                // Cycle through and copy the array over.
                for (int index = 0; index < count; index++)
                    // Marshal the value over.
                    sessionInfo[index] = (WTS_SESSION_INFO)Marshal.PtrToStructure(new IntPtr(buffer.ToInt64() + (Marshal.SizeOf(typeof(WTS_SESSION_INFO)) * index)), typeof(WTS_SESSION_INFO));
            }

            // Close the buffer.
            WTSFreeMemory(buffer);

            if (sessionInfo == null || sessionInfo.Length == 0)
                return sessions;
            else if (connectionClasses == null || connectionClasses.Length == 0)
                sessions.AddRange(sessionInfo);
            else
            {
                foreach (WTS_CONNECTSTATE_CLASS connectionClass in connectionClasses)
                    connectionClassesHash[connectionClass] = null;

                foreach (WTS_SESSION_INFO session in sessionInfo)
                    if (connectionClassesHash.ContainsKey(session.State))
                        sessions.Add(session);
            }

            return sessions;
        }

        //Retrieves the SID of a user, given a username and domain
        internal static string TryGetUserSid(string username, string domain)
        {
            NTAccount userAccount = new NTAccount(domain, username);
            string userSid = null;

            try
            {
                SecurityIdentifier securityIdentifier = userAccount.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
                userSid = securityIdentifier.ToString();
            }
            catch (IdentityNotMappedException)
            {
                userSid = null;
            }

            return userSid;
        }

        #endregion

        #region Private Members

        private static WTS_SESSION_INFO GetUserSession(int sessionId)
        {
            List<WTS_SESSION_INFO> sessions = GetUserSessions();
            foreach (WTS_SESSION_INFO session in sessions)
                if (session.SessionId == sessionId)
                    return session;

            return new WTS_SESSION_INFO();
        }        

        private static bool WaitForSessionState(int sessionId, bool waitForLock, TimeSpan interval, TimeSpan totalWait, params WTS_CONNECTSTATE_CLASS[] connectionStateFlags)
        {
            //We use a params list instead of flag operators because WTSActive maps to 0
            //This makes the operator '&' unusable (0 & ?? = 0)
            List<WTS_CONNECTSTATE_CLASS> connectionStates = new List<WTS_CONNECTSTATE_CLASS>(connectionStateFlags);

            //Poll to ensure the new session is ready for 4 minutes
            if (interval > totalWait)
                interval = totalWait;

            while (totalWait.Ticks > 0)
            {
                //Ensure the target session is ready
                WTS_SESSION_INFO session = GetUserSession(sessionId);

                //The target session no longer exists (i.e. someone logs on, or kills the target session)
                if (session.SessionId < 0)
                    return false;

                //Ensure the target session is ready to be connected
                if (session.SessionId == sessionId && connectionStates.Contains(session.State))
                {
                    if (!waitForLock)
                        return true;

                    if (UserConsoleLocked)
                        return true;
                }

                //Wait the interval period and adjust the remaining waiting time
                Thread.Sleep(Convert.ToInt32(interval.TotalMilliseconds));
                totalWait -= interval;
            }

            return false;
        }

        #endregion

        #region Unmanaged Interop

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, EntryPoint="WTSEnumerateSessionsW", SetLastError = true)]
        private static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError=true)]
        private static extern int WTSDisconnectSession(IntPtr hServer, int sessionId, bool wait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool wait);

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr WTSOpenServer(string serverName);
        
        [DllImport("wtsapi32.dll")]
        private static extern void WTSCloseServer(IntPtr hServer);
        
        [DllImport("winsta.dll", CharSet = CharSet.Auto, EntryPoint="WinStationConnectW", SetLastError = true)]
        private static extern bool WinStationConnect(IntPtr hServer, int logonId, int targetLogonId, string password, bool wait);

        [DllImport("winsta.dll", SetLastError = true)]
        private static extern bool WinStationDisconnect(IntPtr hServer, int logonId, bool wait);

        [DllImport("winsta.dll", CharSet = CharSet.Auto, EntryPoint = "LogonIdFromWinStationNameW", SetLastError = true)]
        private static extern bool LogonIdFromWinStationName(IntPtr hServer, string stationName, out int logonId);

        [DllImport("winsta.dll", CharSet = CharSet.Auto, EntryPoint = "WinStationNameFromLogonIdW", SetLastError = true)]
        private static extern bool WinStationNameFromLogonId(IntPtr hServer, int logonId, out string stationName);

        private const int WTS_CURRENT_SERVER_HANDLE = -1;

        [DllImport("Wtsapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "WTSQuerySessionInformationW", SetLastError = true)]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        #endregion
    }

    #region Unmanaged Interop

    internal enum WTS_INFO_CLASS
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class WTS_SESSION_INFO
    {
        public WTS_SESSION_INFO()
        {
            SessionId = -1;
        }
        public int SessionId;
        public string WinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }

    [Flags]
    internal enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    };

    #endregion
}
