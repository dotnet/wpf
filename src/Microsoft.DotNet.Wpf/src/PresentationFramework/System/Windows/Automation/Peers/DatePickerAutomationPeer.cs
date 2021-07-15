// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for DatePicker Control
    /// </summary>
    public sealed class DatePickerAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IValueProvider
    {
        /// <summary>
        /// Initializes a new instance of the AutomationPeer for DatePicker control.
        /// </summary>
        /// <param name="owner">DatePicker</param>
        public DatePickerAutomationPeer(DatePicker owner)
            : base(owner)
        {
        }

        #region Private Properties

        private DatePicker OwningDatePicker
        {
            get
            {
                return this.Owner as DatePicker;
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
            if (patternInterface == PatternInterface.ExpandCollapse || patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void SetFocusCore()
        {
            DatePicker owner = OwningDatePicker;
            if (owner.Focusable)
            {
                if (!owner.Focus())
                {
                    TextBox tb = owner.TextBox;
                    //The focus should have gone to the TextBox inside DatePicker
                    if (tb == null || !tb.IsKeyboardFocused)
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

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();
            
            if (OwningDatePicker.IsDropDownOpen && OwningDatePicker.Calendar != null)
            {
                CalendarAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(OwningDatePicker.Calendar) as CalendarAutomationPeer;
                if (peer != null)
                {
                    children.Add(peer);
                }
            }
            return children;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType, 
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        /// <summary>
        /// Overrides the GetLocalizedControlTypeCore method for DatePicker
        /// </summary>
        /// <returns></returns>
        protected override string GetLocalizedControlTypeCore()
        {
            return SR.Get(SRID.DatePickerAutomationPeer_LocalizedControlType);
        }

        #endregion Protected Methods

        #region IExpandCollapseProvider

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                if (this.OwningDatePicker.IsDropDownOpen)
                {
                    return ExpandCollapseState.Expanded;
                }
                else
                {
                    return ExpandCollapseState.Collapsed;
                }
            }
        }

        void IExpandCollapseProvider.Collapse()
        {
            this.OwningDatePicker.IsDropDownOpen = false;
        }

        void IExpandCollapseProvider.Expand()
        {
            this.OwningDatePicker.IsDropDownOpen = true;
        }

        #endregion IExpandCollapseProvider

        #region IValueProvider

        bool IValueProvider.IsReadOnly 
        { 
            get { return false; } 
        }

        string IValueProvider.Value 
        {
            get { return this.OwningDatePicker.ToString(); } 
        }

        void IValueProvider.SetValue(string value)
        {
            this.OwningDatePicker.Text = value;
        }

        #endregion IValueProvider

        #region Internal Methods
        // Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
        {
            if (oldValue != newValue)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
        }
        #endregion
    }
}
