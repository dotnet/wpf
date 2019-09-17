// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Imports from unmanaged UiaCore DLL

using System;
using System.Security;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using Microsoft.Internal;

namespace MS.Internal.Automation
{
    internal static class UiaCoreProviderApi
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //
        // Provider-side methods...
        //
        #region Provider methods

        private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

        internal static IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el)
        {
            return RawUiaReturnRawElementProvider( hwnd, wParam, lParam, el );
        }

        internal static IRawElementProviderSimple UiaHostProviderFromHwnd(IntPtr hwnd)
        {
            IRawElementProviderSimple provider;
            CheckError(RawUiaHostProviderFromHwnd(hwnd, out provider));
            return provider;
        }
        #endregion Provider methods

        //
        // Event methods (client and provider)
        //
        #region Event methods

        internal static void UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int propertyId, object oldValue, object newValue)
        {
            CheckError(RawUiaRaiseAutomationPropertyChangedEvent(provider, propertyId, oldValue, newValue));
        }

        internal static void UiaRaiseAutomationEvent(IRawElementProviderSimple provider, int eventId)
        {
            CheckError(RawUiaRaiseAutomationEvent(provider, eventId));
        }

        internal static void UiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId)
        {
            CheckError(RawUiaRaiseStructureChangedEvent(provider, structureChangeType, runtimeId, runtimeId == null ? 0 : runtimeId.Length));
        }

        internal static void UiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete)
        {
            CheckError(RawUiaRaiseAsyncContentLoadedEvent(provider, asyncContentLoadedState, PercentComplete));
        }

        internal static bool UiaClientsAreListening()
        {
            return RawUiaClientsAreListening();
        }

        #endregion Event methods

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
            if (hr >= 0 || hr == UIA_E_ELEMENTNOTAVAILABLE)
            {
                return;
            }

            Marshal.ThrowExceptionForHR(hr, (IntPtr)(-1));
        }

        #endregion Private Methods

        #region Raw API methods

        //
        // Provider-side methods...
        //

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaReturnRawElementProvider", CharSet = CharSet.Unicode)]
        private static extern IntPtr RawUiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaHostProviderFromHwnd", CharSet = CharSet.Unicode)]
        private static extern int RawUiaHostProviderFromHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple provider);

        // Event APIs...

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseAutomationPropertyChangedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int id, object oldValue, object newValue);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseAutomationEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAutomationEvent(IRawElementProviderSimple provider, int id);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseStructureChangedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId, int runtimeIdLen);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseAsyncContentLoadedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete);

        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaClientsAreListening", CharSet = CharSet.Unicode)]
        private static extern bool RawUiaClientsAreListening();

        #endregion Raw API methods
    }
}
