// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Imports from unmanaged UiaCore DLL


using System;
using System.Security;
using System.Runtime.InteropServices;
using MS.Internal.UIAutomationTypes;
using MS.Win32;

namespace MS.Internal.Automation
{
    internal static class UiaCoreTypesApi
    {
        //------------------------------------------------------
        //
        //  Other API types
        //
        //------------------------------------------------------

        #region Other
        internal enum AutomationIdType
        {
            Property,
            Pattern,
            Event,
            ControlType,
            TextAttribute
        }

        internal const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
        internal const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
        internal const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);
        internal const int UIA_E_PROXYASSEMBLYNOTLOADED = unchecked((int)0x80040203);

        #endregion Other

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //
        // Support methods...
        //

        internal static int UiaLookupId(AutomationIdType type, ref Guid guid)
        {   
            return RawUiaLookupId( type, ref guid );
        }

        internal static object UiaGetReservedNotSupportedValue()
        {
            object notSupportedValue;
            CheckError(RawUiaGetReservedNotSupportedValue(out notSupportedValue));
            return notSupportedValue;
        }

        internal static object UiaGetReservedMixedAttributeValue()
        {
            object mixedAttributeValue;
            CheckError(RawUiaGetReservedMixedAttributeValue(out mixedAttributeValue));
            return mixedAttributeValue;
        }

        internal static bool SupportsWin7Identifiers()
        {
            IntPtr automationCoreHandle = LoadLibraryHelper.SecureLoadLibraryEx(DllImport.UIAutomationCore, IntPtr.Zero, UnsafeNativeMethods.LoadLibraryFlags.LOAD_LIBRARY_SEARCH_SYSTEM32);
            if (automationCoreHandle != IntPtr.Zero)
            {
                IntPtr entryPoint = UnsafeNativeMethods.GetProcAddressNoThrow(new HandleRef(null, automationCoreHandle), StartListeningExportName);
                if (entryPoint != IntPtr.Zero)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        
        /// Check hresult for error...
        private static void CheckError(int hr)
        {
            if (hr >= 0)
            {
                return;
            }

            Marshal.ThrowExceptionForHR(hr, (IntPtr)(-1));
        }

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaLookupId", CharSet = CharSet.Unicode)]
        private static extern int RawUiaLookupId(AutomationIdType type, ref Guid guid);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetReservedNotSupportedValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetReservedNotSupportedValue([MarshalAs(UnmanagedType.IUnknown)] out object notSupportedValue);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaGetReservedMixedAttributeValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetReservedMixedAttributeValue([MarshalAs(UnmanagedType.IUnknown)] out object mixedAttributeValue);

        #endregion Private Methods

        #region Private Constants

        private const string StartListeningExportName = "SynchronizedInputPattern_StartListening";

        #endregion Private Constants
    }
}
