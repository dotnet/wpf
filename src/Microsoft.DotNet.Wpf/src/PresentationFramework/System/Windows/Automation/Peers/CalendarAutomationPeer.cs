// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MS.Internal.Automation;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for Calendar Control
    /// </summary>
    public sealed class CalendarAutomationPeer : FrameworkElementAutomationPeer, IGridProvider, IMultipleViewProvider, ISelectionProvider, ITableProvider, IItemContainerProvider
    {
        /// <summary>
        /// Initializes a new instance of the CalendarAutomationPeer class.
        /// </summary>
        /// <param name="owner">Owning Calendar</param>
        public CalendarAutomationPeer(System.Windows.Controls.Calendar owner)
            : base(owner)
        {
        }

        #region Private Properties

        private System.Windows.Controls.Calendar OwningCalendar
        {
            get
            {
                return this.Owner as System.Windows.Controls.Calendar;
            }
        }

        private Grid OwningGrid
        {
            get
            {
                if (this.OwningCalendar != null && this.OwningCalendar.MonthControl != null)
                {
                    if (this.OwningCalendar.DisplayMode == CalendarMode.Month)
                    {
                        return this.OwningCalendar.MonthControl.MonthView;
                    }
                    else
                    {
                        return this.OwningCalendar.MonthControl.YearView;
                    }
                }

                return null;
            }
        }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Gets the control pattern that is associated with the specified System.Windows.Automation.Peers.PatternInterface.
        /// </summary>
        /// <param name="patternInterface">A value from the System.Windows.Automation.Peers.PatternInterface enumeration.</param>
        /// <returns>The object that supports the specified pattern, or null if unsupported.</returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.Grid:
                case PatternInterface.Table:
                case PatternInterface.MultipleView:
                case PatternInterface.Selection:
                case PatternInterface.ItemContainer:
                    {
                        if (this.OwningGrid != null)
                        {
                            return this;
                        }

                        break;
                    }

                default: break;
            }

            return base.GetPattern(patternInterface);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Calendar;
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            if (OwningCalendar.MonthControl == null)
            {
                return null;
            }

            List<AutomationPeer> peers = new List<AutomationPeer>();
            Dictionary<DateTimeCalendarModePair, DateTimeAutomationPeer> newChildren = new Dictionary<DateTimeCalendarModePair,DateTimeAutomationPeer>();

            // Step 1: Add previous, header and next buttons
            AutomationPeer buttonPeer;
            buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(OwningCalendar.MonthControl.PreviousButton);
            if (buttonPeer != null)
            {
                peers.Add(buttonPeer);
            }
            buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(OwningCalendar.MonthControl.HeaderButton);
            if (buttonPeer != null)
            {
                peers.Add(buttonPeer);
            }
            buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(OwningCalendar.MonthControl.NextButton);
            if (buttonPeer != null)
            {
                peers.Add(buttonPeer);
            }

            // Step 2: Add Calendar Buttons depending on the Calendar.DisplayMode
            DateTime date;
            DateTimeAutomationPeer peer;
            foreach (UIElement child in this.OwningGrid.Children)
            {
                int childRow = (int)child.GetValue(Grid.RowProperty);
                // first row is day titles
                if (OwningCalendar.DisplayMode == CalendarMode.Month && childRow == 0)
                {
                    AutomationPeer dayTitlePeer = UIElementAutomationPeer.CreatePeerForElement(child);
                    if (dayTitlePeer != null)
                    {
                        peers.Add(dayTitlePeer);
                    }
                }
                else
                {
                    Button owningButton = child as Button;
                    if (owningButton != null && owningButton.DataContext is DateTime)
                    {
                        date = (DateTime)owningButton.DataContext;
                        peer = GetOrCreateDateTimeAutomationPeer(date, OwningCalendar.DisplayMode, /*addParentInfo*/ false);
                        peers.Add(peer);

                        DateTimeCalendarModePair key = new DateTimeCalendarModePair(date, OwningCalendar.DisplayMode);
                        newChildren.Add(key, peer);
                    }
                }
            }

            DateTimePeers = newChildren;
            return peers;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType, 
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        protected override void SetFocusCore()
        {
            System.Windows.Controls.Calendar owner = OwningCalendar;
            if (owner.Focusable)
            {
                if (!owner.Focus())
                {
                    DateTime focusedDate;
                    // Focus should have moved to either SelectedDate or DisplayDate
                    if (owner.SelectedDate.HasValue && DateTimeHelper.CompareYearMonth(owner.SelectedDate.Value, owner.DisplayDateInternal) == 0)
                    {
                        focusedDate = owner.SelectedDate.Value;
                    }
                    else
                    {
                        focusedDate = owner.DisplayDate;
                    }

                    DateTimeAutomationPeer focusedItem = GetOrCreateDateTimeAutomationPeer(focusedDate, owner.DisplayMode, /*addParentInfo*/ false);
                    FrameworkElement focusedButton = focusedItem.OwningButton;

                    if (focusedButton == null || !focusedButton.IsKeyboardFocused)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
            }
        }

        #endregion Protected Methods

        #region InternalMethods

        private DateTimeAutomationPeer GetOrCreateDateTimeAutomationPeer(DateTime date, CalendarMode buttonMode)
        {
            return GetOrCreateDateTimeAutomationPeer(date, buttonMode, /*addParentInfo*/ true);
        }
        
        private DateTimeAutomationPeer GetOrCreateDateTimeAutomationPeer(DateTime date, CalendarMode buttonMode, bool addParentInfo)
        {
            // try to reuse old peer if it exists either in Current AT or in WeakRefStorage of Peers being sent to Client
            DateTimeCalendarModePair key = new DateTimeCalendarModePair(date, buttonMode);
            DateTimeAutomationPeer peer = null;
            DateTimePeers.TryGetValue(key, out peer);

            if (peer == null)
            {
                peer = GetPeerFromWeakRefStorage(key);
                if (peer != null && !addParentInfo)
                {
                    // As cached peer is getting used it must be invalidated. addParentInfo check ensures that call is coming from GetChildrenCore
                    peer.AncestorsInvalid = false;
                    peer.ChildrenValid = false;
                }
            }

            if( peer == null )
            {
                peer = new DateTimeAutomationPeer(date, OwningCalendar, buttonMode);
                
                // Sets hwnd and parent info
                if (addParentInfo)
                {
                    if(peer != null)
                        peer.TrySetParentInfo(this);
                }
            }
            // Set EventsSource if visual exists
            AutomationPeer wrapperPeer = peer.WrapperPeer;
            if (wrapperPeer != null)
            {
                wrapperPeer.EventsSource = peer;
            }

            return peer;
        }

        // Provides Peer if exist in Weak Reference Storage
        private DateTimeAutomationPeer GetPeerFromWeakRefStorage(DateTimeCalendarModePair dateTimeCalendarModePairKey)
        {
            DateTimeAutomationPeer returnPeer = null;
            WeakReference weakRefEP = null;
            WeakRefElementProxyStorage.TryGetValue(dateTimeCalendarModePairKey, out weakRefEP);

            if (weakRefEP != null)
            {
                ElementProxy provider = weakRefEP.Target as ElementProxy;
                if (provider != null)
                {
                    returnPeer = PeerFromProvider(provider as IRawElementProviderSimple) as DateTimeAutomationPeer;
                    if (returnPeer == null)
                        WeakRefElementProxyStorage.Remove(dateTimeCalendarModePairKey);
                }
                else
                    WeakRefElementProxyStorage.Remove(dateTimeCalendarModePairKey);
            }

            return returnPeer;
        }

        // Called by DateTimeAutomationPeer
        internal void AddProxyToWeakRefStorage(WeakReference wr, DateTimeAutomationPeer dateTimePeer)
        {
            DateTimeCalendarModePair key = new DateTimeCalendarModePair(dateTimePeer.Date, dateTimePeer.ButtonMode);

            if (GetPeerFromWeakRefStorage(key) == null)
                WeakRefElementProxyStorage.Add(key, wr);
        }


        internal void RaiseSelectionEvents(SelectionChangedEventArgs e)
        {
            int numSelected = OwningCalendar.SelectedDates.Count;
            int numAdded = e.AddedItems.Count;

            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected) && numSelected == 1 && numAdded == 1)
            {
                DateTimeAutomationPeer peer = GetOrCreateDateTimeAutomationPeer((DateTime)e.AddedItems[0], CalendarMode.Month);
                if (peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
                }
            }
            else
            {
                if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementAddedToSelection))
                {
                    foreach (DateTime date in e.AddedItems)
                    {
                        DateTimeAutomationPeer peer = GetOrCreateDateTimeAutomationPeer(date, CalendarMode.Month);
                        if (peer != null)
                        {
                            peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
                        }
                    }
                }
            }

            if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
            {
                foreach (DateTime date in e.RemovedItems)
                {
                    DateTimeAutomationPeer peer = GetOrCreateDateTimeAutomationPeer(date, CalendarMode.Month);
                    if (peer != null)
                    {
                        peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
                    }
                }
            }
        }

        #endregion InternalMethods

        #region IGridProvider

        int IGridProvider.ColumnCount
        {
            get
            {
                if (this.OwningGrid != null)
                {
                    return this.OwningGrid.ColumnDefinitions.Count;
                }

                return 0;
            }
        }

        int IGridProvider.RowCount
        {
            get
            {
                if (this.OwningGrid != null)
                {
                    if (this.OwningCalendar.DisplayMode == CalendarMode.Month)
                    {
                        // In Month DisplayMode, since first row is DayTitles, we return the RowCount-1
                        return Math.Max(0, this.OwningGrid.RowDefinitions.Count - 1);
                    }
                    else
                    {
                        return this.OwningGrid.RowDefinitions.Count;
                    }
                }

                return 0;
            }
        }

        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            if (this.OwningCalendar.DisplayMode == CalendarMode.Month)
            {
                // In Month DisplayMode, since first row is DayTitles, we increment the row number by 1
                row++;
            }

            if (this.OwningGrid != null && row >= 0 && row < this.OwningGrid.RowDefinitions.Count && column >= 0 && column < this.OwningGrid.ColumnDefinitions.Count)
            {
                foreach (UIElement child in this.OwningGrid.Children)
                {
                    int childRow = (int)child.GetValue(Grid.RowProperty);
                    int childColumn = (int)child.GetValue(Grid.ColumnProperty);
                    if (childRow == row && childColumn == column)
                    {
                        object dataContext = (child as FrameworkElement).DataContext;
                        if (dataContext is DateTime)
                        {
                            DateTime date = (DateTime)dataContext;
                            AutomationPeer peer = GetOrCreateDateTimeAutomationPeer(date, OwningCalendar.DisplayMode);
                            return ProviderFromPeer(peer);
                        }
                    }
                }
            }

            return null;
        }

        #endregion IGridProvider

        #region IMultipleViewProvider

        int IMultipleViewProvider.CurrentView 
        { 
            get 
            { 
                return (int)this.OwningCalendar.DisplayMode; 
            } 
        }

        int[] IMultipleViewProvider.GetSupportedViews()
        {
            int[] supportedViews = new int[3];

            supportedViews[0] = (int)CalendarMode.Month;
            supportedViews[1] = (int)CalendarMode.Year;
            supportedViews[2] = (int)CalendarMode.Decade;

            return supportedViews;
        }

        string IMultipleViewProvider.GetViewName(int viewId)
        {
            switch (viewId)
            {
                case 0:
                    {
                        return SR.Get(SRID.CalendarAutomationPeer_MonthMode);
                    }

                case 1:
                    {
                        return SR.Get(SRID.CalendarAutomationPeer_YearMode);
                    }

                case 2:
                    {
                        return SR.Get(SRID.CalendarAutomationPeer_DecadeMode);
                    }
            }

            return String.Empty;
        }

        void IMultipleViewProvider.SetCurrentView(int viewId)
        {
            this.OwningCalendar.DisplayMode = (CalendarMode)viewId;
        }

        #endregion IMultipleViewProvider

        #region ISelectionProvider

        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                return this.OwningCalendar.SelectionMode == CalendarSelectionMode.SingleRange || this.OwningCalendar.SelectionMode == CalendarSelectionMode.MultipleRange;
            }
        }

        bool ISelectionProvider.IsSelectionRequired 
        { 
            get 
            { 
                return false; 
            } 
        }

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            List<IRawElementProviderSimple> providers = new List<IRawElementProviderSimple>();
            
            foreach (DateTime date in OwningCalendar.SelectedDates)
            {
                AutomationPeer peer = GetOrCreateDateTimeAutomationPeer(date, CalendarMode.Month);
                providers.Add(ProviderFromPeer(peer));
            }

            if (providers.Count > 0)
            {
                return providers.ToArray();
            }

            return null;
        }

        #endregion ISelectionProvider

        #region IItemContainerProvider
        
        IRawElementProviderSimple IItemContainerProvider.FindItemByProperty(IRawElementProviderSimple startAfterProvider, int propertyId, object value)
        {
            DateTimeAutomationPeer startAfterDatePeer = null;
            
            if (startAfterProvider != null)
            {
                startAfterDatePeer = PeerFromProvider(startAfterProvider) as DateTimeAutomationPeer;
                // if provider is not null, peer must exist
                if (startAfterDatePeer == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.InavalidStartItem));
                }
            }

            DateTime? nextDate = null;
            CalendarMode currentMode = 0;

            if( propertyId == SelectionItemPatternIdentifiers.IsSelectedProperty.Id)
            {
                currentMode = CalendarMode.Month;
                nextDate = GetNextSelectedDate(startAfterDatePeer, (bool)value);
            }
            else if (propertyId == AutomationElementIdentifiers.NameProperty.Id)
            {
                // finds the button for the given DateTime
                DateTimeFormatInfo format = DateTimeHelper.GetCurrentDateFormat();
                DateTime parsedDate;
                if (DateTime.TryParse((value as string), format, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    nextDate = parsedDate;
                }

                if( !nextDate.HasValue || (startAfterDatePeer != null && nextDate <= startAfterDatePeer.Date) )
                {
                    throw new InvalidOperationException(SR.Get(SRID.CalendarNamePropertyValueNotValid));
                }

                currentMode = (startAfterDatePeer != null) ? startAfterDatePeer.ButtonMode : OwningCalendar.DisplayMode;
            }
            else if (propertyId == 0 || propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id)
            {
                // propertyId = 0 returns the button next to the startAfter or the DisplayDate if startAfter is null
                // All items here are buttons, so same behaviour as propertyId = 0
                if (propertyId == AutomationElementIdentifiers.ControlTypeProperty.Id && (int)value != ControlType.Button.Id)
                {
                    return null;
                }
                currentMode = (startAfterDatePeer != null) ? startAfterDatePeer.ButtonMode : OwningCalendar.DisplayMode;
                nextDate = GetNextDate(startAfterDatePeer, currentMode);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.PropertyNotSupported));
            }

            if (nextDate.HasValue)
            {
                AutomationPeer nextPeer = GetOrCreateDateTimeAutomationPeer(nextDate.Value, currentMode);
                if (nextPeer != null)
                {
                    return ProviderFromPeer(nextPeer);
                }
            }
            return null;
        }

        private DateTime? GetNextDate(DateTimeAutomationPeer currentDatePeer, CalendarMode currentMode)
        {
            DateTime? nextDate = null;

            DateTime startDate = (currentDatePeer != null) ? currentDatePeer.Date : OwningCalendar.DisplayDate;
            
            if (currentMode == CalendarMode.Month)
                nextDate = startDate.AddDays(1);
            else if (currentMode == CalendarMode.Year)
                nextDate = startDate.AddMonths(1);
            else if (currentMode == CalendarMode.Decade)
                nextDate = startDate.AddYears(1);

            return nextDate;
        }

        private DateTime? GetNextSelectedDate(DateTimeAutomationPeer currentDatePeer, bool isSelected)
        {
            DateTime startDate = (currentDatePeer != null) ? currentDatePeer.Date : OwningCalendar.DisplayDate;

            if (isSelected)
            {
                // If SelectedDates is empty or startDate is beyond last SelectedDate
                if (!OwningCalendar.SelectedDates.MaximumDate.HasValue || OwningCalendar.SelectedDates.MaximumDate <= startDate)
                {
                    return null;
                }
                // startDate is before first SelectedDate
                if (OwningCalendar.SelectedDates.MinimumDate.HasValue && startDate < OwningCalendar.SelectedDates.MinimumDate)
                {
                    return OwningCalendar.SelectedDates.MinimumDate;
                }
            }
            while (true)
            {
                startDate = startDate.AddDays(1);
                if (OwningCalendar.SelectedDates.Contains(startDate) == isSelected)
                {
                    break;
                }
            }

            return startDate;
        }

        #endregion IItemContainerProvider

        #region ITableProvider

        RowOrColumnMajor ITableProvider.RowOrColumnMajor
        {
            get
            {
                return RowOrColumnMajor.RowMajor;
            }
        }

        IRawElementProviderSimple[] ITableProvider.GetColumnHeaders()
        {
            if (this.OwningCalendar.DisplayMode == CalendarMode.Month)
            {
                List<IRawElementProviderSimple> providers = new List<IRawElementProviderSimple>();

                foreach (UIElement child in this.OwningGrid.Children)
                {
                    int childRow = (int)child.GetValue(Grid.RowProperty);

                    if (childRow == 0)
                    {
                        AutomationPeer peer = CreatePeerForElement(child);

                        if (peer != null)
                        {
                            providers.Add(ProviderFromPeer(peer));
                        }
                    }
                }

                if (providers.Count > 0)
                {
                    return providers.ToArray();
                }
            }

            return null;
        }

        // If WeekNumber functionality is supported by Calendar in the future,
        // this method should return weeknumbers
        IRawElementProviderSimple[] ITableProvider.GetRowHeaders()
        {
            return null;
        }

        #endregion ITableProvider

        /// <summary>
        /// Used to cache realized peers. We donot store references to virtualized peers.
        /// </summary>
        private Dictionary<DateTimeCalendarModePair, DateTimeAutomationPeer> DateTimePeers
        {
            get { return _dataChildren; }

            set { _dataChildren = value; }
        }

        private Dictionary<DateTimeCalendarModePair, WeakReference> WeakRefElementProxyStorage
        {
            get { return _weakRefElementProxyStorage; }
        }

        #region Private Data
        private Dictionary<DateTimeCalendarModePair, DateTimeAutomationPeer> _dataChildren = new Dictionary<DateTimeCalendarModePair, DateTimeAutomationPeer>();
        private Dictionary<DateTimeCalendarModePair, WeakReference> _weakRefElementProxyStorage = new Dictionary<DateTimeCalendarModePair, WeakReference>();

        #endregion Private Data
    }


    internal struct DateTimeCalendarModePair
    {
        internal DateTimeCalendarModePair(DateTime date, CalendarMode mode)
        {
            ButtonMode = mode;
            Date = date;
        }

       CalendarMode ButtonMode;
       DateTime Date;
    }
}
