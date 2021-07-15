// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper methods for code that uses types from System.Xml.Linq.
//

using System;
using System.ComponentModel;

namespace MS.Internal
{
    internal static class SystemXmlLinqHelper
    {
        // return true if the item is an XElement
        internal static bool IsXElement(object item)
        {
            SystemXmlLinqExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXmlLinq();
            return (extensions != null) ? extensions.IsXElement(item) : false;
        }

        // return a string of the form "{http://my.namespace}TagName"
        internal static string GetXElementTagName(object item)
        {
            SystemXmlLinqExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXmlLinq();
            return (extensions != null) ? extensions.GetXElementTagName(item) : null;
        }

        // XLinq exposes two synthetic properties - Elements and Descendants -
        // on XElement that return IEnumerable<XElement>.  We handle these specially
        // to work around problems involving identity and change notifications
        internal static bool IsXLinqCollectionProperty(PropertyDescriptor pd)
        {
            SystemXmlLinqExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXmlLinq();
            return (extensions != null) ? extensions.IsXLinqCollectionProperty(pd) : false;
        }

        // XLinq exposes several properties on XElement that create new objects
        // every time the getter is called.
        internal static bool IsXLinqNonIdempotentProperty(PropertyDescriptor pd)
        {
            SystemXmlLinqExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemXmlLinq();
            return (extensions != null) ? extensions.IsXLinqNonIdempotentProperty(pd) : false;
        }
    }
}
