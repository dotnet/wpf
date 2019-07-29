// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implementation of XmlNamespaceMappingCollection object.
//
// Specs:       XmlDataSource.mht
//              WCP DataSources.mht
//

using System;
using System.Collections; // IEnumerator
using System.Collections.Generic; // ICollection<T>
using System.Xml;
using System.Windows.Markup;
using MS.Utility;

namespace System.Windows.Data
{
    /// <summary>
    /// XmlNamespaceMappingCollection Class
    /// Used to declare namespaces to be used in Xml data binding XPath queries
    /// </summary>
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class XmlNamespaceMappingCollection : XmlNamespaceManager, ICollection<XmlNamespaceMapping>, IAddChildInternal
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public XmlNamespaceMappingCollection() : base(new NameTable())
        {}

#region IAddChild

        /// <summary>
        /// IAddChild implementation
        /// <see cref="IAddChild"/>
        /// </summary>
        /// <param name="value"></param>
        void IAddChild.AddChild(object value)
        {
            AddChild(value);
        }

        /// <summary>
        /// IAddChild implementation
        /// </summary>
        /// <param name="value"></param>
        protected virtual void AddChild(object value)
        {
            XmlNamespaceMapping mapping = value as XmlNamespaceMapping;
            if (mapping == null)
                throw new ArgumentException(SR.Get(SRID.RequiresXmlNamespaceMapping, value.GetType().FullName), "value");

            Add(mapping);
        }

        /// <summary>
        /// IAddChild implementation
        /// </summary>
        /// <param name="text"></param>
        void IAddChild.AddText(string text)
        {
            AddText(text);
        }

        /// <summary>
        /// IAddChild implementation
        /// </summary>
        /// <param name="text"></param>
        protected virtual void AddText(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }

#endregion

#region ICollection<XmlNamespaceMapping>

        /// <summary>
        /// Add XmlNamespaceMapping
        /// </summary>
        /// <exception cref="ArgumentNullException">mapping is null</exception>
        public void Add(XmlNamespaceMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (mapping.Uri == null)
                throw new ArgumentException(SR.Get(SRID.RequiresXmlNamespaceMappingUri), nameof(mapping));

            // BUG 983685: change this to take Uri when AddNamespace is fixed to use Uri instead of String.
            // SECURITY: this workaround (passing the original string) defeats the security benefits of using Uri.
            this.AddNamespace(mapping.Prefix, mapping.Uri.OriginalString);
        }

        /// <summary>
        /// Remove all XmlNamespaceMappings
        /// </summary>
        /// <remarks>
        /// This is potentially an expensive operation.
        /// It may be cheaper to simply create a new XmlNamespaceMappingCollection.
        /// </remarks>
        public void Clear()
        {
            int count = Count;
            XmlNamespaceMapping[] array = new XmlNamespaceMapping[count];
            CopyTo(array, 0);
            for (int i = 0; i < count; ++i)
            {
                Remove(array[i]);
            }
        }

        /// <summary>
        /// Add XmlNamespaceMapping
        /// </summary>
        /// <exception cref="ArgumentNullException">mapping is null</exception>
        public bool Contains(XmlNamespaceMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (mapping.Uri == null)
                throw new ArgumentException(SR.Get(SRID.RequiresXmlNamespaceMappingUri), nameof(mapping));

            return (this.LookupNamespace(mapping.Prefix) == mapping.Uri.OriginalString);
        }

        /// <summary>
        /// Copy XmlNamespaceMappings to array
        /// </summary>
        public void CopyTo(XmlNamespaceMapping[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int i = arrayIndex;
            int maxLength = array.Length;
            foreach (XmlNamespaceMapping mapping in this)
            {
                if (i >= maxLength)
                    throw new ArgumentException(SR.Get(SRID.Collection_CopyTo_NumberOfElementsExceedsArrayLength, nameof(arrayIndex), nameof(array)));
                array[i] = mapping;
                ++ i;
            }
        }

        /// <summary>
        /// Remove XmlNamespaceMapping
        /// </summary>
        /// <returns>
        /// true if the mapping was removed
        /// </returns>
        /// <exception cref="ArgumentNullException">mapping is null</exception>
        public bool Remove(XmlNamespaceMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (mapping.Uri == null)
                throw new ArgumentException(SR.Get(SRID.RequiresXmlNamespaceMappingUri), nameof(mapping));

            if (Contains(mapping))
            {
                this.RemoveNamespace(mapping.Prefix, mapping.Uri.OriginalString);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Count of the number of XmlNamespaceMappings
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (XmlNamespaceMapping mapping in this)
                {
                    ++ count;
                }
                return count;
            }
        }

        /// <summary>
        /// Value to indicate if this collection is read only
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// IEnumerable implementation.
        /// </summary>
        /// <remarks>
        /// This enables the serializer to serialize the contents of the XmlNamespaceMappingCollection.
        /// The default namespaces (xnm, xml, and string.Empty) are not included in this enumeration.
        /// </remarks>
        public override IEnumerator GetEnumerator()
        {
            return ProtectedGetEnumerator();
        }

        /// <summary>
        /// IEnumerable (generic) implementation.
        /// </summary>
        IEnumerator<XmlNamespaceMapping> IEnumerable<XmlNamespaceMapping>.GetEnumerator()
        {
            return ProtectedGetEnumerator();
        }

        /// <summary>
        /// Protected member for use by IEnumerable implementations.
        /// </summary>
        protected IEnumerator<XmlNamespaceMapping> ProtectedGetEnumerator()
        {
            IEnumerator enumerator = BaseEnumerator;

            while (enumerator.MoveNext())
            {
                string prefix = (string) enumerator.Current;
                // ignore the default namespaces added automatically in the XmlNamespaceManager
                if (prefix == "xmlns" || prefix == "xml")
                    continue;
                string ns = this.LookupNamespace(prefix);
                // ignore the empty prefix if the namespace has not been reassigned
                if ((prefix == string.Empty) && (ns == string.Empty))
                    continue;

                Uri uri = new Uri(ns, UriKind.RelativeOrAbsolute);
                XmlNamespaceMapping xnm = new XmlNamespaceMapping(prefix, uri);

                yield return xnm;
            }
        }

        // The iterator above cannot access base.GetEnumerator directly - this
        // causes build warning 1911, and makes MoveNext throw a security
        // exception under partial trust (bug 1785518).  Accessing it indirectly
        // through this property fixes the problem.
        private IEnumerator BaseEnumerator
        {
            get { return base.GetEnumerator(); }
        }

#endregion ICollection<XmlNamespaceMapping>
    }
}

