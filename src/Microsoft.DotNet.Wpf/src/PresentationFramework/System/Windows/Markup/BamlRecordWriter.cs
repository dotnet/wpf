// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Helper class for writing BAML records
*
\***************************************************************************/

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;

using MS.Utility;

#if !PBTCOMPILER

using System.Windows;
using System.Windows.Threading;

#endif

#if PBTCOMPILER
using System.Globalization;

namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// Class for writing baml to a stream from XamlParser calls
    /// </summary>
    internal class BamlRecordWriter
    {
#region Constructor

        /// <summary>
        /// Constructor that allows defer loading feature to be enabled, even
        /// for non-compile cases.
        /// </summary>
        public BamlRecordWriter(
            Stream        stream,
            ParserContext parserContext,
            bool          deferLoadingSupport)
        {
            _bamlStream = stream;
            _xamlTypeMapper = parserContext.XamlTypeMapper;
            _deferLoadingSupport = deferLoadingSupport;
            _bamlMapTable = parserContext.MapTable;
            _parserContext = parserContext;

            _debugBamlStream = false;
            _lineNumber = -1;
            _linePosition = -1;

            _bamlBinaryWriter = new BamlBinaryWriter(stream,new System.Text.UTF8Encoding());
            _bamlRecordManager = new BamlRecordManager();
        }

#endregion Constructor

#region Methods


        // Called by BamlRecordWriter records to persist a record.
        internal virtual void WriteBamlRecord(
            BamlRecord bamlRecord,
            int        lineNumber,
            int        linePosition)
        {
            try
            {
                bamlRecord.Write(BinaryWriter);

                if(DebugBamlStream)
                {
                    if(BamlRecordHelper.DoesRecordTypeHaveDebugExtension(bamlRecord.RecordType))
                    {
                        WriteDebugExtensionRecord(lineNumber, linePosition);
                    }
                }
            }
            catch (XamlParseException e)
            {
                _xamlTypeMapper.ThrowExceptionWithLine(e.Message, e.InnerException);
            }
        }

        // Indicates if ParentNodes should be updated.
        // This is only used when loading compiled baml so can have node information
        internal virtual bool UpdateParentNodes
        {
            get
            {
                return true;
            }
        }

#if !PBTCOMPILER
        // review - this needs to check that setparseMode is called only
        // once and only on the Root tag.
        internal void SetParseMode(XamlParseMode xamlParseMode)
        {
            // only update if we are allowed to update the parent nodes
            if (UpdateParentNodes)
            {
                 // update the parseMode in the StartRecord.
                if (xamlParseMode == XamlParseMode.Asynchronous)
                {
                    Debug.Assert(null != DocumentStartRecord);
                    if (null != DocumentStartRecord)
                    {
                        DocumentStartRecord.LoadAsync = true;
                        DocumentStartRecord.UpdateWrite(BinaryWriter);
                    }
                }
            }
        }
#endif

        // Sets the number of Records that can be read at a time
        // in async mode. Main use is for debugging.
        internal virtual void SetMaxAsyncRecords(int maxAsyncRecords)
        {
            // only update if we are allowed to update the parent nodes
            if (UpdateParentNodes)
            {
                Debug.Assert(null != DocumentStartRecord);
                if (null != DocumentStartRecord)
                {
                    DocumentStartRecord.MaxAsyncRecords = maxAsyncRecords;
                    DocumentStartRecord.UpdateWrite(BinaryWriter);
                }
            }
        }

#region Record Writing

#region Debug Record Writing

        public bool DebugBamlStream
        {
            get { return _debugBamlStream; }
#if PBTCOMPILER
            set { _debugBamlStream = value; }
#endif
        }

        internal BamlLineAndPositionRecord LineAndPositionRecord
        {
            get
            {
                if(_bamlLineAndPositionRecord == null)
                {
                    _bamlLineAndPositionRecord = new BamlLineAndPositionRecord();
                }
                return _bamlLineAndPositionRecord;
            }
        }

        internal BamlLinePositionRecord LinePositionRecord
        {
            get
            {
                if(_bamlLinePositionRecord == null)
                {
                    _bamlLinePositionRecord = new BamlLinePositionRecord();
                }
                return _bamlLinePositionRecord;
            }
        }

        internal void WriteDebugExtensionRecord(int lineNumber, int linePosition)
        {
            // if the Linenumber has changed then the Position had also.
            // So write out a record with both Line and Position.
            if(lineNumber != _lineNumber)
            {
                BamlLineAndPositionRecord rec = LineAndPositionRecord;
                _lineNumber = lineNumber;
                rec.LineNumber = (uint)lineNumber;

                _linePosition = linePosition;
                rec.LinePosition = (uint)linePosition;

                rec.Write(BinaryWriter);
            }
            // if the Line has NOT changed but the position has then
            // write a smaller record with just the position.
            else if(linePosition != _linePosition)
            {
                _linePosition = linePosition;
                BamlLinePositionRecord rec = LinePositionRecord;
                rec.LinePosition = (uint)linePosition;
                rec.Write(BinaryWriter);
            }
            // if neither has changed then don't waste space and
            // don't write a record.
        }

#endregion Debug Record Writing

        // called to start writing the BAML
        internal void WriteDocumentStart(XamlDocumentStartNode xamlDocumentNode)
        {
            // Always put a Version Block before the Document Start record.
            BamlVersionHeader bamlVersion = new BamlVersionHeader();
            bamlVersion.WriteVersion(BinaryWriter);

            DocumentStartRecord = (BamlDocumentStartRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.DocumentStart);
            DocumentStartRecord.DebugBaml = DebugBamlStream;

            // go ahead and write initial values to the Stream, will back fill.
            // the rootElement.
            WriteBamlRecord(DocumentStartRecord, xamlDocumentNode.LineNumber,
                            xamlDocumentNode.LinePosition);

            BamlRecordManager.ReleaseWriteRecord(DocumentStartRecord);
        }

        // called when BAML is completely written
        internal void WriteDocumentEnd(XamlDocumentEndNode xamlDocumentEndNode)
        {
            // write end of document record
            BamlDocumentEndRecord endDocument =
                (BamlDocumentEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.DocumentEnd);

            WriteBamlRecord(endDocument, xamlDocumentEndNode.LineNumber,
                            xamlDocumentEndNode.LinePosition);

            BamlRecordManager.ReleaseWriteRecord(endDocument);

            // should be done now and evertying fully initialized.
        }

        internal void WriteConnectionId(Int32 connectionId)
        {
            BamlConnectionIdRecord bamlCxnId =
                   (BamlConnectionIdRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.ConnectionId);

            bamlCxnId.ConnectionId = connectionId;
            WriteAndReleaseRecord(bamlCxnId, null);
        }

        // following are for writing to the BAML
        // Somewhat mimics XMLTextWriter
        internal void WriteElementStart(XamlElementStartNode xamlElementNode)
        {
            // initialize the element and add to the stack
            BamlElementStartRecord bamlElement =
                   (BamlElementStartRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.ElementStart);

            // If we do not already have a type record for the type of this element,
            // then add a new TypeInfo record to the map table.
            short typeId;
            if (!MapTable.GetTypeInfoId(BinaryWriter,
                                        xamlElementNode.AssemblyName,
                                        xamlElementNode.TypeFullName,
                                        out typeId))
            {
                string serializerAssemblyName = string.Empty;
                if (xamlElementNode.SerializerType != null)
                {
                    serializerAssemblyName = xamlElementNode.SerializerType.Assembly.FullName;
                }
                typeId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                 xamlElementNode.AssemblyName,
                                                 xamlElementNode.TypeFullName,
                                                 xamlElementNode.ElementType,
                                                 serializerAssemblyName,
                                                 xamlElementNode.SerializerTypeFullName);
            }
            bamlElement.TypeId = typeId;
            bamlElement.CreateUsingTypeConverter = xamlElementNode.CreateUsingTypeConverter;
            bamlElement.IsInjected = xamlElementNode.IsInjected;

            // Check if the element we are about to write supports deferable content.
            // If so, then we have to queue up all the baml records that are contained
            // within this element and extract key information (if this is a dictionary).
            // At the end tag, the queued records will be written in an optimal order
            // and offsets inserted to permit fast runtime indexing of content.
            if (_deferLoadingSupport && _deferElementDepth > 0)
            {
                _deferElementDepth++;

                if (InStaticResourceSection)
                {
                    // Gather all the BamlRecords within the StaticResource section
                    _staticResourceElementDepth++;
                    _staticResourceRecordList.Add(new ValueDeferRecord(bamlElement, xamlElementNode.LineNumber, xamlElementNode.LinePosition));
                }
                else if (CollectingValues && KnownTypes.Types[(int)KnownElements.StaticResourceExtension] == xamlElementNode.ElementType)
                {
                    // Mark the beginning of a StaticResource section
                    _staticResourceElementDepth = 1;
                    _staticResourceRecordList = new List<ValueDeferRecord>(5);
                    _staticResourceRecordList.Add(new ValueDeferRecord(bamlElement, xamlElementNode.LineNumber, xamlElementNode.LinePosition));
                }
                else
                {
                    // Detect that we are within a DynamicResource Section.
                    if (InDynamicResourceSection)
                    {
                        _dynamicResourceElementDepth++;
                    }
                    else if (CollectingValues && KnownTypes.Types[(int)KnownElements.DynamicResourceExtension] == xamlElementNode.ElementType)
                    {
                        _dynamicResourceElementDepth = 1;
                    }

                    ValueDeferRecord deferRecord = new ValueDeferRecord(bamlElement,
                                            xamlElementNode.LineNumber,
                                            xamlElementNode.LinePosition);

                    if(_deferComplexPropertyDepth > 0)
                    {
                        // If we are in the middle of a complex property specified for a defered
                        // type, we need to append to the _deferElement array.
                        _deferElement.Add(deferRecord);
                    }
                    else if (_deferElementDepth == 2)
                    {
                        // If this element is directly below the dictionary root, then put a
                        // placeholder record in the key collection.  If this is not filled
                        // in before we reach the end of this element's scope, then we don't
                        // have a key, and that's an error.

                        _deferKeys.Add(new KeyDeferRecord(xamlElementNode.LineNumber,
                                                          xamlElementNode.LinePosition));

                        // Remember that this element record is for the start of a value,
                        // so that the offset in the associated key record should be set
                        // when this record is actually written out to the baml stream.
                        deferRecord.UpdateOffset = true;
                        _deferValues.Add(deferRecord);
                    }
                    else if (_deferKeyCollecting)
                    {
                        // Don't allow a bind or resource reference in a deferable key, since this
                        // causes problems for resolution.  Multi-pass or recursive key resolution
                        // would be needed to robustly support this feature.
                        if (typeof(String).IsAssignableFrom(xamlElementNode.ElementType) ||
                            KnownTypes.Types[(int)KnownElements.StaticExtension].IsAssignableFrom(xamlElementNode.ElementType) ||
                            KnownTypes.Types[(int)KnownElements.TypeExtension].IsAssignableFrom(xamlElementNode.ElementType))
                        {
                            ((KeyDeferRecord)_deferKeys[_deferKeys.Count-1]).RecordList.Add(deferRecord);
                        }
                        else
                        {
                            XamlParser.ThrowException(SRID.ParserBadKey,
                                                      xamlElementNode.TypeFullName,
                                                      xamlElementNode.LineNumber,
                                                      xamlElementNode.LinePosition);
                        }
                    }
                    else
                    {
                        _deferValues.Add(deferRecord);
                    }
                }
            }
            else if (_deferLoadingSupport && KnownTypes.Types[(int)KnownElements.ResourceDictionary].IsAssignableFrom(xamlElementNode.ElementType))
            {
                _deferElementDepth = 1;
                _deferEndOfStartReached = false;
                _deferElement = new ArrayList(2);
                _deferKeys = new ArrayList(10);
                _deferValues = new ArrayList(100);

                _deferElement.Add(new ValueDeferRecord(bamlElement,
                                        xamlElementNode.LineNumber,
                                        xamlElementNode.LinePosition));
            }

            else
            {
                WriteBamlRecord(bamlElement, xamlElementNode.LineNumber,
                                xamlElementNode.LinePosition);

                BamlRecordManager.ReleaseWriteRecord(bamlElement);
            }
          }

        // The end of an element has been reached, so write out the end indication
        internal void WriteElementEnd(XamlElementEndNode xamlElementEndNode)
        {
             BamlElementEndRecord bamlElementEnd =
                (BamlElementEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.ElementEnd);

            // Check if we're queuing up deferable content.  If so and we're at the
            // end tag for that content, now is the time to actually write everything
            // to the baml stream and rewrite in the file locations for the values
            // sections.
            if (_deferLoadingSupport && _deferElementDepth > 0 && _deferElementDepth-- == 1)
            {
                WriteDeferableContent(xamlElementEndNode);

                // Clear all defer related instance data
                Debug.Assert(_deferElementDepth == 0);
                Debug.Assert(_deferComplexPropertyDepth == 0);
                Debug.Assert(_staticResourceElementDepth == 0);
                Debug.Assert(_dynamicResourceElementDepth == 0);
                Debug.Assert(_staticResourceRecordList == null);
                _deferKeys = null;
                _deferValues = null;
                _deferElement = null;
            }
            else
            {
                WriteAndReleaseRecord(bamlElementEnd, xamlElementEndNode);

                if (_deferLoadingSupport && _staticResourceElementDepth > 0 && _staticResourceElementDepth-- == 1)
                {
                    // This marks the end of a StaticResource section

                    // Process the StaticResourceRecordList that we
                    // have been gathering this far
                    WriteStaticResource();

                    // Cleanup the list after processing
                    Debug.Assert(_staticResourceElementDepth == 0);
                    _staticResourceRecordList = null;
                }
                else if (_deferLoadingSupport && _dynamicResourceElementDepth > 0 && _dynamicResourceElementDepth-- == 1)
                {
                    // We have now exited the dynamic resource section
                }
            }

            // we've come to the end of the element.
         }

        // The end of the start tag has been reached.  For compile cases, check
        // if we are accumulating a deferable block of records.
        internal void WriteEndAttributes(XamlEndAttributesNode xamlEndAttributesNode)
        {
            if (_deferLoadingSupport && _deferElementDepth > 0)
            {
                _deferEndOfStartReached = true;
            }
        }

        // Write a literal content blob to BAML
        internal void WriteLiteralContent(XamlLiteralContentNode xamlLiteralContentNode)
        {
            // initialize the element and add to the stack
            BamlLiteralContentRecord bamlLiteralContent =
                (BamlLiteralContentRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.LiteralContent);
            bamlLiteralContent.Value = xamlLiteralContentNode.Content;

            WriteAndReleaseRecord(bamlLiteralContent, xamlLiteralContentNode);
        }

        // Write an x:Key="value" where value has been resolved to a Type object
        // at compile or parse time.  This means less reflection at runtime.
        internal void WriteDefAttributeKeyType(XamlDefAttributeKeyTypeNode xamlDefNode)
        {
            Debug.Assert(!InStaticResourceSection, "We do not support x:Key within a StaticResource Section");

            // If we do not already have a type record for the type of this key,
            // then add a new TypeInfo record to the map table.
            short typeId;
            if (!MapTable.GetTypeInfoId(BinaryWriter,
                                        xamlDefNode.AssemblyName,
                                        xamlDefNode.Value,
                                        out typeId))
            {
                typeId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                 xamlDefNode.AssemblyName,
                                                 xamlDefNode.Value,
                                                 xamlDefNode.ValueType,
                                                 string.Empty,
                                                 string.Empty);
            }


            BamlDefAttributeKeyTypeRecord bamlDefRecord = BamlRecordManager.GetWriteRecord(
                                                          BamlRecordType.DefAttributeKeyType)
                                                          as BamlDefAttributeKeyTypeRecord;

            bamlDefRecord.TypeId = typeId;
            ((IBamlDictionaryKey)bamlDefRecord).KeyObject = xamlDefNode.ValueType;

            // If we are currently parsing a deferable content section then store it
            // in the keys collection.
            if (_deferLoadingSupport &&
                _deferElementDepth == 2)
            {
                KeyDeferRecord keyRecord = (KeyDeferRecord)(_deferKeys[_deferKeys.Count-1]);

                TransferOldSharedData(keyRecord.Record as IBamlDictionaryKey, bamlDefRecord as IBamlDictionaryKey);
                keyRecord.Record = bamlDefRecord;

                keyRecord.LineNumber = xamlDefNode.LineNumber;
                keyRecord.LinePosition = xamlDefNode.LinePosition;
                return;
            }

            if (_deferLoadingSupport && _deferElementDepth > 0)
            {
                _deferValues.Add(new ValueDeferRecord(bamlDefRecord,
                                            xamlDefNode.LineNumber,
                                            xamlDefNode.LinePosition));
            }
            else
            {
                WriteBamlRecord(bamlDefRecord, xamlDefNode.LineNumber,
                                    xamlDefNode.LinePosition);

                BamlRecordManager.ReleaseWriteRecord(bamlDefRecord);
            }
        }

        private void TransferOldSharedData(IBamlDictionaryKey oldRecord, IBamlDictionaryKey newRecord)
        {
            if ((oldRecord != null) && (newRecord != null))
            {
                newRecord.Shared = oldRecord.Shared;
                newRecord.SharedSet = oldRecord.SharedSet;
            }
        }

        private IBamlDictionaryKey FindBamlDictionaryKey(KeyDeferRecord record)
        {
            if (record != null)
            {
                if (record.RecordList != null)
                {
                    for (int i = 0; i < record.RecordList.Count; i++)
                    {
                        ValueDeferRecord valueDeferRecord = (ValueDeferRecord)record.RecordList[i];
                        IBamlDictionaryKey dictionaryKey = valueDeferRecord.Record as IBamlDictionaryKey;
                        if (dictionaryKey != null)
                        {
                            return dictionaryKey;
                        }
                    }
                }

                return record.Record as IBamlDictionaryKey;
            }

            return null;
        }

        // Write a x:attribute="value" record.  One typical use of this is
        // to specify the key to use when inserting the current object into
        // a Dictionary, but there can be other uses also
        internal void WriteDefAttribute(XamlDefAttributeNode xamlDefNode)
        {
            // If we are currently parsing a deferable content section, then check
            // to see if we have a dictionary key value here.  If so, then store it
            // in the keys collection.
            if (_deferLoadingSupport &&
                _deferElementDepth == 2 &&
                xamlDefNode.Name == XamlReaderHelper.DefinitionName)
            {
                Debug.Assert(!InStaticResourceSection, "We do not support x:Key within a StaticResource Section");

                // Note that if we get to here the assumption is that the value of the Name
                // attribute is *NOT* a MarkupExtension.  A MarkupExtension would cause
                // WriteKeyElementStart being called.
                KeyDeferRecord keyRecord = (KeyDeferRecord)(_deferKeys[_deferKeys.Count-1]);
                BamlDefAttributeKeyStringRecord defKeyRecord = keyRecord.Record as BamlDefAttributeKeyStringRecord;
                if (defKeyRecord == null)
                {
                    defKeyRecord =
                        (BamlDefAttributeKeyStringRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.DefAttributeKeyString);
                    TransferOldSharedData(keyRecord.Record as IBamlDictionaryKey, defKeyRecord as IBamlDictionaryKey);
                    keyRecord.Record = defKeyRecord;
                }

                // Store the string value in the string table, since there will mostly be a
                // [Static/Dynamic]Resource referring to it and we can combine the storage
                // for both these string into a single StringInfoRecord.
                short valueId;
                if (!MapTable.GetStringInfoId(xamlDefNode.Value,
                                              out valueId))
                {
                    valueId = MapTable.AddStringInfoMap(BinaryWriter,
                                                         xamlDefNode.Value);
                }

                defKeyRecord.Value = xamlDefNode.Value;
                defKeyRecord.ValueId = valueId;

                keyRecord.LineNumber = xamlDefNode.LineNumber;
                keyRecord.LinePosition = xamlDefNode.LinePosition;
                return;
            }
            else if (_deferLoadingSupport &&
                     _deferElementDepth == 2 &&
                     xamlDefNode.Name == XamlReaderHelper.DefinitionShared)
            {
                Debug.Assert(!InStaticResourceSection, "We do not support x:Shared within a StaticResource Section");

                // NOTE:  This does not properly handle MarkupExtensions....
                KeyDeferRecord keyRecord = (KeyDeferRecord)(_deferKeys[_deferKeys.Count-1]);

                IBamlDictionaryKey defKeyRecord = FindBamlDictionaryKey(keyRecord);
                if (defKeyRecord == null)
                {
                    BamlDefAttributeKeyStringRecord defStringKeyRecord =
                        (BamlDefAttributeKeyStringRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.DefAttributeKeyString);
                    keyRecord.Record = defStringKeyRecord;
                    defKeyRecord = (IBamlDictionaryKey)defStringKeyRecord;
                }

                defKeyRecord.Shared = Boolean.Parse(xamlDefNode.Value);
                defKeyRecord.SharedSet = true;
                keyRecord.LineNumber = xamlDefNode.LineNumber;
                keyRecord.LinePosition = xamlDefNode.LinePosition;
                return;
            }

            // Add definition attribute record.  Store the attribute name in the string table, since
            // the names are likely to be repeated.
            short stringId;
            if (!MapTable.GetStringInfoId(xamlDefNode.Name,
                                          out stringId))
            {
                stringId = MapTable.AddStringInfoMap(BinaryWriter,
                                                     xamlDefNode.Name);
            }

            BamlDefAttributeRecord defRecord =
                (BamlDefAttributeRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.DefAttribute);

            defRecord.Value  = xamlDefNode.Value;
            defRecord.Name   = xamlDefNode.Name;
            defRecord.AttributeUsage = xamlDefNode.AttributeUsage;
            defRecord.NameId = stringId;

            WriteAndReleaseRecord(defRecord, xamlDefNode);
        }

        // Attributes used to specify WPF-specific parsing options
        internal void WritePresentationOptionsAttribute(XamlPresentationOptionsAttributeNode xamlPresentationOptionsNode)
        {
            // Add definition attribute record.  Store the attribute name in the string table, since
            // the names are likely to be repeated.
            short stringId;
            if (!MapTable.GetStringInfoId(xamlPresentationOptionsNode.Name,
                                          out stringId))
            {
                stringId = MapTable.AddStringInfoMap(BinaryWriter,
                                                     xamlPresentationOptionsNode.Name);
            }

            BamlPresentationOptionsAttributeRecord attributeRecord =
                (BamlPresentationOptionsAttributeRecord) BamlRecordManager.GetWriteRecord(
                                        BamlRecordType.PresentationOptionsAttribute);

            attributeRecord.Value = xamlPresentationOptionsNode.Value;
            attributeRecord.Name = xamlPresentationOptionsNode.Name;
            attributeRecord.NameId = stringId;

            WriteAndReleaseRecord(attributeRecord, xamlPresentationOptionsNode);
        }


        // The current element contains xmlns combinations we want scoped in case
        // InnerXAML is done at runtime.
        internal void WriteNamespacePrefix(XamlXmlnsPropertyNode xamlXmlnsPropertyNode)
        {
            BamlXmlnsPropertyRecord xmlnsRecord =
                (BamlXmlnsPropertyRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.XmlnsProperty);

            xmlnsRecord.Prefix = xamlXmlnsPropertyNode.Prefix;
            xmlnsRecord.XmlNamespace  = xamlXmlnsPropertyNode.XmlNamespace;

#if PBTCOMPILER

            //
            // Get a list of Assemblies that contain XmlnsDefinitionAttribute for the
            // specific XmlNamespace. Add the relevant assemblies into MapTable.
            //

            if (xamlXmlnsPropertyNode.XmlNamespace.StartsWith(XamlReaderHelper.MappingProtocol, StringComparison.Ordinal) == false)
            {
                NamespaceMapEntry[] nsMapEntry = _xamlTypeMapper.GetNamespaceMapEntries(xamlXmlnsPropertyNode.XmlNamespace);

                if (nsMapEntry != null && nsMapEntry.Length > 0)
                {
                    ArrayList asmList = new ArrayList();
                    for (int i = 0; i < nsMapEntry.Length; i++)
                    {
                        string asmName = nsMapEntry[i].AssemblyName;

                        if (!asmList.Contains(asmName))
                        {
                            asmList.Add(asmName);
                        }
                    }

                    if (asmList.Count > 0)
                    {
                        short[] assemblyIds = new short[asmList.Count];

                        for (int i = 0; i < asmList.Count; i++)
                        {
                            BamlAssemblyInfoRecord bamlAssemblyInfoRecord = MapTable.AddAssemblyMap(BinaryWriter, (string)asmList[i]);

                            assemblyIds[i] = bamlAssemblyInfoRecord.AssemblyId;
                        }

                        xmlnsRecord.AssemblyIds = assemblyIds;
                    }
                }
            }

#endif


            // NOTE:  If we are defining a new namespace prefix in the value object's
            //        start record, AND using that prefix in x:Key, then we have a
            //        problem, since the x:Key keys are hoisted out before this
            //        record in the baml stream....  I don't have a solution for
            //        this yet, but this isn't an issue for theme files...
            WriteAndReleaseRecord(xmlnsRecord, xamlXmlnsPropertyNode);
        }

        // Write a xml to clr namespace mapping record
        internal void WritePIMapping(XamlPIMappingNode xamlPIMappingNode)
        {
            BamlPIMappingRecord piMappingRecord =
                (BamlPIMappingRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PIMapping);

            BamlAssemblyInfoRecord bamlAssemblyInfoRecord = MapTable.AddAssemblyMap(BinaryWriter,
                                                                  xamlPIMappingNode.AssemblyName);

            piMappingRecord.XmlNamespace = xamlPIMappingNode.XmlNamespace;
            piMappingRecord.ClrNamespace = xamlPIMappingNode.ClrNamespace;
            piMappingRecord.AssemblyId   = bamlAssemblyInfoRecord.AssemblyId;

            WriteBamlRecord(piMappingRecord, xamlPIMappingNode.LineNumber,
                            xamlPIMappingNode.LinePosition);

            BamlRecordManager.ReleaseWriteRecord(piMappingRecord);
        }

        // Write the start of a complex property
        internal void WritePropertyComplexStart(XamlPropertyComplexStartNode
                                    xamlComplexPropertyNode)
        {
            // review same as property + flag, combine code
            BamlPropertyComplexStartRecord bamlComplexProperty =
                (BamlPropertyComplexStartRecord) BamlRecordManager.GetWriteRecord(
                                                        BamlRecordType.PropertyComplexStart);

            bamlComplexProperty.AttributeId =
                MapTable.AddAttributeInfoMap(BinaryWriter,
                                             xamlComplexPropertyNode.AssemblyName,
                                             xamlComplexPropertyNode.TypeFullName,
                                             xamlComplexPropertyNode.PropDeclaringType,
                                             xamlComplexPropertyNode.PropName,
                                             xamlComplexPropertyNode.PropValidType,
                                             BamlAttributeUsage.Default);

            WriteAndReleaseRecord(bamlComplexProperty, xamlComplexPropertyNode);
        }

        // Write the end of a complex property
        internal void WritePropertyComplexEnd(
            XamlPropertyComplexEndNode xamlPropertyComplexEnd)
        {
            BamlPropertyComplexEndRecord endPropertyRecord =
                (BamlPropertyComplexEndRecord) BamlRecordManager.GetWriteRecord(
                                                         BamlRecordType.PropertyComplexEnd);

            WriteAndReleaseRecord(endPropertyRecord, xamlPropertyComplexEnd);
        }

        // Write the start of a def attribute element used as the key in an
        // IDictionary
        public void WriteKeyElementStart(
            XamlElementStartNode xamlKeyElementNode)
        {
            Debug.Assert(!InStaticResourceSection, "We do not support x:Key within a StaticResource Section");

            // Don't allow a bind or resource reference in a key element, since this
            // causes problems for resolution.  Multi-pass or recursive key resolution
            // would be needed to robustly support this feature.
            if (!typeof(String).IsAssignableFrom(xamlKeyElementNode.ElementType) &&
                !KnownTypes.Types[(int)KnownElements.StaticExtension].IsAssignableFrom(xamlKeyElementNode.ElementType) &&
                !KnownTypes.Types[(int)KnownElements.TypeExtension].IsAssignableFrom(xamlKeyElementNode.ElementType) &&
                !KnownTypes.Types[(int)KnownElements.ResourceKey].IsAssignableFrom(xamlKeyElementNode.ElementType))
            {
                XamlParser.ThrowException(SRID.ParserBadKey,
                                          xamlKeyElementNode.TypeFullName,
                                          xamlKeyElementNode.LineNumber,
                                          xamlKeyElementNode.LinePosition);
            }

            // initialize the element and add to the stack
            BamlKeyElementStartRecord bamlElement =
                   (BamlKeyElementStartRecord) BamlRecordManager.GetWriteRecord(
                                               BamlRecordType.KeyElementStart);

            // If we do not already have a type record for the type of this element,
            // then add a new TypeInfo record to the map table.
            short typeId;
            if (!MapTable.GetTypeInfoId(BinaryWriter,
                                        xamlKeyElementNode.AssemblyName,
                                        xamlKeyElementNode.TypeFullName,
                                        out typeId))
            {
                string serializerAssemblyName = string.Empty;
                if (xamlKeyElementNode.SerializerType != null)
                {
                    serializerAssemblyName = xamlKeyElementNode.SerializerType.Assembly.FullName;
                }
                typeId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                 xamlKeyElementNode.AssemblyName,
                                                 xamlKeyElementNode.TypeFullName,
                                                 xamlKeyElementNode.ElementType,
                                                 serializerAssemblyName,
                                                 xamlKeyElementNode.SerializerTypeFullName);
            }
            bamlElement.TypeId = typeId;

            // Check if the element we are about to write supports deferable content.
            // If so, then we have to queue up all the baml records that are contained
            // within this element and use this as a key (if this is a dictionary).
            // At the end tag, the queued records will be written in an optimal order
            // and offsets inserted to permit fast runtime indexing of content.
            if (_deferLoadingSupport && _deferElementDepth == 2)
            {
                _deferElementDepth++;
                _deferKeyCollecting = true;

                KeyDeferRecord keyRecord = (KeyDeferRecord)(_deferKeys[_deferKeys.Count-1]);
                keyRecord.RecordList = new ArrayList(5);

                Debug.Assert(keyRecord.RecordList.Count == 0, "Should have empty record list");
                keyRecord.RecordList.Add(new ValueDeferRecord(bamlElement,
                                                              xamlKeyElementNode.LineNumber,
                                                              xamlKeyElementNode.LinePosition));

                if (keyRecord.Record != null)
                {
                    TransferOldSharedData(keyRecord.Record as IBamlDictionaryKey, bamlElement as IBamlDictionaryKey);
                    keyRecord.Record = null;
                }

                keyRecord.LineNumber = xamlKeyElementNode.LineNumber;
                keyRecord.LinePosition = xamlKeyElementNode.LinePosition;
                return;
            }
            else
            {
                WriteAndReleaseRecord(bamlElement, xamlKeyElementNode);
            }
        }

        // Write the end of a def attribute tree section used as the key
        // in an IDictionary
        internal void WriteKeyElementEnd(
            XamlElementEndNode xamlKeyElementEnd)
        {
            Debug.Assert(!InStaticResourceSection, "We do not support x:Key within a StaticResource Section");

            BamlKeyElementEndRecord endRecord =
                (BamlKeyElementEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.KeyElementEnd);

            WriteAndReleaseRecord(endRecord, xamlKeyElementEnd);

            if (_deferLoadingSupport && _deferKeyCollecting)
            {
                _deferKeyCollecting = false;
                _deferElementDepth--;
                Debug.Assert(_deferElementDepth == 2);
            }
        }

#if PBTCOMPILER
        /// <summary>
        /// Write the constructor parameter that has been resolved to a Type
        /// </summary>
        internal void WriteConstructorParameterType(
            XamlConstructorParameterTypeNode xamlConstructorParameterTypeNode)
        {
            // If we do not already have a type record for the type of this property,
            // then add a new TypeInfo record to the map table.
            short typeId;
            if (!MapTable.GetTypeInfoId(BinaryWriter,
                                        xamlConstructorParameterTypeNode.ValueTypeAssemblyName,
                                        xamlConstructorParameterTypeNode.ValueTypeFullName,
                                        out typeId))
            {
                typeId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                 xamlConstructorParameterTypeNode.ValueTypeAssemblyName,
                                                 xamlConstructorParameterTypeNode.ValueTypeFullName,
                                                 xamlConstructorParameterTypeNode.ValueElementType,
                                                 string.Empty,
                                                 string.Empty);
            }


            BamlConstructorParameterTypeRecord bamlConstructor = BamlRecordManager.GetWriteRecord(
                                                          BamlRecordType.ConstructorParameterType)
                                                          as BamlConstructorParameterTypeRecord;
            bamlConstructor.TypeId = typeId;

            WriteAndReleaseRecord(bamlConstructor, xamlConstructorParameterTypeNode);
        }
#endif

        /// <summary>
        /// Write the start of a constructor parameter section
        /// </summary>
        internal void WriteConstructorParametersStart(
            XamlConstructorParametersStartNode xamlConstructorParametersStartNode)
        {
            // Create a new baml record
            BamlConstructorParametersStartRecord startRecord =
                (BamlConstructorParametersStartRecord) BamlRecordManager.GetWriteRecord(
                     BamlRecordType.ConstructorParametersStart);

            WriteAndReleaseRecord(startRecord, xamlConstructorParametersStartNode);
        }

        /// <summary>
        /// Write the end of a constructor parameter section
        /// </summary>
        internal void WriteConstructorParametersEnd(
            XamlConstructorParametersEndNode xamlConstructorParametersEndNode)
        {
            // Create a new baml record
            BamlConstructorParametersEndRecord startRecord =
                (BamlConstructorParametersEndRecord) BamlRecordManager.GetWriteRecord(
                     BamlRecordType.ConstructorParametersEnd);

            WriteAndReleaseRecord(startRecord, xamlConstructorParametersEndNode);
        }

        internal virtual void WriteContentProperty(XamlContentPropertyNode xamlContentPropertyNode)
        {
            BamlContentPropertyRecord bamlContentPropertyRecord =
                (BamlContentPropertyRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.ContentProperty);

            bamlContentPropertyRecord.AttributeId =
                MapTable.AddAttributeInfoMap(BinaryWriter,
                                             xamlContentPropertyNode.AssemblyName,
                                             xamlContentPropertyNode.TypeFullName,
                                             xamlContentPropertyNode.PropDeclaringType,
                                             xamlContentPropertyNode.PropName,
                                             xamlContentPropertyNode.PropValidType,
                                             BamlAttributeUsage.Default);

            WriteAndReleaseRecord(bamlContentPropertyRecord, xamlContentPropertyNode);
        }

        // Write a property baml record.  If the type of this property supports
        // custom serialization or type conversion, then write out a special property
        // record.  Otherwise write out a 'normal' record, which will cause type
        // converter resolution to happen at load time.


        internal virtual void WriteProperty(XamlPropertyNode xamlProperty)
        {
            short attributeId =
                    MapTable.AddAttributeInfoMap(BinaryWriter,
                                                 xamlProperty.AssemblyName,
                                                 xamlProperty.TypeFullName,
                                                 xamlProperty.PropDeclaringType,
                                                 xamlProperty.PropName,
                                                 xamlProperty.PropValidType,
                                                 xamlProperty.AttributeUsage);

            if (xamlProperty.AssemblyName != string.Empty && xamlProperty.TypeFullName != string.Empty)
            {
                short converterOrSerializerTypeId;
                Type  converterOrSerializerType;
                bool isCustomSerializer = MapTable.GetCustomSerializerOrConverter(
                              BinaryWriter,
                              xamlProperty.ValueDeclaringType,
                              xamlProperty.ValuePropertyType,
                              xamlProperty.ValuePropertyMember,
                              xamlProperty.ValuePropertyName,
                          out converterOrSerializerTypeId,
                          out converterOrSerializerType);

                if (converterOrSerializerType != null)
                {
                    if (isCustomSerializer)
                    {
                        BamlPropertyCustomWriteInfoRecord bamlPropertyCustom =
                            (BamlPropertyCustomWriteInfoRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyCustom);
                        bamlPropertyCustom.AttributeId = attributeId;
                        bamlPropertyCustom.Value = xamlProperty.Value;
                        bamlPropertyCustom.ValueType = xamlProperty.ValuePropertyType;
                        bamlPropertyCustom.SerializerTypeId = converterOrSerializerTypeId;
                        bamlPropertyCustom.SerializerType = converterOrSerializerType;
                        bamlPropertyCustom.TypeContext = TypeConvertContext;
                        if (converterOrSerializerTypeId == (short)KnownElements.DependencyPropertyConverter)
                        {
                            // if ValueId\MemberName have alredy been resolved, just write it out.
                            if (xamlProperty.HasValueId)
                            {
                                bamlPropertyCustom.ValueId = xamlProperty.ValueId;
                                bamlPropertyCustom.ValueMemberName = xamlProperty.MemberName;
                            }
                            else
                            {
                                // else try to resolve the DP value of this property now
                                string dpName;

                                // get the ownerType and name of the DP value
                                Type ownerType = _xamlTypeMapper.GetDependencyPropertyOwnerAndName(xamlProperty.Value,
                                                                                                   ParserContext,
                                                                                                   xamlProperty.DefaultTargetType,
                                                                                               out dpName);
                                short typeId;

                                // get the known property Id or TypeId of the owner of the DP value
                                short propertyId = MapTable.GetAttributeOrTypeId(BinaryWriter,
                                                                                 ownerType,
                                                                                 dpName,
                                                                             out typeId);

                                // write it out as appropriate.
                                if (propertyId < 0)
                                {
                                    bamlPropertyCustom.ValueId = propertyId;
                                    bamlPropertyCustom.ValueMemberName = null;
                                }
                                else
                                {
                                    bamlPropertyCustom.ValueId = typeId;
                                    bamlPropertyCustom.ValueMemberName = dpName;
                                }
                            }
                        }

                        WriteAndReleaseRecord(bamlPropertyCustom, xamlProperty);
                    }
                    else
                    {
                        BamlPropertyWithConverterRecord bamlPropertyWithConverter = (BamlPropertyWithConverterRecord)BamlRecordManager.GetWriteRecord(
                                                              BamlRecordType.PropertyWithConverter);
                        bamlPropertyWithConverter.AttributeId = attributeId;
                        bamlPropertyWithConverter.Value = xamlProperty.Value;
                        bamlPropertyWithConverter.ConverterTypeId = converterOrSerializerTypeId;
                        WriteAndReleaseRecord(bamlPropertyWithConverter, xamlProperty);
                    }

                    return;
                }
            }

            BaseWriteProperty(xamlProperty);
        }

        internal virtual void WritePropertyWithExtension(XamlPropertyWithExtensionNode xamlPropertyNode)
        {
            short valueId = 0;
            short extensionTypeId = xamlPropertyNode.ExtensionTypeId;
            bool isValueTypeExtension = false;
            bool isValueStaticExtension = false;
            Debug.Assert(extensionTypeId != (short)KnownElements.TypeExtension);

            // if the extension is a DynamicResourceExtension or a StaticResourceExtension
            // dig in to see if its param is a simple extension or just a string.
            if ((extensionTypeId == (short)KnownElements.DynamicResourceExtension) ||
                (extensionTypeId == (short)KnownElements.StaticResourceExtension))
            {
                // if the value is a simple nested extension
                if (xamlPropertyNode.IsValueNestedExtension)
                {
                    // Yes, see if it is a TypeExtension or StaticExtension
                    if (xamlPropertyNode.IsValueTypeExtension)
                    {
                        // nested TypeExtension value
                        Type typeValue = _xamlTypeMapper.GetTypeFromBaseString(xamlPropertyNode.Value,
                                                                               ParserContext,
                                                                               true);
                        Debug.Assert(typeValue != null);
                        if (!MapTable.GetTypeInfoId(BinaryWriter,
                                                    typeValue.Assembly.FullName,
                                                    typeValue.FullName,
                                                    out valueId))
                        {
                            valueId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                              typeValue.Assembly.FullName,
                                                              typeValue.FullName,
                                                              typeValue,
                                                              string.Empty,
                                                              string.Empty);
                        }

                        isValueTypeExtension = true;
                    }
                    else
                    {
                        // nested StaticExtension value
                        valueId = MapTable.GetStaticMemberId(BinaryWriter,
                                                             ParserContext,
                                                             (short)KnownElements.StaticExtension,
                                                             xamlPropertyNode.Value,
                                                             xamlPropertyNode.DefaultTargetType);

                        isValueStaticExtension = true;
                    }
                }
                else
                {
                    // No, it is a string value.
                    // Store the string value in the string table, since these records
                    // are already used as the key for a [Static/Dynamic]Resource.
                    if (!MapTable.GetStringInfoId(xamlPropertyNode.Value, out valueId))
                    {
                        valueId = MapTable.AddStringInfoMap(BinaryWriter, xamlPropertyNode.Value);
                    }
                }
            }
            else
            {
                // toplevel StaticExtension or TemplateBindingExtension value
                valueId = MapTable.GetStaticMemberId(BinaryWriter,
                                                     ParserContext,
                                                     extensionTypeId,
                                                     xamlPropertyNode.Value,
                                                     xamlPropertyNode.DefaultTargetType);
            }

            short attributeId = MapTable.AddAttributeInfoMap(BinaryWriter,
                                                             xamlPropertyNode.AssemblyName,
                                                             xamlPropertyNode.TypeFullName,
                                                             xamlPropertyNode.PropDeclaringType,
                                                             xamlPropertyNode.PropName,
                                                             xamlPropertyNode.PropValidType,
                                                             BamlAttributeUsage.Default);

            if (_deferLoadingSupport && _deferElementDepth > 0 && CollectingValues &&
                extensionTypeId == (short)KnownElements.StaticResourceExtension)
            {
                // If we are currently processing a StaticResourceExtension
                // within a deferable content section then the information in
                // the xamlPropertyNode is distributed among two separate
                // BamlRecords viz. BamlOptimizedStaticResourceRecord which
                // belongs in the header of the deferred section and a
                // BamlPropertyWithStaticResourceIdRecord which is an inline
                // place holder for the same.


                // Create and populate the BamlOptimizedStaticResourceRecord that
                // is stored in the header of the deferred section.

                BamlOptimizedStaticResourceRecord bamlOptimizedStaticResource =
                    (BamlOptimizedStaticResourceRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.OptimizedStaticResource);

                bamlOptimizedStaticResource.IsValueTypeExtension = isValueTypeExtension;
                bamlOptimizedStaticResource.IsValueStaticExtension = isValueStaticExtension;
                bamlOptimizedStaticResource.ValueId = valueId;

                _staticResourceRecordList = new List<ValueDeferRecord>(1);
                _staticResourceRecordList.Add(new ValueDeferRecord(
                    bamlOptimizedStaticResource,
                    xamlPropertyNode.LineNumber,
                    xamlPropertyNode.LinePosition));

                // Add the current StaticResource to the list on the current key record
                KeyDeferRecord keyDeferRecord = ((KeyDeferRecord)_deferKeys[_deferKeys.Count-1]);
                keyDeferRecord.StaticResourceRecordList.Add(_staticResourceRecordList);

                // Write a PropertyWithStaticResourceId to the values collection
                BamlPropertyWithStaticResourceIdRecord bamlPropertyWithStaticResourceId =
                    (BamlPropertyWithStaticResourceIdRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyWithStaticResourceId);

                bamlPropertyWithStaticResourceId.AttributeId = attributeId;
                bamlPropertyWithStaticResourceId.StaticResourceId = (short)(keyDeferRecord.StaticResourceRecordList.Count-1);

                _deferValues.Add(new ValueDeferRecord(
                    bamlPropertyWithStaticResourceId,
                    xamlPropertyNode.LineNumber,
                    xamlPropertyNode.LinePosition));

                _staticResourceRecordList = null;
            }
            else
            {
                BamlPropertyWithExtensionRecord bamlPropertyWithExtension =
                    (BamlPropertyWithExtensionRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyWithExtension);

                bamlPropertyWithExtension.AttributeId = attributeId;
                bamlPropertyWithExtension.ExtensionTypeId = extensionTypeId;
                bamlPropertyWithExtension.IsValueTypeExtension = isValueTypeExtension;
                bamlPropertyWithExtension.IsValueStaticExtension = isValueStaticExtension;
                bamlPropertyWithExtension.ValueId = valueId;

                WriteAndReleaseRecord(bamlPropertyWithExtension, xamlPropertyNode);
            }
        }

        // Write a property baml record for a property that is of type 'Type'.  This means
        // that the baml record contains a typeid reference for a type which was resolved at
        // compile time.
        internal virtual void WritePropertyWithType(XamlPropertyWithTypeNode xamlPropertyWithType)
        {
            short attributeId =
                    MapTable.AddAttributeInfoMap(BinaryWriter,
                                                 xamlPropertyWithType.AssemblyName,
                                                 xamlPropertyWithType.TypeFullName,
                                                 xamlPropertyWithType.PropDeclaringType,
                                                 xamlPropertyWithType.PropName,
                                                 xamlPropertyWithType.PropValidType,
                                                 BamlAttributeUsage.Default);

            // If we do not already have a type record for the type of this property,
            // then add a new TypeInfo record to the map table.
            short typeId;
            if (!MapTable.GetTypeInfoId(BinaryWriter,
                                        xamlPropertyWithType.ValueTypeAssemblyName,
                                        xamlPropertyWithType.ValueTypeFullName,
                                        out typeId))
            {
                typeId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                 xamlPropertyWithType.ValueTypeAssemblyName,
                                                 xamlPropertyWithType.ValueTypeFullName,
                                                 xamlPropertyWithType.ValueElementType,
                                                 xamlPropertyWithType.ValueSerializerTypeAssemblyName,
                                                 xamlPropertyWithType.ValueSerializerTypeFullName);
            }

            BamlPropertyTypeReferenceRecord bamlProperty = BamlRecordManager.GetWriteRecord(
                                                          BamlRecordType.PropertyTypeReference) as BamlPropertyTypeReferenceRecord;

            bamlProperty.AttributeId = attributeId;
            bamlProperty.TypeId = typeId;

            WriteAndReleaseRecord(bamlProperty, xamlPropertyWithType);
        }

        // Write out property to BAML record, with the property value as a string.
        // This is used if the value cannot stream itself out directly, or if we
        // are creating a tree directly and not storing BAML to a file.
        internal void BaseWriteProperty(XamlPropertyNode xamlProperty)
        {
            short attributeId =
                    MapTable.AddAttributeInfoMap(BinaryWriter,
                                                 xamlProperty.AssemblyName,
                                                 xamlProperty.TypeFullName,
                                                 xamlProperty.PropDeclaringType,
                                                 xamlProperty.PropName,
                                                 xamlProperty.PropValidType,
                                                 xamlProperty.AttributeUsage);

            BamlPropertyRecord bamlClrProperty =
            (BamlPropertyRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.Property);
            bamlClrProperty.AttributeId = attributeId;
            bamlClrProperty.Value = xamlProperty.Value;
            WriteAndReleaseRecord(bamlClrProperty, xamlProperty);
        }

        internal void WriteClrEvent(XamlClrEventNode xamlClrEventNode)
        {
            // This should have been overridden by AC to catch the CLR event case before
            // this point is reached.  If not it means we have a designer which
            // should have been given the pertinent information, so keep this as a placeholder,
            // but don't do anything now.
        }

        // Write the start of an array property
        internal void WritePropertyArrayStart(
            XamlPropertyArrayStartNode xamlPropertyArrayStartNode)
        {
            // initialize the element and add to the stack
            BamlPropertyArrayStartRecord bamlPropertyArrayStart =
                (BamlPropertyArrayStartRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyArrayStart);

            bamlPropertyArrayStart.AttributeId =
                MapTable.AddAttributeInfoMap(BinaryWriter,
                                             xamlPropertyArrayStartNode.AssemblyName,
                                             xamlPropertyArrayStartNode.TypeFullName,
                                             xamlPropertyArrayStartNode.PropDeclaringType,
                                             xamlPropertyArrayStartNode.PropName,
                                             null,
                                             BamlAttributeUsage.Default);

            WriteAndReleaseRecord(bamlPropertyArrayStart, xamlPropertyArrayStartNode);
        }

        // Write the end of an array property
        internal virtual void WritePropertyArrayEnd(
            XamlPropertyArrayEndNode xamlPropertyArrayEndNode)
        {
            // initialize the element and add to the stack
            BamlPropertyArrayEndRecord bamlPropertyArrayEndRecord =
                (BamlPropertyArrayEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyArrayEnd);

            WriteAndReleaseRecord(bamlPropertyArrayEndRecord, xamlPropertyArrayEndNode);
        }

        // Write the start of a complex property that implements IList
        internal void WritePropertyIListStart(
                XamlPropertyIListStartNode xamlPropertyIListStart)
        {
            // initialize the element and add to the stack
            BamlPropertyIListStartRecord bamlPropertyIListStart =
                (BamlPropertyIListStartRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyIListStart);

            bamlPropertyIListStart.AttributeId =
                MapTable.AddAttributeInfoMap(BinaryWriter,
                                             xamlPropertyIListStart.AssemblyName,
                                             xamlPropertyIListStart.TypeFullName,
                                             xamlPropertyIListStart.PropDeclaringType,
                                             xamlPropertyIListStart.PropName,
                                             null,
                                             BamlAttributeUsage.Default);

            WriteAndReleaseRecord(bamlPropertyIListStart, xamlPropertyIListStart);
        }

        // Write the end of a complex property that implements IList
        internal virtual void WritePropertyIListEnd(
                XamlPropertyIListEndNode xamlPropertyIListEndNode)
        {
            // initialize the element and add to the stack
            BamlPropertyIListEndRecord bamlPropertyIListEndRecord =
                (BamlPropertyIListEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyIListEnd);

            WriteAndReleaseRecord(bamlPropertyIListEndRecord, xamlPropertyIListEndNode);
        }

        // Write the start of a complex property that implements IDictionary
        internal void WritePropertyIDictionaryStart(
                XamlPropertyIDictionaryStartNode xamlPropertyIDictionaryStartNode)
        {
            // initialize the element and add to the stack
            BamlPropertyIDictionaryStartRecord bamlPropertyIDictionaryStart =
                (BamlPropertyIDictionaryStartRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyIDictionaryStart);

            bamlPropertyIDictionaryStart.AttributeId =
                MapTable.AddAttributeInfoMap(BinaryWriter,
                                             xamlPropertyIDictionaryStartNode.AssemblyName,
                                             xamlPropertyIDictionaryStartNode.TypeFullName,
                                             xamlPropertyIDictionaryStartNode.PropDeclaringType,
                                             xamlPropertyIDictionaryStartNode.PropName,
                                             null,
                                             BamlAttributeUsage.Default);

            WriteAndReleaseRecord(bamlPropertyIDictionaryStart, xamlPropertyIDictionaryStartNode);
       }

        // Write the end of a complex property that implements IDictionary
        internal virtual void WritePropertyIDictionaryEnd(
                XamlPropertyIDictionaryEndNode xamlPropertyIDictionaryEndNode)
        {
            // initialize the element and add to the stack
            BamlPropertyIDictionaryEndRecord bamlPropertyIDictionaryEndRecord =
                (BamlPropertyIDictionaryEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.PropertyIDictionaryEnd);

            WriteAndReleaseRecord(bamlPropertyIDictionaryEndRecord, xamlPropertyIDictionaryEndNode);
        }

#if !PBTCOMPILER

        // Write a routed event record
        internal void WriteRoutedEvent(XamlRoutedEventNode xamlRoutedEventNode)
        {
            BamlRoutedEventRecord bamlEvent =
                (BamlRoutedEventRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.RoutedEvent);

            BamlAttributeInfoRecord bamlAttributeInfoRecord;
            MapTable.AddAttributeInfoMap(BinaryWriter,
                                         xamlRoutedEventNode.AssemblyName,
                                         xamlRoutedEventNode.TypeFullName,
                                         null,
                                         xamlRoutedEventNode.EventName,
                                         null,
                                         BamlAttributeUsage.Default,
                                         out bamlAttributeInfoRecord);

            bamlAttributeInfoRecord.Event = xamlRoutedEventNode.Event; // set the table value

            bamlEvent.AttributeId = bamlAttributeInfoRecord.AttributeId;
            bamlEvent.Value = xamlRoutedEventNode.Value;

            WriteAndReleaseRecord(bamlEvent, xamlRoutedEventNode);
        }

#endif

        // Write text content to baml
        internal void WriteText(XamlTextNode xamlTextNode)
        {
            BamlTextRecord bamlText;
            if (xamlTextNode.ConverterType == null)
            {
                if (!InStaticResourceSection && !InDynamicResourceSection)
                {
                    bamlText = (BamlTextRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.Text);
                }
                else
                {
                    bamlText = (BamlTextWithIdRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.TextWithId);

                    // Store the string value in the string table, since these records
                    // are often used as the key for a [Static/Dynamic]Resource.
                    short valueId;
                    if (!MapTable.GetStringInfoId(xamlTextNode.Text,
                                                  out valueId))
                    {
                        valueId = MapTable.AddStringInfoMap(BinaryWriter,
                                                             xamlTextNode.Text);
                    }

                    ((BamlTextWithIdRecord)bamlText).ValueId = valueId;
                }
            }
            else
            {
                bamlText = (BamlTextWithConverterRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.TextWithConverter);

                short typeId;
                string converterAssemblyFullName = xamlTextNode.ConverterType.Assembly.FullName;
                string converterTypeFullName = xamlTextNode.ConverterType.FullName;

                // If we do not already have a type record for the type of this converter,
                // then add a new TypeInfo record to the map table.
                if (!MapTable.GetTypeInfoId(BinaryWriter, converterAssemblyFullName,
                                            converterTypeFullName,
                                            out typeId))
                {
                    typeId = MapTable.AddTypeInfoMap(BinaryWriter,
                                                     converterAssemblyFullName,
                                                     converterTypeFullName,
                                                     xamlTextNode.ConverterType,
                                                     string.Empty,
                                                     string.Empty);
                }
                ((BamlTextWithConverterRecord)bamlText).ConverterTypeId = typeId;
            }

             bamlText.Value = xamlTextNode.Text;

             // up the parent node count, wait to update until endElement.

             // add text to the Tree.
            WriteAndReleaseRecord(bamlText, xamlTextNode);
        }

        // Helper to write out the baml record, with line numbers obtained from
        // the associated xaml node.
        private void WriteAndReleaseRecord(
            BamlRecord    bamlRecord,
            XamlNode      xamlNode)
        {
            int lineNumber = xamlNode != null ? xamlNode.LineNumber : 0;
            int linePosition = xamlNode != null ? xamlNode.LinePosition : 0;

            // If we are currently parsing a deferable content section, then queue
            // up the records for later writing
            if (_deferLoadingSupport && _deferElementDepth > 0)
            {
                if (InStaticResourceSection)
                {
                    // Gather all the BamlRecords within the StaticResource section
                    _staticResourceRecordList.Add(new ValueDeferRecord(bamlRecord, lineNumber, linePosition));
                }
                else
                {
                    ValueDeferRecord deferRec = new ValueDeferRecord(bamlRecord,
                                                                     lineNumber,
                                                                     linePosition);
                    if (_deferEndOfStartReached)
                    {
                        // If we are starting/ending a complex property, and we are at the same
                        // depth as the defered element, then track a mode so that we write to
                        // the _deferElement array instead of the key/value arrays.
                        if(_deferElementDepth == 1 && xamlNode is XamlPropertyComplexStartNode)
                        {
                            _deferComplexPropertyDepth++;
                        }

                        if(_deferComplexPropertyDepth > 0)
                        {
                            _deferElement.Add(deferRec);

                            if(_deferElementDepth == 1 && xamlNode is XamlPropertyComplexEndNode)
                            {
                                _deferComplexPropertyDepth--;
                            }
                        }
                        else if (_deferKeyCollecting)
                        {
                            ((KeyDeferRecord)_deferKeys[_deferKeys.Count-1]).RecordList.Add(deferRec);
                        }
                        else
                        {
                            _deferValues.Add(deferRec);
                        }
                    }
                    else
                    {
                        _deferElement.Add(deferRec);
                    }
                }
            }
            else
            {
                WriteBamlRecord(bamlRecord,
                                lineNumber,
                                linePosition);

                BamlRecordManager.ReleaseWriteRecord(bamlRecord);
            }
        }

        // We've reached the end tag of a deferable content section.  Write out
        // the following information, in order:
        //   1) Start record for deferable content element, and any properties
        //      that are set on that element
        //   2) All keys for keyed content (if this is a dictionary)
        //   3) All value sections.  If this is a dictionary, then go back
        //      and update the positions in the key records to point to the value
        //   4) End record for the deferable content element.
        private void WriteDeferableContent(XamlElementEndNode xamlNode)
        {
            // 1) Write Start record and all property information for the start tag
            for (int h = 0; h<_deferElement.Count; h++)
            {
                ValueDeferRecord deferRecord = (ValueDeferRecord)_deferElement[h];
                WriteBamlRecord(deferRecord.Record,
                                deferRecord.LineNumber,
                                deferRecord.LinePosition);
            }

            // Find where the deferable content starts, which is after the end
            // of the start tag for the deferable element, and insert a deferable
            // block start record here.
            BamlDeferableContentStartRecord bamlDeferableContentStart =
                (BamlDeferableContentStartRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.DeferableContentStart);
            WriteBamlRecord(bamlDeferableContentStart,
                            xamlNode.LineNumber,
                            xamlNode.LinePosition);
            Int64 endOfStart = BinaryWriter.Seek(0, SeekOrigin.Current);

            // 2) Write key collection
            for (int i = 0; i<_deferKeys.Count; i++)
            {
                KeyDeferRecord keyRecord = (KeyDeferRecord)_deferKeys[i];
                // If we don't have a Record stored here, then we didn't find a key
                // for this dictionary entry.  In that case, throw an exception.
                // Otherwise loop through the records if there is a collection, or
                // write out the single record if it is a simple key.
                // NOTE:  Make sure to check the List before the individual record because
                //        the list of records takes precedence over a single record.  It is
                //        possible for a single record to be stored first, and then later a
                //        Key Element in complex property is found which overrides the
                //        original key value.
                if (keyRecord.RecordList != null &&
                    keyRecord.RecordList.Count > 0)
                {
                    for (int j = 0; j < keyRecord.RecordList.Count; j++)
                    {
                        ValueDeferRecord keyValueRec = (ValueDeferRecord)keyRecord.RecordList[j];
                        WriteBamlRecord(keyValueRec.Record,
                                        keyValueRec.LineNumber,
                                        keyValueRec.LinePosition);
                    }
                }
                else
                {
                    if (keyRecord.Record == null)
                    {
                        XamlParser.ThrowException(SRID.ParserNoDictionaryKey,
                                              keyRecord.LineNumber,
                                              keyRecord.LinePosition);
                    }
                    else
                    {
                        WriteBamlRecord(keyRecord.Record,
                                        keyRecord.LineNumber,
                                        keyRecord.LinePosition);
                    }
                }

                // Write out the BamlRecords for all the StaticResources belonging to this key
                List<List<ValueDeferRecord>> staticResourceRecordList = keyRecord.StaticResourceRecordList;
                if (staticResourceRecordList.Count > 0)
                {
                    // Iterate through each one of the StaticResources in the list
                    for (int j=0; j<staticResourceRecordList.Count; j++)
                    {
                        // Iterate through each one of the BamlRecords for a StaticResource
                        List<ValueDeferRecord> srRecords = staticResourceRecordList[j];
                        for (int k=0; k<srRecords.Count; k++)
                        {
                            ValueDeferRecord srRecord = srRecords[k];
                            WriteBamlRecord(srRecord.Record,
                                            srRecord.LineNumber,
                                            srRecord.LinePosition);
                        }
                    }
                }
            }

            // 3) Write Value collection, updating each key to point to the value as
            //    it is encountered.  Note that the value offsets are relative to the
            //    start of the Values section, not the deferable block as a whole.
            Int64 endOfKeys = BinaryWriter.Seek(0, SeekOrigin.Current);
            int keyIndex = 0;
            for (int j = 0; j<_deferValues.Count; j++)
            {
                ValueDeferRecord deferRecord = (ValueDeferRecord)_deferValues[j];
                if (deferRecord.UpdateOffset)
                {
                    KeyDeferRecord deferKeyRecord = (KeyDeferRecord)_deferKeys[keyIndex++];
                    Int64 position = BinaryWriter.Seek(0, SeekOrigin.Current);
                    IBamlDictionaryKey keyRecord;
                    if (deferKeyRecord.RecordList != null &&
                        deferKeyRecord.RecordList.Count > 0)
                    {
                        ValueDeferRecord elementDeferRec = (ValueDeferRecord)(deferKeyRecord.RecordList[0]);
                        keyRecord = (IBamlDictionaryKey)elementDeferRec.Record;
                    }
                    else
                    {
                        keyRecord = (IBamlDictionaryKey)deferKeyRecord.Record;
                    }
                    Debug.Assert(keyRecord != null, "Unknown key record type in defer load dictionary");
                    if (keyRecord != null)
                    {
                        keyRecord.UpdateValuePosition((Int32)(position-endOfKeys), BinaryWriter);
                    }
                }
                WriteBamlRecord(deferRecord.Record,
                                deferRecord.LineNumber,
                                deferRecord.LinePosition);
            }

            Debug.Assert(keyIndex == _deferKeys.Count,
                "Number of keys and values don't match");

            // 4) Write end record and update the content size in start record
            Int64 startOfEnd = BinaryWriter.Seek(0, SeekOrigin.Current);
            bamlDeferableContentStart.UpdateContentSize((Int32)(startOfEnd - endOfStart),
                                           BinaryWriter);

            BamlElementEndRecord bamlElementEnd =
                (BamlElementEndRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.ElementEnd);
            WriteBamlRecord(bamlElementEnd,
                            xamlNode.LineNumber,
                            xamlNode.LinePosition);

            BamlRecordManager.ReleaseWriteRecord(bamlElementEnd);
        }

        /// <summary>
        /// This method is responsible for processing and writing out the StaticResource
        /// records that we have been gathering this far to DeferredContent
        /// </summary>
        private void WriteStaticResource()
        {
            Debug.Assert(_deferElementDepth > 0 && CollectingValues,
                "Special processing of StaticResources happens only when collecting values within a deferred section");

            // Replace the first record in the list with the StaticResource start record
            ValueDeferRecord valueDeferRecord = _staticResourceRecordList[0];
            int lineNumber = valueDeferRecord.LineNumber;
            int linePosition = valueDeferRecord.LinePosition;

            Debug.Assert(valueDeferRecord.Record != null &&
                         valueDeferRecord.Record.RecordType == BamlRecordType.ElementStart &&
                         ((BamlElementStartRecord)valueDeferRecord.Record).TypeId == BamlMapTable.GetKnownTypeIdFromType(KnownTypes.Types[(int)KnownElements.StaticResourceExtension]),
                "The first record in the list must be the ElementStart record for the StaticResourceExtension tag");

            BamlStaticResourceStartRecord bamlStaticResourceStart =
                (BamlStaticResourceStartRecord)BamlRecordManager.GetWriteRecord(BamlRecordType.StaticResourceStart);
            bamlStaticResourceStart.TypeId = ((BamlElementStartRecord)valueDeferRecord.Record).TypeId;
            valueDeferRecord.Record = bamlStaticResourceStart;

            // Replace last record in the list with the StaticResource end record
            valueDeferRecord = _staticResourceRecordList[_staticResourceRecordList.Count-1];
            Debug.Assert(valueDeferRecord.Record != null && valueDeferRecord.Record.RecordType == BamlRecordType.ElementEnd,
                "The last record in the list must be the ElementEnd record for the StaticResourceExtension tag");

            BamlStaticResourceEndRecord bamlStaticResourceEnd =
                (BamlStaticResourceEndRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.StaticResourceEnd);
            valueDeferRecord.Record = bamlStaticResourceEnd;

            // Add the current StaticResource to the list on the current key record
            KeyDeferRecord keyDeferRecord = ((KeyDeferRecord)_deferKeys[_deferKeys.Count-1]);
            keyDeferRecord.StaticResourceRecordList.Add(_staticResourceRecordList);

            // Write a StaticResourceId to the values collection
            BamlStaticResourceIdRecord bamlStaticResourceId =
                (BamlStaticResourceIdRecord) BamlRecordManager.GetWriteRecord(BamlRecordType.StaticResourceId);
            bamlStaticResourceId.StaticResourceId = (short)(keyDeferRecord.StaticResourceRecordList.Count-1);

            _deferValues.Add(new ValueDeferRecord(bamlStaticResourceId, lineNumber, linePosition));
        }

#endregion Record Writing

#endregion Methods

#region Private Classes

        // DeferRecord contains information about a BamlRecord or list of
        // BamlRecords that has not been written to the baml stream yet
        // because we are processing a deferable content element.
        private class DeferRecord
        {
            internal DeferRecord (
                int        lineNumber,
                int        linePosition)
            {
                _lineNumber = lineNumber;
                _linePosition = linePosition;
            }

            internal int LineNumber
            {
                get { return _lineNumber; }
                set { _lineNumber = value; }
            }

            internal int LinePosition
            {
                get { return _linePosition; }
                set { _linePosition = value; }
            }

            private int        _lineNumber;
            private int        _linePosition;
        }

        private class ValueDeferRecord : DeferRecord
        {
            internal ValueDeferRecord (
                BamlRecord record,
                int        lineNumber,
                int        linePosition) : base(lineNumber, linePosition)
            {
                _record = record;
                _updateOffset = false;
            }

            internal BamlRecord Record
            {
                get { return _record; }
                set { _record = value; }
            }

            internal bool UpdateOffset
            {
                get { return _updateOffset; }
                set { _updateOffset = value; }
            }

            private bool       _updateOffset;
            private BamlRecord _record;
        }

        private class KeyDeferRecord : DeferRecord
        {
            internal KeyDeferRecord (
                int        lineNumber,
                int        linePosition) : base(lineNumber, linePosition)
            {
            }

            internal BamlRecord Record
            {
                get { return _record; }
                set { _record = value; }
            }

            internal ArrayList RecordList
            {
                get { return _recordList; }
                set { _recordList = value; }
            }

            internal List<List<ValueDeferRecord>> StaticResourceRecordList
            {
                get
                {
                    if (_staticResourceRecordList == null)
                    {
                        _staticResourceRecordList = new List<List<ValueDeferRecord>>(1);
                    }

                    return _staticResourceRecordList;
                }
            }

            private BamlRecord _record;
            private ArrayList _recordList;
            private List<List<ValueDeferRecord>> _staticResourceRecordList;
        }

#endregion Private Classes

#region Properties

#if PBTCOMPILER

        internal bool InDeferLoadedSection
        {
            get { return _deferElementDepth > 0; }
        }

#endif

        /// <summary>
        /// returns stream that BamlRecordWriter is writing Baml records to
        /// </summary>
        /// <ExternalAPI Inherit="true"/>
        public Stream BamlStream
        {
            get { return _bamlStream; }
        }

        internal BamlBinaryWriter BinaryWriter
        {
            get { return _bamlBinaryWriter; }
        }

        internal BamlMapTable MapTable
        {
            get { return _bamlMapTable ; }
        }

        internal ParserContext ParserContext
        {
            get { return _parserContext ; }
        }

        internal virtual BamlRecordManager BamlRecordManager
        {
            get { return _bamlRecordManager; }
        }

        BamlDocumentStartRecord DocumentStartRecord
        {
            get { return _startDocumentRecord; }
            set {  _startDocumentRecord = value; }
        }

        private bool CollectingValues
        {
            get { return _deferEndOfStartReached && !_deferKeyCollecting && _deferComplexPropertyDepth <= 0; }
        }

        // ITypeDescriptorContext used when running type convertors on serializable
        // DP values.
        ITypeDescriptorContext TypeConvertContext
        {
            get
            {
#if !PBTCOMPILER  // Don't run type converters for compilation
                if (null == _typeConvertContext)
                {
                    _typeConvertContext = new TypeConvertContext(_parserContext);
                }

                return _typeConvertContext;
#else
                _typeConvertContext = null;
                return _typeConvertContext;
#endif
            }
        }

        /// <summary>
        /// Are we currently processing a StaticResource section?
        /// </summary>
        private bool InStaticResourceSection
        {
            get { return _staticResourceElementDepth > 0; }
        }

        /// <summary>
        /// Are we currently processing a DynamicResource section?
        /// </summary>
        private bool InDynamicResourceSection
        {
            get { return _dynamicResourceElementDepth > 0; }
        }

#endregion Properties


#region Data
        XamlTypeMapper          _xamlTypeMapper;
        Stream                  _bamlStream;
        BamlBinaryWriter        _bamlBinaryWriter;

        BamlDocumentStartRecord _startDocumentRecord;
        ParserContext           _parserContext;
        BamlMapTable            _bamlMapTable;
        BamlRecordManager       _bamlRecordManager;
        ITypeDescriptorContext  _typeConvertContext;

        bool                    _deferLoadingSupport;  // true if defer load of ResourceDictionary
                                                       // is enabled.
        int                     _deferElementDepth = 0;

        // True if we are processing a defered content element and we have reached the end
        // end of the start record for the element.  At this point all properties for that
        // element have been collected.
        bool                    _deferEndOfStartReached = false;

        // How deep are we in a complex property of a defered type?
        int                    _deferComplexPropertyDepth = 0;

        // True if we are processing a defered content block and we are collecting all the
        // baml records that make up a single key for the keys section of defered content
        bool                    _deferKeyCollecting = false;

        // List of keys for a defered content section.  Each item is a KeyDeferRecord
        ArrayList               _deferKeys;

        // List of values for a defered content section.  Each item is a ValueDeferRecord
        ArrayList               _deferValues;       // Values in the dictionary

        // List of properties set on an element that is the root of a defered content
        // section.  Each item is a ValueDeferRecord.
        ArrayList               _deferElement;      // Start tag and properties
                                                    // of deferable content

        short                   _staticResourceElementDepth = 0; // Used to identify the StaticResource EndRecord

        short                   _dynamicResourceElementDepth = 0; // Used to identify the DynamicResource EndRecord

        List<ValueDeferRecord>  _staticResourceRecordList;  // List of BamlRecords between the start and end of a StaticResource definition (both ends inclusive).

        bool                    _debugBamlStream;
        int                     _lineNumber;
        int                     _linePosition;
        BamlLineAndPositionRecord  _bamlLineAndPositionRecord;
        BamlLinePositionRecord     _bamlLinePositionRecord;

#endregion Data
    }
}
