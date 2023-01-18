// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.Controls
{
    using MS.Internal.AppModel;

    /// <summary>
    /// Represents a visual group in file dialogs.
    /// </summary>
    public sealed class FileDialogVisualGroup : FileDialogText
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogVisualGroup"/> using the specified label.
        /// </summary>
        /// <param name="label">The visual group label. Supports accelerators</param>
        public FileDialogVisualGroup(string label) : base(label)
        {
            Controls = new FileDialogControlCollection();
        }
        /// <summary>
        /// Initializes a new instance of <see cref="FileDialogVisualGroup"/> using the specified lable and controls.
        /// </summary>
        /// <param name="label">The visual group label. Supports accelerators.</param>
        /// <param name="controls">The controls to add to the visual group.</param>
        public FileDialogVisualGroup(string label, params FileDialogControl[] controls) : this(label)
        {
            if (controls != null)
            {
                foreach (FileDialogControl control in controls)
                {
                    Controls.Add(control);
                }
            }
        }

        /// <summary>
        /// Gets the collection of controls in the visual group.
        /// </summary>
        public FileDialogControlCollection Controls { get; }

        /// <summary>
        /// Add control to the visual group.
        /// </summary>
        /// <param name="control">The control to add to the visual group.</param>
        public void Add(FileDialogControl control)
        {
            Controls.Add(control);
        }

        /// <summary>
        /// Creates a new instance of the visual group and its controls not associated with any dialog.
        /// </summary>
        public override object Clone()
        {
            FileDialogVisualGroup clone = new FileDialogVisualGroup(Label);
            foreach (FileDialogControl control in clone.Controls)
            {
                clone.Controls.Add((FileDialogControl)control.Clone());
            }
            return clone;
        }

        /// <inheritdoc/>
        internal override void CacheState()
        {
            base.CacheState();
            Controls.CacheState();
        }
        /// <inheritdoc/>
        internal override void DetachAndUnlock()
        {
            base.DetachAndUnlock();
            Controls.DetachAndUnlock();
        }
        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            AttachLabel();
            owner.StartVisualGroup(ID, ConvertAccelerators(Label));
            Controls.LockAndAttach(owner);
            owner.EndVisualGroup();
        }
    }
}

