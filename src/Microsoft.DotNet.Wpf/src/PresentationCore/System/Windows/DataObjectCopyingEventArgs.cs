// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: DataObject.Copying event arguments
//

using System;

namespace System.Windows
{
    /// <summary>
    /// Arguments for the DataObject.Copying event.
    /// 
    /// The DataObject.Copying event is raised when an editor
    /// has converted a content of selection into all appropriate
    /// clipboard data formats, collected them all in DataObject
    /// and is ready to put the object onto the Clipboard.
    /// An application can inspect DataObject, change, remove or
    /// add some data formats into it and decide whether to proceed
    /// with the copying or cancel it.
    /// </summary>
    public sealed class DataObjectCopyingEventArgs : DataObjectEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates an arguments for a DataObject.Copying event.
        /// This object created by editors executing a Copy and Drag.
        /// </summary>
        /// <param name="dataObject">
        /// DataObject filled in with all appropriate data formats
        /// and ready for putting into the Clipboard
        /// or to used in dragging gesture.
        /// </param>
        /// <param name="isDragDrop">
        /// A flag indicating if this operation is part of drag/drop.
        /// Copying event is fired on drag start.
        /// </param>
        public DataObjectCopyingEventArgs(IDataObject dataObject, bool isDragDrop) //
            : base(System.Windows.DataObject.CopyingEvent, isDragDrop)
        {
            if (dataObject == null)
            {
                throw new ArgumentNullException("dataObject");
            }

            _dataObject = dataObject;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DataObject filled in with all appropriate data formats
        /// and ready for putting into the Clipboard.
        /// </summary>
        public IDataObject DataObject
        {
            get { return _dataObject; }
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
            DataObjectCopyingEventHandler handler = (DataObjectCopyingEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IDataObject _dataObject;

        #endregion Private Fields
    }

    /// <summary>
    /// The delegate to use for handlers that receive the DataObject.Copying event.
    /// </summary>
    /// <remarks>
    /// A handler for DataObject.Copying event.
    /// The handler is expected to inspect the content of a data object
    /// passed via event arguments (DataObjectCopyingEventArgs.DataObject)
    /// and add additional (custom) data format to it.
    /// It's also possible for the handler to change
    /// the contents of other data formats already put on DataObject
    /// or even remove some of those formats.
    /// All this happens before DataObject is put on
    /// the Clipboard (in copy operation) or before DragDrop
    /// process starts.
    /// The handler can cancel the whole copying event
    /// by calling DataObjectCopyingEventArgs.CancelCommand method.
    /// For the case of Copy a command will be cancelled,
    /// for the case of DragDrop a dragdrop process will be
    /// terminated in the beginning.
    /// </remarks>
    public delegate void DataObjectCopyingEventHandler(object sender, DataObjectCopyingEventArgs e);
}
