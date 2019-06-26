// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  FullTextLine text store
//
//


using System;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MS.Internal.Shaping;
using MS.Internal.Generic;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// FullTextLine text store
    /// </summary>
    /// <remarks>
    ///
    ///     Text store produces and keeps 'lsrun' of different type. Each type has its own
    ///     characteristics and each lsrun involves with three different 'character length'
    ///     values created and used for different purpose by different component.
    ///
    ///     plsrunSpan.length:  character length created by text store used by LS.
    ///     lsrun.Length:       character length assigned and used by TextSource.
    ///     lsrun.StringLength: character length created by text store used by bidi algorithm.
    ///
    ///
    ///                     plsrunSpan.length  lsrun.Length  lsrun.StringLength
    ///     CloseAnchor             1               0               1
    ///     Reverse                 1               0               1
    ///     FakeLineBreak           1               0               1
    ///     FormatAnchor            1               1               1
    ///     Hidden                  n               n               1
    ///     Text                    n               n               n
    ///     InlineObject            1               n               1
    ///     LineBreak               1               n               1
    ///
    ///     FakeLineBreak is used to chop off a line by simulating a line break when
    ///     none is actually present in the backing store. This is done as a security
    ///     mitigation for malicious text where we might otherwise spend too much time
    ///     formatting a line. The IsForceBreakRequired method contains the mitigation
    ///     logic; see also the comment for MaxCharactersPerLine.
    ///
    /// </remarks>
    internal class TextStore
    {
        private FormatSettings          _settings;                  // format settings
        private int                     _lscpFirstValue;            // first lscp value
        private int                     _cpFirst;                   // store first cp (both cp and lscp start out the same)
        private int                     _lscchUpTo;                 // number of lscp resolved
        private int                     _cchUpTo;                   // number of cp resolved
        private int                     _cchEol;                    // number of chars for end-of-line mark
        private int                     _accNominalWidthSoFar;      // accumulated nominal width so far
        private int                     _accTextLengthSoFar;        // accumulated count of text characters so far
        private NumberContext           _numberContext;             // cached number context for contextual digit substitution
        private int                     _cpNumberContext;           // cp at which _numberContext is valid

        private SpanVector              _plsrunVector;
        private SpanPosition            _plsrunVectorLatestPosition;
        private ArrayList               _lsrunList;                 // lsrun list
        private BidiState               _bidiState;                 // (defer initialization until FetchRun)
        private TextModifierScope       _modifierScope;             // top-most frame of the text modifier stack, or null

        private int                     _formatWidth;               // formatting width LS sees
        private SpanVector              _textObjectMetricsVector;   // inline object cache

        internal static LSRun[]          ControlRuns;               // Control text runs e.g. Bidi reversal


        /// <summary>
        /// Initialize all static members
        /// </summary>
        static TextStore()
        {
            EscStringInfo esc = new EscStringInfo();

            UnsafeNativeMethods.LoGetEscString(ref esc);

            ControlRuns = new LSRun[3];

            ControlRuns[0] = new LSRun(Plsrun.CloseAnchor, esc.szObjectTerminator);
            ControlRuns[1] = new LSRun(Plsrun.Reverse, esc.szObjectReplacement);
            ControlRuns[2] = new LSRun(Plsrun.FakeLineBreak, esc.szLineSeparator);

            PwchNbsp              = esc.szNbsp;
            PwchHidden            = esc.szHidden;
            PwchParaSeparator     = esc.szParaSeparator;
            PwchLineSeparator     = esc.szLineSeparator;
            PwchObjectReplacement = esc.szObjectReplacement;
            PwchObjectTerminator  = esc.szObjectTerminator;
        }


        /// <summary>
        /// Constructing an intermediate text store for FullTextLine
        /// </summary>
        /// <param name="settings">text formatting settings</param>
        /// <param name="cpFirst">first cp of the line</param>
        /// <param name="lscpFirstValue">lscp first value</param>
        /// <param name="formatWidth">formatting width LS sees</param>
        public TextStore(
            FormatSettings          settings,
            int                     cpFirst,
            int                     lscpFirstValue,
            int                     formatWidth
            )
        {
            _settings = settings;
            _formatWidth = formatWidth;

            _cpFirst = cpFirst;
            _lscpFirstValue = lscpFirstValue;

            _lsrunList = new ArrayList(2);
            _plsrunVector = new SpanVector(null);
            _plsrunVectorLatestPosition = new SpanPosition();

            // Recreate the stack of text modifiers if there is one.
            TextLineBreak previousLineBreak = settings.PreviousLineBreak;

            if (    previousLineBreak != null
                &&  previousLineBreak.TextModifierScope != null)
            {
                _modifierScope = previousLineBreak.TextModifierScope.CloneStack();

                // Construct bidi state from input settings and modifier scopes
                _bidiState = new BidiState(_settings, _cpFirst, _modifierScope);
            }
        }


        /// <summary>
        /// Fetch lsrun at the specified LSCP
        /// </summary>
        /// <param name="lscpFetch">lscp to fetch</param>
        /// <param name="textFormattingMode">The layout mode</param>
        /// <param name="isSideways">Whether the text in the run should be sideways</param>
        /// <param name="plsrun">plsrun of lsrun being fetched</param>
        /// <param name="lsrunOffset">offset from the start of the LSRun to the specified lscp</param>
        /// <param name="lsrunLength">distance from the specified lscp to the end of the LSRun</param>
        /// <returns>lsrun being fetched</returns>
        internal LSRun FetchLSRun(
            int             lscpFetch,
            TextFormattingMode  textFormattingMode,
            bool            isSideways,
            out Plsrun      plsrun,
            out int         lsrunOffset,
            out int         lsrunLength
            )
        {
            lscpFetch -= _lscpFirstValue;

            Invariant.Assert(lscpFetch >= _cpFirst);

            if (_cpFirst + _lscchUpTo <= lscpFetch)
            {
                ushort charFlagsSoFar = 0;
                ushort bidiCharFlagsSoFar = 0;
                int cchResolved = 0;
                int cchFetched = _cchUpTo;
                int cch = 0;
                int cchText = 0;

                SpanVector runInfoVector     = new SpanVector(null);
                SpanVector textEffectsVector = new SpanVector(null);
                byte[] bidiLevels = null;
                int lastBidiLevel = GetLastLevel();

                // Fetch runs until enough characters get resolved by bidi algorithm
                do
                {
                    // Read runs up ahead to the point where accumulated run width exceeds the upper limit value.
                    // We do this to optimize the cost of breaking down lsruns by doing as much as we can in
                    // one go, thus generating smaller setup cost of run fetching.

                    TextRunInfo runInfo;

                    do
                    {
                        runInfo = FetchTextRun(_cpFirst + cchFetched);

                        if (runInfo == null)
                        {
                            // no more content to fetch
                            break;
                        }

                        if (  _bidiState == null
                            &&
                               (   IsDirectionalModifier(runInfo.TextRun as TextModifier)
                                || IsEndOfDirectionalModifier(runInfo)
                               )
                            )
                        {
                            // When directional embedding TextModifier or corresponding TextEndOfSegment
                            // is encountered, we need to do bidi analysis to correctly update the bidi state.
                            // We create a bidi state to trigger bidi analysis.
                            _bidiState = new BidiState(_settings, _cpFirst);
                        }

                        int stringLength = runInfo.StringLength;

                        if(runInfo.TextRun is ITextSymbols)
                        {
                            // Let stopMask specify which types of characters need to be isolated in special runs.
                            // Let bidiMask specify which types of characters require bidi analysis.
                            ushort stopMask;
                            ushort bidiMask;

                            if (!runInfo.IsSymbol)
                            {
                                // It's an ordinary Unicode font. Isolate various line breaks and format anchor.
                                stopMask = (ushort)(CharacterAttributeFlags.CharacterLineBreak |
                                        CharacterAttributeFlags.CharacterParaBreak |
                                        CharacterAttributeFlags.CharacterFormatAnchor);

                                // Mask of character flags that require us to perform bidi analysis.
                                bidiMask = (ushort)(CharacterAttributeFlags.CharacterRTL);
                            }
                            else
                            {
                                // It's a non-Unicode font, meaning code points have non-standard meanings. The only
                                // characters we recognize as line breaks are LF (0x0A) and CR (0x0D).
                                stopMask = (ushort)(CharacterAttributeFlags.CharacterCRLF |
                                        CharacterAttributeFlags.CharacterFormatAnchor);

                                // Layout is always left-to-right for non-Unicode fonts.
                                bidiMask = 0;
                            }

                            // Scan until end-of-run or one of the characters specified by stopMask. The accumulated
                            // flags of the characters we advanced past are stored in charFlags.
                            ushort charFlags;
                            stringLength = Classification.AdvanceUntilUTF16(
                                runInfo.CharacterBuffer,
                                runInfo.OffsetToFirstChar,
                                runInfo.Length, // text is chopped at run length not string length
                                stopMask,
                                out charFlags
                                );

                            // If it's a non-Unicode font we may have advanced past line break characters,
                            // but if so we don't want to treat them as such.
                            charFlags &= (ushort)~(CharacterAttributeFlags.CharacterLineBreak | CharacterAttributeFlags.CharacterParaBreak);

                            if(stringLength <= 0)
                            {
                                // There are special characters such as various linebreaks or format anchor
                                // character in the middle of text stream. We isolate such characters into
                                // a separate run and treat them accordingly.

                                runInfo = CreateSpecialRunFromTextContent(runInfo, cchFetched);
                                stringLength = runInfo.StringLength;
                                charFlags = runInfo.CharacterAttributeFlags;

                                Debug.Assert(stringLength > 0 && runInfo.Length > 0);
                            }
                            else if(stringLength != runInfo.Length)
                            {
                                // shorten the run length if the character string is being cut short
                                runInfo.Length = stringLength;
                            }

                            runInfo.CharacterAttributeFlags |= charFlags;
                            charFlagsSoFar |= charFlags;
                            bidiCharFlagsSoFar |= (ushort)(charFlags & bidiMask);
                            cchText += stringLength;
                        }

                        _accNominalWidthSoFar += runInfo.GetRoughWidth(TextFormatterImp.ToIdeal);

                        // store up the run info in a span indexed by actual character index
                        runInfoVector.SetReference(cch, stringLength, runInfo);

                        TextEffectCollection textEffects = (runInfo.Properties != null) ? runInfo.Properties.TextEffects : null;
                        if (textEffects != null && textEffects.Count != 0)
                        {
                            SetTextEffectsVector(textEffectsVector, cch, runInfo, textEffects);
                        }

                        cch += stringLength;

                        cchFetched += runInfo.Length;
} while(
                            _accNominalWidthSoFar < _formatWidth
                        && !runInfo.IsEndOfLine
                        && !IsNewline(charFlagsSoFar)
                        && _accTextLengthSoFar + cchText <= MaxCharactersPerLine
                        );


                    // if bidi is detected, resolve all fetched runs

                    if (   lastBidiLevel > 0
                        || bidiCharFlagsSoFar != 0
                        || _bidiState != null
                       )
                    {
                        cchResolved = BidiAnalyze(runInfoVector, cch, out bidiLevels);

                        // for security reasons, limit how far we'll scan ahead to resolve bidi levels
                        if (cchResolved == 0 && _accTextLengthSoFar + cchText >= MaxCharactersPerLine)
                        {
                            cchResolved = cch;
                            bidiLevels = null;
                        }
                    }
                    else
                    {
                        cchResolved = cch;
                    }
} while(cchResolved <= 0);

                Debug.Assert(
                        runInfoVector != null
                    &&  (   bidiLevels == null
                        ||  cchResolved <= bidiLevels.Length)
                    );

                bool forceBreak = IsForceBreakRequired(runInfoVector, ref cchResolved);

                if(bidiLevels == null)
                {
                    // no bidi detected, all characters are left-to-right
                    CreateLSRunsUniformBidiLevel(
                        runInfoVector,
                        textEffectsVector,
                        _cchUpTo,
                        0,
                        cchResolved,
                        0,  // uniformBidiLevel
                        textFormattingMode,
                        isSideways,
                        ref lastBidiLevel
                        );
                }
                else
                {
                    int runInfoFirstCp = _cchUpTo;
                    int ichUniform = 0;

                    while(ichUniform < cchResolved)
                    {
                        int cchUniform = 1;
                        int uniformBidiLevel = bidiLevels[ichUniform];

                        while(  ichUniform + cchUniform < cchResolved
                            &&  bidiLevels[ichUniform + cchUniform] == uniformBidiLevel)
                        {
                            cchUniform++;
                        }

                        // create lsruns within a range of uniform level
                        CreateLSRunsUniformBidiLevel(
                            runInfoVector,
                            textEffectsVector,
                            runInfoFirstCp,
                            ichUniform,
                            cchUniform,
                            uniformBidiLevel,
                            textFormattingMode,
                            isSideways,
                            ref lastBidiLevel
                            );

                        ichUniform += cchUniform;
                    }
                }

                if (forceBreak)
                {
                    // close reverse runs
                    if (lastBidiLevel != 0)
                    {
                        lastBidiLevel = CreateReverseLSRuns(BaseBidiLevel, lastBidiLevel);
                    }

                    // add a fake linebreak
                    _plsrunVectorLatestPosition = _plsrunVector.SetValue(_lscchUpTo, 1, Plsrun.FakeLineBreak, _plsrunVectorLatestPosition);
                    _lscchUpTo += 1;
                }
            }

            // lsrun at the specified lscp was created, just grab it and go
            return GrabLSRun(
                lscpFetch,
                out plsrun,
                out lsrunOffset,
                out lsrunLength
                );
        }

        /// <summary>
        /// Wrapper to TextRun fetching from the cache
        /// </summary>
        internal TextRunInfo  FetchTextRun(int cpFetch)
        {
            int runLength;
            TextRun textRun;

            // fetch TextRun from the formatting state
            CharacterBufferRange charString = _settings.FetchTextRun(
                cpFetch,
                _cpFirst,
                out textRun,
                out runLength
                );

            CultureInfo digitCulture = null;
            bool contextualSubstitution = false;
            bool symbolTypeface = false;

            Plsrun runType = TextRunInfo.GetRunType(textRun);

            if (runType == Plsrun.Text)
            {
                TextRunProperties properties = textRun.Properties;
                symbolTypeface = properties.Typeface.Symbol;
                if (!symbolTypeface)
                {
                    _settings.DigitState.SetTextRunProperties(properties);
                    digitCulture = _settings.DigitState.DigitCulture;
                    contextualSubstitution = _settings.DigitState.Contextual;
                }
            }

            TextModifierScope currentScope = _modifierScope;
            TextModifier modifier = textRun as TextModifier;

            if (modifier != null)
            {
                _modifierScope = new TextModifierScope(
                    _modifierScope,
                    modifier,
                    cpFetch
                    );

                // The new scope inclues the current TextModifier run
                currentScope = _modifierScope;
            }
            else if (_modifierScope != null && textRun is TextEndOfSegment)
            {
                // The new scope only affects subsequent runs. TextEndOfSegment run itself is
                // still in the old scope such that its coresponding TextModifier run can be tracked.
                _modifierScope = _modifierScope.ParentScope;
            }

            return new TextRunInfo(
                charString,
                runLength,
                cpFetch - _cpFirst, // offsetToFirstCp
                textRun,
                runType,
                0,   // charFlags
                digitCulture,
                contextualSubstitution,
                symbolTypeface,
                currentScope
                );
        }


        /// <summary>
        /// Split a TextRunInfo into multiple ranges each with a uniform set of
        /// TextEffects.
        /// </summary>
        /// <remarks>
        /// A TextRun can have a collection of TextEffect. Each of them can be applied to
        /// an arbitrary range of text. This method breaks the TextRunInfo into sub-ranges
        /// that have identical set of TextEffects. For example
        ///
        /// Current Run :   |----------------------------------------|
        /// Effect 1:     |------------------------------------------------------|
        /// Effect 2:                  |------------------------|
        /// Splitted runs:  |----------|------------------------|----|
        ///
        /// It can be observed that the effected ranges are dividied at the boundaries of the
        /// TextEffects. We sort all the boundaries of TextEffects according to their positions
        /// and create the effected range in between of any two ajacent boundaries. For each efffected
        /// range, we store all the active TextEffect into a list.
        /// </remarks>
        private void SetTextEffectsVector(
            SpanVector              textEffectsVector,
            int                     ich,
            TextRunInfo             runInfo,
            TextEffectCollection    textEffects
            )
        {
            // We already check for empty text effects at the call site.
            Debug.Assert(textEffects != null && textEffects.Count != 0);

            int cpFetched = _cpFirst + _cchUpTo + ich; // get text source character index

            // Offset from client Cp to text effect index.
            int offset = cpFetched - _settings.TextSource.GetTextEffectCharacterIndexFromTextSourceCharacterIndex(cpFetched);

            int textEffectsCount = textEffects.Count;
            TextEffectBoundary[] bounds = new TextEffectBoundary[textEffectsCount * 2];
            for (int i = 0; i < textEffectsCount; i++)
            {
                TextEffect effect = textEffects[i];
                bounds[2 * i] = new TextEffectBoundary(effect.PositionStart, true); // effect starting boundary
                bounds[2 * i + 1] = new TextEffectBoundary(effect.PositionStart + effect.PositionCount, false); // effect end boundary
            }

            Array.Sort(bounds); // sort the TextEffect bounds.

            int effectedRangeStart = Math.Max(cpFetched - offset, bounds[0].Position);
            int effectedRangeEnd   = Math.Min(cpFetched - offset + runInfo.Length, bounds[bounds.Length - 1].Position);

            int currentEffectsCount = 0;
            int currentPosition = effectedRangeStart;
            for (int i = 0; i < bounds.Length && currentPosition < effectedRangeEnd; i++)
            {
                // Have we reached the end of a non-empty subrange with at least one text effect?
                if (currentPosition < bounds[i].Position && currentEffectsCount > 0)
                {
                    // Let [currentPosition,currentRangeEnd) delimit the subrange ending at bounds[i].
                    int currentRangeEnd = Math.Min(bounds[i].Position, effectedRangeEnd);

                    // Consolidate all the active effects in the subrange.
                    IList<TextEffect> activeEffects = new TextEffect[currentEffectsCount];
                    int effectIndex = 0;
                    for (int j = 0; j < textEffectsCount; j++)
                    {
                        TextEffect effect = textEffects[j];
                        if (currentPosition >= effect.PositionStart && currentPosition < (effect.PositionStart + effect.PositionCount))
                        {
                            activeEffects[effectIndex++] = effect;
                        }
                    }

                    Invariant.Assert(effectIndex == currentEffectsCount);

                    // Set the active effects for this CP subrange. The vector index is relative
                    // to the starting cp of the current run-fetching loop.
                    textEffectsVector.SetReference(
                        currentPosition + offset - _cchUpTo - _cpFirst,    // client cp index
                        currentRangeEnd - currentPosition,                 // length
                        activeEffects                                      // text effects
                        );

                    currentPosition = currentRangeEnd;
                }

                // Adjust the current count depending on if it is a TextEffect's starting or ending boundary.
                currentEffectsCount += (bounds[i].IsStart ? 1 : -1);

                if (currentEffectsCount == 0 && i < bounds.Length - 1)
                {
                   // There is no effect on the current position. Move it to the start of next TextEffect.
                   Invariant.Assert(bounds[i + 1].IsStart);
                   currentPosition = Math.Max(currentPosition, bounds[i + 1].Position);
                }
            }
        }

        /// <summary>
        /// Structure representing one boundary of a TextEffect. Each TextEffect has
        /// two boundaries: the beginning and the end.
        /// </summary>
        private struct TextEffectBoundary : IComparable<TextEffectBoundary>
        {
            private readonly int _position;
            private readonly bool _isStart;

            internal TextEffectBoundary(int position, bool isStart)
            {
                _position = position;
                _isStart = isStart;
            }

            internal int Position
            {
                get { return _position; }
            }

            internal bool IsStart
            {
                get { return _isStart; }
            }

            public int CompareTo(TextEffectBoundary other)
            {
                if (Position != other.Position)
                    return Position - other.Position;

                if (IsStart == other.IsStart) return 0;

                // Starting edge is always in front.
                return IsStart ? -1 : 1;
            }
        }


        /// <summary>
        /// Create special run that matches the content of specified text run
        /// </summary>
        private TextRunInfo CreateSpecialRunFromTextContent(
            TextRunInfo     runInfo,
            int             cchFetched
            )
        {
            // -FORMAT ANCHOR-
            //
            // Format anchor character is what we create internally to drive LS. If it
            // is present in the middle of text stream sent from the client, we will
            // have to filter it out and replace it with NBSP. This is to protect LS
            // from running into a bad state due to misinterpreting such character as
            // our format anchor. Following is the list of anchor character we use todate.
            //
            //      "\uFFFB" (Unicode 'Annotation Terminator')
            //
            // -LINEBREAK-
            //
            // Following the Unicode guideline on newline characters, we recognize
            // both LS (U+2028) and PS (U+2029) as explicit linebreak (PS also breaks
            // paragraph but that's handled outside line level formatting). We also
            // treat the following sequence of characters as linebreak
            //
            //      "CR"    ("\u000D")
            //      "LF"    ("\u000A")
            //      "CRLF"  ("\u000D\u000A")
            //      "NEL"   ("\u0085")
            //      "VT"    ("\u000B")
            //      "FF"    ("\u000C")
            //
            // Note: http://www.unicode.org/unicode/reports/tr13/tr13-9.html
            Debug.Assert(runInfo.StringLength > 0 && runInfo.Length > 0);

            CharacterBuffer charBuffer = runInfo.CharacterBuffer;
            int offsetToFirstChar = runInfo.OffsetToFirstChar;
            char firstChar = charBuffer[offsetToFirstChar];
            ushort charFlags;

            charFlags = (ushort)Classification.CharAttributeOf(
                (int)Classification.GetUnicodeClassUTF16(firstChar)
                ).Flags;

            if ((charFlags & (ushort)CharacterAttributeFlags.CharacterLineBreak) != 0)
            {
                // Get cp length of newline sequence
                //
                // It is possible that client run ends in between two codepoints that
                // make up a single newline sequence e.g. CRLF. Therefore, when we
                // encounter the first codepoint of the sequence, we need to make sure
                // we have enough codepoints to determine the correct whole sequence.
                // In an uncommon event, we may be forced to look ahead by fetching more
                // runs.

                int newlineLength = 1;  // most sequences take one cp

                if (firstChar == '\r')
                {
                    if (runInfo.Length > 1)
                    {
                        newlineLength += ((charBuffer[offsetToFirstChar + 1] == '\n') ? 1 : 0);
                    }
                    else
                    {
                        TextRunInfo nextRunInfo = FetchTextRun(_cpFirst + cchFetched + 1);

                        if (nextRunInfo != null && nextRunInfo.TextRun is ITextSymbols)
                        {
                            newlineLength += ((nextRunInfo.CharacterBuffer[nextRunInfo.OffsetToFirstChar] == '\n') ? 1 : 0);
                        }
                    }
                }

                unsafe
                {
                    runInfo = new TextRunInfo(
                        new CharacterBufferRange((char*)PwchLineSeparator, 1),
                        newlineLength, // run length
                        runInfo.OffsetToFirstCp,
                        runInfo.TextRun,
                        Plsrun.LineBreak, // LineBreak run
                        charFlags,
                        null,  // digit culture
                        false, // contextual substitution
                        false, // is not Unicode
                        runInfo.TextModifierScope
                        );
                }
            }
            else if ((charFlags & (ushort)CharacterAttributeFlags.CharacterParaBreak) != 0)
            {
                unsafe
                {
                    // This character is a paragraph separator. Split it into a
                    // separate run.
                    runInfo = new TextRunInfo(
                        new CharacterBufferRange((char*)PwchParaSeparator, 1),
                        1,
                        runInfo.OffsetToFirstCp,
                        runInfo.TextRun,
                        Plsrun.ParaBreak,  // parabreak run
                        charFlags,
                        null,   // digit culture
                        false,  // contextual substitution
                        false,  // is not Unicode
                        runInfo.TextModifierScope
                        );
                }
            }
            else
            {
                Invariant.Assert((charFlags & (ushort)CharacterAttributeFlags.CharacterFormatAnchor) != 0);

                unsafe
                {
                    runInfo = new TextRunInfo(
                        new CharacterBufferRange((char*)PwchNbsp, 1),
                        1, // run length
                        runInfo.OffsetToFirstCp,
                        runInfo.TextRun,
                        runInfo.Plsrun,
                        charFlags,
                        null,   // digit culture
                        false,  // contextual substitution
                        false,  // is not Unicode
                        runInfo.TextModifierScope
                        );
                }
            }

            return runInfo;
        }


        /// <summary>
        /// Grab existing lsrun at specified LSCP
        /// </summary>
        private LSRun GrabLSRun(
            int             lscpFetch,
            out Plsrun      plsrun,
            out int         lsrunOffset,
            out int         lsrunLength
            )
        {
            int offsetToFirstCp = lscpFetch - _cpFirst;

            SpanRider rider = new SpanRider(_plsrunVector, _plsrunVectorLatestPosition, offsetToFirstCp);
            _plsrunVectorLatestPosition = rider.SpanPosition;
            plsrun = (Plsrun)rider.CurrentElement;

            LSRun lsrun;
            if (plsrun < Plsrun.FormatAnchor)
            {
                lsrun = ControlRuns[(int)plsrun];
                lsrunOffset = 0;
            }
            else
            {
                lsrun = (LSRun)_lsrunList[(int)(ToIndex(plsrun) - Plsrun.FormatAnchor)];
                lsrunOffset = offsetToFirstCp - rider.CurrentSpanStart;
            }

            if (_lscpFirstValue != 0)
            {
                // this is marker store, differentiate the plsrun from
                // plsrun from the main text store.
                plsrun = MakePlsrunMarker(plsrun);
            }

            // SpanRider.Length yields the distance to the end of the Span, not
            // the total length of the Span.
            lsrunLength = rider.Length;

            return lsrun;
        }


        /// <summary>
        /// Get the Bidi level of the character before the currently fetched one
        /// </summary>
        private int GetLastLevel()
        {
            if (_lscchUpTo > 0)
            {
                SpanRider rider = new SpanRider(_plsrunVector, _plsrunVectorLatestPosition, _lscchUpTo - 1);
                _plsrunVectorLatestPosition = rider.SpanPosition;
                return GetRun((Plsrun)rider.CurrentElement).BidiLevel;
            }
            return BaseBidiLevel;
        }


        /// <summary>
        /// Base bidi level
        /// </summary>
        private int BaseBidiLevel
        {
            get { return _settings.Pap.RightToLeft ? 1 : 0; }
        }

        /// <summary>
        /// Analyze bidirectional level of runs
        /// </summary>
        /// <param name="runInfoVector">run info vector indexed by ich</param>
        /// <param name="stringLength">character length of string to be analyzed</param>
        /// <param name="bidiLevels">array of bidi levels, each for a character</param>
        /// <returns>Number of characters resolved</returns>
        /// <remarks>
        /// BiDi Analysis in line layout imposes a higher level protocol on top of Unicode bidi algorithm
        /// to support rich editing behavior. Explicit directional embedding controls is to be done
        /// through TextModifier runs and corresponding TextEndOfSegment. Directional controls (such as
        /// LRE, RLE, PDF, etc) in the text stream are ignored in the Bidi Analysis to avoid conflict with the higher
        /// level protocol.
        ///
        /// The implementation analyzes directional embedding one level at a time. Input text runs are divided
        /// at the point where directional embedding level is changed.
        /// </remarks>
        private int BidiAnalyze(
            SpanVector                  runInfoVector,
            int                         stringLength,
            out byte[]                  bidiLevels
            )
        {
            CharacterBuffer charBuffer = null;
            int offsetToFirstChar;

            SpanRider runInfoSpanRider = new SpanRider(runInfoVector);
            if (runInfoSpanRider.Length >= stringLength)
            {
                // typical case, only one string is analyzed
                TextRunInfo runInfo = (TextRunInfo)runInfoSpanRider.CurrentElement;

                if (!runInfo.IsSymbol)
                {
                    charBuffer = runInfo.CharacterBuffer;
                    offsetToFirstChar = runInfo.OffsetToFirstChar;
                    Debug.Assert(runInfo.StringLength >= stringLength);
                }
                else
                {
                    // Treat all characters in non-Unicode runs as strong left-to-right.
                    // The literal 'A' could be any Latin character.
                    charBuffer = new StringCharacterBuffer(new string('A', stringLength));
                    offsetToFirstChar = 0;
                }
            }
            else
            {
                // build up a consolidated character buffer for bidi analysis of
                // concatenated strings in multiple textruns.
                int ich = 0;
                int cch;

                StringBuilder stringBuilder = new StringBuilder(stringLength);

                while(ich < stringLength)
                {
                    runInfoSpanRider.At(ich);
                    cch = runInfoSpanRider.Length;
                    TextRunInfo runInfo = (TextRunInfo)runInfoSpanRider.CurrentElement;

                    Debug.Assert(cch <= runInfo.StringLength);

                    if (!runInfo.IsSymbol)
                    {
                        runInfo.CharacterBuffer.AppendToStringBuilder(
                            stringBuilder,
                            runInfo.OffsetToFirstChar,
                            cch
                            );
                    }
                    else
                    {
                        // Treat all characters in non-Unicode runs as strong left-to-right.
                        // The literal 'A' could be any Latin character.
                        stringBuilder.Append('A', cch);
                    }

                    ich += cch;
                }

                charBuffer = new StringCharacterBuffer(stringBuilder.ToString());
                offsetToFirstChar = 0;
            }

            if(_bidiState == null)
            {
                // make sure the initial state is setup
                _bidiState = new BidiState(_settings, _cpFirst);
            }

            bidiLevels = new byte[stringLength];
            DirectionClass[] directionClasses = new DirectionClass[stringLength];

            int resolvedLength = 0;

            for(int i = 0; i < runInfoVector.Count; i++)
            {
                int cchResolved = 0;

                TextRunInfo currentRunInfo = (TextRunInfo) runInfoVector[i].element;
                TextModifier modifier = currentRunInfo.TextRun as TextModifier;

                if (IsDirectionalModifier(modifier))
                {
                    bidiLevels[resolvedLength] = AnalyzeDirectionalModifier(_bidiState, modifier.FlowDirection);
                    cchResolved = 1;
                }
                else if (IsEndOfDirectionalModifier(currentRunInfo))
                {
                    bidiLevels[resolvedLength] = AnalyzeEndOfDirectionalModifier(_bidiState);
                    cchResolved = 1;
                }
                else
                {
                    int ich = resolvedLength;
                    do
                    {
                        CultureInfo culture = CultureMapper.GetSpecificCulture(currentRunInfo.Properties == null ? null : currentRunInfo.Properties.CultureInfo);
                        DirectionClass europeanNumberOverride = _bidiState.GetEuropeanNumberClassOverride(culture);

                        //
                        // The European number in the input text is explictly set to AN or EN base on the
                        // culture of the text. We set the input DirectionClass of this range of text to
                        // AN or EN to indicate that any EN in this range should be explicitly set to this override
                        // value.
                        //
                        for(int k = 0; k < runInfoVector[i].length; k++)
                        {
                            directionClasses[ich + k] = europeanNumberOverride;
                        }

                        ich += runInfoVector[i].length;
                        if ((++i) >= runInfoVector.Count)
                            break; // end of all runs.

                        currentRunInfo = (TextRunInfo) runInfoVector[i].element;
                        if ( currentRunInfo.Plsrun == Plsrun.Hidden &&
                              (  IsDirectionalModifier(currentRunInfo.TextRun as TextModifier)
                              || IsEndOfDirectionalModifier(currentRunInfo)
                              )
                           )
                        {
                            i--;
                            break;   // break bidi analysis at the point of embedding level change
                        }
                    }
                    while (true);

                    const Bidi.Flags BidiFlags = Bidi.Flags.ContinueAnalysis | Bidi.Flags.IgnoreDirectionalControls | Bidi.Flags.OverrideEuropeanNumberResolution;

                    // The last runs will be marked as IncompleteText as their resolution
                    // may depend on following runs that haven't been fetched yet.
                    Bidi.Flags flags = (i < runInfoVector.Count) ?
                            BidiFlags
                          : BidiFlags | Bidi.Flags.IncompleteText;


                    Bidi.BidiAnalyzeInternal(
                        charBuffer,
                        offsetToFirstChar + resolvedLength,
                        ich - resolvedLength,
                        0, // no max hint
                        flags,
                        _bidiState,
                        new PartialArray<byte>(bidiLevels, resolvedLength, ich - resolvedLength),
                        new PartialArray<DirectionClass>(directionClasses, resolvedLength, ich - resolvedLength),
                        out cchResolved
                        );

                    // Text must be completely resolved if there is no IncompleteText flag.
                    Invariant.Assert(cchResolved == ich - resolvedLength || (flags & Bidi.Flags.IncompleteText) != 0);
                }

                resolvedLength += cchResolved;
            }

            Invariant.Assert(resolvedLength <= bidiLevels.Length);
            return resolvedLength;
        }

        /// <summary>
        /// Update BidiState base to the new directional embedding level.
        /// </summary>
        /// <returns>
        /// The method returns the embedding level before the start of the Modifier.
        /// Contents inside the modifier scope is at a higher embedding level and hence
        /// separated from the content before the modifier scope.
        /// </returns>
        private byte AnalyzeDirectionalModifier(
            BidiState       state,
            FlowDirection   flowDirection
            )
        {
            bool leftToRight = (flowDirection == FlowDirection.LeftToRight);

            ulong levelStack = state.LevelStack;

            byte parentLevel = Bidi.BidiStack.GetMaximumLevel(levelStack);

            byte topLevel;

            // Push to Bidi stack. Increment overflow counter if so.
            if (!Bidi.BidiStack.Push(ref levelStack, leftToRight, out topLevel))
            {
                state.Overflow++;
            }

            state.LevelStack = levelStack;

            // set the default last strong such that text without CultureInfo
            // can be resolved correctly.
            state.SetLastDirectionClassesAtLevelChange();
            return parentLevel;
        }

        /// <summary>
        /// Update BidiState at the end of a directional emebedding level.
        /// </summary>
        /// <returns>
        /// The method returns the embedding level after the end of the modifier.
        /// Contents inside the modifier scope is at a higher embedding level and hence separated
        /// from the content after the modifier scope.
        /// </returns>
        private byte AnalyzeEndOfDirectionalModifier(BidiState state)
        {
            // Pop level stack
            if (state.Overflow > 0)
            {
                state.Overflow --;
                return state.CurrentLevel;
            }

            byte parentLevel;
            ulong stack = state.LevelStack;

            bool success = Bidi.BidiStack.Pop(ref stack, out parentLevel);
            Invariant.Assert(success);
            state.LevelStack = stack;

            // set the default last strong such that text without CultureInfo
            // can be resolved correctly.
            state.SetLastDirectionClassesAtLevelChange();
            return parentLevel;
        }

        private bool IsEndOfDirectionalModifier(TextRunInfo runInfo)
        {
            return (  runInfo.TextModifierScope != null
                   && runInfo.TextModifierScope.TextModifier.HasDirectionalEmbedding
                   && runInfo.TextRun is TextEndOfSegment
                   );
        }

        private bool IsDirectionalModifier(TextModifier modifier)
        {
            return modifier != null && modifier.HasDirectionalEmbedding;
        }

        internal bool InsertFakeLineBreak(int cpLimit)
        {
            for (int i = 0, cp = 0, lscp = 0; i < _plsrunVector.Count; ++i)
            {
                Span span = _plsrunVector[i];
                Plsrun plsrun = (Plsrun)span.element;

                // Is it a normal, non-static, LSRun?
                if (plsrun >= Plsrun.FormatAnchor)
                {
                    // Get the run.
                    LSRun lsrun = GetRun(plsrun);

                    // Have we reached the limit?
                    if (cp + lsrun.Length >= cpLimit)
                    {
                        // Remove all subsequent runs.
                        _plsrunVector.Delete(i + 1, _plsrunVector.Count - (i + 1), ref _plsrunVectorLatestPosition);

                        // Truncate the run if it exeeds the limit.
                        if (lsrun.Type == Plsrun.Text && cp + lsrun.Length > cpLimit)
                        {
                            lsrun.Truncate(cpLimit - cp);
                            span.length = lsrun.Length;
                        }

                        _lscchUpTo = lscp + lsrun.Length;

                        // Close any reverse runs.
                        CreateReverseLSRuns(BaseBidiLevel, lsrun.BidiLevel);

                        // Add the fake line break.
                        _plsrunVectorLatestPosition = _plsrunVector.SetValue(_lscchUpTo, 1, Plsrun.FakeLineBreak, _plsrunVectorLatestPosition);
                        _lscchUpTo += 1;

                        return true;
                    }

                    cp += lsrun.Length;
                }

                lscp += span.length;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a line needs to be truncated for security reasons due to exceeding
        /// the maximum number of characters per line. See the comment for MaxCharactersPerLine.
        /// </summary>
        /// <param name="runInfoVector">Vector of fetched text runs.</param>
        /// <param name="cchToAdd">Number of cp to be added to _plsrunVector; the method
        /// may change this value if the line needs to be truncated.</param>
        /// <returns>Returns true if the line should be truncated, false it not.</returns>
        private bool IsForceBreakRequired(SpanVector runInfoVector, ref int cchToAdd)
        {
            bool forceBreak = false;
            int ichRun = 0;

            for (int i = 0; i < runInfoVector.Count && ichRun < cchToAdd; ++i)
            {
                Span span = runInfoVector[i];
                TextRunInfo runInfo = (TextRunInfo)span.element;

                int runLength = Math.Min(span.length, cchToAdd - ichRun);

                // Only Plsrun.Text runs count against the limit
                if (runInfo.Plsrun == Plsrun.Text && !IsNewline((ushort)runInfo.CharacterAttributeFlags))
                {
                    if (_accTextLengthSoFar + runLength <= MaxCharactersPerLine)
                    {
                        // we're still under the limit; accumulate the number of characters so far
                        _accTextLengthSoFar += runLength;
                    }
                    else
                    {
                        // accumulated number of characters has exceeded the maximum allowed number
                        // of characters per line; we need to generate a fake line break
                        runLength = MaxCharactersPerLine - _accTextLengthSoFar;
                        _accTextLengthSoFar = MaxCharactersPerLine;
                        cchToAdd = ichRun + runLength;
                        forceBreak = true;
                    }
                }

                ichRun += runLength;
            }

            return forceBreak;
        }

        [Flags]
        private enum NumberContext
        {
            Unknown             = 0,

            Arabic              = 0x0001,
            European            = 0x0002,
            Mask                = 0x0003,

            FromLetter          = 0x0004,
            FromFlowDirection   = 0x0008
        }

        private NumberContext GetNumberContext(TextModifierScope scope)
        {
            int limitCp = _cpFirst + _cchUpTo;
            int firstCp = _cpNumberContext;
            NumberContext cachedNumberContext = _numberContext;

            // Is there a current bidi scope?
            for (; scope != null; scope = scope.ParentScope)
            {
                if (scope.TextModifier.HasDirectionalEmbedding)
                {
                    int cpScope = scope.TextSourceCharacterIndex;
                    if (cpScope >= _cpNumberContext)
                    {
                        // Only scan back to the start of the current scope and don't use the cached number
                        // context since it's outside the current scope.
                        firstCp = cpScope;
                        cachedNumberContext = NumberContext.Unknown;
                    }
                    break;
                }
            }

            // Is it a right to left context?
            bool rightToLeft = (scope != null) ?
                scope.TextModifier.FlowDirection == FlowDirection.RightToLeft :
                Pap.RightToLeft;

            // Scan for a preceding letter.
            while (limitCp > firstCp)
            {
                TextSpan<CultureSpecificCharacterBufferRange> textSpan = _settings.GetPrecedingText(limitCp);

                // Stop if there's an empty TextSpan
                if (textSpan.Length <= 0)
                {
                    break;
                }

                CharacterBufferRange charRange = textSpan.Value.CharacterBufferRange;
                if (!charRange.IsEmpty)
                {
                    CharacterBuffer charBuffer = charRange.CharacterBuffer;

                    // Index just past the last character in the range.
                    int limit = charRange.OffsetToFirstChar + charRange.Length;

                    // Index of the first character in the range, not including any characters before firstCp.
                    int first = limit - Math.Min(charRange.Length, limitCp - firstCp);

                    // We'll stop scanning at letter or line break.
                    const ushort flagsMask =
                        (ushort)CharacterAttributeFlags.CharacterLetter |
                        (ushort)CharacterAttributeFlags.CharacterLineBreak;

                    // Iterate over the characters in reverse order.
                    for (int i = limit - 1; i >= first; --i)
                    {
                        char ch = charBuffer[i];
                        CharacterAttribute charAttributes = Classification.CharAttributeOf(Classification.GetUnicodeClassUTF16(ch));

                        ushort flags = (ushort)(charAttributes.Flags & flagsMask);
                        if (flags != 0)
                        {
                            if ((flags & (ushort)CharacterAttributeFlags.CharacterLetter) != 0)
                            {
                                // It's a letter so the number context depends on its script.
                                return (charAttributes.Script == (byte)ScriptID.Arabic || charAttributes.Script == (byte)ScriptID.Syriac) ?
                                    NumberContext.Arabic | NumberContext.FromLetter :
                                    NumberContext.European | NumberContext.FromLetter;
                            }
                            else
                            {
                                // It's a line break. There are no preceding letters so number context depends only on
                                // whether the current bidi scope is right to left.
                                return rightToLeft ?
                                    NumberContext.Arabic | NumberContext.FromFlowDirection :
                                    NumberContext.European | NumberContext.FromFlowDirection;
                            }
                        }
                    }
                }

                limitCp -= textSpan.Length;
            }

            // If we have a cached number context that's still valid the use it. Valid means (1) we
            // scanned back as far as the cp of the number context, and (2) the number context was
            // determined from a letter. (A cached number context derived from flow direction might
            // not be valid because an embedded bidi level may have ended.)
            if (limitCp <= firstCp && (cachedNumberContext & NumberContext.FromLetter) != 0)
            {
                return cachedNumberContext;
            }

            // There are no preceding letters so number context depends only on whether the current
            // bidi scope is right to left.
            return rightToLeft ?
                NumberContext.Arabic | NumberContext.FromFlowDirection :
                NumberContext.European | NumberContext.FromFlowDirection;
        }

        /// <summary>
        /// Create lsruns within a range of uniform bidi level.
        /// </summary>
        private void CreateLSRunsUniformBidiLevel(
            SpanVector              runInfoVector,
            SpanVector              textEffectsVector,
            int                     runInfoFirstCp,
            int                     ichUniform,
            int                     cchUniform,
            int                     uniformBidiLevel,
            TextFormattingMode          textFormattingMode,
            bool                    isSideways,
            ref int                 lastBidiLevel
            )
        {
            int ichRun = 0;

            // a range of characters with uniform level may span multiple
            // textruns. Create lsrun at runInfo boundary.

            SpanRider runInfoSpanRider = new SpanRider(runInfoVector);
            SpanRider textEffectsSpanRider = new SpanRider(textEffectsVector);

            while(ichRun < cchUniform)
            {
                runInfoSpanRider.At(ichUniform + ichRun);
                textEffectsSpanRider.At(ichUniform + ichRun);

                // Limit the span base on effected ranges.
                int spanLength = Math.Min(runInfoSpanRider.Length, textEffectsSpanRider.Length);
                int ichEnd = Math.Min(ichRun + spanLength, cchUniform);

                int textRunLength;

                TextRunInfo runInfo = (TextRunInfo)runInfoSpanRider.CurrentElement;
                IList<TextEffect> textEffects = (IList<TextEffect>)textEffectsSpanRider.CurrentElement;

                // Initialize digitCulture only if there are digits.
                CultureInfo digitCulture = null;

                // Number context; used only if we do contextual digit substitution.
                NumberContext numberContext = NumberContext.Unknown;

                if ((runInfo.CharacterAttributeFlags & (ushort)CharacterAttributeFlags.CharacterDigit) == 0)
                {
                    // No digits so digitCulture isn't used.
                }
                else if (!runInfo.ContextualSubstitution)
                {
                    // Render all numbers using the digit culture of the run.
                    digitCulture = runInfo.DigitCulture;
                }
                else
                {
                    // Contextual number substitution means the digit culture of a given number depends on the
                    // nearest preceding letter. If it's an Arabic letter we use the digit culture of the run;
                    // otherwise we use European digits (null digit culture).

                    // Number context of the previous number, if any.
                    NumberContext previousNumberContext = NumberContext.Unknown;

                    CharacterBuffer charBuffer = runInfo.CharacterBuffer;

                    // The character indexes ichRun, ich, etc., are relative to the start of the uniform range;
                    // In order to yield an index into charBuffer, we need to calculate the offset from the
                    // start of the uniform range to the start of the character buffer.
                    //
                    //
                    // _cpFirst
                    //    |----_cchUpTo---->|-----------------runInfoVector----------------------->|
                    //                      |
                    //                      |--ichUniform-->|----ich------>|
                    //                      |               |              |
                    //                      |-----CurrentSpanStart-->|=====x=====runInfo========|
                    //                                      |        |     |
                    // charBuffer=> [---runInfo.OffsetToFirstChar--->|-----x----------------------------]
                    //              |                       |              |

                    //              |---characterOffset---->|----ich------>|
                    //

                    int characterOffset =
                        ichUniform                           // start of the uniform range
                        - runInfoSpanRider.CurrentSpanStart  // make relative to the the start of the runInfo
                        + runInfo.OffsetToFirstChar;         // make relative to the start of the character buffer in runInfo

                    for (int ich = ichRun; ich < ichEnd; ++ich)
                    {
                        char ch = charBuffer[ich + characterOffset];
                        CharacterAttribute charAttributes = Classification.CharAttributeOf(Classification.GetUnicodeClassUTF16(ch));

                        if ((charAttributes.Flags & (ushort)CharacterAttributeFlags.CharacterDigit) != 0)
                        {
                            // If there were no preceding letters in the current run we need to scan backwards to
                            // determine the current number context.
                            if (numberContext == NumberContext.Unknown)
                            {
                                numberContext = GetNumberContext(runInfo.TextModifierScope);
                            }

                            // We need to set the digit culture if
                            //   (a) we haven't set it yet (i.e., this is the first number) or
                            //   (b) we set it but the previous number had a different number context
                            if ((previousNumberContext & NumberContext.Mask) != (numberContext & NumberContext.Mask))
                            {
                                // If there was a previous number with a different digit culture we need to split the run.
                                if (previousNumberContext != NumberContext.Unknown)
                                {
                                    CreateLSRuns(
                                        runInfo,
                                        textEffects,
                                        digitCulture,
                                        ichUniform + ichRun - runInfoSpanRider.CurrentSpanStart,
                                        ich - ichRun,
                                        uniformBidiLevel,
                                        textFormattingMode,
                                        isSideways,
                                        ref lastBidiLevel,
                                        out textRunLength
                                        );
                                    _cchUpTo += textRunLength;
                                    ichRun = ich;
                                }

                                // Set the digitCulture to use for this and subsequent characters.
                                digitCulture = (numberContext & NumberContext.Mask) == NumberContext.Arabic ?
                                    runInfo.DigitCulture :      // subsequent digits use Arabic symbols
                                    null;                       // subsequent digits use European symbols

                                previousNumberContext = numberContext;
                            }
                        }
                        else if ((charAttributes.Flags & (ushort)CharacterAttributeFlags.CharacterLetter) != 0)
                        {
                            // It's a letter so set the current number context based on the letter's script.
                            // Don't set the digit culture until we actually encounter a number.
                            numberContext = (charAttributes.Script == (byte)ScriptID.Arabic || charAttributes.Script == (byte)ScriptID.Syriac) ?
                                NumberContext.Arabic | NumberContext.FromLetter :
                                NumberContext.European | NumberContext.FromLetter;
                        }
                    }
                }

                // Even if we split the run we still have to add the last part.
                Debug.Assert(ichRun < ichEnd);

                // Add the run (or what's left of it).
                CreateLSRuns(
                    runInfo,
                    textEffects,
                    digitCulture,
                    ichUniform + ichRun - runInfoSpanRider.CurrentSpanStart,
                    ichEnd - ichRun,
                    uniformBidiLevel,
                    textFormattingMode,
                    isSideways,
                    ref lastBidiLevel,
                    out textRunLength
                    );
                _cchUpTo += textRunLength;
                ichRun = ichEnd;

                // Save the number of context if known. This reduces the number of calls to GetPrecedingText for
                // lines with more than one number. We do this now, after calling CreateLSRuns, so that _cchUpTo
                // holds the correct cp that corresponds to all the characters scanned so far.
                if (numberContext != NumberContext.Unknown)
                {
                    _numberContext = numberContext;
                    _cpNumberContext = _cpFirst + _cchUpTo;
                }
            }

            Debug.Assert(ichRun == cchUniform);
        }



        /// <summary>
        /// Create reverse lsruns
        /// </summary>
        /// <param name="currentBidiLevel">current bidi level</param>
        /// <param name="lastBidiLevel">last bidi level</param>
        /// <returns>updated last bidi Level</returns>
        private int CreateReverseLSRuns(
            int     currentBidiLevel,
            int     lastBidiLevel
            )
        {
            Plsrun plsrun;
            int levelDiff = currentBidiLevel - lastBidiLevel;

            if(levelDiff > 0)
            {
                // level up
                plsrun = Plsrun.Reverse;
            }
            else
            {
                // level down
                plsrun = Plsrun.CloseAnchor;
                levelDiff = -levelDiff;
            }

            for(int i = 0; i < levelDiff; i++)
            {
                _plsrunVectorLatestPosition = _plsrunVector.SetValue(_lscchUpTo, 1, plsrun, _plsrunVectorLatestPosition);
                _lscchUpTo++;
            }
            return currentBidiLevel;
        }



        /// <summary>
        /// Create lsrun(s)
        /// </summary>
        /// <param name="runInfo">run info</param>
        /// <param name="textEffects">The applicable TextEffects on the LSRun. </param>
        /// <param name="digitCulture">digit culture for number substitution</param>
        /// <param name="offsetToFirstChar">offset the first char from start of run info</param>
        /// <param name="stringLength">lsrun character length</param>
        /// <param name="uniformBidiLevel">uniform bidi level</param>
        /// <param name="textFormattingMode">The layout mode</param>
        /// <param name="isSideways">Whether the text in the run should be sideways</param>
        /// <param name="lastBidiLevel">last bidi level</param>
        /// <param name="textRunLength">text run length</param>
        private void CreateLSRuns(
            TextRunInfo       runInfo,
            IList<TextEffect> textEffects,
            CultureInfo       digitCulture,
            int               offsetToFirstChar,
            int               stringLength,
            int               uniformBidiLevel,
            TextFormattingMode    textFormattingMode,
            bool              isSideways,
            ref int           lastBidiLevel,
            out int           textRunLength
            )
        {
            LSRun lsrun = null;
            int lsrunLength = 0;
            textRunLength = 0;

            switch (runInfo.Plsrun)
            {
                case Plsrun.Text:
                {
                    ushort charFlags = (ushort)runInfo.CharacterAttributeFlags;

                    // LineBreak & ParaBreak are separated into individual TextRunInfo with Plsrun.LineBreak or Plsrun.ParaBreak.
                    Invariant.Assert(!IsNewline(charFlags));

                    if ((charFlags & (ushort)CharacterAttributeFlags.CharacterFormatAnchor) != 0)
                    {
                        lsrun = new LSRun(
                            runInfo,
                            Plsrun.FormatAnchor,
                            PwchNbsp,
                            1,
                            runInfo.OffsetToFirstCp,
                            (byte) uniformBidiLevel
                            );

                        lsrunLength = textRunLength = lsrun.StringLength;
                    }
                    else
                    {
                        // Normal text, run length is character length

                        textRunLength = lsrunLength = stringLength;
                        Debug.Assert(runInfo.OffsetToFirstChar + offsetToFirstChar + lsrunLength <= runInfo.CharacterBuffer.Count);

                        CreateTextLSRuns(
                            runInfo,
                            textEffects,
                            digitCulture,
                            offsetToFirstChar,
                            stringLength,
                            uniformBidiLevel,
                            textFormattingMode,
                            isSideways,
                            ref lastBidiLevel
                            );
                    }

                    break;
                }

                case Plsrun.InlineObject:
                {
                    Debug.Assert(offsetToFirstChar == 0);

                    double realToIdeal = TextFormatterImp.ToIdeal;

                    lsrun = new LSRun(
                        runInfo,
                        textEffects,
                        Plsrun.InlineObject,
                        runInfo.OffsetToFirstCp,
                        runInfo.Length,
                        (int)Math.Round(realToIdeal * runInfo.TextRun.Properties.FontRenderingEmSize),
                        0,          // character flags
                        new CharacterBufferRange(runInfo.CharacterBuffer, 0, stringLength),
                        null,       // no shapeable
                        realToIdeal,
                        (byte)uniformBidiLevel
                        );

                    lsrunLength = stringLength;
                    textRunLength = runInfo.Length;
                    break;
                }

                case Plsrun.LineBreak:
                {
                    //
                    // Line Separator's BIDI class is Neutral (WS). It would take the class of surrounding
                    // characters so it might end up in a reverse run. However, LS would not process Line Separator
                    // in reverse run. Here we always override the BIDI level of Line Separator to paragraph's
                    // embedding level such that it is out of reverse run and LS would process it correctly.
                    //
                    uniformBidiLevel = (Pap.RightToLeft ? 1 : 0);
                    lsrun = CreateLineBreakLSRun(
                        runInfo,
                        stringLength,
                        out lsrunLength,
                        out textRunLength
                        );
                    break;
                }

                case Plsrun.ParaBreak:
                {
                    //
                    // Paragraph Separator ends the paragraph. Its bidi level must be the embedding level.
                    //
                    Debug.Assert(uniformBidiLevel == (Pap.RightToLeft ? 1 : 0));
                    lsrun = CreateLineBreakLSRun(
                        runInfo,
                        stringLength,
                        out lsrunLength,
                        out textRunLength
                        );
                    break;
                }
                case Plsrun.Hidden:
                {
                    // hidden run yields the same cp as its lscp
                    lsrunLength = runInfo.Length - offsetToFirstChar;
                    textRunLength = lsrunLength;
                    lsrun = new LSRun(
                        runInfo,
                        Plsrun.Hidden,
                        PwchHidden,
                        textRunLength,
                        runInfo.OffsetToFirstCp,
                        (byte) uniformBidiLevel
                        );

                    break;
                }
            }

            if(lsrun != null)
            {
                Debug.Assert(lsrunLength > 0);

                if (lastBidiLevel != uniformBidiLevel)
                {
                    lastBidiLevel = CreateReverseLSRuns(uniformBidiLevel, lastBidiLevel);
                }

                // Add the plsrun to the span vector.
                _plsrunVectorLatestPosition = _plsrunVector.SetValue(_lscchUpTo, lsrunLength, AddLSRun(lsrun), _plsrunVectorLatestPosition);
                _lscchUpTo += lsrunLength;
            }
        }



        /// <summary>
        /// Break down text with uniform level into multiple shapeable runs,
        /// then create one LSRun for each of them.
        /// </summary>
        private void CreateTextLSRuns(
            TextRunInfo       runInfo,
            IList<TextEffect> textEffects,
            CultureInfo       digitCulture,
            int               offsetToFirstChar,
            int               stringLength,
            int               uniformBidiLevel,
            TextFormattingMode    textFormattingMode,
            bool              isSideways,
            ref int           lastBidiLevel
            )
        {
            ICollection<TextShapeableSymbols> shapeables = null;

            ITextSymbols textSymbols = runInfo.TextRun as ITextSymbols;

            if (textSymbols != null)
            {
                bool isRightToLeftParagraph = (runInfo.TextModifierScope != null) ?
                    runInfo.TextModifierScope.TextModifier.FlowDirection == FlowDirection.RightToLeft :
                    _settings.Pap.RightToLeft;

                shapeables = textSymbols.GetTextShapeableSymbols(
                    _settings.Formatter.GlyphingCache,
                    new CharacterBufferReference(
                        runInfo.CharacterBuffer, runInfo.OffsetToFirstChar + offsetToFirstChar
                        ),
                    stringLength,
                    (uniformBidiLevel & 1) != 0,    // Indicates the RTL based on the bidi level of text.
                    isRightToLeftParagraph,
                    digitCulture,
                    runInfo.TextModifierScope,
                    textFormattingMode,
                    isSideways
                    );
            }
            else
            {
                TextShapeableSymbols textShapeableSymbols = runInfo.TextRun as TextShapeableSymbols;

                if (textShapeableSymbols != null)
                {
                    shapeables = new TextShapeableSymbols[] { textShapeableSymbols };
                }
            }

            if (shapeables == null)
            {
                Debug.Assert(false);
                throw new NotSupportedException();
            }

            double realToIdeal = TextFormatterImp.ToIdeal;
            int ich = 0;

            foreach (TextShapeableSymbols shapeable in shapeables)
            {
                int cch = shapeable.Length;
                Debug.Assert(cch > 0 && cch <= stringLength - ich);

                int currentBidiLevel = uniformBidiLevel;

                LSRun lsrun = new LSRun(
                    runInfo,
                    textEffects,
                    Plsrun.Text,
                    runInfo.OffsetToFirstCp + offsetToFirstChar + ich,
                    cch,
                    (int)Math.Round(realToIdeal * runInfo.TextRun.Properties.FontRenderingEmSize),
                    runInfo.CharacterAttributeFlags,
                    new CharacterBufferRange(runInfo.CharacterBuffer, runInfo.OffsetToFirstChar + offsetToFirstChar + ich, cch),
                    shapeable,
                    realToIdeal,
                    (byte)currentBidiLevel
                    );

                if (currentBidiLevel != lastBidiLevel)
                {
                    lastBidiLevel = CreateReverseLSRuns(currentBidiLevel, lastBidiLevel);
                }

                // set up an LSRun for each shapeable.
                _plsrunVectorLatestPosition = _plsrunVector.SetValue(_lscchUpTo, cch, AddLSRun(lsrun), _plsrunVectorLatestPosition);
                _lscchUpTo += cch;

                ich += cch;
            }
        }



        /// <summary>
        /// Create LSRun for a linebreak run
        /// </summary>
        private LSRun CreateLineBreakLSRun(
            TextRunInfo     runInfo,
            int             stringLength,
            out int         lsrunLength,
            out int         textRunLength
            )
        {
            lsrunLength = stringLength;
            textRunLength = runInfo.Length;

            return new LSRun(
                runInfo,
                null, // No TextEffects on LineBreak
                runInfo.Plsrun,
                runInfo.OffsetToFirstCp,
                runInfo.Length,
                0,      // emSize
                runInfo.CharacterAttributeFlags,
                new CharacterBufferRange(runInfo.CharacterBuffer, runInfo.OffsetToFirstChar, stringLength),
                null,   // no shapebale
                TextFormatterImp.ToIdeal,
                (byte)(Pap.RightToLeft ? 1 : 0)
                );
        }




        /// <summary>
        /// Add new lsrun to lsrun list
        /// </summary>
        /// <param name="lsrun">lsrun to add</param>
        /// <returns>plsrun of added lsrun</returns>
        private Plsrun AddLSRun(LSRun lsrun)
        {
            if(lsrun.Type < Plsrun.FormatAnchor)
            {
                return lsrun.Type;
            }

            Plsrun plsrun = (Plsrun)((uint)_lsrunList.Count + Plsrun.FormatAnchor);

            if (lsrun.IsSymbol)
            {
                plsrun = MakePlsrunSymbol(plsrun);
            }

            _lsrunList.Add(lsrun);

            return plsrun;
        }


        #region lsrun/cp mapping

        /// <summary>
        /// Map internal LSCP to text source cp
        /// </summary>
        /// <remarks>
        /// This method does not handle mapping of LSCP beyond the last one
        /// </remarks>
        internal int GetExternalCp(int lscp)
        {
            lscp -= _lscpFirstValue;

            SpanRider rider = new SpanRider(_plsrunVector, _plsrunVectorLatestPosition, lscp - _cpFirst);
            _plsrunVectorLatestPosition = rider.SpanPosition;

            return GetRun((Plsrun)rider.CurrentElement).OffsetToFirstCp +
                    lscp - rider.CurrentSpanStart;
        }


        /// <summary>
        /// Get LSRun from plsrun
        /// </summary>
        internal LSRun GetRun(Plsrun plsrun)
        {
            plsrun = ToIndex(plsrun);

            return  (LSRun)(
                IsContent(plsrun) ?
                _lsrunList[(int)(plsrun - Plsrun.FormatAnchor)] :
                ControlRuns[(int)plsrun]
                );
        }


        /// <summary>
        /// Check if plsrun is marker
        /// </summary>
        internal static bool IsMarker(Plsrun plsrun)
        {
            return (plsrun & Plsrun.IsMarker) != 0;
        }


        /// <summary>
        /// Make this plsrun a marker plsrun
        /// </summary>
        internal static Plsrun MakePlsrunMarker(Plsrun plsrun)
        {
            return (plsrun | Plsrun.IsMarker);
        }


        /// <summary>
        /// Make this plsrun a symbol plsrun
        /// </summary>
        internal static Plsrun MakePlsrunSymbol(Plsrun plsrun)
        {
            return (plsrun | Plsrun.IsSymbol);
        }


        /// <summary>
        /// Convert plsrun to index to lsrun list
        /// </summary>
        internal static Plsrun ToIndex(Plsrun plsrun)
        {
            return (plsrun & Plsrun.UnmaskAll);
        }


        /// <summary>
        /// Check if run is content
        /// </summary>
        internal static bool IsContent(Plsrun plsrun)
        {
            plsrun = ToIndex(plsrun);
            return plsrun >= Plsrun.FormatAnchor;
        }


        /// <summary>
        /// Check if character is space
        /// </summary>
        internal static bool IsSpace(char ch)
        {
            return ch == ' ' || ch == '\u00a0';
        }


        /// <summary>
        /// Check if character is of strong directional type
        /// </summary>
        internal static bool IsStrong(char ch)
        {
            int unicodeClass = Classification.GetUnicodeClass(ch);
            ItemClass itemClass = (ItemClass)Classification.CharAttributeOf(unicodeClass).ItemClass;
            return itemClass == ItemClass.StrongClass;
        }


        /// <summary>
        /// Check if the run is a line break or paragraph break
        /// </summary>
        internal static bool IsNewline (Plsrun plsrun)
        {
            return plsrun == Plsrun.LineBreak || plsrun == Plsrun.ParaBreak;
        }

        /// <summary>
        /// Check if the character is a line break or paragraph break character
        /// </summary>
        internal static bool IsNewline(ushort flags)
        {
            return ( (flags & (ushort) CharacterAttributeFlags.CharacterLineBreak) != 0
                  || (flags & (ushort) CharacterAttributeFlags.CharacterParaBreak) != 0 );
        }

        #endregion


        /// <summary>
        /// Repositioning text lsruns according to its BaselineAlignmentment property
        /// </summary>
        internal void AdjustRunsVerticalOffset(
            int             dcpLimit,
            int             height,
            int             baselineOffset,
            out int         cellHeight,
            out int         cellAscent
            )
        {
            // Following are all alignment point offsets from the baseline of the line.
            // Value grows positively in paragraph flow direction.
            int top = 0;
            int bottom = 0;
            int textTop = 0;
            int textBottom = 0;
            int super = 0;
            int sub = 0;
            int center = 0;

            ArrayList lsruns = new ArrayList(3);

            // Find TextTop from all Baseline lsruns

            int dcp = 0;
            int i = 0;
            while(dcp < dcpLimit)
            {
                Debug.Assert(i < _plsrunVector.Count);

                Span span = _plsrunVector[i++];
                LSRun lsrun = GetRun((Plsrun)span.element);

                if(     lsrun.Type == Plsrun.Text
                    ||  lsrun.Type == Plsrun.InlineObject)
                {
                    if(lsrun.RunProp.BaselineAlignment == BaselineAlignment.Baseline)
                    {
                        textTop = Math.Max(textTop, lsrun.BaselineOffset);
                        textBottom = Math.Max(textBottom, lsrun.Descent);
                    }

                    lsruns.Add(lsrun);
                }

                dcp += span.length;
            }

            textTop = -textTop; // offset from the baseline in paragraph flow direction

            top = height > 0 ? -baselineOffset : textTop;

            // Finalize Bottom by ignoring all but Top, TextTop and Baseline lsruns

            foreach(LSRun lsrun in lsruns)
            {
                switch (lsrun.RunProp.BaselineAlignment)
                {
                    case BaselineAlignment.Top:
                        textBottom = Math.Max(textBottom, lsrun.Height + top);
                        break;

                    case BaselineAlignment.TextTop:
                        textBottom = Math.Max(textBottom, lsrun.Height + textTop);
                        break;
                }
            }

            bottom = height > 0 ? height - baselineOffset : textBottom;


            // hardcode the positions for now
            center = (top + bottom) / 2;
            sub = bottom / 2;
            super = top * 2 / 3;


            // Now move all lsruns according to its BaselineAlignment property

            cellAscent = 0;
            int cellDescent = 0;

            foreach(LSRun lsrun in lsruns)
            {
                int move = 0;

                switch (lsrun.RunProp.BaselineAlignment)
                {
                    case BaselineAlignment.Top:
                        // lsrun top to line top
                        move = top + lsrun.BaselineOffset;
                        break;

                    case BaselineAlignment.Bottom:
                        // lsrun bottom to line bottom
                        move = bottom - lsrun.Height + lsrun.BaselineOffset;
                        break;

                    case BaselineAlignment.TextTop:
                        // lsrun top to line text top
                        move = textTop + lsrun.BaselineOffset;
                        break;

                    case BaselineAlignment.TextBottom:
                        // lsrun bottom to line text bottom
                        move = textBottom - lsrun.Height + lsrun.BaselineOffset;
                        break;

                    case BaselineAlignment.Center:
                        // lsrun center to line center
                        move = center - lsrun.Height/2 + lsrun.BaselineOffset;
                        break;

                    case BaselineAlignment.Superscript:
                        // lsrun baseline to line superscript
                        move = super;
                        break;

                    case BaselineAlignment.Subscript:
                        // lsrun baseline to line subscript
                        move = sub;
                        break;
                }

                lsrun.Move(move);

                // Recalculate line ascent and descent
                cellAscent = Math.Max(cellAscent, lsrun.BaselineOffset - move);
                cellDescent = Math.Max(cellDescent, lsrun.Descent + move);
            }

            cellHeight = cellAscent + cellDescent;
        }


        /// <summary>
        /// Collect a piece of raw text that makes up a word containing the specified LSCP.
        /// The text returned from this method is used for hyphenation of a single word.
        /// In addition to the raw text, it also returns the mapping between the raw character
        /// indices and the LSCP indices in plsrunVector. This is used later on when we
        /// map the lexical result back to the positions used to communicate with LS.
        /// </summary>
        /// <remarks>
        /// "word" here is not meant for a linguistic term. It only means array of characters
        /// from space to space i.e. a word in SE Asian language is not separated by spaces.
        /// </remarks>
        internal char[] CollectRawWord(
            int                 lscpCurrent,
            bool                isCurrentAtWordStart,
            bool                isSideways,
            out int             lscpChunk,
            out int             lscchChunk,
            out CultureInfo     textCulture,
            out int             cchWordMax,
            out SpanVector<int> textVector
            )
        {
            // Fetch the lsrun containing the current position make sure
            // all LSCP before it are properly retained in plsrun vector.
            // We need this before we could reliably walk back the plsrun
            // vector to establish the chunk's start position in the following
            // step.

            textVector = new SpanVector<int>();
            textCulture = null;

            lscpChunk = lscpCurrent;
            lscchChunk = 0;
            cchWordMax = 0;

            Plsrun plsrun;
            int lsrunOffset;
            int lsrunLength;

            LSRun lsrun = FetchLSRun(
                lscpCurrent,
                Settings.Formatter.TextFormattingMode,
                isSideways,
                out plsrun,
                out lsrunOffset,
                out lsrunLength
                );

            if (lsrun == null)
                return null;

            textCulture = lsrun.TextCulture;

            int lscpLim;
            int cchBefore = 0;

            if (!isCurrentAtWordStart && lscpChunk > _cpFirst)
            {
                // The specified position may not be start of word.
                // Expand backward to the first non-space character following a space
                // before the current position. If the current position is already
                // at space, no skip is needed.

                SpanRider rider = new SpanRider(_plsrunVector, _plsrunVectorLatestPosition);

                do
                {
                    rider.At(lscpChunk - _cpFirst - 1);

                    lscpLim = rider.CurrentSpanStart + _cpFirst;

                    lsrun = GetRun((Plsrun)rider.CurrentElement);

                    if (   IsNewline(lsrun.Type)
                        || lsrun.Type == Plsrun.InlineObject)
                    {
                        // Stop expanding due to hard break
                        break;
                    }

                    if (lsrun.Type == Plsrun.Text)
                    {
                        if (!lsrun.TextCulture.Equals(textCulture))
                        {
                            // Stop expanding due to change of text culture
                            break;
                        }

                        int cchLim = lscpChunk - lscpLim;
                        int ichFirst = lsrun.OffsetToFirstChar + lscpChunk - _cpFirst - rider.CurrentSpanStart;
                        int cch = 0;

                        // Skip all non-space characters until a space is found
                        while (cch < cchLim && !IsSpace(lsrun.CharacterBuffer[ichFirst - cch - 1]))
                            cch++;

                        cchBefore += cch;

                        if (cch < cchLim)
                        {
                            // Start of chunk is found
                            lscpChunk -= cch;
                            break;
                        }
                    }

                    // Reposition start of chunk to the beginning of the current run and continue expanding
                    Invariant.Assert(lscpLim < lscpChunk);
                    lscpChunk = lscpLim;
} while (lscpChunk > _cpFirst && cchBefore <= MaxCchWordToHyphenate);

                _plsrunVectorLatestPosition = rider.SpanPosition;
            }

            if (cchBefore > MaxCchWordToHyphenate)
            {
                // The word is unusually long. This is already a situation we dont want
                // bring hyphenation into the picture.
                return null;
            }


            // Expand forward from the beginning of a word to the end of the word.
            // If the start position is already at space, skip passed all leading spaces

            StringBuilder stringBuilder = null;
            int lscp = lscpChunk;
            int cchText = 0;
            int cchLastWord = 0;

            do
            {
                lsrun = FetchLSRun(
                    lscp,
                    Settings.Formatter.TextFormattingMode,
                    isSideways,
                    out plsrun,
                    out lsrunOffset,
                    out lsrunLength
                    );

                if (lsrun == null)
                    return null;

                lscpLim = lscp + lsrunLength;

                if (   IsNewline(lsrun.Type)
                    || lsrun.Type == Plsrun.InlineObject)
                {
                    // Stop expanding due to hard break
                    break;
                }

                if (lsrun.Type == Plsrun.Text)
                {
                    if (!lsrun.TextCulture.Equals(textCulture))
                    {
                        // Stop expanding due to change of text culture
                        break;
                    }

                    int cchLim = lscpLim - lscp;
                    int ichFirst = lsrun.OffsetToFirstChar + lsrun.Length - lsrunLength;
                    int cch = 0;

                    if (cchText == 0)
                    {
                        // Skip all leading spaces
                        while (cch < cchLim && IsSpace(lsrun.CharacterBuffer[ichFirst + cch]))
                            cch++;
                    }

                    // Skip all non-space characters until a following space is found
                    char ch;
                    int cchWord = cchLastWord;

                    while (     cch < cchLim
                            &&  cchText + cch < MaxCchWordToHyphenate
                            &&  !IsSpace((ch = lsrun.CharacterBuffer[ichFirst + cch])))
                    {
                        cch++;

                        if (IsStrong(ch))
                        {
                            cchWord++;
                        }
                        else
                        {
                            // Non-strong character marks the end of the current word length,
                            // Keep the length of the greatest length word found so far.
                            if (cchWord > cchWordMax)
                                cchWordMax = cchWord;

                            cchWord = 0;
                        }
                    }

                    // Keep the length so far of the last word found.
                    cchLastWord = cchWord;

                    if (cchLastWord > cchWordMax)
                    {
                        // Keep the length of the greatest length word found so far.
                        cchWordMax = cchLastWord;
                    }

                    if (stringBuilder == null)
                        stringBuilder = new StringBuilder();

                    // Gathering the raw text
                    lsrun.CharacterBuffer.AppendToStringBuilder(stringBuilder, ichFirst, cch);

                    // Keep the map between indices to raw text and its correspondent LSCP
                    textVector.Set(cchText, cch, lscp - lscpChunk);

                    cchText += cch;

                    if (cch < cchLim)
                    {
                        // End of chunk is found
                        lscp += cch;
                        break;
                    }
                }

                Invariant.Assert(lscpLim > lscp);
                lscp = lscpLim;
} while (cchText < MaxCchWordToHyphenate);

            if (stringBuilder == null)
                return null;

            lscchChunk = lscp - lscpChunk;
            Invariant.Assert(stringBuilder.Length == cchText);

            char[] rawText = new char[stringBuilder.Length];
            stringBuilder.CopyTo(0, rawText, 0, rawText.Length);
            return rawText;
        }


        /// <summary>
        /// Fetch cached inline metrics
        /// </summary>
        /// <param name="textObject">text object to format</param>
        /// <param name="cpFirst">firs cp of text object</param>
        /// <param name="currentPosition">inline's current pen position</param>
        /// <param name="rightMargin">line's right margin</param>
        /// <returns>inline info</returns>
        /// <remarks>
        /// Right margin is not necessarily the same as column max width. Right margin
        /// is usually greater than actual column width during object formatting. LS
        /// increases the margin to 1/32 of the column width to provide a leaway for
        /// breaking.
        ///
        /// However TextBlock/TextFlow functions in such a way that it needs to know the exact width
        /// left in the line in order to compute the inline's correct size. We make sure
        /// that it'll never get oversize max width.
        ///
        /// Inline object's reported size can be so huge that it may overflow LS's maximum value.
        /// If a given width is a finite value, we'll respect that and out-of-range exception may be thrown as appropriate.
        /// If the width is Positive Infinity, the width is trimmed to the maximum remaining value that LS can handle. This is
        /// appropriate for the cases where client measures inline objects at Infinite size.
        /// </remarks>
        internal TextEmbeddedObjectMetrics FormatTextObject(
            TextEmbeddedObject  textObject,
            int                 cpFirst,
            int                 currentPosition,
            int                 rightMargin
            )
        {
            if(_textObjectMetricsVector == null)
            {
                _textObjectMetricsVector = new SpanVector(null);
            }

            SpanRider rider = new SpanRider(_textObjectMetricsVector);
            rider.At(cpFirst);

            TextEmbeddedObjectMetrics metrics = (TextEmbeddedObjectMetrics)rider.CurrentElement;

            if(metrics == null)
            {
                int widthLeft = _formatWidth - currentPosition;

                if(widthLeft <= 0)
                {
                    // we're formatting this object outside the actual column width,
                    // we give the host the max width from the current position up
                    // to the margin.
                    widthLeft = rightMargin - _formatWidth;
                }

                metrics = textObject.Format(_settings.Formatter.IdealToReal(widthLeft, _settings.TextSource.PixelsPerDip));

                if (Double.IsPositiveInfinity(metrics.Width))
                {
                    // If the inline object has Width to be positive infinity, trim the width to
                    // the maximum value that LS can handle.
                    metrics = new TextEmbeddedObjectMetrics(
                        _settings.Formatter.IdealToReal((Constants.IdealInfiniteWidth - currentPosition), _settings.TextSource.PixelsPerDip),
                        metrics.Height,
                        metrics.Baseline
                        );
                }
                else if (metrics.Width > _settings.Formatter.IdealToReal((Constants.IdealInfiniteWidth - currentPosition), _settings.TextSource.PixelsPerDip))
                {
                    // LS cannot compute value greater than its maximum computable value
                    throw new ArgumentException(SR.Get(SRID.TextObjectMetrics_WidthOutOfRange));
                }

                _textObjectMetricsVector.SetReference(cpFirst, textObject.Length, metrics);
            }

            Debug.Assert(metrics != null);
            return metrics;
        }


        #region ENUMERATIONS & CONST

        // first negative cp of bullet marker
        internal const int LscpFirstMarker = (-0x7FFFFFFF);

        // Note: Trident uses this figure
        internal const int TypicalCharactersPerLine = 100;

        internal const char CharLineSeparator = '\x2028';
        internal const char CharParaSeparator = '\x2029';
        internal const char CharLineFeed      = '\x000a';
        internal const char CharCarriageReturn= '\x000d';
        internal const char CharTab           = '\x0009';

        // Hardcoded strings in LS memory
        // They are kept as a pointer such that it would be fast to return
        // them as pointers back into LS.
        internal static IntPtr PwchParaSeparator;
        internal static IntPtr PwchLineSeparator;
        internal static IntPtr PwchNbsp;
        internal static IntPtr PwchHidden;
        internal static IntPtr PwchObjectTerminator;
        internal static IntPtr PwchObjectReplacement;


        /// !! DO NOT update this enum without looking at its unmanaged pair in lslo.cpp !!
        /// [wchao, 10-1-2001]
        //
        internal enum ObjectId : ushort
        {
            Reverse         = 0,
            MaxNative       = 1,
            InlineObject    = 1,
            Max             = 2,
            Text_chp        = 0xffff,
        }

        // Maximum number of characters per line. If we exceed this number of characters
        // without breaking in a normal way, we chop off the line by generating a fake
        // line break run. This is to mitigate potential denial-of-service attacks by
        // ensuring that there is a reasonable upper bound on the time required to
        // format a single line.
        //
        // In ordinary documents with word-wrap enabled, we should always reach the
        // right margin long before MaxCharactersPerLine characters. The only reason
        // we might forcibly break the line in such cases would be:
        //   (a) Extreme right margin
        //   (b) Extreme number of zero-width characters
        //   (c) Extremely small font size (i.e., fraction of a pixel)
        //   (d) Lack of break opportunity (e.g., no spaces)
        // All of these are security cases, for which chopping off the line is a
        // reasonable mitigation. Limiting the line length addresses all of these
        // potential attacks so we don't need separate mitigations for, e.g.,
        // zero-width characters.
        //
        // Extremely long lines are less unlikely in nowrap scenarios, such as a code
        // editor. However, the same security issues apply so we still chop off the line
        // if we exceed the maximum number of characters with no line break. Note that
        // Notepad (with nowrap) does the same thing.
        //
        // The value chosen corresponds roughly to four pages of text at 60 characters
        // per line and 40 lines per page. Testing shows this to be a resonable limit
        // in terms of run time.
        internal const int MaxCharactersPerLine = 9600; // 60 * 40 * 4

        /// <summary>
        /// The maximum number of characters within a single word that is still considered a legitimate
        /// input for hyphenation. This value is suggested by Stefanie Schiller - the NLG expert when
        /// considering a theoretical example of a German compound word which consists of 12 compound
        /// segments. The following is that word.
        ///
        /// "DONAUDAMPFSCHIFFAHRTSELEKTRIZITAETENHAUPTBETRIEBSWERKBAUUNTERBEAMTENGESELLSCHAFT"
        ///
        /// [Wchao, 3/15/2006]
        /// </summary>
        private const int MaxCchWordToHyphenate = 80;

        #endregion

        #region Properties
        internal FormatSettings Settings
        {
            get { return _settings; }
        }

        internal ParaProp Pap
        {
            get { return _settings.Pap; }
        }

        internal int CpFirst
        {
            get { return _cpFirst; }
        }

        internal SpanVector PlsrunVector
        {
            get { return _plsrunVector; }
        }

        internal ArrayList LsrunList
        {
            get { return _lsrunList; }
        }

        internal int FormatWidth
        {
            get { return _formatWidth; }
        }

        internal int CchEol
        {
            get { return _cchEol; }
            set { _cchEol = value; }
        }
        #endregion
    }


    /// <summary>
    /// Bidi state that applie across line. If no preceding state is available internally,
    /// it calls back to the client to obtain additional Bidi control and explicit embedding level.
    /// </summary>
    internal sealed class BidiState : Bidi.State
    {
        public BidiState(FormatSettings settings, int cpFirst)
            : this(settings, cpFirst, null)
        {
        }

        public BidiState(FormatSettings settings, int cpFirst, TextModifierScope modifierScope)
            : base (settings.Pap.RightToLeft)
        {
            _settings = settings;
            _cpFirst = cpFirst;

            NumberClass = DirectionClass.ClassInvalid;
            StrongCharClass = DirectionClass.ClassInvalid;


            // find the top most scope that has the direction embedding
            while ( modifierScope != null && !modifierScope.TextModifier.HasDirectionalEmbedding)
            {
                modifierScope = modifierScope.ParentScope;
            }

            if (modifierScope != null)
            {
                _cpFirstScope = modifierScope.TextSourceCharacterIndex;

                // Initialize Bidi stack base on modifier scope
                Bidi.BidiStack stack = new Bidi.BidiStack();
                stack.Init(LevelStack);

                ushort overflowLevels = 0;
                InitLevelStackFromModifierScope(stack, modifierScope, ref overflowLevels);

                LevelStack = stack.GetData();
                Overflow = overflowLevels;
            }
        }


        /// <summary>
        /// Set the default last strongs when an embedding level is changed such that
        /// ambiguous characters (i.e. characters with null or InvariantCulture) at the beginning
        /// of the current embedding level can be resolved correctly.
        /// </summary>
        internal void SetLastDirectionClassesAtLevelChange()
        {
            if ((CurrentLevel & 1) == 0)
            {
                LastStrongClass = DirectionClass.Left;
                LastNumberClass = DirectionClass.Left;
            }
            else
            {
                LastStrongClass = DirectionClass.ArabicLetter;
                LastNumberClass = DirectionClass.ArabicNumber;
            }
        }

        internal byte CurrentLevel
        {
            get { return Bidi.BidiStack.GetMaximumLevel(LevelStack); }
        }


        /// <summary>
        /// Method to get the last number class overridden by bidi algorithm implementer
        /// </summary>
        public override DirectionClass LastNumberClass
        {
            get
            {
                if (this.NumberClass == DirectionClass.ClassInvalid )
                {
                    GetLastDirectionClasses();
                }

                return this.NumberClass;
            }

            set { this.NumberClass = value; }
        }


        /// <summary>
        /// Method to get the last strong class overridden by bidi algorithm implementer
        /// </summary>
        public override DirectionClass LastStrongClass
        {
            get
            {
                if (this.StrongCharClass == DirectionClass.ClassInvalid)
                {
                    GetLastDirectionClasses();
                }
                return this.StrongCharClass;
            }

            set
            {
                this.StrongCharClass = value;
                this.NumberClass = value;
            }
        }


        /// <summary>
        /// Last strong class not found internally, call out to client
        /// </summary>
        private void GetLastDirectionClasses()
        {
            DirectionClass  strongClass = DirectionClass.ClassInvalid;
            DirectionClass  numberClass = DirectionClass.ClassInvalid;

            // It is a flag to indicate whether to continue calling GetPrecedingText.
            // Because Bidi algorithm works within a paragraph only, we should terminate the
            // loop at paragraph boundary and fall back to the appropriate defaults.

            bool continueScanning = true;

            while (continueScanning && _cpFirst > _cpFirstScope)
            {
                TextSpan<CultureSpecificCharacterBufferRange> textSpan = _settings.GetPrecedingText(_cpFirst);
                CultureSpecificCharacterBufferRange charString = textSpan.Value;

                if (textSpan.Length <= 0)
                {
                    break;  // stop when preceding text span has length 0.
                }

                if (!charString.CharacterBufferRange.IsEmpty)
                {
                    continueScanning = Bidi.GetLastStongAndNumberClass(
                        charString.CharacterBufferRange,
                        ref strongClass,
                        ref numberClass
                        );

                    if (strongClass != DirectionClass.ClassInvalid)
                    {
                        this.StrongCharClass = strongClass;

                        if (this.NumberClass == DirectionClass.ClassInvalid)
                        {
                            if (numberClass == DirectionClass.EuropeanNumber)
                            {
                                // Override EuropeanNumber class as appropriate.
                                numberClass = GetEuropeanNumberClassOverride(CultureMapper.GetSpecificCulture(charString.CultureInfo));
                            }

                            this.NumberClass = numberClass;
                        }

                        break;
                    }
                }

                _cpFirst -= textSpan.Length;
            }


            // If we don't have the strong class and/or number class, select appropriate defaults
            // according to the base bidi level.
            //
            // To determine the base bidi level, we look at bit 0 if the LevelStack. This is NOT
            // an even/odd test. LevelStack is an array of bits corresponding to all of the bidl
            // levels on the stack. Thus, bit 0 is set if and only if the base bidi level is zero,
            // i.e., it's a left-to-right paragraph.

            if(strongClass == DirectionClass.ClassInvalid)
            {
                this.StrongCharClass = ((CurrentLevel & 1) == 0) ? DirectionClass.Left : DirectionClass.ArabicLetter;
            }

            if(numberClass == DirectionClass.ClassInvalid)
            {
                this.NumberClass = ((CurrentLevel & 1) == 0) ? DirectionClass.Left : DirectionClass.ArabicNumber;
            }
        }

        /// <summary>
        /// Walk the TextModifierScope to reinitialize the bidi stack.
        /// We push to bidi-stack from the earliest directional modifier (i.e. from bottom of the
        /// the scope chain onwards). We use a stack to reverse the scope chain first.
        /// </summary>
        private static void InitLevelStackFromModifierScope(
            Bidi.BidiStack    stack,
            TextModifierScope scope,
            ref ushort        overflowLevels
            )
        {
            Stack<TextModifier> directionalEmbeddingStack = new Stack<TextModifier>(32);

            for (TextModifierScope currentScope = scope; currentScope != null; currentScope = currentScope.ParentScope)
            {
                if (currentScope.TextModifier.HasDirectionalEmbedding)
                {
                    directionalEmbeddingStack.Push(currentScope.TextModifier);
                }
            }

            while (directionalEmbeddingStack.Count > 0)
            {
                TextModifier modifier = directionalEmbeddingStack.Pop();

                if (overflowLevels > 0)
                {
                    // Bidi level stack overflows. Just increment the bidi stack overflow number
                    overflowLevels ++;
                }
                else if (!stack.Push(modifier.FlowDirection == FlowDirection.LeftToRight))
                {
                    // Push stack not successful. Stack starts to overflow.
                    overflowLevels = 1;
                }
}
        }

        /// <summary>
        /// Obtain the explict direction class of European number based on culture and current flow direction.
        /// European numbers in Arabic/Farsi culture and RTL flow direction are to be considered as Arabic numbers.
        /// </summary>
        internal DirectionClass GetEuropeanNumberClassOverride(CultureInfo cultureInfo)
        {
            if (   cultureInfo != null
                 &&(   (cultureInfo.LCID & 0xFF) == 0x01 // Arabic culture
                    || (cultureInfo.LCID & 0xFF) == 0x29 // Farsi culture
                   )
                 && (CurrentLevel & 1) != 0 // RTL flow direction
                )
            {
                return DirectionClass.ArabicNumber;
            }

            return DirectionClass.EuropeanNumber;
        }

        private FormatSettings  _settings;
        private int             _cpFirst;
        private int             _cpFirstScope; // The first Cp of the current scope. GetLastStrong() should not go beyond it.
    }
}
