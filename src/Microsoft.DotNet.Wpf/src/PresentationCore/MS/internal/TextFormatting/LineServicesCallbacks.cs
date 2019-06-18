// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* SSS_DROP_BEGIN */

/*************************************************************************
* NOTICE: Code excluded from Developer Reference Sources.
*         Don't remove the SSS_DROP_BEGIN directive on top of the file.
*******************************************************************/


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using MS.Internal;
using MS.Internal.Shaping;
using MS.Internal.FontCache;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

using MS.Internal.Text.TextInterface;

// Disabling 1634 and 1691: 
// In order to avoid generating warnings about unknown message numbers and 
// unknown pragmas when compiling C# source code with the C# compiler, 
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

// Disabling 6500: 
// Suppressing PRESHARP:Warning 6500 Fatal exceptions (NULLReferenceException, SEHException) 
// potentially ignored by this catch.
// LineServices callbacks are designed to catch all exceptions such than an error code can be 
// returned to Line Services engine. An exception is eventually re-thrown to the user after line
// services engine finishes cleaning up and returns the control to line layout code.
//
#pragma warning disable 6500

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Line Services callbacks
    /// </summary>
    internal sealed class LineServicesCallbacks
    {
        /// <summary>
        /// LineServices fetches a run of text. The run is broken down at the following
        /// boundaries.
        ///     o  Physical font
        ///     o  Unicode block (shaping engine script ID)
        ///     o  Change of bidirectional level
        ///     o  Change of number substitution method
        /// </summary>
        /// <remarks>
        /// The suffix "Redefined" in the method name indicates that this is one
        /// such callback that is not managed code friendly and requires redefinition
        /// to be able to perform at its optimal performance. 
        /// 
        /// The problem with the original LS FetchRun is that it requires a raw character 
        /// pointer in return. This forces us to have to pin the managed memory using
        /// GCHandle for every fetch. It hurts performance not only because of the 
        /// immediate CLR cost of allocating/releasing a GCHandle, but also the implied
        /// side effect of leaving GC heap at fragmented state. The latter requires 
        /// more timely collection which takes significant hit. 
        /// 
        /// We solve this by redirecting LS to a proxy FetchRun callback in our unmanaged
        /// wrapper. The wrapper allocates fixed memory buffer in process heap and delegates
        /// the call to this method with extra parameters. This method could decide whether
        /// to use the incoming fixed-size buffer, or to return a raw pointer. When dealing
        /// with unmanaged client (i.e. Office), it would be more efficient to just return
        /// pointer since the client's backing store memory is already all fixed. However
        /// when dealing with managed client (i.e XAML), it would fills the incoming buffer
        /// instead to avoid pinning of managed memory. 
        /// 
        /// When filling the incoming fixed-size buffer is required but the given buffer size  
        /// is too small, we fail the call by setting the output param 'pwchText' to null 
        /// and 'fIsBufferUsed' flags to false. The return code is still LsErr.None. The 
        /// proxy unmanaged FetchRun will respond to this result by expanding the buffer size 
        /// to be as big as the result 'cchText' value and retry the call.
        ///
        /// </remarks>
        internal unsafe LsErr FetchRunRedefined(
            IntPtr              pols,               // Line Layout context
            int                 lscpFetch,          // position to fetch
            int                 fIsStyle,           // flag indicates if pstyle is given
            IntPtr              pstyle,             // current demanded style
            char*               pwchTextBuffer,     // [in/out] fixed-size character buffer
            int                 cchTextBuffer,      // buffer length in characters
            ref int             fIsBufferUsed,      // [out] Boolean flag indicating the fixed-size buffer is used
            out char*           pwchText,           // [out] pointer to run's character string
            ref int             cchText,            // [out] length of string
            ref int             fIsHidden,          // [out] Is this run hidden?
            ref LsChp           lschp,              // [out] run's character properties
            ref IntPtr          lsplsrun            // [out] fetched run
            )
        {
            LsErr lserr = LsErr.None;
            pwchText = null;
            Plsrun plsrun = Plsrun.Undefined;
            LSRun lsrun = null;

            try
            {
                FullTextState fullTextState = FullText;
                TextStore store = fullTextState.StoreFrom(lscpFetch);

                int lsrunOffset;

                lsrun = store.FetchLSRun(
                    lscpFetch,
                    fullTextState.TextFormattingMode,
                    fullTextState.IsSideways,
                    out plsrun,
                    out lsrunOffset,
                    out cchText
                    );

                fIsBufferUsed = 0;
                pwchText = lsrun.CharacterBuffer.GetCharacterPointer();

                if (pwchText == null)
                {
                    // Unable to obtain the raw character pointer of the associated run character string,
                    // avoid pinning the managed memory by using the specified local buffer.
                    //
                    // Pinning via allocating GCHandle is very costly both in term of the immediate cost
                    // of GCHandle.Alloc and GCHandle.Free, and the implied cost of a fragemented GC heap
                    // which reduces the GC's ability to compact the managed heap. That situation leads to
                    // more collection down the road.
                    //
                    // The cost of copying the character string is significantly less than the effect of 
                    // GCHandle, especially in common UI scenario where the number of individual string objects
                    // tends to be quite high.

                    if (cchText <= cchTextBuffer)
                    {
                        Invariant.Assert(pwchTextBuffer != null);

                        int j = lsrun.OffsetToFirstChar + lsrunOffset;
                        for (int i = 0; i < cchText; i++, j++)
                        {
                            pwchTextBuffer[i] = lsrun.CharacterBuffer[j];
                        }

                        fIsBufferUsed = 1;
                    }
                    else
                    {
                        return LsErr.None;
                    }
                }
                else
                {
                    pwchText += lsrun.OffsetToFirstChar + lsrunOffset;
                }


                lschp = new LsChp();
                fIsHidden = 0;

                switch (lsrun.Type)
                {
                    case Plsrun.Reverse:
                        lschp.idObj = (ushort)TextStore.ObjectId.Reverse;
                        break;

                    case Plsrun.FormatAnchor:
                    case Plsrun.CloseAnchor:
                        lschp.idObj = (ushort)TextStore.ObjectId.Text_chp;
                        break;

                    case Plsrun.InlineObject:
                        lschp.idObj = (ushort)TextStore.ObjectId.InlineObject;
                        SetChpFormat(lsrun.RunProp, ref lschp);
                        break;

                    case Plsrun.Hidden:
                        lschp.idObj = (ushort)TextStore.ObjectId.Text_chp;
                        fIsHidden = 1;
                        break;

                    case Plsrun.Text:
                        {
                            Debug.Assert(TextStore.IsContent(plsrun), "Unrecognizable run!");
                            Debug.Assert(lsrun.RunProp != null, "invalid lsrun!");

                            lschp.idObj = (ushort)TextStore.ObjectId.Text_chp;

                            if (    lsrun.Shapeable != null
                                &&  lsrun.Shapeable.IsShapingRequired)
                            {
                                lschp.flags |= LsChp.Flags.fGlyphBased;

                                if (lsrun.Shapeable.NeedsMaxClusterSize)
                                {
                                    // 
                                    // dcpMaxContext hints LS the maximum number of characters that could be 
                                    // shaped into a cluster. We set it in order to prevent LS from breaking 
                                    // the line among characters that could be shaped into a single cluster 
                                    // within the formatting width. 
                                    // Trident sets it to 15 to support Indic. We had been using 8 because we
                                    // didn't support Indic in V1, but this is bumped to 15 for V3.5 (Indic
                                    // support has been added)
                                    //
                                    lschp.dcpMaxContent = lsrun.Shapeable.MaxClusterSize;
                                }
                            }

                            SetChpFormat(lsrun.RunProp, ref lschp);

                            // All LineBreak and ParaBreak are separated into individual runs of type Plsrun.LineBreak or Plsrun.ParaBreak.
                            Invariant.Assert(!TextStore.IsNewline(lsrun.CharacterAttributeFlags));
                            break;
                        }

                    default :
                        //  case Plsrun.LineBreak, Plsrun.ParaBreak, Plsrun.FakeLineBreak.
                        lschp.idObj = (ushort)TextStore.ObjectId.Text_chp;
                        store.CchEol = lsrun.Length;
                        break;                                            
                }


                if (    lsrun.Type == Plsrun.Text
                    ||  lsrun.Type == Plsrun.InlineObject)
                {
                    // Run properties trigger repositioning
                    Debug.Assert(lsrun.RunProp != null);

                    if (    lsrun.RunProp != null
                        &&  lsrun.RunProp.BaselineAlignment != BaselineAlignment.Baseline)
                    {
                        FullText.VerticalAdjust = true;
                    }
                }

                // plsrun is defined as IntPtr on LS side
                lsplsrun = (IntPtr)plsrun;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("FetchRunRedefined", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        private void SetChpFormat(
            TextRunProperties   runProp,
            ref LsChp           lschp
            )
        {
            SetChpFormat(runProp.TextDecorations, ref lschp);
            SetChpFormat(FullText.TextStore.Pap.TextDecorations, ref lschp);
        }

        private void SetChpFormat(
            TextDecorationCollection    textDecorations,
            ref LsChp                   lschp
            )
        {
            // TextDecorations can be null.
            if (textDecorations != null)
            {
                // enumerate through all the TextDecoration and fill Chp accordingly.
                for (int i = 0; i < textDecorations.Count; i++)
                {
                    switch (textDecorations[i].Location)
                    {
                        case TextDecorationLocation.Underline:
                            lschp.flags |= LsChp.Flags.fUnderline;
                            break;

                        case TextDecorationLocation.OverLine:
                        case TextDecorationLocation.Strikethrough:
                        case TextDecorationLocation.Baseline:
                            lschp.flags |= LsChp.Flags.fStrike;
                            break;
                    }
                }
            }
        }


        internal LsErr FetchPap(
            IntPtr      pols,           // Line Layout context
            int         lscpFetch,      // position to fetch
            ref LsPap   lspap           // [out] paragraph properties
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                lspap = new LsPap();

                TextStore store = FullText.StoreFrom(lscpFetch);

                lspap.cpFirst = lspap.cpFirstContent = lscpFetch;   // note: LS doesnt really care
                lspap.lskeop = LsKEOP.lskeopEndPara1;

                //
                // Set flag fFmiTreatHyphenAsRegular to make Hyphen follow line breaking class table like 
                // regular characters. If the flag is not set, LS will consider Hyphen to have direct break 
                // opp before and after which is not always desirable, e.g. space after hyphen may be put 
                // to the start of next line.
                //                
                lspap.grpf |= LsPap.Flags.fFmiTreatHyphenAsRegular;

                ParaProp pap = store.Pap;

                if (FullText.ForceWrap)
                {
                    lspap.grpf |= LsPap.Flags.fFmiApplyBreakingRules;
                }
                else if (pap.Wrap)
                {
                    lspap.grpf |= LsPap.Flags.fFmiApplyBreakingRules;

                    if (!pap.EmergencyWrap)
                    {
                        lspap.grpf |= LsPap.Flags.fFmiForceBreakAsNext;
                    }

                    if (pap.Hyphenator != null)
                    {
                        lspap.grpf |= LsPap.Flags.fFmiAllowHyphenation;
                    }
                }

                if (pap.FirstLineInParagraph)
                {
                    lspap.cpFirstContent = store.CpFirst;
                    lspap.cpFirst = lspap.cpFirstContent;

                    if (FullText.TextMarkerStore != null)
                    {
                        lspap.grpf |= LsPap.Flags.fFmiAnm;
                    }
                }

                lspap.fJustify = (pap.Justify ? 1 : 0);

                if (pap.Wrap && pap.OptimalBreak)
                {
                    lspap.lsbrj = LsBreakJust.lsbrjBreakOptimal;
                    lspap.lskj = LsKJust.lskjFullMixed;
                }
                else
                {
                    lspap.lsbrj = LsBreakJust.lsbrjBreakJustify;
                    if (pap.Justify)
                    {
                        lspap.lskj = LsKJust.lskjFullInterWord;
                    }
                }

                lspap.lstflow = pap.RightToLeft ? LsTFlow.lstflowWS : LsTFlow.lstflowES;
            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("FetchPap", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        internal LsErr FetchLineProps(
            IntPtr              pols,               // Line Layout context
            int                 lscpFetch,          // character position to fetch
            int                 firstLineInPara,    // (bool) whether this the first line in paragraph
            ref LsLineProps     lsLineProps         // [out] line properties
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                TextStore store = FullText.TextStore;
                TextStore markerStore = FullText.TextMarkerStore;
                ParaProp pap = store.Pap;
                FormatSettings settings = store.Settings;

                lsLineProps = new LsLineProps();

                if (FullText.GetMainTextToMarkerIdealDistance() != 0)
                    lsLineProps.durLeft = TextFormatterImp.RealToIdeal(markerStore.Pap.TextMarkerProperties.Offset);
                else
                    lsLineProps.durLeft = settings.TextIndent;

                if (    pap.Wrap 
                    &&  pap.OptimalBreak
                    &&  settings.MaxLineWidth < FullText.FormatWidth)
                {
                    // durRightBreak & durRightJustify are the distances from the paragraph right margin
                    lsLineProps.durRightBreak = lsLineProps.durRightJustify = (FullText.FormatWidth -  settings.MaxLineWidth);
                }
            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("FetchLineProps", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        internal LsErr GetRunTextMetrics(
            System.IntPtr       pols,           // Line Layout context
            Plsrun              plsrun,         // plsrun
            LsDevice            lsDevice,       // kind of device
            LsTFlow             lstFlow,        // text flow
            ref LsTxM           lstTextMetrics  // [out] returning metrics
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                FullTextState fullText = FullText;
                TextStore store = fullText.StoreFrom(plsrun);
                lsrun = store.GetRun(plsrun);

                if (lsrun.Height > 0)
                {
                    lstTextMetrics.dvAscent = lsrun.BaselineOffset;
                    lstTextMetrics.dvMultiLineHeight = lsrun.Height;
                }
                else
                {
                    Typeface typeface = store.Pap.DefaultTypeface;
                    lstTextMetrics.dvAscent = (int)Math.Round(typeface.Baseline(store.Pap.EmSize, Constants.DefaultIdealToReal, store.Settings.TextSource.PixelsPerDip, fullText.TextFormattingMode));
                    lstTextMetrics.dvMultiLineHeight = (int)Math.Round(typeface.LineSpacing(store.Pap.EmSize, Constants.DefaultIdealToReal, store.Settings.TextSource.PixelsPerDip, fullText.TextFormattingMode));
                }

                lstTextMetrics.dvDescent = lstTextMetrics.dvMultiLineHeight - lstTextMetrics.dvAscent;
                lstTextMetrics.fMonospaced = 0;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetRunTextMetrics", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }



        internal unsafe LsErr GetRunCharWidths(
            IntPtr          pols,               // Line Layout context
            Plsrun          plsrun,             // plsrun
            LsDevice        device,             // kind of device
            char*           charString,         // character string
            int             stringLength,       // string length
            int             maxWidth,           // max width allowance
            LsTFlow         textFlow,           // text flow
            int*            charWidths,         // [out] returning char widths up to given upperbound
            ref int         totalWidth,         // [out] total run width
            ref int         stringLengthFitted  // [out] number of char fitted
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                TextFormatterImp formatter;

                if (FullText != null)
                {
                    lsrun = FullText.StoreFrom(plsrun).GetRun(plsrun);
                    formatter = FullText.Formatter;
                }
                else
                {
                    // LS calls this API at display time when tab leader is used.
                    // We generally do not carry full text state around at display time as it
                    // is rather heavy. LS also does not generally request formatting info at
                    // display time. However there are specific cases (such as tab leader) 
                    // when this might be needed. For better performance of all other more
                    // common cases, we treat this as an exception rather than the norm.
                    // We assume at this point that the current line has been formatted with
                    // full text state retained in the line.
                    #if DEBUG
                    // FullTextState property is only used in this Assert. 
                    // Put both the property and the assert under #if DEBUG to avoid
                    // FxCop violations
                    Debug.Assert(Draw.CurrentLine.FullTextState != null);
                    #endif
                    lsrun = Draw.CurrentLine.GetRun(plsrun);
                    formatter = Draw.CurrentLine.Formatter;
                }

                if (lsrun.Type == Plsrun.Text)
                {
                    Debug.Assert(lsrun.Shapeable != null && stringLength > 0);
                    lsrun.Shapeable.GetAdvanceWidthsUnshaped(charString, stringLength, TextFormatterImp.ToIdeal, charWidths);

                    totalWidth = 0;
                    stringLengthFitted = 0;

                    do
                    {
                        totalWidth += charWidths[stringLengthFitted];

                    } while (
                            ++stringLengthFitted < stringLength
                        && totalWidth <= maxWidth
                        );

                    if (totalWidth <= maxWidth && FullText != null)
                    {
                        int cpLimit = lsrun.OffsetToFirstCp + stringLengthFitted;
                        if (cpLimit > FullText.CpMeasured)
                        {
                            FullText.CpMeasured = cpLimit;
                        }
                    }
                }
                else
                {
                    //  synthetic run
                    charWidths[0] = 0;
                    totalWidth = 0;
                    stringLengthFitted = stringLength;
                }
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetRunCharWidths", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        internal LsErr GetDurMaxExpandRagged(
            IntPtr      pols,               // Line Layout context
            Plsrun      plsrun,             // plsrun
            LsTFlow     lstFlow,            // text flow
            ref int     maxExpandRagged     // [out] em width
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                // According to Knuth, the recommended value of the maximum "good" amount of 
                // empty space for the ragged case is the width of 3 space characters.
                // A space width is generally 1/3 of an em. 
                lsrun = FullText.StoreFrom(plsrun).GetRun(plsrun);
                maxExpandRagged = lsrun.EmSize;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetDurMaxExpandRagged", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        internal LsErr GetAutoNumberInfo(
            IntPtr          pols,               // Line Layout context
            ref LsKAlign    alignment,          // [out] Marker alignment
            ref LsChp       lschp,              // [out] Marker properties
            ref IntPtr      lsplsrun,           // [out] Marker run
            ref ushort      addedChar,          // [out] Character to add after marker
            ref LsChp       lschpAddedChar,     // [out] Added character properties
            ref IntPtr      lsplsrunAddedChar,  // [out] Added character run
            ref int         fWord95Model,        // [out] true iff follow Word95 autonumbering model
            ref int         offset,             // [out] Offset from marker to start of main text (relevant iff word95Model is true)
            ref int         width               // [out] Offset from margin to start of main text (relevant iff word95Model is true)
            )
        {
            LsErr lserr = LsErr.None;
            Plsrun plsrun = Plsrun.Undefined;
            LSRun lsrun = null;

            try
            {
                FullTextState fullTextState = FullText;
                TextStore markerStore = fullTextState.TextMarkerStore;
                TextStore store = fullTextState.TextStore;
                Debug.Assert(markerStore != null, "No marker store, yet autonumbering is specified!");

                int lscp = TextStore.LscpFirstMarker;
                int lsrunLength;

                do
                {
                    int lsrunOffset;

                    lsrun = markerStore.FetchLSRun(
                        lscp, 
                        fullTextState.TextFormattingMode,
                        fullTextState.IsSideways,
                        out plsrun, 
                        out lsrunOffset,
                        out lsrunLength
                        );

                    lscp += lsrunLength;

                } while (!TextStore.IsContent(plsrun));

                alignment = LsKAlign.lskalRight;

                lschp = new LsChp();
                lschp.idObj = (ushort)TextStore.ObjectId.Text_chp;

                SetChpFormat(lsrun.RunProp, ref lschp);

                addedChar = FullText.GetMainTextToMarkerIdealDistance() != 0 ? (ushort)'\t' : (ushort)0;

                lschpAddedChar = lschp;

                fWord95Model = 0;   // Word95 model requires precise marker width in which we never have
                offset = 0;         // marker offset is controlled by tab stop
                width = 0;

                lsplsrun = (IntPtr)plsrun;
                lsplsrunAddedChar = lsplsrun;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetAutoNumberInfo", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        internal LsErr GetRunUnderlineInfo(
            IntPtr          pols,           // Line Layout context
            Plsrun          plsrun,         // plsrun
            ref LsHeights   lsHeights,      // run height
            LsTFlow         textFlow,       // text flow direction
            ref LsULInfo    ulInfo          // [out] result underline info
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                lsrun = Draw.CurrentLine.GetRun(plsrun);

                Debug.Assert(
                    !TextStore.IsContent(plsrun) || lsrun.Type == Plsrun.Text || lsrun.Type == Plsrun.InlineObject,
                    "Invalid run"
                    );

                ulInfo = new LsULInfo();

                double underlinePositionInEm;
                double underlineThicknessInEm;

                if (lsrun.Shapeable != null)
                {
                    underlinePositionInEm = lsrun.Shapeable.UnderlinePosition;
                    underlineThicknessInEm = lsrun.Shapeable.UnderlineThickness;
                }
                else
                {
                    // e.g. underline on inline object
                    underlinePositionInEm = lsrun.RunProp.Typeface.UnderlinePosition;
                    underlineThicknessInEm = lsrun.RunProp.Typeface.UnderlineThickness;
                }

                ulInfo.cNumberOfLines = 1;
                ulInfo.dvpFirstUnderlineOffset = (int)Math.Round(lsrun.EmSize * -underlinePositionInEm);
                ulInfo.dvpFirstUnderlineSize = (int)Math.Round(lsrun.EmSize * underlineThicknessInEm);

                // Some fonts (e.g. Bodoni MT Condensed) have underline thickness value of zero,
                // or we can arrive at zero after integer rounding. Since Line Services require underline thickness
                // to be greater than zero, we replace zero and negative values with 1.
                // Note that the font driver already corrects zero thickness to something more reasonable,
                // but we can still end up with zero if em size multiplied by position is a small value that rounds to zero.
                Debug.Assert(ulInfo.dvpFirstUnderlineSize >= 0);

                if (ulInfo.dvpFirstUnderlineSize <= 0)
                    ulInfo.dvpFirstUnderlineSize = 1;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetAutoNumberInfo", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        internal LsErr GetRunStrikethroughInfo(
            IntPtr          pols,           // Line Layout context
            Plsrun          plsrun,         // plsrun
            ref LsHeights   lsHeights,      // run height
            LsTFlow         textFlow,       // text flow direction
            ref LsStInfo    stInfo          // [out] result strikethrough info
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                lsrun = Draw.CurrentLine.GetRun(plsrun);

                Debug.Assert(
                    !TextStore.IsContent(plsrun) || lsrun.Type == Plsrun.Text || lsrun.Type == Plsrun.InlineObject,
                    "Invalid run"
                    );

                stInfo = new LsStInfo();

                double strikeThroughPositionInEm;
                double strikeThroughThicknessInEm;

                GetLSRunStrikethroughMetrics(lsrun, out strikeThroughPositionInEm, out strikeThroughThicknessInEm);

                stInfo.cNumberOfLines = 1;
                stInfo.dvpLowerStrikethroughOffset = (int)Math.Round(lsrun.EmSize * strikeThroughPositionInEm);
                stInfo.dvpLowerStrikethroughSize = (int)Math.Round(lsrun.EmSize * strikeThroughThicknessInEm);

                Debug.Assert(stInfo.dvpLowerStrikethroughSize >= 0);

                // Since Line Services require strikethrough thickness to be greater than zero,
                // we replace potential zero and negative values with 1.
                // Note that the font driver already corrects zero thickness to something more reasonable,
                // but we can still end up with zero if em size multiplied by position is a small value that rounds to zero.
                if (stInfo.dvpLowerStrikethroughSize <= 0)
                    stInfo.dvpLowerStrikethroughSize = 1;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetRunStrikethroughInfo", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        private void GetLSRunStrikethroughMetrics(
            LSRun       lsrun,
            out double  strikeThroughPositionInEm,
            out double  strikeThroughThicknessInEm
            )
        {
            if (lsrun.Shapeable != null)
            {
                strikeThroughPositionInEm = lsrun.Shapeable.StrikethroughPosition;
                strikeThroughThicknessInEm = lsrun.Shapeable.StrikethroughThickness;
            }
            else
            {
                // e.g. strike on inline object
                strikeThroughPositionInEm = lsrun.RunProp.Typeface.StrikethroughPosition;
                strikeThroughThicknessInEm = lsrun.RunProp.Typeface.StrikethroughThickness;
            }
        }


        internal LsErr Hyphenate(
            IntPtr          pols,                   // Line Layout context
            int             fLastHyphenFound,       // whether last hyphen found?
            int             lscpLastHyphen,         // cp of the last found hyphen
            ref LsHyph      lastHyph,               // [in] last found hyphenation
            int             lscpWordStart,          // first character of word
            int             lscpExceed,             // first character in this word that exceeds column
            ref int         fHyphenFound,           // [out] hyphenation opportunity found?
            ref int         lscpHyphen,             // [out] cp of the character before hyphen
            ref LsHyph      lsHyph                  // [out] hyphen info
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                fHyphenFound = FullText.FindNextHyphenBreak(
                    lscpWordStart,
                    lscpExceed - lscpWordStart,
                    true,   // isCurrentAtWordStart
                    ref lscpHyphen,
                    ref lsHyph
                    ) ? 1 : 0;

                Invariant.Assert(fHyphenFound == 0 || (lscpHyphen >= lscpWordStart && lscpHyphen < lscpExceed));
            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("Hyphenate", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        
        internal LsErr GetNextHyphenOpp(
            IntPtr              pols,                   // Line Layout context
            int                 lscpStartSearch,        // LSCP to start search for hyphen opportunity
            int                 lsdcpSearch,            // number of LSCP to look for the hyphen opportunity
            ref int             fHyphenFound,           // [out] hyphen found
            ref int             lscpHyphen,             // [out] LSCP of character before hyphen
            ref LsHyph          lsHyph                  // [out] hyphen info
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                fHyphenFound = FullText.FindNextHyphenBreak(
                    lscpStartSearch,
                    lsdcpSearch,
                    false,  // !isCurrentAtWordStart
                    ref lscpHyphen,
                    ref lsHyph
                    ) ? 1 : 0;

                Invariant.Assert(fHyphenFound == 0 || (lscpHyphen >= lscpStartSearch && lscpHyphen < lscpStartSearch + lsdcpSearch));
            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetNextHyphenOpp", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        
        internal LsErr GetPrevHyphenOpp(
            IntPtr              pols,                   // Line Layout context
            int                 lscpStartSearch,        // LSCP to start search for hyphen opportunity
            int                 lsdcpSearch,            // number of LSCP to look for the hyphen opportunity
            ref int             fHyphenFound,           // [out] hyphen found
            ref int             lscpHyphen,             // [out] LSCP of character before hyphen
            ref LsHyph          lsHyph                  // [out] hyphen info
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                fHyphenFound = FullText.FindNextHyphenBreak(
                    // plus 1 here because LS also wants to examine whether hyphen can occur after
                    // the character identified by lscpStartSearch while the hyphenator generates 
                    // break before the character. This plus 1 is safe, it'll never trigger buffer
                    // overread since the code never read the character buffer at this index, it is
                    // properly bound-check'd.
                    lscpStartSearch + 1,
                    -lsdcpSearch,
                    false,  // !isCurrentAtWordStart
                    ref lscpHyphen,
                    ref lsHyph
                    ) ? 1 : 0;

                Invariant.Assert(fHyphenFound == 0 || (lscpHyphen > lscpStartSearch - lsdcpSearch && lscpHyphen <= lscpStartSearch));
            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetPrevHyphenOpp", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        internal LsErr DrawStrikethrough(
            IntPtr          pols,           // Line Layout context
            Plsrun          plsrun,         // plsrun
            uint            stType,         // kind of strike
            ref LSPOINT     ptOrigin,       // [in] drawing origin
            int             stLength,       // strike length
            int             stThickness,    // strike thickness
            LsTFlow         textFlow,       // text flow direction
            uint            displayMode,    // display mode
            ref LSRECT      clipRect        // [in] clipping rectangle
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;
            try
            {
                if (!TextStore.IsContent(plsrun))
                {
                    // dont draw for non-content run e.g. reversal
                    return LsErr.None;
                }

                TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                lsrun = currentLine.GetRun(plsrun);

                double strikeThroughPositionInEm;
                double strikeThroughThicknessInEm;

                GetLSRunStrikethroughMetrics(lsrun, out strikeThroughPositionInEm, out strikeThroughThicknessInEm);

                int baselineTop = ptOrigin.y + (int)Math.Round(lsrun.EmSize * strikeThroughPositionInEm);
                int overlineTop = baselineTop - (lsrun.BaselineOffset - (int)Math.Round(lsrun.EmSize * strikeThroughThicknessInEm));

                const uint locationMask = 
                    (1U << (int)TextDecorationLocation.OverLine) |
                    (1U << (int)TextDecorationLocation.Strikethrough) |
                    (1U << (int)TextDecorationLocation.Baseline);

                DrawTextDecorations(
                    lsrun,
                    locationMask,
                    ptOrigin.x,   // left
                    0,            // underline top; not used
                    overlineTop,
                    ptOrigin.y,   // strikethrough top from LS
                    baselineTop,
                    stLength,
                    stThickness,
                    textFlow
                    );
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("DrawStrikethrough", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        internal LsErr DrawUnderline(
            IntPtr pols,                // Line Layout context
            Plsrun      plsrun,         // plsrun
            uint        ulType,         // kind of underline
            ref LSPOINT ptOrigin,       // [in] drawing origin
            int         ulLength,       // underline length
            int         ulThickness,    // underline thickness
            LsTFlow     textFlow,       // text flow direction
            uint        displayMode,    // display mode
            ref LSRECT  clipRect        // [in] clipping rectangle
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;
            try
            {
                if (!TextStore.IsContent(plsrun))
                {
                    // dont draw for non-content run e.g. reversal
                    return LsErr.None;
                }

                lsrun = Draw.CurrentLine.GetRun(plsrun);

                const uint locationMask = (1U << (int)TextDecorationLocation.Underline);

                DrawTextDecorations(
                    lsrun,
                    locationMask,
                    ptOrigin.x,   // left
                    ptOrigin.y,   // underline top from LS
                    0,            // overline top; not used
                    0,            // strikethrough top; not used
                    0,            // baseline top; not used
                    ulLength,
                    ulThickness,
                    textFlow
                    );
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("DrawUnderline", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        private void DrawTextDecorations(
            LSRun    lsrun,
            uint     locationMask,
            int      left,
            int      underlineTop,
            int      overlineTop,
            int      strikethroughTop,
            int      baselineTop,
            int      length,
            int      thickness,
            LsTFlow  textFlow
            )
        {
            TextMetrics.FullTextLine currentLine = Draw.CurrentLine;

            // Draw paragraph-level text decorations (if any).
            TextDecorationCollection textDecorations = currentLine.TextDecorations;
            if (textDecorations != null)
            {
                DrawTextDecorationCollection(
                    lsrun,
                    locationMask,
                    textDecorations,
                    currentLine.DefaultTextDecorationsBrush,
                    left,
                    underlineTop,
                    overlineTop,
                    strikethroughTop,
                    baselineTop,
                    length,
                    thickness,
                    textFlow
                    );
            }

            // Draw run-level text decorations (if any).
            textDecorations = lsrun.RunProp.TextDecorations;
            if (textDecorations != null)
            {
                DrawTextDecorationCollection(
                    lsrun,
                    locationMask,
                    textDecorations,
                    lsrun.RunProp.ForegroundBrush,
                    left,
                    underlineTop,
                    overlineTop,
                    strikethroughTop,
                    baselineTop,
                    length,
                    thickness,
                    textFlow
                    );
            }
        }


        private void DrawTextDecorationCollection(
            LSRun                     lsrun,
            uint                      locationMask,
            TextDecorationCollection  textDecorations,
            Brush                     foregroundBrush,
            int                       left,
            int                       underlineTop,
            int                       overlineTop,
            int                       strikethroughTop,
            int                       baselineTop,
            int                       length,
            int                       thickness,
            LsTFlow                   textFlow
            )
        {
            Invariant.Assert(textDecorations != null);

            foreach (TextDecoration td in textDecorations)
            {
                if (((1U << (int)td.Location) & locationMask) != 0)
                {
                    switch (td.Location)
                    {
                        case TextDecorationLocation.Underline:
                            _boundingBox.Union(
                                DrawTextDecoration(
                                    lsrun,
                                    foregroundBrush,
                                    new LSPOINT(left, underlineTop),
                                    length,
                                    thickness,
                                    textFlow,
                                    td
                                    )
                                );
                            break;

                        case TextDecorationLocation.OverLine:
                            _boundingBox.Union(
                                DrawTextDecoration(
                                    lsrun,
                                    foregroundBrush,
                                    new LSPOINT(left, overlineTop),
                                    length,
                                    thickness,
                                    textFlow,
                                    td
                                    )
                                );
                            break;

                        case TextDecorationLocation.Strikethrough:
                            _boundingBox.Union(
                                DrawTextDecoration(
                                    lsrun,
                                    foregroundBrush,
                                    new LSPOINT(left, strikethroughTop),
                                    length,
                                    thickness,
                                    textFlow,
                                    td
                                    )
                                );
                            break;

                        case TextDecorationLocation.Baseline:
                            _boundingBox.Union(
                                DrawTextDecoration(
                                    lsrun,
                                    foregroundBrush,
                                    new LSPOINT(left, baselineTop),
                                    length,
                                    thickness,
                                    textFlow,
                                    td
                                    )
                                );
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Draw any text decoration line
        /// </summary>
        private Rect DrawTextDecoration(
            LSRun           lsrun,           // lsrun
            Brush           foregroundBrush, // default brush if text decoration has no pen
            LSPOINT         ptOrigin,        // drawing origin
            int             ulLength,        // underline length
            int             ulThickness,     // underline thickness
            LsTFlow         textFlow,        // text flow direction
            TextDecoration  textDecoration   //TextDecoration to be draw (add to sublinecollection
            )
        {
            switch (textFlow)
            {
                case LsTFlow.lstflowWS:
                case LsTFlow.lstflowNE:
                case LsTFlow.lstflowNW:
                    // line is drawn in the direction opposite to XY
                    ptOrigin.x -= ulLength;
                    break;
            }

            TextMetrics.FullTextLine currentLine = Draw.CurrentLine;

            if (currentLine.RightToLeft)
            {
                ptOrigin.x = -ptOrigin.x;
            }

            int u = currentLine.LSLineUToParagraphU(ptOrigin.x);

            Point baselineOrigin = LSRun.UVToXY(
                Draw.LineOrigin,
                Draw.VectorToLineOrigin,
                u,
                currentLine.BaselineOffset,
                currentLine
                );

            Point lineOrigin = LSRun.UVToXY(
                Draw.LineOrigin,
                Draw.VectorToLineOrigin,
                u,
                ptOrigin.y + lsrun.BaselineMoveOffset,
                currentLine
                );
            //
            // Resolve the final thickness of the text decoration
            //
            double penThickness = 1.0;
            if (textDecoration.Pen != null)
            {
                penThickness = textDecoration.Pen.Thickness;
            }

            // multiplied penThickness value by PenThicknessUnit to get final absolute thickenss
            switch (textDecoration.PenThicknessUnit)
            {
                case TextDecorationUnit.FontRecommended:
                    // ulThickness is the averaged thickness for Underline case
                    penThickness = currentLine.Formatter.IdealToReal(ulThickness * penThickness, currentLine.PixelsPerDip);
                    break;

                case TextDecorationUnit.FontRenderingEmSize:
                    penThickness = currentLine.Formatter.IdealToReal(penThickness * lsrun.EmSize, currentLine.PixelsPerDip);
                    break;

                case TextDecorationUnit.Pixel:
                    // Don't need to change the thickness for absolute pixels
                    break;

                default:
                    Debug.Assert(false, "Not supported TextDecorationUnit");
                    break;
            }

            // pen thickness can be negative, which has the same effect as its absolute value
            penThickness = Math.Abs(penThickness);            

            //
            // Resolve text decoration offset unit
            //
            double unitValue = 1.0;
            switch (textDecoration.PenOffsetUnit)
            {
                case TextDecorationUnit.FontRecommended:
                    // lineOrigin.Y is the averaged position for Underline case.
                    unitValue = (lineOrigin.Y - baselineOrigin.Y);
                    break;

                case TextDecorationUnit.FontRenderingEmSize:
                    unitValue = currentLine.Formatter.IdealToReal(lsrun.EmSize, currentLine.PixelsPerDip);
                    break;

                case TextDecorationUnit.Pixel:
                    unitValue = 1.0;
                    break;

                default:
                    Debug.Assert(false, "Not supported TextDecorationUnit");
                    break;
            }


            double lineLength = currentLine.Formatter.IdealToReal(ulLength, currentLine.PixelsPerDip);

            DrawingContext drawingContext = Draw.DrawingContext;

            if (drawingContext != null)
            {      
                
                
                // Thickness used to draw the text decoration. 
                // It might be scaled to account for PenOffset animation
                double drawingPenThickness = penThickness;

                // The origin used to draw the text decoration
                // It might be offset to account for PenOffset animation
                Point  drawingLineOrigin   = lineOrigin;

                bool animated = !textDecoration.CanFreeze && (unitValue != 0);

                int pushCount = 0; // counter for the number of explicit DrawingContext.Push()
                
                // put the guideline collection for the text decoration.
                Draw.SetGuidelineY(baselineOrigin.Y);                
                
                try 
                {
                    if (animated)
                    {                    
                        //
                        // When TextDecoration has animation, we use Translate transform to
                        // directly apply animations for PenOffset property.
                        // The final position is:
                        //      (calculated position) + (TextDecoration.PenOffset) * (Unit Value) 
                        // We also apply a ScaleTransform for the (Unit Value) factor. When Unit Value is zero
                        // there is no need to perform animation
                        //                        

                        ScaleTransform scaleTransform = new ScaleTransform(
                            1.0,        // X scale
                            unitValue,  // y scale 
                            drawingLineOrigin.X,  // reference point of scaling
                            drawingLineOrigin.Y  // reference point of scaling
                            );
                           
                        TranslateTransform yTranslate = new TranslateTransform(
                            0,                       // x translate
                            textDecoration.PenOffset // y translate
                            );

                        // adjust the pen's thickness as it will be scaled back by the scale transform
                        drawingPenThickness = drawingPenThickness / Math.Abs(unitValue);
                                                    
                        // applied transforms
                        drawingContext.PushTransform(scaleTransform);
                        pushCount++;
                        drawingContext.PushTransform(yTranslate);
                        pushCount++;

                    }
                    else
                    {
                        // TextDecoration doesn't have animation, adjust the line origin directly
                        drawingLineOrigin.Y += unitValue * textDecoration.PenOffset;
                    }                    

                    // Apply the pair of guidelines: one for baseline and another
                    // for top edge of undelining line. Both will be snapped to pixel grid.
                    // Guideline pairing algorithm detects the case when these two
                    // guidelines happen to be close to one another and provides
                    // synchronous snapping, so that the gap between baseline and
                    // undelining line does not depend on the position of text line.
                    drawingContext.PushGuidelineY2(baselineOrigin.Y, drawingLineOrigin.Y - drawingPenThickness * 0.5 - baselineOrigin.Y);
                    pushCount++;

                    //
                    // Drawing the actual text decoration line
                    // As perf optimization, if the Pen given is null, the text decoration is drawn as Rectangle
                    // to avoid the cost of creating a new Pen (estimated to be 200 bytes overhead). 
                    // If a non-null pen is given, DrawLine will be used and we will pay the price of create a new pen.
                    // However, this will not be very common.
                    //

                    if (textDecoration.Pen == null)
                    {
                        // Draw text decoration by DrawRectangle. It avoids the overhead of creating a new Pen.
                        drawingContext.DrawRectangle(
                            foregroundBrush,               // fill using foreground
                            null,                          // null pen for rectangle stroke
                            new Rect(
                                drawingLineOrigin.X,
                                drawingLineOrigin.Y - drawingPenThickness * 0.5,
                                lineLength,
                                drawingPenThickness
                                )                    
                            );                    
                    }
                    else                    
                    {
                        // a pen is specified for the text decoration. need to create a new copy 
                        // in order to set the thickness. 
                        // Try to get a copy through CloneCurrentValue() first because it can resolve 
                        // the animation on the Pen. 

                        Pen textDecorationPen = textDecoration.Pen.CloneCurrentValue();
                        if (Object.ReferenceEquals(textDecoration.Pen, textDecorationPen))
                        {
                            // If it is still the same pen, we'll call Copy() to get a new one.
                            textDecorationPen = textDecoration.Pen.Clone();
                        }
                        
                        textDecorationPen.Thickness = drawingPenThickness;                  
                        
                        // draw the text decoration
                        drawingContext.DrawLine(
                            textDecorationPen,
                            drawingLineOrigin,
                            new Point(drawingLineOrigin.X + lineLength, drawingLineOrigin.Y)
                            );
                    }
                }
                finally 
                {               
                    for (int i = 0; i < pushCount; i++)
                    {
                        drawingContext.Pop(); 
                    }

                    Draw.UnsetGuidelineY();
                }
            }
            
            return new Rect(
                lineOrigin.X,
                lineOrigin.Y + unitValue * textDecoration.PenOffset - penThickness * 0.5,
                lineLength,
                penThickness
                );
        }


        internal unsafe LsErr DrawTextRun(
            IntPtr          pols,               // Line Layout context
            Plsrun          plsrun,             // plsrun
            ref LSPOINT     ptText,             // [in] text origin
            char*           pwchText,           // character string
            int*            piCharAdvances,     // char advance widths
            int             cchText,            // text length
            LsTFlow         textFlow,           // text flow
            uint            displayMode,        // draw in transparent or opaque
            ref LSPOINT     ptRun,              // [in] run origin
            ref LsHeights   lsHeights,          // [in] run height
            int             dupRun,             // run length
            ref LSRECT      clipRect            // [in] from DisplayLine's clip rectangle param
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                lsrun = currentLine.GetRun(plsrun);

                GlyphRun glyphRun = ComputeUnshapedGlyphRun(
                    lsrun, 
                    textFlow, 
                    currentLine.Formatter,
                    true,       // origin of the glyph run provided at drawing time                    
                    ptText, 
                    dupRun, 
                    cchText, 
                    pwchText, 
                    piCharAdvances,
                    currentLine.IsJustified
                    );

                if (glyphRun != null)
                {
                    DrawingContext drawingContext = Draw.DrawingContext;

                    Draw.SetGuidelineY(glyphRun.BaselineOrigin.Y);                    

                    try 
                    {
                        _boundingBox.Union(
                            lsrun.DrawGlyphRun(
                                drawingContext, 
                                null,   // draw with the run's foreground brush
                                glyphRun
                                )
                            );
                    }
                    finally
                    {
                        Draw.UnsetGuidelineY();
                    }
                }
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("DrawTextRun", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        internal LsErr FInterruptShaping(
            IntPtr          pols,               // Line Layout context
            LsTFlow         textFlow,           // text flow
            Plsrun          plsrunFirst,        // first run
            Plsrun          plsrunSecond,       // second run
            ref int         fIsInterruptOk      // [out] disconnect glyphs between runs?
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                TextStore store = FullText.StoreFrom(plsrunFirst);

                if (    !TextStore.IsContent(plsrunFirst)
                    ||  !TextStore.IsContent(plsrunSecond))
                {
                    fIsInterruptOk = 1;
                    return LsErr.None;
                }


                LSRun lsrunFirst = store.GetRun(plsrunFirst);
                LSRun lsrunSecond = store.GetRun(plsrunSecond);

                // shape any runs together as long as they share the following attributes
                fIsInterruptOk = !(
                    // same bidi level
                    lsrunFirst.BidiLevel == lsrunSecond.BidiLevel
                    // both are shapeable and equals
                    && lsrunFirst.Shapeable != null
                    && lsrunSecond.Shapeable != null
                    && lsrunFirst.Shapeable.CanShapeTogether(lsrunSecond.Shapeable)
                    ) ? 1 : 0;

            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("FInterruptShaping", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        internal static CultureInfo GetNumberCulture(TextRunProperties properties, out NumberSubstitutionMethod method)
        {
            NumberSubstitution sub = properties.NumberSubstitution;
            if (sub == null)
            {
                method = NumberSubstitutionMethod.AsCulture;
                return CultureMapper.GetSpecificCulture(properties.CultureInfo);
            }

            method = sub.Substitution;

            switch (sub.CultureSource)
            {
                case NumberCultureSource.Text:
                    return CultureMapper.GetSpecificCulture(properties.CultureInfo);

                case NumberCultureSource.User:
                    return CultureInfo.CurrentCulture;

                case NumberCultureSource.Override:
                    return sub.CultureOverride;
            }

            return null;
        }

        /// <summary>
        /// LineServices to get glyph indices and related per-glyph data generated from
        /// the shaping engine of the specified input character string.
        /// </summary>
        /// <remarks>
        /// The suffix "Redefined" in the method name indicates that this is one
        /// such callback that is not managed code friendly and requires redefinition
        /// to be able to perform at its optimal performance. 
        /// 
        /// Similar to FetchRunRedefined, we redefine and wrap this callback with the 
        /// proxy GetGlyphs callback on the unmanaged side with the same goal. And that is
        /// is to avoid high frequency pinning of managed arrays generated by OpenType 
        /// library via shaping engine code.
        /// 
        /// When the specified fixed-size buffer is too small, this method returns LsErr.None
        /// with 'fIsGlyphBuffersUsed' set to false and 'glyphCount' indicates the number
        /// of glyphs generated. The unmanaged GetGlyphs proxy callback then extends the buffer 
        /// size to at least the returning value of 'glyphCount' and call the method again.
        /// 
        /// </remarks>
        internal unsafe LsErr GetGlyphsRedefined(
            IntPtr                      pols,                   // Line Layout context
            IntPtr*                     plsplsruns,             // array of plsruns
            int*                        pcchPlsrun,             // array of character count per run
            int                         plsrunCount,            // number of runs
            char*                       pwchText,               // character string
            int                         cchText,                // character count
            LsTFlow                     textFlow,               // text flow direction
            ushort*                     puGlyphsBuffer,         // [in/out] fixed-size buffer for glyph indices
            uint*                       piGlyphPropsBuffer,     // [in/out] fixed-size buffer for glyph properties list
            int                         cgiGlyphBuffers,        // glyph buffers length in glyphs
            ref int                     fIsGlyphBuffersUsed,    // [out] Boolean flag indicates glyph buffers being used
            ushort*                     puClusterMap,           // [out] character-to-glyph cluster map
            ushort*                     puCharProperties,       // [out] character properties
            int*                        pfCanGlyphAlone,        // [out] parallel to character codes: glyphing does not depend on neighbor?
            ref int                     glyphCount              // [out] glyph buffer length and returning actual glyph count
            )
        {
            Invariant.Assert(puGlyphsBuffer != null && piGlyphPropsBuffer != null);

            LsErr lserr = LsErr.None;
            LSRun lsrunFirst = null;

            try
            {
                LSRun[] lsruns = RemapLSRuns(plsplsruns, plsrunCount);
                lsrunFirst = lsruns[0];

                Debug.Assert(lsrunFirst.Shapeable != null);
                Debug.Assert(cchText > 0); // LineServices should not pass in zero character count;

                bool isRightToLeft = ((lsrunFirst.BidiLevel & 1) != 0);

                DWriteFontFeature[][] fontFeatures;
                uint[]                fontFeatureRanges;
                uint                  actualGlyphCount;
                checked 
                {
                    uint uCchText = (uint)cchText;
                    LSRun.CompileFeatureSet(lsruns, pcchPlsrun, uCchText, out fontFeatures, out fontFeatureRanges);
                    
                    GlyphTypeface glyphTypeface = lsrunFirst.Shapeable.GlyphTypeFace;
                    
                    FullText.Formatter.TextAnalyzer.GetGlyphs(
                        pwchText,
                        uCchText,
                        glyphTypeface.FontDWrite,
                        glyphTypeface.BlankGlyphIndex,
                        false,
                        isRightToLeft,
                        lsrunFirst.RunProp.CultureInfo,
                        fontFeatures,
                        fontFeatureRanges,
                        (uint)cgiGlyphBuffers,
                        FullText.TextFormattingMode,
                        lsrunFirst.Shapeable.ItemProps,
                        puClusterMap,
                        puCharProperties,
                        puGlyphsBuffer,
                        piGlyphPropsBuffer,
                        pfCanGlyphAlone,
                        out actualGlyphCount
                        );

                    glyphCount = (int)actualGlyphCount;
                   
                    if (glyphCount <= cgiGlyphBuffers)
                    {
                        fIsGlyphBuffersUsed = 1;
                    }
                    else
                    {
                        fIsGlyphBuffersUsed = 0;
                    }
                }
            }
            catch (Exception e)
            {
                SaveException(e, (Plsrun)(plsplsruns[0]), lsrunFirst);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetGlyphsRedefined", (Plsrun)(plsplsruns[0]), lsrunFirst);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
            
        }


        internal unsafe LsErr GetGlyphPositions(
            IntPtr                      pols,               // Line Layout context
            IntPtr*                     plsplsruns,         // array of plsruns
            int*                        pcchPlsrun,         // array of character count per run
            int                         plsrunCount,        // number of runs
            LsDevice                    device,             // on reference or presentation device
            char*                       pwchText,           // character string
            ushort*                     puClusterMap,       // character-to-glyph cluster map
            ushort*                     puCharProperties,   // character properties
            int                         cchText,            // character count
            ushort*                     puGlyphs,           // glyph indices
            uint*                       piGlyphProperties,  // glyph properties
            int                         glyphCount,         // glyph count
            LsTFlow                     textFlow,           // text flow direction
            int*                        piGlyphAdvances,    // [out] glyph advances
            GlyphOffset*                piiGlyphOffsets     // [out] glyph offsets
            )
        {            
            LsErr lserr = LsErr.None;
            LSRun lsrunFirst = null;

            try
            {
                LSRun[] lsruns = RemapLSRuns(plsplsruns, plsrunCount);
                lsrunFirst = lsruns[0];

                bool isRightToLeft = ((lsrunFirst.BidiLevel & 1) != 0);

                GlyphOffset[] glyphOffset;

                GlyphTypeface glyphTypeface = lsrunFirst.Shapeable.GlyphTypeFace;

                DWriteFontFeature[][] fontFeatures;
                uint[] fontFeatureRanges;
                LSRun.CompileFeatureSet(lsruns, pcchPlsrun, checked((uint)cchText), out fontFeatures, out fontFeatureRanges);

                
                FullText.Formatter.TextAnalyzer.GetGlyphPlacements(
                    pwchText,
                    puClusterMap,
                    (ushort*)puCharProperties,
                    (uint)cchText,
                    puGlyphs,
                    piGlyphProperties,
                    (uint)glyphCount,
                    glyphTypeface.FontDWrite,
                    lsrunFirst.Shapeable.EmSize,
                    TextFormatterImp.ToIdeal,
                    false,
                    isRightToLeft,
                    lsrunFirst.RunProp.CultureInfo,
                    fontFeatures,
                    fontFeatureRanges,
                    FullText.TextFormattingMode,
                    lsrunFirst.Shapeable.ItemProps,
                    (float)FullText.StoreFrom(lsrunFirst.Type).Settings.TextSource.PixelsPerDip,
                    piGlyphAdvances,
                    out glyphOffset
                    );

                for (int i = 0; i < glyphCount; ++i)
                {
                    piiGlyphOffsets[i].du = glyphOffset[i].du;
                    piiGlyphOffsets[i].dv = glyphOffset[i].dv;
                }                
                 
            }
            catch (Exception e)
            {
                SaveException(e, (Plsrun)(plsplsruns[0]), lsrunFirst);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetGlyphPositions", (Plsrun)(plsplsruns[0]), lsrunFirst);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
            
        }


        /// <summary>
        /// Generate a list of correspondent lsruns
        /// </summary>
        private unsafe LSRun[] RemapLSRuns(
            IntPtr*         plsplsruns,
            int             plsrunCount
            )
        {
            LSRun[] lsruns = new LSRun[plsrunCount];
            TextStore store = FullText.StoreFrom((Plsrun)(*plsplsruns));

            for (int i = 0; i < lsruns.Length; i++)
            {
                Plsrun plsrun = (Plsrun)plsplsruns[i];
                lsruns[i] = store.GetRun(plsrun);
                Debug.Assert(TextStore.IsContent(plsrun) && lsruns[i] != null);
            }
            return lsruns;
        }


        internal unsafe LsErr DrawGlyphs(
            IntPtr                      pols,                       // Line Layout context
            Plsrun                      plsrun,                     // plsrun
            char*                       pwchText,                   // character string
            ushort*                     puClusterMap,               // character-to-cluster map
            ushort*                     puCharProperties,           // character properties
            int                         charCount,                  // character count
            ushort*                     puGlyphs,                   // glyph indices
            int*                        piJustifiedGlyphAdvances,   // justified glyph advances
            int*                        piGlyphAdvances,            // original ideal glyph advances
            GlyphOffset*                piiGlyphOffsets,            // glyph offsets
            uint*                       piGlyphProperties,          // glyph properties
            LsExpType*                  plsExpType,                 // glyph expansion types
            int                         glyphCount,                 // glyph count
            LsTFlow                     textFlow,                   // text flow
            uint                        displayMode,                // draw transparent or opaque
            ref LSPOINT                 ptRun,                      // [in] display position (at baseline)
            ref LsHeights               lsHeights,                  // [in] run height metrics
            int                         runWidth,                   // run overall advance width
            ref LSRECT                  clippingRect                // [in] clipping rectangle if any applied
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                lsrun = currentLine.GetRun(plsrun);

                Debug.Assert(TextStore.IsContent(plsrun) && lsrun.Shapeable != null);

                GlyphRun glyphRun = ComputeShapedGlyphRun(
                    lsrun,
                    currentLine.Formatter,
                    true,           // origin of the glyph run provided at drawing time
                    ptRun,
                    charCount,
                    pwchText,
                    puClusterMap,
                    glyphCount,
                    puGlyphs,
                    piJustifiedGlyphAdvances,
                    piiGlyphOffsets,
                    currentLine.IsJustified
                    );

                if (glyphRun != null)
                {
                    DrawingContext drawingContext = Draw.DrawingContext;

                    Draw.SetGuidelineY(glyphRun.BaselineOrigin.Y);

                    try 
                    {
                        _boundingBox.Union(
                            lsrun.DrawGlyphRun(
                                drawingContext, 
                                null,     // draw with the run's foreground
                                glyphRun
                                )
                            );
                    }
                    finally 
                    {
                        Draw.UnsetGuidelineY();
                    }

                }
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("DrawGlyphs", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        /// <summary>
        /// LS calls this method to fill in compression amount between characters in
        /// full-mixed justification used only by optimal break mode. It may fill in 
        /// the critical _exception member.
        /// </summary>
        internal unsafe LsErr GetCharCompressionInfoFullMixed(
            IntPtr              pols,                   // Line Layout context
            LsDevice            device,                 // kind of device
            LsTFlow             textFlow,               // text flow
            LsCharRunInfo       *plscharrunInfo,        // char-based run info
            LsNeighborInfo      *plsneighborInfoLeft,   // left neighbor info
            LsNeighborInfo      *plsneighborInfoRight,  // right neigbor info
            int                 maxPriorityLevel,       // maximum priority level
            int**               pplscompressionLeft,    // [in/out] fill in left compression amount per priority level on the way out
            int**               pplscompressionRight    // [in/out] fill in right compression amount per priority level on the way out
            )
        {
            LsErr lserr = LsErr.None;
            Plsrun plsrun = Plsrun.Undefined;
            LSRun lsrun = null;

            try
            {
                Invariant.Assert(maxPriorityLevel == 3);

                plsrun = plscharrunInfo->plsrun;
                lsrun = FullText.StoreFrom(plsrun).GetRun(plsrun);

                return AdjustChars(
                    plscharrunInfo,
                    false,  // compressing
                    (int)(lsrun.EmSize * Constants.MinInterWordCompressionPerEm),
                    pplscompressionLeft,
                    pplscompressionRight
                    );
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetCharCompressionInfoFullMixed", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        /// <summary>
        /// LS calls this method to fill in expansion amount between characters in
        /// full-mixed justification used only by optimal break mode. It may fill in 
        /// the critical _exception member.
        /// </summary>
        internal unsafe LsErr GetCharExpansionInfoFullMixed(
            IntPtr              pols,                   // Line Layout context
            LsDevice            device,                 // kind of device
            LsTFlow             textFlow,               // text flow
            LsCharRunInfo       *plscharrunInfo,        // char-based run info
            LsNeighborInfo      *plsneighborInfoLeft,   // left neighbor info
            LsNeighborInfo      *plsneighborInfoRight,  // right neigbor info
            int                 maxPriorityLevel,       // maximum priority level
            int**               pplsexpansionLeft,      // [in/out] fill in left expansion amount per priority level on the way out
            int**               pplsexpansionRight      // [in/out] fill in right expansion amount per priority level on the way out
            )
        {
            LsErr lserr = LsErr.None;
            Plsrun plsrun = Plsrun.Undefined;
            LSRun lsrun = null;

            try
            {
                Invariant.Assert(maxPriorityLevel == 3);

                plsrun = plscharrunInfo->plsrun;
                lsrun = FullText.StoreFrom(plsrun).GetRun(plsrun);

                return AdjustChars(
                    plscharrunInfo,
                    true,   // expanding
                    (int)(lsrun.EmSize * Constants.MaxInterWordExpansionPerEm),
                    pplsexpansionLeft,
                    pplsexpansionRight
                    );
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetCharExpansionInfoFullMixed", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        /// <summary>
        /// Adjust characters at inter-word spacing position targetting the specified amount.
        /// </summary>
        private unsafe LsErr AdjustChars(
            LsCharRunInfo       *plscharrunInfo,
            bool                expanding,
            int                 interWordAdjustTo,
            int**               pplsAdjustLeft,
            int**               pplsAdjustRight
            )
        {
            char* pwch = plscharrunInfo->pwch;
            int cchRun = plscharrunInfo->cwch;


            for (int i = 0; i < cchRun; i++)
            {
                int adjustedCharWidth = plscharrunInfo->rgduNominalWidth[i] + plscharrunInfo->rgduChangeLeft[i] + plscharrunInfo->rgduChangeRight[i];

                // no adjust to the left
                pplsAdjustLeft[0][i] = 0;
                pplsAdjustLeft[1][i] = 0;
                pplsAdjustLeft[2][i] = 0;

                // no valid adjustment except at interword spacing
                pplsAdjustRight[0][i] = 0;
                pplsAdjustRight[1][i] = 0;
                pplsAdjustRight[2][i] = 0;

                ushort flags = (ushort)(Classification.CharAttributeOf((int)Classification.GetUnicodeClassUTF16(pwch[i]))).Flags;
                if ((flags & ((ushort)CharacterAttributeFlags.CharacterSpace)) != 0)
                {
                    if (expanding)
                    {
                        int expandedBy = Math.Max(0, interWordAdjustTo - adjustedCharWidth);
                        pplsAdjustRight[0][i] = expandedBy;
                        pplsAdjustRight[1][i] = expandedBy * Constants.AcceptableLineStretchability;
                        pplsAdjustRight[2][i] = FullText.FormatWidth;
                    }
                    else
                    {
                        pplsAdjustRight[0][i] = Math.Max(0, adjustedCharWidth - interWordAdjustTo);
                    }
                }
                else if (expanding)
                {
                    // emergency expansion, use the column width as maximum allowance
                    pplsAdjustRight[2][i] = FullText.FormatWidth; 
                }
            }
            return LsErr.None;
        }


        /// <summary>
        /// LS calls this method to fill in compression amount between glyphs in
        /// full-mixed justification used only by optimal break mode. It may fill in 
        /// the critical _exception member.
        /// </summary>
        internal unsafe LsErr GetGlyphCompressionInfoFullMixed(
            IntPtr              pols,                   // Line Layout context
            LsDevice            device,                 // kind of device
            LsTFlow             textFlow,               // text flow
            LsGlyphRunInfo      *plsglyphrunInfo,       // glyph-based run info
            LsNeighborInfo      *plsneighborInfoLeft,   // left neighbor info
            LsNeighborInfo      *plsneighborInfoRight,  // right neigbor info
            int                 maxPriorityLevel,       // maximum priority level
            int                 **pplscompressionLeft,  // [in/out] fill in left compression amount per priority level on the way out
            int                 **pplscompressionRight  // [in/out] fill in right compression amount per priority level on the way out
            )
        {
            LsErr lserr = LsErr.None;
            Plsrun plsrun = Plsrun.Undefined;
            LSRun lsrun = null;

            try
            {
                Invariant.Assert(maxPriorityLevel == 3);

                plsrun = plsglyphrunInfo->plsrun;
                lsrun = FullText.StoreFrom(plsrun).GetRun(plsrun);
                int em = lsrun.EmSize;

                return CompressGlyphs(
                    plsglyphrunInfo,
                    (int)(em * Constants.MinInterWordCompressionPerEm),
                    pplscompressionLeft,
                    pplscompressionRight
                    );
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetGlyphCompressionInfoFullMixed", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        /// <summary>
        /// Compress glyphs at inter-word spacing position targetting the specified compression amount.
        /// Further compression beyond the specified amount at inter-word or inter-letter positions 
        /// is not allowed at all time.
        /// </summary>
        private unsafe LsErr CompressGlyphs(
            LsGlyphRunInfo      *plsglyphrunInfo,
            int                 interWordCompressTo,
            int                 **pplsCompressionLeft,
            int                 **pplsCompressionRight
            )
        {
            char* pwch = plsglyphrunInfo->pwch;
            ushort* pgmap = plsglyphrunInfo->rggmap;
            int cchRun = plsglyphrunInfo->cwch;
            int cgiRun = plsglyphrunInfo->cgindex;

            int ich = 0;
            int igi = pgmap[ich];
            int cgi = 0;


            while (ich < cchRun)
            {
                // get the number of chars of the current cluster
                int cch = 1;
                while (ich + cch < cchRun && pgmap[ich + cch] == igi)
                    cch++;

                // get the number of glyphs of the current cluster
                cgi = (ich + cch == cchRun) ? cgiRun - igi : pgmap[ich + cch] - igi;

                int i, j;

                // scan cluster to find interword spacing
                for (j = 0; j < cch; j++)
                {
                    ushort flags = (ushort)(Classification.CharAttributeOf((int)Classification.GetUnicodeClassUTF16(pwch[ich + j]))).Flags;
                    if ((flags & ((ushort)CharacterAttributeFlags.CharacterSpace)) != 0)
                        break;
                }

                int glyphAdvance = 0;
                for (i = 0; i < cgi; i++)
                {
                    glyphAdvance += plsglyphrunInfo->rgduWidth[igi + i];

                    // no compression to the left
                    pplsCompressionLeft[0][igi + i] = 0;
                    pplsCompressionLeft[1][igi + i] = 0;
                    pplsCompressionLeft[2][igi + i] = 0;

                    // no compression except at interword spacing
                    pplsCompressionRight[0][igi + i] = 0;
                    pplsCompressionRight[1][igi + i] = 0;
                    pplsCompressionRight[2][igi + i] = 0;

                    if (    i == cgi - 1
                        &&  cch == 1
                        &&  j < cch
                        )
                    {
                        // cluster has interword space, compress to the right of the last glyph of the cluster
                        pplsCompressionRight[0][igi + i] = Math.Max(0, glyphAdvance - interWordCompressTo);
                    }
                }

                ich += cch;
                igi += cgi;
            }

            Invariant.Assert(igi == cgiRun);
            return LsErr.None;
        }


        /// <summary>
        /// LS calls this method to fill in expansion amount between glyphs in
        /// full-mixed justification used only by optimal break mode. 
        /// </summary>
        internal unsafe LsErr GetGlyphExpansionInfoFullMixed(
            IntPtr              pols,                   // Line Layout context
            LsDevice            device,                 // kind of device
            LsTFlow             textFlow,               // text flow
            LsGlyphRunInfo      *plsglyphrunInfo,       // glyph-based run info
            LsNeighborInfo      *plsneighborInfoLeft,   // left neighbor info
            LsNeighborInfo      *plsneighborInfoRight,  // right neigbor info
            int                 maxPriorityLevel,       // maximum priority level
            int                 **pplsexpansionLeft,    // [in/out] fill in left expansion amount per priority level on the way out
            int                 **pplsexpansionRight,   // [in/out] fill in right expansion amount per priority level on the way out
            LsExpType           *plsexptype,            // [in/out] fill in glyph expansion type for each glyph
            int                 *pduMinInk              // [in/out] fill in glyph minimum expansion for exptAddInkContinuous
            )
        {
            LsErr lserr = LsErr.None;
            Plsrun plsrun = Plsrun.Undefined;
            LSRun lsrun = null;

            try
            {
                Invariant.Assert(maxPriorityLevel == 3);

                plsrun = plsglyphrunInfo->plsrun;
                lsrun = FullText.StoreFrom(plsrun).GetRun(plsrun);
                int em = lsrun.EmSize;

                return ExpandGlyphs(
                    plsglyphrunInfo,
                    (int)(em * Constants.MaxInterWordExpansionPerEm),
                    pplsexpansionLeft,
                    pplsexpansionRight,
                    plsexptype,
                    LsExpType.AddWhiteSpace,    // inter-word expansion type

                    // No inter-letter expansion for RTL run for now
                    ((lsrun.BidiLevel & 1) == 0 ? LsExpType.AddWhiteSpace : LsExpType.None)
                    );
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetGlyphExpansionInfoFullMixed", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        /// <summary>
        /// Expand glyphs at inter-word spacing position targetting the specified expansion amount.
        /// Inter-letter expansion may be allowed in emergency case.
        /// </summary>
        private unsafe LsErr ExpandGlyphs(
            LsGlyphRunInfo      *plsglyphrunInfo,
            int                 interWordExpandTo,
            int                 **pplsExpansionLeft,
            int                 **pplsExpansionRight,
            LsExpType           *plsexptype,
            LsExpType           interWordExpansionType,
            LsExpType           interLetterExpansionType
            )
        {
            char* pwch = plsglyphrunInfo->pwch;
            ushort* pgmap = plsglyphrunInfo->rggmap;
            int cchRun = plsglyphrunInfo->cwch;
            int cgiRun = plsglyphrunInfo->cgindex;

            int ich = 0;
            int igi = pgmap[ich];
            int cgi = 0;


            while (ich < cchRun)
            {
                // get the number of chars of the current cluster
                int cch = 1;
                while (ich + cch < cchRun && pgmap[ich + cch] == igi)
                    cch++;

                // get the number of glyphs of the current cluster
                cgi = (ich + cch == cchRun) ? cgiRun - igi : pgmap[ich + cch] - igi;

                int i, j;

                // scan cluster to find interword spacing
                for (j = 0; j < cch; j++)
                {
                    ushort flags = (ushort)(Classification.CharAttributeOf((int)Classification.GetUnicodeClassUTF16(pwch[ich + j]))).Flags;
                    if ((flags & ((ushort)CharacterAttributeFlags.CharacterSpace)) != 0)
                        break;
                }

                int glyphAdvance = 0;
                for (i = 0; i < cgi; i++)
                {
                    glyphAdvance += plsglyphrunInfo->rgduWidth[igi + i];

                    // no expansion to the left
                    pplsExpansionLeft[0][igi + i] = 0;
                    pplsExpansionLeft[1][igi + i] = 0;
                    pplsExpansionLeft[2][igi + i] = 0;

                    // no expansion except at interword spacing
                    pplsExpansionRight[0][igi + i] = 0;
                    pplsExpansionRight[1][igi + i] = 0;
                    pplsExpansionRight[2][igi + i] = 0;

                    if (i == cgi - 1)
                    {
                        if (cch == 1 && j < cch)
                        {
                            // cluster has interword space, expand to the right of the last glyph of the cluster
                            int expandedBy = Math.Max(0, interWordExpandTo - glyphAdvance);
                            pplsExpansionRight[0][igi + i] = expandedBy;
                            pplsExpansionRight[1][igi + i] = expandedBy * Constants.AcceptableLineStretchability;
                            pplsExpansionRight[2][igi + i] = FullText.FormatWidth;
                            plsexptype[igi + i] = interWordExpansionType;
                        }
                        else
                        {
                            // emergency expansion, use the column width as maximum allowance
                            pplsExpansionRight[2][igi + i] = FullText.FormatWidth;
                            plsexptype[igi + i] = interLetterExpansionType;
                        }
                    }
                }

                ich += cch;
                igi += cgi;
            }

            Invariant.Assert(igi == cgiRun);
            return LsErr.None;
        }


        //
        //  Line Services object handler callbacks
        //
        //
        internal unsafe LsErr GetObjectHandlerInfo(
            System.IntPtr   pols,               // Line Layout context
            uint            objectId,           // installed object id
            void*           objectInfo          // [out] object handler info
            )
        {
            LsErr lserr = LsErr.None;

            try
            {
                if (objectId < (uint)TextStore.ObjectId.MaxNative)
                {
                    // Send to native object handler
                    return UnsafeNativeMethods.LocbkGetObjectHandlerInfo(
                        pols,
                        objectId,
                        objectInfo
                        );
                }

                /////   Custom object handler
                //
                switch (objectId)
                {
                    case (uint)TextStore.ObjectId.InlineObject:
                        InlineInit inlineInit = new InlineInit();
                        inlineInit.pfnFormat = this.InlineFormatDelegate;
                        inlineInit.pfnDraw = this.InlineDrawDelegate;
                        Marshal.StructureToPtr(inlineInit, (System.IntPtr)objectInfo, false);
                        break;

                    default:
                        Debug.Assert(false, "Unsupported installed object!");
                        break;
                }
            }
            catch (Exception e)
            {
                SaveException(e, Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("GetObjectHandlerInfo", Plsrun.Undefined, null);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }

        internal LsErr InlineFormat(
            System.IntPtr           pols,               // Line Layout context
            Plsrun                  plsrun,             // plsrun
            int                     lscpInline,         // first cp of the run
            int                     currentPosition,    // inline's current pen location in text direction
            int                     rightMargin,        // right margin
            ref ObjDim              pobjDim,            // [out] object dimension
            out int                 fFirstRealOnLine,   // [out] is this run the first in line
            out int                 fPenPositionUsed,   // [out] is pen position used to format object
            out LsBrkCond           breakBefore,        // [out] break condition before this object
            out LsBrkCond           breakAfter          // [out] break condition after this object
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            fFirstRealOnLine = 0;
            fPenPositionUsed = 0;
            breakBefore = LsBrkCond.Please;
            breakAfter = LsBrkCond.Please;

            try
            {
                TextFormatterImp formatter = FullText.Formatter;

                TextStore store = FullText.StoreFrom(plsrun);
                lsrun = store.GetRun(plsrun);
                TextEmbeddedObject textObject = lsrun.TextRun as TextEmbeddedObject;

                Debug.Assert(textObject != null);

                int cpInline = store.GetExternalCp(lscpInline);

                fFirstRealOnLine = (cpInline == store.CpFirst) ? 1 : 0;

                TextEmbeddedObjectMetrics metrics = store.FormatTextObject(
                    textObject,
                    cpInline,
                    currentPosition,
                    rightMargin
                    );

                pobjDim = new ObjDim();
                pobjDim.dur = TextFormatterImp.RealToIdeal(metrics.Width);
                pobjDim.heightsRef.dvMultiLineHeight = TextFormatterImp.RealToIdeal(metrics.Height);
                pobjDim.heightsRef.dvAscent = TextFormatterImp.RealToIdeal(metrics.Baseline);
                pobjDim.heightsRef.dvDescent = pobjDim.heightsRef.dvMultiLineHeight - pobjDim.heightsRef.dvAscent;
                pobjDim.heightsPres = pobjDim.heightsRef;

                breakBefore = BreakConditionToLsBrkCond(textObject.BreakBefore);
                breakAfter = BreakConditionToLsBrkCond(textObject.BreakAfter);
                fPenPositionUsed = (!textObject.HasFixedSize) ? 1 : 0;

                // update lsrun metrics of text object
                lsrun.BaselineOffset = pobjDim.heightsRef.dvAscent;
                lsrun.Height = pobjDim.heightsRef.dvMultiLineHeight;
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("InlineFormat", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        private LsBrkCond BreakConditionToLsBrkCond(LineBreakCondition breakCondition)
        {
            switch (breakCondition)
            {
                case LineBreakCondition.BreakDesired:
                    return LsBrkCond.Please;

                case LineBreakCondition.BreakPossible:
                    return LsBrkCond.Can;

                case LineBreakCondition.BreakRestrained:
                    return LsBrkCond.Never;

                case LineBreakCondition.BreakAlways:
                    return LsBrkCond.Must;
            }
            Debug.Assert(false);
            return LsBrkCond.Please;
        }


        internal LsErr InlineDraw(
            System.IntPtr   pols,           // Line Layout context
            Plsrun          plsrun,         // plsrun
            ref LSPOINT     ptRun,          // [in] pen position at which to render the object
            LsTFlow         textFlow,       // text flow direction
            int             runWidth        // object width
            )
        {
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try
            {
                TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                lsrun = currentLine.GetRun(plsrun);

                LSPOINT lsrunOrigin = ptRun;

                Debug.Assert(lsrun.Type == Plsrun.InlineObject);

                int baseDirection = currentLine.RightToLeft ? 1 : 0;
                int runDirection = (int)(lsrun.BidiLevel & 1);

                if (baseDirection != 0)
                {
                    lsrunOrigin.x = -lsrunOrigin.x;
                }

                TextEmbeddedObject textObject = lsrun.TextRun as TextEmbeddedObject;

                Debug.Assert(textObject != null);
                Debug.Assert(textFlow != LsTFlow.lstflowWS || runDirection != 0);

                if ((baseDirection ^ runDirection) != 0)
                {
                    // always draw as if the object run has the same direction as the base level
                    lsrunOrigin.x -= runWidth;
                }

                // object baseline origin in UV relative to paragraph start
                Point baselineOrigin = new Point(
                    currentLine.Formatter.IdealToReal(currentLine.LSLineUToParagraphU(lsrunOrigin.x), currentLine.PixelsPerDip)+ Draw.VectorToLineOrigin.X,
                    currentLine.Formatter.IdealToReal((lsrunOrigin.y + lsrun.BaselineMoveOffset), currentLine.PixelsPerDip) + Draw.VectorToLineOrigin.Y
                    );

                // get object bounding box
                Rect objectBounds = textObject.ComputeBoundingBox(
                     baseDirection != 0, // rightToLeft
                    false  // no sideway support yet
                    );

                if (!objectBounds.IsEmpty)
                {
                    // bounding box received from text object is relative to 
                    // calculated object baseline origin
                    objectBounds.X += baselineOrigin.X;
                    objectBounds.Y += baselineOrigin.Y;
                }

                // map object bounds to XY space and integrate with the line bounding box
                _boundingBox.Union(
                    new Rect(
                    // map logical top-left location
                    LSRun.UVToXY(
                            Draw.LineOrigin,
                            new Point(),
                            objectBounds.Location.X,
                            objectBounds.Location.Y,
                            currentLine
                            ),
                    // map logical bottom-right location
                    LSRun.UVToXY(
                            Draw.LineOrigin,
                            new Point(),
                            objectBounds.Location.X + objectBounds.Size.Width,
                            objectBounds.Location.Y + objectBounds.Size.Height,
                            currentLine
                            )
                        )
                    );

                DrawingContext drawingContext = Draw.DrawingContext;                
                
                if (drawingContext != null)
                {
                    // snapping for inline object
                    Draw.SetGuidelineY(baselineOrigin.Y);

                    try 
                    {                    
                        if (Draw.AntiInversion == null)
                        {
                            // Draw at XY origin
                            textObject.Draw(
                                drawingContext,
                                LSRun.UVToXY(
                                    Draw.LineOrigin,
                                    new Point(),
                                    baselineOrigin.X,
                                    baselineOrigin.Y,
                                    currentLine
                                    ),
                                baseDirection != 0,
                                false
                                );
                        }
                        else
                        {
                            // restore the original state of the drawing surface if we've inverted it,
                            // client should be able to draw a text object on to the original surface 
                            // they intend to draw it.

                            drawingContext.PushTransform(Draw.AntiInversion);
                            try 
                            {
                                textObject.Draw(drawingContext, baselineOrigin, baseDirection != 0, false);
                            } 
                            finally
                            {
                                drawingContext.Pop();
                            }
                        }
                    }
                    finally 
                    {
                        Draw.UnsetGuidelineY();
                    }
                }
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("InlineDraw", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            return lserr;
        }


        //
        // Line enumeration methods through Line Services LsEnumLine callbacks 
        //
        // We want to map cp in backing store onto its GlyphRun. We aren't able to achieve this through 
        // LineServices drawing callbacks because they (i.e. DrawGlyphs, DrawTextRun) only give
        // characters in GlyphRun which don't always match cps in the backing store (e.g. Hyphen, Tab)
        // We can resolve the matching cp of a GlyphRun through LSRun boundaries either because a GlyphRun 
        // may span two LSRun (i.e. two LSRun are shaped together) 
        //
        // Line service enumeration API gives the actual backing store CP range as well as all the necessary info
        // to contruct the GlyphRun. 
        //        
        internal unsafe LsErr EnumText(        
            IntPtr                      pols,                           // ls context
            Plsrun                      plsrun,                         // plsrun
            int                         cpFirst,                        // first cp of the ls dnode
            int                         dcp,                            // dcp of the dnode
            char                        *pwchText,                      // characters for glyph run
            int                         cchText,                        // length of characters 
            LsTFlow                     lstFlow,                        // flow direction
            int                         fReverseOrder,                  // flag for reverse order enumeration
            int                         fGeometryProvided,              // flag for providing geometry 
            ref LSPOINT                 pptStart,                       // [in] logical start of the run
            ref LsHeights               pheights,                       // [in] height (iff geometryProvided)
            int                         dupRun,                         // width of the run
            int                         glyphBaseRun,                   // flag for glyph based run
            int                         *piCharAdvances,                // character advance widths (iff !glyphBaseRun)
            ushort                      *puClusterMap,                  // cluster map (iff glyphBaseRun)
            ushort                      *characterProperties,           // character properties (iff glyphBaseRun)
            ushort                      *puGlyphs,                      // glyph indices (iff glyphBaseRun)
            int                         *piJustifiedGlyphAdvances,      // glyph advances (iff glyphBaseRun)
            GlyphOffset                 *piiGlyphOffsets,               // glyph offsets (iff glyphBaseRun)
            uint                        *piGlyphProperties,             // glyph properties (iff glyphProperties)
            int                         glyphCount                      // glyph count
            )
        {
            Debug.Assert(fGeometryProvided == 0, "Line enumeration doesn't need geometry information");

            if (cpFirst < 0)
            {
                // Do not enumerate negative cps because they are not in the backing store.
                return LsErr.None;
            }
            
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try                 
            {   
                TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                lsrun = currentLine.GetRun(plsrun);
                GlyphRun glyphRun = null;
                if (glyphBaseRun != 0)
                {
                    // it is a glyph based run
                    if (glyphCount > 0)
                    {
                        // create shaped glyph run
                        glyphRun = ComputeShapedGlyphRun(
                            lsrun, 
                            currentLine.Formatter,
                            false,      // glyph run origin not provided
                            pptStart, 
                            cchText, 
                            pwchText, 
                            puClusterMap, 
                            glyphCount, 
                            puGlyphs,
                            piJustifiedGlyphAdvances, 
                            piiGlyphOffsets,
                            currentLine.IsJustified
                            );
                    }
                }
                else if (cchText > 0)
                {
                    // need to accumulate the width of the run
                    dupRun = 0;
                    for (int i = 0; i < cchText; i++)
                    {
                        dupRun += piCharAdvances[i];
                    }

                    
                    // it is an unshaped glyphrun
                    glyphRun = ComputeUnshapedGlyphRun(
                        lsrun, 
                        lstFlow, 
                        currentLine.Formatter,
                        false,      // glyph run origin not provided at enumeration
                        pptStart, 
                        dupRun, 
                        cchText, 
                        pwchText, 
                        piCharAdvances,
                        currentLine.IsJustified
                        );
                }
                
                if (glyphRun != null)
                {
                    // 
                    // Add this glyph run into the enumeration list
                    // Note that we are using the cpFirst/dcp pair as index.
                    // They correspond to actualy cps in backing store.
                    //  
                    IndexedGlyphRuns.Add(
                        new IndexedGlyphRun(
                           currentLine.GetExternalCp(cpFirst),
                           dcp,
                           glyphRun
                           )
                    );
                }                   
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("EnumText", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            
            return lserr;
        }

        // enumerating a tab
        internal unsafe LsErr EnumTab(
            IntPtr              pols,               // pointer to context
            Plsrun              plsrun,             // plsrun
            int                 cpFirst,            // first cp of the dnode run
            char                *pwchText,          // a single tab character
            char                tabLeader,          // a single tab leader character
            LsTFlow             lstFlow,            // flow direction
            int                 fReverseOrder,      // flag for reverse order enumeration
            int                 fGeometryProvided,  // flag for providing geometry information
            ref LSPOINT         pptStart,           // [in] logical start of the run (iff geometryProvided)
            ref LsHeights       heights,            // [in] height (iff geometryProvided)
            int                 dupRun              // width of the run
            )
        {       
            if (cpFirst < 0)
            {
                // Do not enumerate negative cps because they are not in the backing store.
                return LsErr.None;
            }
        
            LsErr lserr = LsErr.None;
            LSRun lsrun = null;

            try 
            {
                TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                lsrun = currentLine.GetRun(plsrun);
                GlyphRun glyphRun = null;                
                
                if (lsrun.Type == Plsrun.Text)
                {         
                    // Construct glyph run for the single tableader.
                    // We don't repeat the tab leader justification logic here.
                    int charWidth = 0;
                    lsrun.Shapeable.GetAdvanceWidthsUnshaped(
                        &tabLeader, 
                        1,
                        TextFormatterImp.ToIdeal, 
                        &charWidth
                        );                

                    glyphRun = ComputeUnshapedGlyphRun(
                        lsrun, 
                        lstFlow,
                        currentLine.Formatter,
                        false,      // glyph run origin not provided at enumeration time
                        pptStart, 
                        charWidth, 
                        1, 
                        &tabLeader, 
                        &charWidth,
                        currentLine.IsJustified
                        );

                }

                if (glyphRun != null)
                {                    
                    IndexedGlyphRuns.Add(
                        new IndexedGlyphRun(
                           currentLine.GetExternalCp(cpFirst),
                           1,       // dcp is 1 for a Tab character
                           glyphRun
                           )
                    );                    
                }                
            }
            catch (Exception e)
            {
                SaveException(e, plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }
            catch
            {
                SaveNonCLSException("EnumTab", plsrun, lsrun);
                lserr = LsErr.ClientAbort;
            }

            return lserr;
        }

        /// <summary>
        /// Returns whether a given character is a space character and hence can safely be expanded/compressed
        /// with little visual impact on the text.
        /// </summary>
        private bool IsSpace(char ch)
        {
            if (   ch == '\u0009' // tab
                || ch == '\u0020' // Space
                )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Scale real value to LS ideal resolution
        /// </summary>
        private static int RealToIdeal(double i)
        {
            return TextFormatterImp.RealToIdeal(i);
        }

        /// <summary>
        /// The default behavior of Math.Round() leads to undesirable behavior
        /// When used for display mode justified text, where we can find 
        /// characters belonging to the same word jumping sideways.
        /// A word can break among several GlyphRuns. So we need consistent
        /// rounding of the width of the GlyphRuns. If the width of one GlyphRun
        /// rounds up and the next GlyphRun rounds down then we see characters 
        /// overlapping and so on.
        /// It is too late to change the behavior of our rounding universally
        /// so we are making the change targeted to Display mode + Justified text
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static double RoundDipForDisplayModeJustifiedText(double value, double pixelsPerDip)
        {
            return TextFormatterImp.RoundDipForDisplayModeJustifiedText(value, pixelsPerDip);
        }

        /// <summary>
        /// Scale LS ideal resolution value to real value
        /// </summary>
        private static double IdealToRealWithNoRounding(double i)
        {
            return TextFormatterImp.IdealToRealWithNoRounding(i);
        }

        /// <summary>
        /// This method is used to make sure we do not accumulate rounding errors more than 1 pixel.
        /// It is used specifically for Display mode justifed text. The reason is that when text
        /// is justified LS will tweek the glyph advance widths to spread the text to fill the paragraph.
        /// This process will take place in the ideal metrics domain and hence we can incur large amounts of rounding
        /// errors when we convert each metric separately to the real domain.
        /// So this method checks the glyph run as a whole and distribute the rounding errors such that in the end
        /// the whole glyph run will be have a 1 pixel error at most.
        /// </summary>
        /// <param name="pwchText"></param>
        /// <param name="piGlyphAdvances"></param>
        /// <param name="glyphCount"></param>
        /// <param name="isRightToLeft"></param>
        /// <param name="baselineOrigin"></param>
        /// <param name="adjustedAdvanceWidths"></param>
        private unsafe void AdjustMetricsForDisplayModeJustifiedText(
            char              *pwchText,
            int               *piGlyphAdvances,
            int               glyphCount,
            bool              isRightToLeft,
            int               idealBaselineOriginX,
            int               idealBaselineOriginY,
            double            pixelsPerDip,
            out Point         baselineOrigin,
            out IList<double> adjustedAdvanceWidths
            )
        {
            adjustedAdvanceWidths = new double[glyphCount];

            baselineOrigin = new Point(RoundDipForDisplayModeJustifiedText(IdealToRealWithNoRounding(idealBaselineOriginX), pixelsPerDip),
                                       RoundDipForDisplayModeJustifiedText(IdealToRealWithNoRounding(idealBaselineOriginY), pixelsPerDip));

            int idealRoundedBaselineOriginX = RealToIdeal(baselineOrigin.X);
            
            // Floating point errors were causing issues since it could tip a number
            // over the 0.5 boundary and cause the number to round incorrectly.
            // By "incorrectly" we mean in a different way than the way the number was 
            // rounded by the previous GlyphRun (refer to the comment below about calculating the error)
            int idealStartingError = idealBaselineOriginX - idealRoundedBaselineOriginX;

            if (isRightToLeft)
            {
                idealStartingError *= -1; 
            }

            if (glyphCount > 0)
            {
                // We first try to compensate for the rounding errors by adding the accumulated error whenever it rounds to 1
                // to the last known space character. This is done because if we were to add the error to the character that caused
                // it to grow to be approximately 1 pixel we will end up shifting characters belonging to a word left/right by 1 pixel.
                double realAccumulatedRoundedAdvanceWidth = 0;
                double realAccumulatedAdvanceWidth        = 0;
                int    idealAccumulatedAdvanceWidth       = idealStartingError;
                
                double error                   = 0;
                double realAdvanceWidth        = 0;
                int    indexOfLastKnownSpace   = -1;
                double realRoundedAdvanceWidth = 0;

                for (int i = 0; i < glyphCount; ++i)
                {
                    if (IsSpace(pwchText[i]))
                    {
                        indexOfLastKnownSpace = i;
                    }

                    idealAccumulatedAdvanceWidth       += piGlyphAdvances[i];
                    realAccumulatedAdvanceWidth         = IdealToRealWithNoRounding(idealAccumulatedAdvanceWidth);

                    realAdvanceWidth                    = IdealToRealWithNoRounding(piGlyphAdvances[i]);
                    realRoundedAdvanceWidth             = RoundDipForDisplayModeJustifiedText(realAdvanceWidth, pixelsPerDip);
                    realAccumulatedRoundedAdvanceWidth += realRoundedAdvanceWidth;

                    // The error is calculated as the difference between where the glyph will be after rounding all the previous 
                    // advance widths and where it would have been if we round the accumulated unrounded advance widths.
                    // This is necessary to handle the case where a word spans more than one GlyphRun. 
                    // The next GlyphRun will start from the rounded value of its baselineOrigin.X.
                    // BaselineOrigin.X is equal to the sum of the previous GlyphRun's BaselineOrigin.X + The sum of the GlyphRun's
                    // AdvanceWidths.
                    // So a GlyphRun's only knowledge of the preceeding GlyphRun comes from the knowledge of the current GlyphRun's
                    // baseline origin. Hence, when rounding the current GlyphRun we must pay attention to where will
                    // the next GlyphRun start after rounding its baseline origin.
                    //
                    // Consider this example:
                    // Suppose you have 2 glyphruns and each glyphrun contains 1 glyph.  
                    // AB. 
                    // Each glyph has an advance width 1.5 pixels.
                    // If we did not Round the realAccumulatedAdvanceWidth in the error calculation below
                    // Then we will compute the error to be:
                    // error = Round(2 - 1.5)
                    //       = 1
                    // Hence the advance width for the A will be 
                    // adjustedAdvanceWidths = RoundedAw - error
                    //                       = 2 - 1
                    //                       = 1;
                    //
                    // Now consider what will happen to the run that contains the "B". It X value for the 
                    // Baseline origin will be 1.5. So after rounding it will be 2!
                    // So while A actually occupied 1 pixel B will start on the 3rd pixel and hence the space
                    // between them will be more than needed.
                    // Of course the above exmaple is very simplistic. The effect described above will introduce an extra
                    // space between characters belonging to the same word (if a GlyphRun boundary intersects with it).
                    // This effect of this will be very visible in scripts like Arabic where the characters of a word are
                    // joined.
                    // Note: We round the end result again because floating point errors in high dpi are not trivial.
                    error += RoundDipForDisplayModeJustifiedText(
                                        realAccumulatedRoundedAdvanceWidth 
                                        - RoundDipForDisplayModeJustifiedText(realAccumulatedAdvanceWidth, pixelsPerDip),
                                        pixelsPerDip
                                        );

                    adjustedAdvanceWidths[i] = realRoundedAdvanceWidth;

                    if (indexOfLastKnownSpace >= 0)
                    {
                        adjustedAdvanceWidths[indexOfLastKnownSpace] -= error;
                        realAccumulatedRoundedAdvanceWidth           -= error;
                        error = 0;
                    }
                }


                // We have a long glyphrun that has no spaces, so we are left with no other option but to add/subtract
                // 1 pixel to the characters that increased the error so as it rounds to 1 pixel. By long we mean more than 150 characters with no spaces in between.
                // this is because roundtripping the glyph metrics through LS might introduce a 1/300 error per character.
                if (indexOfLastKnownSpace < 0)
                {
                    realAccumulatedRoundedAdvanceWidth = 0;
                    realAccumulatedAdvanceWidth        = 0;
                    idealAccumulatedAdvanceWidth       = idealStartingError;
                    realAdvanceWidth                   = 0;
                    realRoundedAdvanceWidth            = 0;
                    error                              = 0;

                    for (int i = 0; i < glyphCount; ++i)
                    {
                        idealAccumulatedAdvanceWidth       += piGlyphAdvances[i];
                        realAccumulatedAdvanceWidth         = IdealToRealWithNoRounding(idealAccumulatedAdvanceWidth);

                        realAdvanceWidth                    = IdealToRealWithNoRounding(piGlyphAdvances[i]);
                        realRoundedAdvanceWidth             = RoundDipForDisplayModeJustifiedText(realAdvanceWidth, pixelsPerDip);
                        realAccumulatedRoundedAdvanceWidth += realRoundedAdvanceWidth;

                        // The error is calculated as the difference between where the glyph will be after rounding all the previous 
                        // Advance Widths and where it would have been if there were no rounding involved at all for the previous glyphs.                     
                        error = RoundDipForDisplayModeJustifiedText(
                                        realAccumulatedRoundedAdvanceWidth
                                        - RoundDipForDisplayModeJustifiedText(realAccumulatedAdvanceWidth, pixelsPerDip),
                                        pixelsPerDip
                                        );
                        adjustedAdvanceWidths[i]            = realRoundedAdvanceWidth - error;
                        realAccumulatedRoundedAdvanceWidth -= error;
                    }
                }
            }
        }

        // Compute shaped glyph run from LS data
        private unsafe GlyphRun ComputeShapedGlyphRun(
            LSRun                   lsrun,                      // ls run
            TextFormatterImp        textFormatterImp,           // The TextFormatter Implementation
            bool                    originProvided,             // flag indicate whether the origin of the run is provided                        
            LSPOINT                 lsrunOrigin,                // physical start of the run
            int                     charCount,                  // characters count
            char                    *pwchText,                  // characters for the GlyphRun
            ushort                  *puClusterMap,              // cluster map
            int                     glyphCount,                 // glyph count
            ushort                  *puGlyphs,                  // glyph indices
            int                     *piJustifiedGlyphAdvances,  // glyph advances
            GlyphOffset             *piiGlyphOffsets,           // glyph offsets
            bool                    justify
            )
        {
            TextMetrics.FullTextLine currentLine = Draw.CurrentLine;

            Point runOrigin = new Point();
            int nominalX = 0;
            int nominalY = 0;

            if (originProvided)
            {   
                if (currentLine.RightToLeft)
                {
                    // line origin is actually in XY as it is computed by LS during display.
                    // For simplicity, we always set line origin to (0,0) for LS. This means
                    // that all the run X positions in an RTL paragraph would always be 
                    // negative values. Therefore, inverting that value would result in an 
                    // ideal distance in U-axis.
                    lsrunOrigin.x = -lsrunOrigin.x;
                }

                if (textFormatterImp.TextFormattingMode == TextFormattingMode.Display && justify)
                {
                    LSRun.UVToNominalXY(
                        Draw.LineOrigin,
                        Draw.VectorToLineOrigin,
                        currentLine.LSLineUToParagraphU(lsrunOrigin.x),
                        lsrunOrigin.y + lsrun.BaselineMoveOffset,
                        currentLine,
                        out nominalX,
                        out nominalY
                        );
                }
                else
                {
                    runOrigin = LSRun.UVToXY(
                        Draw.LineOrigin,
                        Draw.VectorToLineOrigin,
                        currentLine.LSLineUToParagraphU(lsrunOrigin.x),
                        lsrunOrigin.y + lsrun.BaselineMoveOffset,
                        currentLine
                        );
                }
            }

            // We have to copy all the arrays here because glyphrun retains its own
            // copy of drawing data. It cannot hold on to pointers to LS memory since
            // those memory lifetime is bound to the lifetime of the line. But the drawing
            // data's bound to lifetime of Drawing.
            char[] charString = new char[charCount];
            ushort[] clusterMap = new ushort[charCount];

            for (int i = 0; i < charCount; i++)
            {
                charString[i] = pwchText[i];
                clusterMap[i] = puClusterMap[i];
            }

            ushort[] glyphIndices = new ushort[glyphCount];            
            IList<double> glyphAdvances;
            IList<Point> glyphOffsets;

            bool isRightToLeft = (lsrun.BidiLevel & 1) != 0;

            if (textFormatterImp.TextFormattingMode == TextFormattingMode.Ideal)
            {
                glyphAdvances = new ThousandthOfEmRealDoubles(textFormatterImp.IdealToReal(lsrun.EmSize, currentLine.PixelsPerDip), glyphCount);
                glyphOffsets = new ThousandthOfEmRealPoints(textFormatterImp.IdealToReal(lsrun.EmSize, currentLine.PixelsPerDip), glyphCount);

                for (int i = 0; i < glyphCount; i++)
                {
                    glyphIndices[i] = puGlyphs[i];
                    glyphAdvances[i] = textFormatterImp.IdealToReal(piJustifiedGlyphAdvances[i], currentLine.PixelsPerDip);
                    glyphOffsets[i] = new Point(
                        textFormatterImp.IdealToReal(piiGlyphOffsets[i].du, currentLine.PixelsPerDip),
                        textFormatterImp.IdealToReal(piiGlyphOffsets[i].dv, currentLine.PixelsPerDip)
                        );
                }
            }
            else
            {
                if (justify)
                {
                    AdjustMetricsForDisplayModeJustifiedText(
                        pwchText,
                        piJustifiedGlyphAdvances,
                        glyphCount,
                        isRightToLeft,
                        nominalX,
                        nominalY,
                        currentLine.PixelsPerDip,
                        out runOrigin,
                        out glyphAdvances
                        );
                }
                else
                {
                    glyphAdvances = new List<double>(glyphCount);
                    for (int i = 0; i < glyphCount; i++)
                    {
                        glyphAdvances.Add(textFormatterImp.IdealToReal(piJustifiedGlyphAdvances[i], currentLine.PixelsPerDip));
                    }
                }
                glyphOffsets  = new List<Point>(glyphCount);
                for (int i = 0; i < glyphCount; i++)
                {
                    glyphIndices[i] = puGlyphs[i];
                    glyphOffsets.Add(new Point(
                            textFormatterImp.IdealToReal(piiGlyphOffsets[i].du, currentLine.PixelsPerDip),
                            textFormatterImp.IdealToReal(piiGlyphOffsets[i].dv, currentLine.PixelsPerDip)
                            ));
                }
            }

#if CHECK_GLYPHS
            if (   lsrun._glyphs != null
                && glyphCount <= lsrun._glyphs.Length)
            {
                for (int i = 0; i < glyphCount; i++)
                {
                    Debug.Assert(glyphIndices[i] == lsrun._glyphs[i], "Corrupted glyphs");
                }
            }
#endif            

            GlyphRun glyphRun = lsrun.Shapeable.ComputeShapedGlyphRun(
                runOrigin,
                charString, 
                clusterMap, 
                glyphIndices, 
                glyphAdvances, 
                glyphOffsets,
                isRightToLeft, 
                false   // no sideway support yet
                );

            return glyphRun;
        }

        // Compute unshaped glyph run from LS data        
        private unsafe GlyphRun ComputeUnshapedGlyphRun(
            LSRun               lsrun,              // LSrun used to shape the GlyphRun            
            LsTFlow             textFlow,           // flow direction
            TextFormatterImp    textFormatterImp,   // The TextFormatter Implementation
            bool                originProvided,     // flag indicate whether the origin of the run is provided                        
            LSPOINT             lsrunOrigin,        // physical start of the run
            int                 dupRun,             // width of the run
            int                 cchText,            // character count
            char                *pwchText,          // characters for display 
            int                 *piCharAdvances,    // character advance widths,
            bool                justify
            )
        {
            GlyphRun glyphRun = null;
            if (lsrun.Type == Plsrun.Text)
            {
                Debug.Assert(lsrun.Shapeable != null);
                Point runOrigin    = new Point();
                int nominalX = 0;
                int nominalY = 0;

                if (originProvided)
                {                   
                    TextMetrics.FullTextLine currentLine = Draw.CurrentLine;
                    
                    if (textFlow == LsTFlow.lstflowWS)
                    {
                        lsrunOrigin.x -= dupRun;
                    }

                    if (currentLine.RightToLeft)
                    {
                        lsrunOrigin.x = -lsrunOrigin.x;
                    }

                    if (textFormatterImp.TextFormattingMode == TextFormattingMode.Display && justify)
                    {
                        LSRun.UVToNominalXY(
                            Draw.LineOrigin,
                            Draw.VectorToLineOrigin,
                            currentLine.LSLineUToParagraphU(lsrunOrigin.x),
                            lsrunOrigin.y + lsrun.BaselineMoveOffset,
                            currentLine,
                            out nominalX,
                            out nominalY
                            );
                    }
                    else
                    {
                        runOrigin = LSRun.UVToXY(
                            Draw.LineOrigin,
                            Draw.VectorToLineOrigin,
                            currentLine.LSLineUToParagraphU(lsrunOrigin.x),
                            lsrunOrigin.y + lsrun.BaselineMoveOffset,
                            currentLine
                            );
                    }
                }

                // We have to copy the character string here due to the same reason
                // we copy glyph arrays in ComputeShapedGlyphRun.
                
                char[] charString = new char[cchText];
                IList<double> charWidths;

                bool isRightToLeft = (lsrun.BidiLevel & 1) != 0;

                if (textFormatterImp.TextFormattingMode == TextFormattingMode.Ideal)
                {
                    charWidths = new ThousandthOfEmRealDoubles(textFormatterImp.IdealToReal(lsrun.EmSize, Draw.CurrentLine.PixelsPerDip), cchText);
                    for (int i = 0; i < cchText; i++)
                    {
                        charString[i] = pwchText[i];
                        charWidths[i] = textFormatterImp.IdealToReal(piCharAdvances[i], Draw.CurrentLine.PixelsPerDip);
                    }
                }
                else
                {
                    if (justify)
                    {
                        AdjustMetricsForDisplayModeJustifiedText(
                            pwchText,
                            piCharAdvances,
                            cchText,
                            isRightToLeft,
                            nominalX,
                            nominalY,
                            Draw.CurrentLine.PixelsPerDip,
                            out runOrigin,
                            out charWidths
                            );
                    }
                    else
                    {
                        charWidths = new List<double>(cchText);
                        for (int i = 0; i < cchText; i++)
                        {
                            charWidths.Add(textFormatterImp.IdealToReal(piCharAdvances[i], Draw.CurrentLine.PixelsPerDip));
                        }
                    }
                    for (int i = 0; i < cchText; i++)
                    {
                        charString[i] = pwchText[i];
                    }
                }

                

                glyphRun = lsrun.Shapeable.ComputeUnshapedGlyphRun(
                    runOrigin,
                    charString,
                    charWidths
                    );
            }            

            return glyphRun;
        } 



        /////   Delegate holder
        //
        //      It is critical to have an object holding all delegates exercised
        //      by LS within the lifetime of the context, as it guarantees none
        //      of these delegates is to be garbagged collected.
        //
        internal unsafe LineServicesCallbacks()
        {
            _pfnFetchRunRedefined                   = new FetchRunRedefined(this.FetchRunRedefined);
            _pfnFetchLineProps                      = new FetchLineProps(this.FetchLineProps);
            _pfnFetchPap                            = new FetchPap(this.FetchPap);
            _pfnGetRunTextMetrics                   = new GetRunTextMetrics(this.GetRunTextMetrics);
            _pfnGetRunCharWidths                    = new GetRunCharWidths(this.GetRunCharWidths);
            _pfnGetDurMaxExpandRagged               = new GetDurMaxExpandRagged(this.GetDurMaxExpandRagged);
            _pfnDrawTextRun                         = new DrawTextRun(this.DrawTextRun);
            _pfnGetGlyphsRedefined                  = new GetGlyphsRedefined(this.GetGlyphsRedefined);
            _pfnGetGlyphPositions                   = new GetGlyphPositions(this.GetGlyphPositions);
            _pfnGetAutoNumberInfo                   = new GetAutoNumberInfo(this.GetAutoNumberInfo);
            _pfnDrawGlyphs                          = new DrawGlyphs(this.DrawGlyphs);
            _pfnGetObjectHandlerInfo                = new GetObjectHandlerInfo(this.GetObjectHandlerInfo);
            _pfnGetRunUnderlineInfo                 = new GetRunUnderlineInfo(this.GetRunUnderlineInfo);
            _pfnGetRunStrikethroughInfo             = new GetRunStrikethroughInfo(this.GetRunStrikethroughInfo);
            _pfnHyphenate                           = new Hyphenate(this.Hyphenate);
            _pfnGetNextHyphenOpp                    = new GetNextHyphenOpp(this.GetNextHyphenOpp);
            _pfnGetPrevHyphenOpp                    = new GetPrevHyphenOpp(this.GetPrevHyphenOpp);
            _pfnDrawUnderline                       = new DrawUnderline(this.DrawUnderline);
            _pfnDrawStrikethrough                   = new DrawStrikethrough(this.DrawStrikethrough);
            _pfnFInterruptShaping                   = new FInterruptShaping(this.FInterruptShaping);
            _pfnGetCharCompressionInfoFullMixed     = new GetCharCompressionInfoFullMixed(this.GetCharCompressionInfoFullMixed);
            _pfnGetCharExpansionInfoFullMixed       = new GetCharExpansionInfoFullMixed(this.GetCharExpansionInfoFullMixed);
            _pfnGetGlyphCompressionInfoFullMixed    = new GetGlyphCompressionInfoFullMixed(this.GetGlyphCompressionInfoFullMixed);
            _pfnGetGlyphExpansionInfoFullMixed      = new GetGlyphExpansionInfoFullMixed(this.GetGlyphExpansionInfoFullMixed);
            _pfnEnumText                            = new EnumText(this.EnumText);
            _pfnEnumTab                             = new EnumTab(this.EnumTab);
        }

        internal void PopulateContextInfo(ref LsContextInfo contextInfo, ref LscbkRedefined lscbkRedef)
        {
            lscbkRedef.pfnFetchRunRedefined                 = _pfnFetchRunRedefined;
            lscbkRedef.pfnGetGlyphsRedefined                = _pfnGetGlyphsRedefined;
            lscbkRedef.pfnFetchLineProps                    = _pfnFetchLineProps;
            contextInfo.pfnFetchLineProps                   = _pfnFetchLineProps;
            contextInfo.pfnFetchPap                         = _pfnFetchPap;
            contextInfo.pfnGetRunTextMetrics                = _pfnGetRunTextMetrics;
            contextInfo.pfnGetRunCharWidths                 = _pfnGetRunCharWidths;
            contextInfo.pfnGetDurMaxExpandRagged            = _pfnGetDurMaxExpandRagged;
            contextInfo.pfnDrawTextRun                      = _pfnDrawTextRun;
            contextInfo.pfnGetGlyphPositions                = _pfnGetGlyphPositions;
            contextInfo.pfnGetAutoNumberInfo                = _pfnGetAutoNumberInfo;
            contextInfo.pfnDrawGlyphs                       = _pfnDrawGlyphs;
            contextInfo.pfnGetObjectHandlerInfo             = _pfnGetObjectHandlerInfo;
            contextInfo.pfnGetRunUnderlineInfo              = _pfnGetRunUnderlineInfo;
            contextInfo.pfnGetRunStrikethroughInfo          = _pfnGetRunStrikethroughInfo;
            contextInfo.pfnHyphenate                        = _pfnHyphenate;
            contextInfo.pfnGetNextHyphenOpp                 = _pfnGetNextHyphenOpp;
            contextInfo.pfnGetPrevHyphenOpp                 = _pfnGetPrevHyphenOpp;
            contextInfo.pfnDrawUnderline                    = _pfnDrawUnderline;
            contextInfo.pfnDrawStrikethrough                = _pfnDrawStrikethrough;
            contextInfo.pfnFInterruptShaping                = _pfnFInterruptShaping;
            contextInfo.pfnGetCharCompressionInfoFullMixed  = _pfnGetCharCompressionInfoFullMixed;
            contextInfo.pfnGetCharExpansionInfoFullMixed    = _pfnGetCharExpansionInfoFullMixed;
            contextInfo.pfnGetGlyphCompressionInfoFullMixed = _pfnGetGlyphCompressionInfoFullMixed;
            contextInfo.pfnGetGlyphExpansionInfoFullMixed   = _pfnGetGlyphExpansionInfoFullMixed;
            contextInfo.pfnEnumText                         = _pfnEnumText;
            contextInfo.pfnEnumTab                          = _pfnEnumTab;
        }

        private FetchRunRedefined                   _pfnFetchRunRedefined;
        private FetchLineProps                      _pfnFetchLineProps;
        private FetchPap                            _pfnFetchPap;
        private GetRunTextMetrics                   _pfnGetRunTextMetrics;
        private GetRunCharWidths                    _pfnGetRunCharWidths;
        private GetDurMaxExpandRagged               _pfnGetDurMaxExpandRagged;
        private GetAutoNumberInfo                   _pfnGetAutoNumberInfo;
        private DrawTextRun                         _pfnDrawTextRun;
        private GetGlyphsRedefined                  _pfnGetGlyphsRedefined;
        private GetGlyphPositions                   _pfnGetGlyphPositions;
        private DrawGlyphs                          _pfnDrawGlyphs;
        private GetObjectHandlerInfo                _pfnGetObjectHandlerInfo;
        private GetRunUnderlineInfo                 _pfnGetRunUnderlineInfo;
        private GetRunStrikethroughInfo             _pfnGetRunStrikethroughInfo;
        private Hyphenate                           _pfnHyphenate;
        private GetNextHyphenOpp                    _pfnGetNextHyphenOpp;
        private GetPrevHyphenOpp                    _pfnGetPrevHyphenOpp;
        private DrawUnderline                       _pfnDrawUnderline;
        private DrawStrikethrough                   _pfnDrawStrikethrough;
        private FInterruptShaping                   _pfnFInterruptShaping;
        private GetCharCompressionInfoFullMixed     _pfnGetCharCompressionInfoFullMixed;
        private GetCharExpansionInfoFullMixed       _pfnGetCharExpansionInfoFullMixed;
        private GetGlyphCompressionInfoFullMixed    _pfnGetGlyphCompressionInfoFullMixed;
        private GetGlyphExpansionInfoFullMixed      _pfnGetGlyphExpansionInfoFullMixed;
        private EnumText                            _pfnEnumText;
        private EnumTab                             _pfnEnumTab;


        /////   Delegates used by custom object handler
        //

        private InlineFormat _pfnInlineFormat;
        internal InlineFormat InlineFormatDelegate
        {
            get
            {
                unsafe
                {
                    if (_pfnInlineFormat == null)
                        _pfnInlineFormat = new InlineFormat(this.InlineFormat);
                    return _pfnInlineFormat;
                }
            }
        }

        private InlineDraw _pfnInlineDraw;
        internal InlineDraw InlineDrawDelegate
        {
            get
            {
                if (_pfnInlineDraw == null)
                    _pfnInlineDraw = new InlineDraw(this.InlineDraw);
                return _pfnInlineDraw;
            }
        }


        /////   Caught exception occurring inside the callback
        //

        private void SaveException(Exception e, Plsrun plsrun, LSRun lsrun)
        {
            e.Data[ExceptionContext.Key] = new ExceptionContext(e.Data[ExceptionContext.Key], e.StackTrace, plsrun, lsrun);
            _exception = e;
        }

        private void SaveNonCLSException(string methodName, Plsrun plsrun, LSRun lsrun)
        {
            Exception e = new System.Exception(SR.Get(SRID.NonCLSException));
            e.Data[ExceptionContext.Key] = new ExceptionContext(null, methodName, plsrun, lsrun);
            _exception = e;
        }

        [Serializable()]
        private class ExceptionContext
        {
            public ExceptionContext(object innerContext, string stackTraceOrMethodName, Plsrun plsrun, LSRun lsrun)
            {
                _stackTraceOrMethodName = stackTraceOrMethodName;
                _plsrun = (uint)plsrun;
                _lsrun = lsrun;
                _innerContext = innerContext;
            }

            public override string ToString()
            {
                return _stackTraceOrMethodName;
            }

            public const string Key = "ExceptionContext";

            private object _innerContext;
            private string _stackTraceOrMethodName;
            private uint _plsrun;

            [NonSerialized()]
            private LSRun _lsrun;
        }

        private Exception _exception;

        internal Exception Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }


        /// <summary>
        /// Object that owns this callback for the time being until it gets released.
        /// It could only be either a FullTextState or a DrawingState and not else as
        /// both are only LS clients.
        /// </summary>
        private object _owner;

        internal object Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        private FullTextState FullText
        {
            get { return _owner as FullTextState; }
        }

        private DrawingState Draw
        {
            get { return _owner as DrawingState; }
        }


        private Rect _boundingBox;

        /// <summary>
        /// Empty the bounding box
        /// </summary>
        internal void EmptyBoundingBox()
        {
            _boundingBox = Rect.Empty;
        }

        /// <summary>
        /// Accumulated bounding box of the current line
        /// </summary>
        internal Rect BoundingBox
        {
            get { return _boundingBox; }
        }


        // accumulate the indexed glyphruns in line enumeration
        private ICollection<IndexedGlyphRun> _indexedGlyphRuns;

        internal void ClearIndexedGlyphRuns()
        {
            // Throw aways the list. 
            _indexedGlyphRuns = null;
        }

        /// <summary>
        /// IndexedGlyphRuns of the line
        /// </summary>
        internal ICollection<IndexedGlyphRun> IndexedGlyphRuns
        {
            get 
            {
                if (_indexedGlyphRuns == null)
                {
                    _indexedGlyphRuns = new List<IndexedGlyphRun>(8);                    
                }
                
                return _indexedGlyphRuns;
            }            
        }
    }
}
