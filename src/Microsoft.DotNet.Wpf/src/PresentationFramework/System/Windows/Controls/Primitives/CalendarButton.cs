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
    public sealed class CalendarButton : Button
    {
        /// <summary>
        /// Static constructor
        /// </summary>
        static CalendarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarButton), new FrameworkPropertyMetadata(typeof(CalendarButton)));
        }

        /// <summary>
        /// Represents the CalendarButton that is used in Calendar Control.
        /// </summary>
        public CalendarButton()
            : base()
        {
        }

        #region Public Properties

        #region HasSelectedDays

        internal static readonly DependencyPropertyKey HasSelectedDaysPropertyKey = DependencyProperty.RegisterReadOnly(
            "HasSelectedDays",
            typeof(bool),
            typeof(CalendarButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for HasSelectedDays property
        /// </summary>
        public static readonly DependencyProperty HasSelectedDaysProperty = HasSelectedDaysPropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarButton represents a date range containing the display date
        /// </summary>
        public bool HasSelectedDays
        {
            get { return (bool)GetValue(HasSelectedDaysProperty); }
            internal set { SetValue(HasSelectedDaysPropertyKey, value); }
        }

        #endregion HasSelectedDays

        #region IsInactive

        internal static readonly DependencyPropertyKey IsInactivePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsInactive",
            typeof(bool),
            typeof(CalendarButton),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnVisualStatePropertyChanged)));

        /// <summary>
        /// Dependency property field for IsInactive property
        /// </summary>
        public static readonly DependencyProperty IsInactiveProperty = IsInactivePropertyKey.DependencyProperty;

        /// <summary>
        /// True if the CalendarButton represents
        ///     a month that falls outside the current year
        ///     or
        ///     a year that falls outside the current decade
        /// </summary>
        public bool IsInactive
        {
            get { return (bool)GetValue(IsInactiveProperty); }
            internal set { SetValue(IsInactivePropertyKey, value); }
        }

        #endregion IsInactive

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
        /// Change to the correct visual state for the button.
        /// </summary>
        /// <param name="useTransitions">
        /// true to use transitions when updating the visual state, false to
        /// snap directly to the new visual state.
        /// </param>
        internal override void ChangeVisualState(bool useTransitions)
        {
            // Update the SelectionStates group
            if (HasSelectedDays)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateSelected, VisualStates.StateUnselected);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnselected, useTransitions);
            }

            // Update the ActiveStates group
            if (!IsInactive)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateActive, VisualStates.StateInactive); 
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateInactive, useTransitions);
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

        /// <summary>
        /// Creates the automation peer for the DayButton.
        /// </summary>
        /// <returns></returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CalendarButtonAutomationPeer(this);
        }

        #endregion Protected Methods

        #region Internal Methods

        internal void SetContentInternal(string value)
        {
            SetCurrentValueInternal(ContentControl.ContentProperty, value);
        }

        #endregion
    }
}
