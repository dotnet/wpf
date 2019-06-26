// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System.Diagnostics;
using System.Security;
using System;
using System.IO;

namespace MS.Internal.Shaping
{

    internal static class Positioning
    {
        public static int DesignToPixels(ushort DesignUnitsPerEm, ushort PixelsPerEm, int Value)
        {
            //Result requested in design units 
            if (DesignUnitsPerEm==0) return Value;

            int  rounding = ((int)DesignUnitsPerEm)/2;;

            if (Value >= 0)
            {
                // Half of Units per Em            
                rounding = ((int)DesignUnitsPerEm)/2; 
            }
            else
            {
                // -(Half of Units per Em). +1 to ensure rounding
                rounding = -((int)DesignUnitsPerEm >> 1)+1; 
            }

            return (Value*(int)PixelsPerEm + rounding)/DesignUnitsPerEm;
        }
        
        
        /// <summary>
        ///  Align to anchors between two glyphs (e.g. mark and base) 
        ///  by changing adv.width and offsets for both of them 
        /// </summary>
        /// <param name="Font"></param>
        /// <param name="Table"></param>
        /// <param name="Metrics"></param>
        /// <param name="GlyphInfo"></param>
        /// <param name="Advances"></param>
        /// <param name="Offsets"></param>
        /// <param name="StaticGlyph"></param>
        /// <param name="MobileGlyph"></param>
        /// <param name="StaticAnchor"></param>
        /// <param name="MobileAnchor"></param>
        /// <param name="UseAdvances"></param>
        /// <returns></returns>
        public static unsafe void AlignAnchors(    
                                        IOpenTypeFont   Font,
                                        FontTable          Table,
                                        LayoutMetrics   Metrics,
                                        GlyphInfoList   GlyphInfo,   
                                        int*            Advances,
                                        LayoutOffset*   Offsets,
                                        int             StaticGlyph,
                                        int             MobileGlyph,
                                        AnchorTable     StaticAnchor,
                                        AnchorTable     MobileAnchor,
                                        bool            UseAdvances
                                      )
        {
            Invariant.Assert(StaticGlyph>=0 && StaticGlyph<GlyphInfo.Length);
            Invariant.Assert(MobileGlyph>=0 && MobileGlyph<GlyphInfo.Length);
            Invariant.Assert(!StaticAnchor.IsNull());
            Invariant.Assert(!MobileAnchor.IsNull());

            LayoutOffset ContourPoint = new LayoutOffset();
            if (StaticAnchor.NeedContourPoint(Table))
            {
                ContourPoint = Font.GetGlyphPointCoord(GlyphInfo.Glyphs[MobileGlyph],
                                                       StaticAnchor.ContourPointIndex(Table));
            }
            LayoutOffset StaticAnchorPoint = StaticAnchor.AnchorCoordinates(Table,Metrics,ContourPoint);
            
            if (MobileAnchor.NeedContourPoint(Table))
            {
                ContourPoint = Font.GetGlyphPointCoord(GlyphInfo.Glyphs[MobileGlyph],
                                                                  MobileAnchor.ContourPointIndex(Table));
            }
            LayoutOffset MobileAnchorPoint = MobileAnchor.AnchorCoordinates(Table,Metrics,ContourPoint);
            
            int AdvanceInBetween=0;
            if (StaticGlyph<MobileGlyph)
                for(int i=StaticGlyph+1;i<MobileGlyph;i++) AdvanceInBetween+=Advances[i];
            else
                for(int i=MobileGlyph+1;i<StaticGlyph;i++) AdvanceInBetween+=Advances[i];
                
            if (Metrics.Direction==TextFlowDirection.LTR ||
                Metrics.Direction==TextFlowDirection.RTL)
            {
                Offsets[MobileGlyph].dy = Offsets[StaticGlyph].dy + StaticAnchorPoint.dy
                                                                  - MobileAnchorPoint.dy;
 
                if ((Metrics.Direction==TextFlowDirection.LTR)==(StaticGlyph<MobileGlyph))
                {
                    // static glyph is on the left(phisically, not logically) to the mobile glyph
                    int dx = Offsets[StaticGlyph].dx - Advances[StaticGlyph] +  StaticAnchorPoint.dx 
                                        - AdvanceInBetween - MobileAnchorPoint.dx;

                    if (UseAdvances)
                    {
                        Advances[StaticGlyph] += dx;
                    }
                    else
                    {
                        Offsets[MobileGlyph].dx = dx;
                    }
                }
                else
                {
                    // static glyph is on the right(phisically, not logically) to the mobile glyph
                    int dx = Offsets[StaticGlyph].dx + Advances[MobileGlyph] +  StaticAnchorPoint.dx
                        + AdvanceInBetween - MobileAnchorPoint.dx;

                    if (UseAdvances)
                    {
                        Advances[MobileGlyph] -= dx;
                    }
                    else
                    {
                        Offsets[MobileGlyph].dx = dx;
                    }
                }
            }
            else
            {
                 // Not yet implemented
            }
}
    }

    internal struct DeviceTable
    {
        private const int offsetStartSize = 0;
        private const int offsetEndSize = 2;
        private const int offsetDeltaFormat = 4;
        private const int offsetDeltaValueArray = 6;
        private const int sizeDeltaValue = 2;
        
        private ushort StartSize(FontTable Table)
        {
            return Table.GetUShort(offset + offsetStartSize);
        }

        private ushort EndSize(FontTable Table)
        {
            return Table.GetUShort(offset + offsetEndSize);
        }
        
        private ushort DeltaFormat(FontTable Table)
        {
            return Table.GetUShort(offset + offsetDeltaFormat);
        }
        
        private ushort DeltaValue(FontTable Table, ushort Index)
        {
            return Table.GetUShort( offset + offsetDeltaValueArray + 
                                             Index * sizeDeltaValue);
        }
        
        public int Value(FontTable Table, ushort PixelsPerEm)
        {
            if (IsNull()) return 0;
            
            ushort startSize = StartSize(Table);
            ushort endSize   = EndSize(Table);
            
            if (PixelsPerEm<startSize || PixelsPerEm>endSize) return 0;
            
            ushort sizeIndex = (ushort)(PixelsPerEm-startSize);
            ushort valueIndex, shiftUp, shiftDown;
            
            switch (DeltaFormat(Table))
            {
                case 1:
                    valueIndex = (ushort)(sizeIndex>>3);
                    shiftUp    = (ushort)(16 + 2*(sizeIndex&0x0007));
                    shiftDown  = 30;
                    break;
                    
                case 2:
                    valueIndex = (ushort)(sizeIndex>>2);
                    shiftUp    = (ushort)(16 + 4*(sizeIndex&0x0003));
                    shiftDown  = 28;
                    break;

                case 3:
                    valueIndex = (ushort)(sizeIndex>>1);
                    shiftUp    = (ushort)(16 + 8*(sizeIndex&0x0001));
                    shiftDown  = 24;
                    break;
                    
                default: 
                    return 0; //Unknown format
            }
            
            int delta = DeltaValue(Table,valueIndex);
            delta <<= shiftUp;      //clear leading bits
            delta >>= shiftDown;    //extend sign and clear trailing bits
            
            return delta;
        }
    
        public DeviceTable(int Offset) { offset = Offset; }
        bool IsNull() { return (offset==0); }
        private int offset;
    }

    internal struct ValueRecordTable
    {
        const ushort XPlacmentFlag = 0x0001;
        const ushort YPlacmentFlag = 0x0002;
        const ushort XAdvanceFlag   = 0x0004;
        const ushort YAdvanceFlag  = 0x0008;
        const ushort XPlacementDeviceFlag = 0x0010;
        const ushort YPlacementDeviceFlag = 0x0020;
        const ushort XAdvanceDeviceFlag  = 0x0040;
        const ushort YAdvanceDeviceFlag  = 0x0080;
        
        private static ushort[] BitCount = 
                    new ushort[16] { 0, 2, 2, 4,  2, 4, 4, 6,  2, 4, 4, 6,  4, 6, 6, 8 };

        public static ushort Size(ushort Format)
        {
            return (ushort)(BitCount[Format&0x000F]+BitCount[(Format>>4)&0x000F]);
        }
         
        public void AdjustPos(  FontTable Table,
                                LayoutMetrics Metrics,
                                ref LayoutOffset GlyphOffset,
                                ref int    GlyphAdvance
                             )
        {
            int curOffset=offset;
            
            if ((format&XPlacmentFlag)!=0) 
            {
                GlyphOffset.dx += Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,
                                                                Table.GetShort(curOffset));
                curOffset+=2;
            }

            if ((format&YPlacmentFlag)!=0)
            {
                GlyphOffset.dy += Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmHeight,
                                                                Table.GetShort(curOffset));

                curOffset+=2;
            }

            if ((format&XAdvanceFlag)!=0) 
            {
                    GlyphAdvance += Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,
                                                                    Table.GetShort(curOffset));
                curOffset+=2;
            }

            if ((format&YAdvanceFlag)!=0) 
            {
                    GlyphAdvance += Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmHeight,
                        Table.GetShort(curOffset));
                curOffset+=2;
            }
            
            if ((format&XPlacementDeviceFlag)!=0) 
            {
                int deviceTableOffset = Table.GetOffset(curOffset);
                if (deviceTableOffset != FontTable.NullOffset) 
                {
                    DeviceTable deviceTable  = new DeviceTable(baseTableOffset+deviceTableOffset);
                    GlyphOffset.dx += deviceTable.Value(Table,Metrics.PixelsEmWidth);
                }
                                                        
                curOffset+=2;
            }            

            if ((format&YPlacementDeviceFlag)!=0) 
            {
                int deviceTableOffset = Table.GetOffset(curOffset);
                if (deviceTableOffset != FontTable.NullOffset) 
                {
                    DeviceTable deviceTable  = new DeviceTable(baseTableOffset+deviceTableOffset);
                    GlyphOffset.dy += deviceTable.Value(Table,Metrics.PixelsEmHeight);
                }
                                                        
                curOffset+=2;
            }            

            if ((format&XAdvanceDeviceFlag)!=0) 
            {
                if (Metrics.Direction==TextFlowDirection.LTR || Metrics.Direction==TextFlowDirection.RTL)
                {
                    int deviceTableOffset = Table.GetOffset(curOffset);
                    if (deviceTableOffset != FontTable.NullOffset) 
                    {
                        DeviceTable deviceTable  = new DeviceTable(baseTableOffset+deviceTableOffset);
                        GlyphAdvance += deviceTable.Value(Table,Metrics.PixelsEmWidth);
                    }
                }
                
                curOffset+=2;
            }            

            if ((format&YAdvanceDeviceFlag)!=0) 
            {
                if (Metrics.Direction==TextFlowDirection.TTB || Metrics.Direction==TextFlowDirection.BTT)
                {
                    int deviceTableOffset = Table.GetOffset(curOffset);
                    if (deviceTableOffset != FontTable.NullOffset) 
                    {
                        DeviceTable deviceTable  = new DeviceTable(baseTableOffset+deviceTableOffset);
                        GlyphAdvance += deviceTable.Value(Table,Metrics.PixelsEmHeight);
                    }
                }
                
                curOffset+=2;
            }            
        }

        public ValueRecordTable(int Offset, int BaseTableOffset, ushort Format) 
        { 
            offset = Offset; 
            baseTableOffset = BaseTableOffset;
            format=Format; 
        }
        
        private ushort format;
        private int baseTableOffset;
        private int offset;
    }

    internal struct AnchorTable
    {
        private const int offsetFormat = 0;
        private const int offsetXCoordinate = 2;
        private const int offsetYCoordinate = 4;
        private const int offsetFormat2AnchorPoint = 6;
        private const int offsetFormat3XDeviceTable = 6;
        private const int offsetFormat3YDeviceTable = 8;
        
        private short XCoordinate(FontTable Table)
        {
            return Table.GetShort(offset + offsetXCoordinate);
        }
        
        private short YCoordinate(FontTable Table)
        {
            return Table.GetShort(offset + offsetYCoordinate);
        }
        
        private ushort Format2AnchorPoint(FontTable Table)
        {
            Invariant.Assert(format==2);
            return Table.GetUShort(offset + offsetFormat2AnchorPoint);
        }
        
        private DeviceTable Format3XDeviceTable(FontTable Table)
        {
            Invariant.Assert(format==3);
            int DeviceOffset = Table.GetUShort(offset + offsetFormat3XDeviceTable);
            
            if (DeviceOffset!=0)
            {
                return new DeviceTable(offset + DeviceOffset);
            }
            else
            {
                return new DeviceTable(0);
            }
        }

        private DeviceTable Format3YDeviceTable(FontTable Table)
        {
            Invariant.Assert(format==3);

            int DeviceOffset = Table.GetUShort(offset + offsetFormat3YDeviceTable);
            
            if (DeviceOffset!=0)
            {
                return new DeviceTable(offset + DeviceOffset);
            }
            else
            {
                return new DeviceTable(0);
            }
        }
        
        public bool NeedContourPoint(FontTable Table)
        {
            return (format==2);
        }
        
        public ushort ContourPointIndex(FontTable Table)
        {
            Invariant.Assert(NeedContourPoint(Table));
            return Format2AnchorPoint(Table);
        }
        
        public LayoutOffset AnchorCoordinates( 
                                    FontTable       Table,
                                    LayoutMetrics   Metrics,
                                    LayoutOffset    ContourPoint
                                )
        {
            LayoutOffset Point = new LayoutOffset();
            
            switch (format)
            {
                case 1: //Simple coordinates 
                        Point.dx = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,XCoordinate(Table));
                        Point.dy = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmHeight,YCoordinate(Table));
                        break;
                
                case 2: //Coordinates + anchor point
                        if (ContourPoint.dx==int.MinValue)
                        {
                            Point.dx = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,XCoordinate(Table));
                            Point.dy = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmHeight,YCoordinate(Table));
                        }
                        else
                        {
                            Point.dx = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,ContourPoint.dx);
                            Point.dy = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,ContourPoint.dy);
                        }
                        break;
                
                case 3: //Coordinates + Device table
                        Point.dx = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmWidth,XCoordinate(Table))+
                                        Format3XDeviceTable(Table).Value(Table,Metrics.PixelsEmWidth);
                        Point.dy = Positioning.DesignToPixels(Metrics.DesignEmHeight,Metrics.PixelsEmHeight,YCoordinate(Table))+
                                        Format3YDeviceTable(Table).Value(Table,Metrics.PixelsEmHeight);
                        break;
                
                default: //Unknown format
                    Point.dx = 0;
                    Point.dx = 0;
                    break;
            }
            
            return Point;
        }
                            
    
        public AnchorTable(FontTable Table, int Offset) 
        { 
            offset = Offset;
            if (offset != 0)
                format = Table.GetUShort(offset + offsetFormat);
            else
                format = 0;
        }

        public bool IsNull() { return (offset==0); } 
        private int offset;
        private ushort format;
    }

    internal struct SinglePositioningSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetValueFormat = 4;
        private const int offsetFormat1Value = 6;
        private const int offsetFormat2ValueCount = 6;
        private const int offsetFormat2ValueArray = 8;
    
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }
        
        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetOffset(offset + offsetCoverage));
        }
        
        private ushort ValueFormat(FontTable Table)
        {
            return Table.GetUShort(offset + offsetValueFormat);
        }

        private ValueRecordTable Format1ValueRecord(FontTable Table)
        {
            Invariant.Assert(Format(Table)==1);

            return new ValueRecordTable(offset + offsetFormat1Value,
                                        offset,
                                        ValueFormat(Table));
        }
        
        private ValueRecordTable Format2ValueRecord(FontTable Table, ushort Index)
        {
            Invariant.Assert(Format(Table)==2);

            return new ValueRecordTable(offset +
                                            offsetFormat2ValueArray +
                                            Index * ValueRecordTable.Size(ValueFormat(Table)),
                                        offset,
                                        ValueFormat(Table));
        }
        
        public unsafe bool Apply(
            FontTable               Table,
            LayoutMetrics           Metrics,        // LayoutMetrics
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            int                     FirstGlyph,     // where to apply lookup
            int                     AfterLastGlyph, // how long is a context we can use
            out int                 NextGlyph       // Next glyph to process
        )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph + 1; //In case we don't match;
                
            int glyphCount=GlyphInfo.Length;
            ushort glyphId = GlyphInfo.Glyphs[FirstGlyph];
            
            int coverageIndex = Coverage(Table).GetGlyphIndex(Table,glyphId);
            if (coverageIndex == -1) return false;
            
            ValueRecordTable valueRecord;
            
            switch (Format(Table))
            {
                case 1:
                    valueRecord = Format1ValueRecord(Table);
                    break;
                case 2: 
                    valueRecord = Format2ValueRecord(Table,(ushort)coverageIndex);
                    break;
                default:
                    return false;
            }

            valueRecord.AdjustPos(Table, Metrics, ref Offsets[FirstGlyph], ref Advances[FirstGlyph]);
            
            return true;
        }        
        
        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            return Coverage(table).IsAnyGlyphCovered(table,
                                                     glyphBits,
                                                     minGlyphId,
                                                     maxGlyphId
                                                    );
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return Coverage(table);
        }

        public SinglePositioningSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct PairPositioningSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetValueFormat1 = 4;
        private const int offsetValueFormat2 = 6;
        private const int offsetFormat1PairSetCount = 8;
        private const int offsetFormat1PairSetArray = 10;
        private const int sizeFormat1PairSetOffset = 2;
        private const int offsetFormat2ClassDef1 = 8;
        private const int offsetFormat2ClassDef2 = 10;
        private const int offsetFormat2Class1Count = 12;
        private const int offsetFormat2Class2Count = 14;
        private const int offsetFormat2ValueRecordArray = 16;
        
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }
        
        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetOffset(offset + offsetCoverage));
        }
        
        private ushort FirstValueFormat(FontTable Table)
        {
            return Table.GetUShort(offset + offsetValueFormat1);
        }
        
        private ushort SecondValueFormat(FontTable Table)
        {
            return Table.GetUShort(offset + offsetValueFormat2);
        }

        // Not used. This value should be equal to glyph count in coverage table
        // Keeping it for future reference
        //private ushort Format1PairSetCount(FontTable Table)
        //{
        //    Debug.Assert(Format(Table)==1);
        //    return Table.GetUShort(offset + offsetFormat1PairSetCount);
        //}

        private PairSetTable Format1PairSet(FontTable Table, ushort Index)
        {
            Invariant.Assert(Format(Table)==1);
            return new PairSetTable(offset + Table.GetUShort(offset +
                                                             offsetFormat1PairSetArray +
                                                             Index * sizeFormat1PairSetOffset),
                                    FirstValueFormat(Table),
                                    SecondValueFormat(Table));
        }
        
        private ClassDefTable Format2Class1Table(FontTable Table)
        {
            Invariant.Assert(Format(Table)==2);
            return new ClassDefTable(offset+Table.GetUShort(offset + offsetFormat2ClassDef1));
        }
        
        private ClassDefTable Format2Class2Table(FontTable Table)
        {
            Invariant.Assert(Format(Table)==2);
            return new ClassDefTable(offset+Table.GetUShort(offset + offsetFormat2ClassDef2));
        }
    
        private ushort Format2Class1Count(FontTable Table)
        {
            Invariant.Assert(Format(Table)==2);
            return Table.GetUShort(offset + offsetFormat2Class1Count);
        }

        private ushort Format2Class2Count(FontTable Table)
        {
            Invariant.Assert(Format(Table)==2);
            return Table.GetUShort(offset + offsetFormat2Class2Count);
        }

        private ValueRecordTable Format2FirstValueRecord(FontTable Table,
                                             ushort Class2Count,
                                             ushort Class1Index, 
                                             ushort Class2Index
                                            )
        {
            Invariant.Assert(Format(Table)==2);

            ushort firstValueFormat  = FirstValueFormat(Table),
                   secondValueFormat = SecondValueFormat(Table);
            int    recordSize = ValueRecordTable.Size(firstValueFormat) + 
                                ValueRecordTable.Size(secondValueFormat);
            
            return new ValueRecordTable(offset + offsetFormat2ValueRecordArray +
                                            (Class1Index*Class2Count+Class2Index)*recordSize,
                                        offset,
                                        firstValueFormat);
        }
        
        private ValueRecordTable Format2SecondValueRecord(FontTable Table,
                                              ushort Class2Count,
                                              ushort Class1Index, 
                                              ushort Class2Index
                                             )
        {
            Invariant.Assert(Format(Table)==2);

            ushort firstValueFormat   = FirstValueFormat(Table),
                   secondValueFormat = SecondValueFormat(Table);
            int secondRecordOffset = ValueRecordTable.Size(firstValueFormat),
                recordSize = secondRecordOffset + ValueRecordTable.Size(secondValueFormat);
                   
            return new ValueRecordTable(offset + 
                                           offsetFormat2ValueRecordArray +
                                           (Class1Index*Class2Count+Class2Index)*recordSize +
                                           secondRecordOffset,
                                        offset,
                                        secondValueFormat
                                       );
        }
        
#region Pair positioing child structures

        private struct PairSetTable
        {
            private const int offsetPairValueCount = 0;
            private const int offsetPairValueArray = 2;
            private const int offsetPairValueSecondGlyph = 0;
            private const int offsetPairValueValue1 = 2;
    
            public ushort PairValueCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetPairValueCount);
            }
        
            public ushort PairValueGlyph(FontTable Table, ushort Index)
            {
                return Table.GetUShort( offset + offsetPairValueArray + 
                    Index*pairValueRecordSize +
                    offsetPairValueSecondGlyph);
            }
        
            public ValueRecordTable FirstValueRecord(FontTable Table, ushort Index, ushort Format)
            {
                return new ValueRecordTable(offset + offsetPairValueArray +
                    Index*pairValueRecordSize +
                    offsetPairValueValue1, 
                    offset, 
                    Format);
            }
        
            public ValueRecordTable SecondValueRecord(FontTable Table, ushort Index, ushort Format)
            {
                return new ValueRecordTable(offset + offsetPairValueArray +
                    Index*pairValueRecordSize + 
                    secondValueRecordOffset, 
                    offset, 
                    Format);
            }
        
            //Search for second glyph in pair. returns -1 if not found
            public int FindPairValue(FontTable Table, ushort Glyph)
            {
                //PERF: binary search
                ushort pairCount = PairValueCount(Table);
                for(ushort  i=0; i<pairCount;i++)
                {
                    if (PairValueGlyph(Table,i)==Glyph) return i;
                }
                return -1;
            }
        
            public PairSetTable(int Offset, ushort firstValueRecordSize, ushort secondValueRecordSize)  
            {
                secondValueRecordOffset = (ushort)(offsetPairValueValue1 + ValueRecordTable.Size(firstValueRecordSize));
                pairValueRecordSize = (ushort)(secondValueRecordOffset + ValueRecordTable.Size(secondValueRecordSize));
                                  
                offset = Offset;
            }
            private int offset;
            private ushort pairValueRecordSize;
            private ushort secondValueRecordOffset;
        }
#endregion

        public unsafe bool Apply(
                            IOpenTypeFont   Font,
                            FontTable       Table,
                            LayoutMetrics   Metrics,        // LayoutMetrics
                            GlyphInfoList   GlyphInfo,      // List of GlyphInfo structs
                            ushort          LookupFlags,    // Lookup flags for glyph lookups
                            int*            Advances,       // Glyph adv.widths
                            LayoutOffset*   Offsets,        // Glyph offsets
                            int             FirstGlyph,     // where to apply lookup
                            int             AfterLastGlyph, // how long is a context we can use
                            out int         NextGlyph       // Next glyph to process
                         )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph+1; //Always move to the next glyph, whether matched or not
                    
            int glyphCount=GlyphInfo.Length;
            ushort firstGlyphId = GlyphInfo.Glyphs[FirstGlyph];
            
            int secondGlyph = LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,FirstGlyph+1,LookupFlags,LayoutEngine.LookForward);            
            if (secondGlyph>=AfterLastGlyph) return false;
            
            ushort secondGlyphId = GlyphInfo.Glyphs[secondGlyph];
            
            ValueRecordTable firstValueRecord, secondValueRecord;
            
            switch (Format(Table))
            {
                case 1:
                {
                    int coverageIndex = Coverage(Table).GetGlyphIndex(Table,firstGlyphId);
                    if (coverageIndex==-1) return false;
                    
                    PairSetTable pairSet = Format1PairSet(Table,(ushort)coverageIndex);
                    
                    int pairValueIndex = pairSet.FindPairValue(Table, secondGlyphId);
                    if (pairValueIndex == -1) return false;
                    
                    firstValueRecord  = pairSet.FirstValueRecord(Table,(ushort)pairValueIndex,FirstValueFormat(Table));
                    secondValueRecord = pairSet.SecondValueRecord(Table,(ushort)pairValueIndex,SecondValueFormat(Table));
                    
                    break;
                }                        
                case 2:
                {
                    int coverageIndex = Coverage(Table).GetGlyphIndex(Table,firstGlyphId);
                    if (coverageIndex == -1) return false;

                    ushort firstClassIndex = Format2Class1Table(Table).GetClass(Table,firstGlyphId);
                    if (firstClassIndex >= Format2Class1Count(Table)) return false; //this is invalid font;
                    ushort secondClassIndex = Format2Class2Table(Table).GetClass(Table,secondGlyphId);
                    if (secondClassIndex >= Format2Class2Count(Table)) return false; //this is invalid font;
                    
                    ushort class2Count = Format2Class2Count(Table);
                    firstValueRecord = Format2FirstValueRecord(Table,
                                                               class2Count,
                                                               firstClassIndex,
                                                               secondClassIndex
                                                              );
                    secondValueRecord = Format2SecondValueRecord(Table,
                                                                 class2Count,
                                                                 firstClassIndex,
                                                                 secondClassIndex
                                                                );
                    break;
                }    
                default:
                    return false;
            }
            
            //Now adjust positions
            firstValueRecord.AdjustPos (Table, Metrics, ref Offsets[FirstGlyph],  ref Advances[FirstGlyph]);
            secondValueRecord.AdjustPos(Table, Metrics, ref Offsets[secondGlyph], ref Advances[secondGlyph]);
            
            return true;
        }

        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            return Coverage(table).IsAnyGlyphCovered(table,
                                                     glyphBits,
                                                     minGlyphId,
                                                     maxGlyphId
                                                    );
            // Consider checking second glyph
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return Coverage(table);
        }

        public PairPositioningSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct MarkArray
    {
        private const int offsetClassArray = 2;
        private const int sizeClassRecord = 4;
        private const int offsetClassRecordClass = 0;
        private const int offsetClassRecordAnchor = 2;
    
        public ushort Class(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetClassArray + 
                                            Index*sizeClassRecord + 
                                            offsetClassRecordClass);
        }
        
        public AnchorTable MarkAnchor(FontTable Table, ushort Index)
        {
            int anchorTableOffset = Table.GetUShort(offset + offsetClassArray + 
                                                    Index*sizeClassRecord+
                                                    offsetClassRecordAnchor
                                                   );
            if (anchorTableOffset == 0)
            {
                return new AnchorTable(Table, 0);
            }
        
            return new AnchorTable(Table,offset + anchorTableOffset);
        }

        public MarkArray(int Offset)  { offset = Offset; }
        private int offset;
    }

    internal struct MarkToBasePositioningSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetBaseCoverage = 4;
        private const int offsetClassCount = 6;
        private const int offsetMarkArray = 8;
        private const int offsetBaseArray = 10;
    
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable MarkCoverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }
    
        private CoverageTable BaseCoverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + 
                                                                  offsetBaseCoverage));
        }
        
        private ushort ClassCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetClassCount);
        }
        
        private MarkArray Marks(FontTable Table)
        {
            return new MarkArray(offset + Table.GetUShort(offset + offsetMarkArray));
        }
        
        private BaseArray Bases(FontTable Table)
        {
            return new BaseArray(offset + Table.GetUShort(offset + offsetBaseArray));
        }
        
#region Mark to base positioning child structures
        private struct BaseArray
        {
            private const int offsetAnchorArray = 2;
            private const int sizeAnchorOffset = 2;
        
            public AnchorTable BaseAnchor(FontTable Table, ushort BaseIndex, 
                ushort MarkClassCount, 
                ushort MarkClass)
            {
                int anchorTableOffset = Table.GetUShort(offset + offsetAnchorArray +
                                                        (BaseIndex*MarkClassCount + MarkClass) *
                                                        sizeAnchorOffset
                                                       );
                if (anchorTableOffset == 0)
                {
                    return new AnchorTable(Table, 0);
                }
            
                return new AnchorTable(Table, offset + anchorTableOffset);
            }
    
            public BaseArray(int Offset)  { offset = Offset; }
            private int offset;
        }
#endregion

        public unsafe bool Apply(
            IOpenTypeFont   Font,
            FontTable       Table,
            LayoutMetrics   Metrics,        // LayoutMetrics
            GlyphInfoList   GlyphInfo,      // List of GlyphInfo structs
            ushort          LookupFlags,    // Lookup flags for glyph lookups
            int*            Advances,       // Glyph adv.widths
            LayoutOffset*   Offsets,        // Glyph offsets
            int             FirstGlyph,     // where to apply lookup
            int             AfterLastGlyph, // how long is a context we can use
            out int         NextGlyph       // Next glyph to process
            )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph+1; //Always move to the next glyph, whether matched or not
            
            if (Format(Table) != 1) return false; //unknown format
                    
            int glyphCount=GlyphInfo.Length;
            
            int markGlyph=FirstGlyph;
            
            //Lookup works with marks only
            if ((GlyphInfo.GlyphFlags[markGlyph]&(ushort)GlyphFlags.GlyphTypeMask)!=(ushort)GlyphFlags.Mark) return false;
            
            int markCoverageIndex = MarkCoverage(Table).GetGlyphIndex(Table,GlyphInfo.Glyphs[markGlyph]);
            if (markCoverageIndex==-1) return false;
            
            //Find preceeding base (precisely, not mark ). Uses special lookup flag
            int baseGlyph = LayoutEngine.GetNextGlyphInLookup(Font,
                                                              GlyphInfo,
                                                              FirstGlyph - 1,
                                                              LayoutEngine.LookupFlagFindBase,
                                                              LayoutEngine.LookBackward);
            if (baseGlyph<0) return false;
            
            int baseCoverageIndex = BaseCoverage(Table).GetGlyphIndex(Table,GlyphInfo.Glyphs[baseGlyph]);
            if (baseCoverageIndex == -1) return false;

            ushort classCount = ClassCount(Table);
            MarkArray marks = Marks(Table);

            ushort markClass = marks.Class(Table,(ushort)markCoverageIndex);
            if (markClass>=classCount) return false; //Invalid mark class
            
            AnchorTable markAnchor = marks.MarkAnchor(Table,(ushort)markCoverageIndex);
            if (markAnchor.IsNull())
            {
                return false;
            }

            AnchorTable baseAnchor = Bases(Table).BaseAnchor(Table,(ushort)baseCoverageIndex,classCount,markClass);
            if (baseAnchor.IsNull())
            {
                return false;
            }
            
            Positioning.AlignAnchors(Font,Table,Metrics,GlyphInfo,Advances,Offsets,
                                        baseGlyph,markGlyph,baseAnchor,markAnchor,false);
            return true;
        }        
        
        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            return false;
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return MarkCoverage(table);
        }

        public MarkToBasePositioningSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct MarkToMarkPositioningSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetMark2Coverage = 4;
        private const int offsetClassCount = 6;
        private const int offsetMark1Array = 8;
        private const int offsetMark2Array = 10;
        
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable Mark1Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }
    
        private CoverageTable Mark2Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetMark2Coverage));
        }
        
        private ushort Mark1ClassCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetClassCount);
        }
        
        private MarkArray Mark1Array(FontTable Table)
        {
            return new MarkArray(offset + Table.GetUShort(offset + offsetMark1Array));
        }
        
        private Mark2Array Marks2(FontTable Table)
        {
            return new Mark2Array(offset + Table.GetUShort(offset + offsetMark2Array));
        }
        
#region Mark to mark positioning child structures
        private struct Mark2Array
        {
            private const int offsetCount = 0;
            private const int offsetAnchors = 2;
            private const int sizeAnchorOffset = 2;
    
            public AnchorTable Anchor(FontTable Table, 
                ushort Mark2Index, 
                ushort Mark1ClassCount, 
                ushort Mark1Class)
            {
                int anchorTableOffset = Table.GetUShort(offset + offsetAnchors + 
                                                        (Mark2Index*Mark1ClassCount+Mark1Class) * 
                                                        sizeAnchorOffset
                                                       );
                if (anchorTableOffset == 0)
                {
                    return new AnchorTable(Table, 0);
                }
            
                return new AnchorTable(Table, offset + anchorTableOffset);
            }                                  
#endregion        
        
            public Mark2Array(int Offset)  { offset = Offset; }
            private int offset;
        }

        public unsafe bool Apply(
            IOpenTypeFont   Font,
            FontTable       Table,
            LayoutMetrics   Metrics,        // LayoutMetrics
            GlyphInfoList   GlyphInfo,      // List of GlyphInfo structs
            ushort          LookupFlags,    // Lookup flags for glyph lookups
            int*            Advances,       // Glyph adv.widths
            LayoutOffset*   Offsets,        // Glyph offsets
            int             FirstGlyph,     // where to apply lookup
            int             AfterLastGlyph, // how long is a context we can use
            out int         NextGlyph       // Next glyph to process
            )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph+1; //Always move to the next glyph, whether matched or not
            
            if (Format(Table) != 1) return false; //unknown format
                    
            int glyphCount=GlyphInfo.Length;
            
            int mark1Glyph=FirstGlyph;
            
            //Lookup works with marks only
            if ((GlyphInfo.GlyphFlags[mark1Glyph]&(ushort)GlyphFlags.GlyphTypeMask)!=(ushort)GlyphFlags.Mark) return false;
            
            int mark1CoverageIndex = Mark1Coverage(Table).GetGlyphIndex(Table,GlyphInfo.Glyphs[mark1Glyph]);
            if (mark1CoverageIndex==-1) return false;
            
            //Find preceeding mark according mark from specified class
            int mark2Glyph = LayoutEngine.GetNextGlyphInLookup(Font,
                GlyphInfo,
                FirstGlyph-1,
                (ushort)(LookupFlags & 0xFF00), //Clear Ignore... flags
                LayoutEngine.LookBackward);
            if (mark2Glyph<0) return false;
            
            int mark2CoverageIndex = Mark2Coverage(Table).GetGlyphIndex(Table,GlyphInfo.Glyphs[mark2Glyph]);
            if (mark2CoverageIndex==-1) return false;

            ushort classCount = Mark1ClassCount(Table);
            MarkArray mark1Array = Mark1Array(Table);

            ushort mark1Class = mark1Array.Class(Table,(ushort)mark1CoverageIndex);
            if (mark1Class>=classCount) return false; //Invalid mark class
            
            AnchorTable mark1Anchor = mark1Array.MarkAnchor(Table,(ushort)mark1CoverageIndex);
            if (mark1Anchor.IsNull())
            {
                return false;
            }
            
            AnchorTable mark2Anchor = Marks2(Table).Anchor(Table,(ushort)mark2CoverageIndex,classCount,mark1Class);
            if (mark2Anchor.IsNull())
            {
                return false;
            }
            
            Positioning.AlignAnchors(Font,Table,Metrics,GlyphInfo,Advances,Offsets,
                mark2Glyph,mark1Glyph,mark2Anchor,mark1Anchor,false);
            return true;
        }        
        
        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            return false;
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return Mark1Coverage(table);
        }
        
        public MarkToMarkPositioningSubtable(int Offset)  { offset = Offset; }
        private int offset;
    }

    struct CursivePositioningSubtable
    {
        private const ushort offsetFormat = 0;
        private const ushort offsetCoverage = 2;
        private const ushort offsetEntryExitCount = 4;
        private const ushort offsetEntryExitArray = 6;
        private const ushort sizeEntryExitRecord = 4;
        private const ushort offsetEntryAnchor = 0;
        private const ushort offsetExitAnchor = 2;

        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }

        // Not used. This value should be equal to glyph count in Coverage.
        // Keeping it for future reference
        //private ushort EntryExitCount(FontTable Table)
        //{
        //    return Table.GetUShort(offsetEntryExitCount);
        //}        

        private AnchorTable EntryAnchor(FontTable Table, int Index)
        {
            int anchorTableOffset = Table.GetUShort(offset + 
                                                        offsetEntryExitArray + 
                                                        sizeEntryExitRecord * Index +
                                                        offsetEntryAnchor);
            if (anchorTableOffset == 0) return new AnchorTable(Table,0);
            
            return new AnchorTable(Table, offset + anchorTableOffset);
        }
        
        private AnchorTable ExitAnchor(FontTable Table, int Index)
        {
            int anchorTableOffset = Table.GetUShort(offset + 
                                                        offsetEntryExitArray + 
                                                        sizeEntryExitRecord * Index +
                                                        offsetExitAnchor);
            if (anchorTableOffset == 0) return new AnchorTable(Table,0);
            
            return new AnchorTable(Table, offset + anchorTableOffset);
        }

        public unsafe bool Apply(
            IOpenTypeFont   Font,
            FontTable       Table,
            LayoutMetrics   Metrics,        // LayoutMetrics
            GlyphInfoList   GlyphInfo,      // List of GlyphInfo structs
            ushort          LookupFlags,    // Lookup flags for glyph lookups
            int*            Advances,       // Glyph adv.widths
            LayoutOffset*   Offsets,        // Glyph offsets
            int             FirstGlyph,     // where to apply lookup
            int             AfterLastGlyph, // how long is a context we can use
            out int         NextGlyph       // Next glyph to process
            )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph+1;

            if (Format(Table) != 1) return false; // Unknown format            
            
            bool RTL = (LookupFlags & LayoutEngine.LookupFlagRightToLeft) != 0;
            ushort cursiveBit = (ushort)GlyphFlags.CursiveConnected;

            // Consider optimizing the whole range processing, 
            // ? probably send "single subtable" flag here since 99%
            //   cursive attachment lookups will have one subtable. 
            // ?  The same for anywhere we can reuse coverage, like kerning ?

            int glyphIndex, prevGlyphIndex,
                coverageIndex, prevCoverageIndex;
                
            glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,
                                                               FirstGlyph,LookupFlags,
                                                               LayoutEngine.LookForward
                                                           );

            //clear "CursiveConected" bit,
            //we will set it only if there is a connection to previous glyph
            if (RTL)
            {
                GlyphInfo.GlyphFlags[glyphIndex] &= (ushort)~cursiveBit;
            }
            
            if ( glyphIndex >= AfterLastGlyph ) 
            {
                return false;
            }
            
            prevGlyphIndex = LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,
                                                               FirstGlyph-1,LookupFlags,
                                                               LayoutEngine.LookBackward
                                                              );
            if ( prevGlyphIndex < 0 ) 
            {
                return false;
            }
            
            CoverageTable coverage = Coverage(Table);
            
            coverageIndex = coverage.GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex]);
            if (coverageIndex == -1)
            {
                return false;
            }
            
            prevCoverageIndex = coverage.
                                   GetGlyphIndex(Table,GlyphInfo.Glyphs[prevGlyphIndex]);
            if (prevCoverageIndex == -1)
            {
                return false;
            }
            
            AnchorTable prevExitAnchor, entryAnchor;
            
            prevExitAnchor = ExitAnchor(Table, prevCoverageIndex);
            if (prevExitAnchor.IsNull())
            {
                return false;
            }
            
            entryAnchor = EntryAnchor(Table,coverageIndex);
            if (entryAnchor.IsNull())
            {
                return false;
            }
            
            Positioning.AlignAnchors(Font, Table, Metrics,
                                     GlyphInfo, Advances, Offsets,
                                     prevGlyphIndex, glyphIndex,
                                     prevExitAnchor, entryAnchor, true);
            
            if (RTL)
            {
                UshortList glyphFlags = GlyphInfo.GlyphFlags;
                
                int index;
                
                //set "cursive" bit for everything up to prevGlyphIndex
                for(index = glyphIndex; index>prevGlyphIndex; index--)
                {
                    glyphFlags[index] |= cursiveBit;
                }                    
                
                //fix cursive dependencies
                int yCorrection = Offsets[glyphIndex].dy;
                for(index = glyphIndex; 
                    (glyphFlags[index] & cursiveBit) != 0 ;
                    index--
                   )
                {
                    Offsets[index].dy -= yCorrection;
                }
                Invariant.Assert(glyphIndex>=0); //First glyph should not have bit set
                Offsets[index].dy  -= yCorrection;
            }
            
            return true;
        }
        
        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            return true;
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return Coverage(table);
        }

        public CursivePositioningSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct LigatureAttachTable
    {
        private const int offsetAnchorArray = 2;
        private const int sizeAnchorOffset = 2;

        public AnchorTable LigatureAnchor(FontTable Table, 
                                          ushort Component,
                                          ushort MarkClass)
        {
            int anchorTableOffset = Table.GetUShort(offset + offsetAnchorArray +
                                                    (Component*classCount + MarkClass) *
                                                                      sizeAnchorOffset);
            if (anchorTableOffset == 0)
            {
                return new AnchorTable(Table, 0);
            }
                
            return new AnchorTable(Table, offset + anchorTableOffset);
        }
    
        public LigatureAttachTable(int Offset,ushort ClassCount) 
        { 
            offset = Offset; 
            classCount = ClassCount;
        }
        
        private int offset;
        private int classCount;
    }

    internal struct MarkToLigaturePositioningSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetMarkCoverage = 2;
        private const int offsetLigatureCoverage = 4;
        private const int offsetClassCount = 6;
        private const int offsetMarkArray = 8;
        private const int offsetLigatureArray = 10;
        private const int offsetLigatureAttachArray = 2;
        private const int sizeOffset = 2;
        
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable MarkCoverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetMarkCoverage));
        }
    
        private CoverageTable LigatureCoverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetLigatureCoverage));
        }
        
        private ushort ClassCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetClassCount);
        }
        
        private MarkArray Marks(FontTable Table)
        {
            return new MarkArray(offset + Table.GetUShort(offset + offsetMarkArray));
        }
        
        private LigatureAttachTable Ligatures(FontTable Table, int Index, ushort ClassCount)
        {
            int offsetLigatureArrayTable = offset + Table.GetUShort(offset + offsetLigatureArray);
            return new LigatureAttachTable(offsetLigatureArrayTable + 
                                           Table.GetUShort(offsetLigatureArrayTable + 
                                                           offsetLigatureAttachArray +
                                                           Index * sizeOffset),
                                           ClassCount
                                          );
        }
        
        // Find base ligature and component corresponding to the mark
        private unsafe void FindBaseLigature (
            int             CharCount,
            UshortList      Charmap,
            GlyphInfoList   GlyphInfo,
            int             markGlyph,
            out ushort      component,
            out int         ligatureGlyph
           )
        {
            int ligatureChar = 0;
            ligatureGlyph = -1;
            component = 0;
            
            bool FoundBase = false;
            for (int ch = GlyphInfo.FirstChars[markGlyph];
                        ch >= 0 && !FoundBase; ch--)
            {
                ushort glyph = Charmap[ch];
                if ((GlyphInfo.GlyphFlags[glyph] & (ushort)GlyphFlags.GlyphTypeMask) != 
                                                                    (ushort)GlyphFlags.Mark)
                {
                    ligatureChar = ch;
                    ligatureGlyph = glyph;
                    FoundBase = true;
                }
            }
            if (!FoundBase) return;

            ushort comp = 0;
            for(ushort ch = GlyphInfo.FirstChars[ligatureGlyph];
                ch<CharCount; ch++)
            {
                if (ch == ligatureChar) break;
                if (Charmap[ch]==ligatureGlyph) comp++;
            }
            component = comp;
        }

        public unsafe bool Apply(
            IOpenTypeFont   Font,
            FontTable       Table,
            LayoutMetrics   Metrics,        // LayoutMetrics
            GlyphInfoList   GlyphInfo,      // List of GlyphInfo structs
            ushort          LookupFlags,    // Lookup flags for glyph lookups
            int             CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList      Charmap,        // Char to glyph mapping
            int*            Advances,       // Glyph adv.widths
            LayoutOffset*   Offsets,        // Glyph offsets
            int             FirstGlyph,     // where to apply lookup
            int             AfterLastGlyph, // how long is a context we can use
            out int         NextGlyph       // Next glyph to process
            )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph+1; //Always move to the next glyph, whether matched or not
            
            if (Format(Table) != 1) return false; //unknown format
                    
            int glyphCount=GlyphInfo.Length;
            int markGlyph=FirstGlyph;
            
            //Lookup works with marks only
            if ((GlyphInfo.GlyphFlags[markGlyph]&(ushort)GlyphFlags.GlyphTypeMask)!=(ushort)GlyphFlags.Mark) return false;
            
            int markCoverageIndex = MarkCoverage(Table).GetGlyphIndex(Table,GlyphInfo.Glyphs[markGlyph]);
            if (markCoverageIndex==-1) return false;
            
            int baseGlyph;
            ushort component;
            
            FindBaseLigature(CharCount, Charmap,GlyphInfo,markGlyph, 
                                                out component, out baseGlyph);
            if (baseGlyph<0) return false;
            
            int baseCoverageIndex = LigatureCoverage(Table).
                                        GetGlyphIndex(Table,GlyphInfo.Glyphs[baseGlyph]);
            if (baseCoverageIndex == -1) return false;
            
            ushort classCount = ClassCount(Table);
            MarkArray marks = Marks(Table);

            ushort markClass = marks.Class(Table,(ushort)markCoverageIndex);
            if (markClass>=classCount) return false; //Invalid mark class
            
            AnchorTable baseAnchor = Ligatures(Table,baseCoverageIndex, classCount).
                                            LigatureAnchor(Table,component,markClass);
            if (baseAnchor.IsNull())
            {
                return false;
            }
                                            
            AnchorTable markAnchor = marks.MarkAnchor(Table,(ushort)markCoverageIndex);
            if (markAnchor.IsNull())
            {
                return false;
            }

            Positioning.AlignAnchors(Font,Table,Metrics,GlyphInfo,Advances,Offsets,
                baseGlyph,markGlyph,baseAnchor,markAnchor,false);
            
            return true;
        }        
        
        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            return false;
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return MarkCoverage(table);
        }

        public MarkToLigaturePositioningSubtable(int Offset) { offset = Offset; }
        private int offset;
    }
}
