// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.CommonDialogControls
{
    using MS.Internal.AppModel;

    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    /// <summary>
    /// Holds the list of items that constitute the content of a <see cref="FileDialogItemsControl"/>.
    /// </summary>
    public class FileDialogControlItemCollection : Collection<FileDialogControlItem>
    {
        // Unlike controls, items can be removed and added while the dialog is shown (except for RadioButtonList).
        // The collection ensures items are added and removed as long as the container is attached to a dialog,
        // but it is the responsibility of the container to attach and detach the items.

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogControlItemCollection"/> for the specified container.
        /// </summary>
        /// <param name="container">The control hosting this collection.</param>
        internal FileDialogControlItemCollection(IFileDialogCustomizeOwner container)
        {
            Debug.Assert(container != null, "FileDialogControlItemCollection requires a container.");

            _container = container;
        }

        /// <summary>
        /// Creates a new <see cref="FileDialogControlItem"/> and adds it to the collection.
        /// </summary>
        /// <param name="itemText">The text of the item.</param>
        /// <returns>the created item.</returns>
        public FileDialogControlItem Add(string itemText)
        {
            FileDialogControlItem item = new FileDialogControlItem(itemText);
            Add(item);
            return item;
        }

        /// <summary>
        /// Gets the index of an item by its ID.
        /// </summary>
        /// <param name="itemID">The item ID.</param>
        /// <returns>the zero-based index of the item with specified ID if found; otherwise, -1.</returns>
        internal int IndexOf(int itemID)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Items[i].ID == itemID)
                {
                    return i;
                }
            }

            return -1;
        }

        internal bool IsLocked => _isLocked;
        internal void Lock() => _isLocked = true;
        internal void Unlock() => _isLocked = false;

        protected override void InsertItem(int index, FileDialogControlItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ThrowIfLocked();
            OnItemAdded(index, item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            ThrowIfLocked();
            OnItemRemoved(Items[index]);
            base.RemoveItem(index);

            if (Count == 0 && _container is FileDialogOkButton button)
            {
                button.OnItemsCleared();
            }
        }

        protected override void SetItem(int index, FileDialogControlItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ThrowIfLocked();

            // we can handle the last one by removing and adding
            if (_container.Owner != null && index != Count - 1)
            {
                throw new InvalidOperationException("Cannot insert item while dialog is shown.");
            }

            OnItemRemoved(Items[index]);
            OnItemAdded(index, item);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            ThrowIfLocked();

            if (_container.Owner is IFileDialogCustomize owner)
            {
                // OK button does not support RemoveAllControlItems
                if (_container is FileDialogOkButton button)
                {
                    foreach (FileDialogControlItem item in button.Items)
                    {
                        owner.RemoveControlItem(_container.ID, item.ID);
                    }

                    button.OnItemsCleared();
                }
                else
                {
                    owner.RemoveAllControlItems(_container.ID);
                }
            }

            foreach (FileDialogControlItem item in Items)
            {
                item.UnassignContainer(removeFromDialog: false);
            }

            base.ClearItems();
        }

        private void OnItemRemoved(FileDialogControlItem item)
        {
            item.UnassignContainer(removeFromDialog: true);
        }
        private void OnItemAdded(int index, FileDialogControlItem item)
        {
            // the native API only allows us to append items
            if (_container.Owner != null && index != Count)
            {
                throw new InvalidOperationException("Cannot insert item while dialog is shown.");
            }

            item.AssignContainer(_container);
        }

        private void ThrowIfLocked()
        {
            if (_isLocked)
            {
                throw new InvalidOperationException("Cannot modify collection while dialog is shown.");
            }
        }

        private IFileDialogCustomizeOwner _container;
        private bool _isLocked;
    }
}

