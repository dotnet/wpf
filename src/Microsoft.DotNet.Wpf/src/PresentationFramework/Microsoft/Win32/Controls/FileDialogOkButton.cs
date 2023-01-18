// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    using System;

    // File dialog buttons are special because they have very limited functionality and cannot be part of collections
    /// <summary>
    /// Represents the OK button on file dialogs.
    /// </summary>
    public sealed class FileDialogOkButton : IFileDialogCustomizeOwner
    {
        // Verified: setting selection not supported, no selection event, but reading selected item works
        // Verified: disabling and hiding not supported, not even for read
        // Verified: items can be added while dialog is shown even when there were none when it opened and vice versa

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogOkButton"/>.
        /// </summary>
        internal FileDialogOkButton()
        {
            _id = FileDialogControlBase.NextID();
            Items = new FileDialogControlItemCollection(this);
        }

        public FileDialogControlItemCollection Items { get; }

        /// <summary>
        /// Gets or sets a custom OK button label. Supports accelerators. 
        /// </summary>
        /// <remarks>
        /// Populating the <see cref="Items"/> collection takes precedence over <see cref="CustomLabel"/>.
        /// </remarks>
        public string CustomLabel
        {
            get { return _owner != null ? _attachedLabel : _label; }
            set
            {
                if (_owner is IFileDialog dialog)
                {
                    dialog.SetOkButtonLabel(FileDialogControlBase.ConvertAccelerators(value));
                    _attachedLabel = value;
                }
                else
                {
                    _label = value;
                }
            }
        }

        /// <summary>
        /// Gets the index of the drop down item that confirmed the file dialog.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return Items.IndexOf(SelectedItem);
            }
        }

        /// <summary>
        /// Gets the item that confirmed the file dialog.
        /// </summary>
        public FileDialogControlItem SelectedItem
        {
            get
            {
                if (_owner is IFileDialogCustomize owner)
                {
                    if (owner.GetSelectedControlItem(_id, out int itemID).Succeeded)
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
                    return _selectedItem;
                }
            }
        }

        /// <summary>
        /// Occurs when the menu is about to display its contents.
        /// </summary>
        /// <remarks>
        /// In response to this notification, an application can update the contents of the menu to be displayed, based on the current state of the dialog.
        /// </remarks>
        public event EventHandler Activating;

        /// <summary>
        /// Invokes the <see cref="Activating"/> event handler.
        /// </summary>
        /// <param name="itemID"></param>
        internal void RaiseActivating()
        {
            Activating?.Invoke(this, EventArgs.Empty);
        }

        internal void OnItemsCleared()
        {
            // Custom label only applies when there are no items. However, if all items are removed, 
            // the text of the last item removed will be used as the new button label (Clear() is not supported).
            // Ensure the CustomLabel is reapplied if all items are removed.

            if (CustomLabel is string label && !string.IsNullOrEmpty(label))
            {
                CustomLabel = label;
            }
        }
        /// <summary>
        /// Update the local state with the current state from the dialog.
        /// </summary>
        internal void CacheState()
        {
            _selectedItem = SelectedItem;
            _label = _attachedLabel;
        }
        /// <summary>
        /// Add the control to the owning dialog and apply any pending state.
        /// Some modifications of the control may no longer be allowed.
        /// </summary>
        internal void LockAndAttach(IFileDialogCustomize owner)
        {
            if (!string.IsNullOrEmpty(_label) && owner is IFileDialog dialog)
            {
                dialog.SetOkButtonLabel(FileDialogControlBase.ConvertAccelerators(_label));
            }

            // we do not need to lock but competing with OpenFileDialog.ShowReadOnly
            if (!Items.IsLocked)
            {
                owner.EnableOpenDropDown(_id);
            }

            foreach (FileDialogControlItem item in Items)
            {
                item.AddToDialog(owner);
            }

            _selectedItem = null;
            _owner = owner;
        }
        /// <summary>
        /// Release the reference to the dialog and allow design-time modifications again.
        /// </summary>
        internal void DetachAndUnlock()
        {
            Items.Unlock();
            _owner = null;
        }

        int IFileDialogCustomizeOwner.ID => _id;
        IFileDialogCustomize IFileDialogCustomizeOwner.Owner => _owner;
        private FileDialogControlItem _selectedItem;
        private IFileDialogCustomize _owner;
        private int _id;

        // Native API does not allow us to get the current label from the dialog,
        // but it allows us to change it, so we need to maintain a separate copy.
        private string _attachedLabel;
        private string _label;
    }
}

