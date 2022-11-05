// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents the edit box control for file dialogs.
    /// </summary>
    /// <remarks>
    /// To add a label next to the edit box, place it inside a <see cref="FileDialogVisualGroup"/>.
    /// </remarks>
    public sealed class FileDialogEditBox : FileDialogControl
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogEditBox"/> control.
        /// </summary>
        public FileDialogEditBox() { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogEditBox"/> control using the specified text content.
        /// </summary>
        /// <param name="text">The initial text content of the edit box.</param>
        public FileDialogEditBox(string text)
        {
            Text = text;
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new FileDialogEditBox(Text);
        }

        /// <summary>
        /// Gets or sets the current text of the edit box.
        /// </summary>
        public string Text
        {
            get
            {
                if (Owner is IFileDialogCustomize owner)
                {
                    return owner.GetEditBoxText(ID);
                }
                else
                {
                    return _text;
                }
            }
            set
            {
                if (Owner is IFileDialogCustomize owner)
                {
                    owner.SetEditBoxText(ID, value);
                }
                else
                {
                    _text = value;
                }
            }
        }

        /// <inheritdoc/>
        internal override void CacheState()
        {
            base.CacheState();
            _text = Owner.GetEditBoxText(ID);
        }

        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            owner.AddEditBox(ID, _text);
        }

        private string _text;
    }
}

