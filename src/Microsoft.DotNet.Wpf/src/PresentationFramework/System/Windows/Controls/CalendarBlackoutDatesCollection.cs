// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    /// Represents a collection of DateTimeRanges.
    /// </summary>
    public sealed class CalendarBlackoutDatesCollection : ObservableCollection<CalendarDateRange>
    {
        #region Data

        private Thread _dispatcherThread;
        private Calendar _owner;

        #endregion Data

        /// <summary>
        /// Initializes a new instance of the CalendarBlackoutDatesCollection class.
        /// </summary>
        /// <param name="owner"></param>
        public CalendarBlackoutDatesCollection(Calendar owner)
        {
            _owner = owner;
            this._dispatcherThread = Thread.CurrentThread;
        }

        #region Public Methods

        /// <summary>
        /// Dates that are in the past are added to the BlackoutDates.
        /// </summary>
        public void AddDatesInPast()
        {
            this.Add(new CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1)));
        }

        /// <summary>
        /// Checks if a DateTime is in the Collection
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool Contains(DateTime date)
        {
            return null != GetContainingDateRange(date);
        }

        /// <summary>
        /// Checks if a Range is in the collection
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public bool Contains(DateTime start, DateTime end)
        {
            DateTime rangeStart, rangeEnd;
            int n = Count;

            if (DateTime.Compare(end, start) > -1)
            {
                rangeStart = DateTimeHelper.DiscardTime(start).Value;
                rangeEnd = DateTimeHelper.DiscardTime(end).Value;
            }
            else
            {
                rangeStart = DateTimeHelper.DiscardTime(end).Value;
                rangeEnd = DateTimeHelper.DiscardTime(start).Value;
            }

            for (int i = 0; i < n; i++)
            {
                if (DateTime.Compare(this[i].Start, rangeStart) == 0 && DateTime.Compare(this[i].End, rangeEnd) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if any day in the given DateTime range is contained in the BlackOutDays.
        /// </summary>
        /// <param name="range">CalendarDateRange that is searched in BlackOutDays</param>
        /// <returns>true if at least one day in the range is included in the BlackOutDays</returns>
        public bool ContainsAny(CalendarDateRange range)
        {
            foreach (CalendarDateRange item in this)
            {
                if (item.ContainsAny(range))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This finds the next date that is not blacked out in a certian direction.
        /// </summary>
        /// <param name="requestedDate"></param>
        /// <param name="dayInterval"></param>
        /// <returns></returns>
        internal DateTime? GetNonBlackoutDate(DateTime? requestedDate, int dayInterval)
        {
            Debug.Assert(dayInterval != 0);

            DateTime? currentDate = requestedDate;
            CalendarDateRange range = null;

            if (requestedDate == null)
            {
                return null;
            }

            if ((range = GetContainingDateRange((DateTime)currentDate)) == null)
            {
                return requestedDate;
            }

            do
            {
                if (dayInterval > 0)
                {
                    // Moving Forwards.
                    // The DateRanges require start <= end
                    currentDate = DateTimeHelper.AddDays(range.End, dayInterval );
                }
                else
                {
                    //Moving backwards.
                    currentDate = DateTimeHelper.AddDays(range.Start, dayInterval );
                }
            } while (currentDate != null && ((range = GetContainingDateRange((DateTime)currentDate)) != null));



            return currentDate;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// All the items in the collection are removed.
        /// </summary>
        protected override void ClearItems()
        {
            if (!IsValidThread())
            {
                throw new NotSupportedException(SR.Get(SRID.CalendarCollection_MultiThreadedCollectionChangeNotSupported));
            }

            foreach (CalendarDateRange item in Items)
            {
                UnRegisterItem(item);
            }

            base.ClearItems();
            this._owner.UpdateCellItems();
        }

        /// <summary>
        /// The item is inserted in the specified place in the collection.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        protected override void InsertItem(int index, CalendarDateRange item)
        {
            if (!IsValidThread())
            {
                throw new NotSupportedException(SR.Get(SRID.CalendarCollection_MultiThreadedCollectionChangeNotSupported));
            }

            if (IsValid(item))
            {
                RegisterItem(item);
                base.InsertItem(index, item);
                _owner.UpdateCellItems();
            }
            else
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.Calendar_UnSelectableDates));
            }
        }

        /// <summary>
        /// The item in the specified index is removed from the collection.
        /// </summary>
        /// <param name="index"></param>
        protected override void RemoveItem(int index)
        {
            if (!IsValidThread())
            {
                throw new NotSupportedException(SR.Get(SRID.CalendarCollection_MultiThreadedCollectionChangeNotSupported));
            }

            if (index >= 0 && index < this.Count)
            {
                UnRegisterItem(Items[index]);
            }

            base.RemoveItem(index);
            _owner.UpdateCellItems();
        }

        /// <summary>
        /// The object in the specified index is replaced with the provided item.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        protected override void SetItem(int index, CalendarDateRange item)
        {
            if (!IsValidThread())
            {
                throw new NotSupportedException(SR.Get(SRID.CalendarCollection_MultiThreadedCollectionChangeNotSupported));
            }

            if (IsValid(item))
            {
                CalendarDateRange oldItem = null;
                if (index >= 0 && index < this.Count)
                {
                    oldItem = Items[index];
                }

                base.SetItem(index, item);

                UnRegisterItem(oldItem);
                RegisterItem(Items[index]);

                _owner.UpdateCellItems();
            }
            else
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.Calendar_UnSelectableDates));
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Registers for change notification on date ranges
        /// </summary>
        /// <param name="item"></param>
        private void RegisterItem(CalendarDateRange item)
        {
            if (item != null)
            {
                item.Changing += new EventHandler<CalendarDateRangeChangingEventArgs>(Item_Changing);
                item.PropertyChanged += new PropertyChangedEventHandler(Item_PropertyChanged);
            }
        }

        /// <summary>
        /// Un registers for change notification on date ranges
        /// </summary>
        private void UnRegisterItem(CalendarDateRange item)
        {
            if (item != null)
            {
                item.Changing -= new EventHandler<CalendarDateRangeChangingEventArgs>(Item_Changing);
                item.PropertyChanged -= new PropertyChangedEventHandler(Item_PropertyChanged);
            }
        }

        /// <summary>
        /// Reject date range changes that would make the blackout dates collection invalid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_Changing(object sender, CalendarDateRangeChangingEventArgs e)
        {
            CalendarDateRange item = sender as CalendarDateRange;
            if (item != null)
            {
                if (!IsValid(e.Start, e.End))
                {
                    throw new ArgumentOutOfRangeException(SR.Get(SRID.Calendar_UnSelectableDates));
                }
            }
        }

        /// <summary>
        /// Update the calendar view to reflect the new blackout dates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CalendarDateRange)
            {
                _owner.UpdateCellItems();
            }
        }

        /// <summary>
        /// Tests to see if a date range is not already selected
        /// </summary>
        /// <param name="item">date range to test</param>
        /// <returns>True if no selected day falls in the given date range</returns>
        private bool IsValid(CalendarDateRange item)
        {
            return IsValid(item.Start, item.End);
        }

        /// <summary>
        /// Tests to see if a date range is not already selected
        /// </summary>
        /// <param name="start">First day of date range to test</param>
        /// <param name="end">Last day of date range to test</param>
        /// <returns>True if no selected day falls between start and end</returns>
        private bool IsValid(DateTime start, DateTime end)
        {
            foreach (object child in _owner.SelectedDates)
            {
                DateTime? day = child as DateTime?;
                Debug.Assert(day != null);
                if (DateTimeHelper.InRange(day.Value, start, end))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsValidThread()
        {
            return Thread.CurrentThread == this._dispatcherThread;
        }

        /// <summary>
        /// Gets the DateRange that contains the date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private CalendarDateRange GetContainingDateRange(DateTime date)
        {
            for (int i = 0; i < Count; i++)
            {
                if (DateTimeHelper.InRange(date, this[i]))
                {
                    return this[i];
                }
            }
            return null;
        }
        #endregion Private Methods
		
    }
}

