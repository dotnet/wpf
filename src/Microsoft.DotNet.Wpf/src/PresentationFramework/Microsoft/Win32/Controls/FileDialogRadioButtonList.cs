// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents the option button (also known as radio button) group for file dialogs.
    /// </summary>
    public sealed class FileDialogRadioButtonList : FileDialogSelectorControl
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogRadioButtonList"/>.
        /// </summary>
        public FileDialogRadioButtonList() { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogRadioButtonList"/> using the specified items.
        /// </summary>
        /// <param name="items">The items to add to the option list.</param>
        public FileDialogRadioButtonList(params string[] items) : base(items) { }

        /// <inheritdoc/>
        internal override void DetachAndUnlock()
        {
            base.DetachAndUnlock();
            Items.Unlock();
        }
        /// <inheritdoc/>
        private protected override void LockAndAttachContainer(IFileDialogCustomize owner)
        {
            Items.Lock();
            owner.AddRadioButtonList(ID);
        }
        /// <inheritdoc/>
        private protected override FileDialogItemsControl CloneContainer()
        {
            return new FileDialogRadioButtonList();
        }
    }
}

