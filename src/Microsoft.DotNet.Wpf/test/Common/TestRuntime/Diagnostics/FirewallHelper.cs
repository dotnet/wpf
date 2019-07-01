// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Microsoft.Test.Diagnostics
{
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    internal static class FirewallHelper
    {
        #region Internal Members

        internal static FirewallRule GetFirewallRuleForApplication(string processExePath)
        {
            FirewallRule firewallRule = FirewallRule.None;

            if (!FirewallEnabled)
                return firewallRule;

            INetFwMgr fwManager = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
            INetFwAuthorizedApplications firewallApplications = fwManager.LocalPolicy.CurrentProfile.AuthorizedApplications;

            foreach (INetFwAuthorizedApplication firewallApplication in firewallApplications)
            {
                if (firewallApplication.ProcessImageFileName.ToLowerInvariant() == processExePath.ToLowerInvariant())
                {
                    firewallRule = firewallApplication.Enabled ? FirewallRule.Enabled : FirewallRule.Exist;                    
                    break;
                }
            }

            return firewallRule;
        }

        internal static void OpenPortInFirewall(int port, string description, string processExePath)
        {
            if (!FirewallEnabled)
                return;

            INetFwMgr fwManager = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

            //On Longhorn if there is this application is blocked, opening the port
            //won't do any good. Because this happens when you dismiss the firewall
            //dialog we make sure the app is not explicitly blocked in case someone
            //accidentily ran the app under LUA and dismissed the dialog.
            INetFwAuthorizedApplications apps = fwManager.LocalPolicy.CurrentProfile.AuthorizedApplications;
            string currentAppPath = processExePath;
            foreach (INetFwAuthorizedApplication app in apps)
            {
                if (app.ProcessImageFileName.ToLowerInvariant() == currentAppPath.ToLowerInvariant() && !app.Enabled)
                {
                    apps.Remove(currentAppPath);
                    break;
                }
            }

            //Make sure that the port is Logging Port is open in the Windows Firewall so that we dont get dialogs on this
            //NOTE: this will not work if the user is not an Admin
            INetFwOpenPort portExclusion = (INetFwOpenPort)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwOpenPort"));
            portExclusion.Port = port;
            portExclusion.Enabled = true;
            portExclusion.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
            portExclusion.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            portExclusion.Name = description;
            portExclusion.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;

            fwManager.LocalPolicy.CurrentProfile.GloballyOpenPorts.Add(portExclusion);

            //HACK: Verify because having the firewall dialog enabled
            //does not result in a UnauthorizedAccess exception, even if the operation fails
            bool added = false;
            INetFwOpenPorts openPorts = fwManager.LocalPolicy.CurrentProfile.GloballyOpenPorts;
            foreach (INetFwOpenPort openPort in openPorts)
            {
                if (openPort.Port == port)
                {
                    added = true;
                    break;
                }
            }

            if (!added)
                throw new UnauthorizedAccessException();
        }

        internal static void ClosePortInFirewall(int port)
        {
            if (!FirewallEnabled)
                return;

            //Remove the Logging Port from the Windows Firewall exception list
            //NOTE: this will not work if the user is not an Admin
            INetFwMgr fwManager = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
            fwManager.LocalPolicy.CurrentProfile.GloballyOpenPorts.Remove(port, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);

            //HACK: Verify because having the firewall dialog enabled
            //does not result in a UnauthorizedAccess exception, even if the operation fails
            bool removed = true;
            INetFwOpenPorts openPorts = fwManager.LocalPolicy.CurrentProfile.GloballyOpenPorts;
            foreach (INetFwOpenPort openPort in openPorts)
            {
                if (openPort.Port == port)
                {
                    removed = false;
                    break;
                }
            }

            if (!removed)
                throw new UnauthorizedAccessException();
        }

        internal static void EnableFirewallForExecutingApplication(string processExePath, bool removeRule)
        {
            if (!FirewallEnabled)
                return;

            //Remove the exception in the Windows Firewall for this application is made so that we dont get dialogs
            //NOTE: this will not work if the user is not an Admin
            INetFwMgr fwManager = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

            if (removeRule)
                fwManager.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(processExePath);

            //HACK: Verify because having the firewall dialog enabled
            //does not result in a UnauthorizedAccess exception, even if the operation fails
            bool foundRule = false;
            INetFwAuthorizedApplications authorizedApplications = fwManager.LocalPolicy.CurrentProfile.AuthorizedApplications;
            foreach (INetFwAuthorizedApplication authorizedApplication in authorizedApplications)
            {
                if (authorizedApplication.ProcessImageFileName.ToLowerInvariant().Equals(processExePath.ToLowerInvariant()))
                {
                    if (!removeRule)    //Disable the rule instead of removing it
                        authorizedApplication.Enabled = false;
                    foundRule = true;                    
                    break;
                }
            }

            if (removeRule && foundRule)
                throw new UnauthorizedAccessException();
        }

        internal static void DisableFirewallForExecutingApplication(string processExePath)
        {
            if (!FirewallEnabled)
                return;

            //Create an exception in the Windows Firewall for this application so that we dont get dialogs
            //NOTE: this will not work if the user is not an Admin

            //Create the Application Exception
            INetFwAuthorizedApplication appExclusion = (INetFwAuthorizedApplication)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));
            appExclusion.ProcessImageFileName = processExePath;
            appExclusion.Name = Path.GetFileNameWithoutExtension(processExePath);
            appExclusion.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            appExclusion.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
            appExclusion.Enabled = true;

            //Add the application exception
            INetFwMgr fwManager = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
            fwManager.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(appExclusion);

            //HACK: Verify the application was added successfully because having the firewall dialog enabled
            //does not result in a UnauthorizedAccess exception, even if the operation fails
            bool added = false;
            INetFwAuthorizedApplications authorizedApplications = fwManager.LocalPolicy.CurrentProfile.AuthorizedApplications;
            foreach (INetFwAuthorizedApplication authorizedApplication in authorizedApplications)
            {
                if (authorizedApplication.ProcessImageFileName.ToLowerInvariant().Equals(processExePath.ToLowerInvariant()))
                {
                    added = true;
                    break;
                }
            }

            if (!added)
                throw new UnauthorizedAccessException();
        }

        internal static string GetCurrentProcessExePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        #endregion

        #region Private Members

        private static string GetCurrentAssemblyExePath()
        {
            //TODO: This may not work if the hosting process is unmanaged
            //      such as PresentationHost.exe            
            //TODO: Is this really what's needed? Leaving in for backcompat, but it
            //may no longer be needed and GetCurrentProcessExePath() should be used instead.
            Assembly assembly = Assembly.GetEntryAssembly();
            return (assembly != null) ? assembly.Location : string.Empty;
        }

        private static bool FirewallEnabled
        {
            get
            {
                Type fwManagerType = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                if (fwManagerType != null)
                {
                    INetFwMgr fwManager = (INetFwMgr)Activator.CreateInstance(fwManagerType);
                    try
                    {
                        return fwManager.LocalPolicy.CurrentProfile.FirewallEnabled;
                    }
                    catch (COMException)
                    {
                        //if the firewall service is not running we get a COM exception when
                        //calling LocalPolicy.CurrentProfile.
                        return false;
                    }
                }
                return false;
            }
        }

        #endregion

        #region Windows Firewall API COM Interop

        [ComImport, TypeLibType(0x1040), Guid("B5E64FFA-C2C5-444E-A301-FB5E00018050")]
        private interface INetFwAuthorizedApplication
        {
            [DispId(1)]
            string Name { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] set; }
            [DispId(2)]
            string ProcessImageFileName { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] set; }
            [DispId(3)]
            NET_FW_IP_VERSION_ IpVersion { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] set; }
            [DispId(4)]
            NET_FW_SCOPE_ Scope { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
            [DispId(5)]
            string RemoteAddresses { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] set; }
            [DispId(6)]
            bool Enabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] set; }
        }

        [ComImport, Guid("644EFD52-CCF9-486C-97A2-39F352570B30"), TypeLibType(0x1040)]
        private interface INetFwAuthorizedApplications : IEnumerable
        {
            [DispId(1)]
            int Count { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)]
            void Add([In, MarshalAs(UnmanagedType.Interface)] INetFwAuthorizedApplication app);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)]
            void Remove([In, MarshalAs(UnmanagedType.BStr)] string imageFileName);
            [return: MarshalAs(UnmanagedType.Interface)]
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)]
            INetFwAuthorizedApplication Item([In, MarshalAs(UnmanagedType.BStr)] string imageFileName);
        }

        [ComImport, Guid("A6207B2E-7CDD-426A-951E-5E1CBC5AFEAD"), TypeLibType(0x1040)]
        private interface INetFwIcmpSettings
        {
            [DispId(1)]
            bool AllowOutboundDestinationUnreachable { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] set; }
            [DispId(2)]
            bool AllowRedirect { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] set; }
            [DispId(3)]
            bool AllowInboundEchoRequest { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] set; }
            [DispId(4)]
            bool AllowOutboundTimeExceeded { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
            [DispId(5)]
            bool AllowOutboundParameterProblem { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] set; }
            [DispId(6)]
            bool AllowOutboundSourceQuench { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] set; }
            [DispId(7)]
            bool AllowInboundRouterRequest { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] set; }
            [DispId(8)]
            bool AllowInboundTimestampRequest { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(8)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(8)] set; }
            [DispId(9)]
            bool AllowInboundMaskRequest { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(9)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(9)] set; }
            [DispId(10)]
            bool AllowOutboundPacketTooBig { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(10)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(10)] set; }
        }

        [ComImport, Guid("F7898AF5-CAC4-4632-A2EC-DA06E5111AF2"), TypeLibType(0x1040)]
        private interface INetFwMgr
        {
            [DispId(1)]
            INetFwPolicy LocalPolicy { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [DispId(2)]
            NET_FW_PROFILE_TYPE_ CurrentProfileType { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; }
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)]
            void RestoreDefaults();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)]
            void IsPortAllowed([In, MarshalAs(UnmanagedType.BStr)] string imageFileName, [In] NET_FW_IP_VERSION_ IpVersion, [In] int portNumber, [In, MarshalAs(UnmanagedType.BStr)] string localAddress, [In] NET_FW_IP_PROTOCOL_ ipProtocol, [MarshalAs(UnmanagedType.Struct)] out object allowed, [MarshalAs(UnmanagedType.Struct)] out object restricted);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)]
            void IsIcmpTypeAllowed([In] NET_FW_IP_VERSION_ IpVersion, [In, MarshalAs(UnmanagedType.BStr)] string localAddress, [In] byte Type, [MarshalAs(UnmanagedType.Struct)] out object allowed, [MarshalAs(UnmanagedType.Struct)] out object restricted);
        }

        [ComImport, Guid("4FBE7FE9-4AD1-4D70-BB77-66963016FD09"), TypeLibType(0x1040)]
        private interface INetFwMgrPrivate : INetFwMgr
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)]
            INetFwPolicy GetScratchPolicy([In, MarshalAs(UnmanagedType.BStr)] string policyKeyName);
        }

        [ComImport, Guid("E0483BA0-47FF-4D9C-A6D6-7741D0B195F7"), TypeLibType(0x1040)]
        private interface INetFwOpenPort
        {
            [DispId(1)]
            string Name { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] set; }
            [DispId(2)]
            NET_FW_IP_VERSION_ IpVersion { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] set; }
            [DispId(3)]
            NET_FW_IP_PROTOCOL_ Protocol { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] set; }
            [DispId(4)]
            int Port { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
            [DispId(5)]
            NET_FW_SCOPE_ Scope { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] set; }
            [DispId(6)]
            string RemoteAddresses { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] set; }
            [DispId(7)]
            bool Enabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] set; }
            [DispId(8)]
            bool BuiltIn { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(8)] get; }
        }

        [ComImport, Guid("C0E9D7FA-E07E-430A-B19A-090CE82D92E2"), TypeLibType(0x1040)]
        private interface INetFwOpenPorts : IEnumerable
        {
            [DispId(1)]
            int Count { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)]
            void Add([In, MarshalAs(UnmanagedType.Interface)] INetFwOpenPort Port);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)]
            void Remove([In] int portNumber, [In] NET_FW_IP_PROTOCOL_ ipProtocol);
            [return: MarshalAs(UnmanagedType.Interface)]
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)]
            INetFwOpenPort Item([In] int portNumber, [In] NET_FW_IP_PROTOCOL_ ipProtocol);
        }

        [ComImport, TypeLibType(0x1040), Guid("D46D2478-9AC9-4008-9DC7-5563CE5536CC")]
        private interface INetFwPolicy
        {
            [DispId(1)]
            INetFwProfile CurrentProfile { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [return: MarshalAs(UnmanagedType.Interface)]
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)]
            INetFwProfile GetProfileByType([In] NET_FW_PROFILE_TYPE_ profileType);
        }

        [ComImport, TypeLibType(0x1040), Guid("174A0DDA-E9F9-449D-993B-21AB667CA456")]
        private interface INetFwProfile
        {
            [DispId(1)]
            NET_FW_PROFILE_TYPE_ Type { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [DispId(2)]
            bool FirewallEnabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] set; }
            [DispId(3)]
            bool ExceptionsNotAllowed { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] set; }
            [DispId(4)]
            bool NotificationsDisabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
            [DispId(5)]
            bool UnicastResponsesToMulticastBroadcastDisabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] set; }
            [DispId(6)]
            INetFwRemoteAdminSettings RemoteAdminSettings { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] get; }
            [DispId(7)]
            INetFwIcmpSettings IcmpSettings { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] get; }
            [DispId(8)]
            INetFwOpenPorts GloballyOpenPorts { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(8)] get; }
            [DispId(9)]
            INetFwServices Services { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(9)] get; }
            [DispId(10)]
            INetFwAuthorizedApplications AuthorizedApplications { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(10)] get; }
        }

        [ComImport, TypeLibType(0x1040), Guid("D4BECDDF-6F73-4A83-B832-9C66874CD20E")]
        private interface INetFwRemoteAdminSettings
        {
            [DispId(1)]
            NET_FW_IP_VERSION_ IpVersion { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] set; }
            [DispId(2)]
            NET_FW_SCOPE_ Scope { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] set; }
            [DispId(3)]
            string RemoteAddresses { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] set; }
            [DispId(4)]
            bool Enabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
        }

        [ComImport, TypeLibType(0x1040), Guid("79FD57C8-908E-4A36-9888-D5B3F0A444CF")]
        private interface INetFwService
        {
            [DispId(1)]
            string Name { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [DispId(2)]
            NET_FW_SERVICE_TYPE_ Type { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)] get; }
            [DispId(3)]
            bool Customized { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(3)] get; }
            [DispId(4)]
            NET_FW_IP_VERSION_ IpVersion { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(4)] set; }
            [DispId(5)]
            NET_FW_SCOPE_ Scope { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(5)] set; }
            [DispId(6)]
            string RemoteAddresses { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] get; [param: In, MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(6)] set; }
            [DispId(7)]
            bool Enabled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(7)] set; }
            [DispId(8)]
            INetFwOpenPorts GloballyOpenPorts { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(8)] get; }
        }

        [ComImport, TypeLibType(0x1040), Guid("79649BB4-903E-421B-94C9-79848E79F6EE")]
        private interface INetFwServices : IEnumerable
        {
            [DispId(1)]
            int Count { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(1)] get; }
            [return: MarshalAs(UnmanagedType.Interface)]
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId(2)]
            INetFwService Item([In] NET_FW_SERVICE_TYPE_ svcType);
        }

        private enum NET_FW_IP_PROTOCOL_
        {
            // Fields
            NET_FW_IP_PROTOCOL_TCP = 6,
            NET_FW_IP_PROTOCOL_UDP = 0x11
        }

        private enum NET_FW_IP_VERSION_
        {
            NET_FW_IP_VERSION_V4,
            NET_FW_IP_VERSION_V6,
            NET_FW_IP_VERSION_ANY,
            NET_FW_IP_VERSION_MAX
        }

        private enum NET_FW_PROFILE_TYPE_
        {
            NET_FW_PROFILE_DOMAIN,
            NET_FW_PROFILE_STANDARD,
            NET_FW_PROFILE_CURRENT,
            NET_FW_PROFILE_TYPE_MAX
        }

        private enum NET_FW_SCOPE_
        {
            NET_FW_SCOPE_ALL,
            NET_FW_SCOPE_LOCAL_SUBNET,
            NET_FW_SCOPE_CUSTOM,
            NET_FW_SCOPE_MAX
        }

        private enum NET_FW_SERVICE_TYPE_
        {
            NET_FW_SERVICE_FILE_AND_PRINT,
            NET_FW_SERVICE_UPNP,
            NET_FW_SERVICE_REMOTE_DESKTOP,
            NET_FW_SERVICE_NONE,
            NET_FW_SERVICE_TYPE_MAX
        }

        #endregion
	}
}
