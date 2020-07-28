// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xaml;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Diagnostics;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using MS.Internal;
using System.Globalization;
using XamlReaderHelper = System.Windows.Markup.XamlReaderHelper;

namespace System.Windows.Baml2006
{
    public class Baml2006Reader : XamlReader, IXamlLineInfo, IFreezeFreezables
    {
        private Baml2006ReaderSettings _settings;
        private bool _isBinaryProvider;
        private bool _isEof;
        private int _lookingForAKeyOnAMarkupExtensionInADictionaryDepth;
        private XamlNodeList _lookingForAKeyOnAMarkupExtensionInADictionaryNodeList;
        private BamlBinaryReader _binaryReader;
        private Baml2006ReaderContext _context;

        private XamlNodeQueue _xamlMainNodeQueue;
        private XamlNodeList _xamlTemplateNodeList;
        private XamlReader _xamlNodesReader;
        private XamlWriter _xamlNodesWriter;
        private Stack<XamlWriter> _xamlWriterStack = new Stack<XamlWriter>();
        private Dictionary<int, TypeConverter> _typeConverterMap = new Dictionary<int, TypeConverter>();
        private Dictionary<Type, TypeConverter> _enumTypeConverterMap = new Dictionary<Type, TypeConverter>();
        private Dictionary<string, Freezable> _freezeCache;

        private const Int16 ExtensionIdMask = 0x0FFF;
        private const Int16 TypeExtensionValueMask = 0x4000;
        private const Int16 StaticExtensionValueMask = 0x2000;

        private const sbyte ReaderFlags_AddedToTree = 0x02;
        private object _root;


        #region Constructors

        public Baml2006Reader(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var schemaContext = new Baml2006SchemaContext(null);
            var settings = System.Windows.Markup.XamlReader.CreateBamlReaderSettings();
            settings.OwnsStream = true;

            Initialize(stream, schemaContext, settings);
        }

        public Baml2006Reader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var schemaContext = new Baml2006SchemaContext(null);
            var settings = new Baml2006ReaderSettings();

            Initialize(stream, schemaContext, settings);
        }

        public Baml2006Reader(Stream stream, XamlReaderSettings xamlReaderSettings)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (xamlReaderSettings == null)
            {
                throw new ArgumentNullException("xamlReaderSettings");
            }
            Baml2006SchemaContext schemaContext;
            if (xamlReaderSettings.ValuesMustBeString)
            {
                schemaContext = new Baml2006SchemaContext(xamlReaderSettings.LocalAssembly, System.Windows.Markup.XamlReader.XamlV3SharedSchemaContext);
            }
            else
            {
                schemaContext = new Baml2006SchemaContext(xamlReaderSettings.LocalAssembly);
            }
            var settings = new Baml2006ReaderSettings(xamlReaderSettings);

            Initialize(stream, schemaContext, settings);
        }

        internal Baml2006Reader(Stream stream,
            Baml2006SchemaContext schemaContext,
            Baml2006ReaderSettings settings)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (schemaContext == null)
            {
                throw new ArgumentNullException("schemaContext");
            }

            Initialize(stream, schemaContext, settings ?? new Baml2006ReaderSettings());
        }

        internal Baml2006Reader(
            Stream stream,
            Baml2006SchemaContext baml2006SchemaContext,
            Baml2006ReaderSettings baml2006ReaderSettings,
            object root)
            : this(stream, baml2006SchemaContext, baml2006ReaderSettings)
        {
            _root = root;
        }

        private void Initialize(Stream stream,
            Baml2006SchemaContext schemaContext,
            Baml2006ReaderSettings settings)
        {
            schemaContext.Settings = settings;
            _settings = settings;
            _context = new Baml2006ReaderContext(schemaContext);
            _xamlMainNodeQueue = new XamlNodeQueue(schemaContext);
            _xamlNodesReader = _xamlMainNodeQueue.Reader;
            _xamlNodesWriter = _xamlMainNodeQueue.Writer;
            _lookingForAKeyOnAMarkupExtensionInADictionaryDepth = -1;

            _isBinaryProvider = !settings.ValuesMustBeString;

            // Since the reader owns the stream and is responsible for its lifetime
            // it can safely hand out shared streams.
            if (_settings.OwnsStream)
            {
                stream = new SharedStream(stream);
            }

            _binaryReader = new BamlBinaryReader(stream);

            _context.TemplateStartDepth = -1;

            if (!_settings.IsBamlFragment)
            {
                Process_Header();
            }
        }

        #endregion

        #region XamlReader Members

        override public bool Read()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Baml2006Reader");
            }
            if (IsEof)
            {
                return false;
            }

            while (!_xamlNodesReader.Read())
            {
                if (!Process_BamlRecords())
                {
                    _isEof = true;
                    return false;
                }
            }

            if (_binaryReader.BaseStream.Length == _binaryReader.BaseStream.Position &&
                _xamlNodesReader.NodeType != XamlNodeType.EndObject)
            {
                _isEof = true;
                return false;
            }

            return true;
        }

        override public XamlNodeType NodeType
        {
            get { return _xamlNodesReader.NodeType; }
        }

        override public bool IsEof
        {
            get { return _isEof; }
        }

        override public NamespaceDeclaration Namespace
        {
            get { return _xamlNodesReader.Namespace; }
        }

        override public XamlSchemaContext SchemaContext
        {
            get { return _xamlNodesReader.SchemaContext; }
        }

        override public XamlType Type
        {
            get { return _xamlNodesReader.Type; }
        }

        override public object Value
        {
            get { return _xamlNodesReader.Value; }
        }

        override public XamlMember Member
        {
            get { return _xamlNodesReader.Member; }
        }

        protected override void Dispose(bool disposing)
        {
            if (_binaryReader != null)
            {
                if (_settings.OwnsStream)
                {
                    SharedStream sharedStream = _binaryReader.BaseStream as SharedStream;
                    if (sharedStream != null && sharedStream.SharedCount < 1)
                    {
                        // The reader is responsible for the streams lifetime and no one is sharing the stream
                        _binaryReader.Close();
                    }
                }

                _binaryReader = null;
                _context = null;
            }
        }


        #endregion

        #region IXamlLineInfo Members

        bool IXamlLineInfo.HasLineInfo
        {
            get { return _context.CurrentFrame != null; }
        }

        int IXamlLineInfo.LineNumber
        {
            get { return _context.LineNumber; }
        }

        int IXamlLineInfo.LinePosition
        {
            get { return _context.LineOffset; }
        }

        #endregion

        #region Internal Methods

        internal List<KeyRecord> ReadKeys()
        {
            _context.KeyList = new List<KeyRecord>();
            _context.CurrentFrame.IsDeferredContent = true;

            bool keepReading = true;
            while (keepReading)
            {
                Baml2006RecordType recordType = Read_RecordType();
                switch (recordType)
                {
                    case Baml2006RecordType.KeyElementStart:
                        Process_KeyElementStart();
                        while (true)
                        {
                            recordType = Read_RecordType();
                            if (recordType == Baml2006RecordType.KeyElementEnd)
                            {
                                Process_KeyElementEnd();
                                break;
                            }
                            else
                            {
                                _binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);
                                Process_OneBamlRecord();
                            }
                        }
                        break;
                    case Baml2006RecordType.StaticResourceStart:
                        Process_StaticResourceStart();
                        while (true)
                        {
                            recordType = Read_RecordType();
                            if (recordType == Baml2006RecordType.StaticResourceEnd)
                            {
                                Process_StaticResourceEnd();
                                break;
                            }
                            else
                            {
                                _binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);
                                Process_OneBamlRecord();
                            }
                        }
                        break;
                    case Baml2006RecordType.DefAttributeKeyType:
                        Process_DefAttributeKeyType();
                        break;
                    case Baml2006RecordType.DefAttributeKeyString:
                        Process_DefAttributeKeyString();
                        break;
                    case Baml2006RecordType.OptimizedStaticResource:
                        Process_OptimizedStaticResource();
                        break;
                    default:
                        keepReading = false;
                        _binaryReader.BaseStream.Seek(-1, SeekOrigin.Current);
                        break;
                }

                // If we're at the end of the stream, just return
                if (_binaryReader.BaseStream.Length == _binaryReader.BaseStream.Position)
                {
                    keepReading = false;
                    break;
                }
            }

            // ValuePosition is relative to the end of the Keys Section.
            // Patch the relative offsets to absolute positions now that we know where the keys end.
            // And record the size of each record. (in bytes)
            KeyRecord previousKeyRecord = null;
            long endOfKeysStartOfObjects = _binaryReader.BaseStream.Position;
            foreach (KeyRecord keyRecord in _context.KeyList)
            {
                keyRecord.ValuePosition += endOfKeysStartOfObjects;
                if (previousKeyRecord != null)
                {
                    previousKeyRecord.ValueSize = (int)(keyRecord.ValuePosition - previousKeyRecord.ValuePosition);
                }
                previousKeyRecord = keyRecord;
            }
            previousKeyRecord.ValueSize = (int)(_binaryReader.BaseStream.Length - previousKeyRecord.ValuePosition);

            keepReading = false;

            return _context.KeyList;
        }

        internal XamlReader ReadObject(KeyRecord record)
        {
            // This means we're at the end of the Deferred Content
            // Break out and return null
            if (record.ValuePosition == _binaryReader.BaseStream.Length)
            {
                return null;
            }

            _binaryReader.BaseStream.Seek(record.ValuePosition, SeekOrigin.Begin);

            _context.CurrentKey = _context.KeyList.IndexOf(record);

            if (_xamlMainNodeQueue.Count > 0)
            {
                throw new XamlParseException();
            }

            if (Read_RecordType() != Baml2006RecordType.ElementStart)
            {
                throw new XamlParseException();
            }

            XamlWriter oldQueueWriter = _xamlNodesWriter;

            // estimates from statistical examiniation of Theme Resource Data.
            // small RD entries appear to have about a 2.2 bytes to XamlNode ration.
            // larger RD entries are less dense (due to data) at about 4.25.
            // Presizing the XamlNodeLists save upto 50% in memory useage for same.
            int initialSizeOfNodeList = (record.ValueSize < 800)
                        ? (int)(record.ValueSize / 2.2)
                        : (int)(record.ValueSize / 4.25);

            initialSizeOfNodeList = (initialSizeOfNodeList < 8) ? 8 : initialSizeOfNodeList;

            var result = new XamlNodeList(_xamlNodesReader.SchemaContext, initialSizeOfNodeList);
            _xamlNodesWriter = result.Writer;

            Baml2006ReaderFrame baseFrame = _context.CurrentFrame;
            Process_ElementStart();

            while (baseFrame != _context.CurrentFrame)
            {
                Process_OneBamlRecord();
            }
            _xamlNodesWriter.Close();

            _xamlNodesWriter = oldQueueWriter;
            return result.GetReader();
        }

        internal Type GetTypeOfFirstStartObject(KeyRecord record)
        {
            _context.CurrentKey = _context.KeyList.IndexOf(record);

            // This means we're at the end of the Deferred Content
            // Break out and return null
            if (record.ValuePosition == _binaryReader.BaseStream.Length)
            {
                return null;
            }

            _binaryReader.BaseStream.Seek(record.ValuePosition, SeekOrigin.Begin);

            if (Read_RecordType() != Baml2006RecordType.ElementStart)
            {
                throw new XamlParseException();
            }

            return BamlSchemaContext.GetClrType(_binaryReader.ReadInt16());
        }

        #endregion

        #region Private Methods

        // Processes BAML records until we can return at least one XAML node.
        private bool Process_BamlRecords()
        {
            int initialCount = _xamlMainNodeQueue.Count;

            while (Process_OneBamlRecord())
            {
                if (_xamlMainNodeQueue.Count > initialCount)
                {
                    return true;
                }
            }
            return false;
        }

        // Reads one Baml record and returns whether it can return a XAML node or not.
        private bool Process_OneBamlRecord()
        {
            // If we're at the end, Exit out.
            if (_binaryReader.BaseStream.Position == _binaryReader.BaseStream.Length)
            {
                _isEof = true;
                return false;
            }

            Baml2006RecordType recordType = Read_RecordType();
            switch (recordType)
            {
                // Not useful to us so we skip.
                case Baml2006RecordType.DocumentStart:
                    SkipBytes(6);
                    break;

                // At the end so break out.
                case Baml2006RecordType.DocumentEnd:
                    return false;

                #region Object Records
                case Baml2006RecordType.ElementStart:
                    Process_ElementStart();
                    break;

                case Baml2006RecordType.ElementEnd:
                    Process_ElementEnd();
                    break;

                case Baml2006RecordType.NamedElementStart:
                    // This is only used by template code, and only as a temporary record, so should never occur here.
                    throw new XamlParseException();

                case Baml2006RecordType.KeyElementStart:
                    Process_KeyElementStart();
                    break;

                case Baml2006RecordType.KeyElementEnd:
                    Process_KeyElementEnd();
                    break;
                #endregion

                #region Property Records
                case Baml2006RecordType.XmlnsProperty:
                    // This is only valid right after an ElementStart and before any other real nodes.  
                    // Should never be seen here.
                    throw new XamlParseException("Found unexpected Xmlns BAML record");

                case Baml2006RecordType.Property:
                    Process_Property();
                    break;

                case Baml2006RecordType.PropertyCustom:
                    Process_PropertyCustom();
                    break;

                case Baml2006RecordType.PropertyWithConverter:
                    Process_PropertyWithConverter();
                    break;

                case Baml2006RecordType.PropertyWithExtension:
                    Process_PropertyWithExtension();
                    break;

                case Baml2006RecordType.PropertyTypeReference:
                    Process_PropertyTypeReference();
                    break;

                case Baml2006RecordType.PropertyStringReference:
                    Process_PropertyStringReference();
                    break;

                case Baml2006RecordType.PropertyWithStaticResourceId:
                    Process_PropertyWithStaticResourceId();
                    break;

                case Baml2006RecordType.ContentProperty:
                    Process_ContentProperty();
                    break;

                case Baml2006RecordType.RoutedEvent:
                    Process_RoutedEvent();
                    break;

                case Baml2006RecordType.ClrEvent:
                    Process_ClrEvent();
                    break;

                case Baml2006RecordType.ConstructorParametersStart:
                    Process_ConstructorParametersStart();
                    break;

                case Baml2006RecordType.ConstructorParameterType:
                    Process_ConstructorParameterType();
                    break;

                case Baml2006RecordType.ConstructorParametersEnd:
                    Process_ConstructorParametersEnd();
                    break;

                case Baml2006RecordType.PropertyComplexStart:
                    Process_PropertyComplexStart();
                    break;

                case Baml2006RecordType.PropertyArrayStart:
                case Baml2006RecordType.PropertyIListStart:
                    Process_PropertyArrayStart();
                    break;

                case Baml2006RecordType.PropertyIDictionaryStart:
                    Process_PropertyIDictionaryStart();
                    break;

                // Just need to output EndMember so we handle all at once.
                case Baml2006RecordType.PropertyComplexEnd:
                case Baml2006RecordType.PropertyArrayEnd:
                case Baml2006RecordType.PropertyIListEnd:
                    Process_PropertyEnd();
                    break;

                case Baml2006RecordType.PropertyIDictionaryEnd:
                    Process_PropertyIDictionaryEnd();
                    break;
                #endregion

                #region Property values
                case Baml2006RecordType.StaticResourceId:
                    Process_StaticResourceId();
                    break;

                case Baml2006RecordType.StaticResourceStart:
                    // these are Key records, but they are read here in BRAT mode.
                    // BRAT == BAML Reader As Text.
                    Process_StaticResourceStart();
                    break;

                case Baml2006RecordType.StaticResourceEnd:
                    // these are Key records, but they are read here in BRAT mode.
                    Process_StaticResourceEnd();
                    break;

                case Baml2006RecordType.OptimizedStaticResource:
                    // these are Key records, but they are read here in BRAT mode.
                    Process_OptimizedStaticResource();
                    break;

                case Baml2006RecordType.Text:
                    Process_Text();
                    break;

                case Baml2006RecordType.TextWithConverter:
                    Process_TextWithConverter();
                    break;

                case Baml2006RecordType.TextWithId:
                    Process_TextWithId();
                    break;

                case Baml2006RecordType.LiteralContent:
                    Process_LiteralContent();
                    break;
                #endregion

                case Baml2006RecordType.DefAttribute:
                    Process_DefAttribute();
                    break;

                case Baml2006RecordType.DefAttributeKeyString:
                    Process_DefAttributeKeyString();
                    break;

                case Baml2006RecordType.DefAttributeKeyType:
                    Process_DefAttributeKeyType();
                    break;

                case Baml2006RecordType.DefTag:
                    Process_DefTag();
                    break;

                case Baml2006RecordType.DeferableContentStart:
                    Process_DeferableContentStart();
                    break;

                case Baml2006RecordType.EndAttributes:
                    Process_EndAttributes();
                    break;

                case Baml2006RecordType.XmlAttribute:
                    Process_XmlAttribute();
                    break;

                case Baml2006RecordType.PresentationOptionsAttribute:
                    Process_PresentationOptionsAttribute();
                    break;

                #region Schema Records
                case Baml2006RecordType.ProcessingInstruction:
                    Process_ProcessingInstruction();
                    break;

                case Baml2006RecordType.PIMapping:
                    Process_PIMapping();
                    break;

                case Baml2006RecordType.AssemblyInfo:
                    Process_AssemblyInfo();
                    break;

                case Baml2006RecordType.TypeInfo:
                    Process_TypeInfo();
                    break;

                case Baml2006RecordType.TypeSerializerInfo:
                    Process_TypeSerializerInfo();
                    break;

                case Baml2006RecordType.AttributeInfo:
                    Process_AttributeInfo();
                    break;

                case Baml2006RecordType.StringInfo:
                    Process_StringInfo();
                    break;
                #endregion

                #region Debugging Records
                case Baml2006RecordType.LinePosition:
                    Process_LinePosition();
                    break;

                case Baml2006RecordType.LineNumberAndPosition:
                    Process_LineNumberAndPosition();
                    break;

                case Baml2006RecordType.Comment:
                    Process_Comment();
                    break;

                #endregion

                case Baml2006RecordType.ConnectionId:
                    Process_ConnectionId();
                    break;

                case Baml2006RecordType.Unknown:
                default:
                    throw new XamlParseException(string.Format(CultureInfo.CurrentCulture, SR.Get(SRID.UnknownBamlRecord, recordType)));
            }

            return true;
        }

        // Have not seen a BAML file that has this...
        private void Process_ProcessingInstruction()
        {
            throw new NotImplementedException();
        }

        // Have not seen a BAML file that has this...
        private void Process_DefTag()
        {
            throw new NotImplementedException();
        }

        // Have not seen a BAML file that has this...
        private void Process_EndAttributes()
        {
            throw new NotImplementedException();
        }

        // Have not seen a BAML file that has this...
        private void Process_XmlAttribute()
        {
            throw new NotImplementedException();
        }

        // This is for the f:Freeze directive.  Need work in System.Xaml to support returning this.
        private void Process_PresentationOptionsAttribute()
        {
            Common_Process_Property();

            Read_RecordSize();
            string value = _binaryReader.ReadString();
            string name = _context.SchemaContext.GetString(_binaryReader.ReadInt16());

            if (_context.TemplateStartDepth < 0)
            {
                _xamlNodesWriter.WriteStartMember(System.Windows.Markup.XamlReaderHelper.Freeze);
                _xamlNodesWriter.WriteValue(value); // Do we need to parse the boolean value?
                _xamlNodesWriter.WriteEndMember();
            }
        }

        // Have not seen a BAML file that has this...
        private void Process_Comment()
        {
            throw new NotImplementedException();
        }

        // String for Content.  Sometimes also drops out the Content Property
        private void Process_LiteralContent()
        {
            Read_RecordSize();
            string value = _binaryReader.ReadString();
            int lineNumber = _binaryReader.ReadInt32();
            int lineOffset = _binaryReader.ReadInt32();

            bool shouldInjectContentProperty = _context.CurrentFrame.Member == null;

            if (shouldInjectContentProperty)
            {
                if (_context.CurrentFrame.XamlType.ContentProperty != null)
                {
                    Common_Process_Property();

                    _xamlNodesWriter.WriteStartMember(_context.CurrentFrame.XamlType.ContentProperty);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            if (!_isBinaryProvider)
            {
                _xamlNodesWriter.WriteStartObject(XamlLanguage.XData);
                XamlMember xDataTextProperty = XamlLanguage.XData.GetMember("Text");
                _xamlNodesWriter.WriteStartMember(xDataTextProperty);
                _xamlNodesWriter.WriteValue(value);
                _xamlNodesWriter.WriteEndMember();
                _xamlNodesWriter.WriteEndObject();
            }
            else
            {
                var xData = new System.Windows.Markup.XData();
                xData.Text = value;
                _xamlNodesWriter.WriteValue(xData);
            }

            if (shouldInjectContentProperty)
            {
                _xamlNodesWriter.WriteEndMember();
            }
        }

        // Text to be written for type converters.  Also drops a InitProperty as well
        private void Process_TextWithConverter()
        {
            Read_RecordSize();
            string value = _binaryReader.ReadString();
            short converterId = _binaryReader.ReadInt16();

            bool shouldDropProperty = _context.CurrentFrame.Member == null;

            if (shouldDropProperty)
            {
                Common_Process_Property();

                _xamlNodesWriter.WriteStartMember(XamlLanguage.Initialization);
            }

            // It would try to check the custom XamlSerializers but that is always done through a different record.
            _xamlNodesWriter.WriteValue(value);

            if (shouldDropProperty)
            {
                _xamlNodesWriter.WriteEndMember();
            }
        }

        private void Process_StaticResourceEnd()
        {
            XamlWriter writer = GetLastStaticResource().ResourceNodeList.Writer;
            writer.WriteEndObject();
            writer.Close();

            _context.InsideStaticResource = false;

            _xamlNodesWriter = _xamlWriterStack.Pop();
            _context.PopScope();
        }

        private void Process_StaticResourceStart()
        {
            XamlType type = BamlSchemaContext.GetXamlType(_binaryReader.ReadInt16());
            byte flags = _binaryReader.ReadByte();

            StaticResource staticResource = new StaticResource(type, BamlSchemaContext);
            _context.LastKey.StaticResources.Add(staticResource);
            _context.InsideStaticResource = true;

            _xamlWriterStack.Push(_xamlNodesWriter);
            _xamlNodesWriter = staticResource.ResourceNodeList.Writer;

            _context.PushScope();
            _context.CurrentFrame.XamlType = type;
        }

        private void Process_StaticResourceId()
        {
            InjectPropertyAndFrameIfNeeded(_context.SchemaContext.GetXamlType(typeof(StaticResourceExtension)), 0);

            short resourceId = _binaryReader.ReadInt16();
            object value = _context.KeyList[_context.CurrentKey - 1].StaticResources[resourceId];

            // If we are just loading the BAML to create a node stream (like localization, BRAT mode)
            // then these will be StaticResources. (not to be confused with StaticResourceExtensions)
            // If we are loading the BAML and setting Resource Dictionary DeferrableContent property
            // then these will have been upgraded to StaticResourceHolders (an ME).
            var staticResource = value as StaticResource;
            if (staticResource != null)
            {
                XamlServices.Transform(staticResource.ResourceNodeList.GetReader(), _xamlNodesWriter, false);
            }
            else
            {
                _xamlNodesWriter.WriteValue(value);
            }
        }

        private void Process_ClrEvent()
        {
            throw new NotImplementedException();
        }

        private void Process_RoutedEvent()
        {
            throw new NotImplementedException();
        }

        private void Process_PropertyStringReference()
        {
            throw new NotImplementedException();
        }

        private void Process_OptimizedStaticResource()
        {
            byte flags = _binaryReader.ReadByte();
            short keyId = _binaryReader.ReadInt16();

            var optimizedStaticResource = new OptimizedStaticResource(flags, keyId);

            if (_isBinaryProvider)
            {
                // Compute the Value from the ValueId
                // This code could/should be in "OptimizedStaticResource.Value", but that class
                // doesn't have access to the BamlSchemaContext or the GetStaticExtension()
                // method below.
                if (optimizedStaticResource.IsKeyTypeExtension)
                {
                    XamlType xamlType = BamlSchemaContext.GetXamlType(keyId);
                    optimizedStaticResource.KeyValue = xamlType.UnderlyingType;
                }
                else if (optimizedStaticResource.IsKeyStaticExtension)
                {
                    Type memberType;
                    object providedValue;
                    string propertyName = GetStaticExtensionValue(keyId, out memberType, out providedValue);
                    if (providedValue == null)
                    {
                        var staticExtension = new System.Windows.Markup.StaticExtension(propertyName);
                        staticExtension.MemberType = memberType;
                        providedValue = staticExtension.ProvideValue(null);
                    }
                    optimizedStaticResource.KeyValue = providedValue;
                }
                else
                {
                    optimizedStaticResource.KeyValue = _context.SchemaContext.GetString(keyId);
                }
            }

            var staticResources = _context.LastKey.StaticResources;
            staticResources.Add(optimizedStaticResource);
        }

        private void Process_DeferableContentStart()
        {
            Int32 contentSize = _binaryReader.ReadInt32();

            if (_isBinaryProvider && contentSize > 0)
            {
                object binaryData = null;
                if (_settings.OwnsStream)
                {
                    // Creates a SharedStream that will be used be shared 
                    long position = _binaryReader.BaseStream.Position;
                    binaryData = new SharedStream(_binaryReader.BaseStream, position, contentSize);
                    _binaryReader.BaseStream.Seek(position + contentSize, SeekOrigin.Begin);
                }
                else
                {
                    // The stream may be closed after the end of the baml read
                    // Copy the defered content so that it is available after the read has completed
                    binaryData = new MemoryStream(_binaryReader.ReadBytes(contentSize));
                }

                Common_Process_Property();

                _xamlNodesWriter.WriteStartMember(BamlSchemaContext.ResourceDictionaryDeferredContentProperty);
                _xamlNodesWriter.WriteValue(binaryData);
                _xamlNodesWriter.WriteEndMember();
            }
            else
            {
                _context.KeyList = new List<KeyRecord>();
                _context.CurrentKey = 0;
                _context.CurrentFrame.IsDeferredContent = true;
            }
        }

        private void Process_DefAttribute()
        {
            Read_RecordSize();
            string value = _binaryReader.ReadString();
            Int16 stringId = _binaryReader.ReadInt16();

            XamlMember property = BamlSchemaContext.GetXamlDirective(XamlLanguage.Xaml2006Namespace, BamlSchemaContext.GetString(stringId));

            if (property == XamlLanguage.Key)
            {
                _context.CurrentFrame.Key = new KeyRecord(false, false, 0, value);
            }
            else
            {
                Common_Process_Property();

                _xamlNodesWriter.WriteStartMember(property);
                _xamlNodesWriter.WriteValue(value);
                _xamlNodesWriter.WriteEndMember();
            }
        }

        private void Process_DefAttributeKeyString()
        {
            Read_RecordSize();
            Int16 keyStringId = _binaryReader.ReadInt16();
            Int32 valuePosition = _binaryReader.ReadInt32();
            bool isShared = _binaryReader.ReadBoolean();
            bool isSharedSet = _binaryReader.ReadBoolean();
            string value = _context.SchemaContext.GetString(keyStringId);

            KeyRecord key = new KeyRecord(isShared, isSharedSet, valuePosition, value);
            if (_context.CurrentFrame.IsDeferredContent)
            {
                _context.KeyList.Add(key);
            }
            else
            {
                _context.CurrentFrame.Key = key;
            }
        }

        private void Process_DefAttributeKeyType()
        {
            Int16 typeId = _binaryReader.ReadInt16();
            byte flags = _binaryReader.ReadByte();
            Int32 valuePosition = _binaryReader.ReadInt32();
            bool isShared = _binaryReader.ReadBoolean();
            bool isSharedSet = _binaryReader.ReadBoolean();

            Type type = Baml2006SchemaContext.KnownTypes.GetKnownType(typeId);
            if (type == null)
            {
                type = BamlSchemaContext.GetClrType(typeId);
            }
            KeyRecord key = new KeyRecord(isShared, isSharedSet, valuePosition, type);
            if (_context.CurrentFrame.IsDeferredContent)
            {
                _context.KeyList.Add(key);
            }
            else
            {
                _context.CurrentFrame.Key = key;
            }
        }

        private bool IsStringOnlyWhiteSpace(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }

        // Need to write just a text node out but the V3 markup compiler has a bug where it will occasionally output
        // text even though it shouldn't.  The checks are to get around that.
        private void Process_Text()
        {
            Read_RecordSize();
            string textValue = _binaryReader.ReadString();
            Process_Text_Helper(textValue);
        }

        // Need to look up a string in the string table via ID.
        private void Process_TextWithId()
        {
            Read_RecordSize();
            short textId = _binaryReader.ReadInt16();
            string textValue = BamlSchemaContext.GetString(textId);
            Process_Text_Helper(textValue);
        }

        private void Process_Text_Helper(string stringValue)
        {
            if (_context.InsideKeyRecord != true && _context.InsideStaticResource != true)
            {
                InjectPropertyAndFrameIfNeeded(_context.SchemaContext.GetXamlType(typeof(String)), 0);
            }

            // Whitespace inside PositionalParameters is interpreted according to MarkupExtension syntax rules,
            // which means that if the compiler left it in the BAML, it's significant.
            // Otherwise, it's possible that the compiler wasn't sure whether the whitespace was significant,
            // so we may need to strip it here.
            if (IsStringOnlyWhiteSpace(stringValue) &&
                _context.CurrentFrame.Member != XamlLanguage.PositionalParameters)
            {
                if (_context.CurrentFrame.XamlType != null && _context.CurrentFrame.XamlType.IsCollection)
                {
                    if (!_context.CurrentFrame.XamlType.IsWhitespaceSignificantCollection)
                    {
                        return;
                    }
                }
                else
                {
                    if (_context.CurrentFrame.Member.Type != null &&
                        !_context.CurrentFrame.Member.Type.UnderlyingType.IsAssignableFrom(typeof(String)))
                        return;
                }
            }

            _xamlNodesWriter.WriteValue(stringValue);
        }

        private void Process_ConstructorParametersEnd()
        {
            _xamlNodesWriter.WriteEndMember();
            _context.CurrentFrame.Member = null;
        }

        private void Process_ConstructorParametersStart()
        {
            Common_Process_Property();

            _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
            _context.CurrentFrame.Member = XamlLanguage.PositionalParameters;
        }

        private void Process_ConstructorParameterType()
        {
            Int16 typeId = _binaryReader.ReadInt16();

            if (_isBinaryProvider)
            {
                _xamlNodesWriter.WriteValue(BamlSchemaContext.GetXamlType(typeId).UnderlyingType);
            }
            else
            {
                _xamlNodesWriter.WriteStartObject(XamlLanguage.Type);
                _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                _xamlNodesWriter.WriteValue(Logic_GetFullyQualifiedNameForType(BamlSchemaContext.GetXamlType(typeId)));
                _xamlNodesWriter.WriteEndMember();
                _xamlNodesWriter.WriteEndObject();
            }
        }

        private void Process_Header()
        {
            Int32 stringLength = _binaryReader.ReadInt32();

            byte[] headerString = _binaryReader.ReadBytes(stringLength);

            Int32 readerVersion = _binaryReader.ReadInt32();
            Int32 updateVersion = _binaryReader.ReadInt32();
            Int32 writerVersion = _binaryReader.ReadInt32();
        }

        private void Process_ElementStart()
        {
            XamlType type;
            Int16 typeId = _binaryReader.ReadInt16();
            // Obfuscated root types remain unobfuscated in BAML.
            // Reflect the root instance for the root type in case of obfuscation.
            if (_root != null && _context.CurrentFrame.Depth == 0)
            {
                Type rootType = _root.GetType();
                type = BamlSchemaContext.GetXamlType(rootType);
            }
            else
            {
                type = BamlSchemaContext.GetXamlType(typeId);
            }
            SByte flags = _binaryReader.ReadSByte();

            if (flags < 0 || flags > 3)
            {
                throw new XamlParseException();
            }

            InjectPropertyAndFrameIfNeeded(type, flags);

            _context.PushScope();
            _context.CurrentFrame.XamlType = type;

            //Peek ahead to read all the Xmlns properties
            bool isPeeking = true;
            do
            {
                Baml2006RecordType recordType = Read_RecordType();
                switch (recordType)
                {
                    case Baml2006RecordType.XmlnsProperty:
                        Process_XmlnsProperty();
                        break;
                    case Baml2006RecordType.AssemblyInfo:
                        Process_AssemblyInfo();
                        break;
                    case Baml2006RecordType.LinePosition:
                        Process_LinePosition();
                        break;
                    case Baml2006RecordType.LineNumberAndPosition:
                        Process_LineNumberAndPosition();
                        break;
                    default:
                        // We have peeked far enough undo the last Read_RecordType()
                        SkipBytes(-1);
                        isPeeking = false;
                        break;
                }
            }
            while (isPeeking);

            // Determine if the object was retrieved or not
            bool isRetrieved = (flags & ReaderFlags_AddedToTree) > 0;

            if (isRetrieved)
            {
                _xamlNodesWriter.WriteGetObject();
            }
            else
            {
                _xamlNodesWriter.WriteStartObject(_context.CurrentFrame.XamlType);
            }

            // If you're the first element, write out the BaseUri
            if (_context.CurrentFrame.Depth == 1)
            {
                if (_settings.BaseUri != null && !String.IsNullOrEmpty(_settings.BaseUri.ToString()))
                {
                    _xamlNodesWriter.WriteStartMember(XamlLanguage.Base);
                    _xamlNodesWriter.WriteValue(_settings.BaseUri.ToString());
                    _xamlNodesWriter.WriteEndMember();
                }
            }

            // Need to output the keys if we're in deferred content
            if (_context.PreviousFrame.IsDeferredContent && _context.InsideStaticResource == false)
            {         
                // If we're providing binary, that means we've delay loaded the ResourceDictionary
                // and the object we're currently creating doens't actually need the key.
                if (!_isBinaryProvider)
                {
                    _xamlNodesWriter.WriteStartMember(XamlLanguage.Key);

                    KeyRecord record = _context.KeyList[_context.CurrentKey];
                    // We have a string for a key, just write it out.
                    if (!String.IsNullOrEmpty(record.KeyString))
                    {
                        _xamlNodesWriter.WriteValue(record.KeyString);
                    }
                    else if (record.KeyType != null)
                    {
                        _xamlNodesWriter.WriteStartObject(XamlLanguage.Type);
                        _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                        _xamlNodesWriter.WriteValue(Logic_GetFullyQualifiedNameForType(SchemaContext.GetXamlType(record.KeyType)));
                        _xamlNodesWriter.WriteEndMember();
                        _xamlNodesWriter.WriteEndObject();
                    }
                    else
                    {
                        // This is the complex key scenario.  Just write everything out to the writer.
                        XamlServices.Transform(record.KeyNodeList.GetReader(), _xamlNodesWriter, false);
                    }

                    _xamlNodesWriter.WriteEndMember();
                }

                _context.CurrentKey++;
            }
        }

        private void Process_ElementEnd()
        {
            RemoveImplicitFrame();

            if (_context.CurrentFrame.Key != null)
            {
                _xamlNodesWriter.WriteStartMember(XamlLanguage.Key);

                KeyRecord keyRecord = _context.CurrentFrame.Key;
                if (keyRecord.KeyType != null)
                {
                    if (_isBinaryProvider)
                    {
                        _xamlNodesWriter.WriteValue(keyRecord.KeyType);
                    }
                    else
                    {
                        _xamlNodesWriter.WriteStartObject(XamlLanguage.Type);
                        _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                        _xamlNodesWriter.WriteValue(Logic_GetFullyQualifiedNameForType(SchemaContext.GetXamlType(keyRecord.KeyType)));
                        _xamlNodesWriter.WriteEndMember();
                        _xamlNodesWriter.WriteEndObject();
                    }
                }
                else if (keyRecord.KeyNodeList != null)
                {
                    XamlServices.Transform(keyRecord.KeyNodeList.GetReader(), _xamlNodesWriter, false);
                }
                else
                {
                    _xamlNodesWriter.WriteValue(keyRecord.KeyString);
                }

                _xamlNodesWriter.WriteEndMember();

                _context.CurrentFrame.Key = null;
            }

            if (_context.CurrentFrame.DelayedConnectionId != -1)
            {
                _xamlNodesWriter.WriteStartMember(XamlLanguage.ConnectionId);
                if (_isBinaryProvider)
                {
                    _xamlNodesWriter.WriteValue(_context.CurrentFrame.DelayedConnectionId);
                }
                else
                {
                    _xamlNodesWriter.WriteValue(_context.CurrentFrame.DelayedConnectionId.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS));
                }
                _xamlNodesWriter.WriteEndMember();
            }

            _xamlNodesWriter.WriteEndObject();

            if (_context.CurrentFrame.IsDeferredContent)
            {
                _context.KeyList = null;
            }
            _context.PopScope();
        }

        private void Process_KeyElementStart()
        {
            Int16 typeId = _binaryReader.ReadInt16();
            byte flags = _binaryReader.ReadByte();
            Int32 valuePosition = _binaryReader.ReadInt32();
            bool isShared = _binaryReader.ReadBoolean();
            bool isSharedSet = _binaryReader.ReadBoolean();

            XamlType type = _context.SchemaContext.GetXamlType(typeId);

            _context.PushScope();
            _context.CurrentFrame.XamlType = type;

            // Store a key record that can be accessed later.
            // This is a complex scenario so we need to write to the keyList
            KeyRecord key = new KeyRecord(isShared, isSharedSet, valuePosition, _context.SchemaContext);
            key.Flags = flags;
            key.KeyNodeList.Writer.WriteStartObject(type);


            _context.InsideKeyRecord = true;

            // Push the current writer onto a stack and add the KeyNodeList writer.
            // All subsequent calls will be added to that writer.
            _xamlWriterStack.Push(_xamlNodesWriter);
            _xamlNodesWriter = key.KeyNodeList.Writer;

            if (_context.PreviousFrame.IsDeferredContent)
            {
                _context.KeyList.Add(key);
            }
            else
            {
                _context.PreviousFrame.Key = key;
            }
        }

        private void Process_KeyElementEnd()
        {
            KeyRecord key = null;

            if (_context.PreviousFrame.IsDeferredContent)
            {
                key = _context.LastKey;
            }
            else
            {
                key = _context.PreviousFrame.Key;
            }
            key.KeyNodeList.Writer.WriteEndObject();
            key.KeyNodeList.Writer.Close();

            // Revert the writer
            _xamlNodesWriter = _xamlWriterStack.Pop();

            _context.InsideKeyRecord = false;

            _context.PopScope();
        }

        private void Process_Property()
        {
            Common_Process_Property();

            Read_RecordSize();

            // Turns out if we have an EventSetter and this Property BamlRecord, we want to expand the property
            // to be the Event=Property, Handler=Value
            if (_context.CurrentFrame.XamlType.UnderlyingType == typeof(EventSetter))
            {
                // Write Event=Property
                _xamlNodesWriter.WriteStartMember(_context.SchemaContext.EventSetterEventProperty);
                XamlMember eventProperty = GetProperty(_binaryReader.ReadInt16(), false);
                Type currentType = eventProperty.DeclaringType.UnderlyingType;

                // Force load the Statics by walking up the hierarchy and running class constructors
                while (null != currentType)
                {
                    MS.Internal.WindowsBase.SecurityHelper.RunClassConstructor(currentType);
                    currentType = currentType.BaseType;
                }

                RoutedEvent routedEvent = EventManager.GetRoutedEventFromName(eventProperty.Name,
                    eventProperty.DeclaringType.UnderlyingType);
                _xamlNodesWriter.WriteValue(routedEvent);
                _xamlNodesWriter.WriteEndMember();

                // Write Handler=Value
                _xamlNodesWriter.WriteStartMember(_context.SchemaContext.EventSetterHandlerProperty);
                _xamlNodesWriter.WriteValue(_binaryReader.ReadString());
                _xamlNodesWriter.WriteEndMember();
            }
            else
            {
                XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);
                _xamlNodesWriter.WriteStartMember(property);
                _xamlNodesWriter.WriteValue(_binaryReader.ReadString());
                _xamlNodesWriter.WriteEndMember();
            }
        }

        private void Common_Process_Property()
        {
            if (_context.InsideKeyRecord || _context.InsideStaticResource)
            {
                return;
            }

            RemoveImplicitFrame();

            // baml property start is only valid betweeen ElementStart and ElementEnd
            if (_context.CurrentFrame.XamlType == null)
            {
                throw new XamlParseException(SR.Get(SRID.PropertyFoundOutsideStartElement));
            }

            // new start properties not appear without having ended an old property
            if (_context.CurrentFrame.Member != null)
            {
                throw new XamlParseException(SR.Get(SRID.PropertyOutOfOrder, _context.CurrentFrame.Member));
            }

            // Emit NS nodes for xmlns records encountered between ElementStart and Property
            // The first xmlns also has the prefix for the SO node we will emit later
        }

        private System.Windows.Media.Int32Collection GetInt32Collection()
        {
            BinaryReader reader = new BinaryReader(_binaryReader.BaseStream);

            System.Windows.Markup.XamlInt32CollectionSerializer.IntegerCollectionType type =
                (System.Windows.Markup.XamlInt32CollectionSerializer.IntegerCollectionType)reader.ReadByte();
            int capacity = reader.ReadInt32();
            if (capacity < 0)
            {
                throw new ArgumentException(SR.Get(SRID.IntegerCollectionLengthLessThanZero, new object[0]));
            }
            System.Windows.Media.Int32Collection ints = new System.Windows.Media.Int32Collection(capacity);
            switch (type)
            {
                case System.Windows.Markup.XamlInt32CollectionSerializer.IntegerCollectionType.Byte:
                    for (int i = 0; i < capacity; i++)
                    {
                        ints.Add(reader.ReadByte());
                    }
                    return ints;

                case System.Windows.Markup.XamlInt32CollectionSerializer.IntegerCollectionType.UShort:
                    for (int j = 0; j < capacity; j++)
                    {
                        ints.Add(reader.ReadUInt16());
                    }
                    return ints;

                case System.Windows.Markup.XamlInt32CollectionSerializer.IntegerCollectionType.Integer:
                    for (int k = 0; k < capacity; k++)
                    {
                        int num7 = reader.ReadInt32();
                        ints.Add(num7);
                    }
                    return ints;

                case System.Windows.Markup.XamlInt32CollectionSerializer.IntegerCollectionType.Consecutive:
                    {
                        int num2 = reader.ReadInt32();
                        for (int m = 0; m < capacity; m++)
                        {
                            ints.Add(num2 + m);
                        }
                        return ints;
                    }
            }

            throw new InvalidOperationException(SR.Get(SRID.UnableToConvertInt32));
        }

        private XamlMember GetProperty(Int16 propertyId, XamlType parentType)
        {
            XamlMember property = BamlSchemaContext.GetProperty(propertyId, parentType);
            return property;
        }

        private XamlMember GetProperty(Int16 propertyId, bool isAttached)
        {
            XamlMember property = BamlSchemaContext.GetProperty(propertyId, isAttached);
            return property;
        }

        // (property, serializer, value) value is binary data that needs to be de-serialized
        private void Process_PropertyCustom()
        {
            Common_Process_Property();

            int recordSize = Read_RecordSize();
            XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);
            _xamlNodesWriter.WriteStartMember(property);
            short serializerTypeId = _binaryReader.ReadInt16();

            // If it is a value type
            if ((serializerTypeId & TypeExtensionValueMask) == TypeExtensionValueMask)
            {
                serializerTypeId &= (short)(~TypeExtensionValueMask);
            }
            if (_isBinaryProvider)
            {
                WriteTypeConvertedInstance(serializerTypeId, recordSize - 5); // Header is fixed length
            }
            else
            {
                // Need to translate Binary back to text.
                _xamlNodesWriter.WriteValue(GetTextFromBinary(_binaryReader.ReadBytes(recordSize - 5),
                                      serializerTypeId, property, _context.CurrentFrame.XamlType));
            }
            _xamlNodesWriter.WriteEndMember();
        }

        // Writes either Binary or the type converted binary.  
        // Things like boolean, enum, strings, and DPs can be type since they aren't thread affine
        private bool WriteTypeConvertedInstance(short converterId, int dataByteSize)
        {
            TypeConverter converter;
            switch (converterId)
            {
                case Baml2006SchemaContext.KnownTypes.XamlInt32CollectionSerializer:
                    _xamlNodesWriter.WriteValue(GetInt32Collection());
                    break;
                case Baml2006SchemaContext.KnownTypes.EnumConverter:
                    converter = new EnumConverter(_context.CurrentFrame.XamlType.UnderlyingType);
                    _xamlNodesWriter.WriteValue(converter.ConvertFrom(_binaryReader.ReadBytes(dataByteSize)));
                    break;
                case Baml2006SchemaContext.KnownTypes.BooleanConverter:
                    Debug.Assert(dataByteSize == 1);
                    _xamlNodesWriter.WriteValue((_binaryReader.ReadBytes(1)[0] == 0) ? false : true);
                    break;
                case Baml2006SchemaContext.KnownTypes.StringConverter:
                    _xamlNodesWriter.WriteValue(_binaryReader.ReadString());
                    break;
                case Baml2006SchemaContext.KnownTypes.DependencyPropertyConverter:
                    DependencyProperty property = null;
                    if (dataByteSize == 2)
                    {
                        property = BamlSchemaContext.GetDependencyProperty(_binaryReader.ReadInt16());
                    }
                    else
                    {
                        Type type = BamlSchemaContext.GetXamlType(_binaryReader.ReadInt16()).UnderlyingType;
                        property = DependencyProperty.FromName(_binaryReader.ReadString(), type);
                    }
                    _xamlNodesWriter.WriteValue(property);
                    break;
                case Baml2006SchemaContext.KnownTypes.XamlBrushSerializer:
                case Baml2006SchemaContext.KnownTypes.XamlPathDataSerializer:
                case Baml2006SchemaContext.KnownTypes.XamlPoint3DCollectionSerializer:
                case Baml2006SchemaContext.KnownTypes.XamlPointCollectionSerializer:
                case Baml2006SchemaContext.KnownTypes.XamlVector3DCollectionSerializer:
                    DeferredBinaryDeserializerExtension deserializerME = new DeferredBinaryDeserializerExtension(this, _binaryReader, converterId, dataByteSize);
                    _xamlNodesWriter.WriteValue(deserializerME);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }


        // (property, value, typeconverter) The value is string data that needs to be type converted
        private void Process_PropertyWithConverter()
        {
            Common_Process_Property();

            Read_RecordSize();

            XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);
            _xamlNodesWriter.WriteStartMember(property);

            object value = _binaryReader.ReadString();
            short typeConverterId = _binaryReader.ReadInt16();
            if (_isBinaryProvider && 
                typeConverterId < 0 &&
                -typeConverterId != System.Windows.Baml2006.Baml2006SchemaContext.KnownTypes.StringConverter)
            {
                TypeConverter converter = null;
                if (-typeConverterId == Baml2006SchemaContext.KnownTypes.EnumConverter)
                {
                    Type propertyType = property.Type.UnderlyingType;
                    // Setter.Value is of type object but will write out EnumConverter.  
                    if (propertyType.IsEnum)
                    {
                        if (!_enumTypeConverterMap.TryGetValue(propertyType, out converter))
                        {
                            converter = new EnumConverter(propertyType);
                            _enumTypeConverterMap[propertyType] = converter;
                        }
                    }
                }
                else
                {
                    if (!_typeConverterMap.TryGetValue(typeConverterId, out converter))
                    {
                        converter = Baml2006SchemaContext.KnownTypes.CreateKnownTypeConverter(typeConverterId);
                        _typeConverterMap[typeConverterId] = converter;
                    }
                }
                if (converter != null)
                {
                    value = CreateTypeConverterMarkupExtension(property, converter, value, _settings);
                }
            }

            _xamlNodesWriter.WriteValue(value);
            _xamlNodesWriter.WriteEndMember();
        }

        //  When processing ResourceDictionary.Source we may find a Uri that references the
        // local assembly, but if another version of the same assembly is loaded we may have trouble resolving
        // to the correct one. Baml2006ReaderInternal has an override of this method that returns a custom markup
        // extension that passes down the local assembly information to help in this case.
        internal virtual object CreateTypeConverterMarkupExtension(XamlMember property, TypeConverter converter, object propertyValue, Baml2006ReaderSettings settings)
        {
            return new TypeConverterMarkupExtension(converter, propertyValue);
        }

        // (property, markup extension, value) including the well known WPF TemplateBinding, StaticResource or DynamicResource markup extensions
        // Writes out a property with a markup extension.  However, if there is a x:Static, Type, or TemplateBinding inside the ME, we code that 
        // specially.  e.g. Property="{FooExtension {x:Static f:Foo.Bar}}" would be written out as one Baml Record
        private void Process_PropertyWithExtension()
        {
            Common_Process_Property();

            short propertyId = _binaryReader.ReadInt16();
            Int16 extensionId = _binaryReader.ReadInt16();
            Int16 valueId = _binaryReader.ReadInt16();


            XamlMember property = GetProperty(propertyId, _context.CurrentFrame.XamlType);
            Int16 extensionTypeId = (Int16)(extensionId & ExtensionIdMask);
            XamlType extensionType = BamlSchemaContext.GetXamlType((short)(-extensionTypeId));

            bool isValueTypeExtension = (extensionId & TypeExtensionValueMask) == TypeExtensionValueMask;
            bool isValueStaticExtension = (extensionId & StaticExtensionValueMask) == StaticExtensionValueMask;

            Type typeExtensionType = null;
            Type memberType = null;
            object value = null;


            // Write out the main Extension {FooExtension
            _xamlNodesWriter.WriteStartMember(property);

            // If we're in the binary case, we try to write the value out directly
            bool handled = false;
            if (_isBinaryProvider)
            {
                object param = null;
                object providedValue = null;

                if (isValueStaticExtension)
                {
                    Type ownerType = null;
                    string staticExParam = GetStaticExtensionValue(valueId, out ownerType, out providedValue);

                    // If it's a Known command or a SystemResourceKey, send the value across directly
                    if (providedValue != null)
                    {
                        param = providedValue;
                    }
                    else
                    {
                        System.Windows.Markup.StaticExtension staticExtension =
                            new System.Windows.Markup.StaticExtension(staticExParam);
                        Debug.Assert(ownerType != null);
                        staticExtension.MemberType = ownerType;
                        param = staticExtension.ProvideValue(null); // if MemberType is set we don't need ITypeResolver
                    }
                }
                else if (isValueTypeExtension)
                {
                    param = BamlSchemaContext.GetXamlType(valueId).UnderlyingType;
                }
                else
                {
                    // For all other scenarios, we want to just write out the type

                    if (extensionTypeId == Baml2006SchemaContext.TemplateBindingTypeId)
                    {
                        param = _context.SchemaContext.GetDependencyProperty(valueId);
                    }
                    else if (extensionTypeId == Baml2006SchemaContext.StaticExtensionTypeId)
                    {
                        param = GetStaticExtensionValue(valueId, out memberType, out providedValue);
                    }
                    else if (extensionTypeId == Baml2006SchemaContext.TypeExtensionTypeId)
                    {
                        param = BamlSchemaContext.GetXamlType(valueId).UnderlyingType;
                    }
                    else
                    {
                        param = BamlSchemaContext.GetString(valueId);
                    }
                }

                // Doing == comparison since we only know how to create those quickly here
                if (extensionTypeId == Baml2006SchemaContext.DynamicResourceTypeId)
                {
                    value = new DynamicResourceExtension(param);
                    handled = true;
                }
                else if (extensionTypeId == Baml2006SchemaContext.StaticResourceTypeId)
                {
                    value = new StaticResourceExtension(param);
                    handled = true;
                }
                else if (extensionTypeId == Baml2006SchemaContext.TemplateBindingTypeId)
                {
                    value = new TemplateBindingExtension((DependencyProperty)param);
                    handled = true;
                }
                else if (extensionTypeId == Baml2006SchemaContext.TypeExtensionTypeId)
                {
                    value = param;
                    handled = true;
                }
                else if (extensionTypeId == Baml2006SchemaContext.StaticExtensionTypeId)
                {
                    if (providedValue != null)
                    {
                        value = providedValue;
                    }
                    else
                    {
                        System.Windows.Markup.StaticExtension staticExtension = new System.Windows.Markup.StaticExtension((string)param);
                        staticExtension.MemberType = memberType;
                        value = staticExtension;
                    }
                    handled = true;
                }

                if (handled)
                {
                    _xamlNodesWriter.WriteValue(value);
                    _xamlNodesWriter.WriteEndMember();

                    return;
                }
            }

            if (!handled)
            {
                _xamlNodesWriter.WriteStartObject(extensionType);
                _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);

                if (isValueStaticExtension)
                {
                    Type ownerType = null;
                    object providedValue;
                    value = GetStaticExtensionValue(valueId, out ownerType, out providedValue);

                    // If it's a Known command or a SystemResourceKey, send the value across directly
                    if (providedValue != null)
                    {
                        _xamlNodesWriter.WriteValue(providedValue);
                    }
                    else
                    {
                        // Special case for {x:Static ...} inside the main extension
                        _xamlNodesWriter.WriteStartObject(XamlLanguage.Static);
                        _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                        _xamlNodesWriter.WriteValue(value);
                        _xamlNodesWriter.WriteEndMember();

                        // In BAML scenario, we want to pass MemberType directly along cuz it's optimal
                        if (ownerType != null)
                        {
                            _xamlNodesWriter.WriteStartMember(BamlSchemaContext.StaticExtensionMemberTypeProperty);
                            _xamlNodesWriter.WriteValue(ownerType);
                            _xamlNodesWriter.WriteEndMember();
                        }
                        _xamlNodesWriter.WriteEndObject();
                    }
                }
                else if (isValueTypeExtension)
                {
                    // Special case for {x:Type ...} inside the main extension
                    _xamlNodesWriter.WriteStartObject(XamlLanguage.Type);
                    _xamlNodesWriter.WriteStartMember(BamlSchemaContext.TypeExtensionTypeProperty);

                    typeExtensionType = BamlSchemaContext.GetXamlType(valueId).UnderlyingType;
                    if (_isBinaryProvider)
                    {
                        _xamlNodesWriter.WriteValue(typeExtensionType);
                    }
                    else
                    {
                        _xamlNodesWriter.WriteValue(Logic_GetFullyQualifiedNameForType(BamlSchemaContext.GetXamlType(valueId)));
                    }
                    _xamlNodesWriter.WriteEndMember();
                    _xamlNodesWriter.WriteEndObject();
                }
                else
                {
                    // For all other scenarios, we want to just write out the type

                    if (extensionTypeId == Baml2006SchemaContext.TemplateBindingTypeId)
                    {
                        if (this._isBinaryProvider)
                        {
                            value = BitConverter.GetBytes(valueId);
                        }
                        else
                        {       
                            value = Logic_GetFullyQualifiedNameForMember(valueId);
                        }
                    }
                    else if (extensionTypeId == Baml2006SchemaContext.StaticExtensionTypeId)
                    {
                        // If we're here, that means we're not a binary provider which means we can't 
                        // support writing the provided value out directly.
                        object providedValue;
                        value = GetStaticExtensionValue(valueId, out memberType, out providedValue);
                    }
                    else if (extensionTypeId == Baml2006SchemaContext.TypeExtensionTypeId)
                    {
                        value = BamlSchemaContext.GetXamlType(valueId).UnderlyingType;
                    }
                    else
                    {
                        value = BamlSchemaContext.GetString(valueId);
                    }

                    _xamlNodesWriter.WriteValue(value);
                }

                _xamlNodesWriter.WriteEndMember();
                if (memberType != null)
                {
                    _xamlNodesWriter.WriteStartMember(BamlSchemaContext.StaticExtensionMemberTypeProperty);
                    _xamlNodesWriter.WriteValue(memberType);
                    _xamlNodesWriter.WriteEndMember();
                }
            }
            _xamlNodesWriter.WriteEndObject();
            _xamlNodesWriter.WriteEndMember();
        }

        // (property, value) value is a Type object
        private void Process_PropertyTypeReference()
        {
            Common_Process_Property();

            XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);
            XamlType type = BamlSchemaContext.GetXamlType(_binaryReader.ReadInt16());

            _xamlNodesWriter.WriteStartMember(property);


            if (_isBinaryProvider)
            {
                _xamlNodesWriter.WriteValue(type.UnderlyingType);
            }
            else
            {
                _xamlNodesWriter.WriteStartObject(XamlLanguage.Type);
                _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                _xamlNodesWriter.WriteValue(Logic_GetFullyQualifiedNameForType(type));
                _xamlNodesWriter.WriteEndMember();
                _xamlNodesWriter.WriteEndObject();
            }

            _xamlNodesWriter.WriteEndMember();
        }

        // (property, resourcekey)
        private void Process_PropertyWithStaticResourceId()
        {
            Common_Process_Property();

            short propertyId = _binaryReader.ReadInt16();
            Int16 resourceId = _binaryReader.ReadInt16();

            XamlMember property = _context.SchemaContext.GetProperty(propertyId, _context.CurrentFrame.XamlType);

            // PropertyWithStaticResourceId records live inside compiled resource Dictionary Entries.
            // The "Id" in ResourceId is the index into the KeyList[current].StaticResources[].
            // If you are reading the BAML in BRAT mode then the entries will be StaticResource
            // and OptimizedStaticResource elements.  (not ME's)
            // If the Dictionary KeyList was loaded into a ResourceDictionary's DeferredContent Property then the
            // KeyList[].StaticResource elements are processed and replaced with StaticResourceHolders. (an ME)
            Object resource = _context.KeyList[_context.CurrentKey - 1].StaticResources[resourceId];

            // resource is either a StaticResource, OptimizedStaticResource or StaticResourceHolder.
            // We store them in a List<Object> since they don't have/need a common base class
            if (resource is System.Windows.Markup.StaticResourceHolder)
            {
                _xamlNodesWriter.WriteStartMember(property);
                _xamlNodesWriter.WriteValue(resource);
                _xamlNodesWriter.WriteEndMember();
                return;
            }

            _xamlNodesWriter.WriteStartMember(property);
            _xamlNodesWriter.WriteStartObject(BamlSchemaContext.StaticResourceExtensionType);
            _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);

            OptimizedStaticResource optimizedResource = resource as OptimizedStaticResource;
            if (optimizedResource != null)
            {
                if (optimizedResource.IsKeyStaticExtension)
                {
                    Type memberType = null;
                    object providedValue;
                    string param = GetStaticExtensionValue(optimizedResource.KeyId, out memberType, out providedValue);

                    if (providedValue != null)
                    {
                        _xamlNodesWriter.WriteValue(providedValue);
                    }
                    else
                    {
                        _xamlNodesWriter.WriteStartObject(XamlLanguage.Static);
                        _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                        _xamlNodesWriter.WriteValue(param);
                        _xamlNodesWriter.WriteEndMember();
                        if (memberType != null)
                        {
                            _xamlNodesWriter.WriteStartMember(BamlSchemaContext.StaticExtensionMemberTypeProperty);
                            _xamlNodesWriter.WriteValue(memberType);
                            _xamlNodesWriter.WriteEndMember();
                        }
                        _xamlNodesWriter.WriteEndObject();
                    }
                }
                else if (optimizedResource.IsKeyTypeExtension)
                {
                    if (_isBinaryProvider)
                    {
                        XamlType xamlType = BamlSchemaContext.GetXamlType(optimizedResource.KeyId);
                        _xamlNodesWriter.WriteValue(xamlType.UnderlyingType);
                    }
                    else
                    {
                        _xamlNodesWriter.WriteStartObject(XamlLanguage.Type);
                        _xamlNodesWriter.WriteStartMember(XamlLanguage.PositionalParameters);
                        _xamlNodesWriter.WriteValue(Logic_GetFullyQualifiedNameForType(BamlSchemaContext.GetXamlType(optimizedResource.KeyId)));
                        _xamlNodesWriter.WriteEndMember();
                        _xamlNodesWriter.WriteEndObject();
                    }
                }
                else
                {
                    string text = _context.SchemaContext.GetString(optimizedResource.KeyId);
                    _xamlNodesWriter.WriteValue(text);
                }
            }
            else
            {
                StaticResource sr = resource as StaticResource;
                Debug.Assert(sr != null);
                XamlServices.Transform(sr.ResourceNodeList.GetReader(), _xamlNodesWriter, false);
            }
            _xamlNodesWriter.WriteEndMember();
            _xamlNodesWriter.WriteEndObject();
            _xamlNodesWriter.WriteEndMember();
        }

        // Complex property (Canvas.Left)
        private void Process_PropertyComplexStart()
        {
            Common_Process_Property();

            XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);

            _context.CurrentFrame.Member = property;
            _xamlNodesWriter.WriteStartMember(property);
        }

        // Start inserting lots of implict items properties
        private void Process_PropertyArrayStart()
        {
            Common_Process_Property();

            XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);

            _context.CurrentFrame.Member = property;
            _xamlNodesWriter.WriteStartMember(property);
        }

        private void Process_PropertyIDictionaryStart()
        {
            Common_Process_Property();

            XamlMember property = GetProperty(_binaryReader.ReadInt16(), _context.CurrentFrame.XamlType);

            _context.CurrentFrame.Member = property;
            _xamlNodesWriter.WriteStartMember(property);
        }

        private void Process_PropertyEnd()
        {
            RemoveImplicitFrame();

            _context.CurrentFrame.Member = null;
            _xamlNodesWriter.WriteEndMember();
        }

        private void Process_PropertyIDictionaryEnd()
        {
            // If we saved the Node stream for the first ME element of a dictionary to 
            // check if it had a key then process that now.
            if (_lookingForAKeyOnAMarkupExtensionInADictionaryDepth == _context.CurrentFrame.Depth)
            {
                RestoreSavedFirstItemInDictionary();
            }

            RemoveImplicitFrame();

            _context.CurrentFrame.Member = null;
            _xamlNodesWriter.WriteEndMember();
        }

        private string Logic_GetFullyQualifiedNameForMember(Int16 propertyId)
        {
            return Logic_GetFullyQualifiedNameForType(BamlSchemaContext.GetPropertyDeclaringType(propertyId)) + "." +
                BamlSchemaContext.GetPropertyName(propertyId, false);
        }

        private string Logic_GetFullyQualifiedNameForType(XamlType type)
        {
            Baml2006ReaderFrame currentFrame = _context.CurrentFrame;

            IList<string> xamlNamespaces = type.GetXamlNamespaces();

            while (currentFrame != null)
            {
                foreach(string xmlns in xamlNamespaces)
                {
                    string prefix = null;

                    if (currentFrame.TryGetPrefixByNamespace(xmlns, out prefix))
                    {
                        if (String.IsNullOrEmpty(prefix))
                        {
                            return type.Name;
                        }
                        else
                        {
                            return prefix + ":" + type.Name;
                        }
                    }
                }

                currentFrame = (Baml2006ReaderFrame)currentFrame.Previous;
            }

            throw new InvalidOperationException("Could not find prefix for type: " + type.Name);
        }

        private string Logic_GetFullXmlns(string uriInput)
        {
            const string clrNamespace = "clr-namespace:";
            const string assembly = "assembly";

            if (uriInput.StartsWith(clrNamespace, StringComparison.Ordinal))
            {
                //We have a clr-namespace so do special processing
                int semicolonIdx = uriInput.IndexOf(';', clrNamespace.Length);
                if (semicolonIdx < 0)
                {
                    // We need to append local assembly
                    return (_settings.LocalAssembly != null)
                                            ? uriInput + ";assembly=" + GetAssemblyNameForNamespace(_settings.LocalAssembly)
                                            : uriInput;
                }
                else
                {
                    int assemblyKeywordStartIdx = semicolonIdx + 1;
                    int equalIdx = uriInput.IndexOf('=', semicolonIdx);
                    if (equalIdx < 0)
                    {
                        ThrowMissingTagInNamespace(uriInput);
                    }

                    int assemblyTagLength = equalIdx - assemblyKeywordStartIdx;
                    if (assemblyTagLength != assembly.Length ||
                        string.CompareOrdinal(uriInput, assemblyKeywordStartIdx, assembly, 0, assembly.Length) != 0)
                    {
                        ThrowAssemblyTagMissing(uriInput);
                    }

                    if (uriInput.Length == equalIdx + 1)
                    {
                        return uriInput + GetAssemblyNameForNamespace(_settings.LocalAssembly);
                    }
                }
            }

            return uriInput;

            static void ThrowAssemblyTagMissing(string uriInput)
            {
                throw new ArgumentException(SR.Get(SRID.AssemblyTagMissing, "assembly", uriInput));
            }

            static void ThrowMissingTagInNamespace(string uriInput)
            {
                throw new ArgumentException(SR.Get(SRID.MissingTagInNamespace, "=", uriInput));
            }
        }

        //  Providing the assembly short name may lead to ambiguity between two versions of the same assembly, but we need to
        // keep it this way since it is exposed publicly via the Namespace property, Baml2006ReaderInternal provides the full Assembly name.
        // We need to avoid Assembly.GetName() so we run in PartialTrust without asserting.
        internal virtual string GetAssemblyNameForNamespace(Assembly assembly)
        {
            string assemblyLongName = assembly.FullName;
            string assemblyShortName = assemblyLongName.Substring(0, assemblyLongName.IndexOf(','));
            return assemblyShortName;
        }

        // (prefix, namespaceUri)
        private void Process_XmlnsProperty()
        {
            Debug.Assert(_context.CurrentFrame.XamlType != null, "BAML Xmlns record is only legal between ElementStart and ElementEnd");

            Read_RecordSize();
            string prefix = _binaryReader.ReadString();
            string xamlNs = _binaryReader.ReadString();
            
            xamlNs = Logic_GetFullXmlns(xamlNs);

            _context.CurrentFrame.AddNamespace(prefix, xamlNs);

            NamespaceDeclaration namespaceDeclaration = new NamespaceDeclaration(xamlNs, prefix);
            _xamlNodesWriter.WriteNamespace(namespaceDeclaration);

            // Record format:
            // num of records : short
            // assemblyId : short
            // ...
            // assemblyId : short
            short recordSize = _binaryReader.ReadInt16();
            if (xamlNs.StartsWith(XamlReaderHelper.MappingProtocol, StringComparison.Ordinal))
            {
                SkipBytes(recordSize * 2);  // Each entry is 2 bytes
            }
            else if (recordSize > 0)
            {
                short[] assemblies = new short[recordSize];
                for (int i = 0; i < recordSize; i++)
                {
                    assemblies[i] = _binaryReader.ReadInt16();
                }

                BamlSchemaContext.AddXmlnsMapping(xamlNs, assemblies);
            }
        }

        // offset
        private void Process_LinePosition()
        {
            _context.LineOffset = _binaryReader.ReadInt32();
            // We do this cast on every line info, but that is harmless for perf since line info is only in debug build
            IXamlLineInfoConsumer consumer = _xamlNodesWriter as IXamlLineInfoConsumer;
            if (consumer != null)
            {
                consumer.SetLineInfo(_context.LineNumber, _context.LineOffset);
            }
        }

        // (line, offset)
        private void Process_LineNumberAndPosition()
        {
            _context.LineNumber = _binaryReader.ReadInt32();
            _context.LineOffset = _binaryReader.ReadInt32();
            // We do this cast on every line info, but that is harmless for perf since line info is only in debug build
            IXamlLineInfoConsumer consumer = _xamlNodesWriter as IXamlLineInfoConsumer;
            if (consumer != null)
            {
                consumer.SetLineInfo(_context.LineNumber, _context.LineOffset);
            }
        }

        private void Process_PIMapping()
        {
            Read_RecordSize();
            string xmlNamespace = _binaryReader.ReadString();
            string clrNamespace = _binaryReader.ReadString();
            Int16 assemblyId = _binaryReader.ReadInt16();
        }

        private void Process_AssemblyInfo()
        {
            Read_RecordSize();
            Int16 assemblyId = _binaryReader.ReadInt16();
            string assemblyName = _binaryReader.ReadString();

            BamlSchemaContext.AddAssembly(assemblyId, assemblyName);
        }

        private void Process_TypeInfo()
        {
            Read_RecordSize();
            Int16 typeId = _binaryReader.ReadInt16();
            Int16 assemblyId = _binaryReader.ReadInt16();
            string typeName = _binaryReader.ReadString();
            Baml2006SchemaContext.TypeInfoFlags flags = (Baml2006SchemaContext.TypeInfoFlags)(assemblyId >> 12);
            assemblyId &= 0x0FFF;

            BamlSchemaContext.AddXamlType(typeId, assemblyId, typeName, flags);
        }

        private void Process_TypeSerializerInfo()
        {
            Read_RecordSize();
            Int16 typeId = _binaryReader.ReadInt16();
            Int16 assemblyId = _binaryReader.ReadInt16();
            string typeName = _binaryReader.ReadString();
            Int16 serializerId = _binaryReader.ReadInt16();   // currently not used.  (found through reflection)
            Baml2006SchemaContext.TypeInfoFlags flags = (Baml2006SchemaContext.TypeInfoFlags)(assemblyId >> 12);
            assemblyId &= 0x0FFF;

            BamlSchemaContext.AddXamlType(typeId, assemblyId, typeName, flags);
        }

        private void Process_AttributeInfo()
        {
            Read_RecordSize();
            short propertyId = _binaryReader.ReadInt16();
            Int16 declaringTypeId = _binaryReader.ReadInt16();
            byte usage = _binaryReader.ReadByte();  
            string propertyName = _binaryReader.ReadString();

            BamlSchemaContext.AddProperty(propertyId, declaringTypeId, propertyName);
        }

        private void Process_StringInfo()
        {
            Read_RecordSize();
            Int16 id = _binaryReader.ReadInt16();
            string value = _binaryReader.ReadString();

            BamlSchemaContext.AddString(id, value);
        }

        private void Process_ContentProperty()
        {
            Int16 propertyId = _binaryReader.ReadInt16();

            // Ignore the VisualTree property for FrameworkTemplates
            if (propertyId != Baml2006SchemaContext.KnownTypes.VisualTreeKnownPropertyId)
            {
                XamlMember contentProperty = GetProperty(propertyId, false);
                WpfXamlMember wpfProperty = contentProperty as WpfXamlMember;
                if (wpfProperty != null)
                {
                    contentProperty = wpfProperty.AsContentProperty;
                }
                _context.CurrentFrame.ContentProperty = contentProperty;
            }
        }

        private void Process_ConnectionId()
        {
            int connectionId = _binaryReader.ReadInt32();

            if (_context.CurrentFrame.Member != null)
            {
                // ConnectionId could come in the middle of a collection.  In that case, we must wait until the end of the
                // collection to set the ConnectionId.

                Baml2006ReaderFrame frame = _context.CurrentFrame;
                if (frame.Flags == Baml2006ReaderFrameFlags.IsImplict)
                {
                    frame = _context.PreviousFrame;
                }
                frame.DelayedConnectionId = connectionId;
            }
            else
            {
                Common_Process_Property();

                _xamlNodesWriter.WriteStartMember(XamlLanguage.ConnectionId);
                if (_isBinaryProvider)
                {
                    _xamlNodesWriter.WriteValue(connectionId);
                }
                else
                {
                    _xamlNodesWriter.WriteValue(connectionId.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS));
                }
                _xamlNodesWriter.WriteEndMember();
            }
        }

        private Baml2006RecordType Read_RecordType()
        {
            byte value = _binaryReader.ReadByte();
            if (value < 0)
            {
                return Baml2006RecordType.DocumentEnd;
            }

            return (Baml2006RecordType)value;
        }

        private int Read_RecordSize()
        {
            long offset = _binaryReader.BaseStream.Position;
            int value = _binaryReader.Read7BitEncodedInt();
            int sizeOfEncoding = (int)(_binaryReader.BaseStream.Position - offset);
            if (sizeOfEncoding == 1)
            {
                return value;
            }
            else
            {
                return value - sizeOfEncoding + 1;
            }
        }

        private void SkipBytes(long offset)
        {
            _binaryReader.BaseStream.Seek(offset, SeekOrigin.Current);
        }

        private void RemoveImplicitFrame()
        {
            if (_context.CurrentFrame.Flags == Baml2006ReaderFrameFlags.IsImplict)
            {
                _xamlNodesWriter.WriteEndMember();
                _xamlNodesWriter.WriteEndObject();

                _context.PopScope();
            }

            if (_context.CurrentFrame.Flags == Baml2006ReaderFrameFlags.HasImplicitProperty)
            {
                // If we are encoding a template there is some extra checking.
                if (_context.CurrentFrame.Depth == _context.TemplateStartDepth)
                {
                    // If the template is done.  Switch back to the previous writer.
                    // Write the spooled Template Node List as a single Value.
                    _xamlNodesWriter.Close();
                    _xamlNodesWriter = _xamlWriterStack.Pop();
                    _xamlNodesWriter.WriteValue(_xamlTemplateNodeList);
                    _xamlTemplateNodeList = null;
                    _context.TemplateStartDepth = -1;
                }

                _xamlNodesWriter.WriteEndMember();

                _context.CurrentFrame.Member = null;
                _context.CurrentFrame.Flags = Baml2006ReaderFrameFlags.None;
            }
        }

        private void InjectPropertyAndFrameIfNeeded(XamlType elementType, SByte flags)
        {
            // If we saved the Node stream for the first ME element of a dictionary to 
            // check if it had a key then process that now.
            if (_lookingForAKeyOnAMarkupExtensionInADictionaryDepth == _context.CurrentFrame.Depth)
            {
                RestoreSavedFirstItemInDictionary();
            }

            XamlType parentType = _context.CurrentFrame.XamlType;
            XamlMember parentProperty = _context.CurrentFrame.Member;

            if (parentType != null)
            {
                if (parentProperty == null)
                {
                    // We have got two consecutive ElementStart records
                    // We must insert an implicit content property between them
                    if (_context.CurrentFrame.ContentProperty != null)
                    {
                        _context.CurrentFrame.Member = parentProperty = _context.CurrentFrame.ContentProperty;
                    }
                    else if (parentType.ContentProperty != null)
                    {
                        _context.CurrentFrame.Member = parentProperty = parentType.ContentProperty;
                    }
                    else
                    {
                        if (parentType.IsCollection || parentType.IsDictionary)
                        {
                            _context.CurrentFrame.Member = parentProperty = XamlLanguage.Items;
                        }
                        else if (parentType.TypeConverter != null)
                        {
                            _context.CurrentFrame.Member = parentProperty = XamlLanguage.Initialization;
                        }
                        else
                        {
                            throw new XamlParseException(SR.Get(SRID.RecordOutOfOrder, parentType.Name));
                        }
                    }
                    _context.CurrentFrame.Flags = Baml2006ReaderFrameFlags.HasImplicitProperty;
                    _xamlNodesWriter.WriteStartMember(parentProperty);

                    // if we are NOT already spooling a template
                    if (_context.TemplateStartDepth < 0 && _isBinaryProvider)
                    {
                        if (parentProperty == BamlSchemaContext.FrameworkTemplateTemplateProperty)
                        {
                            // If this is a template then spool the template into a Node List.
                            _context.TemplateStartDepth = _context.CurrentFrame.Depth;
                            _xamlTemplateNodeList = new XamlNodeList(_xamlNodesWriter.SchemaContext);

                            _xamlWriterStack.Push(_xamlNodesWriter);
                            _xamlNodesWriter = _xamlTemplateNodeList.Writer;

                            if (XamlSourceInfoHelper.IsXamlSourceInfoEnabled)
                            {
                                // Push the current line info in the new XamlNodeList
                                // This is needed to ensure that template root element carries a line info
                                // which can then be used when it is instantiated
                                IXamlLineInfoConsumer consumer = _xamlNodesWriter as IXamlLineInfoConsumer;
                                if (consumer != null)
                                {
                                    consumer.SetLineInfo(_context.LineNumber, _context.LineOffset);
                                }
                            }
                        }
                    }
                }

                XamlType parentPropertyType = parentProperty.Type;
                // Normally an error except for collections
                if (parentPropertyType != null && (parentPropertyType.IsCollection || parentPropertyType.IsDictionary) &&
                    !parentProperty.IsDirective && (flags & ReaderFlags_AddedToTree) == 0)
                {
                    bool emitPreamble = false;

                    // If the collection property is Readonly then "retrieve" the collection.
                    if (parentProperty.IsReadOnly)
                    {
                        emitPreamble = true;
                    }
                    // OR if the Value isn't assignable to the Collection emit the preable.
                    else if (!elementType.CanAssignTo(parentPropertyType))
                    {
                        // UNLESS: the Value is a Markup extension, then it is assumed that
                        // the ProvidValue type will be AssignableFrom.
                        if (!elementType.IsMarkupExtension)
                        {
                            emitPreamble = true;
                        }
                        // EXCEPT: if the BAML said it was Retrived
                        else if (_context.CurrentFrame.Flags == Baml2006ReaderFrameFlags.HasImplicitProperty)
                        {
                            emitPreamble = true;
                        }
                        // OR: the ME is Array
                        else if (elementType == XamlLanguage.Array)
                        {
                            emitPreamble = true;
                        }
                    }
                    if (emitPreamble)
                    {
                        EmitGoItemsPreamble(parentPropertyType);
                    }

                    // We may need to look for an x:Key on the ME in a dictionary.
                    // so save up the node stream for the whole element definition and check it
                    // for an x:Key later.
                    if (!emitPreamble && parentPropertyType.IsDictionary && elementType.IsMarkupExtension)
                    {
                        StartSavingFirstItemInDictionary();
                    }
                }
            }
        }

        private void StartSavingFirstItemInDictionary()
        {
            _lookingForAKeyOnAMarkupExtensionInADictionaryDepth = _context.CurrentFrame.Depth;
            _lookingForAKeyOnAMarkupExtensionInADictionaryNodeList = new XamlNodeList(_xamlNodesWriter.SchemaContext);
            _xamlWriterStack.Push(_xamlNodesWriter);
            _xamlNodesWriter = _lookingForAKeyOnAMarkupExtensionInADictionaryNodeList.Writer;
        }

        private void RestoreSavedFirstItemInDictionary()
        {
            // Restore the real (previous) output node stream.
            _xamlNodesWriter.Close();
            _xamlNodesWriter = _xamlWriterStack.Pop();

            // Check in the saved nodes if the x:Key was set and if it was insert a "GO;SM _items"
            if (NodeListHasAKeySetOnTheRoot(_lookingForAKeyOnAMarkupExtensionInADictionaryNodeList.GetReader()))
            {
                EmitGoItemsPreamble(_context.CurrentFrame.Member.Type);
            }

            // dump the saved nodes into the real (previous) output node stream.
            XamlReader lookAheadNodesReader = _lookingForAKeyOnAMarkupExtensionInADictionaryNodeList.GetReader();
            XamlServices.Transform(lookAheadNodesReader, _xamlNodesWriter, false);
            _lookingForAKeyOnAMarkupExtensionInADictionaryDepth = -1;
        }

        private void EmitGoItemsPreamble(XamlType parentPropertyType)
        {
            _context.PushScope();
            _context.CurrentFrame.XamlType = parentPropertyType;
            _xamlNodesWriter.WriteGetObject();
            _context.CurrentFrame.Flags = Baml2006ReaderFrameFlags.IsImplict;

            _context.CurrentFrame.Member = XamlLanguage.Items;
            _xamlNodesWriter.WriteStartMember(_context.CurrentFrame.Member);
        }

        private StaticResource GetLastStaticResource()
        {
            return _context.LastKey.LastStaticResource;
        }

        private string GetTextFromBinary(byte[] bytes,
                                        Int16 serializerId,
                                        XamlMember property,
                                        XamlType type)
        {
            switch (serializerId)
            {
                case Baml2006SchemaContext.KnownTypes.BooleanConverter:
                    return (bytes[0] == 0) ? false.ToString() : true.ToString();

                case Baml2006SchemaContext.KnownTypes.EnumConverter:
                    return Enum.ToObject(type.UnderlyingType, bytes).ToString();

                case Baml2006SchemaContext.KnownTypes.XamlBrushSerializer:
                    using (MemoryStream memStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader binReader = new BinaryReader(memStream))
                        {
                            System.Windows.Media.SolidColorBrush brush = System.Windows.Media.SolidColorBrush.DeserializeFrom(binReader) as System.Windows.Media.SolidColorBrush;
                            return brush.ToString();
                        }
                    }

                case Baml2006SchemaContext.KnownTypes.XamlPathDataSerializer:
                    using (MemoryStream memStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader binReader = new BinaryReader(memStream))
                        {
                            System.Windows.Markup.XamlPathDataSerializer serializer = 
                                new System.Windows.Markup.XamlPathDataSerializer();
                            object o = serializer.ConvertCustomBinaryToObject(binReader);
                            return o.ToString();
                        }
                    }

                case Baml2006SchemaContext.KnownTypes.XamlPoint3DCollectionSerializer:
                    using (MemoryStream memStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader binReader = new BinaryReader(memStream))
                        {
                            System.Windows.Markup.XamlPoint3DCollectionSerializer serializer =
                                new System.Windows.Markup.XamlPoint3DCollectionSerializer();
                            object o = serializer.ConvertCustomBinaryToObject(binReader);
                            return o.ToString();
                        }
                    }

                case Baml2006SchemaContext.KnownTypes.XamlVector3DCollectionSerializer:
                    using (MemoryStream memStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader binReader = new BinaryReader(memStream))
                        {
                            System.Windows.Markup.XamlVector3DCollectionSerializer serializer = 
                                new System.Windows.Markup.XamlVector3DCollectionSerializer();
                            object o = serializer.ConvertCustomBinaryToObject(binReader);
                            return o.ToString();
                        }
                    }

                case Baml2006SchemaContext.KnownTypes.XamlPointCollectionSerializer:
                    using (MemoryStream memStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader binReader = new BinaryReader(memStream))
                        {
                            System.Windows.Markup.XamlPointCollectionSerializer serializer =
                                new System.Windows.Markup.XamlPointCollectionSerializer();
                            object o = serializer.ConvertCustomBinaryToObject(binReader);
                            return o.ToString();
                        }
                    }

                case Baml2006SchemaContext.KnownTypes.XamlInt32CollectionSerializer:
                    using (MemoryStream memStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader binReader = new BinaryReader(memStream))
                        {
                            System.Windows.Markup.XamlInt32CollectionSerializer serializer = 
                                new System.Windows.Markup.XamlInt32CollectionSerializer();
                            object o = serializer.ConvertCustomBinaryToObject(binReader);
                            return o.ToString();
                        }
                    }

                case 0:
                case Baml2006SchemaContext.KnownTypes.DependencyPropertyConverter:
                    Debug.Assert(property.Type.UnderlyingType == typeof(DependencyProperty));
                    if (bytes.Length == 2)
                    {
                        Int16 propId = (short)(bytes[0] | (bytes[1] << 8));
                        return Logic_GetFullyQualifiedNameForMember(propId);
                    }
                    else
                    {
                        using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
                        {
                            XamlType declaringType = BamlSchemaContext.GetXamlType(reader.ReadInt16());
                            string propertyName = reader.ReadString();

                            return Logic_GetFullyQualifiedNameForType(declaringType) + "." + propertyName;
                        }
                    }

                default:
                    throw new NotImplementedException();
            }
        }


        private string GetStaticExtensionValue(short valueId, out Type memberType, out object providedValue)
        {
            string currentText = "";
            memberType = null;
            providedValue = null;

            if (valueId < 0)
            {
                valueId = (short)-valueId;
                bool isKey = true;

                // this is a known StaticExtension param.
                // if keyId is more than the range it is the actual resource, 
                // else it is the key.
                // if keyId is more than the range it is the actual resource, else it is the key.

                valueId = SystemResourceKey.GetSystemResourceKeyIdFromBamlId(valueId, out isKey);

                if (valueId > (short)SystemResourceKeyID.InternalSystemColorsStart && valueId < (short)SystemResourceKeyID.InternalSystemColorsExtendedEnd)
                {
                    if (_isBinaryProvider)
                    {
                        if (isKey)
                        {
                            providedValue = SystemResourceKey.GetResourceKey(valueId);
                        }
                        else
                        {
                            providedValue = SystemResourceKey.GetResource(valueId);
                        }
                    }
                    else
                    {
                        System.Windows.SystemResourceKeyID keyId = (System.Windows.SystemResourceKeyID)valueId;
                        XamlType type = _context.SchemaContext.GetXamlType(System.Windows.Markup.SystemKeyConverter.GetSystemClassType(keyId));
                        currentText = Logic_GetFullyQualifiedNameForType(type) + ".";

                        if (isKey)
                        {
                            currentText += System.Windows.Markup.SystemKeyConverter.GetSystemKeyName(keyId);
                        }
                        else
                        {
                            currentText += System.Windows.Markup.SystemKeyConverter.GetSystemPropertyName(keyId);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.BamlBadExtensionValue));
                }
            }
            else
            {         
                if (_isBinaryProvider)
                {
                    memberType = BamlSchemaContext.GetPropertyDeclaringType(valueId).UnderlyingType;
                    currentText = BamlSchemaContext.GetPropertyName(valueId, false);

                    providedValue = System.Windows.Input.CommandConverter.GetKnownControlCommand(memberType, currentText);
                }
                else
                {
                    currentText = Logic_GetFullyQualifiedNameForMember(valueId);
                }
            }

            return currentText;
        }

        private bool NodeListHasAKeySetOnTheRoot(XamlReader reader)
        {
            int depth = 0;
            while (reader.Read())
            {
                switch(reader.NodeType)
                {
                    case XamlNodeType.StartObject:
                        depth += 1;
                        break;

                    case XamlNodeType.EndObject:
                        depth -= 1;
                        break;

                    case XamlNodeType.StartMember:
                        if (reader.Member == XamlLanguage.Key)
                        {
                            if (depth == 1)
                            {
                                return true;
                            }
                        }
                        break;
                }
            }
            return false;
        }


        #endregion

        // Set/Get whether or not Freezables within the current scope
        // should be Frozen.
        internal bool FreezeFreezables
        {
            get { return _context.CurrentFrame.FreezeFreezables; }
            set { _context.CurrentFrame.FreezeFreezables = value; }
        }


        #region IFreezeFreezables Members

        bool IFreezeFreezables.FreezeFreezables
        {
            get { return _context.CurrentFrame.FreezeFreezables; }
        }

        bool IFreezeFreezables.TryFreeze(string value, Freezable freezable)
        {
            // We don't check FreezeFreezables since this is used only by the BrushBinary deserializer interanally
            // It will check FreezeFreezables.
            if (freezable.CanFreeze)
            {
                if (!freezable.IsFrozen)
                {
                    freezable.Freeze();
                }
                if (_freezeCache == null)
                {
                    _freezeCache = new Dictionary<string, Freezable>();
                }
                _freezeCache.Add(value, freezable);
                return true;
            }

            return false;
        }

        Freezable IFreezeFreezables.TryGetFreezable(string value)
        {
            Freezable freezable = null;
            if (_freezeCache != null)
            {
                _freezeCache.TryGetValue(value, out freezable);
            }

            return freezable;
        }

        #endregion

        #region Private Data

        private Baml2006SchemaContext BamlSchemaContext
        {
            get { return (Baml2006SchemaContext)SchemaContext; }
        }

        #endregion
    }
}
