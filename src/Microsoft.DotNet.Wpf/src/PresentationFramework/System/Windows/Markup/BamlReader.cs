// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Public interface for reading BamlRecords with an interface
*           that is similar to XmlReader
*
\***************************************************************************/

using System;
using System.Xml;
using System.IO;
using System.Windows;
using System.Windows.Navigation;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;

using System.Globalization;
using MS.Utility;
using MS.Internal;

namespace System.Windows.Markup
{
    /// <summary>
    /// Type of BAML node at the current BamlReader location in
    /// the BAML stream.
    /// </summary>
    internal enum BamlNodeType
    {
        /// <summary>
        /// This is returned if Read() has not been called, or the end of the
        /// file has been reached.
        /// </summary>
        None,

        /// <summary>
        /// Start of a BAML document.  This contains version information about the BAML.
        /// </summary>
        StartDocument,

        /// <summary>
        /// End of a BAML document
        /// </summary>
        EndDocument,

        /// <summary>
        /// Connection Id.
        /// </summary>
        ConnectionId,

        /// <summary>
        /// Start of an Element.  An Element is any object that exists in
        /// an object tree.  This has a rough correspondance to Element nodes in XML
        /// </summary>
        StartElement,

        /// <summary>
        /// End of an Element.
        /// </summary>
        EndElement,

        /// <summary>
        /// A simple property of an Element.  This can be a native property, attached
        /// property, or XML specific properties such as namespaces
        /// </summary>
        Property,

        /// <summary>
        /// The content property of an Element.
        /// </summary>
        ContentProperty,

        /// <summary>
        /// A namespace property that defines prefix to namespace mappings in xml
        /// </summary>
        XmlnsProperty,

        /// <summary>
        /// The start of a compound property on an Element.  This is used when the property value is
        /// represented as a single string Value, but as an element tree.
        /// </summary>
        StartComplexProperty,

        /// <summary>
        /// The end of a compound property on an Element.
        /// </summary>
        EndComplexProperty,

        /// <summary>
        /// A section of literal content that is handled by an object
        /// </summary>
        LiteralContent,

        /// <summary>
        /// Text content of an element.
        /// </summary>
        Text,

        /// <summary>
        /// A RoutedEvent
        /// </summary>
        RoutedEvent,

        /// <summary>
        /// A non-routed, or normal CLR Event
        /// </summary>
        Event,

        /// <summary>
        /// A reference to a Resource that is included at this point in the BAML stream
        /// </summary>
        IncludeReference,

        /// <summary>
        /// A specific attribute or property in the reserved "Definition" namespace.
        /// These attributes do not map to CLR properties or events, but are used
        /// as processing directives.
        /// </summary>
        DefAttribute,

        /// <summary>
        /// A specific attribute or property in the reserved "PresentationOptions" namespace.
        /// These attributes do not map to CLR properties or events, but are used
        /// as processing directives.
        /// </summary>
        PresentationOptionsAttribute,

        /// <summary>
        /// A namespace mapping instruction used for including custom code and namespaces
        /// </summary>
        PIMapping,

        /// <summary>
        /// Start of a section that specifies a list of objects to be passed to the
        /// element's constructor
        /// </summary>
        StartConstructor,

        /// <summary>
        /// End of a section that specifies a list of objects to be passed to the
        /// element's constructor
        /// </summary>
        EndConstructor
    }

    /// <summary>
    /// Reads BAML from a Stream and exposes an XmlReader-liker interface for BAML
    /// </summary>
    internal class BamlReader
    {
        #region Constructor

        /// <summary>
        /// Create an instance of the BamlReader on the passed stream using
        /// the passed ParserContext.
        /// </summary>summary>
        public BamlReader(Stream bamlStream)
        {
            _parserContext = new ParserContext();
            _parserContext.XamlTypeMapper = XmlParserDefaults.DefaultMapper;
            _bamlRecordReader = new BamlRecordReader(bamlStream, _parserContext, false);
            _readState = ReadState.Initial;
            _bamlNodeType = BamlNodeType.None;
            _prefixDictionary = new XmlnsDictionary();
            _value = string.Empty;
            _assemblyName = string.Empty;
            _prefix = string.Empty;
            _xmlNamespace = string.Empty;
            _clrNamespace = string.Empty;
            _name = string.Empty;
            _localName = string.Empty;
            _ownerTypeName = string.Empty;
            _properties = new ArrayList();
            _haveUnprocessedRecord = false;
            _deferableContentBlockDepth = -1;
            _nodeStack = new Stack();
            _reverseXmlnsTable = new Dictionary<String, List<String>>();
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Return the number of properties.  Note that this
        /// does not include complex properties or children elements
        /// </summary>
        public int PropertyCount
        {
            get { return _properties.Count; }
        }

        /// <summary>
        /// Return true if the current node has any simple properties
        /// </summary>
        public bool HasProperties
        {
            get { return PropertyCount > 0; }
        }

        /// <summary>
        /// Return the connection Id of current element for hooking up
        /// IDs and events.
        /// </summary>
        public Int32 ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Defines what this attribute is used for such as being an alias for
        /// xml:lang, xml:space or x:ID
        /// </summary>
        public BamlAttributeUsage AttributeUsage
        {
            get { return _attributeUsage; }
        }

        /// <summary>
        /// Gets the type of the current node (eg  Element, StartComplexProperty,
        /// Text, etc)
        /// </summary>
        public BamlNodeType NodeType
        {
            get { return _bamlNodeType; }
        }

        /// <summary>
        /// Gets the fully qualified name of the current node.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the local name only, with prefix and owning class removed
        /// </summary>
        public string LocalName
        {
            get { return _localName; }
        }

        /// <summary>
        /// Gets the prefix associated with the current node, if there is one
        /// </summary>
        //
        // NOTE: Used by the localization tools via reflection.
        //
        public string Prefix
        {
            get { return _prefix; }
        }

        /// <summary>
        /// Gets the assembly name associated with the type of the current node, if there is one
        /// </summary>
        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        /// <summary>
        /// Gets the XML namespace URI of the node on which the reader is positioned
        /// </summary>
        public string XmlNamespace
        {
            get { return _xmlNamespace; }
        }

        /// <summary>
        /// Gets the CLR namespace of the node on which the reader is positioned
        /// </summary>
        public string ClrNamespace
        {
            get { return _clrNamespace; }
        }

        /// <summary>
        /// Gets the text value of the current node (eg  property value or text content)
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        public bool IsInjected
        {
            get { return _isInjected; }
        }

        // Whether this object instance is expected to be created via TypeConverter
        public bool CreateUsingTypeConverter
        {
            get { return _useTypeConverter; }
        }

        public string TypeConverterName
        {
            get { return _typeConverterName; }
        }

        public string TypeConverterAssemblyName
        {
            get { return _typeConverterAssemblyName; }
        }

        /// <summary>
        /// Reads the next node from the stream.
        /// </summary>
        public bool Read()
        {
            if (_readState == ReadState.EndOfFile ||
                _readState == ReadState.Closed)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlReaderClosed));
            }

            ReadNextRecord();

            return _readState != ReadState.EndOfFile;
        }

        private BamlNodeType NodeTypeInternal
        {
            set { _bamlNodeType = value; }
        }

        private void AddToPropertyInfoCollection(object info)
        {
            _properties.Add(info);
        }

        /// <summary>
        /// Close the underlying BAML stream.
        /// </summary>
        /// <remarks>
        /// Once the BamlReader is closed, it cannot be used
        /// for any further operations.  Calling any public interfaces will fail.
        /// </remarks>
        public void Close()
        {
            if (_readState != ReadState.Closed)
            {
                _bamlRecordReader.Close();
                _currentBamlRecord = null;
                _bamlRecordReader = null;
                _readState = ReadState.Closed;
            }
        }

        /// <summary>
        /// Moves to the first property for this element or object.
        /// Return true if property exists, false otherwise.
        /// </summary>
        public bool MoveToFirstProperty()
        {
            if (HasProperties)
            {
                _propertiesIndex = -1;
                return MoveToNextProperty();
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Move to the next property for this element or object.
        /// Return true if there is a next property; false if there are no more properties.
        /// </summary>
        public bool MoveToNextProperty()
        {
            if (_propertiesIndex < _properties.Count - 1)
            {
                _propertiesIndex++;
                object obj = _properties[_propertiesIndex];

                BamlPropertyInfo info = obj as BamlPropertyInfo;
                if (info != null)
                {
                    _name = info.Name;
                    _localName = info.LocalName;
                    int index = info.Name.LastIndexOf('.');
                    if (index > 0)
                    {
                        _ownerTypeName = info.Name.Substring(0, index);
                    }
                    else
                    {
                        // Eg. xmlns property
                        _ownerTypeName = string.Empty;
                    }
                    _value = info.Value;
                    _assemblyName = info.AssemblyName;
                    _prefix = info.Prefix;
                    _xmlNamespace = info.XmlNamespace;
                    _clrNamespace = info.ClrNamespace;
                    _connectionId = 0;
                    _contentPropertyName = string.Empty;
                    _attributeUsage = info.AttributeUsage;

                    // There are several node types for properties, but for now the only one that
                    // doesn't map to BamlNodeType.Property is xml namespace declarations.
                    if (info.RecordType == BamlRecordType.XmlnsProperty)
                    {
                        NodeTypeInternal = BamlNodeType.XmlnsProperty;
                    }
                    else if (info.RecordType == BamlRecordType.DefAttribute)
                    {
                        NodeTypeInternal = BamlNodeType.DefAttribute;
                    }
                    else if (info.RecordType == BamlRecordType.PresentationOptionsAttribute)
                    {
                        NodeTypeInternal = BamlNodeType.PresentationOptionsAttribute;
                    }
                    else
                    {
                        NodeTypeInternal = BamlNodeType.Property;
                    }
                    return true;
                }

                BamlContentPropertyInfo cpInfo = obj as BamlContentPropertyInfo;
                if(null != cpInfo)
                {
                    _contentPropertyName = cpInfo.LocalName;
                    _connectionId = 0;
                    _prefix = string.Empty;
                    _name = cpInfo.Name;
                    int index = cpInfo.Name.LastIndexOf('.');
                    if (index > 0)
                    {
                        _ownerTypeName = cpInfo.Name.Substring(0, index);
                    }

                    _localName = cpInfo.LocalName;
                    _ownerTypeName = string.Empty;
                    _assemblyName = cpInfo.AssemblyName;
                    _xmlNamespace = string.Empty;
                    _clrNamespace = string.Empty;
                    _attributeUsage = BamlAttributeUsage.Default;
                    _value = cpInfo.LocalName;
                    NodeTypeInternal = BamlNodeType.ContentProperty;
                    return true;
                }
                // Otherwise it must be a ConnectionId.
                // Is there something we can Assert on for that?
                _connectionId = (Int32)obj;
                _contentPropertyName = string.Empty;
                _prefix = string.Empty;
                _name = string.Empty;
                _localName = string.Empty;
                _ownerTypeName = string.Empty;
                _assemblyName = string.Empty;
                _xmlNamespace = string.Empty;
                _clrNamespace = string.Empty;
                _attributeUsage = BamlAttributeUsage.Default;
                _value = _connectionId.ToString(CultureInfo.CurrentCulture);
                NodeTypeInternal = BamlNodeType.ConnectionId;
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Gets the next BamlRecord to be processed
        /// </summary>
        private void GetNextRecord()
        {
            if (_currentStaticResourceRecords != null)
            {
                // Load the record from the front loaded static resource
                _currentBamlRecord = _currentStaticResourceRecords[_currentStaticResourceRecordIndex++];

                if (_currentStaticResourceRecordIndex == _currentStaticResourceRecords.Count)
                {
                    // We are done with the records for this front loaded static resource
                    _currentStaticResourceRecords = null;
                    _currentStaticResourceRecordIndex = -1;
                }
            }
            else
            {
                // Use the BamlRecord Reader to get the record
                _currentBamlRecord = _bamlRecordReader.GetNextRecord();
            }
        }

        /***************************************************************************\
        *
        * BamlReader.ReadNextRecord
        *
        * Read the next record, setting the ReadState, NodeType and other pertinent
        * information about the record just read.
        *
        \***************************************************************************/

        private void ReadNextRecord()
        {
            // If this is the first call to Read.  Then read the Version Header.
            if(_readState == ReadState.Initial)
            {
                _bamlRecordReader.ReadVersionHeader();
            }

            // We'll read in a loop until we get to a significant record.  Note that Assembly,
            // Type and Attribute records are read and processed, but the BamlReader never stops
            // on one of these records, since they are not externally exposed.
            bool keepOnReading = true;
            while (keepOnReading)
            {
                // We may already have a record that was previously read in but not
                // processed.  This occurs when we've looped through all the properties
                // on an element and have encountered a non-property record to stop
                // the loop.  In that case don't read another record and just process
                // the one we have.
                if (_haveUnprocessedRecord)
                {
                    _haveUnprocessedRecord = false;
                }
                else
                {
                    GetNextRecord();
                }

                // If the current baml record is null, then the stream is finished, closed
                // or something else that prevents us reading further, so treat this
                // as an end-of-file condition
                if (_currentBamlRecord == null)
                {
                    NodeTypeInternal = BamlNodeType.None;
                    _readState = ReadState.EndOfFile;
                    ClearProperties();
                    return;
                }


                // By default, the read state is interactive after a record has been read, and we
                // should stop reading after this record is processed.  This may be altered for
                // specific record types.
                _readState = ReadState.Interactive;
                keepOnReading = false;

                switch (_currentBamlRecord.RecordType)
                {
                    // The following three records are internal to the BAMLReader and
                    // are not exposed publicly.  They are used to update the map table
                    // that maps ids to assemblies, types and attributes.
                    case BamlRecordType.AssemblyInfo:
                        ReadAssemblyInfoRecord();
                        keepOnReading = true;
                        break;

                    case BamlRecordType.TypeInfo:
                    case BamlRecordType.TypeSerializerInfo:
                        MapTable.LoadTypeInfoRecord((BamlTypeInfoRecord)_currentBamlRecord);
                        keepOnReading = true;
                        break;

                    case BamlRecordType.AttributeInfo:
                        MapTable.LoadAttributeInfoRecord((BamlAttributeInfoRecord)_currentBamlRecord);
                        keepOnReading = true;
                        break;

                    case BamlRecordType.StringInfo:
                        MapTable.LoadStringInfoRecord((BamlStringInfoRecord)_currentBamlRecord);
                        keepOnReading = true;
                        break;

                    case BamlRecordType.ContentProperty:
                        // This is just a cache of meta-data, no visible effect.
                        ReadContentPropertyRecord();
                        keepOnReading = true;
                        break;

                    // The following records are publically exposed
                    case BamlRecordType.DocumentStart:
                        ReadDocumentStartRecord();
                        break;

                    case BamlRecordType.DocumentEnd:
                        ReadDocumentEndRecord();
                        break;

                    case BamlRecordType.PIMapping:
                        ReadPIMappingRecord();
                        break;

                    case BamlRecordType.LiteralContent:
                        ReadLiteralContentRecord();
                        break;

                    case BamlRecordType.ElementStart:
                    case BamlRecordType.StaticResourceStart:
                        ReadElementStartRecord();
                        break;

                    case BamlRecordType.ElementEnd:
                    case BamlRecordType.StaticResourceEnd:
                        ReadElementEndRecord();
                        break;

                    case BamlRecordType.PropertyComplexStart:
                    case BamlRecordType.PropertyArrayStart:
                    case BamlRecordType.PropertyIListStart:
                    case BamlRecordType.PropertyIDictionaryStart:
                        ReadPropertyComplexStartRecord();
                        break;

                    case BamlRecordType.PropertyComplexEnd:
                    case BamlRecordType.PropertyArrayEnd:
                    case BamlRecordType.PropertyIListEnd:
                    case BamlRecordType.PropertyIDictionaryEnd:
                        ReadPropertyComplexEndRecord();
                        break;

                    case BamlRecordType.Text:
                    case BamlRecordType.TextWithId:
                    case BamlRecordType.TextWithConverter:
                        ReadTextRecord();
                        break;

                    case BamlRecordType.DeferableContentStart:
                        ReadDeferableContentRecord();
                        keepOnReading = true;
                        break;

                    case BamlRecordType.ConstructorParametersStart:
                        ReadConstructorStart();
                        break;

                    case BamlRecordType.ConstructorParametersEnd:
                        ReadConstructorEnd();
                        break;

                    case BamlRecordType.ConnectionId:
                        ReadConnectionIdRecord();
                        break;

                    case BamlRecordType.StaticResourceId:
                        ReadStaticResourceId();
                        keepOnReading = true;
                        break;

                    default:
                        // Can't have any other type of record at this point.
                        throw new InvalidOperationException(SR.Get(SRID.ParserUnknownBaml,
                                         ((int)_currentBamlRecord.RecordType).ToString(CultureInfo.CurrentCulture)));
                }
            }
        }

        /***************************************************************************\
        *
        * BamlReader.ReadProperties
        *
        * This is called when an element start has been encountered (or something
        * similar) and a number of properties may or may not follow.  Read all the
        * properties, storing them in the _properties arraylist.  When a non-property
        * record is encountered, stop, but store that record as the
        * _currentBamlRecord.
        *
        \***************************************************************************/

        private void ReadProperties()
        {
            // Keep reading records until we get one that is not processed
            while (!_haveUnprocessedRecord)
            {
                GetNextRecord();

                ProcessPropertyRecord();
            }
        }

        /***************************************************************************\
        *
        * BamlReader.ProcessPropertyRecord
        *
        * This is called we assume we have a record that is an attribute in the start
        * tag of an xml element.  It is processed here.  If we encounter something
        * that is not a 'property', then set the _haveUnprocessedRecord flag.
        *
        \***************************************************************************/

        private void ProcessPropertyRecord()
        {
            switch (_currentBamlRecord.RecordType)
            {
                // The following five records are internal to the BAMLReader and
                // are not exposed publicly.  They are used to update the map table
                // that maps ids to assemblies, types and attributes.
                case BamlRecordType.AssemblyInfo:
                    ReadAssemblyInfoRecord();
                    break;

                case BamlRecordType.TypeInfo:
                case BamlRecordType.TypeSerializerInfo:
                    MapTable.LoadTypeInfoRecord((BamlTypeInfoRecord)_currentBamlRecord);
                    break;

                case BamlRecordType.AttributeInfo:
                    MapTable.LoadAttributeInfoRecord((BamlAttributeInfoRecord)_currentBamlRecord);
                    break;

                case BamlRecordType.StringInfo:
                    MapTable.LoadStringInfoRecord((BamlStringInfoRecord)_currentBamlRecord);
                    break;

                // The following records are property-like
                case BamlRecordType.XmlnsProperty:
                    ReadXmlnsPropertyRecord();
                    break;

                case BamlRecordType.ConnectionId:
                    ReadConnectionIdRecord();
                    break;

                case BamlRecordType.Property:
                case BamlRecordType.PropertyWithConverter:
                    ReadPropertyRecord();
                    break;

                case BamlRecordType.ContentProperty:
                    // This is just a cache of meta-data, no visible effect.
                    ReadContentPropertyRecord();
                    break;

                case BamlRecordType.PropertyStringReference:
                    ReadPropertyStringRecord();
                    break;

                case BamlRecordType.PropertyTypeReference:
                    ReadPropertyTypeRecord();
                    break;

                case BamlRecordType.PropertyWithExtension:
                    ReadPropertyWithExtensionRecord();
                    break;

                case BamlRecordType.PropertyWithStaticResourceId:
                    ReadPropertyWithStaticResourceIdRecord();
                    break;

                case BamlRecordType.PropertyCustom:
                    ReadPropertyCustomRecord();
                    break;

                case BamlRecordType.DefAttribute:
                    ReadDefAttributeRecord();
                    break;

                case BamlRecordType.PresentationOptionsAttribute:
                    ReadPresentationOptionsAttributeRecord();
                    break;

                case BamlRecordType.DefAttributeKeyType:
                    ReadDefAttributeKeyTypeRecord();
                    break;

                case BamlRecordType.RoutedEvent:
                    ReadRoutedEventRecord();
                    break;

                case BamlRecordType.ClrEvent:
                    ReadClrEventRecord();
                    break;

                case BamlRecordType.KeyElementStart:
                    {
                        // Process the subtree that is stored as part of a key tree and
                        // translate this back into a compact MarkupExtension string that
                        // is represented as a x:Key="something"
                        BamlKeyInfo info = ProcessKeyTree();
                        AddToPropertyInfoCollection(info);
                    }
                    break;

                // Any other record types are not processed here
                default:
                    _haveUnprocessedRecord = true;
                    break;
            }
        }


        /***************************************************************************\
        *
        * BamlReader.ReadXmlnsPropertyRecord
        *
        * Read the namespace record and update the namespace dictionary with the
        * key being the namespace prefix and the value being the namespace string.
        * Note that Xmlns properties are not the same as regular properties, since
        * prefix, xmlnamespace, clrnamespace and name have quite different meanings.
        *
        \***************************************************************************/

        private void ReadXmlnsPropertyRecord()
        {
            BamlXmlnsPropertyRecord bamlRecord = (BamlXmlnsPropertyRecord)_currentBamlRecord;
            _parserContext.XmlnsDictionary[bamlRecord.Prefix] = bamlRecord.XmlNamespace;
            _prefixDictionary[bamlRecord.XmlNamespace] = bamlRecord.Prefix;

            BamlPropertyInfo info = new BamlPropertyInfo();
            info.Value = bamlRecord.XmlNamespace;
            info.XmlNamespace = string.Empty;
            info.ClrNamespace = string.Empty;
            info.AssemblyName = string.Empty;
            info.Prefix = "xmlns";
            info.LocalName = bamlRecord.Prefix == null ? string.Empty : bamlRecord.Prefix;
            info.Name = (bamlRecord.Prefix == null || bamlRecord.Prefix == string.Empty) ?
                                          "xmlns" :
                                          "xmlns:" + bamlRecord.Prefix;
            info.RecordType = BamlRecordType.XmlnsProperty;

            AddToPropertyInfoCollection(info);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyRecord
        *
        * Read the property record and store the pertinent contents in the
        * _properties array list.
        *
        \***************************************************************************/

        private void ReadPropertyRecord()
        {
            string value = ((BamlPropertyRecord)_currentBamlRecord).Value;

            // Escape the text as necessary to avoid being mistaken for a MarkupExtension.
            value = MarkupExtensionParser.AddEscapeToLiteralString(value);

            AddToPropertyInfoCollection(ReadPropertyRecordCore(value));
        }

        private void ReadContentPropertyRecord()
        {
            BamlContentPropertyInfo cpInfo = new BamlContentPropertyInfo();

            BamlContentPropertyRecord bamlRecord = (BamlContentPropertyRecord)_currentBamlRecord;
            SetCommonPropertyInfo(cpInfo, bamlRecord.AttributeId);

            cpInfo.RecordType = _currentBamlRecord.RecordType;
            AddToPropertyInfoCollection(cpInfo);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyStringRecord
        *
        * Read the property record and store the pertinent contents in the
        * _properties array list.
        *
        \***************************************************************************/

        private void ReadPropertyStringRecord()
        {
            string value = MapTable.GetStringFromStringId(((BamlPropertyStringReferenceRecord)_currentBamlRecord).StringId);
            AddToPropertyInfoCollection(ReadPropertyRecordCore(value));
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyTypeRecord
        *
        * Read the property record and store the pertinent contents in the
        * _properties array list.  Convert the TypeId into a fully qualified type
        * name.
        *
        \***************************************************************************/

        private void ReadPropertyTypeRecord()
        {
            BamlPropertyInfo info = new BamlPropertyInfo();

            SetCommonPropertyInfo(info,
                   ((BamlPropertyTypeReferenceRecord)_currentBamlRecord).AttributeId);

            info.RecordType = _currentBamlRecord.RecordType;
            info.Value = GetTypeValueString(((BamlPropertyTypeReferenceRecord)_currentBamlRecord).TypeId);
            info.AttributeUsage = BamlAttributeUsage.Default;

            AddToPropertyInfoCollection(info);
        }

        private void ReadPropertyWithExtensionRecord()
        {
            BamlPropertyInfo info = new BamlPropertyInfo();
            SetCommonPropertyInfo(info, ((BamlPropertyWithExtensionRecord)_currentBamlRecord).AttributeId);

            info.RecordType = _currentBamlRecord.RecordType;
            info.Value = GetExtensionValueString((IOptimizedMarkupExtension)_currentBamlRecord);
            info.AttributeUsage = BamlAttributeUsage.Default;

            AddToPropertyInfoCollection(info);
        }

        private void ReadPropertyWithStaticResourceIdRecord()
        {
            BamlPropertyWithStaticResourceIdRecord bamlPropertyWithStaticResourceIdRecord =
                (BamlPropertyWithStaticResourceIdRecord)_currentBamlRecord;

            BamlPropertyInfo info = new BamlPropertyInfo();
            SetCommonPropertyInfo(info, bamlPropertyWithStaticResourceIdRecord.AttributeId);

            info.RecordType = _currentBamlRecord.RecordType;

            BamlOptimizedStaticResourceRecord optimizedStaticResourceRecord =
                (BamlOptimizedStaticResourceRecord)_currentKeyInfo.StaticResources[bamlPropertyWithStaticResourceIdRecord.StaticResourceId][0];

            info.Value = GetExtensionValueString((IOptimizedMarkupExtension)optimizedStaticResourceRecord);
            info.AttributeUsage = BamlAttributeUsage.Default;

            AddToPropertyInfoCollection(info);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyRecordCore
        *
        * Read the property record and return the pertinent contents in a
        * BamlPropertyInfo record.
        *
        \***************************************************************************/

        private BamlPropertyInfo ReadPropertyRecordCore(string value)
        {
            BamlPropertyInfo info = new BamlPropertyInfo();

            SetCommonPropertyInfo(info,
                   ((BamlPropertyRecord)_currentBamlRecord).AttributeId);

            info.RecordType = _currentBamlRecord.RecordType;
            info.Value = value;

            return info;
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyCustomRecord
        *
        * Read the custom property record and store the pertinent contents in the
        * _properties array list.  This involves reversing the binary representation
        * of the data back into a string using the TypeConverter for the
        * property type.
        *
        \***************************************************************************/

        private void ReadPropertyCustomRecord()
        {
            BamlPropertyInfo info = GetPropertyCustomRecordInfo();
            AddToPropertyInfoCollection(info);
        }

        private BamlPropertyInfo GetPropertyCustomRecordInfo()
        {
            BamlPropertyInfo info = new BamlPropertyInfo();

            BamlAttributeInfoRecord attrInfo = SetCommonPropertyInfo(info,
                ((BamlPropertyCustomRecord)_currentBamlRecord).AttributeId);
            info.RecordType = _currentBamlRecord.RecordType;
            info.AttributeUsage = BamlAttributeUsage.Default;

            BamlPropertyCustomRecord bamlRecord = (BamlPropertyCustomRecord)_currentBamlRecord;

            // Reverse the binary data stored in the record into a string by first getting the
            // property.  If it has not already been cached in the attribute info record, then
            // attempt to resolve it as a DependencyProperty or a PropertyInfo.
            if (attrInfo.DP == null && attrInfo.PropInfo == null)
            {
                attrInfo.DP = MapTable.GetDependencyProperty(attrInfo);

                if (attrInfo.OwnerType == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.BamlReaderNoOwnerType, attrInfo.Name, AssemblyName));
                }
                if (attrInfo.DP == null)
                {
                    try
                    {
                        attrInfo.PropInfo = attrInfo.OwnerType.GetProperty(attrInfo.Name,
                                BindingFlags.Instance | BindingFlags.Public);
                    }
                    catch (AmbiguousMatchException)
                    {
                        // Handle ambiguous match just like XamlTypeMapper.PropertyInfoFromName does.
                        // This is for consistency, although it's probably wrong.
                        // The doc for GetProperties says:
                        //      The GetProperties method does not return properties
                        //      in a particular order, such as alphabetical or
                        //      declaration order. Your code must not depend on the
                        //      order in which properties are returned, because that
                        //      order varies.
                        // It's probably more correct to walk up the base class
                        // tree calling GetProperty with the DeclaredOnly flag.

                        PropertyInfo[] infos = attrInfo.OwnerType.GetProperties(
                                      BindingFlags.Instance | BindingFlags.Public);
                        for (int i = 0; i < infos.Length; i++)
                        {
                            if (infos[i].Name == attrInfo.Name)
                            {
                                attrInfo.PropInfo = infos[i];
                                break;
                            }
                        }
                    }

                    if (attrInfo.PropInfo == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.ParserCantGetDPOrPi, info.Name));
                    }
                }
            }

            // If we have a property, then get its type and call GetCustomValue,
            // which uses the XamlSerializer to turn the binary data into a
            // real object
            Type propertyType = attrInfo.GetPropertyType();
            string propertyName = attrInfo.Name;
            short sid = bamlRecord.SerializerTypeId;

            // if a Setter of Trigger's Property property is being set, then its value is always
            // a DP. Get the attribInfo of this DP property from the ValueId read into the custom
            // property record and resolve it into an actual DP instance.
            if (sid == (short)KnownElements.DependencyPropertyConverter)
            {
                Type declaringType = null;
                _propertyDP = _bamlRecordReader.GetCustomDependencyPropertyValue(bamlRecord, out declaringType);
                declaringType = declaringType == null ? _propertyDP.OwnerType : declaringType;
                info.Value = declaringType.Name + "." + _propertyDP.Name;

                string xmlns = _parserContext.XamlTypeMapper.GetXmlNamespace(declaringType.Namespace,
                                                                             declaringType.Assembly.FullName);

                string prefix = GetXmlnsPrefix(xmlns);
                if (prefix != string.Empty)
                {
                    info.Value = prefix + ":" + info.Value;
                }

                if (!_propertyDP.PropertyType.IsEnum)
                {
                    _propertyDP = null;
                }
            }
            else
            {
                if (_propertyDP != null)
                {
                    propertyType = _propertyDP.PropertyType;
                    propertyName = _propertyDP.Name;
                    _propertyDP = null;
                }

                object value = _bamlRecordReader.GetCustomValue(bamlRecord, propertyType, propertyName);

                // Once we have a real object, turn that back into a string, and store this
                // as the value for this property
                TypeConverter converter = TypeDescriptor.GetConverter(value.GetType());
                info.Value = converter.ConvertToString(null,
                                                       TypeConverterHelper.InvariantEnglishUS,
                                                       value);
            }

            return info;
        }

        /***************************************************************************\
        *
        * BamlReader.ReadDefAttributeRecord
        *
        * Read a x: record that contains the object to use as a key when inserting
        * the current element into a dictionary.
        *
        \***************************************************************************/

        private void ReadDefAttributeRecord()
        {
            BamlDefAttributeRecord bamlRecord = (BamlDefAttributeRecord)_currentBamlRecord;
            bamlRecord.Name = MapTable.GetStringFromStringId(bamlRecord.NameId);

            BamlPropertyInfo info = new BamlPropertyInfo();
            info.Value = bamlRecord.Value;
            info.AssemblyName = string.Empty;
            info.Prefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
            info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
            info.ClrNamespace = string.Empty;
            info.Name = bamlRecord.Name;
            info.LocalName = info.Name;
            info.RecordType = BamlRecordType.DefAttribute;

            AddToPropertyInfoCollection(info);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPresentationOptionsAttributeRecord
        *
        * Read a PresentationsOptions: record used for WPF-specific
        * parsing options (e.g., PresentationOptions:Freeze).
        *
        \***************************************************************************/

        private void ReadPresentationOptionsAttributeRecord()
        {
            BamlPresentationOptionsAttributeRecord bamlRecord = (BamlPresentationOptionsAttributeRecord)_currentBamlRecord;
            bamlRecord.Name = MapTable.GetStringFromStringId(bamlRecord.NameId);

            BamlPropertyInfo info = new BamlPropertyInfo();
            info.Value = bamlRecord.Value;
            info.AssemblyName = string.Empty;
            info.Prefix = (string)_prefixDictionary[XamlReaderHelper.PresentationOptionsNamespaceURI];
            info.XmlNamespace = XamlReaderHelper.PresentationOptionsNamespaceURI;
            info.ClrNamespace = string.Empty;
            info.Name = bamlRecord.Name;
            info.LocalName = info.Name;
            info.RecordType = BamlRecordType.PresentationOptionsAttribute;

            AddToPropertyInfoCollection(info);
        }


        /***************************************************************************\
        *
        * BamlReader.ReadDefAttributeKeyTypeRecord
        *
        * Read a x: record that contains the object to use as a key when inserting
        * the current element into a dictionary.
        *
        \***************************************************************************/

        private void ReadDefAttributeKeyTypeRecord()
        {
            BamlDefAttributeKeyTypeRecord bamlRecord = (BamlDefAttributeKeyTypeRecord)_currentBamlRecord;

            BamlPropertyInfo info = new BamlPropertyInfo();
            info.Value = GetTypeValueString(bamlRecord.TypeId);
            info.AssemblyName = string.Empty;
            info.Prefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
            info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
            info.ClrNamespace = string.Empty;
            info.Name = XamlReaderHelper.DefinitionName;
            info.LocalName = info.Name;
            info.RecordType = BamlRecordType.DefAttribute;

            AddToPropertyInfoCollection(info);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadDeferableContentRecord
        *
        * Read defered content section of a baml file.  Note that this is written
        * to baml with the following format:
        *     BamlDeferableContentStartRecord
        *     BamlDefAttributeKeyString/Type records, one for each value
        *     BamlElementStartRecord, one for each value
        *       BamlRecords within the element
        *     BamlElementEndRecord
        *
        * This is presented to the user like a 'normal' dictionary, so here is
        * what the user should see:
        *     ElementStart for the dictionary
        *      ElementStart, one for each value
        *       DictionaryKey record, one for each value
        *       Records within the element
        *      ElementEnd
        *     ElementEnd for the dictionary
        *
        * To do this, queue up the entire contents of the Deferable block and
        * re-arrange the records.
        *
        \***************************************************************************/

        private void ReadDeferableContentRecord()
        {
            _deferableContentBlockDepth = _nodeStack.Count;

            // The start of a block of deferable content has been reached.  Build
            // a key table that will be inserted into the Values as they are loaded.
            _deferableContentPosition = ReadDeferKeys();
        }

        /***************************************************************************\
        *
        * BamlReader.ReadDeferKeys
        *
        * Read the keys in a defered content section, and build a table that holds
        * these records.  These keys are automagically inserted into the outer level
        * value start element records to make it look like a x:Key attribute.
        * Return the baml stream position for the end of the key section to which
        * all key offsets are relative
        *
        \***************************************************************************/

        private Int64 ReadDeferKeys()
        {
            // Keep reading records until we get one that is not processed
            Int64 endOfDefKeys = -1;
            _deferKeys = new List<BamlKeyInfo>();
            while (!_haveUnprocessedRecord)
            {
                GetNextRecord();

                ProcessDeferKey();

                if (!_haveUnprocessedRecord)
                {
                    endOfDefKeys = _bamlRecordReader.StreamPosition;
                }
            }

            return endOfDefKeys;
        }

        /***************************************************************************\
        *
        * BamlReader.ProcessDeferKey
        *
        * Read a single baml record.  If it is a defer key, add it to the table of
        * keys.  If we encounter something that is not a 'key', then set the
        *_haveUnprocessedRecord flag.
        *
        \***************************************************************************/

        private void ProcessDeferKey()
        {
            switch (_currentBamlRecord.RecordType)
            {
                // The following three records are internal to the BAMLReader and
                // are not exposed publicly.  They are used to update the map table
                // that maps ids to assemblies, types and attributes.
                case BamlRecordType.DefAttributeKeyString:
                    BamlDefAttributeKeyStringRecord stringKeyRecord = _currentBamlRecord as BamlDefAttributeKeyStringRecord;
                    if (stringKeyRecord != null)
                    {
                        BamlKeyInfo info;

                        // The "Shared"ness is stored in the BAML with the Key
                        // But at the XAML level it is a sibling attribute of the key.
                        info = CheckForSharedness();
                        if (null != info)
                            _deferKeys.Add(info);

                        // Get the value string from the string table, and cache it in the
                        // record.
                        stringKeyRecord.Value = MapTable.GetStringFromStringId(
                                                        stringKeyRecord.ValueId);

                        // Add information to the key list to indicate we have a x:Key
                        // attribute
                        info = new BamlKeyInfo();
                        info.Value = stringKeyRecord.Value;
                        info.AssemblyName = string.Empty;
                        info.Prefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
                        info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
                        info.ClrNamespace = string.Empty;
                        info.Name = XamlReaderHelper.DefinitionName;
                        info.LocalName = info.Name;
                        info.RecordType = BamlRecordType.DefAttribute;
                        info.Offset = ((IBamlDictionaryKey)stringKeyRecord).ValuePosition;
                        _deferKeys.Add(info);
                    }
                    break;

                case BamlRecordType.DefAttributeKeyType:
                    BamlDefAttributeKeyTypeRecord typeKeyRecord = _currentBamlRecord as BamlDefAttributeKeyTypeRecord;
                    if (typeKeyRecord != null)
                    {
                        // Translate the type information held in the baml record into
                        // the {x:Type prefix:Classname} format that would be used on
                        // a x:Key attribute.
                        string typeExtensionPrefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
                        string typeExtensionName;
                        if (typeExtensionPrefix != string.Empty)
                        {
                            typeExtensionName = "{" + typeExtensionPrefix + ":Type ";
                        }
                        else
                        {
                            typeExtensionName = "{Type ";
                        }
                        BamlTypeInfoRecord typeInfo = MapTable.GetTypeInfoFromId(typeKeyRecord.TypeId);
                        string typeName = typeInfo.TypeFullName;
                        typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
                        string assemblyName;
                        string prefix;
                        string xmlNamespace;
                        GetAssemblyAndPrefixAndXmlns(typeInfo, out assemblyName, out prefix, out xmlNamespace);
                        if (prefix != string.Empty)
                        {
                            typeName = typeExtensionName + prefix + ":" + typeName + "}";
                        }
                        else
                        {
                            typeName = typeExtensionName + typeName + "}";
                        }

                        // Add information to the key list to indicate we have a x:Key
                        // attribute
                        BamlKeyInfo info = new BamlKeyInfo();
                        info.Value = typeName;
                        info.AssemblyName = string.Empty;
                        info.Prefix = typeExtensionPrefix;
                        info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
                        info.ClrNamespace = string.Empty;
                        info.Name = XamlReaderHelper.DefinitionName;
                        info.LocalName = info.Name;
                        info.RecordType = BamlRecordType.DefAttribute;
                        info.Offset = ((IBamlDictionaryKey)typeKeyRecord).ValuePosition;
                        _deferKeys.Add(info);
                    }
                    break;

                case BamlRecordType.KeyElementStart:
                    {
                        BamlKeyInfo info;

                        // The "Shared"ness is stored in the BAML with the Key
                        // But at the XAML level it is a sibling attribute of the key.
                        info = CheckForSharedness();
                        if(null != info)
                            _deferKeys.Add(info);

                        // Process the subtree that is stored as part of a key tree and
                        // translate this back into a compact MarkupExtension string.
                        // Add information to the key list to indicate we have a x:Key
                        // with a MarkupExtension
                        info = ProcessKeyTree();
                        _deferKeys.Add(info);
                    }
                    break;

                case BamlRecordType.StaticResourceStart:
                case BamlRecordType.OptimizedStaticResource:
                    {
                        // Process the subtree stored as part of a StaticResource
                        List<BamlRecord> srRecords = new List<BamlRecord>();

                        // This is for the start record
                        _currentBamlRecord.Pin();
                        srRecords.Add(_currentBamlRecord);

                        // Note that BamlOptmizedStaticResourceRecord is a singleton record
                        if (_currentBamlRecord.RecordType == BamlRecordType.StaticResourceStart)
                        {
                            // Process the subtree that is stored as part of this static resource
                            ProcessStaticResourceTree(srRecords);
                        }

                        // Add the current StaticResource record to the list of StaticResources held per key
                        BamlKeyInfo keyInfo = _deferKeys[_deferKeys.Count-1];
                        keyInfo.StaticResources.Add(srRecords);
                    }
                    break;

                // Any other record types are not processed here
                default:
                    _haveUnprocessedRecord = true;
                    break;
            }
        }

        private BamlKeyInfo CheckForSharedness()
        {
            IBamlDictionaryKey dictKey = (IBamlDictionaryKey)_currentBamlRecord;
            Debug.Assert(dictKey != null, "Bad Key record");
            if (!dictKey.SharedSet)
                return null;

            BamlKeyInfo info = new BamlKeyInfo();
            info.Value = dictKey.Shared.ToString();
            info.AssemblyName = string.Empty;
            info.Prefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
            info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
            info.ClrNamespace = string.Empty;
            info.Name = XamlReaderHelper.DefinitionShared;
            info.LocalName = info.Name;
            info.RecordType = BamlRecordType.DefAttribute;
            info.Offset = dictKey.ValuePosition;

            return info;
        }

        /***************************************************************************\
        *
        * BamlReader.ProcessKeyTree
        *
        * Read a tree of baml records that make up a dictionary key and translate them
        * back in the compact syntax representation of a MarkupExtension section.
        * When we encounter KeyElementEnd record, then stop.
        *
        \***************************************************************************/

        private BamlKeyInfo ProcessKeyTree()
        {
            BamlKeyElementStartRecord keyStartRecord = _currentBamlRecord as BamlKeyElementStartRecord;
            Debug.Assert(keyStartRecord != null, "Bad Key Element Start record");

            // Translate the type information held in the baml record into
            // the "{prefix:Classname " format that would be used on
            // a x:Key attribute.
            BamlTypeInfoRecord typeInfo = MapTable.GetTypeInfoFromId(keyStartRecord.TypeId);
            string markupString = typeInfo.TypeFullName;
            markupString = markupString.Substring(markupString.LastIndexOf('.') + 1);
            string assemblyName;
            string prefix;
            string xmlNamespace;
            GetAssemblyAndPrefixAndXmlns(typeInfo, out assemblyName, out prefix, out xmlNamespace);
            if (prefix != string.Empty)
            {
                markupString = "{" + prefix + ":" + markupString + " ";
            }
            else
            {
                markupString = "{" + markupString + " ";
            }

            bool notDone = true;
            BamlNodeInfo nodeInfo;

            // Keep track of whether we have written a property or not at a given nesting
            // level so that we know when to add commas between properties.  Also keep
            // track of when we have entered a constructor parameter section and when
            // we have written out the first parameter to handle adding commas between
            // constructor parameters.
            Stack readProperty = new Stack();
            Stack readConstructor = new Stack();
            Stack readFirstConstructor = new Stack();
            readProperty.Push(false);         // Property has not yet been read
            readConstructor.Push(false);      // Constructor section has not been read
            readFirstConstructor.Push(false); // First constructor parameter has not been read

            while (notDone)
            {
                // Read the next record.  Some of the processing below reads ahead one
                // record and sets _haveUnprocessedRecord to true, in which case we
                // don't want to read another one.
                if (!_haveUnprocessedRecord)
                {
                    GetNextRecord();
                }
                else
                {
                    _haveUnprocessedRecord = false;
                }

                switch (_currentBamlRecord.RecordType)
                {
                    // The following five records are internal to the BAMLReader and
                    // are not exposed publicly.  They are used to update the map table
                    // that maps ids to assemblies, types and attributes.
                    case BamlRecordType.AssemblyInfo:
                        ReadAssemblyInfoRecord();
                        break;

                    case BamlRecordType.TypeInfo:
                    case BamlRecordType.TypeSerializerInfo:
                        MapTable.LoadTypeInfoRecord((BamlTypeInfoRecord)_currentBamlRecord);
                        break;

                    case BamlRecordType.AttributeInfo:
                        MapTable.LoadAttributeInfoRecord((BamlAttributeInfoRecord)_currentBamlRecord);
                        break;

                    case BamlRecordType.StringInfo:
                        MapTable.LoadStringInfoRecord((BamlStringInfoRecord)_currentBamlRecord);
                        break;


                    case BamlRecordType.PropertyComplexStart:
                        ReadPropertyComplexStartRecord();
                        nodeInfo = (BamlNodeInfo)_nodeStack.Pop();
                        if ((bool)readProperty.Pop())
                        {
                            markupString += ", ";
                        }
                        markupString += nodeInfo.LocalName + "=";
                        readProperty.Push(true);
                        break;

                    case BamlRecordType.PropertyComplexEnd:
                        break;

                    case BamlRecordType.Text:
                    case BamlRecordType.TextWithId:

                        BamlTextWithIdRecord textWithIdRecord = _currentBamlRecord as BamlTextWithIdRecord;
                        if (textWithIdRecord != null)
                        {
                            // Get the value string from the string table, and cache it in the
                            // record.
                            textWithIdRecord.Value = MapTable.GetStringFromStringId(
                                                            textWithIdRecord.ValueId);
                        }

                        // If the text contains '{' or '}' then we have to escape these
                        // so that it won't be interpreted as a MarkupExtension
                        string escapedString = EscapeString(((BamlTextRecord)_currentBamlRecord).Value);
                        if ((bool)readFirstConstructor.Peek())
                        {
                            markupString += ", ";
                        }
                        markupString += escapedString;
                        if ((bool)readConstructor.Peek())
                        {
                            readFirstConstructor.Pop();
                            readFirstConstructor.Push(true);
                        }
                        break;

                    case BamlRecordType.ElementStart:
                        // Process commas between constructor parameters
                        if ((bool)readFirstConstructor.Peek())
                        {
                            markupString += ", ";
                        }
                        if ((bool)readConstructor.Peek())
                        {
                            readFirstConstructor.Pop();
                            readFirstConstructor.Push(true);
                        }
                        // Setup for the next level
                        readProperty.Push(false);
                        readConstructor.Push(false);
                        readFirstConstructor.Push(false);
                        // Write element type. Translate the type information held in the
                        // baml record into the "prefix:Classname" format
                        BamlElementStartRecord elementStartRecord = _currentBamlRecord as BamlElementStartRecord;
                        BamlTypeInfoRecord elementTypeInfo = MapTable.GetTypeInfoFromId(elementStartRecord.TypeId);
                        string typename = elementTypeInfo.TypeFullName;
                        typename = typename.Substring(typename.LastIndexOf('.') + 1);
                        GetAssemblyAndPrefixAndXmlns(elementTypeInfo, out assemblyName, out prefix, out xmlNamespace);
                        if (prefix != string.Empty)
                        {
                            markupString += "{" + prefix + ":" + typename + " ";
                        }
                        else
                        {
                            markupString = "{" + typename + " ";
                        }
                        break;

                    case BamlRecordType.ElementEnd:
                        readProperty.Pop();
                        readConstructor.Pop();
                        readFirstConstructor.Pop();
                        markupString += "}";
                        break;

                    case BamlRecordType.ConstructorParametersStart:
                        readConstructor.Pop();
                        readConstructor.Push(true);
                        break;

                    case BamlRecordType.ConstructorParametersEnd:
                        readConstructor.Pop();
                        readConstructor.Push(false);
                        readFirstConstructor.Pop();
                        readFirstConstructor.Push(false);
                        break;

                    case BamlRecordType.ConstructorParameterType:
                        // Process commas between constructor parameters
                        if ((bool)readFirstConstructor.Peek())
                        {
                            markupString += ", ";
                        }
                        if ((bool)readConstructor.Peek())
                        {
                            readFirstConstructor.Pop();
                            readFirstConstructor.Push(true);
                        }
                        BamlConstructorParameterTypeRecord constTypeRecord = _currentBamlRecord as BamlConstructorParameterTypeRecord;
                        markupString += GetTypeValueString(constTypeRecord.TypeId);
                        break;

                    case BamlRecordType.Property:
                    case BamlRecordType.PropertyWithConverter:
                        {
                            string value = ((BamlPropertyRecord)_currentBamlRecord).Value;
                            BamlPropertyInfo propertyInfo = ReadPropertyRecordCore(value);
                            if ((bool)readProperty.Pop())
                            {
                                markupString += ", ";
                            }
                            markupString += propertyInfo.LocalName + "=" + propertyInfo.Value;
                            readProperty.Push(true);
                        }
                        break;

                    case BamlRecordType.PropertyCustom:
                        {
                            BamlPropertyInfo propertyInfo = GetPropertyCustomRecordInfo();
                            if ((bool)readProperty.Pop())
                            {
                                markupString += ", ";
                            }
                            markupString += propertyInfo.LocalName + "=" + propertyInfo.Value;
                            readProperty.Push(true);
                        }
                        break;

                    case BamlRecordType.PropertyStringReference:
                        {
                            string value = MapTable.GetStringFromStringId(((BamlPropertyStringReferenceRecord)_currentBamlRecord).StringId);
                            BamlPropertyInfo propertyInfo = ReadPropertyRecordCore(value);
                            if ((bool)readProperty.Pop())
                            {
                                markupString += ", ";
                            }
                            markupString += propertyInfo.LocalName + "=" + propertyInfo.Value;
                            readProperty.Push(true);
                        }
                        break;

                    case BamlRecordType.PropertyTypeReference:
                        {
                            string value = GetTypeValueString(((BamlPropertyTypeReferenceRecord)_currentBamlRecord).TypeId);
                            string attributeName = MapTable.GetAttributeNameFromId(
                                                          ((BamlPropertyTypeReferenceRecord)_currentBamlRecord).AttributeId);
                            if ((bool)readProperty.Pop())
                            {
                                markupString += ", ";
                            }
                            markupString += attributeName + "=" + value;
                            readProperty.Push(true);
                        }
                        break;

                    case BamlRecordType.PropertyWithExtension:
                        {
                            string value = GetExtensionValueString((BamlPropertyWithExtensionRecord)_currentBamlRecord);
                            string attributeName = MapTable.GetAttributeNameFromId(
                                                          ((BamlPropertyWithExtensionRecord)_currentBamlRecord).AttributeId);
                            if ((bool)readProperty.Pop())
                            {
                                markupString += ", ";
                            }
                            markupString += attributeName + "=" + value;
                            readProperty.Push(true);
                        }
                        break;

                    case BamlRecordType.KeyElementEnd:
                        markupString += "}";
                        notDone = false;
                        _haveUnprocessedRecord = false;
                        break;

                    default:
                        // Can't have any other type of record at this point.
                        throw new InvalidOperationException(SR.Get(SRID.ParserUnknownBaml,
                                         ((int)_currentBamlRecord.RecordType).ToString(CultureInfo.CurrentCulture)));
                }
            }

            // At this point the markup string representing the MarkupExtension should
            // be complete, so set this as the value for this key.
            BamlKeyInfo info = new BamlKeyInfo();
            info.Value = markupString;
            info.AssemblyName = string.Empty;
            info.Prefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
            info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
            info.ClrNamespace = string.Empty;
            info.Name = XamlReaderHelper.DefinitionName;
            info.LocalName = info.Name;
            info.RecordType = BamlRecordType.DefAttribute;
            info.Offset = ((IBamlDictionaryKey)keyStartRecord).ValuePosition;

            return info;
        }

        /// <summary>
        /// Picks up all the BamlRecords for a front loaded static resource into a
        /// list of BamlRecords.
        /// </summary>
        private void ProcessStaticResourceTree(List<BamlRecord> srRecords)
        {
            bool notDone = true;
            while (notDone)
            {
                // We may already have a record that was previously read in but not
                // processed.  This occurs when we've looped through all the properties
                // on an element and have encountered a non-property record to stop
                // the loop.  In that case don't read another record and just process
                // the one we have.
                if (_haveUnprocessedRecord)
                {
                    _haveUnprocessedRecord = false;
                }
                else
                {
                    GetNextRecord();
                }

                // Remember the BamlRecords beloning to this StaticResource
                _currentBamlRecord.Pin();
                srRecords.Add(_currentBamlRecord);

                if (_currentBamlRecord.RecordType == BamlRecordType.StaticResourceEnd)
                {
                    notDone = false;
                }
            }
        }

        /// <summary>
        /// Picks up the list of BamlRecords in the deferred
        /// section corresponding to this StaticResourceId
        /// </summary>
        private void ReadStaticResourceId()
        {
            BamlStaticResourceIdRecord bamlRecord = (BamlStaticResourceIdRecord)_currentBamlRecord;
            _currentStaticResourceRecords = _currentKeyInfo.StaticResources[bamlRecord.StaticResourceId];
            _currentStaticResourceRecordIndex = 0;
        }

        /***************************************************************************\
        *
        * BamlReader.EscapeString
        *
        * Check for '{' and '}' and escape any that are found in the passed value.
        * Don't create a new string unless you have to.
        *
        \***************************************************************************/

        private string EscapeString(string value)
        {
            StringBuilder builder = null;
            for (int i=0; i<value.Length; i++)
            {
                if (value[i] == '{' || value[i] == '}')
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(value.Length+2);
                        builder.Append(value,0,i);
                    }
                    builder.Append('\\');
                }
                if (builder != null)
                {
                    builder.Append(value[i]);
                }
            }

            if (builder == null)
            {
                return value;
            }
            else
            {
                return builder.ToString();
            }
        }

        /***************************************************************************\
        *
        * BamlReader.ReadRoutedEventRecord
        *
        * Read a routed event record.  These are currently not stored in the
        * BAML stream, but are handled by code generated by the compiler.
        *
        \***************************************************************************/

        private void ReadRoutedEventRecord()
        {
            throw new InvalidOperationException(SR.Get(SRID.ParserBamlEvent, string.Empty));
        }

        /***************************************************************************\
        *
        * BamlReader.ReadClrEventRecord
        *
        * Read a clr event record.  These are currently not stored in the
        * BAML stream, but are handled by code generated by the compiler.
        *
        \***************************************************************************/

        private void ReadClrEventRecord()
        {
            throw new InvalidOperationException(SR.Get(SRID.ParserBamlEvent, string.Empty));
        }

        /***************************************************************************\
        *
        * BamlReader.ReadDocumentStartRecord
        *
        * Read the start of the document record.  This should contain some
        * version information.
        *
        \***************************************************************************/

        private void ReadDocumentStartRecord()
        {
            ClearProperties();
            NodeTypeInternal = BamlNodeType.StartDocument;

            BamlDocumentStartRecord documentStartRecord = (BamlDocumentStartRecord)_currentBamlRecord;
            _parserContext.IsDebugBamlStream = documentStartRecord.DebugBaml;

            // Push information on the node stack to indicate we have a start document
            BamlNodeInfo nodeInfo = new BamlNodeInfo();
            nodeInfo.RecordType = BamlRecordType.DocumentStart;
            _nodeStack.Push(nodeInfo);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadDocumentEndRecord
        *
        * Read the end of the document record.  This is used to flag that the end
        * of the file has been reached.
        *
        \***************************************************************************/

        private void ReadDocumentEndRecord()
        {
            // Pop information off the node stack to ensure we have matched all the
            // start and end nodes and have nothing left but the start document node.
            BamlNodeInfo nodeInfo = (BamlNodeInfo)_nodeStack.Pop();
            if (nodeInfo.RecordType != BamlRecordType.DocumentStart)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlScopeError,
                                                    nodeInfo.RecordType.ToString(),
                                                    "DocumentEnd"));
            }

            ClearProperties();
            NodeTypeInternal = BamlNodeType.EndDocument;
        }

        private void ReadAssemblyInfoRecord()
        {
            BamlAssemblyInfoRecord asmRecord = (BamlAssemblyInfoRecord)_currentBamlRecord;
            MapTable.LoadAssemblyInfoRecord(asmRecord);

            Assembly asm = Assembly.Load(asmRecord.AssemblyFullName);
            foreach (XmlnsDefinitionAttribute xmlnsDef in asm.GetCustomAttributes(typeof(XmlnsDefinitionAttribute), true))
            {
                SetXmlNamespace(xmlnsDef.ClrNamespace, asm.FullName, xmlnsDef.XmlNamespace);
            }
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPIMappingRecord
        *
        * Read the clr to xml namespace to assembly mapping record.  The contents
        * of this record are represented as three properties, one each for
        * XmlNamespace, ClrNamespace and Assembly Name.
        *
        \***************************************************************************/

        private void ReadPIMappingRecord()
        {
            BamlPIMappingRecord piMappingRecord = (BamlPIMappingRecord)_currentBamlRecord;
            BamlAssemblyInfoRecord assemblyInfo = MapTable.GetAssemblyInfoFromId(
                                                                  piMappingRecord.AssemblyId);
            if (assemblyInfo == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.ParserMapPIMissingAssembly));
            }

            // If this mapping has not already been set up, then set it now
            if (!_parserContext.XamlTypeMapper.PITable.Contains(piMappingRecord.XmlNamespace))
            {
                // Add information to the MappingPI hashtable and the reverse lookup
                // hashtable.
                _parserContext.XamlTypeMapper.AddMappingProcessingInstruction(piMappingRecord.XmlNamespace,
                                                   piMappingRecord.ClrNamespace,
                                                   assemblyInfo.AssemblyFullName);
            }

            ClearProperties();
            NodeTypeInternal = BamlNodeType.PIMapping;
            _name = "Mapping";
            _localName = _name;
            _ownerTypeName = string.Empty;

            // Set the xml namespace, clr namespace and assembly properties as defined
            // in the mapping PI.
            _xmlNamespace = piMappingRecord.XmlNamespace;
            _clrNamespace = piMappingRecord.ClrNamespace;
            _assemblyName = assemblyInfo.AssemblyFullName;

            StringBuilder valueBuilder = new StringBuilder(100);
            valueBuilder.Append("XmlNamespace=\"");
            valueBuilder.Append(_xmlNamespace);
            valueBuilder.Append("\" ClrNamespace=\"");
            valueBuilder.Append(_clrNamespace);
            valueBuilder.Append("\" Assembly=\"");
            valueBuilder.Append(_assemblyName);
            valueBuilder.Append("\"");
            _value = valueBuilder.ToString();
        }

        /***************************************************************************\
        *
        * BamlReader.ReadLiteralContentRecord
        *
        * Read literal content record, which is the responsibility of the current
        * element to parse.
        *
        \***************************************************************************/

        private void ReadLiteralContentRecord()
        {
            ClearProperties();

            BamlLiteralContentRecord bamlRecord = (BamlLiteralContentRecord)_currentBamlRecord;
            NodeTypeInternal = BamlNodeType.LiteralContent;
            _value = bamlRecord.Value;
        }

        private void ReadConnectionIdRecord()
        {
            BamlConnectionIdRecord bamlRecord = (BamlConnectionIdRecord)_currentBamlRecord;
            AddToPropertyInfoCollection(bamlRecord.ConnectionId);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadElementStartRecord
        *
        * Read the start of an element.  This is either a CLR or DependencyObject
        * that is part of an object tree.
        *
        \***************************************************************************/

        private void ReadElementStartRecord()
        {
            ClearProperties();
            _propertyDP = null;
            _parserContext.PushScope();
            _prefixDictionary.PushScope();

            BamlElementStartRecord bamlRecord = (BamlElementStartRecord)_currentBamlRecord;
            BamlTypeInfoRecord typeInfo = MapTable.GetTypeInfoFromId(bamlRecord.TypeId);

            NodeTypeInternal = BamlNodeType.StartElement;
            _name = typeInfo.TypeFullName;
            _localName = _name.Substring(_name.LastIndexOf('.') + 1);
            _ownerTypeName = string.Empty;
            _clrNamespace = typeInfo.ClrNamespace;
            GetAssemblyAndPrefixAndXmlns(typeInfo, out _assemblyName, out _prefix, out _xmlNamespace);

            // Push information on the node stack to indicate we have a start element
            BamlNodeInfo nodeInfo = new BamlNodeInfo();
            nodeInfo.Name = _name;
            nodeInfo.LocalName = _localName;
            nodeInfo.AssemblyName = _assemblyName;
            nodeInfo.Prefix = _prefix;
            nodeInfo.ClrNamespace = _clrNamespace;
            nodeInfo.XmlNamespace = _xmlNamespace;
            nodeInfo.RecordType = BamlRecordType.ElementStart;

            _useTypeConverter = bamlRecord.CreateUsingTypeConverter;
            _isInjected = bamlRecord.IsInjected;

            // If we are in a deferable block, then see if this is a top level element for
            // that block that matches an offset in the list of defered dictionary keys.  If
            // so, then insert a x:Key="keystring" to make this appear like a normal
            // dictionary.
            if (_deferableContentBlockDepth == _nodeStack.Count)
            {
                // Calculate the offset for the start of the current element record in
                // the baml stream.
                Int32 offset = (Int32)(_bamlRecordReader.StreamPosition - _deferableContentPosition);

                // Subtract off the size of the current Record.
                offset -= bamlRecord.RecordSize + BamlRecord.RecordTypeFieldLength;

                // If there is a debug extension record then subtract that off also.
                if (BamlRecordHelper.HasDebugExtensionRecord(_parserContext.IsDebugBamlStream, bamlRecord))
                {
                    BamlRecord bamlDebugRecord = bamlRecord.Next;
                    offset -= bamlDebugRecord.RecordSize + BamlRecord.RecordTypeFieldLength;
                }
                InsertDeferedKey(offset);
            }

            _nodeStack.Push(nodeInfo);

            // Read the properties that may be part of the start tag of this element
            ReadProperties();
        }

        /***************************************************************************\
        *
        * BamlReader.ReadElementEndRecord
        *
        * Read the end of an element.  This is either a CLR or DependencyObject
        * that is part of an object tree.
        *
        \***************************************************************************/

        private void ReadElementEndRecord()
        {
            // If we are processing a deferable content block and we've reached the
            // end record for the deferable element, then pop off the deferable content
            // start record that is on the stack.
            if (_deferableContentBlockDepth == _nodeStack.Count)
            {
                _deferableContentBlockDepth = -1;
                _deferableContentPosition = -1;
            }

            // Pop information off the node stack that tells us what element this
            // is the end of.  Check to make sure the record on the stack is for a
            // start element.
            BamlNodeInfo nodeInfo = (BamlNodeInfo)_nodeStack.Pop();
            if (nodeInfo.RecordType != BamlRecordType.ElementStart)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlScopeError,
                                                 _currentBamlRecord.RecordType.ToString(),
                                                 BamlRecordType.ElementEnd.ToString()));
            }

            ClearProperties();
            NodeTypeInternal = BamlNodeType.EndElement;
            _name = nodeInfo.Name;
            _localName = nodeInfo.LocalName;
            _ownerTypeName = string.Empty;
            _assemblyName = nodeInfo.AssemblyName;
            _prefix = nodeInfo.Prefix;
            _xmlNamespace = nodeInfo.XmlNamespace;
            _clrNamespace = nodeInfo.ClrNamespace;
            _parserContext.PopScope();
            _prefixDictionary.PopScope();

            // read properties, if any, after this end tag.
            ReadProperties();
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyComplexStartRecord
        *
        * Read the start of a complex property.  This can be any type of complex
        * property, including arrays, ILists, IDictionaries, Clr properties or
        * dependency properties.
        *
        \***************************************************************************/

        private void ReadPropertyComplexStartRecord()
        {
            ClearProperties();
            _parserContext.PushScope();
            _prefixDictionary.PushScope();

            BamlNodeInfo nodeInfo = new BamlNodeInfo();

            SetCommonPropertyInfo(nodeInfo,
                      ((BamlPropertyComplexStartRecord)_currentBamlRecord).AttributeId);

            // Set instance variables to node info extracted from record.
            NodeTypeInternal = BamlNodeType.StartComplexProperty;
            _localName = nodeInfo.LocalName;
            int index = nodeInfo.Name.LastIndexOf('.');
            if (index > 0)
            {
                _ownerTypeName = nodeInfo.Name.Substring(0, index);
            }
            else
            {
                // Eg. xmlns property
                _ownerTypeName = string.Empty;
            }
            _name = nodeInfo.Name;
            _clrNamespace = nodeInfo.ClrNamespace;
            _assemblyName = nodeInfo.AssemblyName;
            _prefix = nodeInfo.Prefix;
            _xmlNamespace = nodeInfo.XmlNamespace;
            nodeInfo.RecordType = _currentBamlRecord.RecordType;


            _nodeStack.Push(nodeInfo);

            // Read the properties that may be part of the start tag
            ReadProperties();
        }

        /***************************************************************************\
        *
        * BamlReader.ReadPropertyComplexEndRecord
        *
        * Read the end of a complex property.  This can be any type of complex
        * property, including arrays, ILists, IDictionaries, Clr properties or
        * dependency properties.
        *
        \***************************************************************************/

        private void ReadPropertyComplexEndRecord()
        {
            // Pop information off the node info stack that tells us what the starting
            // record was for this ending record.  Check to make sure it is the
            // correct type.  If not, throw an exception.
            BamlNodeInfo nodeInfo = (BamlNodeInfo)_nodeStack.Pop();
            BamlRecordType expectedType;
            switch (nodeInfo.RecordType)
            {
                case BamlRecordType.PropertyComplexStart:
                    expectedType = BamlRecordType.PropertyComplexEnd;
                    break;
                case BamlRecordType.PropertyArrayStart:
                    expectedType = BamlRecordType.PropertyArrayEnd;
                    break;
                case BamlRecordType.PropertyIListStart:
                    expectedType = BamlRecordType.PropertyIListEnd;
                    break;
                case BamlRecordType.PropertyIDictionaryStart:
                    expectedType = BamlRecordType.PropertyIDictionaryEnd;
                    break;
                default:
                    expectedType = BamlRecordType.Unknown;
                    break;
            }

            if (_currentBamlRecord.RecordType != expectedType)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlScopeError,
                                          _currentBamlRecord.RecordType.ToString(),
                                          expectedType.ToString()));
            }

            ClearProperties();
            NodeTypeInternal = BamlNodeType.EndComplexProperty;
            _name = nodeInfo.Name;
            _localName = nodeInfo.LocalName;
            int index = nodeInfo.Name.LastIndexOf('.');
            if (index > 0)
            {
                _ownerTypeName = nodeInfo.Name.Substring(0, index);
            }
            else
            {
                // Eg. xmlns property
                _ownerTypeName = string.Empty;
            }
            _assemblyName = nodeInfo.AssemblyName;
            _prefix = nodeInfo.Prefix;
            _xmlNamespace = nodeInfo.XmlNamespace;
            _clrNamespace = nodeInfo.ClrNamespace;
            _parserContext.PopScope();
            _prefixDictionary.PopScope();

            ReadProperties();
        }

        /***************************************************************************\
        *
        * BamlReader.ReadTextRecord
        *
        * Read record containing text content that goes between the start and end
        * tags of an object.
        *
        \***************************************************************************/

        private void ReadTextRecord()
        {
            ClearProperties();

            BamlTextWithIdRecord textWithIdRecord = _currentBamlRecord as BamlTextWithIdRecord;
            if (textWithIdRecord != null)
            {
                // Get the value string from the string table, and cache it in the
                // record.
                textWithIdRecord.Value = MapTable.GetStringFromStringId(
                                                textWithIdRecord.ValueId);
            }

            BamlTextWithConverterRecord textWithConverter = _currentBamlRecord as BamlTextWithConverterRecord;
            if (textWithConverter != null)
            {
                short converterTypeId = textWithConverter.ConverterTypeId;
                Type converter = MapTable.GetTypeFromId(converterTypeId);
                _typeConverterAssemblyName = converter.Assembly.FullName;
                _typeConverterName = converter.FullName;
            }

            NodeTypeInternal = BamlNodeType.Text;
            _prefix = string.Empty;
            _value = ((BamlTextRecord)_currentBamlRecord).Value;
        }

        /***************************************************************************\
        *
        * BamlReader.ReadConstructorStart
        *
        * Read a <x:ConstructorParameters   ...   > start tag, which indicates that
        * the following objects are to be used as constructor parameters.
        *
        \***************************************************************************/

        private void ReadConstructorStart()
        {
            ClearProperties();

            NodeTypeInternal = BamlNodeType.StartConstructor;

            // Push information on the node stack to indicate we have a start array
            BamlNodeInfo nodeInfo = new BamlNodeInfo();
            nodeInfo.RecordType = BamlRecordType.ConstructorParametersStart;

            _nodeStack.Push(nodeInfo);
        }

        /***************************************************************************\
        *
        * BamlReader.ReadConstructorEnd
        *
        * Read a <\x:ConstructorParameters   ...   > end tag, which indicates that
        * the previous objects are to be used as constructor parameters.
        *
        \***************************************************************************/

        private void ReadConstructorEnd()
        {
            ClearProperties();

            NodeTypeInternal = BamlNodeType.EndConstructor;

            // Pop information off the node stack that tells us what element this
            // is the end of.  Check to make sure the record on the stack is for a
            // start element.
            BamlNodeInfo nodeInfo = (BamlNodeInfo)_nodeStack.Pop();
            if (nodeInfo.RecordType != BamlRecordType.ConstructorParametersStart)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlScopeError,
                                                 _currentBamlRecord.RecordType.ToString(),
                                                 BamlRecordType.ConstructorParametersEnd.ToString()));
            }

            // read properties, if any, after this end tag.
            ReadProperties();
        }

        /***************************************************************************\
        *
        * BamlReader.InsertDeferedKey
        *
        * Search the _deferedKeys list for a dictionary key that has the same offset
        * as the current baml stream position.  If one is found, generate a
        * def attribute record to simulate a dictionary key.
        *
        \***************************************************************************/

        private void InsertDeferedKey(Int32 valueOffset)
        {
            if (_deferKeys == null)
            {
                return;
            }

            BamlKeyInfo keyInfo = _deferKeys[0];
            while (keyInfo.Offset == valueOffset)
            {
                // Remember the _currentKeyInfo so that we can use it to resolve StaticResourceId
                // records that may occur within the corresponding value.
                _currentKeyInfo = keyInfo;

                BamlPropertyInfo info = new BamlPropertyInfo();
                info.Value = keyInfo.Value;
                info.AssemblyName = string.Empty;
                info.Prefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
                info.XmlNamespace = XamlReaderHelper.DefinitionNamespaceURI;
                info.ClrNamespace = string.Empty;
                info.Name = keyInfo.Name;
                info.LocalName = info.Name;
                info.RecordType = BamlRecordType.DefAttribute;

                AddToPropertyInfoCollection(info);

                // We no longer need this key record, so remove it to make subsequent
                // searches faster.
                _deferKeys.RemoveAt(0);

                if (_deferKeys.Count > 0)
                {
                    keyInfo = _deferKeys[0];
                }
                else
                {
                    return;
                }
            }
        }

        /***************************************************************************\
        *
        * BamlReader.ClearProperties
        *
        * Clear properties that are likely to change as different baml records
        * are read in.
        *
        \***************************************************************************/

        private void ClearProperties()
        {
            _value = string.Empty;
            _prefix = string.Empty;
            _name = string.Empty;
            _localName = string.Empty;
            _ownerTypeName = string.Empty;
            _assemblyName = string.Empty;
            _xmlNamespace = string.Empty;
            _clrNamespace = string.Empty;
            _connectionId = 0;
            _contentPropertyName = string.Empty;
            _attributeUsage = BamlAttributeUsage.Default;
            _typeConverterAssemblyName = string.Empty;
            _typeConverterName = string.Empty;
            _properties.Clear();
        }

        /***************************************************************************\
        *
        * BamlReader.SetCommonPropertyInfo
        *
        * Get information that is common to all types of property records and
        * fill in the passed node info record with this information.
        * Return the attribute info found.
        *
        \***************************************************************************/

        private BamlAttributeInfoRecord SetCommonPropertyInfo(
            BamlNodeInfo       nodeInfo,
            short              attrId)
        {
            BamlAttributeInfoRecord attrInfo = MapTable.GetAttributeInfoFromId(attrId);
            BamlTypeInfoRecord typeInfo = MapTable.GetTypeInfoFromId(attrInfo.OwnerTypeId);

            // Fill node info record with this data.
            nodeInfo.LocalName = attrInfo.Name;
            nodeInfo.Name = typeInfo.TypeFullName + "." + nodeInfo.LocalName;
            string assembly, prefix, namespaceUri;
            GetAssemblyAndPrefixAndXmlns(typeInfo, out assembly, out prefix, out namespaceUri);
            nodeInfo.AssemblyName = assembly;
            nodeInfo.Prefix = prefix;
            nodeInfo.XmlNamespace = namespaceUri;
            nodeInfo.ClrNamespace = typeInfo.ClrNamespace;
            nodeInfo.AttributeUsage = attrInfo.AttributeUsage;

            return attrInfo;
        }

        private string GetTemplateBindingExtensionValueString(short memberId)
        {
            string valueString = string.Empty;
            string valuePrefix = null;
            string typeName = null;
            string propName = null;

            if (memberId < 0)
            {
                memberId = (short)-memberId;
                DependencyProperty dp = null;
                if (memberId < (short)KnownProperties.MaxDependencyProperty)
                {
                    KnownProperties knownId = (KnownProperties)(memberId);
                    {
                        dp = KnownTypes.GetKnownDependencyPropertyFromId(knownId);
                    }
                }

                if (dp == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.BamlBadExtensionValue));
                }
                else
                {
                    typeName = dp.OwnerType.Name;
                    propName = dp.Name;
                }

                object prefixObject = _prefixDictionary[XamlReaderHelper.DefaultNamespaceURI];
                valuePrefix = (prefixObject == null) ? string.Empty : (string)prefixObject;
            }
            else
            {
                BamlAttributeInfoRecord attrInfo = MapTable.GetAttributeInfoFromId(memberId);
                BamlTypeInfoRecord valueTypeInfo = MapTable.GetTypeInfoFromId(attrInfo.OwnerTypeId);
                string valueXmlNamespace;
                string valueAssemblyName;
                GetAssemblyAndPrefixAndXmlns(valueTypeInfo, out valueAssemblyName, out valuePrefix, out valueXmlNamespace);
                typeName = valueTypeInfo.TypeFullName;
                typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
                propName = attrInfo.Name;
            }

            if (valuePrefix == string.Empty)
            {
                valueString += typeName;
            }
            else
            {
                valueString += valuePrefix + ":" + typeName;
            }

            valueString += "." + propName + "}";
            return valueString;
        }

        private string GetStaticExtensionValueString(short memberId)
        {
            string valueString = string.Empty;
            string valuePrefix = null;
            string typeName = null;
            string propName = null;
            string extensionPrefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];

            if (extensionPrefix != string.Empty)
            {
                valueString = "{" + extensionPrefix + ":Static ";
            }
            else
            {
                valueString = "{Static ";
            }

            if (memberId < 0)
            {
                memberId = (short)-memberId;
                bool isKey = true;

                // this is a known StaticExtension param.
                // if keyId is more than the range it is the actual resource,
                // else it is the key.

                memberId = SystemResourceKey.GetSystemResourceKeyIdFromBamlId(memberId, out isKey);

                if (Enum.IsDefined(typeof(SystemResourceKeyID), (int)memberId))
                {
                    SystemResourceKeyID keyId = (SystemResourceKeyID)memberId;
                    typeName = SystemKeyConverter.GetSystemClassName(keyId);

                    if (isKey)
                    {
                        propName = SystemKeyConverter.GetSystemKeyName(keyId);
                    }
                    else
                    {
                        propName = SystemKeyConverter.GetSystemPropertyName(keyId);
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.BamlBadExtensionValue));
                }

                object prefixObject = _prefixDictionary[XamlReaderHelper.DefaultNamespaceURI];
                valuePrefix = (prefixObject == null) ? string.Empty : (string)prefixObject;
            }
            else
            {
                BamlAttributeInfoRecord attrInfo = MapTable.GetAttributeInfoFromId(memberId);
                BamlTypeInfoRecord valueTypeInfo = MapTable.GetTypeInfoFromId(attrInfo.OwnerTypeId);
                string valueXmlNamespace;
                string valueAssemblyName;
                GetAssemblyAndPrefixAndXmlns(valueTypeInfo, out valueAssemblyName, out valuePrefix, out valueXmlNamespace);
                typeName = valueTypeInfo.TypeFullName;
                typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
                propName = attrInfo.Name;
            }

            if (valuePrefix == string.Empty)
            {
                valueString += typeName;
            }
            else
            {
                valueString += valuePrefix + ":" + typeName;
            }

            valueString += "." + propName + "}";
            return valueString;
        }

        private string GetExtensionPrefixString(string extensionName)
        {
            string valueString = string.Empty;
            string extensionPrefix = (string)_prefixDictionary[XamlReaderHelper.DefaultNamespaceURI];

            if (!string.IsNullOrEmpty(extensionPrefix))
            {
                valueString = "{" + extensionPrefix + ":" + extensionName + " ";
            }
            else
            {
                valueString = "{" + extensionName + " ";
            }

            return valueString;
        }

        private string GetInnerExtensionValueString(IOptimizedMarkupExtension optimizedMarkupExtensionRecord)
        {
            string valueString = string.Empty;
            short memberId = optimizedMarkupExtensionRecord.ValueId;

            if (optimizedMarkupExtensionRecord.IsValueTypeExtension)
            {
                valueString = GetTypeValueString(memberId);
            }
            else if (optimizedMarkupExtensionRecord.IsValueStaticExtension)
            {
                valueString = GetStaticExtensionValueString(memberId);
            }
            else
            {
                valueString = MapTable.GetStringFromStringId(memberId);
            }

            return valueString + "}";
        }

        private string GetExtensionValueString(IOptimizedMarkupExtension optimizedMarkupExtensionRecord)
        {
            string valueString = string.Empty;
            short memberId = optimizedMarkupExtensionRecord.ValueId;
            short extensionId = optimizedMarkupExtensionRecord.ExtensionTypeId;

            switch (extensionId)
            {
                case (short)KnownElements.StaticExtension:
                    valueString = GetStaticExtensionValueString(memberId);
                    break;

                case (short)KnownElements.TemplateBindingExtension:
                    valueString = GetExtensionPrefixString("TemplateBinding");
                    valueString += GetTemplateBindingExtensionValueString(memberId);
                    break;

                case (short)KnownElements.DynamicResourceExtension:
                    valueString = GetExtensionPrefixString("DynamicResource");
                    valueString += GetInnerExtensionValueString(optimizedMarkupExtensionRecord);
                    break;

                case (short)KnownElements.StaticResourceExtension:
                    valueString = GetExtensionPrefixString("StaticResource");
                    valueString += GetInnerExtensionValueString(optimizedMarkupExtensionRecord);
                    break;
            }

            return valueString;
        }

        /***************************************************************************\
        *
        * BamlReader.GetTypeValueString
        *
        * Construct a MarkupExtension that represents the type given its ID in the
        * BamlMapTable.
        *
        \***************************************************************************/

        private string GetTypeValueString(short typeId)
        {
            string typeExtensionPrefix = (string)_prefixDictionary[XamlReaderHelper.DefinitionNamespaceURI];
            string valueString;
            if (typeExtensionPrefix != string.Empty)
            {
                valueString = "{" + typeExtensionPrefix + ":Type ";
            }
            else
            {
                valueString = "{Type ";
            }

            BamlTypeInfoRecord valueTypeInfo = MapTable.GetTypeInfoFromId(typeId);
            string valueXmlNamespace;
            string valuePrefix;
            string valueAssemblyName;
            GetAssemblyAndPrefixAndXmlns(valueTypeInfo, out valueAssemblyName, out valuePrefix, out valueXmlNamespace);
            string typeName = valueTypeInfo.TypeFullName;
            typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
            if (valuePrefix == string.Empty)
            {
                valueString += typeName;
            }
            else
            {
                valueString += valuePrefix + ":" + typeName;
            }
            valueString +="}";

            return valueString;
        }

        /***************************************************************************\
        *
        * BamlReader.GetAssemblyAndPrefixAndXmlns
        *
        * Get a namespace prefix and the associated Xml namespace for a given type.
        * If none, return empty strings.
        *
        \***************************************************************************/

        private void GetAssemblyAndPrefixAndXmlns(
                BamlTypeInfoRecord typeInfo,
            out string assemblyFullName,
            out string prefix,
            out string xmlns)
        {
            // If the typeInfo indicates the type is NOT a core Avalon type, then the
            // assembly should be in the Assembly table of the BamlMapTable.  Otherwise
            // we have to get the Assembly information from the actual type.
            if (typeInfo.AssemblyId >= 0 || typeInfo.Type == null)
            {
                BamlAssemblyInfoRecord assyInfo = MapTable.GetAssemblyInfoFromId(
                                                                     typeInfo.AssemblyId);
                assemblyFullName = assyInfo.AssemblyFullName;
            }
            else
            {
                Assembly typeAssembly = typeInfo.Type.Assembly;
                assemblyFullName = typeAssembly.FullName;
            }

            // Look through the mapping table for an xml namespace that matches
            // the assembly and type namespace.
            // Note that it may be one of the known namespaces, such as the definition
            // namespace so check for that first.
            if (typeInfo.ClrNamespace == "System.Windows.Markup" &&
                (assemblyFullName.StartsWith("PresentationFramework", StringComparison.Ordinal)
                || assemblyFullName.StartsWith("System.Xaml", StringComparison.Ordinal)))
            {
                xmlns = XamlReaderHelper.DefinitionNamespaceURI;
            }
            else
            {
                // XamlTypeMapper only stored MappingPI Xml Namesaces
                xmlns = _parserContext.XamlTypeMapper.GetXmlNamespace(
                          typeInfo.ClrNamespace, assemblyFullName);

                // Now check our own private list for URI based Xml Namespaces
                if(String.IsNullOrEmpty(xmlns))
                {
                    List<String> xmlnsList = GetXmlNamespaceList(typeInfo.ClrNamespace, assemblyFullName);
                    prefix = GetXmlnsPrefix(xmlnsList);
                    return;
                }
            }

            prefix = GetXmlnsPrefix(xmlns);
        }


        // store the all XmlNs UIRs that map to each CLRNamespace + AssemblyName
        private void SetXmlNamespace(string clrNamespace, string assemblyFullName, string xmlNs)
        {
            String fullName = clrNamespace + "#" + assemblyFullName;
            List<String> list;
            if(_reverseXmlnsTable.ContainsKey(fullName))
            {
                list = _reverseXmlnsTable[fullName];
            }
            else
            {
                list = new List<String>();
                _reverseXmlnsTable[fullName] = list;
            }
            list.Add(xmlNs);
        }


        // Retrieve the XmlNs UIRs that map to a CLRNamespace + AssemblyName
        private List<String> GetXmlNamespaceList(string clrNamespace, string assemblyFullName)
        {
            String fullName = clrNamespace + "#" + assemblyFullName;
            List<String> xmlnsList=null;

            if (_reverseXmlnsTable.ContainsKey(fullName))
            {
                xmlnsList = _reverseXmlnsTable[fullName];
            }
            return xmlnsList;
        }


        internal string GetXmlnsPrefix(string xmlns)
        {
            string prefix = string.Empty;

            // If we don't find an xmlns, then the clr namespace must be in the
            // default definition file group for this file.  Otherwise, lookup the
            // prefix in the xmlns-to-prefix dictionary built up in ReadXmlnsProperty
            if (xmlns == string.Empty)
            {
                xmlns = _parserContext.XmlnsDictionary[string.Empty];
            }
            else
            {
                object prefixObject = _prefixDictionary[xmlns];
                // If there is nothing in the prefix dictionary for this namespace,
                // then assume this is the default namespace and set prefix to
                // an empty string.
                if (prefixObject != null)
                {
                    prefix = (string)prefixObject;
                }
            }

            return prefix;
        }

        private string GetXmlnsPrefix(List<String> xmlnsList)
        {
            string prefix;
            string xmlns;

            if (xmlnsList != null)
            {
                // return the first non-null prefix defined.
                // the default prefix is "" and is non-null.
                for (int i=0; i<xmlnsList.Count; i++)
                {
                    xmlns = xmlnsList[i];
                    prefix = _prefixDictionary[xmlns];
                    if(prefix != null)
                        return prefix;
                }
            }
            return String.Empty;   // and error actually but old code defaulted this way.
        }

        /***************************************************************************\
        *
        * BamlReader.MapTable
        *
        * Get value of map table from the parser context.
        *
        \***************************************************************************/

        private BamlMapTable MapTable
        {
            get { return _parserContext.MapTable; }
        }

        #endregion Internal Methods

        #region Data

        // The BamlRecordReader that is handling getting records from the baml stream
        private BamlRecordReader _bamlRecordReader;

        // Dictionary with XML namespaces as keys and prefixes as values.  This is
        // the same information kept in _parserContext.XmlnsDictionary, but we lookup
        // by Xmlns more often, so it is more efficient to keep another dictionary.
        private XmlnsDictionary _prefixDictionary;

        // The last record read in from the _bamlRecordReader
        private BamlRecord _currentBamlRecord;

        // The _currentBamlRecord is valid, but has not been processed yet.
        private bool _haveUnprocessedRecord;

        // Stack depth where a deferable content block starts.  -1 if not in deferable content.
        private int _deferableContentBlockDepth;

        // The position in the stream where deferable content values begin
        private Int64 _deferableContentPosition;

        // List of keys for a deferable content dictionary.  These are arranged at the front
        // of the deferable content block and are read into a list before processing the
        // values for the dictionary.
        private List<BamlKeyInfo> _deferKeys;

        // Info for the key being currently read.
        private BamlKeyInfo _currentKeyInfo;

        // The currently active StaticResourceInfo
        private List<BamlRecord> _currentStaticResourceRecords;
        private int              _currentStaticResourceRecordIndex;

        // The type of current BAML node (or a condensed version of the real baml record type)
        private BamlNodeType _bamlNodeType;

        // The current read state of this BamlReader.
        private ReadState _readState;

        // The value of the assembly name for the current node
        private string _assemblyName;

        // The value of the namespace prefix for the current node
        private string _prefix;

        // Xml namespace for the current node;
        private string _xmlNamespace;

        // Clr namespace for the current node;
        private string _clrNamespace;

        // The attribute value for the current node, if a property or namespace
        private string _value;

        // The fully qualified name of the current node
        private string _name;

        // The local part of the name of the current node, without class or prefix
        private string _localName;

        // The fully qualified class name of the current node's owner type, without prefix
        // Applies only to properties and events
        private string _ownerTypeName;

        // Arraylist of various PropertyInfo objects in the order retrieved from the baml stream
        private ArrayList _properties;

        // DP value of a property. If this is set the type of this property is used to resolve the
        // value of any subsequent property. Setter.Property & Setter.Value is an example of this sceanrio.
        private DependencyProperty _propertyDP;

        // Index of current property in _properties collection that is being viewed.
        private int _propertiesIndex;

        // connection Id of current element for hooking up IDs and events.
        private Int32 _connectionId;

        // contentProperty Name of current element.
        private string _contentPropertyName;

        // Defines what this property is used for such as being an alias for
        // xml:lang, xml:space or x:ID
        private BamlAttributeUsage _attributeUsage;

        // Stack of node information about the element tree being built.
        private Stack _nodeStack;

        // Context information used when reading baml file.  This contains the XamlTypeMapper used
        // for resolving binary property information into strings.
        private ParserContext _parserContext;

        private bool _isInjected;
        private bool _useTypeConverter;

        private string _typeConverterAssemblyName;
        private string _typeConverterName;

        // Maps CLRNameSpace#AssemblyFullName  <-->  List of XmlNamespacesURIs.
        private Dictionary<String, List<String>> _reverseXmlnsTable;

#endregion Data


        /***************************************************************************\
        *
        * BamlNodeInfo
        *
        * This class holds information about a single element or other node record
        * that is encountered when reading the baml file.
        *
        \***************************************************************************/

        internal class BamlNodeInfo
        {
            // Create an empty property info record
            internal BamlNodeInfo()
            {
            }

            // The type of record, be it element, complex property, array, etc.
            internal BamlRecordType RecordType
            {
                get { return _recordType; }
                set { _recordType = value; }
            }

            // The value of the assembly name for the declaring type of the current node or property
            internal string AssemblyName
            {
                get { return _assemblyName; }
                set { _assemblyName = value; }
            }

            // The value of the namespace prefix for the current node or property
            internal string Prefix
            {
                get { return _prefix; }
                set { _prefix = value; }
            }

            // Xml namespace for the current node or property
            internal string XmlNamespace
            {
                get { return _xmlNamespace; }
                set { _xmlNamespace = value; }
            }

            // Clr namespace for the current node or property
            internal string ClrNamespace
            {
                get { return _clrNamespace; }
                set { _clrNamespace = value; }
            }

            // The fully qualified name of the current node or property
            internal string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            // The local part of the name of the current node or property
            internal string LocalName
            {
                get { return _localName; }
                set { _localName = value; }
            }

            // Defines what this property is used for such as being an alias for
            // xml:lang, xml:space or x:ID
            internal BamlAttributeUsage AttributeUsage
            {
                get { return _attributeUsage; }
                set { _attributeUsage = value; }
            }

            // The type of record, be it element, complex property, array, etc.
            private BamlRecordType _recordType;

            // The value of the assembly name for the declaring type of the current node or property
            private string _assemblyName;

            // The value of the namespace prefix for the current node or property
            private string _prefix;

            // Xml namespace for the current node or property
            private string _xmlNamespace;

            // Clr namespace for the current node or property
            private string _clrNamespace;

            // The fully qualified name of the current node or property
            private string _name;

            // The local part of the name of the current node or property
            private string _localName;

            // Defines what this property is used for such as being an alias for
            // xml:lang, xml:space or x:ID
            private BamlAttributeUsage _attributeUsage;
        }

        /***************************************************************************\
        *
        * BamlPropertyInfo
        *
        * This class holds information about a single Baml property record that is
        * encountered when reading all the property-like records on an element.
        *
        \***************************************************************************/

        internal class BamlPropertyInfo : BamlNodeInfo
        {
            // Create an empty property info record
            internal BamlPropertyInfo()
            {
            }

            // The string value for the current property
            internal string Value
            {
                get { return _value; }
                set { _value = value; }
            }

            // The string value for the current property
            private string _value;
        }

        /***************************************************************************\
        *
        * BamlContentPropertyInfo
        *
        * This class holds information about a single Baml property record that is
        * encountered when reading all the property-like records on an element.
        *
        \***************************************************************************/

        internal class BamlContentPropertyInfo : BamlNodeInfo
        {
            // this doesn't need any different fields it just needs to be
            // a different type.
        }

        /***************************************************************************\
        *
        * BamlKeyInfo
        *
        * This class holds information about a single Baml property record that is
        * encountered when reading all the property-like records on an element.
        *
        \***************************************************************************/

        [DebuggerDisplay("{_offset}")]
        internal class BamlKeyInfo : BamlPropertyInfo
        {
            // Create an empty info record
            internal BamlKeyInfo()
            {
            }

            // The offset of the value from the start of the values section.
            internal Int32 Offset
            {
                get { return _offset; }
                set { _offset = value; }
            }

            internal List<List<BamlRecord>> StaticResources
            {
                get
                {
                    if (_staticResources == null)
                    {
                        _staticResources = new List<List<BamlRecord>>();
                    }

                    return _staticResources;
                }
            }

            private Int32 _offset;
            private List<List<BamlRecord>> _staticResources;
        }
    }
}
