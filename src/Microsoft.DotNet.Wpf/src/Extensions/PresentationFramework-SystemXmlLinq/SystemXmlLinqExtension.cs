// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Helper methods for code that uses types from System.Xml.Linq.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace MS.Internal
{
    //FxCop can't tell that this class is instantiated via reflection, so suppress the FxCop warning.
    [SuppressMessage("Microsoft.Performance","CA1812:AvoidUninstantiatedInternalClasses")]
    internal class SystemXmlLinqExtension : SystemXmlLinqExtensionMethods
    {
        static SystemXmlLinqExtension()
        {
            // pre-load the types of various special property descriptors
            XElement xelement = new XElement("Dummy");
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(xelement);

            s_XElementElementsPropertyDescriptorType = pdc["Elements"].GetType();
            s_XElementDescendantsPropertyDescriptorType = pdc["Descendants"].GetType();
            s_XElementAttributePropertyDescriptorType = pdc["Attribute"].GetType();
            s_XElementElementPropertyDescriptorType = pdc["Element"].GetType();
        }

        // return true if the item is an XElement
        internal override bool IsXElement(object item)
        {
            return item is XElement;
        }

        // return a string of the form "{http://my.namespace}TagName"
        internal override string GetXElementTagName(object item)
        {
            XName name = ((XElement)item).Name;
            return (name != null) ? name.ToString() : null;
        }

        // XLinq exposes two synthetic properties - Elements and Descendants -
        // on XElement that return IEnumerable<XElement>.  We handle these specially
        // to work around problems involving identity and change notifications
        internal override bool IsXLinqCollectionProperty(PropertyDescriptor pd)
        {
            Type pdType = pd.GetType();
            return (pdType == s_XElementElementsPropertyDescriptorType) ||
                (pdType == s_XElementDescendantsPropertyDescriptorType);
        }

        // XLinq exposes several properties on XElement that create new objects
        // every time the getter is called.  We have to live with this, since
        // the property descriptors carry state that depends on components later
        // in the path (e.g. when path=Attribute[FirstName], the value returned by
        // the Attribute PD can only be used to look up "FirstName").  But we need
        // to be aware of it in certain circumstances - e.g. when checking for
        // event leapfrogging.
        internal override bool IsXLinqNonIdempotentProperty(PropertyDescriptor pd)
        {
            Type pdType = pd.GetType();
            return (pdType == s_XElementAttributePropertyDescriptorType) ||
                (pdType == s_XElementElementPropertyDescriptorType);
        }

        private static Type s_XElementElementsPropertyDescriptorType;
        private static Type s_XElementDescendantsPropertyDescriptorType;
        private static Type s_XElementAttributePropertyDescriptorType;
        private static Type s_XElementElementPropertyDescriptorType;
    }
}
