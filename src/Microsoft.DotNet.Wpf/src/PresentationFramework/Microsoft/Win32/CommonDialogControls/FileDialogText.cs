// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.CommonDialogControls
{
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents the text content control for file dialogs.
    /// </summary>
    public class FileDialogText : FileDialogControl
    {
        // Verified: Label can be changed when dialog is shown

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogText"/> control.
        /// </summary>
        public FileDialogText() : this(string.Empty) { }

        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogText"/> control using the specified text content.
        /// </summary>
        /// <param name="label">The text content of the control. Supports accelerators.</param>
        public FileDialogText(string label)
        {
            Label = label;
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new FileDialogText(Label);
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
                    Owner?.SetControlLabel(ID, ConvertAccelerators(_attachedLabel));
                    _attachedLabel = value;
                }
                else
                {
                    _label = value;
                }
            }
        }

        /// <inheritdoc/>
        internal override void CacheState()
        {
            base.CacheState();
            _label = _attachedLabel;
        }
        private protected void AttachLabel()
        {
            _attachedLabel = _label;
        }
        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            AttachLabel();
            owner.AddText(ID, ConvertAccelerators(_attachedLabel));
        }

        // Native API does not allow us to get the current label from the dialog,
        // but it allows us to change it, so we need to maintain a separate copy.
        private string _label;
        private string _attachedLabel;
    }
}

