// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    // Not inheritable due to internal abstract members (LockAndAttachContainer).
    /// <summary>
    /// <see cref="FileDialogItemsControl"/> is a base class for controls that can be used in file dialogs and that contain items.
    /// </summary>
    public abstract class FileDialogItemsControl : FileDialogControl
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogItemsControl"/> class.
        /// </summary>
        protected FileDialogItemsControl()
        {
            Items = new FileDialogControlItemCollection(this);
        }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogItemsControl"/> class using the specified items.
        /// </summary>
        /// <param name="items">The items to add to the control.</param>
        protected FileDialogItemsControl(string[] items) : this()
        {
            if (items != null)
            {
                foreach (string item in items)
                {
                    Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Gets the collection of items.
        /// </summary>
        public FileDialogControlItemCollection Items { get; }

        /// <summary>
        /// Creates a new instance of the items control and its items not associated with any dialog.
        /// </summary>
        public override sealed object Clone()
        {
            FileDialogItemsControl clone = CloneContainer();
            foreach (FileDialogControlItem item in Items)
            {
                clone.Items.Add((FileDialogControlItem)item.Clone());
            }
            return clone;
        }

        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            LockAndAttachContainer(owner);
            foreach (FileDialogControlItem item in Items)
            {
                item.AddToDialog(owner);
            }
        }

        private protected abstract FileDialogItemsControl CloneContainer();
        private protected abstract void LockAndAttachContainer(IFileDialogCustomize owner);
    }
}

