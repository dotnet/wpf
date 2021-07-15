// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
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
    internal abstract class SystemXmlExtensionMethods
    {
        // return true if the item is an XmlNode
        internal abstract bool IsXmlNode(object item);

        // return true if the item is an XmlNamespaceManager
        internal abstract bool IsXmlNamespaceManager(object item);

        // if the item is an XmlNode, get the value corresponding to the given name
        internal abstract bool TryGetValueFromXmlNode(object item, string name, out object value);

        // create a comparer for an Xml collection (if applicable)
        internal abstract IComparer PrepareXmlComparer(IEnumerable collection, SortDescriptionCollection sort, CultureInfo culture);

        // return true if parent is an empty XmlDataCollection.
        internal abstract bool IsEmptyXmlDataCollection(object parent);

        // when item is an XmlNode, get its tag name (using the target DO as context
        // for namespace lookups)
        internal abstract string GetXmlTagName(object item, DependencyObject target);

        // find a node with the given string as its InnerText
        internal abstract object FindXmlNodeWithInnerText(IEnumerable items, object innerText, out int index);

        // get the InnerText of the given node
        internal abstract object GetInnerText(object item);
    }
}

