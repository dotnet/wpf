// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691
//
// Description:
//     ContentLocatorBase contains an ordered set of ContentLocatorParts which each identify
//     a piece of content within a certain context. Resolving one part
//     provides the context to resolve the next part.  Locators are used 
//     to refer to external data in a structured way.
//
//     Spec: Simplifying Store Cache Model.doc
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Annotations.Storage;
using System.Windows.Data;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MS.Internal;
using MS.Internal.Annotations;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Annotations
{
    /// <summary>
    ///     ContentLocatorBase contains an ordered set of ContentLocatorParts which each identify
    ///     a piece of content within a certain context. Resolving one part
    ///     provides the context to resolve the next part.  Locators are used
    ///     to refer to external data in a structured way.
    /// </summary>
    [XmlRoot(Namespace = AnnotationXmlConstants.Namespaces.CoreSchemaNamespace, ElementName = AnnotationXmlConstants.Elements.ContentLocator)]
    public sealed class ContentLocator : ContentLocatorBase, IXmlSerializable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        #region Constructors

        /// <summary>
        ///     Creates an instance of ContentLocator.
        /// </summary>
        public ContentLocator()
        {
            _parts = new AnnotationObservableCollection<ContentLocatorPart>();
            _parts.CollectionChanged += OnCollectionChanged;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        
        #region Public Methods

        /// <summary>
        ///     Determines if this list begins with the ContentLocatorParts that
        ///     make up matchList.  All ContentLocatorParts in matchList must 
        ///     be present and in the same order in this list for 
        ///     true to be returned.
        /// </summary>
        /// <param name="locator">the list to compare with</param>
        /// <returns>
        ///     true if this list begins with the ContentLocatorParts in locator; 
        ///     false otherwise.  If locator is longer than this locator, will 
        ///     return false as well.
        /// </returns>
        /// <exception cref="ArgumentNullException">locator is null</exception>
        public bool StartsWith(ContentLocator locator)
        {
            if (locator == null)
            {
                throw new ArgumentNullException("locator");
            }

            Invariant.Assert(locator.Parts != null, "Locator has null Parts property.");

            // If this locator is shorter than matchList, then this can't contain matchList.            
            #pragma warning suppress 6506 // Invariant.Assert(locator.Parts != null)
            if (this.Parts.Count < locator.Parts.Count)
            {
                return false;
            }

            for (int locatorPartIndex = 0; locatorPartIndex < locator.Parts.Count; locatorPartIndex++)
            {
                ContentLocatorPart left = locator.Parts[locatorPartIndex];
                ContentLocatorPart right = this.Parts[locatorPartIndex];

                // ContentLocator parts can be null so check for that case here
                if (left == null && right != null)
                {
                    return false;
                }

                if (!left.Matches(right))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Creates a deep copy of this list.  A new list with a clone of 
        ///     every ContentLocatorPart in this list, in the same order, is returned.
        ///     Never returns null.
        /// </summary>
        /// <returns>a deep copy of this list</returns>
        public override Object Clone()
        {
            ContentLocator clone = new ContentLocator();
            ContentLocatorPart newPart = null;
            foreach (ContentLocatorPart part in this.Parts)
            {
                if (part != null)
                    newPart = (ContentLocatorPart)part.Clone();
                else
                    newPart = null;

                clone.Parts.Add(newPart);
            }

            return clone;
        }

        #region IXmlSerializable Implementation

        /// <summary>
        ///     Returns the null.  The annotations schema can be found at
        ///     http://schemas.microsoft.com/windows/annotations/2003/11/core.
        /// </summary>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        ///     Writes the internal data for this ContentLocator to the writer.  This 
        ///     method is used by an XmlSerializer to serialize a ContentLocator.  Don't 
        ///     use this method directly, to serialize a ContentLocator to Xml, use an 
        ///     XmlSerializer.
        /// </summary>
        /// <param name="writer">the writer to write internal data to</param>
        /// <exception cref="ArgumentNullException">writer is null</exception>
        public void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            string prefix = writer.LookupPrefix(AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
            if (prefix == null)
            {
                writer.WriteAttributeString(AnnotationXmlConstants.Prefixes.XmlnsPrefix, AnnotationXmlConstants.Prefixes.CoreSchemaPrefix, null, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
            }
            prefix = writer.LookupPrefix(AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
            if (prefix == null)
            {
                writer.WriteAttributeString(AnnotationXmlConstants.Prefixes.XmlnsPrefix, AnnotationXmlConstants.Prefixes.BaseSchemaPrefix, null, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
            }

            // Write each ContentLocatorPart as its own element
            foreach (ContentLocatorPart part in _parts)
            {
                prefix = writer.LookupPrefix(part.PartType.Namespace);
                if (String.IsNullOrEmpty(prefix))
                {
                    prefix = "tmp";                    
                }

                // ContentLocatorParts cannot write themselves out becuase the element
                // name is based on the part's type.  The ContentLocatorPart instance
                // has no way (through normal serialization) to change the element
                // name it writes out at runtime.
                writer.WriteStartElement(prefix, part.PartType.Name, part.PartType.Namespace);

                // Each name/value pair for the ContentLocatorPart becomes an attribute
                foreach (KeyValuePair<string, string> pair in part.NameValuePairs)
                {
                    writer.WriteStartElement(AnnotationXmlConstants.Elements.Item, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
                    writer.WriteAttributeString(AnnotationXmlConstants.Attributes.ItemName, pair.Key);
                    writer.WriteAttributeString(AnnotationXmlConstants.Attributes.ItemValue, pair.Value);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        ///     Reads the internal data for this ContentLocator from the reader.  This 
        ///     method is used by an XmlSerializer to deserialize a ContentLocator.  Don't 
        ///     use this method directly, to deserialize a ContentLocator from Xml, use an 
        ///     XmlSerializer.
        /// </summary>
        /// <param name="reader">the reader to read internal data from</param>
        /// <exception cref="ArgumentNullException">reader is null</exception>
        public void ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            // We expect no attributes on a "ContentLocator", 
            // so throw using the name of one of the unexpected attributes
            Annotation.CheckForNonNamespaceAttribute(reader, AnnotationXmlConstants.Elements.ContentLocator);

            if (!reader.IsEmptyElement)
            {
                reader.Read();  // Reads the start of the "ContentLocator" element

                // ContentLocatorParts cannot write themselves out (see above).  They could read
                // themselves in but instead of having write code in one place and read 
                // code somewhere else - we keep it together in this class.
                while (!(AnnotationXmlConstants.Elements.ContentLocator == reader.LocalName && XmlNodeType.EndElement == reader.NodeType))
                {
                    if (XmlNodeType.Element != reader.NodeType)
                    {
                        throw new XmlException(SR.Get(SRID.InvalidXmlContent, AnnotationXmlConstants.Elements.ContentLocator));
                    }

                    ContentLocatorPart part = new ContentLocatorPart(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI));
                    
                    // Read each of the Item elements within the ContentLocatorPart
                    if (!reader.IsEmptyElement)
                    {
                        // We expect no attributes on a locator part tag, 
                        // so throw using the name of one of the unexpected attributes
                        Annotation.CheckForNonNamespaceAttribute(reader, part.PartType.Name);

                        reader.Read(); // Read the start of the locator part tag

                        while (!(XmlNodeType.EndElement == reader.NodeType && part.PartType.Name == reader.LocalName))
                        {
                            if (AnnotationXmlConstants.Elements.Item == reader.LocalName && reader.NamespaceURI == AnnotationXmlConstants.Namespaces.CoreSchemaNamespace)
                            {
                                string name = null;
                                string value = null;

                                while (reader.MoveToNextAttribute())
                                {
                                    switch (reader.LocalName)
                                    {
                                        case AnnotationXmlConstants.Attributes.ItemName:
                                            name = reader.Value;
                                            break;
                                        case AnnotationXmlConstants.Attributes.ItemValue:
                                            value = reader.Value;
                                            break;
                                        default:
                                            if (!Annotation.IsNamespaceDeclaration(reader))
                                                throw new XmlException(SR.Get(SRID.UnexpectedAttribute, reader.LocalName, AnnotationXmlConstants.Elements.Item));
                                            break;
                                    }
                                }

                                if (name == null)
                                {
                                    throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.ItemName, AnnotationXmlConstants.Elements.Item));
                                }
                                if (value == null)
                                {
                                    throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.ItemValue, AnnotationXmlConstants.Elements.Item));
                                }

                                reader.MoveToContent();

                                part.NameValuePairs.Add(name, value);

                                bool isEmpty = reader.IsEmptyElement;

                                reader.Read();  // Read the beginning of the complete Item tag

                                if (!isEmpty)
                                {
                                    if (!(XmlNodeType.EndElement == reader.NodeType && AnnotationXmlConstants.Elements.Item == reader.LocalName))
                                    {
                                        // Should not contain any content, only attributes
                                        throw new XmlException(SR.Get(SRID.InvalidXmlContent, AnnotationXmlConstants.Elements.Item));
                                    }
                                    else
                                    {
                                        reader.Read(); // Read the end of the Item tag
                                    }
                                }
                            }
                            else
                            {
                                // The locator part contains data other than just "Item" tags
                                throw new XmlException(SR.Get(SRID.InvalidXmlContent, part.PartType.Name));
                            }
                        }
                    }

                    _parts.Add(part);

                    reader.Read();  // Read the ContentLocatorPart element                
                }
            }

            reader.Read(); // Reads the end of the "ContentLocator" element (or whole element if empty)
        }

        #endregion IXmlSerializable Implementation

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public Collection<ContentLocatorPart> Parts
        {
            get
            {
                return _parts;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region InternalMethods

        /// <summary>
        ///     Creates the dot product of this ContentLocator and the list of
        ///     ContentLocatorParts.  The result is n Locators where n is the number of
        ///     ContentLocatorParts passed in.
        ///     One of the resulting Locators is this ContentLocator.  If there are no
        ///     additional ContentLocatorParts a list with just this ContentLocator (unmodified)
        ///     is returned.
        /// </summary>
        /// <param name="additionalLocatorParts">array of ContentLocatorParts</param>
        /// <returns>array of Locators (one for each additional ContentLocatorPart)</returns>
        internal IList<ContentLocatorBase> DotProduct(IList<ContentLocatorPart> additionalLocatorParts)
        {
            List<ContentLocatorBase> results = null;

            // If there aren't any additional locator parts - this is basically a no-op
            if (additionalLocatorParts == null || additionalLocatorParts.Count == 0)
            {
                results = new List<ContentLocatorBase>(1);
                results.Add(this);
            }
            else
            {
                results = new List<ContentLocatorBase>(additionalLocatorParts.Count);

                for (int i = 1; i < additionalLocatorParts.Count; i++)
                {
                    ContentLocator loc = (ContentLocator)this.Clone();
                    loc.Parts.Add(additionalLocatorParts[i]);
                    results.Add(loc);
                }

                this.Parts.Add(additionalLocatorParts[0]);
                results.Insert(0, this);
            }
            return results;
        }

        /// <summary>
        ///     Merges this ContentLocator with a ContentLocatorBase.  If other is a 
        ///     ContentLocatorGroup, each of its Locators are added to clones of 
        ///     this ContentLocatorBase and are added to a new ContentLocatorGroup which is 
        ///     returned.  If other is a ContentLocatorBase, its appended to this 
        ///     ContentLocatorBase and this ContentLocatorBase is returned.
        ///     Both operation modify this ContentLocatorBase.
        /// </summary>
        /// <param name="other">the ContentLocatorBase to merge with</param>
        /// <returns>a ContentLocatorBase containing the final merged product</returns>
        internal override ContentLocatorBase Merge(ContentLocatorBase other)
        {
            if (other == null)
                return this;

            ContentLocatorGroup locatorGroup = other as ContentLocatorGroup;
            if (locatorGroup != null)
            {
                ContentLocatorGroup newGroup = new ContentLocatorGroup();

                ContentLocator temp = null;
                // Create n-1 clones of this LPS and append all but one
                // LPSs in the set to the clones, adding the clones to a
                // new ContentLocatorGroup
                foreach (ContentLocator loc in locatorGroup.Locators)
                {
                    if (temp == null)
                    {
                        temp = loc;
                    }
                    else
                    {
                        ContentLocator clone = (ContentLocator)this.Clone();
                        clone.Append(loc);
                        newGroup.Locators.Add(clone);
                    }
                }

                // Finally, add the remaining LPS in the set to this LPS
                // and add this to the new ContentLocatorGroup
                if (temp != null)
                {
                    this.Append(temp);
                    newGroup.Locators.Add(this);
                }

                if (newGroup.Locators.Count == 0)
                    return this;
                else
                    return newGroup;
            }
            else
            {
                // Safe cast - ContentLocator only has two subclasses
                this.Append((ContentLocator)other);
                return this;
            }
        }

        /// <summary>
        ///     Appends a ContentLocator to this ContentLocator.  The passed in 
        ///     ContentLocator is not modified in anyway.  Its ContentLocatorParts are cloned.
        /// </summary>
        /// <param name="other">locator to append</param>
        internal void Append(ContentLocator other)
        {
            Invariant.Assert(other != null, "Parameter 'other' is null.");

            foreach(ContentLocatorPart part in other.Parts)
            {
                this.Parts.Add((ContentLocatorPart)part.Clone());
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Listens for change events from the list of parts.  Fires a change event
        /// for this ContentLocator when an event is received.
        /// </summary>
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FireLocatorChanged("Parts");
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        ///     List of ContentLocatorParts in this locator.
        /// </summary>
        private AnnotationObservableCollection<ContentLocatorPart> _parts;

        #endregion Private Fields
    }
}
