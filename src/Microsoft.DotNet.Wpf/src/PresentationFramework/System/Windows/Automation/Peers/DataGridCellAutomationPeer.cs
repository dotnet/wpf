// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using MS.Internal;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer for DataGridCell
    /// </summary>
    public sealed class DataGridCellAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridCell.
        /// This automation peer should not be part of the automation tree.
        /// It should act as a wrapper peer for DataGridCellItemAutomationPeer
        /// </summary>
        /// <param name="owner">DataGridCell</param>
        public DataGridCellAutomationPeer(DataGridCell owner)
            : base(owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
        }

        #endregion
        
        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
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

        #endregion
    }
}
