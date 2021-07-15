// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    /// Event arguments to notify clients that the range is changing and what the new range will be
    /// </summary>
    internal class CalendarDateRangeChangingEventArgs : EventArgs
    {
        public CalendarDateRangeChangingEventArgs(DateTime start, DateTime end)
        {
            _start = start;
            _end = end;
        }

        public DateTime Start
        {
            get { return _start; }
        }

        public DateTime End
        {
            get { return _end; }
        }

        private DateTime _start;
        private DateTime _end;
    }
}