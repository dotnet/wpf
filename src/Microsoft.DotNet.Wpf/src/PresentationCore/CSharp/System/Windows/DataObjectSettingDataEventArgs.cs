// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: DataObjectSettingData event arguments
//

using System;

namespace System.Windows
{
    /// <summary>
    /// Arguments for the DataObject.SettingData event.
    /// The DataObject.SettingData event is raised during
    /// Copy (or Drag start) command when an editor
    /// is going to start data conversion for some
    /// of data formats. By handling this event an application
    /// can prevent from editon doing that thus making
    /// Copy performance better.
    /// </summary>
    public sealed class DataObjectSettingDataEventArgs : DataObjectEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a DataObjectSettingDataEventArgs.
        /// </summary>
        /// <param name="dataObject">
        /// DataObject to which a new data format is going to be added.
        /// </param>
        /// <param name="format">
        /// Format which is going to be added to the DataObject.
        /// </param>
        public DataObjectSettingDataEventArgs(IDataObject dataObject, string format) //
            : base(System.Windows.DataObject.SettingDataEvent, /*isDragDrop:*/false)
        {
            if (dataObject == null)
            {
                throw new ArgumentNullException("dataObject");
            }

            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            _dataObject = dataObject;
            _format = format;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// DataObject to which a new data format is going to be added.
        /// </summary>
        public IDataObject DataObject
        {
            get { return _dataObject; }
        }

        /// <summary>
        /// Format which is going to be added to the DataObject.
        /// </summary>
        public string Format
        {
            get { return _format; }
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
            DataObjectSettingDataEventHandler handler = (DataObjectSettingDataEventHandler)genericHandler;
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

        private string _format;

        #endregion Private Fields
    }

    /// <summary>
    /// The delegate to use for handlers that receive the DataObject.QueryingCopy/QueryingPaste events.
    /// </summary>
    /// <remarks>
    /// A handler for a DataObject.SettingData event.
    /// Te event is fired as part of Copy (or Drag) command
    /// once for each of data formats added to a DataObject.
    /// The purpose of this handler is mostly copy command
    /// optimization. With the help of it application
    /// can filter some formats from being added to DataObject.
    /// The other opportunity of doing that exists in
    /// DataObject.Copying event, which could set all undesirable
    /// formats to null, but in this case the work for data
    /// conversion is already done, which may be too expensive.
    /// By handling DataObject.SettingData event an application
    /// can prevent from each particular data format conversion.
    /// By calling DataObjectSettingDataEventArgs.CancelCommand
    /// method the handler tells an editor to skip one particular
    /// data format (identified by DataObjectSettingDataEventArgs.Format
    /// property). Note that calling CancelCommand method
    /// for this event does not cancel the whole Copy or Drag
    /// command.
    /// </remarks>
    public delegate void DataObjectSettingDataEventHandler(object sender, DataObjectSettingDataEventArgs e);
}
