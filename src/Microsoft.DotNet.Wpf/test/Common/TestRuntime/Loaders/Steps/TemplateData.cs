// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;

using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Default Data in a XVariation document.
    /// </summary>
	internal class TemplateData
    {
        #region Member variables
		
		internal XmlElement templatedataelement;
		internal XmlElement rootelement;

        internal bool bcontainsrootnodeelement = false;

		ArrayList variationlist;

        #endregion Member variables

        #region Internal API.

        /// <summary>
        /// Constructor that takes a xml files as an argument.
        /// </summary>
        /// <param name="xmlfile"></param>
		internal TemplateData(string xmlfile)
		{
            UtilsLogger.LogDiagnostic = "TemplateData Constructor - Xml File";
            Initialize();

            LoadDocument(xmlfile);
        }

		/// <summary>
		/// Constructor takes an XMLNode as an argument.
		/// </summary>
		/// <param name="templatenode"></param>
		internal TemplateData(XmlNode templatenode)
		{
			Initialize();

			LoadDocument(templatenode);
		}


		/// <summary>
        /// Initializes the default lists.
        /// </summary>
        private void Initialize()
        {
            UtilsLogger.LogDiagnostic = "TemplateData:Initialize.";
            templatedataelement = null;
            variationlist = new ArrayList();
        }

		/// <summary>
		/// Save Default document data from file into XmlNode.
		/// </summary>
		/// <param name="xmlfile"></param>
		private void LoadDocument(string xmlfile)
		{
			UtilsLogger.LogDiagnostic = "TemplateData: Read - Begin";
			string tempfile = CommonHelper.VerifyFileExists(xmlfile);
			if (tempfile == null)
			{
				throw new System.IO.FileNotFoundException(xmlfile + "Could not be found");
			}

			XmlDocumentSW temp = new XmlDocumentSW();
			temp.Load(tempfile);

			XmlNode defaultdatanode = (XmlNode)temp.DocumentElement;
			LoadDocument(defaultdatanode);

			temp = null;
			defaultdatanode = null;
		}

		/// <summary>
		/// Save Default document from XmlNode.
		/// </summary>
		/// <param name="templatenode"></param>
		private void LoadDocument(XmlNode templatenode)
		{
			// There can only be one node under the TemplateData element.
			// This is essentially the document that needs to be output and by 
			// Xml rules only one Root is allowed.

			// Check if there is only one node and if there is only one capture that element.

			int templaterootelementcount = 0;

			for (int i = 0; i < templatenode.ChildNodes.Count; i++)
			{
				if (templatenode.ChildNodes[i].NodeType == XmlNodeType.Element)
				{
                    if (templatenode.ChildNodes[i].Name == Constants.RootNodeVariationElement)
                    {
                        if ( templatenode.ChildNodes[i].ChildNodes.Count != 1 )
                        {
                            throw new ApplicationException("Multiple RootNodeVariations found.");
                        }

                        rootelement = (XmlElement)templatenode.ChildNodes[0];
                        continue;
                    }

                    if ( templatenode.ChildNodes[i].Name == Constants.NodeVariationElement ||
                        templatenode.ChildNodes[i].Name == Constants.AttributeVariationElement ||
                        templatenode.ChildNodes[i].Name == Constants.TextVariationElement)
                    {
                        continue;
                    }

					if (templaterootelementcount++ > 1)
					{
						throw new ApplicationException("TemplateData element cannot have more than one DocumentElement");
					}

                    rootelement = (XmlElement)templatenode.ChildNodes[i];
				}
			}

            templatedataelement = (XmlElement)templatenode;
		}

        #endregion Internal API

    }
}
