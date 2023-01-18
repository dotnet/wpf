// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    // File dialog buttons are special because they have very limited functionality and cannot be part of collections
    /// <summary>
    /// Represents the Cancel button on file dialogs.
    /// </summary>
    public sealed class FileDialogCancelButton
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogCancelButton"/>.
        /// </summary>
        internal FileDialogCancelButton() { }

        /// <summary>
        /// Gets or sets a custom OK button label. Supports accelerators. 
        /// </summary>
        /// <remarks>
        /// Populating the <see cref="Items"/> collection takes 
        /// </remarks>
        public string CustomLabel
        {
            get { return _dialog != null ? _attachedLabel : _label; }
            set
            {
                if (_dialog != null)
                {
                    _dialog.SetCancelButtonLabel(FileDialogControlBase.ConvertAccelerators(value));
                    _attachedLabel = value;
                }
                else
                {
                    _label = value;
                }
            }
        }

        /// <summary>
        /// Update the local state with the current state from the dialog.
        /// </summary>
        internal void CacheState()
        {
            _label = _attachedLabel;
        }
        /// <summary>
        /// Add the control to the owning dialog and apply any pending state.
        /// Some modifications of the control may no longer be allowed.
        /// </summary>
        internal void LockAndAttach(IFileDialogCustomize owner)
        {
            _attachedLabel = _label;
            _dialog = owner as IFileDialog2;

            if (!string.IsNullOrEmpty(_label))
            {
                _dialog?.SetCancelButtonLabel(FileDialogControlBase.ConvertAccelerators(_label));
            }
        }
        /// <summary>
        /// Release the reference to the dialog and allow design-time modifications again.
        /// </summary>
        internal void DetachAndUnlock()
        {
            _dialog = null;
        }

        IFileDialog2 _dialog;

        // Native API does not allow us to get the current label from the dialog,
        // but it allows us to change it, so we need to maintain a separate copy.
        private string _attachedLabel;
        private string _label;

    }
}

