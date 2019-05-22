// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provides functionality that Win32/Avalon servers need (non-Avalon specific)

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Windows.Automation;
using MS.Internal.Automation;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Class containing methods used by Win32 Automation implementations
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class AutomationInteropProvider
#else
    public static class AutomationInteropProvider
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants & readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants & readonly Fields

        /// <summary>WM_GETOBJECT lParam value indicating that server should return a reference to the root RawElementProvider</summary>
        public const int RootObjectId = -25;

        /// <summary>Maximum number of events to send before batching</summary>
        public const int InvalidateLimit = 20;

        /// <summary>When returned as the first element of IRawElementProviderFragment.GetRuntimeId(), indicates
        /// that the ID is partial and should be appended to the ID provided by the base provider. Typically
        /// only used by Win32 proxies</summary>
        public const int AppendRuntimeId = 3;

        /// <summary>Maximum number of events to send before batching for Items in Containers</summary>
        public const int ItemsInvalidateLimit = 5;

        #endregion Public Constants & readonly Fields

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Servers that are slotting into the HWND tree can use this to get a base implementation.
        /// </summary>
        /// <param name="hwnd">HWND that server is slotting in over</param>
        /// <returns>base raw element for specified window</returns>
        public static IRawElementProviderSimple HostProviderFromHandle ( IntPtr hwnd )
        {
            ValidateArgument(hwnd != IntPtr.Zero, nameof(SRID.HwndMustBeNonNULL));
            return UiaCoreProviderApi.UiaHostProviderFromHwnd(hwnd);
        }
    
        /// <summary>
        /// Server uses this to return an element in response to WM_GETOBJECT.
        /// </summary>
        /// <param name="hwnd">hwnd from the WM_GETOBJECT message</param>
        /// <param name="wParam">wParam from the WM_GETOBJECT message</param>
        /// <param name="lParam">lParam from the WM_GETOBJECT message</param>
        /// <param name="el">element to return</param>
        /// <returns>Server should return the return value as the lresult return value to the WM_GETOBJECT windows message</returns>
        public static IntPtr ReturnRawElementProvider (IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el )
        {
            ValidateArgument( hwnd != IntPtr.Zero, nameof(SRID.HwndMustBeNonNULL));
            ValidateArgumentNonNull(el, "el" );
            
            return UiaCoreProviderApi.UiaReturnRawElementProvider(hwnd, wParam, lParam, el);
        }

        /// <summary>
        /// Called by a server to determine if there are any listeners for events.
        /// </summary>
        public static bool ClientsAreListening 
        { 
            get 
            {
                return UiaCoreProviderApi.UiaClientsAreListening();
            } 
        }

        /// <summary>
        /// Called by a server to notify the UIAccess server of a AutomationPropertyChangedEvent event.
        /// </summary>
        /// <param name="element">The actual server-side element associated with this event.</param>
        /// <param name="e">Contains information about the property that changed.</param>
        public static void RaiseAutomationPropertyChangedEvent(IRawElementProviderSimple element, AutomationPropertyChangedEventArgs e)
        {
            ValidateArgumentNonNull(element, "element");
            ValidateArgumentNonNull(e, "e");

            // PRESHARP will flag this as warning 56506/6506:Parameter 'e' to this public method must be validated: A null-dereference can occur here.
            // False positive, e is checked, see above
#pragma warning suppress 6506
            UiaCoreProviderApi.UiaRaiseAutomationPropertyChangedEvent(element, e.Property.Id, e.OldValue, e.NewValue);
        }

        /// <summary>
        /// Called to notify listeners of a pattern or custom event.  This could could be called by a server implementation or by a proxy's event
        /// translator.  
        /// </summary>
        /// <param name="eventId">An AutomationEvent representing this event.</param>
        /// <param name="provider">The actual server-side element associated with this event.</param>
        /// <param name="e">Contains information about the event (may be null).</param>
        public static void RaiseAutomationEvent(AutomationEvent eventId, IRawElementProviderSimple provider, AutomationEventArgs e)
        {
            ValidateArgumentNonNull(eventId, "eventId");
            ValidateArgumentNonNull(provider, "provider");
            ValidateArgumentNonNull(e, "e");

            // PRESHARP will flag this as warning 56506/6506:Parameter 'e' to this public method must be validated: A null-dereference can occur here.
            // False positive, e is checked, see above
#pragma warning suppress 6506
            if (e.EventId == AutomationElementIdentifiers.AsyncContentLoadedEvent)
            {
                AsyncContentLoadedEventArgs asyncArgs = e as AsyncContentLoadedEventArgs;
                if(asyncArgs == null)
                    ThrowInvalidArgument("e");

                UiaCoreProviderApi.UiaRaiseAsyncContentLoadedEvent(provider, asyncArgs.AsyncContentLoadedState, asyncArgs.PercentComplete);
                return;
            }
            // PRESHARP will flag this as warning 56506/6506:Parameter 'e' to this public method must be validated: A null-dereference can occur here.
            // False positive, e is checked, see above
#pragma warning suppress 6506
            if (e.EventId == WindowPatternIdentifiers.WindowClosedEvent && !(e is WindowClosedEventArgs))
                ThrowInvalidArgument("e");

            // fire to all clients
            // PRESHARP will flag this as warning 56506/6506:Parameter 'eventId' to this public method must be validated: A null-dereference can occur here.
            // False positive, eventId is checked, see above
#pragma warning suppress 6506
            UiaCoreProviderApi.UiaRaiseAutomationEvent(provider, eventId.Id);
        }

        /// <summary>
        /// Called by a server to notify the UIAccess server of a tree change event.
        /// </summary>
        /// <param name="provider">The actual server-side element associated with this event.</param>
        /// <param name="e">Contains information about the event.</param>
        public static void RaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangedEventArgs e)
        {
            ValidateArgumentNonNull(provider, "provider");
            ValidateArgumentNonNull(e, "e");

            // PRESHARP will flag this as warning 56506/6506:Parameter 'e' to this public method must be validated: A null-dereference can occur here.
            // False positive, e is checked, see above
#pragma warning suppress 6506
            UiaCoreProviderApi.UiaRaiseStructureChangedEvent(provider, e.StructureChangeType, e.GetRuntimeId());
        }
        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Check that specified argument is non-null, if so, throw exception
        private static void ValidateArgumentNonNull(object obj, string argName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        // Throw an argument Exception with a generic error
        private static void ThrowInvalidArgument(string argName)
        {
            throw new ArgumentException(SR.Format(SRID.GenericInvalidArgument, argName));
        }

        // Check that specified condition is true; if not, throw exception
        private static void ValidateArgument(bool cond, string reason)
        {
            if (!cond)
            {
                throw new ArgumentException(SR.GetResourceString(reason, null));
            }
        }

        #endregion Private Methods
    }
}
