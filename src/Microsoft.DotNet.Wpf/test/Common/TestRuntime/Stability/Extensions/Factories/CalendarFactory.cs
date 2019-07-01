// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create Calendar.
    /// </summary>
    [TargetTypeAttribute(typeof(Calendar))]
    internal class CalendarFactory : DiscoverableFactory<Calendar>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a ConstrainedDateTime to set Calendar DisplayDate property.
        /// </summary>
        public ConstrainedDateTime DisplayDate { get; set; }

        /// <summary>
        /// Gets or sets a ConstrainedDateTime to set Calendar DisplayDateEnd property.
        /// </summary>
        public ConstrainedDateTime DisplayDateEnd { get; set; }

        /// <summary>
        /// Gets or sets a ConstrainedDateTime to set Calendar DisplayDateStart property.
        /// </summary>
        public ConstrainedDateTime DisplayDateStart { get; set; }

        /// <summary>
        /// Gets or sets a ConstrainedDateTime to set Calendar SelectedDate property.
        /// </summary>
        public ConstrainedDateTime SelectedDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Calendar is TodayHighlighted or not property.
        /// </summary>
        public bool IsTodayHighlighted { get; set; }

        /// <summary>
        /// Gets or sets a DayOfWeek to set Calendar FirstDayOfWeek property.
        /// </summary>
        public DayOfWeek FirstDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets a CalendarSelectionMode to set Calendar SelectionMode property.
        /// </summary>
        public CalendarSelectionMode SelectionMode { get; set; }

        /// <summary>
        /// Gets or sets a CalendarMode to set Calendar DisplayMode property.
        /// </summary>
        public CalendarMode DisplayMode { get; set; }

        /// <summary>
        /// Gets or sets a list of CalendarDateRange to set Calendar BlackoutDates property.
        /// </summary>
        public List<CalendarDateRange> BlackoutDates { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Createa a Calendar.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Calendar Create(DeterministicRandom random)
        {
            Calendar calendar = new Calendar();

            if (DisplayDate != null)
            {
                calendar.DisplayDate = (DateTime)DisplayDate.GetData(random);
            }

            if (DisplayDateEnd != null)
            {
                calendar.DisplayDateEnd = (DateTime)DisplayDateEnd.GetData(random);
            }

            if (DisplayDateStart != null)
            {
                calendar.DisplayDateStart = (DateTime)DisplayDateStart.GetData(random);
            }

            calendar.FirstDayOfWeek = FirstDayOfWeek;
            calendar.IsTodayHighlighted = IsTodayHighlighted;
            calendar.SelectionMode = SelectionMode;
            calendar.DisplayMode = DisplayMode;
            HomelessTestHelpers.Merge(calendar.BlackoutDates, BlackoutDates);

            //SelectedDate property cannot be set when the selection mode is None.
            // and
            //BlackoutDates cannot contain the SelectedDate.
            if (SelectedDate != null && calendar.SelectionMode != CalendarSelectionMode.None)
            {
                DateTime date = (DateTime)SelectedDate.GetData(random);
                if (!calendar.BlackoutDates.Contains(date))
                {
                    calendar.SelectedDate = date;
                }
            }

            return calendar;
        }

        #endregion
    }
#endif
}
