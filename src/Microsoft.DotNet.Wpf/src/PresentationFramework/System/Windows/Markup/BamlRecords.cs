// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Contains implementation for specific BamlRecords
*
\***************************************************************************/

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Specialized;
using MS.Internal.IO.Packaging.CompoundFile;

#if !PBTCOMPILER
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using MS.Internal.PresentationFramework; // SafeSecurityHelper
#endif

using System.Runtime.InteropServices;
using MS.Utility;
using MS.Internal;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    // Types of records.  Note that this is a superset of XamlNodeTypes
    internal enum BamlRecordType : byte
    {
        /// <summary>
        /// Unknown Node
        /// </summary>
        // !!BamlRecordManager class relies on Unknown = 0 for initialization
        Unknown = 0,

        /// <summary>
        /// Start Document Node
        /// </summary>
        DocumentStart,              // 1

        /// <summary>
        /// End Document Node
        /// </summary>
        DocumentEnd,                // 2

        /// <summary>
        /// Start Element Node, which may be a CLR object or a DependencyObject
        /// </summary>
        ElementStart,               // 3

        /// <summary>
        /// End Element Node
        /// </summary>
        ElementEnd,                 // 4

        /// <summary>
        /// Property Node, which may be a CLR property or a DependencyProperty
        /// </summary>
        Property,                   // 5

        /// <summary>
        /// Binary serialization of a property
        /// </summary>
        PropertyCustom,             // 6

        /// <summary>
        /// Complex Property Node
        /// </summary>
        PropertyComplexStart,       // 7

        /// <summary>
        /// End Complex Property Node
        /// </summary>
        PropertyComplexEnd,         // 8

        /// <summary>
        /// Start Array Property Node
        /// </summary>
        PropertyArrayStart,         // 9

        /// <summary>
        /// End Array Property Node
        /// </summary>
        PropertyArrayEnd,           // 10

        /// <summary>
        /// Star IList Property Node
        /// </summary>
        PropertyIListStart,         // 11

        /// <summary>
        /// End PropertyIListStart Node
        /// </summary>
        PropertyIListEnd,           // 12

        /// <summary>
        /// Start IDictionary Property Node
        /// </summary>
        PropertyIDictionaryStart,   // 13

        /// <summary>
        /// End IDictionary Property Node
        /// </summary>
        PropertyIDictionaryEnd,     // 14

        /// <summary>
        /// LiteralContent Node
        /// </summary>
        LiteralContent,             // 15

        /// <summary>
        /// Text Node
        /// </summary>
        Text,                       // 16

        /// <summary>
        /// Text that has an associated custom typeconverter
        /// </summary>
        TextWithConverter,          // 17

        /// <summary>
        /// RoutedEventNode
        /// </summary>
        RoutedEvent,                // 18

        /// <summary>
        /// ClrEvent Node
        /// </summary>
        ClrEvent,                   // 19

        /// <summary>
        /// XmlnsProperty Node
        /// </summary>
        XmlnsProperty,              // 20

        /// <summary>
        /// XmlAttribute Node
        /// </summary>
        XmlAttribute,               // 21

        /// <summary>
        /// Processing Intstruction Node
        /// </summary>
        ProcessingInstruction,      // 22

        /// <summary>
        /// Comment Node
        /// </summary>
        Comment,                    // 23

        /// <summary>
        /// DefTag Node
        /// </summary>
        DefTag,                     // 24

        /// <summary>
        /// x:name="value" attribute.  One typical use of this
        /// attribute is to define a key to use when inserting an item into an IDictionary
        /// </summary>
        DefAttribute,               // 25

        /// <summary>
        /// EndAttributes Node
        /// </summary>
        EndAttributes,              // 26

        /// <summary>
        /// PI xml - clr namespace mapping
        /// </summary>
        PIMapping,                  // 27

        /// <summary>
        /// Assembly information
        /// </summary>
        AssemblyInfo,               // 28

        /// <summary>
        /// Type information
        /// </summary>
        TypeInfo,                   // 29

        /// <summary>
        /// Type information for a Type that has an associated custom serializer
        /// </summary>
        TypeSerializerInfo,         // 30

        /// <summary>
        /// Attribute (eg - properties and events) information
        /// </summary>
        AttributeInfo,              // 31

        /// <summary>
        /// Resource information
        /// </summary>
        StringInfo,                 // 32

        /// <summary>
        /// Property Resource Reference
        /// </summary>
        PropertyStringReference,    // 33

        /// <summary>
        /// Record for setting a property to a Type reference.  This is used for
        /// properties that are of type "Type"
        /// </summary>
        PropertyTypeReference,      // 34

        /// <summary>
        /// Property that has a simple MarkupExtension value.
        /// </summary>
        PropertyWithExtension,      // 35

        /// <summary>
        /// Property that has an associated custom typeconverter
        /// </summary>
        PropertyWithConverter,      // 36

        /// <summary>
        /// Start a deferable content block
        /// </summary>
        DeferableContentStart,      // 37

        /// <summary>
        /// x:name="value" attribute when used within a defer load
        /// dictionary.  These keys are hoisted to the front of the dictionary when
        /// written to baml.
        /// </summary>
        DefAttributeKeyString,      // 38

        /// <summary>
        /// Implied key that is a Type attribute when used within a defer load
        /// dictionary.  These keys are hoisted to the front of the dictionary when
        /// written to baml.
        /// </summary>
        DefAttributeKeyType,        // 39

        /// <summary>
        /// This marks the start of an element tree that is used as the key in
        /// an IDictionary.
        /// </summary>
        KeyElementStart,            // 40


        /// <summary>
        /// This marks the end of an element tree that is used as the key in
        /// an IDictionary.
        /// </summary>
        KeyElementEnd,              // 41

        /// <summary>
        /// Record marks the start of a section containing constructor parameters
        /// </summary>
        ConstructorParametersStart, // 42

        /// <summary>
        /// Record marks the end of a section containing constructor parameters
        /// </summary>
        ConstructorParametersEnd,   // 43

        /// <summary>
        /// Constructor parameter that has been resolved to a Type.
        /// </summary>
        ConstructorParameterType,   // 44

        /// <summary>
        /// Record that has info about which event or id to connect to in an object tree.
        /// </summary>
        ConnectionId,               // 45

        /// <summary>
        /// Record that set the conntent property context for the element
        /// </summary>
        ContentProperty,            // 46

        /// <summary>
        /// ElementStartRecord that also carries an element name.
        /// </summary>
        NamedElementStart,          // 47

        /// <summary>
        /// Start of StaticResourceExtension within the header of a deferred section.
        /// </summary>
        StaticResourceStart,        // 48

        /// <summary>
        /// End of a StaticResourceExtension within the header of a deferred section.
        /// </summary>
        StaticResourceEnd,          // 49

        /// <summary>
        /// BamlRecord that carries an identifier for a StaticResourceExtension 
        /// within the header of a deferred section.
        /// </summary>
        StaticResourceId,           // 50

        /// <summary>
        /// This is a TextRecord that holds an Id for the String value it represents.
        /// </summary>
        TextWithId,                 // 51

        /// <summary>
        /// PresentationOptions:Freeze="value" attribute. Used for ignorable
        /// WPF-specific parsing options
        /// </summary>
        PresentationOptionsAttribute, // 52

        /// <summary>
        /// Debugging information record that holds the source XAML linenumber.
        /// </summary>
        LineNumberAndPosition,      // 53

        /// <summary>
        /// Debugging information record that holds the source XAML line position.
        /// </summary>
        LinePosition,               // 54

        /// <summary>
        /// OptimizedStaticResourceExtension within the header of a deferred section.
        /// </summary>
        OptimizedStaticResource,      // 55

        /// <summary>
        /// BamlPropertyRecord that carries an identifier for a StaticResourceExtension 
        /// within the header of a deferred section.
        /// </summary>
        PropertyWithStaticResourceId, // 56

        /// <summary>
        /// Placeholder to mark last record
        /// </summary>
        LastRecordType
    }

    /// <summary>
    /// Some attributes have special usages or cause additional actions when they
    /// are set on an element.  This can be have some other effects
    /// such as setting the xml:lang or xml:space values in the parser context.
    /// The PropertyUsage describes addition effects or usage for this property.
    /// </summary>
    internal enum BamlAttributeUsage : short
    {
        /// <summary> A regular property that has no other use </summary>
        Default = 0,

        /// <summary> A property that has xml:lang information </summary>
        XmlLang,

        /// <summary> A property that has xml:space information </summary>
        XmlSpace,

        /// <summary> A property that has the RuntimeIdProperty information </summary>
        RuntimeName,
    }

    // This class handles allocation, read and write management of baml records.
    internal class BamlRecordManager
    {
#if !PBTCOMPILER
        // Genericaly load and create the proper class.
        // This method assumes the seek pointer has already moved passed the recordType
        // field and is at the RecordSize or record contents (depending on record).
        // This method is used so the caller can first read the type of record, and expects
        // to get back the entire record, or nothing (for async support).
        internal BamlRecord ReadNextRecord(
            BinaryReader   bamlBinaryReader,
            long           bytesAvailable,
            BamlRecordType recordType)
        {
            BamlRecord bamlRecord; // = null

            // Create the proper BamlRecord based on the recordType.  The assembly,
            // type and attribute records are created every time, since they are
            // used by the BamlMapTable.  The other records are re-used, so they
            // are created once and cached.
            switch(recordType)
            {
                case BamlRecordType.AssemblyInfo:
                    bamlRecord = new BamlAssemblyInfoRecord();
                    break;
                case BamlRecordType.TypeInfo:
                    bamlRecord = new BamlTypeInfoRecord();
                    break;
                case BamlRecordType.TypeSerializerInfo:
                    bamlRecord = new BamlTypeInfoWithSerializerRecord();
                    break;
                case BamlRecordType.AttributeInfo:
                    bamlRecord = new BamlAttributeInfoRecord();
                    break;
                case BamlRecordType.StringInfo:
                    bamlRecord = new BamlStringInfoRecord();
                    break;
                case BamlRecordType.DefAttributeKeyString:
                    bamlRecord = new BamlDefAttributeKeyStringRecord();
                    break;
                case BamlRecordType.DefAttributeKeyType:
                    bamlRecord = new BamlDefAttributeKeyTypeRecord();
                    break;
                case BamlRecordType.KeyElementStart:
                    bamlRecord = new BamlKeyElementStartRecord();
                    break;

                default:

                    // Get the current record from the cache.  If there's nothing there yet,
                    // or if what is there is pinned, then create one.  Note that records in the
                    // read cache are implicitly recycled, and records in the write cache are explicitly
                    // recycled (i.e., there's a ReleaseWriteRecord, but no ReleaseReadRecord).

                    bamlRecord = _readCache[(int)recordType];
                    if (null == bamlRecord || bamlRecord.IsPinned )
                    {
                        bamlRecord = _readCache[(int)recordType] = AllocateRecord(recordType);
                    }

                    break;
            }

            bamlRecord.Next = null;

            if (null != bamlRecord)
            {
                // If LoadRecordSize indicates it can determine the record size
                // and has determined that there is enough content to load the
                // entire record, then continue.
                if (bamlRecord.LoadRecordSize(bamlBinaryReader, bytesAvailable) &&
                    bytesAvailable >= bamlRecord.RecordSize)
                {
                    bamlRecord.LoadRecordData(bamlBinaryReader);
                }
                else
                {
                    bamlRecord = null;
                }
            }

            return bamlRecord;
        }

        /// <summary>
        /// Return the object if it should be treated as IAddChild, otherwise return null
        /// </summary>
        static internal IAddChild AsIAddChild(object obj)
        {
            IAddChild iac = obj as IAddChildInternal;
            return iac;
        }
#endif

        /// <summary>
        /// True if type should be treated as IAddChild
        /// </summary>
        static internal bool TreatAsIAddChild(Type parentObjectType)
        {
            return (KnownTypes.Types[(int)KnownElements.IAddChildInternal].IsAssignableFrom( parentObjectType ));
        }

        static internal BamlRecordType GetPropertyStartRecordType(Type propertyType, bool propertyCanWrite)
        {
            BamlRecordType recordType;
            if (propertyType.IsArray)
            {
                recordType = BamlRecordType.PropertyArrayStart;
            }
#if PBTCOMPILER
            else if (ReflectionHelper.GetMscorlibType(typeof(IDictionary)).IsAssignableFrom(propertyType))
            {
                recordType = BamlRecordType.PropertyIDictionaryStart;
            }
            else if (ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(propertyType) ||
               BamlRecordManager.TreatAsIAddChild(propertyType) ||
               (ReflectionHelper.GetMscorlibType(typeof(IEnumerable)).IsAssignableFrom(propertyType) && !propertyCanWrite))
#else
            else if (typeof(IDictionary).IsAssignableFrom(propertyType))
            {
                recordType = BamlRecordType.PropertyIDictionaryStart;
            }
            else if ((typeof(IList).IsAssignableFrom(propertyType) ||
                       BamlRecordManager.TreatAsIAddChild(propertyType) ||
                       (typeof(IEnumerable).IsAssignableFrom(propertyType) && !propertyCanWrite)))
#endif
            {
                // we're a list if:
                // 1) the property type is an IList.
                // 2) the property type is an IAddChild (internal).
                // 3) the property type is an IEnumerable and read-only and the parent is an IAddChild (internal).
                // for the third case, we can't check the parent until run-time.
                recordType = BamlRecordType.PropertyIListStart;
            }
            else
            {
                recordType = BamlRecordType.PropertyComplexStart;
            }

            return recordType;
        }

#if !PBTCOMPILER
        internal BamlRecord CloneRecord(BamlRecord record)
        {
            BamlRecord newRecord;
            
            switch (record.RecordType)
            {
                case BamlRecordType.ElementStart:
                    if (record is BamlNamedElementStartRecord)
                    {
                        newRecord= new BamlNamedElementStartRecord();
                    }
                    else
                    {
                        newRecord = new BamlElementStartRecord();
                    }
                    break;
                    
                case BamlRecordType.PropertyCustom:
                    if (record is BamlPropertyCustomWriteInfoRecord)
                    {
                        newRecord = new BamlPropertyCustomWriteInfoRecord();
                    }
                    else
                    {
                        newRecord = new BamlPropertyCustomRecord();
                    }
                    break;
            
                default:
                    newRecord = AllocateRecord(record.RecordType);
                    break;
            }

            record.Copy(newRecord);
            
            return newRecord;
        }
#endif

        // Helper function to create a BamlRecord from a BamlRecordType
        private BamlRecord AllocateWriteRecord(BamlRecordType recordType)
        {
            BamlRecord record;

            switch (recordType)
            {
                case BamlRecordType.PropertyCustom:
                    record = new BamlPropertyCustomWriteInfoRecord();
                    break;

                default:
                    record = AllocateRecord(recordType);
                    break;
            }

            return record;
        }

        // Helper function to create a BamlRecord from a BamlRecordType
        private BamlRecord AllocateRecord(BamlRecordType recordType)
        {
            BamlRecord record;

            switch(recordType)
            {
                case BamlRecordType.DocumentStart:
                    record = new BamlDocumentStartRecord();
                    break;
                case BamlRecordType.DocumentEnd:
                    record = new BamlDocumentEndRecord();
                    break;
                case BamlRecordType.ConnectionId:
                    record = new BamlConnectionIdRecord();
                    break;
                case BamlRecordType.ElementStart:
                    record = new BamlElementStartRecord();
                    break;
                case BamlRecordType.ElementEnd:
                    record = new BamlElementEndRecord();
                    break;
                case BamlRecordType.DeferableContentStart:
                    record = new BamlDeferableContentStartRecord();
                    break;
                case BamlRecordType.DefAttributeKeyString:
                    record = new BamlDefAttributeKeyStringRecord();
                    break;
                case BamlRecordType.DefAttributeKeyType:
                    record = new BamlDefAttributeKeyTypeRecord();
                    break;
                case BamlRecordType.LiteralContent:
                    record = new BamlLiteralContentRecord();
                    break;
                case BamlRecordType.Property:
                    record = new BamlPropertyRecord();
                    break;
                case BamlRecordType.PropertyWithConverter:
                    record = new BamlPropertyWithConverterRecord();
                    break;
                case BamlRecordType.PropertyStringReference:
                    record = new BamlPropertyStringReferenceRecord();
                    break;
                case BamlRecordType.PropertyTypeReference:
                    record = new BamlPropertyTypeReferenceRecord();
                    break;
                case BamlRecordType.PropertyWithExtension:
                    record = new BamlPropertyWithExtensionRecord();
                    break;
                case BamlRecordType.PropertyCustom:
                    record = new BamlPropertyCustomRecord();
                    break;
                case BamlRecordType.PropertyComplexStart:
                    record = new BamlPropertyComplexStartRecord();
                    break;
                case BamlRecordType.PropertyComplexEnd:
                    record = new BamlPropertyComplexEndRecord();
                    break;
                case BamlRecordType.RoutedEvent:
                    record = new BamlRoutedEventRecord();
                    break;
                case BamlRecordType.PropertyArrayStart:
                    record = new BamlPropertyArrayStartRecord();
                    break;
                case BamlRecordType.PropertyArrayEnd:
                    record = new BamlPropertyArrayEndRecord();
                    break;
                case BamlRecordType.PropertyIListStart:
                    record = new BamlPropertyIListStartRecord();
                    break;
                case BamlRecordType.PropertyIListEnd:
                    record = new BamlPropertyIListEndRecord();
                    break;
                case BamlRecordType.PropertyIDictionaryStart:
                    record = new BamlPropertyIDictionaryStartRecord();
                    break;
                case BamlRecordType.PropertyIDictionaryEnd:
                    record = new BamlPropertyIDictionaryEndRecord();
                    break;
                case BamlRecordType.Text:
                    record = new BamlTextRecord();
                    break;
                case BamlRecordType.TextWithConverter:
                    record = new BamlTextWithConverterRecord();
                    break;
                case BamlRecordType.TextWithId:
                    record = new BamlTextWithIdRecord();
                    break;
                case BamlRecordType.XmlnsProperty:
                    record = new BamlXmlnsPropertyRecord();
                    break;
                case BamlRecordType.PIMapping:
                    record = new BamlPIMappingRecord();
                    break;
                case BamlRecordType.DefAttribute:
                    record = new BamlDefAttributeRecord();
                    break;
                case BamlRecordType.PresentationOptionsAttribute:
                    record = new BamlPresentationOptionsAttributeRecord();
                    break;                    
                case BamlRecordType.KeyElementStart:
                    record = new BamlKeyElementStartRecord();
                    break;
                case BamlRecordType.KeyElementEnd:
                    record = new BamlKeyElementEndRecord();
                    break;
                case BamlRecordType.ConstructorParametersStart:
                    record = new BamlConstructorParametersStartRecord();
                    break;
                case BamlRecordType.ConstructorParametersEnd:
                    record = new BamlConstructorParametersEndRecord();
                    break;
                case BamlRecordType.ConstructorParameterType:
                    record = new BamlConstructorParameterTypeRecord();
                    break;
                case BamlRecordType.ContentProperty:
                    record = new BamlContentPropertyRecord();
                    break;
                case BamlRecordType.AssemblyInfo:
                case BamlRecordType.TypeInfo:
                case BamlRecordType.TypeSerializerInfo:
                case BamlRecordType.AttributeInfo:
                case BamlRecordType.StringInfo:
                    Debug.Assert(false,"Assembly, Type and Attribute records are not cached, so don't ask for one.");
                    record = null;
                    break;
                case BamlRecordType.StaticResourceStart:
                    record = new BamlStaticResourceStartRecord();
                    break;
                case BamlRecordType.StaticResourceEnd:
                    record = new BamlStaticResourceEndRecord();
                    break;
                case BamlRecordType.StaticResourceId:
                    record = new BamlStaticResourceIdRecord();
                    break;
                case BamlRecordType.LineNumberAndPosition:
                    record = new BamlLineAndPositionRecord();
                    break;
                case BamlRecordType.LinePosition:
                    record = new BamlLinePositionRecord();
                    break;
                case BamlRecordType.OptimizedStaticResource:
                    record = new BamlOptimizedStaticResourceRecord();
                    break;
                case BamlRecordType.PropertyWithStaticResourceId:
                    record = new BamlPropertyWithStaticResourceIdRecord();
                    break;
                default:
                    Debug.Assert(false,"Unknown RecordType");
                    record = null;
                    break;
            }

            return record;
        }

        // This should only be called from BamlRecordWriter -- it gets a record from the record
        // cache that must be freed with ReleaseRecord before GetRecord is called again.
        internal BamlRecord GetWriteRecord(BamlRecordType recordType)
        {
            // Create the cache of records used in writing, on demand

            if( _writeCache == null )
            {
                _writeCache = new BamlRecord[(int)BamlRecordType.LastRecordType];
            }

            BamlRecord record = _writeCache[(int)recordType];
            if (null == record)
            {
                record = AllocateWriteRecord(recordType);
            }
            else
            {
                _writeCache[(int)recordType] = null;
            }

            // It is important to set RecordSize for variable size records
            // to a negative number to indicate that it has not been set yet.
            // Fixed size records should ignore this set.
            record.RecordSize = -1;
            return record;
        }


        //+---------------------------------------------------------------------------------------------
        //
        //  ReleaseWriteRecord
        //
        //  Frees a record originally claimed with GetWriteRecord. Note that records in the
        //  read cache are implicitly recycled, and records in the write cache are explicitly
        //  recycled (i.e., there's a ReleaseWriteRecord, but no ReleaseReadRecord).
        //
        //+---------------------------------------------------------------------------------------------

        internal void ReleaseWriteRecord(BamlRecord record)
        {
            // Put the write record back into the cache, if we're allowed to recycle it.

            if( !record.IsPinned )
            {
                Debug.Assert(null == _writeCache[(int)record.RecordType]);
                if (null != _writeCache[(int)record.RecordType])
                {
                    // This is really an internal error.
                    throw new InvalidOperationException(SR.Get(SRID.ParserMultiBamls));
                }
                _writeCache[(int)record.RecordType] = record;
            }
        }


        // Cache of BamlRecords, used during read, to avoid lots of records from being
        // created.  If a record gets pinned (BamlRecord.IsPinned gets set), it is not re-used.

        #if !PBTCOMPILER
        BamlRecord[] _readCache = new BamlRecord[(int)BamlRecordType.LastRecordType];
        #endif

        // Cache of BamlRecords, used during write, also to avoid lots of records
        // from being created.

        BamlRecord[] _writeCache = null; //new BamlRecord[(int)BamlRecordType.LastRecordType];
    }

    // The base of all baml records.  This gives a fixed size record that contains
    // line number information used for generating error messages.  Note that the
    // line number information is not currently written out to the baml stream.
    internal abstract class BamlRecord
    {
 #region Methods

#if !PBTCOMPILER
        // If there are enough bytes available, load the record size from the
        // binary reader.  For fixed size records that derive from BamlRecord,
        // there is no size field in the baml file, so this always succeeds.
        internal virtual bool LoadRecordSize(
            BinaryReader bamlBinaryReader,
            long         bytesAvailable)
        {
            return true;
        }

        // Load record data.  This does not include the record type, or the
        // size field, which are loaded separately.  If the subclass has no
        // specific data to load, then don't override this.
        internal virtual void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
        }
#endif

        // Writes data at the current position seek pointer points
        // to byte after the end of record when done.
        internal virtual void Write(BinaryWriter bamlBinaryWriter)
        {
            // BamlRecords may be used without a stream, so if you attempt to write when there
            // isn't a writer, just ignore it.
            if (bamlBinaryWriter == null)
            {
                return;
            }

            // Baml records always start with record type
            bamlBinaryWriter.Write((byte) RecordType);

            // IMPORTANT:  The RecordType is the last thing written before calling
            //             WriteRecordData.  Some records assume the record type is located
            //             directly before the current stream location and may change it, so
            //             don't change where the record type is written in the stream!!!
            //             Paint is one example of a DP object that will seek back to change
            //             the record type if it is unable to serialize itself.
            WriteRecordData(bamlBinaryWriter);
        }

        // Write contents of the record, excluding size (if any) and record type.
        // If the subclass has no specific data to write out, don't override this.
        internal virtual void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
        }


#endregion Methods

#region Properties

        // Actual size of the complete BamlRecord (excluding RecordType) in bytes.
        // Currently limited to 2 gigabytes.  Default size is 0 bytes of data.
        // Subclasses must override if they have a different size.
        internal virtual Int32 RecordSize
        {
            get { return 0; }
            set { Debug.Assert (value == -1, "Setting fixed record to an invalid size"); }
        }

        // Identifies the type off BAML record.  This is used when casting to
        // a BamlRecord subclass.  All subclasses **MUST** override this.
        internal virtual BamlRecordType RecordType
        {
            get
            {
                Debug.Assert(false, "Must override RecordType");
                return BamlRecordType.Unknown;
            }
        }


#if !PBTCOMPILER
        // Next Record pointer - used in BamlObjectFactory
        internal BamlRecord Next
        {
            get { return _nextRecord; }
            set { _nextRecord = value ; }
        }
#endif

        // The BamlRecorManager keeps a cache of baml records and tries to reuse them automatically.
        // To keep a record from being cached, it can be pinned.  For correct pinning we keep a
        // pin count.  To save working set, we only have two bits for the reference count.
        // So if the reference count reaches three the record becomes permanently pinned.

        internal bool IsPinned
        {
            get
            {
                return PinnedCount > 0;
            }
        }

        // (See comment on IsPinned.)
        internal int PinnedCount
        {
            get
            {
                return _flags[_pinnedFlagSection];
            }

            set
            {
                Debug.Assert( value <= 3 && value >= 0 );
                _flags[_pinnedFlagSection] = value;
            }
        }

        // (See comment on IsPinned.)
        internal void Pin()
        {
            if( PinnedCount < 3 )
            {
                ++PinnedCount;
            }
        }

#if !PBTCOMPILER        
        // (See comment on IsPinned.)
        internal void Unpin()
        {
            if( PinnedCount < 3 )
            {
                --PinnedCount;
            }
        }

        internal virtual void Copy(BamlRecord record)
        {
            record._flags = _flags;
            record._nextRecord = _nextRecord;
        }
        
#endif



#endregion Properties

#region Data

        // Internal flags for efficient storage
        // NOTE: bits here are used by sub-classes also.
        // This BitVector32 field is shared by subclasses to save working set.  Sharing flags like this
        // is easier in e.g. FrameworkElement, where the class hierarchy is linear, but can be bug-prone otherwise.  To make the 
        // code less fragile, each class abstractly provides it's last section to subclasses(LastFlagsSection), which they can
        // use in their call to CreateSection.

        internal BitVector32 _flags;

        // Allocate space in _flags.

        private static BitVector32.Section _pinnedFlagSection = BitVector32.CreateSection( 3 /* Allocates two bits to store values up to 3 */ );

        // This provides subclasses with a referece section to create their own section.
        internal static BitVector32.Section LastFlagsSection
        {
            get { return _pinnedFlagSection; }
        }


#if !PBTCOMPILER
        private BamlRecord _nextRecord = null;
#endif


        // Size of the record type field in the baml file.
        internal const int RecordTypeFieldLength = 1;

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", RecordType);
        }

        protected static string GetTypeName(int typeId)
        {
            string typeName = typeId.ToString(CultureInfo.InvariantCulture);
            if(typeId < 0)
            {
                KnownElements elm = (KnownElements)(-typeId);
                typeName = elm.ToString();
            }
            return typeName;
        }


        // This helper checks for records that indicate that you're out of
        // an element start, and into it's "content" (in the xml sense).
        // We have to infer this, because unlike Xml, Baml doesn't provide
        // an end-attributes record.

        internal static bool IsContentRecord( BamlRecordType bamlRecordType )
        {
            return bamlRecordType == BamlRecordType.PropertyComplexStart
                   ||
                   bamlRecordType == BamlRecordType.PropertyArrayStart
                   ||
                   bamlRecordType == BamlRecordType.PropertyIListStart
                   ||
                   bamlRecordType == BamlRecordType.PropertyIDictionaryStart
                   ||
                   bamlRecordType == BamlRecordType.Text;
        }

#endif

#endregion Data
    }

    // An abstract base class for records that record their size as part of the
    // baml stream.
    internal abstract class BamlVariableSizedRecord : BamlRecord
    {
 #region Methods

#if !PBTCOMPILER
        // If there are enough bytes available, load the record size from the
        // binary reader.  The default action is to load the 4 byte size from
        // the reader, if there are at least 4 bytes available.
        internal override bool LoadRecordSize(
            BinaryReader bamlBinaryReader,
            long         bytesAvailable)
        {
           int recordSize;
           bool loadedSize = LoadVariableRecordSize(bamlBinaryReader, bytesAvailable, out recordSize);
           if (loadedSize)
           {
               RecordSize = recordSize;
           }
           return loadedSize;
        }

        // If there are enough bytes available, load the record size from the
        // binary reader.  The default action is to load the 4 byte size from
        // the reader, if there are at least 4 bytes available.
        internal static bool LoadVariableRecordSize(
                BinaryReader bamlBinaryReader,
                long         bytesAvailable,
            out int          recordSize)
        {
            if (bytesAvailable >= MaxRecordSizeFieldLength)
            {
                recordSize = ((BamlBinaryReader)bamlBinaryReader).Read7BitEncodedInt();
                return true;
            }
            else
            {
                recordSize = -1;
                return false;
            }
        }
#endif

        protected int ComputeSizeOfVariableLengthRecord(long start, long end)
        {
            int size = (Int32)(end - start);
            int sizeOfSize = BamlBinaryWriter.SizeOf7bitEncodedSize(size);
            sizeOfSize = BamlBinaryWriter.SizeOf7bitEncodedSize(sizeOfSize+size);
            return (sizeOfSize+size);
        }

        // Writes data at the current position seek pointer points
        // to byte after the end of record when done.
        internal override void Write(BinaryWriter bamlBinaryWriter)
        {
            // BamlRecords may be used without a stream, so if you attempt to write when there
            // isn't a writer, just ignore it.
            if (bamlBinaryWriter == null)
            {
                return;
            }


            // Baml records always start with record type
            bamlBinaryWriter.Write((byte) RecordType);

            // Remember the file location of this baml record.  This
            // is needed if we have to come back later to update the sync mode.
            // IMPORTANT:  The RecordType is the last thing written before calling
            //             WriteRecordData.  Some records assume the record type is located
            //             directly before the current stream location and may change it, so
            //             don't change where the record type is written in the stream!!!
            //             Paint is one example of a DP object that will seek back to change
            //             the record type if it is unable to serialize itself.

            //  Write just the data, this is just to measure the size.
            long startSeekPosition = bamlBinaryWriter.Seek(0,SeekOrigin.Current);
            WriteRecordData(bamlBinaryWriter);
            long endSeekPosition = bamlBinaryWriter.Seek(0,SeekOrigin.Current);

            Debug.Assert(RecordSize < 0);
            RecordSize = ComputeSizeOfVariableLengthRecord(startSeekPosition, endSeekPosition);

            // seek back to the begining,  this time write the size, then the data.
            bamlBinaryWriter.Seek((int)startSeekPosition, SeekOrigin.Begin);
            WriteRecordSize(bamlBinaryWriter);
            WriteRecordData(bamlBinaryWriter);
        }

        // Write the size of this record.  The default action is to write the 4 byte
        // size, which may be overwritten later once WriteRecordData has been called.
        internal void WriteRecordSize(BinaryWriter bamlBinaryWriter)
        {
            ((BamlBinaryWriter)bamlBinaryWriter).Write7BitEncodedInt(RecordSize);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlVariableSizedRecord newRecord = (BamlVariableSizedRecord)record;
            newRecord._recordSize = _recordSize;
        }
#endif
        
#endregion Methods

#region Properties

        // Actual size of the complete BamlRecord in bytes.  Currently
        // limited to 2 gigabytes.
        internal override Int32 RecordSize
        {
            get { return _recordSize; }
            set { _recordSize = value; }
        }

        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return BamlRecord.LastFlagsSection; }
        }


#endregion Properties

#region Data

        // Size of the RecordSize field in the baml file.  This must be in
        // sync the type type of _recordSize below.
        internal const int MaxRecordSizeFieldLength = 4;

        Int32          _recordSize = -1;   // we use a 7 bit encoded variable size

#endregion Data
    }

    internal class BamlXmlnsPropertyRecord : BamlVariableSizedRecord
    {
#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            Prefix  =  bamlBinaryReader.ReadString();
            XmlNamespace  =   bamlBinaryReader.ReadString();

            short count = bamlBinaryReader.ReadInt16();

            if (count > 0)
            {
                AssemblyIds = new short[count];

                for (short i = 0; i < count; i++)
                {
                    AssemblyIds[i] = bamlBinaryReader.ReadInt16();
                }
            }
            else
            {
                AssemblyIds = null;
            }
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(Prefix);
            bamlBinaryWriter.Write(XmlNamespace);

            // Write the AssemblyIds which contain XmlnsDefinitionAttribute
            // for this xmlns Uri.
            // The format should be CountN Id1 Id2 ... IdN
            //
            short count = 0;

            if (AssemblyIds != null && AssemblyIds.Length > 0)
            {
                count = (short) AssemblyIds.Length;
            }

            bamlBinaryWriter.Write(count);

            if (count > 0)
            {
                for (short i = 0; i < count; i++)
                {
                    bamlBinaryWriter.Write(AssemblyIds[i]);
                }
            }
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlXmlnsPropertyRecord newRecord = (BamlXmlnsPropertyRecord)record;
            newRecord._prefix = _prefix;
            newRecord._xmlNamespace = _xmlNamespace;
            newRecord._assemblyIds = _assemblyIds;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.XmlnsProperty; }
        }

        internal string Prefix
        {
            get { return _prefix; }
            set {_prefix = value; }
        }

        internal string XmlNamespace
        {
            get { return _xmlNamespace; }
            set { _xmlNamespace = value; }
        }

        internal short[] AssemblyIds
        {
            get { return _assemblyIds; }
            set { _assemblyIds = value; }
        }

#endregion Properties

#region Data

        string _prefix;
        string _xmlNamespace;
        short[] _assemblyIds;

#endregion Data
    }

    internal class BamlPIMappingRecord : BamlVariableSizedRecord
    {
#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            XmlNamespace  =  bamlBinaryReader.ReadString();
            ClrNamespace  =  bamlBinaryReader.ReadString();
            AssemblyId    =  bamlBinaryReader.ReadInt16();
        }
#endif
        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            // write out an int for record size but we'll go back and fill
            bamlBinaryWriter.Write(XmlNamespace);
            bamlBinaryWriter.Write(ClrNamespace);
            bamlBinaryWriter.Write(AssemblyId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPIMappingRecord newRecord = (BamlPIMappingRecord)record;
            newRecord._xmlns = _xmlns;
            newRecord._clrns = _clrns;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PIMapping; }
        }

        internal string XmlNamespace
        {
            get { return _xmlns; }
            set {_xmlns = value; }
        }

        internal string ClrNamespace
        {
            get { return _clrns; }
            set { _clrns = value; }
        }

        internal short AssemblyId
        {
            get
            {
                short value = (short) _flags[_assemblyIdLowSection];
                value |= (short) (_flags[_assemblyIdHighSection] << 8);

                return value;
            }

            set
            {
                _flags[_assemblyIdLowSection] = (short)  (value & 0xff);
                _flags[_assemblyIdHighSection] = (short) ((value & 0xff00) >> 8);
            }
        }

        // Allocate space in _flags.
        // BitVector32 doesn't support 16 bit sections, so we have to break
        // it up into 2 sections.

        private static BitVector32.Section _assemblyIdLowSection
            = BitVector32.CreateSection( (short)0xff, BamlVariableSizedRecord.LastFlagsSection );
        
        private static BitVector32.Section _assemblyIdHighSection
            = BitVector32.CreateSection( (short)0xff, _assemblyIdLowSection );
        
#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _assemblyIdHighSection; }
        }
#endif



#endregion Properties

#region Data
        string _xmlns;
        string _clrns;
#endregion Data

    }

    // Common base class for variables sized records that contain a string value
    internal abstract class BamlStringValueRecord : BamlVariableSizedRecord
    {
#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            Value  =  bamlBinaryReader.ReadString();
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(Value);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlStringValueRecord newRecord = (BamlStringValueRecord)record;
            newRecord._value = _value;
        }
#endif

#endregion Methods

#region Properties

        internal string Value
        {
            get { return _value; }
            set { _value = value; }
        }

#endregion Properties

#region Data
        string _value;
#endregion Data

    }

    // Common methods for baml records that serve as keys in a dictionary.
    internal interface IBamlDictionaryKey
    {
        // Update the pointer to the Value that was written out when WriteRecordData
        // was first called.
        void UpdateValuePosition(
            Int32        newPosition,
            BinaryWriter bamlBinaryWriter);

        // Relative stream position in the baml stream where the value associated
        // with this key starts.  It is relative to the end of the keys section,
        // or the start of the values section.
        Int32 ValuePosition { get; set; }

        // The actual key object used in the dictionary.  This may be a string,
        // field, type or other object.
        object KeyObject { get; set; }

        // Position in the stream where ValuePosition was written.  This is needed
        // when updating the ValuePosition.
        Int64 ValuePositionPosition { get; set; }

        // True if the value associated with this key is shared.
        bool Shared { get; set; }

        // Whether Shared was set.
        bool SharedSet { get; set; }

#if !PBTCOMPILER
        object[] StaticResourceValues {get; set;}
#endif
    }

    // Common interface implemented by BamlRecords that 
    // use optimized storage for MarkupExtensions.
    internal interface IOptimizedMarkupExtension
    {
        short ExtensionTypeId
        {
            get;
        }

        short ValueId
        {
            get;
        }

        bool IsValueTypeExtension
        {
            get;
        }

        bool IsValueStaticExtension
        {
            get;
        }
    }

    // BamlRecord use in a defer loaded dictionary as the key for adding a value.
    // The value is a type that is refered to using a TypeID
    internal class BamlDefAttributeKeyTypeRecord : BamlElementStartRecord, IBamlDictionaryKey
    {
        internal BamlDefAttributeKeyTypeRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            base.LoadRecordData(bamlBinaryReader);
            _valuePosition =  bamlBinaryReader.ReadInt32();
            ((IBamlDictionaryKey)this).Shared = bamlBinaryReader.ReadBoolean();
            ((IBamlDictionaryKey)this).SharedSet = bamlBinaryReader.ReadBoolean();
        }
#endif

        // Write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            base.WriteRecordData(bamlBinaryWriter);
            _valuePositionPosition = bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            bamlBinaryWriter.Write(_valuePosition);
            bamlBinaryWriter.Write(((IBamlDictionaryKey)this).Shared);
            bamlBinaryWriter.Write(((IBamlDictionaryKey)this).SharedSet);
        }

        // Update the pointer to the Value that was written out when WriteRecordData
        // was first called.  At that time the true position was probably not known,
        // so it is written out later.  Be certain to leave the passed writer pointing
        // to the same location it was at when this call was made.
        void IBamlDictionaryKey.UpdateValuePosition(
            Int32        newPosition,
            BinaryWriter bamlBinaryWriter)
        {
            Debug.Assert(_valuePositionPosition != -1,
                    "Must call WriteRecordData before updating position");

            // Use relative positions to reduce the possibility of truncation,
            // since Seek takes a 32 bit int, but position is a 64 bit int.
            Int64 existingPosition = bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            Int32 deltaPosition = (Int32)(_valuePositionPosition-existingPosition);

            bamlBinaryWriter.Seek(deltaPosition, SeekOrigin.Current);
            bamlBinaryWriter.Write(newPosition);
            bamlBinaryWriter.Seek(-ValuePositionSize-deltaPosition, SeekOrigin.Current);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlDefAttributeKeyTypeRecord newRecord = (BamlDefAttributeKeyTypeRecord)record;
            newRecord._valuePosition = _valuePosition;
            newRecord._valuePositionPosition = _valuePositionPosition;
            newRecord._keyObject = _keyObject;
            newRecord._staticResourceValues = _staticResourceValues;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.DefAttributeKeyType; }
        }

        // Relative stream position in the baml stream where the value associated
        // with this key starts.  It is relative to the end of the keys section,
        // or the start of the values section.
        Int32 IBamlDictionaryKey.ValuePosition
        {
            get { return _valuePosition; }
            set { _valuePosition = value; }
        }

        // The actual key used in the defer loaded dictionary.  For this type of
        // record the key is a Type that is obtained at runtime from the base
        // classes TypeId
        object IBamlDictionaryKey.KeyObject
        {
            get { return _keyObject; }
            set { _keyObject = value; }
        }

        // Position in the stream where ValuePosition was written.  This is needed
        // when updating the ValuePosition.
        Int64 IBamlDictionaryKey.ValuePositionPosition
        {
            get { return _valuePositionPosition; }
            set { _valuePositionPosition = value; }
        }

        // True if the value associated with this key is shared.
        bool IBamlDictionaryKey.Shared
        {
            get
            {
                return _flags[_sharedSection] == 1 ? true : false;
            }

            set
            {
                _flags[_sharedSection] = value ? 1 : 0;
            }
        }

        // Whether Shared was set
        bool IBamlDictionaryKey.SharedSet
        {
            get
            {
                return _flags[_sharedSetSection] == 1 ? true : false;
            }

            set
            {
                _flags[_sharedSetSection] = value ? 1 : 0;
            }
        }

#if !PBTCOMPILER
        object[] IBamlDictionaryKey.StaticResourceValues
        {
            get { return _staticResourceValues; }
            set { _staticResourceValues = value; }
        }
#endif


        // Allocate space in _flags.

        private static BitVector32.Section _sharedSection
            = BitVector32.CreateSection( 1, BamlElementStartRecord.LastFlagsSection );
        
        private static BitVector32.Section _sharedSetSection
            = BitVector32.CreateSection( 1, _sharedSection );
        
#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _sharedSetSection; }
        }
#endif



#endregion Properties

#region Data

        // Size in bytes of the ValuePosition field written out to baml.  This
        // must be in sync with the size of _valuePosition below.
        internal const Int32 ValuePositionSize = 4;

        // Relative position in the stream where the value associated with this key starts
        Int32 _valuePosition;

        // Position in the stream where ValuePosition was written.  This is needed
        // when updating the ValuePosition.
        Int64 _valuePositionPosition = -1;

        // Actual object key used by a dictionary.  This is a Type object
        object _keyObject = null;

#if !PBTCOMPILER
        object[] _staticResourceValues;
#endif

#endregion Data
    }


    // BamlRecord for x:Key attribute when used in a defer loaded dictionary
    // as the key for adding a value.  The value is stored as a string.
    internal class BamlDefAttributeKeyStringRecord : BamlStringValueRecord, IBamlDictionaryKey
    {
        internal BamlDefAttributeKeyStringRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            ValueId = bamlBinaryReader.ReadInt16();
            _valuePosition =  bamlBinaryReader.ReadInt32();
            ((IBamlDictionaryKey)this).Shared = bamlBinaryReader.ReadBoolean();
            ((IBamlDictionaryKey)this).SharedSet = bamlBinaryReader.ReadBoolean();
            _keyObject = null;
        }
#endif

        // Write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(ValueId);
            _valuePositionPosition = bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            bamlBinaryWriter.Write(_valuePosition);
            bamlBinaryWriter.Write(((IBamlDictionaryKey)this).Shared);
            bamlBinaryWriter.Write(((IBamlDictionaryKey)this).SharedSet);
        }

        // Update the pointer to the Value that was written out when WriteRecordData
        // was first called.  At that time the true position was probably not known,
        // so it is written out later.  Be certain to leave the passed writer pointing
        // to the same location it was at when this call was made.
        void IBamlDictionaryKey.UpdateValuePosition(
            Int32        newPosition,
            BinaryWriter bamlBinaryWriter)
        {
            Debug.Assert(_valuePositionPosition != -1,
                    "Must call WriteRecordData before updating position");

            // Use relative positions to reduce the possibility of truncation,
            // since Seek takes a 32 bit int, but position is a 64 bit int.
            Int64 existingPosition = bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            Int32 deltaPosition = (Int32)(_valuePositionPosition-existingPosition);

            bamlBinaryWriter.Seek(deltaPosition, SeekOrigin.Current);
            bamlBinaryWriter.Write(newPosition);
            bamlBinaryWriter.Seek(-ValuePositionSize-deltaPosition, SeekOrigin.Current);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlDefAttributeKeyStringRecord newRecord = (BamlDefAttributeKeyStringRecord)record;
            newRecord._valuePosition = _valuePosition;
            newRecord._valuePositionPosition = _valuePositionPosition;
            newRecord._keyObject = _keyObject;
            newRecord._valueId = _valueId;
            newRecord._staticResourceValues = _staticResourceValues;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.DefAttributeKeyString; }
        }

        // Relative stream position in the baml stream where the value associated
        // with this key starts.  It is relative to the end of the keys section,
        // or the start of the values section.
        Int32 IBamlDictionaryKey.ValuePosition
        {
            get { return _valuePosition; }
            set { _valuePosition = value; }
        }

        // True if the value associated with this key is shared.
        bool IBamlDictionaryKey.Shared
        {
            get
            {
                return _flags[_sharedSection] == 1 ? true : false;
            }

            set
            {
                _flags[_sharedSection] = value ? 1 : 0;
            }
        }

        // Whether Shared was set
        bool IBamlDictionaryKey.SharedSet
        {
            get
            {
                return _flags[_sharedSetSection] == 1 ? true : false;
            }

            set
            {
                _flags[_sharedSetSection] = value ? 1 : 0;
            }
        }

        // Allocate space in _flags.

        private static BitVector32.Section _sharedSection
            = BitVector32.CreateSection( 1, BamlStringValueRecord.LastFlagsSection );
        
        private static BitVector32.Section _sharedSetSection
            = BitVector32.CreateSection( 1,  _sharedSection );
        
#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _sharedSetSection; }
        }
#endif


        // The following are NOT written out to BAML but are cached at runtime

        // The string value translated into a key object.  The string may represent
        // a type, field, or other object that can be translated into an object using
        // using the Mapper.
        object IBamlDictionaryKey.KeyObject
        {
            get { return _keyObject; }
            set { _keyObject = value; }
        }

        // Position in the stream where ValuePosition was written.  This is needed
        // when updating the ValuePosition.
        Int64 IBamlDictionaryKey.ValuePositionPosition
        {
            get { return _valuePositionPosition; }
            set { _valuePositionPosition = value; }
        }
        
        internal Int16 ValueId
        {
            get { return _valueId; }
            set { _valueId = value; }
        }


#if !PBTCOMPILER
        object[] IBamlDictionaryKey.StaticResourceValues
        {
            get { return _staticResourceValues; }
            set { _staticResourceValues = value; }
        }
#endif

#endregion Properties

#region Data

        // Size in bytes of the ValuePosition field written out to baml.  This
        // must be in sync with the size of _valuePosition below.
        internal const Int32 ValuePositionSize = 4;

        // Relative position in the stream where the value associated with this key starts
        Int32 _valuePosition;

        // Position in the stream where ValuePosition was written.  This is needed
        // when updating the ValuePosition.
        Int64 _valuePositionPosition = -1;

        // Actual object key used by a dictionary.  This is the Value string
        // after conversion.
        object _keyObject = null;

        Int16 _valueId;

#if !PBTCOMPILER
        object[] _staticResourceValues;
#endif

#endregion Data
    }

    // BamlRecord for x:Whatever attribute
    internal class BamlDefAttributeRecord : BamlStringValueRecord
    {
#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            Value      =  bamlBinaryReader.ReadString();
            NameId     =  bamlBinaryReader.ReadInt16();
            Name       =  null;
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(Value);
            bamlBinaryWriter.Write(NameId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlDefAttributeRecord newRecord = (BamlDefAttributeRecord)record;
            newRecord._name = _name;
            newRecord._nameId = _nameId;
            newRecord._attributeUsage = _attributeUsage;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.DefAttribute; }
        }

        // The following is written out the baml file.

        internal Int16 NameId
        {
            get { return _nameId; }
            set { _nameId = value; }
        }

        // The following are cached locally, but not written to baml.

        internal string Name
        {
#if !PBTCOMPILER
            get { return _name; }
#endif
            set { _name = value; }
        }

        // Some attributes have special usage, such as setting the XmlLang and XmlSpace
        // strings in the parser context.  This is flagged with this property
        internal BamlAttributeUsage AttributeUsage
        {
#if !PBTCOMPILER
            get { return _attributeUsage; }
#endif
            set { _attributeUsage = value; }
        }

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} nameId({1}) is '{2}' usage={3}",
                                 RecordType, NameId, Name, AttributeUsage);
        }
#endif

#region Data
        string _name;
        Int16  _nameId;
        BamlAttributeUsage _attributeUsage;
#endregion Data

    }

    // BamlRecord for PresentationOptions:Whatever attribute
    internal class BamlPresentationOptionsAttributeRecord : BamlStringValueRecord
    {
#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            Value      =  bamlBinaryReader.ReadString();
            NameId     =  bamlBinaryReader.ReadInt16();
            Name       =  null;
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(Value);
            bamlBinaryWriter.Write(NameId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPresentationOptionsAttributeRecord newRecord = (BamlPresentationOptionsAttributeRecord)record;
            newRecord._name = _name;
            newRecord._nameId = _nameId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PresentationOptionsAttribute; }
        }

        // The following is written out the baml file.

        internal Int16 NameId
        {
            get { return _nameId; }
            set { _nameId = value; }
        }

        // The following are cached locally, but not written to baml.

        internal string Name
        {
#if !PBTCOMPILER
            get { return _name; }
#endif
            set { _name = value; }
        }

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} nameId({1}) is '{2}' ",
                                 RecordType, NameId, Name);
        }
#endif

#region Data
        string _name;
        Int16  _nameId;
#endregion Data

    }    

    //
    // BamlPropertyComplexStartRecord is for Complex DependencyProperty declarations
    // in markup, where the actual type and value is determined by subsequent records.
    //
    internal class BamlPropertyComplexStartRecord : BamlRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId   = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyComplexStartRecord newRecord = (BamlPropertyComplexStartRecord)record;
            newRecord._attributeId = _attributeId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyComplexStart; }
        }

        internal short AttributeId
        {
            get { return _attributeId; }
            set { _attributeId = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 2; }
            set { Debug.Assert (value == -1, "Wrong size set for complex prop record"); }
        }

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} attr({1})",
                                 RecordType, _attributeId);
        }
#endif

#region Data
        short _attributeId = -1;
#endregion Data

    }

    //
    // BamlPropertyStringReferenceRecord is for Property values that are written
    // out as references into the string table.
    //
    internal class BamlPropertyStringReferenceRecord : BamlPropertyComplexStartRecord
    {
        #region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId = bamlBinaryReader.ReadInt16();
            StringId  = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
            bamlBinaryWriter.Write(StringId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyStringReferenceRecord newRecord = (BamlPropertyStringReferenceRecord)record;
            newRecord._stringId = _stringId;
        }
#endif

        #endregion Methods

        #region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyStringReference; }
        }

        internal short StringId
        {
            get { return _stringId; }
#if !PBTCOMPILER
            set { _stringId = value; }
#endif
        }

        internal override Int32 RecordSize
        {
            get { return 4; }
            set { Debug.Assert (value == -1, "Wrong size set for complex prop record"); }
        }

        #endregion Properties

        #region Data
        short _stringId = 0;

        #endregion Data
    }

    //
    // BamlPropertyTypeReferenceRecord is for Property values that are written
    // out as references into the type table.  So the property value is a 'Type' object.
    //
    internal class BamlPropertyTypeReferenceRecord : BamlPropertyComplexStartRecord
    {
        #region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId = bamlBinaryReader.ReadInt16();
            TypeId  = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
            bamlBinaryWriter.Write(TypeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyTypeReferenceRecord newRecord = (BamlPropertyTypeReferenceRecord)record;
            newRecord._typeId = _typeId;
        }
#endif

        #endregion Methods

        #region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyTypeReference; }
        }

        internal short TypeId
        {
            get { return _typeId; }
            set { _typeId = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 4; }
            set { Debug.Assert (value == -1, "Wrong size set for complex prop record"); }
        }

        #endregion Properties

        #region Data
        short _typeId = 0;
        #endregion Data
    }

    //
    // BamlPropertyWithConverterRecord information for property with custom type converter
    //
    internal class BamlPropertyWithConverterRecord : BamlPropertyRecord
    {
        #region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            base.LoadRecordData(bamlBinaryReader);
            ConverterTypeId  = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            base.WriteRecordData(bamlBinaryWriter);
            bamlBinaryWriter.Write(ConverterTypeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyWithConverterRecord newRecord = (BamlPropertyWithConverterRecord)record;
            newRecord._converterTypeId = _converterTypeId;
        }
#endif

        #endregion Methods

        #region Properties

        // The following are stored in the baml stream

        // ID of this type converter.  Referenced in other baml records where a
        // Type is needed.
        internal short ConverterTypeId
        {
            get { return _converterTypeId; }
            set { _converterTypeId = value; }
        }

        // Additional properties not stored in the baml stream

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyWithConverter; }
        }

        #endregion Properties

        #region Data

        short _converterTypeId = 0;

        #endregion Data
    }

    //
    // BamlPropertyRecord is for DependencyProperty values that are written
    // out as strings.
    //
    internal class BamlPropertyRecord : BamlStringValueRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId   = bamlBinaryReader.ReadInt16();
            Value         = bamlBinaryReader.ReadString();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
            bamlBinaryWriter.Write(Value);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyRecord newRecord = (BamlPropertyRecord)record;
            newRecord._attributeId = _attributeId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.Property; }
        }

        internal short AttributeId
        {
            get { return _attributeId; }
            set { _attributeId = value; }
        }

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} attr({1}) <== '{2}'",
                                 RecordType, _attributeId, Value);
        }
#endif

#region Data
        short  _attributeId = -1;
#endregion Data


    }

    //
    // BamlPropertyWithExtensionRecord is for property values that are Markup extensions
    // with a single param member that are written out as attributeIds.
    //
    internal class BamlPropertyWithExtensionRecord : BamlRecord, IOptimizedMarkupExtension
    {
        #region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId = bamlBinaryReader.ReadInt16();
            short extensionTypeId = bamlBinaryReader.ReadInt16();
            ValueId = bamlBinaryReader.ReadInt16();

            // The upper 4 bits of the ExtensionTypeId are used as flags
            _extensionTypeId = (short)(extensionTypeId & ExtensionIdMask);
            IsValueTypeExtension = (extensionTypeId & TypeExtensionValueMask) == TypeExtensionValueMask;
            IsValueStaticExtension = (extensionTypeId & StaticExtensionValueMask) == StaticExtensionValueMask;
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
            short extensionTypeId = ExtensionTypeId;
            if (IsValueTypeExtension)
            {
                extensionTypeId |= TypeExtensionValueMask;
            }
            else if (IsValueStaticExtension)
            {
                extensionTypeId |= StaticExtensionValueMask;
            }
            bamlBinaryWriter.Write(extensionTypeId);
            bamlBinaryWriter.Write(ValueId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyWithExtensionRecord newRecord = (BamlPropertyWithExtensionRecord)record;
            newRecord._attributeId = _attributeId;
            newRecord._extensionTypeId = _extensionTypeId;
            newRecord._valueId = _valueId;
        }
#endif

        #endregion Methods

        #region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyWithExtension; }
        }

        // Id of the property whose value is the simple ME
        internal short AttributeId
        {
            get { return _attributeId; }
            set { _attributeId = value; }
        }

        // KnownElement Id of the MarkupExtension
        public short ExtensionTypeId
        {
            get { return _extensionTypeId; }
            set 
            {
                // we shouldn't ever be intruding on the flags portion of the ExtensionTypeId
                Debug.Assert(value <= ExtensionIdMask);
                _extensionTypeId = value;
            }
        }

        // For StaticExtension: AttributeId of a member 
        // For TemplateBindingExtension: AttributeId of a DependencyProperty 
        // For a DynamicResourceExtension:
        //      StringId if the value is a string
        //      TypeId if the value is a TypeExtension
        //      AttributeId of the member if the value is a StaticExtension
        public short ValueId
        {
            get { return _valueId; }
            set { _valueId = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 6; }
            set { Debug.Assert(value == -1, "Wrong size set for complex prop record"); }
        }

        // For DynamicResourceExtension, if the value is itself a simple TypeExtension
        public bool IsValueTypeExtension
        {
            get { return _flags[_isValueTypeExtensionSection] == 1 ? true : false; }
            set { _flags[_isValueTypeExtensionSection] = value ? 1 : 0; }
        }

        // For DynamicResourceExtension, if the value is itself a simple StaticExtension
        public bool IsValueStaticExtension
        {
            get { return _flags[_isValueStaticExtensionSection] == 1 ? true : false; }
            set { _flags[_isValueStaticExtensionSection] = value ? 1 : 0; }
        }

        // Allocate space in _flags.
        private static BitVector32.Section _isValueTypeExtensionSection
            = BitVector32.CreateSection(1, BamlRecord.LastFlagsSection);

        private static BitVector32.Section _isValueStaticExtensionSection
            = BitVector32.CreateSection(1, _isValueTypeExtensionSection);

#if !PBTCOMPILER
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _isValueStaticExtensionSection; }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} attr({1}) extn({2}) valueId({3})",
                                 RecordType, _attributeId, _extensionTypeId, _valueId);
        }
#endif

        #endregion Properties

        #region Data
        short _attributeId = -1;
        short _extensionTypeId = 0;
        short _valueId = 0;

        private static readonly short ExtensionIdMask = 0x0FFF;
        private static readonly short TypeExtensionValueMask = 0x4000;
        private static readonly short StaticExtensionValueMask = 0x2000;
        #endregion Data
    }

    //
    // BamlPropertyCustomWriteInfoRecord is for DependencyProperty values that support
    // custom Avalon serialization. The property value objects write directly onto
    // the BAML stream in whatever format they understand. This record is used only
    // during BAML write time.
    //
    internal class BamlPropertyCustomWriteInfoRecord : BamlPropertyCustomRecord
    {
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            int writePositionStart = (int)bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            short serializerTypeId = SerializerTypeId;

            bamlBinaryWriter.Write(AttributeId);
            if (serializerTypeId == (short)KnownElements.DependencyPropertyConverter)
            {
                // There is no need to actually use a real Converter here since we already have the
                // DP value as an AttributeInfoId.

                // if ValueMemberName exists then remember that the ValueId is a TypeId of the
                // type that declares ValueMemberName, so that it can be resolved correctly at
                // load time.
                if (ValueMemberName != null)
                {
                    bamlBinaryWriter.Write((short)(serializerTypeId | TypeIdValueMask));
                }
                else
                {
                    bamlBinaryWriter.Write(serializerTypeId);
                }

                // if ValueMemberName does not exist, ValueId is a KnownProperty Id
                // else it is a TypeId of the declaring type.
                bamlBinaryWriter.Write(ValueId);

                // Write out the ValueMemberName if it exists
                if (ValueMemberName != null)
                {
                    bamlBinaryWriter.Write(ValueMemberName);
                }
                
                return;
            }

            bamlBinaryWriter.Write(serializerTypeId);

            bool converted = false;

            // If we have an enum or a bool, do conversion to custom binary data here,
            // since we do not have a serializer associated with these types.
            if (ValueType != null && ValueType.IsEnum)
            {
                uint uintValue = 0;
                string [] enumValues = Value.Split(new Char[] { ',' });

                // if the Enum is a flag, then resolve each flag value in the enum value string.
                foreach (string enumValue in enumValues)
                {
                    FieldInfo enumField = ValueType.GetField(enumValue.Trim(), BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (enumField != null)
                    {
                        // get the raw va;ue of the enum field and convert to a uint.
                        object rawEnumValue = enumField.GetRawConstantValue();
                        uintValue += (uint)Convert.ChangeType(rawEnumValue, typeof(uint), TypeConverterHelper.InvariantEnglishUS);
                        converted = true;
                    }
                    else
                    {
                        converted = false;
                        break;
                    }
                }

                if (converted)
                {
                    bamlBinaryWriter.Write(uintValue);
                }
            }
            else if (ValueType == typeof(Boolean))
            {
                TypeConverter boolConverter = TypeDescriptor.GetConverter(typeof(Boolean));
                object convertedValue = boolConverter.ConvertFromString(TypeContext, TypeConverterHelper.InvariantEnglishUS, Value);
                bamlBinaryWriter.Write((byte)Convert.ChangeType(convertedValue, typeof(byte), TypeConverterHelper.InvariantEnglishUS));
                converted = true;
            }
            else if (SerializerType == typeof(XamlBrushSerializer))
            {
                XamlSerializer serializer = new XamlBrushSerializer();

                // If we custom serialize this particular value at this point, then see
                // if it can convert.
                // NOTE:  This is sensitive to changes in the BamlRecordWriter and
                //        BamlRecordManager code and must be kept in sync with them...
                converted = serializer.ConvertStringToCustomBinary(bamlBinaryWriter, Value);
            }
            else if (SerializerType == typeof(XamlPoint3DCollectionSerializer))
            {
                XamlSerializer serializer = new XamlPoint3DCollectionSerializer();

                // If we custom serialize this particular value at this point, then see
                // if it can convert.
                // NOTE:  This is sensitive to changes in the BamlRecordWriter and
                //        BamlRecordManager code and must be kept in sync with them...
                converted = serializer.ConvertStringToCustomBinary(bamlBinaryWriter, Value);
            }
            else if (SerializerType == typeof(XamlVector3DCollectionSerializer))
            {
                XamlSerializer serializer = new XamlVector3DCollectionSerializer();

                // If we custom serialize this particular value at this point, then see
                // if it can convert.
                // NOTE:  This is sensitive to changes in the BamlRecordWriter and
                //        BamlRecordManager code and must be kept in sync with them...
                converted = serializer.ConvertStringToCustomBinary(bamlBinaryWriter, Value);
            }
            else if (SerializerType == typeof(XamlPointCollectionSerializer))
            {
                XamlSerializer serializer = new XamlPointCollectionSerializer();

                // If we custom serialize this particular value at this point, then see
                // if it can convert.
                // NOTE:  This is sensitive to changes in the BamlRecordWriter and
                //        BamlRecordManager code and must be kept in sync with them...
                converted = serializer.ConvertStringToCustomBinary(bamlBinaryWriter, Value);
            }
            else if (SerializerType == typeof(XamlInt32CollectionSerializer))
            {
                XamlSerializer serializer = new XamlInt32CollectionSerializer();

                // If we custom serialize this particular value at this point, then see
                // if it can convert.
                // NOTE:  This is sensitive to changes in the BamlRecordWriter and
                //        BamlRecordManager code and must be kept in sync with them...
                converted = serializer.ConvertStringToCustomBinary(bamlBinaryWriter, Value);
            }
            else if (SerializerType == typeof(XamlPathDataSerializer))
            {
                XamlSerializer serializer = new XamlPathDataSerializer();
                
                // If we custom serialize this particular value at this point, then see
                // if it can convert.
                // NOTE:  This is sensitive to changes in the BamlRecordWriter and
                //        BamlRecordManager code and must be kept in sync with them...
                converted = serializer.ConvertStringToCustomBinary(bamlBinaryWriter, Value);
            }

            if (!converted)
            {
                throw new XamlParseException(SR.Get(SRID.ParserBadString, Value, ValueType.Name));
            }
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyCustomWriteInfoRecord newRecord = (BamlPropertyCustomWriteInfoRecord)record;
            newRecord._valueId = _valueId;
            newRecord._valueType = _valueType;
            newRecord._value = _value;
            newRecord._valueMemberName = _valueMemberName;
            newRecord._serializerType = _serializerType;
            newRecord._typeContext = _typeContext;
        }
#endif

        // The KnownProperty Id of the Value, if it is a property and can be converted into one, 
        // or the TypeId of the owner of the property value
        internal short ValueId
        {
            get { return _valueId; }
            set { _valueId = value; }
        }

        // If ValueId is a TypeId, then this holds the name of the member.
        internal string ValueMemberName
        {
            get { return _valueMemberName; }
            set { _valueMemberName = value; }
        }

        // The following properties are NOT written to the BAML stream.

        // Type of this property
        internal Type ValueType
        {
            get { return _valueType; }
            set { _valueType = value; }
        }

        // The string Value of the property.
        internal string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        // Type of the XamlSerializer associated with this property.  Null
        // if this type is custom serialized by the parser itself.
        internal Type SerializerType
        {
            get { return _serializerType; }
            set { _serializerType = value; }
        }

        // Context used for type conversion of built in types.
        internal ITypeDescriptorContext TypeContext
        {
            get { return _typeContext; }
            set { _typeContext = value; }
        }

        short                  _valueId;
        Type                   _valueType;
        string                 _value;
        string                 _valueMemberName;
        Type                   _serializerType;
        ITypeDescriptorContext _typeContext;
    }

    //
    // BamlPropertyCustomRecord is for DependencyProperty values that support
    // custom Avalon serialization. This record is used only during BAML load.
    // The property value objects are read directly from the BAML stream by the
    // custom binary serializer for the property.
    //
    internal class BamlPropertyCustomRecord : BamlVariableSizedRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId      = bamlBinaryReader.ReadInt16();
            short serializerTypeId = bamlBinaryReader.ReadInt16();
            
            IsValueTypeId = (serializerTypeId & TypeIdValueMask) == TypeIdValueMask;
            if (IsValueTypeId)
            {
                serializerTypeId &= (short)(~TypeIdValueMask);
            }
            
            SerializerTypeId = serializerTypeId;

            ValueObjectSet  = false;
            IsRawEnumValueSet = false;
            _valueObject = null;

            // ValueObject and ValueObject are not set until BamlRecordReader.ReadPropertyCustomRecord
            // because the Mapper is needed for custom DPs

            // NOTE: above may no longer true, so this could be potentially changed to be in sync with
            // other record. Needs more investigation.
        }

        // Read the binary data using the passed reader and use that to set the ValueObject.
        internal object GetCustomValue(BinaryReader reader, Type propertyType, short serializerId, BamlRecordReader bamlRecordReader)
        {
            Debug.Assert(!ValueObjectSet);

            // Handle enums and bools here directly.
            // Otherwise call the known custom serializers directly.
            switch (serializerId)
            {
                case (short)KnownElements.EnumConverter:

                    uint enumBits;
                    if (_valueObject == null)
                    {
                        // if no raw value has been read in yet, read it now
                        // from the baml stream.
                        enumBits = reader.ReadUInt32();
                    }
                    else
                    {
                        // raw value has been read in earlier, so try to resolve into
                        // an actual enum value now.
                        enumBits = (uint)_valueObject;
                    }

                    if (propertyType.IsEnum)
                    {
                        // property Type is an enum, so raw value can be resolved now.
                        _valueObject = Enum.ToObject(propertyType, enumBits);
                        ValueObjectSet = true;
                        IsRawEnumValueSet = false;
                    }
                    else
                    {
                        // property Type is not available yet, so raw value cannot
                        // be resolved now. Store it and try later.
                        _valueObject = enumBits;
                        ValueObjectSet = false;
                        IsRawEnumValueSet = true;
                    }

                    return _valueObject;

                case (short)KnownElements.BooleanConverter:
                    
                    byte boolByte = reader.ReadByte();
                    _valueObject = boolByte == 1;
                    break;

                case (short)KnownElements.XamlBrushSerializer:

                    // Don't bother creating a XamlBrushSerializer instance & calling ConvertCustomBinaryToObject
                    // on it since that just calls SCB directly liek below. This saves big on perf.
                    _valueObject = SolidColorBrush.DeserializeFrom(reader, bamlRecordReader.TypeConvertContext);
                    break;

                case (short)KnownElements.XamlPathDataSerializer:

                    _valueObject = XamlPathDataSerializer.StaticConvertCustomBinaryToObject(reader);
                    break;

                case (short)KnownElements.XamlPoint3DCollectionSerializer:

                    _valueObject = XamlPoint3DCollectionSerializer.StaticConvertCustomBinaryToObject(reader);
                    break;

                case (short)KnownElements.XamlVector3DCollectionSerializer:

                    _valueObject = XamlVector3DCollectionSerializer.StaticConvertCustomBinaryToObject(reader);
                    break;

                case (short)KnownElements.XamlPointCollectionSerializer:

                    _valueObject = XamlPointCollectionSerializer.StaticConvertCustomBinaryToObject(reader);
                    break;

                case (short)KnownElements.XamlInt32CollectionSerializer:

                    _valueObject = XamlInt32CollectionSerializer.StaticConvertCustomBinaryToObject(reader); 
                    break;

                default:
                    Debug.Assert (false, "Unknown custom serializer");
                    return null;
            }

            ValueObjectSet = true;
            return _valueObject;
        }
        
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyCustomRecord newRecord = (BamlPropertyCustomRecord)record;
            newRecord._valueObject = _valueObject;
            newRecord._attributeId = _attributeId;
            newRecord._serializerTypeId = _serializerTypeId;
        }
        
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyCustom; }
        }

        internal short AttributeId
        {
            get { return _attributeId; }
            set { _attributeId = value; }
        }

        // ID of this serializer type.  Referenced in other baml records where a
        // Type is needed.
        internal short SerializerTypeId
        {
            get { return _serializerTypeId; }
            set { _serializerTypeId = value; }
        }

        // The following properties are NOT written to the BAML stream.

#if !PBTCOMPILER
        // Value of the converted object.
        internal object ValueObject
        {
            get { return _valueObject; }
            set { _valueObject = value; }
        }

        // Return true if GetCustomValue has been called, indicating that
        // a conversion from binary custom data to a ValueObject has occurred.
        internal bool ValueObjectSet
        {
            get { return _flags[_isValueSetSection] == 1 ? true : false; }
            set { _flags[_isValueSetSection] = value ? 1 : 0; }
        }

        internal bool IsValueTypeId
        {
            get { return _flags[_isValueTypeIdSection] == 1 ? true : false; }
            set { _flags[_isValueTypeIdSection] = value ? 1 : 0; }
        }

        // true if only the raw value of enum has been read as it cannot yet be
        // converted into an enum as the Type is not available yet.
        internal bool IsRawEnumValueSet
        {
            get { return _flags[_isRawEnumValueSetSection] == 1 ? true : false; }
            set { _flags[_isRawEnumValueSetSection] = value ? 1 : 0; }
        }

        object _valueObject;

        // Allocate space in _flags.
        private static BitVector32.Section _isValueSetSection
            = BitVector32.CreateSection(1, BamlVariableSizedRecord.LastFlagsSection);

        // Allocate space in _flags.
        private static BitVector32.Section _isValueTypeIdSection
            = BitVector32.CreateSection(1, _isValueSetSection);

        // Allocate space in _flags.
        private static BitVector32.Section _isRawEnumValueSetSection
            = BitVector32.CreateSection(1, _isValueTypeIdSection);

        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _isRawEnumValueSetSection; }
        }
#endif

#endregion Properties

#region Data

        internal static readonly short TypeIdValueMask = 0x4000;

        short                  _attributeId = 0;
        short                  _serializerTypeId = 0;

#endregion Data
    }

    internal class BamlPropertyArrayEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyArrayEnd; }
        }

#endregion Properties
    }

    internal class BamlConstructorParametersStartRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ConstructorParametersStart; }
        }

#endregion Properties
    }

    internal class BamlConstructorParametersEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ConstructorParametersEnd; }
        }

#endregion Properties
    }

    internal class BamlConstructorParameterTypeRecord : BamlRecord
    {
        #region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            TypeId  = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(TypeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlConstructorParameterTypeRecord newRecord = (BamlConstructorParameterTypeRecord)record;
            newRecord._typeId = _typeId;
        }
#endif

        #endregion Methods

        #region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ConstructorParameterType; }
        }

        internal short TypeId
        {
            get { return _typeId; }
            set { _typeId = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 2; }
            set { Debug.Assert (value == -1, "Wrong size set for complex prop record"); }
        }

        #endregion Properties

        #region Data
        short _typeId = 0;
        #endregion Data
    }

    internal class BamlPropertyIListEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyIListEnd; }
        }

#endregion Properties
    }

    internal class BamlPropertyIDictionaryEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyIDictionaryEnd; }
        }

#endregion Properties
    }

    internal class BamlPropertyComplexEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyComplexEnd; }
        }

#endregion Properties
    }


    internal class BamlPropertyArrayStartRecord : BamlPropertyComplexStartRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyArrayStart; }
        }

#endregion Properties
    }

    internal class BamlPropertyIListStartRecord : BamlPropertyComplexStartRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyIListStart; }
        }

#endregion Properties
    }

    internal class BamlPropertyIDictionaryStartRecord : BamlPropertyComplexStartRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyIDictionaryStart; }
        }

#endregion Properties
    }

    internal class BamlRoutedEventRecord : BamlStringValueRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId   = bamlBinaryReader.ReadInt16();
            Value         = bamlBinaryReader.ReadString();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
            bamlBinaryWriter.Write(Value);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlRoutedEventRecord newRecord = (BamlRoutedEventRecord)record;
            newRecord._attributeId = _attributeId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.RoutedEvent; }
        }

        internal short AttributeId
        {
            get { return _attributeId; }
#if !PBTCOMPILER
            set { _attributeId = value; }
#endif
        }

#endregion Properties

#region Data

        short _attributeId = -1;

#endregion Data
    }


    // A section of literal content.
    internal class BamlLiteralContentRecord : BamlStringValueRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            Value  =  bamlBinaryReader.ReadString();

            Int32 _lineNumber = bamlBinaryReader.ReadInt32();
            Int32 _linePosition = bamlBinaryReader.ReadInt32();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(Value);

            bamlBinaryWriter.Write((Int32)0);
            bamlBinaryWriter.Write((Int32)0);
        }

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.LiteralContent; }
        }

#endregion Properties
     }

    // An record for the connection id that the (Style)BamlRecordReader uses to
    // hookup an ID or event on any element in the object tree or Style visual tree.
    internal class BamlConnectionIdRecord : BamlRecord
    {
#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            ConnectionId = bamlBinaryReader.ReadInt32();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(ConnectionId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlConnectionIdRecord newRecord = (BamlConnectionIdRecord)record;
            newRecord._connectionId = _connectionId;
        }
#endif

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ConnectionId; }
        }

        // Id of the type of this object
        internal Int32 ConnectionId
        {
            get { return _connectionId; }
            set { _connectionId = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 4; }
            set { Debug.Assert(value == -1, "Wrong size set for element record"); }
        }

        Int32 _connectionId = -1;
    }

    // An object record in the object tree.  This can be a CLR
    // object or a DependencyObject.
    internal class BamlElementStartRecord : BamlRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            TypeId = bamlBinaryReader.ReadInt16();
            byte flags = bamlBinaryReader.ReadByte();
            CreateUsingTypeConverter = (flags & 1) != 0;
            IsInjected = (flags & 2) != 0;
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(TypeId);
            byte flags = (byte)((CreateUsingTypeConverter ? 1 : 0) | (IsInjected ? 2 : 0));
            bamlBinaryWriter.Write(flags);
        }

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ElementStart; }
        }

        // Id of the type of this object
        internal short TypeId
        {
            get
            {
                short value = (short) _flags[_typeIdLowSection];
                value |= (short) (_flags[_typeIdHighSection] << 8);

                return value;
            }

            set
            {
                _flags[_typeIdLowSection] = (short)  (value & 0xff);
                _flags[_typeIdHighSection] = (short) ((value & 0xff00) >> 8);
            }
        }

        // Whether this object instance is expected to be created via TypeConverter
        internal bool CreateUsingTypeConverter
        {
            get
            {
                return _flags[_useTypeConverter] == 1 ? true : false;
            }

            set
            {
                _flags[_useTypeConverter] = value ? 1 : 0;
            }
        }

        // Whether this element start record is just an injected tag that should not be processed
        internal bool IsInjected
        {
            get
            {
                return _flags[_isInjected] == 1 ? true : false;
            }

            set
            {
                _flags[_isInjected] = value ? 1 : 0;
            }
        }
        
        internal override Int32 RecordSize
        {
            get { return 3; }
            set { Debug.Assert(value == -1, "Wrong size set for element record"); }
        }

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} typeId={1}",
                                 RecordType, GetTypeName(TypeId));
        }
#endif


        // Allocate space in _flags.
        // BitVector32 doesn't support 16 bit sections, so we have to break
        // it up into 2 sections.

        private static BitVector32.Section _typeIdLowSection
            = BitVector32.CreateSection( (short)0xff, BamlRecord.LastFlagsSection );
        
        private static BitVector32.Section _typeIdHighSection
            = BitVector32.CreateSection( (short)0xff, _typeIdLowSection );

        private static BitVector32.Section _useTypeConverter
            = BitVector32.CreateSection( 1, _typeIdHighSection );

        private static BitVector32.Section _isInjected
            = BitVector32.CreateSection( 1, _useTypeConverter );

        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _isInjected; }
        }
    }



    //+----------------------------------------------------------------------------------------------------------------
    //
    //  BamlNamedElementStartRecord
    //
    //  This is a BamlElementStartRecord that also carries an element name.
    //
    //  This is currently internal, used only for templates.  The original intent for this record was that
    //  it become the new design for named objects; any object with an x:Name set, would have that name
    //  incorporated into the element start record.  But that design did not happen, instead the 
    //  property attribute are re-ordered such that the name always immediately follows the element
    //  start record.  So this should be removed, and the template code updated accordingly.  (And in fact,
    //  the template design should be updated so as not to be reliant on naming, as that is too fragile.)
    //
    //+----------------------------------------------------------------------------------------------------------------
#if !PBTCOMPILER
    internal class BamlNamedElementStartRecord : BamlElementStartRecord
    {
#region Methods

        #if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            TypeId = bamlBinaryReader.ReadInt16();
            RuntimeName = bamlBinaryReader.ReadString();
        }
        #endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(TypeId);

            if( RuntimeName != null )
            {
                bamlBinaryWriter.Write(RuntimeName);
            }
        }
        
#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlNamedElementStartRecord newRecord = (BamlNamedElementStartRecord)record;
            newRecord._isTemplateChild = _isTemplateChild;
            newRecord._runtimeName = _runtimeName;
        }
#endif        

#endregion Methods

#region Properties

        internal string RuntimeName
        {
            get { return _runtimeName; }
            set { _runtimeName = value; }
        }

        // This flag is used by templates to indicate that an ElementStart
        // record is for an object that will be a template child.  We had to add
        // this to allow some validation during template application.  This isn't 
        // a good solution, because we shouldn't have this record understanding
        // template children.  But the long-term plan is to break the template design
        // away from a dependence on names, at which point this whole BamlNamedElementStartRecord
        // will go away.
        private bool _isTemplateChild = false;
        internal bool IsTemplateChild
        {
            get { return _isTemplateChild; }
            set { _isTemplateChild = value; }
        }

#endregion Properties


#region Data

        // Id of the type of this object
        string _runtimeName = null;

#endregion Data
    }
#endif

    // Marks a block that has deferable content.  This record contains the size
    // of the deferable section, excluding the start and end records themselves.
    internal class BamlDeferableContentStartRecord : BamlRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            ContentSize = bamlBinaryReader.ReadInt32();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            _contentSizePosition = bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            bamlBinaryWriter.Write(ContentSize);
        }

        // Update the size of the content contained between the end of the start
        // record and the beginning of the end record.  The size of the content is
        // usually not known when the start record is written out.
        internal void UpdateContentSize(
            Int32         contentSize,
            BinaryWriter  bamlBinaryWriter)
        {
            Debug.Assert(_contentSizePosition != -1,
                    "Must call WriteRecordData before updating content size");

            // Use relative positions to reduce the possibility of truncation,
            // since Seek takes a 32 bit int, but position is a 64 bit int.
            Int64 existingPosition = bamlBinaryWriter.Seek(0, SeekOrigin.Current);
            Int32 deltaPosition = (Int32)(_contentSizePosition-existingPosition);

            bamlBinaryWriter.Seek(deltaPosition, SeekOrigin.Current);
            bamlBinaryWriter.Write(contentSize);
            bamlBinaryWriter.Seek((int)(-ContentSizeSize-deltaPosition), SeekOrigin.Current);
        }
        
#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlDeferableContentStartRecord newRecord = (BamlDeferableContentStartRecord)record;
            newRecord._contentSize = _contentSize;
            newRecord._contentSizePosition = _contentSizePosition;
            newRecord._valuesBuffer = _valuesBuffer;
        }
#endif        

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.DeferableContentStart; }
        }

        internal Int32 ContentSize
        {
            get { return _contentSize; }
#if !PBTCOMPILER
            set { _contentSize = value; }
#endif
        }

        internal override Int32 RecordSize
        {
            get { return 4; }
            set { Debug.Assert(value == -1, "Wrong size set for element record"); }
        }

#if !PBTCOMPILER

        /// <summary>
        /// For the case of a ResourceDictionary inside template content, we read 
        /// the dictionary values into a byte array while creating the template 
        /// content. Later during template instantiation when the dictionary instance 
        /// is created we use this buffer to create a memory stream so that the 
        /// ResourceDictionary can use it to RealizeDeferredContent. This is required 
        /// because at template instantiation time we do not have a stream to work with. 
        /// The reader operates on a linked list of BamlRecords.
        /// </summary>
        internal byte[] ValuesBuffer
        {
            get { return _valuesBuffer; }
            set { _valuesBuffer = value; }
        }
#endif

#endregion Properties


#region Data

        // Size of the ContentSize field written out to the baml stream.  This
        // must be kept in sync with the size of the _contentSize field.
        const Int64 ContentSizeSize = 4;

        // Size of the content between the end of the start record and the
        // beginning of the end record for this element.
        Int32 _contentSize = - 1;

        // Absolute position in the stream where ContentSize is written.
        Int64 _contentSizePosition = -1;

#if !PBTCOMPILER

        byte[] _valuesBuffer;

#endif

#endregion Data
    }

    //+----------------------------------------------------------------------------------------------------------------
    //
    //  BamlStaticResourceStartRecord
    //
    //  This record marks the start of a StaticResourceExtension within the header for a deferred section. 
    //
    //+----------------------------------------------------------------------------------------------------------------
    
    internal class BamlStaticResourceStartRecord : BamlElementStartRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.StaticResourceStart; }
        }

#endregion Properties
    }

    //+----------------------------------------------------------------------------------------------------------------
    //
    //  BamlStaticResourceEndRecord
    //
    //  This record marks the end of a StaticResourceExtension within the header for a deferred section. 
    //
    //+----------------------------------------------------------------------------------------------------------------
    
    internal class BamlStaticResourceEndRecord : BamlElementEndRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.StaticResourceEnd; }
        }

#endregion Properties
    }

    //+----------------------------------------------------------------------------------------------------------------
    //
    //  BamlOptimizedStaticResourceRecord
    //
    //  This record represents an optimized StaticResourceExtension within the header for a deferred section. 
    //
    //+----------------------------------------------------------------------------------------------------------------
    
    internal class BamlOptimizedStaticResourceRecord : BamlRecord, IOptimizedMarkupExtension
    {
#region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            byte flags = bamlBinaryReader.ReadByte();
            ValueId = bamlBinaryReader.ReadInt16();

            IsValueTypeExtension = (flags & TypeExtensionValueMask) != 0;
            IsValueStaticExtension = (flags & StaticExtensionValueMask) != 0;
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            byte flags = 0;
            if (IsValueTypeExtension)
            {
                flags |= TypeExtensionValueMask;
            }
            else if (IsValueStaticExtension)
            {
                flags |= StaticExtensionValueMask;
            }
            bamlBinaryWriter.Write(flags);
            bamlBinaryWriter.Write(ValueId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlOptimizedStaticResourceRecord newRecord = (BamlOptimizedStaticResourceRecord)record;
            newRecord._valueId = _valueId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.OptimizedStaticResource; }
        }

        public short ExtensionTypeId
        {
            get { return (short)KnownElements.StaticResourceExtension; }
        }

        // StringId if the value is a string
        // TypeId if the value is a TypeExtension
        // AttributeId of the member if the value is a StaticExtension
        public short ValueId
        {
            get { return _valueId; }
            set { _valueId = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 3; }
            set { Debug.Assert(value == -1, "Wrong size set for complex prop record"); }
        }

        // If the value is itself a simple TypeExtension
        public bool IsValueTypeExtension
        {
            get { return _flags[_isValueTypeExtensionSection] == 1 ? true : false; }
            set { _flags[_isValueTypeExtensionSection] = value ? 1 : 0; }
        }

        // If the value is itself a simple StaticExtension
        public bool IsValueStaticExtension
        {
            get { return _flags[_isValueStaticExtensionSection] == 1 ? true : false; }
            set { _flags[_isValueStaticExtensionSection] = value ? 1 : 0; }
        }

#endregion Properties

#region Data

        short _valueId = 0;

        private static readonly byte TypeExtensionValueMask = 0x01;
        private static readonly byte StaticExtensionValueMask = 0x02;

        // Allocate space in _flags.
        private static BitVector32.Section _isValueTypeExtensionSection
            = BitVector32.CreateSection(1, BamlRecord.LastFlagsSection);

        private static BitVector32.Section _isValueStaticExtensionSection
            = BitVector32.CreateSection(1, _isValueTypeExtensionSection);

        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _isValueStaticExtensionSection; }
        }
    
#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} extn(StaticResourceExtension) valueId({1})",
                                 RecordType, _valueId);
        }
#endif
    
#endregion Data


    }

    //+----------------------------------------------------------------------------------------------------------------
    //
    //  BamlStaticResourceIdRecord
    //
    //  This BamlRecord is an identifier for a StaticResourceExtension within the header for a deferred section.
    //
    //+----------------------------------------------------------------------------------------------------------------
    
    internal class BamlStaticResourceIdRecord : BamlRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            StaticResourceId = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(StaticResourceId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlStaticResourceIdRecord newRecord = (BamlStaticResourceIdRecord)record;
            newRecord._staticResourceId = _staticResourceId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.StaticResourceId; }
        }

        internal override Int32 RecordSize
        {
            get { return 2; }
            set { Debug.Assert(value == -1, "Wrong size set for complex prop record"); }
        }

        internal short StaticResourceId
        {
            get { return _staticResourceId; }
            set { _staticResourceId = value; }
        }

#endregion Properties


#region Data

        short _staticResourceId = -1;

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} staticResourceId({1})",
                                 RecordType, StaticResourceId);
        }
#endif

        
#endregion Data

    }

    //+----------------------------------------------------------------------------------------------------------------
    //
    //  BamlPropertyWithStaticResourceIdRecord
    //
    //  This BamlRecord represents a BamlPropertyRecord with a StaticResourceId as place holder for 
    //  a StaticResourceExtension within a deferred section.
    //
    //+----------------------------------------------------------------------------------------------------------------
    
    internal class BamlPropertyWithStaticResourceIdRecord : BamlStaticResourceIdRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId = bamlBinaryReader.ReadInt16();
            StaticResourceId = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(AttributeId);
            bamlBinaryWriter.Write(StaticResourceId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlPropertyWithStaticResourceIdRecord newRecord = (BamlPropertyWithStaticResourceIdRecord)record;
            newRecord._attributeId = _attributeId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.PropertyWithStaticResourceId; }
        }

        internal override Int32 RecordSize
        {
            get { return 4; }
            set { Debug.Assert(value == -1, "Wrong size set for complex prop record"); }
        }
        
        // Id of the property whose value is the simple SR
        internal short AttributeId
        {
            get { return _attributeId; }
            set { _attributeId = value; }
        }

#endregion Properties

#region Data

        short _attributeId = -1;

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} attr({1}) staticResourceId({2})",
                                 RecordType, AttributeId, StaticResourceId);
        }
#endif
        
#endregion Data

    }


    // Text content between the begin and end tag of an element.
    internal class BamlTextRecord : BamlStringValueRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.Text; }
        }

#endregion Properties
    }

    // This is a text record within a [Static/Dynamic]ResourceExtension.
    internal class BamlTextWithIdRecord : BamlTextRecord
    {
#region Methods
        
#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            ValueId  =  bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(ValueId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlTextWithIdRecord newRecord = (BamlTextWithIdRecord)record;
            newRecord._valueId = _valueId;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.TextWithId; }
        }

        internal Int16 ValueId
        {
            get { return _valueId; }
            set { _valueId = value; }
        }

#endregion Properties

#region Data
        Int16 _valueId;
#endregion Data
    }

    // Text content between the begin and end tag of an element that will be parsed using a type converter.
    internal class BamlTextWithConverterRecord : BamlTextRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            base.LoadRecordData(bamlBinaryReader);
            ConverterTypeId  = bamlBinaryReader.ReadInt16();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            base.WriteRecordData(bamlBinaryWriter);
            bamlBinaryWriter.Write(ConverterTypeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlTextWithConverterRecord newRecord = (BamlTextWithConverterRecord)record;
            newRecord._converterTypeId = _converterTypeId;
        }
#endif

#endregion Methods

#region Properties

        // The following are stored in the baml stream

        // ID of this type converter.  Referenced in other baml records where a
        // Type is needed.
        internal short ConverterTypeId
        {
            get { return _converterTypeId; }
            set { _converterTypeId = value; }
        }

        // Additional properties not stored in the baml stream

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.TextWithConverter; }
        }

#endregion Properties

#region Data

        short _converterTypeId = 0;

#endregion Data
    }

    // Marks the start of a Baml document.  This must always be the first
    // record in a BAML stream.   It contains version information, and other
    // document wide directives.
    internal class BamlDocumentStartRecord : BamlRecord
    {
#region Methods

        // Writes data at the current position.  The seek pointer points
        // to byte after the end of record when done.
        internal override void Write(BinaryWriter bamlBinaryWriter)
        {
            // Remember the file location of this baml record.  This
            // is needed if we have to come back later to update the sync mode.
            if (FilePos == -1 && bamlBinaryWriter != null)
            {
                FilePos = bamlBinaryWriter.Seek(0,SeekOrigin.Current);
            }

            base.Write(bamlBinaryWriter);
        }

        // Adjust seeks pointer to this Record and updates the data.
        // Then sets seek pointer pack to original.
        // NOTE:  This will ONLY work for file sizes under 2 gig.  This is
        //        not a problem for current useage, since this is mostly used
        //        when updating LoadAsync attribute on the DocumentStart record,
        //        which is usually set on the first element in the xaml file.
        internal virtual void UpdateWrite(BinaryWriter bamlBinaryWriter)
        {
            // default implementation, class should override if
            // wants to optimize to only update dirty data.
            long currentPosiition = bamlBinaryWriter.Seek(0,SeekOrigin.Current);

            // seek to original record position.

            Debug.Assert(FilePos != -1,"UpdateWrite called but Write Never was");

            // Note: This only works for files up to 2 gig in length.
            //       This is not a new restriction, but it should be
            //       fixed to work with larger files...
            bamlBinaryWriter.Seek((int)FilePos,SeekOrigin.Begin);
            Write(bamlBinaryWriter);
            bamlBinaryWriter.Seek( (int) currentPosiition,SeekOrigin.Begin);
        }

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            LoadAsync =  bamlBinaryReader.ReadBoolean();
            MaxAsyncRecords =  bamlBinaryReader.ReadInt32();
            DebugBaml = bamlBinaryReader.ReadBoolean();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(LoadAsync);
            bamlBinaryWriter.Write(MaxAsyncRecords);
            bamlBinaryWriter.Write(DebugBaml);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlDocumentStartRecord newRecord = (BamlDocumentStartRecord)record;
            newRecord._maxAsyncRecords = _maxAsyncRecords;
            newRecord._loadAsync = _loadAsync;
            newRecord._filePos = _filePos;
            newRecord._debugBaml = _debugBaml;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.DocumentStart; }
        }

        internal bool LoadAsync
        {
            get { return _loadAsync; }
#if !PBTCOMPILER
            set { _loadAsync = value; }
#endif
        }

        internal int MaxAsyncRecords
        {
            get { return _maxAsyncRecords; }
            set { _maxAsyncRecords = value; }
        }

        // Position in the baml file stream
        internal long FilePos
        {
            get { return _filePos; }
            set { _filePos  = value; }
        }

        // Are there Debug Baml Records in this Baml Stream
        internal bool DebugBaml
        {
            get { return _debugBaml; }
            set { _debugBaml  = value; }
        }

#endregion Properties

#region Data
        int         _maxAsyncRecords  = -1;
        bool        _loadAsync = false;
        long        _filePos = -1;
        bool        _debugBaml = false;
#endregion Data
    }

    // This marks the end tag of an element
    internal class BamlElementEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ElementEnd; }
        }

#endregion Properties
    }

    // This marks the start tag of an element being used as the key for an IDictionary
    internal class BamlKeyElementStartRecord : BamlDefAttributeKeyTypeRecord, IBamlDictionaryKey
    {
        internal BamlKeyElementStartRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
        }

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.KeyElementStart; }
        }

#endregion Properties
    }

    // This marks the end tag of an element being used as the key for an IDictionary
    internal class BamlKeyElementEndRecord : BamlElementEndRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.KeyElementEnd; }
        }

#endregion Properties
    }

    // This marks the end of the baml stream, or document.
    internal class BamlDocumentEndRecord : BamlRecord
    {
#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.DocumentEnd; }
        }

#endregion Properties
    }

    // The following records are used internally in the baml stream to
    // define attribute (eg - property), type and assembly information
    // for records that follow later on in the stream.  They are never
    // publically exposed

    // Information about an assembly where a type is defined
    internal class BamlAssemblyInfoRecord : BamlVariableSizedRecord
    {
        internal BamlAssemblyInfoRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
            AssemblyId = -1;
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AssemblyId       =  bamlBinaryReader.ReadInt16();
            AssemblyFullName =  bamlBinaryReader.ReadString();
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            // write out an int for record size but we'll go back and fill
            bamlBinaryWriter.Write(AssemblyId);
            bamlBinaryWriter.Write(AssemblyFullName);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlAssemblyInfoRecord newRecord = (BamlAssemblyInfoRecord)record;
            newRecord._assemblyFullName = _assemblyFullName;
            newRecord._assembly = _assembly;
        }
#endif

#endregion Methods

#region Properties

        // The following are stored in the baml stream

        // ID of this assembly
        internal short AssemblyId
        {
            get
            {
                short value = (short) _flags[_assemblyIdLowSection];
                value |= (short) (_flags[_assemblyIdHighSection] << 8);

                return value;
            }

            set
            {
                _flags[_assemblyIdLowSection] = (short)  (value & 0xff);
                _flags[_assemblyIdHighSection] = (short) ((value & 0xff00) >> 8);
            }
        }

        // Allocate space in _flags.
        // BitVector32 doesn't support 16 bit sections, so we have to break
        // it up into 2 sections.

        private static BitVector32.Section _assemblyIdLowSection
            = BitVector32.CreateSection( (short)0xff, BamlVariableSizedRecord.LastFlagsSection );
        
        private static BitVector32.Section _assemblyIdHighSection
            = BitVector32.CreateSection( (short)0xff, _assemblyIdLowSection );
        
#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _assemblyIdHighSection; }
        }
#endif

        // Full name of this assembly, excluding any suffix.  This has
        // the format "AssemblyName, Version, Culture, PublicKeyToken" when we
        // have a true full name.  Sometimes we aren't given the full assembly
        // name, in which case the full name is the same as the short name.
        internal string AssemblyFullName
        {
            get { return _assemblyFullName; }
            set { _assemblyFullName = value; }
        }

        // The following are not part of the BAML stream

        // Identify type of record
        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.AssemblyInfo; }
        }

        // The actual loaded assembly
        internal Assembly Assembly
        {
            get { return _assembly; }
            set { _assembly = value; }
        }


#endregion Properties


#region Data

        string   _assemblyFullName;
        Assembly _assembly;

#endregion Data
    }

    // Information about a type for an element, object or property
    internal class BamlTypeInfoRecord : BamlVariableSizedRecord
    {
        internal BamlTypeInfoRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
            TypeId = -1;
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            TypeId       =   bamlBinaryReader.ReadInt16();
            AssemblyId   =   bamlBinaryReader.ReadInt16();
            TypeFullName =   bamlBinaryReader.ReadString();

            // Note that the upper 4 bits of the AssemblyId are used for flags
            _typeInfoFlags = (TypeInfoFlags)(AssemblyId >> 12);
            _assemblyId &= 0x0FFF;
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            // write out an int for record size but we'll go back and fill
            bamlBinaryWriter.Write(TypeId);
            // Note that the upper 4 bits of the AssemblyId are used for flags
            bamlBinaryWriter.Write((short)(((ushort)AssemblyId) | (((ushort)_typeInfoFlags) << 12)));
            bamlBinaryWriter.Write(TypeFullName);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlTypeInfoRecord newRecord = (BamlTypeInfoRecord)record;
            newRecord._typeInfoFlags = _typeInfoFlags;
            newRecord._assemblyId = _assemblyId;
            newRecord._typeFullName = _typeFullName;
            newRecord._type = _type;
        }
#endif

#endregion Methods

#region Properties

        // The following are stored in the baml stream

        // ID of this type.  Refenced in other baml records where a
        // Type is needed.
        internal short TypeId
        {
            get
            {
                short value = (short) _flags[_typeIdLowSection];
                value |= (short) (_flags[_typeIdHighSection] << 8);

                return value;
            }

            set
            {
                _flags[_typeIdLowSection] = (short)  (value & 0xff);
                _flags[_typeIdHighSection] = (short) ((value & 0xff00) >> 8);
            }
        }

        // Assembly id of the assembly where this type is defined.
        // NOTE:  This is always positive in BAML files, but can be set
        //        to -1 for known types when created programmatically.
        internal short AssemblyId
        {
            get { return _assemblyId; }
            set 
            { 
                // Make sure we don't intrude on the Flags portion of the assembly ID
                if (_assemblyId > 0x0FFF)
                {
                    throw new XamlParseException(SR.Get(SRID.ParserTooManyAssemblies));
                }
                _assemblyId = value;
            }
        }

        // Fully qualified name of type, including namespace
        internal string TypeFullName
        {
            get { return _typeFullName; }
            set { _typeFullName = value; }
        }

        // Additional properties not stored in the baml stream

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.TypeInfo; }
        }

#if !PBTCOMPILER
        // Actual type.  Filled in here when xaml is used to create
        // a tree, and the token reader knows the type
        internal Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        // Extract the namespace from the type full name and return
        // it.  We are assuming here that the type full name has a single
        // classname at the end and we are not refering to a nested class...
        internal string ClrNamespace
        {
            get
            {
                int periodIndex = _typeFullName.LastIndexOf('.');
                return periodIndex > 0 ?
                            _typeFullName.Substring(0, periodIndex) :
                            string.Empty;
            }
        }
#endif

        // True if there is a serializer associated with this type
        internal virtual bool HasSerializer
        {
            get { return false; }
        }

        internal bool IsInternalType
        {
#if !PBTCOMPILER
            get
            {
                return ((_typeInfoFlags & TypeInfoFlags.Internal) == TypeInfoFlags.Internal);
            }
#endif

            set
            {
                // Don't allow resetting to false (i.e. converting back top public if
                // it becomes non-public, for added safety.
                if (value)
                {
                    _typeInfoFlags |= TypeInfoFlags.Internal;
                }
            }
        }

#endregion Properties

#region Data

        // Allocate space in _flags.
        // BitVector32 doesn't support 16 bit sections, so we have to break
        // it up into 2 sections.

        private static BitVector32.Section _typeIdLowSection
            = BitVector32.CreateSection( (short)0xff, BamlVariableSizedRecord.LastFlagsSection );
        
        private static BitVector32.Section _typeIdHighSection
            = BitVector32.CreateSection( (short)0xff, _typeIdLowSection );
        
#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _typeIdHighSection; }
        }
#endif


        // Flags contained in TypeInfo that give additional information
        // about the type that is determined at compile time.
        [Flags]
        private enum TypeInfoFlags : byte
        {
            Internal             = 0x1,
            UnusedTwo            = 0x2,
            UnusedThree          = 0x4,
        }

        TypeInfoFlags _typeInfoFlags = 0;
        short         _assemblyId = -1;
        string        _typeFullName;
#if !PBTCOMPILER
        Type          _type;
#endif

#endregion Data


    }

    // Type info record for a type that has a custom serializer associated with it.
    // This gives the serializer type that will be used when deserializing this type
    internal class BamlTypeInfoWithSerializerRecord : BamlTypeInfoRecord
    {
        internal BamlTypeInfoWithSerializerRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            base.LoadRecordData(bamlBinaryReader);
            SerializerTypeId  =   bamlBinaryReader.ReadInt16();
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            base.WriteRecordData(bamlBinaryWriter);
            bamlBinaryWriter.Write(SerializerTypeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlTypeInfoWithSerializerRecord newRecord = (BamlTypeInfoWithSerializerRecord)record;
            newRecord._serializerTypeId = _serializerTypeId;
            newRecord._serializerType = _serializerType;
        }
#endif

#endregion Methods

#region Properties

        // The following are stored in the baml stream

        // ID of this type.  Refenced in other baml records where a
        // Type is needed.
        internal short SerializerTypeId
        {
            get { return _serializerTypeId; }
            set { _serializerTypeId = value; }
        }

        // Additional properties not stored in the baml stream

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.TypeSerializerInfo; }
        }

#if !PBTCOMPILER
        // Actual type of associated serializer.  Filled in here when xaml is used to create
        // a tree, and the token reader knows the type of the serializer, or
        // when we are reading the baml file and have determined the
        // serializer type.
        internal Type SerializerType
        {
            get { return _serializerType; }
            set { _serializerType = value; }
        }
#endif

        // True if there is a serializer associated with this type.  A serializer
        // will never be the first type object in a baml file, so its type ID will
        // never be 0.  Any other ID indicates we have a serializer.
        internal override bool HasSerializer
        {
            get
            {
                Debug.Assert( SerializerTypeId != 0 );
                return true;
            }
        }

#endregion Properties

#region Data

        short _serializerTypeId = 0;
#if !PBTCOMPILER
        Type _serializerType;
#endif

#endregion Data

    }

    // Used for mapping properties and events to an owner type, given the
    // name of the attribute.  Note that Attribute is used for historical
    // reasons and for similarities to Xml attributes.  For us attributes
    // are just properties and events.
    internal class BamlAttributeInfoRecord : BamlVariableSizedRecord
    {
        internal BamlAttributeInfoRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
            AttributeUsage = BamlAttributeUsage.Default;
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId  =   bamlBinaryReader.ReadInt16();
            OwnerTypeId  =   bamlBinaryReader.ReadInt16();
            AttributeUsage = (BamlAttributeUsage)bamlBinaryReader.ReadByte();
            Name  =          bamlBinaryReader.ReadString();
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            // write out an int for record size but we'll go back and fill
            bamlBinaryWriter.Write(AttributeId);
            bamlBinaryWriter.Write(OwnerTypeId);
            bamlBinaryWriter.Write((Byte)AttributeUsage);
            bamlBinaryWriter.Write(Name);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlAttributeInfoRecord newRecord = (BamlAttributeInfoRecord)record;
            newRecord._ownerId = _ownerId;
            newRecord._attributeId = _attributeId;
            newRecord._name = _name;
            newRecord._ownerType = _ownerType;
            newRecord._Event = _Event;
            newRecord._dp = _dp;
            newRecord._ei = _ei;
            newRecord._pi = _pi;
            newRecord._smi = _smi;
            newRecord._gmi = _gmi;
            newRecord._dpOrMiOrPi = _dpOrMiOrPi;
        }
#endif

#endregion Methods

#region Properties

        // The following 3 properties are stored in the Baml file and are read and
        // written by the BamlRecordReader and BamlXamlNodeWriter

        internal short OwnerTypeId
        {
            get { return _ownerId; }
            set { _ownerId = value; }
        }

        internal short AttributeId
        {
            set { _attributeId = value; }
            get { return _attributeId; }
        }

        internal string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        // The following properties are derived at runtime from the above 3 properties using
        // the Mapper.  Which are set depends on the attribute and the context in which it is
        // used.

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.AttributeInfo; }
        }

#if !PBTCOMPILER
        // Return type of property.  Note that this uses the same logic as
        // Mapper.GetPropertyType but uses the cached values of DP, PropInfo
        // and AttachedPropertySetter.
        internal Type GetPropertyType()
        {
            Type validType = null;
            DependencyProperty dp = DP;
            if (dp == null)
            {
                MethodInfo methodInfo = AttachedPropertySetter;
                if (methodInfo == null)
                {
                    PropertyInfo propInfo = PropInfo;
                    validType = propInfo.PropertyType;
                }
                else
                {
                    ParameterInfo[] paramInfo = methodInfo.GetParameters();
                    validType = paramInfo[1].ParameterType;
                }
            }
            else
            {
                validType = dp.PropertyType;
            }
            return validType;
        }
#endif

        /// <summary>
        /// Set the PropertyMember, which can is assumed to be a MethodInfo for
        /// the static setter method for a DP or a PropertyInfo for the clr property
        /// </summary>
        /// <remarks>
        /// The possibility of having multiple member info cached for an attribute is when a
        /// dependency property that does not belong to the default namespace is used in once
        /// in a once with a namespace prefix and once without it. When it has a namespace
        /// prefix we correctly find the dependency property for it. However when it does not
        /// have a namespace prefix it the parser tries to look it up in the default namespace
        /// and falls back to using the clr wrapper's property info for it instead. Another
        /// scenario that requires caching more than one property info is when a dependency
        /// property has both a static settor and a clr wrapper.
        /// </remarks>
        internal void SetPropertyMember (object propertyMember)
        {
            Debug.Assert((propertyMember is MethodInfo) || (propertyMember is PropertyInfo)
                        || (KnownTypes.Types[(int)KnownElements.DependencyProperty].IsAssignableFrom(propertyMember.GetType())),
                "Cache can hold either a MethodInfo and/or a PropertyInfo and/or a DependencyProperty for a given attribute");

            if (PropertyMember == null)
            {
                PropertyMember = propertyMember;
            }
            else
            {
                // Cache a additional MemberInfo for the given attribute
                object[] arr = PropertyMember as object[];
                if (arr == null)
                {
                    arr = new object[3];
                    arr[0] = PropertyMember;
                    arr[1] = propertyMember;
                }
                else
                {
                    Debug.Assert(arr.Length == 3 && arr[0] != null && arr[1] != null);
                    arr[2] = propertyMember;
                }
            }
        }

        /// <summary>
        /// Return the PropertyMember, which can is assumed to be a MethodInfo for
        /// the static setter method for a DP or a PropertyInfo for the clr property
        /// </summary>
        /// <remarks>
        /// The possibility of having multiple member info cached for an attribute is when a
        /// dependency property that does not belong to the default namespace is used in once
        /// in a once with a namespace prefix and once without it. When it has a namespace
        /// prefix we correctly find the dependency property for it. However when it does not
        /// have a namespace prefix it the parser tries to look it up in the default namespace
        /// and falls back to using the clr wrapper's property info for it instead. Another
        /// scenario that requires caching more than one property info is when a dependency
        /// property has both a static settor and a clr wrapper.
        /// </remarks>
        internal object GetPropertyMember(bool onlyPropInfo)
        {
            if (PropertyMember == null ||
                PropertyMember is MemberInfo ||
                KnownTypes.Types[(int)KnownElements.DependencyProperty].IsAssignableFrom(PropertyMember.GetType( )) )
            {
                if (onlyPropInfo)
                {
#if PBTCOMPILER
                    return PropertyMember as PropertyInfo;
#else
                    return PropInfo;
#endif
                }
                else
                {
                    return PropertyMember;
                }
            }
            else
            {
                // The attribute has multiple member info. Choose which one to return.
                object[] arr = (object[])PropertyMember;
                Debug.Assert(arr.Length == 3 && arr[0] != null && arr[1] != null);

                // If someone queries any MemberInfo for the given attribute then we return the
                // first member info cached for it. If they are looking specifically for a
                // PropertyInfo we try and find them one.
                if (onlyPropInfo)
                {
                    if (arr[0] is PropertyInfo)
                    {
                        return (PropertyInfo)arr[0];
                    }
                    else if (arr[1] is PropertyInfo)
                    {
                        return (PropertyInfo)arr[1];
                    }
                    else
                    {
                        return arr[2] as PropertyInfo;
                    }
                }
                else
                {
                    return arr[0];
                }
            }
        }

        // Cached value of the DependencyProperty, MethodInfo for the static setter
        // method, or the PropertyInfo for a given property.  If this is an
        // event, then this is null.
        internal object PropertyMember
        {
            get { return _dpOrMiOrPi; }
            set { _dpOrMiOrPi = value; }
        }

#if !PBTCOMPILER

        // The cached type of the owner or declarer of this property
        internal Type OwnerType
        {
            get { return _ownerType; }
            set { _ownerType = value; }
        }

        // Cached value of the routed event id, if this attribute is for a
        // routed event.  If not a routed event, this is null.
        internal RoutedEvent Event
        {
            get {  return _Event; }
            set { _Event = value; }
        }

        // Cached value of DP, if available
        internal DependencyProperty DP
        {
            get
            {
                if (null != _dp)
                    return _dp;
                else
                    return _dpOrMiOrPi as DependencyProperty;
            }
            set
            {
                _dp = value;
                if (_dp != null)
                {
                    // Release the other copy of the string
                    _name = _dp.Name;
                }
            }
        }

        // Cached value of static property setter method info, if available
        internal MethodInfo AttachedPropertySetter
        {
            get
            {
                return _smi;
            }

            set
            {
                _smi = value;
            }
        }

        // Cached value of static property getter method info, if available
        internal MethodInfo AttachedPropertyGetter
        {
            get
            {
                return _gmi;
            }

            set
            {
                _gmi = value;
            }
        }

        // Cached value of EventInfo, if available
        internal EventInfo EventInfo
        {
            get { return _ei; }
            set { _ei = value; }
        }

        // Cached value of PropertyInfo, if available
        internal PropertyInfo PropInfo
        {
            get
            {
                return _pi;
            }
            set { _pi = value; }
        }

        internal bool IsInternal
        {
            get
            {
                return _flags[_isInternalSection] == 1 ? true : false;
            }

            set
            {
                _flags[_isInternalSection] = value ? 1 : 0;
            }
        }
#endif

        // Some attributes have special usage, such as setting the XmlLang and XmlSpace
        // strings in the parser context.  This is flagged with this property
        internal BamlAttributeUsage AttributeUsage
        {
            get
            {
                return (BamlAttributeUsage) _flags[_attributeUsageSection];
            }

            set
            {
                _flags[_attributeUsageSection] = (int) value;
            }
        }


        // Allocate space in _flags.

        private static BitVector32.Section _isInternalSection
            = BitVector32.CreateSection( 1, BamlVariableSizedRecord.LastFlagsSection );
        
        private static BitVector32.Section _attributeUsageSection
            = BitVector32.CreateSection( 3, _isInternalSection );
        
#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _attributeUsageSection; }
        }
#endif

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} owner={1} attr({2}) is '{3}'",
                                 RecordType, GetTypeName(OwnerTypeId), AttributeId, _name);
        }
#endif

#region Data

        short _ownerId;
        short _attributeId;
        string _name;

#if !PBTCOMPILER
        Type               _ownerType = null;
        RoutedEvent        _Event = null;
        DependencyProperty _dp = null;
        EventInfo          _ei = null;
        PropertyInfo       _pi = null;
        MethodInfo         _smi = null;
        MethodInfo         _gmi = null;
#endif

        object             _dpOrMiOrPi = null;   // MethodInfo, PropertyInfo or DependencyProperty

#endregion Data
    }

    // Information about a String that is an entry in the String table.
    internal class BamlStringInfoRecord : BamlVariableSizedRecord
    {
        internal BamlStringInfoRecord()
        {
            Pin(); // Don't allow this record to be recycled in the read cache.
            StringId = -1;
        }

#region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            StringId = bamlBinaryReader.ReadInt16();
            Value    = bamlBinaryReader.ReadString();
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            // write out an int for string Id
            bamlBinaryWriter.Write(StringId);
            bamlBinaryWriter.Write(Value);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlStringInfoRecord newRecord = (BamlStringInfoRecord)record;
            newRecord._value = _value;
        }
#endif

#endregion Methods

#region Properties
        // Resource Identifier pointing to the StringTable Entry
        internal short StringId
        {
            get
            {
                short value = (short) _flags[_stringIdLowSection];
                value |= (short) (_flags[_stringIdHighSection] << 8);

                return value;
            }

            set
            {
                _flags[_stringIdLowSection] = (short)  (value & 0xff);
                _flags[_stringIdHighSection] = (short) ((value & 0xff00) >> 8);
            }
        }

        // Resource String
        internal string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        // Additional properties not stored in the baml stream
        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.StringInfo; }
        }

        // True if there is a serializer associated with this type
        internal virtual bool HasSerializer
        {
            get { return false; }
        }
#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} stringId({1}='{2}'",
                                 RecordType, StringId, _value);
        }
#endif

#region Data


        // Allocate space in _flags.
        // BitVector32 doesn't support 16 bit sections, so we have to break
        // it up into 2 sections.

        private static BitVector32.Section _stringIdLowSection
            = BitVector32.CreateSection( (short)0xff, BamlVariableSizedRecord.LastFlagsSection );
        
        private static BitVector32.Section _stringIdHighSection
            = BitVector32.CreateSection( (short)0xff, _stringIdLowSection );

#if !PBTCOMPILER        
        // This provides subclasses with a referece section to create their own section.
        internal new static BitVector32.Section LastFlagsSection
        {
            get { return _stringIdHighSection; }
        }
#endif

        string _value ;
#endregion Data
    }

    // Sets the content property context for an element
    internal class BamlContentPropertyRecord : BamlRecord
    {
        #region Methods

#if !PBTCOMPILER
        // LoadRecord specific data
        internal override void LoadRecordData(BinaryReader bamlBinaryReader)
        {
            AttributeId = bamlBinaryReader.ReadInt16();
        }
#endif

        // write record specific Data.
        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            // write out an int for attribute Id
            bamlBinaryWriter.Write(AttributeId);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlContentPropertyRecord newRecord = (BamlContentPropertyRecord)record;
            newRecord._attributeId = _attributeId;
        }
#endif

        #endregion Methods

        #region Properties
        // Id of the property being set as the context
        internal short AttributeId
        {
            get { return _attributeId; }
            set { _attributeId = value; }
        }

        // Additional properties not stored in the baml stream
        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.ContentProperty; }
        }

        // True if there is a serializer associated with this type
        internal virtual bool HasSerializer
        {
            get { return false; }
        }
        #endregion Properties

        #region Data
        short _attributeId = -1;
        #endregion Data
    }


    // Debugging Linenumber record.  Linenumber from the XAML
    internal class BamlLineAndPositionRecord : BamlRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            LineNumber = (uint) bamlBinaryReader.ReadInt32();
            LinePosition = (uint) bamlBinaryReader.ReadInt32();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(LineNumber);
            bamlBinaryWriter.Write(LinePosition);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlLineAndPositionRecord newRecord = (BamlLineAndPositionRecord)record;
            newRecord._lineNumber = _lineNumber;
            newRecord._linePosition = _linePosition;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.LineNumberAndPosition; }
        }

        // Id of the type of this object
        internal uint LineNumber
        {
            get { return _lineNumber; }
            set { _lineNumber = value; }
        }

        internal uint LinePosition
        {
            get { return _linePosition; }
            set { _linePosition = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 8; }
        }

        uint _lineNumber;
        uint _linePosition;

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} LineNum={1} Pos={2}", RecordType, LineNumber, LinePosition);
        }
#endif
    }


    // Debugging Line Position record.  Line Position from the XAML
    internal class BamlLinePositionRecord : BamlRecord
    {
#region Methods

#if !PBTCOMPILER
        internal override void  LoadRecordData(BinaryReader bamlBinaryReader)
        {
            LinePosition = (uint) bamlBinaryReader.ReadInt32();
        }
#endif

        internal override void WriteRecordData(BinaryWriter bamlBinaryWriter)
        {
            bamlBinaryWriter.Write(LinePosition);
        }

#if !PBTCOMPILER
        internal override void Copy(BamlRecord record)
        {
            base.Copy(record);

            BamlLinePositionRecord newRecord = (BamlLinePositionRecord)record;
            newRecord._linePosition = _linePosition;
        }
#endif

#endregion Methods

#region Properties

        internal override BamlRecordType RecordType
        {
            get { return BamlRecordType.LinePosition; }
        }

        internal uint LinePosition
        {
            get { return _linePosition; }
            set { _linePosition = value; }
        }

        internal override Int32 RecordSize
        {
            get { return 4; }
        }

        uint _linePosition;

#endregion Properties

#if !PBTCOMPILER
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} LinePos={1}", RecordType, LinePosition);
        }
#endif
    }
}
