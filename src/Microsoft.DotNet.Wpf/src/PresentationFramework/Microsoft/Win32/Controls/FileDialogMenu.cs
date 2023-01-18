// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using System;
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents the menu control for file dialogs.
    /// </summary>
    public sealed class FileDialogMenu : FileDialogItemsControl
    {
        // Verified: GetSelectedControlItem not available

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogMenu"/>.
        /// </summary>
        public FileDialogMenu()
        {
            Label = string.Empty;
        }
        /// <summary>
        /// Initializes a new instenace of <see cref="FileDialogMenu"/> using the specified items.
        /// </summary>
        /// <param name="label">The menu label. Accelerators are supported.</param>
        /// <param name="items">The items to add to the menu.</param>
        public FileDialogMenu(string label, params string[] items) : base(items)
        {
            Label = label;
        }

        /// <summary>
        /// Gets or sets the control label. Supports accelerators.
        /// </summary>
        public string Label
        {
            get { return HasOwner ? _attachedLabel : _label; }
            set
            {
                value ??= string.Empty;

                if (Owner is IFileDialogCustomize owner)
                {
                    Owner?.SetControlLabel(ID, ConvertAccelerators(value));
                    _attachedLabel = value;
                }
                else
                {
                    _label = value;
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
        /// Occurs when a menu item is selected.
        /// </summary>
        public event EventHandler<FileDialogItemEventArgs> ItemSelected;

        /// <summary>
        /// Invokes the <see cref="Activating"/> event handler.
        /// </summary>
        /// <param name="itemID"></param>
        internal void RaiseActivating()
        {
            Activating?.Invoke(this, EventArgs.Empty);
        }

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

        /// <inheritdoc/>
        internal override void CacheState()
        {
            base.CacheState();
            _label = _attachedLabel;
        }
        /// <inheritdoc/>
        private protected override void LockAndAttachContainer(IFileDialogCustomize owner)
        {
            _attachedLabel = _label;
            owner.AddMenu(ID, ConvertAccelerators(_attachedLabel));
        }
        /// <inheritdoc/>
        private protected override FileDialogItemsControl CloneContainer()
        {
            return new FileDialogMenu();
        }

        // Native API does not allow us to get the current label from the dialog,
        // but it allows us to change it, so we need to maintain a separate copy.
        private string _label;
        private string _attachedLabel;
    }
}

