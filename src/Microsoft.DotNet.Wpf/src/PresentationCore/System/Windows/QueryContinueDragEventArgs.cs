// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// 
// Description: QueryContinueDragEventArgs for drag-and-drop operation.
// 
//

using System;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    /// The QueryContinueDragEventArgs class represents a type of RoutedEventArgs that
    /// are relevant to QueryContinueDrag event.
    /// </summary>
    public sealed class QueryContinueDragEventArgs : RoutedEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
    
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the QueryContinueDragEventArgs class.
        /// </summary>
        /// <param name="escapePressed">
        /// Escape key was pressed.
        /// </param>
        /// <param name="dragDropKeyStates">
        /// Input states.
        /// </param>
        internal QueryContinueDragEventArgs(bool escapePressed, DragDropKeyStates dragDropKeyStates)
        {
            if (!DragDrop.IsValidDragDropKeyStates(dragDropKeyStates))
            {
                Debug.Assert(false, "Invalid dragDropKeyStates");
            }

            this._escapePressed = escapePressed;
            this._dragDropKeyStates = dragDropKeyStates;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Escape key was pressed.
        /// </summary>
        public bool EscapePressed
        {
            get { return _escapePressed; }
        }

        /// <summary>
        /// The DragDropKeyStates that indicates the current states for 
        /// physical keyboard keys and mouse buttons.
        /// </summary>
        public DragDropKeyStates KeyStates
        {
            get {return _dragDropKeyStates;}
        }

        /// <summary>
        /// The action of drag operation
        /// </summary>
        public DragAction Action
        {
            get 
            {   
                return _action;
            }

            set 
            {
                if (!DragDrop.IsValidDragAction(value))
                {
                    throw new ArgumentException(SR.Get(SRID.DragDrop_DragActionInvalid, "value"));
                }

                _action = value; 
            }
        }

        #endregion Public Methods

        #region Protected Methods

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// The mechanism used to call the type-specific handler on the target.
        /// </summary>
        /// <param name="genericHandler">
        /// The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        /// The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            QueryContinueDragEventHandler handler = (QueryContinueDragEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _escapePressed;
        private DragDropKeyStates _dragDropKeyStates;
        private DragAction _action;

        #endregion Private Fields
    }
}

