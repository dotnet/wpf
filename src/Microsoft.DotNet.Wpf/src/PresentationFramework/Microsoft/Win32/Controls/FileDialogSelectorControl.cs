// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    using System;

    /// <summary>
    ///<see cref="FileDialogSelectorControl "/> is a base class for controls that can be used in file dialogs and that allow a user to select one of its items.
    /// </summary>
    public abstract class FileDialogSelectorControl : FileDialogItemsControl
    {
        // Verified: when a selected item is removed, selection is cleared and GetSelectedControlItem returns E_FAIL
        // Verified: when non-selected item is removed, selection is preserved

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogSelectorControl"/> class using the specified items.
        /// </summary>
        /// <param name="items">The items to add to the control.</param>
        protected FileDialogSelectorControl(params string[] items) : base(items) { }

        /// <summary>
        /// Gets or sets the index of the selected item.
        /// </summary>
        /// <remarks>
        /// When the dialog is shown, items cannot be unselected.
        /// </remarks>
        public int SelectedIndex
        {
            get
            {
                return Items.IndexOf(SelectedItem);
            }
            set
            {
                if (value == -1)
                {
                    SelectedItem = null;
                }
                else if (value < 0 || value >= Items.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                SelectedItem = Items[value];
            }
        }

        // We do not need to update selection during removal of items because it is returned directly from dialog.
        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <remarks>
        /// When the dialog is shown, items cannot be unselected.
        /// </remarks>
        public FileDialogControlItem SelectedItem
        {
            get
            {
                if (Owner is IFileDialogCustomize owner)
                {
                    // when nothing is selected the call fails
                    if (owner.GetSelectedControlItem(ID, out int itemID).Succeeded)
                    {
                        int index = Items.IndexOf(itemID);
                        if (index >= 0)
                        {
                            return Items[index];
                        }
                    }

                    return null;
                }
                else
                {
                    if (_selectedItem?.Container == this)
                    {
                        return _selectedItem;
                    }
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    // item cannot be unselected while dialog is shown
                    if (HasOwner)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        _selectedItem = null;
                        return;
                    }
                }

                // not our item
                if (value.Container != this)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (Owner is IFileDialogCustomize owner)
                {
                    owner.SetSelectedControlItem(ID, value.ID);
                }
                else
                {
                    _selectedItem = value;
                }
            }
        }

        /// <inheritdoc />
        internal override void CacheState()
        {
            base.CacheState();
            _selectedItem = SelectedItem;
        }
        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            base.LockAndAttachInternal(owner); // adds items to the dialog

            if (SelectedItem is FileDialogControlItem item)
            {
                owner.SetSelectedControlItem(ID, item.ID);
            }
        }

        /// <summary>
        /// Occurs when an item is selected by a user.
        /// </summary>
        public event EventHandler<FileDialogItemEventArgs> ItemSelected;

        /// <summary>
        /// Invokes the <see cref="ItemSelected"/> event handler.
        /// </summary>
        /// <param name="itemID"></param>
        internal void RaiseItemSelected(int itemID)
        {
            int index = Items.IndexOf(itemID);
            if (index >= 0)
            {
                ItemSelected?.Invoke(this, new FileDialogItemEventArgs(Items[index]));
            }
        }

        private FileDialogControlItem _selectedItem;
    }


    /// <summary>
    /// Provides data for the ItemSelected event.
    /// </summary>
    public class FileDialogItemEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileDialogItemEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item associated with the event.</param>
        public FileDialogItemEventArgs(FileDialogControlItem item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets the item associated with the event.
        /// </summary>
        public FileDialogControlItem Item { get; }
    }
}

