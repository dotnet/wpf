// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for CalendarDayButton and CalendarButton
    /// </summary>
    public sealed class DateTimeAutomationPeer : AutomationPeer, IGridItemProvider, ISelectionItemProvider, ITableItemProvider, IInvokeProvider , IVirtualizedItemProvider
    {
        /// <summary>
        /// Initializes a new instance of the DateTimeAutomationPeer class.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="owningCalendar"></param>
        /// <param name="buttonMode"></param>
        internal DateTimeAutomationPeer(DateTime date, Calendar owningCalendar, CalendarMode buttonMode)
            : base()
        {
            if (date == null)
            {
                throw new ArgumentNullException("date");
            }
            if (owningCalendar == null)
            {
                throw new ArgumentNullException("owningCalendar");
            }

            Date = date;
            ButtonMode = buttonMode;
            OwningCalendar = owningCalendar;
        }

        ///
        internal override bool AncestorsInvalid
        {
            get { return base.AncestorsInvalid; }
            set
            {
                base.AncestorsInvalid = value;
                if (value)
                    return;
                AutomationPeer wrapperPeer = WrapperPeer;
                if (wrapperPeer != null)
                {
                    wrapperPeer.AncestorsInvalid = false;
                }
            }
        }

        #region Private Properties

        private Calendar OwningCalendar
        {
            get;
            set;
        }

        internal DateTime Date
        {
            get;
            private set;
        }

        internal CalendarMode ButtonMode
        {
            get;
            private set;
        }

        internal bool IsDayButton
        {
            get
            {
                return ButtonMode == CalendarMode.Month;
            }
        }

        private IRawElementProviderSimple OwningCalendarProvider
        {
            get
            {
                if (this.OwningCalendar != null)
                {
                    AutomationPeer peer = FrameworkElementAutomationPeer.CreatePeerForElement(this.OwningCalendar);

                    if (peer != null)
                    {
                        return ProviderFromPeer(peer);
                    }
                }

                return null;
            }
        }

        internal Button OwningButton
        {
            get
            {
                if (OwningCalendar.DisplayMode != ButtonMode)
                {
                    return null;
                }

                if (IsDayButton)
                {
                    return OwningCalendar.MonthControl?.GetCalendarDayButton(this.Date);
                }
                else
                {
                    return OwningCalendar.MonthControl?.GetCalendarButton(this.Date, ButtonMode);
                }
            }
        }

        internal FrameworkElementAutomationPeer WrapperPeer
        {
            get
            {
                Button owningButton = OwningButton;
                if (owningButton != null)
                {
                    return FrameworkElementAutomationPeer.CreatePeerForElement(owningButton) as FrameworkElementAutomationPeer;
                }
                return null;
            }
        }

        #endregion Private Properties

        #region AutomationPeer override Methods
                
        protected override string GetAcceleratorKeyCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetAcceleratorKey();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return String.Empty;
        }

        protected override string GetAccessKeyCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetAccessKey();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return String.Empty;
        }
        
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }
        
        protected override string GetAutomationIdCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetAutomationId();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return String.Empty;
        }
        
        protected override Rect GetBoundingRectangleCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetBoundingRectangle();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return new Rect();
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetChildren();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return null;
        }

        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            return (wrapperPeer != null) ? wrapperPeer.GetClassName() : (IsDayButton)? "CalendarDayButton" : "CalendarButton";
        }

        protected override Point GetClickablePointCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClickablePoint();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return new Point(double.NaN, double.NaN);
        }

        protected override string GetHelpTextCore()
        {
            string dateString = DateTimeHelper.ToLongDateString(Date, DateTimeHelper.GetCulture(OwningCalendar));
            if (IsDayButton && this.OwningCalendar.BlackoutDates.Contains(Date))
            {
                return string.Format(DateTimeHelper.GetCurrentDateFormat(), SR.Get(SRID.CalendarAutomationPeer_BlackoutDayHelpText), dateString);
            }

            return dateString;
        }
    
        protected override string GetItemStatusCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetItemStatus();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return String.Empty;
        }

        protected override string GetItemTypeCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetItemType();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return String.Empty;
        }

        protected override AutomationPeer GetLabeledByCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetLabeledBy();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return null;
        }

        protected override AutomationLiveSetting GetLiveSettingCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetLiveSetting();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return AutomationLiveSetting.Off;
        }

        protected override string GetLocalizedControlTypeCore()
        {
            return IsDayButton ? SR.Get(SRID.CalendarAutomationPeer_DayButtonLocalizedControlType) : SR.Get(SRID.CalendarAutomationPeer_CalendarButtonLocalizedControlType);
        }

        protected override string GetNameCore()
        {
            string dateString = "";

            switch (ButtonMode)
            {
                case CalendarMode.Month:
                    dateString = DateTimeHelper.ToLongDateString(Date, DateTimeHelper.GetCulture(OwningCalendar));
                    break;
                case CalendarMode.Year:
                    dateString = DateTimeHelper.ToYearMonthPatternString(Date, DateTimeHelper.GetCulture(OwningCalendar));
                    break;
                case CalendarMode.Decade:
                    dateString = DateTimeHelper.ToYearString(Date, DateTimeHelper.GetCulture(OwningCalendar));
                    break;
            }

            return dateString;
        }

        protected override AutomationOrientation GetOrientationCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetOrientation();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return AutomationOrientation.None;
        }

        /// <summary>
        /// Gets the control pattern that is associated with the specified System.Windows.Automation.Peers.PatternInterface.
        /// </summary>
        /// <param name="patternInterface">A value from the System.Windows.Automation.Peers.PatternInterface enumeration.</param>
        /// <returns>The object that supports the specified pattern, or null if unsupported.</returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            object result = null;
            Button owningButton = OwningButton;

            switch (patternInterface)
            {
                case PatternInterface.Invoke:
                case PatternInterface.GridItem:
                    {
                        if (owningButton != null)
                        {
                            result = this;
                        }
                        break;
                    }
                case PatternInterface.TableItem:
                    {
                        if (IsDayButton && owningButton != null)
                        {
                            result = this;
                        }
                        break;
                    }
                case PatternInterface.SelectionItem:
                    {
                        result = this;
                        break;
                    }
                case PatternInterface.VirtualizedItem:
                    if (VirtualizedItemPatternIdentifiers.Pattern != null)
                    {
                        if (owningButton == null)
                        {
                            result = this;
                        }
                        else
                        {
                            // If the Item is in Automation Tree we consider it Realized and need not return VirtualizedItem pattern.
                            if (!IsItemInAutomationTree())
                            {
                                return this;
                            }
                        }
                    }
                    break;

                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets the position of a this DateTime element within a set.
        /// </summary>
        /// <remarks>
        /// Forwards the call to the wrapperPeer.
        /// </remarks>
        /// <returns>
        /// The PositionInSet property value from the wrapper peer
        /// </returns>
        protected override int GetPositionInSetCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetPositionInSet();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return AutomationProperties.AutomationPositionInSetDefault;
        }

        /// <summary>
        /// Gets the size of a set that contains this DateTime element.
        /// </summary>
        /// <remarks>
        /// Forwards the call to the wrapperPeer.
        /// </remarks>
        /// <returns>
        /// The SizeOfSet property value from the wrapper peer
        /// </returns>
        protected override int GetSizeOfSetCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetSizeOfSet();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return AutomationProperties.AutomationSizeOfSetDefault;
        }

        internal override Rect GetVisibleBoundingRectCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            return (wrapperPeer != null) ? wrapperPeer.GetVisibleBoundingRect() : GetBoundingRectangle();
        }

        protected override bool HasKeyboardFocusCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.HasKeyboardFocus();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return false;
        }

        protected override bool IsContentElementCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsContentElement();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return true;
        }

        protected override bool IsControlElementCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsControlElement();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return true;
        }

        protected override bool IsEnabledCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsEnabled();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return false;
        }

        protected override bool IsKeyboardFocusableCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsKeyboardFocusable();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return false;
        }
        
        protected override bool IsOffscreenCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsOffscreen();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return true;
        }

        protected override bool IsPasswordCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsPassword();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return false;
        }
        
        protected override bool IsRequiredForFormCore()
        {
            AutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.IsRequiredForForm();
            }
            else
            {
                ThrowElementNotAvailableException();
            }

            return false;
        }
        
        protected override void SetFocusCore()
        {
            UIElementAutomationPeer wrapperPeer = WrapperPeer;
            if (wrapperPeer != null)
            {
                wrapperPeer.SetFocus();
            }
            else
            {
                ThrowElementNotAvailableException();
            }
        }

        #endregion AutomationPeer override Methods

        #region IGridItemProvider

        /// <summary>
        /// Grid item column.
        /// </summary>
        int IGridItemProvider.Column
        {
            get
            {
                Button owningButton = OwningButton;
                if (owningButton != null)
                {
                    return (int)owningButton.GetValue(Grid.ColumnProperty);
                }
                else
                {
                    throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
                }
            }
        }

        /// <summary>
        /// Grid item column span.
        /// </summary>
        int IGridItemProvider.ColumnSpan
        {
            get
            {
                Button owningButton = OwningButton;
                if (owningButton != null)
                {
                    return (int)owningButton.GetValue(Grid.ColumnSpanProperty);
                }
                else
                {
                    throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
                }
            }
        }

        /// <summary>
        /// Grid item's containing grid.
        /// </summary>
        IRawElementProviderSimple IGridItemProvider.ContainingGrid
        {
            get
            {
                return this.OwningCalendarProvider;
            }
        }

        /// <summary>
        /// Grid item row.
        /// </summary>
        int IGridItemProvider.Row
        {
            get
            {
                Button owningButton = OwningButton;
                if (owningButton != null)
                {
                    if (IsDayButton)
                    {
                        Debug.Assert((int)owningButton.GetValue(Grid.RowProperty) > 0);

                        // we decrement the Row value by one since the first row is composed of DayTitles
                        return (int)owningButton.GetValue(Grid.RowProperty) - 1;
                    }
                    else
                    {
                        return (int)owningButton.GetValue(Grid.RowProperty);
                    }
                }
                else
                {
                    throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
                }
            }
        }

        /// <summary>
        /// Grid item row span.
        /// </summary>
        int IGridItemProvider.RowSpan
        {
            get
            {
                Button owningButton = OwningButton;
                if (owningButton != null)
                {
                    if (IsDayButton)
                    {
                        return (int)owningButton.GetValue(Grid.RowSpanProperty);
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
                }
            }
        }

        #endregion IGridItemProvider

        #region ISelectionItemProvider

        /// <summary>
        /// True if the owning CalendarDayButton is selected.
        /// </summary>
        bool ISelectionItemProvider.IsSelected 
        { 
            get 
            {
                if (IsDayButton)
                {
                    return this.OwningCalendar.SelectedDates.Contains(Date);
                }

                return false;
            } 
        }

        /// <summary>
        /// Selection items selection container.
        /// </summary>
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                return this.OwningCalendarProvider;
            }
        }

        /// <summary>
        /// Adds selection item to selection.
        /// </summary>
        void ISelectionItemProvider.AddToSelection()
        {
            // Return if the day is already selected or if it is a BlackoutDate
            if (((ISelectionItemProvider)this).IsSelected)
            {
                return;
            }

            if (IsDayButton && EnsureSelection())
            {
                if (this.OwningCalendar.SelectionMode == CalendarSelectionMode.SingleDate)
                {
                    this.OwningCalendar.SelectedDate = Date;
                }
                else
                {
                    this.OwningCalendar.SelectedDates.Add(Date);
                }
            }

            return;
        }

        /// <summary>
        /// Removes selection item from selection.
        /// </summary>
        void ISelectionItemProvider.RemoveFromSelection()
        {
            // Return if the item is not already selected.
            if (!((ISelectionItemProvider)this).IsSelected)
            {
                return;
            }

            if (IsDayButton)
            {
                this.OwningCalendar.SelectedDates.Remove(Date);
            }

            return;
        }

        /// <summary>
        /// Selects this item.
        /// </summary>
        void ISelectionItemProvider.Select()
        {
            Button owningButton = OwningButton;
            if (IsDayButton)
            {
                if (EnsureSelection() && this.OwningCalendar.SelectionMode == CalendarSelectionMode.SingleDate)
                {
                    this.OwningCalendar.SelectedDate = Date;
                }
            }
            else if (owningButton != null && owningButton.IsEnabled)
            {
                owningButton.Focus();
            }
        }

        #endregion ISelectionItemProvider

        #region ITableItemProvider

        /// <summary>
        /// Gets the table item's column headers.
        /// </summary>
        /// <returns>The table item's column headers</returns>
        IRawElementProviderSimple[] ITableItemProvider.GetColumnHeaderItems()
        {
            if (IsDayButton && OwningButton != null)
            {
                if (this.OwningCalendar != null && this.OwningCalendarProvider != null)
                {
                    IRawElementProviderSimple[] headers = ((ITableProvider)FrameworkElementAutomationPeer.CreatePeerForElement(this.OwningCalendar)).GetColumnHeaders();

                    if (headers != null)
                    {
                        int column = ((IGridItemProvider)this).Column;
                        return new IRawElementProviderSimple[] { headers[column] };
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get's the table item's row headers.
        /// </summary>
        /// <returns>The table item's row headers</returns>
        IRawElementProviderSimple[] ITableItemProvider.GetRowHeaderItems()
        {
            return null;
        }

        #endregion ITableItemProvider

        #region IInvokeProvider
        
        void IInvokeProvider.Invoke()
        {
            Button owningButton = OwningButton;
            if (owningButton == null || !this.IsEnabled())
                throw new ElementNotEnabledException();

            // Async call of click event
            // In ClickHandler opens a dialog and suspend the execution we don't want to block this thread
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
            {
                owningButton.AutomationButtonBaseClick();
               
                return null;
            }), null);
        }
        
        #endregion IInvokeProvider

        # region IVirtualizedItemProvider

        void IVirtualizedItemProvider.Realize()
        {
            // Change Display mode
            if (OwningCalendar.DisplayMode != ButtonMode)
            {
                OwningCalendar.DisplayMode = ButtonMode;
            }
            
            // Bring into view
            OwningCalendar.DisplayDate = this.Date;
        }

        #endregion IVirtualizedItemProvider

        override internal bool IsDataItemAutomationPeer()
        {
            return true;
        }

        override internal void AddToParentProxyWeakRefCache()
        {
            CalendarAutomationPeer owningCalendarPeer = FrameworkElementAutomationPeer.CreatePeerForElement(OwningCalendar) as CalendarAutomationPeer;
            if (owningCalendarPeer != null)
            {
                owningCalendarPeer.AddProxyToWeakRefStorage(this.ElementProxyWeakReference, this);
            }
        }

        #region Private Methods

        private bool EnsureSelection()
        {
            // If the day is a blackout day or the SelectionMode is None, selection is not allowed
            if (this.OwningCalendar.BlackoutDates.Contains(Date) ||
                this.OwningCalendar.SelectionMode == CalendarSelectionMode.None)
            {
                return false;
            }

            return true;
        }

        private bool IsItemInAutomationTree()
        {
            AutomationPeer parent = this.GetParent();
            if (this.Index != -1 && parent != null && parent.Children != null && this.Index < parent.Children.Count && parent.Children[this.Index] == this)
                return true;
            else return false;
        }

        private void ThrowElementNotAvailableException()
        {
            // To avoid the situation on legacy systems which may not have new unmanaged core. this check with old unmanaged core
            // avoids throwing exception and provide older behavior returning default values for items which are virtualized rather than throwing exception.
            if (VirtualizedItemPatternIdentifiers.Pattern != null)
                throw new ElementNotAvailableException(SR.Get(SRID.VirtualizedElement));
        }

        #endregion Private Methods
    }
}
