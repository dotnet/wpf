// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    using System;

    // Not inheritable or instantiable (internal constructor).
    /// <summary>
    /// Represents an ordered collection of common item dialog controls in a file dialog.
    /// </summary>
    public class FileDialogCustomControls : FileDialogControlCollection
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogCustomControls"/> class.
        /// </summary>
        internal FileDialogCustomControls()
        {

        }

        /// <summary>
        /// Gets or sets the control that will be positioned prominently next to the Ok button.
        /// </summary>
        /// <remarks>
        /// If there is only one control in the collection, it will be automatically treated as prominent.
        /// Only <see cref="FileDialogCheckButton"/>, <see cref="FileDialogPushButton"/>, <see cref="FileDialogMenu"/>,
        /// and <see cref="FileDialogComboBox"/> can be prominent. <see cref="FileDialogVisualGroup"/> can be prominent
        /// only if it contains one of these controls.
        /// </remarks>
        /// <exception cref="InvalidOperationException">A file dialog with this control collection is currently shown.</exception>
        public FileDialogControl Prominent
        {
            get { return _prominent; }
            set
            {
                ThrowIfLocked();
                _prominent = value;
            }
        }

        /// <summary>
        /// Adds a visual group to the collection.
        /// </summary>
        /// <param name="label">The visual group label. Supports accelerators.</param>
        /// <param name="controls">The controls to add to the visual group.</param>
        /// <returns></returns>
        public FileDialogVisualGroup AddVisualGroup(string label, params FileDialogControl[] controls) => AddAndReturn(new FileDialogVisualGroup(label, controls));
        /// <summary>
        ///  Adds a visual group with a combo box to the collection.
        /// </summary>
        /// <param name="label">The visual group label. Supports accelerators.</param>
        /// <param name="items">The items to add to the control.</param>
        /// <returns>The <see cref="FileDialogComboBox"/> inside the visual group added to the collection.</returns>
        public FileDialogComboBox AddComboBoxWithLabel(string label, params string[] items)
        {
            FileDialogComboBox combobox = new FileDialogComboBox(items);
            AddVisualGroup(label, combobox);
            return combobox;
        }
        /// <summary>
        /// Adds a visual group with an edit box to the collection.
        /// </summary>
        /// <param name="label">The visual group label. Supports accelerators.</param>
        /// <param name="text">The initial text content for the edit box.</param>
        /// <returns>The <see cref="FileDialogEditBox"/> inside the visual group added to the collection.</returns>
        public FileDialogEditBox AddEditBoxWithLabel(string label, string text)
        {
            FileDialogEditBox editbox = new FileDialogEditBox(text);
            AddVisualGroup(label, editbox);
            return editbox;
        }
        /// <summary>
        /// Adds a visual group with a radio button list to the collection.
        /// </summary>
        /// <param name="label">The visual group label. Supports accelerators.</param>
        /// <param name="items">The items to add to the option list.</param>
        /// <returns>The <see cref="FileDialogRadioButtonList"/> inside the visual group added to the collection.</returns>
        public FileDialogRadioButtonList AddRadioButtonListWithLabel(string label, params string[] items)
        {
            FileDialogRadioButtonList list = new FileDialogRadioButtonList(items);
            AddVisualGroup(label, list);
            return list;
        }

        internal override void LockAndAttach(IFileDialogCustomize owner)
        {
            base.LockAndAttach(owner);

            if (Prominent?.Owner == owner)
            {
                owner.MakeProminent(Prominent.ID);
            }
        }

        private protected override void ThrowIfInvalid(FileDialogControl control)
        {
            // allow visual groups by not throwing
        }

        private FileDialogControl _prominent;
    }
}

