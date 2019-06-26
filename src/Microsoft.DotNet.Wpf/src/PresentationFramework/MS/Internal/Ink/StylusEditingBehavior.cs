// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Security;

namespace MS.Internal.Ink
{
    /// <summary>
    /// IStylusEditing Interface
    /// </summary>
    internal interface IStylusEditing
    {
        /// <summary>
        /// AddStylusPoints
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">only true if eventArgs.UserInitiated is true</param>
        void AddStylusPoints(StylusPointCollection stylusPoints, bool userInitiated);
    }

    /// <summary>
    /// StylusEditingBehavior - a base class for all stylus related editing behaviors
    /// </summary>
    internal abstract class StylusEditingBehavior : EditingBehavior, IStylusEditing
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="editingCoordinator"></param>
        /// <param name="inkCanvas"></param>
        internal StylusEditingBehavior(EditingCoordinator editingCoordinator, InkCanvas inkCanvas)
            : base(editingCoordinator, inkCanvas)
        {
        }
        
        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// An internal method which performs a mode change in mid-stroke.
        /// </summary>
        /// <param name="mode"></param>
        internal void SwitchToMode(InkCanvasEditingMode mode)
        {
            // 
            // The dispather frames can be entered. If one calls InkCanvas.Select/Paste from a dispather frame
            // during the user editing, this method will be called. But before the method is processed completely,
            // the user input could kick in AddStylusPoints. So EditingCoordinator.UserIsEditing flag may be messed up.
            // Now we use _disableInput to disable the input during changing the mode in mid-stroke. 
            _disableInput = true;
            try
            {
                OnSwitchToMode(mode);
            }
            finally
            {
                _disableInput = false;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // IStylusEditing Interface
        //
        //-------------------------------------------------------------------------------

        #region IStylusEditing Interface

        /// <summary>
        /// IStylusEditing.AddStylusPoints
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the eventArgs source had UserInitiated set to true</param>
        void IStylusEditing.AddStylusPoints(StylusPointCollection stylusPoints, bool userInitiated)
        {
            EditingCoordinator.DebugCheckActiveBehavior(this);

            // Don't process if SwitchToMode is called during the mid-stroke.
            if ( _disableInput )
            {
                return;
            }

            if ( !EditingCoordinator.UserIsEditing )
            {
                EditingCoordinator.UserIsEditing = true;
                StylusInputBegin(stylusPoints, userInitiated);
            }
            else
            {
                StylusInputContinue(stylusPoints, userInitiated);
            }
        }

        #endregion IStylusEditing Interface

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// An abstract method which performs a mode change in mid-stroke.
        /// </summary>
        /// <param name="mode"></param>
        protected abstract void OnSwitchToMode(InkCanvasEditingMode mode);
        
        /// <summary>
        /// Called when the InkEditingBehavior is activated.
        /// </summary>
        protected override void OnActivate()
        {
        }

        /// <summary>
        /// Called when the InkEditingBehavior is deactivated.
        /// </summary>
        protected override void OnDeactivate()
        {
        }

        /// <summary>
        /// OnCommit
        /// </summary>
        /// <param name="commit"></param>
        protected sealed override void OnCommit(bool commit)
        {
            // Make sure that user is still editing
            if ( EditingCoordinator.UserIsEditing )
            {
                EditingCoordinator.UserIsEditing = false;

                // The follow code raises variety editing events.
                // The out-side code could throw exception in the their handlers. We use try/finally block to protect our status.
                StylusInputEnd(commit);
            }
            else
            {
                // If user isn't editing, we should still call the derive class.
                // So the dynamic behavior like LSB can be self deactivated when it has been commited.
                OnCommitWithoutStylusInput(commit);
            }
        }

        /// <summary>
        /// StylusInputBegin
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag is true</param>
        protected virtual void StylusInputBegin(StylusPointCollection stylusPoints, bool userInitiated)
        {
            //defer to derived classes
        }

        /// <summary>
        /// StylusInputContinue
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag is true</param>
        protected virtual void StylusInputContinue(StylusPointCollection stylusPoints, bool userInitiated)
        {
            //defer to derived classes
        }

        /// <summary>
        /// StylusInputEnd
        /// </summary>
        /// <param name="commit"></param>
        protected virtual void StylusInputEnd(bool commit)
        {
            //defer to derived classes
        }

        /// <summary>
        /// OnCommitWithoutStylusInput
        /// </summary>
        /// <param name="commit"></param>
        protected virtual void OnCommitWithoutStylusInput(bool commit)
        {
            //defer to derived classes
        }
       
        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private bool    _disableInput;  // No need for initializing. The default value is false.

        #endregion Private Fields
    }
}
