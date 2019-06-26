// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  OpentTypeLayout higher level internal engine and structures
//
//  contact:   sergeym
//
//

using System.Diagnostics;
using System.Security;
using System;
using System.IO;
using MS.Internal;

namespace MS.Internal.Shaping
{
    /// <summary>
    /// Provide core layout functionality
    /// </summary>
    internal static unsafe class LayoutEngine
    {
        /// <summary>
        /// Main layout loop. Execute lookups in right order for enabled ranges
        /// </summary>
        /// <param name="Font">In: Font access interface</param>
        /// <param name="workspace">In: Layout engine wokrspace</param>
        /// <param name="TableTag">In: Layout table tag (GSUB or GPOS)</param>
        /// <param name="Table">In: Layout Table (GSUB or GPOS)</param>
        /// <param name="Metrics">In: Layout metrics</param>
        /// <param name="LangSys">In: Language system table</param>
        /// <param name="Features">In: FeatureList</param>
        /// <param name="Lookups">In: Lookup list</param>
        /// <param name="FeatureSet">In: List of feature to apply</param>
        /// <param name="featureCount">In: Actual number of features in <paramref name="FeatureSet"/></param>
        /// <param name="featureSetOffset">In: offset of input characters inside FeatureSet</param>
        /// <param name="CharCount">In: Characters count (i.e. Charmap.Length)</param>
        /// <param name="Charmap">InOut: Character to glyph method</param>
        /// <param name="GlyphInfo">InOut: List of Glyph Information structures</param>
        /// <param name="Advances">InOut: Glyph advances (used only for positioning)</param>
        /// <param name="Offsets">InOut: Glyph offsets (used only for positioning)</param>
        public static void ApplyFeatures(
            IOpenTypeFont           Font,
            OpenTypeLayoutWorkspace workspace,
            OpenTypeTags            TableTag,
            FontTable               Table,
            LayoutMetrics           Metrics,
            LangSysTable            LangSys,
            FeatureList             Features,
            LookupList              Lookups,
            Feature[]               FeatureSet,
            int                     featureCount,
            int                     featureSetOffset,
            int                     CharCount,
            UshortList              Charmap,
            GlyphInfoList           GlyphInfo,
            int*                    Advances,
            LayoutOffset*           Offsets
            )
        {
            UpdateGlyphFlags(Font, GlyphInfo, 0, GlyphInfo.Length, false, GlyphFlags.NotChanged);

            // if client did not supply us with workspace
            // we will create our own (temporarily)
            if (workspace == null)
            {
                workspace = new OpenTypeLayoutWorkspace();
            }

            ushort lookupCount=Lookups.LookupCount(Table);

            //Compile feature set
            CompileFeatureSet(
                                FeatureSet,
                                featureCount,
                                featureSetOffset,
                                CharCount,
                                Table,
                                LangSys,
                                Features,
                                lookupCount,
                                workspace
                             );

            OpenTypeLayoutCache.InitCache(Font, TableTag, GlyphInfo, workspace);

            for(ushort lookupIndex = 0; lookupIndex < lookupCount; lookupIndex++)
            {
                if (!workspace.IsAggregatedFlagSet(lookupIndex))
                {
                    continue;
                }

                int  firstChar=0,
                     afterLastChar=0,
                     firstGlyph=0,
                     afterLastGlyph=0;

                OpenTypeLayoutCache.FindNextLookup(workspace, 
                                                   GlyphInfo, 
                                                   lookupIndex, 
                                                   out lookupIndex, 
                                                   out firstGlyph);

                // We need to check this again, because FindNextLookup will change lookupIndex
                if (lookupIndex >= lookupCount)
                {
                    break;
                }

                if (!workspace.IsAggregatedFlagSet(lookupIndex))
                {
                    continue;
                }

                LookupTable lookup = Lookups.Lookup(Table, lookupIndex);

                uint parameter=0;
                bool isLookupReversal = IsLookupReversal(TableTag, lookup.LookupType());

                while(firstGlyph < GlyphInfo.Length) // While we have ranges to work on
                {
                    if (!OpenTypeLayoutCache.FindNextGlyphInLookup(workspace, lookupIndex, isLookupReversal, ref firstGlyph, ref afterLastGlyph))
                    {
                        firstGlyph = afterLastGlyph;
                    }

                    if (firstGlyph < afterLastGlyph) // Apply lookup while in one range
                    {
                        int nextGlyph;
                        int oldLength = GlyphInfo.Length;
                        int glyphsAfterLastChar = oldLength - afterLastGlyph;

                        bool match = ApplyLookup(
                                            Font,           // In: Font access interface
                                            TableTag,       // Layout table tag (GSUB or GPOS)
                                            Table,          // Layout table (GSUB or GPOS)
                                            Metrics,        // In: LayoutMetrics
                                            lookup,         // Lookup definition structure
                                            CharCount,
                                            Charmap,        // In: Char to glyph mapping
                                            GlyphInfo,      // In/out: List of GlyphInfo structs
                                            Advances,       // In/out: Glyph adv.widths
                                            Offsets,        // In/out: Glyph offsets

                                            firstGlyph,     // where to apply it
                                            afterLastGlyph, // how long is a context we can use
                                            parameter,      // lookup parameter
                                            0,              // Nesting level for contextual lookups
                                            out nextGlyph   // out: next glyph index
                                                            // !!!: for reversal lookup, should
                                                            //      return new afterLastGlyph
                                            );

                        if (match)
                        {
                            //Adjust range end if length changed,
                            // for reversal changes happens beyond afterLast, no change needed
                            if (!isLookupReversal)
                            {
                                OpenTypeLayoutCache.OnGlyphsChanged(workspace, GlyphInfo, oldLength, firstGlyph, nextGlyph);

                                afterLastGlyph = GlyphInfo.Length - glyphsAfterLastChar;
                                firstGlyph = nextGlyph;
                            }
                            else
                            {
                                OpenTypeLayoutCache.OnGlyphsChanged(workspace, GlyphInfo, oldLength, nextGlyph, afterLastGlyph);

                                afterLastGlyph = nextGlyph;
                            }
                        }
                        else
                        {
                            if (isLookupReversal)
                                afterLastGlyph = nextGlyph;
                            else
                                firstGlyph = nextGlyph;
                        }
                    }
                    else // End of range. Get next
                    {
                        GetNextEnabledGlyphRange(
                            FeatureSet,
                            featureCount,
                            featureSetOffset,
                            Table,
                            workspace,
                            LangSys,
                            Features,
                            lookupIndex,
                            CharCount,
                            Charmap,

                            afterLastChar,
                            afterLastGlyph,
                            GlyphInfo.Length,

                            out firstChar,
                            out afterLastChar,
                            out firstGlyph,
                            out afterLastGlyph,
                            out parameter);
                    }
                }
            }
        }

        internal static bool ApplyLookup(
            IOpenTypeFont           Font,           // Font access interface
            OpenTypeTags            TableTag,       // Layout table tag (GSUB or GPOS)
            FontTable               Table,          // Layout table (GSUB or GPOS)
            LayoutMetrics           Metrics,        // LayoutMetrics
            LookupTable             Lookup,         // List definition structure
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Char to glyph mapping
            GlyphInfoList           GlyphInfo,      // List of GlyphInfo structs
            int*                    Advances,       // Glyph adv.widths
            LayoutOffset*           Offsets,        // Glyph offsets
            int                     FirstGlyph,     // where to apply it
            int                     AfterLastGlyph, // how long is a context we can use
            uint                    Parameter,      // lookup parameter
            int                     nestingLevel,   // Contextual lookup nesting level
            out int                 NextGlyph       // out: next glyph index
            )
        {
            Debug.Assert(TableTag==OpenTypeTags.GSUB || TableTag==OpenTypeTags.GPOS);
            Debug.Assert(FirstGlyph<AfterLastGlyph);
            Debug.Assert(AfterLastGlyph<=GlyphInfo.Length);

            ushort lookupType = Lookup.LookupType();
            ushort lookupFlags = Lookup.LookupFlags();
            ushort subtableCount = Lookup.SubTableCount();

            bool match=false;
            NextGlyph=FirstGlyph+1; //Just to avoid compiler error

            // Find first glyph
            if (!IsLookupReversal(TableTag,lookupType))
            {
                FirstGlyph=LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,FirstGlyph,
                                                                lookupFlags,LayoutEngine.LookForward);
            }
            else
            {
                AfterLastGlyph = LayoutEngine.GetNextGlyphInLookup(Font,GlyphInfo,AfterLastGlyph-1,
                                                                      lookupFlags,LayoutEngine.LookBackward) + 1;
            }
            if (FirstGlyph>=AfterLastGlyph) return match;

            ushort originalLookupType = lookupType; // We need it to recover, if extension lookup updated lookupFormat

            for(ushort subtableIndex=0; !match && subtableIndex < subtableCount; subtableIndex++)
            {
                lookupType = originalLookupType;
                int subtableOffset = Lookup.SubtableOffset(Table, subtableIndex);

                switch (TableTag)
                {
                    case OpenTypeTags.GSUB:
                    {
                        if (lookupType == 7)
                        {
                            ExtensionLookupTable extension =
                                    new ExtensionLookupTable(subtableOffset);

                            lookupType = extension.LookupType(Table);
                            subtableOffset = extension.LookupSubtableOffset(Table);
                        }

                        switch (lookupType)
                        {
                            case 1: //SingleSubst
                                SingleSubstitutionSubtable singleSub =
                                    new SingleSubstitutionSubtable(subtableOffset);
                                match = singleSub.Apply( Table,
                                                         GlyphInfo,
                                                         FirstGlyph,
                                                         out NextGlyph
                                                       );
                                break;

                            case 2: //MultipleSubst
                                MultipleSubstitutionSubtable multipleSub =
                                    new MultipleSubstitutionSubtable(subtableOffset);
                                match = multipleSub.Apply(  Font,
                                                            Table,
                                                            CharCount,
                                                            Charmap,
                                                            GlyphInfo,
                                                            lookupFlags,
                                                            FirstGlyph,
                                                            AfterLastGlyph,
                                                            out NextGlyph
                                                         );
                                break;

                            case 3: //AlternateSubst
                                AlternateSubstitutionSubtable alternateSub =
                                    new AlternateSubstitutionSubtable(subtableOffset);
                                match = alternateSub.Apply( Table,
                                                            GlyphInfo,
                                                            Parameter,
                                                            FirstGlyph,
                                                            out NextGlyph
                                                          );
                                break;

                            case 4: //Ligature subst
                                LigatureSubstitutionSubtable ligaSub =
                                    new LigatureSubstitutionSubtable(subtableOffset);
                                match = ligaSub.Apply( Font,
                                                       Table,
                                                       CharCount,
                                                       Charmap,
                                                       GlyphInfo,
                                                       lookupFlags,
                                                       FirstGlyph,
                                                       AfterLastGlyph,
                                                       out NextGlyph
                                                     );
                                break;

                            case 5: //ContextualSubst
                                ContextSubtable contextSub =
                                    new ContextSubtable(subtableOffset);
                                match = contextSub.Apply( Font,
                                                          TableTag,
                                                          Table,
                                                          Metrics,
                                                          CharCount,
                                                          Charmap,
                                                          GlyphInfo,
                                                          Advances,
                                                          Offsets,
                                                          lookupFlags,
                                                          FirstGlyph,
                                                          AfterLastGlyph,
                                                          Parameter,
                                                          nestingLevel,
                                                          out NextGlyph
                                                        );
                                break;

                            case 6: //ChainingSubst
                                ChainingSubtable chainingSub =
                                                    new ChainingSubtable(subtableOffset);
                                match = chainingSub.Apply(  Font,
                                                            TableTag,
                                                            Table,
                                                            Metrics,
                                                            CharCount,
                                                            Charmap,
                                                            GlyphInfo,
                                                            Advances,
                                                            Offsets,
                                                            lookupFlags,
                                                            FirstGlyph,
                                                            AfterLastGlyph,
                                                            Parameter,
                                                            nestingLevel,
                                                            out NextGlyph
                                                          );
                                break;

                            case 7: //Extension lookup
                                // Ext.Lookup processed earlier. It can't contain another ext.lookups in it.
                                // Just skip it (do nothing);

                                NextGlyph = FirstGlyph + 1;
                                break;

                            case 8: //ReverseCahiningSubst
                                ReverseChainingSubtable reverseChainingSub =
                                    new ReverseChainingSubtable(subtableOffset);
                                match = reverseChainingSub.Apply(
                                                                    Font,
                                                                    TableTag,
                                                                    Table,
                                                                    Metrics,
                                                                    CharCount,
                                                                    Charmap,
                                                                    GlyphInfo,
                                                                    Advances,
                                                                    Offsets,
                                                                    lookupFlags,
                                                                    FirstGlyph,
                                                                    AfterLastGlyph,
                                                                    Parameter,
                                                                    out NextGlyph
                                                                 );
                                break;

                            default:
                                // Unknown format
                                NextGlyph = FirstGlyph+1;
                                break;
                        }

                        if (match)
                        {
                            if (!IsLookupReversal(TableTag,lookupType))
                            {
                                UpdateGlyphFlags(Font,GlyphInfo,FirstGlyph,NextGlyph,true,GlyphFlags.Substituted);
                            }
                            else
                            {
                                UpdateGlyphFlags(Font,GlyphInfo,NextGlyph,AfterLastGlyph,true,GlyphFlags.Substituted);
                            }
                        }

                        break;
                    }

                    case OpenTypeTags.GPOS:
                    {
                        if (lookupType == 9)
                        {
                            ExtensionLookupTable extension =
                                    new ExtensionLookupTable(subtableOffset);

                            lookupType = extension.LookupType(Table);
                            subtableOffset = extension.LookupSubtableOffset(Table);
}

                        switch (lookupType)
                        {
                            case 1: //SinglePos
                                SinglePositioningSubtable singlePos =
                                    new SinglePositioningSubtable(subtableOffset);
                                match = singlePos.Apply(Table,
                                                        Metrics,
                                                        GlyphInfo,
                                                        Advances,
                                                        Offsets,
                                                        FirstGlyph,
                                                        AfterLastGlyph,
                                                        out NextGlyph
                                                       );
                                break;

                            case 2: //PairPos
                                PairPositioningSubtable pairPos =
                                    new PairPositioningSubtable(subtableOffset);
                                match = pairPos.Apply(  Font,
                                                        Table,
                                                        Metrics,        // LayoutMetrics
                                                        GlyphInfo,      // List of GlyphInfo structs
                                                        lookupFlags,    // Lookup flags for glyph lookups
                                                        Advances,       // Glyph adv.widths
                                                        Offsets,        // Glyph offsets
                                                        FirstGlyph,     // where to apply lookup
                                                        AfterLastGlyph, // how long is a context we can use
                                                        out NextGlyph   // Next glyph to process
                                                     );
                                break;

                            case 3: // CursivePos
                                // Under construction
                                CursivePositioningSubtable cursivePositioningSubtable =
                                    new CursivePositioningSubtable(subtableOffset);

                                cursivePositioningSubtable.Apply(   Font,
                                                                    Table,
                                                                    Metrics,        // LayoutMetrics
                                                                    GlyphInfo,      // List of GlyphInfo structs
                                                                    lookupFlags,    // Lookup flags for glyph lookups
                                                                    Advances,       // Glyph adv.widths
                                                                    Offsets,        // Glyph offsets
                                                                    FirstGlyph,     // where to apply lookup
                                                                    AfterLastGlyph, // how long is a context we can use
                                                                    out NextGlyph   // Next glyph to process
                                                                );

                                break;

                            case 4: //MarkToBasePos
                                MarkToBasePositioningSubtable markToBasePos =
                                    new MarkToBasePositioningSubtable(subtableOffset);
                                match = markToBasePos.Apply(Font,
                                                            Table,
                                                            Metrics,        // LayoutMetrics
                                                            GlyphInfo,      // List of GlyphInfo structs
                                                            lookupFlags,    // Lookup flags for glyph lookups
                                                            Advances,       // Glyph adv.widths
                                                            Offsets,        // Glyph offsets
                                                            FirstGlyph,     // where to apply lookup
                                                            AfterLastGlyph, // how long is a context we can use
                                                            out NextGlyph   // Next glyph to process
                                                           );
                                break;


                            case 5: //MarkToLigaturePos
                                // Under construction
                                MarkToLigaturePositioningSubtable markToLigaPos =
                                   new MarkToLigaturePositioningSubtable(subtableOffset);
                                match = markToLigaPos.Apply(
                                                            Font,
                                                            Table,
                                                            Metrics,        // LayoutMetrics
                                                            GlyphInfo,      // List of GlyphInfo structs
                                                            lookupFlags,    // Lookup flags for glyph lookups
                                                            CharCount,      // Characters count (i.e. Charmap.Length);
                                                            Charmap,        // Char to glyph mapping
                                                            Advances,       // Glyph adv.widths
                                                            Offsets,        // Glyph offsets
                                                            FirstGlyph,     // where to apply lookup
                                                            AfterLastGlyph, // how long is a context we can use
                                                            out NextGlyph   // Next glyph to process
                                                           );
                                break;

                            case 6: //MarkToMarkPos
                                MarkToMarkPositioningSubtable markToMarkPos =
                                    new MarkToMarkPositioningSubtable(subtableOffset);
                                match = markToMarkPos.Apply(
                                                            Font,
                                                            Table,
                                                            Metrics,        // LayoutMetrics
                                                            GlyphInfo,      // List of GlyphInfo structs
                                                            lookupFlags,    // Lookup flags for glyph lookups
                                                            Advances,       // Glyph adv.widths
                                                            Offsets,        // Glyph offsets
                                                            FirstGlyph,     // where to apply lookup
                                                            AfterLastGlyph, // how long is a context we can use
                                                            out NextGlyph   // Next glyph to process
                                                           );
                                break;

                            case 7: // Contextual
                                ContextSubtable contextSub =
                                    new ContextSubtable(subtableOffset);
                                match = contextSub.Apply( Font,
                                                          TableTag,
                                                          Table,
                                                          Metrics,
                                                          CharCount,
                                                          Charmap,
                                                          GlyphInfo,
                                                          Advances,
                                                          Offsets,
                                                          lookupFlags,
                                                          FirstGlyph,
                                                          AfterLastGlyph,
                                                          Parameter,
                                                          nestingLevel,
                                                          out NextGlyph
                                                        );
                                break;

                            case 8: // Chaining
                                ChainingSubtable chainingSub =
                                    new ChainingSubtable(subtableOffset);
                                match = chainingSub.Apply( Font,
                                                           TableTag,
                                                           Table,
                                                           Metrics,
                                                           CharCount,
                                                           Charmap,
                                                           GlyphInfo,
                                                           Advances,
                                                           Offsets,
                                                           lookupFlags,
                                                           FirstGlyph,
                                                           AfterLastGlyph,
                                                           Parameter,
                                                           nestingLevel,
                                                           out NextGlyph
                                                         );
                                break;

                            case 9: //Extension lookup
                                // Ext.Lookup processed earlier. It can't contain another ext.lookups in it.
                                // Just skip it (do nothing);

                                NextGlyph = FirstGlyph + 1;
                                break;

                            default:
                                // Unknown format
                                NextGlyph = FirstGlyph + 1;
                                break;
                        }

                        if (match)
                        {
                            UpdateGlyphFlags(Font,GlyphInfo,FirstGlyph,NextGlyph,false,GlyphFlags.Positioned);
                        }

                        break;
                    }
                    default:
                        Debug.Assert(false,"Unknown OpenType layout table!");
                        break;
                }
            }

            return match;
        }

        private static bool IsLookupReversal(OpenTypeTags TableTag, ushort LookupType)
        {
            return (TableTag == OpenTypeTags.GSUB && LookupType == 8);
        }

        private static void CompileFeatureSet(
            Feature[]               FeatureSet,     // In: List of features to apply
            int                     featureCount,   // In: Actual number of features in FeatureSet
            int                     featureSetOffset, //In: Offset of character input sequence inside feature set
            int                     charCount,      // In: number of characters in the input string
            FontTable               Table,          // In: Layout table (GSUB or GPOS)
            LangSysTable            LangSys,        // In: Language system
            FeatureList             Features,       // In: List of Features in layout table
            int                     lookupCount,    // In: number of lookup in layout table
            OpenTypeLayoutWorkspace workspace       // In: workspace with compiled feature set
            )
        {
            workspace.InitLookupUsageFlags(lookupCount, featureCount);

            //Set lookup uasge flags for required feature
            FeatureTable requiredFeatureTable = LangSys.RequiredFeature(Table, Features);
            if (!requiredFeatureTable.IsNull)
            {
                int featureLookupCount = requiredFeatureTable.LookupCount(Table);
                for(ushort lookup = 0; lookup < featureLookupCount; lookup++)
                {
                    workspace.SetRequiredFeatureFlag(requiredFeatureTable.LookupIndex(Table,lookup));
                }
            }

            //Set lookup usage flags for each feature in the FeatureSet
            for(int feature = 0; feature < featureCount; feature++)
            {
                Feature featureDescription = FeatureSet[feature];

                //Filter out features which:
                // Not enabled or applied completely before or after input characters
                if (featureDescription.Parameter == 0 ||
                    featureDescription.StartIndex >= (featureSetOffset + charCount) ||
                    (featureDescription.StartIndex+featureDescription.Length) <= featureSetOffset
                   )
                {
                    continue;
                }

                FeatureTable featureTable = LangSys.FindFeature( Table,
                                                                 Features,
                                                                 featureDescription.Tag);
                if (featureTable.IsNull)
                {
                    continue;
                }

                int featureLookupCount = featureTable.LookupCount(Table);
                for(ushort lookup = 0; lookup < featureLookupCount; lookup++)
                {
                    workspace.SetFeatureFlag(featureTable.LookupIndex(Table,lookup), feature);
                }
            }
        }

        /// <SecurityNotes>
        /// Critical - This method reads into unsafe cluster map. 
        /// </SecurityNotes>
        private static void GetNextEnabledGlyphRange(
            Feature[]               FeatureSet,     // In: List of features to apply
            int                     featureCount,   // In: Actual nubmer of features in FeatureSet
            int                     featureSetOffset, // In: offset of input chars inside feature set
            FontTable               Table,          // Layout table (GSUB or GPOS)
            OpenTypeLayoutWorkspace workspace,      // workspace with compiled feature set
            LangSysTable            LangSys,        // Language system
            FeatureList             Features,       // List of Features in layout table
            ushort                  lookupIndex,    // List of lokups definitions in layout table
            int                     CharCount,      // Characters count (i.e. Charmap.Length);
            UshortList              Charmap,        // Character to glyph mapping

            int                     StartChar,
            int                     StartGlyph,
            int                     GlyphRunLength,

            out int                 FirstChar,      // First char in enabled range
            out int                 AfterLastChar,  // next char after enabled range
            out int                 FirstGlyph,     // First char in enabled range
            out int                 AfterLastGlyph, // next char after enabled range
            out uint                Parameter       // applied feature parameter
            )
        {
            FirstChar       = int.MaxValue;
            AfterLastChar   = int.MaxValue;
            FirstGlyph      = StartGlyph;
            AfterLastGlyph  = GlyphRunLength;
            Parameter       = 0;

            if (workspace.IsRequiredFeatureFlagSet(lookupIndex))
            {
                FirstChar       = StartChar;
                AfterLastChar   = CharCount;
                FirstGlyph      = StartGlyph;
                AfterLastGlyph  = GlyphRunLength;

                return;
            }

            for(int feature=0; feature < featureCount; feature++)
            {
                if (!workspace.IsFeatureFlagSet(lookupIndex,feature))
                {
                    continue;
                }

                Feature featureDescription = FeatureSet[feature];

                // Shift values from the feature by specified offset and
                // work with these values from here
                int featureStart = featureDescription.StartIndex - featureSetOffset;
                if (featureStart < 0)
                {
                    featureStart = 0;
                }

                int featureAfterEnd = featureDescription.StartIndex + featureDescription.Length
                                            - featureSetOffset;
                if (featureAfterEnd > CharCount)
                {
                    featureAfterEnd = CharCount;
                }

                //If feature is disabled there should not be any flag set
                Debug.Assert(featureDescription.Parameter != 0);

                if (featureAfterEnd <= StartChar)
                {
                    continue;
                }

                if (featureStart < FirstChar ||
                    (
                      featureStart == FirstChar &&
                      featureAfterEnd >= AfterLastChar
                    )
                   )
                {
                    FirstChar     = featureStart;
                    AfterLastChar = featureAfterEnd;
                    Parameter     = featureDescription.Parameter;
                    continue;
                }
            }

            //No ranges found
            if (FirstChar == int.MaxValue)
            {
                FirstGlyph      = GlyphRunLength;
                AfterLastGlyph  = GlyphRunLength;
            }
            else
            {
                if (StartGlyph > Charmap[FirstChar])
                    FirstGlyph = StartGlyph;
                else
                    FirstGlyph = Charmap[FirstChar];

                if (AfterLastChar < CharCount)
                    AfterLastGlyph = Charmap[AfterLastChar];
                else
                    AfterLastGlyph = GlyphRunLength;
            }
}

        private static void UpdateGlyphFlags(
                                                IOpenTypeFont   Font,
                                                GlyphInfoList   GlyphInfo,
                                                int             FirstGlyph,
                                                int             AfterLastGlyph,
                                                bool            DoAll,
                                                GlyphFlags      FlagToSet
                                            )
        {
            Debug.Assert( FlagToSet==GlyphFlags.NotChanged ||
                            FlagToSet==GlyphFlags.Substituted ||
                            FlagToSet==GlyphFlags.Positioned);

            ushort typemask = (ushort)GlyphFlags.GlyphTypeMask;

            FontTable gdefTable = Font.GetFontTable(OpenTypeTags.GDEF);

            if (!gdefTable.IsPresent)
            {
                //GDEF(i.e. class def in it) is not present.
                //Assign unassigned to all glyphs
                for(int i=FirstGlyph;i<AfterLastGlyph;i++)
                {
                    ushort flags = (ushort)(
                        (GlyphInfo.GlyphFlags[i] & (ushort)~typemask) |
                        (ushort)GlyphFlags.Unassigned |
                        (ushort)FlagToSet);
                }
                return;
            }

            GDEFHeader gdefHeader = new GDEFHeader(0);
            ClassDefTable GlyphClassDef = gdefHeader.GetGlyphClassDef(gdefTable);

            for(int i=FirstGlyph;i<AfterLastGlyph;i++)
            {
                ushort flags = (ushort)(GlyphInfo.GlyphFlags[i] | (ushort)FlagToSet);

                if ((flags & typemask) == (ushort)GlyphFlags.Unresolved ||
                                                FlagToSet!=GlyphFlags.NotChanged)
                {
                    ushort glyph = GlyphInfo.Glyphs[i];

                    flags &= (ushort)~typemask;

                    int glyphClass = GlyphClassDef.GetClass(gdefTable,glyph);

                    GlyphInfo.GlyphFlags[i] = (ushort)(flags|
                                                        ((glyphClass==-1)?
                                                          (ushort)GlyphFlags.Unassigned:
                                                          (ushort)glyphClass
                                                        )
                                                      );
                }
            }
        }

        public const ushort LookupFlagRightToLeft            = 0x0001;
        public const ushort LookupFlagIgnoreBases            = 0x0002;
        public const ushort LookupFlagIgnoreLigatures        = 0x0004;
        public const ushort LookupFlagIgnoreMarks            = 0x0008;
        public const ushort LookupFlagMarkAttachmentTypeMask = 0xFF00;

        //To find base glyph for mark positioning
        public const ushort LookupFlagFindBase = LookupFlagIgnoreMarks;


        //Search direction
        public const int LookForward  = 1;
        public const int LookBackward =-1;


        internal static int GetNextGlyphInLookup(
            IOpenTypeFont   Font,           //
            GlyphInfoList   GlyphInfo,      // Glyph run
            int             FirstGlyph,     // Current glyph index
            ushort          LookupFlags,    // Lookup flags to use
            int             Direction     // Search direction (forward/back)
            )
        {
            FontTable gdefTable;
            ClassDefTable markAttachClassDef;

             //assign them only to avoid error: using unassigned variable
            gdefTable = null;
            markAttachClassDef = ClassDefTable.InvalidClassDef;

            if (LookupFlags==0) return FirstGlyph;

            //we will mark classes only if mark filter is set
            if ((LookupFlags&(ushort)LookupFlagMarkAttachmentTypeMask)!=0)
            {
                gdefTable = Font.GetFontTable(OpenTypeTags.GDEF);

                if (gdefTable.IsPresent)
                {
                    markAttachClassDef = (new GDEFHeader(0)).GetMarkAttachClassDef(gdefTable);
                }
            }

            UshortList glyphFlags = GlyphInfo.GlyphFlags;
            ushort attachClass = (ushort)((LookupFlags&LookupFlagMarkAttachmentTypeMask)>>8);

            int glyph;

            int glyphRunLength    = GlyphInfo.Length;
            for(glyph=FirstGlyph; glyph<glyphRunLength && glyph>=0; glyph+=Direction)
            {
                if (
                    (LookupFlags&LookupFlagIgnoreBases)!=0 &&
                    (glyphFlags[glyph]&(ushort)GlyphFlags.GlyphTypeMask)==(ushort)GlyphFlags.Base
                    ) continue;

                if (
                    (LookupFlags&LookupFlagIgnoreMarks)!=0 &&
                    (glyphFlags[glyph]&(ushort)GlyphFlags.GlyphTypeMask)==(ushort)GlyphFlags.Mark
                   ) continue;

                if (
                    (LookupFlags&LookupFlagIgnoreLigatures)!=0 &&
                    (glyphFlags[glyph]&(ushort)GlyphFlags.GlyphTypeMask)==(ushort)GlyphFlags.Ligature
                   ) continue;

                if (attachClass!=0 &&
                    (glyphFlags[glyph]&(ushort)GlyphFlags.GlyphTypeMask)==(ushort)GlyphFlags.Mark &&
                     !markAttachClassDef.IsInvalid &&
                     attachClass!=markAttachClassDef.GetClass(gdefTable,GlyphInfo.Glyphs[glyph])
                   ) continue;

                return glyph;
            }

            return glyph;
        }

        /// <summary>
        /// Returns list of the languages, that can not be optimized for simple shaping
        /// </summary>
        internal static void GetComplexLanguageList(
                                                OpenTypeTags            tableTag,
                                                FontTable               table,
                                                uint[]                  featureTagsList,
                                                uint[]                  glyphBits,
                                                ushort                  minGlyphId,
                                                ushort                  maxGlyphId,
                                                out WritingSystem[]     complexLanguages,
                                                out int                 complexLanguageCount
                                             )
        {
            ScriptList  scriptList  = new ScriptList(0);
            FeatureList featureList = new FeatureList(0);
            LookupList  lookupList  = new LookupList(0);

            Debug.Assert(tableTag == OpenTypeTags.GSUB || tableTag == OpenTypeTags.GPOS);

            switch (tableTag)
            {
                case OpenTypeTags.GSUB:
                    GSUBHeader gsubHeader = new GSUBHeader(0);
                    scriptList  = gsubHeader.GetScriptList(table);
                    featureList = gsubHeader.GetFeatureList(table);
                    lookupList  = gsubHeader.GetLookupList(table);
                    break;

                case OpenTypeTags.GPOS:
                    GPOSHeader gposHeader = new GPOSHeader(0);
                    scriptList  = gposHeader.GetScriptList(table);
                    featureList = gposHeader.GetFeatureList(table);
                    lookupList  = gposHeader.GetLookupList(table);
                    break;
            }

            int scriptCount  = scriptList.GetScriptCount(table);
            int featureCount = featureList.FeatureCount(table);
            int lookupCount  = lookupList.LookupCount(table);

            // We will mark lookups that should be tested.
            // At the end, we will have only complex ones marked
            uint[] lookupBits = new uint[(lookupCount+31)>>5];

            for(int i = 0; i < (lookupCount+31)>>5; i++)
            {
                lookupBits[i] = 0;
            }

            // Iterate through list of featuers in the table
            for(ushort featureIndex = 0; featureIndex < featureCount; featureIndex++)
            {
                uint featureTag = (uint)featureList.FeatureTag(table, featureIndex);
                bool tagFound   = false;

                //Search for tag in the list of features in question
                for(int j = 0; j < featureTagsList.Length; j++)
                {
                    if (featureTagsList[j] == featureTag)
                    {
                        tagFound = true;
                        break;
                    }
                }

                if (tagFound)
                {
                    // We should mark all lookup mapped to this feature
                    FeatureTable featureTable = featureList.FeatureTable(table, featureIndex);
                    ushort featureLookupCount = featureTable.LookupCount(table);

                    for(ushort j = 0; j < featureLookupCount; j++)
                    {
                        ushort lookupIndex = featureTable.LookupIndex(table, j);
                        
                        if (lookupIndex >= lookupCount)
                        {
                            //This should be invalid font. Lookup associated with the feature is not in lookup array.
                            throw new FileFormatException();
                        }

                        lookupBits[lookupIndex>>5] |= (uint)(1 << (lookupIndex%32));
                    }
                }
            }

            //
            // In the future, we should mark required features for all language systems




            //Now test all marked lookups
            for (ushort lookupIndex = 0; lookupIndex < lookupCount; lookupIndex++)
            {
                if ((lookupBits[lookupIndex>>5] & (1 << (lookupIndex%32))) == 0)
                {
                    continue;
                }

                LookupTable lookup = lookupList.Lookup(table,lookupIndex);

                ushort lookupType    = lookup.LookupType();
                ushort subtableCount = lookup.SubTableCount();

                bool lookupIsCovered = false;

                ushort originalLookupType = lookupType; // We need it to recover,
                                                        // if extension lookup updated lookupFormat
                for(ushort subtableIndex = 0;
                    !lookupIsCovered && subtableIndex < subtableCount;
                    subtableIndex++)
                {
                    lookupType = originalLookupType;
                    int subtableOffset = lookup.SubtableOffset(table, subtableIndex);

                    switch (tableTag)
                    {
                        case OpenTypeTags.GSUB:
                        {
                            if (lookupType == 7)
                            {
                                ExtensionLookupTable extension =
                                        new ExtensionLookupTable(subtableOffset);

                                lookupType = extension.LookupType(table);
                                subtableOffset = extension.LookupSubtableOffset(table);
                            }

                            switch (lookupType)
                            {
                                case 1: //SingleSubst
                                    SingleSubstitutionSubtable singleSub =
                                        new SingleSubstitutionSubtable(subtableOffset);
                                    lookupIsCovered = singleSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 2: //MultipleSubst
                                    MultipleSubstitutionSubtable multipleSub =
                                        new MultipleSubstitutionSubtable(subtableOffset);
                                    lookupIsCovered = multipleSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 3: //AlternateSubst
                                    AlternateSubstitutionSubtable alternateSub =
                                        new AlternateSubstitutionSubtable(subtableOffset);
                                    lookupIsCovered = alternateSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 4: //Ligature subst
                                    LigatureSubstitutionSubtable ligaSub =
                                        new LigatureSubstitutionSubtable(subtableOffset);
                                    lookupIsCovered = ligaSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 5: //ContextualSubst
                                    ContextSubtable contextSub =
                                        new ContextSubtable(subtableOffset);
                                    lookupIsCovered = contextSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 6: //ChainingSubst
                                    ChainingSubtable chainingSub =
                                                        new ChainingSubtable(subtableOffset);
                                    lookupIsCovered = chainingSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 7: //Extension lookup
                                    Debug.Assert(false,"Ext.Lookup processed earlier!");
                                    break;

                                case 8: //ReverseCahiningSubst
                                    ReverseChainingSubtable reverseChainingSub =
                                        new ReverseChainingSubtable(subtableOffset);
                                    lookupIsCovered = reverseChainingSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                default:
                                    // Unknown format
                                    lookupIsCovered = true;
                                    break;
                            }

                            break;
                        }

                        case OpenTypeTags.GPOS:
                        {
                            if (lookupType == 9)
                            {
                                ExtensionLookupTable extension =
                                        new ExtensionLookupTable(subtableOffset);

                                lookupType = extension.LookupType(table);
                                subtableOffset = extension.LookupSubtableOffset(table);
}

                            switch (lookupType)
                            {
                                case 1: //SinglePos
                                    SinglePositioningSubtable singlePos =
                                        new SinglePositioningSubtable(subtableOffset);
                                    lookupIsCovered = singlePos.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 2: //PairPos
                                    PairPositioningSubtable pairPos =
                                        new PairPositioningSubtable(subtableOffset);
                                    lookupIsCovered = pairPos.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 3: // CursivePos
                                    // Under construction
                                    CursivePositioningSubtable cursivePositioningSubtable =
                                        new CursivePositioningSubtable(subtableOffset);

                                    lookupIsCovered = cursivePositioningSubtable.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);

                                    break;

                                case 4: //MarkToBasePos
                                    MarkToBasePositioningSubtable markToBasePos =
                                        new MarkToBasePositioningSubtable(subtableOffset);
                                    lookupIsCovered = markToBasePos.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;


                                case 5: //MarkToLigaturePos
                                    // Under construction
                                    MarkToLigaturePositioningSubtable markToLigaPos =
                                       new MarkToLigaturePositioningSubtable(subtableOffset);
                                    lookupIsCovered = markToLigaPos.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 6: //MarkToMarkPos
                                    MarkToMarkPositioningSubtable markToMarkPos =
                                        new MarkToMarkPositioningSubtable(subtableOffset);
                                    lookupIsCovered = markToMarkPos.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 7: // Contextual
                                    ContextSubtable contextSub =
                                        new ContextSubtable(subtableOffset);
                                    lookupIsCovered = contextSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 8: // Chaining
                                    ChainingSubtable chainingSub =
                                        new ChainingSubtable(subtableOffset);
                                    lookupIsCovered = chainingSub.IsLookupCovered(table,glyphBits,minGlyphId,maxGlyphId);
                                    break;

                                case 9: //Extension lookup
                                    Debug.Assert(false,"Ext.Lookup processed earlier!");
                                    break;

                                default:
                                    // Unknown format
                                    lookupIsCovered = true;
                                    break;
                            }

                            break;
                        }

                        default:
                            Debug.Assert(false,"Unknown OpenType layout table!");
                            break;
                    }
                }

                if (!lookupIsCovered)
                {
                    // Clean the flag
                    lookupBits[lookupIndex>>5] &= ~(uint)(1 << (lookupIndex%32));
                }
            }


            // Check if we have any lookup left
            bool complexLookupFound = false;

            for(int i = 0; i < (lookupCount+31)>>5; i++)
            {
                if (lookupBits[i] != 0)
                {
                    complexLookupFound = true;
                    break;
                }
            }

            if (!complexLookupFound)
            {
                // There are no complex lookups
                complexLanguages = null;
                complexLanguageCount = 0;
                return;
            }

            // Now go through all langauages and fill the list
            complexLanguages = new WritingSystem[10];
            complexLanguageCount = 0;

            for(ushort scriptIndex = 0; scriptIndex < scriptCount; scriptIndex++)
            {
                ScriptTable  scriptTable = scriptList.GetScriptTable(table, scriptIndex);
                uint scriptTag = scriptList.GetScriptTag(table, scriptIndex);

                ushort langSysCount = scriptTable.GetLangSysCount(table);

                if (scriptTable.IsDefaultLangSysExists(table))
                {
                    AppendLangSys(scriptTag, (uint)OpenTypeTags.dflt,
                                  scriptTable.GetDefaultLangSysTable(table),
                                  featureList,
                                  table,
                                  featureTagsList,
                                  lookupBits,
                                  ref complexLanguages,
                                  ref complexLanguageCount
                                 );
                }

                for(ushort langSysIndex = 0; langSysIndex < langSysCount; langSysIndex++)
                {
                    uint langSysTag = scriptTable.GetLangSysTag(table, langSysIndex);

                    AppendLangSys(scriptTag, langSysTag,
                                  scriptTable.GetLangSysTable(table, langSysIndex),
                                  featureList,
                                  table,
                                  featureTagsList,
                                  lookupBits,
                                  ref complexLanguages,
                                  ref complexLanguageCount
                                 );
                }
            }
        }

        private static void AppendLangSys(
                            uint                scriptTag,
                            uint                langSysTag,
                            LangSysTable        langSysTable,
                            FeatureList         featureList,
                            FontTable           table,
                            uint[]              featureTagsList,
                            uint[]              lookupBits,
                            ref WritingSystem[] complexLanguages,
                            ref int             complexLanguageCount
                     )
        {
            ushort featureCount  = langSysTable.FeatureCount(table);

            bool complexFeatureFound = false;

            // Future enhancement: Check required feature

            for(ushort i = 0; !complexFeatureFound && i < featureCount; i++)
            {
                ushort featureIndex = langSysTable.GetFeatureIndex(table, i);

                uint featureTag = featureList.FeatureTag(table,featureIndex);
                bool tagFound = false;

                for(int j = 0; !complexFeatureFound && j < featureTagsList.Length; j++)
                {
                    if (featureTagsList[j] == featureTag)
                    {
                        tagFound = true;
                        break;
                    }
                }

                if (tagFound)
                {
                    // We should check if any of lookups is complex
                    FeatureTable featureTable = featureList.FeatureTable(table, featureIndex);
                    ushort featureLookupCount = featureTable.LookupCount(table);

                    for(ushort j = 0; j < featureLookupCount; j++)
                    {
                        ushort lookupIndex = featureTable.LookupIndex(table, j);

                        if ((lookupBits[lookupIndex>>5] & (1 << (lookupIndex%32))) != 0)
                        {
                            complexFeatureFound = true;
                            break;
                        }
                    }
                }
            }

            if (complexFeatureFound)
            {
                if (complexLanguages.Length == complexLanguageCount)
                {
                    WritingSystem[] newComplexLanguages =
                                        new WritingSystem[complexLanguages.Length * 3 /2];

                    for(int i = 0; i < complexLanguages.Length; i++)
                    {
                        newComplexLanguages[i] = complexLanguages[i];
                    }

                    complexLanguages = newComplexLanguages;
                }

                complexLanguages[complexLanguageCount].scriptTag = scriptTag;
                complexLanguages[complexLanguageCount].langSysTag = langSysTag;
                complexLanguageCount++;
            }
}
}

    internal struct GSUBHeader
    {
        private const int offsetScriptList = 4;
        private const int offsetFeatureList = 6;
        private const int offsetLookupList = 8;

        public ScriptList GetScriptList(FontTable Table)
        {
            return new ScriptList(offset+Table.GetOffset(offset+offsetScriptList));
        }

        public FeatureList GetFeatureList(FontTable Table)
        {
            return new FeatureList(offset+Table.GetOffset(offset+offsetFeatureList));
        }

        public LookupList GetLookupList(FontTable Table)
        {
            return new LookupList(offset+Table.GetOffset(offset+offsetLookupList));
        }

        public GSUBHeader(int Offset)
        {
            offset = Offset;
        }

        private int offset;
    }

    internal struct GPOSHeader
    {
        private const int offsetScriptList = 4;
        private const int offsetFeatureList = 6;
        private const int offsetLookupList = 8;

        public ScriptList GetScriptList(FontTable Table)
        {
            return new ScriptList(offset+Table.GetOffset(offset+offsetScriptList));
        }

        public FeatureList GetFeatureList(FontTable Table)
        {
            return new FeatureList(offset+Table.GetOffset(offset+offsetFeatureList));
        }

        public LookupList GetLookupList(FontTable Table)
        {
            return new LookupList(offset+Table.GetOffset(offset+offsetLookupList));
        }

        public GPOSHeader(int Offset)
        {
            offset = Offset;
        }

        private int offset;
    }

    internal struct GDEFHeader
    {
        private const int offsetGlyphClassDef = 4;
        private const int offsetGlyphAttachList = 6;
        private const int offsetLigaCaretList = 8;
        private const int offsetMarkAttachClassDef = 10;

        public ClassDefTable GetGlyphClassDef(FontTable Table)
        {
            //There is only one place where GDEF classdef is retireved -
            // UpdateGlyphFlags. We will check if GDEF is present there
            Invariant.Assert(Table.IsPresent);

            return new ClassDefTable(offset + Table.GetOffset(offset + offsetGlyphClassDef));
        }

        // /// <summary>
        // /// Under construction
        // /// Glyph attachment points list
        // /// </summary>
        // /// <param name="Table"></param>
        // /// <returns></returns>
        //public GlyphAttachList GetGlyphAttachList(FontTable Table)
        //{
        //    return new GlyphAttachList(offset + Table.GetOffset(offset + offsetGlyphAttachList));
        //}

        // /// <summary>
        // /// Under construction
        // /// Ligature caret positioning data
        // /// </summary>
        // /// <param name="Table"></param>
        // /// <returns></returns>
        // //public LigatureCaretList GetLigatureCaretlist(FontTable Table)
        // //{
        // //    return new LigatureCaretList(offset + Table.GetOffset(offset + offsetLigaCaretList));
        // //}
        public ClassDefTable GetMarkAttachClassDef(FontTable Table)
        {
            //There is only one place where GDEF classdef is retireved -
            // GetNextGlyphInLokup We will check if GDEF is present there.
            Invariant.Assert(Table.IsPresent);

            return new ClassDefTable(offset+Table.GetOffset(offset+offsetMarkAttachClassDef));
        }

        public GDEFHeader(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct ScriptList
    {
        private const int offsetScriptCount = 0;
        private const int offsetScriptRecordArray = 2;
        private const int sizeScriptRecord = 6;
        private const int offsetScriptRecordTag = 0;
        private const int offsetScriptRecordOffset = 4;

        public ScriptTable FindScript(FontTable Table, uint Tag)
        {
            for(ushort i=0;i<GetScriptCount(Table);i++)
            {
                if (GetScriptTag(Table,i)==Tag)
                {
                    return GetScriptTable(Table,i);
                }
            }
            return new ScriptTable(FontTable.InvalidOffset);
        }

        public ushort GetScriptCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetScriptCount);
        }

        public uint GetScriptTag(FontTable Table, ushort Index)
        {
            return Table.GetUInt(offset + offsetScriptRecordArray +
                                            Index*sizeScriptRecord +
                                            offsetScriptRecordTag);
        }

        public ScriptTable GetScriptTable(FontTable Table, ushort Index)
        {
            return new ScriptTable(offset +
                                    Table.GetOffset(offset + offsetScriptRecordArray +
                                                             Index*sizeScriptRecord +
                                                             offsetScriptRecordOffset));
        }

        public ScriptList(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct ScriptTable
    {
        private const int offsetDefaultLangSys = 0;
        private const int offsetLangSysCount = 2;
        private const int offsetLangSysRecordArray = 4;
        private const int sizeLangSysRecord = 6;
        private const int offsetLangSysRecordTag = 0;
        private const int offsetLangSysRecordOffset = 4;

        public LangSysTable FindLangSys(FontTable Table, uint Tag)
        {
            if (IsNull)
            {
                return new LangSysTable(FontTable.InvalidOffset);
            }

            if ((OpenTypeTags)Tag==OpenTypeTags.dflt)
            {
                if (IsDefaultLangSysExists(Table))
                    return new LangSysTable(offset +
                                    Table.GetOffset(offset + offsetDefaultLangSys));

                return new LangSysTable(FontTable.InvalidOffset);
            }

            for(ushort i=0;i<GetLangSysCount(Table);i++)
            {
                if (GetLangSysTag(Table,i)==Tag)
                {
                    return GetLangSysTable(Table,i);
                }
            }
            return new LangSysTable(FontTable.InvalidOffset);
        }

        public bool IsDefaultLangSysExists(FontTable Table)
        {
            return Table.GetOffset(offset + offsetDefaultLangSys)!=0;
        }

        public LangSysTable GetDefaultLangSysTable(FontTable Table)
        {
            if (IsDefaultLangSysExists(Table))
                return new LangSysTable(offset+Table.GetOffset(offset+offsetDefaultLangSys));

            return new LangSysTable(FontTable.InvalidOffset);
        }

        public ushort GetLangSysCount(FontTable Table)
        {
            return Table.GetUShort(offset+offsetLangSysCount);
        }

        public uint GetLangSysTag(FontTable Table,ushort Index)
        {
            return Table.GetUInt(offset + offsetLangSysRecordArray +
                                     Index*sizeLangSysRecord + offsetLangSysRecordTag);
        }

        public LangSysTable GetLangSysTable(FontTable Table,ushort Index)
        {
            return new LangSysTable(offset + Table.GetOffset(offset+
                                                               offsetLangSysRecordArray +
                                                               Index*sizeLangSysRecord +
                                                               offsetLangSysRecordOffset));
        }

        public ScriptTable(int Offset) { offset = Offset; }
        public bool IsNull { get{ return (offset==FontTable.InvalidOffset); } }
        private int offset;
    }

    internal struct LangSysTable
    {
        private const int offsetRequiredFeature = 2;
        private const int offsetFeatureCount = 4;
        private const int offsetFeatureIndexArray = 6;
        private const int sizeFeatureIndex = 2;

        public FeatureTable FindFeature(FontTable Table, FeatureList Features, uint FeatureTag)
        {
            ushort featureCount = FeatureCount(Table);
            for(ushort i=0;i<featureCount;i++)
            {
                ushort featureIndex = GetFeatureIndex(Table,i);
                if (Features.FeatureTag(Table,featureIndex) == FeatureTag)
                {
                    return Features.FeatureTable(Table,featureIndex);
                }
            }
            return new FeatureTable(FontTable.InvalidOffset);
        }

        public FeatureTable RequiredFeature(FontTable Table, FeatureList Features)
        {
            ushort requiredFeatureIndex = Table.GetUShort(offset + offsetRequiredFeature);
            if (requiredFeatureIndex != 0xFFFF)
            {
                return Features.FeatureTable(Table,requiredFeatureIndex);
            }
            else
            {
                return new FeatureTable(FontTable.InvalidOffset);
            }
        }

        public ushort FeatureCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFeatureCount);
        }

        public ushort GetFeatureIndex(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFeatureIndexArray + Index*sizeFeatureIndex);
        }

        public LangSysTable(int Offset) { offset = Offset; }
        public bool IsNull { get{ return (offset==FontTable.InvalidOffset); } }
        private int offset;
    }

    internal struct FeatureList
    {
        private const int offsetFeatureCount = 0;
        private const int offsetFeatureRecordArray = 2;
        private const int sizeFeatureRecord = 6;
        private const int offsetFeatureRecordTag = 0;
        private const int offsetFeatureRecordOffset = 4;

        public ushort FeatureCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFeatureCount);
        }

        public uint FeatureTag(FontTable Table,ushort Index)
        {
            return Table.GetUInt(offset + offsetFeatureRecordArray +
                                            Index * sizeFeatureRecord +
                                            offsetFeatureRecordTag);
        }

        public FeatureTable FeatureTable(FontTable Table,ushort Index)
        {
            return new FeatureTable(offset+Table.GetUShort(offset +
                                                             offsetFeatureRecordArray +
                                                             Index * sizeFeatureRecord +
                                                             offsetFeatureRecordOffset));
        }

        public FeatureList(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct FeatureTable
    {
        private const int offsetLookupCount = 2;
        private const int offsetLookupIndexArray = 4;
        private const int sizeLookupIndex = 2;

        public ushort LookupCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetLookupCount);
        }

        public ushort LookupIndex(FontTable Table,ushort Index)
        {
            return Table.GetUShort(offset + offsetLookupIndexArray + Index*sizeLookupIndex);
        }

        public FeatureTable(int Offset){ offset = Offset; }
        public bool IsNull { get{ return (offset==FontTable.InvalidOffset); } }
        private int offset;
    }


    internal struct LookupList
    {
        private const int offsetLookupCount = 0;
        private const int LookupOffsetArray = 2;
        private const int sizeLookupOffset = 2;

        public ushort LookupCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetLookupCount);
        }

        public LookupTable Lookup(FontTable Table, ushort Index)
        {
            return new LookupTable(Table, offset + Table.GetUShort(offset + LookupOffsetArray +
                                                                Index * sizeLookupOffset));
        }

        public LookupList(int Offset) { offset = Offset; }
        private int offset;
    }

    internal struct LookupTable
    {
        private const int offsetLookupType = 0;
        private const int offsetLookupFlags = 2;
        private const int offsetSubtableCount = 4;
        private const int offsetSubtableArray = 6;
        private const int sizeSubtableOffset = 2;

        public ushort LookupType()
        {
            return lookupType;
        }

        public ushort LookupFlags()
        {
            return lookupFlags;
        }

        public ushort SubTableCount()
        {
            return subtableCount;
        }

        public int SubtableOffset(FontTable Table, ushort Index)
        {
            Debug.Assert(Index < SubTableCount());
            return offset+Table.GetOffset(offset + offsetSubtableArray +
                                                    Index*sizeSubtableOffset);
        }

        public LookupTable(FontTable table, int Offset)
        {
            offset        = Offset;
            lookupType    = table.GetUShort(offset + offsetLookupType);
            lookupFlags   = table.GetUShort(offset + offsetLookupFlags);
            subtableCount = table.GetUShort(offset + offsetSubtableCount);
        }

        private int     offset;
        private ushort  lookupType;
        private ushort  lookupFlags;
        private ushort  subtableCount;
    }


    internal struct CoverageTable
    {
        private const int offsetFormat = 0;
        private const int offsetFormat1GlyphCount = 2;
        private const int offsetFormat1GlyphArray = 4;
        private const int sizeFormat1GlyphId = 2;
        private const int offsetFormat2RangeCount = 2;
        private const int offsetFormat2RangeRecordArray = 4;
        private const int sizeFormat2RangeRecord = 6;
        private const int offsetFormat2RangeRecordStart = 0;
        private const int offsetFormat2RangeRecordEnd = 2;
        private const int offsetFormat2RangeRecordStartIndex = 4;

        public ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        public ushort Format1GlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat1GlyphCount);
        }

        public ushort Format1Glyph(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat1GlyphArray +
                                            Index*sizeFormat1GlyphId);
        }

        public ushort Format2RangeCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat2RangeCount);
        }

        public ushort Format2RangeStartGlyph(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat2RangeRecordArray +
                                            Index*sizeFormat2RangeRecord +
                                            offsetFormat2RangeRecordStart);
        }

        public ushort Format2RangeEndGlyph(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat2RangeRecordArray +
                                            Index*sizeFormat2RangeRecord +
                                            offsetFormat2RangeRecordEnd);
        }

        public ushort Format2RangeStartCoverageIndex(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat2RangeRecordArray +
                                            Index*sizeFormat2RangeRecord +
                                            offsetFormat2RangeRecordStartIndex);
        }


        public int GetGlyphIndex(FontTable Table, ushort glyph)
        {
            switch (Format(Table))
            {
                case 1: // Coverage array
                    {
                        ushort lowIndex = 0;
                        ushort highIndex = Format1GlyphCount(Table);
                        while(lowIndex < highIndex)
                        {
                            ushort middleIndex = (ushort)((lowIndex + highIndex) >> 1);
                            ushort middleGlyph = Format1Glyph(Table,middleIndex);

                            if (glyph < middleGlyph)
                            {
                                highIndex = middleIndex;
                            }
                            else if (glyph > middleGlyph)
                            {
                                lowIndex = (ushort)(middleIndex+1);
                            }
                            else
                            {
                                return middleIndex;
                            }
                        }

                        return  -1;
                    }

                case 2: //Glyph Ranges
                    {
                        ushort lowIndex = 0;
                        ushort highIndex = Format2RangeCount(Table);
                        while(lowIndex < highIndex)
                        {
                            ushort middleIndex = (ushort)((lowIndex + highIndex) >> 1);

                            if (glyph < Format2RangeStartGlyph(Table, middleIndex))
                            {
                                highIndex = middleIndex;
                            }
                            else if (glyph > Format2RangeEndGlyph(Table, middleIndex))
                            {
                                lowIndex = (ushort)(middleIndex + 1);
                            }
                            else
                            {
                                return (glyph - Format2RangeStartGlyph(Table,middleIndex))
                                        + Format2RangeStartCoverageIndex(Table,middleIndex);
                            }
                        }

                        return -1;
                    }

                default:
                    //unknown format. Return NoMatch.
                    return -1;
            }
        }

        public bool IsAnyGlyphCovered(
                        FontTable table,
                        uint[] glyphBits,
                        ushort minGlyphId,
                        ushort maxGlyphId)
        {
            switch (Format(table))
            {
                case 1: // Coverage array
                    {
                        ushort glyphCount   = Format1GlyphCount(table);
                        if (glyphCount == 0) return false;

                        ushort firstGlyphId = Format1Glyph(table, 0);
                        ushort lastGlyphId  = Format1Glyph(table, (ushort)(glyphCount - 1));

                        if (maxGlyphId < firstGlyphId || minGlyphId > lastGlyphId) return false;

                        for(ushort i = 0; i < glyphCount; i++)
                        {
                            ushort glyphId = Format1Glyph(table, i);

                            if (glyphId <= maxGlyphId &&
                                glyphId >= minGlyphId &&
                                (glyphBits[glyphId >> 5] & (1 << (glyphId % 32))) != 0
                               )
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                case 2: //Glyph Ranges
                    {
                        ushort rangeCount   = Format2RangeCount(table);
                        if (rangeCount == 0) return false;

                        ushort firstGlyphId = Format2RangeStartGlyph(table,0);
                        ushort lastGlyphId  = Format2RangeEndGlyph(table, (ushort)(rangeCount - 1));

                        if (maxGlyphId < firstGlyphId || minGlyphId > lastGlyphId) return false;

                        for (ushort rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
                        {
                            ushort startGlyphId = Format2RangeStartGlyph(table,rangeIndex);
                            ushort endGlyphId = Format2RangeEndGlyph(table, rangeIndex);

                            for (ushort glyphId = startGlyphId; glyphId <= endGlyphId; glyphId++)
                            {
                                if (glyphId <= maxGlyphId &&
                                    glyphId >= minGlyphId &&
                                    (glyphBits[glyphId >> 5] & (1 << (glyphId % 32))) != 0
                                   )
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }

            default:
                    //unknown format. Return true.
                    return true;
            }
        }

        public static CoverageTable InvalidCoverage
        {
            get { return new CoverageTable(-1); }
        }
        
        public bool IsInvalid
        {
            get { return offset == -1; }
        }

        public CoverageTable(int Offset) { offset = Offset; }
        
        private int offset;
    }

    internal struct ClassDefTable
    {
        private const int offsetFormat = 0;
        private const int offsetFormat1StartGlyph = 2;
        private const int offsetFormat1GlyphCount = 4;
        private const int offsetFormat1ClassValueArray = 6;
        private const int sizeFormat1ClassValue = 2;
        private const int offsetFormat2RangeCount = 2;
        private const int offsetFormat2RangeRecordArray = 4;
        private const int sizeFormat2RangeRecord = 6;
        private const int offsetFormat2RangeRecordStart = 0;
        private const int offsetFormat2RangeRecordEnd = 2;
        private const int offsetFormat2RangeRecordClass = 4;


        private ushort Format(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat);
        }

        private ushort Format1StartGlyph(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat1StartGlyph);
        }

        private ushort Format1GlyphCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat1GlyphCount);
        }

        private ushort Format1ClassValue(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat1ClassValueArray +
                                                Index*sizeFormat1ClassValue);
        }

        private ushort Format2RangeCount(FontTable Table)
        {
            return Table.GetUShort(offset + offsetFormat2RangeCount);
        }

        private ushort Format2RangeStartGlyph(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat2RangeRecordArray +
                                            Index*sizeFormat2RangeRecord +
                                            offsetFormat2RangeRecordStart);
        }

        private ushort Format2RangeEndGlyph(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat2RangeRecordArray +
                                            Index*sizeFormat2RangeRecord +
                                            offsetFormat2RangeRecordEnd);
        }

        private ushort Format2RangeClassValue(FontTable Table, ushort Index)
        {
            return Table.GetUShort(offset + offsetFormat2RangeRecordArray +
                                            Index*sizeFormat2RangeRecord +
                                            offsetFormat2RangeRecordClass);
        }

        public ushort GetClass(FontTable Table, ushort glyph)
        {
            //PERF: binary search!!!
            switch (Format(Table))
            {
                case 1: // ClassDef array
                {
                    ushort startGlyph = Format1StartGlyph(Table);
                    ushort glyphCount = Format1GlyphCount(Table);

                    if (glyph >= startGlyph && (glyph - startGlyph) < glyphCount)
                        return Format1ClassValue(Table,(ushort)(glyph - startGlyph));
                    else
                        return 0;
}
                case 2: //ClassDef Ranges
                {
                    ushort lowIndex = 0;
                    ushort highIndex = Format2RangeCount(Table);
                    while(lowIndex < highIndex)
                    {
                        ushort middleIndex = (ushort)((lowIndex + highIndex) >> 1);

                        if (glyph < Format2RangeStartGlyph(Table, middleIndex))
                        {
                            highIndex = middleIndex;
                        }
                        else if (glyph > Format2RangeEndGlyph(Table, middleIndex))
                        {
                            lowIndex = (ushort)(middleIndex + 1);
                        }
                        else
                        {
                            return Format2RangeClassValue(Table,middleIndex);
                        }
                    }

                    return 0;
                }
                default:
                    //unknown format. Return default: 0
                    return 0;
            }
        }

        public static ClassDefTable InvalidClassDef
        {
            get { return new ClassDefTable(-1); }
        }
        
        public bool IsInvalid
        {
            get { return offset == -1; }
        }

        public ClassDefTable(int Offset) { offset = Offset; }
        private int offset;
    }


    internal struct ExtensionLookupTable
    {
        private const int offsetFormat          = 0;
        private const int offsetLookupType      = 2;
        private const int offsetExtensionOffset = 4;

        internal ushort LookupType(FontTable Table)
        {
            return Table.GetUShort(offset + offsetLookupType);
        }

        internal int LookupSubtableOffset(FontTable Table)
        {
            return offset + (int)Table.GetUInt(offset + offsetExtensionOffset);
        }

        public ExtensionLookupTable(int Offset) { offset = Offset; }
        private int offset;
    }
}

