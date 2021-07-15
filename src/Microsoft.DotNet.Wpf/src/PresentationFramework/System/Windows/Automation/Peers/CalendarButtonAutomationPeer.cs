// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for CalendarButton and CalendarDayButton
    /// </summary>
    public sealed class CalendarButtonAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// This peer is not a part of the AutomationTree.
        /// It acts as a wrapper class for DateTimeAutomationPeer
        /// </summary>
        /// <param name="owner">Owning CalendarButton or CalendarDayButton</param>
        public CalendarButtonAutomationPeer(Button owner)
            : base(owner)
        {
        }

        #region Private Properties

        private bool IsDayButton
        {
            get
            {
                return (Owner is CalendarDayButton);
            }
        }

        #endregion Private Properties
        #region Protected Methods

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
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
        /// Overrides the GetLocalizedControlTypeCore method for CalendarButtonAutomationPeer
        /// </summary>
        /// <returns></returns>
        protected override string GetLocalizedControlTypeCore()
        {
            return IsDayButton ? SR.Get(SRID.CalendarAutomationPeer_DayButtonLocalizedControlType) : SR.Get(SRID.CalendarAutomationPeer_CalendarButtonLocalizedControlType);
        }

        #endregion Protected Methods
    }
}
