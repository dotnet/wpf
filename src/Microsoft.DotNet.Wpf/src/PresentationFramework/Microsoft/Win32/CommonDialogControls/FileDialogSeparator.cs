// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.CommonDialogControls
{
    using MS.Internal.AppModel;

    // The separator is a horizontal line diving controls (e.g. inside a visual group).
    /// <summary>
    /// Represents a separator control for file dialogs, allowing a visual separation of controls.
    /// </summary>
    /// <remarks>
    /// As visual groups are already separated, inserting a single separator between them has no visible effect.
    /// </remarks>
    public sealed class FileDialogSeparator : FileDialogControl
    {
        /// <inheritdoc/>
        public override object Clone()
        {
            return new FileDialogSeparator();
        }

        /// <inheritdoc/>
        private protected override void LockAndAttachInternal(IFileDialogCustomize owner)
        {
            owner.AddSeparator(ID);
        }
    }
}

