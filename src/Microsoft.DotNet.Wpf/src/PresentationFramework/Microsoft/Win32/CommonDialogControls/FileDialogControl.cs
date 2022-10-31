// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.Win32.CommonDialogControls
{
    using System;
    using MS.Internal.AppModel;
    using MS.Internal.Interop;

    internal interface IFileDialogCustomizeOwner
    {
        int ID { get; }
        IFileDialogCustomize Owner { get; }
    }

    // Not inheritable due to internal abstract members (LockAndAttach).
    /// <summary>
    /// <see cref="FileDialogControl"/> is a base class for controls that can be used in file dialogs.
    /// </summary>
    public abstract class FileDialogControl : FileDialogControlBase, IFileDialogCustomizeOwner, ICloneable
    {
        /// <summary>
        /// Gets the file dialog the control is attached to.
        /// </summary>
        internal IFileDialogCustomize Owner { get; private set; }
        /// <summary>
        /// Gets whether the control is attached to any file dialog.
        /// </summary>
        internal bool HasOwner => Owner != null;

        // In native API, controls stay associated with a dialog after it closes.
        // This allows users to read the state of the controls (such as picked options) after user closed the dialog.
        //
        // Since 1) there is a bug that EditBox always returns empty string after detaching;
        //       2) WPF dialog API has the notion of reverting properties if user cancels the dialog;
        //       3) being associated with a dialog complicates lifetime and reusing controls in multiple dialogs;
        //       4) the native API caches the values on detach,
        //
        // controls should implement the following strategy:
        //       1) before and after detaching, use local fields for the state values;
        //       2) the dialog will call LockAndAttach() when dialog is about to be opened, apply the values to the dialog as appropriate;
        //       3) during attachment, read and write directly to the dialog, but do not update local values;
        //       4) the dialog will call CacheState() when the dialog is about to be closed due to OK, read the state from the dialog and cache it to local fields;
        //       5) the dialog will call DetachAndUnlock() after it closed, the dialog should no longer be considered a valid reference at that point.

        /// <summary>
        /// Adds the control to a dialog and applies any pending state.
        /// Some modifications of the control may no longer be allowed.
        /// </summary>
        /// <param name="owner">The dialog to add this control to.</param>
        /// <exception cref="InvalidOperationException">Control is already attached to a dialog.</exception>
        internal void LockAndAttach(IFileDialogCustomize owner)
        {
            if (HasOwner)
            {
                throw new InvalidOperationException("Control is already attached to a dialog.");
            }

            LockAndAttachInternal(owner);
            Owner = owner;

            if (_state != CDCS.ENABLEDVISIBLE)
            {
                owner.SetControlState(ID, _state);
            }
        }
        /// <summary>
        /// Update the local state with the current state from the dialog.
        /// </summary>
        /// <remarks>
        /// This method will be invoked when the dialog is about to be succesfully closed.
        /// Inheritors must call the base implementation when overriding this method.
        /// </remarks>
        internal virtual void CacheState()
        {
            _state = GetState();
        }
        /// <summary>
        /// Release the reference to the dialog and allow design-time modifications again.
        /// </summary>
        /// <remarks>
        /// This method will be invoked when the dialog has already closed, the held reference should already be considered invalid.
        /// Inheritors must call the base implementation when overriding this method.
        /// </remarks>
        internal virtual void DetachAndUnlock()
        {
            Owner = null;
        }

        /// <summary>
        /// Add the control to the owning dialog and apply any pending state.
        /// Some modifications of the control may no longer be allowed.
        /// </summary>
        /// <remarks>
        /// While the owner is set by the time this method will be invoked, be careful not to ask it about your control until you add it.
        /// </remarks>
        private protected abstract void LockAndAttachInternal(IFileDialogCustomize owner);

        private protected sealed override CDCS GetState()
        {
            if (Owner is IFileDialogCustomize owner)
            {
                return owner.GetControlState(ID);
            }
            else
            {
                return _state;
            }
        }
        private protected sealed override void SetState(CDCS state)
        {
            if (Owner is IFileDialogCustomize owner)
            {
                owner.SetControlState(ID, _state);
            }
            else
            {
                _state = state;
            }
        }

        IFileDialogCustomize IFileDialogCustomizeOwner.Owner => Owner;
        int IFileDialogCustomizeOwner.ID => ID;

        private CDCS _state = CDCS.ENABLEDVISIBLE;
    }
}

