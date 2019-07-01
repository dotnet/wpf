// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;

namespace Microsoft.Test.Diagnostics
{   
    // Provides a way to Impersonate current logged on user.
    // Some user mode changes (eg: adding KeyboardLayouts on XP) don�t seem to work 
    // even when using this API. It works fine with other changes like changing color profile
    public class ImpersonateUserHelper
    {
        /// <summary>
        /// Use this function with a using construct like below:
        /// using (ImpersonateUserHelper.ImpersonateUserContext())
        /// {
        ///     ...work which needs to be done while impersonating...
        /// }
        /// Tests using this API will fail when run over TS
        /// </summary>
        /// <returns></returns>
        public static IDisposable ImpersonateUserContext()
        {
            return new ImpersonateUser();
        }

        #region Private Types

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private class ImpersonateUser : IDisposable
        {
            internal ImpersonateUser()
            {
                string eventLogContent = "";                

                // Log current user identity
                eventLogContent += "Identity before impersonation: [" +
                    WindowsIdentity.GetCurrent().Name + "]" + Environment.NewLine;

                // get the user token to impersonate. 
                int userSessionId = UserSessionHelper.GetUserConsoleSession();
                if (!WTSQueryUserToken((uint)userSessionId, out userTokenHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception("WTSQueryUserToken failed with error code: [" + errorCode + "]" + 
                        Environment.NewLine + "This will also fail when test is run over TS");
                }                                                                          

                // do the actual impersonation
                if (!ImpersonateLoggedOnUser(userTokenHandle))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception("ImpersonateLoggedOnUser failed with error code: [" + errorCode + "]");
                }

                // Log impersonated user identity
                eventLogContent += "Identity after impersonation: [" +
                    WindowsIdentity.GetCurrent().Name + "] " + Environment.NewLine + 
                    "Session Id used for impersonation: [" +
                    userSessionId + "]" + Environment.NewLine;                
                
                // Services that change impersonation should call RegDisablePredefinedCache 
                // before using any of the predefined handles
                int isPredinedCacheDisabled;
                if (Environment.OSVersion.Version.Major < 6)
                {
                    isPredinedCacheDisabled = RegDisablePredefinedCache();                        
                }
                else
                {
                    // This API is introduced only from Vista.
                    isPredinedCacheDisabled = RegDisablePredefinedCacheEx();
                }
                // non-zero values indicates fail
                if (isPredinedCacheDisabled != 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    eventLogContent += "RegDisablePredefinedCache returned non-zero value. ErrorCode: [" +
                        errorCode + "]" + Environment.NewLine;                    
                }

                // log the events into event log
                EventLog eventLog = new EventLog();
                eventLog.Source = serviceName;
                eventLog.WriteEntry(eventLogContent, EventLogEntryType.Information, eventId); // random event Id
                eventLog.Close();
            }

            void IDisposable.Dispose()
            {
                string eventLogContent = "";
                
                // Revert the user impersonation
                if (!RevertToSelf())
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    CloseUserTokenHandle(); // Close the user token handle before we throw an exception.

                    // If revert fails we want to throw an exception.
                    throw new Win32Exception("RevertToSelf failed with error code: [" + errorCode + "]");
                }

                CloseUserTokenHandle();

                // log current user identity
                eventLogContent += "Identity after impersonation undo : [" + WindowsIdentity.GetCurrent().Name + "]" + Environment.NewLine;

                // log the events into event log
                EventLog eventLog = new EventLog();
                eventLog.Source = serviceName;
                eventLog.WriteEntry(eventLogContent, EventLogEntryType.Information, eventId);
                eventLog.Close();                
            }

            private void CloseUserTokenHandle()
            {
                if (userTokenHandle != IntPtr.Zero)
                {
                    CloseHandle(userTokenHandle);
                }
            }

            private bool LoadUserProfileWorker(int userSessionId)
            {
                // When a user logs on interactively, the system automatically loads the 
                // user's profile. If a service or an application impersonates a user, 
                // the system does not load the user's profile. Therefore, the service or 
                // application should load the user's profile with LoadUserProfile.
                profileInfo = new PROFILEINFO();
                profileInfo.dwSize = Marshal.SizeOf(profileInfo);
                profileInfo.lpUserName = UserSessionHelper.GetUserSessionQueryInfo(userSessionId,
                    WTS_INFO_CLASS.WTSUserName);
                return LoadUserProfile(userTokenHandle, ref profileInfo);
            }

            private IntPtr userTokenHandle;
            private PROFILEINFO profileInfo;
            private const int eventId = 700; //random event id for logging.
            private const string serviceName = "ExecutionService";            

            #region Unmanaged Interop     
            
            [StructLayout(LayoutKind.Sequential)]
            private struct PROFILEINFO
            {
                public int dwSize;
                public int dwFlags;
                [MarshalAs(UnmanagedType.LPTStr)]
                public String lpUserName;
                [MarshalAs(UnmanagedType.LPTStr)]
                public String lpProfilePath;
                [MarshalAs(UnmanagedType.LPTStr)]
                public String lpDefaultPath;
                [MarshalAs(UnmanagedType.LPTStr)]
                public String lpServerName;
                [MarshalAs(UnmanagedType.LPTStr)]
                public String lpPolicyPath;
                public IntPtr hProfile;
            }       

            [DllImport("wtsapi32.dll", SetLastError = true)]
            private static extern bool WTSQueryUserToken(UInt32 sessionId, out IntPtr token);

            [DllImport("advapi32.dll", SetLastError = true)]
            protected static extern bool ImpersonateLoggedOnUser(IntPtr token);

            [DllImport("advapi32.dll", SetLastError = true)]
            protected static extern bool RevertToSelf();

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private extern static bool CloseHandle(IntPtr handle);            

            [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern bool LoadUserProfile(IntPtr hToken, ref PROFILEINFO lpProfileInfo);

            [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern bool UnloadUserProfile(IntPtr hToken, IntPtr hProfile);

            [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern int RegDisablePredefinedCacheEx();

            [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern int RegDisablePredefinedCache();            

            #endregion
        }

        #endregion 
    }    
}