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
    /// AutomationPeer for DataGridRow
    /// </summary>
    public sealed class DataGridRowAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridRow
        /// </summary>
        /// <param name="owner">DataGridRow</param>
        public DataGridRowAutomationPeer(DataGridRow owner)
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
            return AutomationControlType.DataItem;
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

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // see whether the DataGridRow uses the standard control template
            DataGridCellsPresenter cellsPresenter = OwningDataGridRow.CellsPresenter;
            if (cellsPresenter != null && cellsPresenter.ItemsHost != null)
            {
                // this is the normal case
                List<AutomationPeer> children = new List<AutomationPeer>(3);

                // Step 1: Add row header if exists
                AutomationPeer dataGridRowHeaderAutomationPeer = RowHeaderAutomationPeer;
                if (dataGridRowHeaderAutomationPeer != null)
                {
                    children.Add(dataGridRowHeaderAutomationPeer);
                }

                // Step 2: Add all cells
                DataGridItemAutomationPeer itemPeer = this.EventsSource as DataGridItemAutomationPeer;
                if (itemPeer != null)
                {
                    children.AddRange(itemPeer.GetCellItemPeers());
                }

                // Step 3: Add DetailsPresenter last if exists
                AutomationPeer dataGridDetailsPresenterAutomationPeer = DetailsPresenterAutomationPeer;
                if (dataGridDetailsPresenterAutomationPeer != null)
                {
                    children.Add(dataGridDetailsPresenterAutomationPeer);
                }

                return children;
            }
            else
            {
                // in the unusual case where the app uses a non-standard control template
                // for the DataGridRow, fall back to the base implementation
                return base.GetChildrenCore();
            }
        }

        #endregion

        #region Private helpers
        internal AutomationPeer RowHeaderAutomationPeer
        {
            get
            {
                DataGridRowHeader dataGridRowHeader = OwningDataGridRow.RowHeader;
                if (dataGridRowHeader != null)
                {
                    return CreatePeerForElement(dataGridRowHeader);
                }

                return null;
            }
        }

        private AutomationPeer DetailsPresenterAutomationPeer
        {
            get
            {
                DataGridDetailsPresenter dataGridDetailsPresenter = OwningDataGridRow.DetailsPresenter;
                if (dataGridDetailsPresenter != null)
                {
                    return CreatePeerForElement(dataGridDetailsPresenter);
                }

                return null;
            }
        }

        private DataGridRow OwningDataGridRow
        {
            get
            {
                return (DataGridRow)Owner;
            }
        }

        #endregion
    }
}
