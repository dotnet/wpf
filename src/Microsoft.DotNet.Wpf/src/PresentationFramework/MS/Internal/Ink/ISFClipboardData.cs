// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description:
//      A class which can convert the clipboard data from/to StrokeCollection
//
// Features:
//


using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Security;

namespace MS.Internal.Ink
{
    internal class ISFClipboardData : ClipboardData
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        // The default constructor
        internal ISFClipboardData() { }

        // The constructor which takes StrokeCollection argument
        internal ISFClipboardData(StrokeCollection strokes)
        {
            _strokes = strokes;
        }

        // Checks if the data can be pasted.
        internal override bool CanPaste(IDataObject dataObject)
        {
            return dataObject.GetDataPresent(StrokeCollection.InkSerializedFormat, false);
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        // Checks if there is stroke data in this instance
        protected override bool CanCopy()
        {
            return (Strokes != null && Strokes.Count != 0);
        }

        // Copies the internal strokes to the IDataObject
        protected override void DoCopy(IDataObject dataObject)
        {
            // samgeo - Presharp issue
            // Presharp gives a warning when local IDisposable variables are not closed
            // in this case, we can't call Dispose since it will also close the underlying stream
            // which needs to be open for consumers to read
#pragma warning disable 1634, 1691
#pragma warning disable 6518

            // Save the data in the data object.
            MemoryStream stream = new MemoryStream();
            Strokes.Save(stream);
            stream.Position = 0;
            dataObject.SetData(StrokeCollection.InkSerializedFormat, stream);
#pragma warning restore 6518
#pragma warning restore 1634, 1691
        }

        // Retrieves the stroks from the IDataObject
        protected override void DoPaste(IDataObject dataObject)
        {
            // Check if we have ink data
            MemoryStream stream = dataObject.GetData(StrokeCollection.InkSerializedFormat) as MemoryStream;

            StrokeCollection newStrokes = null;
            bool fSucceeded = false;
            if ( stream != null && stream != Stream.Null )
            {
                try
                {
                    // Now add these ink strokes to the InkCanvas ink collection.
                    newStrokes = new StrokeCollection(stream);
                    fSucceeded = true;
                }
                catch ( ArgumentException )
                {
                    // If an invalid stream was passed in, we should get ArgumentException here.
                    // We catch this specific exception and eat it.
                    fSucceeded = false;
                }
            }

            // Depending on whether we are succeeded or not, we set the correct stroke collection here.
            _strokes = fSucceeded ? newStrokes : new StrokeCollection();
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Internal Properties
        //
        //-------------------------------------------------------------------------------

        #region Internal Properties

        // Gets the strokes
        internal StrokeCollection Strokes
        {
            get
            {
                return _strokes;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        StrokeCollection    _strokes;

        #endregion Private Fields
    }
}
