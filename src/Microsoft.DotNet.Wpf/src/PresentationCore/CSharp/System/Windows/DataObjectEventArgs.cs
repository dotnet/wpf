// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Base class for DataObject events arguments
//

using System;

namespace System.Windows
{
    /// <summary>
    /// Base class for DataObject.Copying/Pasting events.
    /// These events are raised when an editor deals with
    /// a data object before putting it to clipboard on copy
    /// and before starting drag operation;
    /// or before Pasting its content into a selection
    /// on Paste/Drop operations.
    /// 
    /// This class is abstract - it provides only common
    /// members for the events. Particular commands
    /// must use more specific event arguments -
    /// DataObjectCopyingEventArgs or DataObjectPastingEventArgs.
    /// </summary>
    public abstract class DataObjectEventArgs : RoutedEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a DataObjectCopyEvent.
        /// This object created by editors executing a Copy/Paste
        /// and Drag/Drop comands.
        /// </summary>
        /// <param name="routedEvent">
        /// An event id. One of: CopyingEvent or PastingEvent
        /// </param>
        /// <param name="isDragDrop">
        /// A flag indicating if this operation is part of drag/drop.
        /// Copying event fired on drag start, Pasting - on drop.
        /// Cancelling the command stops drag/drop process in
        /// an appropriate moment.
        /// </param>
        internal DataObjectEventArgs(RoutedEvent routedEvent, bool isDragDrop) : base()
        {
            if (routedEvent != DataObject.CopyingEvent && routedEvent != DataObject.PastingEvent && routedEvent != DataObject.SettingDataEvent)
            {
                throw new ArgumentOutOfRangeException("routedEvent");
            }

            RoutedEvent = routedEvent;
            
            _isDragDrop = isDragDrop;
            _commandCancelled = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// A flag indicating if this operation is part of drag/drop.
        /// Copying event fired on drag start, Pasting - on drop.
        /// Cancelling the command stops drag/drop process in
        /// an appropriate moment.
        /// </summary>
        public bool IsDragDrop
        {
            get { return _isDragDrop; }
        }

        /// <summary>
        /// A current cancellation status of the event.
        /// When set to true, copy command is going to be cancelled.
        /// </summary>
        public bool CommandCancelled
        {
            get { return _commandCancelled; }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Sets cancelled status of a command to true.
        /// After calling this method the command will be
        /// stopped from calling.
        /// Applied to Drag (event="Copying", isDragDrop="true")
        /// this would stop the whole dragdrop process.
        /// </summary>
        /// <remarks>
        /// After an event has been cancelled it's impossible
        /// to re-enable it.
        /// </remarks>
        public void CancelCommand()
        {
            _commandCancelled = true;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _isDragDrop;

        private bool _commandCancelled;

        #endregion Private Fields
    }
}
