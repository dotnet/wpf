// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper methods for code that uses types from System.Xml.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

using System.Windows;

namespace MS.Internal
{
    internal static class SystemXmlHelper
    {
        // return true if the item is an XmlNode
        internal static bool IsXmlNode(object item)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            return (extensions != null) ? extensions.IsXmlNode(item) : false;
        }

        // return true if the item is an XmlNamespaceManager
        internal static bool IsXmlNamespaceManager(object item)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            return (extensions != null) ? extensions.IsXmlNamespaceManager(item) : false;
        }

        // if the item is an XmlNode, get the value corresponding to the given name
        internal static bool TryGetValueFromXmlNode(object item, string name, out object value)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            if (extensions != null)
            {
                return extensions.TryGetValueFromXmlNode(item, name, out value);
            }

            value = null;
            return false;
        }

        // create a comparer for an Xml collection (if applicable)
        internal static IComparer PrepareXmlComparer(IEnumerable collection, SortDescriptionCollection sort, CultureInfo culture)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            if (extensions != null)
            {
                return extensions.PrepareXmlComparer(collection, sort, culture);
            }

            return null;
        }

        // return true if parent is an empty XmlDataCollection.
        internal static bool IsEmptyXmlDataCollection(object parent)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            return (extensions != null) ? extensions.IsEmptyXmlDataCollection(parent) : false;
        }

        // when item is an XmlNode, get its tag name (using the target DO as context
        // for namespace lookups)
        internal static string GetXmlTagName(object item, DependencyObject target)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            return (extensions != null) ? extensions.GetXmlTagName(item, target) : null;
        }

        // find a node with the given string as its InnerText
        internal static object FindXmlNodeWithInnerText(IEnumerable items, object innerText, out int index)
        {
            index = -1;
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            return (extensions != null) ? extensions.FindXmlNodeWithInnerText(items, innerText, out index) : DependencyProperty.UnsetValue;
        }

        // get the InnerText of the given node
        internal static object GetInnerText(object item)
        {
            SystemXmlExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXml();
            return (extensions != null) ? extensions.GetInnerText(item) : null;
        }
    }
}

