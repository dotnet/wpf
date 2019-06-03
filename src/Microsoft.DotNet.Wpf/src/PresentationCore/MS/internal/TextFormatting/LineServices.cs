// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Content:   Line Services managed wrapper
//
//

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Security;
using System.Runtime.InteropServices;
using MS.Internal.Shaping;
using MS.Internal.Text.TextInterface;
using MS.Internal.PresentationCore;

namespace MS.Internal.TextFormatting
{
    //
    //  Line Services application callback delegates
    //
    //  Note:   The declared type of some parameters differs from the logical type.
    //
    //          Some parameters are declared int instead of bool because on the unmanaged side they have type BOOL,
    //          which is a typedef for int. By convention, we use 'f' prefix to indicate that an int represents a
    //          boolean flag.
    //
    //          One parameter is declared "ref ushort" instead of "ref char" because on the unmanaged side WCHAR 
    //          is a typedef for unsigned short. This appears to be necessary for correct behavior even though
    //          sizeof(char) == sizeof(ushort) because char by default is marshalled as an ANSI char.
    //
    //          We could use the more descriptive types and specify [MarshalAs(UnamangedType.Bool)] and
    //          [MarshalAs(UnmanagedType.U2)], respectively, but the slight gain in readibility comes at an
    //          unacceptable cost in run-time overhead due to use of the slow marshaller.
    //

    internal delegate LsErr FetchPap(
        IntPtr                      pols,
        int                         lscpFetch,
        ref LsPap                   lspap
        );                          

    internal delegate LsErr FetchLineProps(
        IntPtr                      pols,               
        int                         lscpFetch,
        int                         firstLineInPara,    
        ref LsLineProps             lsLineProps         
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr FetchRunRedefined(
        IntPtr                      pols,               
        int                         lscpFetch,
        int                         fIsStyle,       // logically boolean (see above)
        IntPtr                      pstyle,             
        char*                       pwchTextBuffer,     
        int                         cchTextBuffer,      
        ref int                     fIsBufferUsed,  // logically boolean (see above)
        out char*                   pwchText,           
        ref int                     cchText,            
        ref int                     fIsHidden,      // logically boolean (see above)     
        ref LsChp                   lschp,              
        ref IntPtr                  lsplsrun            
        );                                       

    internal delegate LsErr GetRunTextMetrics (
        IntPtr                      pols,
        Plsrun                      plsrun,
        LsDevice                    lsDevice,
        LsTFlow                     lstFlow,
        ref LsTxM                   lstTextMetrics
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetRunCharWidths(
        IntPtr                      pols,
        Plsrun                      plsrun,
        LsDevice                    device,
        char                        *runText,
        int                         cchRun,
        int                         maxWidth,
        LsTFlow                     textFlow,
        int                         *charWidths,
        ref int                     totalWidth,
        ref int                     cchProcessed
        );                                        

    internal delegate LsErr GetDurMaxExpandRagged(
        IntPtr                      pols,
        Plsrun                      plsrun,
        LsTFlow                     lstFlow,
        ref int                     maxExpandRagged
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr DrawTextRun(
        IntPtr                      pols,
        Plsrun                      plsrun,
        ref LSPOINT                 ptText,
        char                        *runText,
        int                         *charWidths,
        int                         cchText,
        LsTFlow                     textFlow,
        uint                        displayMode,
        ref LSPOINT                 ptRun,
        ref LsHeights               lsHeights,
        int                         dupRun,
        ref LSRECT                  clipRect
        );

    internal delegate LsErr FInterruptShaping(
        IntPtr                      pols,
        LsTFlow                     textFlow,
        Plsrun                      firstPlsrun,
        Plsrun                      secondPlsrun,
        ref int                     fIsInterruptOk      // logically boolean (see above)
        );

    
    internal delegate LsErr GetRunUnderlineInfo(
        IntPtr                      pols,           
        Plsrun                      plsrun,         
        ref LsHeights               lsHeights,      
        LsTFlow                     textFlow,       
        ref LsULInfo                ulInfo          
        );

    internal delegate LsErr GetRunStrikethroughInfo(
        IntPtr                      pols,           
        Plsrun                      plsrun,         
        ref LsHeights               lsHeights,      
        LsTFlow                     textFlow,       
        ref LsStInfo                stInfo          
        );

    internal delegate LsErr Hyphenate(
        IntPtr                      pols,
        int                         fLastHyphenationFound,  // logically boolean (see above)
        int                         lscpLastHyphenation,
        ref LsHyph                  lastHyphenation,
        int                         lscpBeginWord,
        int                         lscpExceed,
        ref int                     fHyphenFound,           // logically boolean (see above)
        ref int                     lscpHyphen,
        ref LsHyph                  plsHyph
        );

    internal delegate LsErr GetNextHyphenOpp(
        IntPtr                      pols,
        int                         lscpStartSearch,
        int                         lsdcpSearch,
        ref int                     fHyphenFound,           // logically boolean (see above)
        ref int                     lscpHyphen,
        ref LsHyph                  lsHyph
        );

    internal delegate LsErr GetPrevHyphenOpp(
        IntPtr                      pols,
        int                         lscpStartSearch,
        int                         lsdcpSearch,
        ref int                     fHyphenFound,           // logically boolean (see above)
        ref int                     lscpHyphen,
        ref LsHyph                  lsHyph
        );
    
    internal delegate LsErr GetAutoNumberInfo(
        IntPtr                      pols,               
        ref LsKAlign                alignment,          
        ref LsChp                   lschp,              
        ref IntPtr                  lsplsrun,
        ref ushort                  addedChar,              // logically char (see above)
        ref LsChp                   lschpAddedChar,     
        ref IntPtr                  lsplsrunAddedChar,  
        ref int                     fWord95Model,           // logically boolean (see above)
        ref int                     offset,             
        ref int                     width               
        );

    internal delegate LsErr DrawUnderline(
        IntPtr                      pols,       
        Plsrun                      plsrun,     
        uint                        ulType,     
        ref LSPOINT                 ptOrigin,   
        int                         ulLength,   
        int                         ulThickness,
        LsTFlow                     textFlow,   
        uint                        displayMode,
        ref LSRECT                  clipRect    
        );

    internal delegate LsErr DrawStrikethrough(
        IntPtr                      pols,
        Plsrun                      plsrun,
        uint                        stType,
        ref LSPOINT                 ptOrigin,
        int                         stLength,
        int                         stThickness,
        LsTFlow                     textFlow,
        uint                        displayMode,
        ref LSRECT                  clipRect
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetGlyphsRedefined(
        IntPtr                      pols,                   
        IntPtr*                     plsplsruns,             
        int*                        pcchPlsrun,             
        int                         plsrunCount,            
        char*                       pwchText,               
        int                         cchText,                
        LsTFlow                     textFlow,               
        ushort*                     puGlyphsBuffer,         
        uint*                       piGlyphPropsBuffer,     
        int                         cgiGlyphBuffers,
        ref int                     fIsGlyphBuffersUsed,    // logically boolean (see above)  
        ushort*                     puClusterMap,           
        ushort*                     puCharProperties,       
        int*                        pfCanGlyphAlone,        
        ref int                     glyphCount              
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetGlyphPositions(
        IntPtr                      pols,               
        IntPtr                      *plsplsruns,
        int                         *pcchPlsrun,
        int                         plsrunCount,
        LsDevice                    device,             
        char                        *pwchText,          
        ushort                      *puClusterMap,        
        ushort                      *puCharProperties,
        int                         cchText,            
        ushort                      *puGlyphs,          
        uint                        *piGlyphProperties,      
        int                         glyphCount,         
        LsTFlow                     textFlow,           
        int                         *piGlyphAdvances,   
        GlyphOffset                 *piiGlyphOffsets 
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr DrawGlyphs(
        IntPtr                      pols,
        Plsrun                      plsrun,
        char                        *pwchText,
        ushort                      *puClusterMap,
        ushort                      *puCharProperties,
        int                         cchText,
        ushort                      *puGlyphs,
        int                         *piJustifiedGlyphAdvances,
        int                         *puGlyphAdvances,
        GlyphOffset                 *piiGlyphOffsets,
        uint                        *piGlyphProperties,
        LsExpType                   *plsExpType,
        int                         glyphCount,
        LsTFlow                     textFlow,
        uint                        displayMode,
        ref LSPOINT                 origin,
        ref LsHeights               lsHeights,
        int                         runWidth,
        ref LSRECT                  clippingRect
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr EnumText(
        IntPtr                      pols,
        Plsrun                      plsrun,
        int                         cpFirst,
        int                         dcp,
        char                        *pwchText,
        int                         cchText,
        LsTFlow                     lstFlow,
        int                         fReverseOrder,      // logically boolean (see above)
        int                         fGeometryProvided,  // logically boolean (see above)
        ref LSPOINT                 pptStart,
        ref LsHeights               pheights,
        int                         dupRun,
        int                         glyphBaseRun,
        int                         *charWidths,
        ushort                      *pClusterMap,
        ushort                      *characterProperties,
        ushort                      *puglyphs,
        int                         *pGlyphAdvances,
        GlyphOffset                 *pGlyphOffsets,
        uint                        *pGlyphProperties,
        int                         glyphCount
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr EnumTab(
        IntPtr                      pols,
        Plsrun                      plsrun,
        int                         cpFirst,
        char                        *pwchText,
        char                        tabLeader,
        LsTFlow                     lstFlow,
        int                         fReverseOrder,      // logically boolean (see above)
        int                         fGeometryProvided,  // logically boolean (see above)
        ref LSPOINT                 pptStart,
        ref LsHeights               heights,
        int                         dupRun
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetCharCompressionInfoFullMixed(
        IntPtr                      pols,
        LsDevice                    device,
        LsTFlow                     textFlow,
        LsCharRunInfo               *plscharrunInfo,
        LsNeighborInfo              *plsneighborInfoLeft,
        LsNeighborInfo              *plsneighborInfoRight,
        int                         maxPriorityLevel,
        int                         **pplscompressionLeft,
        int                         **pplscompressionRight
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetCharExpansionInfoFullMixed(
        IntPtr                      pols,
        LsDevice                    device,
        LsTFlow                     textFlow,
        LsCharRunInfo               *plscharrunInfo,
        LsNeighborInfo              *plsneighborInfoLeft,
        LsNeighborInfo              *plsneighborInfoRight,
        int                         maxPriorityLevel,
        int                         **pplsexpansionLeft,
        int                         **pplsexpansionRight
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetGlyphCompressionInfoFullMixed(
        IntPtr                      pols,
        LsDevice                    device,
        LsTFlow                     textFlow,
        LsGlyphRunInfo              *plsglyphrunInfo,
        LsNeighborInfo              *plsneighborInfoLeft,
        LsNeighborInfo              *plsneighborInfoRight,
        int                         maxPriorityLevel,
        int                         **pplscompressionLeft,
        int                         **pplscompressionRight
        );

    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetGlyphExpansionInfoFullMixed(
        IntPtr                      pols,
        LsDevice                    device,
        LsTFlow                     textFlow,
        LsGlyphRunInfo              *plsglyphrunInfo,
        LsNeighborInfo              *plsneighborInfoLeft,
        LsNeighborInfo              *plsneighborInfoRight,
        int                         maxPriorityLevel,
        int                         **pplsexpansionLeft,
        int                         **pplsexpansionRight,
        LsExpType                   *plsexptype,
        int                         *pduMinInk
        );


    //
    //  Line Services object handler callback delegates
    //
    //
    /// <SecurityNote>
    /// Critical    - Recieves native pointers as parameters.
    /// </SecurityNote>
    [SecurityCritical]
    internal unsafe delegate LsErr GetObjectHandlerInfo(
        IntPtr                      pols,               // Line Layout context
        uint                        objectId,           // installed object id
        void*                       objectInfo          // object handler info
        );

    internal delegate LsErr InlineFormat(
        IntPtr                      pols,               // Line Layout context
        Plsrun                      plsrun,             // plsrun
        int                         lscpInline,         // first cp of the run
        int                         currentPosition,    // inline's current pen location in text direction
        int                         rightMargin,        // right margin
        ref ObjDim                  pobjDim,            // object dimension
        out int                     fFirstRealOnLine,   // is this run the first in line; logically boolean (see above)
        out int                     fPenPositionUsed,   // is pen position used to format object; logically boolean (see above)
        out LsBrkCond               breakBefore,        // break condition before this object
        out LsBrkCond               breakAfter          // break condition after this object
        );

    internal delegate LsErr InlineDraw(
        IntPtr                      pols,               // Line Layout context
        Plsrun                      plsrun,             // plsrun
        ref LSPOINT                 runOrigin,          // pen position at which to render the object
        LsTFlow                     textFlow,           // text flow direction
        int                         runWidth            // object width
        );


    //
    //  Line Services enumerations
    //
    //

    //  handle to text run opaque to Line Services
    internal enum Plsrun : uint
    {
        CloseAnchor             = 0,
        Reverse                 = 1,
        FakeLineBreak           = 2, // simulated line break for security mitigation
        FormatAnchor            = 3,
        Hidden                  = 4,
        Text                    = 5, // run with type starting at this point is stored
        InlineObject            = 6,
        LineBreak               = 7,
        ParaBreak               = 8,

        Undefined               = 0x80000000,   // Bit 31 cannot be set without causing an overflow exception in LineServices so we reserve it as a 'undefined' Plsrun value
        IsMarker                = 0x40000000,   // Bit indicates this run is part of marker symbol
        UseNewCharacterBuffer   = 0x20000000,   // Bit indicates this run uses dynamically allocated heap buffer for run characters
        IsSymbol                = 0x10000000,   // Bit indicates run uses a non-Unicode font; don't change this value without updating unmanaged GetBreakingClasses callback
        UnmaskAll               = 0x0FFFFFFF,   // Value to unmask all the masking bits
    }


    // Unmanaged enum counterpart is in LSLO.H. 
    // == Both sides must be binary compatible ==
    [Flags]    
    internal enum LineFlags 
    {
        // line break flags
        None                = 0,
        BreakClassWide      = 0x00000001,
        BreakClassStrict    = 0x00000002,
        BreakAlways         = 0x00000004,
        MinMax              = 0x00000008,
        KeepState           = 0x00000010,
    }


    internal enum LsEndRes
    {
        endrNormal,
        endrHyphenated,
        endrEndPara,
        endrAltPara,
        endrSoftCR,
        endrEndColumn,
        endrEndSection,
        endrEndPage,
        endrEndParaSection,
        endrStopped,
        endrBeforeFillLineObject,
        endrAfterFillLineObject,
        endrMathUserRequiredBreak
    }

    internal enum LsBreakJust
    {
        lsbrjBreakJustify,          // regular US
        lsbrjBreakWithCompJustify,  // FE & Newspaper
        lsbrjBreakThenExpand,       // Arabic
        lsbrjBreakOptimal,          // Best fit/Optimal paragraph
        lsbrjBreakThenSqueeze       // WordPerfect
    }

    internal enum LsKJust
    {
        lskjFullInterWord,
        lskjFullInterLetterAligned,
        lskjFullScaled,
        lskjFullGlyphs,
        lskjFullMixed,
        lskjSnapGrid
    }

    internal enum LsKAlign 
    {
        lskalLeft,
        lskalCentered,
        lskalRight,
    }

    internal enum LsKEOP   
    {
        lskeopEndPara1,
        lskeopEndPara2,
        lskeopEndPara12,
        lskeopEndParaAlt
    }

    internal enum LsKTab
    {
        lsktLeft,
        lsktCenter,
        lsktRight,
        lsktDecimal,
        lsktChar
    }

    internal enum LsTFlow 
    {
        lstflowDefault = 0,
        lstflowES      = 0,
        lstflowEN,
        lstflowSE,
        lstflowSW,
        lstflowWS,
        lstflowWN,
        lstflowNE,
        lstflowNW,
    }

    internal enum LsBrkCond
    {
        Never,
        Can,
        Please,
        Must
    }

    internal enum LsDevice 
    {
        Presentation,
        Reference,
    }

    internal enum LsExpType : byte 
    {
        None = 0,
        AddWhiteSpace,
        AddInkContinuous,
        AddInkDiscrete,
    }

    internal enum LsKysr
    {
        /// <summary>
        /// Normal Hyphenation
        /// </summary>
        kysrNormal,

        /// <summary>
        /// Add letter before hyphen
        /// </summary>
        kysrAddBefore,

        /// <summary>
        /// Change letter before hyphen
        /// </summary>
        kysrChangeBefore,

        /// <summary>
        /// Delete letter before hyphen
        /// </summary>
        kysrDeleteBefore,

        /// <summary>
        /// Change letter after hyphen
        /// </summary>
        kysrChangeAfter,

        /// <summary>
        /// Delete letter before the hypen and change the preceding one
        /// </summary>
        kysrDelAndChange,

        /// <summary>
        /// Add letter before the hyphen and change letter after it
        /// </summary>
        kysrAddBeforeChangeAfter,
    }

    internal enum LsHyphenQuality
    {
        lshqExcellent,
        lshqGood,
        lshqFair,
        lshqPoor,
        lshqBad
    }

    internal enum LsErr 
    {
        None                                    = 0,
        InvalidParameter                        = -1,
        OutOfMemory                             = -2,
        NullOutputParameter                     = -3,
        InvalidContext                          = -4,
        InvalidLine                             = -5,
        InvalidDnode                            = -6,
        InvalidDeviceResolution                 = -7,
        InvalidRun                              = -8,
        MismatchLineContext                     = -9,
        ContextInUse                            = -10,
        DuplicateSpecialCharacter               = -11,
        InvalidAutonumRun                       = -12,
        FormattingFunctionDisabled              = -13,
        UnfinishedDnode                         = -14,
        InvalidDnodeType                        = -15,
        InvalidPenDnode                         = -16,
        InvalidNonPenDnode                      = -17,
        InvalidBaselinePenDnode                 = -18,
        InvalidFormatterResult                  = -19,
        InvalidObjectIdFetched                  = -20,
        InvalidDcpFetched                       = -21,
        InvalidCpContentFetched                 = -22,
        InvalidBookmarkType                     = -23,
        SetDocDisabled                          = -24,
        FiniFunctionDisabled                    = -25,
        CurrentDnodeIsNotTab                    = -26,
        PendingTabIsNotResolved                 = -27,
        WrongFiniFunction                       = -28,
        InvalidBreakingClass                    = -29,
        BreakingTableNotSet                     = -30,
        InvalidModWidthClass                    = -31,
        ModWidthPairsNotSet                     = -32,
        WrongTruncationPoint                    = -33,
        WrongBreak                              = -34,
        DupInvalid                              = -35,
        RubyInvalidVersion                      = -36,
        TatenakayokoInvalidVersion              = -37,
        WarichuInvalidVersion                   = -38,
        WarichuInvalidData                      = -39,
        CreateSublineDisabled                   = -40,
        CurrentSublineDoesNotExist              = -41,
        CpOutsideSubline                        = -42,
        HihInvalidVersion                       = -43,
        InsufficientQueryDepth                  = -44,
        InvalidBreakRecord                      = -45,
        InvalidPap                              = -46,
        ContradictoryQueryInput                 = -47,
        LineIsNotActive                         = -48,
        TooLongParagraph                        = -49,
        TooManyCharsToGlyph                     = -50,
        WrongHyphenationPosition                = -51,
        TooManyPriorities                       = -52,
        WrongGivenCp                            = -53,
        WrongCpFirstForGetBreaks                = -54,
        WrongJustTypeForGetBreaks               = -55,
        WrongJustTypeForCreateLineGivenCp       = -56,
        TooLongGlyphContext                     = -57,
        InvalidCharToGlyphMapping               = -58, 
        InvalidMathUsage                        = -59,
        InconsistentChp                         = -60,
        StoppedInSubline                        = -61,
        PenPositionCouldNotBeUsed               = -62,
        DebugFlagsInShip                        = -63,
        InvalidOrderTabs                        = -64,
        OutputArrayTooSmall                     = -110,
        SystemRestrictionsExceeded              = -100,
        LsInternalError                         = -1000,
        NotImplemented                          = -10000,
        ClientAbort                             = -100000,
    }

    //
    //  Line Services structures
    //
    //

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSPOINT
    {
        public LSPOINT(int horizontalPosition, int verticalPosition)
        {
            x = horizontalPosition;
            y = verticalPosition;
        }

        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSRECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        internal LSRECT(int x1, int y1, int x2, int y2)
        {
            left    = x1;
            top     = y1;
            right   = x2;
            bottom  = y2;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSSIZE
    {
        public int width;
        public int height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EscStringInfo
    {
        public IntPtr   szParaSeparator;
        public IntPtr   szLineSeparator;
        public IntPtr   szHidden;
        public IntPtr   szNbsp;
        public IntPtr   szObjectTerminator;
        public IntPtr   szObjectReplacement;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjDim
    {
        public LsHeights    heightsRef;
        public LsHeights    heightsPres;
        public int          dur;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsTbd
    {
        public LsKTab       lskt;
        public int          ur;

        [MarshalAs(UnmanagedType.U2)]
        public char         wchTabLeader;

        [MarshalAs(UnmanagedType.U2)]
        public char         wchCharTab;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsTabs
    {
        public int       durIncrementalTab;
        public int       iTabUserDefMac;
        public IntPtr    plsTbd;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsULInfo
    {
        public uint     kulBase;
        public int      cNumberOfLines;
        public int      dvpUnderlineOriginOffset;
        public int      dvpFirstUnderlineOffset;
        public int      dvpFirstUnderlineSize;
        public int      dvpGapBetweenLines;
        public int      dvpSecondUnderlineSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsStInfo
    {
        public uint     kstBase;
        public int      cNumberOfLines;
        public int      dvpLowerStrikethroughOffset;
        public int      dvpLowerStrikethroughSize;
        public int      dvpUpperStrikethroughOffset;
        public int      dvpUpperStrikethroughSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct LsHyph
    {
        public LsKysr               kysr;
        public char                 wchYsr;
        public char                 wchYsr2;
        public LsHyphenQuality      lshq;
    }


    //  Installed object handler methods
    /// <SecurityNote>
    /// Critical - as this structure points to several callbacks that'll be
    ///            invoked from unmanaged code.  Marked all members Critical.
    ///            Just putting the SecurityNote on the top instead of on every
    ///            member.
    /// </SecurityNote>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsIMethods
    {
        [SecurityCritical]
        public IntPtr pfnCreateILSObj;
        [SecurityCritical]
        public IntPtr pfnDestroyILSObj;
        [SecurityCritical]
        public IntPtr pfnSetDoc;
        [SecurityCritical]
        public IntPtr pfnCreateLNObj;
        [SecurityCritical]
        public IntPtr pfnDestroyLNObj;
        [SecurityCritical]
        public IntPtr pfnFmt;
        [SecurityCritical]
        public IntPtr pfnFmtResume;
        [SecurityCritical]
        public IntPtr pfnGetModWidthPrecedingChar;
        [SecurityCritical]
        public IntPtr pfnGetModWidthFollowingChar;
        [SecurityCritical]
        public IntPtr pfnTruncate;
        [SecurityCritical]
        public IntPtr pfnFindPrevBreakOppInside;
        [SecurityCritical]
        public IntPtr pfnFindNextBreakOppInside;
        [SecurityCritical]
        public IntPtr pfnFindBreakOppBeforeCpTruncate;
        [SecurityCritical]
        public IntPtr pfnFindBreakOppAfterCpTruncate;
        [SecurityCritical]
        public IntPtr pfnCreateStartOppInside;
        [SecurityCritical]
        public IntPtr pfnProposeBreakAfter;
        [SecurityCritical]
        public IntPtr pfnProposeBreakBefore;
        [SecurityCritical]
        public IntPtr pfnCreateBreakOppAfter;
        [SecurityCritical]
        public IntPtr pfnCreateStartOppBefore;
        [SecurityCritical]
        public IntPtr pfnCreateDobjFragment;
        [SecurityCritical]
        public IntPtr pfnForceBreak;
        [SecurityCritical]
        public IntPtr pfnCreateBreakRecord;
        [SecurityCritical]
        public IntPtr pfnSetBreak;
        [SecurityCritical]
        public IntPtr pfnDestroyStartOpp;
        [SecurityCritical]
        public IntPtr pfnDestroyBreakOpp;
        [SecurityCritical]
        public IntPtr pfnDuplicateBreakRecord;
        [SecurityCritical]
        public IntPtr pfnDestroyBreakRecord;
        [SecurityCritical]
        public IntPtr pfnGetSpecialEffectsFromDobj;
        [SecurityCritical]
        public IntPtr pfnGetSpecialEffectsFromDobjFragment;
        [SecurityCritical]
        public IntPtr pfnGetSubmissionInfoFromDobj;
        [SecurityCritical]
        public IntPtr pfnGetSublinesFromDobj;
        [SecurityCritical]
        public IntPtr pfnGetSubmissionInfoFromDobjFragment;
        [SecurityCritical]
        public IntPtr pfnGetSubsFromDobjFragment;
        [SecurityCritical]
        public IntPtr pfnFExpandWithPrecedingChar;
        [SecurityCritical]
        public IntPtr pfnFExpandWithFollowingChar;
        [SecurityCritical]
        public IntPtr pfnCalcPresentation;
        [SecurityCritical]
        public IntPtr pfnQueryPointPcp;
        [SecurityCritical]
        public IntPtr pfnQueryCpPpoint;
        [SecurityCritical]
        public IntPtr pfnEnum;
        [SecurityCritical]
        public IntPtr pfnDisplay;
        [SecurityCritical]
        public IntPtr pfnDestroyDobj;
        [SecurityCritical]
        public IntPtr pfnDestroyDobjFragment;
    }


    /// <summary>
    /// Small set of callbacks that are slightly different from its correspondent original
    /// LS callbacks due to logistics of managed code callback interop with LS.
    /// </summary>
    /// <SecurityNote>
    /// Critical - as this structure points to several callbacks that'll be
    ///            invoked from unmanaged code.  Marked all function pointer
    ///            members Critical.
    /// 
    ///            Just putting the SecurityNote on the top instead of on every
    ///            member.
    /// </SecurityNote>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct LscbkRedefined
    {
        [SecurityCritical]
        public FetchRunRedefined        pfnFetchRunRedefined;
        [SecurityCritical]
        public GetGlyphsRedefined       pfnGetGlyphsRedefined;
        [SecurityCritical]
        public FetchLineProps           pfnFetchLineProps;
    }


    //  Context info (LSCONTEXTINFO)
    /// <SecurityNote>
    /// Critical - as this structure points to several callbacks that'll be
    ///            invoked from unmanaged code.  Marked all function pointer
    ///            members Critical.
    /// 
    ///            Just putting the SecurityNote on the top instead of on every
    ///            member.
    /// </SecurityNote>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal unsafe struct LsContextInfo
    {
        public uint                             version;            // version number
        public int                              cInstalledHandlers; // total installed object handlers
        public IntPtr                           plsimethods;        // array of installed objects LSIMETHODS

        // Text config (LSTXTCFG)
        public int                              cEstimatedCharsPerLine;
        // used for input array size for all four FullMixed justification callbacks (cPriorityLevelMax)
        public int                              cJustPriorityLim;   

        public char                             wchUndef;
        public char                             wchNull;
        public char                             wchSpace;
        public char                             wchHyphen;
        public char                             wchTab;
        public char                             wchPosTab;
        public char                             wchEndPara1;
        public char                             wchEndPara2;
        public char                             wchAltEndPara;
        public char                             wchEndLineInPara;
        public char                             wchColumnBreak;
        public char                             wchSectionBreak;
        public char                             wchPageBreak;
        public char                             wchNonBreakSpace;
        public char                             wchNonBreakHyphen;
        public char                             wchNonReqHyphen;
        public char                             wchEmDash;
        public char                             wchEnDash;
        public char                             wchEmSpace;
        public char                             wchEnSpace;
        public char                             wchNarrowSpace;
        public char                             wchOptBreak;
        public char                             wchNoBreak;
        public char                             wchFESpace;
        public char                             wchJoiner;
        public char                             wchNonJoiner;
        public char                             wchToReplace;
        public char                             wchReplace;

        public char                             wchVisiNull;
        public char                             wchVisiAltEndPara;
        public char                             wchVisiEndLineInPara;
        public char                             wchVisiEndPara;
        public char                             wchVisiSpace;
        public char                             wchVisiNonBreakSpace;
        public char                             wchVisiNonBreakHyphen;
        public char                             wchVisiNonReqHyphen;
        public char                             wchVisiTab;
        public char                             wchVisiPosTab;
        public char                             wchVisiEmSpace;
        public char                             wchVisiEnSpace;
        public char                             wchVisiNarrowSpace;
        public char                             wchVisiOptBreak;
        public char                             wchVisiNoBreak;
        public char                             wchVisiFESpace;
        public char                             wchEscAnmRun;
        public char                             wchPad;


        //  More LSCONTEXTINFO
        public IntPtr                           pols;  // Opaque client object
    
        // Memory management on unmanaged heap
        [SecurityCritical]
        public IntPtr                           pfnNewPtr;
        [SecurityCritical]
        public IntPtr                           pfnDisposePtr;
        [SecurityCritical]
        public IntPtr                           pfnReallocPtr;

        // General application callbacks
        [SecurityCritical]
        public IntPtr                           pfnFetchRun;
        [SecurityCritical]
        public GetAutoNumberInfo                pfnGetAutoNumberInfo;
        [SecurityCritical]
        public IntPtr                           pfnGetNumericSeparators;
        [SecurityCritical]
        public IntPtr                           pfnCheckForDigit;
        [SecurityCritical]
        public FetchPap                         pfnFetchPap;
        [SecurityCritical]
        public FetchLineProps                   pfnFetchLineProps;
        [SecurityCritical]
        public IntPtr                           pfnFetchTabs;
        [SecurityCritical]
        public IntPtr                           pfnReleaseTabsBuffer;
        [SecurityCritical]
        public IntPtr                           pfnGetBreakThroughTab;
        [SecurityCritical]
        public IntPtr                           pfnGetPosTabProps;        
        [SecurityCritical]
        public IntPtr                           pfnFGetLastLineJustification;
        [SecurityCritical]
        public IntPtr                           pfnCheckParaBoundaries;
        [SecurityCritical]
        public GetRunCharWidths                 pfnGetRunCharWidths;
        [SecurityCritical]
        public IntPtr                           pfnCheckRunKernability;
        [SecurityCritical]
        public IntPtr                           pfnGetRunCharKerning;
        [SecurityCritical]
        public GetRunTextMetrics                pfnGetRunTextMetrics;
        [SecurityCritical]
        public GetRunUnderlineInfo              pfnGetRunUnderlineInfo;
        [SecurityCritical]
        public GetRunStrikethroughInfo          pfnGetRunStrikethroughInfo;
        [SecurityCritical]
        public IntPtr                           pfnGetBorderInfo;
        [SecurityCritical]
        public IntPtr                           pfnReleaseRun;
        [SecurityCritical]
        public IntPtr                           pfnReleaseRunBuffer;
        [SecurityCritical]
        public Hyphenate                        pfnHyphenate;
        [SecurityCritical]
        public GetPrevHyphenOpp                 pfnGetPrevHyphenOpp;
        [SecurityCritical]
        public GetNextHyphenOpp                 pfnGetNextHyphenOpp;
        [SecurityCritical]
        public IntPtr                           pfnGetHyphenInfo;
        [SecurityCritical]
        public DrawUnderline                    pfnDrawUnderline;
        [SecurityCritical]
        public DrawStrikethrough                pfnDrawStrikethrough;
        [SecurityCritical]
        public IntPtr                           pfnDrawBorder;
        [SecurityCritical]
        public IntPtr                           pfnFInterruptUnderline;
        [SecurityCritical]
        public IntPtr                           pfnFInterruptShade;
        [SecurityCritical]
        public IntPtr                           pfnFInterruptBorder;
        [SecurityCritical]
        public IntPtr                           pfnShadeRectangle;
        [SecurityCritical]
        public DrawTextRun                      pfnDrawTextRun;
        [SecurityCritical]
        public IntPtr                           pfnDrawSplatLine;
        [SecurityCritical]
        public FInterruptShaping                pfnFInterruptShaping;
        [SecurityCritical]
        public IntPtr                           pfnGetGlyphs;
        [SecurityCritical]
        public GetGlyphPositions                pfnGetGlyphPositions;
        [SecurityCritical]
        public DrawGlyphs                       pfnDrawGlyphs;
        [SecurityCritical]
        public IntPtr                           pfnReleaseGlyphBuffers;
        [SecurityCritical]
        public IntPtr                           pfnGetGlyphExpansionInfo;
        [SecurityCritical]
        public IntPtr                           pfnGetGlyphExpansionInkInfo;
        [SecurityCritical]
        public IntPtr                           pfnGetGlyphRunInk;
        [SecurityCritical]
        public IntPtr                           pfnGetEms;
        [SecurityCritical]
        public IntPtr                           pfnPunctStartLine;
        [SecurityCritical]
        public IntPtr                           pfnModWidthOnRun;
        [SecurityCritical]
        public IntPtr                           pfnModWidthSpace;
        [SecurityCritical]
        public IntPtr                           pfnCompOnRun;
        [SecurityCritical]
        public IntPtr                           pfnCompWidthSpace;
        [SecurityCritical]
        public IntPtr                           pfnExpOnRun;
        [SecurityCritical]
        public IntPtr                           pfnExpWidthSpace;
        [SecurityCritical]
        public IntPtr                           pfnGetModWidthClasses;
        [SecurityCritical]
        public IntPtr                           pfnGetBreakingClasses;
        [SecurityCritical]
        public IntPtr                           pfnFTruncateBefore;
        [SecurityCritical]
        public IntPtr                           pfnCanBreakBeforeChar;
        [SecurityCritical]
        public IntPtr                           pfnCanBreakAfterChar;
        [SecurityCritical]
        public IntPtr                           pfnFHangingPunct;
        [SecurityCritical]
        public IntPtr                           pfnGetSnapGrid;
        [SecurityCritical]
        public IntPtr                           pfnDrawEffects;
        [SecurityCritical]
        public IntPtr                           pfnFCancelHangingPunct;
        [SecurityCritical]
        public IntPtr                           pfnModifyCompAtLastChar;
        [SecurityCritical]
        public GetDurMaxExpandRagged            pfnGetDurMaxExpandRagged;
        [SecurityCritical]
        public GetCharExpansionInfoFullMixed    pfnGetCharExpansionInfoFullMixed;
        [SecurityCritical]
        public GetGlyphExpansionInfoFullMixed   pfnGetGlyphExpansionInfoFullMixed;
        [SecurityCritical]
        public GetCharCompressionInfoFullMixed  pfnGetCharCompressionInfoFullMixed;
        [SecurityCritical]
        public GetGlyphCompressionInfoFullMixed pfnGetGlyphCompressionInfoFullMixed;
        [SecurityCritical]
        public IntPtr                           pfnGetCharAlignmentStartLine;
        [SecurityCritical]
        public IntPtr                           pfnGetCharAlignmentEndLine;
        [SecurityCritical]
        public IntPtr                           pfnGetGlyphAlignmentStartLine;
        [SecurityCritical]
        public IntPtr                           pfnGetGlyphAlignmentEndLine;
        [SecurityCritical]
        public IntPtr                           pfnGetPriorityForGoodTypography;

        [SecurityCritical]
        public EnumText                         pfnEnumText;
        [SecurityCritical]
        public EnumTab                          pfnEnumTab;
        [SecurityCritical]
        public IntPtr                           pfnEnumPen;
        [SecurityCritical]
        public GetObjectHandlerInfo             pfnGetObjectHandlerInfo;      
    
        // Debugging
        [SecurityCritical]
        public IntPtr                           pfnAssertFailedPtr;

        // Even more LSCONTEXTINFO
        public int                              fDontReleaseRuns;
    }

    // Presentation/Rendering device resolutions (LSDEVRES)
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsDevRes
    {
        public uint   dxpInch;
        public uint   dypInch;
        public uint   dxrInch;
        public uint   dyrInch;
    }

    // Presentation/Rendering device resolutions (LSDEVRES)
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsLInfo
    {
        public int                  dvpAscent;
        public int                  dvrAscent;
        public int                  dvpDescent;
        public int                  dvrDescent;
        public int                  dvpMultiLineHeight;
        public int                  dvrMultiLineHeight;
        public int                  dvpAscentAutoNumber;
        public int                  dvrAscentAutoNumber;
        public int                  dvpDescentAutoNumber;
        public int                  dvrDescentAutoNumber;
        public int                  cpLimToContinue;
        public int                  cpLimToStay;
        public int                  dcpDepend;
        public int                  cpFirstVis;
        public LsEndRes             endr;
        public int                  fAdvanced;
        public int                  vaAdvance;
        public int                  fFirstLineInPara;
        public int                  fTabInMarginExLine;
        public int                  fForcedBreak;
        public uint                 EffectsFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsBreakRecSubl
    {
        public int      lscpFetch;
        public int      idobj;
        public IntPtr   pbreakrecobj;
    }


    // Paragraph Properties (LSPAP)
    // Returned to LS thru FetchPap, called upon LsCreateLine.
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsPap
    {
        public int          cpFirst;            // First CP
        public int          cpFirstContent;     // First content CP

        [Flags]
        public enum Flags : uint
        {
            None = 0,

            //  Visi flags
            fFmiVisiCondHyphens             = 0x00000001,
            fFmiVisiParaMarks               = 0x00000002,
            fFmiVisiSpaces                  = 0x00000004,
            fFmiVisiTabs                    = 0x00000008,
            fFmiVisiSplats                  = 0x00000010,
            fFmiVisiBreaks                  = 0x00000020,

            //  Advanced typography
            fFmiApplyBreakingRules          = 0x00000040,
            fFmiApplyOpticalAlignment       = 0x00000080,
            fFmiPunctStartLine              = 0x00000100,
            fFmiHangingPunct                = 0x00000200,

            //  WYSIWYG flags
            fFmiPresSuppressWiggle          = 0x00000400,
            fFmiPresExactSync               = 0x00000800,

            //  AutoNumbering flags
            fFmiAnm                         = 0x00001000,

            //  Misc.
            fFmiAutoDecimalTab              = 0x00002000,
            fFmiUnderlineTrailSpacesRM      = 0x00004000,
            fFmiSpacesInfluenceHeight       = 0x00008000,
            fFmiIgnoreSplatBreak            = 0x00010000,
            fFmiLimSplat                    = 0x00020000,
            fFmiAllowSplatLine              = 0x00040000,
            fFmiForceBreakAsNext            = 0x00080000,
            fFmiAllowHyphenation            = 0x00100000,
            fFmiDrawInCharCodes             = 0x00200000,
            fFmiTreatHyphenAsRegular        = 0x00400000,
            fFmiWrapTrailingSpaces          = 0x00800000,
            fFmiWrapAllSpaces               = 0x01000000,

            //  Compatibility flags for bugs in older versions of Word
            fFmiFCheckTruncateBefore        = 0x02000000,
            fFmiForgetLastTabAlignment      = 0x10000000,
            fFmiIndentChangesHyphenZone     = 0x20000000,
            fFmiNoPunctAfterAutoNumber      = 0x40000000,
            fFmiResolveTabsAsWord97         = 0x80000000,
        }

        public Flags        grpf;
        public LsBreakJust  lsbrj;
        public LsKJust      lskj;
        public int          fJustify;
        public int          durAutoDecimalTab;
        public LsKEOP       lskeop;
        public LsTFlow      lstflow;
    }


    // Line properties
    // Returned to LS thru FetchLineProps
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsLineProps
    {
        public LsKAlign     lskal;                  // Alignment type
        public int          durLeft;                // Left line boundary
        public int          durRightBreak;          // Right linebreak boundary
        public int          durRightJustify;        // Right justification boundary
        public int          fProhibitHyphenation;   // prohibit hyphenation on this line?
        public int          durHyphenationZone;     // hyphenation zone for non-optimal breaking
    }


    // Charater properties (LsChp)
    // Returned to LS thru FetchRun
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsChp
    {
        public ushort       idObj;
        public ushort       dcpMaxContent;
        public uint         effectsFlags;

        [Flags]
        public enum Flags : uint
        {
            None = 0,
            fApplyKern              = 0x0001,
            fModWidthOnRun          = 0x0002,
            fModWidthSpace          = 0x0004,
            fModWidthPairs          = 0x0008,
            fCompressOnRun          = 0x0010,
            fCompressSpace          = 0x0020,
            fCompressTable          = 0x0040,
            fExpandOnRun            = 0x0080,
            fExpandSpace            = 0x0100,
            fExpandTable            = 0x0200,
            fGlyphBased             = 0x0400,
            // 5 bits of padding to align to next word
            fInvisible              = 0x00010000,
            fUnderline              = 0x00020000,               
            fStrike                 = 0x00040000,
            fShade                  = 0x00080000,
            fBorder                 = 0x00100000,
            fSymbol                 = 0x00200000,
            fHyphen                 = 0x00400000,   // Hyphenation opportunity (YSR info)
            fCheckForReplaceChar    = 0x00800000,   // Activate the replace char mechanizm for Yen
            // 8 bits of padding
        }

        public Flags        flags;      // bitfields
        public int          dvpPos;
    }

    // TextMetrics (LSTXM)
    // Returned to LS by GetRunTextMetrics
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsTxM
    {
        public int      dvAscent;
        public int      dvDescent;
        public int      dvMultiLineHeight;
        public int      fMonospaced;         // BOOL
    }

    // Heights (heights)
    // Input parameter to DrawTextRun
    [StructLayout(LayoutKind.Sequential)]
    internal struct LsHeights
    {
        public int      dvAscent;
        public int      dvDescent;
        public int      dvMultiLineHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsQSubInfo
    {
        public LsTFlow     lstflowSubLine;
        public int         lscpFirstSubLine;
        public int         lsdcpSubLine;
        public LSPOINT     pointUvStartSubLine;
        public LsHeights   lsHeightsPresSubLine;
        public int         dupSubLine;

        public uint        idobj;
        public IntPtr      plsrun;
        public int         lscpFirstRun;
        public int         lsdcpRun;
        public LSPOINT     pointUvStartRun;
        public LsHeights   lsHeightsPresRun;
        public int         dupRun;
        public int         dvpPosRun;
        public int         dupBorderBefore;
        public int         dupBorderAfter;

        public LSPOINT     pointUvStartObj;
        public LsHeights   lsHeightsPresObj;
        public int         dupObj;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsTextCell
    {
        public int       lscpStartCell;
        public int       lscpEndCell;
        public LSPOINT   pointUvStartCell;
        public int       dupCell;
        public int       cCharsInCell;
        public int       cGlyphsInCell;
        public IntPtr    plsCellDetails;  // client-defined structure
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LsLineWidths
    {
        public int      upStartMarker;      // column start to marker start
        public int      upLimMarker;        // column start to marker end
        public int      upStartMainText;    // column start to main text start
        public int      upStartTrailing;    // column start to trailing space start
        public int      upLimLine;          // column start to line end
        public int      upMinStartTrailing; // the smallest upStartTrailing possible
        public int      upMinLimLine;       // the smallest upLimLine possible
    }


    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct LsBreaks
    {
        public int              cBreaks;                // number of total breaks
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public LsLInfo*         plslinfoArray;          // array of LSLINFO structs, each per each break
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public IntPtr*          plinepenaltyArray;      // array of unsafe handle to TSLINEPENALITYINFO struct, each per each break
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public IntPtr*          pplolineArray;          // array of unsafe handle to Loline struct, each per each break
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct LsNeighborInfo
    {
        public uint                         fNeighborIsPresent;
        public uint                         fNeighborIsText;
        public Plsrun                       plsrun;
        [MarshalAs(UnmanagedType.U2)]
        public char                         wch;
        public uint                         fGlyphBased;
        public ushort                       chprop;
        public ushort                       gindex;
        public uint                         gprop;
    }


    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct LsGlyphRunInfo
    {
        public Plsrun                       plsrun;
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public char*                        pwch;           // array of character codes
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public ushort*                      rggmap;         // wchar->glyph mapping
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public ushort*                       rgchprop;       // array of char properties as returned by GetGlyphs
        public int                          cwch;           // number of characters
        public int                          duChangeRight;  // nominal-to-ideal changes on the right side of the run
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public ushort*                      rggindex;       // glyph indices
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public uint*                        rggprop;        // array of glyph properties as returned by GetGlyphs
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public int*                         rgduWidth;      // array of glyph widths as returned by GetGlypPositions
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public GlyphOffset*                 rggoffset;      // array of glyph offset as returned by GetGlypPositions
        public int                          cgindex;        // number of glyphs
    }


    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct LsCharRunInfo
    {
        public Plsrun   plsrun;
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public char*    pwch;                   // array of character codes
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public int*     rgduNominalWidth;       // array of nominal widths
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public int*     rgduChangeLeft;         // array of nominal-to-ideal changes on the left side
        
        /// <SecurityNote>
        ///   Critical : Native pointer
        /// <SecurityNote>
        [SecurityCritical]
        public int*     rgduChangeRight;        // array of nominal-to-ideal changes on the right side
        public int      cwch;                   // number of characters
    }



    //
    //  Custom object handler structures
    //
    //
    /// <SecurityNote>
    /// Critical - as this structure points to several callbacks that'll be
    ///            invoked from unmanaged code.  Marked all members Critical.
    ///            Just putting the SecurityNote on the top instead of on every
    ///            member.
    /// </SecurityNote>
    [StructLayout(LayoutKind.Sequential)]
    internal struct InlineInit
    {
        public uint             dwVersion;
        [SecurityCritical]
        public InlineFormat     pfnFormat;
        [SecurityCritical]
        public InlineDraw       pfnDraw;
    }


    /// <summary>
    /// Line Services computational constants
    /// </summary>
    internal static partial class Constants
    {
        public const double DefaultRealToIdeal = 28800.0 / 96;
        public const double DefaultIdealToReal = 1 / DefaultRealToIdeal;
        public const int    IdealInfiniteWidth = 0x3FFFFFFE;
        public const double RealInfiniteWidth = IdealInfiniteWidth * DefaultIdealToReal;

        // A reasonable maximum interword spacing for normal text is half an em
        // while a good average is a third of em. 
        // A reasonable minimum interword spacing for normal text is a fifth of em
        // while a good average is a quarter of em.
        //
        // [Robert Bringhurst "The Elements of Typographic Style" p.26]
        public const double MinInterWordCompressionPerEm = 0.2;
        public const double MaxInterWordExpansionPerEm = 0.5;

        // According to Knuth, the linebreak that results in a line with stretchability
        // 4.7 or greater is considered awful and should be thrown away from the optimal
        // calculation. 
        //
        // Stretchability (S) is the value proportion of the expandable excess width in
        // the line and the maximum expansion allowed. S <= 1 is considered a 'good'
        // linebreak. 1 < S < 4.7 is kinda bad but still considered acceptable, and 
        // S >= 4.7 is just plain awful and should never be chosen.
        //
        // PTLS allows for multiple layers of expansion insertion. We define the first
        // layer to be for 'good' break, which by definition max out at 1.0 stretchability. 
        // The second layout is defined as 'acceptable' break and max out at an addition of
        // 2.0. The third layer is defined as 'bad'. It is at this layer where we start to
        // distribute expansion between letters (inter-letter expansion). This layer max
        // out at infinity. The value defined below is the max out stretchability of the
        // second layer. This means we are saying - we'll start inter-letter distribution
        // for a break which yields the line with stretchability greater than 3.0. Thus, a
        // line with inter-letter expansion may be accepted within an optimal paragraph 
        // calculation only if it has stretchability >= 3.0 but < 4.7.
        //
        // So, just to give an idea of how difference a line with S = 1.0 and S = 4.7 is in 
        // Knuth's calculation; the badness of the line is the power of six of its stretchability 
        // value. Line with greater stretchability is significantly worse than one with smaller value.
        //
        // For more detail on the badness calculation; see the "Optimal Paragraph" design spec
        // Paragraph.doc.
        public const int AcceptableLineStretchability = 2;

        // Minimum number of characters before and after the current position to
        // be analyzed by the lexical service component such as hyphenation or 
        // word-break and cached so future call within the same character range 
        // can be done efficiently.
        //
        // Making these numbers too small would result in more cache miss during 
        // line-breaking. Making it too great wastes memory.
        public const int MinCchToCacheBeforeAndAfter = 16;
    }

    
    /// <summary>
    /// Helper functions to convert to and from LS enum values
    /// </summary>
    internal sealed class Convert
    {
        // Helper class not instantiable
        private Convert() {}

        /// <summary>
        /// From LsTFlow to FlowDirection
        /// </summary>
        public static FlowDirection LsTFlowToFlowDirection(LsTFlow lstflow)
        {
            switch (lstflow)
            {
                //case LsTFlow.lstflowDefault:
                case LsTFlow.lstflowES:
                case LsTFlow.lstflowEN:
                    return FlowDirection.LeftToRight;


                case LsTFlow.lstflowWS:
                case LsTFlow.lstflowWN:
                    return FlowDirection.RightToLeft;

                // vertical flow is not supported
                case LsTFlow.lstflowSE:
                case LsTFlow.lstflowSW:
                case LsTFlow.lstflowNE:
                case LsTFlow.lstflowNW:
                    break;
            }
            return FlowDirection.LeftToRight;
        }

        
        /// <summary>
        /// From TabAlignment to LsKTab
        /// </summary>
        public static LsKTab LsKTabFromTabAlignment(TextTabAlignment tabAlignment)
        {
            switch (tabAlignment)
            {
                case TextTabAlignment.Right:
                    return LsKTab.lsktRight;

                case TextTabAlignment.Center:
                    return LsKTab.lsktCenter;

                case TextTabAlignment.Character:
                    return LsKTab.lsktChar;
            }
            return LsKTab.lsktLeft;
        }
    }

    
    //
    //  Line Services exported functions
    //
    //

    /// <SecurityNote>
    /// 
    /// This is a meta security comment for all the native functions in this class.
    /// 
    /// Line services does not do any security critical operations as such.  All it does
    /// really is to compute the layout information.  What's unsecure is the design pattern
    /// that involves a chat'ty interface with unmanaged code.
    /// 
    /// All the native functions that call into the line services code take a context,
    /// usually PLOC, as a parameter.  This is opaque to the managed code and is handled
    /// as an IntPtr.  Since the memory pointed to by this parameter can be written to
    /// by the line services code, all these native methods are considered Unsafe.
    /// 
    /// There may be other parameters too that'll be written to directly by LS code
    /// but these will be appropriately marked Critical or validated when the function
    /// is called.
    /// 
    /// See PTSInPartialTrust.mht in the framework\ms\internal\ptshost directory that
    /// explains the design philosophy.  All of it applies to LineServices code also.
    /// 
    /// </SecurityNote>
    internal static class UnsafeNativeMethods
    {
        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoCreateContext")]
        internal static extern LsErr LoCreateContext(
            ref LsContextInfo               contextInfo,      // const
            ref LscbkRedefined              lscbkRedef,
            out IntPtr                      ploc
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoDestroyContext")]
        internal static extern LsErr LoDestroyContext(
            IntPtr                  ploc
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoCreateLine")]
        internal static extern LsErr LoCreateLine(
            IntPtr                  ploc,
            int                     cp,
            int                     ccpLim,
            int                     durColumn,
            uint                    dwLineFlags,
            IntPtr                  pInputBreakRec,
            out LsLInfo             plslinfo,
            out IntPtr              pploline,
            out int                 maxDepth,
            out LsLineWidths        lineWidths
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoDisposeLine")]
        internal static extern LsErr LoDisposeLine(
            IntPtr                  ploline,
            [MarshalAs(UnmanagedType.Bool)]
            bool                    finalizing
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoAcquireBreakRecord")]
        internal static extern LsErr LoAcquireBreakRecord(
            IntPtr                  ploline,
            out IntPtr              pbreakrec
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoDisposeBreakRecord")]
        internal static extern LsErr LoDisposeBreakRecord(
            IntPtr                  pBreakRec,
            [MarshalAs(UnmanagedType.Bool)]
            bool                    finalizing
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoCloneBreakRecord")]
        internal static extern LsErr LoCloneBreakRecord(
            IntPtr                  pBreakRec,
            out IntPtr              pBreakRecClone
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "LoRelievePenaltyResource")]
        internal static extern LsErr LoRelievePenaltyResource(
            IntPtr                  ploline
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoSetBreaking")]
        internal static extern LsErr LoSetBreaking(
            IntPtr                  ploc,
            int                     strategy
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoSetDoc")]
        internal static extern LsErr LoSetDoc(
            IntPtr                  ploc,
            int                     isDisplay,
            int                     isReferencePresentationEqual,
            ref LsDevRes            deviceInfo
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoSetTabs")]
        internal static unsafe extern LsErr LoSetTabs(
            IntPtr                  ploc,
            int                     durIncrementalTab,
            int                     tabCount,
            LsTbd*                  pTabs
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoDisplayLine")]
        internal static extern LsErr LoDisplayLine(
            IntPtr                  ploline,
            ref LSPOINT             pt,
            uint                    displayMode,
            ref LSRECT              clipRect
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoEnumLine")]
        internal static extern LsErr LoEnumLine(
            IntPtr                  ploline,
            bool                    reverseOder,
            bool                    fGeometryneeded,
            ref LSPOINT             pt
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoQueryLineCpPpoint")]
        internal static extern LsErr LoQueryLineCpPpoint(
            IntPtr                  ploline,
            int                     lscpQuery,
            int                     depthQueryMax,
            IntPtr                  pSubLineInfo,   // passing raw pinned pointer for out array
            out int                 actualDepthQuery,
            out LsTextCell          lsTextCell
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoQueryLinePointPcp")]
        internal static extern LsErr LoQueryLinePointPcp(
            IntPtr                  ploline,
            ref LSPOINT             ptQuery,        //  use POINT as POINTUV
            int                     depthQueryMax,
            IntPtr                  pSubLineInfo,   // passing raw pinned pointer for out array
            out int                 actualDepthQuery,
            out LsTextCell          lsTextCell
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoCreateBreaks")]
        internal static extern LsErr LoCreateBreaks(
            IntPtr                  ploc,               // Line Services context
            int                     cpFirst,
            IntPtr                  previousBreakRecord,
            IntPtr                  ploparabreak,
            IntPtr                  ptslinevariantRestriction,
            ref LsBreaks            lsbreaks,
            out int                 bestFitIndex            
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoCreateParaBreakingSession")]
        internal static extern LsErr LoCreateParaBreakingSession(
            IntPtr                  ploc,               // Line Services context
            int                     cpParagraphFirst,
            int                     maxWidth,
            IntPtr                  previousParaBreakRecord,
            ref IntPtr              pploparabreak,
            [MarshalAs(UnmanagedType.Bool)]
            ref bool                fParagraphJustified
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LoDisposeParaBreakingSession")]
        internal static extern LsErr LoDisposeParaBreakingSession(
            IntPtr                  ploparabreak,
            [MarshalAs(UnmanagedType.Bool)]
            bool                    finalizing
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint="LocbkGetObjectHandlerInfo")]
        internal unsafe static extern LsErr LocbkGetObjectHandlerInfo(
            IntPtr                  ploc,               // Line Services context
            uint                    objectId,           // installed object id
            void*                   objectInfo          // object handler info
            );

        ///<SecurityNote>
        /// Critical - as this calls a function with SUC on it.
        /// Safe - as this just fills a data structure.
        /// 
        /// We need to do this wrapper for the TAS to work correctly with enforcement.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void LoGetEscString(
            ref EscStringInfo escStringInfo)
        {
            LoGetEscStringImpl(ref escStringInfo);
        }

        ///<SecurityNote>
        /// Critical - as this code performs an elevation.
        ///</SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "LoGetEscString")]
        private static extern void LoGetEscStringImpl(
            ref EscStringInfo escStringInfo
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "LoAcquirePenaltyModule")]
        internal static extern LsErr LoAcquirePenaltyModule(
            IntPtr                  ploc,       // Line Services context
            out IntPtr              penaltyModuleHandle
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "LoDisposePenaltyModule")]
        internal static extern LsErr LoDisposePenaltyModule(
            IntPtr                  penaltyModuleHandle
            );

        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "LoGetPenaltyModuleInternalHandle")]
        internal static extern LsErr LoGetPenaltyModuleInternalHandle(
            IntPtr                  penaltyModuleHandle,
            out IntPtr              penaltyModuleInternalHandle
            );

        /// <summary>
        /// This method creates an object that implements IDWriteTextAnalysisSink that is defined in PresentationNative*.dll.
        /// </summary>
        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "CreateTextAnalysisSink")]
        internal unsafe static extern void* CreateTextAnalysisSink();

        /// <summary>
        /// This method is passed the IDWriteTextAnalysisSink object we get using CreateTextAnalysisSink to retrieve
        /// the results from analyzing the scripts.
        /// </summary>
        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "GetScriptAnalysisList")]
        internal unsafe static extern void* GetScriptAnalysisList(void* textAnalysisSink);

        /// <summary>
        /// This method is passed the IDWriteTextAnalysisSink object we get using CreateTextAnalysisSink to retrieve
        /// the results from analyzing the number substitution.
        /// </summary>
        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "GetNumberSubstitutionList")]
        internal unsafe static extern void* GetNumberSubstitutionList(void* textAnalysisSink);

        /// <summary>
        /// This method creates an object that implements IDWriteTextAnalysiSource that is defined in PresentationNative*.dll.
        /// </summary>
        /// <SecurityNote>
        /// Critical - as there is an elevation - SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity, DllImport(DllImport.PresentationNative, EntryPoint = "CreateTextAnalysisSource")]
        internal unsafe static extern int CreateTextAnalysisSource(char* text,
                                                                   uint    length,
                                                                   char*   culture,
                                                                   void*   factory,
                                                                   bool    isRightToLeft,
                                                                   char*   numberCulture,
                                                                   bool    ignoreUserOverride,
                                                                   uint    numberSubstitutionMethod,
                                                                   void**  ppTextAnalysisSource);
    }
}
