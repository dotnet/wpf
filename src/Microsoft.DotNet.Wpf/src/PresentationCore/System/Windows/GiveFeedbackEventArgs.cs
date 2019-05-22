// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// 
// Description: GiveFeedbackEventArgs for drag-and-drop operation.//
// 
//

using System;
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    /// The GiveFeedbackEventArgs class represents a type of RoutedEventArgs that
    /// are relevant to GiveFeedback.
    /// </summary>
    public sealed class GiveFeedbackEventArgs : RoutedEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
    
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the GiveFeedbackEventArgs class.
        /// </summary>
        /// <param name="effects">
        /// The effect of the drag operation.
        /// </param>    
        /// <param name="useDefaultCursors">
        /// Use the default cursors.
        /// </param>    
        internal GiveFeedbackEventArgs(DragDropEffects effects, bool useDefaultCursors)
        {
            if (!DragDrop.IsValidDragDropEffects(effects))
            {
                Debug.Assert(false, "Invalid effects");
            }

            this._effects = effects;
            this._useDefaultCursors = useDefaultCursors;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// The effects of drag operation
        /// </summary>
        public DragDropEffects Effects
        {
            get 
            {   
                return _effects;
            }
        }

        /// <summary>
        /// Use the default cursors.
        /// </summary>
        public bool UseDefaultCursors
        {
            get 
            {   
                return _useDefaultCursors;
            }

            set 
            {
                _useDefaultCursors = value;
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
            GiveFeedbackEventHandler handler = (GiveFeedbackEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private DragDropEffects _effects;
        private bool _useDefaultCursors;

        #endregion Private Fields
    }
}

