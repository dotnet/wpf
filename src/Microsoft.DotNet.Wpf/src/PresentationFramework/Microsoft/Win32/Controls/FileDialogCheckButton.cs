// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    using System;

    /// <summary>
    /// Represents the check button (check box) control for file dialogs.
    /// </summary>
    public sealed class FileDialogCheckButton : FileDialogText
    {
        // Verified: State can be changed when dialog is shown

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogCheckButton"/> control.
        /// </summary>
        public FileDialogCheckButton() { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogCheckButton"/> control using the specified label.
        /// </summary>
        /// <param name="label">The check button text. Accelerators are supported.</param>
        public FileDialogCheckButton(string label) : base(label) { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogCheckButton"/> control using the specified label and checked state.
        /// </summary>
        /// <param name="label">The check button text. Accelerators are supported.</param>
        /// <param name="isChecked">The initial state of the check button.</param>
        public FileDialogCheckButton(string label, bool isChecked) : base(label)
        {
            IsChecked = isChecked;
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new FileDialogCheckButton(Label, IsChecked);
        }

        /// <summary>
        /// Gets or sets the state of the check button (check box). Supports accelerators.
        /// </summary>
        public bool IsChecked
        {
            get
            {
                if (Owner is IFileDialogCustomize owner)
                {
                    return owner.GetCheckButtonState(ID);
                }
                else
                {
                    return _isChecked;
                }
            }
            set
            {
                if (Owner is IFileDialogCustomize owner)
                {
                    Owner.SetCheckButtonState(ID, value);
                }
                else
                {
                    _isChecked = value;
                }
            }
        }

        /// <summary>
        /// Occurs when a <see cref="FileDialogCheckButton"/> is checked.
        /// </summary>
        public event EventHandler Checked;
        /// <summary>
        /// Occurs when a <see cref="FileDialogCheckButton"/> is unchecked.
        /// </summary>
        public event EventHandler Unchecked;

        /// <summary>
        /// Invokes the <see cref="Checked"/> event handler.
        /// </summary>
        internal void RaiseChecked()
        {
            Checked?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Invokes the <see cref="Unchecked"/> event handler.
        /// </summary>
        internal void RaiseUnchecked()
        {
            Unchecked?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        internal override void CacheState()
        {
            base.CacheState();
            _isChecked = Owner.GetCheckButtonState(ID);
        }

        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            AttachLabel();
            owner.AddCheckButton(ID, ConvertAccelerators(Label), _isChecked);
        }

        private bool _isChecked;
    }
}

