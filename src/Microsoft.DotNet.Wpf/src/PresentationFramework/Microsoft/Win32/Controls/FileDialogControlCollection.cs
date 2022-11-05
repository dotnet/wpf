// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents an ordered collection of common item dialog controls.
    /// </summary>
    public class FileDialogControlCollection : Collection<FileDialogControl>
    {
        // Verified: controls cannot be added when dialog shown
        // Verified: cannot make control prominent when dialog shown
        // Checked: supported prominent controls are: CheckButton, PushButton, Menu, ComboBox
        //                                            and visual group iff it has only zero or one such control

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDialogControlCollection"/> class.
        /// </summary>
        public FileDialogControlCollection() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FileDialogControlCollection"/> class using a set of controls. 
        /// </summary>
        /// <param name="controls">The controls to add to the collection.</param>
        public FileDialogControlCollection(params FileDialogControl[] controls)
        {
            if (controls != null)
            {
                foreach (var control in controls)
                {
                    if (control != null)
                    {
                        Add(control);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a combo box to the collection.
        /// </summary>
        /// <param name="items">The items to add to the control.</param>
        /// <returns>The <see cref="FileDialogComboBox"/> added to the collection.</returns>
        public FileDialogComboBox AddComboBox(params string[] items) => AddAndReturn(new FileDialogComboBox(items));
        /// <summary>
        /// Adds a check button to the collection.
        /// </summary>
        /// <param name="label">The check button text. Accelerators are supported.</param>
        /// <param name="isChecked">The initial state of the check button.</param>
        /// <returns>The <see cref="FileDialogComboBox"/> added to the collection.</returns>
        public FileDialogCheckButton AddCheckButton(string label, bool isChecked = false) => AddAndReturn(new FileDialogCheckButton(label, isChecked));
        /// <summary>
        /// Adds an edit box to the collection.
        /// </summary>
        /// <param name="text">The initial text content for the edit box.</param>
        /// <returns>The <see cref="FileDialogEditBox"/> added to the collection.</returns>
        public FileDialogEditBox AddEditBox(string text) => AddAndReturn(new FileDialogEditBox(text));
        /// <summary>
        /// Adds a menu to the collection.
        /// </summary>
        /// <param name="label">The menu label. Accelerators are supported.</param>
        /// <param name="items">The items to add to the menu.</param>
        /// <returns>The <see cref="FileDialogMenu"/> added to the collection.</returns>
        public FileDialogMenu AddMenu(string label, params string[] items) => AddAndReturn(new FileDialogMenu(label, items));
        /// <summary>
        /// Adds a push button to the collection.
        /// </summary>
        /// <param name="label">The push button label. Accelerators are supported.</param>
        /// <returns>The <see cref="FileDialogPushButton"/> add to the collection.</returns>
        public FileDialogPushButton AddPushButton(string label) => AddAndReturn(new FileDialogPushButton(label));
        /// <summary>
        /// Adds a radio button list to the collection.
        /// </summary>
        /// <param name="items">The items to add to the control.</param>
        /// <returns>The <see cref="FileDialogRadioButtonList"/> added to the collection.</returns>
        public FileDialogRadioButtonList AddRadioButtonList(params string[] items) => AddAndReturn(new FileDialogRadioButtonList(items));
        /// <summary>
        /// Adds a seperator to the collection.
        /// </summary>
        /// <returns>The <see cref="FileDialogSeparator"/> added to the collection.</returns>
        public FileDialogSeparator AddSeparator() => AddAndReturn(new FileDialogSeparator());
        /// <summary>
        /// Adds a text to the collection.
        /// </summary>
        /// <param name="label">The text content of the control. Supports accelerators.</param>
        /// <returns>The <see cref="FileDialogText"/> added to the collection.</returns>
        public FileDialogText AddText(string label) => AddAndReturn(new FileDialogText(label));

        /// <summary>
        /// Adds controls in the collection to the file dialog, sets the prominent control if any and locks the collection for further modifications.
        /// </summary>
        internal virtual void LockAndAttach(IFileDialogCustomize owner)
        {
            _locked = true;

            // children can be added after the control is added to the collection
            // so we have to register them only when the dialog is to be shown
            _controlsLookup.Clear();
            foreach (FileDialogControl control in this)
            {
                foreach (FileDialogControl child in SelfAndChildren(control))
                {
                    _controlsLookup[child.ID] = child;
                }

                control.LockAndAttach(owner);
            }
        }
        /// <summary>
        /// Request all controls in the collection to cache the current state.
        /// This should be called when the dialog is about to be succesfully closed.
        /// </summary>
        internal void CacheState()
        {
            foreach (var control in this)
            {
                control.CacheState();
            }
        }
        /// <summary>
        /// Allows the collection to be modififed.
        /// This should be called after the dialog is closed.
        /// </summary>
        internal void DetachAndUnlock()
        {
            _locked = false;

            foreach (var control in this)
            {
                control.DetachAndUnlock();
            }
        }

        /// <summary>
        /// Lookup a control by its ID. Note that the ID lookup is only valid when the collection is locked.
        /// </summary>
        internal bool TryGetControl(int id, out FileDialogControl control)
        {
            Debug.Assert(_locked, "Attempt to lookup a file dialog control when the dialog is not shown.");
            return _controlsLookup.TryGetValue(id, out control);
        }

        /// <summary>
        /// Inserts a control into the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the control should be inserted.</param>
        /// <param name="item">The control to insert. The value cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero. -or- <paramref name="index"/> is equal to or greater than <see cref="Count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">A file dialog with this control collection is currently shown.</exception>
        protected override void InsertItem(int index, FileDialogControl item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ThrowIfLocked();
            ThrowIfInvalid(item);
            base.InsertItem(index, item);
        }
        /// <summary>
        /// Replaces the control at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the conotrol to replace.</param>
        /// <param name="item">The new control to put at the specified index. The value cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero. -or- <paramref name="index"/> is equal to or greater than <see cref="Count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">A file dialog with this control collection is currently shown.</exception>
        protected override void SetItem(int index, FileDialogControl item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ThrowIfLocked();
            ThrowIfInvalid(item);
            base.SetItem(index, item);
        }
        /// <summary>
        /// Removes the control at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the control to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero. -or- <paramref name="index"/> is equal to or greater than <see cref="Count"/>.</exception>
        /// <exception cref="InvalidOperationException">A file dialog with this control collection is currently shown.</exception>
        protected override void RemoveItem(int index)
        {
            ThrowIfLocked();
            base.RemoveItem(index);
        }
        /// <summary>
        /// Removes all controls from the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">A file dialog with this control collection is currently shown.</exception>
        protected override void ClearItems()
        {
            ThrowIfLocked();
            base.ClearItems();
            _controlsLookup.Clear();
        }

        private protected void ThrowIfLocked()
        {
            if (_locked)
            {
                throw new InvalidOperationException("Cannot modify collection while dialog is shown.");
            }
        }
        private protected virtual void ThrowIfInvalid(FileDialogControl control)
        {
            if (control is FileDialogVisualGroup)
            {
                throw new ArgumentException("Cannot add visual groups to this collection.");
            }
        }
        private protected T AddAndReturn<T>(T control) where T : FileDialogControl
        {
            Add(control);
            return control;
        }

        // Unlike the native API model, our API model uses visual groups as containers for other controls.
        // Use this method to flatten the list of controls.
        private static IEnumerable<FileDialogControl> SelfAndChildren(FileDialogControl control)
        {
            if (control != null)
            {
                yield return control;

                // Visual groups cannot be nested, there is only one level
                if (control is FileDialogVisualGroup group)
                {
                    foreach (var child in group.Controls)
                    {
                        yield return child;
                    }
                }
            }
        }

        private Dictionary<int, FileDialogControl> _controlsLookup = new Dictionary<int, FileDialogControl>();
        private bool _locked;
    }
}

