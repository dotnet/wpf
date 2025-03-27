// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace MS.Internal.Shaping
{
    internal struct LayoutOffset
    {
        public LayoutOffset(int dx, int dy) { this.dx=dx; this.dy=dy; }
        public int dx;
        public int dy;
    }

    /// <summary>
    /// Tags used in OpenTypeLayout
    /// </summary>
    internal enum OpenTypeTags :uint
    {
        Null = 0x00000000,

        GSUB = 0x47535542,
        GPOS = 0x47504F53,
        GDEF = 0x47444546,
        BASE = 0x42415345,
        name = 0x6e616D65,
        post = 0x706F7374,
        dflt = 0x64666c74,
        head = 0x68656164,

        //GSUB feature tags
        locl = 0x6c6f636c,
        ccmp = 0x63636d70,
        rlig = 0x726c6967,
        liga = 0x6c696761,
        clig = 0x636c6967,
        pwid = 0x70776964,
        init = 0x696e6974,
        medi = 0x6d656469,
        fina = 0x66696e61,
        isol = 0x69736f6c,
        calt = 0x63616c74,
        //Indic subst
        nukt = 0x6e756b74,
        akhn = 0x616b686e,
        rphf = 0x72706866,
        blwf = 0x626c7766,
        half = 0x68616c66,
        vatu = 0x76617475,
        pres = 0x70726573,
        abvs = 0x61627673,
        blws = 0x626c7773,
        psts = 0x70737473,
        haln = 0x68616c6e,

        //GPOS feature tags
        kern = 0x6b65726e,
        mark = 0x6d61726b,
        mkmk = 0x6d6b6d6b,
        curs = 0x63757273,
        //Indic pos
        abvm = 0x6162766d,
        blwm = 0x626c776d,
        dist = 0x64697374,

        //script tags
        latn = 0x6c61746e
    }

    /// <summary>
    /// FeatureInfo flags, describing actions implemented in OT feature
    /// </summary>
    [Flags]
    internal enum TagInfoFlags : uint
    {
        Substitution    = 0x01, // does glyph substitution
        Positioning     = 0x02, // does glyph positioning
        Both            = 0x03, // does both substitution and positioning
        None            = 0x00  // neither of them
    }

    /// <summary>
    /// Table pointer wrapper. Checking table boundaries
    /// </summary>
    internal unsafe class FontTable
    {
        public FontTable(byte[] data)
        {
            m_data = data;
            if (data != null)
            {
                m_length = (uint)data.Length;
            }
            else
            {
                m_length = 0;
            }
        }

        public const int InvalidOffset  = int.MaxValue;
        public const int NullOffset     = 0;

        public bool IsPresent
        {
              get
           {
                 return (m_data!=null);
           }
        }

        public ushort GetUShort(int offset)
        {
            Invariant.Assert(m_data!= null);

            if ((offset + 1) >= m_length) throw new FileFormatException();
            return (ushort)((m_data[offset]<<8) + m_data[offset+1]);
        }
        public short GetShort(int offset)
        {
            Invariant.Assert(m_data != null);

            if ((offset + 1) >= m_length) throw new FileFormatException();
            return (short)((m_data[offset]<<8) + m_data[offset+1]);
        }
        public uint GetUInt(int offset)
        {
            Invariant.Assert(m_data != null);

            if ((offset + 3) >= m_length) throw new FileFormatException();
            return (uint)((m_data[offset]<<24) + (m_data[offset+1]<<16) + (m_data[offset+2]<<8) + m_data[offset+3]);
        }
        public ushort GetOffset(int offset)
        {
            Invariant.Assert(m_data != null);

            if ((offset+1)>=m_length) throw new FileFormatException();
            return (ushort)((m_data[offset]<<8) + m_data[offset+1]);
        }

        private byte[] m_data;

        private uint  m_length;
    }

    /// <summary>
    /// Font file access callbacks
    /// </summary>
    internal interface IOpenTypeFont
    {
        /// <summary>
        /// Returns array containing font table data
        /// Return empty array if table does not exist.
        /// </summary>
        FontTable GetFontTable(OpenTypeTags TableTag);

        /// <summary>
        /// Returns glyph coordinate
        /// </summary>
        LayoutOffset GetGlyphPointCoord(ushort Glyph, ushort PointIndex);

        /// <summary>
        /// Returns cache for layout table. If cache not found, return null Checked pointer
        /// </summary>
        byte[] GetTableCache(OpenTypeTags tableTag);

        /// <summary>
        /// Allocate space for layout table cache. If space is not available
        /// client should return null checked pointer.
        /// Only font cache implementation need to implement this interface.
        /// Normal layout funcitons will not call it.
        /// </summary>
        byte[] AllocateTableCache(OpenTypeTags tableTag, int size);
    }

    /// <summary>
    /// Text direction
    /// </summary>
    internal enum TextFlowDirection : ushort
    {
        LTR,
        RTL,
        TTB,
        BTT
    }

    /// <summary>
    /// Layout metrics
    /// </summary>
    internal struct LayoutMetrics
    {
        public TextFlowDirection Direction;

        //if DesignEmHeight==0, result requested in design units
        public ushort      DesignEmHeight; // font design units per Em

        public ushort      PixelsEmWidth;   // Em width in pixels
        public ushort      PixelsEmHeight;  // Em height in pixels

        public LayoutMetrics(TextFlowDirection Direction,
                             ushort DesignEmHeight,
                             ushort PixelsEmWidth,
                             ushort PixelsEmHeight)
        {
            this.Direction=Direction;
            this.DesignEmHeight=DesignEmHeight;
            this.PixelsEmWidth=PixelsEmWidth;
            this.PixelsEmHeight=PixelsEmHeight;
        }
    }

    internal class Feature
    {
        public Feature(
            ushort  startIndex,
            ushort  length,
            uint    tag,
            uint    parameter //0 if disabled
            )
        {
            _startIndex = startIndex;
            _length = length;
            _tag = tag;
            _parameter = parameter;
        }

        public uint Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public uint Parameter
        {
            get { return _parameter; }
            set { _parameter = value; }
        }

        public ushort StartIndex
        {
            get { return _startIndex; }
            set { _startIndex = value; }
        }

        public ushort Length
        {
            get { return _length; }
            set { _length = value; }
        }

        private ushort  _startIndex;   // first to be applied
        private ushort  _length;       // length to be applied
        private uint    _tag;          // OpenType feature tag
        private uint    _parameter;    // feature parameter
    }

    /// <summary>
    /// OpenTypeLayout class provides access to OpenType Layout services
    /// </summary>
    internal static unsafe class OpenTypeLayout
    {
        ///<summary>
        /// Internal method to test layout tables if they are uitable for fast path.
        /// Returns list of script-langauge pairs that are not optimizable.
        ///</summary>
        internal static OpenTypeLayoutResult GetComplexLanguageList (
            IOpenTypeFont       Font,           //In: Font access interface
            uint[]              featureList,     //In: Feature to look in
            uint[]              glyphBits,
            ushort              minGlyphId,
            ushort              maxGlyphId,
            out WritingSystem[] complexLanguages
                                                          // Out: List of script/langauge pair
                                                          //      that are not optimizable
        )
        {
            try
            {
                WritingSystem[] gsubComplexLanguages = null;
                WritingSystem[] gposComplexLanguages = null;
                int gsubComplexLanguagesCount = 0;
                int gposComplexLanguagesCount = 0;

                FontTable GsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                FontTable GposTable = Font.GetFontTable(OpenTypeTags.GPOS);

                if (GsubTable.IsPresent)
                {
                    LayoutEngine.GetComplexLanguageList(
                                        OpenTypeTags.GSUB,
                                        GsubTable,
                                        featureList,
                                        glyphBits,
                                        minGlyphId,
                                        maxGlyphId,
                                        out gsubComplexLanguages,
                                        out gsubComplexLanguagesCount
                                 );
                }

                if (GposTable.IsPresent)
                {
                    LayoutEngine.GetComplexLanguageList(
                                        OpenTypeTags.GPOS,
                                        GposTable,
                                        featureList,
                                        glyphBits,
                                        minGlyphId,
                                        maxGlyphId,
                                        out gposComplexLanguages,
                                        out gposComplexLanguagesCount
                                 );
                }

                if (gsubComplexLanguages == null && gposComplexLanguages == null)
                {
                    complexLanguages = null;
                    return OpenTypeLayoutResult.Success;
                }

                // Both tables have complex scrips, merge results

                // Count gpos unique Languages
                // and pack them at the same time
                // so we do not research them again.
                int gposNewLanguages=0, i, j;

                for(i = 0; i < gposComplexLanguagesCount ;i++)
                {
                    bool foundInGsub = false;

                    for(j = 0; j < gsubComplexLanguagesCount ;j++)
                    {
                        if (gsubComplexLanguages[j].scriptTag == gposComplexLanguages[i].scriptTag &&
                            gsubComplexLanguages[j].langSysTag == gposComplexLanguages[i].langSysTag
                           )
                        {
                            foundInGsub = true;
                            break;
                        };
                    }

                    if (!foundInGsub)
                    {
                        if (gposNewLanguages < i)
                        {
                            gposComplexLanguages[gposNewLanguages] = gposComplexLanguages[i];
                        }

                        gposNewLanguages++;
                    }
                }

                //realloc array for merged results, merge both arrays
                complexLanguages = new WritingSystem[gsubComplexLanguagesCount + gposNewLanguages];

                for(i = 0; i < gsubComplexLanguagesCount; i++)
                {
                    complexLanguages[i] = gsubComplexLanguages[i];
                }

                for(i = 0; i < gposNewLanguages; i++)
                {
                    complexLanguages[gsubComplexLanguagesCount + i] = gposComplexLanguages[i];
                }

                return OpenTypeLayoutResult.Success;
}
            catch (FileFormatException)
            {
                complexLanguages = null;
                return OpenTypeLayoutResult.BadFontTable;
            }
        }
    }

    internal struct WritingSystem
    {
        internal uint scriptTag;
        internal uint langSysTag;
    }

    /// <summary>
    /// </summary>
    internal enum OpenTypeLayoutResult
    {
        Success,
        InvalidParameter,
        TableNotFound,
        ScriptNotFound,
        LangSysNotFound,
        BadFontTable,
        UnderConstruction
    }

    /// <summary>
    /// Class for internal OpenType use to store per font
    /// information and temporary buffers.
    ///
    /// We do not use fontcache now, so this information
    /// will be recreated every time shaping engine
    /// will be called, so, in the future, design interfaces to use
    /// fontcache and define what information can be stored there.
    /// </summary>
    internal class OpenTypeLayoutWorkspace
    {
        /// <summary>
        /// Init buffers to initial values.
        /// </summary>
        internal unsafe OpenTypeLayoutWorkspace()
        {
            _bytesPerLookup     = 0;
            _lookupUsageFlags   = null;
        }

        //lookup usage flags access
        private const byte AggregatedFlagMask        = 0x01;
        private const byte RequiredFeatureFlagMask   = 0x02;
        private const int  FeatureFlagsStartBit      = 2;

        public void InitLookupUsageFlags(int lookupCount, int featureCount)
        {
            _bytesPerLookup = (featureCount + FeatureFlagsStartBit + 7) >> 3;

            int requiredLookupUsageArraySize = lookupCount * _bytesPerLookup;

            if ( _lookupUsageFlags == null ||
                 _lookupUsageFlags.Length < requiredLookupUsageArraySize)
            {
                _lookupUsageFlags = new byte[requiredLookupUsageArraySize];
            }

            Array.Clear(_lookupUsageFlags, 0, requiredLookupUsageArraySize);
        }

        public bool IsFeatureFlagSet(int lookupIndex, int featureIndex)
        {
            int flagIndex = featureIndex + FeatureFlagsStartBit;
            int flagByte = (lookupIndex * _bytesPerLookup) + (flagIndex >> 3);
            byte flagMask = (byte)(1 << (flagIndex %    8));

            return ((_lookupUsageFlags[flagByte] & flagMask) != 0);
        }

        public bool IsRequiredFeatureFlagSet(int lookupIndex)
        {
            return ((_lookupUsageFlags[lookupIndex * _bytesPerLookup] & RequiredFeatureFlagMask) != 0);
        }

        public void SetFeatureFlag(int lookupIndex, int featureIndex)
        {
            int startLookupByte = lookupIndex * _bytesPerLookup;
            int flagIndex = featureIndex + FeatureFlagsStartBit;
            int flagByte = startLookupByte + (flagIndex >> 3);
            byte flagMask = (byte)(1 << (flagIndex % 8));

            if (flagByte >= _lookupUsageFlags.Length)
            {
                //This should be invalid font. Lookup associated with the feature is not in lookup array.
                throw new FileFormatException();
            }
            
            _lookupUsageFlags[flagByte] |= flagMask;

            // Also set agregated usage flag
            _lookupUsageFlags[startLookupByte] |= AggregatedFlagMask;
        }

        public void SetRequiredFeatureFlag(int lookupIndex)
        {
            int flagByte = lookupIndex * _bytesPerLookup;

            if (flagByte >= _lookupUsageFlags.Length)
            {
                //This should be invalid font. Lookup associated with the feature is not in lookup array.
                throw new FileFormatException();
            }

            //set RequiredFeature and aggregated flag at the same time
            _lookupUsageFlags[flagByte] |= (AggregatedFlagMask | RequiredFeatureFlagMask);
        }

        // Define cache which lookup is enabled by which feature.
        // Buffer grows with number of features applied
        private int _bytesPerLookup;
        private byte[] _lookupUsageFlags;

    }
}
