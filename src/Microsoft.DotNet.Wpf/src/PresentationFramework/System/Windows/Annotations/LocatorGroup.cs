// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     ContentLocatorGroup represents a set of Locators.  This is used to 
//     model an anchor that is made up of several separately identifiable
//     pieces such as a set of Visio objects.
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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MS.Internal;

using MS.Internal.Annotations;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     ContentLocatorGroup represents a set of Locators.  This is used to 
    ///     model an anchor that is made up of several separately identifiable
    ///     pieces such as a set of Visio objects.
    /// </summary>
    [XmlRoot(Namespace = AnnotationXmlConstants.Namespaces.CoreSchemaNamespace, ElementName = AnnotationXmlConstants.Elements.ContentLocatorGroup)]
    public sealed class ContentLocatorGroup : ContentLocatorBase, IXmlSerializable
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an empty ContentLocatorGroup.
        /// </summary>
        public ContentLocatorGroup()
        {
            _locators = new AnnotationObservableCollection<ContentLocator>();
            _locators.CollectionChanged += OnCollectionChanged;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Create a deep clone of this ContentLocatorGroup including clones of all
        ///     the Locators it contains.
        /// </summary>
        /// <returns>a deep clone of this ContentLocatorGroup; never returns null</returns>
        public override Object Clone()
        {
            ContentLocatorGroup clone = new ContentLocatorGroup();

            foreach (ContentLocator loc in _locators)
            {
                clone.Locators.Add((ContentLocator)loc.Clone());
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
        ///     Writes the internal data for this ContentLocatorGroup to the writer.  This method
        ///     is used by an XmlSerializer to serialize a ContentLocatorGroup.  To serialize a
        ///     ContentLocatorGroup to Xml, use an XmlSerializer.
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

            foreach (ContentLocatorBase locator in _locators)
            {
                if (locator != null)
                {
                    AnnotationResource.ListSerializer.Serialize(writer, locator);
                }
            }            
        }

        /// <summary>
        ///     Reads the internal data for this ContentLocatorGroup from the reader.  This method
        ///     is used by an XmlSerializer to deserialize a ContentLocatorGroup.  To deserialize a
        ///     ContentLocatorGroup from Xml, use an XmlSerializer.
        /// </summary>
        /// <param name="reader">the reader to read internal data from</param>
        /// <exception cref="ArgumentNullException">reader is null</exception>
        public void ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            // We expect no attributes on a "ContentLocatorGroup", 
            // so throw using the name of one of the unexpected attributes
            Annotation.CheckForNonNamespaceAttribute(reader, AnnotationXmlConstants.Elements.ContentLocatorGroup);

            if (!reader.IsEmptyElement)
            {
                reader.Read();  // Reads the start of the "ContentLocatorGroup" element

                // Consume everything inside of the ContentLocatorGroup tag.
                while (!(AnnotationXmlConstants.Elements.ContentLocatorGroup == reader.LocalName && XmlNodeType.EndElement == reader.NodeType))
                {
                    // If a child node is a <ContentLocatorBase>, deserialize a ContentLocatorBase
                    if (AnnotationXmlConstants.Elements.ContentLocator == reader.LocalName)
                    {
                        ContentLocator locator = (ContentLocator)AnnotationResource.ListSerializer.Deserialize(reader);                        
                        _locators.Add(locator);
                    }
                    else
                    {
                        // The ContentLocatorGroup contains a child that is not a ContentLocatorBase or 
                        // text.  This isn't valid in the schema so we throw.
                        throw new XmlException(SR.Get(SRID.InvalidXmlContent, AnnotationXmlConstants.Elements.ContentLocatorGroup));
                    }
                }
            }

            reader.Read();   // Reads the end of the "ContentLocatorGroup" element (or whole element if empty)
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
        public Collection<ContentLocator> Locators
        {
            get
            {
                return _locators;
            }
        }

        #endregion Public Properties
        
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///     Merges the ContentLocatorGroup with a ContentLocatorBase.  If other is a ContentLocatorBase, 
        ///     it is added to the end of every ContentLocatorBase in this ContentLocatorGroup.
        ///     If other is a ContentLocatorGroup, then each ContentLocatorBase in it must be added to
        ///     each ContentLocatorBase (or a clone) in this ContentLocatorGroup.  The result is this 
        ///     ContentLocatorGroup will contain n*m ContentLocatorBase.
        ///     In both cases, this ContentLocatorGroup is modified.
        /// </summary>
        /// <param name="other">other ContentLocatorBase to merge with</param>
        /// <returns>this ContentLocatorGroup</returns>
        internal override ContentLocatorBase Merge(ContentLocatorBase other)
        {
            if (other == null)
                return this;

            ContentLocator firstRight = null;
            ContentLocatorGroup locatorGroup = other as ContentLocatorGroup;
            if (locatorGroup != null)
            {
                List<ContentLocatorBase> tempList = new List<ContentLocatorBase>(locatorGroup.Locators.Count * (this.Locators.Count - 1));
                foreach (ContentLocator left in this.Locators)
                {
                    foreach (ContentLocator right in locatorGroup.Locators)
                    {
                        if (firstRight == null)
                        {
                            firstRight = right;
                        }
                        else
                        {
                            ContentLocator clone = (ContentLocator)left.Clone();
                            clone.Append(right);
                            tempList.Add(clone);
                        }
                    }

                    // No need to clone or add here - just use the locator
                    // already in the ContentLocatorGroup
                    left.Append(firstRight);
                    firstRight = null;
                }

                foreach (ContentLocator list in tempList)
                {
                    this.Locators.Add(list);
                }
            }
            else
            {
                ContentLocator otherLoc = other as ContentLocator;
                Invariant.Assert(otherLoc != null, "other should be of type ContentLocator");  // Only other possible type for the ContentLocatorBase

                foreach(ContentLocator loc in this.Locators)
                {
                    loc.Append(otherLoc);
                }
            }
            return this;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Listens for change events from the collection of locators.  Fires a change event
        /// from this LocatorSet when an event is received.
        /// </summary>
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FireLocatorChanged("Locators");
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        
        #region Private Fields

        /// <summary>
        /// Private data structure holding the ContentLocatorBase.
        /// </summary>
        private AnnotationObservableCollection<ContentLocator>   _locators;

        #endregion Private Fields
    }
}
