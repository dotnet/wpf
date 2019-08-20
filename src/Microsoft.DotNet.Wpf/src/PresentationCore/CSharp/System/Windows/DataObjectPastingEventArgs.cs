// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: DataObject.Pasting event arguments
//

using System;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    /// Arguments for the DataObject.Pasting event.
    /// 
    /// The DataObject.Pasting event is raising when an editor
    /// has inspected all formats available on a data object
    /// has choosen one of them as the most suitable and
    /// is ready for pasting it into a current selection.
    /// An application can inspect a DataObject, change, remove or
    /// add some data formats into it and decide whether to proceed
    /// with the pasting or cancel it.
    /// </summary>
    public sealed class DataObjectPastingEventArgs : DataObjectEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a DataObjectPastingEvent.
        /// This object created by editors executing a Copy/Paste
        /// and Drag/Drop comands.
        /// </summary>
        /// <param name="dataObject">
        /// DataObject extracted from the Clipboard and intended
        /// for using in pasting.
        /// </param>
        /// <param name="isDragDrop">
        /// A flag indicating whether this operation is part of drag/drop.
        /// Pasting event is fired on drop.
        /// </param>
        /// <param name="formatToApply">
        /// String identifying a format an editor has choosen
        /// as a candidate for applying in Paste operation.
        /// An application can change this choice after inspecting
        /// the content of data object.
        /// </param>
        public DataObjectPastingEventArgs(IDataObject dataObject, bool isDragDrop, string formatToApply) //
            : base(System.Windows.DataObject.PastingEvent, isDragDrop)
        {
            if (dataObject == null)
            {
                throw new ArgumentNullException("dataObject");
            }

            if (formatToApply == null)
            {
                throw new ArgumentNullException("formatToApply");
            }

            if (formatToApply == string.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_EmptyFormatNotAllowed));
            }

            if (!dataObject.GetDataPresent(formatToApply))
            {
                throw new ArgumentException(SR.Get(SRID.DataObject_DataFormatNotPresentOnDataObject, formatToApply));
            }

            _originalDataObject = dataObject;
            _dataObject = dataObject;
            _formatToApply = formatToApply;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DataObject original extracted from the Clipboard.
        /// It's content cannot be changed to avoid side effects
        /// on subsequent paste operations in this or other
        /// applications.
        /// To change the content of a data object custon handler
        /// must create new instance of an object and assign it
        /// to DataObject property, which will be used by
        /// an editor to perform a paste operation.
        /// Initially both properties DataObject and SourceDataObject
        /// have the same value. DataObject property can be changed
        /// by a custom handler, SourceDataObject keeps original value.
        /// SourceDataObject can be useful in a case when several
        /// independent DataObjectPastingEventHandlers workone after onother.
        /// After one handler added its new DataObject SourceDataObject
        /// property allows other handlers to access original clipboard.
        /// </summary>
        public IDataObject SourceDataObject
        {
            get
            {
                return _originalDataObject;
            }
        }

        /// <summary>
        /// DataObject suggested for a pasting operation.
        /// Originally this property has the same value as SourceDataObject.
        /// Custom handlers can change it by assigning some new dataobject.
        /// This new dataobject must have at least one format set to it,
        /// which will become a suggested format (FormatToAppy) when
        /// dataobject is assigned. Thus FormatToAlly is always consistent
        /// with current DataObject (but not necessarily with SourceDataObject).
        /// </summary>
        public IDataObject DataObject
        {
            get 
            { 
                return _dataObject; 
            }

            set
            {
                string[] availableFormats;

                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                availableFormats = value.GetFormats(/*autoConvert:*/false);
                if (availableFormats == null || availableFormats.Length == 0)
                {
                    throw new ArgumentException(SR.Get(SRID.DataObject_DataObjectMustHaveAtLeastOneFormat));
                }

                _dataObject = value;
                _formatToApply = availableFormats[0];
            }
        }

        /// <summary>
        /// String identifying a format an editor has choosen
        /// as a candidate for applying in Paste operation.
        /// An application can change this choice after inspecting
        /// the content of data object.
        /// The value assigned to FormatToApply must be present
        /// on a current DataObject. The invariant is
        /// this.DataObject.GetDataPresent(this.FormatToApply) === true.
        /// </summary>
        public string FormatToApply
        {
            get 
            { 
                return _formatToApply; 
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (!_dataObject.GetDataPresent(value))
                {
                    throw new ArgumentException(SR.Get(SRID.DataObject_DataFormatNotPresentOnDataObject, value));
                }

                _formatToApply = value;
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
            DataObjectPastingEventHandler handler = (DataObjectPastingEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IDataObject _originalDataObject;

        private IDataObject _dataObject;

        private string _formatToApply;

        #endregion Private Fields
    }

    /// <summary>
    /// The delegate to use for handlers that receive the DataObject.Pasting event.
    /// </summary>
    /// <remarks>
    /// An event handler for a DataObject.Pasting event.
    /// It is called when ah editor already made a decision
    /// what format (from available on the Cliipboard)
    /// to apply to selection. With this handler an application
    /// has a chance to inspect a content of DataObject extracted
    /// from the Clipboard and decide what format to use instead.
    /// There are four options for the handler here:
    /// a) to cancel the whole Paste/Drop event by calling
    /// DataObjectPastingEventArgs.CancelCommand method,
    /// b) change an editor's choice of format by setting
    /// new value for DataObjectPastingEventArgs.FormatToApply
    /// property (the new value is supposed to be understandable
    /// by an editor - it's application's code responsibility
    /// to act consistently with an editor; example is to
    /// replace "rich text" (xml) format to "plain text" format -
    /// both understandable by the TextEditor).
    /// c) choose it's own custom format, apply it to a selection
    /// and cancel a command for the following execution in an
    /// editor by calling DataObjectPastingEventArgs.CancelCommand
    /// method. This is how custom data formats are expected
    /// to be pasted.
    /// d) create new piece of data and suggest it in a new instance
    /// of DataObject. newDataObject instance must be created
    /// with some format set to it and assigned to DataObject property.
    /// SourceDataObject property keeps an original DataObject
    /// came from the Clipboard. This original dataobject cannot be changed.
    /// So by assigning new dataobject a custom handler can suggest
    /// ned data formats or change existing dataformats.
    /// </remarks>
    public delegate void DataObjectPastingEventHandler(object sender, DataObjectPastingEventArgs e);
}
