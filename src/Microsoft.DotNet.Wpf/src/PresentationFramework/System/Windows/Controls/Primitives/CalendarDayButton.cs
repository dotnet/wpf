// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Represents a button control used in Calendar Control, which reacts to the Click event.
    /// </summary>
    public sealed class CalendarDayButton : Button
    {
        #region Constants
        /// <summary>
        /// Default content for the CalendarDayButton
        /// </summary>
        private const int DEFAULTCONTENT = 1;

        #endregion

        /// <summary>
        /// Static constructor
        /// </summary>
        static CalendarDayButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarDayButton), new FrameworkPropertyMetadata(typeof(CalendarDayButton)));
        }

        /// <summary>
        /// Represents the CalendarDayButton that is used in Calendar Control.
        /// </summary>
        public CalendarDayButton()
            : base()
        {        
        }

        #region Public Properties

        #region IsToday

        internal static readonly DependencyPropertyKey IsTodayPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsToday",
            typeof(bool),
            typeof(CalendarDayButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for IsToday property
        /// </summary>
        public static readonly DependencyProperty IsTodayProperty = IsTodayPropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarDayButton represents today
        /// </summary>
        public bool IsToday
        {
            get { return (bool)GetValue(IsTodayProperty); }
        }

        #endregion IsToday

        #region IsSelected

        internal static readonly DependencyPropertyKey IsSelectedPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsSelected",
            typeof(bool),
            typeof(CalendarDayButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for IsSelected property
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = IsSelectedPropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarDayButton is selected
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
        }

        #endregion IsSelected

        #region IsInactive

        internal static readonly DependencyPropertyKey IsInactivePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsInactive",
            typeof(bool),
            typeof(CalendarDayButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for IsActive property
        /// </summary>
        public static readonly DependencyProperty IsInactiveProperty = IsInactivePropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarDayButton represents a day that falls in the currently displayed month
        /// </summary>
        public bool IsInactive
        {
            get { return (bool)GetValue(IsInactiveProperty); }
        }

        #endregion IsInactive

        #region IsBlackedOut

        internal static readonly DependencyPropertyKey IsBlackedOutPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsBlackedOut",
            typeof(bool),
            typeof(CalendarDayButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for IsBlackedOut property
        /// </summary>
        public static readonly DependencyProperty IsBlackedOutProperty = IsBlackedOutPropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarDayButton represents a blackout date
        /// </summary>
        public bool IsBlackedOut
        {
            get { return (bool)GetValue(IsBlackedOutProperty); }
        }

        #endregion IsBlackedOut

        #region IsHighlighted

        internal static readonly DependencyPropertyKey IsHighlightedPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsHighlighted",
            typeof(bool),
            typeof(CalendarDayButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for IsHighlighted property
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty = IsHighlightedPropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarDayButton represents a highlighted date
        /// </summary>
        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
        }

        #endregion IsHighlighted

        #endregion Public Properties

        #region Internal Properties

        internal Calendar Owner
        {
            get;
            set;
        }

        #endregion Internal Properties

        #region Public Methods

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Creates the automation peer for the CalendarDayButton.
        /// </summary>
        /// <returns></returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CalendarButtonAutomationPeer(this);
        }

        #endregion Protected Methods

        #region Internal Methods

        /// <summary>
        /// Change to the correct visual state for the button.
        /// </summary>
        /// <param name="useTransitions">
        /// true to use transitions when updating the visual state, false to
        /// snap directly to the new visual state.
        /// </param>
        internal override void ChangeVisualState(bool useTransitions)
        {
            // During the visual state change, refresh group state so that proper foreground and background colors are set.

            // Update the ActiveStates group
            // (Visual state "Inactive" doesn't take effect on previous selected CalendarDayButton in other month)
            // Force refresh to have matching InActive foreground and background colors.
            VisualStates.GoToState(this, useTransitions, VisualStates.StateActive, VisualStates.StateInactive);
            if (IsInactive)
            {
                VisualStateManager.GoToState(this, VisualStates.StateInactive, useTransitions);
            }

            // Update the DayStates group
            // (Visual state "Today" doesn't take effect when select and unselect today)
            // Force refresh to have matching Today foreground and background colors.
            VisualStateManager.GoToState(this, VisualStates.StateRegularDay, useTransitions);
            if (IsToday && this.Owner != null && this.Owner.IsTodayHighlighted)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateToday, VisualStates.StateRegularDay);
            }

            // Update the SelectionStates group last, to take priority over previous group state changes.
            // Force refresh to have matching Selected foreground and background colors.
            VisualStateManager.GoToState(this, VisualStates.StateUnselected, useTransitions);
            if (IsSelected || IsHighlighted)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected, VisualStates.StateUnselected);
            }

            // Update the BlackoutDayStates group
            if (IsBlackedOut)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateBlackoutDay, VisualStates.StateNormalDay);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormalDay, useTransitions);
            }

            // Update the FocusStates group
            if (IsKeyboardFocused)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateCalendarButtonFocused, VisualStates.StateCalendarButtonUnfocused);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateCalendarButtonUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        internal void NotifyNeedsVisualStateUpdate()
        {
            UpdateVisualState();
        }

        internal void SetContentInternal(string value)
        {
            SetCurrentValueInternal(ContentControl.ContentProperty, value);
        }

        #endregion Internal Methods
    }
}
