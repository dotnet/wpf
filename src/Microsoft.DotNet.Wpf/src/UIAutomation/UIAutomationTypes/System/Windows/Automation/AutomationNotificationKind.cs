// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Description: Indicates the type of notification when calling AutomationPeer.RaiseNotificationEvent
//

namespace System.Windows.Automation
{
    /// <summary>
    /// Indicates the type of notification when calling AutomationPeer.RaiseNotificationEvent
    /// </summary>
    public enum AutomationNotificationKind
    {
        /// <summary>
        /// The current element has had something added to it that should be presented to the user.
        /// </summary>
        ItemAdded = 0,

        /// <summary>
        /// The current element has had something removed from inside it that should be presented to the user.
        /// </summary>
        ItemRemoved = 1,

        /// <summary>
        /// The current element has a notification that an action was completed.
        /// </summary>
        ActionCompleted = 2,

        /// <summary>
        /// The current element has a notification that an action was aborted.
        /// </summary>
        ActionAborted = 3,

        /// <summary>
        /// The current element has a notification not an add, remove, completed, or aborted action.
        /// </summary>
        Other = 4,
    }
}
