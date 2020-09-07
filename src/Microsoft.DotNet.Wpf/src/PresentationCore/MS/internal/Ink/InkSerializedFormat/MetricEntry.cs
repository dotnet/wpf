// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Ink;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// Summary description for MetricEnty.
    /// </summary>
    internal enum MetricEntryType
    {
        Optional = 0,
        Must,
        Never,
        Custom,
    }

    // This is used while comparing two Metric Collections. This defines the relationship between
    // two collections when one is superset of the other and can replace the other in the global
    // list of collections
    internal enum SetType
    {
        SubSet = 0,
        SuperSet,
    }

    internal struct MetricEntryList
    {
        public KnownTagCache.KnownTagIndex Tag;
        public StylusPointPropertyInfo PropertyMetrics;

        public MetricEntryList (KnownTagCache.KnownTagIndex tag, StylusPointPropertyInfo prop)
        {
            Tag = tag;
            PropertyMetrics = prop;
        }
    }

    /// <summary>
    /// This class holds the MetricEntries corresponding to a PacketProperty in the form of a link list
    /// </summary>
    internal class MetricEntry
    {
        //Maximum buffer size required to store the largest MetricEntry
        private static int MAX_METRIC_DATA_BUFF = 24;

        private KnownTagCache.KnownTagIndex _tag = 0;
        private uint _size = 0;
        private MetricEntry _next;
        private byte[] _data = new Byte[MAX_METRIC_DATA_BUFF]; // We always allocate the max buffer needed to store the largest possible Metric Information blob
        private static MetricEntryList[] _metricEntryOptional;

        // helpers for Ink-local property metrics for X/Y coordiantes
        public static StylusPointPropertyInfo DefaultXMetric = MetricEntry_Optional[0].PropertyMetrics;

        public static StylusPointPropertyInfo DefaultYMetric = MetricEntry_Optional[1].PropertyMetrics;


        /// <summary>
        /// List of MetricEntry that may or may not appear in the serialized form depending on their default Metrics.
        /// </summary>
        public static MetricEntryList[] MetricEntry_Optional
        {
            get
            {
                if (_metricEntryOptional == null)
                {
                    _metricEntryOptional = new MetricEntryList[] {
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.X,                     StylusPointPropertyInfoDefaults.X),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.Y,                     StylusPointPropertyInfoDefaults.Y),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.Z,                     StylusPointPropertyInfoDefaults.Z),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.NormalPressure,        StylusPointPropertyInfoDefaults.NormalPressure),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.TangentPressure,       StylusPointPropertyInfoDefaults.TangentPressure),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.ButtonPressure,        StylusPointPropertyInfoDefaults.ButtonPressure),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.XTiltOrientation,      StylusPointPropertyInfoDefaults.XTiltOrientation),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.YTiltOrientation,      StylusPointPropertyInfoDefaults.YTiltOrientation),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.AzimuthOrientation,    StylusPointPropertyInfoDefaults.AzimuthOrientation),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.AltitudeOrientation,   StylusPointPropertyInfoDefaults.AltitudeOrientation),
                        new MetricEntryList (KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.TwistOrientation,      StylusPointPropertyInfoDefaults.TwistOrientation)};
}
                return _metricEntryOptional;
            }
        }

        /// <summary>
        /// List of MetricEntry whose Metric Information must appear in the serialized form and always written as they do not have proper default
        /// </summary>
        static KnownTagCache.KnownTagIndex[] MetricEntry_Must = new KnownTagCache.KnownTagIndex[]
        {
            (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.PitchRotation),
            (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.RollRotation),
            (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.YawRotation),
        };

        /// <summary>
        /// List of MetricEntry whose Metric information will never appear in the Serialized format and always ignored
        /// </summary>
        static KnownTagCache.KnownTagIndex[] MetricEntry_Never = new KnownTagCache.KnownTagIndex[]
        {
            (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.PacketStatus),
            (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.TimerTick),
            (KnownTagCache.KnownTagIndex)((uint)KnownIdCache.KnownGuidBaseIndex + (uint)KnownIdCache.OriginalISFIdIndex.SerialNumber),
        };

        /// <summary>
        /// Default StylusPointPropertyInfo for any property
        /// </summary>
        static StylusPointPropertyInfo DefaultPropertyMetrics = StylusPointPropertyInfoDefaults.DefaultValue;

        /// <summary>
        /// Gets or sets the Tag associated with this entry
        /// </summary>
        public KnownTagCache.KnownTagIndex Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                _tag = value;
            }
        }

        /// <summary>
        /// Gets the size associated with this entry
        /// </summary>
        public uint Size
        {
            get
            {
                return _size;
            }
        }

        /// <summary>
        /// Gets or Sets the data associated with this metric entry
        /// </summary>
        public byte[] Data
        {
            get
            {
                return _data;
            }
            set
            {
                byte [] data = (byte[])value;
                if( data.Length > MAX_METRIC_DATA_BUFF )
                    _size = (uint)MAX_METRIC_DATA_BUFF;
                else
                    _size = (uint)data.Length;
                for( int i = 0; i < (int)_size; i++ )
                    _data[i] = data[i];
            }
        }

        /// <summary>
        /// Compares a metricEntry object with this one and returns true or false
        /// </summary>
        /// <param name="metricEntry"></param>
        /// <returns></returns>
        public bool Compare(MetricEntry metricEntry)
        {
            if( Tag != metricEntry.Tag )
                return false;
            if( Size != metricEntry.Size )
                return false;
            for( int i = 0; i < Size; i++ )
            {
                if( Data[i] != metricEntry.Data[i] )
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Gets/Sets the next entry in the list
        /// </summary>
        public MetricEntry Next
        {
            get
            {
                return _next;
            }
            set
            {
                _next = value;
            }
        }

        /// <summary>
        /// Constructro
        /// </summary>
        public MetricEntry()
        {
        }

        /// <summary>
        /// Adds an entry in the list
        /// </summary>
        /// <param name="next"></param>
        public void Add(MetricEntry next)
        {
            if( null == _next )
            {
                _next = next;
                return;
            }
            MetricEntry prev = _next;
            while( null != prev.Next )
            {
                prev = prev.Next;
            }
            prev.Next = next;
        }

        /// <summary>
        /// Initializes a MetricEntry based on StylusPointPropertyInfo and default StylusPointPropertyInfo for the property
        /// </summary>
        /// <param name="originalInfo"></param>
        /// <param name="defaultInfo"></param>
        /// <returns></returns>
        public void Initialize(StylusPointPropertyInfo originalInfo, StylusPointPropertyInfo defaultInfo)
        {
            _size = 0;
            using (MemoryStream strm = new MemoryStream(_data))
            {
                if (!DoubleUtil.AreClose(originalInfo.Resolution, defaultInfo.Resolution))
                {
                    // First min value
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Minimum);
                    // Max value
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Maximum);
                    // Units
                    _size += SerializationHelper.Encode(strm, (uint)originalInfo.Unit);
                    // resolution
                    using (BinaryWriter bw = new BinaryWriter(strm))
                    {
                        bw.Write(originalInfo.Resolution);
                        _size += 4; // sizeof(float)
                    }
                }
                else if (originalInfo.Unit != defaultInfo.Unit)
                {
                    // First min value
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Minimum);
                    // Max value
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Maximum);
                    // Units
                    _size += SerializationHelper.Encode(strm, (uint)originalInfo.Unit);
                }
                else if (originalInfo.Maximum != defaultInfo.Maximum)
                {
                    // First min value
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Minimum);
                    // Max value
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Maximum);
                }
                else if (originalInfo.Minimum != defaultInfo.Minimum)
                {
                    _size += SerializationHelper.SignEncode(strm, originalInfo.Minimum);
                }
            }
        }

        /// <summary>
        /// Creates a metric entry based on a PropertyInfo and Tag and returns the Metric Entry Type created
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public MetricEntryType CreateMetricEntry(StylusPointPropertyInfo propertyInfo, KnownTagCache.KnownTagIndex tag)
        {
            // First create the default Metric entry based on the property and type of metric entry and then use that to initialize the
            // metric entry data.
            uint index = 0;
            Tag = tag;

            MetricEntryType type;
            if( IsValidMetricEntry(propertyInfo, Tag, out type, out index) )
            {
                switch(type)
                {
                    case MetricEntryType.Optional:
                    {
                        Initialize(propertyInfo, MetricEntry_Optional[index].PropertyMetrics);
                        break;
                    }
                    case MetricEntryType.Must :
                    case MetricEntryType.Custom:
                        Initialize(propertyInfo, DefaultPropertyMetrics);
                        break;
                    default:
                        throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("MetricEntryType was persisted with Never flag which should never happen"));
                }
            }
            return type;
        }
        /// <summary>
        /// This function checks if this packet property results in a valid metric entry. This will be a valid entry if
        /// 1. it is a custom property, 2. Does not belong to the global list of gaMetricEntry_Never, 3. Belongs to the
        /// global list of gaMetricEntry_Must and 4. Belongs to global list of gaMetricEntry_Optional and at least one of
        /// its metric values is different from the corresponding default.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="tag"></param>
        /// <param name="metricEntryType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        
        static bool IsValidMetricEntry(StylusPointPropertyInfo propertyInfo, KnownTagCache.KnownTagIndex tag, out MetricEntryType metricEntryType, out uint index)
        {
            index = 0;
            // If this is a custom property, check if all the Metric values are null or not. If they are then this is not a 
            // valid metric entry
            if (tag >= (KnownTagCache.KnownTagIndex)KnownIdCache.CustomGuidBaseIndex)
            {
                metricEntryType = MetricEntryType.Custom;
                if( Int32.MinValue == propertyInfo.Minimum &&
                    Int32.MaxValue == propertyInfo.Maximum &&
                    StylusPointPropertyUnit.None == propertyInfo.Unit &&
                    DoubleUtil.AreClose(1.0, propertyInfo.Resolution) )
                    return false;
                else
                    return true;
            }
            else
            {
                int ul;
                // First find the property in the gaMetricEntry_Never. If it belongs to this list,
                // we will never write the metric table for this prop. So return FALSE;
                for( ul = 0; ul < MetricEntry_Never.Length ; ul++ )
                {
                    if( MetricEntry_Never[ul] == tag )
                    {
                        metricEntryType = MetricEntryType.Never;
                        return false;
                    }
                }

                // Then search the property in the gaMetricEntry_Must list. If it belongs to this list,
                // we must always write the metric table for this prop. So return TRUE;
                for( ul = 0; ul<MetricEntry_Must.Length; ul++ )
                {
                    if( MetricEntry_Must[ul] == tag )
                    {
                        metricEntryType = MetricEntryType.Must;
                        if( propertyInfo.Minimum == DefaultPropertyMetrics.Minimum &&
                            propertyInfo.Maximum == DefaultPropertyMetrics.Maximum &&
                            propertyInfo.Unit == DefaultPropertyMetrics.Unit &&
                            DoubleUtil.AreClose(propertyInfo.Resolution, DefaultPropertyMetrics.Resolution ))
                            return false;
                        else
                            return true;
                    }
                }

                // Now seach it in the gaMetricEntry_Optional list. If it is there, check the metric values
                // agianst the default values and if there is any non default value, return TRUE;
                for( ul = 0; ul<MetricEntry_Optional.Length; ul++ )
                {
                    if( ((MetricEntryList)MetricEntry_Optional[ul]).Tag == tag )
                    {
                        metricEntryType = MetricEntryType.Optional;
                        if( propertyInfo.Minimum == MetricEntry_Optional[ul].PropertyMetrics.Minimum &&
                            propertyInfo.Maximum == MetricEntry_Optional[ul].PropertyMetrics.Maximum &&
                            propertyInfo.Unit == MetricEntry_Optional[ul].PropertyMetrics.Unit &&
                            DoubleUtil.AreClose(propertyInfo.Resolution, MetricEntry_Optional[ul].PropertyMetrics.Resolution) )
                            return false;
                        else
                        {
                            index = (uint)ul;
                            return true;
                        }
                    }
                }
                // it is not found in any of the list. Force to write all metric entries for the property.
                metricEntryType = MetricEntryType.Must;
                return true;
            }
        }
    }

    /// <summary>
    /// CMetricBlock owns CMetricEntry which is created based on the Packet Description of the stroke. It also
    /// stores the pointer of the next Block. This is not used in the context of a stroke but is used in the
    /// context of WispInk. Wispink forms a linked list based on the CMetricBlocks of all the strokes.
    /// </summary>
    
        internal class MetricBlock
    {
        private MetricEntry _Entry;
        private uint        _Count;
        private uint        _size;

        /// <summary>
        /// Constructor
        /// </summary>
        public MetricBlock()
        {
        }
        
        /// <summary>
        /// Gets the MetricEntry list associated with this instance
        /// </summary>
        /// <returns></returns>
        public MetricEntry GetMetricEntryList() 
        { 
            return _Entry;
        }

        /// <summary>
        /// Gets the count of MetricEntry for this instance
        /// </summary>
        public uint MetricEntryCount
        {
            get
            {
                return _Count;
            }
        }

        // Returns the size required to serialize this instance
        public uint Size
        {
            get
            {
                return (_size + SerializationHelper.VarSize(_size));
            }
        }

        /// <summary>
        /// Adds a new metric entry in the existing list of metric entries
        /// </summary>
        /// <param name="newEntry"></param>
        /// <returns></returns>
        public void AddMetricEntry(MetricEntry newEntry)
        {
            if (null == newEntry)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("MetricEntry cannot be null"));
            }
            if( null == _Entry )
                _Entry = newEntry;
            else
                _Entry.Add(newEntry);  // tack on at the end
            _Count ++;
            _size += newEntry.Size + SerializationHelper.VarSize(newEntry.Size) + SerializationHelper.VarSize((uint)newEntry.Tag);
        }

        /// <summary>
        /// Adds a new metric entry in the existing list of metric entries
        /// </summary>
        /// <param name="property"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public MetricEntryType AddMetricEntry(StylusPointPropertyInfo property, KnownTagCache.KnownTagIndex tag)
        {
            // Create a new metric entry based on the packet information passed.
            MetricEntry entry = new MetricEntry();
            MetricEntryType type = entry.CreateMetricEntry(property, tag);

            // Don't add this entry to the global list if size is 0, means default metric values!
            if( 0 == entry.Size )
            {
                return type;
            }

            MetricEntry start = _Entry;
            if( null == start )
            {
                _Entry = entry;
            }
            else    // tack on data at the end, want to keep x,y at the beginning
            {
                while(start.Next != null)
                {
                    start = start.Next;
                }
                start.Next = entry;
            }
            _Count++;
            _size += entry.Size + SerializationHelper.VarSize(entry.Size) + SerializationHelper.VarSize((uint)_Entry.Tag);
            return type;
        }
        
        /// <summary>
        /// This function Packs the data in the buffer provided. The
        /// function is being called during the save loop and caller
        /// must call GetSize for the block before calling this function.
        /// The buffer must be preallocated and buffer size must be at
        /// least the size of the block. 
        /// On return cbBuffer contains the size of the data written.
        /// Called only by BuildMetricTable funtion 
        /// </summary>
        /// <param name="strm"></param>
        /// <returns></returns>
        public uint Pack(Stream strm)
        {
            // Write the size of the Block at the begining of the buffer.
            // But first check the validity of the buffer & its size
            uint cbWrite  = 0;
            // First write the size of the block
            cbWrite = SerializationHelper.Encode(strm, _size);

            // Now write each entry for the block
            MetricEntry entry = _Entry;
            while( null != entry )
            {
                cbWrite += SerializationHelper.Encode(strm, (uint)entry.Tag);
                cbWrite += SerializationHelper.Encode(strm, entry.Size);
                strm.Write(entry.Data, 0, (int)entry.Size);
                cbWrite += entry.Size;
                entry = entry.Next;
            }
            return cbWrite;
        }
        //
        //

        /// <summary>
        /// This function compares pMetricColl with the current one. If pMetricColl has more entries apart from the one
        /// in the current list with which some of its entries are identical, setType is set as SUPERSET.
        /// </summary>
        /// <param name="metricColl"></param>
        /// <param name="setType"></param>
        /// <returns></returns>
        public bool CompareMetricBlock( MetricBlock metricColl, ref SetType setType)
        {
            if( null == metricColl )
                return false;

            // if both have null entry, implies default metric Block for both of them 
            // and it already exists in the list. Return TRUE
            // If only one of the blocks is empty, return FALSE - they cannot be merged 
            // because the other block may have customized GUID_X or GUID_Y.

            if (null == GetMetricEntryList())
                return (metricColl.GetMetricEntryList() == null);
            
            if (null == metricColl.GetMetricEntryList()) 
                return false;

            // Else compare the entries

            bool  fTagFound = false;
            uint cbLhs = this.MetricEntryCount;    // No of entries in this block
            uint cbRhs = metricColl.MetricEntryCount;   // No of entries in the block to be compared

            MetricEntry outside, inside;
            if( metricColl.MetricEntryCount <= MetricEntryCount )
            {
                outside = metricColl.GetMetricEntryList();
                inside  = GetMetricEntryList();
            }
            else
            {
                inside   = metricColl.GetMetricEntryList();
                outside  = GetMetricEntryList();
                setType   = SetType.SuperSet;
            }

            // For each entry in metricColl, search for the same in this Block. 
            // If it is found, continue with the next entry of smaller Block. 
            while( null != outside )
            {
                fTagFound = false;
                // Always start at the begining of the larger block
                MetricEntry temp = inside;
                while( null != temp )
                {
                    if( outside.Compare(temp) )
                    {
                        fTagFound = true;
                        break;
                    }
                    else
                        temp = temp.Next;
                }
                if( !fTagFound )
                    return false;

                // Found the entry; Continue with the next entry in the outside block
                outside = outside.Next;
            }

            return true;
        }
    }
}
