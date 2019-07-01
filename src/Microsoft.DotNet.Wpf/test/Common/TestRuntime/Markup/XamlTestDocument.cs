// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Reflection;


using Microsoft.Test.Serialization;

namespace Microsoft.Test.Markup
{
    /// <summary>
    /// XamlTestDocument is a wrapper of XmlDocument that adds several helpers
    /// for creating the type of Xaml file used in core test cases. 
    /// </summary>  
    public class XamlTestDocument : XmlDocument
    {
        /// <summary>
        /// Create a XamlTestDocument instance from a template file and an elements file.
        /// This XmlDocument will be an instance of the template document.
        /// </summary>
        public XamlTestDocument(string xamlTemplateFilename, string xamlElementsFilename)
            : base()
        {
            _xamlTemplateFilename = xamlTemplateFilename;
            _xamlElementsFilename = xamlElementsFilename;

            // Initialize this document using the template file.
            if (_xamlTemplateFilename != null)
            {
                this.Load(_xamlTemplateFilename);
            }

            // Construct an XmlNamespaceManager for xpath queries.
            NameTable ntable = new NameTable();
            _nsmgr = new XmlNamespaceManager(ntable);
            _nsmgr.AddNamespace("av", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            _nsmgr.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            _nsmgr.AddNamespace("cmn", "clr-namespace:Microsoft.Test.Serialization.CustomElements;assembly=TestRuntime");

            // Load XmlDocument with snippets of xaml for pasting to the test doc.
            XmlDocument tempDoc = new XmlDocument();
            tempDoc.Load(_xamlElementsFilename);
            _snippetsDoc = tempDoc;

            // Save the test root.
            // Initialize this document using the template file.
            if (_xamlTemplateFilename != null)
            {
                _testRootElement = (XmlElement)this.SelectSingleNode("//*[@Name='TestRoot']");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public XmlElement TestRoot
        {
            get
            {
                return _testRootElement;
            }
        }

        /// <summary>
        /// Don't use this!
        /// It is required for TemplateModel which makes little use of TestId's and 
        /// has several unique XmlNode.SelectNodes/SelectSingleNode calls. 
        /// </summary>
        public XmlNamespaceManager Nsmgr
        {
            get
            {
                return _nsmgr;
            }
        }

        /// <summary>
        /// Replace a substring in an xml attribute with a value.
        /// </summary>
        public void ReplaceAttributeSubstring(XmlElement element, string attributeName, string substring, string value)
        {
            XmlAttribute attribute = element.Attributes[attributeName];
            attribute.Value = attribute.Value.Replace(substring, value);
        }

        /// <summary>
        /// Remove attribute from an xml element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        public void RemoveAttribute(XmlElement element, string attributeName)
        {
            element.Attributes.Remove(element.Attributes[attributeName]);
        }

        private void _ValidateDocumentElement()
        {
            if (_xamlTemplateFilename == null || this.DocumentElement == null)
            {
                throw new InvalidOperationException("The main xaml document used as a template for xaml generation has not been loaded.");
            }
        }

        /// <summary>
        /// Sets Verifier attribute to string representation of verifier routine
        /// that will be called when the root element is rendered.
        /// </summary>
        [CLSCompliant(false)]
        public void SetVerifierRoutine(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            _ValidateDocumentElement();

            XmlAttribute attrib = this.DocumentElement.GetAttributeNode("Verifier");

            if (attrib == null)
            {
                attrib = this.CreateAttribute("Verifier");
                this.DocumentElement.Attributes.Append(attrib);
            }

            attrib.Value = methodInfo.DeclaringType.Assembly.CodeBase +
                           "#" + methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
        }


        /// <summary>
        /// Add attribute to xml element.
        /// </summary>
        public XmlAttribute AddAttribute(XmlElement element, string attributeName, string attributeValue)
        {
            XmlAttribute attrib = this.CreateAttribute(attributeName);

            attrib.Value = attributeValue;

            element.Attributes.Append(attrib);

            return attrib;
        }

        /// <summary>
        /// Create a child element under parent element.
        /// </summary>
        /// <remarks>Should be FindOrAddElement? TryAddElement? </remarks>
        public XmlElement AddElement(XmlElement parent, string childName)
        {
            // Don't add element if it already exists.
            XmlElement element = parent[childName];

            if (element != null)
            {
                return element;
            }

            XmlElement child = this.CreateElement(childName, parent.NamespaceURI);
            return (XmlElement)parent.PrependChild(child);
        }


        /// <summary>
        /// Clone a sub-tree from one XmlDocument instance to another.
        /// </summary>
        /// <param name="firstElement"></param>
        /// <param name="testId"></param>
        /// <returns></returns>
        public XmlElement ImportAndAppendElement(XmlElement firstElement, string testId)
        {
            XmlElement snippet = GetSnippet(testId);
            return ImportAndAppendElement(firstElement, snippet);
        }

        /// <summary>
        /// Clones a sub-tree from one XmlDocument instance to another.
        /// </summary>
        public XmlElement ImportAndAppendElement(XmlElement parentElement, XmlElement childElement)
        {
            // Import second element to first element's document.
            XmlElement newElement = (XmlElement)parentElement.OwnerDocument.ImportNode(childElement, true);

            // Insert newly-imported element under the first element.
            parentElement.AppendChild(newElement);

            return newElement;
        }

        /// <summary>
        /// Clones a sub-tree from one XmlDocument instance to another.
        /// </summary>
        public XmlElement ImportAndPrependElement(XmlElement parentElement, XmlElement childElement)
        {
            // Import second element to first element's document.
            XmlElement newElement = (XmlElement)parentElement.OwnerDocument.ImportNode(childElement, true);

            // Insert newly-imported element under the first element.
            parentElement.PrependChild(newElement);

            return newElement;
        }

        /// <summary>
        /// Xml document that contains xaml snippets used for xaml generation.
        /// </summary>
        public XmlDocument SnippetsDoc
        {
            get
            {
                return _snippetsDoc;
            }
        }

        /// <summary>
        /// Retrive xaml snippet from elements doc.
        /// </summary>
        public XmlElement GetSnippet(string testId)
        {
            return GetSnippetByXPath("//*[@TestId='" + testId + "']");
        }

        /// <summary>
        /// Retrieve xaml snippet from elements doc using xpath.
        /// </summary>
        public XmlElement GetSnippetByXPath(string xpath)
        {
            return GetSnippetByXPath(xpath, true);
        }

        /// <summary>
        /// Retrieve xaml snippet from elements doc using xpath.
        /// </summary>
        public XmlElement GetSnippetByXPath(string xpath, bool verifySnippet)
        {
            XmlElement snippet = (XmlElement)_snippetsDoc.SelectSingleNode(xpath, _nsmgr);

            if (verifySnippet && snippet == null)
            {
                throw new InvalidOperationException("Could not get snippet with xpath: " + xpath);
            }

            return snippet;
        }

        /// <summary>
        /// Returns the xml node in an &lt;TypeContainer/&gt; element with the Type attribute matching type.
        /// If the same type is not found looks for a base Type.
        /// </summary>
        public XmlElement GetContainerNode(Type type)
        {
            XmlElement containerNode = null;
            Type baseType = type;

            // Find a container node for this type or, failing that, one of its base types.
            while (baseType != null && containerNode == null)
            {
                containerNode = (XmlElement)_snippetsDoc.SelectSingleNode("//*[@Type='" + baseType.Name + "']", _nsmgr);

                baseType = baseType.BaseType;
            }

            if (containerNode != null)
            {
                containerNode = (XmlElement)containerNode.FirstChild;
            }

            if (containerNode == null)
            {
                throw new InvalidOperationException("Could not find xaml container snippet for type '" + type.Name + "' in '" + _xamlElementsFilename + "'.");
            }

            return containerNode;
        }

        /// <summary>
        /// Returns the xml node that has Content for the specified type.
        /// </summary>
        public XmlElement GetContentNode(Type type)
        {
            XmlElement containerNode = null;
            Type baseType = type;

            while (baseType != null && containerNode == null)
            {
                containerNode = (XmlElement)_snippetsDoc.SelectSingleNode("//av:TypeContainer[@Type='" + baseType.Name + "']", _nsmgr);

                baseType = baseType.BaseType;
            }

            if (containerNode != null)
            {
                if (containerNode.ChildNodes.Count == 2)
                    containerNode = (XmlElement)containerNode.ChildNodes[1];
                else
                    return null;
            }

            if (containerNode == null)
            {
                throw new InvalidOperationException("Could not find xaml container snippet for type '" + type.Name + "' in '" + _xamlElementsFilename + "'.");
            }

            return containerNode;
        }

        /// <summary>
        /// Removes all TestIds from main document.
        /// </summary>
        /// <remarks>
        /// Candidate for refactor to CoreXamlHelper
        /// </remarks>
        public void RemoveTestIds()
        {
            // Get all elements with TestId attribute.
            XmlNodeList testList = this.SelectNodes(".//*[@TestId]", _nsmgr);

            // For each one, remove the TestId attribute.
            for (int i = 0; i < testList.Count; i++)
            {
                XmlAttribute attrib = testList[i].Attributes["TestId"];
                attrib.OwnerElement.Attributes.Remove(attrib);
            }
        }

        //
        // Private fields
        //

        private string _xamlTemplateFilename = null;
        private string _xamlElementsFilename = null;

        // Convenient references to xml nodes used throughout 
        // the xaml construction routines.
        private XmlDocument _snippetsDoc = null;

        private XmlElement _testRootElement = null;

        // Namespace manager for xpath queries.
        private XmlNamespaceManager _nsmgr = null;
    }
}

