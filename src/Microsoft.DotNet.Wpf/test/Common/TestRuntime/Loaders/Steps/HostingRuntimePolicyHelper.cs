// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test.Loaders.Steps.HostingRuntimePolicy
{
    public static class HostingRuntimePolicyHelper
    {
        #region Private Fields

        private const string HostingRegistryBaseKeyName =
            @"HKEY_LOCAL_MACHINE";

        private const RegistryHive HostingRegistryHive = RegistryHive.LocalMachine;

        private const string HostingRegistrySubKeyName =
            @"Software\Microsoft\.NETFramework\Windows Presentation Foundation\Hosting";

        private const string DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName =
            @"DoNotLaunchVersion3HostedApplicationInVersion4Runtime";

        private const RegistryValueKind DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueKind =
            RegistryValueKind.DWord;

        #endregion

        #region Private Properties

        /// <summary>
        /// Registry Views supported on the current OS. On 64 bit OS, there are 2 views (<see cref="RegistryView.Registry32"/>
        /// and <see cref="RegistryView.Registry64"/>, and on 32 bit systems, there is only 1 
        /// view (<see cref="RegistryView.Registry32"/>)
        /// </summary>
        private static List<RegistryView> SupportedRegistryViews
        {
            get
            {
                var views = new List<RegistryView>
                {
                    RegistryView.Registry32
                };

                if (Environment.Is64BitOperatingSystem)
                {
                    views.Add(RegistryView.Registry64);
                }

                return views;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns a <see cref="RegistryKey"/> instance corresponding to 
        /// 'HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\Windows Presentation Foundation\Hosting' registry key. 
        /// </summary>
        /// <param name="registryView">The registry view to open</param>
        /// <param name="writeable">whether to open the key in writable mode</param>
        /// <param name="createIfMissing">When true, the key is created if it was not present before</param>
        /// <returns>The key, or null if an error occurs</returns>
        private static RegistryKey GetHostingRegistryKey(RegistryView registryView, bool writeable = false, bool createIfMissing = false)
        {
            RegistryKey baseKey = RegistryKey.OpenBaseKey(HostingRegistryHive, registryView);

            RegistryKey hostingKey = null;
            try
            {
                if (baseKey != null)
                {
                    hostingKey =
                         createIfMissing
                         ? baseKey.CreateSubKey(HostingRegistrySubKeyName, writeable)
                         : baseKey.OpenSubKey(HostingRegistrySubKeyName, writeable);
                }

            }
            catch (Exception e) when (
                e is SecurityException ||
                e is UnauthorizedAccessException ||
                e is IOException)
            {
                // Do nothing
            }
            finally
            {
                if (baseKey != null)
                {
                    baseKey.Dispose();
                    baseKey = null;
                }
            }

            return hostingKey;
        }

        /// <summary>
        /// Obtains the value of registry value 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' from 
        /// 'HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\Windows Presentation Foundation\Hosting'. 
        /// 
        /// On 64 bit Operating Systems, it also queries 
        /// 'HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\Windows Presentation Foundation\Hosting'
        /// key
        /// </summary>
        /// <returns>A dictionary of (<see cref="RegistryView"/>, bool) pairs</returns>
        [RegistryPermission(SecurityAction.Demand, Read = HostingRegistryBaseKeyName + @"\" + HostingRegistrySubKeyName)]
        private static bool GetHostingRuntimePolicyValue(RegistryView registryView)
        {
            bool doNotLaunchV3AppInV4Runtime = false;

            using (var regKey = GetHostingRegistryKey(registryView))
            {
                var value = regKey?.GetValue(
                    DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName,
                    defaultValue: Convert.ToInt32(false, CultureInfo.InvariantCulture));
                try
                {
                    doNotLaunchV3AppInV4Runtime = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                }
                catch (Exception e) when (
                    e is FormatException ||
                    e is InvalidCastException)
                {
                    // Do nothing
                }
            }

            return doNotLaunchV3AppInV4Runtime;
        }

        /// <summary>
        /// Sets the value of registry value 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' in 
        /// 'HKEY_LOCAL_MACHINE\Software\Microsoft\.NETFramework\Windows Presentation Foundation\Hosting'. 
        /// 
        /// On 64 bit Operating Systems, it sets the value under the key
        /// 'HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\Windows Presentation Foundation\Hosting'
        /// </summary>
        /// <param name="doNotLaunchV3AppInV4Runtime">value to set</param>
        [RegistryPermission(SecurityAction.Demand, ViewAndModify = HostingRegistryBaseKeyName + @"\" + HostingRegistrySubKeyName)]
        private static void SetHostingRuntimePolicyValue(RegistryView registryView, bool doNotLaunchV3AppInV4Runtime)
        {
            if (GetHostingRuntimePolicyValue(registryView) == doNotLaunchV3AppInV4Runtime)
            {
                return;
            }

            using (var regKey = GetHostingRegistryKey(registryView, writeable: true, createIfMissing: true))
            {
                try
                {
                    // When true, set the registry value to 1
                    regKey.SetValue(
                        DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName,
                        value: Convert.ToInt32(doNotLaunchV3AppInV4Runtime, CultureInfo.InvariantCulture),
                        valueKind: RegistryValueKind.DWord);
#if DRT
                    // In DRT, which is the most common tests we run for this scenario, 
                    // remove the registry value when doNotLaunchV3AppInV4Runtime == false. 
                    // This would correspond to the typical cusotmer scenario. 
                    //
                    // In feature tests, we will leave the registry value intact, but set it to 0
                    // when doNotLaunchV3AppInV4Runtime == false - this would help us test the functionality
                    // of the registry value thoroughly. 
                    if (!doNotLaunchV3AppInV4Runtime)
                    {
                        regKey.DeleteValue(
                            DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName);
                    }
#endif 

                }
                catch (ArgumentException)
                {
                    // Somehow, this registry value exists but it is not REG_DWORD. Try to 
                    // remove the value and recreate it correctly. 
                    regKey.DeleteValue(
                        DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName);
                    // Only create the registry value if it is non-zero
                    if (doNotLaunchV3AppInV4Runtime)
                    {
                        regKey.SetValue(
                            DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName,
                            value: Convert.ToInt32(doNotLaunchV3AppInV4Runtime, CultureInfo.InvariantCulture),
                            valueKind: RegistryValueKind.DWord);
#if DRT
                        if (!doNotLaunchV3AppInV4Runtime)
                        {
                            regKey.DeleteValue(
                                DoNotLaunchVersion3HostedApplicationInVersion4RuntimeRegistryValueName);
                        }
#endif 
                    }
                }
            }
        }

        #endregion

        #region Publics 

        /// <summary>
        /// Gets or sets the value of the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime'
        /// registry value in the default registry view. The registry view is determined by the bitness 
        /// of the process executing this code.
        /// </summary>
        public static bool DoNotLaunchV3AppInV4Runtime
        {
            get
            {
                return GetHostingRuntimePolicyValue(RegistryView.Default);
            }

            set
            {
                SetHostingRuntimePolicyValue(RegistryView.Default, value);
            }
        }

        /// <summary>
        /// Sets the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry values
        /// </summary>
        /// <param name="doNotLaunchV3AppInV4Runtime32Bit">value to set in the 32-bit registry view</param>
        /// <param name="doNotLaunchV3AppInV4Runtime64Bit">value to set in teh 64-bit registry view</param>
        /// <param name="restoreMode">Determines whether the returned IDisposable will, upon disposal, revert the registry to 
        /// the prior state, or whether it will reset the registry to it's default state (i.e., set policy 
        /// values to false) </param>
        /// <returns>An IDisposable object which, upon disposal, will revert or reset the state of the registry in 
        /// accordance with the policy requested by <paramref name="restoreMode"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static IDisposable SetHostingRuntimePolicyValues(
            bool? doNotLaunchV3AppInV4Runtime32Bit,
            bool? doNotLaunchV3AppInV4Runtime64Bit,
            HostingRuntimePolicyRestoreMode restoreMode)
        {
            var registryViews = new List<RegistryView>();

            if (doNotLaunchV3AppInV4Runtime32Bit.HasValue)
            {
                registryViews.Add(RegistryView.Registry32);
            }

            if (doNotLaunchV3AppInV4Runtime64Bit.HasValue && Environment.Is64BitOperatingSystem)
            {
                registryViews.Add(RegistryView.Registry64);
            }

            var restoreHelper =
                new HostingRuntimePolicyRestoreHelper(registryViews, restoreMode);

            if (doNotLaunchV3AppInV4Runtime32Bit.HasValue)
            {
                SetHostingRuntimePolicyValue(RegistryView.Registry32, doNotLaunchV3AppInV4Runtime32Bit.Value);
            }

            if (doNotLaunchV3AppInV4Runtime64Bit.HasValue && Environment.Is64BitOperatingSystem)
            {
                SetHostingRuntimePolicyValue(RegistryView.Registry64, doNotLaunchV3AppInV4Runtime64Bit.Value);
            }

            return restoreHelper;
        }

        /// <summary>
        /// Sets the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry values
        /// </summary>
        /// <param name="doNotLaunchV3AppInV4Runtime32Bit">value to set in the 32-bit registry view</param>
        /// <param name="doNotLaunchV3AppInV4Runtime64Bit">value to set in teh 64-bit registry view</param>
        /// <returns>An IDisposable object which, upon disposal, will reset the state of the registry 
        /// back to default (i.e., set the policy value to 'false' in all registry views) </returns>
        public static IDisposable SetHostingRuntimePolicyValues(
            bool? doNotLaunchV3AppInV4Runtime32Bit,
            bool? doNotLaunchV3AppInV4Runtime64Bit)
        {
            return SetHostingRuntimePolicyValues(
                doNotLaunchV3AppInV4Runtime32Bit,
                doNotLaunchV3AppInV4Runtime64Bit,
                HostingRuntimePolicyRestoreMode.ResetToDefault);
        }

        /// <summary>
        /// Sets the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry values
        /// </summary>
        /// <param name="doNotLaunchV3AppInV4Runtime">Value to set in both 32-bit and 64-bit registry views</param>
        /// <param name="restoreMode">Determines whether the returned IDisposable will, upon disposal, revert the registry to 
        /// the prior state, or whether it will reset the registry to it's default state (i.e., set policy 
        /// values to false) </param>
        /// <returns>An IDisposable object which, upon disposal, will revert or reset the state of the registry in 
        /// accordance with the policy requested by <paramref name="restoreMode"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static IDisposable SetHostingRuntimePolicyValues(
            bool doNotLaunchV3AppInV4Runtime,
            HostingRuntimePolicyRestoreMode restoreMode)
        {
            var restoreHelper =
                new HostingRuntimePolicyRestoreHelper(SupportedRegistryViews, restoreMode);
            SupportedRegistryViews.ForEach((rv) => SetHostingRuntimePolicyValue(rv, doNotLaunchV3AppInV4Runtime));

            return restoreHelper;
        }

        /// <summary>
        /// Sets the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry values
        /// </summary>
        /// <param name="doNotLaunchV3AppInV4Runtime">Value to set in both 32-bit and 64-bit registry views</param>
        /// <returns>An IDisposable object which, upon disposal, will revert or reset the state of the registry 
        /// back to default (i.e., set the policy value to 'false' in all registry views)</returns>
        public static IDisposable SetHostingRuntimePolicyValues(
            bool doNotLaunchV3AppInV4Runtime)
        {
            return SetHostingRuntimePolicyValues(
                doNotLaunchV3AppInV4Runtime,
                HostingRuntimePolicyRestoreMode.ResetToDefault);
        }

        /// <summary>
        /// Returns the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry value
        /// for each applicable registry view
        /// </summary>
        public static Dictionary<RegistryView, bool> HostingRuntimePolicyValues
        {
            get
            {
                var result = new Dictionary<RegistryView, bool>();
                SupportedRegistryViews.ForEach((rv) => result.Add(rv, GetHostingRuntimePolicyValue(rv)));

                return result;
            }
        }

        #endregion

        #region Inner Types

        /// <summary>
        /// Controls how the <see cref="IDisposable"/> objects returned by 
        /// <see cref="SetHostingRuntimePolicyValues(bool, HostingRuntimePolicyRestoreMode)"/>
        /// and <see cref="HostingRuntimePolicyHelper.SetHostingRuntimePolicyValues(bool?, bool?, HostingRuntimePolicyRestoreMode)"/> 
        /// will behave when they go about restoring the state of the registry. 
        /// </summary>
        public enum HostingRuntimePolicyRestoreMode
        {
            /// <summary>
            /// Restores the state of the registry to what it was prior 
            /// to the call to set new values
            /// </summary>
            RestoreToPrevious,

            /// <summary>
            /// Resets the state of the registry to its default state, i.e., it 
            /// sets the 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry 
            /// value to false in all available registry views
            /// </summary>
            ResetToDefault
        }

        /// <summary>
        /// An IDisposable based helper used to automatically undo the values set by 
        /// <see cref="SetHostingRuntimePolicyValues(bool, HostingRuntimePolicyRestoreMode)"/>
        /// or <see cref="SetHostingRuntimePolicyValues(bool?, bool?, HostingRuntimePolicyRestoreMode)"/>
        /// </summary>
        private class HostingRuntimePolicyRestoreHelper : IDisposable
        {
            /// <summary>
            /// These are the values to which each registry view will 
            /// be restored to when <see cref="Dispose"/> happens
            /// </summary>
            private Dictionary<RegistryView, bool> RestoreValues { get; }

            /// <summary>
            /// Default value for 'DoNotLaunchVersion3HostedApplicationInVersion4Runtime' registry
            /// value
            /// </summary>
            private const bool DefaultValue = false;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="registryViews">Registry Views being recorded for later restoration</param>
            /// <param name="restoreMode">Determines whether values are being saved for later restoration, or 
            /// whether <see cref="Dispose"/> will reset the values to <see cref="DefaultValue"/></param>
            public HostingRuntimePolicyRestoreHelper(List<RegistryView> registryViews, HostingRuntimePolicyRestoreMode restoreMode)
            {
                RestoreValues = new Dictionary<RegistryView, bool>();
                foreach (var registryView in registryViews)
                {
                    bool restoreValue
                        = restoreMode == HostingRuntimePolicyRestoreMode.ResetToDefault
                        ? DefaultValue
                        : GetHostingRuntimePolicyValue(registryView);
                    RestoreValues.Add(registryView, restoreValue);
                }
            }

            /// <summary>
            /// Restores the registry values 
            /// </summary>
            private void Restore()
            {
                foreach (var restoreValue in RestoreValues)
                {
                    HostingRuntimePolicyHelper.SetHostingRuntimePolicyValue(restoreValue.Key, restoreValue.Value);
                }
            }

            #region IDisposable Support

            /// <summary>
            /// Flag to make sure that <see cref="Dispose"/>
            /// is called only once
            /// </summary>
            private bool _disposed = false;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Restore();
                }
            }

            #endregion
        }

        #endregion
    }
}