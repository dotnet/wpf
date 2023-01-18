// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;
    using MS.Internal.Interop;

    using System.Diagnostics;

    /// <summary>
    /// Represents an item used by <see cref="FileDialogItemsControl"/>.
    /// </summary>
    public sealed class FileDialogControlItem : FileDialogControlBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogControlItem"/>.
        /// </summary>
        public FileDialogControlItem() { }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogControlItem"/> using the specified text.
        /// </summary>
        /// <param name="label">The text of the item. Supports accelerators except when used in a <see cref="FileDialogComboBox"/>.</param>
        public FileDialogControlItem(string text)
        {
            Text = text;
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new FileDialogControlItem(Text);
        }

        /// <summary>
        /// Gets or sets the item text. Supports accelerators except when used in <see cref="FileDialogComboBox"/>.
        /// </summary>
        /// <remarks>
        /// The text of items cannot be changed when the dialog is shown.
        /// Consider recreating the collection or showing and hiding items with different texts.
        /// </remarks>
        public string Text
        {
            get { return Container?.Owner != null ? _attachedText :_text; }
            set
            {
                if (Container?.Owner is IFileDialogCustomize owner)
                {
                    // text cannot be changed once a dialog is shown, this will throw
                    owner.SetControlItemText(Container.ID, ID, ConvertItemAccelerators(value));
                    _attachedText = value;
                }
                else
                {
                    _text = value;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref=" FileDialogItemsControl"/> containing the item.
        /// </summary>
        internal IFileDialogCustomizeOwner Container { get; private set; }
        /// <summary>
        /// Gets whether the item is in any container.
        /// </summary>
        internal bool HasContainer => Container != null;

        internal void AssignContainer(IFileDialogCustomizeOwner container)
        {
            Debug.Assert(!HasContainer, "FileDialogControlItem is already assigned to a container.");

            Container = container;

            if (container.Owner is IFileDialogCustomize owner)
            {
                AddToDialog(owner);
            }
        }

        internal void UnassignContainer(bool removeFromDialog)
        {
            if (removeFromDialog && Container?.Owner is IFileDialogCustomize owner)
            {
                // the item is being removed from collection while dialog is shown
                owner.RemoveControlItem(Container.ID, ID);
            }

            Container = null;
        }

        internal void AddToDialog(IFileDialogCustomize owner)
        {
            owner.AddControlItem(Container.ID, ID, ConvertItemAccelerators(_text));

            if (_state != CDCS.ENABLEDVISIBLE)
            {
                owner.SetControlItemState(Container.ID, ID, _state);
            }
        }

        private string ConvertItemAccelerators(string s)
        {
            // combo box does not support acceleratros
            if (Container is FileDialogComboBox)
            {
                return s;
            }
            else
            {
                return ConvertAccelerators(s);
            }
        }

        private protected sealed override CDCS GetState()
        {
            if (Container?.Owner is IFileDialogCustomize owner)
            {
                return owner.GetControlItemState(Container.ID, ID);
            }
            else
            {
                return _state;
            }
        }
        private protected sealed override void SetState(CDCS state)
        {
            if (Container?.Owner is IFileDialogCustomize owner)
            {
                owner.SetControlItemState(Container.ID, ID, _state);
            }
            else
            {
                _state = state;
            }
        }

        // Native API does not allow us to get the current text from the dialog,
        // but it allows us to change it, so we need to maintain a separate copy.
        private string _text;
        private string _attachedText;
        private CDCS _state = CDCS.ENABLEDVISIBLE;
    }
}