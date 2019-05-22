// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Panel that lays out individual rows top to bottom.  
    /// </summary>
    public class DataGridRowsPresenter : VirtualizingStackPanel
    {
        #region Scroll Methods

        /// <summary>
        ///     Calls the protected method BringIndexIntoView.
        /// </summary>
        /// <param name="index">The index of the row to scroll into view.</param>
        /// <remarks>
        ///     BringIndexIntoView should be callable either from the ItemsControl
        ///     or directly on the panel. This was not done in WPF, so we are
        ///     building this internally for the DataGrid. However, if a public
        ///     way of calling BringIndexIntoView becomes a reality, then
        ///     this method is no longer needed.
        /// </remarks>
        internal void InternalBringIndexIntoView(int index)
        {
            BringIndexIntoView(index);
        }

        /// <summary>
        ///     This method is invoked when the IsItemsHost property changes.
        /// </summary>
        /// <param name="oldIsItemsHost">The old value of the IsItemsHost property.</param>
        /// <param name="newIsItemsHost">The new value of the IsItemsHost property.</param>
        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
        {
            base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);

            if (newIsItemsHost)
            {
                DataGrid dataGrid = Owner;
                if (dataGrid != null)
                {
                    // ItemsHost should be the "root" element which has
                    // IsItemsHost = true on it.  In the case of grouping,
                    // IsItemsHost is true on all panels which are generating
                    // content.  Thus, we care only about the panel which
                    // is generating content for the ItemsControl.
                    IItemContainerGenerator generator = dataGrid.ItemContainerGenerator as IItemContainerGenerator;
                    if (generator != null && generator == generator.GetItemContainerGeneratorForPanel(this))
                    {
                        dataGrid.InternalItemsHost = this;
                    }
                }
            }
            else
            {
                // No longer the items host, clear out the property on the DataGrid
                if ((_owner != null) && (_owner.InternalItemsHost == this))
                {
                    _owner.InternalItemsHost = null;
                }

                _owner = null;
            }
        }

        /// <summary>
        ///     Override of ViewportSizeChanged method which enqueues a dispatcher operation to redistribute
        ///     the width among columns.
        /// </summary>
        /// <param name="oldViewportSize">viewport size before the change</param>
        /// <param name="newViewportSize">viewport size after the change</param>
        protected override void OnViewportSizeChanged(Size oldViewportSize, Size newViewportSize)
        {
            DataGrid dataGrid = Owner;
            if (dataGrid != null)
            {
                ScrollContentPresenter scrollContentPresenter = dataGrid.InternalScrollContentPresenter;
                if (scrollContentPresenter == null || scrollContentPresenter.CanContentScroll)
                {
                    dataGrid.OnViewportSizeChanged(oldViewportSize, newViewportSize);
                }
            }
        }

        #endregion

        #region Column Virtualization

        /// <summary>
        ///     Measure
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            _availableSize = constraint;
            return base.MeasureOverride(constraint);
        }

        /// <summary>
        ///     Property which returns the last measure input size
        /// </summary>
        internal Size AvailableSize
        {
            get
            {
                return _availableSize;
            }
        }

        #endregion

        #region Row Virtualization

        /// <summary>
        ///     Override method to suppress the cleanup of rows
        ///     with validation errors.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnCleanUpVirtualizedItem(CleanUpVirtualizedItemEventArgs e)
        {
            base.OnCleanUpVirtualizedItem(e);

            if (e.UIElement != null &&
                Validation.GetHasError(e.UIElement))
            {
                e.Cancel = true;
            }
        }

        #endregion

        #region Helpers

        internal DataGrid Owner
        {
            get
            {
                if (_owner == null)
                {
                    _owner = ItemsControl.GetItemsOwner(this) as DataGrid;
                }

                return _owner;
            }
        }

        #endregion

        #region Data

        private DataGrid _owner;
        private Size _availableSize;

        #endregion
    }
}