// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




using System;
using System.Security;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal.Shaping;
using MS.Internal.Generic;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Formatting state of full text
    /// </summary>
    internal sealed class FullTextState
    {
        private TextStore           _store;                     // formatting store for main text
        private TextStore           _markerStore;               // store specifically for marker
        private StatusFlags         _statusFlags;               // status flags
        private int                 _cpMeasured;                // number of CP successfully measured and fit within column width
        private int                 _lscpHyphenationLookAhead;  // LSCP after the last character examined by the hyphenation code.
        private bool                _isSideways;


        [Flags]
        private enum StatusFlags
        {
            None            = 0,
            VerticalAdjust  = 0x00000001,   // vertical adjustment on some runs required
            ForceWrap       = 0x00000002,   // force wrap
            KeepState       = 0x00000040,   // state should be kept in the line
        }


        /// <summary>
        /// A word smaller than seven characters does not require hyphenation. This is based on 
        /// observation in real Western books and publications. Advanced research on readability
        /// seems to support this finding. It suggests that the 'fovea' which is the center
        /// point of our vision, can only see three to four letters before and after the center
        /// of a word.
        /// </summary>
        /// <remarks>
        /// "It has been known for over 100 years that when we read, our eyes don't move smoothly 
        /// across the page, but rather make discrete jumps from word to word. We fixate on a word 
        /// for a period of time, roughly 200-250ms, then make a ballistic movement to another word. 
        /// These movements are called saccades and usually take 20-35ms. Most saccades are forward 
        /// movements from 7 to 9 letters.
        /// ...
        /// During a single fixation, there is a limit to the amount of information that can be 
        /// recognized. The fovea, which is the clear center point of our vision, can only see 
        /// three to four letters to the left and right of fixation at normal reading distances."
        /// 
        /// ["The Science of Word Recognition" by Kevin Larson; July 2004]
        /// </remarks>
        private const int MinCchWordToHyphenate = 7;


        /// <summary>
        /// Create a fulltext state object from formatting info for subsequent fulltext formatting
        /// e.g. fulltext line, mix/max computation, potential breakpoints for optimal paragraph formatting.
        /// </summary>
        internal static FullTextState Create(
            FormatSettings      settings,
            int                 cpFirst,
            int                 finiteFormatWidth
            )
        {
            // prepare text stores
            TextStore store = new TextStore(
                settings, 
                cpFirst, 
                0, 
                settings.GetFormatWidth(finiteFormatWidth)
                );

            ParaProp pap = settings.Pap;
            TextStore markerStore = null;

            if(     pap.FirstLineInParagraph
                &&  pap.TextMarkerProperties != null
                &&  pap.TextMarkerProperties.TextSource != null)
            {
                // create text store specifically for marker
                markerStore = new TextStore(
                    // create specialized settings for marker store e.g. with marker text source
                    new FormatSettings(
                        settings.Formatter,
                        pap.TextMarkerProperties.TextSource,
                        new TextRunCacheImp(),   // no cross-call run cache available for marker store
                        pap,                     // marker by default use the same paragraph properties
                        null,                    // not consider previousLineBreak
                        true,                    // isSingleLineFormatting
                        settings.TextFormattingMode,
                        settings.IsSideways
                        ),
                    0,                           // marker store always started with cp == 0
                    TextStore.LscpFirstMarker,   // first lscp value for marker text
                    Constants.IdealInfiniteWidth // formatWidth 
                    );
            }

            // construct a new fulltext state object
            return new FullTextState(store, markerStore, settings.IsSideways);
        }


        /// <summary>
        /// Construct full text state for formatting
        /// </summary>
        private FullTextState(
            TextStore       store,
            TextStore       markerStore,
            bool isSideways
            )
        {
            _isSideways = isSideways;
            _store = store;
            _markerStore = markerStore;
        }


        /// <summary>
        /// Number of client CP for which we know the nominal widths fit in the
        /// available margin (i.e., because we've measured them).
        /// </summary>
        internal int CpMeasured 
        {
            get 
            { 
                return _cpMeasured; 
            }
            set 
            { 
                _cpMeasured = value; 
            }
        }


        /// <summary>
        /// LSCP immediately after the last LSCP examined by hyphenation code while being run.
        /// This value is used to calculate a more precise DependentLength value for the line
        /// ended by automatic hyphenation. 
        /// </summary>
        internal int LscpHyphenationLookAhead
        {
            get
            {
                return _lscpHyphenationLookAhead;
            }
        }

        internal TextFormattingMode TextFormattingMode
        {
            get
            {
                return Formatter.TextFormattingMode;
            }
        }

        internal bool IsSideways
        {
            get
            {
                return _isSideways;
            }
        }

        /// <summary>
        /// Set tab stops
        /// </summary>
        internal void SetTabs(TextFormatterContext context)
        {
            unsafe
            {
                ParaProp pap = _store.Pap;
                FormatSettings settings = _store.Settings;

                // set up appropriate tab stops
                int incrementalTab = TextFormatterImp.RealToIdeal(pap.DefaultIncrementalTab);
                int lsTbdCount = pap.Tabs != null ? pap.Tabs.Count : 0;
                LsTbd[] lsTbds;

                if (_markerStore != null)
                {
                    if (pap.Tabs != null && pap.Tabs.Count > 0)
                    {
                        lsTbdCount = pap.Tabs.Count + 1;
                        lsTbds = new LsTbd[lsTbdCount];
                        lsTbds[0].ur = settings.TextIndent; // marker requires a tab stop at text start position
                        fixed (LsTbd* plsTbds = &lsTbds[1])
                        {
                            CreateLsTbds(pap, plsTbds, lsTbdCount - 1);
                            context.SetTabs(incrementalTab, plsTbds - 1, lsTbdCount);
                        }
                    }
                    else
                    {
                        LsTbd markerRequiredLsTbd = new LsTbd();
                        markerRequiredLsTbd.ur = settings.TextIndent; // marker requires a tab stop at text start position
                        context.SetTabs(incrementalTab, &markerRequiredLsTbd, 1);
                    }
                }
                else
                {
                    if (pap.Tabs != null && pap.Tabs.Count > 0)
                    {
                        lsTbds = new LsTbd[lsTbdCount];
                        fixed (LsTbd* plsTbds = &lsTbds[0])
                        {
                            CreateLsTbds(pap, plsTbds, lsTbdCount);
                            context.SetTabs(incrementalTab, plsTbds, lsTbdCount);
                        }
                    }
                    else
                    {
                        // work with only incremental tab
                        context.SetTabs(incrementalTab, null, 0);
                    }
                }
            }
        }


        /// <summary>
        /// Fill a fixed buffer of LsTbd with 
        /// </summary>
        private unsafe void CreateLsTbds(
            ParaProp        pap,
            LsTbd*          plsTbds,
            int             lsTbdCount
            )
        {
            for (int i = 0; i < lsTbdCount; i++)
            {
                TextTabProperties tab = (TextTabProperties)pap.Tabs[i];
                plsTbds[i].lskt = Convert.LsKTabFromTabAlignment(tab.Alignment);
                plsTbds[i].ur = TextFormatterImp.RealToIdeal(tab.Location);

                if (tab.TabLeader != 0)
                {
                    // Note: LS does not currently support surrogate character as tab leader and aligning character
                    plsTbds[i].wchTabLeader = (char)tab.TabLeader;

                    // tab leader requires state at display time for tab leader width fetching
                    _statusFlags |= StatusFlags.KeepState;
                }
                plsTbds[i].wchCharTab = (char)tab.AligningCharacter;
            }
        }


        /// <summary>
        /// Get distance from the start of main text to the end of marker
        /// </summary>
        /// <remarks>
        /// Positive distance is filtered out. Marker overlapping the main text is not supported.
        /// </remarks>
        internal int GetMainTextToMarkerIdealDistance()
        {
            if (_markerStore != null)
            {
                return Math.Min(0, TextFormatterImp.RealToIdeal(_markerStore.Pap.TextMarkerProperties.Offset) - _store.Settings.TextIndent);
            }
            return 0;
        }

        
        /// <summary>
        /// Map LSCP to host CP, and return the last LSRun
        /// before the specified limit.
        /// </summary>
        internal LSRun CountText(
            int         lscpLim,
            int         cpFirst,
            out int     count
            )
        {
            LSRun lastRun = null;
            count = 0;

            int lsccp = lscpLim - _store.CpFirst;
            Debug.Assert(lsccp > 0, "Zero-length text line!");

            foreach (Span span in _store.PlsrunVector)
            {
                if (lsccp <= 0)
                    break;

                Plsrun plsrun = (Plsrun)span.element;

                // There should be no marker runs in _plsrunVector.
                Debug.Assert(!TextStore.IsMarker(plsrun));

                // Is it a normal, non-static, LSRun?
                if (plsrun >= Plsrun.FormatAnchor)
                {
                    // Get the run and remember the last run.
                    lastRun = _store.GetRun(plsrun);

                    // Accumulate the length.
                    int cpRun = lastRun.Length;
                    if (cpRun > 0)
                    {
                        if (lsccp < span.length && cpRun == span.length)
                        {
                            count += lsccp;
                            break;
                        }
                        count += cpRun;
                    }
                }

                lsccp -= span.length;
            }

            // make char count relative to cpFirst as the cpFirst of this metrics may not
            // be the same as the cpFirst of the store in optimal paragraph formatting.
            count = count - cpFirst + _store.CpFirst;

            return lastRun;
        }


        /// <summary>
        /// Convert the specified external CP to LSCP corresponding to a possible break position for optimal break.
        /// </summary>
        /// <remarks>
        /// There is a generic issue that one CP could map to multiple LSCPs when it comes to
        /// lsrun that occupies no actual CP. Such lsrun is generated by line layout during
        /// formatting to accomplsih specific purpose i.e. run representing open and close
        /// reverse object or a fake-linebreak lsrun.
        /// 
        /// According to SergeyGe and Antons, LS will never breaks immediately after open reverse
        /// or immediately before close reverse. It also never break before a linebreak character. 
        ///
        /// Therefore, it is safe to make an assumption here that one CP will indeed map to one 
        /// LSCP given being the LSCP before the open reverse and the LSCP after the close reverse.
        /// Never the vice-versa.
        ///
        /// This is the reason why the loop inside this method may overread the PLSRUN span by
        /// one span at the end. The loop is designed to skip over all the lsruns with zero CP
        /// which is not the open reverse.
        /// 
        /// This logic is exactly the same as the one used by FullTextLine.PrefetchLSRuns. Any
        /// attempt to change it needs to be thoroughly reviewed. The same logic is to be applied
        /// accordingly on PrefetchLSRuns.
        /// 
        /// [Wchao, 5-24-2005]
        /// 
        /// </remarks>
        internal int GetBreakpointInternalCp(int cp)
        {
            int ccp = cp - _store.CpFirst;
            int lscp = _store.CpFirst;
            int ccpCurrent = 0;

            SpanVector plsrunVector = _store.PlsrunVector;
            LSRun lsrun;
            int i = 0;

            int lastSpanLength = 0;
            int lastRunLength = 0;

            do
            {
                Span span = plsrunVector[i];
                Plsrun plsrun = (Plsrun)span.element;
                lsrun = _store.GetRun(plsrun);

                if (ccp == ccpCurrent && lsrun.Type == Plsrun.Reverse)
                {
                    break;
                }

                lastSpanLength = span.length;
                lastRunLength = (plsrun >= Plsrun.FormatAnchor ? lsrun.Length : 0);

                lscp += lastSpanLength;
                ccpCurrent += lastRunLength;
} while (   ++i < plsrunVector.Count
                    &&  lsrun.Type != Plsrun.ParaBreak
                    &&  ccp >= ccpCurrent
                );

            // Since we may overread the span vector by one span,
            // we need to subtract the accumulated lscp by the number of cp we may have overread.

            if (ccpCurrent == ccp || lastSpanLength == lastRunLength)
                return lscp - ccpCurrent + ccp;

            Invariant.Assert(ccpCurrent - ccp == lastRunLength);
            return lscp - lastSpanLength;
        }


        /// <summary>
        /// Find the hyphen break following or preceding the specified current LSCP
        /// </summary>
        /// <remarks>
        /// This method never checks whether the specified current LSCP is already right
        /// at a hyphen break. It either finds the next or the previous break regardless.
        /// 
        /// A negative lscchLim param value indicates the caller finds the hyphen immediately
        /// before the specified character index.
        /// </remarks>
        /// <param name="lscpCurrent">the current LSCP</param>
        /// <param name="lscchLim">the number of LSCP to search for break</param>
        /// <param name="isCurrentAtWordStart">flag indicates whether lscpCurrent is the beginning of the word to hyphenate</param>
        /// <param name="lscpHyphen">LSCP of the hyphen</param>
        /// <param name="lshyph">Hyphen properties</param>
        internal bool FindNextHyphenBreak(
            int         lscpCurrent,
            int         lscchLim,
            bool        isCurrentAtWordStart,
            ref int     lscpHyphen,
            ref LsHyph  lshyph
            )
        {
            lshyph = new LsHyph();  // no additional hyphen properties for now

            if (_store.Pap.Hyphenator != null)
            {
                int lscpChunk;
                int lscchChunk;

                LexicalChunk chunk = GetChunk(
                    _store.Pap.Hyphenator,
                    lscpCurrent,
                    lscchLim,
                    isCurrentAtWordStart,
                    out lscpChunk,
                    out lscchChunk
                    );

                _lscpHyphenationLookAhead = lscpChunk + lscchChunk;

                if (!chunk.IsNoBreak)
                {
                    int ichCurrent = chunk.LSCPToCharacterIndex(lscpCurrent - lscpChunk);
                    int ichLim = chunk.LSCPToCharacterIndex(lscpCurrent + lscchLim - lscpChunk);

                    if (lscchLim >= 0)
                    {
                        int ichNext = chunk.Breaks.GetNextBreak(ichCurrent);

                        if (ichNext >= 0 && ichNext > ichCurrent && ichNext <= ichLim)
                        {
                            // -1 because ichNext is the character index where break occurs in front of it,
                            // while LSCP is the position where break occurs after it. 
                            lscpHyphen = chunk.CharacterIndexToLSCP(ichNext - 1) + lscpChunk;
                            return true;
                        }
                    }
                    else
                    {
                        int ichPrev = chunk.Breaks.GetPreviousBreak(ichCurrent);

                        if (ichPrev >= 0 && ichPrev <= ichCurrent && ichPrev > ichLim)
                        {
                            // -1 because ichPrev is the character index where break occurs in front of it,
                            // while LSCP is the position where break occurs after it. 
                            lscpHyphen = chunk.CharacterIndexToLSCP(ichPrev - 1) + lscpChunk;
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Get the lexical chunk the specified current LSCP is within.
        /// </summary>
        private LexicalChunk GetChunk(
            TextLexicalService      lexicalService,
            int                     lscpCurrent,
            int                     lscchLim,
            bool                    isCurrentAtWordStart,
            out int                 lscpChunk,
            out int                 lscchChunk
            )
        {
            int lscpStart = lscpCurrent;
            int lscpLim = lscpStart + lscchLim;

            int cpFirst = _store.CpFirst;

            if (lscpStart > lscpLim)
            {
                // Start is always before limit
                lscpStart = lscpLim;
                lscpLim = lscpCurrent;
            }

            LexicalChunk chunk = new LexicalChunk();
            int cchWordMax;
            CultureInfo textCulture;
            SpanVector<int> textVector;

            char[] rawText = _store.CollectRawWord(
                lscpStart,
                isCurrentAtWordStart,
                _isSideways,
                out lscpChunk,
                out lscchChunk,
                out textCulture,
                out cchWordMax,
                out textVector
                );

            if (    rawText != null
                &&  cchWordMax >= MinCchWordToHyphenate
                &&  lscpLim < lscpChunk + lscchChunk
                &&  textCulture != null 
                &&  lexicalService != null
                &&  lexicalService.IsCultureSupported(textCulture)
                )
            {
                // analyze the chunk and produce the lexical chunk to cache
                TextLexicalBreaks breaks = lexicalService.AnalyzeText(
                    rawText,
                    rawText.Length,
                    textCulture
                    );

                if (breaks != null)
                {
                    chunk = new LexicalChunk(breaks, textVector);
                }
            }

            return chunk;
        }


        /// <summary>
        /// Get a text store containing the specified plsrun
        /// </summary>
        internal TextStore StoreFrom(Plsrun plsrun)
        {
            return TextStore.IsMarker(plsrun) ? _markerStore : _store;
        }


        /// <summary>
        /// Get a text store containing the specified lscp
        /// </summary>
        internal TextStore StoreFrom(int lscp)
        {
            return lscp < 0 ? _markerStore : _store;
        }


        /// <summary>
        /// Flag indicating whether vertical adjustment of some runs is required
        /// </summary>
        internal bool VerticalAdjust
        {
            get { return (_statusFlags & StatusFlags.VerticalAdjust) != 0; }
            set 
            {
                if (value)
                    _statusFlags |= StatusFlags.VerticalAdjust; 
                else
                    _statusFlags &= ~StatusFlags.VerticalAdjust;
            }
        }


        /// <summary>
        /// Flag indicating whether force wrap is required
        /// </summary>
        internal bool ForceWrap
        {
            get { return (_statusFlags & StatusFlags.ForceWrap) != 0; }
            set 
            {
                if (value)
                    _statusFlags |= StatusFlags.ForceWrap; 
                else
                    _statusFlags &= ~StatusFlags.ForceWrap;
            }
        }


        /// <summary>
        /// Flag indicating whether state should be kept in the line
        /// </summary>
        internal bool KeepState
        {
            get { return (_statusFlags & StatusFlags.KeepState) != 0; }
        }


        /// <summary>
        /// Formatting store for main text
        /// </summary>
        internal TextStore TextStore
        {
            get { return _store; }
        }


        /// <summary>
        /// Formatting store for marker text
        /// </summary>
        internal TextStore TextMarkerStore
        {
            get { return _markerStore; }
        }


        /// <summary>
        /// Current formatter
        /// </summary>
        internal TextFormatterImp Formatter
        {
            get { return _store.Settings.Formatter; }
        }


        /// <summary>
        /// formattng ideal width
        /// </summary>
        internal int FormatWidth
        {
            get { return _store.FormatWidth; }
        }
    }
}
