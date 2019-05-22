// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Extends the Selector class by adding a multi selection support.
    /// </summary>
    public abstract class MultiSelector : Selector
    {
        /// <summary>
        /// Returns whether or not multiple items can be selected
        /// </summary>
        protected bool CanSelectMultipleItems
        {
            get { return base.CanSelectMultiple; }
            set { base.CanSelectMultiple = value; }
        }

        /// <summary>
        /// Returns the collection of currently Selected Items.
        /// Note, this is not the set of items that are pending selection.
        /// Exceptions:
        /// While IsUpdatingSelectedItems, using the indexer, Insert, and RemoveAt will throw InvalidOperationExceptions.
        /// If CanSelectMultiple is false then Adding one item to SelectedItems is valid but adding items after that is invalid and will result in an InvalidOperationException.
        /// </summary>
        [Bindable(true), Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList SelectedItems
        {
            get
            {
                return SelectedItemsImpl;
            }
        }

        #region BulkSelection

        /// <summary>
        /// Starts a new selection transaction, setting IsUpdatingSelectedItems to true.
        /// Exceptions: InvalidOperationException if IsUpdatingSelectedItems is true. 
        /// </summary>
        protected void BeginUpdateSelectedItems()
        {
            ((SelectedItemCollection)SelectedItems).BeginUpdateSelectedItems();
        }

        /// <summary>
        /// Commits a selection transaction, populating or removing items from the SelectedItems collection and resets IsUpdatingSelectedItems to false.
        /// Exceptions: InvalidOperationException if IsUpdatingSelectedItems is false 
        /// </summary>
        protected void EndUpdateSelectedItems()
        {
            ((SelectedItemCollection)SelectedItems).EndUpdateSelectedItems();
        }

        /// <summary>
        /// Returns true if SelectedItems is being updated using the deferred update behavior.
        /// Otherwise, it is false and updating SelectedItems is immediate.
        /// Calling BeginUpdateSelectedItems will set this value to become true.
        /// Calling EndUpdateSelectedItems will cause the deferred selections to be submitted and this value to become false.
        /// </summary>
        protected bool IsUpdatingSelectedItems
        {
            get
            {
                return ((SelectedItemCollection)SelectedItems).IsUpdatingSelectedItems;
            }
        }

        /// <summary>
        ///     Select all the items
        /// Exceptions: InvalidOperationExcpetion if CanSelectMultipleItems is false
        /// </summary>
        public void SelectAll()
        {
            if (CanSelectMultipleItems)
            {
                SelectAllImpl();
            }
            else
            {
                throw new NotSupportedException(SR.Get(SRID.MultiSelectorSelectAll));
            }
        }

        /// <summary>
        ///     Clears all of the selected items.
        /// </summary>
        public void UnselectAll()
        {
            UnselectAllImpl();
        }

        #endregion BulkSelection
    }
}

