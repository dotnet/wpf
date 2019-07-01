// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Microsoft.Test.Modeling
{
    /// <summary>
    /// Facilitates changing an XmlDocument using transforms defined
    /// in a second XmlDocument.
    /// </summary>
    /// <remarks>
    /// XamlTransformer is intended to be a light weight XSLT. It uses XPaths to
    /// identify transform targets but provides a limited set of transforming operations.
    /// </remarks>
    public sealed class XamlTransformer
    {
        #region Private Members

        private XmlNamespaceManager nsmgr = null;
        private XmlDocument transformDocument = null;

        private static readonly string wpfNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private static readonly string xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        #endregion

        #region Constructors

        /// <summary>
        /// Create XamlTransformer for a transform file.
        /// </summary>
        /// <param name="xamlTransformFileName">XML file defining transformations.</param>
        public XamlTransformer(string xamlTransformFileName)
        {
            transformDocument = new XmlDocument();
            transformDocument.Load(xamlTransformFileName);
        }

        /// <summary>
        /// Create XamlTransformer for a transform Xaml document.
        /// </summary>
        /// <param name="xamlTransformDocument">XmlDocument defining transformations.</param>
        public XamlTransformer(XmlDocument xamlTransformDocument)
        {
            transformDocument = xamlTransformDocument;
        }

        #endregion

        #region Public and Protected Members

        
        /// <summary>
        /// Find and apply a transform in the transform Xml document to a target
        /// document.
        /// </summary>
        /// <param name="targetXamlFileName">Xml file containing transforms.</param>
        /// <param name="transformName">Name of transform to apply to target document.</param>
        /// <returns>Transformed XmlDocument</returns>
        public XmlDocument ApplyTransform(string targetXamlFileName, string transformName)
        {
            if (String.IsNullOrEmpty(targetXamlFileName))
            {
                throw new ArgumentNullException("targetXamlFileName");
            }

            XmlDocument targetDocument = new XmlDocument();
            targetDocument.Load(targetXamlFileName);

            return ApplyTransform(targetDocument, transformName);
        }

        /// <summary>
        /// Find and apply a transform in the transform Xml document to a target
        /// document.
        /// </summary>
        /// <param name="targetXamlDocument">XmlDocument to transform.</param>
        /// <param name="transformName">Name of transform to apply to target document.</param>
        /// <returns>Transformed XmlDocument</returns>
        public XmlDocument ApplyTransform(XmlDocument targetXamlDocument, string transformName)
        {
            if (targetXamlDocument == null)
            {
                throw new ArgumentNullException("targetXamlDocument");
            }
            if (String.IsNullOrEmpty(transformName))
            {
                throw new ArgumentNullException("transformName");
            }

            XmlDocument modifiedDoc = (XmlDocument)targetXamlDocument.Clone();

            // Construct the XmlNamespaceManager used for xpath queries.
            NameTable ntable = new NameTable();
            nsmgr = new XmlNamespaceManager(ntable);
            nsmgr.AddNamespace("av", wpfNamespace);
            nsmgr.AddNamespace("x", xamlNamespace);

            // Get selected transform.
            XmlElement transform = transformDocument.SelectSingleNode(@"av:XamlTransformer/av:XmlTransform[@Name='" + transformName + "']", nsmgr) as XmlElement;

            if (transform == null)
            {
                throw new ArgumentException("Could not find Xml transform '" + transformName + "'.");
            }

            if (transform == null) { throw new ArgumentNullException("xmlTransform"); }
            if (transform.ChildNodes == null || transform.ChildNodes.Count == 0)
            {
                throw new ArgumentException("XmlTransform " + transform.Attributes["Name"] + " does not contain any transform operations");
            }

            foreach (XmlNode transformOperation in transform.ChildNodes)
            {
                if (transformOperation.NodeType == XmlNodeType.Comment) { continue; }

                string elementXPath = transformOperation.Attributes["TargetElement"].Value;
                if (String.IsNullOrEmpty(elementXPath))
                {
                    throw new InvalidOperationException(transformOperation.Name + " node missing TargetElement attribute: " + transformOperation.OuterXml);
                }

                // Perform XPath search for target element.
                XmlElement targetElement = modifiedDoc.SelectSingleNode(elementXPath, nsmgr) as XmlElement;
                if (targetElement == null)
                {
                    throw new InvalidOperationException("Could not find element with XPath '" + elementXPath + "'.");
                }

                switch (transformOperation.Name)
                {
                    // Add or replace attribute with property and value from node.
                    case "AddAttribute":

                        targetElement.SetAttribute(transformOperation.Attributes["Property"].Value, transformOperation.Attributes["Value"].Value);

                        break;

                    // Add transformation node's content to target element.
                    case "PrependXml":
                    case "AppendXml":

                        XmlNode firstChild = targetElement.FirstChild;

                        foreach (XmlNode childNode in transformOperation.ChildNodes)
                        {
                            XmlNode newNode = modifiedDoc.ImportNode(childNode, true);                           

                            if (transformOperation.Name == "PrependXml")
                            {
                                targetElement.InsertBefore(newNode, firstChild);
                            }
                            else
                            {
                                targetElement.AppendChild(newNode);
                            }
                        }

                        break;

                    default:
                        throw new ArgumentException("Unknown Xml transform operation '" + transformOperation.Name + "'.");
                }
            }

            return modifiedDoc;
        }

        #endregion

    }
}
