// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents the combo box for file dialogs.
    /// </summary>
    public sealed class FileDialogComboBox : FileDialogSelectorControl
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogComboBox"/>.
        /// </summary>
        public FileDialogComboBox() { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogComboBox"/> using the specified items.
        /// </summary>
        /// <param name="items">The items to add to the control.</param>
        public FileDialogComboBox(params string[] items) : base(items) { }

        /// <inheritdoc/>
        private protected override void LockAndAttachContainer(IFileDialogCustomize owner)
        {
            owner.AddComboBox(ID);
        }
        /// <inheritdoc/>
        private protected override FileDialogItemsControl CloneContainer()
        {
            return new FileDialogComboBox();
        }
    }
}

