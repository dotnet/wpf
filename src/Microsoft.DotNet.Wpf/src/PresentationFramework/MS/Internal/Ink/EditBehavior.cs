// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description:
// Base EditingBehavior for InkCanvas
// Features:
//


using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Media;

namespace MS.Internal.Ink
{
    /// <summary>
    /// Base class for all EditingBehaviors in the InkCanvas
    /// Please see the design detain at http://tabletpc/longhorn/Specs/Mid-Stroke%20and%20Pen%20Cursor%20Dev%20Design.mht
    /// </summary>
    internal abstract class EditingBehavior
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
        /// <param name="editingCoordinator">EditngCoordinator instance</param>
        /// <param name="inkCanvas">InkCanvas instance</param>
        internal EditingBehavior(EditingCoordinator editingCoordinator, InkCanvas inkCanvas)
        {
            if (inkCanvas == null)
            {
                throw new ArgumentNullException("inkCanvas");
            }
            if (editingCoordinator == null)
            {
                throw new ArgumentNullException("editingCoordinator");
            }
            _inkCanvas = inkCanvas;
            _editingCoordinator = editingCoordinator;
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Public Methods
        //
        //-------------------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// An internal method which should be called from:
        ///     EditingCoordinator.PushEditingBehavior
        ///     EditingCoordinator.PopEditingBehavior
        /// The mehod's called when the behavior switching occurs.
        /// </summary>
        public void Activate()
        {
            // Debug verification which will never be compiled into release bits.
            EditingCoordinator.DebugCheckActiveBehavior(this);

            // Invoke the virtual OnActivate method.
            OnActivate();
        }

        /// <summary>
        /// An internal method which should be called from:
        ///     EditingCoordinator.PushEditingBehavior
        ///     EditingCoordinator.PopEditingBehavior
        /// The mehod's called when the behavior switching occurs.
        /// </summary>
        public void Deactivate()
        {
            // Debug verification which will never be compiled into release bits.
            EditingCoordinator.DebugCheckActiveBehavior(this);

            // Invoke the virtual OnDeactivate method.
            OnDeactivate();
        }

        /// <summary>
        /// An internal method which should be called from:
        ///     EditingCoordinator.OnInkCanvasStylusUp
        ///     EditingCoordinator.OnInkCanvasLostStylusCapture
        ///     EditingCoordinator.UpdateEditingState
        /// The mehod's called when the current editing state is committed or discarded.
        /// </summary>
        /// <param name="commit">A flag which indicates either editing is committed or discarded</param>
        public void Commit(bool commit)
        {
            // Debug verification which will never be compiled into release bits.
            EditingCoordinator.DebugCheckActiveBehavior(this);

            // Invoke the virtual OnCommit method.
            OnCommit(commit);
        }

        /// <summary>
        /// UpdateTransform
        ///     Called by: EditingCoordinator.InvalidateBehaviorCursor
        /// </summary>
        public void UpdateTransform()
        {
            if ( !EditingCoordinator.IsTransformValid(this) )
            {
                OnTransformChanged();
            }
        }

        #endregion Public Methods

        //-------------------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Retrieve the cursor which is associated to this behavior.
        /// The method which should be called from:
        ///     EditingCoordinator.InvalidateCursor
        ///     EditingCoordinator.PushEditingBehavior
        ///     EditingCoordinator.PopEditingBehavior
        /// </summary>
        /// <returns>The current cursor</returns>
        public Cursor Cursor
        {
            get
            {
                // If the cursor instance hasn't been created or is dirty, we will create a new instance.
                if ( _cachedCursor == null || !EditingCoordinator.IsCursorValid(this) )
                {
                    // Invoke the virtual method to get cursor. Then cache it.
                    _cachedCursor = GetCurrentCursor();
                }

                return _cachedCursor;
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------
        
        #region Protected Methods

        /// <summary>
        /// Called when the EditingBehavior is activated.  EditingBehaviors
        /// should register for event handlers in this call
        /// Called from:
        ///     EditingBehavior.Activate
        /// </summary>
        protected abstract void OnActivate();

        /// <summary>
        /// Called when the EditingBehavior is deactivated.  EditingBehaviors
        /// should unregister from event handlers in this call
        /// Called from:
        ///     EditingBehavior.Deactivate
        /// </summary>
        protected abstract void OnDeactivate();

        /// <summary>
        /// Called when the user editing is committed or discarded.
        /// Called from:
        ///     EditingBehavior.Commit
        /// </summary>
        /// <param name="commit">A flag which indicates either editing is committed or discarded</param>
        protected abstract void OnCommit(bool commit);

        /// <summary>
        /// Called when the new cursor instance is required.
        /// Called from:
        ///     EditingBehavior.GetCursor
        /// </summary>
        /// <returns></returns>
        protected abstract Cursor GetCurrentCursor();

        /// <summary>
        /// Unload the dynamic behavior. The method should be only called from:
        ///     SelectionEditingBehavior
        ///     LassoSelectionBehavior
        /// </summary>
        protected void SelfDeactivate()
        {
            EditingCoordinator.DeactivateDynamicBehavior();
        }

        /// <summary>
        /// Calculate the transform which is accumalated with the InkCanvas' XXXTransform properties.
        /// </summary>
        /// <returns></returns>
        protected Matrix GetElementTransformMatrix()
        {
            Transform layoutTransform = this.InkCanvas.LayoutTransform;
            Transform renderTransform = this.InkCanvas.RenderTransform;
            Matrix xf = layoutTransform.Value;
            xf *= renderTransform.Value;

            return xf;
        }

        /// <summary>
        /// OnTransformChanged
        /// </summary>
        protected virtual void OnTransformChanged()
        {
            // Defer to derived.
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Protected Properties
        //
        //-------------------------------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// Provides access to the InkCanvas this EditingBehavior is attached to
        /// </summary>
        /// <value></value>
        protected InkCanvas InkCanvas
        {
            get { return _inkCanvas; }
        }
        
        /// <summary>
        /// Provides access to the EditingStack this EditingBehavior is on
        /// </summary>
        /// <value></value>
        protected EditingCoordinator EditingCoordinator
        {
            get { return _editingCoordinator; }
        }

        #endregion Protected Properties


        //-------------------------------------------------------------------------------
        //
        // Protected Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The InkCanvas this EditingBehavior is operating on
        /// </summary>
        private InkCanvas           _inkCanvas;
        
        /// <summary>
        /// The EditingStack this EditingBehavior is on
        /// </summary>
        private EditingCoordinator  _editingCoordinator;

        /// <summary>
        /// Fields related to the cursor
        /// </summary>
        private Cursor              _cachedCursor;

        #endregion Private Fields
    }
}
