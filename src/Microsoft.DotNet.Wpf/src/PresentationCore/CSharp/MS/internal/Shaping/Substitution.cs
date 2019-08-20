// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  OpentTypeLayout substitution classes

using System.Diagnostics;
using System.Security;
using System;
using System.IO;

namespace MS.Internal.Shaping
{
    /// <remarks>
    /// Correct algorithm for multiple substitution hasn't been implemented yet.
    /// </remarks>
    internal struct SingleSubstitutionSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetFormat1DeltaGlyphId = 4;
        private const int offsetFormat2GlyphCount = 4;
        private const int offsetFormat2SubstitutehArray = 6;
        private const int sizeFormat2SubstituteSize = 2;

        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }
        
        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset+Table.GetUShort(offset + offsetCoverage));
        }
        
        private short Format1DeltaGlyphId(FontTable Table)
        {
            Invariant.Assert(Format(Table)==1);
            return Table.GetShort(offset + offsetFormat1DeltaGlyphId);
        }
        
        // Not used. This value should be equal to glyph count in Coverage.
        // Keeping it for future reference
        //private ushort Foramt2GlyphCount(FontTable Table)
        //{
        //    Debug.Assert(Format(Table)==2);
        //    return Table.GetUShort(offset + offsetFormat2GlyphCount);
        //}       
        private ushort Format2SubstituteGlyphId(FontTable Table,ushort Index)
        {
            Invariant.Assert(Format(Table)==2);
            return Table.GetUShort(offset + offsetFormat2SubstitutehArray + 
                                            Index * sizeFormat2SubstituteSize);
        }

        public bool Apply(
                            FontTable          Table,
                            GlyphInfoList   GlyphInfo,      // List of GlyphInfo structs
                            int             FirstGlyph,     // where to apply it
                            out int         NextGlyph       // Next glyph to process
                         )
        {
            Invariant.Assert(FirstGlyph >= 0);
        
            NextGlyph = FirstGlyph + 1; //In case we don't match;
                
            ushort GlyphId = GlyphInfo.Glyphs[FirstGlyph];
            int CoverageIndex = Coverage(Table).GetGlyphIndex(Table,GlyphId);
            if (CoverageIndex == -1) return false;
            
            switch(Format(Table))
            {
                case 1:
                    GlyphInfo.Glyphs[FirstGlyph] = (ushort)(GlyphId + Format1DeltaGlyphId(Table));
                    GlyphInfo.GlyphFlags[FirstGlyph] = (ushort)(GlyphFlags.Unresolved | GlyphFlags.Substituted);
                    NextGlyph = FirstGlyph + 1;
                    return true;

                case 2:
                    GlyphInfo.Glyphs[FirstGlyph] = Format2SubstituteGlyphId(Table,(ushort)CoverageIndex);
                    GlyphInfo.GlyphFlags[FirstGlyph] = (ushort)(GlyphFlags.Unresolved | GlyphFlags.Substituted);
                    NextGlyph = FirstGlyph + 1;
                    return true;

                default:
                    NextGlyph = FirstGlyph+1;
                    return false;
            }
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
        
        public SingleSubstitutionSubtable(int Offset) { offset = Offset; }
        private int offset;
    }


    internal struct LigatureSubstitutionSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetLigatureSetCount = 4;
        private const int offsetLigatureSetArray = 6;
        private const int sizeLigatureSet = 2;
         
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }
        
        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }

        private ushort LigatureSetCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetLigatureSetCount);
        }
        
        private LigatureSetTable LigatureSet(FontTable Table, ushort Index)
        {
            return new LigatureSetTable(offset+Table.GetUShort(offset+
                                                               offsetLigatureSetArray + 
                                                               Index * sizeLigatureSet));
        }

#region Ligature Substitution subtable private structures        
        private struct LigatureSetTable
        {
            private const int offsetLigatureCount = 0;
            private const int offsetLigatureArray = 2;
            private const int sizeLigatureOffset  = 2;

            public ushort LigatureCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetLigatureCount);
            }
        
            public LigatureTable Ligature(FontTable Table, ushort Index)
            {
                return new LigatureTable(offset + Table.GetUShort(offset + 
                    offsetLigatureArray + 
                    Index * sizeLigatureOffset));
            }
        
            public LigatureSetTable(int Offset) { offset = Offset; }
            private int offset;
        }

        private struct LigatureTable
        {
            private const int offsetLigatureGlyph = 0;
            private const int offsetComponentCount = 2;
            private const int offsetComponentArray = 4;
            private const int sizeComponent = 2;
    
            public ushort LigatureGlyph(FontTable Table)
            {
                return Table.GetUShort(offset + offsetLigatureGlyph);
            }
        
            public ushort ComponentCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetComponentCount);
            }

            public ushort Component(FontTable Table, ushort Index)
            {
                //LigaTable includes comps from 1 to N. So, (Index-1)
                return Table.GetUShort(offset + offsetComponentArray +
                    (Index-1) * sizeComponent);
            }
        
            public LigatureTable(int Offset) { offset = Offset; }
            private int offset;
        }
#endregion

        public unsafe bool Apply(
                            IOpenTypeFont   Font,
                            FontTable       Table,
                            int             CharCount,
                            UshortList      Charmap,        // Character to glyph map
                            GlyphInfoList   GlyphInfo,      // List of GlyphInfo
                            ushort          LookupFlags,    // Lookup flags for glyph lookups
                            int             FirstGlyph,     // where to apply it
                            int             AfterLastGlyph, // how long is a context we can use
                            out int         NextGlyph       // Next glyph to process
                          )
        {
            Invariant.Assert(FirstGlyph>=0);
            Invariant.Assert(AfterLastGlyph<=GlyphInfo.Length);

            NextGlyph = FirstGlyph + 1; //In case we don't match;
            
            if (Format(Table) != 1) return false; // Unknown format
                
            int glyphCount=GlyphInfo.Length;
            ushort glyphId = GlyphInfo.Glyphs[FirstGlyph];
            int CoverageIndex = Coverage(Table).GetGlyphIndex(Table,glyphId);
            if (CoverageIndex==-1) return false;

            int curGlyph;
            ushort ligatureGlyph=0;
            bool match = false;
            ushort compCount=0;

            LigatureSetTable ligatureSet = LigatureSet(Table,(ushort)CoverageIndex);
            ushort ligaCount = ligatureSet.LigatureCount(Table);
            for(ushort liga=0; liga<ligaCount; liga++)
            {
                LigatureTable ligature = ligatureSet.Ligature(Table,liga);
                compCount = ligature.ComponentCount(Table);
                if (compCount == 0)
                {
                    throw new FileFormatException();
                }
                
                curGlyph=FirstGlyph;
                ushort comp=1;
                for(comp=1;comp<compCount;comp++)
                {
                    curGlyph = LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,curGlyph+1,LookupFlags,LayoutEngine.LookForward);
                    if (curGlyph>=AfterLastGlyph) break;
                    
                    if (GlyphInfo.Glyphs[curGlyph]!=ligature.Component(Table,comp)) break;
                }
                
                if (comp==compCount) //liga matched
                {
                    match=true;
                    ligatureGlyph = ligature.LigatureGlyph(Table);
                    break; //Liga found
                }
            }
            //If no ligature found, match will remain false after last iteration
            
            if (match) 
            {
                //Fix character and glyph Mapping
            
                //PERF: localize ligature character range
                    
                //Calculate Ligature CharCount
                int totalLigaCharCount=0;
                int firstLigaChar=int.MaxValue;
                curGlyph=FirstGlyph;
                for(ushort comp=0;comp<compCount;comp++)
                {
                    Invariant.Assert(curGlyph<AfterLastGlyph);
                    
                    int curFirstChar = GlyphInfo.FirstChars[curGlyph];
                    int curLigaCount = GlyphInfo.LigatureCounts[curGlyph];
                    
                    totalLigaCharCount += curLigaCount;
                    if (curFirstChar<firstLigaChar) firstLigaChar=curFirstChar;
                    
                    curGlyph = LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,curGlyph+1,LookupFlags,LayoutEngine.LookForward);
                }
                    
                curGlyph=FirstGlyph;
                int prevGlyph=FirstGlyph;
                ushort shift=0;
                for(ushort comp=1;comp<=compCount;comp++)
                {
                    prevGlyph=curGlyph;
                    
                    if (comp<compCount)
                    {
                        curGlyph = LayoutEngine.
                                        GetNextGlyphInLookup(Font,GlyphInfo,
                                                             curGlyph+1,
                                                             LookupFlags,
                                                             LayoutEngine.LookForward);
                    }
                    else curGlyph = GlyphInfo.Length; // to the end from last component
                    
                    // Set charmap for ligature component
                    for(int curChar=0; curChar<CharCount; curChar++)
                    {
                        if (Charmap[curChar]==prevGlyph)
                        {
                            Charmap[curChar] = (ushort)FirstGlyph;
                        }
                    }

                    //Shift glyphInfo
                    if (shift>0)
                    {
                        for(int glyph=prevGlyph+1; glyph<curGlyph; glyph++)
                        {
                            GlyphInfo.Glyphs[glyph-shift]         = GlyphInfo.Glyphs[glyph];
                            GlyphInfo.GlyphFlags[glyph-shift]     = GlyphInfo.GlyphFlags[glyph];
                            GlyphInfo.FirstChars[glyph-shift]     = GlyphInfo.FirstChars[glyph];
                            GlyphInfo.LigatureCounts[glyph-shift] = GlyphInfo.LigatureCounts[glyph];
                        }
                    
                        if (curGlyph-prevGlyph>1) //do fixing only if have glyphs in between
                        {
                            for(int curChar=0; curChar<CharCount; curChar++)
                            {
                                ushort curCharmap = Charmap[curChar];
                                if (curCharmap>prevGlyph && curCharmap<curGlyph)
                                {
                                    Charmap[curChar] -= shift;
                                }
                            }
                        }
                    }
                                        
                    ++shift;
                }
                
                //Place new glyph into position of first ligature glyph
                GlyphInfo.Glyphs[FirstGlyph]         = ligatureGlyph;
                GlyphInfo.GlyphFlags[FirstGlyph]     = (ushort)(GlyphFlags.Unresolved | GlyphFlags.Substituted);
                GlyphInfo.FirstChars[FirstGlyph]     = (ushort)firstLigaChar;
                GlyphInfo.LigatureCounts[FirstGlyph] = (ushort)totalLigaCharCount;
                
                //remove empty space
                if (compCount > 1)
                {
                    GlyphInfo.Remove(GlyphInfo.Length-compCount+1,compCount-1);
                }
                
                NextGlyph=prevGlyph-(compCount-1)+1;
            }
            
            return match;
        }

        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            if (!Coverage(table).IsAnyGlyphCovered(table,
                                                     glyphBits,
                                                     minGlyphId,
                                                     maxGlyphId
                                                    )
               ) return false;

            ushort ligatureSetCount = LigatureSetCount(table);
                        
            for(ushort setIndex = 0; setIndex < ligatureSetCount; setIndex++)
            {
                LigatureSetTable ligatureSet = LigatureSet(table, setIndex);
                ushort ligaCount = ligatureSet.LigatureCount(table);
                
                for (ushort liga = 0; liga < ligaCount; liga++)
                {
                    LigatureTable ligature = ligatureSet.Ligature(table, liga);
                    ushort compCount = ligature.ComponentCount(table);
                    
                    bool ligatureIsComplex = true;
                    
                    for(ushort compIndex = 1; compIndex < compCount; compIndex++)
                    {
                        ushort glyphId = ligature.Component(table,compIndex);

                        if (glyphId > maxGlyphId || 
                            glyphId < minGlyphId ||
                            (glyphBits[glyphId >> 5] & (1 << (glyphId % 32))) == 0
                           )
                        {
                            ligatureIsComplex = false;
                            break;
                        }
                    }
                    
                    if (ligatureIsComplex) return true;
                }
            }
            
            return false;
}

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            return Coverage(table);
        }

        public LigatureSubstitutionSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct MultipleSubstitutionSequenceTable
    {
        private const int offsetGlyphCount = 0;
        private const int offsetGlyphArray = 2;
        private const int sizeGlyphId      = 2;

        public ushort GlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetGlyphCount);
        }
        
        public ushort Glyph(FontTable Table, ushort index)
        {
            return Table.GetUShort(offset + offsetGlyphArray + index * sizeGlyphId);
        }

        public MultipleSubstitutionSequenceTable(int Offset) { offset = Offset; }
        private int offset;
    }
    
    internal struct MultipleSubstitutionSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetSequenceCount = 4;
        private const int offsetSequenceArray = 6;
        private const int sizeSequenceOffset = 2;
         
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
        //private ushort SequenceCount(FontTable Table)
        //{
        //    return Table.GetUShort(offset + offsetSequenceCount);
        //}
        
        private MultipleSubstitutionSequenceTable Sequence(FontTable Table, int Index)
        {
            return new MultipleSubstitutionSequenceTable(
                                        offset + 
                                        Table.GetUShort(offset +
                                                        offsetSequenceArray +
                                                        Index * sizeSequenceOffset)
                                       );
        }

        public unsafe bool Apply(
            IOpenTypeFont   Font,
            FontTable       Table,
            int             CharCount,
            UshortList      Charmap,        // Character to glyph map
            GlyphInfoList   GlyphInfo,      // List of GlyphInfo
            ushort          LookupFlags,    // Lookup flags for glyph lookups
            int             FirstGlyph,     // where to apply it
            int             AfterLastGlyph, // how long is a context we can use
            out int         NextGlyph       // Next glyph to process
            )
         {
            NextGlyph = FirstGlyph + 1; // in case we don't match
            
            if (Format(Table) != 1) return false; //unknown format
            
            int oldGlyphCount=GlyphInfo.Length;
            
            ushort glyphId = GlyphInfo.Glyphs[FirstGlyph];
            int coverageIndex = Coverage(Table).GetGlyphIndex(Table,glyphId);
            if (coverageIndex==-1) return false;
            
            MultipleSubstitutionSequenceTable sequence = Sequence(Table,coverageIndex);

            ushort sequenceLength = sequence.GlyphCount(Table);
            int lengthDelta = sequenceLength - 1;
            
            if (sequenceLength==0)
            {
                // This is illegal, because mapping will be broken -
                // corresponding char will be lost. Just leave it as it is.
                // (char will be attached to the following glyph).
                GlyphInfo.Remove(FirstGlyph,1);
            }
            else
            {
                ushort firstChar = GlyphInfo.FirstChars[FirstGlyph];
                ushort ligatureCount = GlyphInfo.LigatureCounts[FirstGlyph];

                if (lengthDelta > 0)
                {
                    GlyphInfo.Insert(FirstGlyph,lengthDelta);
                }
                
                //put glyphs in place
                for(ushort gl=0; gl<sequenceLength; gl++)
                {
                    GlyphInfo.Glyphs[FirstGlyph + gl] = sequence.Glyph(Table,gl);
                    GlyphInfo.GlyphFlags[FirstGlyph + gl] =
                                (ushort)(GlyphFlags.Unresolved | GlyphFlags.Substituted);
                    GlyphInfo.FirstChars[FirstGlyph + gl] = firstChar;
                    GlyphInfo.LigatureCounts[FirstGlyph + gl] = ligatureCount;
                }
            }
            
            // Fix char mapping - very simple for now. 
            // Works only for arabic base+mark -> base and marks decomposition
            // Needs work for full mapping
            for(int ch=0;ch<CharCount;ch++)
            {
                if (Charmap[ch]>FirstGlyph) Charmap[ch] = (ushort)(Charmap[ch]+lengthDelta);
            }
            
            NextGlyph = FirstGlyph + lengthDelta + 1;
            
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

        public MultipleSubstitutionSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    struct AlternateSubstitutionSubtable
    {
        private const int offsetFormat              = 0;
        private const int offsetCoverage            = 2;
        private const int offsetAlternateSetCount   = 4;
        private const int offsetAlternateSets       = 6;
        private const int sizeAlternateSetOffset    = 2;

        private const ushort InvalidAlternateGlyph = 0xFFFF;

        public ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }
        
        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }
        
        // Not used. This value should be equal to glyph count in Coverage.
        // Keeping it for future reference
        //private ushort AlternateSetCount(FontTable Table)
        //{
        //    return Table.GetUShort(offset + offsetAlternateSetCount);
        //}
        
        private AlternateSetTable AlternateSet(FontTable Table, int index)
        {
            return new AlternateSetTable(offset + 
                                         Table.GetUShort(offset + 
                                                         offsetAlternateSets +
                                                         index * sizeAlternateSetOffset)
                                        );
        }

        private struct AlternateSetTable
        {
            private const int offsetGlyphCount = 0;
            private const int offsetGlyphs     = 2;
            private const int sizeGlyph        = 2;

            public ushort GlyphCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetGlyphCount);
            }
            
            public ushort Alternate(FontTable Table, uint FeatureParam)
            {
                Invariant.Assert(FeatureParam > 0); // Parameter 0 means feautre is disabled.
                                                //Should be filtered out in GetNextEnabledGlyphRange

                // Off by one - alternate number 1 is stored under index 0
                uint index = FeatureParam - 1;
                
                if (index >= GlyphCount(Table)) 
                {
                    return AlternateSubstitutionSubtable.InvalidAlternateGlyph;
                }
                
                return Table.GetUShort(offset + offsetGlyphs + (ushort)index*sizeGlyph);
            }
            
            public AlternateSetTable(int Offset) { offset = Offset; }
            private int offset;
        }

        public unsafe bool Apply(
            FontTable       Table,
            GlyphInfoList   GlyphInfo,      // List of GlyphInfo
            uint            FeatureParam,   // For this lookup - index of glyph alternate
            int             FirstGlyph,     // where to apply it
            out int         NextGlyph       // Next glyph to process
            )
        {
            NextGlyph = FirstGlyph + 1; // always move one glyph forward, 
                                        // doesn't matter whether we matched context
                                        
            if (Format(Table) != 1) return false; //Unknown format
            
            int oldGlyphCount=GlyphInfo.Length;
            
            int coverageIndex = Coverage(Table).
                                    GetGlyphIndex(Table,GlyphInfo.Glyphs[FirstGlyph]);
            if (coverageIndex==-1) return false;
            
            AlternateSetTable alternateSet = AlternateSet(Table,coverageIndex);
            
            ushort alternateGlyph = alternateSet.Alternate(Table, FeatureParam);

            if (alternateGlyph != InvalidAlternateGlyph)
            {
                GlyphInfo.Glyphs[FirstGlyph] = alternateGlyph;
                GlyphInfo.GlyphFlags[FirstGlyph] = (ushort)(GlyphFlags.Unresolved | GlyphFlags.Substituted);
                return true;
            }

            return false;            
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

        public AlternateSubstitutionSubtable(int Offset) { offset = Offset; }
        private int offset;
    }
}
