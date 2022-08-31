// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: NotificationEventArgs event args class

using System;
using System.Windows.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// NotificationEventArgs event args class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class NotificationEventArgs  : AutomationEventArgs
#else
    public sealed class NotificationEventArgs : AutomationEventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor for notification event args.
        /// </summary>
        /// <param name="notificationKind">Specifies the type of the notification.</param>
        /// <param name="notificationProcessing">Specifies the order in which to process the notification.</param>
        /// <param name="displayString">A display string describing the event.</param>
        /// <param name="activityId">A unique non-localized string to identify an action or group of actions. Use this to pass additional information to the event handler.</param>
        public NotificationEventArgs(AutomationNotificationKind notificationKind,
                                    AutomationNotificationProcessing notificationProcessing,
                                    string displayString,
                                    string activityId)
            : base(AutomationElementIdentifiers.NotificationEvent)
        {
            NotificationKind = notificationKind;
            NotificationProcessing = notificationProcessing;
            DisplayString = displayString;
            ActivityId = activityId;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns the type of the notification.
        /// </summary>
        public AutomationNotificationKind NotificationKind { get; private set; }

        /// <summary>
        /// Returns the order in which to process the notification.
        /// </summary>
        public AutomationNotificationProcessing NotificationProcessing { get; private set; }

        /// <summary>
        /// Returns the the display string of the notification.
        /// </summary>
        public string DisplayString { get; private set; }

        /// <summary>
        /// Returns the activity ID string of the notification.
        /// </summary>
        public string ActivityId { get; private set; }

        #endregion Public Properties
    }
}
