// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provider interface for UI that raises events

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Implemented on the root element of a UI fragment to allow it to be notified
    /// of when it is required to raise automation events.
    /// </summary>
    [ComVisible(true)]
    [Guid("a407b27b-0f6d-4427-9292-473c7bf93258")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRawElementProviderAdviseEvents : IRawElementProviderSimple
#else
    public interface IRawElementProviderAdviseEvents : IRawElementProviderSimple
#endif
    {
        /// <summary>
        /// Called by the client-side UIAccess to notify a server when a client is listening
        /// for a specific event (and properties, if the event is a property changed event).
        /// This allows the server to optimize its notifications by tracking which events 
        /// are being listened for.
        /// </summary>
        /// <param name="eventId">The identifier of the event being removed</param>
        /// <param name="properties">An array of identifiers of the properties being removed</param>
        void AdviseEventAdded(int eventId, int [] properties);

        /// <summary>
        /// Called by the client-side UIAccess to notify a server when a client stops listening
        /// for a specific event (and properties, if the event is a property changed event).
        /// This allows the server to optimize its notifications by tracking which events 
        /// are being listened for.
        /// </summary>
        /// <param name="eventId">The identifier of the event being removed</param>
        /// <param name="properties">An array of identifiers of the properties being removed</param>
        void AdviseEventRemoved(int eventId, int [] properties);
    }
}
