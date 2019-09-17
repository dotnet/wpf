// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* Purpose:  Main class to handle reading a Baml file
*
\***************************************************************************/

using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Globalization;
using MS.Utility;
using MS.Internal;
using System.Runtime.InteropServices;
using MS.Internal.Utility;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Documents;
using System.Windows.Controls;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Markup
{
    // Unlike the tokenizer and the writer, the reader knows the difference between CLR
    // object and DependencyObjects.  This is done because DependencyObjects support a
    // superset of CLR functionality, such as the ability to quickly set a property
    // using DependencyObject.SetValue(DependencyProperty, object)
    internal enum ReaderFlags : ushort
    {
        // Context types
        Unknown                   = 0x0000,

        DependencyObject          = 0x1000,
        ClrObject                 = 0x2000,

        PropertyComplexClr        = 0x3000,
        PropertyComplexDP         = 0x4000,

        PropertyArray             = 0x5000,
        PropertyIList             = 0x6000,
        PropertyIDictionary       = 0x7000,
        PropertyIAddChild         = 0x8000,

        RealizeDeferContent       = 0x9000,

        ConstructorParams         = 0xA000,

        ContextTypeMask           = 0xF000,

        StyleObject               = 0x0100,
        FrameworkTemplateObject   = 0x0200,
        TableTemplateObject       = 0x0400,
        SingletonConstructorParam = 0x0800,

        // Element flags
        NeedToAddToTree           = 0x0001,    // Need to add to element tree, but haven't yet
        AddedToTree               = 0x0002,    // Has already been added to element tree, so don't do it again
        InjectedElement           = 0x0004,    // Context was an injected element, so skip over it
        CollectionHolder          = 0x0008,
        IDictionary               = 0x0010,
        IList                     = 0x0020,
        ArrayExt                  = 0x0040,
        IAddChild                 = 0x0080,
    }

    /// <summary>
    /// reads BAML from a Stream
    /// This is an internal class
    /// </summary>
    internal class BamlRecordReader
    {
#region Constructor
        /// <summary>
        /// Eventually should need xamltypemapper when finish compiler integration
        /// and namespaceMaps are written to Baml.
        /// </summary>summary>
        internal BamlRecordReader(
            Stream        bamlStream,
            ParserContext parserContext)
            : this(bamlStream,parserContext,true)
        {
            XamlParseMode = XamlParseMode.Synchronous;
        }

        internal BamlRecordReader(
            Stream           bamlStream,
            ParserContext    parserContext,
            object           root)
        {
            Debug.Assert(null != bamlStream);
            Debug.Assert(null != parserContext && null != parserContext.XamlTypeMapper);

            ParserContext = parserContext;
            _rootElement = root;
            _bamlAsForest = (root != null);
            if (_bamlAsForest)
            {
                ParserContext.RootElement = _rootElement;
            }
            _rootList = new ArrayList(1);
            BamlStream = bamlStream;
        }

        /// <summary>
        /// BamlRecordReader constructor
        /// </summary>
        /// <param name="bamlStream">The input BAML stream</param>
        /// <param name="parserContext">The parser context</param>
        /// <param name="loadMapper">Ensure parser context has same XamlTypeMapper and
        ///                            map table as this reader</param>
        internal BamlRecordReader(
            Stream           bamlStream,
            ParserContext    parserContext,
            bool             loadMapper)
        {
            Debug.Assert(null != parserContext && null != parserContext.XamlTypeMapper);

            ParserContext = parserContext;
            _rootList = new ArrayList(1);
            BamlStream = bamlStream;

            if (loadMapper)
            {
                ParserContext.XamlTypeMapper = XamlTypeMapper;
            }
        }

        /// <summary>
        /// Default internal constructor
        /// </summary>
        protected internal BamlRecordReader()
        {
        }

#endregion Constructor

#region Methods

        /// <summary>
        /// Set up the XamlTypeMapper and baml map table prior to starting to read.
        /// </summary>
        internal void Initialize()
        {
            MapTable.Initialize();
            XamlTypeMapper.Initialize();
            ParserContext.Initialize();
        }

        /// <summary>
        /// Array of root objects contained in the baml stream.  This is added to
        /// as the baml is loaded.
        /// </summary>
        internal ArrayList RootList
        {
            get { return _rootList; }
            set { _rootList = value; }
        }


        /// <summary>
        /// True if tree is to be built strictly (well, more or less strictly)
        /// top down.
        /// </summary>
        internal bool BuildTopDown
        {
            get { return _buildTopDown; }
            set { _buildTopDown = value; }
        }

        internal int BytesAvailible
        {
            get
            {
                Stream stream = BinaryReader.BaseStream;
                return (int)(stream.Length - stream.Position);
            }
        }

        /// <summary>
        /// Read a BamlRecord from the underlying binary stream and return it.
        /// If we're at the end of the stream, return null.
        /// </summary>summary>
        internal BamlRecord GetNextRecord()
        {
            BamlRecord bamlRecord = null;

            if (null == PreParsedRecordsStart)
            {
                Stream stream = BinaryReader.BaseStream;

                // If we have a ReaderStream we know we are coming from XAML (check if can read full record)
                // When reading BAML we don't get a ReaderStream so:
                //      network - should check for number of bits downloaded but currently
                //          no way to get this information. For now read sync.
                //          NOTE:  This has to be addressed if we want async download
                //                 of BAML.
                //      local file - in memory or something, so else read the record sync.

                if (null != XamlReaderStream)
                {
                    long currentPosition = stream.Position;
                    long bytesAvailable = stream.Length - currentPosition;

                    // Make sure there is room for the record type in the stream.
                    if (BamlRecord.RecordTypeFieldLength > bytesAvailable)
                    {
                        return null;
                    }

                    BamlRecordType recordType = (BamlRecordType)BinaryReader.ReadByte();

                    // We've read the record type, so decrement available bytes.
                    bytesAvailable -= BamlRecord.RecordTypeFieldLength;

                    // call GetNextRecord passing in the record type.  If this returns null,
                    // then the complete record was not yet available and could not be
                    // read.
                    bamlRecord = ReadNextRecordWithDebugExtension(bytesAvailable, recordType);

                    if (bamlRecord == null)
                    {
#if DEBUG
                        // this case can happen if doing BAML Async and entire record hasn't
                        // been downloaded.
                        Debug.Assert(false == XamlReaderStream.IsWriteComplete,
                                "not enough bytes for RecordSize but write is complete");
#endif
                        stream.Seek(currentPosition,SeekOrigin.Begin);
                        return null;
                    }

                    // tell stream we are done with these file bits.
                    XamlReaderStream.ReaderDoneWithFileUpToPosition(stream.Position -1);
                }
                else
                {
                    // default to reading a single record synchronous.  Don't attempt
                    // to read if its already at the end of the stream.
                    bool keepOnReading = true;
                    while (keepOnReading)
                    {
                        if (BinaryReader.BaseStream.Length >
                            BinaryReader.BaseStream.Position)
                        {
                            // If we are supposed to skip info records, then just advance the stream
                            // for info records and continue until we get a non-info record, or we
                            // run out of stream data to read.
                            BamlRecordType recordType = (BamlRecordType)BinaryReader.ReadByte();
                            bamlRecord = ReadNextRecordWithDebugExtension(Int64.MaxValue, recordType);
                            keepOnReading = false;
                        }
                        else
                        {
                            keepOnReading = false;
                        }
                    }
                }
            }
            else if (PreParsedCurrentRecord != null) // If the preparsed list has not reached its end
            {
                bamlRecord = PreParsedCurrentRecord;   // return the record pointed to index
                PreParsedCurrentRecord = PreParsedCurrentRecord.Next;

                // if the next record is a debug record then process it and advance over it.
                // The Debug record extension record is process BEFORE the current record because
                // it is debug information regarding the current record.
                if (BamlRecordHelper.HasDebugExtensionRecord(ParserContext.IsDebugBamlStream, bamlRecord))
                {
                    ProcessDebugBamlRecord(PreParsedCurrentRecord);
                    PreParsedCurrentRecord = PreParsedCurrentRecord.Next;
                }
            }
            return bamlRecord;
        }

        internal BamlRecord ReadNextRecordWithDebugExtension(
            long           bytesAvailable,
            BamlRecordType recordType)
        {
            BamlRecord bamlRecord = BamlRecordManager.ReadNextRecord(BinaryReader, bytesAvailable, recordType);
            if (IsDebugBamlStream)
            {
                if (BamlRecordHelper.DoesRecordTypeHaveDebugExtension(bamlRecord.RecordType))
                {
                    BamlRecord debugExtensionRecord = ReadDebugExtensionRecord();
                    bamlRecord.Next = debugExtensionRecord;
                }
            }
            return bamlRecord;
        }

        internal BamlRecord ReadDebugExtensionRecord()
        {
            Stream stream = BinaryReader.BaseStream;
            long bytesAvailable = stream.Length - stream.Position;
            if(bytesAvailable == 0)
                return null;

            BamlRecordType nextRecordType = (BamlRecordType)BinaryReader.ReadByte();

            if (BamlRecordHelper.IsDebugBamlRecordType(nextRecordType))
            {
                BamlRecord debugBamlRecord = BamlRecordManager.ReadNextRecord(BinaryReader, bytesAvailable, nextRecordType);
                ProcessDebugBamlRecord(debugBamlRecord);
                return debugBamlRecord;
            }
            else
            {
                // if it wasn't a debug record then backup.
                stream.Seek( -1, SeekOrigin.Current);
                return null;
            }
        }

        internal void ProcessDebugBamlRecord(BamlRecord bamlRecord)
        {
            if(bamlRecord.RecordType == BamlRecordType.LineNumberAndPosition)
            {
                BamlLineAndPositionRecord bamlLineAndPositionRecord = (BamlLineAndPositionRecord)bamlRecord;
                LineNumber = (int)bamlLineAndPositionRecord.LineNumber;
                LinePosition = (int)bamlLineAndPositionRecord.LinePosition;
            }
            else
            {
                Debug.Assert(bamlRecord.RecordType == BamlRecordType.LinePosition);
                BamlLinePositionRecord bamlLinePositionRecord = (BamlLinePositionRecord)bamlRecord;
                LinePosition = (int)bamlLinePositionRecord.LinePosition;
            }
        }

        /// <summary>
        /// Gets the type of the record at the current position of the reader.
        /// </summary>
        internal BamlRecordType GetNextRecordType()
        {
            BamlRecordType bamlRecordType;

            if (null == PreParsedRecordsStart)
            {
                bamlRecordType = (BamlRecordType)BinaryReader.PeekChar();
            }
            else
            {
                bamlRecordType = PreParsedCurrentRecord.RecordType;
            }

            return bamlRecordType;
        }

        /// <summary>
        /// Close the underlying baml stream
        /// </summary>
        internal void Close()
        {
            if (BamlStream != null)
            {
                BamlStream.Close();
            }
            EndOfDocument = true;
        }

        /// <summary>
        /// Read the Baml and buld a Tree.
        /// </summary>
        internal bool Read(bool singleRecord)
        {
            BamlRecord bamlRecord = null;
            bool moreData = true;

            // loop through the records until the end building the Tree.
            while ( (true == moreData)
                && null != (bamlRecord = GetNextRecord()))
            {
                moreData = ReadRecord(bamlRecord);

                // if singleRecordMode then break
                if (singleRecord)
                {
                    break;
                }
            }

            // if next bamlRecord read comes back null
            // then moreData is false
            if (null == bamlRecord)
            {
                moreData = false;
            }

            // return true for more data meaning it is worth calling
            // read again if in Single Record mode. May or may not
            // really be another record.

            return moreData;
        }

        /// <summary>
        /// Synchronous read from Baml
        /// </summary>
        internal bool Read()
        {
            return Read(false); // not in single record mode.
        }

        /// <summary>
        /// Synchronous read callback that passes line information from original xaml file.
        /// This line information is used when reporting errors.  Make certain that the
        /// parser context line numbers are correct, since this is passed to subparsers and
        /// serializers and they may wish to report line information also.
        /// </summary>
        internal bool Read(
            BamlRecord bamlRecord,
            int        lineNumber,
            int        linePosition)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;

            return ReadRecord(bamlRecord);
        }

        internal void ReadVersionHeader()
        {
            BamlVersionHeader version = new BamlVersionHeader();
            version.LoadVersion(BinaryReader);
        }

        /// <summary>
        /// Read the Baml starting at the current location until the end of this
        /// element scope has been reached.  Return the object parsed.
        /// </summary>
        /// <remarks>
        /// Note that the first record read MUST be a BamlElementStartRecord, and
        /// reading will continue until the matching BamlElementEndRecord is reached.
        ///
        /// The dictionaryKey property, if set, indicates that this element is in a
        /// dictionary, and this was the key used to identify it.  This is used to
        /// provide better exception messages for deferred instantiation from a dictionary.
        /// </remarks>
        internal object ReadElement(Int64 startPosition,
                                    XamlObjectIds contextXamlObjectIds,
                                    object dictionaryKey )
        {
            BamlRecord bamlRecord = null;
            bool moreData = true;
            BinaryReader.BaseStream.Position = startPosition;
            int elementDepth = 0;
            object data = null;
            bool isKeySetInContext = false;

            // Push a special context onto the stack that is used as a placeholder
            // for surrounding context information about this element.

            PushContext(ReaderFlags.RealizeDeferContent, null, null, 0);
            CurrentContext.ElementNameOrPropertyName = contextXamlObjectIds.Name;
            CurrentContext.Uid = contextXamlObjectIds.Uid;
            CurrentContext.Key = dictionaryKey;

            #if DEBUG
            int stackDepth = ReaderContextStack.Count;
            #endif

            // Loop through the records until the matching end record is reached.
            while ( moreData
                && null != (bamlRecord = GetNextRecord()))
            {
                // Count start and end records and stop when we've reached the end
                // record associated with the first start record.  Note that
                // serializers handle the end record, so don't increment the element
                // depth for types that have serializers (such as styles).
                BamlElementStartRecord startRecord = bamlRecord as BamlElementStartRecord;
                if (startRecord != null)
                {
                    if (!MapTable.HasSerializerForTypeId(startRecord.TypeId))
                    {
                        elementDepth++;
                    }
                }
                else if (bamlRecord is BamlElementEndRecord)
                {
                    elementDepth--;
                }

                moreData = ReadRecord(bamlRecord);

                // If we got a key from the caller, it indicates that this element is being
                // defer-loaded from a dictionary, and this was the element's key.  Set it into
                // the context, as would happen in the non-deferred case, so that it is available to
                // make a good exception message.

                if( !isKeySetInContext )
                {
                    CurrentContext.Key = dictionaryKey;
                    isKeySetInContext = true;
                }

                // if singleRecordMode then break
                if (elementDepth == 0)
                {
                    break;
                }
            }

            // Get the element out of the context, then restore it
            // to null (as it was from the PushContext above).

            data = CurrentContext.ObjectData;
            CurrentContext.ObjectData = null;

            #if DEBUG  // ifdef's around Debug.Assert are necessary because stackDepth is only DEBUG defined
            Debug.Assert( stackDepth == ReaderContextStack.Count );
            #endif

            PopContext();

            MapTable.ClearConverterCache();

            return data;
        }

        protected virtual void ReadConnectionId(BamlConnectionIdRecord bamlConnectionIdRecord)
        {
            // Hookup any IDs or events that correspond to this connectionId on the component
            if (_componentConnector != null)
            {
                object target = GetCurrentObjectData();
                Debug.Assert(bamlConnectionIdRecord.ConnectionId > 0);
                _componentConnector.Connect(bamlConnectionIdRecord.ConnectionId, target);
            }
        }

        void ReadDocumentStartRecord(BamlDocumentStartRecord documentStartRecord)
        {
            IsDebugBamlStream = documentStartRecord.DebugBaml;
        }

        void ReadDocumentEndRecord()
        {
            Debug.Assert(0 == ReaderContextStack.Count); // if not zero we missed an EndElement
            SetPropertyValueToParent(false /*fromStartTag*/);
            ParserContext.RootElement = null;
            MapTable.ClearConverterCache();
            EndOfDocument = true;
        }

        // Read a single record and process it.  Return false if there are no
        // more records to process.
        internal virtual bool ReadRecord(BamlRecord bamlRecord)
        {
            bool moreData = true;

#if !STRESS
            try
            {
#endif
                switch (bamlRecord.RecordType)
                {
                    case BamlRecordType.DocumentStart:
                        ReadDocumentStartRecord((BamlDocumentStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.DocumentEnd:
                        ReadDocumentEndRecord();
                        moreData = false;
                        break;

                    case BamlRecordType.XmlnsProperty:
                        ReadXmlnsPropertyRecord((BamlXmlnsPropertyRecord)bamlRecord);
                        break;

                    case BamlRecordType.PIMapping:
                        {
                            // If this mapping has not already been set up, then set it now
                            BamlPIMappingRecord piMappingRecord = (BamlPIMappingRecord)bamlRecord;
                            if (!XamlTypeMapper.PITable.Contains(piMappingRecord.XmlNamespace))
                            {
                                BamlAssemblyInfoRecord assemblyInfo = MapTable.GetAssemblyInfoFromId(piMappingRecord.AssemblyId);
                                // Add information to the MappingPI hashtable
                                ClrNamespaceAssemblyPair mapping = new ClrNamespaceAssemblyPair(
                                                                        piMappingRecord.ClrNamespace,
                                                                        assemblyInfo.AssemblyFullName);
                                XamlTypeMapper.PITable.Add(piMappingRecord.XmlNamespace, mapping);
                            }
                            break;
                        }

                    case BamlRecordType.AssemblyInfo:
                        MapTable.LoadAssemblyInfoRecord((BamlAssemblyInfoRecord)bamlRecord);
                        break;

                    case BamlRecordType.TypeInfo:
                    case BamlRecordType.TypeSerializerInfo:
                        MapTable.LoadTypeInfoRecord((BamlTypeInfoRecord)bamlRecord);
                        break;

                    case BamlRecordType.AttributeInfo:
                        MapTable.LoadAttributeInfoRecord((BamlAttributeInfoRecord)bamlRecord);
                        break;

                    case BamlRecordType.StringInfo:
                        MapTable.LoadStringInfoRecord((BamlStringInfoRecord)bamlRecord);
                        break;

                    case BamlRecordType.LiteralContent:
                        ReadLiteralContentRecord((BamlLiteralContentRecord)bamlRecord);
                        break;

                    case BamlRecordType.ElementStart:
                    case BamlRecordType.StaticResourceStart:
                        if (((BamlElementStartRecord)bamlRecord).IsInjected)
                        {
                            CurrentContext.SetFlag(ReaderFlags.InjectedElement);
                        }
                        else
                        {
                            ReadElementStartRecord((BamlElementStartRecord)bamlRecord);
                        }
                        break;

                    case BamlRecordType.NamedElementStart:
                        // This is only used by template code, and only as a temporary record, so should never occur here.
                        // See comment on BamlNamedElementStartRecord
                        Debug.Assert(false);
                        break;

                    case BamlRecordType.ConnectionId:
                        ReadConnectionId((BamlConnectionIdRecord)bamlRecord);
                        break;

                    case BamlRecordType.ElementEnd:
                    case BamlRecordType.StaticResourceEnd:
                        if (CurrentContext.CheckFlag(ReaderFlags.InjectedElement))
                        {
                            CurrentContext.ClearFlag(ReaderFlags.InjectedElement);
                        }
                        else
                        {
                            ReadElementEndRecord(false);
                        }
                        break;

                    case BamlRecordType.PropertyComplexStart:
                        ReadPropertyComplexStartRecord((BamlPropertyComplexStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyComplexEnd:
                        ReadPropertyComplexEndRecord();
                        break;

                    case BamlRecordType.Property:
                        ReadPropertyRecord((BamlPropertyRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyStringReference:
                        ReadPropertyStringRecord((BamlPropertyStringReferenceRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyTypeReference:
                        ReadPropertyTypeRecord((BamlPropertyTypeReferenceRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyWithExtension:
                        ReadPropertyWithExtensionRecord((BamlPropertyWithExtensionRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyWithConverter:
                        ReadPropertyConverterRecord((BamlPropertyWithConverterRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyCustom:
                        ReadPropertyCustomRecord((BamlPropertyCustomRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyArrayStart:
                        ReadPropertyArrayStartRecord((BamlPropertyArrayStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyArrayEnd:
                        ReadPropertyArrayEndRecord();
                        break;

                    case BamlRecordType.PropertyIListStart:
                        ReadPropertyIListStartRecord((BamlPropertyIListStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyIListEnd:
                        ReadPropertyIListEndRecord();
                        break;

                    case BamlRecordType.PropertyIDictionaryStart:
                        ReadPropertyIDictionaryStartRecord((BamlPropertyIDictionaryStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyIDictionaryEnd:
                        ReadPropertyIDictionaryEndRecord();
                        break;

                    case BamlRecordType.DefAttribute:
                        ReadDefAttributeRecord((BamlDefAttributeRecord)bamlRecord);
                        break;

                    case BamlRecordType.DefAttributeKeyType:
                        ReadDefAttributeKeyTypeRecord((BamlDefAttributeKeyTypeRecord)bamlRecord);
                        break;

                    case BamlRecordType.PresentationOptionsAttribute:
                        ReadPresentationOptionsAttributeRecord((BamlPresentationOptionsAttributeRecord)bamlRecord);
                        break;

                    case BamlRecordType.RoutedEvent:
                        {
                            Debug.Assert(ReaderFlags.DependencyObject == CurrentContext.ContextType);
                            DependencyObject currentParent = GetCurrentObjectData() as DependencyObject;
                            BamlRoutedEventRecord bamlRoutedEventRecord = (BamlRoutedEventRecord)bamlRecord;

                            ThrowException(SRID.ParserBamlEvent, bamlRoutedEventRecord.Value);
                        }
                        break;

                    case BamlRecordType.Text:
                    case BamlRecordType.TextWithId:
                    case BamlRecordType.TextWithConverter:
                        ReadTextRecord((BamlTextRecord)bamlRecord);
                        break;

                    case BamlRecordType.DeferableContentStart:
                        ReadDeferableContentStart((BamlDeferableContentStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.KeyElementStart:
                        ReadKeyElementStartRecord((BamlKeyElementStartRecord)bamlRecord);
                        break;

                    case BamlRecordType.KeyElementEnd:
                        ReadKeyElementEndRecord();
                        break;

                    case BamlRecordType.ConstructorParametersStart:
                        ReadConstructorParametersStartRecord();
                        break;

                    case BamlRecordType.ConstructorParametersEnd:
                        ReadConstructorParametersEndRecord();
                        break;

                    case BamlRecordType.ConstructorParameterType:
                        ReadConstructorParameterTypeRecord((BamlConstructorParameterTypeRecord)bamlRecord);
                        break;

                    case BamlRecordType.ContentProperty:
                        ReadContentPropertyRecord((BamlContentPropertyRecord)bamlRecord);
                        break;

                    case BamlRecordType.StaticResourceId:
                        ReadStaticResourceIdRecord((BamlStaticResourceIdRecord)bamlRecord);
                        break;

                    case BamlRecordType.PropertyWithStaticResourceId:
                        ReadPropertyWithStaticResourceIdRecord((BamlPropertyWithStaticResourceIdRecord)bamlRecord);
                        break;

                    case BamlRecordType.LineNumberAndPosition:  // Should be skipped in ReadNextRecordWithDebugExtension.
                    case BamlRecordType.LinePosition:           // Should be skipped in ReadNextRecordWithDebugExtension.
                    default:
                        ThrowException(SRID.ParserUnknownBaml, ((int)bamlRecord.RecordType).ToString(CultureInfo.CurrentCulture));
                        break;
                }

#if !STRESS
            }

            catch (Exception e)
            {
                // Don't wrap critical exceptions or already-wrapped exceptions.
                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                XamlParseException.ThrowException( ParserContext,
                                               LineNumber,
                                               LinePosition,
                                               String.Empty /*message*/,
                                               e );
            }
#endif
            return moreData;
        }

        // Read an XML namespace definition record.  This contains a mapping from
        // an xml namespace to a prefix used within the scope of the current element.
        protected virtual void ReadXmlnsPropertyRecord(BamlXmlnsPropertyRecord xmlnsRecord)
        {
            Debug.Assert(ReaderFlags.DependencyObject == CurrentContext.ContextType ||
                ReaderFlags.ClrObject == CurrentContext.ContextType ||
                ReaderFlags.PropertyComplexClr == CurrentContext.ContextType ||
                ReaderFlags.PropertyComplexDP == CurrentContext.ContextType);

            // Accept name space directives for DependencyObjects and clr objects only
            if (ReaderFlags.DependencyObject == CurrentContext.ContextType ||
                ReaderFlags.ClrObject == CurrentContext.ContextType ||
                ReaderFlags.PropertyComplexClr == CurrentContext.ContextType ||
                ReaderFlags.PropertyComplexDP == CurrentContext.ContextType)
            {
                XmlnsDictionary[xmlnsRecord.Prefix] = xmlnsRecord.XmlNamespace;

                XamlTypeMapper.SetUriToAssemblyNameMapping(xmlnsRecord.XmlNamespace, xmlnsRecord.AssemblyIds);

                // Making this call will populate XamlTypeMapper.NamespaceMapHashList
                //  with appropriate values for this namespace URI.
                // We do this here because we need to save this data now to make it
                //  available later for XamlTypeMapper.GetTypeFromName() which gets it
                //  as value of XmlAttributeProperties.XmlNamespaceMapsProperty.

         // XamlTypeMapper.GetNamespaceMapEntries(xmlnsRecord);

                if (ReaderFlags.DependencyObject == CurrentContext.ContextType)
                {
                    SetXmlnsOnCurrentObject(xmlnsRecord);
                }
            }
        }

        // Determine the element type for a baml start record, and either create a new
        // element or mark it for delay creation later.  Update the ReaderFlags to
        // indicate the type of element created.
        private void GetElementAndFlags(
                BamlElementStartRecord bamlElementStartRecord,
                out object element,
                out ReaderFlags flags,
                out Type delayCreatedType,
                out short delayCreatedTypeId)
        {
            short typeId = bamlElementStartRecord.TypeId;
            Type elementType = MapTable.GetTypeFromId(typeId);
            element = null;

            delayCreatedType = null;
            delayCreatedTypeId = 0;
            flags = ReaderFlags.Unknown;

            if (null != elementType)
            {
                if (bamlElementStartRecord.CreateUsingTypeConverter ||
                    typeof(MarkupExtension).IsAssignableFrom(elementType))
                {
                    // CreateUsingTypeConverter means we've decided
                    //  (in XamlRecordReader) that we do not wish to
                    //  create an instance at this time, go to the delay-
                    //  creation state.
                    // MarkupExtensions will be created later after we have
                    //  parsed the constructor parameters.
                    delayCreatedType = elementType;
                    delayCreatedTypeId = typeId;
                }
                else
                {
                    // Create an instance of the object specified by the
                    //  BeginElement, throw if the creation fails.
                    element = CreateInstanceFromType(elementType, typeId, false);
                    if (element == null)
                    {
                        ThrowException(SRID.ParserNoElementCreate2, elementType.FullName);
                    }
                }

                flags = GetFlagsFromType(elementType);
            }
        }

        // Sets the ReaderFlags based on whether the elementType passed in is one of the special
        // collection types and whether the type is a DependencyObject or not.
        protected ReaderFlags GetFlagsFromType(Type elementType)
        {
            ReaderFlags flags = (typeof(DependencyObject).IsAssignableFrom(elementType) ? ReaderFlags.DependencyObject :
                                                                                          ReaderFlags.ClrObject);

            if (typeof(IDictionary).IsAssignableFrom(elementType))
            {
                flags |= ReaderFlags.IDictionary;
            }
            else if (typeof(IList).IsAssignableFrom(elementType))
            {
                flags |= ReaderFlags.IList;
            }
            else if (typeof(ArrayExtension).IsAssignableFrom(elementType))
            {
                flags |= ReaderFlags.ArrayExt;
            }
            else if (BamlRecordManager.TreatAsIAddChild(elementType))
            {
                flags |= ReaderFlags.IAddChild;
            }

            return flags;
        }

        // Modify the passed flags to set the NeedToAddToTree flag based on the current
        // context.  This is needed by subparsers in some cases.
        internal static void CheckForTreeAdd(ref ReaderFlags flags, ReaderContextStackData context)
        {
            // An element doesn't need to be added to a tree if in the context of constructor
            // paramters or deferred content

            if (context == null ||
                (context.ContextType != ReaderFlags.ConstructorParams &&
                 context.ContextType != ReaderFlags.RealizeDeferContent))
            {
                flags |= ReaderFlags.NeedToAddToTree;
            }
        }

        //+-----------------------------------------------------------------------------------------------------------------------
        //
        //  SetDependencyValue
        //
        //  We call this routine to set a DP value onto a DO, but it's virtual so that custom baml record readers
        //  can do their own thing.  This was added so that templates could set unshareable template child property
        //  values into per-FE state.
        //
        //+-----------------------------------------------------------------------------------------------------------------------

        internal void SetDependencyValue(DependencyObject dependencyObject, DependencyProperty dependencyProperty, object value)
        {
            // We don't need to get the metadata if we aren't skipping journaled properties
            FrameworkPropertyMetadata metadata = ParserContext != null && ParserContext.SkipJournaledProperties ?
                dependencyProperty.GetMetadata(dependencyObject.DependencyObjectType) as FrameworkPropertyMetadata
                : null;

            // If the metadata is not null here, we are skipping journaled properties (if the metadata requires it)
            // NOTE: we do not journal expression. So even when the property is journalable but the value is expression,
            // we still want to set the value from parser. See corresponding code for avoiding saving expression in DataStream.SaveSubStreams.
            if ((metadata == null) || (!metadata.Journal) || (value is Expression))
            {
                SetDependencyValueCore(dependencyObject, dependencyProperty, value);
            }
        }

        internal virtual void SetDependencyValueCore(DependencyObject dependencyObject, DependencyProperty dependencyProperty, object value)
        {
            // By default, we just set the DP on the DO.
            dependencyObject.SetValue(dependencyProperty, value);
        }

        //+-----------------------------------------------------------------------------------------------------------------------
        //
        //  ProvideValueFromMarkupExtension
        //
        //  Given a MarkupExtension, call its ProvideValue, passing it the right IServiceProvider.
        //
        //+-----------------------------------------------------------------------------------------------------------------------

        internal object ProvideValueFromMarkupExtension(MarkupExtension markupExtension, object obj, object member)
        {
            object returnValue = null;

            // Get the IServiceProvider
            ProvideValueServiceProvider serviceProvider = ParserContext.ProvideValueProvider;

            // Let it know the target object & property
            serviceProvider.SetData(obj, member);

            try
            {
                returnValue = markupExtension.ProvideValue(serviceProvider);

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.TraceActivityItem(
                                          TraceMarkup.ProvideValue,
                                          markupExtension,
                                          obj,
                                          member,
                                          returnValue );
                }
            }
            finally
            {
                serviceProvider.ClearData();
            }

            return returnValue;
        }

        // Read the start of an element.  This involves creating a new object, and storing it
        // for later addition to the tree or setting as a property.  The base method does
        // not check for a serializer.
        internal void BaseReadElementStartRecord(
            BamlElementStartRecord bamlElementRecord)
        {
            object        element = null;
            Type          delayCreatedType = null;
            short         delayCreatedTypeId = 0;
            ReaderFlags   flags = ReaderFlags.Unknown;
            ReaderContextStackData currentContext = CurrentContext;

            if (_bamlAsForest && currentContext == null)
            {
                Debug.Assert(_rootElement != null);
                element = _rootElement;

                flags = GetFlagsFromType(element.GetType());
            }
            else
            {
                if (null != currentContext &&
                    (ReaderFlags.PropertyComplexClr == currentContext.ContextType ||
                     ReaderFlags.PropertyComplexDP == currentContext.ContextType) &&
                    null == currentContext.ExpectedType)
                {
                    string propName = GetPropNameFrom(currentContext.ObjectData);
                    ThrowException(SRID.ParserNoComplexMulti, propName);
                }

                // If this is the very top element as indicated by there not being a
                // parent context, then we have to add this element to the rootlist
                // by calling SetPropertyValueToParent.  For all other cases we don't want to
                // call this here since we may be building a subtree bottom up and want
                // to defer addition of elements.  
                if (null == ParentContext)
                {
                    SetPropertyValueToParent(true);
                }

                // Get an instance of the element, if it is to be created now.  Also set
                // the flags to indicate what the element is and how to treat it.
                GetElementAndFlags(bamlElementRecord, out element, out flags,
                                   out delayCreatedType, out delayCreatedTypeId);
            }

            Stream bamlStream = BamlStream;

            if (!_bamlAsForest &&
                currentContext == null &&
                element != null &&
                bamlStream != null &&
                !(bamlStream is ReaderStream) &&
                StreamPosition == StreamLength)
            {
                // We are here because the root element was loaded from this baml stream
                // and not passed to us as is the case with the app loader\navigation engine,
                // and we are already at the end of the stream while processing this start
                // element record which would be the case when we have an element that has already
                // been instantiated by an inner LoadBaml call for that element with the same
                // Uri\ResourceLocator. So in this case we need to simulate a DocumentEnd to
                // cleanup properly and add to the RootList so that the caller of the outer
                // LoadBaml can access the element created by the inner LoadBaml.

                Debug.Assert(null == ParentContext);

                ReadDocumentEndRecord();
                if (RootList.Count == 0)
                {
                    RootList.Add(element);
                }

                // Set a flag to prevent the TreeBuilder from clobbering the
                // XamlTypeMapper's xmlns hashtable on the root element as this
                // would have already been set by the inner Baml loading Context.
                IsRootAlreadyLoaded = true;
            }
            else
            {
                // If we have a real element at this point, instead of some object that will be
                // delay created later, check for various interfaces that are called upon
                // element creation.
                if (element != null)
                {
                    // If this is an element start record that carries its name also, then register the name now also.
                    string elementName = null;

                    if (bamlElementRecord is BamlNamedElementStartRecord)
                    {
                        BamlNamedElementStartRecord bamlNamedElementStartRecord = bamlElementRecord as BamlNamedElementStartRecord;
                        elementName = bamlNamedElementStartRecord.RuntimeName;
                    }

                    ElementInitialize(element, elementName);
                }


                // Remember the object that was just created.  It will be added when
                // the end tag is reached.
                CheckForTreeAdd(ref flags, currentContext);
                PushContext(flags, element, delayCreatedType, delayCreatedTypeId, bamlElementRecord.CreateUsingTypeConverter);


                // Add just constructed element to the tree if it is a UIElement.  This
                // builds the tree in a top-down fashion for objects that make up the majority
                // of the logical tree.  Other objects, such as Freezables, are added bottom-up
                // to aid in having all properties set prior to adding them to the tree.
                // See PS workitem #19080
                if (BuildTopDown &&
                    element != null &&
                    ((element is UIElement) ||
                    (element is ContentElement) ||
                    (element is UIElement3D)))
                {
                    SetPropertyValueToParent(true);
                }
                else if (CurrentContext.CheckFlag(ReaderFlags.IDictionary))
                {
                    // AddElement to Tree checks this, but if that wasn't called, then we need
                    // to check for an explicit tag after a dictionary property.
                    bool isMarkupExtension = false;
                    if (CheckExplicitCollectionTag(ref isMarkupExtension))
                    {
                        // if the tag is an explicit element under a collection property, we're done
                        CurrentContext.MarkAddedToTree();

                        // ResourceDictionary objects must be loaded top-down
                        if (element is ResourceDictionary)
                        {
                            SetCollectionPropertyValue(ParentContext);
                        }
                    }
                }
            }
        }

        // Read the start of an element.  This involves creating a new object, and storing it
        // for later addition to the tree or setting as a property.
        // Return true if a serializer has been spun off to parse the next element, or
        // false if it was handled by the BamlRecordReader itself.  If a serializer has
        // been spun off, then the end record that matches this start record will NOT
        // get to this instance of the BamlRecordReader.
        protected virtual bool ReadElementStartRecord(BamlElementStartRecord bamlElementRecord)
        {
            // Check if the type of this element has a serializer associated with it.  If so then
            // we have to create a serializer and pass off processing to it.  Otherwise, continue
            // with default processing.
            bool hasSerializer = MapTable.HasSerializerForTypeId(bamlElementRecord.TypeId);
            if (hasSerializer)
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseRdrCrInstBegin);

                try
                {
                    BamlTypeInfoRecord typeInfo = MapTable.GetTypeInfoFromId(bamlElementRecord.TypeId);
                    XamlSerializer serializer = CreateSerializer((BamlTypeInfoWithSerializerRecord)typeInfo);

                    if (ParserContext.RootElement == null)
                    {
                        ParserContext.RootElement = _rootElement;
                    }
                    if (ParserContext.StyleConnector == null)
                    {
                        ParserContext.StyleConnector = _rootElement as IStyleConnector;
                    }

                    // PreParsedIndex is updated by Serializer after its TreeBuilder.Parse.
                    serializer.ConvertBamlToObject(this, bamlElementRecord, ParserContext);
                }
                finally
                {
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseRdrCrInstEnd);
                }
                return true;
            }

            BaseReadElementStartRecord(bamlElementRecord);
            return false;
        }

        // The end of an element has been reached.  What to do depends on the current
        // context, since the object may be added as a child to another object, placed in
        // in a dictionary, added to a list, set as a property, etc...
        protected internal virtual void ReadElementEndRecord(bool fromNestedBamlRecordReader)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpectedEndEle);
            }

            object currentElement = GetCurrentObjectData();

            // Make sure we have called EndInit on the currentElement before it is set
            // as a property value on the parent. This is because if we encounter an
            // exception during the call to EndInit we want to be able to use the
            // Fallback provided by the parent for the given property. So in order to
            // set the right property value on the parent we should have called EndInit
            // prior to the call to SetPropertyValueToParent. Example
            //
            // <Image>
            // <Image.Source>
            //   <BitmapImage UriSource="c:\\bogus.jpg" />
            // </Image.Source>
            // </Image>
            //
            // Calling EndInit on BitmapImage throws and exception and then we need to
            // use the Fallback provided by Image for the Source property.

            ElementEndInit(ref currentElement);

            // Check if the element on the stack needs to be added to the tree as a child
            // If not, then continue and see if the element should be added to a list, dictionary,
            // array, set as a property value, etc.
            SetPropertyValueToParent(false /*fromStartTag*/);

            Debug.Assert(!CurrentContext.CheckFlag(ReaderFlags.NeedToAddToTree), "Failed to add Element to tree before popping stack");

            ReaderFlags flags = CurrentContext.ContextFlags;

            FreezeIfRequired(currentElement);

            // pop the stack
            PopContext();

            // Deferred content and constructor parameters aren't handled in SetPropertyValueToParent

            if ((flags & (ReaderFlags.AddedToTree)) == 0 &&
                CurrentContext != null)
            {
                Debug.Assert( CurrentContext.ContextType != ReaderFlags.PropertyComplexClr );
                Debug.Assert( CurrentContext.ContextType != ReaderFlags.PropertyComplexDP );

                switch (CurrentContext.ContextType)
                {
                    case ReaderFlags.RealizeDeferContent:
                        // Pass the object up
                        CurrentContext.ObjectData = currentElement;
                        break;

                    case ReaderFlags.ConstructorParams:
                        SetConstructorParameter(currentElement);
                        break;
                }
            }
        }


        // Read the start of an element that is the first
        // object in a key in a resource dictionary.
        internal virtual void ReadKeyElementStartRecord(
            BamlKeyElementStartRecord bamlElementRecord)
        {
            Type elementType = MapTable.GetTypeFromId(bamlElementRecord.TypeId);

            ReaderFlags flags  = (elementType.IsAssignableFrom(typeof(DependencyObject)) ?
                                               ReaderFlags.DependencyObject :
                                               ReaderFlags.ClrObject) |
                                               ReaderFlags.NeedToAddToTree;

            // Remember the object that was just created.  It will be used when
            // the end tag is reached and the key is complete
            PushContext(flags, null, elementType, bamlElementRecord.TypeId);
        }

        // The end of an element that represents a key in an IDictionary has
        // been reached.  Search for the dictionary holder in the reader stack
        // and set the key value to the current object on the top of the
        // reader stack.
        internal virtual void ReadKeyElementEndRecord()
        {
            object key = ProvideValueFromMarkupExtension((MarkupExtension)GetCurrentObjectData(),
                                                         ParentObjectData, null /*member*/);

            SetKeyOnContext(key, XamlReaderHelper.DefinitionName, ParentContext, GrandParentContext);
            PopContext();
        }

        // The constructor parameter with a single Type object has been read in.  Get the
        // type associated with this record and add it to the constructor parameter list.
        internal virtual void ReadConstructorParameterTypeRecord(
            BamlConstructorParameterTypeRecord constructorParameterType)
        {
            Debug.Assert(CurrentContext.ContextType == ReaderFlags.ConstructorParams);
            SetConstructorParameter(MapTable.GetTypeFromId(constructorParameterType.TypeId));
        }

        // Read the content property record and set the ContentProperty in the context.
        internal virtual void ReadContentPropertyRecord(
            BamlContentPropertyRecord bamlContentPropertyRecord)
        {
            object contentProperty = null;

            short attributeId = bamlContentPropertyRecord.AttributeId;

            // Try KnownTypes Optimization: When the property is known use generated code for accessing it
            object parent = GetCurrentObjectData();
            if (parent != null)
            {
                short elementId = BamlMapTable.GetKnownTypeIdFromType(parent.GetType());
                if (elementId < 0)
                {
                    contentProperty = KnownTypes.GetCollectionForCPA(parent, (KnownElements)(-elementId));
                }
            }

            if (contentProperty == null)
            {
                WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(this, attributeId, parent is DependencyObject);

                // Try DependencyProperty Optimization: When a DP exists for the property, use it for accessing the property
                if (propertyDefinition.DependencyProperty != null)
                {
                    Debug.Assert(parent is DependencyObject);

                    if (typeof(IList).IsAssignableFrom(propertyDefinition.PropertyType))
                    {
                        contentProperty = ((DependencyObject)parent).GetValue(propertyDefinition.DependencyProperty) as IList;
                        // We assume that the contentProperty will become a collection now.
                        // However, in case when the DP is implemented incorrectly it may return null even if the clr property has non-null value.
                        // So leaving contentProperty==null will drive us to general clr case below, which must do better job.
                    }
                    else
                    {
                        // When the property is not a IList store a DP itself as a contentProperty for assigning a simple value
                        contentProperty = propertyDefinition.DependencyProperty;
                    }
                }

                if (contentProperty == null)
                {
                    // We consider only PropertyInfo case here.
                    // We do not consider AttachedPropertySetter in this case because content property cannot be attached.
                    if (propertyDefinition.PropertyInfo != null)
                    {
                        // Try to treat the content property as IList
                        if (propertyDefinition.IsInternal)
                        {
                            contentProperty = XamlTypeMapper.GetInternalPropertyValue(ParserContext,
                                                                                      ParserContext.RootElement,
                                                                                      propertyDefinition.PropertyInfo,
                                                                                      parent) as IList;

                            // if Content Property does not support IList, then see if it is
                            // accessible\allowed as a regular setter.
                            if (contentProperty == null)
                            {
                                bool isPublicProperty;
                                bool allowProtected =
                                    (ParserContext.RootElement is IComponentConnector) &&
                                    (ParserContext.RootElement == parent);
                                if (!XamlTypeMapper.IsAllowedPropertySet(propertyDefinition.PropertyInfo, allowProtected, out isPublicProperty))
                                {
                                    ThrowException(SRID.ParserCantSetContentProperty, propertyDefinition.Name, propertyDefinition.PropertyInfo.ReflectedType.Name);
                                }
                            }
                        }
                        else
                        {
                            contentProperty = propertyDefinition.PropertyInfo.GetValue(
                                parent,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy,
                                null, null, TypeConverterHelper.InvariantEnglishUS)
                                as IList;
                        }

                        if (contentProperty == null)
                        {
                            // The property returned null, try setting it directly.
                            contentProperty = propertyDefinition.PropertyInfo;
                        }
                    }
                }
            }

            if (contentProperty == null)
            {
                ThrowException(SRID.ParserCantGetDPOrPi, GetPropertyNameFromAttributeId(attributeId));
            }

            CurrentContext.ContentProperty = contentProperty;
        }

        // The Start of the constructor parameter section has been reached.  Prepare to
        // store all the top level objects that follow up to the ConstructorParametersEnd
        // record is reached.
        internal virtual void ReadConstructorParametersStartRecord()
        {
            PushContext(ReaderFlags.ConstructorParams, null, null, 0);
        }

        // The end of the constructor parameter section has been reached.  Create an
        // instance of the object after finding the appropriate constructor and converting
        // all of the objects held on the stack.
        internal virtual void ReadConstructorParametersEndRecord()
        {
            Type   elementType = ParentContext.ExpectedType;
            short  positiveElementTypeId = (short)-ParentContext.ExpectedTypeId;

            object param = null;
            ArrayList paramList = null;
            int paramCount;
            object instance = null;
            bool foundInstance = false;

            if( TraceMarkup.IsEnabled )
            {
                TraceMarkup.Trace( TraceEventType.Start,
                                 TraceMarkup.CreateMarkupExtension,
                                 elementType );
            }

            if (CurrentContext.CheckFlag(ReaderFlags.SingletonConstructorParam))
            {
                param = CurrentContext.ObjectData;
                paramCount = 1;

                // Fast code path for [static/dynamic] resource extensions &
                // Type/Static/TemplateBinding extensions
                switch (positiveElementTypeId)
                {
                    case (short)KnownElements.TypeExtension:

                        // Note that this assumes that TypeExtension has a
                        // constructor with one param of type Type or String.
                        Type t = param as Type;
                        if (t != null)
                        {
                            instance = new TypeExtension(t);
                        }
                        else
                        {
                            Debug.Assert(param is String);
                            instance = new TypeExtension((String)param);
                        }

                        foundInstance = true;
                        break;

                    case (short)KnownElements.StaticResourceExtension:

                        // Note that this assumes that StaticResourceExtension has a
                        // constructor with one param of type object.
                        instance = new StaticResourceExtension(param);
                        foundInstance = true;
                        break;

                    case (short)KnownElements.DynamicResourceExtension:

                        // Note that this assumes that DynamicResourceExtension has a
                        // constructor with one param of type object.
                        instance = new DynamicResourceExtension(param);
                        foundInstance = true;
                        break;

                    case (short)KnownElements.StaticExtension:

                        // Note that this assumes that StaticExtension has a default
                        // constructor and one public property of type string and one
                        // internal property of type object for optimized member info.
                        instance = new StaticExtension((string)param);
                        foundInstance = true;
                        break;

                    case (short)KnownElements.TemplateBindingExtension:

                        // Note that this assumes that TemplateBindingExtension has a
                        // constructor with one param of type DependencyProperty. If a
                        // string is passed in due to there being other attributes like
                        // converter being set, then that needs to be converted now first.
                        DependencyProperty dp = param as DependencyProperty;
                        if (dp == null)
                        {
                            string paramString = param as string;
                            Type ownerType = ParserContext.TargetType;
                            Debug.Assert(paramString != null);

                            dp = XamlTypeMapper.ParsePropertyName(ParserContext,
                                                                  paramString.Trim(),
                                                                  ref ownerType);

                            if (dp == null)
                            {
                                ThrowException(SRID.ParserNoDPOnOwner, paramString, ownerType.FullName);
                            }
                        }

                        instance = new TemplateBindingExtension(dp);
                        foundInstance = true;
                        break;
                }
            }
            else
            {
                paramList = (ArrayList)CurrentContext.ObjectData;
                paramCount = paramList.Count;
            }

            if (!foundInstance)
            {
                // Find the constructor based on the number of parameters stored in paramList
                XamlTypeMapper.ConstructorData data = XamlTypeMapper.GetConstructors(elementType);
                ConstructorInfo[] infos = data.Constructors;
                for (int i=0; i<infos.Length; i++)
                {
                    ConstructorInfo info = infos[i];
                    ParameterInfo[] paramInfos = data.GetParameters(i);
                    if (paramInfos.Length == paramCount)
                    {
                        object[] paramArray = new object[paramInfos.Length];

                        if (paramCount == 1)
                        {
                            Debug.Assert(param != null && paramList == null, "Must have a single param");
                            ProcessConstructorParameter(paramInfos[0], param, ref paramArray[0]);

                            // Fast code path for other markupextensions
                            if (positiveElementTypeId == (short)KnownElements.RelativeSource)
                            {
                                // Note that this assumes that RelativeSource has a
                                // constructor with one param of type RelativeSourceMode.
                                instance = new System.Windows.Data.RelativeSource((System.Windows.Data.RelativeSourceMode)paramArray[0]);
                                foundInstance = true;
                            }
                        }
                        else
                        {
                            Debug.Assert(param == null && paramList != null, "Must have a paramList");

                            // Check each type and attempt to convert the paramList using
                            // the type converter associated with each parameter type.
                            for (int j=0; j<paramInfos.Length; j++)
                            {
                                ProcessConstructorParameter(paramInfos[j], paramList[j], ref paramArray[j]);
                            }
                        }

                        if (!foundInstance)
                        {
                            // If we make it to here we have a list of converted parameters, so
                            // invoke the constructor with that list.
#if !STRESS
                            try
                            {
#endif
                                instance = info.Invoke(paramArray);
                                foundInstance = true;
#if !STRESS
                            }
                            catch (Exception e)
                            {
                                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException)
                                {
                                    throw;
                                }

                                TargetInvocationException tie = e as TargetInvocationException;
                                if( tie != null )
                                {
                                    e = tie.InnerException;
                                }

                                ThrowExceptionWithLine(SR.Get(SRID.ParserFailedToCreateFromConstructor, info.DeclaringType.Name),  e);
                            }
#endif
                        }
                    }
                }
            }

            if (foundInstance)
            {
                ParentContext.ObjectData = instance;
                ParentContext.ExpectedType = null;
                PopContext();
            }
            else
            {
                // If we get to here, then no matching constructor was found, so complain
                ThrowException(SRID.ParserBadConstructorParams, elementType.Name, paramCount.ToString(CultureInfo.CurrentCulture));
            }

            if( TraceMarkup.IsEnabled )
            {
                TraceMarkup.Trace( TraceEventType.Stop,
                                 TraceMarkup.CreateMarkupExtension,
                                 elementType,
                                 instance );
            }
        }

        // Helper that processes a single constructor parameter
        private void ProcessConstructorParameter(ParameterInfo paramInfo, object param, ref object paramArrayItem)
        {
            // Check for MarkupExtensions in the parameter list and
            // get values from those, if available.
            MarkupExtension me = param as MarkupExtension;
            if (me != null)
            {
                param = ProvideValueFromMarkupExtension(me, null, null);
            }

            // Don't convert parameters of type object.  Leave them alone.
            if (param != null &&
                paramInfo.ParameterType != typeof(Object) &&
                paramInfo.ParameterType != param.GetType())
            {
                object convertedParam;

                TypeConverter converter = XamlTypeMapper.GetTypeConverter(paramInfo.ParameterType);


                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Start,
                                     TraceMarkup.ProcessConstructorParameter,
                                     paramInfo.ParameterType,
                                     converter.GetType(),
                                     param );
                }

#if !STRESS
                try
                {
#endif
                    if (param is String)
                    {
                        convertedParam = converter.ConvertFromString(TypeConvertContext,
                                                                TypeConverterHelper.InvariantEnglishUS,
                                                                (param as String));
                        param = convertedParam;
                    }

                    else
                    {
                        // Let normal type converter errors occur here, since we are not
                        // dealing with strings.  Don't convert if we already have
                        // the correct types.  Not converting hides bugs in type converters
                        // such as BrushConverter that can't convert a Brush to a Brush.
                        if (!paramInfo.ParameterType.IsAssignableFrom(param.GetType()))
                        {
                            convertedParam = converter.ConvertTo(TypeConvertContext,
                                                       TypeConverterHelper.InvariantEnglishUS,
                                                       param,
                                                       paramInfo.ParameterType);
                            param = convertedParam;
                        }
                    }
#if !STRESS
                }

                catch( Exception e )
                {
                    if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                    {
                        throw;
                    }

                    ThrowExceptionWithLine(
                                   SR.Get(SRID.ParserCannotConvertString,
                                          param.ToString(),
                                          paramInfo.ParameterType.FullName),
                                   e);
                }
#endif

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Stop,
                                     TraceMarkup.ProcessConstructorParameter,
                                     paramInfo.ParameterType,
                                     converter.GetType(),
                                     param );
                }
            }

            // Copy the contents of paramList into the paramArray
            paramArrayItem = param;
        }

        // The start of a block of deferable content has been reached.  There should
        // be a ResourceDictionary above us in the reader stack, so call it
        // with the stream and position information for the deferable block.  Then
        // skip over the deferable block after parsing the key table and continue.
        internal virtual void ReadDeferableContentStart(
            BamlDeferableContentStartRecord bamlRecord)
        {
            ResourceDictionary dictionary = GetDictionaryFromContext(CurrentContext, true /*toInsert*/) as ResourceDictionary;

            if (dictionary != null)
            {
                // At present we DO NOT SUPPORT having deferable content in an async
                // stream, since we don't have a good way to defer the load of this block
                // of records.
                Stream stream = BinaryReader.BaseStream;
                long startPosition = stream.Position;
                long bytesAvailable = stream.Length - startPosition;
                if (bytesAvailable < bamlRecord.ContentSize)
                {
                    ThrowException(SRID.ParserDeferContentAsync);
                }

                // Read through all the key records and the static resource records
                ArrayList       defKeyList;
                List<object[]>  staticResourceValuesList;
                BaseReadDeferableContentStart(bamlRecord, out defKeyList, out staticResourceValuesList);

                // If we don't own the stream, someone might close it when the parse is complete,
                // so copy the defer load contents into a buffer.  If we own the
                // stream, then we'll assume the owner will keep it open.
                long endOfKeysPosition = stream.Position;
                Int32 valuesSize = (Int32)(bamlRecord.ContentSize - endOfKeysPosition + startPosition);

                if (!ParserContext.OwnsBamlStream)
                {
                    byte[] buffer = new byte[valuesSize];

                    if (valuesSize > 0)
                    {
                        MS.Internal.IO.Packaging.PackagingUtilities.ReliableRead(
                            BinaryReader, buffer, 0, valuesSize);
                    }

                    throw new NotImplementedException();
                    //dictionary.SetDeferableContent(buffer,
                    //        ParserContext, _rootElement, defKeyList, staticResourceValuesList);
                }
                else
                {
                    throw new NotImplementedException();
                    //dictionary.SetDeferableContent(_bamlStream,
                    //        endOfKeysPosition, valuesSize,
                    //        ParserContext, _rootElement, defKeyList, staticResourceValuesList);

                    //_bamlStream.Seek(valuesSize, SeekOrigin.Current);
                }
            }
        }

        internal void BaseReadDeferableContentStart(
            BamlDeferableContentStartRecord bamlRecord,
            out ArrayList                   defKeyList,
            out List<object[]>              staticResourceValuesList)
        {
            // Check for DefAttributeKeys and create a key table, if some are
            // present.  Peek ahead in the stream looking for IBamlDictionaryKey types
            // and just process those without touching anything else.
            // NOTE:  The typical shell theme file is roughly 450 bytes per value, so presize
            //        the arraylist to be optimal for these scenarios.
            defKeyList = new ArrayList(Math.Max (5, (int)(bamlRecord.ContentSize/400)));
            staticResourceValuesList = new List<object[]>(defKeyList.Capacity);
            ArrayList staticResources = new ArrayList();
            BamlRecordType nextType = GetNextRecordType();
            while (nextType == BamlRecordType.DefAttributeKeyString ||
                   nextType == BamlRecordType.DefAttributeKeyType ||
                   nextType == BamlRecordType.KeyElementStart)
            {
                BamlRecord keyRecord = GetNextRecord();
                IBamlDictionaryKey dictionaryKeyRecord = keyRecord as IBamlDictionaryKey;
                if (nextType == BamlRecordType.KeyElementStart)
                {
                    // Read forward until we get a KeyElementEnd, at which point
                    // we should have the key object on the reader stack.  Use
                    // that to set the key object in the Key Element Start record.
                    ReadKeyElementStartRecord((BamlKeyElementStartRecord)keyRecord);
                    defKeyList.Add(keyRecord);
                    BamlRecord nestedBamlRecord;
                    bool moreData = true;
                    while (moreData && null != (nestedBamlRecord = GetNextRecord()))
                    {
                        if (nestedBamlRecord is BamlKeyElementEndRecord)
                        {
                            object keyObject = GetCurrentObjectData();
                            MarkupExtension me = keyObject as MarkupExtension;
                            if (me != null)
                            {
                                keyObject = ProvideValueFromMarkupExtension(me, GetParentObjectData(), null);
                            }
                            dictionaryKeyRecord.KeyObject = keyObject;
                            PopContext();
                            moreData = false;
                        }
                        else
                        {
                            moreData = ReadRecord(nestedBamlRecord);
                        }
                    }
                }
                else
                {
                    BamlDefAttributeKeyStringRecord stringKeyRecord = keyRecord as BamlDefAttributeKeyStringRecord;
                    if (stringKeyRecord != null)
                    {
                        // Get the value string from the string table, and cache it in the
                        // record.
                        stringKeyRecord.Value = MapTable.GetStringFromStringId(
                                                        stringKeyRecord.ValueId);

                        dictionaryKeyRecord.KeyObject = XamlTypeMapper.GetDictionaryKey(stringKeyRecord.Value, ParserContext);
                        defKeyList.Add(stringKeyRecord);
                    }
                    else
                    {
                        BamlDefAttributeKeyTypeRecord typeKeyRecord = keyRecord as BamlDefAttributeKeyTypeRecord;
                        if (typeKeyRecord != null)
                        {
                            dictionaryKeyRecord.KeyObject = MapTable.GetTypeFromId(
                                             typeKeyRecord.TypeId);
                            defKeyList.Add(typeKeyRecord);
                        }
                        else
                        {
                            // Enum.ToString(culture) is [Obsolete]
                            #pragma warning disable 0618

                            ThrowException(SRID.ParserUnexpInBAML, keyRecord.RecordType.ToString(CultureInfo.CurrentCulture));

                            #pragma warning restore 0618
                        }
                    }
                }

                // Check for StaticResources belonging to this key and create a table, if present.
                // Peek ahead in the stream looking for StaticResource[Start/End] types
                // and just process those without touching anything else.

                nextType = GetNextRecordType();

                // If this dictionary is a top level deferred section then
                // its front loaded section contains StaticResourceRecords

                if (!ParserContext.InDeferredSection)
                {
                    // Example:
                    //
                    // <ResourceDictionary>
                    //   < ... {StaticResource res1} ... />
                    // </ResourceDictionary>

                    while (nextType == BamlRecordType.StaticResourceStart ||
                           nextType == BamlRecordType.OptimizedStaticResource)
                    {
                        BamlRecord srRecord = GetNextRecord();
                        if (nextType == BamlRecordType.StaticResourceStart)
                        {
                            // Read forward until we get a StaticResourceEnd, at which point
                            // we should have the StaticResourceExtension object on the reader stack.
                            BamlStaticResourceStartRecord startRecord = (BamlStaticResourceStartRecord)srRecord;
                            BaseReadElementStartRecord(startRecord);
                            BamlRecord nestedBamlRecord;
                            bool moreData = true;
                            while (moreData && null != (nestedBamlRecord = GetNextRecord()))
                            {
                                if (nestedBamlRecord.RecordType == BamlRecordType.StaticResourceEnd)
                                {
                                    StaticResourceExtension staticResource = (StaticResourceExtension)GetCurrentObjectData();
                                    staticResources.Add(staticResource);
                                    PopContext();
                                    moreData = false;
                                }
                                else
                                {
                                    moreData = ReadRecord(nestedBamlRecord);
                                }
                            }
                        }
                        else
                        {
                            StaticResourceExtension staticResource = (StaticResourceExtension)GetExtensionValue((IOptimizedMarkupExtension)srRecord, null);
                            staticResources.Add(staticResource);
                        }
                        nextType = GetNextRecordType();
                    }
                }
                else
                {
                    // Example:
                    //
                    // <Button>
                    // <Button.Template>
                    // <ControlTemplate>
                    //   <StackPanel ... {StaticResource res1} ... >
                    //   <StackPanel.Resources>
                    //     < ... {StaticResource res2} ... />
                    //   </StackPanel.Resources>
                    //   </StackPanel>
                    // </ControlTemplate>
                    // </Button.Template>
                    // </Button>

                    // If this dictionary is nested within another deferred section such as a template
                    // content then its front loaded section will have StaticResourceId records that
                    // index into the pre-fetched values on the template.

                    object[] staticResourceValues = ParserContext.StaticResourcesStack[ParserContext.StaticResourcesStack.Count-1];

                    while (nextType == BamlRecordType.StaticResourceId)
                    {
                        BamlStaticResourceIdRecord bamlStaticResourceIdRecord = (BamlStaticResourceIdRecord)GetNextRecord();

                        // Find the StaticResourceValue for the given Id
                        Debug.Assert(staticResourceValues != null, "Must have a list of StaticResourceValues for lookup");
                        DeferredResourceReference prefetchedValue = (DeferredResourceReference)staticResourceValues[bamlStaticResourceIdRecord.StaticResourceId];
                        staticResources.Add(new StaticResourceHolder(prefetchedValue.Key, prefetchedValue));

                        // Peek at next record type
                        nextType = GetNextRecordType();
                    }
                }

                // Set the StaticResources collection on the DictionaryKeyRecord
                staticResourceValuesList.Add(staticResources.ToArray());
                staticResources.Clear();

                nextType = GetNextRecordType();
            }
        }

        /// <summary>
        /// Read the node that stores an Id reference to StaticResource records stored in the
        /// front loaded section within the deferred content section in Baml
        /// </summary>
        protected virtual void ReadStaticResourceIdRecord(
            BamlStaticResourceIdRecord bamlStaticResourceIdRecord)
        {
            // Find the StaticResourceValue for the given Id
            object value = GetStaticResourceFromId(bamlStaticResourceIdRecord.StaticResourceId);

            // Push a StaticResourceHolder onto the context. This is to be able to pass the
            // PrefetchedValue through to StaticResourceExtension.ProvideValueInternal.
            PushContext(ReaderFlags.ClrObject | ReaderFlags.NeedToAddToTree, value, null, 0);

            // Process the end record
            ReadElementEndRecord(true);
        }

        /// <summary>
        /// Read the property node that stores an Id reference to StaticResource records stored in the
        /// front loaded section within the deferred content section in Baml
        /// </summary>
        protected virtual void ReadPropertyWithStaticResourceIdRecord(
            BamlPropertyWithStaticResourceIdRecord bamlPropertyWithStaticResourceIdRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "Property");
            }

            // Define attrbuteId
            short attributeId = bamlPropertyWithStaticResourceIdRecord.AttributeId;

            // Identify the target element
            object element = GetCurrentObjectData();

            // Identify the property
            WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(this, attributeId, element is DependencyObject);

            // Find the StaticResourceValue for the given Id
            object value = GetStaticResourceFromId(bamlPropertyWithStaticResourceIdRecord.StaticResourceId);

            // Read and set the value provided by the MarkupExtension on the element's property
            BaseReadOptimizedMarkupExtension(element, attributeId, propertyDefinition, value);
        }

        /// <summary>
        /// This is a helper method to find the StaticResource value
        /// corresponding to the given StaticResourceId
        /// </summary>
        internal StaticResourceHolder GetStaticResourceFromId(short staticResourceId)
        {
            // Get the value of the property
            object[] staticResourceValues = ParserContext.StaticResourcesStack[ParserContext.StaticResourcesStack.Count-1];
            Debug.Assert(staticResourceValues != null, "Must have a list of StaticResourceValues for lookup");

            // Find the StaticResourceValue for the given Id
            DeferredResourceReference prefetchedValue =
                (DeferredResourceReference)staticResourceValues[staticResourceId];

            return new StaticResourceHolder(prefetchedValue.Key, prefetchedValue);
        }

        // The base baml record reader has no support for literal content.  Subclasses may
        // wish to use this if they store xml blobs in the baml file as text literal content.
        internal virtual void ReadLiteralContentRecord(
            BamlLiteralContentRecord bamlLiteralContentRecord)
        {
            if (CurrentContext != null)
            {
                 object dpOrPi = null;
                 object parent = null;
                 if (CurrentContext.ContentProperty != null)
                 {
                    dpOrPi = CurrentContext.ContentProperty;
                    parent = CurrentContext.ObjectData;
                 }
                 else if (   (CurrentContext.ContextType == ReaderFlags.PropertyComplexClr)
                           || (CurrentContext.ContextType == ReaderFlags.PropertyComplexDP) )
                 {
                    dpOrPi = CurrentContext.ObjectData;
                    parent = ParentContext.ObjectData;
                 }

                IXmlSerializable xmlSerializable = null;
                PropertyInfo pi = dpOrPi as PropertyInfo;
                if (pi != null)
                {
                    if (typeof(IXmlSerializable).IsAssignableFrom(pi.PropertyType))
                    {
                        xmlSerializable = pi.GetValue(parent, null) as IXmlSerializable;
                    }
                }
                else
                {
                    DependencyProperty dp = dpOrPi as DependencyProperty;
                    if (dp != null)
                    {
                        if (typeof(IXmlSerializable).IsAssignableFrom(dp.PropertyType))
                        {
                            xmlSerializable = ((DependencyObject)parent).GetValue(dp) as IXmlSerializable;
                        }
                    }
                }
                if (xmlSerializable != null)
                {
                    // REVIEW: keep reader open, some ReadXml impl like XmlDP's will load XML later on different thread
                    // callee is expected to close reader once done
                    FilteredXmlReader reader = new FilteredXmlReader(
                        bamlLiteralContentRecord.Value,
                        XmlNodeType.Element,
                        ParserContext);
                    xmlSerializable.ReadXml(reader);
                    return;
                }
            }
            ThrowException(SRID.ParserUnexpInBAML, "BamlLiteralContent" );
        }

        // Read the start of a complex property section.  Determine the property to set
        // with the object that will be constructed from the following records.
        protected virtual void ReadPropertyComplexStartRecord(
            BamlPropertyComplexStartRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                !(ReaderFlags.ClrObject == CurrentContext.ContextType ||
                  ReaderFlags.DependencyObject == CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "PropertyComplexStart");
            }

            short attributeId = bamlPropertyRecord.AttributeId;

            WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(
                                    this,
                                    attributeId,
                                    ReaderFlags.DependencyObject == CurrentContext.ContextType /*targetIsDependencyObject*/ );

            // Try DependencyProperty optimization.
            if (propertyDefinition.DependencyProperty != null)
            {
                // For the case of a DependencyProperty, store the BamlAttributeInfo on the
                // stack, because we may need to know the actual added owner type for the DP
                // to use in error messages.  This is not available in the DP if there
                // are multiple owners.
                PushContext(ReaderFlags.PropertyComplexDP, propertyDefinition.AttributeInfo, propertyDefinition.PropertyType, 0);
            }
            else if (propertyDefinition.PropertyInfo != null)
            {
                // Regular case for clr property
                PushContext(ReaderFlags.PropertyComplexClr, propertyDefinition.PropertyInfo, propertyDefinition.PropertyType, 0);
            }
            else if (propertyDefinition.AttachedPropertySetter != null)
            {
                // Assignable Attached property
                PushContext(ReaderFlags.PropertyComplexClr, propertyDefinition.AttachedPropertySetter, propertyDefinition.PropertyType, 0);
            }
            else if (propertyDefinition.AttachedPropertyGetter != null)
            {
                // Readonly Attached property
                PushContext(ReaderFlags.PropertyComplexClr, propertyDefinition.AttachedPropertyGetter, propertyDefinition.PropertyType, 0);
            }
            else
            {
                ThrowException(SRID.ParserCantGetDPOrPi, GetPropertyNameFromAttributeId(attributeId));
            }

            // Set the name of the property into the context
            CurrentContext.ElementNameOrPropertyName = propertyDefinition.Name;
        }

        // Reached the end of a complex property.  Not we are not doing validity checking here,
        // since this should have been done when the BAML was created.
        protected virtual void ReadPropertyComplexEndRecord()
        {
            PopContext();
        }

        internal DependencyProperty GetCustomDependencyPropertyValue(BamlPropertyCustomRecord bamlPropertyRecord)
        {
            Type declaringType = null;
            return GetCustomDependencyPropertyValue(bamlPropertyRecord, out declaringType);
        }

        internal DependencyProperty GetCustomDependencyPropertyValue(BamlPropertyCustomRecord bamlPropertyRecord,
                                                                     out Type declaringType)
        {
            declaringType = null;
            DependencyProperty dp = null;
            short serializerTypeId = bamlPropertyRecord.SerializerTypeId;

            Debug.Assert(serializerTypeId == (short)KnownElements.DependencyPropertyConverter);

            if (!bamlPropertyRecord.ValueObjectSet)
            {
                // Handle DP property value stored as an attribInfo Id.
                short dpId = BinaryReader.ReadInt16();
                string dpName = null;
                // if ValueId is a TypeId, then get the DP name that is stored next in the record as a string.
                // else the ValueId is a known DP.
                if (bamlPropertyRecord.IsValueTypeId)
                {
                    dpName = BinaryReader.ReadString();
                }

                // Resolve the attribInfo of the prop value read into ValueIdValueId,
                // into an actual DP instance to be used as the prop value.
                dp = MapTable.GetDependencyPropertyValueFromId(dpId, dpName, out declaringType);
                if (dp == null)
                {
                    ThrowException(SRID.ParserCannotConvertPropertyValue, "Property", typeof(DependencyProperty).FullName);
                }

                bamlPropertyRecord.ValueObject = dp;
                bamlPropertyRecord.ValueObjectSet = true;
            }
            else
            {
                dp = (DependencyProperty)bamlPropertyRecord.ValueObject;
            }

            return dp;
        }

        internal object GetCustomValue(BamlPropertyCustomRecord bamlPropertyRecord, Type propertyType, string propertyName)
        {
            object valueObject = null;

            if (!bamlPropertyRecord.ValueObjectSet)
            {
                Exception innerException = null;
                short sid = bamlPropertyRecord.SerializerTypeId;

                try
                {
                    if (sid == (short)KnownElements.DependencyPropertyConverter)
                    {
                        valueObject = GetCustomDependencyPropertyValue(bamlPropertyRecord);
                    }
                    else
                    {
                        valueObject = bamlPropertyRecord.GetCustomValue(BinaryReader, propertyType, sid, this);
                    }
                }
                catch (Exception e)
                {
                    if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                    {
                        throw;
                    }

                    innerException = e;
                }

                if (!bamlPropertyRecord.ValueObjectSet && !bamlPropertyRecord.IsRawEnumValueSet)
                {
                    string message = SR.Get(SRID.ParserCannotConvertPropertyValue, propertyName, propertyType.FullName);
                    ThrowExceptionWithLine(message, innerException);
                }
            }
            else
            {
                valueObject = bamlPropertyRecord.ValueObject;
            }

            return valueObject;
        }

        // Read a property record that has value information known only to the
        // property's ValidType.
        protected virtual void ReadPropertyCustomRecord(BamlPropertyCustomRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "PropertyCustom");
            }

            // the value of the property
            object valueObject = null;

            // Get the element to set the property on
            object element = GetCurrentObjectData();

            // Get the attributeId
            short attributeId = bamlPropertyRecord.AttributeId;

            // Identify the property
            WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(this, attributeId, element is DependencyObject);

            if (!bamlPropertyRecord.ValueObjectSet)
            {
#if !STRESS
                // Get the value of the property that is obtained from the binary data in the record.
                try
                {
#endif
                    valueObject = GetCustomValue(bamlPropertyRecord, propertyDefinition.PropertyType, propertyDefinition.Name);
#if !STRESS
                }
                catch (Exception e)
                {
                    if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                    {
                        throw;
                    }

                    string message = SR.Get(SRID.ParserCannotConvertPropertyValue, propertyDefinition.Name, propertyDefinition.PropertyType.FullName);
                    ThrowExceptionWithLine(message, e);
                }
#endif
            }
            else
            {
                valueObject = bamlPropertyRecord.ValueObject;
            }

            FreezeIfRequired(valueObject);

            if (propertyDefinition.DependencyProperty != null)
            {
                Debug.Assert(element is DependencyObject, "Guaranteed by PropertyDefinition constructor");
                SetDependencyValue((DependencyObject)element, propertyDefinition.DependencyProperty, valueObject);
            }
            else if (propertyDefinition.PropertyInfo != null)
            {
                // Regular case for CLR property
                if (propertyDefinition.IsInternal)
                {
                    bool set = XamlTypeMapper.SetInternalPropertyValue(ParserContext,
                                                                       ParserContext.RootElement,
                                                                       propertyDefinition.PropertyInfo,
                                                                       element,
                                                                       valueObject);
                    if (!set)
                    {
                        ThrowException(SRID.ParserCantSetAttribute, "property", propertyDefinition.Name, "set");
                    }
                }
                else
                {
                    propertyDefinition.PropertyInfo.SetValue(element, valueObject,
                        BindingFlags.Default, null,
                        null, TypeConverterHelper.InvariantEnglishUS);
                }
            }
            else if (propertyDefinition.AttachedPropertySetter != null)
            {
                propertyDefinition.AttachedPropertySetter.Invoke(null, new object[] { element, valueObject });
            }
            else
            {
                ThrowException(SRID.ParserCantGetDPOrPi, GetPropertyNameFromAttributeId(attributeId));
            }
        }

        // Read a Property record, get the current element off the context stack and convert
        // the string value of the property into a real object.  Then use the element's SetValue
        // method to set the value for this DependencyProperty, or the PropertyInfo's SetValue
        // method if there is no DependencyProperty.
        protected virtual void ReadPropertyRecord(BamlPropertyRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "Property");
            }

            ReadPropertyRecordBase(bamlPropertyRecord.Value, bamlPropertyRecord.AttributeId, 0);
        }

        // Read a Property record, get the current element off the context stack and convert
        // the string value of the property into a real object using the TypeConverter specified
        // in the property record, rather than the Type's converter.
        protected virtual void ReadPropertyConverterRecord(BamlPropertyWithConverterRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "Property");
            }

            ReadPropertyRecordBase(bamlPropertyRecord.Value, bamlPropertyRecord.AttributeId,
                                   bamlPropertyRecord.ConverterTypeId);
        }

        // Read a Property record, get the current element off the context stack and convert
        // the string value of the property into a real object.  Obtain the string value
        // from the string table in the baml file.
        protected virtual void ReadPropertyStringRecord(BamlPropertyStringReferenceRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "Property");
            }

            string attribValue = GetPropertyValueFromStringId(bamlPropertyRecord.StringId);
            ReadPropertyRecordBase(attribValue, bamlPropertyRecord.AttributeId,
                                   0);
        }

        private object GetInnerExtensionValue(IOptimizedMarkupExtension optimizedMarkupExtensionRecord)
        {
            object valueObject = null;
            short memberId = optimizedMarkupExtensionRecord.ValueId;

            if (optimizedMarkupExtensionRecord.IsValueTypeExtension)
            {
                valueObject = MapTable.GetTypeFromId(memberId);
            }
            else if (optimizedMarkupExtensionRecord.IsValueStaticExtension)
            {
                valueObject = GetStaticExtensionValue(memberId);
            }
            else
            {
                valueObject = MapTable.GetStringFromStringId(memberId);
            }

            return valueObject;
        }

        private object GetStaticExtensionValue(short memberId)
        {
            object valueObject = null;

            if (memberId < 0)
            {
                short keyId = (short)-memberId;
                
                // if keyId is more than the range it is the actual resource, else it is the key.

                bool isKey;
                keyId = SystemResourceKey.GetSystemResourceKeyIdFromBamlId(keyId, out isKey);

                if (isKey)
                {
                    valueObject = SystemResourceKey.GetResourceKey(keyId);
                }
                else
                {
                    valueObject = SystemResourceKey.GetResource(keyId);
                }
            }
            else
            {
                BamlAttributeInfoRecord attribInfo = MapTable.GetAttributeInfoFromId(memberId);
                if (attribInfo != null)
                {
                    StaticExtension se = new StaticExtension();
                    se.MemberType = MapTable.GetTypeFromId(attribInfo.OwnerTypeId);
                    se.Member = attribInfo.Name;
                    valueObject = se.ProvideValue(null);
                }
            }

            return valueObject;
        }

        internal virtual object GetExtensionValue(
            IOptimizedMarkupExtension optimizedMarkupExtensionRecord,
            string                    propertyName)
        {
            object innerExtensionValue = null;
            object valueObject = null;
            short memberId = optimizedMarkupExtensionRecord.ValueId;
            short extensionTypeId = optimizedMarkupExtensionRecord.ExtensionTypeId;

            switch (extensionTypeId)
            {
                case (short)KnownElements.StaticExtension:
                    valueObject = GetStaticExtensionValue(memberId);
                    break;

                case (short)KnownElements.DynamicResourceExtension:
                    innerExtensionValue = GetInnerExtensionValue(optimizedMarkupExtensionRecord);
                    valueObject = new DynamicResourceExtension(innerExtensionValue);
                    break;

                case (short)KnownElements.StaticResourceExtension:
                    innerExtensionValue = GetInnerExtensionValue(optimizedMarkupExtensionRecord);
                    valueObject = new StaticResourceExtension(innerExtensionValue);
                    break;
            }

            if (valueObject == null)
            {
                string valueTypeName = string.Empty;

                switch (extensionTypeId)
                {
                    case (short)KnownElements.StaticExtension:
                        valueTypeName = typeof(StaticExtension).FullName;
                        break;
                    case (short)KnownElements.DynamicResourceExtension:
                        valueTypeName = typeof(DynamicResourceExtension).FullName;
                        break;
                    case (short)KnownElements.StaticResourceExtension:
                        valueTypeName = typeof(StaticResourceExtension).FullName;
                        break;
                }

                ThrowException(SRID.ParserCannotConvertPropertyValue, propertyName, valueTypeName);
            }

            return valueObject;
        }

        protected virtual void ReadPropertyWithExtensionRecord(BamlPropertyWithExtensionRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "Property");
            }

            // Define attrbuteId
            short attributeId = bamlPropertyRecord.AttributeId;

            // Identify the target element
            object element = GetCurrentObjectData();

            // Identify the property
            WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(this, attributeId, element is DependencyObject);

            // Get the value of the property
            object value = GetExtensionValue(bamlPropertyRecord, propertyDefinition.Name);

            // Read and set the value provided by the MarkupExtension on the element's property
            BaseReadOptimizedMarkupExtension(element, attributeId, propertyDefinition, value);
        }

        private void BaseReadOptimizedMarkupExtension(
            object             element,
            short              attributeId,
            WpfPropertyDefinition propertyDefinition,
            object             value)
        {
#if !STRESS
            try
            {
#endif
                // if the value is a ME, get the actual value from it.
                MarkupExtension me = value as MarkupExtension;
                if (me != null)
                {
                    value = ProvideValueFromMarkupExtension(me, element, propertyDefinition.DpOrPiOrMi);

                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.TraceActivityItem(
                                              TraceMarkup.ProvideValue,
                                              me,
                                              element,
                                              propertyDefinition.DpOrPiOrMi,
                                              value );
                    }
                }

                if( !SetPropertyValue( element, propertyDefinition, value ))
                {
                    ThrowException(SRID.ParserCantGetDPOrPi, GetPropertyNameFromAttributeId(attributeId));
                }

#if !STRESS
            }
            catch( Exception e )
            {
                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException)
                {
                    throw;
                }

                TargetInvocationException tie = e as TargetInvocationException;
                if( tie != null )
                {
                    e = tie.InnerException;
                }

                string message = SR.Get(SRID.ParserCannotConvertPropertyValue, propertyDefinition.Name, propertyDefinition.PropertyType.FullName);
                ThrowExceptionWithLine(message, e);
            }
#endif
        }


        //
        // Set a value onto a property of an object.  The property could be a DP
        // or a CLR property (figure out from the PropertyDefinition).
        //

        private bool SetPropertyValue( Object o, WpfPropertyDefinition propertyDefinition, object value )
        {
            bool succeeded = true;

            //
            // DP case
            //

            if (propertyDefinition.DependencyProperty != null)
            {
                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Start,
                                     TraceMarkup.SetPropertyValue,
                                     o,
                                     propertyDefinition.DependencyProperty.Name,
                                     value);
                }

                Debug.Assert(o is DependencyObject);
                SetDependencyValue((DependencyObject)o, propertyDefinition.DependencyProperty, value);

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Stop,
                                     TraceMarkup.SetPropertyValue,
                                     o,
                                     propertyDefinition.DependencyProperty.Name,
                                     value);
                }
            }

            //
            // Non-attached CLR property case.
            //

            else if (propertyDefinition.PropertyInfo != null)
            {
                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Start,
                                     TraceMarkup.SetPropertyValue,
                                     o,
                                     propertyDefinition.PropertyInfo.Name,
                                     value);
                }

                if (propertyDefinition.IsInternal)
                {
                    bool set = XamlTypeMapper.SetInternalPropertyValue(ParserContext,
                                                                       ParserContext.RootElement,
                                                                       propertyDefinition.PropertyInfo,
                                                                       o,
                                                                       value);
                    if (!set)
                    {
                        ThrowException(SRID.ParserCantSetAttribute, "property", propertyDefinition.Name, "set");
                    }
                }
                else
                {
                    propertyDefinition.PropertyInfo.SetValue(o, value, BindingFlags.Default, null, null, TypeConverterHelper.InvariantEnglishUS);
                }

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Stop,
                                     TraceMarkup.SetPropertyValue,
                                     o,
                                     propertyDefinition.PropertyInfo.Name,
                                     value);
                }
            }

            //
            // Attached CLR property case
            //

            else if (propertyDefinition.AttachedPropertySetter != null)
            {
                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Start,
                                     TraceMarkup.SetPropertyValue,
                                     o,
                                     propertyDefinition.AttachedPropertySetter.Name,
                                     value);
                }

                propertyDefinition.AttachedPropertySetter.Invoke(null, new object[] { o, value });

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Stop,
                                     TraceMarkup.SetPropertyValue,
                                     o,
                                     propertyDefinition.AttachedPropertySetter.Name,
                                     value);
                }
            }

            //
            // Error case
            //

            else
            {
                succeeded = false;
            }

            return succeeded;
        }


        // Read a Property record, get the current element off the context stack and a Type
        // object from the TypeId in the property record.  This is the object to set
        // on the property.
        protected virtual void ReadPropertyTypeRecord(BamlPropertyTypeReferenceRecord bamlPropertyRecord)
        {
            if (null == CurrentContext ||
                (ReaderFlags.DependencyObject != CurrentContext.ContextType &&
                 ReaderFlags.ClrObject != CurrentContext.ContextType))
            {
                ThrowException(SRID.ParserUnexpInBAML, "Property");
            }

            // Define attrbuteId
            short attributeId = bamlPropertyRecord.AttributeId;

            // Identify the target element
            object element = GetCurrentObjectData();

            // Get value type
            Type valueType = MapTable.GetTypeFromId(bamlPropertyRecord.TypeId);

            // Identify the property
            WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(this, attributeId, element is DependencyObject);

#if !STRESS
            try
            {
#endif
                if( !SetPropertyValue( element, propertyDefinition, valueType ))
                {
                    ThrowException(SRID.ParserCantGetDPOrPi, GetPropertyNameFromAttributeId(attributeId));
                }
#if !STRESS
            }
            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException)
                {
                    throw;
                }

                TargetInvocationException tie = e as TargetInvocationException;
                if( tie != null )
                {
                    e = tie.InnerException;
                }

                ThrowExceptionWithLine(SR.Get(SRID.ParserCannotSetValue, element.GetType().FullName, propertyDefinition.Name, valueType.Name), e);
            }
#endif
        }

        // Common section for setting a property defined by an attributeId to a value.
        private void ReadPropertyRecordBase(
            string   attribValue,
            short    attributeId,
            short    converterTypeId)
        {
            if( CurrentContext.CreateUsingTypeConverter )
            {
                // TypeConverter syntax rules state that there shall be no specifying
                //  property values on the TypeConverter object specification.

                // But like every other rule in XAML, we have an exception.

                // Exception: the xml:space property value is tolerated.  We set the
                //  ParserContext information and drop the rest of the data on the
                //  ground.  (We will not set the XmlSpace attached property like
                //  we would for non-TypeConverter-created objects.)

#if DEBUG
                // If we ever have more than one exception to the TypeConverter
                //  rule, we'll need to run this differentiation code outside of
                //  debug-only code path, too.
                short ownerTypeId;
                string name;
                BamlAttributeUsage attributeUsage;
                MapTable.GetAttributeInfoFromId(attributeId, out ownerTypeId, out name, out attributeUsage);

                Debug.Assert( attributeUsage == BamlAttributeUsage.XmlSpace,
                    "The xml:space attribute is the only one deemed compatible with TypeConverter syntax, but we've encountered something else.  How did this slip by the XamlReaderHelper TypeConverter syntax check?");
#endif

                ParserContext.XmlSpace = attribValue;

                // ParserContext state updated, and that's all we'll do for this record.
                return;
            }

            // If we get this far, it means we are not going to create an object
            //  using its type converter, hence it's safe to call GetCurrentObjectForData().
            //  (GetCurrentObjectData will create an instance using the default constructor,
            //   eliminating the possibility of TypeConverter creation.)
            object element = GetCurrentObjectData();

            // Get attributeUsage - using perf optimized call (avoiding attributeInfo record allocation)
            WpfPropertyDefinition propertyDefinition = new WpfPropertyDefinition(this, attributeId, element is DependencyObject);

#if !STRESS
            try
            {
#endif
                switch (propertyDefinition.AttributeUsage)
                {
                    case BamlAttributeUsage.RuntimeName:
                        // Add Name to appropriate scope
                        DoRegisterName(attribValue, element);
                        break;

                    case BamlAttributeUsage.XmlLang:
                        ParserContext.XmlLang = attribValue;
                        break;

                    case BamlAttributeUsage.XmlSpace:
                        ParserContext.XmlSpace = attribValue;
                        break;
                }

                // Try DependencyProperty case: If the property is a DP we can handle it faster than the general reflection case
                if (propertyDefinition.DependencyProperty != null)
                {
                    Debug.Assert(element is DependencyObject);

                    object propertyValue = ParseProperty(
                        (DependencyObject)element,
                        propertyDefinition.PropertyType,
                        propertyDefinition.Name,
                        propertyDefinition.DependencyProperty,
                        attribValue,
                        converterTypeId);

                    // If the value returned is not unset, then perform a DependencyObject.SetValue.  An
                    // UnsetValue can occur if a resource reference is bound or some other way of setting
                    // up the value is done in ParseProperty.
                    if (propertyValue != DependencyProperty.UnsetValue)
                    {
                        SetPropertyValue( element, propertyDefinition, propertyValue );
                    }
                }
                else if (propertyDefinition.PropertyInfo != null)
                {
                    // General case of CLR or Attached property

                    // Calculate the value of the property
                    object propertyValue = ParseProperty(
                        element,
                        propertyDefinition.PropertyType,
                        propertyDefinition.Name,
                        propertyDefinition.PropertyInfo,
                        attribValue, converterTypeId);

                    // If the value returned is not unset, then perform a PropertyInfo.SetValue.
                    // An UnsetValue can occur if a resource reference is bound or some other
                    // way of setting up the value is done in ParseProperty.
                    if (propertyValue != DependencyProperty.UnsetValue)
                    {
                        // Assign the value to the property
                        SetPropertyValue( element, propertyDefinition, propertyValue );
                    }
                }
                else if (propertyDefinition.AttachedPropertySetter != null)
                {
                    // Attached property
                    // General case of CLR or Attached property

                    // Calculate the value of the property
                    object propertyValue = ParseProperty(
                        element,
                        propertyDefinition.PropertyType,
                        propertyDefinition.Name,
                        propertyDefinition.AttachedPropertySetter,
                        attribValue, converterTypeId);

                    // If the value returned is not unset, then perform a PropertyInfo.SetValue.
                    // An UnsetValue can occur if a resource reference is bound or some other
                    // way of setting up the value is done in ParseProperty.
                    if (propertyValue != DependencyProperty.UnsetValue)
                    {
                        // Attached Property accessible via SetFoo/GetFoo static methods
                        SetPropertyValue( element, propertyDefinition, propertyValue );
                    }
                }
                else
                {
                    // Neither DP, nor Clr, nor Attached property.

                    // We may have found the attribute record (which should always work unless
                    // the file is corrupted), but it may not resolve to a property with the
                    // currently loaded set of assemblies. Try a locally defined event before complaining.
                    bool isRE = false;
                    object reidOrEi = null;
                    bool isInternal = false;

                    if (_componentConnector != null && _rootElement != null)
                    {
                        reidOrEi = GetREOrEiFromAttributeId(attributeId, out isInternal, out isRE);
                    }

                    if (reidOrEi != null)
                    {
                        Delegate d;

                        if (isRE)
                        {
                            RoutedEvent reid = reidOrEi as RoutedEvent;
                            d = XamlTypeMapper.CreateDelegate(ParserContext,
                                                              reid.HandlerType,
                                                              ParserContext.RootElement,
                                                              attribValue);
                            if (d == null)
                            {
                                ThrowException(SRID.ParserCantCreateDelegate, reid.HandlerType.Name, attribValue);
                            }

                            UIElement uiel = element as UIElement;
                            if (uiel != null)
                            {
                                uiel.AddHandler(reid, d);
                            }
                            else
                            {
                                ContentElement ce = element as ContentElement;
                                if (ce != null)
                                {
                                    ce.AddHandler(reid, d);
                                }
                                else
                                {
                                    // In the case where the element doesn't support any routed event AddHandler
                                    // we know the null pointer exception is caught and wrapped in a XAML parse exception
                                    // below (we would have added an error message but 3.5 doesn't allow us to add one).
                                    UIElement3D uie3D = element as UIElement3D;
                                    uie3D.AddHandler(reid, d);
                                }
                            }
                        }
                        else
                        {
                            EventInfo ei = reidOrEi as EventInfo;
                            d = XamlTypeMapper.CreateDelegate(ParserContext,
                                                              ei.EventHandlerType,
                                                              ParserContext.RootElement,
                                                              attribValue);
                            if (d == null)
                            {
                                ThrowException(SRID.ParserCantCreateDelegate, ei.EventHandlerType.Name, attribValue);
                            }

                            if (isInternal)
                            {
                                bool added = XamlTypeMapper.AddInternalEventHandler(ParserContext,
                                                                                    ParserContext.RootElement,
                                                                                    ei,
                                                                                    element,
                                                                                    d);
                                if (!added)
                                {
                                    ThrowException(SRID.ParserCantSetAttribute, "event", ei.Name, "add");
                                }
                            }
                            else
                            {
                                ei.AddEventHandler(element, d);
                            }
                        }

                        return;
                    }
                    else
                    {
                        ThrowException(SRID.ParserCantGetDPOrPi, propertyDefinition.Name);
                    }
                }
#if !STRESS
            }

            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException)
                {
                    throw;
                }

                TargetInvocationException tie = e as TargetInvocationException;
                if( tie != null )
                {
                    e = tie.InnerException;
                }

                ThrowExceptionWithLine(SR.Get(SRID.ParserCannotSetValue, element.GetType().FullName, propertyDefinition.AttributeInfo.Name, attribValue), e);
            }
#endif

        }

        //+-----------------------------------------------------------------------------------------------------
        //
        //  DoRegisterName
        //
        //  Register a name against the name scope.
        //
        //+-----------------------------------------------------------------------------------------------------

        private void DoRegisterName( string name, object element )
        {
            // Store this name in the context (used by XamlParseException).
            if( CurrentContext != null )
            {
                CurrentContext.ElementNameOrPropertyName = name;
            }


            if (ParserContext != null && ParserContext.NameScopeStack != null)
            {
                if (0 != ParserContext.NameScopeStack.Count)
                {
                    INameScope nameScopeTop = ParserContext.NameScopeStack.Pop() as INameScope;
                    if ((NameScope.NameScopeFromObject(element) != null) && 0 != ParserContext.NameScopeStack.Count)
                    {
                        // Consider the following scenario
                        // <Window x:Name="myWindow">
                        //     ...
                        //     <Style x:Name="myStyle">
                        //         ...
                        //         <SolidColorBrush x:Name="myBrush">
                        //         </SolidColorBrush>
                        //     </Style>
                        // </Window>
                        //
                        // "myWindow" gets registered with the Window
                        // "myStyle" also gets registered with the Window
                        // "myBrush" gets registered with the Style
                        INameScope nameScopePeek = ParserContext.NameScopeStack.Peek() as INameScope;
                        if (nameScopePeek != null)
                        {
                            nameScopePeek.RegisterName(name, element);
                        }
                    }
                    else
                    {
                        nameScopeTop.RegisterName(name, element);
                    }
                    ParserContext.NameScopeStack.Push(nameScopeTop);
                }
            }
        }

        // Read the start of an array property.  Set up the BamlArrayHolder that is needed to
        // hold array contents.
        protected void ReadPropertyArrayStartRecord(BamlPropertyArrayStartRecord bamlPropertyArrayStartRecord)
        {
            short  attributeId = bamlPropertyArrayStartRecord.AttributeId;
            object parent = GetCurrentObjectData();
            BamlCollectionHolder holder = new BamlCollectionHolder(this, parent, attributeId, false /*needDefault*/);

            if (!holder.PropertyType.IsArray)
            {
                ThrowException(SRID.ParserNoMatchingArray, GetPropertyNameFromAttributeId(attributeId));
            }

            Debug.Assert(!holder.ReadOnly); // this is being checked in XamlReaderHelper, just assert

            PushContext(ReaderFlags.PropertyArray | ReaderFlags.CollectionHolder, holder, holder.PropertyType, 0);

            // Set the name of the property into the context
            CurrentContext.ElementNameOrPropertyName = holder.AttributeName;
        }

        // Read the end of an array.  Convert the array holder that has been accumulating
        // the array contents into a real array and set it on the property.
        protected virtual void ReadPropertyArrayEndRecord()
        {
            BamlCollectionHolder holder = (BamlCollectionHolder)GetCurrentObjectData();

            // If we don't have a dictionary yet, then create one now.  This can
            // happen if the Array property is read/write, but does not
            // contain a value and there was no array object under the
            // property.
            if (holder.Collection == null)
            {
                InitPropertyCollection(holder, CurrentContext);
            }

            ArrayExtension arrayExt = holder.ArrayExt;

            Debug.Assert(arrayExt != null);

            holder.Collection = ProvideValueFromMarkupExtension(arrayExt, holder, null);
            holder.SetPropertyValue();

            PopContext();
        }

        // Start of an IList or IEnumerable property.  Get the IList value from the parent
        // object and store it on the reader stack, so that records encountered under this
        // list can be added to it.
        protected virtual void ReadPropertyIListStartRecord(
            BamlPropertyIListStartRecord bamlPropertyIListStartRecord)
        {
            short attributeId = bamlPropertyIListStartRecord.AttributeId;
            object parent = GetCurrentObjectData();
            BamlCollectionHolder holder = new BamlCollectionHolder(this, parent, attributeId);
            Type expectedType = holder.PropertyType;

            // If the property does not implement IList or IAddChild, see if the parent
            // (which is the current object on the top of the stack) implements IAddChild
            // and try that for adding items later on.  First look at defined propertyType
            // obtained from the DP or PropertyInfo.  If that doesn't work, then look at
            // the actual type of object retrieved using GetValue.
            ReaderFlags flags = ReaderFlags.Unknown;
            if (typeof(IList).IsAssignableFrom(expectedType))
            {
                flags = ReaderFlags.PropertyIList;
            }
            else if (BamlRecordManager.TreatAsIAddChild(expectedType))
            {
                flags = ReaderFlags.PropertyIAddChild;
                holder.Collection = holder.DefaultCollection;
                holder.ReadOnly = true;
            }
            else if (typeof(IEnumerable).IsAssignableFrom(expectedType) &&
                     (BamlRecordManager.AsIAddChild(GetCurrentObjectData())) != null)
            {
                flags = ReaderFlags.PropertyIAddChild;
                holder.Collection = CurrentContext.ObjectData;
                holder.ReadOnly = true;
                expectedType = CurrentContext.ObjectData.GetType();
            }
            else
            {
                // if we reached this case, then we had a read-only IEnumerable property
                // under a non-IAddChild element.  Throw an exception
                ThrowException(SRID.ParserReadOnlyProp, holder.PropertyDefinition.Name);
            }

            PushContext(flags | ReaderFlags.CollectionHolder, holder, expectedType, 0);

            // Set the name of the property into the context
            CurrentContext.ElementNameOrPropertyName = holder.AttributeName;
        }

        // End of the IList is reached.  Since this is a read only property, there is nothing
        // to do bug pop the reader stack.
        protected virtual void ReadPropertyIListEndRecord()
        {
            SetCollectionPropertyValue(CurrentContext);

            PopContext();
        }

        // Start of an IDictionary encountered.  Set up the BamlDictionaryHolder on the reader
        // stack so that items can be added to it.
        protected virtual void ReadPropertyIDictionaryStartRecord(
            BamlPropertyIDictionaryStartRecord bamlPropertyIDictionaryStartRecord)
        {
            short  attributeId = bamlPropertyIDictionaryStartRecord.AttributeId;
            object parent = GetCurrentObjectData();
            BamlCollectionHolder holder = new BamlCollectionHolder(this, parent, attributeId);

            PushContext(ReaderFlags.PropertyIDictionary | ReaderFlags.CollectionHolder, holder, holder.PropertyType, 0);

            // Set the name of the property into the context
            CurrentContext.ElementNameOrPropertyName = holder.AttributeName;
        }

        // If the dictionary does not exist already, then set it.  This is indicated by
        // having a non-null DpOrPi in the BamlDictionaryHolder object on the context stack.
        protected virtual void ReadPropertyIDictionaryEndRecord()
        {
            SetCollectionPropertyValue(CurrentContext);

            PopContext();
        }

        private void SetCollectionPropertyValue(ReaderContextStackData context)
        {
            BamlCollectionHolder holder = (BamlCollectionHolder)context.ObjectData;

            if (holder.Collection == null)
            {
                // If we don't have a collection yet, then create one now.  This can
                // happen if the IDictionary property is read/write, but does not
                // contain a value and there was no IDictionary object under the property

                InitPropertyCollection(holder, context);
            }

            if (!holder.ReadOnly && holder.Collection != holder.DefaultCollection)
            {
                // If we had the default collection is not the same as the collection in the holder,
                // then either we have a RW property with a null default value or a RW property with an
                // explicit element value.  In either case, set the property's value to be the collection

                holder.SetPropertyValue();
            }
        }

        // takes a BamlCollectionHolder and its context and sets the value of the holder's collection to be
        // either the default collection instantiated by the holder's constructor or a newly created
        // collection created based on the expected type.
        // *** Used when we do not have an explicit tag.
        private void InitPropertyCollection(BamlCollectionHolder holder, ReaderContextStackData context)
        {
            // this method should only be called to initialize the collection
            Debug.Assert (holder.Collection == null);

            if (context.ContextType == ReaderFlags.PropertyArray)
            {
                // arrays are a little different than other collections, because we wrap them in an array extension.
                // Here we create an array extension and assign the element type based on the property.

                ArrayExtension arrayExt = new ArrayExtension();
                arrayExt.Type = context.ExpectedType.GetElementType();
                holder.Collection = arrayExt;
            }
            else if (holder.DefaultCollection != null)
            {
                // if we the property getter returned a default value, then we use that collection to insert
                // as the property's collection.

                holder.Collection = holder.DefaultCollection;
            }
            else
            {
                ThrowException(SRID.ParserNullPropertyCollection, holder.PropertyDefinition.Name);
            }

            context.ExpectedType = null; // Don't want to receive any other values
        }

        private BamlCollectionHolder GetCollectionHolderFromContext(ReaderContextStackData context, bool toInsert)
        {
            BamlCollectionHolder holder = (BamlCollectionHolder)context.ObjectData;

            // If we don't have a collection yet, then create one now.
            if (holder.Collection == null && toInsert)
            {
                // if this collection holder has not yet been used, then initialize its collection
                InitPropertyCollection(holder, context);
            }

            if (toInsert && holder.IsClosed)
            {
                // if an explicit collection was under a collection property and following its end tag an element
                // was placed, then throw an exception.
                ThrowException(SRID.ParserPropertyCollectionClosed, holder.PropertyDefinition.Name);
            }

            return holder;
        }

        protected IDictionary GetDictionaryFromContext(ReaderContextStackData context, bool toInsert)
        {
            IDictionary result = null;

            if (context != null)
            {
                if (context.CheckFlag(ReaderFlags.IDictionary))
                {
                    result = (IDictionary)GetObjectDataFromContext(context);
                }
                else if (context.ContextType == ReaderFlags.PropertyIDictionary)
                {
                    BamlCollectionHolder holder = GetCollectionHolderFromContext(context, toInsert);

                    result = holder.Dictionary;
                }
            }

            return result;
        }

        private IList GetListFromContext(ReaderContextStackData context)
        {
            IList result = null;

            if (context != null)
            {
                if (context.CheckFlag(ReaderFlags.IList))
                {
                    result = (IList)GetObjectDataFromContext(context);
                }
                else if (context.ContextType == ReaderFlags.PropertyIList)
                {
                    BamlCollectionHolder holder = GetCollectionHolderFromContext(context, true /*toInsert*/);

                    result = holder.List;
                }
            }

            return result;
        }

        private IAddChild GetIAddChildFromContext(ReaderContextStackData context)
        {
            IAddChild result = null;

            if (context != null)
            {
                if (context.CheckFlag(ReaderFlags.IAddChild))
                {
                    result = BamlRecordManager.AsIAddChild(context.ObjectData);
                }
                else if (context.ContextType == ReaderFlags.PropertyIAddChild)
                {
                    BamlCollectionHolder holder = GetCollectionHolderFromContext(context, false /*toInsert*/);

                    result = BamlRecordManager.AsIAddChild(holder.Collection);
                }
            }

            return result;
        }

        private ArrayExtension GetArrayExtensionFromContext(ReaderContextStackData context)
        {
            ArrayExtension result = null;

            if (context != null)
            {
                result = context.ObjectData as ArrayExtension;

                if (context.CheckFlag(ReaderFlags.ArrayExt))
                {
                    result = (ArrayExtension)context.ObjectData;
                }
                else if (context.ContextType == ReaderFlags.PropertyArray)
                {
                    BamlCollectionHolder holder = GetCollectionHolderFromContext(context, true /*toInsert*/);

                    result = holder.ArrayExt;
                }
            }

            return result;
        }

        // Read a x:foo="bar" record.  If foo is "Name" then this is
        // a dictionary key record which is stored in the BamlDictionaryHolder until the end of
        // the current object is read.  Then it is used as a key to add the current
        // object to the parent dictionary.  This is currently all the reader understands,
        // but a more general handling of def records will occur with PS# 14348
        //
        // Second special case added: If foo is "Uid" then this is an identifier
        //  unique to this tag.  This is added by a MSBuild pre-processing step
        //  or a tool, and is used for localization or accessibility.  In the
        //  case of UIElement objects, this value will end up as an attached
        //  property DefinitionProperties.UidProperty.
        //
        protected virtual void ReadDefAttributeRecord(BamlDefAttributeRecord bamlDefAttributeRecord)
        {
            // Get the name of the attribute from the string table, and cache it in the
            // def record.
            bamlDefAttributeRecord.Name = MapTable.GetStringFromStringId(
                                            bamlDefAttributeRecord.NameId);

            if (bamlDefAttributeRecord.Name == XamlReaderHelper.DefinitionName)
            {
                object key = XamlTypeMapper.GetDictionaryKey(bamlDefAttributeRecord.Value, ParserContext);

                if (key == null)
                {
                    ThrowException(SRID.ParserNoResource, bamlDefAttributeRecord.Value);
                }

                SetKeyOnContext(key, bamlDefAttributeRecord.Value, CurrentContext, ParentContext);
            }

            else if(bamlDefAttributeRecord.Name == XamlReaderHelper.DefinitionUid ||
                    bamlDefAttributeRecord.NameId == BamlMapTable.UidStringId)
            {
                // The x:Uid attribute is added to all elements in the markup.
                //  This means we'll be called here under all sorts of conditions,
                //  most of which are meaningless.  Just ignore the useless parts.

                // If we don't have an object to set to - bail.
                if( CurrentContext == null )
                    return;

                CurrentContext.Uid = bamlDefAttributeRecord.Value;

                // UIDs are only meaningful on UIElements.  Ignore all cases where
                //  the attribute was added to irrelevant elements.  Don't instantiate delay
                //  created objects here by calling GetCurrentObjectData(), because delay
                //  created objects are never UIElements. Instantiating non-UIElements here
                //  will cause delay creation of value types to fail.
                UIElement element = CurrentContext.ObjectData as UIElement;
                if( element != null )
                {
                    SetDependencyValue(element, UIElement.UidProperty, bamlDefAttributeRecord.Value);
                }
            }

            else if (bamlDefAttributeRecord.Name == XamlReaderHelper.DefinitionShared)
            {
                // The x:Shared token information was stored in the IBamlDictionaryKey
                // in the compiled case. Otherwise, it makes no sense to use it.
                ThrowException(SRID.ParserDefSharedOnlyInCompiled);
            }

            else if (bamlDefAttributeRecord.Name == XamlReaderHelper.DefinitionRuntimeName) // x:Name
            {
                object element = GetCurrentObjectData();

                if( element != null )
                {
                    DoRegisterName(bamlDefAttributeRecord.Value, element);
                }
            }
            else
            {
                // You will only get this error if the XamlParser is out of sync
                // with the BamlRecordReader (since it should catch unknown def
                // attributes), or if a BamlWriter was used to write
                // a bogus attribute.
                ThrowException(SRID.ParserUnknownDefAttribute, bamlDefAttributeRecord.Name);
            }
        }



        // Read a x:Key="{x:Type Foo}" record.  If foo has been resolved at compile or parse
        // time into a type represented by a TypeId in the baml record.  This is done for
        // more efficient key assignments in a dictionary.
        //
        protected virtual void ReadDefAttributeKeyTypeRecord(
            BamlDefAttributeKeyTypeRecord bamlDefAttributeRecord)
        {
            // Get the actual type from the TypeId
            Type keyType = MapTable.GetTypeFromId(bamlDefAttributeRecord.TypeId);

            if (keyType == null)
            {
                ThrowException(SRID.ParserNoResource, XamlReaderHelper.DefinitionName);
            }

            SetKeyOnContext(keyType, XamlReaderHelper.DefinitionName, CurrentContext, ParentContext);
        }


        // Sets the "key" arg as the key on "context".  "parentContext" is the context
        // one level above "context".  Verification is done to make sure that the
        // "parentContext" is a valid Dictionary.  An error is thrown if it is not.
        private void SetKeyOnContext(
            object key,
            string attributeName,
            ReaderContextStackData context,
            ReaderContextStackData parentContext)
        {
            try
            {
                // make sure parent is a dictionary
                GetDictionaryFromContext(parentContext, true /*toInsert*/);
            }
            catch (XamlParseException e)
            {
                // rethrow with a better error message
                if (parentContext.CheckFlag(ReaderFlags.CollectionHolder))
                {
                    BamlCollectionHolder holder = (BamlCollectionHolder)parentContext.ObjectData;
                    object element = context.ObjectData;

                    if (element != null && element == holder.Dictionary)
                    {
                        ThrowExceptionWithLine(SR.Get(SRID.ParserKeyOnExplicitDictionary, attributeName,
                                       element.GetType().ToString(), holder.PropertyDefinition.Name), e);
                    }
                }

                ThrowExceptionWithLine(SR.Get(SRID.ParserNoMatchingIDictionary, attributeName), e);
            }

            // set key on context
            context.Key = key;
        }


        // Text content has been read.  This may be a text child of an object or
        // it may be the value of a complex property, or it may the only content
        // in the stream (in which case we just have Text as the root...)
        protected virtual void ReadTextRecord(BamlTextRecord bamlTextRecord)
        {
            BamlTextWithIdRecord bamlTextWithId = bamlTextRecord as BamlTextWithIdRecord;
            if (bamlTextWithId != null)
            {
                // Get the value string from the string table, and cache it in the
                // record.
                bamlTextWithId.Value = MapTable.GetStringFromStringId(
                                                bamlTextWithId.ValueId);
            }

            if (null == CurrentContext)
            {
                // It is okay to have a null context and we are doing a fragment
                // and this is a top-level item. If the context is null
                // just add the text to the RootList Array.

                // set the _componentConnector and Element properties to NULL.
                _componentConnector = null;
                _rootElement = null;

                // Add raw text to the root list.
                RootList.Add(bamlTextRecord.Value);

                return;
            }

            // Creating a type form text involves a type converter.
            // Check if there is a type converter associated with this text.
            short converterTypeId = 0; // 0 is an invalid typeId for a converter
            BamlTextWithConverterRecord bamlTextWithConverter = bamlTextRecord as BamlTextWithConverterRecord;
            if (bamlTextWithConverter != null)
            {
                converterTypeId = bamlTextWithConverter.ConverterTypeId;
            }

            switch (CurrentContext.ContextType)
            {
                // Typical case of text content under an object
                case ReaderFlags.DependencyObject:
                case ReaderFlags.ClrObject:
                {
                    if (CurrentContext.CreateUsingTypeConverter)
                    {
                        Debug.Assert( CurrentContext.ObjectData == null && CurrentContext.ExpectedType != null,
                            "We had expected to create this object using a TypeConverter - but there's already an instance here.  Who created the instance and broke our ability to use a TypeConverter?");

                        // Use a TypeConverter to create an object instance from text.
                        object o = GetObjectFromString(CurrentContext.ExpectedType, bamlTextRecord.Value, converterTypeId);
                        if (DependencyProperty.UnsetValue != o)
                        {
                            CurrentContext.ObjectData = o;
                            CurrentContext.ExpectedType = null;
                        }
                        else
                        {
                            ThrowException(SRID.ParserCannotConvertString, bamlTextRecord.Value,
                                           CurrentContext.ExpectedType.FullName);
                        }
                    }
                    else
                    {
                        // Should not use a TypeConverter - the text is to be
                        //  treated as content of object.  GetCurrentObjectData will
                        //  create an instance if one doesn't already exist.
                        object parent = GetCurrentObjectData();
                        if (parent == null)
                        {
                            // GetCurrentObjectData failed to create an object for us to add content to.
                            ThrowException(SRID.ParserCantCreateInstanceType, CurrentContext.ExpectedType.FullName);
                        }

                        // We have object instance, and we have several ways to put
                        //  text into that object.
                        IAddChild iacParent = GetIAddChildFromContext(CurrentContext);
                        if (iacParent != null)
                        {
                            iacParent.AddText(bamlTextRecord.Value);
                        }
                        else if (CurrentContext.ContentProperty != null)
                        {
                            AddToContentProperty(parent, CurrentContext.ContentProperty, bamlTextRecord.Value);
                        }
                        else
                        {
                            // All of the above attempts to deal with the text has failed.
                            ThrowException(SRID.ParserIAddChildText,
                                    parent.GetType().FullName,
                                    bamlTextRecord.Value);
                        }
                    }

                    break;
                }

                case ReaderFlags.PropertyComplexDP:
                {
                    if (null == CurrentContext.ExpectedType)
                    {
                        ThrowException(SRID.ParserNoComplexMulti,
                                       GetPropNameFrom(CurrentContext.ObjectData));
                    }

                    // If we get here, the complex property tag's first child is a text element.
                    // The only way text is legal as the child of a complex property is if there are no
                    // other tags under the property, so we assume this is the case and try to convert
                    // from text. If we're wrong, the next tag will throw an error.
                    BamlAttributeInfoRecord attribInfo = CurrentContext.ObjectData as BamlAttributeInfoRecord;
                    object o = ParseProperty(
                                        (DependencyObject)GetParentObjectData(),
                                        attribInfo.DP.PropertyType,
                                        attribInfo.DP.Name,
                                        attribInfo.DP,
                                        bamlTextRecord.Value, converterTypeId);

                    if (DependencyProperty.UnsetValue != o)
                    {
                        SetDependencyComplexProperty(o);
                    }
                    else
                    {
                        ThrowException(SRID.ParserCantCreateTextComplexProp,
                              attribInfo.OwnerType.FullName,
                              bamlTextRecord.Value);
                    }
                    break;
                }

                case ReaderFlags.PropertyComplexClr:
                {
                    if (null == CurrentContext.ExpectedType)
                    {
                        ThrowException(SRID.ParserNoComplexMulti,
                                       GetPropNameFrom(CurrentContext.ObjectData));
                    }

                    // Following same logic as above...
                    object o = GetObjectFromString(CurrentContext.ExpectedType, bamlTextRecord.Value, converterTypeId);

                    if (DependencyProperty.UnsetValue != o)
                    {
                        SetClrComplexProperty(o);
                    }
                    else
                    {
                        ThrowException(SRID.ParserCantCreateTextComplexProp,
                            CurrentContext.ExpectedType.FullName, bamlTextRecord.Value);
                    }

                    break;
                }

                case ReaderFlags.PropertyIAddChild:
                {
                    BamlCollectionHolder holder = GetCollectionHolderFromContext(CurrentContext, true);
                    IAddChild iaddchild = BamlRecordManager.AsIAddChild(holder.Collection);

                    if (iaddchild == null)
                    {
                        ThrowException(SRID.ParserNoMatchingIList, "?");
                    }

                    iaddchild.AddText(bamlTextRecord.Value);
                    break;
                }

                case ReaderFlags.PropertyIList:
                {
                    BamlCollectionHolder holder = GetCollectionHolderFromContext(CurrentContext, true);

                    if (holder.List == null)
                    {
                        ThrowException(SRID.ParserNoMatchingIList, "?");
                    }

                    holder.List.Add(bamlTextRecord.Value);
                    break;
                }

                case ReaderFlags.ConstructorParams:
                {
                    // Store the text parameter in the list of constructor
                    // parameters.  This will be resolved later into the correct
                    // object type once we determine which constructor to use.
                    SetConstructorParameter(bamlTextRecord.Value);
                    break;
                }

                default:
                {
                    ThrowException(SRID.ParserUnexpInBAML, "Text");
                    break;
                }
            }
        }

        // Read a PresentationOptions:foo="bar" record.
        protected virtual void ReadPresentationOptionsAttributeRecord(BamlPresentationOptionsAttributeRecord bamlPresentationOptionsAttributeRecord)
        {
            // Get the name of the attribute from the string table, and cache it in the
            // def record.
            bamlPresentationOptionsAttributeRecord.Name = MapTable.GetStringFromStringId(
                                            bamlPresentationOptionsAttributeRecord.NameId);

            if (bamlPresentationOptionsAttributeRecord.Name == XamlReaderHelper.PresentationOptionsFreeze)
            {
                // Handle PresentationOptions:Freeze attribute by setting
                // a flag in the ParserContext.  When Freezables are added
                // to the tree, they check this flag, and Freeze if required.
                bool freeze = Boolean.Parse(bamlPresentationOptionsAttributeRecord.Value);

                _parserContext.FreezeFreezables = freeze;
            }
            else
            {
                // You will only get this error if the XamlParser is out of sync
                // with the BamlRecordReader (since it should catch unknown def
                // attributes), or if a BamlWriter was used to write
                // a bogus attribute.
                ThrowException(SRID.ParserUnknownPresentationOptionsAttribute, bamlPresentationOptionsAttributeRecord.Name);
            }
        }

        //
        // Set a property value that will go onto a DP.  Assume that the object to receive the
        // value, and the property on which to set it, are on the reader stack.
        //
        private void SetDependencyComplexProperty(object o)
        {
            Debug.Assert(null != CurrentContext &&
                ReaderFlags.PropertyComplexDP == CurrentContext.ContextType &&
                null != CurrentContext.ExpectedType && null != ParentContext);

            object parent = GetParentObjectData();
            BamlAttributeInfoRecord attribInfo = (BamlAttributeInfoRecord)GetCurrentObjectData();

            SetDependencyComplexProperty( parent, attribInfo, o );
        }

        //
        // Set a property value that will go onto a DP
        //
        private void SetDependencyComplexProperty(
                                object currentTarget,
                                BamlAttributeInfoRecord attribInfo,
                                object o)
        {
            DependencyProperty dp = currentTarget is DependencyObject ? attribInfo.DP : null;
            PropertyInfo propertyInfo = attribInfo.PropInfo;
            MethodInfo attachedPropertySetter = null; // postpone allocating a reflection data until necessary

#if !STRESS
            try
            {
#endif
                // ObjectData is either a DependencyProperty or a PropertyInfo.
                // Check if the object is a MarkupExtension.  If so, then get the value
                // to set on this property from the MarkupExtension itself.
                MarkupExtension me = o as MarkupExtension;
                if (me != null)
                {
                    o = ProvideValueFromMarkupExtension(me, currentTarget, dp);
                }

                // Check if we have a Nullable type.  If so and the object being set is
                // not a Nullable or an expression, then attempt a conversion.
                Type propertyType = null;
                if (dp != null)
                {
                    propertyType = dp.PropertyType;
                }
                else if (propertyInfo != null)
                {
                    propertyType = propertyInfo.PropertyType;
                }
                else
                {
                    // Touching reflection information about the setter only now to avoid extra memory allocations
                    if (attribInfo.AttachedPropertySetter == null)
                    {
                        XamlTypeMapper.UpdateAttachedPropertySetter(attribInfo);
                    }
                    attachedPropertySetter = attribInfo.AttachedPropertySetter;
                    if (attachedPropertySetter != null)
                    {
                        propertyType = attachedPropertySetter.GetParameters()[1].ParameterType;
                    }
                    else
                    {
                        Debug.Assert(false); 
                    }
                }
                o = OptionallyMakeNullable(propertyType, o, attribInfo.Name);

                // DependencyProperty, use the associated DependencyObject's SetValue.  Otherwise
                // use the PropertyInfo's SetValue.
                if (dp != null)
                {
                    Debug.Assert(currentTarget is DependencyObject);
                    SetDependencyValue((DependencyObject)currentTarget, dp, o);
                }
                else if (propertyInfo != null)
                {
                    propertyInfo.SetValue(currentTarget,o,BindingFlags.Default,null,null,
                                    TypeConverterHelper.InvariantEnglishUS);
                }
                else if (attachedPropertySetter != null)
                {
                    attachedPropertySetter.Invoke(null, new object[] { currentTarget, o });
                }
                else
                {
                    Debug.Assert(false); // We do not expect to be here after all checks done in propertyType identification above
                }

#if !STRESS
            }

            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException)
                {
                    throw;
                }

                TargetInvocationException tie = e as TargetInvocationException;
                if( tie != null )
                {
                    e = tie.InnerException;
                }

                ThrowExceptionWithLine(SR.Get(SRID.ParserCannotSetValue, currentTarget.GetType().FullName, attribInfo.Name, o), e);
            }
#endif

            // The property has been set, so we do not expect any more child tags --
            // anything more under this context will cause an error
            CurrentContext.ExpectedType = null;
        }

        static private Type NullableType = typeof(Nullable<>);

        static internal bool IsNullable(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == NullableType;
        }

        // If the property being set is nullable and the object is not null or an expression,
        // check if the passed object is the right type.
        internal object OptionallyMakeNullable(Type propertyType, object o, string propName)
        {
            object toReturn = o;
            if( !TryOptionallyMakeNullable( propertyType, propName, ref toReturn ))
            {
                ThrowException(SRID.ParserBadNullableType,
                               propName,
                               ((Type)propertyType.GetGenericArguments()[0]).Name,
                               o.GetType().FullName);
            }

            return toReturn;
        }


        // This is a form of OptionallyMakeNullable that doesn't throw.  We split it out so that the different callers
        // can throw their own exception (StyleHelper uses this too).

        static internal bool TryOptionallyMakeNullable( Type propertyType, string propName, ref object o  )
        {
            // if o was nullable, it has been unwrapped and boxed when it was passed into this function

            if ((o != null) && IsNullable(propertyType) && !(o is Expression) && !(o is MarkupExtension) )
            {
                Type genericType = (Type)propertyType.GetGenericArguments()[0];
                Debug.Assert(genericType != null);

                if (genericType != o.GetType())
                {
                    return false;
                }
            }

            return true;
        }


        //+--------------------------------------------------------------------------------------------------------------------------
        //
        //  SetClrComplexPropertyCore
        //
        //  This virtual is called to set a non-DP property on an object.  The base implementation  sets it via
        //  a property info, but subclasses can override it to avoid reflection.
        //
        //+--------------------------------------------------------------------------------------------------------------------------

        internal virtual void SetClrComplexPropertyCore(object parentObject, object value, MemberInfo memberInfo)
        {
            // Check if the object is a MarkupExtension.  If so, then get the value
            // to set on this property from the MarkupExtension itself.

            MarkupExtension me = value as MarkupExtension;
            if (me != null)
            {
                value = ProvideValueFromMarkupExtension(me, parentObject, memberInfo);
            }

            // Check if we have a Nullable type.  If so and the object being set is
            // not a Nullable or an expression, then attempt a conversion.
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                value = OptionallyMakeNullable(propertyInfo.PropertyType, value, propertyInfo.Name);

                propertyInfo.SetValue(parentObject, value, BindingFlags.Default, null, null
                                , TypeConverterHelper.InvariantEnglishUS);
            }
            else
            {
                Debug.Assert(memberInfo is MethodInfo);
                MethodInfo methodInfo = (MethodInfo)memberInfo;
                value = OptionallyMakeNullable(methodInfo.GetParameters()[1].ParameterType, value, methodInfo.Name.Substring("Set".Length));

                methodInfo.Invoke(null, new object[] { parentObject, value });
            }
        }



        //
        // Set a property value that will go onto a CLR property (not a DP).  Assume that the object to receive the
        // value, and the property on which to set it, are on the reader stack.
        //
        private void SetClrComplexProperty(object o)
        {
            Debug.Assert(null != CurrentContext &&
                ReaderFlags.PropertyComplexClr == CurrentContext.ContextType &&
                null != CurrentContext.ExpectedType);

            MemberInfo memberInfo = (MemberInfo)GetCurrentObjectData();
            object parentObject = GetParentObjectData();

            SetClrComplexProperty( parentObject, memberInfo, o );
        }


        //
        // Set a property value that will go onto a CLR property (not a DP).
        //
        private void SetClrComplexProperty(object parentObject, MemberInfo memberInfo, object o)
        {
#if !STRESS
            try
            {
#endif
                SetClrComplexPropertyCore(parentObject, o, memberInfo);
#if !STRESS
            }
            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e) || e is XamlParseException)
                {
                    throw;
                }

                TargetInvocationException tie = e as TargetInvocationException;
                if( tie != null )
                {
                    e = tie.InnerException;
                }

                ThrowExceptionWithLine(SR.Get(SRID.ParserCannotSetValue, parentObject.GetType().FullName, memberInfo.Name, o), e);
            }
#endif

            // The property has been set, so we do not expect any more child tags --
            // anything more under this context will cause an error
            CurrentContext.ExpectedType = null;
        }

        // Called when we are in the context of a constructor and an object end record is
        // encounted.  Add the newly created object to the parameter list.
        void SetConstructorParameter(object o)
        {
            Debug.Assert(null != CurrentContext &&
                ReaderFlags.ConstructorParams == CurrentContext.ContextType);

            // The object may be a MarkupExtension, so get its value if that's the case
            // and use that as the element.
            MarkupExtension me = o as MarkupExtension;
            if (me != null)
            {
                o = ProvideValueFromMarkupExtension(me, null, null);
            }

            if (CurrentContext.ObjectData == null)
            {
                CurrentContext.ObjectData = o;
                CurrentContext.SetFlag(ReaderFlags.SingletonConstructorParam);
            }
            else if (CurrentContext.CheckFlag(ReaderFlags.SingletonConstructorParam))
            {
                ArrayList paramList = new ArrayList(2);
                paramList.Add(CurrentContext.ObjectData);
                paramList.Add(o);
                CurrentContext.ObjectData = paramList;
                CurrentContext.ClearFlag(ReaderFlags.SingletonConstructorParam);
            }
            else
            {
                ArrayList paramList = (ArrayList) CurrentContext.ObjectData;
                paramList.Add(o);
            }
        }

        // Add an XML namespace dictionary entry to the current object in the
        // context stack.
        // NOTE:  Setting Xmlns information on a delay created type is not currently
        //        supported.  The type must have already been instantiated in order to
        //        set the dictionary property on it.  
        protected void SetXmlnsOnCurrentObject(BamlXmlnsPropertyRecord xmlnsRecord)
        {
            DependencyObject e = CurrentContext.ObjectData as DependencyObject;

            // For cases where we have object factories, we may not have a valid DependencyObject at
            // this point, so don't try to set the xmlns dictionary.
            if (e != null)
            {
                XmlnsDictionary elemDict = XmlAttributeProperties.GetXmlnsDictionary(e);

                if (null != elemDict)
                {
                    elemDict.Unseal();
                    elemDict[xmlnsRecord.Prefix] = xmlnsRecord.XmlNamespace;
                    elemDict.Seal();
                }
                else
                {
                    elemDict = new XmlnsDictionary();
                    elemDict[xmlnsRecord.Prefix] = xmlnsRecord.XmlNamespace;
                    elemDict.Seal();
                    XmlAttributeProperties.SetXmlnsDictionary(e, elemDict);
                }
            }
        }

        // Get a property value object given the property's type, name and value string.
        // Throw an exception of the property could not be resolved.
        internal object ParseProperty(
            object    element,
            Type      propertyType,
            string    propertyName,
            object    dpOrPi,
            string    attribValue,
            short     converterTypeId)
        {
            Object propValue = null;
#if !STRESS
            try
            {
#endif
                propValue = XamlTypeMapper.ParseProperty(element, propertyType, propertyName, dpOrPi,
                                                 TypeConvertContext, ParserContext,
                                                 attribValue, converterTypeId);

                FreezeIfRequired(propValue);
#if !STRESS
            }
            catch (Exception e)
            {
                if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                ThrowPropertyParseError(e, propertyName, attribValue, element, propertyType);
            }
#endif

            if (DependencyProperty.UnsetValue == propValue)
            {
                ThrowException(SRID.ParserNullReturned, propertyName, attribValue);
            }

            return propValue;
        }

        private void ThrowPropertyParseError(
            Exception  e,
            string     propertyName,
            string     attribValue,
            object     element,
            Type       propertyType)

        {
            // Property parses can fail if the user mistakenly specified a resource key
            // directly instead of a StaticResource extension.  Check to see if the
            // attribute value is a resource key.
            string message = string.Empty;
            if (FindResourceInParserStack(attribValue.Trim(), false /*allowDeferredResourceReference*/, false /*mustReturnDeferredResourceReference*/) == DependencyProperty.UnsetValue)
            {
                if (propertyType == typeof(Type))
                {
                    message = SR.Get(SRID.ParserErrorParsingAttribType,
                                     propertyName, attribValue);
                }
                else
                {
                    message = SR.Get(SRID.ParserErrorParsingAttrib,
                                     propertyName, attribValue, propertyType.Name);
                }
            }
            else
            {
                message = SR.Get(SRID.ParserErrorParsingAttribType,
                                 propertyName, attribValue);
            }


            ThrowExceptionWithLine( message, e );
        }

        // Name is self-explanatory -- this is used to create CLR objects and such that aren't
        // underneath Elements (those are handled by ParseProperty above).
        object GetObjectFromString(Type type, string s, short converterTypeId)
        {
            object o = DependencyProperty.UnsetValue;
            o = ParserContext.XamlTypeMapper.ParseProperty(null, type,string.Empty, null,
                                       TypeConvertContext,ParserContext,s, converterTypeId);
            return o;
        }

        private static object Lookup(
            IDictionary     dictionary,
            object          key,
            bool            allowDeferredResourceReference,
            bool            mustReturnDeferredResourceReference)
        {
            ResourceDictionary resourceDictionary;
            if (allowDeferredResourceReference && (resourceDictionary = dictionary as ResourceDictionary) != null)
            {
                // Attempt to delay load resources from ResourceDictionaries
                bool canCache;
                return resourceDictionary.FetchResource(key, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }
            else
            {
                if (!mustReturnDeferredResourceReference)
                {
                    return dictionary[key];
                }
                else
                {
                    return new DeferredResourceReferenceHolder(key, dictionary[key]);
                }
            }
        }

        // Given a key, find object in the parser stack that may hold a
        // ResourceDictionary and search for an object keyed by that key.
        internal object FindResourceInParserStack(
            object  resourceNameObject,
            bool    allowDeferredResourceReference,
            bool    mustReturnDeferredResourceReference)
        {
            object value = DependencyProperty.UnsetValue;

            // Walk up the parser stack, looking for DictionaryHolders and DependencyObjects.
            // Check each one for the resource we are searching for until we reach a DependencyObject
            // which is actually in the element tree.  Stop at this one.

            ParserStack contextStack = ReaderContextStack;
            BamlRecordReader reader = this;

            while( contextStack != null )
            {
                for (int i = contextStack.Count-1; i >= 0; i--)
                {
                    ReaderContextStackData stackData = (ReaderContextStackData) contextStack[i];
                    IDictionary dictionary = GetDictionaryFromContext(stackData, false /*toInsert*/);

                    if (dictionary != null && dictionary.Contains(resourceNameObject))
                    {
                        value = Lookup(dictionary, resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
                    }
                    else if (stackData.ContextType == ReaderFlags.DependencyObject)
                    {
                        DependencyObject feOrfceParent = (DependencyObject)stackData.ObjectData;
                        FrameworkElement feParent;
                        FrameworkContentElement fceParent;

                        Helper.DowncastToFEorFCE(feOrfceParent, out feParent, out fceParent, false /*throwIfNeither*/);
                        if (feParent != null)
                        {
                            value = feParent.FindResourceOnSelf(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
                        }
                        else if (fceParent != null)
                        {
                            value = fceParent.FindResourceOnSelf(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
                        }
                    }
                    else if (stackData.CheckFlag(ReaderFlags.StyleObject))
                    {
                        Style style = (Style)stackData.ObjectData;

                        value = style.FindResource(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
                    }
                    else if (stackData.CheckFlag(ReaderFlags.FrameworkTemplateObject))
                    {
                        FrameworkTemplate frameworkTemplate = (FrameworkTemplate)stackData.ObjectData;

                        value = frameworkTemplate.FindResource(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
                    }

                    if (value != DependencyProperty.UnsetValue)
                    {
                        return value;
                    }
                }


                // If we reach the end of this reader's context stack, see if we should
                // look at another one.

                bool newContextStackFound = false;
                while (reader._previousBamlRecordReader != null)
                {
                    // Yes, move to the next reader
                    reader = reader._previousBamlRecordReader;

                    if (reader.ReaderContextStack != contextStack)
                    {
                        contextStack = reader.ReaderContextStack;
                        newContextStackFound = true;
                        break;
                    }
                }

                if( !newContextStackFound )
                {
                    // Terminate the loop
                    contextStack = null;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Helper function for loading a Resources from RootElement/AppResources/SystemResources.
        /// The passed in object must be a trimmed string or a type.
        /// </summary>
        /// <returns>
        ///  The resource value, if found.  Otherwise DependencyProperty.UnsetValue.
        /// </returns>
        private object FindResourceInRootOrAppOrTheme(
            object  resourceNameObject,
            bool    allowDeferredResourceReference,
            bool    mustReturnDeferredResourceReference)
        {
            // This method should not exist since resource references should be references
            // and not looked up at this point.
            // That not being the case, we need to look at SystemResources and App Resources
            // even when we don't have a reference to an element or tree.
            // System resources lucked out, though...
            object result;
            if (!SystemResources.IsSystemResourcesParsing)
            {
                object source;
                result = FrameworkElement.FindResourceFromAppOrSystem(resourceNameObject, out source, false /*throwOnError*/, allowDeferredResourceReference, mustReturnDeferredResourceReference);
            }
            else
            {
                result = SystemResources.FindResourceInternal(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
            }

            if (result != null)
            {
                return result;
            }

            return DependencyProperty.UnsetValue;
        }

        // Given a key, find object in the parser stack that may hold a
        // ResourceDictionary and search for an object keyed by that key.
        internal object FindResourceInParentChain(
            object  resourceNameObject,
            bool    allowDeferredResourceReference,
            bool    mustReturnDeferredResourceReference)
        {
            // Try the parser stack first.
            object resource = FindResourceInParserStack(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);

            if (resource == DependencyProperty.UnsetValue)
            {
                // If we get to here, we've either walked off the top of the reader stack, or haven't
                // found a value on an element that is in the tree.  In that case, give the RootElement
                // and the parent held in the parser context one last try.  This would occur if the
                // parser is used for literal content that did not contain a DependencyObject tree.
                resource = FindResourceInRootOrAppOrTheme(resourceNameObject, allowDeferredResourceReference, mustReturnDeferredResourceReference);
            }

            if (resource == DependencyProperty.UnsetValue && mustReturnDeferredResourceReference)
            {
                resource = new DeferredResourceReferenceHolder(resourceNameObject, DependencyProperty.UnsetValue);
            }

            return resource;
        }

        // Given a string key, find objects in the parser stack that may hold a
        // ResourceDictionary and call the XamlTypeMapper to try and resolve the key using
        // those objects.
        internal object LoadResource(string resourceNameString)
        {
            string  resourceName = resourceNameString.Substring(1, resourceNameString.Length-2);
            object  resourceNameObject = XamlTypeMapper.GetDictionaryKey(resourceName, ParserContext);
            if (resourceNameObject == null)
            {
               ThrowException(SRID.ParserNoResource, resourceNameString);
            }

            object value = FindResourceInParentChain(resourceNameObject, false /*allowDeferredResourceReference*/, false /*mustReturnDeferredResourceReference*/);
            if (value == DependencyProperty.UnsetValue)
            {
                ThrowException(SRID.ParserNoResource , "{" + resourceNameObject.ToString() + "}");
            }

            return value;
        }

        private object GetObjectDataFromContext(ReaderContextStackData context)
        {
            if (null == context.ObjectData &&
                null != context.ExpectedType)
            {
                Debug.Assert(!context.CreateUsingTypeConverter,
                    "We had expected to use a TypeConverter for this " + context.ExpectedType.FullName +
                    " but somebody is trying to create one now without using a TypeConverter.  If TypeConverter is the correct way, fix the code calling this method.  If not, fix the 'should we use a TypeConverter?' logic in XamlReaderHelper.");

                context.ObjectData = CreateInstanceFromType(context.ExpectedType,
                                                 context.ExpectedTypeId, true);
                if (null == context.ObjectData)
                {
                    ThrowException(SRID.ParserCantCreateInstanceType, context.ExpectedType.FullName);
                }

                // Finish set up of the newly created element.
                context.ExpectedType = null; // Don't want to receive any other values
                ElementInitialize(context.ObjectData, null /*unknown name*/);
            }
            return context.ObjectData;
        }

        // A helper function to ensure that the current Element/ClrObject is created
        // before we add properties to it. This is called in situations where we have
        // given up hope of creating the object from text, and just need to create it
        // from a default constructor (if possible) in order to set one of its
        // attributes.
        internal object GetCurrentObjectData()
        {
            return GetObjectDataFromContext(CurrentContext);
        }

        // A helper function to ensure that the parent Element/ClrObject is created
        // before we add properties to it. This is called in situations where we have
        // given up hope of creating the object from text, and just need to create it
        // from a default constructor (if possible) in order to set one of its
        // attributes.
        protected object GetParentObjectData()
        {
            return GetObjectDataFromContext(ParentContext);
        }

        // Push context data onto the reader context stack
        internal void PushContext(
            ReaderFlags contextFlags,
            object      contextData,
            Type        expectedType,
            short       expectedTypeId)
        {
            PushContext( contextFlags, contextData, expectedType, expectedTypeId, false );
        }

        // Push context data onto the reader context stack
        internal void PushContext(
            ReaderFlags contextFlags,
            object      contextData,
            Type        expectedType,
            short       expectedTypeId,
            bool        createUsingTypeConverter)
        {
            ReaderContextStackData d;

            lock(_stackDataFactoryCache)
            {
                if (_stackDataFactoryCache.Count == 0)
                {
                    d = new ReaderContextStackData();
                }
                else
                {
                    // Get StackData from the factory cache
                    d = _stackDataFactoryCache[_stackDataFactoryCache.Count-1];
                    _stackDataFactoryCache.RemoveAt(_stackDataFactoryCache.Count-1);
                }
            }

            d.ContextFlags = contextFlags;
            d.ObjectData = contextData;
            d.ExpectedType = expectedType;
            d.ExpectedTypeId = expectedTypeId;
            d.CreateUsingTypeConverter = createUsingTypeConverter;
            ReaderContextStack.Push(d);
            ParserContext.PushScope();

            // If the context data is an object that implements INameScope, make it
            // the new Name scope for registering Names

            INameScope nameScope = NameScope.NameScopeFromObject(contextData);

            if (nameScope != null)
            {
                ParserContext.NameScopeStack.Push(nameScope);
            }
        }

        // Pos the reader context stack.
        internal void PopContext()
        {
            ReaderContextStackData stackData = (ReaderContextStackData) ReaderContextStack.Pop();

            // If we're through with an Name scoping point, take it off the stack.
            INameScope nameScope = NameScope.NameScopeFromObject(stackData.ObjectData);

            if (nameScope != null)
            {
                ParserContext.NameScopeStack.Pop();
            }

            ParserContext.PopScope();

            // Clear the stack data and then add it to the factory cache for reuse
            stackData.ClearData();

            lock(_stackDataFactoryCache)
            {
                _stackDataFactoryCache.Add(stackData);
            }
        }

        // Get BaseUri for the right elements.
        Uri GetBaseUri( )
        {
            Uri baseuri = ParserContext.BaseUri;

            if (baseuri == null)
            {
                baseuri = BindUriHelper.BaseUri;
            }
            else if (baseuri.IsAbsoluteUri == false)
            {
                baseuri = new Uri(BindUriHelper.BaseUri, baseuri);
            }

            return baseuri;
        }


        // Call various interfaces on the passed element during initialization or
        // creation of the element.  This is usually called immediately after
        // creating the object.
        // The bamlElementStartRecord may be null.   If it isn't, it represents the
        // record that caused this object to be created.

        private bool ElementInitialize(object element, string name)
        {
            bool result = false; // returns true if need to do EndInit()

            // Tell the Element Initialization has begun and hence postpone the Initialized event
            // If the element implements ISupportInitialize, call it.
            ISupportInitialize supportInitializeElement = element as ISupportInitialize;
            if (supportInitializeElement != null)
            {
                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Start,
                                     TraceMarkup.BeginInit,
                                     supportInitializeElement );
                }

                supportInitializeElement.BeginInit();
                result = true;

                if( TraceMarkup.IsEnabled )
                {
                    TraceMarkup.Trace( TraceEventType.Stop,
                                     TraceMarkup.BeginInit,
                                     supportInitializeElement );
                }
            }

            // If this is an element start record that carries its name also, then register the name now.

            if (name != null)
            {
                DoRegisterName(name, element);
            }


            // Tell element its base uri
            IUriContext uriContext = element as IUriContext;
            if (uriContext != null)
            {
                uriContext.BaseUri = GetBaseUri();
            }
            else
            {
                // Set the ApplicationMarkupBaseUri if this is for AppDef baml stream.
                if (element is Application)
                {
                    ((Application)element).ApplicationMarkupBaseUri = GetBaseUri();
                }
            }

            UIElement uiElement = element as UIElement;
            if (uiElement != null)
            {
                uiElement.SetPersistId(++_persistId);
            }

            // The second consition is to handle events within standalone dictionaries.
            // We need to setup the component connector correctly in this case. Note
            // that the standalone dictionary is preceded by a DictionaryHolder.
            if (CurrentContext == null)
            {
                IComponentConnector icc = null;
                if (_componentConnector == null) // why was this necessary?
                {
                    _componentConnector = icc = element as IComponentConnector;
                    if (_componentConnector != null)
                    {
                        if (ParserContext.RootElement == null)
                        {
                            ParserContext.RootElement = element;
                        }

                        // Connect the root element.
                        _componentConnector.Connect(0, element);
                    }
                }

                _rootElement = element;

                DependencyObject doRoot = element as DependencyObject;
                if (!(element is INameScope)
                    && ParserContext.NameScopeStack.Count == 0
                    && (doRoot != null))
                {
                    NameScope namescope = null;
                    // if the root element is markup sub-classed and already is a namescope, use it.
                    if (icc != null)
                    {
                        namescope = NameScope.GetNameScope(doRoot) as NameScope;
                    }
                    if (namescope == null)
                    {
                        namescope = new NameScope();
                        NameScope.SetNameScope(doRoot, namescope);
                    }
                }

                if (doRoot != null)
                {
                    Uri baseuri = GetBaseUri();
                    SetDependencyValue(doRoot, BaseUriHelper.BaseUriProperty, baseuri);
                }
            }

            return result;
        }

        private void ElementEndInit(ref object element)
        {
            try
            {
                // Tell element initialization is complete since there are no more children.
                // If the element implements ISupportInitialize, call it.
                ISupportInitialize supportInitializeElement = element as ISupportInitialize;
                if (supportInitializeElement != null)
                {
                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.Trace( TraceEventType.Start,
                                         TraceMarkup.EndInit,
                                         supportInitializeElement );
                    }

                    supportInitializeElement.EndInit();

                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.Trace( TraceEventType.Stop,
                                         TraceMarkup.EndInit,
                                         supportInitializeElement );
                    }
                }
            }
            catch( Exception e )
            {
                if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                // Check if the ParentContext represents a property

                ReaderContextStackData parentContext = ParentContext;
                ReaderFlags parentContextType = parentContext != null ? parentContext.ContextType : ReaderFlags.Unknown;

                if (parentContextType == ReaderFlags.PropertyComplexClr ||
                    parentContextType == ReaderFlags.PropertyComplexDP ||
                    parentContextType == ReaderFlags.PropertyIList ||
                    parentContextType == ReaderFlags.PropertyIDictionary ||
                    parentContextType == ReaderFlags.PropertyArray ||
                    parentContextType == ReaderFlags.PropertyIAddChild)
                {
                    // Check if the GrandParent object implements IProvidePropertyFallback

                    IProvidePropertyFallback iProvidePropertyFallback = GrandParentObjectData as IProvidePropertyFallback;
                    if (iProvidePropertyFallback != null)
                    {
                        // Find the property name from the ParentContext

                        string propertyName = parentContext.ElementNameOrPropertyName;

                        // Check if the GrandParent object can provide a property fallback value

                        if (iProvidePropertyFallback.CanProvidePropertyFallback(propertyName))
                        {
                            element = iProvidePropertyFallback.ProvidePropertyFallback(propertyName, e);

                            // Place the element back into the context
                            CurrentContext.ObjectData = element;

                            return;
                        }
                    }
                }

                ThrowExceptionWithLine( SR.Get(SRID.ParserFailedEndInit), e );
            }
        }

        // Checks if the last element on the stack needs to be added to the Tree and
        // if it does adds it. This is done if a CLR or Dependency object is on the
        // stack and the parent object implements IAddChild or IList, and the parent
        // context is an object context.
        // Set AddedToTree context flag if an object was added to its parent via IAddChild or IList.
        private void SetPropertyValueToParent(bool fromStartTag)
        {
            bool isMarkupExtension;
            SetPropertyValueToParent(fromStartTag, out isMarkupExtension);
        }

        private void SetPropertyValueToParent(bool fromStartTag, out bool isMarkupExtension)
        {
            isMarkupExtension = false;
            object traceCurrentObject = null;


            ReaderContextStackData currentContext = CurrentContext;
            ReaderContextStackData parentContext = ParentContext;

            if (currentContext == null ||
                !currentContext.NeedToAddToTree ||
                (ReaderFlags.DependencyObject != currentContext.ContextType &&
                 ReaderFlags.ClrObject != currentContext.ContextType))
            {
                return;
            }

            object currentObject = null;

#if !STRESS
            try
            {
#endif
                currentObject = GetCurrentObjectData();

                // Call FreezeIfRequired to ensure objects are Frozen before they are added to the tree
                //
                // To avoid propagating changed notifiers, make sure to Freeze objects before adding
                // them.  When called from ReadElementEndRecord, this will actually cause the object
                // to be Frozen twice (once now, and once during ReadElementEndRecord).  The second
                // call to Freeze has no affect since the object's already frozen.
                //
                // The second call to Freeze could be avoided by checking the IsFrozen flag in
                // FreezeIfRequired, or by checking for ReaderFlags.AddedToTree after this call is
                // finished.  However, this code-path isn't used for the common Freezable
                // case (Property syntax), so we're choosing to call Freeze twice in the uncommon case
                // instead of either of those checks in the common case (i.e., every element).

                FreezeIfRequired(currentObject);


                // If we don't have a parent, this is the root.

                if (null == parentContext)
                {
                    if (RootList.Count == 0)
                    {
                        RootList.Add(currentObject);
                    }

                    currentContext.MarkAddedToTree();
                    return;
                }

                // If we're under a collection-typed property, this object may be the collection itself,
                // or may be an item in the collection.  Check that here.

                if (CheckExplicitCollectionTag(ref isMarkupExtension))
                {
                    // This object was the collection itself, not just an item, so we're done.
                    currentContext.MarkAddedToTree();
                    return;
                }

                // Otherwise, get the parent.  The rest of the routine we decide how to add to it.

                object parent = GetParentObjectData();

                // Handle parent as a dictionary:

                IDictionary dictionary = GetDictionaryFromContext(parentContext, true /*toInsert*/);
                if (dictionary != null)
                {
                    // if this SetPropertyValueToParent call occurred during ReadElementStart, then we
                    // don't have a key for this element yet, and should wait until ReadElementEnd
                    if (!fromStartTag)
                    {
                        currentObject = GetElementValue(currentObject, dictionary, null /*contentProperty*/, ref isMarkupExtension);

                        if (currentContext.Key == null)
                        {
                            // throw an exception if this element does not have a key
                            ThrowException(SRID.ParserNoDictionaryKey);
                        }

                        dictionary.Add(currentContext.Key, currentObject);

                        currentContext.MarkAddedToTree();
                    }

                    return;
                }

                // Handle parent as an IList:

                IList list = GetListFromContext(parentContext);
                if (list != null)
                {
                    currentObject = GetElementValue(currentObject, list, null /*contentProperty*/, ref isMarkupExtension);

                    list.Add(currentObject);

                    currentContext.MarkAddedToTree();
                    return;
                }

                // Handle parent as an array:

                ArrayExtension arrayExt = GetArrayExtensionFromContext(parentContext);
                if (arrayExt != null)
                {
                    currentObject = GetElementValue(currentObject, arrayExt, null /*contentProperty*/, ref isMarkupExtension);

                    arrayExt.AddChild(currentObject);

                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.Trace( TraceEventType.Stop,
                                         TraceMarkup.AddValueToArray,
                                         traceCurrentObject,
                                         parentContext.ElementNameOrPropertyName,
                                         currentObject );
                    }

                    currentContext.MarkAddedToTree();
                    return;
                }

                // Handle parent as IAddChild:

                IAddChild iac = GetIAddChildFromContext(parentContext);
                if (iac != null)
                {
                    currentObject = GetElementValue(currentObject, iac, null /*contentProperty*/, ref isMarkupExtension);

                    // The object may have changed to a string if it was a MarkupExtension
                    // that returned a string from ProvideValue, so check for this and
                    // call the appropriate IAddChild method.
                    string text = currentObject as string;

                    if (text != null)
                    {
                        iac.AddText(text);
                    }
                    else
                    {
                        iac.AddChild(currentObject);
                    }

                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.Trace( TraceEventType.Stop,
                                         TraceMarkup.AddValueToAddChild,
                                         traceCurrentObject,
                                         currentObject );
                    }

                    currentContext.MarkAddedToTree();
                    return;
                }

                // Handle parent as CPA:

                object contentProperty = parentContext.ContentProperty;
                if (contentProperty != null)
                {
                    currentObject = GetElementValue(currentObject, parentContext.ObjectData, contentProperty, ref isMarkupExtension);
                    AddToContentProperty(parent, contentProperty, currentObject); // Traces to TraceXamnl

                    currentContext.MarkAddedToTree();
                    return;
                }

                // Handle parent as a singleton property:

                if( parentContext.ContextType == ReaderFlags.PropertyComplexClr )
                {
                    object parentObject = GetObjectDataFromContext(GrandParentContext);
                    MemberInfo memberInfo = (MemberInfo)GetParentObjectData();

                    SetClrComplexProperty(parentObject, memberInfo, currentObject);// Traces to TraceXamnl
                    currentContext.MarkAddedToTree();
                    return;
                }

                if( parentContext.ContextType == ReaderFlags.PropertyComplexDP )
                {
                    object parentObject = GetObjectDataFromContext(GrandParentContext);
                    BamlAttributeInfoRecord attribInfo = (BamlAttributeInfoRecord)GetParentObjectData();

                    SetDependencyComplexProperty(parentObject, attribInfo, currentObject);// Traces to TraceXamnl
                    currentContext.MarkAddedToTree();
                    return;
                }

                // If we get here there was no way to set the value to the parent, error:

                Type parentType = GetParentType();
                string typeName = parentType == null ? String.Empty : parentType.FullName;

                if( currentObject == null )
                    ThrowException( SRID.ParserCannotAddAnyChildren, typeName );
                else
                    ThrowException( SRID.ParserCannotAddAnyChildren2, typeName, currentObject.GetType().FullName );

#if !STRESS
            }

            catch( Exception e )
            {
                if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                Type parentType = GetParentType();
                string typeName = parentType == null ? String.Empty : parentType.FullName;

                if( currentObject == null )
                    ThrowException( SRID.ParserCannotAddAnyChildren, typeName );
                else
                    ThrowException( SRID.ParserCannotAddAnyChildren2, typeName, currentObject.GetType().FullName );
            }
#endif

        }


        // Determine the parent type from the context stack data.

        private Type GetParentType()
        {
            ReaderContextStackData parentContext = ParentContext;
            object parent = GetParentObjectData();

            if (parentContext.CheckFlag(ReaderFlags.CollectionHolder))
            {
                parent = ((BamlCollectionHolder)parent).Collection;
            }

            if (parent != null)
            {
                return parent.GetType();
            }
            else if (parentContext.ExpectedType != null)
            {
                return parentContext.ExpectedType;
            }

            return null;
        }

        private object GetElementValue(object element, object parent, object contentProperty, ref bool isMarkupExtension)
        {
            // The element may be a MarkupExtension, so get its value if that's the case
            // and use that as the element.
            MarkupExtension me = element as MarkupExtension;
            if (me != null)
            {
                isMarkupExtension = true;
                element = ProvideValueFromMarkupExtension(me, parent, contentProperty);
                CurrentContext.ObjectData = element;
            }

            return element;
        }

        // this method checks the current context to see if it is the context of a property collection
        // and sets that property's value to the current element if the type is assignable to the type
        // of the property.  If not then, it returns false and the element should be added to the tree.
        private bool CheckExplicitCollectionTag(ref bool isMarkupExtension)
        {
            bool result = false;
            ReaderContextStackData parentContext = ParentContext;

            // if the parent context is a BamlCollectionHolder, and has not been set, then continue check
            if (parentContext != null &&
                parentContext.CheckFlag(ReaderFlags.CollectionHolder) &&
                parentContext.ExpectedType != null)
            {
                BamlCollectionHolder holder = (BamlCollectionHolder)parentContext.ObjectData;

                if (!holder.IsClosed && !holder.ReadOnly)
                {
                    ReaderContextStackData currentContext = CurrentContext;
                    object element = currentContext.ObjectData;
                    Type elementType;

                    if (currentContext.CheckFlag(ReaderFlags.ArrayExt))
                    {
                        // arrays are a little different because we have to get the type of the
                        // elements to be stored in the array and make an array type from it
                        elementType = ((ArrayExtension)element).Type.MakeArrayType();

                        // in case of an explicit tag, we don't resolve this markup extension until ReadPropertyArrayEndRecord
                        isMarkupExtension = false;
                    }
                    else
                    {
                        // the parent is a BamlCollectionHolder representing the property which may
                        // represent the explicit collection and the grandparent is that property's target.
                        element = GetElementValue(element, GrandParentObjectData,
                                                  holder.PropertyDefinition.DependencyProperty, ref isMarkupExtension);
                        elementType = element == null ? null : element.GetType();
                    }

                    // the element is an explicit collection if it is assignable to the expected type of the parent or
                    // it is a MarkupExtension, which means we don't know what it's type will be.
                    if (isMarkupExtension || parentContext.ExpectedType.IsAssignableFrom(elementType))
                    {
                        // if the the property's expected type matches the element type, assign the
                        // collection holder's collection to be this element
                        holder.Collection = element;
                        holder.IsClosed = true;
                        parentContext.ExpectedType = null;
                        result = true;
                    }
                }
            }

            return result;
        }

        private void AddToContentProperty(object container, object contentProperty, object value)
        {
            Debug.Assert(contentProperty != null);
            IList contentList = contentProperty as IList;
            object traceCurrentObject = null;

#if !STRESS
            try
            {
#endif

                // Adding to a collection?

                if (contentList != null)
                {
                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.Trace( TraceEventType.Start,
                                         TraceMarkup.AddValueToList,
                                         traceCurrentObject,
                                         String.Empty,
                                         value);
                    }

                    contentList.Add(value);

                    if( TraceMarkup.IsEnabled )
                    {
                        TraceMarkup.Trace( TraceEventType.Stop,
                                         TraceMarkup.AddValueToList,
                                         traceCurrentObject,
                                         String.Empty,
                                         value);
                    }
                }
                else
                {
                    // Setting to a DP?

                    DependencyProperty dp = contentProperty as DependencyProperty;
                    if (dp != null)
                    {
                        DependencyObject dpo = container as DependencyObject;
                        if (dpo == null)
                        {
                            // ?? This appears to be old code, we shouldn't ever get into this path.
                            ThrowException(SRID.ParserParentDO, value.ToString());
                        }

                        if( TraceMarkup.IsEnabled )
                        {
                            TraceMarkup.Trace( TraceEventType.Start,
                                             TraceMarkup.SetPropertyValue,
                                             traceCurrentObject,
                                             dp.Name,
                                             value);
                        }

                        SetDependencyValue(dpo, dp, value);

                        if( TraceMarkup.IsEnabled )
                        {
                            TraceMarkup.Trace( TraceEventType.Stop,
                                             TraceMarkup.SetPropertyValue,
                                             traceCurrentObject,
                                             dp.Name,
                                             value);
                        }
                    }
                    else
                    {
                        // Setting to a CLR property?

                        PropertyInfo pi = contentProperty as PropertyInfo;
                        if (pi != null)
                        {
                            if( TraceMarkup.IsEnabled )
                            {
                                TraceMarkup.Trace( TraceEventType.Start,
                                                 TraceMarkup.SetPropertyValue,
                                                 traceCurrentObject,
                                                 pi.Name,
                                                 value);
                            }


                            bool set = XamlTypeMapper.SetInternalPropertyValue(ParserContext,
                                                                               ParserContext.RootElement,
                                                                               pi,
                                                                               container,
                                                                               value);
                            if (!set)
                            {
                                ThrowException(SRID.ParserCantSetContentProperty, pi.Name, pi.ReflectedType.Name);
                            }

                            if( TraceMarkup.IsEnabled )
                            {
                                TraceMarkup.Trace( TraceEventType.Stop,
                                                 TraceMarkup.SetPropertyValue,
                                                 traceCurrentObject,
                                                 pi.Name,
                                                 value);
                            }
                        }
                        else
                        {
                            Debug.Assert(false, "The only remaining option is attached property, which is not allowed in xaml for content properties");
                        }
                    }
                }
#if !STRESS
            }
            catch (Exception e)
            {
                if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                ThrowExceptionWithLine(
                    SR.Get(SRID.ParserCannotAddChild,
                           value.GetType().Name,
                           container.GetType().Name),
                        e);
            }
#endif

            if( TraceMarkup.IsEnabled )
            {
                TraceMarkup.Trace( TraceEventType.Stop,
                                 TraceMarkup.SetCPA,
                                 traceCurrentObject,
                                 value);
            }
        }

        // Given a id looks up in the Map table to find the
        // associated attributeinfo for clr properties and returns the property name
        internal string GetPropertyNameFromAttributeId(short id)
        {
            if (null != MapTable)
            {
                return MapTable.GetAttributeNameFromId(id);
            }

            return null;
        }

        // Given an ID looks up in the Map table to find the
        // associated attributeinfo for clr properties and returns the property name
        internal string GetPropertyValueFromStringId(short id)
        {
            string propertyValue = null;

            if (null != MapTable)
            {
                propertyValue = MapTable.GetStringFromStringId(id);
            }

            return propertyValue;
        }

        // Create a serializer, given a type info record with serializer information.
        private XamlSerializer CreateSerializer(
            BamlTypeInfoWithSerializerRecord typeWithSerializerInfo)
        {
            // ID less than 0 means a known serializer in PresentationFramework.
            if (typeWithSerializerInfo.SerializerTypeId < 0)
            {
                return (XamlSerializer)MapTable.CreateKnownTypeFromId(
                                    typeWithSerializerInfo.SerializerTypeId);
            }
            else
            {
                // If the serializer type hasn't been determined yet, do it now.
                if (typeWithSerializerInfo.SerializerType == null)
                {
                    typeWithSerializerInfo.SerializerType = MapTable.GetTypeFromId(
                                             typeWithSerializerInfo.SerializerTypeId);
                }
                return (XamlSerializer)CreateInstanceFromType(
                             typeWithSerializerInfo.SerializerType,
                             typeWithSerializerInfo.SerializerTypeId,
                             false);
            }
        }

        internal object GetREOrEiFromAttributeId(
                short id,
            out bool isInternal,
            out bool isRE)
        {
            object info = null;
            isRE = true;
            isInternal = false;
            BamlAttributeInfoRecord attribInfo = null;

            if (null != MapTable)
            {
                attribInfo = MapTable.GetAttributeInfoFromId(id);
                if (null != attribInfo)
                {
                    info = attribInfo.Event;
                    if (info == null)
                    {
                        info = attribInfo.EventInfo;
                        if (info == null)
                        {
                            attribInfo.Event = MapTable.GetRoutedEvent(attribInfo);
                            info = attribInfo.Event;
                            if (info == null)
                            {
                                Object currentParent = GetCurrentObjectData();
                                Type   currentParentType;
                                currentParentType = currentParent.GetType();

                                if (ReflectionHelper.IsPublicType(currentParentType))
                                {
                                    attribInfo.EventInfo = ParserContext.XamlTypeMapper.GetClrEventInfo(currentParentType, attribInfo.Name);
                                }

                                if (attribInfo.EventInfo == null)
                                {
                                    attribInfo.EventInfo = currentParentType.GetEvent(attribInfo.Name,
                                                                BindingFlags.Instance |
                                                                BindingFlags.Public |
                                                                BindingFlags.NonPublic);

                                    if (attribInfo.EventInfo != null)
                                    {
                                        attribInfo.IsInternal = true;
                                    }
                                }

                                info = attribInfo.EventInfo;
                                isRE = false;
                            }
                        }
                        else
                        {
                            isRE = false;
                        }
                    }
                }
            }

            if (attribInfo != null)
            {
                isInternal = attribInfo.IsInternal;
            }
            return info;
        }

        // Helper method to get the OwnerType.PropertyName string from a
        // attribute info record or PropertyInfo.  This is used
        // primarily for error reporting
        private string GetPropNameFrom(object PiOrAttribInfo)
        {
            BamlAttributeInfoRecord attribInfo = PiOrAttribInfo as BamlAttributeInfoRecord;
            if (attribInfo != null)
            {
                return attribInfo.OwnerType.Name + "." + attribInfo.Name;
            }
            else
            {
                PropertyInfo pi = PiOrAttribInfo as PropertyInfo;
                if (pi != null)
                {
                    return pi.DeclaringType.Name + "." + pi.Name;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        //
        // ThrowException wrappers for 0-3 parameter SRIDs
        //

        protected void ThrowException(
             string    id)
        {
            ThrowExceptionWithLine(SR.Get(id), null);
        }

        protected internal void ThrowException(
            string     id,
            string     parameter)
        {
            ThrowExceptionWithLine(SR.Get(id, parameter), null);
        }

        protected void ThrowException(
            string     id,
            string     parameter1,
            string     parameter2)
        {
            ThrowExceptionWithLine(SR.Get(id, parameter1, parameter2), null);
        }

        protected void ThrowException(
            string     id,
            string     parameter1,
            string     parameter2,
            string     parameter3)
        {
            ThrowExceptionWithLine(SR.Get(id, parameter1, parameter2, parameter3), null);
        }

        // Helper to insert line and position numbers into message, if they are present
        internal void ThrowExceptionWithLine(string message, Exception innerException)
        {
            XamlParseException.ThrowException(ParserContext, LineNumber, LinePosition, message, innerException);
        }

        // Helper method to creates an instance of the specified type
        internal object CreateInstanceFromType(
            Type  type,
            short typeId,
            bool  throwOnFail)
        {
            bool publicOnly = true;
            BamlTypeInfoRecord typeInfo = null;
            if (typeId >= 0)
            {
                typeInfo = MapTable.GetTypeInfoFromId(typeId);
                if (typeInfo != null)
                {
                    publicOnly = !typeInfo.IsInternalType;
                }
            }

            if (publicOnly)
            {
                if (!ReflectionHelper.IsPublicType(type))
                {
                    ThrowException(SRID.ParserNotMarkedPublic, type.Name);
                }
            }
            else
            {
                if (!ReflectionHelper.IsInternalType(type))
                {
                    ThrowException(SRID.ParserNotAllowedInternalType, type.Name);
                }
            }

#if !STRESS
            try
            {
#endif
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseRdrCrInFTypBegin);

                object instance = null;
                try
                {
                    if (TraceMarkup.IsEnabled)
                    {
                        TraceMarkup.Trace(TraceEventType.Start,
                                         TraceMarkup.CreateObject,
                                         type);
                    }

                    // String is a very common object that we can try to create, but it will always
                    // fail since it has no default constructor.  Check for this case to avoid throwing
                    // an expensive exception
                    if (type != typeof(String))
                    {
                        if (typeId < 0)
                        {
                            instance = MapTable.CreateKnownTypeFromId(typeId);
                        }
                        else if (publicOnly)
                        {
                            // Don't use the CreateInstance(Type,BindingFlags) overload, as it's a lot more expensive.
                            instance = Activator.CreateInstance(type);
                        }
                        else
                        {
                            instance = XamlTypeMapper.CreateInternalInstance(ParserContext, type);
                            if (instance == null && throwOnFail)
                            {
                                ThrowException(SRID.ParserNotAllowedInternalType, type.Name);
                            }
                        }
                    }

                    if (TraceMarkup.IsEnabled)
                    {
                        TraceMarkup.Trace(TraceEventType.Stop,
                                         TraceMarkup.CreateObject,
                                         type,
                                         instance);
                    }
                }
                finally
                {
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXamlBaml, EventTrace.Level.Verbose, EventTrace.Event.WClientParseRdrCrInFTypEnd);
                }
                return instance;
#if !STRESS
            }

            catch (System.MissingMethodException e)
            {
               if (throwOnFail)
               {
                   // If we are setting a complex property, it may be that we have
                   // inserted an element tag that the user is not aware of, so
                   // give a more detailed error message.  Otherwise just complain
                   // that the type cannot be created.
                   if (ParentContext != null &&
                       ParentContext.ContextType == ReaderFlags.PropertyComplexDP)
                   {
                       BamlAttributeInfoRecord attribInfo = GetParentObjectData() as BamlAttributeInfoRecord;
                       ThrowException(SRID.ParserNoDefaultPropConstructor,
                                      type.Name, attribInfo.DP.Name);
                   }
                   else
                   {
                       ThrowExceptionWithLine(SR.Get(SRID.ParserNoDefaultConstructor, type.Name), e );
                   }
               }

               // No zero parameter constructor.  Return null.
               return null;
            }

            catch (Exception e)
            {
                if( CriticalExceptions.IsCriticalException(e) || e is XamlParseException )
                {
                    throw;
                }

                ThrowExceptionWithLine( SR.Get(SRID.ParserErrorCreatingInstance, type.Name, type.Assembly.FullName), e);
                return null;
            }
#endif
        }

        // Freeze the object if it a Freezeable, and PresentationOptions:Freeze
        // has been specified.  This method is called after the Freezable
        // has been fully created and is ready to be set on it's parent.
        internal void FreezeIfRequired(object element)
        {
            if (_parserContext.FreezeFreezables)
            {
                Freezable f = element as Freezable;
                if (f != null)
                {
                    f.Freeze();
                }
            }
        }
        internal void PreParsedBamlReset()
        {
            PreParsedCurrentRecord = PreParsedRecordsStart;
        }


        //+--------------------------------------------------------------------------------------------------------------
        //
        //  SetPreviousBamlRecordReader
        //
        //  Link this nested BamlRecordReader to one that is higher in the stack.
        //
        //+--------------------------------------------------------------------------------------------------------------

        protected internal void SetPreviousBamlRecordReader( BamlRecordReader previousBamlRecordReader )
        {
            _previousBamlRecordReader = previousBamlRecordReader;
        }

        #endregion Methods

        #region Properties

        // List of preparsed BamlRecords that are used instead of a
        // record stream.  This is used when reading the contents of
        // a resource dictionary.
        internal BamlRecord PreParsedRecordsStart
        {
            get { return _preParsedBamlRecordsStart; }
            set { _preParsedBamlRecordsStart = value; }
        }

        // Index into the list of preparsed records for the next
        // record to be read.
        internal BamlRecord PreParsedCurrentRecord
        {
            get { return _preParsedIndexRecord; }
            set { _preParsedIndexRecord= value; }
        }

        // Stream that contains baml records in binary form.  This is used when
        // reading from a file.
        internal Stream BamlStream
        {
            get { return _bamlStream; }

            set
            {
                _bamlStream =  value;

                // if this is one of our Readers streams
                // setup the XAMLReaderStream property.
                if (_bamlStream is ReaderStream)
                {
                    _xamlReaderStream = (ReaderStream) _bamlStream;
                }
                else
                {
                    _xamlReaderStream = null;
                }

                if (BamlStream != null)
                {
                    _binaryReader = new BamlBinaryReader(BamlStream, new System.Text.UTF8Encoding());
                }
            }
        }

        // review, should be private
        internal BamlBinaryReader BinaryReader
        {
            get { return _binaryReader; }
        }

        internal XamlTypeMapper XamlTypeMapper
        {
            get { return ParserContext.XamlTypeMapper; }
        }

        internal ParserContext ParserContext
        {
            get { return _parserContext; }
            set
            {
                // Ensure that the parser context always has the reader
                // assigned to it.  This is required for parse time resource
                // searches.
                _parserContext = value;

                // Reset the TypeConvertContext cache because it might have
                // cached the old ParserContext that we just changed.
                _typeConvertContext = null;
            }
        }

        internal TypeConvertContext TypeConvertContext
        {
            get
            {
                if (null == _typeConvertContext)
                {
                    _typeConvertContext = new TypeConvertContext(ParserContext);
                }
                return _typeConvertContext;
            }
        }

        // Determines sync and async parsing modes.  Not used directly by the record
        // reader, but is needed when spinning off other deserializers
        internal XamlParseMode XamlParseMode
        {
            get { return _parseMode; }
            set { _parseMode = value; }
        }

        // The maximum number of records to read while in async mode
        internal int MaxAsyncRecords
        {
            get { return _maxAsyncRecords; }
            set { _maxAsyncRecords = value; }
        }

        // Table for mapping types, attributes and assemblies.
        // This should always be a part of the ParserContext
        internal BamlMapTable MapTable
        {
            get { return ParserContext.MapTable; }
        }

        // Maps namespace prefixes (key) to XML namespaces (value)
        internal XmlnsDictionary XmlnsDictionary
        {
            get { return ParserContext.XmlnsDictionary; }
        }

        internal ReaderContextStackData CurrentContext
        {
            get { return (ReaderContextStackData) ReaderContextStack.CurrentContext; }
        }

        internal ReaderContextStackData ParentContext
        {
            get { return (ReaderContextStackData) ReaderContextStack.ParentContext; }
        }

        internal object ParentObjectData
        {
            get
            {
                ReaderContextStackData contextData = ParentContext;
                return contextData == null ? null : contextData.ObjectData;
            }
        }

        internal ReaderContextStackData GrandParentContext
        {
            get { return (ReaderContextStackData) ReaderContextStack.GrandParentContext; }
        }

        internal object GrandParentObjectData
        {
            get
            {
                ReaderContextStackData contextData = GrandParentContext;
                return contextData == null ? null : contextData.ObjectData;
            }
        }

        internal ReaderContextStackData GreatGrandParentContext
        {
            get { return (ReaderContextStackData) ReaderContextStack.GreatGrandParentContext; }
        }

        internal ParserStack ReaderContextStack
        {
            get { return _contextStack; }
        }

        internal BamlRecordManager BamlRecordManager
        {
            get
            {
                if( _bamlRecordManager == null )
                {
                    _bamlRecordManager = new BamlRecordManager();
                }

                return _bamlRecordManager;
            }
        }

        internal bool EndOfDocument
        {
            get { return _endOfDocument;}
            set { _endOfDocument = value; }
        }

        /// <summary>
        /// Element that is the Root of this document. Can be null
        /// if current Root is not an Element
        /// </summary>
        internal object RootElement
        {
            get { return _rootElement; }
            set { _rootElement = value; }
        }

        internal IComponentConnector ComponentConnector
        {
            get { return _componentConnector; }
            set { _componentConnector = value; }
        }

        ReaderStream XamlReaderStream
        {
            get { return _xamlReaderStream; }
        }

        // The stack of context information accumulated during reading.
        internal ParserStack ContextStack
        {
            get { return _contextStack; }
            set { _contextStack = value; }
        }

        internal int LineNumber
        {
            get { return ParserContext.LineNumber; }
            set { ParserContext.LineNumber = value; }
        }

        internal int LinePosition
        {
            get { return ParserContext.LinePosition; }
            set { ParserContext.LinePosition = value; }
        }

        internal bool IsDebugBamlStream
        {
            get { return ParserContext.IsDebugBamlStream; }
            set { ParserContext.IsDebugBamlStream = value; }
        }

        internal Int64 StreamPosition
        {
            get { return _bamlStream.Position; }
        }

        Int64 StreamLength
        {
            get { return _bamlStream.Length; }
        }

        internal bool IsRootAlreadyLoaded
        {
            get { return _isRootAlreadyLoaded; }
            set { _isRootAlreadyLoaded = value; }
        }

        // The PreviousBamlRecordReader is set when this BRR is nested inside
        // another.

        internal BamlRecordReader PreviousBamlRecordReader
        {
            get { return _previousBamlRecordReader; }
        }

#endregion Properties

#region Data

        // state vars
        IComponentConnector          _componentConnector;
        object                       _rootElement;
        bool                         _bamlAsForest;
        bool                         _isRootAlreadyLoaded;
        ArrayList                    _rootList;
        ParserContext                _parserContext;   // XamlTypeMapper, namespace state, lang/space values
        TypeConvertContext           _typeConvertContext;
        int                          _persistId;
        ParserStack                  _contextStack = new ParserStack();
        XamlParseMode                _parseMode = XamlParseMode.Synchronous;
        int                          _maxAsyncRecords;
        // end of state vars

        Stream                       _bamlStream;
        ReaderStream                 _xamlReaderStream;
        BamlBinaryReader             _binaryReader;
        BamlRecordManager            _bamlRecordManager;
        BamlRecord                   _preParsedBamlRecordsStart = null;
        BamlRecord                   _preParsedIndexRecord = null;
        bool                         _endOfDocument = false;
        bool                         _buildTopDown = true;

        // The outer BRR, when this one is nested.
        BamlRecordReader             _previousBamlRecordReader;

        static List<ReaderContextStackData> _stackDataFactoryCache = new List<ReaderContextStackData>();

#endregion Data
    }

    /// <summary>
    /// This class is used in the resolution of a StaticResourceId. It is used to cache the
    /// prefetched value during that processing. We have sub-classed StaticResourceExtension
    /// so that we do not need to take the cost of increasing the size of a StaticResourceExtension
    /// by 4 bytes.
    /// </summary>
    internal class StaticResourceHolder : StaticResourceExtension
    {
        #region Constructors

        internal StaticResourceHolder(object resourceKey, DeferredResourceReference prefetchedValue) : base(resourceKey)
        {
            _prefetchedValue = prefetchedValue;
        }

        #endregion Constructors

        #region Methods

        internal override DeferredResourceReference PrefetchedValue
        {
            get { return _prefetchedValue; }
        }

        #endregion Methods

        #region Data

        private DeferredResourceReference _prefetchedValue;

        #endregion Data
    }

    // This structure is usedas a generilized property descriptor.
    // It can have three possible states - DependencyProperty, PropertyInfo, AttachedPropertyGetter/Setter.
    // PropertyInfo is used for CLR properties, AttachedPropertySetter is used for attached properties.
    // DependencyProperty is used as an optimization for either CLr or Attached property when it is backed by a DP.
    internal struct WpfPropertyDefinition
    {
        public WpfPropertyDefinition(BamlRecordReader reader, short attributeId, bool targetIsDependencyObject)
        {
            _reader = reader;
            _attributeId = attributeId;
            _dependencyProperty = null;
            _attributeInfo = null;

            if (_reader.MapTable != null && targetIsDependencyObject)
            {
                _dependencyProperty = _reader.MapTable.GetDependencyProperty(_attributeId);
            }
        }

        public DependencyProperty DependencyProperty
        {
            get
            {
                return _dependencyProperty;
            }
        }

        public BamlAttributeUsage AttributeUsage
        {
            get
            {
                if (_attributeInfo != null)
                {
                    return _attributeInfo.AttributeUsage;
                }
                else if (_reader.MapTable != null)
                {
                    short ownerTypeId;
                    string name;
                    BamlAttributeUsage attributeUsage;
                    _reader.MapTable.GetAttributeInfoFromId(_attributeId, out ownerTypeId, out name, out attributeUsage);
                    return attributeUsage;
                }
                else
                {
                    return BamlAttributeUsage.Default;
                }
            }
        }

        public BamlAttributeInfoRecord AttributeInfo
        {
            get
            {
                if (_attributeInfo == null && _reader.MapTable != null)
                {
                    // Either the attribute is not a DP or the record is still needed.
                    // This version of the method makes sure that attributeInfo.OwnerType is calculated.
                    // In most other cases we ant to avoid unnecessary type allocations.
                    _attributeInfo = _reader.MapTable.GetAttributeInfoFromIdWithOwnerType(_attributeId);
                    Debug.Assert(_attributeInfo != null);
                }
                return _attributeInfo;
            }
        }

        public PropertyInfo PropertyInfo
        {
            get
            {
                if (this.AttributeInfo == null)
                {
                    return null;
                }

                if (_attributeInfo.PropInfo == null)
                {
                    Object currentParent = _reader.GetCurrentObjectData();
                    Type currentParentType = currentParent.GetType();

                    _reader.XamlTypeMapper.UpdateClrPropertyInfo(currentParentType, _attributeInfo);
                }

                return _attributeInfo.PropInfo;
            }
        }

        public MethodInfo AttachedPropertyGetter
        {
            get
            {
                if (this.AttributeInfo == null)
                {
                    return null;
                }

                if (_attributeInfo.AttachedPropertyGetter == null)
                {
                    _reader.XamlTypeMapper.UpdateAttachedPropertyGetter(_attributeInfo);
                }

                return _attributeInfo.AttachedPropertyGetter;
            }
        }

        public MethodInfo AttachedPropertySetter
        {
            get
            {
                if (this.AttributeInfo == null)
                {
                    return null;
                }

                if (_attributeInfo.AttachedPropertySetter == null)
                {
                    // Note we update both Setter and Getter in one call; and detect the need of it by Getter==null
                    _reader.XamlTypeMapper.UpdateAttachedPropertySetter(_attributeInfo);
                }

                return _attributeInfo.AttachedPropertySetter;
            }
        }

        public bool IsInternal
        {
            get
            {
                if (this.AttributeInfo == null)
                {
                    return false;
                }

                return _attributeInfo.IsInternal;
            }
        }

        public Type PropertyType
        {
            get
            {
                if (this.DependencyProperty != null)
                {
                    return this.DependencyProperty.PropertyType;
                }
                else if (this.PropertyInfo != null)
                {
                    return this.PropertyInfo.PropertyType;
                }
                else if (this.AttachedPropertySetter != null)
                {
                    return XamlTypeMapper.GetPropertyType(this.AttachedPropertySetter);
                }
                else
                {
                    Debug.Assert(this.AttachedPropertyGetter != null);
                    return this.AttachedPropertyGetter.ReturnType;
                }
            }
        }

        public string Name
        {
            get
            {
                if (this.DependencyProperty != null)
                {
                    return this.DependencyProperty.Name;
                }
                else if (this.PropertyInfo != null)
                {
                    return this.PropertyInfo.Name;
                }
                else if (this.AttachedPropertySetter != null)
                {
                    return this.AttachedPropertySetter.Name.Substring("Set".Length);
                }
                else if (this.AttachedPropertyGetter != null)
                {
                    return this.AttachedPropertyGetter.Name.Substring("Get".Length);
                }
                else
                {
                    // One of the above should have worked.  If all of them had
                    //  failed, then something has gone wrong.  But we still need
                    //  to be able to provide *something* because a name is
                    //  needed for the exception message.
                    if( _attributeInfo != null )
                    {
                        return _attributeInfo.Name;
                    }
                    else
                    {
                        return "<unknown>";
                    }
                }
            }
        }

        internal object DpOrPiOrMi
        {
            get
            {
                return
                    this.DependencyProperty != null ? (object)this.DependencyProperty :
                    this.PropertyInfo != null ? (object)this.PropertyInfo :
                    (object)this.AttachedPropertySetter;
            }
        }

        private BamlRecordReader _reader;
        private short _attributeId;
        private BamlAttributeInfoRecord _attributeInfo;

        // This field is defined when a DP is available for a property.
        // When DP is defined we do not go after PropertyInfo or AttachedPropertyGetter/Setter
        // unless it was explicitly requested - because of a perf concern.
        private DependencyProperty _dependencyProperty;
    }
}
