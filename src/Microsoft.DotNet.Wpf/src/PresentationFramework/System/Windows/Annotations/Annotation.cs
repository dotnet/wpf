// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     Annotation class is the top-level Object Model class for the
//     annotation framework.  It represents a single annotation with
//     all of its anchoring and content data.
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
using System.IO;
using System.Windows.Annotations.Storage;
using System.Windows.Data;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MS.Internal;
using MS.Internal.Annotations;
using MS.Utility;


namespace System.Windows.Annotations
{
    /// <summary>
    ///     Actions that can be taken on subparts of an
    ///     Annotation - authors, anchors, and cargos.
    /// </summary>
    public enum AnnotationAction
    {
        /// <summary>
        ///     The subpart was added to the Annotation.
        /// </summary>
        Added,
        /// <summary>
        ///     The subpart was removed from the Annotation.
        /// </summary>
        Removed,
        /// <summary>
        ///     The subpart already is part of the Annotation and was modified.
        /// </summary>
        Modified
    }

    /// <summary>
    ///     Annotation class is the top-level Object Model class for the
    ///     annotation framework.  It represents a single annotation with
    ///     all of its anchoring and content data.
    /// </summary>
    [XmlRoot(Namespace = AnnotationXmlConstants.Namespaces.CoreSchemaNamespace, ElementName = AnnotationXmlConstants.Elements.Annotation)]
    public sealed class Annotation : IXmlSerializable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of Annotation for use by XML serialization.
        ///     This constructor should not be used directly.  It does not
        ///     initialize the instance.
        /// </summary>
        public Annotation()
        {
            // Should only be used from XML serialization.  This instance will
            // not be a valid Annotation if used outside of serialization.

            _id = Guid.Empty;
            _created = DateTime.MinValue;
            _modified = DateTime.MinValue;

            Init();
        }

        /// <summary>
        ///     Creates an instance of Annotation with the specified type name.
        /// </summary>
        /// <param name="annotationType">fully qualified type name of the new Annotation</param>
        /// <exception cref="ArgumentNullException">annotationType is null</exception>
        /// <exception cref="ArgumentException">annotationType's Name or Namespace is null or empty string</exception>
        public Annotation(XmlQualifiedName annotationType)
        {
            if (annotationType == null)
            {
                throw new ArgumentNullException("annotationType");
            }
            if (String.IsNullOrEmpty(annotationType.Name))
            {
                throw new ArgumentException(SR.Get(SRID.TypeNameMustBeSpecified), "annotationType.Name");// needs better message
            }
            if (String.IsNullOrEmpty(annotationType.Namespace))
            {
                throw new ArgumentException(SR.Get(SRID.TypeNameMustBeSpecified), "annotationType.Namespace");//needs better message
            }

            _id = Guid.NewGuid();
            _typeName = annotationType;

            // For an new instance of Annotation, _created should be == with _modified
            _created = DateTime.Now;
            _modified = _created;

            Init();
        }

        /// <summary>
        ///     Creates an instance of Annotation with the values provided.  This
        ///     constructor is for use by stores using their own serialization method.
        ///     It provides a way to create an annotation with all the necessary
        ///     read-only data.
        /// </summary>
        /// <param name="annotationType">the fully qualified type name of the new Annotation</param>
        /// <param name="id">the id of the new Annotation</param>
        /// <param name="creationTime">the time the annotation was first created</param>
        /// <param name="lastModificationTime">the time the annotation was last modified</param>
        /// <exception cref="ArgumentNullException">annotationType is null</exception>
        /// <exception cref="ArgumentException">lastModificationTime is earlier than creationTime</exception>
        /// <exception cref="ArgumentException">annotationType's Name or Namespace is null or empty string</exception>
        /// <exception cref="ArgumentException">id is equal to Guid.Empty</exception>
        public Annotation(XmlQualifiedName annotationType, Guid id, DateTime creationTime, DateTime lastModificationTime)
        {
            if (annotationType == null)
            {
                throw new ArgumentNullException("annotationType");
            }
            if (String.IsNullOrEmpty(annotationType.Name))
            {
                throw new ArgumentException(SR.Get(SRID.TypeNameMustBeSpecified), "annotationType.Name");//needs better message
            }
            if (String.IsNullOrEmpty(annotationType.Namespace))
            {
                throw new ArgumentException(SR.Get(SRID.TypeNameMustBeSpecified), "annotationType.Namespace");//needs better message
            }

            if (id.Equals(Guid.Empty))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidGuid), "id");
            }
            if (lastModificationTime.CompareTo(creationTime) < 0)
            {
                throw new ArgumentException(SR.Get(SRID.ModificationEarlierThanCreation), "lastModificationTime");
            }
            _id = id;
            _typeName = annotationType;

            _created = creationTime;
            _modified = lastModificationTime;

            Init();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #region IXmlSerializable Implementation

        /// <summary>
        ///     Returns null.  The annotations schema is available at
        ///     http://schemas.microsoft.com/windows/annotations/2003/11/core.
        /// </summary>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        ///     Serializes this Annotation to XML with the passed in XmlWriter.
        /// </summary>
        /// <param name="writer">the writer to serialize the Annotation to</param>
        /// <exception cref="ArgumentNullException">writer is null</exception>
        public void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.SerializeAnnotationBegin);
            try
            {
                if (String.IsNullOrEmpty(writer.LookupPrefix(AnnotationXmlConstants.Namespaces.CoreSchemaNamespace)))
                {
                    writer.WriteAttributeString(AnnotationXmlConstants.Prefixes.XmlnsPrefix, AnnotationXmlConstants.Prefixes.CoreSchemaPrefix, null, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
                }
                if (String.IsNullOrEmpty(writer.LookupPrefix(AnnotationXmlConstants.Namespaces.BaseSchemaNamespace)))
                {
                    writer.WriteAttributeString(AnnotationXmlConstants.Prefixes.XmlnsPrefix, AnnotationXmlConstants.Prefixes.BaseSchemaPrefix, null, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);
                }

                if (_typeName == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotSerializeInvalidInstance));
                }

                // XmlConvert.ToString is [Obsolete]
#pragma warning disable 0618

                writer.WriteAttributeString(AnnotationXmlConstants.Attributes.Id, XmlConvert.ToString(_id));
                writer.WriteAttributeString(AnnotationXmlConstants.Attributes.CreationTime, XmlConvert.ToString(_created));
                writer.WriteAttributeString(AnnotationXmlConstants.Attributes.LastModificationTime, XmlConvert.ToString(_modified));

#pragma warning restore 0618

                writer.WriteStartAttribute(AnnotationXmlConstants.Attributes.TypeName);
                writer.WriteQualifiedName(_typeName.Name, _typeName.Namespace);
                writer.WriteEndAttribute();

                if (_authors != null && _authors.Count > 0)
                {
                    writer.WriteStartElement(AnnotationXmlConstants.Elements.AuthorCollection, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
                    foreach (string author in _authors)
                    {
                        if (author != null)
                        {
                            writer.WriteElementString(AnnotationXmlConstants.Prefixes.BaseSchemaPrefix, AnnotationXmlConstants.Elements.StringAuthor, AnnotationXmlConstants.Namespaces.BaseSchemaNamespace, author);
                        }
                    }
                    writer.WriteEndElement();
                }

                if (_anchors != null && _anchors.Count > 0)
                {
                    writer.WriteStartElement(AnnotationXmlConstants.Elements.AnchorCollection, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
                    foreach (AnnotationResource anchor in _anchors)
                    {
                        if (anchor != null)
                        {
                            ResourceSerializer.Serialize(writer, anchor);
                        }
                    }
                    writer.WriteEndElement();
                }

                if (_cargos != null && _cargos.Count > 0)
                {
                    writer.WriteStartElement(AnnotationXmlConstants.Elements.CargoCollection, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
                    foreach (AnnotationResource cargo in _cargos)
                    {
                        if (cargo != null)
                        {
                            ResourceSerializer.Serialize(writer, cargo);
                        }
                    }
                    writer.WriteEndElement();
                }
            }
            finally
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.SerializeAnnotationEnd);
            }
        }

        /// <summary>
        ///     Deserializes an Annotation from the XmlReader passed in.
        /// </summary>
        /// <param name="reader">reader to deserialize from</param>
        /// <exception cref="ArgumentNullException">reader is null</exception>
        public void ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeserializeAnnotationBegin);

            XmlDocument doc = null;

            try
            {
                doc = new XmlDocument();

                ReadAttributes(reader);

                if (!reader.IsEmptyElement)
                {
                    reader.Read(); // Read the remainder of the "Annotation" start tag

                    while (!(XmlNodeType.EndElement == reader.NodeType && AnnotationXmlConstants.Elements.Annotation == reader.LocalName))
                    {
                        if (AnnotationXmlConstants.Elements.AnchorCollection == reader.LocalName)
                        {
                            CheckForNonNamespaceAttribute(reader, AnnotationXmlConstants.Elements.AnchorCollection);

                            if (!reader.IsEmptyElement)
                            {
                                reader.Read();    // Reads the "Anchors" start tag
                                while (!(AnnotationXmlConstants.Elements.AnchorCollection == reader.LocalName && XmlNodeType.EndElement == reader.NodeType))
                                {
                                    AnnotationResource anchor = (AnnotationResource)ResourceSerializer.Deserialize(reader);
                                    _anchors.Add(anchor);
                                }
                            }
                            reader.Read();    // Reads the "Anchors" end tag (or whole tag if it was empty)
                        }
                        else if (AnnotationXmlConstants.Elements.CargoCollection == reader.LocalName)
                        {
                            CheckForNonNamespaceAttribute(reader, AnnotationXmlConstants.Elements.CargoCollection);

                            if (!reader.IsEmptyElement)
                            {
                                reader.Read();    // Reads the "Cargos" start tag
                                while (!(AnnotationXmlConstants.Elements.CargoCollection == reader.LocalName && XmlNodeType.EndElement == reader.NodeType))
                                {
                                    AnnotationResource cargo = (AnnotationResource)ResourceSerializer.Deserialize(reader);
                                    _cargos.Add(cargo);
                                }
                            }
                            reader.Read();    // Reads the "Cargos" end tag (or whole tag if it was empty)
                        }
                        else if (AnnotationXmlConstants.Elements.AuthorCollection == reader.LocalName)
                        {
                            CheckForNonNamespaceAttribute(reader, AnnotationXmlConstants.Elements.AuthorCollection);

                            if (!reader.IsEmptyElement)
                            {
                                reader.Read();    // Reads the "Authors" start tag
                                while (!(AnnotationXmlConstants.Elements.AuthorCollection == reader.LocalName && XmlNodeType.EndElement == reader.NodeType))
                                {
                                    if (!(AnnotationXmlConstants.Elements.StringAuthor == reader.LocalName && XmlNodeType.Element == reader.NodeType))
                                    {
                                        throw new XmlException(SR.Get(SRID.InvalidXmlContent, AnnotationXmlConstants.Elements.Annotation));
                                    }

                                    XmlNode node = doc.ReadNode(reader);  // Reads the entire "StringAuthor" tag
                                    if (!reader.IsEmptyElement)
                                    {
                                        _authors.Add(node.InnerText);
                                    }
                                }
                            }
                            reader.Read();    // Reads the "Authors" end tag (or whole tag if it was empty)
                        }
                        else
                        {
                            // The annotation must contain some invalid content which is not part of the schema.
                            throw new XmlException(SR.Get(SRID.InvalidXmlContent, AnnotationXmlConstants.Elements.Annotation));
                        }
                    }
                }

                reader.Read(); // Read the end of the "Annotation" tag (or the whole tag if its empty)
            }
            finally
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.DeserializeAnnotationEnd);
            }
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

        #region Public Events

        /// <summary>
        ///     Event fired when an author is added, removed or modified in anyway.
        /// </summary>
        public event AnnotationAuthorChangedEventHandler AuthorChanged;

        /// <summary>
        ///     Event fired when an anchor is added, removed or modified in anyway.
        ///     This includes modifications to an anchor's sub-parts such as
        ///     changing a value on a ContentLocatorPart which is contained by a ContentLocatorBase
        ///     which is contained by an anchor which is contained by this
        ///     Annotation.
        /// </summary>
        public event AnnotationResourceChangedEventHandler AnchorChanged;

        /// <summary>
        ///     Event fired when an cargo is added, removed or modified in anyway.
        ///     This includes modifications to a cargo's sub-parts such as
        ///     changing an attribute on an XmlNode contained by a content which
        ///     is contained by a cargo which is contained by this Annotation.
        /// </summary>
        public event AnnotationResourceChangedEventHandler CargoChanged;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     An Annotation is given a unique Guid when it is first instantiated.
        /// </summary>
        /// <returns>the unique id of this Annotation; this property will return
        /// Guid.Empty if the Annotation was instantied with the default constructor -
        /// which should not be used directly</returns>
        public Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        ///     The type of this Annotation.
        /// </summary>
        /// <returns>the type of this Annotation; this property will only return
        /// null if the Annotation was instantiated with the default constructor - which
        /// should not be used directly</returns>
        public XmlQualifiedName AnnotationType
        {
            get { return _typeName; }
        }

        /// <summary>
        ///     The time of creation for this Annotation.  This is set when
        ///     the Annotation is first instantiated.
        /// </summary>
        /// <returns>the creation time of this Annotation; this property will return
        /// DateTime.MinValue if the Annotation was instantiated with the default constructor -
        /// which should not be used directly</returns>
        public DateTime CreationTime
        {
            get { return _created; }
        }

        /// <summary>
        ///     The time of the Annotation was last modified.  This is set after any
        ///     change to Annotation before any notifications are fired.
        /// </summary>
        /// <returns>the last modification time of this Annotation; this property will
        /// return DateTime.MinValue if the Annotation was instantiated with the default
        /// constructor - which should not be used directly</returns>
        public DateTime LastModificationTime
        {
            get { return _modified; }
        }

        /// <summary>
        ///     The collection of zero or more authors for this Annotation.
        /// </summary>
        /// <returns>collection of authors for this Annotation; never returns null</returns>
        public Collection<String> Authors
        {
            get
            {
                return _authors;
            }
        }

        /// <summary>
        ///     The collection of zero or more Resources that represent the anchors
        ///     of this Annotation.  These could be references to content,
        ///     actually include the content itself, or both (as in the case of
        ///     a snippet being the anchor of a comment).
        /// </summary>
        /// <returns>collection of Resources; never returns null</returns>
        public Collection<AnnotationResource> Anchors
        {
            get
            {
                return _anchors;
            }
        }

        /// <summary>
        ///     The collection of zero or more Resources that represent the cargos
        ///     of this Annotation.  These could be references to content,
        ///     actually include the content itself, or both (as in the case of
        ///     a snippet from a web-page being the cargo).
        /// </summary>
        /// <returns>collection of Resources; never returns null</returns>
        public Collection<AnnotationResource> Cargos
        {
            get
            {
                return _cargos;
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
        /// Returns true if the reader is positioned on an attribute that
        /// is a namespace attribute - for instance xmlns="xx", xmlns:bac="http:bac",
        /// or xml:lang="en-us".
        /// </summary>
        internal static bool IsNamespaceDeclaration(XmlReader reader)
        {
            Invariant.Assert(reader != null);

            // The reader is on a namespace declaration if:
            //   - the current node is an attribute AND either
            //     - the attribute has no prefix and local name is 'xmlns'
            //     - the attribute's prefix is 'xmlns' or 'xml'
            if (reader.NodeType == XmlNodeType.Attribute)
            {
                if (reader.Prefix.Length == 0)
                {
                    if (reader.LocalName == AnnotationXmlConstants.Prefixes.XmlnsPrefix)
                        return true;
                }
                else
                {
                    if (reader.Prefix == AnnotationXmlConstants.Prefixes.XmlnsPrefix ||
                        reader.Prefix == AnnotationXmlConstants.Prefixes.XmlPrefix)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks all attributes for the current node.  If any attribute isn't a
        /// namespace attribute an exception is thrown.
        /// </summary>
        internal static void CheckForNonNamespaceAttribute(XmlReader reader, string elementName)
        {
            Invariant.Assert(reader != null, "No reader supplied.");
            Invariant.Assert(elementName != null, "No element name supplied.");

            while (reader.MoveToNextAttribute())
            {
                // If the attribute is a namespace declaration we should ignore it
                if (Annotation.IsNamespaceDeclaration(reader))
                {
                    continue;
                }

                throw new XmlException(SR.Get(SRID.UnexpectedAttribute, reader.LocalName, elementName));
            }

            // We need to move the reader back to the original element the
            // attributes are on for the next reader operation.  Has no effect
            // if no attributes were looked at
            reader.MoveToContent();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        ///     Returns serializer for Resource objects.  Lazily creates
        ///     the serializer for cases where its not needed.
        /// </summary>
        private static Serializer ResourceSerializer
        {
            get
            {
                if (_ResourceSerializer == null)
                {
                    _ResourceSerializer = new Serializer(typeof(AnnotationResource));
                }
                return _ResourceSerializer;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void ReadAttributes(XmlReader reader)
        {
            Invariant.Assert(reader != null, "No reader passed in.");

            // Read all the attributes
            while (reader.MoveToNextAttribute())
            {
                string value = reader.Value;

                // Skip null and empty values - they will be treated the
                // same as if they weren't specified at all
                if (String.IsNullOrEmpty(value))
                    continue;

                switch (reader.LocalName)
                {
                    case AnnotationXmlConstants.Attributes.Id:
                        _id = XmlConvert.ToGuid(value);
                        break;

                    // XmlConvert.ToDateTime is [Obsolete]
                    #pragma warning disable 0618

                    case AnnotationXmlConstants.Attributes.CreationTime:
                        _created = XmlConvert.ToDateTime(value);
                        break;

                    case AnnotationXmlConstants.Attributes.LastModificationTime:
                        _modified = XmlConvert.ToDateTime(value);
                        break;

                    #pragma warning restore 0618

                    case AnnotationXmlConstants.Attributes.TypeName:
                        string[] typeName = value.Split(_Colon);
                        if (typeName.Length == 1)
                        {
                            typeName[0] = typeName[0].Trim();
                            if (String.IsNullOrEmpty(typeName[0]))
                            {
                                // Just a string of whitespace (empty string doesn't get processed)
                                throw new FormatException(SR.Get(SRID.InvalidAttributeValue, AnnotationXmlConstants.Attributes.TypeName));
                            }
                            _typeName = new XmlQualifiedName(typeName[0]);
                        }
                        else if (typeName.Length == 2)
                        {
                            typeName[0] = typeName[0].Trim();
                            typeName[1] = typeName[1].Trim();
                            if (String.IsNullOrEmpty(typeName[0]) || String.IsNullOrEmpty(typeName[1]))
                            {
                                // One colon, prefix or suffix is empty string or whitespace
                                throw new FormatException(SR.Get(SRID.InvalidAttributeValue, AnnotationXmlConstants.Attributes.TypeName));
                            }
                            _typeName = new XmlQualifiedName(typeName[1], reader.LookupNamespace(typeName[0]));
                        }
                        else
                        {
                            // More than one colon
                            throw new FormatException(SR.Get(SRID.InvalidAttributeValue, AnnotationXmlConstants.Attributes.TypeName));
                        }
                        break;

                    default:
                        if (!Annotation.IsNamespaceDeclaration(reader))
                           throw new XmlException(SR.Get(SRID.UnexpectedAttribute, reader.LocalName, AnnotationXmlConstants.Elements.Annotation));
                       break;
                }
            }

            // Test to see if any required attribute was missing
            if (_id.Equals(Guid.Empty))
            {
                throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.Id, AnnotationXmlConstants.Elements.Annotation));
            }
            if (_created.Equals(DateTime.MinValue))
            {
                throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.CreationTime, AnnotationXmlConstants.Elements.Annotation));
            }
            if (_modified.Equals(DateTime.MinValue))
            {
                throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.LastModificationTime, AnnotationXmlConstants.Elements.Annotation));
            }
            if (_typeName == null)
            {
                throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.TypeName, AnnotationXmlConstants.Elements.Annotation));
            }

            // Move back to the parent "Annotation" element
            reader.MoveToContent();
        }

        private void OnCargoChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FireResourceEvent((AnnotationResource)sender, AnnotationAction.Modified, CargoChanged);
        }

        private void OnCargosChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            AnnotationAction action = AnnotationAction.Added;
            IList changedItems = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    action = AnnotationAction.Added;
                    changedItems = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    action = AnnotationAction.Removed;
                    changedItems = e.OldItems;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // For Replace we need to fire removes and adds.  As in other
                    // event firing code - if a listener for one event throws the
                    // rest of the events won't be fired.
                    foreach (AnnotationResource cargo in e.OldItems)
                    {
                        FireResourceEvent(cargo, AnnotationAction.Removed, CargoChanged);
                    }
                    changedItems = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Move:
                    // ignore - this only happens on sort
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // ignore - this only happens on sort
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action));
            }

            if (changedItems != null)
            {
                foreach (AnnotationResource cargo in changedItems)
                {
                    FireResourceEvent(cargo, action, CargoChanged);
                }
            }
        }

        private void OnAnchorChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FireResourceEvent((AnnotationResource)sender, AnnotationAction.Modified, AnchorChanged);
        }

        private void OnAnchorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            AnnotationAction action = AnnotationAction.Added;
            IList changedItems = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    action = AnnotationAction.Added;
                    changedItems = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    action = AnnotationAction.Removed;
                    changedItems = e.OldItems;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // For Replace we need to fire removes and adds.  As in other
                    // event firing code - if a listener for one event throws the
                    // rest of the events won't be fired.
                    foreach (AnnotationResource anchor in e.OldItems)
                    {
                        FireResourceEvent(anchor, AnnotationAction.Removed, AnchorChanged);
                    }
                    changedItems = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Move:
                    // ignore - this only happens on sort
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // ignore - this only happens on sort
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action));
            }

            if (changedItems != null)
            {
                foreach (AnnotationResource anchor in changedItems)
                {
                    FireResourceEvent(anchor, action, AnchorChanged);
                }
            }
        }

        private void OnAuthorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            AnnotationAction action = AnnotationAction.Added;
            IList changedItems = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    action = AnnotationAction.Added;
                    changedItems = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    action = AnnotationAction.Removed;
                    changedItems = e.OldItems;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // For Replace we need to fire removes and adds.  As in other
                    // event firing code - if a listener for one event throws the
                    // rest of the events won't be fired.
                    foreach (string author in e.OldItems)
                    {
                        FireAuthorEvent(author, AnnotationAction.Removed);
                    }
                    changedItems = e.NewItems;
                    break;

                case NotifyCollectionChangedAction.Move:
                    // ignore - this only happens on sort
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // ignore - this only happens on sort
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action));
            }

            if (changedItems != null)
            {
                foreach (Object author in changedItems)
                {
                    FireAuthorEvent(author, action);
                }
            }
        }

        /// <summary>
        ///     Fires an AuthorChanged event for the given author and action.
        /// </summary>
        /// <param name="author">the author to notify about</param>
        /// <param name="action">the action that took place for that author</param>
        private void FireAuthorEvent(Object author, AnnotationAction action)
        {
            // 'author' can be null because null authors are allowed in the collection
            Invariant.Assert(action >= AnnotationAction.Added && action <= AnnotationAction.Modified, "Unknown AnnotationAction");

            // Always update the modification time before firing change events
            _modified = DateTime.Now;

            if (AuthorChanged != null)
            {
                AuthorChanged(this, new AnnotationAuthorChangedEventArgs(this, action, author));
            }
        }

        /// <summary>
        ///     Fires a ResourceChanged event for the given resource and action.
        /// </summary>
        /// <param name="resource">the resource to notify about</param>
        /// <param name="action">the action that took place for that resource</param>
        /// <param name="handlers">the handlers to notify</param>
        private void FireResourceEvent(AnnotationResource resource, AnnotationAction action, AnnotationResourceChangedEventHandler handlers)
        {
            // resource can be null - we allow that because it could be added or removed from the annotation.
            Invariant.Assert(action >= AnnotationAction.Added && action <= AnnotationAction.Modified, "Unknown AnnotationAction");

            // Always update the modification time before firing change events
            _modified = DateTime.Now;

            if (handlers != null)
            {
                handlers(this, new AnnotationResourceChangedEventArgs(this, action, resource));
            }
        }


        private void Init()
        {
            _cargos = new AnnotationResourceCollection();
            _cargos.ItemChanged += OnCargoChanged;
            _cargos.CollectionChanged += OnCargosChanged;
            _anchors = new AnnotationResourceCollection();
            _anchors.ItemChanged += OnAnchorChanged;
            _anchors.CollectionChanged += OnAnchorsChanged;
            _authors = new ObservableCollection<string>();
            _authors.CollectionChanged += OnAuthorsChanged;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// This annotation's unique indentifier
        /// </summary>
        private Guid _id;

        /// <summary>
        /// Type name of this annotation
        /// </summary>
        private XmlQualifiedName _typeName;

        /// <summary>
        /// Time of creation of this annotation (set by the store)
        /// </summary>
        private DateTime _created;

        /// <summary>
        /// Time of last update to this annotatin (set by the store)
        /// </summary>
        private DateTime _modified;

        /// <summary>
        /// The authors for this annotation - zero or more authors
        /// </summary>
        private ObservableCollection<String> _authors;

        /// <summary>
        /// The cargo for this annotation - nothing or a single resource
        /// </summary>
        private AnnotationResourceCollection _cargos;

        /// <summary>
        /// The contexts for this annotation - zero or more resources
        /// </summary>
        private AnnotationResourceCollection _anchors;

        /// <summary>
        /// Serializer for resources - used to serialize and deserialize annotations
        /// </summary>
        private static Serializer _ResourceSerializer;

        /// <summary>
        /// Colon used to split the parts of a qualified name attribute value
        /// </summary>
        private static readonly char[] _Colon = new char[] { ':' };

        #endregion Private Fields
    }
}
