// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Helper methods for code that uses types from System.Xml.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

using System.Windows;
using System.Windows.Data;
using MS.Internal.Data;

namespace MS.Internal
{
    //FxCop can't tell that this class is instantiated via reflection, so suppress the FxCop warning.
    [SuppressMessage("Microsoft.Performance","CA1812:AvoidUninstantiatedInternalClasses")]
    internal class SystemXmlExtension : SystemXmlExtensionMethods
    {
        // return true if the item is an XmlNode
        internal override bool IsXmlNode(object item)
        {
            return item is XmlNode;
        }

        // return true if the item is an XmlNamespaceManager
        internal override bool IsXmlNamespaceManager(object item)
        {
            return item is XmlNamespaceManager;
        }

        // if the item is an XmlNode, get the value corresponding to the given name
        internal override bool TryGetValueFromXmlNode(object item, string name, out object value)
        {
            XmlNode node = item as XmlNode;
            if (node != null)
            {
                value = SelectStringValue(node, name, null);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        // create a comparer for an Xml collection (if applicable)
        internal override IComparer PrepareXmlComparer(IEnumerable collection, SortDescriptionCollection sort, CultureInfo culture)
        {
            XmlDataCollection xdc = collection as XmlDataCollection;
            if (xdc != null)
            {
                Invariant.Assert(sort != null);
                return new XmlNodeComparer(sort, xdc.XmlNamespaceManager, culture);
            }
            return null;
        }

        // return true if parent is an empty XmlDataCollection.
        internal override bool IsEmptyXmlDataCollection(object parent)
        {
            XmlDataCollection xdc = parent as XmlDataCollection;
            return (xdc != null) ? xdc.Count > 0 : false;
        }

        // when item is an XmlNode, get its tag name (using the target DO as context
        // for namespace lookups)
        internal override string GetXmlTagName(object item, DependencyObject target)
        {
            System.Xml.XmlNode node = (System.Xml.XmlNode)item;
            XmlNamespaceManager namespaceManager = GetXmlNamespaceManager(target);
            if (namespaceManager != null)
            {
                string prefix = namespaceManager.LookupPrefix(node.NamespaceURI);
                if (prefix != string.Empty)
                    return string.Concat(prefix, ":", node.LocalName);
            }

            return node.Name;
        }

        // find a node with the given string as its InnerText
        internal override object FindXmlNodeWithInnerText(IEnumerable items, object innerText, out int index)
        {
            string innerTextString = innerText as string;

            if (innerTextString != null)
            {
                index = 0;
                foreach (object item in items)
                {
                    XmlNode node = item as XmlNode;
                    if (node != null && node.InnerText == innerTextString)
                        return node;
                    ++index;
                }
            }

            index = -1;
            return DependencyProperty.UnsetValue;
        }

        // get the InnerText of the given node
        internal override object GetInnerText(object item)
        {
            XmlNode node = item as XmlNode;

            if (node != null)
            {
                return node.InnerText;
            }
            else
            {
                return null;
            }
        }


        // Return a string by applying an XPath query to an XmlNode.
        internal static string SelectStringValue(XmlNode node, string query, XmlNamespaceManager namespaceManager)
        {
            string strValue;
            XmlNode result;

            result = node.SelectSingleNode(query, namespaceManager);

            if (result != null)
            {
                strValue = ExtractString(result);
            }
            else
            {
                strValue = String.Empty;
            }

            return strValue;
        }

        // Get a string from an XmlNode (of any kind:  element, attribute, etc.)
        private static string ExtractString(XmlNode node)
        {
            string value = String.Empty;

            if (node.NodeType == XmlNodeType.Element)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    if (node.ChildNodes[i].NodeType == XmlNodeType.Text)
                    {
                        value += node.ChildNodes[i].Value;
                    }
                }
            }
            else
            {
                value = node.Value;
            }
            return value;
        }

        // find the appropriate namespace manager for the given element
        private static XmlNamespaceManager GetXmlNamespaceManager(DependencyObject target)
        {
            XmlNamespaceManager nsmgr = Binding.GetXmlNamespaceManager(target);

            if (nsmgr == null)
            {
                XmlDataProvider xdp = Helper.XmlDataProviderForElement(target);
                nsmgr = (xdp != null) ? xdp.XmlNamespaceManager : null;
            }

            return nsmgr;
        }
    }
}

