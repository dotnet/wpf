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
// Description: Provides PTS callbacks implementation / forwarding. 
//
#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol
                                    
#pragma warning disable 6500        // Specifically disable warning about unhandled null reference and SEH exceptions.

using System;                                   // IntPtr
using System.Threading;                         // Monitor
using System.Diagnostics;
using System.Security;                          // SecurityCritical
using System.Windows;
using System.Windows.Documents; 
using System.Windows.Media.TextFormatting;
using MS.Internal.Text;
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS
using System.Collections.Generic;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // PtsHost class:
    // (1) Provides PTS callbacks implementation / forwarding mechanism.
    // (2) Stores PTS Context.
    // ----------------------------------------------------------------------
    internal sealed class PtsHost
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal PtsHost()
        {
            _context = new SecurityCriticalDataForSet<IntPtr>(IntPtr.Zero);
        }

        // ------------------------------------------------------------------
        // PTS Context; required for callbacks.
        // ------------------------------------------------------------------
        internal void EnterContext(PtsContext ptsContext)
        {
            Invariant.Assert(_ptsContext == null);
            _ptsContext = ptsContext;
        }
        internal void LeaveContext(PtsContext ptsContext)
        {
            Invariant.Assert(_ptsContext == ptsContext);
            _ptsContext = null;
        }
        private PtsContext PtsContext
        {
            get { Invariant.Assert(_ptsContext != null); return _ptsContext; }
        }
        private PtsContext _ptsContext;

        // ------------------------------------------------------------------
        // PTS Context Id; required to call PTS APIs.
        // ------------------------------------------------------------------
        internal IntPtr Context
        {
            get { Invariant.Assert(_context.Value != IntPtr.Zero); return _context.Value; }
            set { Invariant.Assert(_context.Value == IntPtr.Zero); _context.Value = value; }
        }
        private SecurityCriticalDataForSet<IntPtr> _context;

        // ------------------------------------------------------------------
        //  Container paragraph id.
        // ------------------------------------------------------------------
        internal static int ContainerParagraphId { get { return _customParaId; } }

        // ------------------------------------------------------------------
        //  Subpage paragraph id.
        // ------------------------------------------------------------------
        internal static int SubpageParagraphId { get { return ContainerParagraphId + 1; } }

        // ------------------------------------------------------------------
        //  UIElement paragraph id.
        // ------------------------------------------------------------------
        internal static int FloaterParagraphId { get { return SubpageParagraphId + 1; } }

        // ------------------------------------------------------------------
        //  Table paragraph id.
        // ------------------------------------------------------------------
        internal static int TableParagraphId { get { return FloaterParagraphId + 1; } }

        // ------------------------------------------------------------------
        // Following number is used to create handle to object context.
        // This is random number (doesn't mean anything). Since ObjectContext
        // is not really created by our PTS host, it is good enough for now.
        // ------------------------------------------------------------------
        private static int _objectContextOffset = 10;

        // ------------------------------------------------------------------
        // Custom paragraph id.
        // Since PTS requires ids of custom paragraph starting from 0, it is 
        // set to 0. All custom paragraph are using subsequent numbers.
        // Do not change this number.
        // ------------------------------------------------------------------
        private static int _customParaId = 0;

        #region PTS callbacks

        // ------------------------------------------------------------------
        // Debugging callbacks
        // ------------------------------------------------------------------
        internal void AssertFailed(
            string arg1,                        // IN:
            string arg2,                        // IN:
            int arg3,                           // IN:
            uint arg4)                          // IN:
        {
            // PTS has some known memory leaks in quick heap. Those issues are not 
            // going to be fixed for V1. Because of those memory leaks, PTS asserts 
            // in debug mode. To workaround this problem, we are ignoring asserts 
            // during shutdown.
            if (!PtsCache.IsDisposed())
            {
                ErrorHandler.Assert(false, ErrorHandler.PTSAssert, arg1, arg2, arg3, arg4);
            }
        }
        // ------------------------------------------------------------------
        // Figure callbacks
        // ------------------------------------------------------------------
        internal int GetFigureProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr nmpFigure,                   // IN:  figure's name
            int fInTextLine,                    // IN:  it is attached to text line
            uint fswdir,                        // IN:  current direction
            int fBottomUndefined,               // IN:  bottom of page is not defined
            out int dur,                        // OUT: width of figure
            out int dvr,                        // OUT: height of figure
            out PTS.FSFIGUREPROPS fsfigprops,   // OUT: figure attributes
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices,                  // OUT: total number of vertices in all polygons
            out int durDistTextLeft,            // OUT: distance to text from MinU side
            out int durDistTextRight,           // OUT: distance to text from MaxU side
            out int dvrDistTextTop,             // OUT: distance to text from MinV side
            out int dvrDistTextBottom)          // OUT: distance to text from MaxV side
        {
            int fserr = PTS.fserrNone;
            try
            {
                FigureParagraph para = PtsContext.HandleToObject(nmpFigure) as FigureParagraph;
                PTS.ValidateHandle(para);
                FigureParaClient paraClient = PtsContext.HandleToObject(pfsparaclientFigure) as FigureParaClient;
                PTS.ValidateHandle(paraClient);
                para.GetFigureProperties(paraClient, fInTextLine, fswdir, fBottomUndefined, out dur,
                    out dvr, out fsfigprops, out cPolygons, out cVertices, out durDistTextLeft,
                    out durDistTextRight, out dvrDistTextTop, out dvrDistTextBottom);
            }
            catch (Exception e)
            {
                dur = dvr = cPolygons = cVertices = 0; fsfigprops = new PTS.FSFIGUREPROPS();
                durDistTextLeft = durDistTextRight = dvrDistTextTop = dvrDistTextBottom = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dur = dvr = cPolygons = cVertices = 0; fsfigprops = new PTS.FSFIGUREPROPS();
                durDistTextLeft = durDistTextRight = dvrDistTextTop = dvrDistTextBottom = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }

            return fserr;
        }
        internal unsafe int GetFigurePolygons(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr nmpFigure,                   // IN:  figure's name
            uint fswdir,                        // IN:  current direction
            int ncVertices,                     // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                          // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            PTS.FSPOINT* rgfspt,                // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough)               // OUT: fill text in empty areas within obstacles?
        {
            int fserr = PTS.fserrNone;
            try
            {
                FigureParagraph para = PtsContext.HandleToObject(nmpFigure) as FigureParagraph;
                PTS.ValidateHandle(para);
                FigureParaClient paraClient = PtsContext.HandleToObject(pfsparaclientFigure) as FigureParaClient;
                PTS.ValidateHandle(paraClient);
                para.GetFigurePolygons(paraClient, fswdir, ncVertices, nfspt, rgcVertices, out ccVertices, 
                    rgfspt, out cfspt, out fWrapThrough);
            }
            catch (Exception e)
            {
                ccVertices = cfspt = fWrapThrough = 0; rgfspt = null;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch 
            {
                ccVertices = cfspt = fWrapThrough = 0; rgfspt = null;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }

            return fserr;
        }
        internal int CalcFigurePosition(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr nmpFigure,                   // IN:  figure's name
            uint fswdir,                        // IN:  current direction
            ref PTS.FSRECT fsrcPage,            // IN:  page rectangle
            ref PTS.FSRECT fsrcMargin,          // IN:  rectangle within page margins
            ref PTS.FSRECT fsrcTrack,           // IN:  track rectangle
            ref PTS.FSRECT fsrcFigurePreliminary,// IN:  prelim figure rect calculated from figure props
            int fMustPosition,                  // IN:  must find position in this track?
            int fInTextLine,                    // IN:  it is attached to text line
            out int fPushToNextTrack,           // OUT: push to next track?
            out PTS.FSRECT fsrcFlow,            // OUT: FlowAround rectangle
            out PTS.FSRECT fsrcOverlap,         // OUT: Overlap rectangle
            out PTS.FSBBOX fsbbox,              // OUT: bbox
            out PTS.FSRECT fsrcSearch)          // OUT: search area for overlap
        {
            int fserr = PTS.fserrNone;
            try
            {
                FigureParagraph para = PtsContext.HandleToObject(nmpFigure) as FigureParagraph;
                PTS.ValidateHandle(para);
                FigureParaClient paraClient = PtsContext.HandleToObject(pfsparaclientFigure) as FigureParaClient;
                PTS.ValidateHandle(paraClient);
                para.CalcFigurePosition(paraClient, fswdir, ref fsrcPage, ref fsrcMargin, ref fsrcTrack, 
                    ref fsrcFigurePreliminary, fMustPosition, fInTextLine, out fPushToNextTrack, out fsrcFlow, 
                    out fsrcOverlap, out fsbbox, out fsrcSearch);
            }
            catch (Exception e)
            {
                fPushToNextTrack = 0; fsrcFlow = fsrcOverlap = fsrcSearch = new PTS.FSRECT(); fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fPushToNextTrack = 0; fsrcFlow = fsrcOverlap = fsrcSearch = new PTS.FSRECT(); fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }

            return fserr;
        }
        // ------------------------------------------------------------------
        // Generic callbacks
        // ------------------------------------------------------------------
        internal int FSkipPage(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of first section on the page
            out int fSkip)                      // OUT: skip it due to odd/even page issue
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nms) as Section;
                PTS.ValidateHandle(section);
                section.FSkipPage(out fSkip);
            }
            catch (Exception e)
            {
                fSkip = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fSkip = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetPageDimensions(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section on page
            out uint fswdir,                    // OUT: direction of main text
            out int fHeaderFooterAtTopBottom,   // OUT: header/footer position on the page
            out int durPage,                    // OUT: page width
            out int dvrPage,                    // OUT: page height
            ref PTS.FSRECT fsrcMargin)          // OUT: rectangle within page margins
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nms) as Section;
                PTS.ValidateHandle(section);
                section.GetPageDimensions(out fswdir, out fHeaderFooterAtTopBottom, 
                    out durPage, out dvrPage, ref fsrcMargin);
            }
            catch (Exception e)
            {
                fswdir = 0; fHeaderFooterAtTopBottom = durPage = dvrPage = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fswdir = 0; fHeaderFooterAtTopBottom = durPage = dvrPage = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetNextSection(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsCur,                      // IN:  name of current section
            out int fSuccess,                   // OUT: next section exists
            out IntPtr nmsNext)                 // OUT: name of the next section
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nmsCur) as Section;
                PTS.ValidateHandle(section);
                section.GetNextSection(out fSuccess, out nmsNext);
            }
            catch (Exception e)
            {
                fSuccess = 0; nmsNext = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fSuccess = 0; nmsNext = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetSectionProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int fNewPage,                   // OUT: stop page before this section?
            out uint fswdir,                    // OUT: direction of this section
            out int fApplyColumnBalancing,      // OUT: apply column balancing to this section?
            out int ccol,                       // OUT: number of columns in the main text segment
            out int cSegmentDefinedColumnSpanAreas, // OUT:
            out int cHeightDefinedColumnSpanAreas)  // OUT:
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nms) as Section;
                PTS.ValidateHandle(section);
                section.GetSectionProperties(out fNewPage, out fswdir, out fApplyColumnBalancing, out ccol,
                    out cSegmentDefinedColumnSpanAreas, out cHeightDefinedColumnSpanAreas);
            }
            catch (Exception e)
            {
                fNewPage = fApplyColumnBalancing = ccol = 0; fswdir = 0;
                cSegmentDefinedColumnSpanAreas = cHeightDefinedColumnSpanAreas = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fNewPage = fApplyColumnBalancing = ccol = 0; fswdir = 0;
                cSegmentDefinedColumnSpanAreas = cHeightDefinedColumnSpanAreas = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal unsafe int GetJustificationProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr* rgnms,                      // IN:  array of the section names on the page
            int cnms,                           // IN:  number of sections on the page
            int fLastSectionNotBroken,          // IN:  is last section on the page broken?
            out int fJustify,                   // OUT: apply justification/alignment to the page?
            out PTS.FSKALIGNPAGE fskal,         // OUT: kind of vertical alignment for the page
            out int fCancelAtLastColumn)        // OUT: cancel justification for the last column of the page?
        {
            Debug.Assert(cnms == 1); // Only one section is supported right now.
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(rgnms[0]) as Section;
                PTS.ValidateHandle(section);
                section.GetJustificationProperties(rgnms, cnms, fLastSectionNotBroken,
                    out fJustify, out fskal, out fCancelAtLastColumn);
            }
            catch (Exception e)
            {
                fJustify = fCancelAtLastColumn = 0; 
                fskal = default(PTS.FSKALIGNPAGE);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fJustify = fCancelAtLastColumn = 0; 
                fskal = default(PTS.FSKALIGNPAGE);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetMainTextSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out IntPtr nmSegment)               // OUT: name of the main text segment for this section
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nmsSection) as Section;
                PTS.ValidateHandle(section);
                section.GetMainTextSegment(out nmSegment);
            }
            catch (Exception e)
            {
                nmSegment = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                nmSegment = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int GetHeaderSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            IntPtr pfsbrpagePrelim,             // IN:  ptr to page break record of main page
            uint fswdir,                        // IN:  direction for dvrMaxHeight/dvrFromEdge
            out int fHeaderPresent,             // OUT: is there header on this page?
            out int fHardMargin,                // OUT: does margin increase with header?
            out int dvrMaxHeight,               // OUT: maximum size of header
            out int dvrFromEdge,                // OUT: distance from top edge of the paper
            out uint fswdirHeader,              // OUT: direction for header
            out IntPtr nmsHeader)               // OUT: name of header segment
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nms) as Section;
                PTS.ValidateHandle(section);
                section.GetHeaderSegment(pfsbrpagePrelim, fswdir, out fHeaderPresent, out fHardMargin, 
                    out dvrMaxHeight, out dvrFromEdge, out fswdirHeader, out nmsHeader);
            }
            catch (Exception e)
            {
                fHeaderPresent = fHardMargin = dvrMaxHeight = dvrFromEdge = 0; fswdirHeader = 0; nmsHeader = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fHeaderPresent = fHardMargin = dvrMaxHeight = dvrFromEdge = 0; fswdirHeader = 0; nmsHeader = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int GetFooterSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            IntPtr pfsbrpagePrelim,             // IN:  ptr to page break record of main page
            uint fswdir,                        // IN:  direction for dvrMaxHeight/dvrFromEdge
            out int fFooterPresent,             // OUT: is there footer on this page?
            out int fHardMargin,                // OUT: does margin increase with footer?
            out int dvrMaxHeight,               // OUT: maximum size of footer
            out int dvrFromEdge,                // OUT: distance from bottom edge of the paper
            out uint fswdirFooter,              // OUT: direction for footer
            out IntPtr nmsFooter)               // OUT: name of footer segment
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nms) as Section;
                PTS.ValidateHandle(section);
                section.GetFooterSegment(pfsbrpagePrelim, fswdir, out fFooterPresent, out fHardMargin, 
                    out dvrMaxHeight, out dvrFromEdge, out fswdirFooter, out nmsFooter);
            }
            catch (Exception e)
            {
                fFooterPresent = fHardMargin = dvrMaxHeight = dvrFromEdge = 0; fswdirFooter = 0; nmsFooter = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFooterPresent = fHardMargin = dvrMaxHeight = dvrFromEdge = 0; fswdirFooter = 0; nmsFooter = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdGetSegmentChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of the segment
            out PTS.FSKCHANGE fskch)            // OUT: kind of change
        {
            int fserr = PTS.fserrNone;
            try
            {
                ContainerParagraph segment = PtsContext.HandleToObject(nms) as ContainerParagraph;
                PTS.ValidateHandle(segment);
                segment.UpdGetSegmentChange(out fskch);
            }
            catch (Exception e)
            {
                fskch = default(PTS.FSKCHANGE);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fskch = default(PTS.FSKCHANGE);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal unsafe int GetSectionColumnInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            uint fswdir,                        // IN:  direction of section
            int ncol,                           // IN:  size of the preallocated fscolinfo array
            PTS.FSCOLUMNINFO* fscolinfo,        // OUT: array of the colinfo structures
            out int ccol)                       // OUT: actual number of the columns in the segment
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nms) as Section;
                PTS.ValidateHandle(section);
                section.GetSectionColumnInfo(fswdir, ncol, fscolinfo, out ccol);
            }
            catch (Exception e)
            {
                ccol = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                ccol = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal unsafe int GetSegmentDefinedColumnSpanAreaInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            int cAreas,                         // IN:  number of areas - size of pre-allocated arrays
            IntPtr* rgnmSeg,                    // OUT: array of segment names for segment-defined areas
            int* rgcColumns,                    // OUT: arrays of number of columns spanned
            out int cAreasActual)               // OUT: actual number of segment-defined areas
        {
            Debug.Assert(false, "PTS.GetSegmentDefinedColumnSpanAreaInfo is not implemented.");
            cAreasActual = 0;
            return PTS.fserrNotImplemented;
        }
        internal unsafe int GetHeightDefinedColumnSpanAreaInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            int cAreas,                         // IN:  number of areas - size of pre-allocated arrays
            int* rgdvrAreaHeight,               // OUT: array of segment names for height-defined areas
            int* rgcColumns,                    // OUT: arrays of number of columns spanned
            out int cAreasActual)               // OUT: actual number of height-defined areas
        {
            Debug.Assert(false, "PTS.GetHeightDefinedColumnSpanAreaInfo is not implemented.");
            cAreasActual = 0;
            return PTS.fserrNotImplemented;
        }

        internal int GetFirstPara(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of segment
            out int fSuccessful,                // OUT: does segment contain any paragraph?
            out IntPtr nmp)                     // OUT: name of the first paragraph in segment
        {
            int fserr = PTS.fserrNone;
            try
            {
                ISegment segment = PtsContext.HandleToObject(nms) as ISegment;
                PTS.ValidateHandle((object)segment);
                segment.GetFirstPara(out fSuccessful, out nmp);
            }
            catch (Exception e)
            {
                fSuccessful = 0; nmp = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fSuccessful = 0; nmp = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetNextPara(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of segment
            IntPtr nmpCur,                      // IN:  name of current para
            out int fFound,                     // OUT: is there next paragraph?
            out IntPtr nmpNext)                 // OUT: name of the next paragraph in section
        {
            int fserr = PTS.fserrNone;
            try
            {
                ISegment segment = PtsContext.HandleToObject(nms) as ISegment;
                PTS.ValidateHandle((object)segment);
                BaseParagraph currentParagraph = PtsContext.HandleToObject(nmpCur) as BaseParagraph;
                PTS.ValidateHandle(currentParagraph);
                segment.GetNextPara(currentParagraph, out fFound, out nmpNext);
            }
            catch (Exception e)
            {
                fFound = 0; nmpNext = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0; nmpNext = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdGetFirstChangeInSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of segment
            out int fFound,                     // OUT: anything changed?
            out int fChangeFirst,               // OUT: first paragraph changed?
            out IntPtr nmpBeforeChange)         // OUT: name of paragraph before the change if !fChangeFirst
        {
            int fserr = PTS.fserrNone;
            try
            {
                ISegment segment = PtsContext.HandleToObject(nms) as ISegment;
                PTS.ValidateHandle(segment);
                segment.UpdGetFirstChangeInSegment(out fFound, out fChangeFirst, out nmpBeforeChange);
            }
            catch (Exception e)
            {
                fFound = fChangeFirst = 0; nmpBeforeChange = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = fChangeFirst = 0; nmpBeforeChange = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdGetParaChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of the paragraph
            out PTS.FSKCHANGE fskch,            // OUT: kind of change
            out int fNoFurtherChanges)          // OUT: no changes after?
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParagraph para = PtsContext.HandleToObject(nmp) as BaseParagraph;
                PTS.ValidateHandle(para);
                para.UpdGetParaChange(out fskch, out fNoFurtherChanges);
            }
            catch (Exception e)
            {
                fskch = default(PTS.FSKCHANGE);
                fNoFurtherChanges = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fskch = default(PTS.FSKCHANGE);
                fNoFurtherChanges = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetParaProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            ref PTS.FSPAP fspap)                // OUT: paragraph properties
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParagraph para = PtsContext.HandleToObject(nmp) as BaseParagraph;
                PTS.ValidateHandle(para);
                para.GetParaProperties(ref fspap);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int CreateParaclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            out IntPtr pfsparaclient)           // OUT: opaque to PTS paragraph client
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParagraph para = PtsContext.HandleToObject(nmp) as BaseParagraph;
                PTS.ValidateHandle(para);
                para.CreateParaclient(out pfsparaclient);
            }
            catch (Exception e)
            {
                pfsparaclient = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pfsparaclient = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int TransferDisplayInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclientOld,            // IN:  opaque to PTS old paragraph client
            IntPtr pfsparaclientNew)            // IN:  opaque to PTS new paragraph client
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParaClient paraClientOld = PtsContext.HandleToObject(pfsparaclientOld) as BaseParaClient;
                PTS.ValidateHandle(paraClientOld);
                BaseParaClient paraClientNew = PtsContext.HandleToObject(pfsparaclientNew) as BaseParaClient;
                PTS.ValidateHandle(paraClientNew);
                paraClientNew.TransferDisplayInfo(paraClientOld);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int DestroyParaclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient)               // IN:
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);
                paraClient.Dispose();
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int FInterruptFormattingAfterPara(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:  opaque to PTS paragraph client
            IntPtr nmp,                         // IN:  name of paragraph
            int vr,                             // IN:  current v position
            out int fInterruptFormatting)       // OUT: is it time to stop formatting?
        {
            fInterruptFormatting = PTS.False;
            return PTS.fserrNone;
        }
        internal int GetEndnoteSeparators(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out IntPtr nmsEndnoteSeparator,     // OUT: name of the endnote separator segment
            out IntPtr nmsEndnoteContSeparator, // OUT: name of endnote cont separator segment
            out IntPtr nmsEndnoteContNotice)    // OUT: name of the endnote cont notice segment
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nmsSection) as Section;
                PTS.ValidateHandle(section);
                section.GetEndnoteSeparators(out nmsEndnoteSeparator, out nmsEndnoteContSeparator, out nmsEndnoteContNotice);
            }
            catch (Exception e)
            {
                nmsEndnoteSeparator = nmsEndnoteContSeparator = nmsEndnoteContNotice = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                nmsEndnoteSeparator = nmsEndnoteContSeparator = nmsEndnoteContNotice = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetEndnoteSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out int fEndnotesPresent,           // OUT: are there endnotes for this segment?
            out IntPtr nmsEndnotes)             // OUT: name of endnote segment
        {
            int fserr = PTS.fserrNone;
            try
            {
                Section section = PtsContext.HandleToObject(nmsSection) as Section;
                PTS.ValidateHandle(section);
                section.GetEndnoteSegment(out fEndnotesPresent, out nmsEndnotes);
            }
            catch (Exception e)
            {
                fEndnotesPresent = 0; nmsEndnotes = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fEndnotesPresent = 0; nmsEndnotes = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetNumberEndnoteColumns(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int ccolEndnote)                // OUT: number of columns in endnote area
        {
            Debug.Assert(false, "PTS.GetNumberEndnoteColumns is not implemented.");
            ccolEndnote = 0;
            return PTS.fserrNotImplemented;
        }
        internal unsafe int GetEndnoteColumnInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            uint fswdir,                        // IN:  direction of section
            int ncolEndnote,                    // IN:  size of preallocated fscolinfo array
            PTS.FSCOLUMNINFO* fscolinfoEndnote, // OUT: array of the colinfo structures
            out int ccolEndnote)                // OUT: actual number of the columns in footnote area
        {
            Debug.Assert(false, "PTS.GetEndnoteColumnInfo is not implemented.");
            ccolEndnote = 0;
            return PTS.fserrNotImplemented;
        }
        internal int GetFootnoteSeparators(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmsSection,                  // IN:  name of section
            out IntPtr nmsFtnSeparator,         // OUT: name of the footnote separator segment
            out IntPtr nmsFtnContSeparator,     // OUT: name of the ftn cont separator segment
            out IntPtr nmsFtnContNotice)        // OUT: name of the footnote cont notice segment
        {
            Debug.Assert(false, "PTS.GetFootnoteSeparators is not implemented.");
            nmsFtnSeparator = nmsFtnContSeparator = nmsFtnContNotice = IntPtr.Zero;
            return PTS.fserrNotImplemented;
        }
        internal int FFootnoteBeneathText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int fFootnoteBeneathText)       // OUT: position footnote right after text?
        {
            Debug.Assert(false, "PTS.FFootnoteBeneathText is not implemented.");
            fFootnoteBeneathText = 0;
            return PTS.fserrNotImplemented;
        }
        internal int GetNumberFootnoteColumns(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            out int ccolFootnote)               // OUT: number of columns in footnote area
        {
            Debug.Assert(false, "PTS.GetNumberFootnoteColumns is not implemented.");
            ccolFootnote = 0;
            return PTS.fserrNotImplemented;
        }
        internal unsafe int GetFootnoteColumnInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nms,                         // IN:  name of section
            uint fswdir,                        // IN:  direction of main text
            int ncolFootnote,                   // IN:  size of preallocated fscolinfo array
            PTS.FSCOLUMNINFO* fscolinfoFootnote,// OUT: array of the colinfo structures
            out int ccolFootnote)               // OUT: actual number of the columns in footnote area
        {
            Debug.Assert(false, "PTS.GetFootnoteColumnInfo is not implemented.");
            ccolFootnote = 0;
            return PTS.fserrNotImplemented;
        }
        internal int GetFootnoteSegment(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmftn,                       // IN:  name of footnote
            out IntPtr nmsFootnote)             // OUT: name of footnote segment
        {
            Debug.Assert(false, "PTS.GetFootnoteSegment is not implemented.");
            nmsFootnote = IntPtr.Zero;
            return PTS.fserrNotImplemented;
        }
        internal unsafe int GetFootnotePresentationAndRejectionOrder(
            IntPtr pfsclient,                           // IN:  client opaque data
            int cFootnotes,                             // IN:  size of all arrays
            IntPtr* rgProposedPresentationOrder,        // IN:  footnotes in proposed pres order
            IntPtr* rgProposedRejectionOrder,           // IN:  footnotes in proposed reject order
            out int fProposedPresentationOrderAccepted, // OUT: agree with proposed order?
            IntPtr* rgFinalPresentationOrder,           // OUT: footnotes in final pres order
            out int fProposedRejectionOrderAccepted,    // OUT: agree with proposed order?
            IntPtr* rgFinalRejectionOrder)              // OUT: footnotes in final reject order
        {
            Debug.Assert(false, "PTS.GetFootnotePresentationAndRejectionOrder is not implemented.");
            fProposedPresentationOrderAccepted = fProposedRejectionOrderAccepted = 0;
            return PTS.fserrNotImplemented;
        }
        internal int FAllowFootnoteSeparation(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmftn,                       // IN:  name of footnote
            out int fAllow)                     // OUT: allow separating footnote from its reference
        {
            Debug.Assert(false, "PTS.FAllowFootnoteSeparation is not implemented.");
            fAllow = 0;
            return PTS.fserrNotImplemented;
        }
        // ------------------------------------------------------------------
        // Object callbacks
        // ------------------------------------------------------------------
        internal int DuplicateMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pmcsclientIn,                // IN:  margin collapsing state
            out IntPtr pmcsclientNew)           // OUT: duplicated margin collapsing state
        {
            int fserr = PTS.fserrNone;
            try
            {
                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                PTS.ValidateHandle(mcs);
                pmcsclientNew  = mcs.Clone().Handle;
            }
            catch (Exception e)
            {
                pmcsclientNew = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pmcsclientNew = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int DestroyMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pmcsclient)                  // IN:  margin collapsing state to destroy
        {
            int fserr = PTS.fserrNone;
            try
            {
                MarginCollapsingState mcs = PtsContext.HandleToObject(pmcsclient) as MarginCollapsingState;
                PTS.ValidateHandle(mcs);
                mcs.Dispose();
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int FEqualMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pmcsclient1,                 // IN:  first margin collapsing state to compare
            IntPtr pmcsclient2,                 // IN:  second margin collapsing state to compare
            out int fEqual)                     // OUT: are MStructs equal?
        {
            int fserr = PTS.fserrNone;
            if (pmcsclient1 == IntPtr.Zero || pmcsclient2 == IntPtr.Zero)
            {
                fEqual = PTS.FromBoolean(pmcsclient1 == pmcsclient2);
            }
            else
            {
                try
                {
                    MarginCollapsingState mcs1 = PtsContext.HandleToObject(pmcsclient1) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs1);
                    MarginCollapsingState mcs2 = PtsContext.HandleToObject(pmcsclient2) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs2);
                    fEqual = PTS.FromBoolean(mcs1.IsEqual(mcs2));
                }
                catch (Exception e)
                {
                    fEqual = 0;
                    PtsContext.CallbackException = e;
                    fserr = PTS.fserrCallbackException;
                }
                catch
                {
                    fEqual = 0;
                    PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                    fserr = PTS.fserrCallbackException;
                }
            }
            return fserr;
        }
        internal int ConvertMcsclient(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            uint fswdir,                        // IN:  current direction
            IntPtr pmcsclient,                  // IN:  pointer to the input margin collapsing state
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of page
            out int dvr)                        // OUT: dvr, calculated based on margin collapsing state
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParagraph para = PtsContext.HandleToObject(nmp) as BaseParagraph;
                PTS.ValidateHandle(para);
                BaseParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclient != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclient) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.CollapseMargin(paraClient, mcs, fswdir, PTS.ToBoolean(fSuppressTopSpace), out dvr);
            }
            catch (Exception e)
            {
                dvr = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dvr = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetObjectHandlerInfo(
            IntPtr pfsclient,                   // IN:  client opaque data
            int idobj,                          // IN:  id of the object handler
            IntPtr pObjectInfo)                 // OUT: initialization information for the specified object
        {
            int fserr = PTS.fserrNone;
            try
            {
                if (idobj == PtsHost.FloaterParagraphId)
                {
                    PtsCache.GetFloaterHandlerInfo(this, pObjectInfo);
                }
                else if (idobj == PtsHost.TableParagraphId)
                {
                    PtsCache.GetTableObjHandlerInfo(this, pObjectInfo);
                }
                else
                {
                    pObjectInfo = IntPtr.Zero;
                }
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        // ------------------------------------------------------------------
        // Text callbacks
        // ------------------------------------------------------------------
        internal int CreateParaBreakingSession(
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
            out int fParagraphJustified)        // OUT: if paragraph is justified
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);

                TextParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as TextParaClient;
                PTS.ValidateHandle(paraClient);

                LineBreakRecord lineBreakRecord = null;
                if(pfsbreakreclineclient != IntPtr.Zero)
                {
                    lineBreakRecord = PtsContext.HandleToObject(pfsbreakreclineclient) as LineBreakRecord;
                    PTS.ValidateHandle(lineBreakRecord);
                }

                bool isParagraphJustified;
                OptimalBreakSession optimalBreakSession;

                para.CreateOptimalBreakSession(paraClient, fsdcpStart, durTrack, lineBreakRecord, out optimalBreakSession, out isParagraphJustified);

                fParagraphJustified = PTS.FromBoolean(isParagraphJustified);
                ppfsparabreakingsession = optimalBreakSession.Handle;                
            }
            catch (Exception e)
            {
                ppfsparabreakingsession = IntPtr.Zero;
                fParagraphJustified = PTS.False;

                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                ppfsparabreakingsession = IntPtr.Zero;
                fParagraphJustified = PTS.False;

                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int DestroyParaBreakingSession(
            IntPtr pfsclient,                   // IN:  client opaque data 
            IntPtr pfsparabreakingsession)      // IN:  session to destroy
        {
            int fserr = PTS.fserrNone;

            OptimalBreakSession optimalBreakSession = PtsContext.HandleToObject(pfsparabreakingsession) as OptimalBreakSession;
            PTS.ValidateHandle(optimalBreakSession);

            optimalBreakSession.Dispose();

            return fserr;
        }
        internal int GetTextProperties(
            IntPtr pfsclient,                   // IN:  client opaque data 
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            ref PTS.FSTXTPROPS fstxtprops)      // OUT: text paragraph properties
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                para.GetTextProperties(iArea, ref fstxtprops);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetNumberFootnotes(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int fsdcpStart,                     // IN:  dcp at the beginning of the range
            int fsdcpLim,                       // IN:  dcp at the end of the range
            out int nFootnote)                  // OUT: number of footnote references in the range
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                para.GetNumberFootnotes(fsdcpStart, fsdcpLim, out nFootnote);
            }
            catch (Exception e)
            {
                nFootnote = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                nFootnote = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal unsafe int GetFootnotes(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int fsdcpStart,                     // IN:  dcp at the beginning of the range
            int fsdcpLim,                       // IN:  dcp at the end of the range
            int nFootnotes,                     // IN:  size of the output array
            IntPtr* rgnmftn,                    // OUT: array of footnote names in the range
            int* rgdcp,                         // OUT: array of footnote refs in the range
            out int cFootnotes)                 // OUT: actual number of footnotes
        {
            Debug.Assert(false, "PTS.GetFootnotes is not implemented.");
            cFootnotes = 0;
            return PTS.fserrNotImplemented;
        }
        internal int FormatDropCap(
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
            out int durText)                    // OUT: distance from text
        {
            Debug.Assert(false, "PTS.FormatDropCap is not implemented.");
            pfsdropc = IntPtr.Zero;
            fInMargin = dur = dvr = cPolygons = cVertices = durText = 0;
            return PTS.fserrNotImplemented;
        }
        internal unsafe int GetDropCapPolygons(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsdropc,                    // IN:  pointer to drop cap
            IntPtr nmp,                         // IN:  para name
            uint fswdir,                        // IN:  current direction
            int  ncVertices,                    // IN:  size of array of vertex counts (= number of polygons)
            int  nfspt,                         // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            PTS.FSPOINT* rgfspt,                // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough)               // OUT: fill text in empty areas within obstacles?
        {
            Debug.Assert(false, "PTS.GetDropCapPolygons is not implemented.");
            ccVertices = cfspt = fWrapThrough = 0;
            return PTS.fserrNotImplemented;
        }
        internal int DestroyDropCap(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsdropc)                    // IN:  pointer to drop cap created by client
        {
            Debug.Assert(false, "PTS.DestroyDropCap is not implemented.");
            return PTS.fserrNotImplemented;
        }
        internal int FormatBottomText(IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int iArea,                          // IN:  column-span area index
            uint fswdir,                        // IN:  current direction
            IntPtr pfslineLast,                 // IN:  last formatted line
            int dvrLine,                        // IN:  height of last line
            out IntPtr pmcsclientOut)           // OUT: margin collapsing state at bottom of text
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                Line line = PtsContext.HandleToObject(pfslineLast) as Line;
                if(line != null)
                {
                    PTS.ValidateHandle(line);
                    para.FormatBottomText(iArea, fswdir, line, dvrLine, out pmcsclientOut);
                }
                else
                {
                    // Improve this to optimal implementation
                    Invariant.Assert(PtsContext.HandleToObject(pfslineLast) is LineBreakpoint);
                    pmcsclientOut = IntPtr.Zero;
                }
            }
            catch (Exception e)
            {
                pmcsclientOut = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pmcsclientOut = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int FormatLine(
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
            out PTS.FSFLRES fsflres,            // OUT: result of formatting
            out int dvrAscent,                  // OUT: ascent of the line
            out int dvrDescent,                 // OUT: descent of the line
            out int urBBox,                     // OUT: ur of the line's ink
            out int durBBox,                    // OUT: dur of of the line's ink
            out int dcpDepend,                  // OUT: number of chars after line break that were considered
            out int fReformatNeighborsAsLastLine)// OUT: should line segments be reformatted?
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                TextParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as TextParaClient;
                PTS.ValidateHandle(paraClient);
                para.FormatLine(paraClient, iArea, dcp, pbrlineIn, fswdir, urStartLine, durLine, urStartTrack, durTrack, urPageLeftMargin, 
                    PTS.ToBoolean(fAllowHyphenation), PTS.ToBoolean(fClearOnLeft), PTS.ToBoolean(fClearOnRight), PTS.ToBoolean(fTreatAsFirstInPara), PTS.ToBoolean(fTreatAsLastInPara), 
                    PTS.ToBoolean(fSuppressTopSpace), out pfsline, out dcpLine, out ppbrlineOut, out fForcedBroken, out fsflres, 
                    out dvrAscent, out dvrDescent, out urBBox, out durBBox,  out dcpDepend, 
                    out fReformatNeighborsAsLastLine);
            }
            catch (Exception e)
            {
                pfsline = ppbrlineOut = IntPtr.Zero; dcpLine = fForcedBroken = dvrAscent = dvrDescent = urBBox = durBBox = dcpDepend = fReformatNeighborsAsLastLine = 0; 
                fsflres = default(PTS.FSFLRES);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pfsline = ppbrlineOut = IntPtr.Zero; dcpLine = fForcedBroken = dvrAscent = dvrDescent = urBBox = durBBox = dcpDepend = fReformatNeighborsAsLastLine = 0; 
                fsflres = default(PTS.FSFLRES);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int FormatLineForced(
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
            out PTS.FSFLRES fsflres,            // OUT: result of formatting
            out int dvrAscent,                  // OUT: ascent of the line
            out int dvrDescent,                 // OUT: descent of the line
            out int urBBox,                     // OUT: ur of the line's ink
            out int durBBox,                    // OUT: dur of of the line's ink
            out int dcpDepend)                  // OUT: number of chars after line break that were considered
        {
            int fserr = PTS.fserrNone;
            try
            {
                int fForcedBrokenIgnore, fReformatNeighborsAsLastLineIgnore;
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                TextParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as TextParaClient;
                PTS.ValidateHandle(paraClient);
                para.FormatLine(paraClient, iArea, dcp, pbrlineIn, fswdir, urStartLine, durLine, urStartTrack, durTrack, urPageLeftMargin, 
                    true/*fAllowHyphenation*/, PTS.ToBoolean(fClearOnLeft), PTS.ToBoolean(fClearOnRight), PTS.ToBoolean(fTreatAsFirstInPara), PTS.ToBoolean(fTreatAsLastInPara), 
                    PTS.ToBoolean(fSuppressTopSpace), out pfsline, out dcpLine, out ppbrlineOut, out fForcedBrokenIgnore, out fsflres, 
                    out dvrAscent, out dvrDescent, out urBBox, out durBBox,  out dcpDepend, 
                    out fReformatNeighborsAsLastLineIgnore);
            }
            catch (Exception e)
            {
                pfsline = ppbrlineOut = IntPtr.Zero; dcpLine = dvrAscent = dvrDescent = urBBox = durBBox = dcpDepend = 0; 
                fsflres = default(PTS.FSFLRES);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pfsline = ppbrlineOut = IntPtr.Zero; dcpLine = dvrAscent = dvrDescent = urBBox = durBBox = dcpDepend = 0; 
                fsflres = default(PTS.FSFLRES);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal unsafe int FormatLineVariants(
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
            IntPtr lineVariantRestriction,      // IN:  line variant restriction pointer
            int nLineVariantsAlloc,             // IN:  size of the pre-allocated variant array
            PTS.FSLINEVARIANT* rgfslinevariant, // OUT: pre-allocatedarray for line variants
            out int nLineVariantsActual,        // OUT: actual number of variants'
            out int iLineVariantBest)           // OUT: best line variant index
        {
            int fserr = PTS.fserrNone;

            try
            {
                OptimalBreakSession optimalBreakSession = PtsContext.HandleToObject(pfsparabreakingsession) as OptimalBreakSession;
                PTS.ValidateHandle(optimalBreakSession);

                TextLineBreak textLineBreak = null;

                if(pbrlineIn != IntPtr.Zero)
                {
                    LineBreakRecord lineBreakRecord = PtsContext.HandleToObject(pbrlineIn) as LineBreakRecord;
                    PTS.ValidateHandle(lineBreakRecord);

                    textLineBreak = lineBreakRecord.TextLineBreak;
                }

                IList<TextBreakpoint> textBreakpoints;

                textBreakpoints = optimalBreakSession.TextParagraph.FormatLineVariants(optimalBreakSession.TextParaClient,                                                                                                            
                                                                                       optimalBreakSession.TextParagraphCache,
                                                                                       optimalBreakSession.OptimalTextSource,
                                                                                       dcp,
                                                                                       textLineBreak,
                                                                                       fswdir,
                                                                                       urStartLine,
                                                                                       durLine,
                                                                                       PTS.ToBoolean(fAllowHyphenation),
                                                                                       PTS.ToBoolean(fClearOnLeft),
                                                                                       PTS.ToBoolean(fClearOnRight),
                                                                                       PTS.ToBoolean(fTreatAsFirstInPara),
                                                                                       PTS.ToBoolean(fTreatAsLastInPara),
                                                                                       PTS.ToBoolean(fSuppressTopSpace),
                                                                                       lineVariantRestriction,
                                                                                       out iLineVariantBest
                                                                                       );

                for(int breakIndex = 0; breakIndex < Math.Min(textBreakpoints.Count, nLineVariantsAlloc); breakIndex++)
                {
                    TextBreakpoint textBreakpoint = textBreakpoints[breakIndex];

#pragma warning disable 56518
                    // Disable PRESharp warning 56518. LineBreakpoint is an UnmamangedHandle, that adds itself
                    // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
                    // calls Destroyline to get rid of it. Destroyline will call Dispose() on the object
                    // and remove it from HandleMapper.
                    LineBreakpoint lineBreakpoint = new LineBreakpoint(optimalBreakSession, textBreakpoint);
#pragma warning restore 56518

                    TextLineBreak textLineBreakOut = textBreakpoint.GetTextLineBreak();

                    if(textLineBreakOut != null)
                    {
#pragma warning disable 56518
                // Disable PRESharp warning 6518. Line is an UnmamangedHandle, that adds itself
                // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and
                // calls DestroyLineBreakRecord to get rid of it. DestroyLineBreakRecord will call Dispose() on the object
                // and remove it from HandleMapper.
                        LineBreakRecord lineBreakRecord = new LineBreakRecord(optimalBreakSession.PtsContext, textLineBreakOut);
#pragma warning disable 56518

                        rgfslinevariant[breakIndex].pfsbreakreclineclient = lineBreakRecord.Handle;
                    }
                    else
                    {
                        rgfslinevariant[breakIndex].pfsbreakreclineclient = IntPtr.Zero;
                    }                      

                    // Fill in line variants array.

                    int dvrAscent = TextDpi.ToTextDpi(textBreakpoint.Baseline);
                    int dvrDescent = TextDpi.ToTextDpi(textBreakpoint.Height - textBreakpoint.Baseline);

                    optimalBreakSession.TextParagraph.CalcLineAscentDescent(dcp, ref dvrAscent, ref dvrDescent);

                    rgfslinevariant[breakIndex].pfslineclient = lineBreakpoint.Handle;
                    rgfslinevariant[breakIndex].dcpLine      = textBreakpoint.Length;

                    rgfslinevariant[breakIndex].fForceBroken = PTS.FromBoolean(textBreakpoint.IsTruncated);
                    rgfslinevariant[breakIndex].fslres       = optimalBreakSession.OptimalTextSource.GetFormatResultForBreakpoint(dcp, textBreakpoint);
                    rgfslinevariant[breakIndex].dvrAscent    = dvrAscent;
                    rgfslinevariant[breakIndex].dvrDescent   = dvrDescent;
                    rgfslinevariant[breakIndex].fReformatNeighborsAsLastLine   = PTS.False;
                    rgfslinevariant[breakIndex].ptsLinePenaltyInfo = textBreakpoint.GetTextPenaltyResource().Value;
                }

                nLineVariantsActual = textBreakpoints.Count;
            }
            catch (Exception e)
            {
                nLineVariantsActual = 0;
                iLineVariantBest = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                nLineVariantsActual = 0;
                iLineVariantBest = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int ReconstructLineVariant(
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
            out PTS.FSFLRES fsflres,            // OUT: result of formatting
            out int dvrAscent,                  // OUT: ascent of the line
            out int dvrDescent,                 // OUT: descent of the line
            out int urBBox,                     // OUT: ur of the line's ink
            out int durBBox,                    // OUT: dur of of the line's ink
            out int dcpDepend,                  // OUT: number of chars after line break that were considered
            out int fReformatNeighborsAsLastLine)   // OUT: should line segments be reformatted?
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                TextParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as TextParaClient;
                PTS.ValidateHandle(paraClient);
                para.ReconstructLineVariant(paraClient, iArea, dcpStart, pbrlineIn, dcpLine, fswdir, urStartLine, durLine, urStartTrack, durTrack, urPageLeftMargin, 
                    PTS.ToBoolean(fAllowHyphenation), PTS.ToBoolean(fClearOnLeft), PTS.ToBoolean(fClearOnRight), PTS.ToBoolean(fTreatAsFirstInPara), PTS.ToBoolean(fTreatAsLastInPara), 
                    PTS.ToBoolean(fSuppressTopSpace), out pfsline, out dcpLine, out ppbrlineOut, out fForcedBroken, out fsflres, 
                    out dvrAscent, out dvrDescent, out urBBox, out durBBox,  out dcpDepend, 
                    out fReformatNeighborsAsLastLine);
            }
            catch (Exception e)
            {
                pfsline = ppbrlineOut = IntPtr.Zero; dcpLine = fForcedBroken = dvrAscent = dvrDescent = urBBox = durBBox = dcpDepend = fReformatNeighborsAsLastLine = 0; 
                fsflres = default(PTS.FSFLRES);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pfsline = ppbrlineOut = IntPtr.Zero; dcpLine = fForcedBroken = dvrAscent = dvrDescent = urBBox = durBBox = dcpDepend = fReformatNeighborsAsLastLine = 0; 
                fsflres = default(PTS.FSFLRES);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int DestroyLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsline)                     // IN:  pointer to line created by client
        {
            UnmanagedHandle unmanagedHandle = (UnmanagedHandle) PtsContext.HandleToObject(pfsline);
            unmanagedHandle.Dispose();

            return PTS.fserrNone;
        }
        internal int DuplicateLineBreakRecord(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pbrlineIn,                   // IN:  client's forced break record to duplicate
            out IntPtr pbrlineDup)              // OUT: duplicated client's forced break record
        {
            int fserr = PTS.fserrNone;
            try
            {
                LineBreakRecord lineBreakRecord = PtsContext.HandleToObject(pbrlineIn) as LineBreakRecord;
                PTS.ValidateHandle(lineBreakRecord);
                pbrlineDup  = lineBreakRecord.Clone().Handle;
            }
            catch (Exception e)
            {
                pbrlineDup = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pbrlineDup = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int DestroyLineBreakRecord(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pbrlineIn)                   // IN:  client's forced break record to duplicate
        {
            int fserr = PTS.fserrNone;
            try
            {
                LineBreakRecord lineBreakRecord = PtsContext.HandleToObject(pbrlineIn) as LineBreakRecord;
                PTS.ValidateHandle(lineBreakRecord);
                lineBreakRecord.Dispose();
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int SnapGridVertical(
            IntPtr pfsclient,                   // IN:  client opaque data
            uint fswdir,                        // IN:  current direction
            int vrMargin,                       // IN:  top margin
            int vrCurrent,                      // IN:  current vertical position
            out int vrNew)                      // OUT: snapped vertical position
        {
            Debug.Assert(false, "PTS.SnapGridVertical is not implemented.");
            vrNew = 0;
            return PTS.fserrNotImplemented;
        }
        internal int GetDvrSuppressibleBottomSpace(IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsline,                     // IN:  pointer to line created by client
            uint fswdir,                        // IN:  current direction
            out int dvrSuppressible)            // OUT: empty space suppressible at the bottom
        {
            int fserr = PTS.fserrNone;
            try
            {
                Line line = PtsContext.HandleToObject(pfsline) as Line;
                if(line != null)
                {
                    PTS.ValidateHandle(line);
                    line.GetDvrSuppressibleBottomSpace(out dvrSuppressible);
                }
                else
                {
                    // Optimal implementation of this? Should be called from real line client.
                    dvrSuppressible = 0;
                }
            }
            catch (Exception e)
            {
                dvrSuppressible = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dvrSuppressible = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetDvrAdvance(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int dcp,                            // IN:  dcp at the beginning of the line
            uint fswdir,                        // IN:  current direction
            out int dvr)                        // OUT: advance amount in tight wrap
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                para.GetDvrAdvance(dcp, fswdir, out dvr);
            }
            catch (Exception e)
            {
                dvr = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dvr = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdGetChangeInText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            out int dcpStart,                   // OUT: start of change
            out int ddcpOld,                    // OUT: number of chars in old range
            out int ddcpNew)                    // OUT: number of chars in new range
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                para.UpdGetChangeInText(out dcpStart, out ddcpOld, out ddcpNew);
            }
            catch (Exception e)
            {
                dcpStart = ddcpOld = ddcpNew = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dcpStart = ddcpOld = ddcpNew = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdGetDropCapChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            out int fChanged)                   // OUT: dropcap changed?
        {
            Debug.Assert(false, "PTS.UpdGetDropCapChange is not implemented.");
            fChanged = 0;
            return PTS.fserrNotImplemented;
        }
        internal int FInterruptFormattingText(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmp,                         // IN:  name of paragraph
            int dcp,                            // IN:  current dcp
            int vr,                             // IN:  current v position
            out int fInterruptFormatting)       // OUT: is it time to stop formatting?
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);
                fInterruptFormatting = PTS.FromBoolean(para.InterruptFormatting(dcp, vr));
            }
            catch (Exception e)
            {
                fInterruptFormatting = PTS.False;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fInterruptFormatting = PTS.False;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetTextParaCache(
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
            out PTS.FSKCLEAR fskclear,          // OUT: kclear after paragraph
            out IntPtr pmcsclientAfterPara,     // OUT: margin collapsing state after parag.
            out int cLines,                     // OUT: number of lines in the paragraph
            out int fOptimalLines,              // OUT: para had its lines optimized
            out int fOptimalLineDcpsCached,     // OUT: cached dcp's for lines available
            out int dvrMinLineHeight)           // OUT: minimal line height
        {
            // Paragraph Cache might be usefull in entire document repagination scenarios.
            // But it is not suitable for incremental update. Hence for now it is not used.
            fFound = PTS.False;
            dcpPara = urBBox = durBBox = dvrPara = cLines = dvrMinLineHeight = fOptimalLines = fOptimalLineDcpsCached = 0;
            pmcsclientAfterPara = IntPtr.Zero;
            fskclear = PTS.FSKCLEAR.fskclearNone;
            return PTS.fserrNone;
        }
        internal unsafe int SetTextParaCache(
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
            PTS.FSKCLEAR fskclear,              // IN:  kclear after paragraph
            IntPtr pmcsclientAfterPara,         // IN:  margin collapsing state after paragraph
            int cLines,                         // IN:  number of lines in the paragraph
            int fOptimalLines,                  // IN:  paragraph has its lines optinmized
            int* rgdcpOptimalLines,             // IN:  array of dcp's of optimal lines
            int dvrMinLineHeight)               // IN:  minimal line height
        {
            // Paragraph Cache might be usefull in entire document repagination scenarios.
            // But it is not suitable for incremental update. Hence for now it is not used.
            return PTS.fserrNone;
        }
        internal unsafe int GetOptimalLineDcpCache(
            IntPtr pfsclient,                   // IN:  client opaque data
            int cLines,                         // IN:  number of lines - size of pre-allocated array
            int* rgdcp)                         // OUT: array of dcp's to fill
        {
            Debug.Assert(false, "PTS.GetOptimalLineDcpCache is not implemented.");
            return PTS.fserrNotImplemented;
        }

        internal int GetNumberAttachedObjectsBeforeTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            out int cAttachedObjects)           // OUT: number of attached objects
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);

                int dcpLast = para.GetLastDcpAttachedObjectBeforeLine(dcpFirst);

                cAttachedObjects = para.GetAttachedObjectCount(dcpFirst, dcpLast);

                return PTS.fserrNone;
            }
            catch (Exception e)
            {
                cAttachedObjects = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                cAttachedObjects = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal unsafe int GetAttachedObjectsBeforeTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            int nAttachedObjects,               // IN:  size of the floater arrays
            IntPtr* rgnmpAttachedObject,        // OUT: array of floater names
            int* rgidobj,                       // OUT: array of idobj's of corresponding objects
            int* rgdcpAnchor,                   // OUT: array of dcp of the object's anchors
            out int cObjects,                   // OUT: actual number of floaters in the range
            out int fEndOfParagraph)            // OUT: paragraph ended after last object
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);

                int dcpLast = para.GetLastDcpAttachedObjectBeforeLine(dcpFirst);

                List<AttachedObject> attachedObjects = para.GetAttachedObjects(dcpFirst, dcpLast);

                for(int objectIndex = 0; objectIndex < attachedObjects.Count; objectIndex++)
                {
                    if(attachedObjects[objectIndex] is FigureObject)
                    {
                        FigureObject figureObject = (FigureObject) attachedObjects[objectIndex];

                        rgnmpAttachedObject[objectIndex] = figureObject.Para.Handle;
                        rgdcpAnchor[objectIndex] = figureObject.Dcp;
                        rgidobj[objectIndex] = PTS.fsidobjFigure;
                    }
                    else
                    {
                        FloaterObject floaterObject = (FloaterObject) attachedObjects[objectIndex];

                        rgnmpAttachedObject[objectIndex] = floaterObject.Para.Handle;
                        rgdcpAnchor[objectIndex] = floaterObject.Dcp;
                        rgidobj[objectIndex] = PtsHost.FloaterParagraphId;
                    }
                }
                cObjects = attachedObjects.Count;
                fEndOfParagraph = PTS.False;
            }
            catch (Exception e)
            {
                cObjects = 0;
                fEndOfParagraph = PTS.False;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                cObjects = 0;
                fEndOfParagraph = PTS.False;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int GetNumberAttachedObjectsInTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsline,                     // IN:  pointer to line created by client
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            int dcpLim,                         // IN:  dcp at the end of the range
            int fFoundAttachedObjectsBeforeLine,// IN:  Attached objects before line found
            int dcpMaxAnchorAttachedObjectBeforeLine, // IN: Max dcp of anchor in objects before line
            out int cAttachedObjects)           // OUT: number of attached objects
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);

                LineBase lineBase = PtsContext.HandleToObject(pfsline) as LineBase;

                if(lineBase == null)
                {
                    LineBreakpoint lineBreakpoint = PtsContext.HandleToObject(pfsline) as LineBreakpoint;
                    PTS.ValidateHandle(lineBreakpoint);

                    lineBase = lineBreakpoint.OptimalBreakSession.OptimalTextSource;
                }

                if(lineBase.HasFigures || lineBase.HasFloaters)
                {
                    int dcpLast = para.GetLastDcpAttachedObjectBeforeLine(dcpFirst);

                    cAttachedObjects = para.GetAttachedObjectCount(dcpLast, dcpLim);
                }
                else
                {
                    cAttachedObjects = 0;
                }

                return PTS.fserrNone;
            }
            catch (Exception e)
            {
                cAttachedObjects = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                cAttachedObjects = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal unsafe int GetAttachedObjectsInTextLine(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsline,                     // IN:  pointer to line created by client
            IntPtr nmp,                         // IN:  name of paragraph
            int dcpFirst,                       // IN:  dcp at the beginning of the range
            int dcpLim,                         // IN:  dcp at the end of the range
            int fFoundAttachedObjectsBeforeLine,// IN:  Attached objects before line found
            int dcpMaxAnchorAttachedObjectBeforeLine, // IN: Max dcp of anchor in objects before line
            int nAttachedObjects,               // IN:  size of the floater arrays
            IntPtr* rgnmpAttachedObject,        // OUT: array of floater names
            int* rgidobj,                       // OUT: array of idobj's of corresponding objects
            int* rgdcpAnchor,                   // OUT: array of dcp of the objects anchors
            out int cObjects)                   // OUT: actual number of objects
        {
            int fserr = PTS.fserrNone;
            try
            {
                TextParagraph para = PtsContext.HandleToObject(nmp) as TextParagraph;
                PTS.ValidateHandle(para);

                int dcpLast = para.GetLastDcpAttachedObjectBeforeLine(dcpFirst);

                List<AttachedObject> attachedObjects = para.GetAttachedObjects(dcpLast, dcpLim);

                for(int objectIndex = 0; objectIndex < attachedObjects.Count; objectIndex++)
                {
                    if(attachedObjects[objectIndex] is FigureObject)
                    {
                        FigureObject figureObject = (FigureObject) attachedObjects[objectIndex];

                        rgnmpAttachedObject[objectIndex] = figureObject.Para.Handle;
                        rgdcpAnchor[objectIndex] = figureObject.Dcp;
                        rgidobj[objectIndex] = PTS.fsidobjFigure;
                    }
                    else
                    {
                        FloaterObject floaterObject = (FloaterObject) attachedObjects[objectIndex];

                        rgnmpAttachedObject[objectIndex] = floaterObject.Para.Handle;
                        rgdcpAnchor[objectIndex] = floaterObject.Dcp;
                        rgidobj[objectIndex] = PtsHost.FloaterParagraphId;
                    }
                }
                cObjects = attachedObjects.Count;
            }
            catch (Exception e)
            {
                cObjects = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                cObjects = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdGetAttachedObjectChange(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmp,                         // IN:  name of text paragraph
            IntPtr nmpObject,                   // IN:  name of figure
            out PTS.FSKCHANGE fskchObject)      // OUT: kind of change for figure
        {
            int fserr = PTS.fserrNone;
            try
            {
                BaseParagraph para = PtsContext.HandleToObject(nmpObject) as BaseParagraph;
                PTS.ValidateHandle(para);
                int fIgnore;
                para.UpdGetParaChange(out fskchObject, out fIgnore);
            }
            catch (Exception e)
            {
                fskchObject = default(PTS.FSKCHANGE);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fskchObject = default(PTS.FSKCHANGE);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetDurFigureAnchor(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsparaclientFigure,         // IN:
            IntPtr pfsline,                     // IN:  pointer to line created by client
            IntPtr nmpFigure,                   // IN:  figure's name
            uint fswdir,                        // IN:  current direction
            IntPtr pfsfmtlinein,                // IN:  input parameters needed to reformat the line.
            out int dur)                        // OUT: distance from the beginning of the line to the anchor
        {
            int fserr = PTS.fserrNone;
            try
            {
                Line line = PtsContext.HandleToObject(pfsline) as Line;
                if(line != null)
                {
                    PTS.ValidateHandle(line);
                    FigureParagraph paraFigure = PtsContext.HandleToObject(nmpFigure) as FigureParagraph;
                    PTS.ValidateHandle(paraFigure);
                    line.GetDurFigureAnchor(paraFigure, fswdir, out dur);
                }
                else
                {
                    // Needs Optimal implementation.
                    Invariant.Assert(false);
                    dur = 0;
                }
            }
            catch (Exception e)
            {
                dur = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dur = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        // ------------------------------------------------------------------
        // Floater callbacks
        // ------------------------------------------------------------------
        internal int GetFloaterProperties(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr nmFloater,                   // IN:  name of the floater
            uint fswdirTrack,                   // IN:  direction of Track
            out PTS.FSFLOATERPROPS fsfloaterprops)// OUT: properties of the floater
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                para.GetFloaterProperties(fswdirTrack, out fsfloaterprops);
            }
            catch (Exception e)
            {
                fsfloaterprops = new PTS.FSFLOATERPROPS();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfloaterprops = new PTS.FSFLOATERPROPS();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int FormatFloaterContentFinite(
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
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
            out PTS.FSFMTR fsfmtr,              // OUT: result of formatting
            out IntPtr pfsFloatContent,         // OUT: opaque for PTS pointer pointer to formatted content
            out IntPtr pbrkrecpara,             // OUT: pointer to the floater content break record
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as FloaterBaseParaClient;
                PTS.ValidateHandle(paraClient);
                para.FormatFloaterContentFinite(paraClient, pfsbrkFloaterContentIn, fBreakRecordFromPreviousPage,
                    pftnrej, fEmptyOk, fSuppressTopSpace, fswdirTrack, fAtMaxWidth, durAvailable, dvrAvailable, 
                    fsksuppresshardbreakbeforefirstparaIn, out fsfmtr, out pfsFloatContent, out pbrkrecpara, out durFloaterWidth, 
                    out dvrFloaterHeight, out fsbbox, out cPolygons, out cVertices);
            }
            catch (Exception e)
            {
                fsfmtr = new PTS.FSFMTR(); pfsFloatContent = pbrkrecpara = IntPtr.Zero; durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtr = new PTS.FSFMTR(); pfsFloatContent = pbrkrecpara = IntPtr.Zero; durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int FormatFloaterContentBottomless(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdirTrack,                   // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out IntPtr pfsFloatContent,         // OUT: opaque for PTS pointer pointer to formatted content
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as FloaterBaseParaClient;
                PTS.ValidateHandle(paraClient);
                para.FormatFloaterContentBottomless(paraClient, fSuppressTopSpace, fswdirTrack, fAtMaxWidth, 
                    durAvailable, dvrAvailable, out fsfmtrbl, out pfsFloatContent, out durFloaterWidth, out dvrFloaterHeight,
                    out fsbbox, out cPolygons, out cVertices);
            }
            catch (Exception e)
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfsFloatContent = IntPtr.Zero; durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfsFloatContent = IntPtr.Zero; durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int UpdateBottomlessFloaterContent(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdirTrack,                   // IN:  direction of Track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as FloaterBaseParaClient;
                PTS.ValidateHandle(paraClient);
                para.UpdateBottomlessFloaterContent(paraClient, fSuppressTopSpace, fswdirTrack, fAtMaxWidth, 
                    durAvailable, dvrAvailable, pfsFloaterContent, out fsfmtrbl, out durFloaterWidth, out dvrFloaterHeight,
                    out fsbbox, out cPolygons, out cVertices);
            }
            catch (Exception e)
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                durFloaterWidth = dvrFloaterHeight = cPolygons = cVertices = 0; fsbbox = new PTS.FSBBOX();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal unsafe int GetFloaterPolygons(
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            IntPtr nmFloater,                   // IN:  name of floater
            uint fswdirTrack,                   // IN:  direction of Track
            int ncVertices,                     // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                          // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            PTS.FSPOINT* rgfspt,                // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough)               // OUT: fill text in empty areas within obstacles?
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as FloaterBaseParaClient;
                PTS.ValidateHandle(paraClient);
                para.GetFloaterPolygons(paraClient, fswdirTrack, ncVertices, nfspt, rgcVertices, out ccVertices, 
                    rgfspt, out cfspt, out fWrapThrough);
            }
            catch (Exception e)
            {
                ccVertices = cfspt = fWrapThrough = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                ccVertices = cfspt = fWrapThrough = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
}

        internal int ClearUpdateInfoInFloaterContent(
            IntPtr pfsFloaterContent)           // IN:  opaque for PTS pointer to floater content
        {
            if (PtsContext.IsValidHandle(pfsFloaterContent))
            {
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsFloaterContent) as FloaterBaseParaClient;
                if (paraClient is UIElementParaClient)
                {
                    return PTS.fserrNone;
                }
            }
            // Cannot be UIElement handle. Hand off to PTS as before
            return PTS.FsClearUpdateInfoInSubpage(Context, pfsFloaterContent);
        }

        internal int CompareFloaterContents(
            IntPtr pfsFloaterContentOld,        // IN:
            IntPtr pfsFloaterContentNew,        // IN:
            out PTS.FSCOMPRESULT fscmpr)        // OUT: result of comparison
        {
            if (PtsContext.IsValidHandle(pfsFloaterContentOld) && PtsContext.IsValidHandle(pfsFloaterContentNew))
            {
                FloaterBaseParaClient paraClientOld = PtsContext.HandleToObject(pfsFloaterContentOld) as FloaterBaseParaClient;
                FloaterBaseParaClient paraClientNew = PtsContext.HandleToObject(pfsFloaterContentNew) as FloaterBaseParaClient;
                if (paraClientOld is UIElementParaClient && !(paraClientNew is UIElementParaClient))
                {
                    fscmpr = PTS.FSCOMPRESULT.fscmprChangeInside;
                    return PTS.fserrNone;
                }
                if (paraClientNew is UIElementParaClient && !(paraClientOld is UIElementParaClient))
                {
                    fscmpr = PTS.FSCOMPRESULT.fscmprChangeInside;
                    return PTS.fserrNone;
                }
                if (paraClientOld is UIElementParaClient && paraClientNew is UIElementParaClient)
                {
                    if (pfsFloaterContentOld.Equals(pfsFloaterContentNew))
                    {
                        fscmpr = PTS.FSCOMPRESULT.fscmprNoChange;
                        return PTS.fserrNone;
                    }
                    else
                    {
                        fscmpr = PTS.FSCOMPRESULT.fscmprChangeInside;
                        return PTS.fserrNone;
                    }
                }
            }
            return PTS.FsCompareSubpages(Context, pfsFloaterContentOld, pfsFloaterContentNew, out fscmpr);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pfsFloaterContent"></param>
        /// <returns></returns>
        internal int DestroyFloaterContent(
            IntPtr pfsFloaterContent)           // IN:  opaque for PTS pointer to floater content
        {
            if (PtsContext.IsValidHandle(pfsFloaterContent))
            {
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsFloaterContent) as FloaterBaseParaClient;
                if (paraClient is UIElementParaClient)
                {
                    return PTS.fserrNone;
                }
            }
            return PTS.FsDestroySubpage(Context, pfsFloaterContent);
        }

        internal int DuplicateFloaterContentBreakRecord(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsbrkFloaterContent,        // IN:  pointer to break record
            out IntPtr pfsbrkFloaterContentDup) // OUT pointer to duplicate break record
        {
            if (PtsContext.IsValidHandle(pfsbrkFloaterContent))
            {
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsbrkFloaterContent) as FloaterBaseParaClient;
                if (paraClient is UIElementParaClient)
                {
                    // Should not get here from a UIElement
                    Invariant.Assert(false, "Embedded UIElement should not have break record");
                }
            }
            return PTS.FsDuplicateSubpageBreakRecord(Context, pfsbrkFloaterContent, out pfsbrkFloaterContentDup);
        }

        internal int DestroyFloaterContentBreakRecord(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsbrkFloaterContent)        // IN:  pointer to break record
        {
            if (PtsContext.IsValidHandle(pfsbrkFloaterContent))
            {
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsbrkFloaterContent) as FloaterBaseParaClient;
                if (paraClient is UIElementParaClient)
                {
                    // Should not get here from a UIElement
                    Invariant.Assert(false, "Embedded UIElement should not have break record");
                }
            }
            return PTS.FsDestroySubpageBreakRecord(Context, pfsbrkFloaterContent);
        }

        internal int GetFloaterContentColumnBalancingInfo(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            uint fswdir,                        // IN:  current direction
            out int nlines,                     // OUT: number of text lines
            out int dvrSumHeight,               // OUT: sum of all line heights
            out int dvrMinHeight)               // OUT: minimum line height
        {
            if (PtsContext.IsValidHandle(pfsFloaterContent))
            {
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsFloaterContent) as FloaterBaseParaClient;
                if (paraClient is UIElementParaClient)
                {
                    if (((BlockUIContainer)paraClient.Paragraph.Element).Child != null)
                    {
                        nlines = 1;
                        UIElement uiElement = ((BlockUIContainer)paraClient.Paragraph.Element).Child;
                        dvrSumHeight = TextDpi.ToTextDpi(uiElement.DesiredSize.Height);
                        dvrMinHeight = TextDpi.ToTextDpi(uiElement.DesiredSize.Height);
                    }
                    else
                    {
                        nlines = 0;
                        dvrSumHeight = dvrMinHeight = 0;
                    }
                    return PTS.fserrNone;
                }
            }
            uint fswdirSubpage;
            return PTS.FsGetSubpageColumnBalancingInfo(Context, pfsFloaterContent,
                out fswdirSubpage, out nlines, out dvrSumHeight, out dvrMinHeight);
        }

        internal int GetFloaterContentNumberFootnotes(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            out int cftn)                       // OUT: number of footnotes
        {
            if (PtsContext.IsValidHandle(pfsFloaterContent))
            {
                FloaterBaseParaClient paraClient = PtsContext.HandleToObject(pfsFloaterContent) as FloaterBaseParaClient;
                if (paraClient is UIElementParaClient)
                {
                    cftn = 0;
                    return PTS.fserrNone;
                }
            }
            return PTS.FsGetNumberSubpageFootnotes(Context, pfsFloaterContent, out cftn);
        }
        internal int GetFloaterContentFootnoteInfo(
            IntPtr pfsFloaterContent,           // IN:  opaque for PTS pointer to floater content
            uint fswdir,                        // IN:  current direction
            int nftn,                           // IN:  size of FSFTNINFO array
            int iftnFirst,                      // IN:  first index in FSFTNINFO array to be used by this para
            ref PTS.FSFTNINFO fsftninf,         // IN/OUT: array of footnote info
            out int iftnLim)                    // OUT: lim index used by this paragraph
        {
            Debug.Assert(false);
            iftnLim = 0;
            return PTS.fserrNone;
        }

        internal int TransferDisplayInfoInFloaterContent(
            IntPtr pfsFloaterContentOld,        // IN:
            IntPtr pfsFloaterContentNew)        // IN:
        {
            if (PtsContext.IsValidHandle(pfsFloaterContentOld) && PtsContext.IsValidHandle(pfsFloaterContentNew))
            {
                FloaterBaseParaClient paraClientOld = PtsContext.HandleToObject(pfsFloaterContentOld) as FloaterBaseParaClient;
                FloaterBaseParaClient paraClientNew = PtsContext.HandleToObject(pfsFloaterContentNew) as FloaterBaseParaClient;
                if (paraClientOld is UIElementParaClient || paraClientNew is UIElementParaClient)
                {
                    return PTS.fserrNone;
                }
            }

            return PTS.FsTransferDisplayInfoSubpage(PtsContext.Context, pfsFloaterContentOld, pfsFloaterContentNew);
        }
        internal int GetMCSClientAfterFloater(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            uint fswdirTrack,                   // IN:  direction of Track
            IntPtr pmcsclientIn,                // IN:  input opaque to PTS MCSCLIENT
            out IntPtr pmcsclientOut)           // OUT: MCSCLIENT that floater will return to track
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.GetMCSClientAfterFloater(fswdirTrack, mcs, out pmcsclientOut);
            }
            catch (Exception e)
            {
                pmcsclientOut = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pmcsclientOut = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int GetDvrUsedForFloater(
            IntPtr pfsclient,                   // IN:  client context
            IntPtr pfsparaclient,               // IN:
            IntPtr nmFloater,                   // IN:  name of floater
            uint fswdirTrack,                   // IN:  direction of Track
            IntPtr pmcsclientIn,                // IN:  margin collapsing state
            int dvrDisplaced,                   // IN: 
            out int dvrUsed)                    // OUT:
        {
            int fserr = PTS.fserrNone;
            try
            {
                FloaterBaseParagraph para = PtsContext.HandleToObject(nmFloater) as FloaterBaseParagraph;
                PTS.ValidateHandle(para);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.GetDvrUsedForFloater(fswdirTrack, mcs, dvrDisplaced, out dvrUsed);
            }
            catch (Exception e)
            {
                dvrUsed = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dvrUsed = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        // ------------------------------------------------------------------
        // Subtrack paragraph callbacks
        // ------------------------------------------------------------------
        #region SubtrackPara
        internal int SubtrackCreateContext(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsc,                        // IN:  FS context
            IntPtr pfscbkobj,                   // IN:  callbacks (FSCBKOBJ)
            uint ffi,                           // IN:  formatting flags
            int idobj,                          // IN:  id of the object
            out IntPtr pfssobjc)                // OUT: object context
        {
            pfssobjc = (IntPtr)(idobj + _objectContextOffset);
            return PTS.fserrNone;
        }
        internal int SubtrackDestroyContext(
            IntPtr pfssobjc)                    // IN:  object context
        {
            // Do nothing
            return PTS.fserrNone;
        }

        internal int SubtrackFormatParaFinite(
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
            ref PTS.FSRECT fsrcToFill,          // IN:  rectangle to fill
            IntPtr pmcsclientIn,                // IN:  input margin collapsing state
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
            int fBreakInside,                   // IN:  produce vertical break inside para; needed for recursive KWN logic;
                                                //      can be set to true if during previous formatting fBreakInsidePossible output was returned as TRUE
            out PTS.FSFMTR fsfmtr,              // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out IntPtr pbrkrecpara,             // OUT: pointer to the para break record
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fBreakInsidePossible)       // OUT: internal vertical break possible, needed for recursive KWN logic
        {
            int fserr = PTS.fserrNone;
            fBreakInsidePossible = PTS.False;
            try
            {
                ContainerParagraph para = PtsContext.HandleToObject(nmp) as ContainerParagraph;
                PTS.ValidateHandle(para);
                ContainerParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as ContainerParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.FormatParaFinite(paraClient, pfsobjbrk, fBreakRecordFromPreviousPage, iArea, pftnrej, pfsgeom, 
                    fEmptyOk, fSuppressTopSpace, fswdir, ref fsrcToFill, mcs, fskclearIn, fsksuppresshardbreakbeforefirstparaIn, out fsfmtr, out pfspara, 
                    out pbrkrecpara, out dvrUsed, out fsbbox, out pmcsclientOut, out fskclearOut, out dvrTopSpace);
            }
            catch (Exception e)
            {
                fsfmtr = new PTS.FSFMTR(); pfspara = pbrkrecpara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtr = new PTS.FSFMTR(); pfspara = pbrkrecpara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int SubtrackFormatParaBottomless(
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
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable)// OUT: interruption is prohibited from now on
        {
            int fserr = PTS.fserrNone;
            try
            {
                ContainerParagraph para = PtsContext.HandleToObject(nmp) as ContainerParagraph;
                PTS.ValidateHandle(para);
                ContainerParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as ContainerParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.FormatParaBottomless(paraClient, iArea, pfsgeom, fSuppressTopSpace, fswdir, urTrack, durTrack, vrTrack, mcs, fskclearIn, fInterruptable, out fsfmtrbl, out pfspara, out dvrUsed, out fsbbox, 
                    out pmcsclientOut, out fskclearOut, out dvrTopSpace, out fPageBecomesUninterruptable);
            }
            catch (Exception e)
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int SubtrackUpdateBottomlessPara(
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
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting the paragraph
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable)// OUT: interruption is prohibited from now on
        {
            int fserr = PTS.fserrNone;
            try
            {
                ContainerParagraph para = PtsContext.HandleToObject(nmp) as ContainerParagraph;
                PTS.ValidateHandle(para);
                ContainerParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as ContainerParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.UpdateBottomlessPara(pfspara, paraClient, iArea, pfsgeom, fSuppressTopSpace, fswdir, urTrack, durTrack, 
                    vrTrack, mcs, fskclearIn, fInterruptable, out fsfmtrbl, out dvrUsed, out fsbbox, 
                    out pmcsclientOut, out fskclearOut, out dvrTopSpace, out fPageBecomesUninterruptable);
            }
            catch (Exception e)
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int SubtrackSynchronizeBottomlessPara(
            IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsgeom,                     // IN:  pointer to geometry
            uint fswdir,                        // IN:  direction
            int dvrShift)                       // IN:  shift by this value
        {
            int fserr = PTS.fserrNone;
            try
            {
                ContainerParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as ContainerParaClient;
                PTS.ValidateHandle(paraClient);
                PTS.Validate(PTS.FsSynchronizeBottomlessSubtrack(Context, pfspara, pfsgeom, fswdir, dvrShift), PtsContext);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int SubtrackComparePara(
            IntPtr pfsparaclientOld,            // IN:
            IntPtr pfsparaOld,                  // IN:  pointer to the old para data
            IntPtr pfsparaclientNew,            // IN:
            IntPtr pfsparaNew,                  // IN:  pointer to the new para data
            uint fswdir,                        // IN:  track's direction
            out PTS.FSCOMPRESULT fscmpr,        // OUT: comparison result
            out int dvrShifted)                 // OUT: amount of shift if result is fscomprShifted
        {
            return PTS.FsCompareSubtrack(Context, pfsparaOld, pfsparaNew, fswdir, out fscmpr, out dvrShifted);
        }

        internal int SubtrackClearUpdateInfoInPara(
            IntPtr pfspara)                     // IN:  pointer to the para data
        {
            return PTS.FsClearUpdateInfoInSubtrack(Context, pfspara);
        }

        internal int SubtrackDestroyPara(
            IntPtr pfspara)                     // IN:  pointer to the para data
        {
            return PTS.FsDestroySubtrack(Context, pfspara);
        }

        internal int SubtrackDuplicateBreakRecord(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsbrkrecparaOrig,           // IN:  pointer to the para break record
            out IntPtr pfsbrkrecparaDup)        // OUT: pointer to the duplicate break record
        {
            return PTS.FsDuplicateSubtrackBreakRecord(Context, pfsbrkrecparaOrig, out pfsbrkrecparaDup);
        }

        internal int SubtrackDestroyBreakRecord(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsobjbrk)                   // IN:  pointer to the para break record
        {
            return PTS.FsDestroySubtrackBreakRecord(Context, pfsobjbrk);
        }

        internal int SubtrackGetColumnBalancingInfo(
            IntPtr pfspara,                     // IN:  pointer to the para data
            uint fswdir,                        // IN:  current direction
            out int nlines,                     // OUT: number of text lines
            out int dvrSumHeight,               // OUT: sum of all line heights
            out int dvrMinHeight)               // OUT: minimum line height
        {
            return PTS.FsGetSubtrackColumnBalancingInfo(Context, pfspara, fswdir, 
                out nlines, out dvrSumHeight, out dvrMinHeight);
        }

        internal int SubtrackGetNumberFootnotes(
            IntPtr pfspara,                     // IN:  pointer to the para data
            out int nftn)                       // OUT: number of footnotes
        {
            return PTS.FsGetNumberSubtrackFootnotes(Context, pfspara, out nftn);
        }
        internal unsafe int SubtrackGetFootnoteInfo(
            IntPtr pfspara,                     // IN:  pointer to the para data
            uint fswdir,                        // IN:  current direction
            int nftn,                           // IN:  size of FSFTNINFO array
            int iftnFirst,                      // IN:  first index in FSFTNINFO array to be used by this para
            PTS.FSFTNINFO* pfsftninf,           // IN/OUT: array of footnote info
            out int iftnLim)                    // OUT: lim index used by this paragraph
        {
            Debug.Assert(false, "PTS.ObjGetFootnoteInfo is not implemented.");
            iftnLim = 0;
            return PTS.fserrNotImplemented;
        }
        internal int SubtrackShiftVertical(IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsshift,                    // IN:  pointer to the shift data
            uint fswdir,                        // IN:  wdir for bbox - the same as the one passed to formatting method
            out PTS.FSBBOX pfsbbox)             // OUT: output BBox
        {
            Debug.Assert(false);
            pfsbbox = new PTS.FSBBOX();
            return PTS.fserrNone;
        }

        internal int SubtrackTransferDisplayInfoPara(
            IntPtr pfsparaOld,                  // IN:  pointer to the old para data
            IntPtr pfsparaNew)                  // IN:  pointer to the new para data
        {
            return PTS.FsTransferDisplayInfoSubtrack(Context, pfsparaOld, pfsparaNew);
        }
        #endregion SubtrackPara
        // ------------------------------------------------------------------
        // Subpage paragraph callbacks
        // ------------------------------------------------------------------
        #region SubpagePara
        internal int SubpageCreateContext(
            IntPtr pfsclient,                   // IN:  client opaque data
            IntPtr pfsc,                        // IN:  FS context
            IntPtr pfscbkobj,                   // IN:  callbacks (FSCBKOBJ)
            uint ffi,                           // IN:  formatting flags
            int idobj,                          // IN:  id of the object
            out IntPtr pfssobjc)                // OUT: object context
        {
            pfssobjc = (IntPtr)(idobj + _objectContextOffset + 1);
            return PTS.fserrNone;
        }
        internal int SubpageDestroyContext(
            IntPtr pfssobjc)                    // IN:  object context
        {
            // Do nothing
            return PTS.fserrNone;
        }

        internal int SubpageFormatParaFinite(
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
            ref PTS.FSRECT fsrcToFill,          // IN:  rectangle to fill
            IntPtr pmcsclientIn,                // IN:  input margin collapsing state
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
            int fBreakInside,                   // IN:  produce vertical break inside para; needed for recursive KWN logic;
                                                //      can be set to true if during previous formatting fBreakInsidePossible output was returned as TRUE
            out PTS.FSFMTR fsfmtr,              // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out IntPtr pbrkrecpara,             // OUT: pointer to the para break record
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fBreakInsidePossible)       // OUT: internal vertical break possible, needed for recursive KWN logic
        {
            int fserr = PTS.fserrNone;
            fBreakInsidePossible = PTS.False;
            try
            {
                SubpageParagraph para = PtsContext.HandleToObject(nmp) as SubpageParagraph;
                PTS.ValidateHandle(para);
                SubpageParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as SubpageParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.FormatParaFinite(paraClient, pfsobjbrk, fBreakRecordFromPreviousPage, pftnrej, 
                    fEmptyOk, fSuppressTopSpace, fswdir, ref fsrcToFill, mcs, fskclearIn, fsksuppresshardbreakbeforefirstparaIn, out fsfmtr, out pfspara, 
                    out pbrkrecpara, out dvrUsed, out fsbbox, out pmcsclientOut, out fskclearOut, out dvrTopSpace);
            }
            catch (Exception e)
            {
                fsfmtr = new PTS.FSFMTR(); pfspara = pbrkrecpara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtr = new PTS.FSFMTR(); pfspara = pbrkrecpara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int SubpageFormatParaBottomless(
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
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting the paragraph
            out IntPtr pfspara,                 // OUT: pointer to the para data
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable)// OUT: interruption is prohibited from now on
        {
            int fserr = PTS.fserrNone;
            try
            {
                SubpageParagraph para = PtsContext.HandleToObject(nmp) as SubpageParagraph;
                PTS.ValidateHandle(para);
                SubpageParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as SubpageParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.FormatParaBottomless(paraClient, fSuppressTopSpace, fswdir, urTrack, durTrack, vrTrack, mcs, 
                    fskclearIn, fInterruptable, out fsfmtrbl, out pfspara, out dvrUsed, out fsbbox, 
                    out pmcsclientOut, out fskclearOut, out dvrTopSpace, out fPageBecomesUninterruptable);
            }
            catch (Exception e)
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        internal int SubpageUpdateBottomlessPara(
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
            PTS.FSKCLEAR fskclearIn,            // IN:  clear property that must be satisfied
            int fInterruptable,                 // IN:  formatting can be interrupted
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting the paragraph
            out int dvrUsed,                    // OUT: vertical space used by the para
            out PTS.FSBBOX fsbbox,              // OUT: para BBox
            out IntPtr pmcsclientOut,           // OUT: margin collapsing state at the bottom
            out PTS.FSKCLEAR fskclearOut,       // OUT: ClearIn for the next paragraph
            out int dvrTopSpace,                // OUT: top space due to collapsed margin
            out int fPageBecomesUninterruptable)// OUT: interruption is prohibited from now on
        {
            int fserr = PTS.fserrNone;
            try
            {
                SubpageParagraph para = PtsContext.HandleToObject(nmp) as SubpageParagraph;
                PTS.ValidateHandle(para);
                SubpageParaClient paraClient = PtsContext.HandleToObject(pfsparaclient) as SubpageParaClient;
                PTS.ValidateHandle(paraClient);
                MarginCollapsingState mcs = null;
                if (pmcsclientIn != IntPtr.Zero)
                {
                    mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;
                    PTS.ValidateHandle(mcs);
                }
                para.UpdateBottomlessPara(pfspara, paraClient, fSuppressTopSpace, fswdir, urTrack, durTrack, 
                    vrTrack, mcs, fskclearIn, fInterruptable, out fsfmtrbl, out dvrUsed, out fsbbox, 
                    out pmcsclientOut, out fskclearOut, out dvrTopSpace, out fPageBecomesUninterruptable);
            }
            catch (Exception e)
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fsfmtrbl = default(PTS.FSFMTRBL); 
                pfspara = pmcsclientOut = IntPtr.Zero; dvrUsed = dvrTopSpace = fPageBecomesUninterruptable = 0; fsbbox = new PTS.FSBBOX(); 
                fskclearOut = default(PTS.FSKCLEAR);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }
        internal int SubpageSynchronizeBottomlessPara(
            IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsgeom,                     // IN:  pointer to geometry
            uint fswdir,                        // IN:  direction
            int dvrShift)                       // IN:  shift by this value
        {
            Debug.Assert(false);
            return PTS.fserrNone;
        }

        internal int SubpageComparePara(
            IntPtr pfsparaclientOld,            // IN:
            IntPtr pfsparaOld,                  // IN:  pointer to the old para data
            IntPtr pfsparaclientNew,            // IN:
            IntPtr pfsparaNew,                  // IN:  pointer to the new para data
            uint fswdir,                        // IN:  track's direction
            out PTS.FSCOMPRESULT fscmpr,        // OUT: comparison result
            out int dvrShifted)                 // OUT: amount of shift if result is fscomprShifted
        {
            dvrShifted = 0;
            return PTS.FsCompareSubpages(Context, pfsparaOld, pfsparaNew, out fscmpr);
        }
        internal int SubpageClearUpdateInfoInPara(
            IntPtr pfspara)                     // IN:  pointer to the para data
        {
            return PTS.FsClearUpdateInfoInSubpage(Context, pfspara);
        }

        internal int SubpageDestroyPara(
            IntPtr pfspara)                     // IN:  pointer to the para data
        {
            return PTS.FsDestroySubpage(Context, pfspara);
        }

        internal int SubpageDuplicateBreakRecord(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsbrkrecparaOrig,           // IN:  pointer to the para break record
            out IntPtr pfsbrkrecparaDup)        // OUT: pointer to the duplicate break record
        {
            return PTS.FsDuplicateSubpageBreakRecord(Context, pfsbrkrecparaOrig, out pfsbrkrecparaDup);
        }

        internal int SubpageDestroyBreakRecord(
            IntPtr pfssobjc,                    // IN:  object context
            IntPtr pfsobjbrk)                   // IN:  pointer to the para break record
        {
            return PTS.FsDestroySubpageBreakRecord(Context, pfsobjbrk);
        }

        internal int SubpageGetColumnBalancingInfo(
            IntPtr pfspara,                     // IN:  pointer to the para data
            uint fswdir,                        // IN:  current direction
            out int nlines,                     // OUT: number of text lines
            out int dvrSumHeight,               // OUT: sum of all line heights
            out int dvrMinHeight)               // OUT: minimum line height
        {
            return PTS.FsGetSubpageColumnBalancingInfo(Context, pfspara, out fswdir,
                out nlines, out dvrSumHeight, out dvrMinHeight);
        }

        internal int SubpageGetNumberFootnotes(
            IntPtr pfspara,                     // IN:  pointer to the para data
            out int nftn)                       // OUT: number of footnotes
        {
            return PTS.FsGetNumberSubpageFootnotes(Context, pfspara, out nftn);
        }

        internal unsafe int SubpageGetFootnoteInfo(
            IntPtr pfspara,                     // IN:  pointer to the para data
            uint fswdir,                        // IN:  current direction
            int nftn,                           // IN:  size of FSFTNINFO array
            int iftnFirst,                      // IN:  first index in FSFTNINFO array to be used by this para
            PTS.FSFTNINFO* pfsftninf,           // IN/OUT: array of footnote info
            out int iftnLim)                    // OUT: lim index used by this paragraph
        {
            return PTS.FsGetSubpageFootnoteInfo(Context, pfspara, nftn, iftnFirst, out fswdir, pfsftninf, out iftnLim);
        }

        internal int SubpageShiftVertical(
            IntPtr pfspara,                     // IN:  pointer to the para data
            IntPtr pfsparaclient,               // IN:
            IntPtr pfsshift,                    // IN:  pointer to the shift data
            uint fswdir,                        // IN:  wdir for bbox - the same as the one passed to formatting method
            out PTS.FSBBOX pfsbbox)             // OUT: output BBox
        {
            Debug.Assert(false);
            pfsbbox = new PTS.FSBBOX();
            return PTS.fserrNone;
        }

        internal int SubpageTransferDisplayInfoPara(
            IntPtr pfsparaOld,                  // IN:  pointer to the old para data
            IntPtr pfsparaNew)                  // IN:  pointer to the new para data
        {
            return PTS.FsTransferDisplayInfoSubpage(Context, pfsparaOld, pfsparaNew);
        }
        #endregion SubpagePara
        // ------------------------------------------------------------------
        // Table callbacks
        // ------------------------------------------------------------------
        internal int GetTableProperties(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            out PTS.FSTABLEOBJPROPS fstableobjprops)// OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetTableProperties(fswdirTrack, out fstableobjprops);
            }
            catch (Exception e)
            {
                fstableobjprops = new PTS.FSTABLEOBJPROPS();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fstableobjprops = new PTS.FSTABLEOBJPROPS();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int AutofitTable(       
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            int durAvailableSpace,                  // IN:  
            out int durTableWidth)                  // OUT: Table width after autofit. It is the same for all rows :)
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParaClient paraClient = PtsContext.HandleToObject(pfsparaclientTable) as TableParaClient;
                PTS.ValidateHandle(paraClient);

                paraClient.AutofitTable(fswdirTrack, durAvailableSpace, out durTableWidth);
            }
            catch (Exception e)
            {
                durTableWidth = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                durTableWidth = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int UpdAutofitTable(        // calculate widths of table
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            int durAvailableSpace,                  // IN:  
            out int durTableWidth,                  // OUT: Table width after autofit.
                                                    // Should we store the old one? It is possible for the
                                                    // table width to change with pfNoChangeInCellWidths = .T. ?
            out int fNoChangeInCellWidths)          // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParaClient paraClient = PtsContext.HandleToObject(pfsparaclientTable) as TableParaClient;
                PTS.ValidateHandle(paraClient);

                paraClient.UpdAutofitTable(fswdirTrack, durAvailableSpace, out durTableWidth, out fNoChangeInCellWidths);
            }
            catch (Exception e)
            {
                durTableWidth = 0;
                fNoChangeInCellWidths = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                durTableWidth = 0;
                fNoChangeInCellWidths = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetMCSClientAfterTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTrack,                       // IN:  
            IntPtr pmcsclientIn,                    // IN:  
            out IntPtr ppmcsclientOut)              // OUT:
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetMCSClientAfterTable(fswdirTrack, pmcsclientIn, out ppmcsclientOut);
            }
            catch (Exception e)
            {
                ppmcsclientOut = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                ppmcsclientOut = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetFirstHeaderRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            int fRepeatedHeader,                    // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmFirstHeaderRow)           // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetFirstHeaderRow(fRepeatedHeader, out fFound, out pnmFirstHeaderRow);
            }
            catch (Exception e)
            {
                fFound = 0;
                pnmFirstHeaderRow = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                pnmFirstHeaderRow = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetNextHeaderRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmHeaderRow,                     // IN:  
            int fRepeatedHeader,                    // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmNextHeaderRow)            // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetNextHeaderRow(fRepeatedHeader, nmHeaderRow, out fFound, out pnmNextHeaderRow);
            }
            catch (Exception e)
            {
                fFound = 0;
                pnmNextHeaderRow = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                pnmNextHeaderRow = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetFirstFooterRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            int fRepeatedFooter,                    // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmFirstFooterRow)           // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetFirstFooterRow(fRepeatedFooter, out fFound, out pnmFirstFooterRow);
            }
            catch (Exception e)
            {
                fFound = 0;
                pnmFirstFooterRow = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                pnmFirstFooterRow = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetNextFooterRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmFooterRow,                     // IN:  
            int fRepeatedFooter,                    // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmNextFooterRow)            // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetNextFooterRow(fRepeatedFooter, nmFooterRow, out fFound, out pnmNextFooterRow);
            }
            catch (Exception e)
            {
                fFound = 0;
                pnmNextFooterRow = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                pnmNextFooterRow = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetFirstRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmFirstRow)                 // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetFirstRow(out fFound, out pnmFirstRow);
            }
            catch (Exception e)
            {
                fFound = 0;
                pnmFirstRow = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                pnmFirstRow = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetNextRow(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmRow,                           // IN:  
            out int fFound,                         // OUT: 
            out IntPtr pnmNextRow)                  // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetNextRow(nmRow, out fFound, out pnmNextRow);
            }
            catch (Exception e)
            {
                fFound = 0;
                pnmNextRow = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                pnmNextRow = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int UpdFChangeInHeaderFooter( // we don't do update in header/footer
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            out int fHeaderChanged,                 // OUT: 
            out int fFooterChanged,                 // OUT: 
            out int fRepeatedHeaderChanged,         // OUT: unneeded for bottomless page, but...
            out int fRepeatedFooterChanged)         // OUT: unneeded for bottomless page, but...
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.UpdFChangeInHeaderFooter(
                    out fHeaderChanged, 
                    out fFooterChanged, 
                    out fRepeatedHeaderChanged, 
                    out fRepeatedFooterChanged);
            }
            catch (Exception e)
            {
                fHeaderChanged = 0;
                fFooterChanged = 0;
                fRepeatedHeaderChanged = 0;
                fRepeatedFooterChanged = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fHeaderChanged = 0;
                fFooterChanged = 0;
                fRepeatedHeaderChanged = 0;
                fRepeatedFooterChanged = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int UpdGetFirstChangeInTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            out int fFound,                         // OUT: 
            out int fChangeFirst,                   // OUT: 
            out IntPtr pnmRowBeforeChange)          // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.UpdGetFirstChangeInTable(
                    out fFound, 
                    out fChangeFirst, 
                    out pnmRowBeforeChange);
            }
            catch (Exception e)
            {
                fFound = 0;
                fChangeFirst = 0;
                pnmRowBeforeChange = IntPtr.Zero;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fFound = 0;
                fChangeFirst = 0;
                pnmRowBeforeChange = IntPtr.Zero;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int UpdGetRowChange(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            IntPtr nmRow,                           // IN:  
            out PTS.FSKCHANGE fskch,                // OUT: 
            out int fNoFurtherChanges)              // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                RowParagraph rowParagraph = PtsContext.HandleToObject(nmRow) as RowParagraph;
                PTS.ValidateHandle(rowParagraph);

                rowParagraph.UpdGetParaChange(out fskch, out fNoFurtherChanges);
            }
            catch (Exception e)
            {
                fskch = default(PTS.FSKCHANGE);
                fNoFurtherChanges = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
               fskch = default(PTS.FSKCHANGE);
                fNoFurtherChanges = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int UpdGetCellChange(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmRow,                           // IN:  
            IntPtr nmCell,                          // IN:  
            out int fWidthChanged,                  // OUT: 
            out PTS.FSKCHANGE fskchCell)            // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParagraph cellParagraph = PtsContext.HandleToObject(nmCell) as CellParagraph;
                PTS.ValidateHandle(cellParagraph);

                cellParagraph.UpdGetCellChange(out fWidthChanged, out fskchCell);
            }
            catch (Exception e)
            {
                fWidthChanged = 0;
                fskchCell = default(PTS.FSKCHANGE);
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fWidthChanged = 0;
                fskchCell = default(PTS.FSKCHANGE);
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetDistributionKind(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmTable,                         // IN:  
            uint fswdirTable,                       // IN:  
            out PTS.FSKTABLEHEIGHTDISTRIBUTION tabledistr)  // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                TableParagraph para = PtsContext.HandleToObject(nmTable) as TableParagraph;
                PTS.ValidateHandle(para);

                para.GetDistributionKind(fswdirTable, out tabledistr);
            }
            catch (Exception e)
            {
                tabledistr = PTS.FSKTABLEHEIGHTDISTRIBUTION.fskdistributeUnchanged;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                tabledistr = PTS.FSKTABLEHEIGHTDISTRIBUTION.fskdistributeUnchanged;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetRowProperties(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmRow,                           // IN:  
            uint fswdirTable,                       // IN:  
            out PTS.FSTABLEROWPROPS rowprops)       // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                RowParagraph rowParagraph = PtsContext.HandleToObject(nmRow) as RowParagraph;
                PTS.ValidateHandle(rowParagraph);

                rowParagraph.GetRowProperties(fswdirTable, out rowprops);
            }
            catch (Exception e)
            {
                rowprops = new PTS.FSTABLEROWPROPS();
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                rowprops = new PTS.FSTABLEROWPROPS();
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }

        internal unsafe int GetCells(
            IntPtr pfsclient,                       // IN:  
            IntPtr nmRow,                           // IN:  
            int cCells,                             // IN:  
            IntPtr* rgnmCell,                       // IN/OUT: 
            PTS.FSTABLEKCELLMERGE* rgkcellmerge)    // IN/OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                RowParagraph rowParagraph = PtsContext.HandleToObject(nmRow) as RowParagraph;
                PTS.ValidateHandle(rowParagraph);

                rowParagraph.GetCells(cCells, rgnmCell, rgkcellmerge);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int FInterruptFormattingTable(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclient,                   // IN:  
            IntPtr nmRow,                           // IN:  
            int dvr,                                // IN:  
            out int fInterrupt)                     // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                RowParagraph rowParagraph = PtsContext.HandleToObject(nmRow) as RowParagraph;
                PTS.ValidateHandle(rowParagraph);

                rowParagraph.FInterruptFormattingTable(dvr, out fInterrupt);
            }
            catch (Exception e)
            {
                fInterrupt = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fInterrupt = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }

        internal unsafe int CalcHorizontalBBoxOfRow(
            IntPtr pfsclient,                       // IN:
            IntPtr nmRow,                           // IN:
            int cCells,                             // IN:
            IntPtr* rgnmCell,                       // IN:
            IntPtr* rgpfscell,                      // IN:
            out int urBBox,                         // OUT:
            out int durBBox)                        // OUT:
        {
            int fserr = PTS.fserrNone;
            try
            {
                RowParagraph rowParagraph = PtsContext.HandleToObject(nmRow) as RowParagraph;
                PTS.ValidateHandle(rowParagraph);

                rowParagraph.CalcHorizontalBBoxOfRow(
                    cCells, 
                    rgnmCell, 
                    rgpfscell, 
                    out urBBox, 
                    out durBBox);
            }
            catch (Exception e)
            {
                urBBox = 0;
                durBBox = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                urBBox = 0;
                durBBox = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        // unless cell has vertical text or is special in some other ways,
        // this calls maps directly to a create subpage call :-)
        internal int FormatCellFinite(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr pfsbrkcell,                      // IN:  not NULL if cell broken from previous page/column
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            IntPtr pfsFtnRejector,                  // IN:  
            int fEmptyOK,                           // IN:  
            uint fswdirTable,                       // IN:  
            int dvrExtraHeight,                     // IN: height above current row (non-zero for vMerged cells)
            int dvrAvailable,                       // IN:  
            out PTS.FSFMTR pfmtr,                   // OUT: 
            out IntPtr ppfscell,                    // OUT: cell object
            out IntPtr pfsbrkcellOut,               // OUT: break if cell does not fit in dvrAvailable
            out int dvrUsed)                        // OUT: height -- min height required 
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParagraph cellParagraph = PtsContext.HandleToObject(nmCell) as CellParagraph;
                PTS.ValidateHandle(cellParagraph);

                TableParaClient tableParaClient = PtsContext.HandleToObject(pfsparaclientTable) as TableParaClient;
                PTS.ValidateHandle(tableParaClient);

                cellParagraph.FormatCellFinite(
                              tableParaClient, pfsbrkcell, pfsFtnRejector, fEmptyOK, fswdirTable, dvrAvailable, 
                              out pfmtr, out ppfscell, out pfsbrkcellOut, out dvrUsed);
            }
            catch (Exception e)
            {
                pfmtr = new PTS.FSFMTR();
                ppfscell = IntPtr.Zero;
                pfsbrkcellOut = IntPtr.Zero;
                dvrUsed = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                pfmtr = new PTS.FSFMTR();
                ppfscell = IntPtr.Zero;
                pfsbrkcellOut = IntPtr.Zero;
                dvrUsed = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int FormatCellBottomless(   
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            uint fswdirTable,                       // IN:  
            out PTS.FSFMTRBL fmtrbl,                // OUT: 
            out IntPtr ppfscell,                    // OUT: cell object
            out int dvrUsed)                        // OUT: height -- min height required 
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParagraph cellParagraph = PtsContext.HandleToObject(nmCell) as CellParagraph;
                PTS.ValidateHandle(cellParagraph);

                TableParaClient tableParaClient = PtsContext.HandleToObject(pfsparaclientTable) as TableParaClient;
                PTS.ValidateHandle(tableParaClient);

                cellParagraph.FormatCellBottomless(
                    tableParaClient, 
                    fswdirTable, 
                    out fmtrbl, 
                    out ppfscell, 
                    out dvrUsed);
            }
            catch (Exception e)
            {
                fmtrbl = default(PTS.FSFMTRBL);
                ppfscell = IntPtr.Zero;
                dvrUsed = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fmtrbl = default(PTS.FSFMTRBL);
                ppfscell = IntPtr.Zero;
                dvrUsed = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        // unless cell has vertical text or is special in some other ways,
        // this calls maps directly to a update subpage call :-)
        internal int UpdateBottomlessCell( 
            IntPtr pfscell,                         // IN/OUT: cell object
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            uint fswdirTable,                       // IN:  
            out PTS.FSFMTRBL fmtrbl,                // IN:  
            out int dvrUsed)                        // OUT: height -- min height required 
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParagraph cellParagraph = PtsContext.HandleToObject(nmCell) as CellParagraph;
                PTS.ValidateHandle(cellParagraph);

                CellParaClient cellParaClient = PtsContext.HandleToObject(pfscell) as CellParaClient;
                PTS.ValidateHandle(cellParaClient);

                TableParaClient tableParaClient = PtsContext.HandleToObject(pfsparaclientTable) as TableParaClient;
                PTS.ValidateHandle(tableParaClient);

                cellParagraph.UpdateBottomlessCell(
                    cellParaClient, 
                    tableParaClient, 
                    fswdirTable, 
                    out fmtrbl, 
                    out dvrUsed);
            }
            catch (Exception e)
            {
                fmtrbl = default(PTS.FSFMTRBL);
                dvrUsed = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                fmtrbl = default(PTS.FSFMTRBL);
               dvrUsed = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }

        internal int CompareCells(
            IntPtr pfscellOld,
            IntPtr pfscellNew,
            out PTS.FSCOMPRESULT fscmpr)                         // IN:OUT: cell object
        {
            fscmpr = PTS.FSCOMPRESULT.fscmprChangeInside;
            return (PTS.fserrNone);
        }


        internal int ClearUpdateInfoInCell(
            IntPtr pfscell)                         // IN:OUT: cell object
        {
            // Do nothing
            return (PTS.fserrNone);
        }
        internal int SetCellHeight(
            IntPtr pfscell,                         // IN/OUT: cell object
            IntPtr pfsparaclientTable,              // IN:  table's para client
            IntPtr pfsbrkcell,                      // IN:  not NULL if cell broken from previous page/column
            IntPtr nmCell,                          // IN:  for vMerged cells, the first cell (master)
            int fBrokenHere,                        // IN:  true if cell broken on this page/column: no reformatting
            uint fswdirTable,                       // IN:  
            int dvrActual)                          // IN:  
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParagraph cellParagraph = PtsContext.HandleToObject(nmCell) as CellParagraph;
                PTS.ValidateHandle(cellParagraph);

                CellParaClient cellParaClient = PtsContext.HandleToObject(pfscell) as CellParaClient;
                PTS.ValidateHandle(cellParaClient);

                TableParaClient tableParaClient = PtsContext.HandleToObject(pfsparaclientTable) as TableParaClient;
                PTS.ValidateHandle(tableParaClient);

                cellParagraph.SetCellHeight(
                    cellParaClient, 
                    tableParaClient,
                    pfsbrkcell, 
                    fBrokenHere, 
                    fswdirTable, 
                    dvrActual);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }

        internal int DuplicateCellBreakRecord(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsbrkcell,                      // IN:  
            out IntPtr ppfsbrkcellDup)              // OUT: 
        {
            return PTS.FsDuplicateSubpageBreakRecord(Context, pfsbrkcell, out ppfsbrkcellDup);
        }

        internal int DestroyCellBreakRecord(
            IntPtr pfsclient,                       // IN:  
            IntPtr pfsbrkcell)                      // IN:  
        {
            return PTS.FsDestroySubpageBreakRecord(Context, pfsbrkcell);
        }
        internal int DestroyCell(
            IntPtr pfsCell)                         // IN:  
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParaClient cellParaClient = PtsContext.HandleToObject(pfsCell) as CellParaClient;
                if (cellParaClient != null)
                {
                    cellParaClient.Dispose();
                }
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetCellNumberFootnotes(
            IntPtr pfsCell,                         // IN:  
            out int cFtn)                           // OUT: 
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParaClient cellParaClient = PtsContext.HandleToObject(pfsCell) as CellParaClient;
                PTS.ValidateHandle(cellParaClient);

                // this is not implemented yet
                cFtn = 0;
            }
            catch (Exception e)
            {
                cFtn = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                cFtn = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }
        internal int GetCellMinColumnBalancingStep(
            IntPtr pfscell,                         // IN:
            uint fswdir,                            // IN:
            out int dvrMinStep)                     // OUT:
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParaClient cellParaClient = PtsContext.HandleToObject(pfscell) as CellParaClient;
                PTS.ValidateHandle(cellParaClient);
                //dvrMinStep = TextDpi.ToTextDpi(cellParaClient.Child.QueryLayoutSize().Height);
                dvrMinStep = TextDpi.ToTextDpi(1);
            }
            catch (Exception e)
            {
                dvrMinStep = 0;
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                dvrMinStep = 0;
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return (fserr);
        }

        internal int TransferDisplayInfoCell(
            IntPtr pfscellOld,            // IN:  opaque to PTS old paragraph client
            IntPtr pfscellNew)            // IN:  opaque to PTS new paragraph client
        {
            int fserr = PTS.fserrNone;
            try
            {
                CellParaClient paraClientOld = PtsContext.HandleToObject(pfscellOld) as CellParaClient;
                PTS.ValidateHandle(paraClientOld);
                CellParaClient paraClientNew = PtsContext.HandleToObject(pfscellNew) as CellParaClient;
                PTS.ValidateHandle(paraClientNew);
                paraClientNew.TransferDisplayInfo(paraClientOld);
            }
            catch (Exception e)
            {
                PtsContext.CallbackException = e;
                fserr = PTS.fserrCallbackException;
            }
            catch
            {
                PtsContext.CallbackException = new System.Exception("Caught a non CLS Exception");
                fserr = PTS.fserrCallbackException;
            }
            return fserr;
        }

        #endregion PTS callbacks
    }
}

#pragma warning enable 6500
#pragma warning enable 1634, 1691

