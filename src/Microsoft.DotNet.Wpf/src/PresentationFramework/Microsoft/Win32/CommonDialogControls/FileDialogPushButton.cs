// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.CommonDialogControls
{
    using MS.Internal.AppModel;

    using System;

    /// <summary>
    /// Represents the button control for file dialogs.
    /// </summary>
    public sealed class FileDialogPushButton : FileDialogText
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogPushButton"/> control.
        /// </summary>
        public FileDialogPushButton() { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogPushButton"/> control using the specified label.
        /// </summary>
        /// <param name="label">The push button label. Accelerators are supported.</param>
        public FileDialogPushButton(string label) : base(label) { }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new FileDialogPushButton(Label);
        }

        /// <summary>
        /// Occurs when a <see cref="FileDialogPushButton"/> is clicked.
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Invokes the <see cref="Click"/> even thandler.
        /// </summary>
        internal void RaiseClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            AttachLabel();
            owner.AddPushButton(ID, ConvertAccelerators(Label));
        }
    }
}

