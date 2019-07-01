// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using Microsoft.Win32;

namespace Microsoft.Test.Execution
{
    /// <summary>
    /// Manages application and rollback of JIT and WER auto-debugging settings for infra via the Registry.
    /// </summary>
    internal static class JitRegistrationUtilities
    {
        #region Private fields

        private static readonly string managedRegistryPath = @"software\microsoft\.netframework";
        private static readonly string managedDebuggerKey = "DbgManagedDebugger";
        private static readonly string managedDebugLaunchSettingKey = "DbgJITDebugLaunchSetting";

        private static readonly string nativeRegistryPath = @"software\microsoft\windows nt\currentversion\aedebug";
        private static readonly string nativeRegistryPathWow6432 = @"software\Wow6432Node\microsoft\windows nt\currentversion\aedebug";
        private static readonly string nativeDebuggerKey = "Debugger";
        private static readonly string nativeDebugLaunchSettingKey = "Auto";

        private static readonly string werRegistryPath = @"Software\Microsoft\Windows\Windows Error Reporting";
        private static readonly string werDisabledKey = "Disabled";
        private static readonly string werDontShowUIKey = "DontShowUI";

        #endregion

        #region Internal API 

        /// <summary>
        /// Registers infra's debugger
        /// </summary>
        /// <param name="debugCommand"></param>
        internal static void Register(string debugCommand)
        {
            RegisterDebuggerKeys(managedRegistryPath, debugCommand, managedDebuggerKey, managedDebugLaunchSettingKey, false);
            RegisterDebuggerKeys(nativeRegistryPath, debugCommand, nativeDebuggerKey, nativeDebugLaunchSettingKey, true);
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(@"ProgramFiles(x86)")))
            {
                RegisterDebuggerKeys(nativeRegistryPathWow6432, debugCommand, nativeDebuggerKey, nativeDebugLaunchSettingKey, true);
            }
            RegisterWerKeys();
        }

        /// <summary>
        /// Rollsback any registry updates that were made by this instance (or a previous instance) in order to
        /// override JitDebugger settings to intercept unhandled exceptions.
        /// </summary>
        /// <returns>True if sucessful unregistering debugger.</returns>
        internal static bool Unregister()
        {
            bool result=true;
            try
            {
                UnregisterDebuggerKeys(managedRegistryPath, managedDebuggerKey, managedDebugLaunchSettingKey);
                UnregisterDebuggerKeys(nativeRegistryPath, nativeDebuggerKey, nativeDebugLaunchSettingKey);
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(@"ProgramFiles(x86)")))
                {
                    UnregisterDebuggerKeys(nativeRegistryPathWow6432, nativeDebuggerKey, nativeDebugLaunchSettingKey);
                }
                UnregisterWerKeys();
            }
            catch (SecurityException)
            {
                // We will always try to unregister the debugger so that we never leave a machine
                // in a bad state, if we didn't have permission to Register the debugger in the 
                // first place, then we will get the same exception when we try to unregister it.
                result = false;
            }
            return result;
        }
        #endregion

        #region Private Implementation

        /// <summary>
        /// Register WER keys to avoid WER dialogs on crashes
        /// Need to apply Disable=1 property and DontShowUI=1 (yes, both are needed.)
        ///  http://msdn.microsoft.com/en-us/library/bb513638(VS.85).aspx
        /// http://forum.soft32.com/windows/disable-exe-stopped-working-dialog-ftopict365124.html
        /// </summary>
        private static void RegisterWerKeys()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(werRegistryPath, true))
            {
                if (key != null)
                {
                    //Backup and apply each key
                    RegistryUtilities.BackupKey(key, werDisabledKey);
                    key.SetValue(werDisabledKey, 1, RegistryValueKind.DWord);

                    RegistryUtilities.BackupKey(key, werDontShowUIKey);
                    key.SetValue(werDontShowUIKey, 1, RegistryValueKind.DWord);
                }
            }
        }

        private static void UnregisterWerKeys()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(werRegistryPath, true))
            {
                if (key != null)
                {
                    RegistryUtilities.RollbackKey(key, werDisabledKey);
                    RegistryUtilities.RollbackKey(key, werDontShowUIKey);
                }
            }
        }

        private static void RegisterDebuggerKeys(string registryPath, string debugCommand, string debuggerKey, string launchSettingKey, bool isNative)
        {
            using (RegistryKey key = RegistryUtilities.ObtainKey(registryPath))
            {               
                // Backup current debugger settings, don't overwrite in case someone manually killed
                // the test before cleanup ran, otherwise we will clobber user's original settings.
                RegistryUtilities.BackupKey(key, debuggerKey);
                RegistryUtilities.BackupKey(key, launchSettingKey);

                key.SetValue(debuggerKey, debugCommand, RegistryValueKind.String);
                // Set JIT debugger to auto launch (e.g. no dialog).
                if (!isNative) { key.SetValue(launchSettingKey, "2", RegistryValueKind.DWord); }
                else { key.SetValue(launchSettingKey, "1", RegistryValueKind.String); }
            }
        }

        private static void UnregisterDebuggerKeys(string registryPath, string debuggerKey, string launchSettingKey)
        {
            using (RegistryKey keyPath = Registry.LocalMachine.OpenSubKey(registryPath, true))
            {
                if (keyPath != null)
                {                    
                    RegistryUtilities.RollbackKey(keyPath, debuggerKey);
                    RegistryUtilities.RollbackKey(keyPath, launchSettingKey);
                }
            }
        }


        #endregion
    }
}