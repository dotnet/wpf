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
            bool hasXamlData = false;
            try
            {
                hasXamlData = dataObject.GetDataPresent(DataFormats.Xaml, false);
            }
            catch ( SecurityException )
            {
                // If we are under the partial trust, the Xaml format will fail when it needs to be registered.
                // In this case, we should just disable the Paste Xaml format content.
                hasXamlData = false;
            }
            
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
        ///<SecurityNote>
        ///  Critical: Calls critical method 
        ///     SecurityHelper.ExtractAppDomainPermissionSetMinusSiteOfOrigin
        /// 
        ///     We call this to ensure that the appdomain permission set is on the clipboard.
        ///     The Clipboard methods use this information to ensure that apps can not paste data
        ///     that was copied in low trust to application to a high trust application
        ///</SecurityNote>
        [SecurityCritical]
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

            //
            //  we need to copy the permission set on the clipboard for 
            //  the Clipboard class methods.  See security note for details.
            //
            PermissionSet permSet = SecurityHelper.ExtractAppDomainPermissionSetMinusSiteOfOrigin();
            string setString = permSet.ToString();
            Debug.Assert(setString.Length > 0);
            dataObject.SetData(DataFormats.ApplicationTrust, setString);
}

        // Retrieves the Xaml from the IDataObject and instantiate the elements based on the Xaml
        protected override void DoPaste(IDataObject dataObject)
        {
            ElementList = new List<UIElement>();
            
            // Get the XML data from the data object.
            string xml = dataObject.GetData(DataFormats.Xaml) as string;

            if ( !String.IsNullOrEmpty(xml) )
            {
                bool useRestrictiveXamlReader = !Clipboard.UseLegacyDangerousClipboardDeserializationMode();
                UIElement element = XamlReader.Load(new System.Xml.XmlTextReader(new System.IO.StringReader(xml)), useRestrictiveXamlReader) as UIElement;
                if (element != null)
                {
                    ElementList.Add(element);
                }
            }
        }

        #endregion Protected Methods
    }
}
