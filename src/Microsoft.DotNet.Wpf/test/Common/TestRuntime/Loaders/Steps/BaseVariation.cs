// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;

using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Abstract class - BaseVariation 
    /// Encapsulates information specific to any kind of Variation which is -
    ///		ID, Unsupportedelements and VariationChildren.
    /// </summary>
    internal abstract class BaseVariation
    {
        #region Member variables
        string id;
        string[] unsupportedelements;
        internal string innerxml;
        internal Hashtable attributestable = null;

        /// <summary>
        /// List of the current variations children.
        /// </summary>
        internal XmlNodeList variationchildren;

        #endregion

        #region Public Methods

        /// <summary>
        /// Base constructor to initialize local variables.
        /// </summary>
        public BaseVariation()
        {
            id = null;
            variationchildren = null;
            unsupportedelements = new string[1];
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method gets the innerxml based on a XmlNodeList instead.
        /// This was implemented as we need to check for the innerxml for unsupported elements.
        /// </summary>
        /// <param name="nodelist">List of nodes from which to get the innerxml</param>
        /// <returns></returns>
        private string GetInnerXml(XmlNodeList nodelist)
        {
            UtilsLogger.LogDiagnostic = "BaseVariation:GetInnerXml - Begin.";
            if (nodelist == null)
            {
                UtilsLogger.LogDiagnostic = "Nodelist input is null,exiting.";
                return null;
            }

            string xml = null;

            // Loop through node list and get innerxml.
            for (int i = 0; i < nodelist.Count; i++)
            {
                XmlNode node = nodelist[i];
                if (node == null)
                {
                    continue;
                }

                if (!CheckforUnsupportedElements(node))
                {
                    UtilsLogger.LogDiagnostic = "BaseVariation:GetInnerXml - Found unsupported element, Exiting";
                    throw new Exception("Unsupported element " + node.Name + " found in document");
                }

                xml += node.OuterXml;
            }

            UtilsLogger.LogDiagnostic = "BaseVariation:GetInnerXml - Exit";
            return xml;
        }

        /// <summary>
        /// Method to check for unsupported elements in the current variation node.
        /// </summary>
        /// <param name="varnode">Variation Node.</param>
        /// <returns></returns>
        private bool CheckforUnsupportedElements(XmlNode varnode)
        {
            UtilsLogger.LogDiagnostic = "BaseVariation:CheckforUnsupportedElements - Begin";
            if (varnode == null)
            {
                UtilsLogger.LogDiagnostic = "Input varnode was null, exiting";
                return false;
            }

            // Find any unsupported elements which are children of the current node.
            // If they are then throw a NotSupportedException otherwise continue.
            for (int i = 0; unsupportedelements != null && i < unsupportedelements.Length; i++)
            {
                if (unsupportedelements[i] == null)
                {
                    continue;
                }

                if (varnode.NodeType == XmlNodeType.Element)
                {
                    if (((XmlElement)varnode).GetElementsByTagName(unsupportedelements[i]).Count > 0)
                    {
                        throw new NotSupportedException("Nested " + unsupportedelements[i] + " elements are not supported");
                    }
                }
            }

            UtilsLogger.LogDiagnostic = "BaseVariation:CheckforUnsupportedElements - End";
            return true;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Based on input XmlNode passed in check to see if there are any unsupported elements.
        /// Initialization method that initializes member variables based on the XmlNode passed in. 
        /// The initialization is done by check if an ID attribute is specified on the VariationElement.
        /// </summary>
        /// <param name="varnode">Xmlnode that holds the variation information.</param>
        internal virtual void Initialize(XmlNode varnode)
        {
            UtilsLogger.LogDiagnostic = "BaseVariation:Initialize";

            if (varnode == null || varnode.Attributes.Count == 0)
            {
                UtilsLogger.LogDiagnostic = "Variation node passed in was null, exiting";
                return;
            }

            if (!CheckforUnsupportedElements(varnode))
            {
                UtilsLogger.LogDiagnostic = "BaseVariation:GetInnerXml - Found unsupported element, Exiting";
                throw new Exception("Unsupported element " + varnode.Name + " found in document");
            }

            // Get variation supported attributes.
            if (varnode.Attributes[Constants.IDAttribute] == null)
            {
                UtilsLogger.Log = "Found variation without ID, ignoring";
                return;
            }

            if (String.IsNullOrEmpty(varnode.Attributes[Constants.IDAttribute].Value))
            {
                UtilsLogger.Log = "Found variation with ID set to blank";
                return;
            }

            id = varnode.Attributes[Constants.IDAttribute].Value;

            UtilsLogger.LogDiagnostic = "Variation ID = " + id;
            if (varnode.HasChildNodes)
            {
                UtilsLogger.LogDiagnostic = "Variation had children, getting innerxml";
                innerxml = GetInnerXml(varnode.ChildNodes);
            }

            UtilsLogger.LogDiagnostic = "BaseVariation : Initialize(XmlNode varnode) - Exit";
        }

        /// <summary>
        /// Process Unrecognized Attributes.
        /// </summary>
        /// <param name="node"></param>
        internal void UnrecognizedAttributes(XmlNode node)
        {
            attributestable = new Hashtable();

            if (node == null)
            {
                return;
            }

            if (node.Attributes.Count == 0)
            {
                return;
            }

            int count = 0;

            // Todo: This logic is still incomplete and needs to be thought over a little bit.
            do
            {
                if (node.Attributes[count].Name == Constants.IDAttribute)
                {
                    count++;
                    continue;
                }

                attributestable.Add(node.Attributes[count].Name, node.Attributes[count].Value);
                node.Attributes.Remove(node.Attributes[count]);
            } while (node.Attributes.Count > count);
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Property to set Unsupported elements on all Variations.
        /// </summary>
        /// <value></value>
        internal string[] NotSupportedNestedElements
        {
            set
            {
                unsupportedelements = value;
            }
        }

        /// <summary>
        /// Property to get the unique ID for the current variation.
        /// </summary>
        /// <value></value>
        internal string ID
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Property getter and setter to set or get the InnerXml for a variation.
        /// </summary>
        /// <value>Xml String.</value>
        internal string VariationChildrenXml
        {
            set
            {
                // Load the string into an XmlDocument after creating a placeholder.
                // Get the innerxml of the the current string by using the GetInnerXml method.
                UtilsLogger.LogDiagnostic = "Creating XmlDocument and PlaceHolder";
                try
                {
                    XmlDocumentSW tempdoc = new XmlDocumentSW();
                    XmlNode tempnode = tempdoc.CreateElement(Constants.NodeVariationElement + "PlaceHolder");

                    tempnode.InnerXml = value;
                    tempdoc.AppendChild(tempnode);
                    innerxml = GetInnerXml(tempdoc.DocumentElement.ChildNodes);
                }
                catch (Exception ex)
                {
                    UtilsLogger.LogError = "Exception occured in BaseVariation set VariationChildrenXml";
                    throw ex;
                }
            }
            get
            {
                return innerxml;
            }
        }

        /// <summary>
        /// Property to set VariationChildren XmlNode list.
        /// </summary>
        /// <value>XmlNodeList list of current variation children.</value>
        internal XmlNodeList VariationChildren
        {
            get
            {
                UtilsLogger.LogDiagnostic = "BaseVariation: VariationChildren Property Start";
                if (innerxml == null)
                {
                    return null;
                }

                if (variationchildren == null)
                {
                    try
                    {
                        XmlDocumentSW tempdoc = new XmlDocumentSW();
                        XmlElement placeholder = tempdoc.CreateElement("PlaceHolder");

                        placeholder.InnerXml = innerxml;
                        variationchildren = placeholder.ChildNodes;

                        placeholder = null;
                    }
                    catch (XmlException ex)
                    {
                        UtilsLogger.LogError = "Error occured when return variation list";
                        throw ex;
                    }
                }

                UtilsLogger.LogDiagnostic = "BaseVariation: VariationChildren Property Exit";
                return variationchildren;
            }
            set
            {
                UtilsLogger.LogDiagnostic = "BaseVariation: VariationChildren Property Start";
                if (value != null)
                {
                    // Get innerxml and set local member variable.
                    innerxml = GetInnerXml(value);

                    variationchildren = value;
                }

                UtilsLogger.LogDiagnostic = "BaseVariation: VariationChildren Property Exit";
            }
        }

        #endregion
    }

}
