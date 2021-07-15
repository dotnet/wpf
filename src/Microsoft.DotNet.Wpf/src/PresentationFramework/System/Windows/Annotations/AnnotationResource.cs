// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     Resource represents a portion of content.  It can refer to the content
//     or directly contain the content, or both.  They are used to model anchors
//     and cargos of annotations.
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
using System.Globalization;
using System.Windows.Annotations.Storage;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MS.Internal;

using MS.Internal.Annotations;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     Resource represents a portion of content.  It can refer to the content
    ///     or directly contain the content, or both.  They are used to model anchors
    ///     and cargos of annotations.
    /// </summary>
    [XmlRoot(Namespace = AnnotationXmlConstants.Namespaces.CoreSchemaNamespace, ElementName = AnnotationXmlConstants.Elements.Resource)]
    public sealed class AnnotationResource : IXmlSerializable, INotifyPropertyChanged2, IOwnedObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of Resource.
        /// </summary>
        public AnnotationResource()
        {
            _id = Guid.NewGuid();
        }

        /// <summary>
        ///     Creates an instance of Resource with specified name.
        /// </summary>
        /// <param name="name">name used to distinguish this Resource
        /// from others in the same annotation; no validation is performed on the name</param>
        /// <exception cref="ArgumentNullException">name is null</exception>
        public AnnotationResource(string name)
            : this()
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            _name = name;
            _id = Guid.NewGuid();
        }

        /// <summary>
        ///     Creates an instance of Resource with the specified Guid as its Id.
        ///     This constructor is for store implementations that use their own
        ///     method of serialization and need a way to create a Resource with
        ///     the correct read-only data.
        /// </summary>
        /// <param name="id">the new Resource's id</param>
        /// <exception cref="ArgumentException">id is equal to Guid.Empty</exception>
        public AnnotationResource(Guid id)
        {
            if (Guid.Empty.Equals(id))
                throw new ArgumentException(SR.Get(SRID.InvalidGuid), "id");

            // Guid is a struct and cannot be null
            _id = id;
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
        ///     Returns the null.  The annotations schema can be found at
        ///     http://schemas.microsoft.com/windows/annotations/2003/11/core.
        /// </summary>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        ///     Serializes this Resource to XML with the passed in XmlWriter.
        /// </summary>
        /// <param name="writer">the writer to serialize the Resource to</param>
        /// <exception cref="ArgumentNullException">writer is null</exception>
        public void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (String.IsNullOrEmpty(writer.LookupPrefix(AnnotationXmlConstants.Namespaces.CoreSchemaNamespace)))
            {
                writer.WriteAttributeString(AnnotationXmlConstants.Prefixes.XmlnsPrefix, AnnotationXmlConstants.Prefixes.CoreSchemaPrefix, null, AnnotationXmlConstants.Namespaces.CoreSchemaNamespace);
            }

            writer.WriteAttributeString(AnnotationXmlConstants.Attributes.Id, XmlConvert.ToString(_id));
            if (_name != null)
            {
                writer.WriteAttributeString(AnnotationXmlConstants.Attributes.ResourceName, _name);
            }

            // Use the actual field here to avoid creating the collection for no reason
            if (_locators != null)
            {
                foreach (ContentLocatorBase locator in _locators)
                {
                    if (locator != null)
                    {
                        if (locator is ContentLocatorGroup)
                        {
                            LocatorGroupSerializer.Serialize(writer, locator);
                        }
                        else
                        {
                            ListSerializer.Serialize(writer, locator);
                        }
                    }
                }
            }

            // Use the actual field here to avoid creating the collection for no reason
            if (_contents != null)
            {
                foreach (XmlElement content in _contents)
                {
                    if (content != null)
                    {
                        content.WriteTo(writer);
                    }
                }
            }
        }

        /// <summary>
        ///     Deserializes an Resource from the XmlReader passed in.
        /// </summary>
        /// <param name="reader">reader to deserialize from</param>
        /// <exception cref="ArgumentNullException">reader is null</exception>        
        public void ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            XmlDocument doc = new XmlDocument();

            ReadAttributes(reader);

            if (!reader.IsEmptyElement)
            {
                reader.Read();  // Reads the remainder of "Resource" start tag

                while (!(AnnotationXmlConstants.Elements.Resource == reader.LocalName && XmlNodeType.EndElement == reader.NodeType))
                {
                    if (AnnotationXmlConstants.Elements.ContentLocatorGroup == reader.LocalName)
                    {
                        ContentLocatorBase locator = (ContentLocatorBase)LocatorGroupSerializer.Deserialize(reader);
                        InternalLocators.Add(locator);
                    }
                    else if (AnnotationXmlConstants.Elements.ContentLocator == reader.LocalName)
                    {
                        ContentLocatorBase locator = (ContentLocatorBase)ListSerializer.Deserialize(reader);
                        InternalLocators.Add(locator);
                    }
                    else if (XmlNodeType.Element == reader.NodeType)
                    {
                        XmlElement element = doc.ReadNode(reader) as XmlElement;
                        InternalContents.Add(element);
                    }
                    else
                    {
                        // The resource must contain a non-XmlElement child such as plain
                        // text which is not part of the schema.
                        throw new XmlException(SR.Get(SRID.InvalidXmlContent, AnnotationXmlConstants.Elements.Resource));
                    }
                }
            }

            reader.Read();   // Reads the end of the "Resource" element (or whole element if empty)
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
        /// 
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add{ _propertyChanged += value; }
            remove{ _propertyChanged -= value; }
        }

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     A Resource is given a unique Guid when it is first instantiated.
        /// </summary>
        /// <returns>the unique id of this Resource; this property will return an
        /// invalid Guid if the Resource was instantied with the default constructor - 
        /// which should not be used directly</returns>
        public Guid Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        ///     The name given this Resource to distinguish it from other anchors or
        ///     cargos in an annotation.
        /// </summary>
        /// <value>the name of this resource; can be null</value>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                bool changed = false;
                if (_name == null)
                {
                    if (value != null)
                    {
                        changed = true;
                    }
                }
                else if (!_name.Equals(value))
                {
                    changed = true;
                }

                _name = value;
                if (changed)
                {
                    FireResourceChanged("Name");
                }
            }
        }

        /// <summary>
        ///     Collection of zero or more Locators in this Resource.
        /// </summary>
        /// <returns>collection of Locators; never returns null</returns>
        public Collection<ContentLocatorBase> ContentLocators
        {
            get
            {
                return InternalLocators;
            }
        }

        /// <summary>
        ///     Collection of zero or more XmlElements representing the
        ///     contents of this Resource.
        /// </summary>
        /// <returns>collection of XmlElements which are the contents of 
        /// this resource; never returns null</returns>
        public Collection<XmlElement> Contents
        {
            get
            {
                return InternalContents;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        bool IOwnedObject.Owned
        {
            get
            {
                return _owned;
            }
            set
            {
                _owned = value;
            }
        }

        /// <summary>
        ///     Returns serializer for ContentLocator objects.  Lazily 
        ///     creates the serializer for cases where its not needed.
        ///     The property is internal so that other classes (ContentLocatorGroup)
        ///     has access to the same serializer.
        /// </summary>
        internal static Serializer ListSerializer
        {
            get
            {
                if (s_ListSerializer == null)
                {
                    s_ListSerializer = new Serializer(typeof(ContentLocator));
                }
                return s_ListSerializer;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        ///     Returns the list of locators - used internally to
        ///     lazily create the list when its needed.
        /// </summary>
        private AnnotationObservableCollection<ContentLocatorBase> InternalLocators
        {
            get
            {
                if (_locators == null)
                {
                    _locators = new AnnotationObservableCollection<ContentLocatorBase>();
                    _locators.CollectionChanged += OnLocatorsChanged;
                }
                return _locators;
            }
        }

        /// <summary>
        ///     Returns the list of contents - used internally to
        ///     lazily create the list when its needed.
        /// </summary>
        private XmlElementCollection InternalContents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = new XmlElementCollection();
                    _contents.CollectionChanged += OnContentsChanged;
                }
                return _contents;
            }
        }

        /// <summary>
        ///     Returns serializer for ContentLocatorGroup objects.  Lazily creates
        ///     the serializer for cases where its not needed.
        /// </summary>
        private static Serializer LocatorGroupSerializer
        {
            get
            {
                if (s_LocatorGroupSerializer == null)
                {
                    s_LocatorGroupSerializer = new Serializer(typeof(ContentLocatorGroup));
                }
                return s_LocatorGroupSerializer;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Reads all attributes for the "Resource" element and throws
        /// appropriate exceptions if any required attributes are missing
        /// or unexpected attributes are found
        /// </summary>
        private void ReadAttributes(XmlReader reader)
        {
            Invariant.Assert(reader != null, "No reader passed in.");

            // Use a temporary variable to determine if a Guid value
            // was actually provided.  The member variable is set in
            // the default ctor and can't be relied on for this.
            Guid tempId = Guid.Empty;

            // Read all the attributes
            while (reader.MoveToNextAttribute())
            {
                string value = reader.Value;

                // Skip null values - they will be treated the same as if
                // they weren't specified at all
                if (value == null)
                    continue;

                switch (reader.LocalName)
                {
                    case AnnotationXmlConstants.Attributes.Id:
                        tempId = XmlConvert.ToGuid(value);
                        break;

                    case AnnotationXmlConstants.Attributes.ResourceName:
                        _name = value;
                        break;

                    default:
                        if (!Annotation.IsNamespaceDeclaration(reader))
                            throw new XmlException(SR.Get(SRID.UnexpectedAttribute, reader.LocalName, AnnotationXmlConstants.Elements.Resource));
                        break;
                }
            }

            if (Guid.Empty.Equals(tempId))
            {
                throw new XmlException(SR.Get(SRID.RequiredAttributeMissing, AnnotationXmlConstants.Attributes.Id, AnnotationXmlConstants.Elements.Resource));
            }

            _id = tempId;

            // Name attribute is optional, so no need to check it

            // Move back to the parent "Resource" element
            reader.MoveToContent();
        }

        /// <summary>
        /// Listens for change events from the list of locators.  Fires a change event
        /// for this resource when an event is received.
        /// </summary>
        private void OnLocatorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FireResourceChanged("Locators");
        }

        /// <summary>
        /// Listens for change events from the list of contents.  Fires a change event
        /// for this resource when an event is received.
        /// </summary>
        private void OnContentsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FireResourceChanged("Contents");
        }

        /// <summary>
        ///     Fires a change notification to the Annotation that contains
        ///     this Resource.
        /// </summary>
        private void FireResourceChanged(string name)
        {
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  2Private Fields
        //
        //------------------------------------------------------

        #region Private Fields


        /// <summary>
        /// Unique ID for this resource
        /// </summary>
        private Guid _id;

        /// <summary>
        /// Name given this resource by the developer.  Used to differentiate
        /// between multiple resources on an annotation.
        /// </summary>
        private string _name;

        /// <summary>
        /// List of locators that refer to the data of this resource
        /// </summary>
        private AnnotationObservableCollection<ContentLocatorBase> _locators;

        /// <summary>
        /// List of instances of the data for this resource
        /// </summary>
        private XmlElementCollection _contents;

        /// <summary>
        /// Serializer we use for serializing and deserializing Locators.  
        /// </summary>
        private static Serializer s_ListSerializer;

        /// <summary>
        /// Serializer we use for serializing and deserializing LocatorGroups.  
        /// </summary>
        private static Serializer s_LocatorGroupSerializer;

        /// <summary>
        /// 
        /// </summary>
        private bool _owned;

        /// <summary>
        /// 
        /// </summary>
        private PropertyChangedEventHandler _propertyChanged;

        #endregion Private Fields
    }
}
