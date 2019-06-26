// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Security;
using System.IO;
using System.Diagnostics;

using MS.Internal.FontCache;

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

/* Used by commented code below
    /// <summary>
    /// OpenType feature information. Returned from GetFeatureList method
    /// </summary>
    internal struct TagInfo
    {
        public uint          Tag;
        public TagInfoFlags  TagFlags;

        public static bool IsNewTag(TagInfo[] Tags, uint Tag)
        {
            for(int i=0; i<Tags.Length; i++)
            {
                if (Tags[i].Tag==Tag) return false;
            }
            return true;
        }

        public static int GetTagIndex(TagInfo[] Tags, uint Tag)
        {
            for(ushort i=0; i<Tags.Length; i++)
            {
                if (Tags[i].Tag==Tag) return i;
            }
            return ushort.MaxValue;
        }
    }
*/

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
        /// <summary>
        /// </summary>
        /// <param name="Font">Font</param>
        /// <param name="ScriptTag">Script to find</param>
        /// <returns>TagInfo, if script not present flags == None</returns>
        internal static TagInfoFlags FindScript(
            IOpenTypeFont       Font,     // In: Font access interface
            uint                ScriptTag // In
            )
        {
            TagInfoFlags flags = TagInfoFlags.None;

            try
            {
                FontTable gsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                if (gsubTable.IsPresent)
                {
                    GSUBHeader gsubHeader = new GSUBHeader(0);
                    if (!gsubHeader.GetScriptList(gsubTable).FindScript(gsubTable,ScriptTag).IsNull)
                    {
                        flags |= TagInfoFlags.Substitution;
                    }
                }
            }
            catch (FileFormatException)
            {
                return TagInfoFlags.None;
            }

            try
            {
                FontTable gposTable = Font.GetFontTable(OpenTypeTags.GPOS);
                if (gposTable.IsPresent)
                {
                    GPOSHeader gposHeader = new GPOSHeader(0);
                    if (!gposHeader.GetScriptList(gposTable).FindScript(gposTable,ScriptTag).IsNull)
                    {
                        flags |= TagInfoFlags.Positioning;
                    }
                }
            }
            catch (FileFormatException)
            {
                return TagInfoFlags.None;
            }

            return flags;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Font">Font</param>
        /// <param name="ScriptTag">Script to search in</param>
        /// <param name="LangSysTag">LangGys to search for</param>
        /// <returns>TagInfoFlags, if script not present == None</returns>
        internal static TagInfoFlags FindLangSys(
            IOpenTypeFont       Font,
            uint                ScriptTag,
            uint                LangSysTag
            )
        {
            TagInfoFlags flags = TagInfoFlags.None;

            try
            {
                FontTable gsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                if (gsubTable.IsPresent)
                {
                    GSUBHeader gsubHeader = new GSUBHeader(0);
                    ScriptTable gsubScript = gsubHeader.GetScriptList(gsubTable).FindScript(gsubTable,ScriptTag);
                    if (!gsubScript.IsNull && !gsubScript.FindLangSys(gsubTable,LangSysTag).IsNull)
                    {
                        flags |= TagInfoFlags.Substitution;
                    }
                }
            }
            catch (FileFormatException)
            {
                return TagInfoFlags.None;
            }

            try
            {
                FontTable gposTable = Font.GetFontTable(OpenTypeTags.GPOS);
                if (gposTable.IsPresent)
                {
                    GPOSHeader gposHeader = new GPOSHeader(0);
                    ScriptTable gposScript = gposHeader.GetScriptList(gposTable).FindScript(gposTable,ScriptTag);
                    if (!gposScript.IsNull && !gposScript.FindLangSys(gposTable,LangSysTag).IsNull)
                    {
                        flags |= TagInfoFlags.Positioning;
                    }
                }
            }
            catch (FileFormatException)
            {
                return TagInfoFlags.None;
            }

            return flags;
        }

/* This is unused code, but will be used later so it is just commented out for now.

        /// <summary>
        /// Enumerates scripts in a font
        /// </summary>
        internal static OpenTypeLayoutResult GetScriptList (
            IOpenTypeFont       Font,     // In: Font access interface
            out TagInfo[]       Scripts   // Out: Array of scripts supported
            )
        {
            ushort i;
            ushort GposNewTags;

            Scripts=null; // Assignment required, because of out attribute.
                          // This value should be owerwritten later.

            try
            {
                FontTable GsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                FontTable GposTable = Font.GetFontTable(OpenTypeTags.GPOS);

                GSUBHeader GsubHeader = new GSUBHeader(0);
                GPOSHeader GposHeader = new GPOSHeader(0);

                ScriptList GsubScriptList;
                ScriptList GposScriptList;
                ushort GsubScriptCount;
                ushort GposScriptCount;

                if (GsubTable.IsNotPresent && GposTable.IsNotPresent)
                {
                    Scripts = new TagInfo[0];
                    return OpenTypeLayoutResult.Success;
                }

                if (GsubTable.IsPresent)
                {
                    GsubScriptList  = GsubHeader.GetScriptList(GsubTable);
                    GsubScriptCount = GsubScriptList.GetScriptCount(GsubTable);
                }
                else
                {
                    GsubScriptList = new ScriptList(FontTable.InvalidOffset);
                    GsubScriptCount = 0;
                }

                if (GposTable.IsPresent)
                {
                    GposScriptList  = GposHeader.GetScriptList(GposTable);
                    GposScriptCount = GposScriptList.GetScriptCount(GposTable);
                }
                else
                {
                    GposScriptList = new ScriptList(FontTable.InvalidOffset);
                    GposScriptCount = 0;
                }

                //This is true in most cases that there is no new tags in GPOS.
                //So, we allocate this array then check GPOS for new tags
                Scripts = new TagInfo[GsubScriptCount];

                for(i=0; i<GsubScriptCount; i++)
                {
                    Scripts[i].Tag      = GsubScriptList.GetScriptTag(GsubTable,i);
                    Scripts[i].TagFlags = TagInfoFlags.Substitution;
                }

                //Check GPOS for tags that is not in GSUB
                GposNewTags=0;

                for(i=0;i<GposScriptCount;i++)
                {
                    uint GposTag = GsubScriptList.GetScriptTag(GposTable,i);
                    if (TagInfo.IsNewTag(Scripts,GposTag)) GposNewTags++;
                }

                //append new tags to ScriptTags if any exists
                if (GposNewTags>0)
                {
                    int CurrentScriptIndex=GposScriptCount;

                    //Allocate new array to fit all tags
                    TagInfo[] tmp = Scripts;
                    Scripts = new TagInfo[GsubScriptCount+GposNewTags];
                    Array.Copy(tmp,0,Scripts,0,tmp.Length);

                    for(i=0;i<GposScriptCount;i++)
                    {
                        uint GposTag = GsubScriptList.GetScriptTag(GposTable,i);
                        if (TagInfo.IsNewTag(Scripts,GposTag))
                        {
                            Scripts[CurrentScriptIndex].Tag=GposTag;
                            Scripts[CurrentScriptIndex].TagFlags
                                = TagInfoFlags.Positioning;
                            ++CurrentScriptIndex;
                        }
                        else
                        {
                            int ScriptIndex = TagInfo.GetTagIndex(Scripts,GposTag);
                            Scripts[ScriptIndex].TagFlags |= TagInfoFlags.Positioning;
                        }
                    }

                    Debug.Assert(CurrentScriptIndex==Scripts.Length);
                }
            }
            catch (FileFormatException)
            {
                return OpenTypeLayoutResult.BadFontTable;
            }

            return OpenTypeLayoutResult.Success;
        }


        ///<summary>
        /// Enumerates language systems for script
        /// </summary>
        internal static OpenTypeLayoutResult  GetLangSysList (
            IOpenTypeFont   Font,       // In: Font access interface
            uint            ScriptTag,  // In: Script tag
            out TagInfo[]   LangSystems // Out: Array of LangSystems for Script
            )
        {
            ushort i;
            ushort GposNewTags;

            LangSystems=null; // Assignment required, because of out attribute.
                              // This value should be owerwritten later.

            try
            {
                FontTable GsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                FontTable GposTable = Font.GetFontTable(OpenTypeTags.GPOS);

                GSUBHeader GsubHeader = new GSUBHeader(0);
                GPOSHeader GposHeader = new GPOSHeader(0);

                ScriptList GsubScriptList;
                ScriptList GposScriptList;
                ScriptTable GsubScript;
                ScriptTable GposScript;
                ushort GsubLangSysCount;
                ushort GposLangSysCount;

                if (GsubTable.IsNotPresent && GposTable.IsNotPresent)
                {
                    return OpenTypeLayoutResult.ScriptNotFound;
                }

                if (GsubTable.IsPresent)
                {
                    GsubScriptList = GsubHeader.GetScriptList(GsubTable);
                    GsubScript = GsubScriptList.FindScript(GsubTable,ScriptTag);
                }
                else
                {
                    GsubScript = new ScriptTable(FontTable.InvalidOffset);
                }

                if (GposTable.IsPresent)
                {
                    GposScriptList  = GposHeader.GetScriptList(GposTable);
                    GposScript = GposScriptList.FindScript(GposTable,ScriptTag);
                }
                else
                {
                    GposScript = new ScriptTable(FontTable.InvalidOffset);
                }

                if (GsubScript.IsNull && GposScript.IsNull)
                {
                    return OpenTypeLayoutResult.ScriptNotFound;
                }

                if (!GsubScript.IsNull)
                {
                    GsubLangSysCount = GsubScript.GetLangSysCount(GsubTable);
                }
                else
                {
                    GsubLangSysCount = 0;
                }

                if (!GposScript.IsNull)
                {
                    GposLangSysCount = GposScript.GetLangSysCount(GposTable);
                }
                else
                {
                    GposLangSysCount = 0;
                }

                //This is true in most cases that there is no new tags in GPOS.
                //So, we allocate this array then check GPOS for new tags
                ushort CurrentLangSysIndex;

                if (GsubScript.IsDefaultLangSysExists(GsubTable))
                {
                    LangSystems = new TagInfo[GsubLangSysCount+1];
                    LangSystems[0].Tag      = (uint)OpenTypeTags.dflt;
                    LangSystems[0].TagFlags = TagInfoFlags.Substitution;
                    CurrentLangSysIndex = 1;
                }
                else
                {
                    LangSystems = new TagInfo[GsubLangSysCount];
                    CurrentLangSysIndex = 0;
                }

                for(i=0; i<GsubLangSysCount; i++)
                {
                    LangSystems[CurrentLangSysIndex].Tag = GsubScript.GetLangSysTag(GsubTable,i);
                    LangSystems[CurrentLangSysIndex].TagFlags = TagInfoFlags.Substitution;
                    ++CurrentLangSysIndex;
                }

                //Check GPOS for tags that is not in GSUB
                GposNewTags=0;

                if (!GposScript.IsNull)
                {
                    if (GposScript.IsDefaultLangSysExists(GposTable) &&
                        TagInfo.IsNewTag(LangSystems,(uint)OpenTypeTags.dflt))
                    {
                        ++GposNewTags;
                    }

                    for(i=0;i<GposLangSysCount;i++)
                    {
                        uint GposTag = GsubScript.GetLangSysTag(GposTable,i);
                        if (TagInfo.IsNewTag(LangSystems,GposTag))
                        {
                            ++GposNewTags;
                        }
                    }
                }

                Debug.Assert(CurrentLangSysIndex==LangSystems.Length);

                //append new tags to ScriptTags if any exists
                if (GposNewTags>0)
                {
                    //Allocate new array to fit all tags
                    TagInfo[] tmp = LangSystems;
                    LangSystems = new TagInfo[GsubLangSysCount+GposNewTags];
                    Array.Copy(tmp,0,LangSystems,0,tmp.Length);

                    if (GposScript.IsDefaultLangSysExists(GposTable))
                    {
                        if (TagInfo.IsNewTag(LangSystems,(uint)OpenTypeTags.dflt))
                        {
                            LangSystems[CurrentLangSysIndex].Tag = (uint)OpenTypeTags.dflt;
                            LangSystems[CurrentLangSysIndex].TagFlags = TagInfoFlags.Positioning;
                            ++CurrentLangSysIndex;
                        }
                        else
                        {
                            int LangSysIndex = TagInfo.GetTagIndex(LangSystems,(uint)OpenTypeTags.dflt);
                            LangSystems[LangSysIndex].TagFlags |= TagInfoFlags.Positioning;
                        }
                    }

                    for(i=0;i<GposLangSysCount;i++)
                    {
                        uint GposTag = GposScript.GetLangSysTag(GposTable,i);

                        if (TagInfo.IsNewTag(LangSystems,GposTag))
                        {
                            LangSystems[CurrentLangSysIndex].Tag = GposTag;
                            LangSystems[CurrentLangSysIndex].TagFlags = TagInfoFlags.Positioning;
                            ++CurrentLangSysIndex;
                        }
                        else
                        {
                            int LangSysIndex = TagInfo.GetTagIndex(LangSystems,GposTag);
                            LangSystems[LangSysIndex].TagFlags |= TagInfoFlags.Positioning;
                        }
                    }

                    Debug.Assert(CurrentLangSysIndex==LangSystems.Length);
                }
            }
            catch (FileFormatException)
            {
                return OpenTypeLayoutResult.BadFontTable;
            }

            return OpenTypeLayoutResult.Success;
        }


        /// <summary>
        /// Enumerates features in a language system
        /// </summary>
        internal static OpenTypeLayoutResult  GetFeatureList (
            IOpenTypeFont   Font,           // In: Font access interface
            uint            ScriptTag,      // In: Script tag
            uint            LangSysTag,     // In: LangSys tag
            out TagInfo[]   Features        // Out: Array of features
            )
        {
            ushort i;
            ushort GposNewTags;

            Features=null; // Assignment required, because of out attribute.
                           // This value should be owerwritten later.

            try
            {
                FontTable GsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                FontTable GposTable = Font.GetFontTable(OpenTypeTags.GPOS);

                GSUBHeader GsubHeader = new GSUBHeader(0);
                GPOSHeader GposHeader = new GPOSHeader(0);

                ScriptList GsubScriptList;
                ScriptList GposScriptList;
                ScriptTable GsubScript;
                ScriptTable GposScript;
                LangSysTable GsubLangSys;
                LangSysTable GposLangSys;
                ushort GsubFeatureCount;
                ushort GposFeatureCount;
                FeatureList GsubFeatureList;
                FeatureList GposFeatureList;


                if (GsubTable.IsNotPresent && GposTable.IsNotPresent)
                {
                    return OpenTypeLayoutResult.ScriptNotFound;
                }

                if (GsubTable.IsPresent)
                {
                    GsubScriptList  = GsubHeader.GetScriptList(GsubTable);
                    GsubScript      = GsubScriptList.FindScript(GsubTable,ScriptTag);
                    GsubLangSys     = GsubScript.FindLangSys(GsubTable,LangSysTag);
                    GsubFeatureList = GsubHeader.GetFeatureList(GsubTable);
                }
                else
                {
                    GsubScript = new ScriptTable(FontTable.InvalidOffset);
                    GsubLangSys = new LangSysTable(FontTable.InvalidOffset);
                    GsubFeatureList = new FeatureList(FontTable.InvalidOffset);
                }

                if (GposTable.IsPresent)
                {
                    GposScriptList  = GposHeader.GetScriptList(GposTable);
                    GposScript      = GposScriptList.FindScript(GposTable,ScriptTag);
                    GposLangSys     = GposScript.FindLangSys(GposTable,LangSysTag);
                    GposFeatureList = GposHeader.GetFeatureList(GposTable);
                }
                else
                {
                    GposScript = new ScriptTable(FontTable.InvalidOffset);
                    GposLangSys = new LangSysTable(FontTable.InvalidOffset);
                    GposFeatureList = new FeatureList(FontTable.InvalidOffset);
                }

                if (GsubScript.IsNull && GposScript.IsNull)
                {
                    return OpenTypeLayoutResult.ScriptNotFound;
                }

                if (GsubLangSys.IsNull && GposLangSys.IsNull)
                {
                    return OpenTypeLayoutResult.LangSysNotFound;
                }

                if (!GsubLangSys.IsNull)
                {
                    GsubFeatureCount = GsubLangSys.FeatureCount(GsubTable);
                }
                else
                {
                    GsubFeatureCount = 0;
                }

                if (!GposLangSys.IsNull)
                {
                    GposFeatureCount = GposLangSys.FeatureCount(GposTable);
                }
                else
                {
                    GposFeatureCount = 0;
                }

                Features = new TagInfo[GsubFeatureCount];
                int CurrentFeatureIndex = 0;

                for(i=0; i<GsubFeatureCount; i++)
                {
                    ushort FeatureIndex = GsubLangSys.GetFeatureIndex(GsubTable,i);
                    Features[CurrentFeatureIndex].Tag = GsubFeatureList.FeatureTag(GsubTable,FeatureIndex);
                    Features[CurrentFeatureIndex].TagFlags = TagInfoFlags.Substitution;
                    ++CurrentFeatureIndex;
                }

                Debug.Assert(CurrentFeatureIndex==Features.Length);

                //Check GPOS for tags that is not in GSUB
                GposNewTags=0;
                if (!GposLangSys.IsNull)
                {
                    for(i=0;i<GposFeatureCount;i++)
                    {
                        ushort FeatureIndex = GposLangSys.GetFeatureIndex(GposTable,i);
                        uint GposTag = GposFeatureList.FeatureTag(GposTable,FeatureIndex);
                        if (TagInfo.IsNewTag(Features,GposTag))
                        {
                            ++GposNewTags;
                        }
                    }
                }

                //append new tags to ScriptTags if any exists
                if (GposNewTags>0)
                {
                    //Allocate new array to fit all tags
                    TagInfo[] tmp = Features;
                    Features = new TagInfo[GsubFeatureCount+GposNewTags];
                    Array.Copy(tmp,0,Features,0,tmp.Length);

                    for(i=0;i<GposFeatureCount;i++)
                    {
                        ushort FeatureIndex = GposLangSys.GetFeatureIndex(GposTable,i);
                        uint GposTag = GposFeatureList.FeatureTag(GposTable,FeatureIndex);

                        if (TagInfo.IsNewTag(Features,GposTag))
                        {
                            Features[CurrentFeatureIndex].Tag = GposTag;
                            Features[CurrentFeatureIndex].TagFlags = TagInfoFlags.Positioning;
                            ++CurrentFeatureIndex;
                        }
                        else
                        {
                            int Index = TagInfo.GetTagIndex(Features,GposTag);
                            Features[Index].TagFlags |= TagInfoFlags.Positioning;
                        }
                    }

                    Debug.Assert(CurrentFeatureIndex==Features.Length);
                }


            }
            catch (FileFormatException)
            {
                return OpenTypeLayoutResult.BadFontTable;
            }

            return OpenTypeLayoutResult.Success;
        }
*/

        /// <summary>
        /// Substitutes glyphs according to features defined in the font.
        /// </summary>
        /// <param name="Font">In: Font access interface</param>
        /// <param name="workspace">In: Workspace for layout engine</param>
        /// <param name="ScriptTag">In: Script tag</param>
        /// <param name="LangSysTag">In: LangSys tag</param>
        /// <param name="FeatureSet">In: List of features to apply</param>
        /// <param name="featureCount">In: Actual number of features in <paramref name="FeatureSet"/></param>
        /// <param name="featureSetOffset">In: offset of input characters inside FeatureSet</param>
        /// <param name="CharCount">In: Characters count (i.e. <paramref name="Charmap"/>.Length);</param>
        /// <param name="Charmap">In/out: Char to glyph mapping</param>
        /// <param name="Glyphs">In/out: List of GlyphInfo structs</param>
        /// <returns>Substitution result</returns>
        internal static OpenTypeLayoutResult SubstituteGlyphs(
            IOpenTypeFont           Font,           // In: Font access interface
            OpenTypeLayoutWorkspace workspace,      // In: Workspace for layout engine
            uint                    ScriptTag,      // In: Script tag
            uint                    LangSysTag,     // In: LangSys tag
            Feature[]               FeatureSet,     // In: List of features to apply
            int                     featureCount,   // In: Actual number of features in FeatureSet
            int                     featureSetOffset,
            int                     CharCount,      // In: Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // In/out: Char to glyph mapping
            GlyphInfoList           Glyphs          // In/out: List of GlyphInfo structs
            )
        {
            try
            {
                FontTable GsubTable = Font.GetFontTable(OpenTypeTags.GSUB);
                if (!GsubTable.IsPresent) {return OpenTypeLayoutResult.ScriptNotFound;}

                GSUBHeader GsubHeader = new GSUBHeader(0);
                ScriptList ScriptList = GsubHeader.GetScriptList(GsubTable);

                ScriptTable Script    = ScriptList.FindScript(GsubTable,ScriptTag);
                if (Script.IsNull) {return OpenTypeLayoutResult.ScriptNotFound;}

                LangSysTable LangSys = Script.FindLangSys(GsubTable,LangSysTag);
                if (LangSys.IsNull) {return OpenTypeLayoutResult.LangSysNotFound;}

                FeatureList FeatureList = GsubHeader.GetFeatureList(GsubTable);
                LookupList LookupList = GsubHeader.GetLookupList(GsubTable);

                LayoutEngine.ApplyFeatures(
                    Font,
                    workspace,
                    OpenTypeTags.GSUB,
                    GsubTable,
                    new LayoutMetrics(), //it is not needed for substitution
                    LangSys,
                    FeatureList,
                    LookupList,
                    FeatureSet,
                    featureCount,
                    featureSetOffset,
                    CharCount,
                    Charmap,
                    Glyphs,
                    null,
                    null
                );
            }
            catch (FileFormatException)
            {
                return OpenTypeLayoutResult.BadFontTable;
            }

            return OpenTypeLayoutResult.Success;
        }

        /// <summary>
        /// Position glyphs according to features defined in the font.
        /// </summary>
        /// <param name="Font">In: Font access interface</param>
        /// <param name="workspace">In: Workspace for layout engine</param>
        /// <param name="ScriptTag">In: Script tag</param>
        /// <param name="LangSysTag">In: LangSys tag</param>
        /// <param name="Metrics">In: LayoutMetrics</param>
        /// <param name="FeatureSet">In: List of features to apply</param>
        /// <param name="featureCount">In: Actual number of features in <paramref name="FeatureSet"/></param>
        /// <param name="featureSetOffset">In: offset of input characters inside FeatureSet</param>
        /// <param name="CharCount">In: Characters count (i.e. <paramref name="Charmap"/>.Length);</param>
        /// <param name="Charmap">In: Char to glyph mapping</param>
        /// <param name="Glyphs">In/out: List of GlyphInfo structs</param>
        /// <param name="Advances">In/out: Glyphs adv.widths</param>
        /// <param name="Offsets">In/out: Glyph offsets</param>
        /// <returns>Substitution result</returns>
        internal static OpenTypeLayoutResult PositionGlyphs(
            IOpenTypeFont           Font,
            OpenTypeLayoutWorkspace workspace,
            uint                    ScriptTag,
            uint                    LangSysTag,
            LayoutMetrics           Metrics,
            Feature[]               FeatureSet,
            int                     featureCount,
            int                     featureSetOffset,
            int                     CharCount,
            UshortList              Charmap,
            GlyphInfoList           Glyphs,
            int*                    Advances,
            LayoutOffset*           Offsets
        )
        {
            try
            {
                FontTable GposTable = Font.GetFontTable(OpenTypeTags.GPOS);
                if (!GposTable.IsPresent) {return  OpenTypeLayoutResult.ScriptNotFound;}

                GPOSHeader GposHeader = new GPOSHeader(0);
                ScriptList ScriptList = GposHeader.GetScriptList(GposTable);

                ScriptTable Script    = ScriptList.FindScript(GposTable,ScriptTag);
                if (Script.IsNull) {return OpenTypeLayoutResult.ScriptNotFound;}

                LangSysTable LangSys = Script.FindLangSys(GposTable,LangSysTag);
                if (LangSys.IsNull) {return OpenTypeLayoutResult.LangSysNotFound;}

                FeatureList FeatureList = GposHeader.GetFeatureList(GposTable);
                LookupList LookupList = GposHeader.GetLookupList(GposTable);

                LayoutEngine.ApplyFeatures(
                    Font,
                    workspace,
                    OpenTypeTags.GPOS,
                    GposTable,
                    Metrics,
                    LangSys,
                    FeatureList,
                    LookupList,
                    FeatureSet,
                    featureCount,
                    featureSetOffset,
                    CharCount,
                    Charmap,
                    Glyphs,
                    Advances,
                    Offsets
                );
            }
            catch (FileFormatException)
            {
                return OpenTypeLayoutResult.BadFontTable;
            }

            return OpenTypeLayoutResult.Success;
        }


        ///<summary>
        ///
        ///</summary>
        internal static OpenTypeLayoutResult CreateLayoutCache (
            IOpenTypeFont       font,           // In: Font access interface
            int                 maxCacheSize    // In: Maximum cache size allowed
        )
        {
            OpenTypeLayoutCache.CreateCache(font, maxCacheSize);
            
            return OpenTypeLayoutResult.Success;
        }
        
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
            _cachePointers      = null;
        }

        /// <summary>
        /// Reset all structures to the new font/OTTable/script/langsys.
        ///
        /// Client need to call it only once per shaping engine call.
        /// This is client's responsibility to ensure that workspace is
        /// used for single font/OTTable/script/langsys between Init() calls
        /// </summary>
        ///<param name="font">In: Font access interface</param>
        ///<param name="tableTag">In: Font table tag</param>
        ///<param name="scriptTag">In: Script tag</param>
        ///<param name="langSysTag">In: Language System tag</param>
        ///<returns>Success if workspace is initialized succesfully, specific error if failed</returns>
        internal OpenTypeLayoutResult Init(
            IOpenTypeFont           font,
            OpenTypeTags            tableTag,
            uint                    scriptTag,
            uint                    langSysTag
            )
        {
            // Currently all buffers are per call,
            // no need to do anything.
            return OpenTypeLayoutResult.Success;
        }

#region Lookup flags 

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

        public bool IsAggregatedFlagSet(int lookupIndex)
        {
            return ((_lookupUsageFlags[lookupIndex * _bytesPerLookup] & AggregatedFlagMask) != 0);
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
#endregion Lookup flags

#region Layout cache pointers

        /// <summary>
        /// Allocate enough memory for array of cache pointers, parallel to glyph run.
        ///
        /// These method should not be used directly, it is only called by OpenTypeLayputCache.
        ///
        /// </summary>
        ///<param name="glyphRunLength">In: Size of a glyph run</param>
        public unsafe void AllocateCachePointers(int glyphRunLength)
        {
            if (_cachePointers != null && _cachePointers.Length >= glyphRunLength) return;

            _cachePointers = new ushort[glyphRunLength];
        }

        /// <summary>
        /// If glyph run is cahnged, update pointers according to the change. Reallocate array if necessary.
        ///
        /// These method should not be used directly, it is only called by OpenTypeLayputCache.
        ///
        /// </summary>
        ///<param name="oldLength">In: Number of glyphs in the run before change</param>
        ///<param name="newLength">In: Number of glyphs in the run after change</param>
        ///<param name="firstGlyphChanged">In: Index of the first changed glyph</param>
        ///<param name="afterLastGlyphChanged">In: Index of the glyph after last changed</param>
        public unsafe void UpdateCachePointers(
                                        int     oldLength,
                                        int     newLength,
                                        int     firstGlyphChanged,
                                        int     afterLastGlyphChanged
                                       )
        {
            if (oldLength != newLength)
            {
                int oldAfterLastGlyphChanged = afterLastGlyphChanged - (newLength - oldLength);

                if (_cachePointers.Length < newLength) 
                {
                    ushort[] tmp = new ushort[newLength];
                    
                    Array.Copy(_cachePointers, tmp, firstGlyphChanged);
                    Array.Copy(_cachePointers, oldAfterLastGlyphChanged, tmp, afterLastGlyphChanged, oldLength - oldAfterLastGlyphChanged);
                    
                    _cachePointers = tmp;
                }
                else
                {
                        Array.Copy(_cachePointers, oldAfterLastGlyphChanged, _cachePointers, afterLastGlyphChanged, oldLength - oldAfterLastGlyphChanged);
                }
            }
        }
        
        public unsafe ushort[] CachePointers
        {
            get { return _cachePointers; }
        }
        
        public byte[] TableCacheData
        {
            get { return _tableCache; }
            set { _tableCache = value; }
        }

        // Array of cache pointers, per glyph
        private unsafe ushort[]  _cachePointers;
        
        // Pointer to the table cache
        private byte[]      _tableCache;

#endregion Layout cache pointers
    }
}
