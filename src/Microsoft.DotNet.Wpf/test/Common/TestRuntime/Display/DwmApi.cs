// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Test.Display
{
    public static class DwmApi
    {
        #region Public Members

        public static bool DwmIsCompositionEnabled()
        {
            bool retVal = false;
            int hresult = _DwmIsCompositionEnabled(out retVal);
            if (FAILED(hresult)) { throw new Win32Exception(); }
            return retVal;
        }

        public static void DwmEnableComposition(bool enableDwm)
        {
            if (System.Environment.OSVersion.Version.Major <= 5)  // Xp or below do not have DWM APIs
            {
                throw new InvalidOperationException("OS <= XP do not have DWM capabilities -- This platfrom is '" + System.Environment.OSVersion.VersionString + "'");
            }
            if (DwmIsCompositionEnabled() == enableDwm) { return; }

            int hresult = _DwmEnableComposition(enableDwm);
            if (FAILED(hresult)) { throw new Win32Exception(); }
        }

        #endregion

        #region Private Members

        private static bool SUCCEEDED(int HRESULT)
        {
            return (HRESULT >= 0);
        }
        private static bool FAILED(int HRESULT)
        {
            return (HRESULT < 0);
        }

        #endregion

        #region Imports

        private const string DLLNAME = "DwmApi.dll";
        [DllImport(DLLNAME, EntryPoint = "DwmIsCompositionEnabled", PreserveSig = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern int _DwmIsCompositionEnabled(out bool isEnabled);

        [DllImport(DLLNAME, EntryPoint = "#106")]
        internal static extern int _DwmIsCompositionSupported(bool remoteSession, out bool supported);

        [DllImport(DLLNAME, EntryPoint = "DwmEnableComposition", PreserveSig = true, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern int _DwmEnableComposition(bool enableDwm);


        #endregion
    }
}
