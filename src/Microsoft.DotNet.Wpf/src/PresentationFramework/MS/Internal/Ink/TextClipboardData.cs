// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      A base class which can copy a unicode text to the IDataObject.
//      It also can get the unicode text from the IDataObject and create a corresponding textbox.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Text;

namespace MS.Internal.Ink
{
    internal class TextClipboardData : ElementsClipboardData
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        // The default constructor
        internal TextClipboardData() : this(null) {}

        // The constructor with a string as argument
        internal TextClipboardData(string text)
        {
            _text = text;
        }

        #endregion Constructors

        // Checks if the data can be pasted.
        internal override bool CanPaste(IDataObject dataObject)
        {
            return ( dataObject.GetDataPresent(DataFormats.UnicodeText, false)
                        || dataObject.GetDataPresent(DataFormats.Text, false)
                        || dataObject.GetDataPresent(DataFormats.OemText, false) );
        }

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        // Checks if the data can be copied.
        protected override bool CanCopy()
        {
            return !string.IsNullOrEmpty(_text);
        }

        // Copy the text to the IDataObject
        protected override void DoCopy(IDataObject dataObject)
        {
            // Put the text to the clipboard
            dataObject.SetData(DataFormats.UnicodeText, _text, true);
        }

        // Retrieves the text from the IDataObject instance.
        // Then create a textbox with the text data.
        protected override void DoPaste(IDataObject dataObject)
        {
            ElementList = new List<UIElement>();
        
            // Get the string from the data object.
            string text = dataObject.GetData(DataFormats.UnicodeText, true) as string;

            if ( String.IsNullOrEmpty(text) )
            {
                // OemText can be retrieved as CF_TEXT.
                text = dataObject.GetData(DataFormats.Text, true) as string;
            }

            if ( !String.IsNullOrEmpty(text) )
            {
                // Now, create a text box and set the text to it.
                TextBox textBox = new TextBox();

                textBox.Text = text;
                textBox.TextWrapping = TextWrapping.Wrap;

                // Add the textbox to the internal array list.
                ElementList.Add(textBox);
            }
}

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private string      _text;

        #endregion Private Fields
    }
}
