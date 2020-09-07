// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;

namespace System.Windows.Controls
{
    /// <summary>
    /// Provides data for the DateSelected and DisplayDateChanged events.
    /// </summary>
    public class CalendarDateChangedEventArgs : System.Windows.RoutedEventArgs
    {
        internal CalendarDateChangedEventArgs(DateTime? removedDate, DateTime? addedDate)
        {
            this.RemovedDate = removedDate;
            this.AddedDate = addedDate;
        }

        /// <summary>
        /// Gets the date to be newly displayed.
        /// </summary>
        public DateTime? AddedDate
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the date that was previously displayed.
        /// </summary>
        public DateTime? RemovedDate
        {
            get;
            private set;
        }
    }
}
