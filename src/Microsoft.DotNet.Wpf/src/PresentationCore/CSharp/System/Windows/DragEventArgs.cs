// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// 
// Description: DragEventArgs for drag-and-drop operation.
//
// 
//

using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    /// The DragEventArgs class represents a type of RoutedEventArgs that
    /// are relevant to all drag events(DragEnter/DragOver/DragLeave/DragDrop).
    /// </summary>
    public sealed class DragEventArgs : RoutedEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
    
        #region Constructors

        /// <summary>
        /// Constructs a DragEventArgs instance.
        /// </summary>
        /// <param name="data">
        /// The data object used in this drag drop operation.
        /// </param>    
        /// <param name="dragDropKeyStates">
        /// The current state of the mouse button and modifier keys.
        /// </param>    
        /// <param name="allowedEffects">
        /// Allowed effects of a drag-and-drop operation.
        /// </param>    
        /// <param name="target">
        /// The target of the event.
        /// </param>    
        /// <param name="point">
        /// The current mouse position of the target.
        /// </param>    
        internal DragEventArgs(IDataObject data, DragDropKeyStates dragDropKeyStates, DragDropEffects allowedEffects, DependencyObject target, Point point)
        {
            if (!DragDrop.IsValidDragDropKeyStates(dragDropKeyStates))
            {
                Debug.Assert(false, "Invalid dragDropKeyStates");
            }

            if (!DragDrop.IsValidDragDropEffects(allowedEffects))
            {
                Debug.Assert(false, "Invalid allowedEffects");
            }

            if (target == null)
            {
                Debug.Assert(false, "Invalid target");
            }

            this._data = data;
            this._dragDropKeyStates = dragDropKeyStates;
            this._allowedEffects = allowedEffects;
            this._target = target;
            this._dropPoint = point;
            this._effects = allowedEffects;
        }

        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// The point of drop operation that based on relativeTo.
        /// </summary>
        public Point GetPosition(IInputElement relativeTo)
        {
            Point dropPoint;

            if (relativeTo == null)
            {
                throw new ArgumentNullException("relativeTo");
            }

            dropPoint = new Point(0, 0);

            if (_target != null)
            {
                // Translate the drop point from the drop target to the relative element.
                dropPoint = InputElement.TranslatePoint(_dropPoint, _target, (DependencyObject)relativeTo);
            }

            return dropPoint;
        }
                
        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The data object of drop operation
        /// </summary>
        public IDataObject Data
        {
            get { return _data; }
        }

        /// <summary>
        /// The DragDropKeyStates that indicates the current states for 
        /// physical keyboard keys and mouse buttons.
        /// </summary>
        public DragDropKeyStates KeyStates
        {
            get { return _dragDropKeyStates; }
        }

        /// <summary>
        /// The allowed effects of drag and drop operation
        /// </summary>
        public DragDropEffects AllowedEffects
        {
            get
            {
                return _allowedEffects;
            }
        }

        /// <summary>
        /// The effects of drag and drop operation
        /// </summary>
        public DragDropEffects Effects
        {
            get
            {
                return _effects;
            }

            set
            {
                if (!DragDrop.IsValidDragDropEffects(value))
                {
                    throw new ArgumentException(SR.Get(SRID.DragDrop_DragDropEffectsInvalid, "value"));
                }

                _effects = value;
            }
        }

        #endregion Public Properties

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
            DragEventHandler handler = (DragEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IDataObject _data;
        private DragDropKeyStates _dragDropKeyStates;
        private DragDropEffects _allowedEffects;
        private DragDropEffects _effects;
        private DependencyObject _target;
        private Point _dropPoint;

        #endregion Private Fields
    }
}

