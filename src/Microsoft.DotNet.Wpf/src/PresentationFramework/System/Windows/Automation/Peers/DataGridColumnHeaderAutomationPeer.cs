// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MS.Internal;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for DataGridColumnHeader
    /// </summary>
    public sealed class DataGridColumnHeaderAutomationPeer : ButtonBaseAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridColumnHeader
        /// </summary>
        /// <param name="owner">DataGridColumnHeader</param>
        public DataGridColumnHeaderAutomationPeer(DataGridColumnHeader owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.HeaderItem;
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

        // AutomationControlType.HeaderItem must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms742202.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }

        #endregion
    }
}
