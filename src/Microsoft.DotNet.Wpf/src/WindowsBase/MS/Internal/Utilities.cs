// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace MS.Internal
{
    /// <summary>
    /// General utility class for macro-type functions.
    /// </summary>
    internal static class Utilities
    {
        private static readonly Version _osVersion = Environment.OSVersion.Version;

        // DWM error code for when desktop composition is disabled
        private const int DWM_E_COMPOSITIONDISABLED = unchecked((int)0x80263001);

        internal static bool IsOSVistaOrNewer
        {
            get { return _osVersion >= new Version(6, 0); }
        }

        internal static bool IsOSWindows7OrNewer
        {
            get { return _osVersion >= new Version(6, 1); }
        }

        internal static bool IsOSWindows8OrNewer
        {
            get { return _osVersion >= new Version(6, 2); }
        }
        
        internal static bool IsCompositionEnabled
        {
            get
            {
                if (!IsOSVistaOrNewer)
                {
                    return false;
                }

                try
                {
                    var result = PInvoke.DwmIsCompositionEnabled(out BOOL isDesktopCompositionEnabled);
                    
                    // Handle the specific case where desktop composition is disabled
                    // Error code DWM_E_COMPOSITIONDISABLED should return false, not throw
                    if (result.Failed && result.Value == DWM_E_COMPOSITIONDISABLED)
                    {
                        return false;
                    }
                    
                    result.ThrowOnFailure();
                    return isDesktopCompositionEnabled;
                }
                catch (COMException ex) when (ex.HResult == DWM_E_COMPOSITIONDISABLED)
                {
                    // Desktop composition is disabled - this is not an error condition,
                    // just return false to indicate composition is not available
                    return false;
                }
            }
        }

        internal static void SafeDispose<T>(ref T disposable) where T : IDisposable
        {
            // Dispose can safely be called on an object multiple times.
            IDisposable t = disposable;
            disposable = default(T);
            t?.Dispose();
        }
        
        internal static void SafeRelease<T>(ref T comObject) where T : class
        {
            T t = comObject;
            comObject = default(T);
            if (null != t)
            {
                Debug.Assert(Marshal.IsComObject(t));
                Marshal.ReleaseComObject(t);
            }
        }
    }
}
