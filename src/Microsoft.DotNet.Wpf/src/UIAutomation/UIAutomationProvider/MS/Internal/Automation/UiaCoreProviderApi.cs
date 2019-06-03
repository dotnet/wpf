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

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: This method is used to return an IRawElementProviderSimple associated with an HWND to UIAutomation in response to a WM_GETOBJECT
        ///                 The returned value is simply an LRESULT, so is harmless, and the input values are verfied on the unmanaged side, so it is not abusable.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el)
        {
            return RawUiaReturnRawElementProvider( hwnd, wParam, lParam, el );
        }

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: This converts an hwnd to a MiniHwndProxy, which while technically implementing IRawElementProviderSimple, has none of the functionality
        ///                 and is therefore simply a harmless hwnd container.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
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

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: Causes an AutomationEvent to fire, requires a functional IRawElementProvider, so cannot even be used to spoof events from other AutomationElements.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static void UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int propertyId, object oldValue, object newValue)
        {
            CheckError(RawUiaRaiseAutomationPropertyChangedEvent(provider, propertyId, oldValue, newValue));
        }

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: Causes an AutomationEvent to fire, requires a functional IRawElementProvider, so cannot even be used to spoof events from other AutomationElements.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static void UiaRaiseAutomationEvent(IRawElementProviderSimple provider, int eventId)
        {
            CheckError(RawUiaRaiseAutomationEvent(provider, eventId));
        }

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: Causes an AutomationEvent to fire, requires a functional IRawElementProvider, so cannot even be used to spoof events from other AutomationElements.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static void UiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId)
        {
            CheckError(RawUiaRaiseStructureChangedEvent(provider, structureChangeType, runtimeId, runtimeId == null ? 0 : runtimeId.Length));
        }

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: Causes an AutomationEvent to fire, requires a functional IRawElementProvider, so cannot even be used to spoof events from other AutomationElements.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        internal static void UiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete)
        {
            CheckError(RawUiaRaiseAsyncContentLoadedEvent(provider, asyncContentLoadedState, PercentComplete));
        }

        /// <SecurityNote>
        ///    Critical: This code calls into the unmanaged UIAutomationCore.dll
        ///    TreatAsSafe: Simply checks whether clients are listening in order to know whether to fire AutomationEvents. This is information we WANT available to
        ///                 Partial Trust users, so is not an information disclosure risk.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
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

        /// <SecurityNote>
        /// Critical: This calls into Marshal.ThrowExceptionForHR which has a link demand
        /// TreatAsSafe: Throwing an exception is deemed as a safe operation (throwing exceptions is allowed in Partial Trust). 
        ///              We pass an IntPtr that has a value of -1 so that ThrowExceptionForHR ignores IErrorInfo of the current thread.
        /// </SecurityNote>
        /// Check hresult for error...
        [SecurityCritical, SecurityTreatAsSafe]
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

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaReturnRawElementProvider", CharSet = CharSet.Unicode)]
        private static extern IntPtr RawUiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaHostProviderFromHwnd", CharSet = CharSet.Unicode)]
        private static extern int RawUiaHostProviderFromHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple provider);

        // Event APIs...

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseAutomationPropertyChangedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int id, object oldValue, object newValue);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseAutomationEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAutomationEvent(IRawElementProviderSimple provider, int id);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseStructureChangedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId, int runtimeIdLen);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaRaiseAsyncContentLoadedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.UIAutomationCore, EntryPoint = "UiaClientsAreListening", CharSet = CharSet.Unicode)]
        private static extern bool RawUiaClientsAreListening();

        #endregion Raw API methods
    }
}
