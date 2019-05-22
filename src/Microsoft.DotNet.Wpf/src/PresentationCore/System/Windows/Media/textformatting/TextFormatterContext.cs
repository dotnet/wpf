// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  Implementation of TextFormatter context
//
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Internal.TextFormatting;

using IndexedGlyphRun = System.Windows.Media.TextFormatting.IndexedGlyphRun;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// TextFormatter context. This class encapsulates the unit of reentrancy of TextFormatter.
    /// </summary>
    /// <remarks>
    /// We do not want to make this class finalizable because its lifetime already ties
    /// to TextFormatterImp which is finalizable. It is not efficient to have too many
    /// finalizable object around in the GC heap.
    /// </remarks>
#if OPTIMALBREAK_API
    public class TextFormatterContext
#else
    [FriendAccessAllowed]   // used by Framework
    internal class TextFormatterContext
#endif
    {
        private SecurityCriticalDataForSet<IntPtr>  _ploc;              // Line Services context
        private LineServicesCallbacks               _callbacks;         // object to hold all delegates for callback
        private State                               _state;             // internal state flags
        private BreakStrategies                     _breaking;          // context's breaking strategy
        private static Dictionary<char,bool>        _specialCharacters; // special characters


        /// <SecurityNote>
        /// Critical - as this calls the constructor for  SecurityCriticalDataForSet.
        /// Safe - as this just initializes it with the default value.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public TextFormatterContext()
        {
            _ploc =  new SecurityCriticalDataForSet<IntPtr>(IntPtr.Zero);
            Init();
        }


        /// <SecurityNote>
        /// Critical - as this calls Critical functions - LoCreateContext, setter for _ploc.Value,
        /// Safe - as this doesn't take any random parameters that can be passed along
        ///        and cause random memory to be written to.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void Init()
        {
            if(_ploc.Value == System.IntPtr.Zero)
            {
                // Initializing context
                LsErr lserr = LsErr.None;

                LsContextInfo contextInfo = new LsContextInfo();
                LscbkRedefined lscbkRedef = new LscbkRedefined();

                _callbacks = new LineServicesCallbacks();
                _callbacks.PopulateContextInfo(ref contextInfo, ref lscbkRedef);


                contextInfo.version = 4;            // we should set this right, they might check it in the future
                contextInfo.pols  = IntPtr.Zero;    // This will be filled in the un-managed code
                contextInfo.cEstimatedCharsPerLine  = TextStore.TypicalCharactersPerLine;
                contextInfo.fDontReleaseRuns        = 1; // dont waste time

                // There are 3 justification priorities right now with one considered good
                // and the other two provided for emergency expansion.
                // Future development to enable international justification will likely change this.
                // e.g. Kashida justification would require more than one good priorities.
                contextInfo.cJustPriorityLim        = 3;

                // Fill up text configuration
                contextInfo.wchNull                 = '\u0000';
                contextInfo.wchUndef                = '\u0001';
                contextInfo.wchTab                  = '\u0009';
                contextInfo.wchPosTab               = contextInfo.wchUndef;
                contextInfo.wchEndPara1             = TextStore.CharParaSeparator;  // Unicode para separator
                contextInfo.wchEndPara2             = contextInfo.wchUndef;
                contextInfo.wchSpace                = '\u0020';

                contextInfo.wchHyphen               = MS.Internal.Text.TextInterface.TextAnalyzer.CharHyphen; //'\x002d';
                contextInfo.wchNonReqHyphen         = '\u00AD';
                contextInfo.wchNonBreakHyphen       = '\u2011';
                contextInfo.wchEnDash               = '\u2013';
                contextInfo.wchEmDash               = '\u2014';
                contextInfo.wchEnSpace              = '\u2002';
                contextInfo.wchEmSpace              = '\u2003';
                contextInfo.wchNarrowSpace          = '\u2009';
                contextInfo.wchJoiner               = '\u200D';
                contextInfo.wchNonJoiner            = '\u200C';
                contextInfo.wchVisiNull             = '\u2050';
                contextInfo.wchVisiAltEndPara       = '\u2051';
                contextInfo.wchVisiEndLineInPara    = '\u2052';
                contextInfo.wchVisiEndPara          = '\u2053';
                contextInfo.wchVisiSpace            = '\u2054';
                contextInfo.wchVisiNonBreakSpace    = '\u2055';
                contextInfo.wchVisiNonBreakHyphen   = '\u2056';
                contextInfo.wchVisiNonReqHyphen     = '\u2057';
                contextInfo.wchVisiTab              = '\u2058';
                contextInfo.wchVisiPosTab           = contextInfo.wchUndef;
                contextInfo.wchVisiEmSpace          = '\u2059';
                contextInfo.wchVisiEnSpace          = '\u205A';
                contextInfo.wchVisiNarrowSpace      = '\u205B';
                contextInfo.wchVisiOptBreak         = '\u205C';
                contextInfo.wchVisiNoBreak          = '\u205D';
                contextInfo.wchVisiFESpace          = '\u205E';
                contextInfo.wchFESpace              = '\u3000';
                contextInfo.wchEscAnmRun            = TextStore.CharParaSeparator;
                contextInfo.wchAltEndPara           = contextInfo.wchUndef;
                contextInfo.wchEndLineInPara        = TextStore.CharLineSeparator;
                contextInfo.wchSectionBreak         = contextInfo.wchUndef;
                contextInfo.wchNonBreakSpace        = '\u00A0';
                contextInfo.wchNoBreak              = contextInfo.wchUndef;
                contextInfo.wchColumnBreak          = contextInfo.wchUndef;
                contextInfo.wchPageBreak            = contextInfo.wchUndef;
                contextInfo.wchOptBreak             = contextInfo.wchUndef;
                contextInfo.wchToReplace            = contextInfo.wchUndef;
                contextInfo.wchReplace              = contextInfo.wchUndef;

                IntPtr ploc = IntPtr.Zero;
                IntPtr ppenaltyModule = IntPtr.Zero;

                lserr = UnsafeNativeMethods.LoCreateContext(
                    ref contextInfo,
                    ref lscbkRedef,
                    out ploc
                    );

                if (lserr != LsErr.None)
                {
                    ThrowExceptionFromLsError(SR.Get(SRID.CreateContextFailure, lserr), lserr);
                }

                if (_specialCharacters == null)
                {
                    SetSpecialCharacters(ref contextInfo);
                }

                _ploc.Value = ploc;
                GC.KeepAlive(contextInfo);

                //  There is a trick here to pass in this resolution as in twips
                //  (1/1440 an inch).
                //
                //  LSCreateLine assumes the max width passed in is in twips so to
                //  allow its client to express their width in page unit. However
                //  it asks client to set up the real device resolution here so
                //  that it can internally translate the "twips" width into client
                //  actual device unit.
                //
                //  We are not device dependent anyway, so instead of following the
                //  rule which will cause us to validate the context every time. We
                //  choose to cheat LS to think that our unit is twips.
                //
                LsDevRes devRes;
                devRes.dxpInch = devRes.dxrInch = TwipsPerInch;
                devRes.dypInch = devRes.dyrInch = TwipsPerInch;

                SetDoc(
                    true,           // Yes, we will be displaying
                    true,           // Yes, reference and presentation are the same device
                    ref devRes      // Device resolutions
                    );

                SetBreaking(BreakStrategies.BreakCJK);
            }
        }


        /// <summary>
        /// Client to get the text penalty module.
        /// </summary>
        internal TextPenaltyModule GetTextPenaltyModule()
        {
            Invariant.Assert(_ploc.Value != System.IntPtr.Zero);
            return new TextPenaltyModule(_ploc);
        }


        /// <summary>
        /// Unclaim the ownership of the context, release it back to the context pool
        /// </summary>
        /// <SecurityNote>
        /// Critical - this sets exception and owner which are critical
        /// </SecurityNote>
        [SecurityCritical]
        internal void Release()
        {
            this.CallbackException = null;
            this.Owner = null;
        }


        /// <summary>
        /// context's owner
        /// </summary>
        /// <SecurityNote>
        /// Critical - Owner object is critical
        /// </SecurityNote>
        internal object Owner
        {
            [SecurityCritical]
            get { return _callbacks.Owner; }
            [SecurityCritical]
            set { _callbacks.Owner = value; }
        }


        /// <summary>
        /// Exception thrown during LS callback
        /// </summary>
        /// <SecurityNote>
        /// Critical - Exception and its message are critical
        /// </SecurityNote>
        internal Exception CallbackException
        {
            [SecurityCritical]
            get { return _callbacks.Exception; }
            [SecurityCritical]
            set { _callbacks.Exception = value; }
        }


        /// <summary>
        /// Make min/max empty
        /// </summary>
        internal void EmptyBoundingBox()
        {
            _callbacks.EmptyBoundingBox();
        }


        /// <summary>
        /// Bounding box of whole black of line
        /// </summary>
        internal Rect BoundingBox
        {
            get { return _callbacks.BoundingBox; }
        }


        /// <summary>
        /// Clear the indexed glyphruns
        /// </summary>
        internal void ClearIndexedGlyphRuns()
        {
            _callbacks.ClearIndexedGlyphRuns();
        }


        /// <summary>
        /// Indexed glyphruns of the line
        /// </summary>
        internal ICollection<IndexedGlyphRun> IndexedGlyphRuns
        {
            get { return _callbacks.IndexedGlyphRuns; }
        }


        /// <summary>
        /// Destroy LS context
        /// </summary>
        /// <SecurityNote>
        /// Critical - as this calls Critical function LoDestroyContext.
        /// Safe - as this can't be used pass in arbitrary parameters.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal void Destroy()
        {
            if(_ploc.Value != System.IntPtr.Zero)
            {
                UnsafeNativeMethods.LoDestroyContext(_ploc.Value);
                _ploc.Value = IntPtr.Zero;
            }
        }


        /// <summary>
        /// Set LS breaking strategy
        /// </summary>
        /// <SecurityNote>
        /// Critical - as this calls LoSetBreaking which is a Crtical function.
        /// Safe - as this doesn't take any parameters that are passed on without validation.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal void SetBreaking(BreakStrategies breaking)
        {
            if (_state == State.Uninitialized ||  breaking != _breaking)
            {
                Invariant.Assert(_ploc.Value != System.IntPtr.Zero);
                LsErr lserr = UnsafeNativeMethods.LoSetBreaking(_ploc.Value, (int) breaking);

                if (lserr != LsErr.None)
                {
                    ThrowExceptionFromLsError(SR.Get(SRID.SetBreakingFailure, lserr), lserr);
                }

                _breaking = breaking;
            }
            _state = State.Initialized;
        }


        //
        //  Line Services managed API
        //
        //
        /// <SecurityNote>
        /// Critical - as this calls Critical function LoCreateLine.
        /// Safe - as this can't be used to pass in arbitrary pointer parameters
        ///        that can be written to.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal LsErr CreateLine(
            int                 cpFirst,
            int                 lineLength,
            int                 maxWidth,
            LineFlags           lineFlags,
            IntPtr              previousLineBreakRecord,
            out IntPtr          ploline,
            out LsLInfo         plslineInfo,
            out int             maxDepth,
            out LsLineWidths    lineWidths
            )
        {
            Invariant.Assert(_ploc.Value != System.IntPtr.Zero);

            return UnsafeNativeMethods.LoCreateLine(
                _ploc.Value,
                cpFirst,
                lineLength,
                maxWidth,
                (uint)lineFlags,    // line flags
                previousLineBreakRecord,
                out plslineInfo,
                out ploline,
                out maxDepth,
                out lineWidths
                );
        }


        /// <SecurityNote>
        /// Critical - as it calls into unsafe function LoCreateBreaks
        /// Safe - as this can't be used to pass in arbitrary pointer parameters
        ///        that can be written to.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal LsErr CreateBreaks(
            int             cpFirst,
            IntPtr          previousLineBreakRecord,
            IntPtr          ploparabreak,
            IntPtr          ptslinevariantRestriction,
            ref LsBreaks    lsbreaks,
            out int         bestFitIndex
            )
        {
            Invariant.Assert(_ploc.Value != System.IntPtr.Zero);

            return UnsafeNativeMethods.LoCreateBreaks(
                _ploc.Value,
                cpFirst,
                previousLineBreakRecord,
                ploparabreak,
                ptslinevariantRestriction,
                ref lsbreaks,
                out bestFitIndex
                );
        }


        /// <SecurityNote>
        /// Critical - as it calls into unsafe function LoCreateParaBreakingSession
        /// Safe - as this can't be used to pass in arbitrary pointer parameters
        ///        that can be written to.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal LsErr CreateParaBreakingSession(
            int             cpFirst,
            int             maxWidth,
            IntPtr          previousLineBreakRecord,
            ref IntPtr      ploparabreak,
            ref bool        penalizedAsJustified
            )
        {
            Invariant.Assert(_ploc.Value != System.IntPtr.Zero);

            return UnsafeNativeMethods.LoCreateParaBreakingSession(
                _ploc.Value,
                cpFirst,
                maxWidth,
                previousLineBreakRecord,
                ref ploparabreak,
                ref penalizedAsJustified
                );
        }


        /// <SecurityNote>
        /// Critical - as this call LoSetDoc which is a Critical function.  It doesn't
        ///            pass any IntPtrs directly without validation.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal void SetDoc(
            bool            isDisplay,
            bool            isReferencePresentationEqual,
            ref LsDevRes    deviceInfo
            )
        {
            Invariant.Assert(_ploc.Value != System.IntPtr.Zero);
            LsErr lserr = UnsafeNativeMethods.LoSetDoc(
                _ploc.Value,
                isDisplay ? 1 : 0,
                isReferencePresentationEqual ? 1 : 0,
                ref deviceInfo
                );

            if(lserr != LsErr.None)
            {
                ThrowExceptionFromLsError(SR.Get(SRID.SetDocFailure, lserr), lserr);
            }
        }

        /// <SecurityNote>
        /// Critical - as this calls LoSetTabs which is a Critical function.  This
        ///            is not safe as this takes tabStopCount parameter, a random
        ///            value of which could cause data to written past the array pointed
        ///            to by tabStops.
        /// </SecurityNote>
        [SecurityCritical]
        internal unsafe void SetTabs(
            int         incrementalTab,
            LsTbd*      tabStops,
            int         tabStopCount
            )
        {
            Invariant.Assert(_ploc.Value != System.IntPtr.Zero);
            LsErr lserr = UnsafeNativeMethods.LoSetTabs(
                _ploc.Value,
                incrementalTab,
                tabStopCount,
                tabStops
                );

            if(lserr != LsErr.None)
            {
                ThrowExceptionFromLsError(SR.Get(SRID.SetTabsFailure, lserr), lserr);
            }
        }


        static internal void ThrowExceptionFromLsError(string message, LsErr lserr)
        {
            if (lserr == LsErr.OutOfMemory)
                throw new OutOfMemoryException (message);

            throw new Exception(message);
        }


        static internal bool IsSpecialCharacter(char c)
        {
            return _specialCharacters.ContainsKey(c);
        }

        static private void SetSpecialCharacters(ref LsContextInfo contextInfo)
        {
            Dictionary<char,bool> dict = new Dictionary<char,bool>();

            /* The first three char fields do not designate special characters
            dict[contextInfo.wchUndef] = true;
            dict[contextInfo.wchNull] = true;
            dict[contextInfo.wchSpace] = true;
            */
            dict[contextInfo.wchHyphen] = true;
            dict[contextInfo.wchTab] = true;
            dict[contextInfo.wchPosTab] = true;
            dict[contextInfo.wchEndPara1] = true;
            dict[contextInfo.wchEndPara2] = true;
            dict[contextInfo.wchAltEndPara] = true;
            dict[contextInfo.wchEndLineInPara] = true;
            dict[contextInfo.wchColumnBreak] = true;
            dict[contextInfo.wchSectionBreak] = true;
            dict[contextInfo.wchPageBreak] = true;
            dict[contextInfo.wchNonBreakSpace] = true;
            dict[contextInfo.wchNonBreakHyphen] = true;
            dict[contextInfo.wchNonReqHyphen] = true;
            dict[contextInfo.wchEmDash] = true;
            dict[contextInfo.wchEnDash] = true;
            dict[contextInfo.wchEmSpace] = true;
            dict[contextInfo.wchEnSpace] = true;
            dict[contextInfo.wchNarrowSpace] = true;
            dict[contextInfo.wchOptBreak] = true;
            dict[contextInfo.wchNoBreak] = true;
            dict[contextInfo.wchFESpace] = true;
            dict[contextInfo.wchJoiner] = true;
            dict[contextInfo.wchNonJoiner] = true;
            dict[contextInfo.wchToReplace] = true;
            dict[contextInfo.wchReplace] = true;
            dict[contextInfo.wchVisiNull] = true;
            dict[contextInfo.wchVisiAltEndPara] = true;
            dict[contextInfo.wchVisiEndLineInPara] = true;
            dict[contextInfo.wchVisiEndPara] = true;
            dict[contextInfo.wchVisiSpace] = true;
            dict[contextInfo.wchVisiNonBreakSpace] = true;
            dict[contextInfo.wchVisiNonBreakHyphen] = true;
            dict[contextInfo.wchVisiNonReqHyphen] = true;
            dict[contextInfo.wchVisiTab] = true;
            dict[contextInfo.wchVisiPosTab] = true;
            dict[contextInfo.wchVisiEmSpace] = true;
            dict[contextInfo.wchVisiEnSpace] = true;
            dict[contextInfo.wchVisiNarrowSpace] = true;
            dict[contextInfo.wchVisiOptBreak] = true;
            dict[contextInfo.wchVisiNoBreak] = true;
            dict[contextInfo.wchVisiFESpace] = true;
            dict[contextInfo.wchEscAnmRun] = true;
            dict[contextInfo.wchPad] = true;

            // Many of these fields have value 'wchUndef'.  Remove it now.
            // (This is robust, even if Init() changes the char field assignments.)
            dict.Remove(contextInfo.wchUndef);

            // Remember the result.  First thread to get here wins.
            System.Threading.Interlocked.CompareExchange<Dictionary<char,bool>>(ref _specialCharacters, dict, null);
        }


        #region Enumerations & constants
        private enum State : byte
        {
            Uninitialized = 0,
            Initialized
        }

        private const uint TwipsPerInch = 1440;


        #endregion

        #region Properties

        /// <summary>
        /// Actual LS unmanaged context
        /// </summary>
        internal SecurityCriticalDataForSet<IntPtr> Ploc
        {
            get { return _ploc; }
        }

        #endregion
    }


    internal enum BreakStrategies
    {
        BreakCJK,
        KeepCJK,
        Max
    }
}
