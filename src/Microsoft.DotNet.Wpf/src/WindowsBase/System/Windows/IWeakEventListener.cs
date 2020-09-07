// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace System.Windows
{
    /// <summary>
    /// A class should implement this interface if it needs to listen to
    /// events via the centralized event dispatcher of WeakEventManager.
    /// The event dispatcher forwards an event by calling the ReceiveWeakEvent
    /// method.
    /// </summary>
    /// <remarks>
    /// The principal reason for doing this is that the event source has a
    /// lifetime independent of the receiver.  Using the central event
    /// dispatching allows the receiver to be GC'd even if the source lives on.
    /// Whereas the normal event hookup causes the source to hold a reference
    /// to the receiver, thus keeping the receiver alive too long.
    /// </remarks>
    public interface IWeakEventListener
    {
        /// <summary>
        /// Handle events from the centralized event table.
        /// </summary>
        /// <returns>
        /// True if the listener handled the event.  It is an error to register
        /// a listener for an event that it does not handle.
        /// </returns>
        bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e);
    }
}

