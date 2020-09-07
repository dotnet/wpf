// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace System.Windows.Controls
{
    /// <summary>
    /// Provides data for the DisplayModeChanged event.
    /// </summary>
    public class CalendarModeChangedEventArgs : System.Windows.RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the CalendarModeChangedEventArgs class.
        /// </summary>
        /// <param name="oldMode">Previous value of the property, prior to the event being raised.</param>
        /// <param name="newMode">Current value of the property at the time of the event.</param>
        public CalendarModeChangedEventArgs(CalendarMode oldMode, CalendarMode newMode)
        {
            this.OldMode = oldMode;
            this.NewMode = newMode;
        }

        /// <summary>
        /// Gets the new mode of the Calendar.
        /// </summary>
        public CalendarMode NewMode
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the previous mode of the Calendar.
        /// </summary>
        public CalendarMode OldMode
        {
            get;
            private set;
        }
    }
}
