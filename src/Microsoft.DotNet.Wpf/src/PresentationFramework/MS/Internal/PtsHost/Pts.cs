// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* SSS_DROP_BEGIN */

/*************************************************************************
* 11/17/07 - bartde
*
* NOTICE: Code excluded from Developer Reference Sources.
*         Don't remove the SSS_DROP_BEGIN directive on top of the file.
*
* Reason for exclusion: obscure PTLS interface
*
**************************************************************************/

//
// Description: Managed PTS wrapper. 
//


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using MS.Internal.Text;
using MS.Utility;
using MS.Internal.TextFormatting;

using DllImport = MS.Internal.PresentationFramework.DllImport;

namespace MS.Internal.PtsHost.UnsafeNativeMethods
{
    // ----------------------------------------------------------------------
    // Wrapper for PTS APIs and data structures.
    // ----------------------------------------------------------------------
    static internal class PTS
    {
        // ------------------------------------------------------------------
        // PTS return result validation.
        // ------------------------------------------------------------------
        internal static void IgnoreError(int fserr)
        {
        }
        internal static void Validate(int fserr)
        {
            if (fserr != fserrNone) { Error(fserr, null); }
        }
        internal static void Validate(int fserr, PtsContext ptsContext)
        {
            if (fserr != fserrNone) { Error(fserr, ptsContext); }
        }
        private static void Error(int fserr, PtsContext ptsContext)
        {
            switch (fserr)
            {
                case fserrOutOfMemory:
                    throw new OutOfMemoryException();

                case fserrCallbackException:
                    Debug.Assert(ptsContext != null, "Null argument 'ptsContext' - required for return value validation.");
                    if (ptsContext != null)
                    {
                        SecondaryException se = new SecondaryException(ptsContext.CallbackException);
                        ptsContext.CallbackException = null;
                        throw se;
                    }
                    else
                    {
                        throw new Exception(SR.Get(SRID.PTSError, fserr));
                    }

                case tserrPageTooLong:
                case tserrSystemRestrictionsExceeded:
                    throw new PtsException(SR.Get(SRID.FormatRestrictionsExceeded, fserr));

                default:
                    throw new PtsException(SR.Get(SRID.PTSError, fserr));
            }
        }
        internal static void ValidateAndTrace(int fserr, PtsContext ptsContext)
        {
            if (fserr != fserrNone) 
            { 
                ErrorTrace(fserr, ptsContext); 
            }
        }
        private static void ErrorTrace(int fserr, PtsContext ptsContext)
        {
            switch (fserr)
            {
                case fserrOutOfMemory:
                    throw new OutOfMemoryException();

                default:
                    Debug.Assert(ptsContext != null, "Null argument 'ptsContext' - required for return value validation."); 
                    if (ptsContext != null)
                    {
                        Exception innerException = GetInnermostException(ptsContext);
                        if (innerException == null || innerException is SecondaryException || innerException is PtsException)
                        {
                            // The only exceptions thrown were our own for PTS errors. We shouldn't throw in this case but should
                            // log the error if debug tracing is enabled
                            string message = (innerException == null) ? String.Empty : innerException.Message;
                            if (TracePageFormatting.IsEnabled)
                            {
                                TracePageFormatting.Trace(
                                    TraceEventType.Start,
                                    TracePageFormatting.PageFormattingError,
                                    ptsContext,
                                    message);
                                TracePageFormatting.Trace(
                                    TraceEventType.Stop,
                                    TracePageFormatting.PageFormattingError,
                                    ptsContext,
                                    message);
                            }
                        }
                        else
                        {
                            // There's an actual third-party exception that we didn't create. Throw it.
                            SecondaryException se = new SecondaryException(innerException);
                            ptsContext.CallbackException = null;
                            throw se;
                        }
                    }
                    else
                    {
                        throw new Exception(SR.Get(SRID.PTSError, fserr));
                    }
                    break;
            }
        }
        private static Exception GetInnermostException(PtsContext ptsContext)
        {
            Invariant.Assert(ptsContext != null);
            Exception exception = ptsContext.CallbackException;
            Exception innerException = exception;
            while (innerException != null)
            {
                innerException = exception.InnerException;
                if (innerException != null)
                {
                    exception = innerException;
                }
                if (!(exception is PtsException || exception is SecondaryException))
                {
                    break;
                }
            }
            return exception;
        }

        // ------------------------------------------------------------------
        // PTS handle validation.
        // ------------------------------------------------------------------
        internal static void ValidateHandle(object handle)
        {
            if (handle == null) { InvalidHandle(); }
        }
        private static void InvalidHandle()
        {
            Debug.Assert(false);
            throw new Exception(SR.Get(SRID.PTSInvalidHandle));
        }

        // ------------------------------------------------------------------
        // Convert from CLR boolean type to PTS boolean type (int).
        // ------------------------------------------------------------------
        internal static int FromBoolean(bool condition)
        {
            return condition ? True : False;
        }

        // ------------------------------------------------------------------
        // Convert from PTS boolean type (int) to CLR boolean type.
        // ------------------------------------------------------------------
        internal static bool ToBoolean(int flag)
        {
            // NOTE: PTS can return -1 as TRUE, so the safest check is != False.
            return (flag != False);
        }

        // ------------------------------------------------------------------
        // Convert from WrapDirection to FSKWRAP.
        // ------------------------------------------------------------------
        internal static FSKWRAP WrapDirectionToFskwrap(WrapDirection wrapDirection)
        {
            return (PTS.FSKWRAP)((int)wrapDirection);
        }

        // ------------------------------------------------------------------
        // Convert from WrapDirection to FSKCLEAR.
        // ------------------------------------------------------------------
        internal static FSKCLEAR WrapDirectionToFskclear(WrapDirection wrapDirection)
        {
            FSKCLEAR fskclear;
            switch (wrapDirection)
            {
                case WrapDirection.None:
                    fskclear = FSKCLEAR.fskclearNone;
                    break;
                case WrapDirection.Left:
                    fskclear = FSKCLEAR.fskclearLeft;
                    break;
                case WrapDirection.Right:
                    fskclear = FSKCLEAR.fskclearRight;
                    break;
                case WrapDirection.Both:
                    fskclear = FSKCLEAR.fskclearBoth;
                    break;
                default:
                    Debug.Assert(false, "Unknown WrapDirection value.");
                    fskclear = FSKCLEAR.fskclearNone;
                    break;
            }
            return fskclear;
        }

        // ------------------------------------------------------------------
        // Convert from FSWDIR to FlowDirection.
        // ------------------------------------------------------------------
        internal static FlowDirection FswdirToFlowDirection(uint fswdir)
        {
            FlowDirection fd;
            switch (fswdir)
            {
                case PTS.fswdirWS:
                    fd = FlowDirection.RightToLeft;
                    break;
                default:
                    fd = FlowDirection.LeftToRight;
                    break;
            }
            return fd;
        }

        // ------------------------------------------------------------------
        // Convert from FlowDirection to FSWDIR.
        // ------------------------------------------------------------------
        internal static uint FlowDirectionToFswdir(FlowDirection fd)
        {
            uint fswdir;
            switch (fd)
            {
                case FlowDirection.RightToLeft:
                    fswdir = PTS.fswdirWS;
                    break;
                default:
                    fswdir = PTS.fswdirES;
                    break;
            }
            return fswdir;
        }

        // ------------------------------------------------------------------
        //
        // Secondary Exception
        //
        // ------------------------------------------------------------------

        #region Exceptions

        // ------------------------------------------------------------------
        // SecondaryException.
        // ------------------------------------------------------------------
        [Serializable]
        private class SecondaryException : Exception
        {
            internal SecondaryException(Exception e) : base(null, e) { }

            protected SecondaryException(
            System.Runtime.Serialization.SerializationInfo  info,
            System.Runtime.Serialization.StreamingContext   context
            ) : base(info, context){}

            public override string HelpLink
            {
                get { return InnerException.HelpLink; }
                set { InnerException.HelpLink = value; }
            }

            public override string Message
            {
                get { return InnerException.Message; }
            }

            public override string Source
            {
                get { return InnerException.Source; }
                set { InnerException.Source = value; }
            }

            public override string StackTrace
            {
                get { return InnerException.StackTrace; }
            }
        }

        // ------------------------------------------------------------------
        // PTS Exceptions with no inner exception
        // ------------------------------------------------------------------
        private class PtsException : Exception
        {
            internal PtsException(string s) : base(s) { }
        }

        #endregion Exceptions

        // ------------------------------------------------------------------
        //
        // Constants.
        //
        // ------------------------------------------------------------------

        #region Constants

        // ------------------------------------------------------------------
        // Boolean values.
        // ------------------------------------------------------------------
        internal const int True = 1;
        internal const int False = 0;

        // ------------------------------------------------------------------
        // Bottomless height limit.
        // ------------------------------------------------------------------
        internal const int dvBottomUndefined = 0x7fffffff;

        // ------------------------------------------------------------------
        // Property restrictions
        // ------------------------------------------------------------------        
        internal const int MaxFontSize = 160000;  // Font parameter restrictions is 166666, rounding down
        internal const int MaxPageSize = 3500000; // tsdv restriction is 3.579mil, rounding down

        // ------------------------------------------------------------------
        // Compatibility flags.
        // ------------------------------------------------------------------
        internal const int fsffiWordFlowTextFinite = 0x00000001;
        internal const int fsffiWordClashFootnotesWithText = 0x00000002;
        internal const int fsffiWordNewSectionAboveFootnotes = 0x00000004;
        internal const int fsffiWordStopAfterFirstCollision = 0x00000008;
        internal const int fsffiUseTextParaCache = 0x00000010;
        internal const int fsffiKeepClientLines = 0x00000020;
        internal const int fsffiUseTextQuickLoop = 0x00000040;
        internal const int fsffiAvalonDisableOptimalInChains = 0x00000100;
        internal const int fsffiWordAdjustTrackWidthsForFigureInWebView = 0x00000200;

        // ------------------------------------------------------------------
        // Built in PTS object's identifiers.
        // ------------------------------------------------------------------
        internal const int fsidobjText = -1;
        internal const int fsidobjFigure = -2;

        // ------------------------------------------------------------------
        // The eight possible writing directions are listed clockwise starting 
        // with default (Latin) one.
        //
        // fswdirES is the coordinate system used when line grows to East 
        // and text grows to South. (Next letter is to the right (east) 
        // of previous, next line is created below (south) the previous.) 
        //
        // For coordinate system corresponding to fswdirES positive u moves to 
        // the right, positive v moves down, and this coordinate system originates 
        // in the left upper corner of the page (each coordinate system originates 
        // in one of four corners of the page so that page contents are 
        // in positive area)
        // ------------------------------------------------------------------
        internal const int fswdirDefault = 0;
        internal const int fswdirES = 0;
        internal const int fswdirEN = 1;
        internal const int fswdirSE = 2;
        internal const int fswdirSW = 3;
        internal const int fswdirWS = 4;
        internal const int fswdirWN = 5;
        internal const int fswdirNE = 6;
        internal const int fswdirNW = 7;

        // ------------------------------------------------------------------
        // The three bits that constitute fswdir happens to have well defined 
        // meanings.
        //
        // Middle bit: on for vertical writing, off for horizontal.
        // First (low value) bit: "off" means v-axis points right or down (positive).
        // Third bit: "off" means u-axis points right or down (positive).
        // ------------------------------------------------------------------
        internal const int fUDirection = 4;
        internal const int fVDirection = 1;
        internal const int fUVertical = 2;

        // ------------------------------------------------------------------
        // PTS restrictions.
        // ------------------------------------------------------------------
        internal struct Restrictions
        {
            internal const int tsduRestriction	                = 0x3FFFFFFF;
            internal const int tsdvRestriction	                = 0x3FFFFFFF;
            internal const int tscColumnRestriction             = 1000;
            internal const int tscSegmentAreaRestriction        = 1000;
            internal const int tscHeightAreaRestriction         = 1000;
            internal const int tscTableColumnsRestriction       = 1000;
            internal const int tscFootnotesRestriction          = 1000;
            internal const int tscAttachedObjectsRestriction    = 100000;
            internal const int tscLineInParaRestriction         = 1000000;
            internal const int tscVerticesRestriction           = 10000;
            internal const int tscPolygonsRestriction           = 200;
            internal const int tscSeparatorsRestriction         = 1000;
            internal const int tscMatrixColumnsRestriction      = 1000;
            internal const int tscMatrixRowsRestriction         = 10000;
            internal const int tscEquationsRestriction          = 10000;
            internal const int tsduFontParameterRestriction     = 50000000;
            internal const int tsdvFontParameterRestriction     = 50000000;
            internal const int tscBreakingClassesRestriction    = 200;
            internal const int tscBreakingUnitsRestriction      = 200;
            internal const int tscModWidthClassesRestriction    = 200;
            internal const int tscPairActionsRestriction        = 200;
            internal const int tscPriorityActionsRestriction    = 200;
            internal const int tscExpansionUnitsRestriction     = 200;
            internal const int tscCharacterRestriction          = 0x0FFFFFFF;
            internal const int tscInstalledHandlersRestriction  = 200;
            internal const int tscJustPriorityLimRestriction    = 20;
        }

        // ------------------------------------------------------------------
        // Error codes.
        // ------------------------------------------------------------------
        internal const int fserrNone = tserrNone;
        internal const int fserrOutOfMemory = tserrOutOfMemory;
        internal const int fserrNotImplemented = tserrNotImplemented;
        internal const int fserrCallbackException = tserrCallbackException;


        internal const int tserrNone =                              0;
        internal const int tserrInvalidParameter =                 -1;
        internal const int tserrOutOfMemory =                      -2;
        internal const int tserrNullOutputParameter =              -3;
        internal const int tserrInvalidLsContext =                 -4;
        internal const int tserrInvalidLine =                      -5;
        internal const int tserrInvalidDnode =                     -6;
        internal const int tserrInvalidDeviceResolution =          -7;
        internal const int tserrInvalidRun =                       -8;
        internal const int tserrMismatchLineContext =              -9;
        internal const int tserrContextInUse =                     -10;
        internal const int tserrDuplicateSpecialCharacter =        -11;
        internal const int tserrInvalidAutonumRun =                -12;
        internal const int tserrFormattingFunctionDisabled =       -13;
        internal const int tserrUnfinishedDnode =                  -14;
        internal const int tserrInvalidDnodeType =                 -15;
        internal const int tserrInvalidPenDnode =                  -16;
        internal const int tserrInvalidNonPenDnode =               -17;
        internal const int tserrInvalidBaselinePenDnode =          -18;
        internal const int tserrInvalidFormatterResult =           -19;
        internal const int tserrInvalidObjectIdFetched =           -20;
        internal const int tserrInvalidDcpFetched =                -21;
        internal const int tserrInvalidCpContentFetched =          -22;
        internal const int tserrInvalidBookmarkType =              -23;
        internal const int tserrSetDocDisabled =                   -24;
        internal const int tserrFiniFunctionDisabled =             -25;
        internal const int tserrCurrentDnodeIsNotTab =             -26;
        internal const int tserrPendingTabIsNotResolved =          -27;
        internal const int tserrWrongFiniFunction =                -28;
        internal const int tserrInvalidBreakingClass =             -29;
        internal const int tserrBreakingTableNotSet =              -30;
        internal const int tserrInvalidModWidthClass =             -31;
        internal const int tserrModWidthPairsNotSet =              -32;
        internal const int tserrWrongTruncationPoint =             -33;
        internal const int tserrWrongBreak =                       -34;
        internal const int tserrDupInvalid =                       -35;
        internal const int tserrRubyInvalidVersion =               -36;
        internal const int tserrTatenakayokoInvalidVersion =       -37;
        internal const int tserrWarichuInvalidVersion =            -38;
        internal const int tserrWarichuInvalidData =               -39;
        internal const int tserrCreateSublineDisabled =            -40;
        internal const int tserrCurrentSublineDoesNotExist=        -41;
        internal const int tserrCpOutsideSubline =                 -42;
        internal const int tserrHihInvalidVersion =                -43;
        internal const int tserrInsufficientQueryDepth =           -44;
        internal const int tserrInvalidBreakRecord =               -45;
        internal const int tserrInvalidPap =                       -46;
        internal const int tserrContradictoryQueryInput =          -47;
        internal const int tserrLineIsNotActive =                  -48;
        internal const int tserrTooLongParagraph =                 -49;
        internal const int tserrTooManyCharsToGlyph =              -50;
        internal const int tserrWrongHyphenationPosition =         -51;
        internal const int tserrTooManyPriorities =                -52;
        internal const int tserrWrongGivenCp =                     -53;
        internal const int tserrWrongCpFirstForGetBreaks =         -54;
        internal const int tserrWrongJustTypeForGetBreaks =        -55;
        internal const int tserrWrongJustTypeForCreateLineGivenCp =-56;
        internal const int tserrTooLongGlyphContext =              -57;
        internal const int tserrInvalidCharToGlyphMapping =        -58;
        internal const int tserrInvalidMathUsage =                 -59;
        internal const int tserrInconsistentChp =                  -60;
        internal const int tserrStoppedInSubline =                 -61;
        internal const int tserrPenPositionCouldNotBeUsed =        -62;
        internal const int tserrDebugFlagsInShip =                 -63;
        internal const int tserrInvalidOrderTabs =                 -64;
        internal const int tserrSystemRestrictionsExceeded =       -100;
        internal const int tserrInvalidPtsContext =                -103;
        internal const int tserrInvalidClientOutput =              -104;
        internal const int tserrInvalidObjectOutput =              -105;
        internal const int tserrInvalidGeometry =                  -106;
        internal const int tserrInvalidFootnoteRejector =          -107;
        internal const int tserrInvalidFootnoteInfo =              -108;
        internal const int tserrOutputArrayTooSmall =              -110;
        internal const int tserrWordNotSupportedInBottomless =     -111;
        internal const int tserrPageTooLong =                      -112;
        internal const int tserrInvalidQuery =                     -113;
        internal const int tserrWrongWritingDirection =            -114;
        internal const int tserrPageNotClearedForUpdate =          -115;
        internal const int tserrInternalError =                    -1000;
        internal const int tserrNotImplemented =                   -10000;
        internal const int tserrClientAbort =                      -100000;

        internal const int tserrPageSizeMismatch =                 -100001;
        internal const int tserrCallbackException =                -100002;

        // ------------------------------------------------------------------
        // Debug flags.
        // ------------------------------------------------------------------
        internal const int fsfdbgCheckVariantsConsistency = 0x00000001;

        #endregion Constants

        // ------------------------------------------------------------------
        //
        // Data structures.
        //
        // ------------------------------------------------------------------

        #region Data structures

        // ------------------------------------------------------------------
        // fsbreakrec.h
        // ------------------------------------------------------------------
        // internal struct FSBREAKRECTEXTELEMENT
        // internal struct FSBREAKRECTEXT
        // internal struct FSBREAKRECTRACKELEMENT
        // internal struct FSBREAKRECTRACK
        // internal struct FSBREAKRECCOMPOSITECOLUMN
        // internal struct FSBREAKRECSUBTRACK
        // internal struct FSBREAKRECSUBPAGE
        // internal struct FSBREAKRECSECTWITHCOLNOTES
        // internal struct FSBREAKRECSECTWITHPAGENOTES
        // internal struct FSBREAKRECSECTION
        // internal struct FSBREAKRECPAGEBODY
        // internal struct FSBREAKRECPAGEPROPER
        // internal struct FSBREAKRECPAGE
        // ------------------------------------------------------------------
        // fscbk.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCBK
        {
            internal FSCBKGEN cbkgen;
            internal FSCBKTXT cbktxt;
            internal FSCBKOBJ cbkobj;
            internal FSCBKFIG cbkfig;
            internal FSCBKWRD cbkwrd;
        }
        // ------------------------------------------------------------------
        // fscbkfig.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCBKFIG
        {
             internal GetFigureProperties pfnGetFigureProperties;
             internal GetFigurePolygons pfnGetFigurePolygons;
             internal CalcFigurePosition pfnCalcFigurePosition;
        }
        // ------------------------------------------------------------------
        // fscbkfigds.h
        // ------------------------------------------------------------------
        internal enum FSKREF : int            // point of reference
        {
            fskrefPage = 0,
            fskrefMargin = 1,
            fskrefParagraph = 2,
            fskrefChar = 3,
            fskrefOutOfMinMargin = 4,
            fskrefOutOfMaxMargin = 5
        }
        internal enum FSKALIGNFIG : int       // kind of alignment
        {
            fskalfMin = 0,
            fskalfCenter = 1,
            fskalfMax = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFIGUREPROPS
        {
            internal FSKREF fskrefU;
            internal FSKREF fskrefV;
            internal FSKALIGNFIG fskalfU;
            internal FSKALIGNFIG fskalfV;
            internal FSKWRAP fskwrap;
            internal int fNonTextPlane;
            internal int fAllowOverlap;
            internal int fDelayable;
        }
        // ------------------------------------------------------------------
        // fscbkgen.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCBKGEN
        {
             internal FSkipPage pfnFSkipPage;
             internal GetPageDimensions pfnGetPageDimensions;
             internal GetNextSection pfnGetNextSection;
             internal GetSectionProperties pfnGetSectionProperties;
             internal GetJustificationProperties pfnGetJustificationProperties;
             internal GetMainTextSegment pfnGetMainTextSegment;
             internal GetHeaderSegment pfnGetHeaderSegment;
             internal GetFooterSegment pfnGetFooterSegment;
             internal UpdGetSegmentChange pfnUpdGetSegmentChange;
             internal GetSectionColumnInfo pfnGetSectionColumnInfo;
             internal GetSegmentDefinedColumnSpanAreaInfo pfnGetSegmentDefinedColumnSpanAreaInfo;
             internal GetHeightDefinedColumnSpanAreaInfo pfnGetHeightDefinedColumnSpanAreaInfo;
             internal GetFirstPara pfnGetFirstPara;
             internal GetNextPara pfnGetNextPara;
             internal UpdGetFirstChangeInSegment pfnUpdGetFirstChangeInSegment;
             internal UpdGetParaChange pfnUpdGetParaChange;
             internal GetParaProperties pfnGetParaProperties;
             internal CreateParaclient pfnCreateParaclient;
             internal TransferDisplayInfo pfnTransferDisplayInfo;
             internal DestroyParaclient pfnDestroyParaclient;
             internal FInterruptFormattingAfterPara pfnFInterruptFormattingAfterPara;
             internal GetEndnoteSeparators pfnGetEndnoteSeparators;
             internal GetEndnoteSegment pfnGetEndnoteSegment;
             internal GetNumberEndnoteColumns pfnGetNumberEndnoteColumns;
             internal GetEndnoteColumnInfo pfnGetEndnoteColumnInfo;
             internal GetFootnoteSeparators pfnGetFootnoteSeparators;
             internal FFootnoteBeneathText pfnFFootnoteBeneathText;
             internal GetNumberFootnoteColumns pfnGetNumberFootnoteColumns;
             internal GetFootnoteColumnInfo pfnGetFootnoteColumnInfo;
             internal GetFootnoteSegment pfnGetFootnoteSegment;
             internal GetFootnotePresentationAndRejectionOrder pfnGetFootnotePresentationAndRejectionOrder;
             internal FAllowFootnoteSeparation pfnFAllowFootnoteSeparation;
        }
        // ------------------------------------------------------------------
        // fscbkgends.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPAP
        {
            internal int idobj;
            internal int fKeepWithNext;
            internal int fBreakPageBefore;
            internal int fBreakColumnBefore;
        }
        // ------------------------------------------------------------------
        // fscbkobj.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCBKOBJ
        {
             internal IntPtr pfnNewPtr;
             internal IntPtr pfnDisposePtr;
             internal IntPtr pfnReallocPtr;
             internal DuplicateMcsclient pfnDuplicateMcsclient;
             internal DestroyMcsclient pfnDestroyMcsclient;
             internal FEqualMcsclient pfnFEqualMcsclient;
             internal ConvertMcsclient pfnConvertMcsclient;
             internal GetObjectHandlerInfo pfnGetObjectHandlerInfo;
        }
        // ------------------------------------------------------------------
        // fscbktxt.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCBKTXT
        {
             internal CreateParaBreakingSession pfnCreateParaBreakingSession;
             internal DestroyParaBreakingSession pfnDestroyParaBreakingSession;
             internal GetTextProperties pfnGetTextProperties;
             internal GetNumberFootnotes pfnGetNumberFootnotes;
             internal GetFootnotes pfnGetFootnotes;
             internal FormatDropCap pfnFormatDropCap;
             internal GetDropCapPolygons pfnGetDropCapPolygons;
             internal DestroyDropCap pfnDestroyDropCap;
             internal FormatBottomText pfnFormatBottomText;
             internal FormatLine pfnFormatLine;
             internal FormatLineForced pfnFormatLineForced;
             internal FormatLineVariants pfnFormatLineVariants;
             internal ReconstructLineVariant pfnReconstructLineVariant;
             internal DestroyLine pfnDestroyLine;
             internal DuplicateLineBreakRecord pfnDuplicateLineBreakRecord;
             internal DestroyLineBreakRecord pfnDestroyLineBreakRecord;
             internal SnapGridVertical pfnSnapGridVertical;
             internal GetDvrSuppressibleBottomSpace pfnGetDvrSuppressibleBottomSpace;
             internal GetDvrAdvance pfnGetDvrAdvance;
             internal UpdGetChangeInText pfnUpdGetChangeInText;
             internal UpdGetDropCapChange pfnUpdGetDropCapChange;
             internal FInterruptFormattingText pfnFInterruptFormattingText;
             internal GetTextParaCache pfnGetTextParaCache;
             internal SetTextParaCache pfnSetTextParaCache;
             internal GetOptimalLineDcpCache pfnGetOptimalLineDcpCache;
             internal GetNumberAttachedObjectsBeforeTextLine pfnGetNumberAttachedObjectsBeforeTextLine;
             internal GetAttachedObjectsBeforeTextLine pfnGetAttachedObjectsBeforeTextLine;
             internal GetNumberAttachedObjectsInTextLine pfnGetNumberAttachedObjectsInTextLine;
             internal GetAttachedObjectsInTextLine pfnGetAttachedObjectsInTextLine;
             internal UpdGetAttachedObjectChange pfnUpdGetAttachedObjectChange;
             internal GetDurFigureAnchor pfnGetDurFigureAnchor;
        }
        // ------------------------------------------------------------------
        // fscbktxtds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSLINEVARIANT
        {
            internal IntPtr pfsbreakreclineclient;
            internal IntPtr pfslineclient;
            internal int dcpLine;
            internal int fForceBroken;
            internal FSFLRES fslres;
            internal int dvrAscent;
            internal int dvrDescent;
            internal int fReformatNeighborsAsLastLine;
            internal IntPtr ptsLinePenaltyInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTXTPROPS
        {
            internal uint fswdir;
            internal int dcpStartContent;
            internal int fKeepTogether;
            internal int fDropCap;
            internal int cMinLinesAfterBreak;
            internal int cMinLinesBeforeBreak;
            internal int fVerticalGrid;
            internal int fOptimizeParagraph;
            internal int fAvoidHyphenationAtTrackBottom;
            internal int fAvoidHyphenationOnLastChainElement;
            internal int cMaxConsecutiveHyphens;
        }

        internal enum FSKFMTLINE : int    // result of comparison method/API
        {
            fskfmtlineNormal  = 0,
            fskfmtlineOptimal = 1,
            fskfmtlineForced  = 2,
            fskfmtlineWord    = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFMTLINEIN
        {
            internal FSKFMTLINE fskfmtline;
            internal IntPtr nmp;
            int iArea;
            int dcpStartLine;
            IntPtr pbrLineIn;
            int urStartLine;
            int durLine;
            int urStartTrack;
            int durTrack;
            int urPageLeftMargin;
            int fAllowHyphenation;
            int fClearOnleft;
            int fClearOnRight;
            int fTreatAsFirstInPara;
            int fTreatAsLastInPara;
            int fSuppressTopSpace;
            int dcpLine; // Only valid if fsklineOptimal
            int dvrAvailable; // Only valid if fsklineForced
            int fChain; // Only valid if fsklineWord
            int vrStartLine; // Only valid if fsklineWord
            int urStartLr; // Only valid if fsklineWord
            int durLr; // Only valid if fsklineWord
            int fHitByPolygon; // Only valid if fsklineWord
            int fClearLeftLrWord; // Only valid if fsklineWord
            int fClearRightLrWord; // Only valid if fsklineWord
        }

        // ------------------------------------------------------------------
        // fscbkwrd.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCBKWRD
        {
             internal IntPtr pfnGetSectionHorizMargins;
             internal IntPtr pfnFPerformColumnBalancing;
             internal IntPtr pfnCalculateColumnBalancingApproximateHeight;
             internal IntPtr pfnCalculateColumnBalancingStep;
             internal IntPtr pfnGetColumnSectionBreak;
             internal IntPtr pfnFSuppressKeepWithNextAtTopOfPage;
             internal IntPtr pfnFSuppressKeepTogetherAtTopOfPage;
             internal IntPtr pfnFAllowSpaceAfterOverhang;
             internal IntPtr pfnFormatLineWord;
             internal IntPtr pfnGetSuppressedTopSpace;
             internal IntPtr pfnChangeSplatLineHeight;
             internal IntPtr pfnGetDvrAdvanceWord;
             internal IntPtr pfnGetMinDvrAdvance;
             internal IntPtr pfnGetDurTooNarrowForFigure;
             internal IntPtr pfnResolveOverlap;
             internal IntPtr pfnGetOffsetForFlowAroundAndBBox;
             internal IntPtr pfnGetClientGeometryHandle;
             internal IntPtr pfnDuplicateClientGeometryHandle;
             internal IntPtr pfnDestroyClientGeometryHandle;
             internal IntPtr pfnObstacleAddNotification;
             internal IntPtr pfnGetFigureObstaclesForRestart;
             internal IntPtr pfnRepositionFigure;
             internal IntPtr pfnFStopBeforeLr;
             internal IntPtr pfnFStopBeforeLine;
             internal IntPtr pfnFIgnoreCollision;
             internal IntPtr pfnGetNumberOfLinesForColumnBalancing;
             internal IntPtr pfnFCancelPageBreakBefore;
             internal IntPtr pfnChangeVrTopLineForFigure;
             internal IntPtr pfnFApplyWidowOrphanControlInFootnoteResolution;
        }
        // ------------------------------------------------------------------
        // fscbkwrdds.h
        // ------------------------------------------------------------------
        // internal enum FSKRESTART
        // internal enum FSKCACHE
        // internal struct FSCACHEINFO
        // ------------------------------------------------------------------
        // fscolumninfo.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCOLUMNINFO
        {
            internal int durBefore;                 // space before column
            internal int durWidth;                  // width of the column
        }
        // ------------------------------------------------------------------
        // fscompresult.h
        // ------------------------------------------------------------------
        internal enum FSCOMPRESULT : int    // result of comparison method/API
        {
            fscmprNoChange = 0,
            fscmprChangeInside = 1,
            fscmprShifted = 2
        }
        // ------------------------------------------------------------------
        // fscrcontxtds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCONTEXTINFO
        {
             internal uint version;                  // version number
             internal uint fsffi;                    // compatibility flags
             internal int drMinColumnBalancingStep;  // min step for col balancing algorithm
             internal int cInstalledObjects;         // number of installed objects
             internal IntPtr pInstalledObjects;      // array of installed objects
             internal IntPtr pfsclient;              // client data for this context
             internal IntPtr ptsPenaltyModule;       // Penalty module
             internal FSCBK fscbk;                   // FS client callbacks
             internal AssertFailed pfnAssertFailed;  // debugging callback
        }
        // ------------------------------------------------------------------
        // fsdefs.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSRECT
        {
            internal int u;
            internal int v;
            internal int du;
            internal int dv;

            // ------------------------------------------------------------------
            // Helper constructors for FSRECT
            // ------------------------------------------------------------------

            internal FSRECT(int inU, int inV, int inDU, int inDV) {  u = inU; v=inV; du = inDU; dv = inDV; }
            internal FSRECT(FSRECT rect) {  u = rect.u; v=rect.v; du = rect.du; dv = rect.dv; }
            internal FSRECT(Rect rect) 
            { 
                if(!rect.IsEmpty)
                {
                    u = TextDpi.ToTextDpi(rect.Left); 
                    v = TextDpi.ToTextDpi(rect.Top);
                    du = TextDpi.ToTextDpi(rect.Width);
                    dv = TextDpi.ToTextDpi(rect.Height);
                }
                else
                {
                    u = v = du = dv = 0;
                }
            }

            // ------------------------------------------------------------------
            // Equality operators for FSRECT
            // ------------------------------------------------------------------
            public static bool operator == (FSRECT rect1, FSRECT rect2)
            {
                return rect1.u == rect2.u && rect1.v == rect2.v && rect1.du == rect2.du && rect1.dv == rect2.dv;
            }
            public static bool operator != (FSRECT rect1, FSRECT rect2) { return !(rect1 == rect2); }
            public override bool Equals(object o)
            { 
                if(o is FSRECT) 
                { 
                    return (FSRECT)o == this; 
                }
                return false;
            }

            // ------------------------------------------------------------------
            // Hash code
            // ------------------------------------------------------------------
            public override int GetHashCode()
            {
                return u.GetHashCode() ^ v.GetHashCode() ^ du.GetHashCode() ^ dv.GetHashCode();
            }

            // ------------------------------------------------------------------
            // Converts from textdpi rect to real rect
            // ------------------------------------------------------------------
            internal Rect FromTextDpi()
            {
                return new Rect(TextDpi.FromTextDpi(u), TextDpi.FromTextDpi(v), TextDpi.FromTextDpi(du), TextDpi.FromTextDpi(dv));
            }

            // ------------------------------------------------------------------
            // True if point is contained in rect
            // ------------------------------------------------------------------
            internal bool Contains(FSPOINT point)
            {
                return (point.u >= u && point.u <= u + du && point.v >= v && point.v <= v + dv);
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPOINT
        {
            internal int u;
            internal int v;

            // ------------------------------------------------------------------
            // Helper constructors for FSPOINT
            // ------------------------------------------------------------------
            internal FSPOINT(int inU, int inV) {  u = inU; v = inV; }

            // ------------------------------------------------------------------
            // Equality operators for FSPOINT
            // ------------------------------------------------------------------
            public static bool operator == (FSPOINT point1, FSPOINT point2) { return point1.u == point2.u && point1.v == point2.v; }
            public static bool operator != (FSPOINT point1, FSPOINT point2) { return !(point1 == point2); }

            public override bool Equals(object o)
            { 
                if(o is FSPOINT) 
                { 
                    return (FSPOINT)o == this; 
                }
                return false;
            }

            public override int GetHashCode()
            {
                return u.GetHashCode() ^ v.GetHashCode();
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSVECTOR
        {
            internal int du;
            internal int dv;

            // ------------------------------------------------------------------
            // Helper constructors for FSVECTOR
            // ------------------------------------------------------------------
            internal FSVECTOR(int inDU, int inDV) { du = inDU; dv = inDV; }

            // ------------------------------------------------------------------
            // Equality operators for FSVECTOR
            // ------------------------------------------------------------------
            public static bool operator == (FSVECTOR vector1, FSVECTOR vector2) { return vector1.du == vector2.du && vector1.dv == vector2.dv; }
            public static bool operator != (FSVECTOR vector1, FSVECTOR vector2) { return !(vector1 == vector2); }

            public override bool Equals(object o)
            { 
                if(o is FSVECTOR) 
                { 
                    return (FSVECTOR)o == this; 
                }
                return false;
            }

            public override int GetHashCode()
            {
                return du.GetHashCode() ^ dv.GetHashCode();
            }

            // ------------------------------------------------------------------
            // Converts from textdpi vector to real vector
            // ------------------------------------------------------------------
            internal Vector FromTextDpi()
            {
                return new Vector(TextDpi.FromTextDpi(du), TextDpi.FromTextDpi(dv));
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSBBOX
        {
            internal int fDefined;
            internal FSRECT fsrc;
        }
        // ------------------------------------------------------------------
        // fsfigobstinfo.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFIGOBSTINFO
        {
            internal IntPtr nmpFigure;
            internal FSFLOWAROUND flow;
            internal FSPOLYGONINFO polyginfo;
            internal FSOVERLAP overlap;
            internal FSBBOX fsbbox;
            internal FSPOINT fsptPosPreliminary;
            internal int fNonTextPlane;
            internal int fUTextRelative;
            internal int fVTextRelative;
        }
        // ------------------------------------------------------------------
        // fsfigobstrs.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFIGOBSTRESTARTINFO
        {
            internal IntPtr nmpFigure;
            internal int fReached;
            internal int fNonTextPlane;
        }
        // ------------------------------------------------------------------
        // fsfloaterbreakrec.h
        // ------------------------------------------------------------------
        // internal struct FSBREAKRECFLOATER
        // ------------------------------------------------------------------
        // fsfloatercbk.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFLOATERCBK
        {
             internal GetFloaterProperties pfnGetFloaterProperties;
             internal FormatFloaterContentFinite pfnFormatFloaterContentFinite;
             internal FormatFloaterContentBottomless pfnFormatFloaterContentBottomless;
             internal UpdateBottomlessFloaterContent pfnUpdateBottomlessFloaterContent;
             internal GetFloaterPolygons pfnGetFloaterPolygons;
             internal ClearUpdateInfoInFloaterContent pfnClearUpdateInfoInFloaterContent;
             internal CompareFloaterContents pfnCompareFloaterContents;
             internal DestroyFloaterContent pfnDestroyFloaterContent;
             internal DuplicateFloaterContentBreakRecord pfnDuplicateFloaterContentBreakRecord;
             internal DestroyFloaterContentBreakRecord pfnDestroyFloaterContentBreakRecord;
             internal GetFloaterContentColumnBalancingInfo pfnGetFloaterContentColumnBalancingInfo;
             internal GetFloaterContentNumberFootnotes pfnGetFloaterContentNumberFootnotes;
             internal GetFloaterContentFootnoteInfo pfnGetFloaterContentFootnoteInfo;
             internal TransferDisplayInfoInFloaterContent pfnTransferDisplayInfoInFloaterContent;
             internal GetMCSClientAfterFloater pfnGetMCSClientAfterFloater;
             internal GetDvrUsedForFloater pfnGetDvrUsedForFloater;
        }
        // ------------------------------------------------------------------
        // fsfloatercbkds.h
        // ------------------------------------------------------------------
        internal enum FSKFLOATALIGNMENT : int
        {
            fskfloatalignMin = 0,
            fskfloatalignCenter = 1,
            fskfloatalignMax = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFLOATERPROPS
        {
            internal FSKCLEAR fskclear;
            internal FSKFLOATALIGNMENT fskfloatalignment;
            internal int fFloat;
            internal FSKWRAP fskwr;
            internal int fDelayNoProgress;

        	internal int durDistTextLeft;   // distance to text from MinU side	
        	internal int durDistTextRight;  // distance to text from MaxU side	
        	internal int dvrDistTextTop;    // distance to text from MinV side	
        	internal int dvrDistTextBottom; // distance to text from MaxV side
}
        // ------------------------------------------------------------------
        // fsfloaterhandlerds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFLOATERINIT
        {
            internal FSFLOATERCBK fsfloatercbk;
        }
        // ------------------------------------------------------------------
        // fsfloaterqueryds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFLOATERDETAILS
        {
            internal FSKUPDATE fskupdContent;
            internal IntPtr fsnmFloater;
            internal FSRECT fsrcFloater;
            internal IntPtr pfsFloaterContent;
        }
        // ------------------------------------------------------------------
        // fsflres.h
        // ------------------------------------------------------------------
        internal enum FSFLRES : int           // line formatting result
        {
            fsflrOutOfSpace = 0,
            fsflrOutOfSpaceHyphenated = 1,
            fsflrEndOfParagraph = 2,
            fsflrEndOfParagraphClearLeft = 3,
            fsflrEndOfParagraphClearRight = 4,
            fsflrEndOfParagraphClearBoth = 5,
            fsflrPageBreak = 6,
            fsflrColumnBreak = 7,
            fsflrSoftBreak = 8,
            fsflrSoftBreakClearLeft = 9,
            fsflrSoftBreakClearRight = 10,
            fsflrSoftBreakClearBoth = 11,
            fsflrNoProgressClear = 12
        }
        // ------------------------------------------------------------------
        // fsfltobstinfo.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFLTOBSTINFO
        {
            internal FSFLOWAROUND flow;
            internal FSPOLYGONINFO polyginfo;
            internal int fSuppressAutoClear;
        }
        // ------------------------------------------------------------------
        // fsfmtr.h
        // ------------------------------------------------------------------
        internal enum FSFMTRKSTOP : int       // formatting result
        {
            fmtrGoalReached = 0,
            fmtrBrokenOutOfSpace = 1,
            fmtrBrokenPageBreak = 2,
            fmtrBrokenColumnBreak = 3,
            fmtrBrokenPageBreakBeforePara = 4,
            fmtrBrokenColumnBreakBeforePara = 5,
            fmtrBrokenPageBreakBeforeSection = 6,
            fmtrBrokenDelayable = 7,
            fmtrNoProgressOutOfSpace = 8,
            fmtrNoProgressPageBreak = 9,
            fmtrNoProgressPageBreakBeforePara = 10,
            fmtrNoProgressColumnBreakBeforePara = 11,
            fmtrNoProgressPageBreakBeforeSection = 12,
            fmtrNoProgressPageSkipped = 13,
            fmtrNoProgressDelayable = 14,
            fmtrCollision = 15
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFMTR
        {
            internal FSFMTRKSTOP kstop;
            internal int fContainsItemThatStoppedBeforeFootnote;
            internal int fForcedProgress;
        }
        internal enum FSFMTRBL : int          // formatting bottomless result
        {
            fmtrblGoalReached = 0,
            fmtrblCollision = 1,
            fmtrblInterrupted = 2
        }
        // ------------------------------------------------------------------
        // fsftninfo.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFTNINFO
        {
            internal IntPtr nmftn;  // name of footnote
            internal int vrAccept;
            internal int vrReject;  // must be equal to vrAccept if fsffiWordFlowTextFinite is not set
        }
        // ------------------------------------------------------------------
        // fsftninfoword.h
        // ------------------------------------------------------------------
        // internal struct FSFTNINFOWORD
        // ------------------------------------------------------------------
        // fsgeomds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSINTERVAL
        {
            internal int ur;
            internal int dur;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFILLINFO
        {
            internal FSRECT fsrc;
            internal int fLastInPara;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSEMPTYSPACE
        {
            internal int ur;
            internal int dur;
            internal int fPolygonInside;
        }
        // ------------------------------------------------------------------
        // fshyphenquality.h
        // ------------------------------------------------------------------
        internal enum FSHYPHENQUALITY : int
        {
            fshqExcellent = 0,
            fshqGood = 1,
            fshqFair = 2,
            fshqPoor = 3,
            fshqBad = 4
        }
        // ------------------------------------------------------------------
        // fsimeth.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSIMETHODS
        {
             internal ObjCreateContext pfnCreateContext;
             internal ObjDestroyContext pfnDestroyContext;
             internal ObjFormatParaFinite pfnFormatParaFinite;
             internal ObjFormatParaBottomless pfnFormatParaBottomless;
             internal ObjUpdateBottomlessPara pfnUpdateBottomlessPara;
             internal ObjSynchronizeBottomlessPara pfnSynchronizeBottomlessPara;
             internal ObjComparePara pfnComparePara;
             internal ObjClearUpdateInfoInPara pfnClearUpdateInfoInPara;
             internal ObjDestroyPara pfnDestroyPara;
             internal ObjDuplicateBreakRecord pfnDuplicateBreakRecord;
             internal ObjDestroyBreakRecord pfnDestroyBreakRecord;
             internal ObjGetColumnBalancingInfo pfnGetColumnBalancingInfo;
             internal ObjGetNumberFootnotes pfnGetNumberFootnotes;
             internal ObjGetFootnoteInfo pfnGetFootnoteInfo;
             internal IntPtr pfnGetFootnoteInfoWord;
             internal ObjShiftVertical pfnShiftVertical;
             internal ObjTransferDisplayInfoPara pfnTransferDisplayInfoPara;
        }
        // ------------------------------------------------------------------
        // fskalignpage.h
        // ------------------------------------------------------------------
        internal enum FSKALIGNPAGE : int      // kinds of page alignment
        {
            fskalpgTop = 0,
            fskalpgCenter = 1,
            fskalpgBottom = 2
        }
        // ------------------------------------------------------------------
        // fskchange.h
        // ------------------------------------------------------------------
        internal enum FSKCHANGE : int         // kind of change of backing store
        {
            fskchNone = 0,
            fskchNew = 1,
            fskchInside = 2
        }
        // ------------------------------------------------------------------
        // fskclear.h
        // ------------------------------------------------------------------
        internal enum FSKCLEAR : int          // kind of clearing
        {
            fskclearNone = 0,
            fskclearLeft = 1,
            fskclearRight = 2,
            fskclearBoth = 3
        }
        // ------------------------------------------------------------------
        // fskfiguretype.h
        // ------------------------------------------------------------------
        // internal enum FSKFIGURETYPE

        // ------------------------------------------------------------------
        // fskwrap.h
        // ------------------------------------------------------------------
        internal enum FSKWRAP : int           // kind of wrapping around obstacle
        {
            fskwrNone = 0,
            fskwrLeft = 1,
            fskwrRight = 2,
            fskwrBoth = 3,
            fskwrLargest = 4
        }

        // ------------------------------------------------------------------
        // fsksuppress.h
        // ------------------------------------------------------------------
        internal enum FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA
        {
        	fsksuppresshardbreakbeforefirstparaNone,
        	fsksuppresshardbreakbeforefirstparaColumn,
        	fsksuppresshardbreakbeforefirstparaPageAndColumn
        };

        // ------------------------------------------------------------------
        // fsmathbreakquality.h
        // ------------------------------------------------------------------
        // internal enum FSMATHBREAKQUALITY
        // ------------------------------------------------------------------
        // fsmathparabreakrec.h
        // ------------------------------------------------------------------
        // internal struct FSBREAKRECEQUATION
        // internal struct FSBREAKRECMATHTRACK
        // internal struct FSBREAKRECMATHPARAPROPER
        // internal struct FSBREAKRECMATHPARA
        // ------------------------------------------------------------------
        // fsmathparacbk.h
        // ------------------------------------------------------------------
        // internal struct FSMATHPARACBK
        // ------------------------------------------------------------------
        // fsmathparacbkds.h
        // ------------------------------------------------------------------
        // internal enum FSKMATHPARAHORIZALIGN
        // internal struct FSMATHPARAPROPERTIES
        // internal enum FSKEQUATIONBREAKRESTRICTION
        // internal enum FSKEQUATIONNUMBERVERTALIGN
        // internal enum FSKEQUATIONNUMBERHORIZALIGN
        // internal enum FSKMATHLINEHORIZALIGN
        // internal struct FSEQUATIONPROPERTIES
        // internal enum FSMATHLRES
        // internal struct FSMATHLINEVARIANT
        // ------------------------------------------------------------------
        // fsmathparahandlerds.h
        // ------------------------------------------------------------------
        // internal struct FSMATHPARAINIT
        // ------------------------------------------------------------------
        // fsmathparaqueryds.h
        // ------------------------------------------------------------------
        // internal struct FSMATHPARADETAILS
        // internal struct FSEQUATIONDESCRIPTION
        // internal struct FSEQUATIONDETAILS
        // internal struct FSEQUATIONNUMBERDESCRIPTION
        // internal struct FSMATHLINEDESCRIPTION
        // ------------------------------------------------------------------
        // fsobstinfo.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFLOWAROUND
        {
            internal FSRECT fsrcBounding;
            internal FSKWRAP fskwr;
            internal int durTooNarrow;
            internal int durDistTextLeft;
            internal int durDistTextRight;
            internal int dvrDistTextTop;
            internal int dvrDistTextBottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPOLYGONINFO
        {
            internal int cPolygons;             // number of polygons
            internal unsafe int* rgcVertices;   // array of vertex counts (array containing number of vertices for each polygon)
            internal int cfspt;                 // total number of vertices in all polygons
            internal unsafe FSPOINT* rgfspt;    // array of all vertices
            internal int fWrapThrough;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSOVERLAP
        {
            internal FSRECT fsrc;
            internal int fAllowOverlap;
        }
        // ------------------------------------------------------------------
        // fsqueryds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFIGUREDETAILS
        {
            internal FSRECT fsrcFlowAround;
            internal FSBBOX fsbbox;
            internal FSPOINT fsptPosPreliminary;
            internal int fDelayed;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSLINEELEMENT
        {
            internal IntPtr pfslineclient;
            internal int dcpFirst;              // dcpFirst of the element
            internal IntPtr pfsbreakreclineclient;  // break record needed to format this element
            internal int dcpLim;                // dcpLim of the element
            internal int urStart;               // starting ur of the element
            internal int dur;                   // width used during formatting
            internal int fAllowHyphenation;     // was hyphenation allowed when formatting?
            internal int urBBox;
            internal int durBBox;
            internal int urLrWord;              // starting ur of LR (for Word only)
            internal int durLrWord;             // width of LR (for Word only)
            internal int dvrAscent;             // ascent (excluding suppressed space)
            internal int dvrDescent;            // descent (including space-after)
            internal int fClearOnLeft;          // this line is clear on left side
            internal int fClearOnRight;         // this line is clear on right side
            internal int fHitByPolygon;         // was this element hit by polygon?
            internal int fForceBroken;          // value of fForcedBroken returned by FormatLine
            internal int fClearLeftLrWord;      // equivalent of word's !lr.fConstrainLeft  (Word only)
            internal int fClearRightLrWord;     // equivalent of word's !lr.fConstrainRight (Word only)
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSLINEDESCRIPTIONCOMPOSITE
        {
            internal IntPtr pline;              // to be used for QueryLineElements
            internal int cElements;             // number of elements in this line
            internal int vrStart;               // vr at the top of line
            internal int dvrAscent;             // aggregated ascent (excluding suppressed space)
            internal int dvrDescent;            // aggregated descent (including space after)
            internal int fTreatedAsFirst;       // was line treated as first during formatting?
            internal int fTreatedAsLast;        // was line treated as last during formatting?
            internal int dvrAvailableForcedLine;// dvrAvailable if FormatLineForced was used
            internal int fUsedWordFormatLineInChain;// Word's FormatLineChain compatible line?
            internal int fFirstLineInWordLr;    // Beginning of LR (Word only)
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSLINEDESCRIPTIONSINGLE
        {
            internal IntPtr pfslineclient;
            internal IntPtr pfsbreakreclineclient;  // break record needed to format this line
            internal int dcpFirst;              // dcpFirst of line
            internal int dcpLim;                // dcpLim of line
            internal int urStart;               // starting ur of the line
            internal int dur;                   // width used during formatting
            internal int fAllowHyphenation;     // was hyphenation allowed when formatting?
            internal int urBBox;
            internal int durBBox;
            internal int vrStart;               // vr at the top of line
            internal int dvrAscent;             // aggregated ascent (excluding suppressed space)
            internal int dvrDescent;            // aggregated descent (including space after)
            internal int fClearOnLeft;          // this line is clear on left side
            internal int fClearOnRight;         // this line is clear on right side
            internal int fTreatedAsFirst;       // was line treated as first during formatting?
            internal int fForceBroken;          // value of fForcedBroken returned by FormatLine
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSATTACHEDOBJECTDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal IntPtr pfspara;
            internal IntPtr pfsparaclient;
            internal IntPtr nmp;
            internal int idobj;
            internal int vrStart;
            internal int dvrUsed;
            internal FSBBOX fsbbox;
            internal int dvrTopSpace;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSDROPCAPDETAILS
        {
            internal FSRECT fsrcDropCap;            // position of drop cap rectangle
            internal int fSuppressDropCapTopSpacing;// was space at the top of the page suppressed?
            internal IntPtr pdcclient;              // ptr to drop cap created by client
        }
        internal enum FSKTEXTLINES : int            // tells which format line callback was used when formatting this paragraph
                                                    // client has to reconstruct lines accordingly
        {
            fsklinesNormal = 0,                     // use normal FormatLine callback
            fsklinesOptimal = 1,                    // use reconstruction ReconstructLineVariant callback
            fsklinesForced = 2,                     // use special forced formatting FormatLineForced callback
                                                    // note that forced paragraph can have only 1 line
            fsklinesWord = 3,                       // use word-compatibility callback pfnFormatLineWord
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTEXTDETAILSFULL
        {
            internal uint fswdir;                   // writing direction in text paragraph
            internal FSKTEXTLINES fsklines;         // kind of text lines: Word, Optimal, Normal
            internal int fLinesComposite;           // if lines are composite
            internal int cLines;                    // number of lines
            internal int cAttachedObjects;          // number of floaters
            internal int dcpFirst;                  // dcp of the first line, only if  cLines > 0
            internal int dcpLim;                    // dcpLim of the last line, only if cLines > 0
            internal int fDropCapPresent;           // is drop cap present?
            internal FSUPDATEINFO fsupdinfDropCap;  // update info for dropcap
            internal FSDROPCAPDETAILS dcdetails;    // drop cap details, iff drop cap present
            internal int fSuppressTopLineSpacing;   // was top spacing of first line suppressed?
            internal int fUpdateInfoForLinesPresent;// is following line update info meaningful? FALSE after initial formatting or clear update
            internal int cLinesBeforeChange;
            internal int dvrShiftBeforeChange;
            internal int cLinesChanged;             // number of formatted lines in new line list
            internal int dcLinesChanged;            // cLinesChanged minus number of old lines on their place
            internal int dvrShiftAfterChange;
            internal int ddcpAfterChange;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTEXTDETAILSCACHED
        {
            internal uint fswdir;                   // writing direction in text paragraph
            internal FSKTEXTLINES fsklines;         // kind of text lines: Word, Optimal, Normal
            internal FSRECT fsrcPara;               // paragraph's rectangle
            internal int fSuppressTopLineSpacing;   // was top spacing of first line suppressed?
            internal int dcpFirst;                  // dcp of the first line
            internal int dcpLim;                    // dcpLim of the last line
            internal int cLines;                    // number of simple lines
            internal int fClearOnLeft;              // clear on left side?
            internal int fClearOnRight;             // clear on right side?
            internal int fOptimalLineDcpsCached;    // dcp's for optimal lines are available
        }
        internal enum FSKTEXTDETAILS : int
        {
            fsktdCached = 0,
            fsktdFull = 1
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTEXTDETAILS
        {
            internal FSKTEXTDETAILS fsktd;
            internal nested_u u;
            [StructLayout(LayoutKind.Explicit)]
            internal struct nested_u
            {
                [FieldOffset(0)]
                internal FSTEXTDETAILSFULL full;
                [FieldOffset(0)]
                internal FSTEXTDETAILSCACHED cached;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPARADESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal IntPtr pfspara;
            internal IntPtr pfsparaclient;
            internal IntPtr nmp;
            internal int idobj;
            internal int dvrUsed;                   // input to paragraph formatting method
            internal FSBBOX fsbbox;
            internal int dvrTopSpace;               // returned by paragraph formatting method
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTRACKDETAILS
        {
            internal int cParas;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTRACKDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal IntPtr nms;
            internal FSRECT fsrc;
            internal FSBBOX fsbbox;
            internal int fTrackRelativeToRect;
            internal IntPtr pfstrack;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSUBTRACKDETAILS
        {
            internal FSUPDATEINFO fsupdinf;
            internal IntPtr nms;
            internal FSRECT fsrc;
            internal int cParas;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSUBPAGEDETAILSCOMPLEX
        {
            internal IntPtr nms;
            internal uint fswdir;
            internal FSRECT fsrc;
            internal FSBBOX fsbbox;
            internal int cBasicColumns;
            internal int cSegmentDefinedColumnSpanAreas;
            internal int cHeightDefinedColumnSpanAreas;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSUBPAGEDETAILSSIMPLE
        {
            internal uint fswdir;
            internal FSTRACKDESCRIPTION trackdescr;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSUBPAGEDETAILS
        {
            internal int fSimple;
            internal nested_u u;
            [StructLayout(LayoutKind.Explicit)]
            internal struct nested_u
            {
                [FieldOffset(0)]
                internal FSSUBPAGEDETAILSSIMPLE simple;
                [FieldOffset(0)]
                internal FSSUBPAGEDETAILSCOMPLEX complex;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCOMPOSITECOLUMNDETAILS
        {
            internal FSTRACKDESCRIPTION trackdescrMainText;
            internal FSTRACKDESCRIPTION trackdescrFootnoteSeparator;
            internal FSTRACKDESCRIPTION trackdescrFootnoteContinuationSeparator;
            internal FSTRACKDESCRIPTION trackdescrFootnoteContinuationNotice;
            internal FSTRACKDESCRIPTION trackdescrEndnoteSeparator;
            internal FSTRACKDESCRIPTION trackdescrEndnoteContinuationSeparator;
            internal FSTRACKDESCRIPTION trackdescrEndnoteContinuationNotice;
            internal int cFootnotes;
            internal FSRECT fsrcFootnotes;
            internal FSBBOX fsbboxFootnotes;
            internal FSTRACKDESCRIPTION trackdescrEndnote;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSENDNOTECOLUMNDETAILS
        {
            internal FSTRACKDESCRIPTION trackdescrEndnoteContinuationSeparator;
            internal FSTRACKDESCRIPTION trackdescrEndnoteContinuationNotice;
            internal FSTRACKDESCRIPTION trackdescrEndnote;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSCOMPOSITECOLUMNDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal FSRECT fsrc;
            internal FSBBOX fsbbox;
            internal IntPtr pfscompcol;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSENDNOTECOLUMNDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal FSRECT fsrc;
            internal FSBBOX fsbbox;
            internal IntPtr pfsendnotecol;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSECTIONDETAILSWITHPAGENOTES
        {
            internal uint fswdir;
            internal int fColumnBalancingApplied;
            internal FSRECT fsrcSectionBody;
            internal FSBBOX fsbboxSectionBody;
            internal int cBasicColumns;
            internal int cSegmentDefinedColumnSpanAreas;
            internal int cHeightDefinedColumnSpanAreas;
            internal FSRECT fsrcEndnote;
            internal FSBBOX fsbboxEndnote;
            internal int cEndnoteColumns;
            internal FSTRACKDESCRIPTION trackdescrEndnoteSeparator;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSECTIONDETAILSWITHCOLNOTES
        {
            internal uint fswdir;
            internal int fColumnBalancingApplied;
            internal int cCompositeColumns;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSECTIONDETAILS
        {
            internal int fFootnotesAsPagenotes;
            internal nested_u u;
            [StructLayout(LayoutKind.Explicit)]
            internal struct nested_u
            {
                [FieldOffset(0)]
                internal FSSECTIONDETAILSWITHPAGENOTES withpagenotes;
                [FieldOffset(0)]
                internal FSSECTIONDETAILSWITHCOLNOTES withcolumnnotes;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSSECTIONDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal IntPtr nms;
            internal FSRECT fsrc;
            internal FSBBOX fsbbox;
            internal int fOtherSectionInside;
            internal int dvrUsedTop;                // valid iff fOtherSectionInside
            internal int dvrUsedBottom;             // valid iff fOtherSectionInside
            internal IntPtr pfssection;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFOOTNOTECOLUMNDETAILS
        {
            internal FSTRACKDESCRIPTION trackdescrFootnoteContinuationSeparator;
            internal FSTRACKDESCRIPTION trackdescrFootnoteContinuationNotice;
            internal int cTracks;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSFOOTNOTECOLUMNDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal FSRECT fsrc;
            internal FSBBOX fsbbox;
            internal IntPtr pfsfootnotecol;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPAGEDETAILSCOMPLEX
        {
            internal int fTopBottomHeaderFooter;
            internal uint fswdirHeader;
            internal FSTRACKDESCRIPTION trackdescrHeader;
            internal uint fswdirFooter;
            internal FSTRACKDESCRIPTION trackdescrFooter;
            internal int fJustified;
            internal FSKALIGNPAGE fskalpg;
            internal uint fswdirPageProper;
            internal FSUPDATEINFO fsupdinfPageBody;
            internal FSRECT fsrcPageBody;
            internal FSRECT fsrcPageMarginActual;   // page margins can be recalculated because of big header/footer
            internal FSBBOX fsbboxPageBody;
            internal int cSections;                 // 0 means that body is empty
            internal FSRECT fsrcFootnote;
            internal FSBBOX fsbboxFootnote;
            internal int cFootnoteColumns;          // 0 means that there are no pagenotes
            internal FSTRACKDESCRIPTION trackdescrFootnoteSeparator;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPAGEDETAILSSIMPLE
        {
            internal FSTRACKDESCRIPTION trackdescr;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSPAGEDETAILS
        {
            internal FSKUPDATE fskupd; // only fskupdNew/fskupdChangeInside/fskupdNoChange are possible
            internal int fSimple;
            internal nested_u u;
            [StructLayout(LayoutKind.Explicit)]
            internal struct nested_u
            {
                [FieldOffset(0)]
                internal FSPAGEDETAILSSIMPLE simple;
                [FieldOffset(0)]
                internal FSPAGEDETAILSCOMPLEX complex;
            }
        }
        // ------------------------------------------------------------------
        // fstablebreakrec.h
        // ------------------------------------------------------------------
        // internal unsafe struct FSBREAKRECTABLEROW
        // internal unsafe struct FSBREAKRECTABLETRACK
        // internal unsafe struct FSBREAKRECTABLE
        // ------------------------------------------------------------------
        // fstablecbk.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLECBKFETCH
        {
             internal GetFirstHeaderRow pfnGetFirstHeaderRow;
             internal GetNextHeaderRow pfnGetNextHeaderRow;
             internal GetFirstFooterRow pfnGetFirstFooterRow;
             internal GetNextFooterRow pfnGetNextFooterRow;
             internal GetFirstRow pfnGetFirstRow;
             internal GetNextRow pfnGetNextRow;
             internal UpdFChangeInHeaderFooter pfnUpdFChangeInHeaderFooter;
             internal UpdGetFirstChangeInTable pfnUpdGetFirstChangeInTable;
             internal UpdGetRowChange pfnUpdGetRowChange;
             internal UpdGetCellChange pfnUpdGetCellChange;
             internal GetDistributionKind pfnGetDistributionKind;
             internal GetRowProperties pfnGetRowProperties;
             internal GetCells pfnGetCells;
             internal FInterruptFormattingTable pfnFInterruptFormattingTable;
             internal CalcHorizontalBBoxOfRow pfnCalcHorizontalBBoxOfRow;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLECBKCELL
        {
             internal FormatCellFinite pfnFormatCellFinite;
             internal FormatCellBottomless pfnFormatCellBottomless;
             internal UpdateBottomlessCell pfnUpdateBottomlessCell;
             internal CompareCells pfnCompareCells;
             internal ClearUpdateInfoInCell pfnClearUpdateInfoInCell;
             internal SetCellHeight pfnSetCellHeight;
             internal DestroyCell pfnDestroyCell;
             internal DuplicateCellBreakRecord pfnDuplicateCellBreakRecord;
             internal DestroyCellBreakRecord pfnDestroyCellBreakRecord;
             internal GetCellNumberFootnotes pfnGetCellNumberFootnotes;
             internal IntPtr pfnGetCellFootnoteInfo;
             internal IntPtr pfnGetCellFootnoteInfoWord;
             internal GetCellMinColumnBalancingStep pfnGetCellMinColumnBalancingStep;
             internal TransferDisplayInfoCell pfnTransferDisplayInfoCell;
        }
        // ------------------------------------------------------------------
        // fstablecbkds.h
        // ------------------------------------------------------------------
        internal enum FSKTABLEHEIGHTDISTRIBUTION : int
        {
            fskdistributeUnchanged = 0,
            fskdistributeEqually = 1,
            fskdistributeProportionally = 2
        }
        internal enum FSKROWHEIGHTRESTRICTION : int
        {
            fskrowheightNatural = 0,
            fskrowheightAtLeast = 1,
            fskrowheightAtMostNoBreak = 2,
            fskrowheightExactNoBreak = 3
        }
        internal enum FSKROWBREAKRESTRICTION : int
        {
            fskrowbreakAnywhere = 0,
            fskrowbreakNoBreakInside = 1,
            fskrowbreakNoBreakInsideAfter = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEROWPROPS
        {
            internal FSKROWBREAKRESTRICTION fskrowbreak;
            internal FSKROWHEIGHTRESTRICTION fskrowheight;
            internal int dvrRowHeightRestriction;
            internal int fBBoxOverflowsBottom;
            internal int dvrAboveRow;
            internal int dvrBelowRow;
            internal int dvrAboveTopRow;
            internal int dvrBelowBottomRow;
            internal int dvrAboveRowBreak;
            internal int dvrBelowRowBreak;
            internal int cCells;
        }
        // ------------------------------------------------------------------
        // fstablekcellmerge.h
        // ------------------------------------------------------------------
        internal enum FSTABLEKCELLMERGE : int   // kinds of merged cell
        {
            fskcellmergeNo = 0,
            fskcellmergeFirst = 1,
            fskcellmergeMiddle = 2,
            fskcellmergeLast = 3
        }
        // ------------------------------------------------------------------
        // fstableobjbreakrec.h
        // ------------------------------------------------------------------
        // internal unsafe struct FSBREAKRECTABLEOBJ
        // ------------------------------------------------------------------
        // fstableobjhandlerds.h
        // ------------------------------------------------------------------
        internal enum FSKTABLEOBJALIGNMENT : int
        {
            fsktableobjAlignLeft = 0,
            fsktableobjAlignRight = 1,
            fsktableobjAlignCenter = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEOBJPROPS
        {
            internal FSKCLEAR fskclear;
            internal FSKTABLEOBJALIGNMENT ktablealignment;
            internal int fFloat;
            internal FSKWRAP fskwr;
            internal int fDelayNoProgress;
            internal int dvrCaptionTop;
            internal int dvrCaptionBottom;
            internal int durCaptionLeft;
            internal int durCaptionRight;
            internal uint fswdirTable;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEOBJCBK
        {
             internal GetTableProperties pfnGetTableProperties;
             internal AutofitTable pfnAutofitTable;
             internal UpdAutofitTable pfnUpdAutofitTable;
             internal GetMCSClientAfterTable pfnGetMCSClientAfterTable;
             internal IntPtr pfnGetDvrUsedForFloatTable;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLECBKFETCHWORD
        {
             internal IntPtr pfnGetTablePropertiesWord;
             internal IntPtr pfnGetRowPropertiesWord;
             internal IntPtr pfnGetRowWidthWord;
             internal IntPtr pfnGetNumberFiguresForTableRow;
             internal IntPtr pfnGetFiguresForTableRow;
             internal IntPtr pfnFStopBeforeTableRowLr;
             internal IntPtr pfnFIgnoreCollisionForTableRow;
             internal IntPtr pfnChangeRowHeightRestriction;
             internal IntPtr pfnNotifyRowPosition;
             internal IntPtr pfnNotifyRowBorderAbove;
             internal IntPtr pfnNotifyTableBreakRec;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEOBJINIT
        {
            internal FSTABLEOBJCBK tableobjcbk;
            internal FSTABLECBKFETCH tablecbkfetch;
            internal FSTABLECBKCELL tablecbkcell;
            internal FSTABLECBKFETCHWORD tablecbkfetchword;
        }
        // ------------------------------------------------------------------
        // fstableobjqueryds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEOBJDETAILS
        {
            internal IntPtr fsnmTable;
            internal FSRECT fsrcTableObj;
            internal int dvrTopCaption;
            internal int dvrBottomCaption;
            internal int durLeftCaption;
            internal int durRightCaption;
            internal uint fswdirTable;
            internal FSKUPDATE fskupdTableProper;
            internal IntPtr pfstableProper;
        }
        // ------------------------------------------------------------------
        // fstablequeryds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEDETAILS
        {
            internal int dvrTable;
            internal int cRows;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEROWDESCRIPTION
        {
            internal FSUPDATEINFO fsupdinf;
            internal IntPtr fsnmRow;
            internal IntPtr pfstablerow;
            internal int fRowInSeparateRect;
            internal nested_u u;
            [StructLayout(LayoutKind.Explicit)]
            internal struct nested_u
            {
                [FieldOffset(0)]
                internal FSRECT fsrcRow;
                [FieldOffset(0)]
                internal int dvrRow;
            }
        }
        internal enum FSKTABLEROWBOUNDARY : int
        {
            fsktablerowboundaryOuter = 0,
            fsktablerowboundaryInner = 1,
            fsktablerowboundaryBreak = 2
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLEROWDETAILS
        {
            internal FSKTABLEROWBOUNDARY fskboundaryAbove;
            internal int dvrAbove;
            internal FSKTABLEROWBOUNDARY fskboundaryBelow;
            internal int dvrBelow;
            internal int cCells;
            internal int fForcedRow;
        }
        // internal struct FSTABLEFIGUREDESCRIPTION
        // ------------------------------------------------------------------
        // fstablesrvds.h
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSTABLESRVCONTEXT
        {
            internal IntPtr pfscontext;
            internal IntPtr pfsclient;
            internal FSCBKOBJ cbkobj;
            internal FSTABLECBKFETCH tablecbkfetch;
            internal FSTABLECBKCELL tablecbkcell;
            internal uint fsffi;
        }
        // ------------------------------------------------------------------
        // fsupdateinfo.h
        // ------------------------------------------------------------------
        internal enum FSKUPDATE : int       // result of update
        {
            fskupdInherited = 0,
            fskupdNoChange = 1,
            fskupdNew = 2,
            fskupdChangeInside = 3,
            fskupdShifted = 4
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct FSUPDATEINFO
        {
            public FSKUPDATE fskupd;
            public int dvrShifted;
        }

        #endregion Data structures

        // ------------------------------------------------------------------
        //
        // Callbacks.
        //
        // ------------------------------------------------------------------

        #region Callbacks

        // ------------------------------------------------------------------
        // assert
        // ------------------------------------------------------------------
         internal delegate void AssertFailed(
            string arg1,                        // IN:
            string arg2,                        // IN:
            int arg3,                           // IN:
            uint arg4);                         // IN:
        // ------------------------------------------------------------------
        // fscbkfig.h
        // ------------------------------------------------------------------
         internal delegate int GetFigureProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr nmpFigure,                   // IN:  figure's name
            int fInTextLine,                    // IN:  it is attached to text line
            uint fswdir,                        // IN:  current direction
            int fBottomUndefined,               // IN:  bottom of page is not defined
            out int dur,                        // OUT: width of figure
            out int dvr,                        // OUT: height of figure
            out FSFIGUREPROPS fsfigprops,       // OUT: figure attributes
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices,                  // OUT: total number of vertices in all polygons
            out int durDistTextLeft,            // OUT: distance to text from MinU side
            out int durDistTextRight,           // OUT: distance to text from MaxU side
            out int dvrDistTextTop,             // OUT: distance to text from MinV side
            out int dvrDistTextBottom);         // OUT: distance to text from MaxV side
         internal unsafe delegate int GetFigurePolygons(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr nmpFigure,                   // IN:  figure's name
            uint fswdir,                        // IN:  current direction
            int ncVertices,                     // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                          // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            FSPOINT* rgfspt,                    // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough);              // OUT: fill text in empty areas within obstacles?
         internal delegate int CalcFigurePosition(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr nmpFigure,                   // IN:  figure's name
            uint fswdir,                        // IN:  current direction
            ref FSRECT fsrcPage,                // IN:  page rectangle
            ref FSRECT fsrcMargin,              // IN:  rectangle within page margins
            ref FSRECT fsrcTrack,               // IN:  track rectangle
            ref FSRECT fsrcFigurePreliminary,   // IN:  prelim figure rect calculated from figure props
            int fMustPosition,                  // IN:  must find position in this track?
            int fInTextLine,                    // IN:  it is attached to text line
            out int fPushToNextTrack,           // OUT: push to next track?
            out FSRECT fsrcFlow,                // OUT: FlowAround rectangle
            out FSRECT fsrcOverlap,             // OUT: Overlap rectangle
            out FSBBOX fsbbox,                  // OUT: bbox
            out FSRECT fsrcSearch);             // OUT: search area for overlap
        // ------------------------------------------------------------------
        // fscbkgen.h
        // ------------------------------------------------------------------
         internal delegate int FSkipPage(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of first section on the page
            out int fSkip);                     // OUT: skip it due to odd/even page issue
         internal delegate int GetPageDimensions(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section on page
            out uint fswdir,                    // OUT: direction of main text
            out int fHeaderFooterAtTopBottom,   // OUT: header/footer position on the page
            out int durPage,                    // OUT: page width
            out int dvrPage,                    // OUT: page height
            ref FSRECT fsrcMargin);             // OUT: rectangle within page margins
         internal delegate int GetNextSection(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsCur,                      // IN:  name of current section
            out int fSuccess,                   // OUT: next section exists
            out IntPtr nmsNext);                // OUT: name of the next section
         internal delegate int GetSectionProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int fNewPage,                   // OUT: stop page before this section?
            out uint fswdir,                    // OUT: direction of this section
            out int fApplyColumnBalancing,      // OUT: apply column balancing to this section?
            out int ccol,                       // OUT: number of columns in the main text segment
            out int cSegmentDefinedColumnSpanAreas, // OUT:
            out int cHeightDefinedColumnSpanAreas); // OUT:
         internal unsafe delegate int GetJustificationProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr* rgnms,                      // IN:  array of the section names on the page
            int cnms,                           // IN:  number of sections on the page
            int fLastSectionNotBroken,          // IN:  is last section on the page broken?
            out int fJustify,                   // OUT: apply justification/alignment to the page?
            out FSKALIGNPAGE fskal,             // OUT: kind of vertical alignment for the page
            out int fCancelAtLastColumn);       // OUT: cancel justification for the last column of the page?
         internal delegate int GetMainTextSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out IntPtr nmSegment);              // OUT: name of the main text segment for this section
         internal delegate int GetHeaderSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            IntPtr pfsbrpagePrelim,             // IN:  ptr to page break record of main page
            uint fswdir,                        // IN:  direction for dvrMaxHeight/dvrFromEdge
            out int fHeaderPresent,             // OUT: is there header on this page?
            out int fHardMargin,                // OUT: does margin increase with header?
            out int dvrMaxHeight,               // OUT: maximum size of header
            out int dvrFromEdge,                // OUT: distance from top edge of the paper
            out uint fswdirHeader,              // OUT: direction for header
            out IntPtr nmsHeader);              // OUT: name of header segment
         internal delegate int GetFooterSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            IntPtr pfsbrpagePrelim,             // IN:  ptr to page break record of main page
            uint fswdir,                        // IN:  direction for dvrMaxHeight/dvrFromEdge
            out int fFooterPresent,             // OUT: is there footer on this page?
            out int fHardMargin,                // OUT: does margin increase with footer?
            out int dvrMaxHeight,               // OUT: maximum size of footer
            out int dvrFromEdge,                // OUT: distance from bottom edge of the paper
            out uint fswdirFooter,              // OUT: direction for footer
            out IntPtr nmsFooter);              // OUT: name of footer segment
         internal delegate int UpdGetSegmentChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of the segment
            out FSKCHANGE fskch);               // OUT: kind of change
         internal unsafe delegate int GetSectionColumnInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            uint fswdir,                        // IN:  direction of section
            int ncol,                           // IN:  size of the preallocated fscolinfo array
            FSCOLUMNINFO* fscolinfo,            // OUT: array of the colinfo structures
            out int ccol);                      // OUT: actual number of the columns in the segment
         internal unsafe delegate int GetSegmentDefinedColumnSpanAreaInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            int cAreas,                         // IN:  number of areas - size of pre-allocated arrays
            IntPtr* rgnmSeg,                    // OUT: array of segment names for segment-defined areas
            int* rgcColumns,                    // OUT: arrays of number of columns spanned
            out int cAreasActual);              // OUT: actual number of segment-defined areas
         internal unsafe delegate int GetHeightDefinedColumnSpanAreaInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            int cAreas,                         // IN:  number of areas - size of pre-allocated arrays
            int* rgdvrAreaHeight,               // OUT: array of segment names for height-defined areas
            int* rgcColumns,                    // OUT: arrays of number of columns spanned
            out int cAreasActual);              // OUT: actual number of height-defined areas
         internal delegate int GetFirstPara(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of segment
            out int fSuccessful,                // OUT: does segment contain any paragraph?
            out IntPtr nmp);                    // OUT: name of the first paragraph in segment
         internal delegate int GetNextPara(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of segment
            IntPtr nmpCur,                      // IN:  name of current para
            out int fFound,                     // OUT: is there next paragraph?
            out IntPtr nmpNext);                // OUT: name of the next paragraph in section
         internal delegate int UpdGetFirstChangeInSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of segment
            out int fFound,                     // OUT: anything changed?
            out int fChangeFirst,               // OUT: first paragraph changed?
            out IntPtr nmpBeforeChange);        // OUT: name of paragraph before the change if !fChangeFirst
         internal delegate int UpdGetParaChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of the paragraph
            out FSKCHANGE fskch,                // OUT: kind of change
            out int fNoFurtherChanges);         // OUT: no changes after?
         internal delegate int GetParaProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            ref FSPAP fspap);                   // OUT: paragraph properties
         internal delegate int CreateParaclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            out IntPtr pfsparaclient);          // OUT: opaque to PTS paragraph client
         internal delegate int TransferDisplayInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientOld,            // IN:  opaque to PTS old paragraph client
            IntPtr pfsparaclientNew);           // IN:  opaque to PTS new paragraph client
         internal delegate int DestroyParaclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient);              // IN:  opaque to PTS paragraph client
         internal delegate int FInterruptFormattingAfterPara(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:  opaque to PTS paragraph client
            IntPtr nmp,                         // IN:  name of paragraph
            int vr,                             // IN:  current v position
            out int fInterruptFormatting);      // OUT: is it time to stop formatting?
         internal delegate int GetEndnoteSeparators(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out IntPtr nmsEndnoteSeparator,     // OUT: name of the endnote separator segment
            out IntPtr nmEndnoteContSeparator,  // OUT: name of endnote cont separator segment
            out IntPtr nmsEndnoteContNotice);   // OUT: name of the endnote cont notice segment
         internal delegate int GetEndnoteSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out int fEndnotesPresent,           // OUT: are there endnotes for this segment?
            out IntPtr nmsEndnotes);            // OUT: name of endnote segment
         internal delegate int GetNumberEndnoteColumns(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int ccolEndnote);               // OUT: number of columns in endnote area
         internal unsafe delegate int GetEndnoteColumnInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            uint fswdir,                        // IN:  direction of section
            int ncolEndnote,                    // IN:  size of preallocated fscolinfo array
            FSCOLUMNINFO* fscolinfoEndnote,     // OUT: array of the colinfo structures
            out int ccolEndnote);               // OUT: actual number of the columns in footnote area
         internal delegate int GetFootnoteSeparators(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out IntPtr nmsFtnSeparator,         // OUT: name of the footnote separator segment
            out IntPtr nmsFtnContSeparator,     // OUT: name of the ftn cont separator segment
            out IntPtr nmsFtnContNotice);       // OUT: name of the footnote cont notice segment
         internal delegate int FFootnoteBeneathText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int fFootnoteBeneathText);      // OUT: position footnote right after text?
         internal delegate int GetNumberFootnoteColumns(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int ccolFootnote);              // OUT: number of columns in footnote area
         internal unsafe delegate int GetFootnoteColumnInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            uint fswdir,                        // IN:  direction of main text
            int ncolFootnote,                   // IN:  size of preallocated fscolinfo array
            FSCOLUMNINFO* fscolinfoFootnote,    // OUT: array of the colinfo structures
            out int ccolFootnote);              // OUT: actual number of the columns in footnote area
         internal delegate int GetFootnoteSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmftn,                       // IN:  name of footnote
            out IntPtr nmsFootnote);            // OUT: name of footnote segment
         internal unsafe delegate int GetFootnotePresentationAndRejectionOrder(
            IntPtr pfsclient,                           // IN:  client opaque data
            int cFootnotes,                             // IN:  size of all arrays
            IntPtr* rgProposedPresentationOrder,        // IN:  footnotes in proposed pres order
            IntPtr* rgProposedRejectionOrder,           // IN:  footnotes in proposed reject order
            out int fProposedPresentationOrderAccepted, // OUT: agree with proposed order?
            IntPtr* rgFinalPresentationOrder,           // OUT: footnotes in final pres order
            out int fProposedRejectionOrderAccepted,    // OUT: agree with proposed order?
            IntPtr* rgFinalRejectionOrder);             // OUT: footnotes in final reject order
         internal delegate int FAllowFootnoteSeparation(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmftn,                       // IN:  name of footnote
            out int fAllow);                    // OUT: allow separating footnote from its reference
        // ------------------------------------------------------------------
        // fscbkobj.h
        // ------------------------------------------------------------------
        // NewPtr
        // DisposePtr
        // ReallocPtr
         internal delegate int DuplicateMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pmcsclientIn,                // IN:  margin collapsing state
            out IntPtr pmcsclientNew);          // OUT: duplicated margin collapsing state
         internal delegate int DestroyMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pmcsclient);                 // IN:  margin collapsing state to destroy
         internal delegate int FEqualMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pmcsclient1,                 // IN:  first margin collapsing state to compare
            IntPtr pmcsclient2,                 // IN:  second margin collapsing state to compare
            out int fEqual);                    // OUT: are MStructs equal?
         internal delegate int ConvertMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            uint fswdir,                        // IN:  current direction
            IntPtr pmcsclient,                  // IN:  pointer to the input margin collapsing state
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            out int dvr);                       // OUT: dvr, calculated based on margin collapsing state
         internal delegate int GetObjectHandlerInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            int idobj,                          // IN:  id of the object handler
            IntPtr pobjectinfo);                // OUT: initialization information for the specified object
        // ------------------------------------------------------------------
        // fscbktxt.h
        // ------------------------------------------------------------------
         internal delegate int CreateParaBreakingSession(
            IntPtr pfsclient,                   // IN:  client opaque data 
            IntPtr pfsparaclient,               // IN:  opaque to PTS paragraph client
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            int fsdcpStart,                     // IN:  dcp where formatting will start
            IntPtr pfsbreakreclineclient,       // IN:  break record for the first line
            uint fswdir,                        // IN:  current direction
            int urStartTrack,                   // IN:  position at the beginning of the track
            int durTrack,                       // IN:  width of track
            int urPageLeftMargin,               // IN:  left margin of the page
            out IntPtr ppfsparabreakingsession, // OUT: paragraph breaking session
            out int fParagraphJustified);       // OUT: if paragraph is justified
         internal delegate int DestroyParaBreakingSession(
            IntPtr pfsclient,                   // IN:  client opaque data 
            IntPtr pfsparabreakingsession);     // IN:  session to destroy
         internal delegate int GetTextProperties(
            IntPtr pfsclient,                   // IN:  client opaque data 
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            ref FSTXTPROPS fstxtprops);         // OUT: text paragraph properties
         internal delegate int GetNumberFootnotes(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int fsdcpStart,                     // IN:  dcp at the beginning of the range
            int fsdcpLim,                       // IN:  dcp at the end of the range
            out int nFootnote);                 // OUT: number of footnote references in the range
         internal unsafe delegate int GetFootnotes(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int fsdcpStart,                     // IN:  dcp at the beginning of the range
            int fsdcpLim,                       // IN:  dcp at the end of the range
            int nFootnotes,                     // IN:  size of the output array
            IntPtr* rgnmftn,                    // OUT: array of footnote names in the range
            int* rgdcp,                         // OUT: array of footnote refs in the range
            out int cFootnotes);                // OUT: actual number of footnotes
         internal delegate int FormatDropCap(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            uint fswdir,                        // IN:  current direction
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            out IntPtr pfsdropc,                // OUT: pointer to drop cap created by client
            out int fInMargin,                  // OUT: should it be positioned in margin or in track?
            out int dur,                        // OUT: width of drop cap
            out int dvr,                        // OUT: height of drop cap
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices,                  // OUT: number of vertices
            out int durText);                   // OUT: distance from text
         internal unsafe delegate int GetDropCapPolygons(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsdropc,                    // IN:  pointer to drop cap
            IntPtr nmp,                         // IN:  para name
            uint fswdir,                        // IN:  current direction
            int ncVertices,                    // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                         // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            FSPOINT* rgfspt,                    // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough);              // OUT: fill text in empty areas within obstacles?
         internal delegate int DestroyDropCap(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsdropc);                   // IN:  pointer to drop cap created by client
         internal delegate int FormatBottomText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            uint fswdir,                        // IN:  current direction
            IntPtr pfslineLast,                 // IN:  last formatted line
            int dvrLine,                        // IN:  height of last line
            out IntPtr pmcsclientOut);          // OUT: margin collapsing state at bottom of text
         internal delegate int FormatLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            int dcp,                            // IN:  dcp at the beginning of the line
            IntPtr pbrlineIn,                   // IN:  client's line break record
            uint fswdir,                        // IN:  current direction
            int urStartLine,                    // IN:  position at the beginning of the line
            int durLine,                        // IN:  maximum width of line
            int urStartTrack,                   // IN:  position at the beginning of the track
            int durTrack,                       // IN:  width of track
            int urPageLeftMargin,               // IN:  left margin of the page
            int fAllowHyphenation,              // IN:  allow hyphenation of the line?
            int fClearOnLeft,                   // IN:  is clear on left side
            int fClearOnRight,                  // IN:  is clear on right side
            int fTreatAsFirstInPara,            // IN:  treat line as first line in paragraph
            int fTreatAsLastInPara,             // IN:  treat line as last line in paragraph
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            out IntPtr pfsline,                 // OUT: pointer to line created by client
            out int dcpLine,                    // OUT: dcp consumed by the line
            out IntPtr ppbrlineOut,             // OUT: client's line break record
            out int fForcedBroken,              // OUT: was line force-broken?
            out FSFLRES fsflres,                // OUT: result of formatting
            out int dvrAscent,                  // OUT: ascent of the line
            out int dvrDescent,                 // OUT: descent of the line
            out int urBBox,                     // OUT: ur of the line's ink
            out int durBBox,                    // OUT: dur of of the line's ink
            out int dcpDepend,                  // OUT: number of chars after line break that were considered
            out int fReformatNeighborsAsLastLine); // OUT: should line segments be reformatted?
         internal delegate int FormatLineForced(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            int dcp,                            // IN:  dcp at the beginning of the line
            IntPtr pbrlineIn,                   // IN:  client's line break record
            uint fswdir,                        // IN:  current direction
            int urStartLine,                    // IN:  position at the beginning of the line
            int durLine,                        // IN:  maximum width of line
            int urStartTrack,                   // IN:  position at the beginning of the track
            int durTrack,                       // IN:  width of track
            int urPageLeftMargin,               // IN:  left margin of the page
            int fClearOnLeft,                   // IN:  is clear on left side
            int fClearOnRight,                  // IN:  is clear on right side
            int fTreatAsFirstInPara,            // IN:  treat line as first line in paragraph
            int fTreatAsLastInPara,             // IN:  treat line as last line in paragraph
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            int dvrAvailable,                   // IN:  available vertical space
            out IntPtr pfsline,                 // OUT: pointer to line created by client
            out int dcpLine,                    // OUT: dcp consumed by the line
            out IntPtr ppbrlineOut,             // OUT: client's line break record
            out FSFLRES fsflres,                // OUT: result of formatting
            out int dvrAscent,                  // OUT: ascent of the line
            out int dvrDescent,                 // OUT: descent of the line
            out int urBBox,                     // OUT: ur of the line's ink
            out int durBBox,                    // OUT: dur of of the line's ink
            out int dcpDepend);                 // OUT: number of chars after line break that were considered
         internal unsafe delegate int FormatLineVariants(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparabreakingsession,      // IN:  current session
            int dcp,                            // IN:  dcp at the beginning line variants
            IntPtr pbrlineIn,                   // IN:  client's line break record
            uint fswdir,                        // IN:  current direction
            int urStartLine,                    // IN:  position at the beginning of the line
            int durLine,                        // IN:  maximum width of line
            int fAllowHyphenation,              // IN:  allow hyphenation of the line?
            int fClearOnLeft,                   // IN:  is clear on left side
            int fClearOnRight,                  // IN:  is clear on right side
            int fTreatAsFirstInPara,            // IN:  treat line as first line in paragraph
            int fTreatAsLastInPara,             // IN:  treat line as last line in paragraph
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            IntPtr lineVariantRestriction,      // IN:  line variant restriction
            int nLineVariantsAlloc,             // IN:  size of the pre-allocated variant array
            FSLINEVARIANT* rgfslinevariant,     // OUT: pre-allocatedarray for line variants
            out int nLineVariantsActual,        // OUT: actual number of variants
            out int iLineVariantBest);          // OUT: best line variant index

         internal delegate int ReconstructLineVariant(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            int dcpStart,                       // IN:  dcp at the beginning of the line
            IntPtr pbrlineIn,                   // IN:  client's line break record to start formatting
            int dcpLine,                        // IN:  dcp this line should end with
            uint fswdir,                        // IN:  current direction
            int urStartLine,                    // IN:  position at the beginning of the line
            int durLine,                        // IN:  maximum width of line
            int urStartTrack,                   // IN:  position at the beginning of the track
            int durTrack,                       // IN:  width of track
            int urPageLeftMargin,               // IN:  left margin of the page
            int fAllowHyphenation,              // IN:  allow hyphenation of the line?
            int fClearOnLeft,                   // IN:  is clear on left side
            int fClearOnRight,                  // IN:  is clear on right side
            int fTreatAsFirstInPara,            // IN:  treat line as first line in paragraph
            int fTreatAsLastInPara,             // IN:  treat line as last line in paragraph
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            out IntPtr pfsline,                 // OUT: pointer to line created by client
            out IntPtr ppbrlineOut,             // OUT: client's line break record
            out int fForcedBroken,              // OUT: was line force-broken?
            out FSFLRES fsflres,                // OUT: result of formatting
            out int dvrAscent,                  // OUT: ascent of the line
            out int dvrDescent,                 // OUT: descent of the line
            out int urBBox,                     // OUT: ur of the line's ink
            out int durBBox,                    // OUT: dur of of the line's ink
            out int dcpDepend,                  // OUT: number of chars after line break that were considered
            out int fReformatNeighborsAsLastLine);  // OUT: should line segments be reformatted?
         internal delegate int DestroyLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsline);                    // IN:  pointer to line created by client
         internal delegate int DuplicateLineBreakRecord(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pbrlineIn,                   // IN:  client's forced break record to duplicate
            out IntPtr pbrlineDup);             // OUT: duplicated client's forced break record
         internal delegate int DestroyLineBreakRecord(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pbrlineIn);                  // IN:  client's forced break record to duplicate
         internal delegate int SnapGridVertical(
            IntPtr pfsclient,                   // IN:  client opaque data
            uint fswdir,                        // IN:  current direction
            int vrMargin,                       // IN:  top margin
            int vrCurrent,                      // IN:  current vertical position
            out int vrNew);                     // OUT: snapped vertical position
         internal delegate int GetDvrSuppressibleBottomSpace(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsline,                     // IN:  pointer to line created by client
            uint fswdir,                        // IN:  current direction
            out int dvrSuppressible);           // OUT: empty space suppressible at the bottom
         internal delegate int GetDvrAdvance(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int dcp,                            // IN:  dcp at the beginning of the line
            uint fswdir,                        // IN:  current direction
            out int dvr);                       // OUT: advance amount in tight wrap
         internal delegate int UpdGetChangeInText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            out int dcpStart,                   // OUT: start of change
            out int ddcpOld,                    // OUT: number of chars in old range
            out int ddcpNew);                   // OUT: number of chars in new range
         internal delegate int UpdGetDropCapChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            out int fChanged);                  // OUT: dropcap changed?
         internal delegate int FInterruptFormattingText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int dcp,                            // IN:  current dcp
            int vr,                             // IN:  current v position
            out int fInterruptFormatting);      // OUT: is it time to stop formatting?
         internal delegate int GetTextParaCache(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            uint fswdir,                        // IN:  current direction
            int urStartLine,                    // IN:  position at the beginning of the line
            int durLine,                        // IN:  maximum width of line
            int urStartTrack,                   // IN:  position at the beginning of the track
            int durTrack,                       // IN:  width of track
            int urPageLeftMargin,               // IN:  left margin of the page
            int fClearOnLeft,                   // IN:  is clear on left side
            int fClearOnRight,                  // IN:  is clear on right side
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            out int fFound,                     // OUT: is there cache for this paragrpaph?
            out int dcpPara,                    // OUT: dcp consumed by the paragraph
            out int urBBox,                     // OUT: ur of the para ink
            out int durBBox,                    // OUT: dur of of the para ink
            out int dvrPara,                    // OUT: height of the para
            out FSKCLEAR fskclear,              // OUT: kclear after paragraph
            out IntPtr pmcsclientAfterPara,     // OUT: margin collapsing state after parag.
            out int cLines,                     // OUT: number of lines in the paragraph
            out int fOptimalLines,              // OUT: para had its lines optimized
            out int fOptimalLineDcpsCached,     // OUT: cached dcp's for lines available
            out int dvrMinLineHeight);          // OUT: minimal line height
         internal unsafe delegate int SetTextParaCache(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            uint fswdir,                        // IN:  current direction
            int urStartLine,                    // IN:  position at the beginning of the line
            int durLine,                        // IN:  maximum width of line
            int urStartTrack,                   // IN:  position at the beginning of the track
            int durTrack,                       // IN:  width of track
            int urPageLeftMargin,               // IN:  left margin of the page
            int fClearOnLeft,                   // IN:  is clear on left side
            int fClearOnRight,                  // IN:  is clear on right side
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            int dcpPara,                        // IN:  dcp consumed by the paragraph
            int urBBox,                         // IN:  ur of the para ink
            int durBBox,                        // IN:  dur of of the para ink
            int dvrPara,                        // IN:  height of the para
            FSKCLEAR fskclear,                  // IN:  kclear after paragraph
            IntPtr pmcsclientAfterPara,         // IN:  margin collapsing state after paragraph
            int cLines,                         // IN:  number of lines in the paragraph
            int fOptimalLines,                  // IN:  paragraph has its lines optinmized
            int* rgdcpOptimalLines,             // IN:  array of dcp's of optimal lines
            int dvrMinLineHeight);              // IN:  minimal line height
         internal unsafe delegate int GetOptimalLineDcpCache(
            IntPtr pfsclient,                   // IN:  client opaque data
            int cLines,                         // IN:  number of lines - size of pre-allocated array
            int* rgdcp);                        // OUT: array of dcp's to fill
         internal delegate int GetNumberAttachedObjectsBeforeTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            out int cAttachedObjects);          // OUT: number of attached objects
         internal unsafe delegate int GetAttachedObjectsBeforeTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            int nAttachedObjects,               // IN:  size of the object arrays
            IntPtr* rgnmpObjects,               // OUT: array of object names
            int* rgidobj,                       // OUT: array of idobj's of corresponding objects
            int* rgdcpAnchor,                   // OUT: array of dcp of the objects anchors
            out int cObjects,                   // OUT: actual number of objects
            out int fEndOfParagraph);           // OUT: paragraph ended after last object

         internal delegate int GetNumberAttachedObjectsInTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsline,                     // IN:  pointer to line created by client
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            int dcpLim,                         // IN:  dcp at the end of the range
            int fFoundAttachedObjectsBeforeLine,// IN:  Attached objects before line found
            int dcpMaxAnchorAttachedObjectBeforeLine, // IN: Max dcp of anchor in objects before line
            out int cAttachedObjects);          // OUT: number of attached objects
         internal unsafe delegate int GetAttachedObjectsInTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsline,                     // IN:  pointer to line created by client
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            int dcpLim,                         // IN:  dcp at the end of the range
            int fFoundAttachedObjectsBeforeLine,// IN:  Attached objects before line found
            int dcpMaxAnchorAttachedObjectBeforeLine, // IN: Max dcp of anchor in objects before line
            int nAttachedObjects,               // IN:  size of the floater arrays
            IntPtr* rgnmpObjects,               // OUT: array of floater names
            int* rgidobj,                       // OUT: array of idobj's of corresponding objects
            int* rgdcpAnchor,                   // OUT: array of dcp of the objects anchors
            out int cObjects);                  // OUT: actual number of objects

         internal delegate int UpdGetAttachedObjectChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of text paragraph
            IntPtr nmpAttachedObject,           // IN:  name of object
            out FSKCHANGE fskchObject);         // OUT: kind of change for object

         internal delegate int GetDurFigureAnchor(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr pfsline,                     // IN:  pointer to line created by client
            IntPtr nmpFigure,                   // IN:  figure's name
            uint fswdir,                        // IN:  current direction
            IntPtr pfsFmtLineIn,                // IN:  data needed to reformat the line
            out int dur);                       // OUT: distance from the beginning of the line to the anchor
        // ------------------------------------------------------------------
        // fsfloatercbk.h
        // ------------------------------------------------------------------
         internal delegate int GetFloaterProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmFloater,                   // IN:  name of the floater
            uint fswdirTrack,                   // IN:  direction of Track
            out FSFLOATERPROPS fsfloaterprops); // OUT: properties of the floater
         internal delegate int FormatFloaterContentFinite(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsbrkFloaterContentIn,      // IN:  break record---use if !NULL
            int fBreakRecordFromPreviousPage,   // IN:  break record was created on previous page
            IntPtr nmFloater,                   // IN:  name of floater
            IntPtr pftnrej,                     // IN: 
            int fEmptyOk,                       // IN:  is it OK not to add anything?
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdirTrack,                   // IN:  direction of Track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
										        // IN: suppress breaks at track start?
            out FSFMTR fsfmtr,                  // OUT: result of formatting
            out IntPtr pfsbrkFloatContentOut,   // OUT: opaque for PTS pointer pointer to formatted content
            out IntPtr pbrkrecpara,             // OUT: pointer to the floater content break record
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out FSBBOX fsbbox,                  // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices);                  // OUT: total number of vertices in all polygons
         internal delegate int FormatFloaterContentBottomless(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdirTrack,                   // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            out FSFMTRBL fsfmtrbl,              // OUT: result of formatting
            out IntPtr pfsbrkFloatContentOut,   // OUT: opaque for PTS pointer pointer to formatted content
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out FSBBOX fsbbox,                  // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices);                 // OUT: total number of vertices in all polygons
         internal delegate int UpdateBottomlessFloaterContent(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdirTrack,                   // IN:  direction of Track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            out FSFMTRBL fsfmtrbl,              // OUT: result of formatting
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out FSBBOX fsbbox,                  // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices);                 // OUT: total number of vertices in all polygons
         internal unsafe delegate int GetFloaterPolygons(
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            IntPtr nmFloater,                   // IN:  name of floater
            uint fswdirTrack,                   // IN:  direction of Track
            int ncVertices,                     // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                          // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int cVertices,                  // OUT: actual number of vertex counts
            FSPOINT* rgfspt,                    // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough);              // OUT: fill text in empty areas within obstacles?
         internal delegate int ClearUpdateInfoInFloaterContent(
            IntPtr pfsFloaterContent);          // IN:  opaque for PTS pointer to floater content
         internal delegate int CompareFloaterContents(
            IntPtr pfsFloaterContentOld,        // IN:
            IntPtr pfsFloaterContentNew,        // IN:
            out FSCOMPRESULT fscmpr);           // OUT: result of comparison
         internal delegate int DestroyFloaterContent(
            IntPtr pfsFloaterContent);          // IN:  opaque for PTS pointer to floater content
         internal delegate int DuplicateFloaterContentBreakRecord(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsbrkFloaterContent,        // IN:  pointer to break record
            out IntPtr pfsbrkFloaterContentDup);// OUT pointer to duplicate break record
         internal delegate int DestroyFloaterContentBreakRecord(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsbrkFloaterContent);       // IN:  pointer to break record
         internal delegate int GetFloaterContentColumnBalancingInfo(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            uint fswdir,                        // IN:  current direction
            out int nlines,                     // OUT: number of text lines
            out int dvrSumHeight,               // OUT: sum of all line heights
            out int dvrMinHeight);              // OUT: minimum line height
         internal delegate int GetFloaterContentNumberFootnotes(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            out int cftn);                      // OUT: number of footnotes
         internal delegate int GetFloaterContentFootnoteInfo(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            uint fswdir,                        // IN:  current direction
            int nftn,                           // IN:  size of FSFTNINFO array
            int iftnFirst,                      // IN:  first index in FSFTNINFO array to be used by this para
            ref FSFTNINFO fsftninf,             // IN/OUT: array of footnote info
            out int iftnLim);                   // OUT: lim index used by this paragraph
         internal delegate int TransferDisplayInfoInFloaterContent(
            IntPtr pfsFloaterContentOld,        // IN:
            IntPtr pfsFloaterContentNew);       // IN:
         internal delegate int GetMCSClientAfterFloater(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            uint fswdirTrack,                   // IN:  direction of Track
            IntPtr pmcsclientIn,                // IN:  input opaque to PTS MCSCLIENT
            out IntPtr pmcsclientOut);          // OUT: MCSCLIENT that floater will return to track
         internal delegate int GetDvrUsedForFloater(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            uint fswdirTrack,                   // IN:  direction of Track
            IntPtr pmcsclientIn,                // IN:  input opaque to PTS MCSCLIENT
            int dvrDisplaced,                   // IN: 
            out int dvrUsed);                   // OUT:
        // ------------------------------------------------------------------
        // fsimeth.h
        // ------------------------------------------------------------------
         internal delegate int ObjCreateContext(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsc,                        // IN:  FS context
            IntPtr pfscbkobj,                   // IN:  callbacks (FSCBKOBJ)
            uint ffi,                           // IN:  formatting flags
            int idobj,                          // IN:  id of the object
            out IntPtr pfssobjc);               // OUT: object context
         internal delegate int ObjDestroyContext(
            IntPtr pfssobjc);                   // IN:  object context
         internal delegate int ObjFormatParaFinite(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsobjbrk,                   // IN:  break record---use if !NULL
            int fBreakRecordFromPreviousPage,   // IN:  break record was created on previous page
            IntPtr nmp,                         // IN:  name of paragraph---use if break record is NULL
            int iArea,                          // IN:  column-span area index
            IntPtr pftnrej,                     // IN:
            IntPtr pfsgeom,                     // IN:  pointer to geometry
            int fEmptyOk,                       // IN:  is it OK not to add anything?
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  current direction
            ref FSRECT fsrcToFill,              // IN:  rectangle to fill
            IntPtr pmcsclientIn,                // IN:  input margin collapsing state
            FSKCLEAR fskclearIn,                // IN:  clear property that must be satisfied
            FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
												// IN:  flags to suppress breaks
            int fBreakInside,                   // IN:  produce vertical break inside para; needed for recursive KWN logic;
                                                //      can be set to true if during previous formatting fBreakInsidePossible output was returned as TRUE
            out FSFMTR fsfmtr,                  // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out IntPtr pbrkrecpara,             // OUT: pointer to the para break record
            out int dvrUsed,                    // OUT: vertical space used by the para
            out FSBBOX fsbbox,                  // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out FSKCLEAR fskclearOut,           // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fBreakInsidePossible);      // OUT: internal vertical break possible, needed for recursive KWN logic
         internal delegate int ObjFormatParaBottomless(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            IntPtr pfsgeom,                     // IN:  pointer to geometry
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  current direction
            int urTrack,                        // IN:  ur of bottomless rectangle to fill
            int durTrack,                       // IN:  dur of bottomless rectangle to fill
            int vrTrack,                        // IN:  vr of bottomless rectangle to fill
            IntPtr pmcsclientIn,                // IN:  input margin collapsing state
            FSKCLEAR fskclearIn,                // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out FSFMTRBL fsfmtrbl,              // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out int dvrUsed,                    // OUT: vertical space used by the para
            out FSBBOX fsbbox,                  // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out FSKCLEAR fskclearOut,           // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable);// OUT: interruption is prohibited from now on
         internal delegate int ObjUpdateBottomlessPara(
            IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            IntPtr pfsgeom,                     // IN:  pointer to geometry
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  current direction
            int urTrack,                        // IN:  u of bootomless rectangle to fill
            int durTrack,                       // IN:  du of bootomless rectangle to fill
            int vrTrack,                        // IN:  v of bootomless rectangle to fill
            IntPtr pmcsclientIn,                // IN:  input margin collapsing state
            FSKCLEAR fskclearIn,                // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out FSFMTRBL fsfmtrbl,              // OUT: result of formatting the paragraph
            out int dvrUsed,                    // OUT: vertical space used by the para
            out FSBBOX fsbbox,                  // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out FSKCLEAR fskclearOut,           // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable);// OUT: interruption is prohibited from now on
         internal delegate int ObjSynchronizeBottomlessPara(
            IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsgeom,                     // IN: pointer to geometry
            uint fswdir,                        // IN: direction
            int dvrShift);                      // IN: shift by this value
         internal delegate int ObjComparePara(
            IntPtr pfsparaclientOld,            // IN:
            IntPtr pfsparaOld,                  // IN:  pointer to the old para data
            IntPtr pfsparaclientNew,            // IN:
            IntPtr pfsparaNew,                  // IN:  pointer to the new para data
            uint fswdir,                        // IN:  track's direction
            out FSCOMPRESULT fscmpr,            // OUT: comparison result
            out int dvrShifted);                // OUT: amount of shift if result is fscomprShifted
         internal delegate int ObjClearUpdateInfoInPara(
            IntPtr pfspara);                    // IN:  pointer to the para data
         internal delegate int ObjDestroyPara(
            IntPtr pfspara);                    // IN:  pointer to the para data
         internal delegate int ObjDuplicateBreakRecord(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsbrkrecparaOrig,           // IN:  pointer to the para break record
            out IntPtr pfsbrkrecparaDup);       // OUT: pointer to the duplicate break record
         internal delegate int ObjDestroyBreakRecord(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsobjbrk);                  // OUT: pointer to the para break record
         internal delegate int ObjGetColumnBalancingInfo(
            IntPtr pfspara,                     // IN:  pointer to the para data
            uint fswdir,                        // IN:  current direction
            out int nlines,                     // OUT: number of text lines
            out int dvrSumHeight,               // OUT: sum of all line heights
            out int dvrMinHeight);              // OUT: minimum line height
         internal delegate int ObjGetNumberFootnotes(
            IntPtr pfspara,                     // IN:  pointer to the para data
            out int nftn);                      // OUT: number of footnotes
         internal unsafe delegate int ObjGetFootnoteInfo(
            IntPtr pfspara,                     // IN:  pointer to the para data
            uint fswdir,                        // IN:  current direction
            int nftn,                           // IN:  size of FSFTNINFO array
            int iftnFirst,                      // IN:  first index in FSFTNINFO array to be used by this para
            FSFTNINFO* pfsftninf,               // IN/OUT: array of footnote info
            out int iftnLim);                   // OUT: lim index used by this paragraph
        //internal unsafe delegate int ObjGetFootnoteInfoWord(
        //    IntPtr pfspara,                     // IN:  pointer to the para data
        //    uint fswdir,                        // IN:  current direction
        //    int nftn,                           // IN:  size of FSFTNINFO array
        //    int iftnFirst,                      // IN:  first index in FSFTNINFO array to be used by this para
        //    FSFTNINFOWORD* pfsftninf,           // IN/OUT: array of footnote info for word
        //    out int iftnLim);                   // OUT: lim index used by this paragraph
         internal delegate int ObjShiftVertical(
            IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsshift,                    // IN:  pointer to the shift data
            uint fswdir,                        // IN:  wdir for bbox - the same as the one passed to formatting method
            out FSBBOX fsbbox);                 // OUT: output BBox
         internal delegate int ObjTransferDisplayInfoPara(
            IntPtr pfsparaOld,                  // IN:  pointer to the old para data
            IntPtr pfsparaNew);                 // IN:  pointer to the new para data
        // ------------------------------------------------------------------
        // fstableobjhandlerds.h
        // ------------------------------------------------------------------
         internal delegate int GetTableProperties(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            out FSTABLEOBJPROPS fstableobjprops);   // OUT: 
         internal delegate int AutofitTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            int durAvailableSpace,                  // IN:  
            out int durTableWidth);                 // OUT: Table width after autofit. It is the same for all rows :)
         internal delegate int UpdAutofitTable(      // calculate widths of table
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            int durAvailableSpace,                  // IN:  
            out int durTableWidth,                  // OUT: Table width after autofit.
            // Should we store the old one? It is possible for the
            // table width to change with pfNoChangeInCellWidths = .T. ?
            out int fNoChangeInCellWidths);         // OUT: 
         internal delegate int GetMCSClientAfterTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            IntPtr pmcsclientIn,                    // IN:  
            out IntPtr ppmcsclientOut);             // OUT:
        /*        
        internal delegate int GetDvrUsedForFloatTable(
            IntPtr pfsclient,
            IntPtr pfsparaclientTable,
            IntPtr nmTable,
            uint fswdirTrack,
            IntPtr pmcsclientIn,
            int dvrDisplaced,
            out int pdvrUsed);
    */
        // ------------------------------------------------------------------
        // fstablecbk.h
        // ------------------------------------------------------------------
         internal delegate int GetFirstHeaderRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            int fRepeatedHeader,                    // IN:  
                            out int fFound,                         // OUT: 
                            out IntPtr pnmFirstHeaderRow);          // OUT: 
                    internal delegate int GetNextHeaderRow(
                            IntPtr pfsclient,                       // IN:  
                            IntPtr nmTable,                         // IN:  
                            IntPtr nmHeaderRow,                     // IN:  
                            int fRepeatedHeader,                    // IN:  
                            out int fFound,                         // OUT: 
                            out IntPtr pnmNextHeaderRow);           // OUT: 
         internal delegate int GetFirstFooterRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            int fRepeatedFooter,                    // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmFirstFooterRow);          // OUT: 
         internal delegate int GetNextFooterRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmFooterRow,                     // IN:  
            int fRepeatedFooter,                    // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmNextFooterRow);           // OUT: 
         internal delegate int GetFirstRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmFirstRow);                // OUT: 
         internal delegate int GetNextRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmRow,                           // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmNextRow);                 // OUT: 
         internal delegate int UpdFChangeInHeaderFooter( // we don't do update in header/footer
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            out int fHeaderChanged,                 // OUT: 
            out int fFooterChanged,                 // OUT: 
            out int fRepeatedHeaderChanged,         // OUT: unneeded for bottomless page, but...
            out int fRepeatedFooterChanged);        // OUT: unneeded for bottomless page, but...
         internal delegate int UpdGetFirstChangeInTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            out int fFound,                         // OUT: 
            out int fChangeFirst,                   // OUT: 
            out IntPtr pnmRowBeforeChange);         // OUT: 
         internal delegate int UpdGetRowChange(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmRow,                           // IN:  
            out FSKCHANGE fskch,                    // OUT: 
            out int fNoFurtherChanges);             // OUT: 
         internal delegate int UpdGetCellChange(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmRow,                           // IN:  
            IntPtr nmCell,                          // IN:  
            out int fWidthChanged,                  // OUT: 
            out FSKCHANGE fskchCell);               // OUT: 
         internal delegate int GetDistributionKind(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTable,                       // IN:  
            out FSKTABLEHEIGHTDISTRIBUTION tabledistr); // OUT: 
         internal delegate int GetRowProperties(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmRow,                           // IN:  
            uint fswdirTable,                       // IN:  
            out FSTABLEROWPROPS rowprops);          // OUT: 
         internal unsafe delegate int GetCells(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmRow,                           // IN:  
            int cCells,                             // IN:  
            IntPtr* rgnmCell,                       // IN/OUT: 
            FSTABLEKCELLMERGE* rgkcellmerge);       // IN/OUT: 
         internal delegate int FInterruptFormattingTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclient,                   // IN:  
            IntPtr nmRow,                           // IN:  
            int dvr,                                // IN:  
            out int fInterrupt);                    // OUT: 
         internal unsafe delegate int CalcHorizontalBBoxOfRow(
            IntPtr pfsclient,                       // IN:
            IntPtr nmRow,                           // IN:
            int cCells,                             // IN:
            IntPtr* rgnmCell,                       // IN:
            IntPtr* rgpfscell,                      // IN:
            out int urBBox,                         // OUT:
            out int durBBox);                       // OUT:
         internal delegate int FormatCellFinite(     // unless cell has vertical text or is special in some other ways,
            // this calls maps directly to a create subpage call :-)
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr pfsbrkcell,                      // IN:  not NULL if cell broken from previous page/column
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            IntPtr pfsFtnRejector,                  // IN:  
            int fEmptyOK,                           // IN:  
            uint fswdirTable,                       // IN:  
            int dvrExtraHeight,                     // IN: height above current row (non-zero for vMerged cells)
            int dvrAvailable,                       // IN:  
            out FSFMTR pfmtr,                       // OUT: 
            out IntPtr ppfscell,                    // OUT: cell object
            out IntPtr pfsbrkcellOut,               // OUT: break if cell does not fit in dvrAvailable
            out int dvrUsed);                       // OUT: height -- min height required 
         internal delegate int FormatCellBottomless(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            uint fswdirTable,                       // IN:  
            out FSFMTRBL fmtrbl,                    // OUT: 
            out IntPtr ppfscell,                    // OUT: cell object
            out int dvrUsed);                       // OUT: height -- min height required 
         internal delegate int UpdateBottomlessCell( // unless cell has vertical text or is special in some other ways,
            // this calls maps directly to a update subpage call :-)
            IntPtr pfscell,                         // IN/OUT: cell object
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            uint fswdirTable,                       // IN:  
            out FSFMTRBL fmtrbl,                    // IN:  
            out int dvrUsed);                       // OUT: height -- min height required 

         internal delegate int CompareCells(
            IntPtr pfscellOld,
            IntPtr pfscellNew,
            out FSCOMPRESULT pfscmpr);

         internal delegate int ClearUpdateInfoInCell(
            IntPtr pfscell);                        // IN/OUT: cell object
         internal delegate int SetCellHeight(
            IntPtr pfscell,                         // IN/OUT: cell object
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr pfsbrkcell,                      // IN:  not NULL if cell broken from previous page/column
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            int fBrokenHere,                        // IN:  true if cell broken on this page/column: no reformatting
            uint fswdirTable,                       // IN:  
            int dvrActual);                         // IN:  
         internal delegate int DestroyCell(
            IntPtr pfsCell);                        // IN:  
         internal delegate int DuplicateCellBreakRecord(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsbrkcell,                      // IN:  
            out IntPtr ppfsbrkcellDup);             // OUT: 
         internal delegate int DestroyCellBreakRecord(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsbrkcell);                     // IN:  
         internal delegate int GetCellNumberFootnotes(
            IntPtr pfscell,                         // IN:  
            out int cFtn);                          // OUT: 
        /*
    internal delegate int GetCellFootnoteInfo(
        PFSTABLECELL pfscell,
        LONG cFtn,
        LONG iFtnFirst,
        PFSFTNINFO pfsftninf,
        LONG* piFtnLim);
    */
         internal delegate int GetCellMinColumnBalancingStep(
            IntPtr pfscell,                         // IN:
            uint fswdir,                            // IN:
            out int pdvrMinStep);                   // OUT:

         internal delegate int TransferDisplayInfoCell(
            IntPtr pfscellOld,
            IntPtr pfscellNew);

        #endregion Callbacks

        // ------------------------------------------------------------------
        //
        // Exported functions.
        //
        // ------------------------------------------------------------------

        #region Exported functions


        // ------------------------------------------------------------------
        // Object handlers
        // ------------------------------------------------------------------

        [DllImport(DllImport.PresentationNative)]
        internal static extern int GetFloaterHandlerInfo(
            [In] 
            ref FSFLOATERINIT pfsfloaterinit,   // IN:  pointer to floater init data (callbacks)
            IntPtr pFloaterObjectInfo);         // IN:  pointer to floater object info

        [DllImport(DllImport.PresentationNative)]
        internal static extern int GetTableObjHandlerInfo(
            [In] 
            ref FSTABLEOBJINIT pfstableobjinit, // IN:  pointer to floater init data (callbacks)
            IntPtr pTableObjectInfo);           // IN:  pointer to floater object info

        [DllImport(DllImport.PresentationNative)]
        internal static extern int CreateInstalledObjectsInfo(
            [In] 
            ref FSIMETHODS fssubtrackparamethods,//IN:  pointer to subtrack paragraph callbacks
            ref FSIMETHODS fssubpageparamethods,// IN:  pointer to subpage paragraph callbacks
            out IntPtr pInstalledObjects,       // OUT: pointer to installed objects array
            out int cInstalledObjects);         // OUT: size of installed objects array

        [DllImport(DllImport.PresentationNative)]
        internal static extern int DestroyInstalledObjectsInfo(
            IntPtr pInstalledObjects);          // IN:  pointer to installed objects array
        // ------------------------------------------------------------------
        // fscrcontxt.h
        // ------------------------------------------------------------------
        [DllImport(DllImport.PresentationNative)]
        internal static extern int CreateDocContext(
            [In] 
            ref FSCONTEXTINFO fscontextinfo,    // IN:  pointer to context information
            out IntPtr pfscontext);             // OUT: pointer to the FS context

        [DllImport(DllImport.PresentationNative)]
        internal static extern int DestroyDocContext(
            IntPtr pfscontext);                 // IN:  pointer to the FS context
#if NEVER
        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsSetDebugFlags(
            IntPtr pfscontext,                  // IN:  pointer to the FS context
            uint dwFlags);                      // IN:  debug flags (see fsdebugflags.h)
        //      Note that old debug flags will be overwritten.
#endif
        // ------------------------------------------------------------------
        // fscrpage.h
        // ------------------------------------------------------------------
        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsCreatePageFinite(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsBRPageStart,              // IN:  ptr to brk record of prev. page
            IntPtr fsnmSectStart,               // IN:  name of the section to start from, if pointer to break rec is NULL
            out FSFMTR pfsfmtrOut,              // OUT: formatting result
            out IntPtr ppfsPageOut,             // OUT: ptr to page, opaque to client
            out IntPtr ppfsBRPageOut);          // OUT: break record of the page

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsUpdateFinitePage(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfspage,                     // IN:  ptr to page to update
            IntPtr pfsBRPageStart,              // IN:  ptr to brk record of prev. page
            IntPtr fsnmSectStart,               // IN:  name of the section to start from, if pointer to break rec is NULL
            out FSFMTR pfsfmtrOut,              // OUT: formatting result
            out IntPtr ppfsBRPageOut);          // OUT: break record of the page

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsCreatePageBottomless(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr fsnmsect,                    // IN:  name of the section to start from
            out FSFMTRBL pfsfmtrbl,             // OUT: formatting result
            out IntPtr ppfspage);               // OUT: ptr to page, opaque to client

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsUpdateBottomlessPage(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfspage,                     // IN:  ptr to page to update
            IntPtr fsnmsect,                    // IN:  name of the section to start from
            out FSFMTRBL pfsfmtrbl);            // OUT: formatting result

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsClearUpdateInfoInPage(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfspage);                    // IN:  ptr to page to clear

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDestroyPage(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfspage);                    // IN:  ptr to page

#if NEVER
[DllImport(DllImport.PresentationNative)]//CASRemoval:
internal static extern int FsDuplicatePageBreakRecord(
    IntPtr pfscontext,              // IN:  ptr to FS context
    IntPtr pfsbreakrecpage,         // IN:  ptr to page break record
    out IntPtr ppfsbreakrecpage);   // OUT: ptr to duplicate break record
#endif

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDestroyPageBreakRecord(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsbreakrec);                // IN:  ptr to page break record

        // ------------------------------------------------------------------
        // fscrsubp.h
        // ------------------------------------------------------------------

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsCreateSubpageFinite(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pBRSubPageStart,             // IN: ptr to brk record of subpage
            int fFromPreviousPage,              // IN:  break record was created on previous page
            IntPtr nSeg,                        // IN:  name of the segment to start from-if pointer to break rec is NULL
            IntPtr pFtnRej,                     // IN:  pftnrej
            int fEmptyOk,                       // IN:  fEmptyOK
            int fSuppressTopSpace,              // IN:  fSuppressTopSpace
            uint fswdir,                        // IN:  fswdir
            int lWidth,                         // IN:  width of subpage
            int lHeight,                        // IN:  height of subpage
            ref FSRECT rcMargin,                // IN:  rectangle within subpage margins
            int cColumns,                       // IN:  number of columns
            FSCOLUMNINFO* rgColumnInfo,         // IN:  array of column info
            int fApplyColumnBalancing,          // IN:  apply column balancing?
            int cSegmentAreas,                  // IN:  number of segment-defined colspan areas
            IntPtr* rgnSegmentForArea,          // IN:  array of segment names for colspan areas
            int* rgSpanForSegmentArea,          // IN:  array of columns spanned for segment-defined areas
            int cHeightAreas,                   // IN:  number of height-defined colspan areas
            int* rgHeightForArea,               // IN:  array of heights for height-defined colspan areas
            int* rgSpanForHeightArea,           // IN:  array of columns spanned for height-defined areas
            int fAllowOverhangBottom,           // IN:  allow overhang over bottom margin?
            FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
																/* IN: suppress breaks at track start?			*/
            out FSFMTR fsfmtr,                  // OUT: why formatting was stopped
            out IntPtr pSubPage,                // OUT: ptr to the subpage
            out IntPtr pBRSubPageOut,           // OUT: break record of the subpage
            out int dvrUsed,                    // OUT: dvrUsed
            out FSBBOX fsBBox,                  // OUT: subpage bbox
            out IntPtr pfsMcsClient,            // OUT: margin collapsing state at the bottom
            out int topSpace);                  // OUT: top space due to collapsed margins

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsCreateSubpageBottomless(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr nSeg,                        // IN:  name of the segment to start from
            int fSuppressTopSpace,              // IN:  suppress top space?
            uint fswdir,                        // IN:  fswdir
            int lWidth,                         // IN:  width of subpage
            int urMargin,                       // IN:  ur of margin
            int durMargin,                      // IN:  dur of margin
            int vrMargin,                       // IN:  vr of margin
            int cColumns,                       // IN:  number of columns
            FSCOLUMNINFO* rgColumnInfo,         // IN:  array of column info
            int cSegmentAreas,                  // IN:  number of segment-defined colspan areas
            IntPtr* rgnSegmentForArea,          // IN:  array of segment names for colspan areas
            int* rgSpanForSegmentArea,          // IN:  array of columns spanned for segment-defined areas
            int cHeightAreas,                   // IN:  number of height-defined colspan areas
            int* rgHeightForArea,               // IN:  array of heights for height-defined colspan areas
            int* rgSpanForHeightArea,           // IN:  array of columns spanned for height-defined areas
            int fINterrruptible,                // IN:  can be interrupted?
            out FSFMTRBL pfsfmtr,               // OUT: why formatting was stopped
            out IntPtr ppSubPage,               // OUT: ptr to the subpage
            out int pdvrUsed,                   // OUT: dvrUsed
            out FSBBOX pfsBBox,                 // OUT: subpage bbox
            out IntPtr pfsMcsClient,            // OUT: margin collapsing state at the bottom
            out int pTopSpace,                  // OUT: top space due to collapsed margins
            out int fPageBecomesUninterruptible);// OUT: interruption is prohibited from now on

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsUpdateBottomlessSubpage(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubpage,                  // IN:  ptr to the subpage
            IntPtr nmSeg,                       // IN:  name of the segment to start from
            int fSuppressTopSpace,              // IN:  suppress top space?
            uint fswdir,                        // IN:  fswdir
            int lWidth,                         // IN:  width of subpage
            int urMargin,                       // IN:  ur of margin
            int durMargin,                      // IN:  dur of margin
            int vrMargin,                       // IN:  vr of margin
            int cColumns,                       // IN:  number of columns
            FSCOLUMNINFO* rgColumnInfo,         // IN:  array of column info
            int cSegmentAreas,                  // IN:  number of segment-defined colspan areas
            IntPtr* rgnSegmentForArea,          // IN:  array of segment names for colspan areas
            int* rgSpanForSegmentArea,          // IN:  array of columns spanned for segment-defined areas
            int cHeightAreas,                   // IN:  number of height-defined colspan areas
            int* rgHeightForArea,               // IN:  array of heights for height-defined colspan areas
            int* rgSpanForHeightArea,           // IN:  array of columns spanned for height-defined areas
            int fINterrruptible,                // IN:  can be interrupted?
            out FSFMTRBL pfsfmtr,               // OUT: why formatting was stopped
            out int pdvrUsed,                   // OUT: dvrUsed
            out FSBBOX pfsBBox,                 // OUT: subpage bbox
            out IntPtr pfsMcsClient,            // OUT: margin collapsing state at the bottom
            out int pTopSpace,                  // OUT: top space due to collapsed margins
            out int fPageBecomesUninterruptible);// OUT: interruption is prohibited from now on

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsCompareSubpages(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubpageOld,               // IN:  ptr to the old subpage
            IntPtr pfsSubpageNew,               // IN:  ptr to the new subpage
            out FSCOMPRESULT fsCompResult);     // OUT: comparison result

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsClearUpdateInfoInSubpage(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pSubpage);                   // IN:  ptr to subpage

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDestroySubpage(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubpage);                   // IN:  ptr to subpage

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDuplicateSubpageBreakRecord(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pBreakRecSubPageIn,          // IN:  ptr to subpage break record
            out IntPtr ppBreakRecSubPageOut);   // OUT: ptr to duplicate break record

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDestroySubpageBreakRecord(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsbreakrec);                // IN:  ptr to subpage break record

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsGetSubpageColumnBalancingInfo(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubpage,                    // IN:  ptr to the subpage
            out uint fswdir,                    // OUT: writing direction for results
            out int lLineNumber,                // OUT: number of text lines
            out int lLineHeights,               // OUT: sum of all line heights
            out int lMinimumLineHeight);        // OUT: minimum line height

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsGetNumberSubpageFootnotes(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubpage,                    // IN:  ptr to the subpage
            out int cFootnotes);                // OUT: number of footnotes

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsGetSubpageFootnoteInfo(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubpage,                    // IN:  ptr to the subpage
            int cArraySize,                     // IN:  size of FSFTNINFO array
            int indexStart,                     // IN:  first index in FSFTNINFO array to be used by this subpage
            out uint fswdir,                    // OUT: writing direction for results
            PTS.FSFTNINFO* rgFootnoteInfo,      // IN/OUT: array of footnote info
            out int indexLim);                  // OUT: lim index used by this subpage

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsTransferDisplayInfoSubpage(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubpageOld,                 // IN:  ptr to the old subpage
            IntPtr pfsSubpageNew);              // IN:  ptr to the new subpage

        // ------------------------------------------------------------------
        // fscrsubtrack.h
        // ------------------------------------------------------------------
        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsFormatSubtrackFinite(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsBRSubtackIn,              // IN:  ptr to brk record of subtrack
            int fFromPreviousPage,              // IN:  break record was created on previous page
            IntPtr fsnmSegment,                 // IN:  name of the segment to start from - if pointer to break rec is NULL
            int iArea,                          // IN:  column-span area index
            IntPtr pfsFtnRej,                   // IN:
            IntPtr pfsGeom,                     // IN:  geometry
            int fEmptyOk,                       // IN:  fEmptyOK
            int fSuppressTopSpace,              // IN:  fSuppressTopSpace
            uint fswdir,                        // IN:  direction
            [In] ref FSRECT fsRectToFill,       // IN:  rectangle to fill
            IntPtr pfsMcsClientIn,              // IN:  input margin collapsing state
            FSKCLEAR fsKClearIn,                // IN:  clear property that must be satisfied
            FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstpara,
                                                // IN: suppress breaks at track start?	
            out FSFMTR pfsfmtr,                 // OUT: why formatting was stopped
            out IntPtr ppfsSubtrack,            // OUT: ptr to the subtrack
            out IntPtr pfsBRSubtrackOut,        // OUT: break record of the subtrack
            out int pdvrUsed,                   // OUT: dvrUsed
            out FSBBOX pfsBBox,                 // OUT: subtrack bbox
            out IntPtr ppfsMcsClientOut,        // OUT: margin collapsing state at the bottom
            out FSKCLEAR pfsKClearOut,          // OUT: ClearIn for the next paragraph
            out int pTopSpace);                 // OUT: top space due to collapsed margin

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsFormatSubtrackBottomless(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr fsnmSegment,                 // IN:  name of the segment to start from
            int iArea,                          // IN:  column-span area index
            IntPtr pfsGeom,                     // IN:  parent geometry
            int fSuppressTopSpace,              // IN:
            uint fswdir,                        // IN:  direction
            int ur,                             // IN:  ur of subtrack
            int dur,                            // IN:  dur of subtrack
            int vr,                             // IN:  vr of subtrack
            IntPtr pfsMcsClientIn,              // IN:  input margin collapsing state
            FSKCLEAR fsKClearIn,                // IN:  clear property that must be satisfied
            int fCanBeInterruptedIn,            // IN:  can be interrupted?
            out FSFMTRBL pfsfmtrbl,             // OUT: why formatting was stopped
            out IntPtr ppfsSubtrack,            // OUT: ptr to subtrack
            out int pdvrUsed,                   // OUT: dvrUsed
            out FSBBOX pfsBBox,                 // OUT: subtrack bbox
            out IntPtr ppfsMcsClientOut,        // OUT: margin collapsing state at the bottom
            out FSKCLEAR pfsKClearOut,          // OUT: ClearIn for the next paragraph
            out int pTopSpace,                  // OUT: top space due to collapsed margin
            out int pfCanBeInterruptedOut);     // OUT: interruption is prohibited from now on

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsUpdateBottomlessSubtrack(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrack,                 // IN:  ptr to subtrack
            IntPtr fsnmSegment,                 // IN:  name of the segment to start from
            int iArea,                          // IN:  column-span area index
            IntPtr pfsGeom,                     // IN:  parent geometry
            int fSuppressTopSpace,              // IN:
            uint fswdir,                        // IN:  direction
            int ur,                             // IN:  ur of subtrack
            int dur,                            // IN:  dur of subtrack
            int vr,                             // IN:  vr of subtrack
            IntPtr pfsMcsClientIn,              // IN:  input margin collapsing state
            FSKCLEAR fsKClearIn,                // IN:  clear property that must be satisfied
            int fCanBeInterruptedIn,            // IN:  can be interrupted?
            out FSFMTRBL pfsfmtrbl,             // OUT: why formatting was stopped
            out int pdvrUsed,                   // OUT: dvrUsed
            out FSBBOX pfsBBox,                 // OUT: subtrack bbox
            out IntPtr ppfsMcsClientOut,        // OUT: margin collapsing state at the bottom
            out FSKCLEAR pfsKClearOut,          // OUT: ClearIn for the next paragraph
            out int pTopSpace,                  // OUT: top space due to collapsed margin
            out int pfCanBeInterruptedOut);     // OUT: interruption is prohibited from now on

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsSynchronizeBottomlessSubtrack(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrack,                 // IN:  ptr to subtrack
            IntPtr pfsGeom,                     // IN:  geometry
            uint fswdir,                        // IN:  direction
            int vrShift);                       // IN:  shift by this value

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsCompareSubtrack(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrackOld,              // IN:  ptr to old subtrack
            IntPtr pfsSubtrackNew,              // IN:  ptr to new subtrack
            uint fswdir,                        // IN:  fswdir
            out FSCOMPRESULT fsCompResult,      // OUT: comparison result
            out int dvrShifted);                // OUT: dvrShifted if result is fscmprShifted

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsClearUpdateInfoInSubtrack(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrack);                // IN:  ptr to subtrack

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDestroySubtrack(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrack);                // IN:  ptr to subtrack

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDuplicateSubtrackBreakRecord(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsBRSubtrackIn,             // IN:  ptr to brk record of subtrack
            out IntPtr ppfsBRSubtrackOut);      // OUT: ptr to duplicate break record

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsDestroySubtrackBreakRecord(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsbreakrec);                // IN:  ptr to subtrack break record

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsGetSubtrackColumnBalancingInfo(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrack,                 // IN:  ptr to subtrack
            uint fswdir,                        // IN:  fswdir
            out int lLineNumber,                // OUT: number of text lines
            out int lLineHeights,               // OUT: sum of all line heights
            out int lMinimumLineHeight);        // OUT: minimum line height

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsGetNumberSubtrackFootnotes(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrack,                 // IN:  ptr to subtrack
            out int cFootnotes);                // OUT: number of footnotes

#if NEVER
FSERR FSAPI FsGetSubtrackFootnoteInfo(
                    PFSCONTEXT,         /* IN: ptr to FS context                        */
                    PFSSUBTRACK,        /* IN: ptr to subtrack                          */
                    LONG,               /* IN: size of FSFTNINFO array                  */
                    LONG,               /* IN: first index in FSFTNINFO array 
                                                to be used by this subtrack             */
                    FSWDIR*,            /* OUT: fswdir                                  */
                    PFSFTNINFO,         /* IN/OUT: array of footnote info               */
                    LONG*);             /* OUT: lim index used by this subtrack         */
// if this API is called with a subtrack that was 
// returned from FsFormatSubTrackBottomless, it will return an error


FSERR FSAPI FsShiftSubtrackVertical(
                    PFSCONTEXT,         /* IN: ptr to FS context                        */
                    PFSSUBTRACK,        /* IN: ptr to subtrack                          */
                    PCFSSHIFT,          /* IN: shift handle                             */
                    FSWDIR,             /* IN: fswdir for bbox                          */
                    FSBBOX*);           /* OUT: subtrack bbox                           */

// if this API is called with a subtrack that was 
// returned from FsFormatSubTrackBottomless, it will return an error

#endif

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsTransferDisplayInfoSubtrack(
            IntPtr pfscontext,                  // IN:  ptr to FS context
            IntPtr pfsSubtrackOld,              // IN:  ptr to old subtrack
            IntPtr pfsSubtrackNew);             // IN: ptr to new subtrack
        // ------------------------------------------------------------------
        // fsfloater.h
        // ------------------------------------------------------------------
        // FSERR FSAPI FsGetFloaterFsimethods
        // ------------------------------------------------------------------
        // fsfloaterquery.h
        // ------------------------------------------------------------------
        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsQueryFloaterDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pfsfloater,                  // IN:
            out FSFLOATERDETAILS fsfloaterdetails); // OUT:
        // ------------------------------------------------------------------
        // fsftnrej.h
        // ------------------------------------------------------------------
        // FsCreateDummyFootnoteRejector
        // FsDestroyFootnoteRejector
        // FsFAllFootnotesAllowed
        // FsFFootnoteAllowed
        // ------------------------------------------------------------------
        // fsgeom.h
        // ------------------------------------------------------------------

#if NEVER
FSERR FSAPI FsDuplicateGeometry(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PCFSGEOM,       /* IN: pointer to input geometry        */
                        PFSGEOM*);      /* OUT: pointer to duplicate geometry   */

FSERR FSAPI FsRestoreGeometry(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PCFSGEOM,       /* IN: pointer to input geometry        */
                        PFSGEOM);       /* IN: pointer to restored geometry     */

FSERR FSAPI FsReleaseGeometry(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PFSGEOM);       /* IN: pointer to geometry              */
[DllImport(DllImport.PresentationNative)]//CASRemoval:
internal static extern int FsRegisterFloatObstacle(
    IntPtr pfsContext,                  // IN:  ptr to FS context
    IntPtr pfsGeom,                     // IN:  pointer to geometry
    uint fswdir,                        // IN:  current direction
    [In]
    ref FSFLTOBSTINFO pfsFloaterObstInfo);// IN:  float obstacle info

[DllImport(DllImport.PresentationNative)]//CASRemoval:
internal static extern int FsGetMaxNumberEmptySpaces(
    IntPtr pfsContext,                  // IN:  ptr to FS context
    IntPtr pGeom,                       // IN:  pointer to geometry
    uint fswdir,                        // IN:  current direction
    out int pMaxNumberEmptySpaces);     // OUT: maximum number of possible EmptySpace elements

[DllImport(DllImport.PresentationNative)]//CASRemoval:
internal static unsafe extern int FsGetEmptySpaces(
    IntPtr pfsContext,                  // IN:  ptr to FS context
    IntPtr pGeom,                       // IN:  pointer to geometry
    uint fswdir,                        // IN:  current direction
    [In]
    ref FSRECT pfsRectToFill,           // IN:  rectangle to fill
    int dvrMinHeight,                   // IN:  required height (dvrOverlap)
    int fIgnoreTightWrap,               // IN:  ignore tight wrap information?
    int lArraySize,                     // IN:  size of EmptySpace array
    out int pfFound,                    // OUT: found?
    out int pdvrHardBottom,             // OUT: dvrHardBottom
    out int pdvrHardBottomForOverhang,  // OUT: dvrHardBottomForOverhang
    out int pdvrSoftBottom,             // OUT: dvrSoftBottom
    FSEMPTYSPACE* rgEmptySpace,         // OUT: EmptySpace array
    out int plActualArraySize,          // OUT: actual number of EmptySpace
    out int pfClearLeft,                // OUT: clear on left side?
    out int pfClearRight,               // OUT: clear on right side?
    out int pfSuppressAutoclear);       // OUT: suppress autoclear?

[DllImport(DllImport.PresentationNative)]//CASRemoval:
internal static extern int FsGetNextTick(
    IntPtr pfsContext,                  // IN:  ptr to FS context
    IntPtr pGeom,                       // IN:  pointer to geometry
    uint fswdir,                        // IN:  current direction
    int vrCurrent,                      // IN:  current vr
    out int pfFound,                    // OUT: found?
    out int pvrNext);                   // OUT: next tick - vr of top or bottom of registered obstacle
FSERR FSAPI FsCommitFilledRectangle(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PFSGEOM,        /* IN: pointer to geometry              */
                        FSWDIR,         /* IN: current direction                */
                        PCFSFILLINFO);  /* IN: filled-space info                */

FSERR FSAPI FsGetPageRectangle(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PCFSGEOM,       /* IN: pointer to geometry              */
                        FSWDIR,         /* IN: current direction                */
                        FSRECT*,        /* OUT: page rectangle                  */
                        FSRECT*);       /* OUT: rectangle within page margins   */


FSERR FSAPI FsGetColumnRectangle(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PCFSGEOM,       /* IN: pointer to geometry              */
                        FSWDIR,         /* IN: current direction                */
                        FSRECT*);       /* OUT: column rectangle                */

FSERR FSAPI FsGetMaxNumberIntervals(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PCFSGEOM,       /* IN: pointer to geometry              */
                        FSWDIR,         /* IN: current direction                */
                        LONG*);         /* OUT: maximum number of 
                                                    possible intervals          */
FSERR FSAPI FsGetIntervals(
                        PFSCONTEXT,     /* IN: ptr to FS context                */
                        PCFSGEOM,       /* IN: pointer to geometry              */
                        FSWDIR,         /* IN: current direction                */
                        PCFSRECT,       /* IN: rect for the interval search     */
                        LONG,           /* IN: size of the interval array       */
                        PFSINTERVAL,    /* OUT: interval array                  */
                        LONG*);         /* OUT: actual number of intervals      */
#endif

        // ------------------------------------------------------------------
        // fsquery.h
        // ------------------------------------------------------------------
        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsQueryPageDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPage,                       // IN:  ptr to page
            out FSPAGEDETAILS pPageDetails);    // OUT: page details

#if NEVER
FSERR FSAPI FsQueryPageFootnoteColumnList(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSPAGE,                   /* IN: ptr to page                              */
            LONG,                       /* IN: size of array of footnote columns        */
            PFSFOOTNOTECOLUMNDESCRIPTION,/* OUT: array of footnote columns descriptions */
            LONG*);                     /* OUT: actual number of footnote columns       */
            
FSERR FSAPI FsQueryFootnoteColumnDetails(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSFOOTNOTECOLUMN,         /* IN: ptr to footnote column                   */
            FSFOOTNOTECOLUMNDETAILS*);  /* OUT: footnote column details                 */

FSERR FSAPI FsQueryFootnoteColumnTrackList(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSFOOTNOTECOLUMN,         /* IN: ptr to footnote column                   */
            LONG,                       /* IN: size of array of track descriptions      */
            PFSTRACKDESCRIPTION,        /* OUT: array of track descriptions             */
            LONG*);                     /* OUT: actual number of tracks                 */
#endif

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQueryPageSectionList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPage,                       // IN:  ptr to page
            int cArraySize,                     // IN:  size of array of section descriptions
            FSSECTIONDESCRIPTION* rgSectionDescription, // OUT: array of section descriptions
            out int cActualSize);               // OUT: actual number of sections

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsQuerySectionDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSection,                    // IN:  ptr to section
            out FSSECTIONDETAILS pSectionDetails); // OUT: section details

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQuerySectionBasicColumnList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSection,                    // IN:  ptr to section
            int cArraySize,                     // IN:  size of array of track descriptions
            FSTRACKDESCRIPTION* rgColumnDescription, // OUT: array of track descriptions
            out int cActualSize);               // OUT: actual number of tracks
#if NEVER
        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQuerySegmentDefinedColumnSpanAreaList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSection,                    // IN:  ptr to section
            int cArraySize,                     // IN:  size of array of track descriptions
            FSTRACKDESCRIPTION* rgColumnDescription, // OUT: array of track descriptions for areas
            out int cActualSize);               // OUT: actual number of segment-defined areas

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQueryHeightDefinedColumnSpanAreaList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSection,                    // IN:  ptr to section
            int cArraySize,                     // IN:  size of array of track descriptions
            FSTRACKDESCRIPTION* rgColumnDescription, // OUT: array of track descriptions for areas
            out int cActualSize);               // OUT: actual number of height-defined areas

FSERR FSAPI FsQuerySectionEndnoteColumnList(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSSECTION,                /* IN: ptr to section                           */
            LONG,                       /* IN: size of array of endnote column descr.   */
            PFSENDNOTECOLUMNDESCRIPTION,/* OUT: array of endnote column descriptions    */
            LONG*);                     /* OUT: actual number of endnote columns        */

FSERR FSAPI FsQueryEndnoteColumnDetails(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSENDNOTECOLUMN,          /* IN: ptr to endnote column                    */
            FSENDNOTECOLUMNDETAILS*);   /* OUT: endnote column details                  */

FSERR FSAPI FsQuerySectionCompositeColumnList(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSSECTION,                /* IN: ptr to section                           */
            LONG,                       /* IN: size of array of composite column descr. */
            PFSCOMPOSITECOLUMNDESCRIPTION,/* OUT: array of composite column descriptions    */
            LONG*);                     /* OUT: actual number of composite columns      */

FSERR FSAPI FsQueryCompositeColumnDetails(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSCOMPOSITECOLUMN,        /* IN: ptr to composite column                  */
            FSCOMPOSITECOLUMNDETAILS*); /* OUT: composite column details                */

FSERR FSAPI FsQueryCompositeColumnFootnoteList(
            PFSCONTEXT,                 /* IN: ptr to FS context                        */
            PCFSCOMPOSITECOLUMN,        /* IN: ptr to composite column                  */
            LONG,                       /* IN: size of array of footnote tracks         */
            PFSTRACKDESCRIPTION,        /* OUT: array of track descriptions             */
            LONG*);                     /* OUT: actual number of footnotes              */
#endif

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsQueryTrackDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pTrack,                      // IN:  ptr to track
            out FSTRACKDETAILS pTrackDetails);  // OUT: track details

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryTrackParaList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pTrack,                      // IN:  ptr to track
            int cParas,                         // IN:  size of array of para descriptions
            FSPARADESCRIPTION* rgParaDesc,      // OUT: array of para descriptions
            out int cParaDesc);                 // OUT: actual number of paragraphs

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQuerySubpageDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubPage,                    // IN:  ptr to subpage
            out FSSUBPAGEDETAILS pSubPageDetails);// OUT: subpage details

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQuerySubpageBasicColumnList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubPage,                    // IN:  ptr to subpage
            int cArraySize,                     // IN:  size of array of track descriptions
            FSTRACKDESCRIPTION* rgColumnDescription, // OUT: array of track descriptions
            out int cActualSize);               // OUT: actual number of tracks

#if NEVER
        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQuerySubpageSegmentDefinedColumnSpanAreaList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubPage,                    // IN:  ptr to subpage
            int cArraySize,                     // IN:  size of array of track descriptions
            FSTRACKDESCRIPTION* rgColumnDescription, // OUT: array of track descriptions for areas
            out int cActualSize);               // OUT: actual number of segment-defined areas

        [DllImport(DllImport.PresentationNative)]
        internal static extern unsafe int FsQuerySubpageHeightDefinedColumnSpanAreaList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubPage,                    // IN:  ptr to subpage
            int cArraySize,                     // IN:  size of array of track descriptions
            FSTRACKDESCRIPTION* rgColumnDescription, // OUT: array of track descriptions for areas
            out int cActualSize);               // OUT: actual number of height-defined areas
#endif

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsQuerySubtrackDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubTrack,                   // IN:  ptr to subtrack
            out FSSUBTRACKDETAILS pSubTrackDetails);// OUT: subpage details

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQuerySubtrackParaList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pSubTrack,                   // IN:  ptr to subtrack
            int cParas,                         // IN:  size of array of para descriptions
            FSPARADESCRIPTION* rgParaDesc,      // OUT: array of para descriptions
            out int cParaDesc);                 // OUT: actual number of paragraphs

        [DllImport(DllImport.PresentationNative)]
        internal static extern int FsQueryTextDetails(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPara,                       // IN:  ptr to text para
            out FSTEXTDETAILS pTextDetails);    // OUT: text details

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryLineListSingle(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPara,                       // IN:  ptr to text para
            int cLines,                         // IN:  size of array of line descriptions
            FSLINEDESCRIPTIONSINGLE* rgLineDesc,// OUT: array of line descriptions
            out int cLineDesc);                 // OUT: actual number of lines

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryLineListComposite(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPara,                       // IN:  ptr to text para
            int cElements,                      // IN:  size of array of line descriptions
            FSLINEDESCRIPTIONCOMPOSITE* rgLineDescription, // OUT: array of line descriptions
            out int cLineElements);             // OUT: actual number of lines

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryLineCompositeElementList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pLine,                       // IN:  ptr to line
            int cElements,                      // IN:  size of array of line elements
            FSLINEELEMENT* rgLineElement,       // OUT: array of line elements
            out int cLineElements);             // OUT: actual number of line elements
#if NEVER
        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryDcpLineVariantsFromCachedTextPara(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPara,                       // IN:  ptr to text para
            int cLines,                         // IN:  number of lines - size of output array
            int* rgDcp,                         // OUT: array of dcp's
            out int cLineActual);               // OUT: actual number of dcp's
#endif

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryAttachedObjectList(
            IntPtr pfsContext,                  // IN:  ptr to FS context
            IntPtr pPara,                       // IN:  ptr to text para
            int cAttachedObject,                // IN:  size of array of attached object descriptions
            FSATTACHEDOBJECTDESCRIPTION* rgAttachedObjects,      // OUT: array of attached object descriptions
            out int cAttachedObjectDesc);               // OUT: actual number of figures

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryFigureObjectDetails(
        			IntPtr pfsContext, /* IN: ptr to FS context						*/
        			IntPtr pPara,      /* IN: ptr to figure para						*/
        			out FSFIGUREDETAILS fsFigureDetails);			/* OUT: figure details							*/


        // ------------------------------------------------------------------
        // fsshiftoff.h
        // ------------------------------------------------------------------
        // FsGetShiftOffset
        // ------------------------------------------------------------------
        // fstableobj.h
        // ------------------------------------------------------------------
        // FsGetTableObjFsimethods
        // ------------------------------------------------------------------
        // fstableobjquery.h
        // ------------------------------------------------------------------
        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryTableObjDetails(
            IntPtr pfscontext,                          // IN:  
            IntPtr pfstableobj,                         // IN:  
            out FSTABLEOBJDETAILS pfstableobjdetails);  // OUT: 

        // FsQueryTableObjFigureCountWord               //  -- MS Word specific 
        // FsQueryTableObjFigureListWord                //  -- MS Word specific 
        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryTableObjTableProperDetails(
            IntPtr pfscontext,                          // IN:  
            IntPtr pfstableProper,                      // IN:  
            out FSTABLEDETAILS pfstabledetailsProper);  // OUT: 

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryTableObjRowList(
            IntPtr pfscontext,                          // IN:  
            IntPtr pfstableProper,                      // IN:  
            int cRows,                                  // IN:  
            FSTABLEROWDESCRIPTION* rgtablerowdescr,     // OUT: 
            out int pcRowsActual);                      // OUT: 

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryTableObjRowDetails(
            IntPtr pfscontext,                          // IN:  
            IntPtr pfstablerow,                         // IN:  
            out FSTABLEROWDETAILS ptableorowdetails);   // OUT: 

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsQueryTableObjCellList(
            IntPtr pfscontext,                          // IN:  
            IntPtr pfstablerow,                         // IN:  
            int cCells,                                 // IN:  
            FSKUPDATE* rgfskupd,                        // OUT: 
            IntPtr* rgpfscell,                          // OUT: 
            FSTABLEKCELLMERGE* rgkcellmerge,            // OUT: 
            out int pcCellsActual);                     // OUT: 


        // ------------------------------------------------------------------
        // fstablesrv.h
        // ------------------------------------------------------------------
        // FsFormatTableSrvFinite
        // FsFormatTableSrvBottomless
        // FsUpdateBottomlessTableSrv
        // FsCompareTableSrv
        // FsClearUpdateInfoInTableSrv
        // FsDestroyTableSrv
        // FsGetTableSrvColumnBalancingInfo
        // FsGetTableSrvNumberFootnotes
        // FsGetTableSrvFootnoteInfo
        // FsDuplicateTableSrvBreakRecord
        // FsDestroyTableSrvBreakRecord
        // FsTransferDisplayInfoTableSrv
        // ------------------------------------------------------------------
        // fstablesrvquery.h
        // ------------------------------------------------------------------
        // FsQueryTableSrvTableDetails
        // FsQueryTableSrvRowList
        // FsQueryTableSrvRowDetails
        // FsQueryTableSrvCellList
        // ------------------------------------------------------------------
        // fstransform.h
        // ------------------------------------------------------------------
        // FOrthogonal
        // FUBackward
        // FVBackward
        // FUEqual
        // FVEqual
        // FsTransformPoint
        // FsTransformVector

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsTransformRectangle(
            uint fswdirIn,                          // IN:  
            ref FSRECT rectPage,                    // IN:  
            ref FSRECT rectTransform,
            uint fswdirOut,
            out FSRECT rectOut);

        [DllImport(DllImport.PresentationNative)]
        internal static unsafe extern int FsTransformBbox(
            uint fswdirIn,                          // IN:  
            ref FSRECT rectPage,                    // IN:  
            ref FSBBOX bboxTransform,
            uint fswdirOut,
            out FSBBOX bboxOut); 

        // ------------------------------------------------------------------
        // fswordframe.h
        // ------------------------------------------------------------------
        // FsAddFigureObstacle
        // FsResolveOverlap
        // FsGetFigureObstacleData
        // FsGetClientHandle

        #endregion Exported functions
    }
}

