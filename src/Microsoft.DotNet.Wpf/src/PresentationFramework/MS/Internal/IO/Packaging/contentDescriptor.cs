// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Types of keys and data in the element table that is used
//              by XamlFilter and initialized by the generated function
//              InitElementDictionary.
//

using System;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Representation of a fully-qualified XML name for a XAML element.
    /// </summary>
    internal class ElementTableKey
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal ElementTableKey(string xmlNamespace, string baseName)
        {
            if (xmlNamespace == null)
            {
                throw new ArgumentNullException("xmlNamespace");
            }
            if (baseName == null)
            {
                throw new ArgumentNullException("baseName");
            }
            _xmlNamespace = xmlNamespace;
            _baseName = baseName;
        }

        /// <summary>
        /// Equality test.
        /// </summary>
        public override bool Equals( object other )
        {
            if (other == null)
                return false;   // Standard behavior.

            if (other.GetType() != GetType())
                return false;

            // Note that because of the GetType() checking above, the casting must be valid.
            ElementTableKey otherElement = (ElementTableKey)other;

            return (   String.CompareOrdinal(BaseName,otherElement.BaseName) == 0
                && String.CompareOrdinal(XmlNamespace,otherElement.XmlNamespace) == 0 );
        }
            
        /// <summary>
        /// Hash on all name components.
        /// </summary>
        public override int GetHashCode()
        {
            return XmlNamespace.GetHashCode() ^ BaseName.GetHashCode();
        }

        /// <summary>
        /// XML namespace.
        /// </summary>
        internal string     XmlNamespace
        {
            get
            {
                return _xmlNamespace; 
            }
        }

        /// <summary>
        /// Local name.
        /// </summary>
        internal string     BaseName
        {
            get
            {
                return _baseName; 
            }
        }

        private string      _baseName;
        private string      _xmlNamespace;

        public static readonly string  XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        public static readonly string  FixedMarkupNamespace = "http://schemas.microsoft.com/xps/2005/06";
    }
                
    ///<summary>Content-location information for an element.</summary>
    internal class ContentDescriptor
    {
        /// <summary>
        /// The name of the key to the value of _xamlElementContentDescriptorDictionary in the resource file.
        /// </summary>
        internal const string ResourceKeyName = "Dictionary";

        /// <summary>
        /// The name of the resource containing the definition of XamlFilter._xamlElementContentDescriptorDictionary.
        /// </summary>
        internal const string ResourceName = "ElementTable";

        /// <summary>
        /// Standard constructor.
        /// </summary>
        internal ContentDescriptor(
            bool hasIndexableContent, 
            bool isInline,
            string contentProp, 
            string titleProp)
        {
            HasIndexableContent = hasIndexableContent;
            IsInline = isInline;
            ContentProp = contentProp;
            TitleProp = titleProp;
        }

        /// <summary>
        /// Constructor with default settings for all but HasIndexableContent.
        /// </summary>
        /// <remarks>
        /// Currently, this constructor is always passed false, since in this case the other values are "don't care".
        /// It would make sense to use it with HasIndexableContent=true, however.
        /// </remarks>
        internal ContentDescriptor(
            bool hasIndexableContent) 
        {
            HasIndexableContent = hasIndexableContent;
            IsInline = false;
            ContentProp = null;
            TitleProp   = null;
        }

        /// <summary>
        /// Whether indexable at all.
        /// </summary>
        /// <remarks>
        /// ContentDescriptor properties are read-write because at table creation time these properties
        /// are discovered and stored incrementally.
        /// </remarks>
        internal bool       HasIndexableContent
        {
            get
            {
                return _hasIndexableContent;
            }
            set
            {
                _hasIndexableContent = value;
            }
        }

        /// <summary>
        /// Block or inline.
        /// </summary>
        internal bool IsInline
        {
            get
            {
                return _isInline;
            }
            set
            {
                _isInline = value;
            }
        }

        /// <summary>
        /// Attribute in which to find content or null.
        /// </summary>
        internal string     ContentProp
        {
            get
            {
                return _contentProp;
            }
            set
            {
                _contentProp = value;
            }
        }

        /// <summary>
        /// Attribute in which to find a title rather than the real content.
        /// </summary>
        internal string     TitleProp
        {
            get
            {
                return _titleProp;
            }
            set
            {
                _titleProp = value;
            }
        }

        private bool        _hasIndexableContent;
        private bool        _isInline;
        private string      _contentProp;
        private string      _titleProp;
    }
}   // namespace MS.Internal.IO.Packaging
