// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  Contextual lookups implementation
//              (Contextual, chaining, reverse chaining)
//
//  contact:   sergeym
//
//

using System.Diagnostics;
using System.Security;
using System;

namespace MS.Internal.Shaping
{
    internal struct ContextualLookupRecords
    {
        private const int offsetSequenceIndex = 0;
        private const int offsetLookupIndex = 2;
        private const int sizeLookupRecord = 4;

        
        private ushort SequenceIndex(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + Index*sizeLookupRecord + offsetSequenceIndex);
        }

        private ushort LookupIndex(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + Index*sizeLookupRecord + offsetLookupIndex);
        }


        const int MaximumContextualLookupNestingLevel = 16;

        public unsafe void ApplyContextualLookups(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 nextGlyph       // out: next glyph index
            )
        {
            // Limit nesting level for contextual lookups to 
            // prevent infinite loops from corrupt fonts
            if (nestingLevel >=  MaximumContextualLookupNestingLevel)
            {
                nextGlyph = AfterLastGlyph;
                return;
            }

            LookupList lookupList;
            if (TableTag == OpenTypeTags.GSUB)
            {
                lookupList = (new GSUBHeader(0)).GetLookupList(Table);
            }
            else
            {
                lookupList = (new GPOSHeader(0)).GetLookupList(Table);
            }
        
            int prevLookupIndex   = -1;
            int prevSequenceIndex = -1;
            
            while (true)
            {
                ushort lookupIndex = ushort.MaxValue;
                ushort sequenceIndex = ushort.MaxValue;
                
                for(ushort i = 0; i < recordCount; i++)
                {
                    ushort recordLookupIndex   = LookupIndex(Table,i);
                    ushort recordSequenceIndex = SequenceIndex(Table,i);

                    if (recordLookupIndex < prevLookupIndex ||
                        (recordLookupIndex == prevLookupIndex &&
                         recordSequenceIndex <= prevSequenceIndex
                        )
                       )
                    {
                        // This record we already should have been processed
                        continue;
                    }

                    // Among not proccessed record, find next one
                    if ( recordLookupIndex < lookupIndex ||
                         (recordLookupIndex == lookupIndex && 
                          recordSequenceIndex < sequenceIndex
                         )
                       )
                    {
                        lookupIndex = recordLookupIndex;
                        sequenceIndex = recordSequenceIndex;
                    }
                }

                if (lookupIndex == ushort.MaxValue)
                {
                    // All records processed (or we had duplicate records, which we skipped automatically)
                    break;
                }

                //remember for the next iteration
                prevLookupIndex   = lookupIndex;
                prevSequenceIndex = sequenceIndex;

                // Now find actual glyph where to apply lookup (may depend on lookup flags)
                
                int recordFirstGlyph = FirstGlyph;
                for (int i = 0; i < sequenceIndex && recordFirstGlyph < AfterLastGlyph; i++)
                {
                    recordFirstGlyph = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                                                                         recordFirstGlyph + 1,
                                                                         LookupFlags,
                                                                         LayoutEngine.LookForward
                                                                        );
                }

                if (recordFirstGlyph >= AfterLastGlyph)
                {
                    // Requested position is outside of input sequence, do nothing.
                    continue;
                }
                                
                // And finally apply lookup
                
                int prevLength = GlyphInfo.Length;
                int dummyNextGlyph;
                LayoutEngine.ApplyLookup(
                                            Font,
                                            TableTag,
                                            Table,
                                            Metrics,
                                            lookupList.Lookup(Table, lookupIndex),
                                            CharCount,
                                            Charmap,
                                            GlyphInfo,
                                            Advances,
                                            Offsets,

                                            recordFirstGlyph,
                                            AfterLastGlyph,
                                            Parameter,
                                            nestingLevel + 1,
                                            out dummyNextGlyph // we don't need it here
                                          );
                //We need to adjust afterLastGlyph, in case non-single substitution happened
                AfterLastGlyph += GlyphInfo.Length - prevLength;
            }

            nextGlyph = AfterLastGlyph;
}

        public ContextualLookupRecords(int Offset, ushort RecordCount)
        {
            offset = Offset;
            recordCount = RecordCount;
        }
        
        private int offset;
        private ushort recordCount;
    }

    internal struct GlyphChainingSubtable
    {
        private const int offsetFormat            = 0;
        private const int offsetCoverage          = 2;
        private const int offsetSubRuleSetCount   = 4;
        private const int offsetSubRuleSetArray   = 6;
        private const int sizeRuleSetOffset       = 2;
    
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
        //private ushort SubRuleSetCount(FontTable Table)
        //{
        //    return Table.GetUShort(offset + offsetSubRuleSetCount);
        //}

        private SubRuleSet RuleSet(FontTable Table, int Index)
        {
            return new SubRuleSet(offset + 
                                    Table.GetUShort(offset + 
                                                    offsetSubRuleSetArray + 
                                                    Index * sizeRuleSetOffset));
        }

        #region GlyphChainingSubtable private classes
        private class SubRuleSet
        {
            private const int offsetRuleCount = 0;
            private const int offsetRuleArray = 2;
            private const int sizeRuleOffset  = 2;
      
            public ushort RuleCount(FontTable Table)
            {
                return Table.GetUShort(offset+offsetRuleCount);
            }

            public SubRule Rule(FontTable Table, ushort Index)
            {
                return new SubRule(offset + 
                                   Table.GetUShort(offset + 
                                                   offsetRuleArray + 
                                                   Index*sizeRuleOffset));
            }
            
            public SubRuleSet(int Offset) { offset = Offset; }
            private int offset;
        }

        private class SubRule
        {
            private const int sizeCount     = 2;
            private const int sizeGlyphId   = 2;
            
            public static ushort GlyphCount(FontTable Table, int Offset)
            {
                return Table.GetUShort(Offset);
            }

            public static ushort GlyphId(FontTable Table, int Offset)
            {
                return Table.GetUShort(Offset);
            }

            public ContextualLookupRecords ContextualLookups(FontTable Table, int CurrentOffset)
            {
                return new ContextualLookupRecords(CurrentOffset+sizeCount,
                    Table.GetUShort(CurrentOffset));
            }

            public unsafe bool Apply(
                IOpenTypeFont           Font,           // Font access interface
                OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
                FontTable               Table,          // Layout table (GSUB or GPOS)
                LayoutMetrics           Metrics,        // LayoutMetrics
                int                     CharCount,      // Characters count (i.e. Charmap.Length);
                UshortList              Charmap,        // Char to glyph mapping
                GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
                int*                    Advances,       // Glyph adv.widths
                LayoutOffset*           Offsets,        // Glyph offsets
                ushort                  LookupFlags,    // Lookup table flags
                int                     FirstGlyph,     // where to apply it
                int                     AfterLastGlyph, // how long is a context we can use
                uint                    Parameter,      // lookup parameter
                int                     nestingLevel,    // Contextual lookup nesting level
                out int                 NextGlyph       // out: next glyph index
                )
            {
                bool match = true;
                NextGlyph = FirstGlyph + 1; //In case we don't match
                
                //We are moving through table. We can pick glyph count or glyph class id.
                int curOffset = offset;
                int glyphIndex;

                //
                //Check backtrack sequence
                //
                int backtrackGlyphCount = GlyphCount(Table,curOffset);
                curOffset += sizeCount;
                
                glyphIndex = FirstGlyph;
                for(ushort backtrackIndex = 0; 
                    backtrackIndex < backtrackGlyphCount && match; 
                    backtrackIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex-1,
                        LookupFlags,
                        LayoutEngine.LookBackward
                        );
                                                    
                    if (glyphIndex<0)
                    {
                        match = false;
                    }
                    else
                    {
                        match = ( GlyphId(Table,curOffset) == GlyphInfo.Glyphs[glyphIndex] );
                        curOffset+=sizeGlyphId;
                    }
                }

                if (!match) return false;

                //
                // Check input sequence
                //
                int inputGlyphCount = GlyphCount(Table,curOffset);
                curOffset += sizeCount;

                glyphIndex = FirstGlyph;
                for(ushort inputIndex = 1; //go from second glyph in the input
                    inputIndex < inputGlyphCount && match; 
                    inputIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex+1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                                                    
                    if (glyphIndex >= AfterLastGlyph)
                    {
                        match = false;
                    }
                    else
                    {
                        match = ( GlyphId(Table,curOffset) == GlyphInfo.Glyphs[glyphIndex] );
                        curOffset+=sizeGlyphId;
                    }
                }

                if (!match) return false;

                int afterInputGlyph = glyphIndex + 1; // remember where we were after input seqence
                
                //
                // Check lookahead sequence
                //
                int lookaheadGlyphCount = GlyphCount(Table,curOffset);
                curOffset += sizeCount;

                // Lokahead sequence starting right after input, 
                // no need to change current glyphIndex
                for(ushort lookaheadIndex = 0; 
                    lookaheadIndex < lookaheadGlyphCount && match; 
                    lookaheadIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex+1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                                                    
                    if (glyphIndex >= GlyphInfo.Length)
                    {
                        match = false;
                    }
                    else
                    {
                        match = ( GlyphId(Table,curOffset) == GlyphInfo.Glyphs[glyphIndex] );
                        curOffset+=sizeGlyphId;
                    }
                }

                if (match)
                {
                    ContextualLookups(Table,curOffset).ApplyContextualLookups(
                        Font,
                        TableTag,
                        Table,
                        Metrics,
                        CharCount,
                        Charmap,
                        GlyphInfo,
                        Advances,
                        Offsets,
                        LookupFlags,
                        FirstGlyph,
                        afterInputGlyph, //As AfterLastGlyph
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                    );
                }

                return match;
            }
            
            public SubRule(int Offset) { { offset = Offset; } }
            private int offset;
        }
        
        #endregion //Glyph based chain private classes

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Invariant.Assert(Format(Table)==1);

            NextGlyph = FirstGlyph + 1; //in case we don't match
           
            int glyphCount = GlyphInfo.Length;
            
            int     glyphIndex = FirstGlyph;
            ushort  glyphId = GlyphInfo.Glyphs[glyphIndex];
            
            int coverageIndex = Coverage(Table).GetGlyphIndex(Table,glyphId);
            if (coverageIndex < 0) return false;
            
            SubRuleSet subRuleSet= RuleSet(Table, coverageIndex);

            ushort ruleCount = subRuleSet.RuleCount(Table);
            
            bool match = false;
            for(ushort i=0; !match && i<ruleCount; i++)
            {
                match = subRuleSet.Rule(Table,i).Apply(Font, TableTag, Table, Metrics,
                                                        CharCount, Charmap,
                                                        GlyphInfo, Advances, Offsets,
                                                        LookupFlags, FirstGlyph, AfterLastGlyph,
                                                        Parameter,
                                                        nestingLevel,
                                                        out NextGlyph
                                                        );
            }
            
            return match;
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

        public GlyphChainingSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct ClassChainingSubtable
    {
        private const int offsetFormat            = 0;
        private const int offsetCoverage          = 2;
        private const int offsetBacktrackClassDef = 4;
        private const int offsetInputClassDef     = 6;
        private const int offsetLookaheadClassDef = 8;
        private const int offsetSubClassSetCount  = 10;
        private const int offsetSubClassSetArray  = 12;
        private const int sizeClassSetOffset      = 2;
              
        public ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }

        private ClassDefTable BacktrackClassDef(FontTable Table)
        {
            return new ClassDefTable(offset + 
                                        Table.GetUShort(offset + 
                                                        offsetBacktrackClassDef)
                                    );
        }

        private ClassDefTable InputClassDef(FontTable Table)
        {
            return new ClassDefTable(offset + 
                                        Table.GetUShort(offset + 
                                                        offsetInputClassDef)
                                    );
        }

        private ClassDefTable LookaheadClassDef(FontTable Table)
        {
            return new ClassDefTable(offset + 
                                        Table.GetUShort(offset + 
                                                        offsetLookaheadClassDef)
                                    );
        }

        private ushort ClassSetCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetSubClassSetCount);
        }

        private SubClassSet ClassSet(FontTable Table, ushort Index)
        {
            int ClassSetOffset = Table.GetUShort(offset + offsetSubClassSetArray +
                                                 Index * sizeClassSetOffset);
            if (ClassSetOffset==0)
                return new SubClassSet(FontTable.InvalidOffset);
            else
                return new SubClassSet(offset + ClassSetOffset);
        }

        #region ClassBasedChain private classes
        private class SubClassSet
        {
            private const int offsetRuleCount = 0;
            private const int offsetRuleArray = 2;
            private const int sizeRuleOffset  = 2;

            public ushort RuleCount(FontTable Table)
            {
                return Table.GetUShort(offset+offsetRuleCount);
            }
            
            public SubClassRule Rule(FontTable Table, ushort Index)
            {
                return new SubClassRule(offset + Table.GetUShort(offset + offsetRuleArray + 
                                                                  Index*sizeRuleOffset));
            }
            
            public bool IsNull { get { return (offset==FontTable.InvalidOffset); } }
            public SubClassSet(int Offset) { offset = Offset; }
            private int offset;
        }

        private class SubClassRule
        {
            private const int sizeCount = 2;
            private const int sizeClassId = 2;
 
            public static ushort GlyphCount(FontTable Table, int Offset)
            {
                return Table.GetUShort(Offset);
            }
            
            public static ushort ClassId(FontTable Table, int Offset)
            {
                return Table.GetUShort(Offset);
            }

            public ContextualLookupRecords ContextualLookups(FontTable Table, int CurrentOffset)
            {
                return new ContextualLookupRecords(CurrentOffset + sizeCount,
                    Table.GetUShort(CurrentOffset));
            }

            public unsafe bool Apply(
                IOpenTypeFont           Font,           // Font access interface
                OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
                FontTable               Table,          // Layout table (GSUB or GPOS)
                LayoutMetrics           Metrics,        // LayoutMetrics
                ClassDefTable           inputClassDef, 
                ClassDefTable           backtrackClassDef,
                ClassDefTable           lookaheadClassDef,
                int                     CharCount,      // Characters count (i.e. Charmap.Length);
                UshortList              Charmap,        // Char to glyph mapping
                GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
                int*                    Advances,       // Glyph adv.widths
                LayoutOffset*           Offsets,        // Glyph offsets
                ushort                  LookupFlags,    // Lookup table flags
                int                     FirstGlyph,     // where to apply it
                int                     AfterLastGlyph, // how long is a context we can use
                uint                    Parameter,      // lookup parameter
                int                     nestingLevel,   // Contextual lookup nesting level
                out int                 NextGlyph       // out: next glyph index
                )
            {
                bool match = true;
                NextGlyph = FirstGlyph + 1; //In case we don't match
                
                //We are moving through table. We can pick glyph count or glyph class id.
                int curOffset = offset;
                int glyphIndex;
                
                //
                //Check backtrack sequence
                //
                int backtrackGlyphCount = GlyphCount(Table,curOffset);
                curOffset += sizeCount;
                
                glyphIndex = FirstGlyph;
                for(ushort backtrackIndex = 0; 
                    backtrackIndex < backtrackGlyphCount && match; 
                    backtrackIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex-1,
                        LookupFlags,
                        LayoutEngine.LookBackward
                        );
                                                    
                    if (glyphIndex<0)
                    {
                        match = false;
                    }
                    else
                    {
                        ushort classId = ClassId(Table,curOffset);
                        curOffset+=sizeClassId;
                        
                        ushort glyphClass = backtrackClassDef.
                                        GetClass(Table,GlyphInfo.Glyphs[glyphIndex]);
                        
                        match = (glyphClass == classId);
                    }
                }

                if (!match) return false;

                //
                // Check input sequence
                //
                int inputGlyphCount = GlyphCount(Table,curOffset);
                curOffset += sizeCount;

                glyphIndex = FirstGlyph;
                for(ushort inputIndex = 1; //go from second glyph in the input
                    inputIndex < inputGlyphCount && match; 
                    inputIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex+1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                                                    
                    if (glyphIndex >= AfterLastGlyph)
                    {
                        match = false;
                    }
                    else
                    {
                        ushort classId = ClassId(Table,curOffset);
                        curOffset+=sizeClassId;
                        
                        ushort glyphClass = inputClassDef.
                            GetClass(Table,GlyphInfo.Glyphs[glyphIndex]);
                        
                        match = (glyphClass == classId);
                    }
                }

                if (!match) return false;

                int afterInputGlyph = glyphIndex + 1; // remember where we were after input seqence

                //
                // Check lookahead sequence
                //
                int lookaheadGlyphCount = GlyphCount(Table,curOffset);
                curOffset += sizeCount;

                // Lokahead sequence starting right after input, 
                // no need to change current glyphIndex
                for(ushort lookaheadIndex = 0; 
                    lookaheadIndex < lookaheadGlyphCount && match; 
                    lookaheadIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex+1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                                                    
                    if (glyphIndex >= GlyphInfo.Length)
                    {
                        match = false;
                    }
                    else
                    {
                        ushort classId = ClassId(Table,curOffset);
                        curOffset+=sizeClassId;
                        
                        ushort glyphClass = lookaheadClassDef.
                            GetClass(Table,GlyphInfo.Glyphs[glyphIndex]);
                        
                        match = (glyphClass == classId);
                    }
                }

                if (match)
                {
                    ContextualLookups(Table,curOffset).ApplyContextualLookups(
                        Font,
                        TableTag,
                        Table,
                        Metrics,
                        CharCount,
                        Charmap,
                        GlyphInfo,
                        Advances,
                        Offsets,
                        LookupFlags,
                        FirstGlyph,
                        afterInputGlyph, //As AfterLastGlyph
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                    );
                }

                return match;
            }
            
            public SubClassRule(int Offset) { { offset = Offset; } }
            private int offset;
        }
        
        #endregion //Class based chain private classes
        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Invariant.Assert(Format(Table)==2);
            
            NextGlyph = FirstGlyph + 1; //in case we don't match
           
            int glyphCount = GlyphInfo.Length;
            
            int     glyphIndex = FirstGlyph;
            ushort  glyphId = GlyphInfo.Glyphs[glyphIndex];
            
            if (Coverage(Table).GetGlyphIndex(Table,glyphId) < 0) return false;
            
            ClassDefTable inputClassDef = InputClassDef(Table),
                          backtrackClassDef =  BacktrackClassDef(Table),
                          lookaheadClassDef = LookaheadClassDef(Table);
            
            ushort GlyphClass = inputClassDef.GetClass(Table,glyphId);
            if (GlyphClass >= ClassSetCount(Table)) return false; //!!! Bad font table

            SubClassSet subClassSet = ClassSet(Table,GlyphClass);
            if (subClassSet.IsNull) return false;   // There are no rules for this class
            
            ushort ruleCount = subClassSet.RuleCount(Table);
            
            bool match = false;
            for(ushort i=0; !match && i<ruleCount; i++)
            {
                match = subClassSet.Rule(Table,i).Apply(Font, TableTag, Table, Metrics,
                                                        inputClassDef,backtrackClassDef,lookaheadClassDef,
                                                        CharCount, Charmap,
                                                        GlyphInfo, Advances, Offsets,
                                                        LookupFlags, FirstGlyph, AfterLastGlyph,
                                                        Parameter,
                                                        nestingLevel,
                                                        out NextGlyph
                                                       );
            }
            
            return match;
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

        public ClassChainingSubtable(int Offset) { offset = Offset; }
        private int offset;
}

    internal struct CoverageChainingSubtable //ChainingContext,Format3
    {
        // Future enhancement: Remove offsets from class members. Like ClassChaining does.

        private const int offsetFormat = 0;
        private const int offsetBacktrackGlyphCount = 2;
        private const int offsetBacktrackCoverageArray = 4;
        private const int sizeGlyphCount = 2;
        private const int sizeCoverageOffset = 2;
                               
        public ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        public ushort BacktrackGlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset+offsetBacktrackGlyphCount);
        }

        public CoverageTable BacktrackCoverage(FontTable Table, ushort Index)
        {
            return new CoverageTable(offset + 
                                        Table.GetUShort(offset+
                                                        offsetBacktrackGlyphCount +
                                                        sizeGlyphCount + 
                                                        Index*sizeCoverageOffset)
                                    );
        }

        public ushort InputGlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset+offsetInputGlyphCount);
        }

        public CoverageTable InputCoverage(FontTable Table, ushort Index)
        {
            return new CoverageTable(offset + 
                                        Table.GetUShort(offset+
                                                        offsetInputGlyphCount +
                                                        sizeGlyphCount + 
                                                        Index*sizeCoverageOffset)
                                    );
        }

        public ushort LookaheadGlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset+offsetLookaheadGlyphCount);
        }

        public CoverageTable LookaheadCoverage(FontTable Table, ushort Index)
        {
            return new CoverageTable(offset + 
                                        Table.GetUShort(offset+
                                                        offsetLookaheadGlyphCount +
                                                        sizeGlyphCount + 
                                                        Index*sizeCoverageOffset)
                                    );
        }

        public ContextualLookupRecords ContextualLookups(FontTable Table)
        {
            int recordCountOffset = offset + offsetLookaheadGlyphCount + sizeGlyphCount +
                                    LookaheadGlyphCount(Table) * sizeCoverageOffset;
            return new ContextualLookupRecords(recordCountOffset+sizeGlyphCount,
                                                Table.GetUShort(recordCountOffset));
        }

        public CoverageChainingSubtable(FontTable Table, int Offset) 
        {
            offset = Offset;
            offsetInputGlyphCount = offsetBacktrackGlyphCount + sizeGlyphCount +
                                      Table.GetUShort(offset+offsetBacktrackGlyphCount) *
                                                                    sizeCoverageOffset;
                                                                      
            offsetLookaheadGlyphCount = offsetInputGlyphCount + sizeGlyphCount + 
                                          Table.GetUShort(offset+offsetInputGlyphCount) *
                                                                   sizeCoverageOffset;
        }

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Invariant.Assert(Format(Table)==3);
            
            NextGlyph = FirstGlyph + 1; //in case we don't match
            
            int glyphCount = GlyphInfo.Length;
            int glyphIndex;
            
            ushort backtrackGlyphCount = BacktrackGlyphCount(Table);
            ushort inputGlyphCount     = InputGlyphCount(Table);
            ushort lookaheadGlyphCount = LookaheadGlyphCount(Table);
            
            if (FirstGlyph < backtrackGlyphCount || 
                (FirstGlyph + inputGlyphCount) > AfterLastGlyph)
            {
                return false;
            }
            
            bool match = true;
            
            //Check backtrack sequence
            glyphIndex = FirstGlyph;
            for(ushort backtrackIndex = 0; 
                backtrackIndex < backtrackGlyphCount && match; 
                backtrackIndex++)
            {
                 glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                                                                glyphIndex-1,
                                                                LookupFlags,
                                                                LayoutEngine.LookBackward
                                                               );
                                                               
                if (glyphIndex<0 ||
                    BacktrackCoverage(Table,backtrackIndex)
                            .GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex])<0) 
                {
                    match=false;
                }
            }

            if (!match) return false;
            
            glyphIndex = FirstGlyph;
            for(ushort inputIndex = 0; 
                inputIndex < inputGlyphCount && match; 
                inputIndex++)
            {
                if (glyphIndex>=AfterLastGlyph ||
                    InputCoverage(Table,inputIndex)
                    .GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex])<0) 
                {
                    match=false;
                }
                else
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex + 1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                }
            }
            
            if (!match) return false;
            
            int afterInputGlyph = glyphIndex; // remember where we were after input seqence

            for(ushort lookaheadIndex = 0; 
                lookaheadIndex < lookaheadGlyphCount && match; 
                lookaheadIndex++)
            {
                if (glyphIndex>=GlyphInfo.Length ||
                    LookaheadCoverage(Table,lookaheadIndex)
                    .GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex])<0) 
                {
                    match=false;
                }
                else
                {
                    glyphIndex = LayoutEngine.
                                    GetNextGlyphInLookup(Font, GlyphInfo, 
                                                         glyphIndex + 1,
                                                         LookupFlags,
                                                         LayoutEngine.LookForward);
                }
            }

            if (match)
            {
                ContextualLookups(Table).ApplyContextualLookups(
                                                Font,
                                                TableTag,
                                                Table,
                                                Metrics,
                                                CharCount,
                                                Charmap,
                                                GlyphInfo,
                                                Advances,
                                                Offsets,
                                                LookupFlags,
                                                FirstGlyph,
                                                afterInputGlyph, //As AfterLastGlyph
                                                Parameter,
                                                nestingLevel,
                                                out NextGlyph
                                            );
            }
            
            return match;
        }

        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            ushort backtrackGlyphCount = BacktrackGlyphCount(table);
            ushort inputGlyphCount = InputGlyphCount(table);
            ushort lookaheadGlyphCount = LookaheadGlyphCount(table);

            for (ushort backtrackIndex = 0;
                 backtrackIndex < backtrackGlyphCount;
                 backtrackIndex++)
            {
                if (!BacktrackCoverage(table, backtrackIndex)
                                    .IsAnyGlyphCovered(table, glyphBits, minGlyphId, maxGlyphId)
                   )
                {
                    return false;
                }
            }

            for (ushort inputIndex = 0;
                 inputIndex < inputGlyphCount;
                 inputIndex++)
            {
                if (!InputCoverage(table, inputIndex)
                                    .IsAnyGlyphCovered(table, glyphBits, minGlyphId, maxGlyphId)
                   )
                {
                    return false;
                }
            }

            for (ushort lookaheadIndex = 0;
                 lookaheadIndex < lookaheadGlyphCount;
                 lookaheadIndex++)
            {
                if (!LookaheadCoverage(table, lookaheadIndex)
                                    .IsAnyGlyphCovered(table, glyphBits, minGlyphId, maxGlyphId)
                   )
                {
                    return false;
                }
            }

            return true;
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            if (InputGlyphCount(table) > 0)
            {
                return InputCoverage(table, 0);
            }
            else
            {
                return CoverageTable.InvalidCoverage;
            }
        }

        private int offset;
        private int offsetInputGlyphCount;
        private int offsetLookaheadGlyphCount;
    }
    

    internal struct ChainingSubtable
    {
        private const int offsetFormat = 0;
                     
        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset+offsetFormat);
        }

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            NextGlyph = FirstGlyph+1; //In case we don't match

            switch (Format(Table))
            {
                case 1:
                    GlyphChainingSubtable glyphChainingSubtable = 
                        new GlyphChainingSubtable(offset);
                    return glyphChainingSubtable.Apply(
                        Font, TableTag, Table, Metrics,
                        CharCount, Charmap,
                        GlyphInfo, Advances, Offsets,
                        LookupFlags, FirstGlyph, AfterLastGlyph, 
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                        );
                case 2:
                    ClassChainingSubtable classChainingSubtable = 
                        new ClassChainingSubtable(offset);
                    return classChainingSubtable.Apply(
                        Font, TableTag, Table, Metrics,
                        CharCount, Charmap,
                        GlyphInfo, Advances, Offsets,
                        LookupFlags, FirstGlyph, AfterLastGlyph, 
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                        );
                case 3:
                    CoverageChainingSubtable coverageChainingSubtable = 
                                    new CoverageChainingSubtable(Table, offset);
                    return coverageChainingSubtable.Apply(
                                            Font, TableTag, Table, Metrics,
                                            CharCount, Charmap,
                                            GlyphInfo, Advances, Offsets,
                                            LookupFlags, FirstGlyph, AfterLastGlyph, 
                                            Parameter,
                                            nestingLevel,
                                            out NextGlyph
                                         );
                default:
                    //Unknown format
                    return false;
            }
        }        

        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            switch (Format(table))
            {
                case 1: 
                    GlyphChainingSubtable glyphChainingSubtable = 
                                                new GlyphChainingSubtable(offset);
                    return glyphChainingSubtable.IsLookupCovered(table, glyphBits, minGlyphId, maxGlyphId);

                case 2: 

                    ClassChainingSubtable classChainingSubtable = 
                                                new ClassChainingSubtable(offset);
                    return classChainingSubtable.IsLookupCovered(table, glyphBits, minGlyphId, maxGlyphId);
                
                case 3:
                
                    CoverageChainingSubtable coverageChainingSubtable = 
                                    new CoverageChainingSubtable(table, offset);

                    return coverageChainingSubtable.IsLookupCovered(table, glyphBits, minGlyphId, maxGlyphId);
                    
                
                default:
                    
                    return true;    
            }
}

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            switch (Format(table))
            {
                case 1:
                    GlyphChainingSubtable glyphChainingSubtable =
                                                new GlyphChainingSubtable(offset);
                    return glyphChainingSubtable.GetPrimaryCoverage(table);

                case 2:

                    ClassChainingSubtable classChainingSubtable =
                                                new ClassChainingSubtable(offset);
                    return classChainingSubtable.GetPrimaryCoverage(table);

                case 3:

                    CoverageChainingSubtable coverageChainingSubtable =
                                    new CoverageChainingSubtable(table, offset);
                    return coverageChainingSubtable.GetPrimaryCoverage(table);


                default:
                    return CoverageTable.InvalidCoverage;
            }
        }

        public ChainingSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct GlyphContextSubtable
    {
        private const int offsetFormat            = 0;
        private const int offsetCoverage          = 2;
        private const int offsetSubRuleSetCount   = 4;
        private const int offsetSubRuleSetArray   = 6;
        private const int sizeRuleSetOffset       = 2;
                   
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
        //private ushort SubRuleSetCount(FontTable Table)
        //{
        //    return Table.GetUShort(offset + offsetSubRuleSetCount);
        //}
        private SubRuleSet RuleSet(FontTable Table, int Index)
        {
            return new SubRuleSet(offset + 
                Table.GetUShort(offset + 
                offsetSubRuleSetArray + 
                Index * sizeRuleSetOffset));
        }

        #region GlyphContextSubtable private classes
        private class SubRuleSet
        {
            private const int offsetRuleCount = 0;
            private const int offsetRuleArray = 2;
            private const int sizeRuleOffset  = 2;

            public ushort RuleCount(FontTable Table)
            {
                return Table.GetUShort(offset+offsetRuleCount);
            }

            public SubRule Rule(FontTable Table, ushort Index)
            {
                return new SubRule(offset + 
                    Table.GetUShort(offset + 
                    offsetRuleArray + 
                    Index*sizeRuleOffset));
            }
            
            public SubRuleSet(int Offset) { offset = Offset; }
            private int offset;
        }

        private class SubRule
        {
            private const int offsetGlyphCount = 0;
            private const int offsetSubstCount = 2;
            private const int offsetInput = 4;
            private const int sizeCount     = 2;
            private const int sizeGlyphId   = 2;
                  
            public ushort GlyphCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetGlyphCount);
            }

            public ushort SubstCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetSubstCount);
            }

            public ushort GlyphId(FontTable Table, int Index)
            {
                return Table.GetUShort(offset + offsetInput + (Index - 1) * sizeGlyphId);
            }

            public ContextualLookupRecords ContextualLookups(FontTable Table)
            {
                return new ContextualLookupRecords(offset + offsetInput +
                                                                 (GlyphCount(Table) - 1) * sizeGlyphId,
                                                   SubstCount(Table));
            }

            public unsafe bool Apply(
                IOpenTypeFont           Font,           // Font access interface
                OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
                FontTable               Table,          // Layout table (GSUB or GPOS)
                LayoutMetrics           Metrics,        // LayoutMetrics
                int                     CharCount,      // Characters count (i.e. Charmap.Length);
                UshortList              Charmap,        // Char to glyph mapping
                GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
                int*                    Advances,       // Glyph adv.widths
                LayoutOffset*           Offsets,        // Glyph offsets
                ushort                  LookupFlags,    // Lookup table flags
                int                     FirstGlyph,     // where to apply it
                int                     AfterLastGlyph, // how long is a context we can use
                uint                    Parameter,      // lookup parameter
                int                     nestingLevel,   // Contextual lookup nesting level
                out int                 NextGlyph       // out: next glyph index
                )
            {
                bool match = true;
                NextGlyph = FirstGlyph + 1; //In case we don't match
                
                //
                //Check backtrack sequence
                //
                int inputGlyphCount = GlyphCount(Table);
                int glyphIndex = FirstGlyph;
                for(ushort inputIndex = 1; //go from second glyph in the input
                    inputIndex < inputGlyphCount && match; 
                    inputIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex+1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                                                    
                    if (glyphIndex >= AfterLastGlyph)
                    {
                        match = false;
                    }
                    else
                    {
                        match = ( GlyphId(Table,inputIndex) == GlyphInfo.Glyphs[glyphIndex] );
                    }
                }

                if (match)
                {
                    ContextualLookups(Table).ApplyContextualLookups(
                        Font,
                        TableTag,
                        Table,
                        Metrics,
                        CharCount,
                        Charmap,
                        GlyphInfo,
                        Advances,
                        Offsets,
                        LookupFlags,
                        FirstGlyph,
                        glyphIndex + 1, //As AfterLastGlyph
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                    );
                }

                return match;
            }
            
            public SubRule(int Offset) { { offset = Offset; } }
            private int offset;
        }
        
        #endregion //Glyph based context private classes

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Invariant.Assert(Format(Table)==1);

            NextGlyph = FirstGlyph + 1; //in case we don't match
           
            int glyphCount = GlyphInfo.Length;
            
            int     glyphIndex = FirstGlyph;
            ushort  glyphId = GlyphInfo.Glyphs[glyphIndex];
            
            int coverageIndex = Coverage(Table).GetGlyphIndex(Table,glyphId);
            if (coverageIndex < 0) return false;
            
            SubRuleSet subRuleSet= RuleSet(Table, coverageIndex);

            ushort ruleCount = subRuleSet.RuleCount(Table);
            
            bool match = false;
            for(ushort i=0; !match && i<ruleCount; i++)
            {
                match = subRuleSet.Rule(Table,i).Apply(Font, TableTag, Table, Metrics,
                    CharCount, Charmap,
                    GlyphInfo, Advances, Offsets,
                    LookupFlags, FirstGlyph, AfterLastGlyph,
                    Parameter,
                    nestingLevel,
                    out NextGlyph
                    );
            }
            
            return match;
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

        public GlyphContextSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct ClassContextSubtable //Context, Format2
    {
        private const int offsetFormat            = 0;
        private const int offsetCoverage          = 2;
        private const int offsetClassDef          = 4;
        private const int offsetSubClassSetCount  = 6;
        private const int offsetSubClassSetArray  = 8;
        private const int sizeClassSetOffset      = 2;

        public ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable Coverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }

        private ClassDefTable ClassDef(FontTable Table)
        {
            return new ClassDefTable(offset + Table.GetUShort(offset + offsetClassDef));
        }

        private ushort ClassSetCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetSubClassSetCount);
        }

        private SubClassSet ClassSet(FontTable Table, ushort Index)
        {
            int ClassSetOffset = Table.GetUShort(offset + offsetSubClassSetArray +
                Index * sizeClassSetOffset);
            if (ClassSetOffset==0)
                return new SubClassSet(FontTable.InvalidOffset);
            else
                return new SubClassSet(offset + ClassSetOffset);
        }

        #region ClassBasedContext private classes
        
        private class SubClassSet
        {
            private const int offsetRuleCount = 0;
            private const int offsetRuleArray = 2;
            private const int sizeRuleOffset  = 2;

            public ushort RuleCount(FontTable Table)
            {
                return Table.GetUShort(offset+offsetRuleCount);
            }

            public SubClassRule Rule(FontTable Table, ushort Index)
            {
                return new SubClassRule(offset + Table.GetUShort(offset + offsetRuleArray + 
                    Index*sizeRuleOffset));
            }
            
            public bool IsNull { get { return (offset==FontTable.InvalidOffset); } }
            public SubClassSet(int Offset) { offset = Offset; }
            private int offset;
        }

        private class SubClassRule
        {
            private const int offsetGlyphCount = 0;
            private const int offsetSubstCount = 2;
            private const int offsetInputSequence = 4;
            private const int sizeCount = 2;
            private const int sizeClassId = 2;
             
            public ushort GlyphCount(FontTable Table)
            {
                return Table.GetUShort(offset+offsetGlyphCount);
            }

            public ushort ClassId(FontTable Table, int Index)
            {
                //we count input class from 1; First is covered in higher level
                return Table.GetUShort(offset + offsetInputSequence + (Index - 1)*sizeClassId);
            }

            public ushort SubstCount(FontTable Table)
            {
                return Table.GetUShort(offset + offsetSubstCount);
            }

            public ContextualLookupRecords ContextualLookups(FontTable Table)
            {
                return new ContextualLookupRecords(offset + offsetInputSequence + 
                                                      (GlyphCount(Table)-1)*sizeClassId,
                                                   SubstCount(Table));
            }

            public unsafe bool Apply(
                IOpenTypeFont           Font,           // Font access interface
                OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
                FontTable               Table,          // Layout table (GSUB or GPOS)
                LayoutMetrics           Metrics,        // LayoutMetrics
                ClassDefTable           ClassDef, 
                int                     CharCount,      // Characters count (i.e. Charmap.Length);
                UshortList              Charmap,        // Char to glyph mapping
                GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
                int*                    Advances,       // Glyph adv.widths
                LayoutOffset*           Offsets,        // Glyph offsets
                ushort                  LookupFlags,    // Lookup table flags
                int                     FirstGlyph,     // where to apply it
                int                     AfterLastGlyph, // how long is a context we can use
                uint                    Parameter,      // lookup parameter
                int                     nestingLevel,   // Contextual lookup nesting level
                out int                 NextGlyph       // out: next glyph index
                )
            {
                NextGlyph = FirstGlyph + 1; //In case we don't match
                
                //
                // Check input sequence
                //
                bool match = true;
                int glyphIndex = FirstGlyph;

                int inputGlyphCount = GlyphCount(Table);
                for(ushort inputIndex = 1; // go from second glyph in the input
                    inputIndex < inputGlyphCount && match; 
                    inputIndex++)
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo,
                        glyphIndex+1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                                                    
                    if (glyphIndex>=AfterLastGlyph)
                    {
                        match = false;
                    }
                    else
                    {
                        ushort classId = ClassId(Table,inputIndex);
                        
                        ushort glyphClass = 
                                  ClassDef.GetClass(Table,GlyphInfo.Glyphs[glyphIndex]);
                        
                        match = (glyphClass == classId);
                    }
                }

                if (match)
                {
                    ContextualLookups(Table).ApplyContextualLookups(
                        Font,
                        TableTag,
                        Table,
                        Metrics,
                        CharCount,
                        Charmap,
                        GlyphInfo,
                        Advances,
                        Offsets,
                        LookupFlags,
                        FirstGlyph,
                        glyphIndex + 1, //As AfterLastGlyph
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                    );
                }

                return match;
            }
            
            public SubClassRule(int Offset) { { offset = Offset; } }
            private int offset;
        }
        
        #endregion //Class based context private classes
        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Invariant.Assert(Format(Table)==2);

            NextGlyph = FirstGlyph + 1; //in case we don't match
           
            int glyphCount = GlyphInfo.Length;
            
            int     glyphIndex = FirstGlyph;
            ushort  glyphId = GlyphInfo.Glyphs[glyphIndex];
            
            if (Coverage(Table).GetGlyphIndex(Table,glyphId) < 0) return false;
            
            ClassDefTable classDef = ClassDef(Table);
            ushort glyphClass = classDef.GetClass(Table,glyphId);
            if (glyphClass >= ClassSetCount(Table)) return false; //!!! Bad font table

            SubClassSet subClassSet = ClassSet(Table,glyphClass);
            if (subClassSet.IsNull) return false;   // There are no rules for this class
            
            ushort ruleCount = subClassSet.RuleCount(Table);
            
            bool match = false;
            for(ushort i=0; !match && i<ruleCount; i++)
            {
                match = subClassSet.Rule(Table,i).Apply(Font, TableTag, Table, Metrics,
                    classDef,
                    CharCount, Charmap,
                    GlyphInfo, Advances, Offsets,
                    LookupFlags, FirstGlyph, AfterLastGlyph,
                    Parameter,
                    nestingLevel,
                    out NextGlyph
                    );
            }
            
            return match;
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

        public ClassContextSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct CoverageContextSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetGlyphCount = 2;
        private const int offsetSubstCount = 4;
        private const int offsetInputCoverage = 6;
        private const int sizeOffset = 2;

        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private ushort GlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetGlyphCount);
        }

        private ushort SubstCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetSubstCount);
        }

        private CoverageTable InputCoverage(FontTable Table, ushort index)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetInputCoverage + index * sizeOffset));
        }

        public ContextualLookupRecords ContextualLookups(FontTable Table)
        {
            return new ContextualLookupRecords(
                offset + offsetInputCoverage + GlyphCount(Table)*sizeOffset,
                SubstCount(Table));
        }

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Invariant.Assert(Format(Table)==3);
        
            NextGlyph = FirstGlyph + 1; //in case we don't match
            
            bool match = true;
            
            int inputGlyphCount = GlyphCount(Table);

            int glyphIndex = FirstGlyph;
            for(ushort inputIndex = 0; 
                inputIndex < inputGlyphCount && match; 
                inputIndex++)
            {
                if (glyphIndex>=AfterLastGlyph ||
                    InputCoverage(Table,inputIndex)
                    .GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex])<0) 
                {
                    match=false;
                }
                else
                {
                    glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                        glyphIndex + 1,
                        LookupFlags,
                        LayoutEngine.LookForward
                        );
                }
            }
            
            if (match)
            {
                ContextualLookups(Table).ApplyContextualLookups(
                    Font,
                    TableTag,
                    Table,
                    Metrics,
                    CharCount,
                    Charmap,
                    GlyphInfo,
                    Advances,
                    Offsets,
                    LookupFlags,
                    FirstGlyph,
                    glyphIndex, //As AfterLastGlyph
                    Parameter,
                    nestingLevel,
                    out NextGlyph
                );
            }
            
            return match;
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
            if (GlyphCount(table) > 0)
            {
                return InputCoverage(table, 0);
            }
            else
            {
                return CoverageTable.InvalidCoverage;
            }
        }
        
        public CoverageContextSubtable(int Offset) { offset = Offset; }
        private int offset;
    }
    

    internal struct ContextSubtable
    {
        private const int offsetFormat = 0;

        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset+offsetFormat);
        }

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            switch (Format(Table))
            {
                case 1:
                    GlyphContextSubtable glyphContextSubtable = 
                                                new GlyphContextSubtable(offset);
                    return glyphContextSubtable.Apply(
                        Font, TableTag, Table, Metrics,
                        CharCount, Charmap,
                        GlyphInfo, Advances, Offsets,
                        LookupFlags, FirstGlyph, AfterLastGlyph, 
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                        );
                case 2:
                    ClassContextSubtable classContextSubtable = 
                                                new ClassContextSubtable(offset);
                    return classContextSubtable.Apply(
                        Font, TableTag, Table, Metrics,
                        CharCount, Charmap,
                        GlyphInfo, Advances, Offsets,
                        LookupFlags, FirstGlyph, AfterLastGlyph, 
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                        );
                case 3:
                    CoverageContextSubtable coverageContextSubtable = 
                                                new CoverageContextSubtable(offset);
                    return coverageContextSubtable.Apply(
                        Font, TableTag, Table, Metrics,
                        CharCount, Charmap,
                        GlyphInfo, Advances, Offsets,
                        LookupFlags, FirstGlyph, AfterLastGlyph, 
                        Parameter,
                        nestingLevel,
                        out NextGlyph
                        );
                default:
                    //Unknown format
                    NextGlyph = FirstGlyph+1; //don't match
                    return false;
            }
        }        

        public bool IsLookupCovered(
                        FontTable table, 
                        uint[] glyphBits, 
                        ushort minGlyphId, 
                        ushort maxGlyphId)
        {
            switch (Format(table))
            {
                case 1:
                    GlyphContextSubtable glyphContextSubtable = 
                                                new GlyphContextSubtable(offset);
                    return glyphContextSubtable.IsLookupCovered(table, glyphBits, minGlyphId, maxGlyphId);
                case 2:
                    ClassContextSubtable classContextSubtable = 
                                                new ClassContextSubtable(offset);
                    return classContextSubtable.IsLookupCovered(table, glyphBits, minGlyphId, maxGlyphId);
                case 3:
                    CoverageContextSubtable coverageContextSubtable = 
                                                new CoverageContextSubtable(offset);
                    return coverageContextSubtable.IsLookupCovered(table, glyphBits, minGlyphId, maxGlyphId);
                default:
                    return true;
            }
        }

        public CoverageTable GetPrimaryCoverage(FontTable table)
        {
            switch (Format(table))
            {
                case 1:
                    GlyphContextSubtable glyphContextSubtable =
                                                new GlyphContextSubtable(offset);
                    return glyphContextSubtable.GetPrimaryCoverage(table);
                case 2:
                    ClassContextSubtable classContextSubtable =
                                                new ClassContextSubtable(offset);
                    return classContextSubtable.GetPrimaryCoverage(table);
                case 3:
                    CoverageContextSubtable coverageContextSubtable =
                                                new CoverageContextSubtable(offset);
                    return coverageContextSubtable.GetPrimaryCoverage(table);
                    
                default:
                    return CoverageTable.InvalidCoverage;
            }
        }
        
        public ContextSubtable(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct ReverseChainingSubtable
    {
        private const int offsetFormat = 0;
        private const int offsetCoverage = 2;
        private const int offsetBacktrackGlyphCount = 4;
        private const int sizeCount = 2;
        private const int sizeOffset = 2;
        private const int sizeGlyphId = 2;

        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private CoverageTable InputCoverage(FontTable Table)
        {
            return new CoverageTable(offset + Table.GetUShort(offset + offsetCoverage));
        }
        
        private CoverageTable Coverage(FontTable Table, int Offset)
        {
            return new CoverageTable(offset + Table.GetUShort(Offset));
        }

        private ushort GlyphCount(FontTable Table, int Offset)
        {
            return Table.GetUShort(Offset);
        }

        private static ushort Glyph(FontTable Table, int Offset)
        {
            return Table.GetUShort(Offset);
        }

        public unsafe bool Apply(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            ushort                  LookupFlags,    // Lookup table flags
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            out int                 NextGlyph       // out: next glyph index
            )
        {
            //This will be the next glyph, does not matter sequence matched or not
            NextGlyph = AfterLastGlyph-1;

            if (Format(Table)!=1) return false; //Unknown
            
            bool match = true;
            int inputGlyphIndex = AfterLastGlyph - 1;
            int glyphIndex;
            
            //Check input glyph
            int coverageIndex = InputCoverage(Table).GetGlyphIndex(Table,GlyphInfo.Glyphs[inputGlyphIndex]);
            
            if (coverageIndex<0) return false;

            //we reading data sequenctially from table, moving pointer through it
            int curOffset = offset + offsetBacktrackGlyphCount;

            //
            // Check backtrack sequence
            //
            ushort backtrackGlyphCount = GlyphCount(Table,curOffset);
            curOffset += sizeCount;
            
            glyphIndex = inputGlyphIndex;
            for(ushort backtrackIndex = 0;
                backtrackIndex < backtrackGlyphCount && match; 
                backtrackIndex++)
            {
                glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                    glyphIndex-1,
                    LookupFlags,
                    LayoutEngine.LookBackward
                    );
                                                    
                if (glyphIndex<0)
                {
                    match = false;
                }
                else
                {
                    match = (Coverage(Table,curOffset).GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex]) >= 0);
                    curOffset += sizeOffset;
                }
            }
            
            ushort lookaheadGlyphCount = GlyphCount(Table,curOffset);
            curOffset += sizeCount;

            glyphIndex = inputGlyphIndex;
            for(ushort lookaheadIndex = 0;
                lookaheadIndex < lookaheadGlyphCount && match; 
                lookaheadIndex++)
            {
                glyphIndex = LayoutEngine.GetNextGlyphInLookup(Font, GlyphInfo, 
                    glyphIndex+1,
                    LookupFlags,
                    LayoutEngine.LookForward
                    );
                                                    
                if (glyphIndex>=GlyphInfo.Length)
                {
                    match = false;
                }
                else
                {
                    match = (Coverage(Table,curOffset).GetGlyphIndex(Table,GlyphInfo.Glyphs[glyphIndex]) >= 0);
                    curOffset += sizeOffset;
                }
            }
          
            if (match)
            {
                curOffset += sizeCount + sizeGlyphId*coverageIndex;
                GlyphInfo.Glyphs[inputGlyphIndex] = Glyph(Table,curOffset);
                GlyphInfo.GlyphFlags[inputGlyphIndex] = (ushort)(GlyphFlags.Unresolved | GlyphFlags.Substituted);
            }
            
            return match;
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
            return InputCoverage(table);
        }

        public ReverseChainingSubtable(int Offset) { offset = Offset; }
        private int offset;
    }
}
