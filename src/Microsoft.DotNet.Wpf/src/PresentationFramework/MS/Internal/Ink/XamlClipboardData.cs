// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description:
//      A base class which can copy an array of UIElement to the IDataObject as Xaml format.
//      It also can retrieve the Xaml data from the IDataObject and create a UIElement array.
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Security;
using System.Text;
using System.Diagnostics;
using MS.Internal; //security helper from PresentationCore

namespace MS.Internal.Ink
{
    internal class XamlClipboardData : ElementsClipboardData
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------
        
        #region Constructors

        // The default constructor
        internal XamlClipboardData() { }

        // The constructor with UIElement[] as an argument
        internal XamlClipboardData(UIElement[] elements) : base (elements)
        {
        }

        // Checks if the data can be pasted.
        internal override bool CanPaste(IDataObject dataObject)
        {
            // Check if we have Xaml data
            bool hasXamlData = dataObject.GetDataPresent(DataFormats.Xaml, false);
            
            return hasXamlData;
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        // Check if we have data to be copied
        protected override bool CanCopy()
        {
            return Elements != null && Elements.Count != 0;
        }

        // Convert the elements to the Xaml and copy the Xaml to the IDataObject

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataObject"></param>
        protected override void DoCopy(IDataObject dataObject)
        {
            // Serialize the selected elements and add it to the data object.
            StringBuilder xmlData = new StringBuilder();

            foreach ( UIElement element in Elements )
            {
                string xml;

                xml = XamlWriter.Save(element);

                xmlData.Append(xml);
            }

            // Set the data object as XML format.
            dataObject.SetData(DataFormats.Xaml, xmlData.ToString());
        }

        // Retrieves the Xaml from the IDataObject and instantiate the elements based on the Xaml
        protected override void DoPaste(IDataObject dataObject)
        {
            ElementList = new List<UIElement>();
            
            // Get the XML data from the data object.
            string xml = dataObject.GetData(DataFormats.Xaml) as string;

            if ( !String.IsNullOrEmpty(xml) )
            {
                UIElement element = XamlReader.Load(new System.Xml.XmlTextReader(new System.IO.StringReader(xml)), useRestrictiveXamlReader: true) as UIElement;
                if (element != null)
                {
                    ElementList.Add(element);
                }
            }
        }

        #endregion Protected Methods
    }
}
