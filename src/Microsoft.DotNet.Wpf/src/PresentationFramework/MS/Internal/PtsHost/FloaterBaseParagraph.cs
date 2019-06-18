// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: FloaterBaseParagraph class provides a wrapper for floater 
//              and UIElement objects.
//
#pragma warning disable 1634, 1691  // avoid generating warnings about unknown
// message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Security;              // SecurityCritical
using System.Windows;
using System.Windows.Documents;
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // FloaterBaseParagraph class provides a wrapper for floater and UIElement objects.
    // ----------------------------------------------------------------------
    internal abstract class FloaterBaseParagraph : BaseParagraph
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      element - Element associated with paragraph.
        //      structuralCache - Content's structural cache
        // ------------------------------------------------------------------
        protected FloaterBaseParagraph(TextElement element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  PTS callbacks
        //
        //-------------------------------------------------------------------

        #region PTS callbacks

        // ------------------------------------------------------------------
        // UpdGetParaChange
        // ------------------------------------------------------------------
        internal override void UpdGetParaChange(
            out PTS.FSKCHANGE fskch,            // OUT: kind of change
            out int fNoFurtherChanges)          // OUT: no changes after?
        {
            fskch = PTS.FSKCHANGE.fskchNew;
            fNoFurtherChanges = PTS.FromBoolean(_stopAsking);
        }

        //-------------------------------------------------------------------
        // GetParaProperties
        //-------------------------------------------------------------------
        internal override void GetParaProperties(
            ref PTS.FSPAP fspap)        // OUT: paragraph properties
        {
            GetParaProperties(ref fspap, false);
            fspap.idobj = PtsHost.FloaterParagraphId;
        }

        //-------------------------------------------------------------------
        // CreateParaclient
        //-------------------------------------------------------------------
        internal override abstract void CreateParaclient(
            out IntPtr paraClientHandle);       // OUT: opaque to PTS paragraph client
        
        //-------------------------------------------------------------------
        // CollapseMargin
        //-------------------------------------------------------------------
        internal override abstract void CollapseMargin(
            BaseParaClient paraClient,          // IN:
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            uint fswdir,                        // IN:  current direction (of the track, in which margin collapsing is happening)
            bool suppressTopSpace,              // IN:  suppress empty space at the top of page
            out int dvr);                        // OUT: dvr, calculated based on margin collapsing state
       
        //-------------------------------------------------------------------
        // GetFloaterProperties
        //-------------------------------------------------------------------
        internal abstract void GetFloaterProperties(
            uint fswdirTrack,                       // IN:  direction of track
            out PTS.FSFLOATERPROPS fsfloaterprops);  // OUT: properties of the floater
        

        //-------------------------------------------------------------------
        // GetFloaterPolygons
        //-------------------------------------------------------------------
        internal unsafe virtual void GetFloaterPolygons(
            FloaterBaseParaClient paraClient,       // IN:
            uint fswdirTrack,                   // IN:  direction of Track
            int ncVertices,                     // IN:  size of array of vertex counts (= number of polygons)
            int nfspt,                          // IN:  size of the array of all vertices
            int* rgcVertices,                   // OUT: array of vertex counts (array containing number of vertices for each polygon)
            out int ccVertices,                 // OUT: actual number of vertex counts
            PTS.FSPOINT* rgfspt,                // OUT: array of all vertices
            out int cfspt,                      // OUT: actual total number of vertices in all polygons
            out int fWrapThrough)               // OUT: fill text in empty areas within obstacles?
        {
            Debug.Assert(false, "Tight wrap is not currently supported.");
            ccVertices = cfspt = fWrapThrough = 0;
        }

        //-------------------------------------------------------------------
        // FormatFloaterContentFinite
        //-------------------------------------------------------------------
        internal abstract void FormatFloaterContentFinite(
            FloaterBaseParaClient paraClient,       // IN:
            IntPtr pbrkrecIn,                   // IN:  break record---use if !IntPtr.Zero
            int fBRFromPreviousPage,            // IN:  break record was created on previous page
            IntPtr footnoteRejector,            // IN: 
            int fEmptyOk,                       // IN:  is it OK not to add anything?
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of Track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA fsksuppresshardbreakbeforefirstparaIn,
                                                // IN: suppress breaks at track start?
            out PTS.FSFMTR fsfmtr,              // OUT: result of formatting
            out IntPtr pfsFloatContent,         // OUT: opaque for PTS pointer pointer to formatted content
            out IntPtr pbrkrecOut,              // OUT: pointer to the floater content break record
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices);                 // OUT: total number of vertices in all polygons
       
        //-------------------------------------------------------------------
        // FormatFloaterContentBottomless
        //-------------------------------------------------------------------
        internal abstract void FormatFloaterContentBottomless(
            FloaterBaseParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out IntPtr pfsFloatContent,         // OUT: opaque for PTS pointer pointer to formatted content
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices);                 // OUT: total number of vertices in all polygons
        
        //-------------------------------------------------------------------
        // FormatFloaterContentBottomless
        //-------------------------------------------------------------------
        internal abstract void UpdateBottomlessFloaterContent(
            FloaterBaseParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            IntPtr pfsFloatContent,             // IN: floater content
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices);                 // OUT: total number of vertices in all polygons

        //-------------------------------------------------------------------
        // GetMCSClientAfterFloater
        //-------------------------------------------------------------------
        internal abstract void GetMCSClientAfterFloater(
            uint fswdirTrack,                   // IN:  direction of Track
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            out IntPtr pmcsclientOut);         // OUT: MCSCLIENT that floater will return to track

        //-------------------------------------------------------------------
        // GetDvrUsedForFloater
        //-------------------------------------------------------------------
        internal virtual void GetDvrUsedForFloater(
            uint fswdirTrack,                   // IN:  direction of Track
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            int dvrDisplaced,                   // IN: 
            out int dvrUsed)                    // OUT:
        {
            // When floater is pushed down due to collision, text may need to be
            // pushed down together with the floater. In such case dvrUsed needs to be 
            // set to height of the floater.
            // But for now there is no case, where we need this feature, hence dvrUsed is
            // always 0.
            dvrUsed = 0;
        }

        #endregion PTS callbacks
    }
}

#pragma warning enable 1634, 1691
