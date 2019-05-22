// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implementation of XmlNamespaceMapping object.
//
// Specs:       XmlDataSource.mht
//              WCP DataSources.mht
//

using System;
using System.ComponentModel;        // ISupportInitialize

namespace System.Windows.Data
{
    /// <summary>
    /// XmlNamespaceMapping Class
    /// used for declaring Xml Namespace Mappings
    /// </summary>
    public class XmlNamespaceMapping : ISupportInitialize
    {
        /// <summary>
        /// Constructor for XmlNamespaceMapping
        /// </summary>
        public XmlNamespaceMapping()
        {
        }

        /// <summary>
        /// Constructor for XmlNamespaceMapping
        /// </summary>
        public XmlNamespaceMapping(string prefix, Uri uri)
        {
            _prefix = prefix;
            _uri = uri;
        }

        /// <summary>
        /// The prefix to be used for this Namespace
        /// </summary>
        public string Prefix
        {
            get { return _prefix; }
            set
            {
                if (!_initializing)
                    throw new InvalidOperationException(SR.Get(SRID.PropertyIsInitializeOnly, "Prefix", this.GetType().Name));
                if (_prefix != null && _prefix != value)
                    throw new InvalidOperationException(SR.Get(SRID.PropertyIsImmutable, "Prefix", this.GetType().Name));

                _prefix = value;
            }
        }

        /// <summary>
        /// The Uri to be used for this Namespace,
        /// can be declared as an attribute or as the
        /// TextContent of the XmlNamespaceMapping markup tag
        /// </summary>
        public Uri Uri
        {
            get { return _uri; }
            set
            {
                if (!_initializing)
                    throw new InvalidOperationException(SR.Get(SRID.PropertyIsInitializeOnly, "Uri", this.GetType().Name));
                if (_uri != null && _uri != value)
                    throw new InvalidOperationException(SR.Get(SRID.PropertyIsImmutable, "Uri", this.GetType().Name));

                _uri = value;
            }
        }

        /// <summary>
        /// Equality comparison by value
        /// </summary>
        public override bool Equals(object obj)
        {
            return (this == (obj as XmlNamespaceMapping));  // call the == operator override
        }

        /// <summary>
        /// Equality comparison by value
        /// </summary>
        public static bool operator == (XmlNamespaceMapping mappingA, XmlNamespaceMapping mappingB)
        {
            // cannot just compare with (mappingX == null), it'll cause recursion and stack overflow!
            if (object.ReferenceEquals(mappingA, null))
                return object.ReferenceEquals(mappingB, null);
            if (object.ReferenceEquals(mappingB, null))
                return false;

#pragma warning disable 1634, 1691

            // presharp false positive for null-checking on mappings
            #pragma warning suppress 56506
            return ((mappingA.Prefix == mappingB.Prefix) && (mappingA.Uri == mappingB.Uri)) ;

#pragma warning restore 1634, 1691
        }

        /// <summary>
        /// Inequality comparison by value
        /// </summary>
        public static bool operator != (XmlNamespaceMapping mappingA, XmlNamespaceMapping mappingB)
        {
            return !(mappingA == mappingB);
        }

        /// <summary>
        /// Hash function for this type
        /// </summary>
        public override int GetHashCode()
        {
            // note that the hash code can change, but only during intialization
            // (_prefix and _uri can only be changed once, from null to
            // non-null, and only during [Begin/End]Init). Technically this is
            // still a violation of the "constant during lifetime" rule, however
            // in practice this is acceptable.   It is very unlikely that someone
            // will put an XmlNamespaceMapping into a hashtable before it is initialized.

            int hash = 0;
            if (_prefix != null)
                hash = _prefix.GetHashCode();
            if (_uri != null)
                return unchecked(hash + _uri.GetHashCode());
            else
                return hash;
        }

#region ISupportInitialize

        /// <summary>Begin Initialization</summary>
        void ISupportInitialize.BeginInit()
        {
            _initializing = true;
        }

        /// <summary>End Initialization, verify that internal state is consistent</summary>
        void ISupportInitialize.EndInit()
        {
            if (_prefix == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.PropertyMustHaveValue, "Prefix", this.GetType().Name));
            }
            if (_uri == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.PropertyMustHaveValue, "Uri", this.GetType().Name));
            }

            _initializing = false;
        }

#endregion ISupportInitialize

        private string _prefix;
        private Uri _uri;
        private bool _initializing;
    }
}
