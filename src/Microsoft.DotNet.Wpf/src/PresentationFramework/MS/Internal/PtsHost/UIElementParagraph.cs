// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 
// Description: UIElementParagraph class provides a wrapper for UIElements.
//
#pragma warning disable 1634, 1691  // avoid generating warnings about unknown
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Collections;                       // IEnumerator
using System.Security;                          // SecurityCritical
using System.Windows;                           // UIElement
using System.Windows.Documents;                 // BlockUIContainer
using MS.Internal.Text;                         // TextDpi
using MS.Internal.Documents;                    // UIElementIsland
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// UIElementParagraph class provides a wrapper for UIElement objects.
    /// </summary>
    internal sealed class UIElementParagraph : FloaterBaseParagraph
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="element">Element associated with paragraph.</param>
        /// <param name="structuralCache">Content's structural cache.</param>
        internal UIElementParagraph(TextElement element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Dispose the object and release handle. 
        /// </summary>
        public override void Dispose()
        {
            ClearUIElementIsland();

            base.Dispose();
        }

        /// <summary>
        /// Invalidate content's structural cache. Returns: 'true' if entire paragraph 
        /// is invalid.
        /// </summary>
        /// <param name="startPosition">
        /// Position to start invalidation from.
        /// </param>
        internal override bool InvalidateStructure(int startPosition)
        {
            if (_uiElementIsland != null)
            {
                _uiElementIsland.DesiredSizeChanged -= new DesiredSizeChangedEventHandler(OnUIElementDesiredSizeChanged);
                _uiElementIsland.Dispose();
                _uiElementIsland = null;
            }
            return base.InvalidateStructure(startPosition);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  PTS callbacks
        //
        //-------------------------------------------------------------------

        #region PTS callbacks

        //-------------------------------------------------------------------
        // CreateParaclient
        //-------------------------------------------------------------------
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)        // OUT: opaque to PTS paragraph client
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. FloaterParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            UIElementParaClient paraClient = new UIElementParaClient(this);
            paraClientHandle = paraClient.Handle;
#pragma warning restore 6518
        }

        //-------------------------------------------------------------------
        // CollapseMargin
        //-------------------------------------------------------------------
        internal override void CollapseMargin(
            BaseParaClient paraClient,          // IN:
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            uint fswdir,                        // IN:  current direction (of the track, in which margin collapsing is happening)
            bool suppressTopSpace,              // IN:  suppress empty space at the top of page
            out int dvr)                        // OUT: dvr, calculated based on margin collapsing state
        {
            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
            MarginCollapsingState mcsNew;
            int margin;
            MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsNew, out margin);
            
            if (suppressTopSpace)
            {
                dvr = 0;
            }
            else
            {
                dvr = margin;
                if (mcsNew != null)
                {
                    dvr += mcsNew.Margin;
                }
            }
            if (mcsNew != null)
            {
                mcsNew.Dispose();
            }
        }

        //-------------------------------------------------------------------
        // GetFloaterProperties
        //-------------------------------------------------------------------
        internal override void GetFloaterProperties(
            uint fswdirTrack,                       // IN:  direction of track
            out PTS.FSFLOATERPROPS fsfloaterprops)  // OUT: properties of the floater
        {
            fsfloaterprops = new PTS.FSFLOATERPROPS();
            fsfloaterprops.fFloat = PTS.False;                     // Floater
            fsfloaterprops.fskclear = PTS.WrapDirectionToFskclear((WrapDirection)Element.GetValue(Block.ClearFloatersProperty));

            // Set alignment to left alignment. Do not allow text wrap on any side
            fsfloaterprops.fskfloatalignment = PTS.FSKFLOATALIGNMENT.fskfloatalignMin;
            fsfloaterprops.fskwr = PTS.FSKWRAP.fskwrNone;

            // Always delay UIElement if there is no progress
            fsfloaterprops.fDelayNoProgress = PTS.True;
        }

        //-------------------------------------------------------------------
        // FormatFloaterContentFinite
        //-------------------------------------------------------------------
        internal override void FormatFloaterContentFinite(
            FloaterBaseParaClient paraClient,   // IN:
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
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            Invariant.Assert(paraClient is UIElementParaClient);
            Invariant.Assert(Element is BlockUIContainer);

            if (fAtMaxWidth == PTS.False && fEmptyOk == PTS.True)
            {
                // Do not format if not at max width, and if fEmptyOk is true
                durFloaterWidth = dvrFloaterHeight = 0;
                cPolygons = cVertices = 0;
                fsfmtr = new PTS.FSFMTR();
                fsfmtr.kstop = PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace;
                fsfmtr.fContainsItemThatStoppedBeforeFootnote = PTS.False;
                fsfmtr.fForcedProgress = PTS.False;
                fsbbox = new PTS.FSBBOX();
                fsbbox.fDefined = PTS.False;
                pbrkrecOut = IntPtr.Zero;
                pfsFloatContent = IntPtr.Zero;
            }
            else
            {
                cPolygons = cVertices = 0;
                // Formatting is not at max width but proceeds anyway because adding no content is not allowed
                fsfmtr.fForcedProgress = PTS.FromBoolean(fAtMaxWidth == PTS.False);

                // Format UIElement
                if (((BlockUIContainer)Element).Child != null)
                {
                    EnsureUIElementIsland();
                    FormatUIElement(durAvailable, out fsbbox);
                }
                else
                {
                    // Child elementis null. Create fsbbox only with border and padding info
                    ClearUIElementIsland();

                    MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
                    fsbbox.fsrc = new PTS.FSRECT();
                    fsbbox.fsrc.du = durAvailable;
                    fsbbox.fsrc.dv = mbp.BPTop + mbp.BPBottom;
                }

                durFloaterWidth = fsbbox.fsrc.du;
                dvrFloaterHeight = fsbbox.fsrc.dv;
                if (dvrAvailable < dvrFloaterHeight && fEmptyOk == PTS.True)
                {
                    // Will not fit in available space. Since fEmptyOk is true, we can return null floater
                    durFloaterWidth = dvrFloaterHeight = 0;
                    fsfmtr = new PTS.FSFMTR();
                    fsfmtr.kstop = PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace;
                    fsbbox = new PTS.FSBBOX();
                    fsbbox.fDefined = PTS.False;
                    pfsFloatContent = IntPtr.Zero;
                }
                else
                {
                    // Either floater fits in available space or it doesn't but we cannot return nothing. 
                    // Use the space needed, and set formatter result to forced progress if BUC does not fit in available space.
                    fsbbox.fDefined = PTS.True;
                    pfsFloatContent = paraClient.Handle;
                    if (dvrAvailable < dvrFloaterHeight)
                    {
                        // Indicate that progress was forced
                        Invariant.Assert(fEmptyOk == PTS.False);
                        fsfmtr.fForcedProgress = PTS.True;
                    }
                    fsfmtr.kstop = PTS.FSFMTRKSTOP.fmtrGoalReached;
                }

                // Set output values for floater content
                pbrkrecOut = IntPtr.Zero;
                fsfmtr.fContainsItemThatStoppedBeforeFootnote = PTS.False;
            }
        }

        //-------------------------------------------------------------------
        // FormatFloaterContentBottomless
        //-------------------------------------------------------------------
        internal override void FormatFloaterContentBottomless(
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
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            Invariant.Assert(paraClient is UIElementParaClient);
            Invariant.Assert(Element is BlockUIContainer);

            if (fAtMaxWidth == PTS.False)
            {
                // BlockUIContainer is only formatted at full column width
                // Set foater width, height to be greater than available values to signal to PTS that floater does not fit in the space
                durFloaterWidth = durAvailable + 1;
                dvrFloaterHeight = dvrAvailable + 1;
                cPolygons = cVertices = 0;
                fsfmtrbl = PTS.FSFMTRBL.fmtrblInterrupted;
                fsbbox = new PTS.FSBBOX();
                fsbbox.fDefined = PTS.False;
                pfsFloatContent = IntPtr.Zero;
            }
            else
            {
                cPolygons = cVertices = 0;

                // Format UIElement
                if (((BlockUIContainer)Element).Child != null)
                {
                    EnsureUIElementIsland();
                    FormatUIElement(durAvailable, out fsbbox);

                    // Set output values for floater content
                    pfsFloatContent = paraClient.Handle;
                    fsfmtrbl = PTS.FSFMTRBL.fmtrblGoalReached;
                    fsbbox.fDefined = PTS.True;
                    durFloaterWidth = fsbbox.fsrc.du;
                    dvrFloaterHeight = fsbbox.fsrc.dv;
                }
                else
                {
                    ClearUIElementIsland();

                    MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
                    fsbbox.fsrc = new PTS.FSRECT();
                    fsbbox.fsrc.du = durAvailable;
                    fsbbox.fsrc.dv = mbp.BPTop + mbp.BPBottom;
                    fsbbox.fDefined = PTS.True;
                    pfsFloatContent = paraClient.Handle;
                    fsfmtrbl = PTS.FSFMTRBL.fmtrblGoalReached;
                    durFloaterWidth = fsbbox.fsrc.du;
                    dvrFloaterHeight = fsbbox.fsrc.dv;
                }
            }
        }

        //-------------------------------------------------------------------
        // UpdateBottomlessFloaterContent
        //-------------------------------------------------------------------
        internal override void UpdateBottomlessFloaterContent(
            FloaterBaseParaClient paraClient,       // IN:
            int fSuppressTopSpace,              // IN:  suppress empty space at the top of the page
            uint fswdir,                        // IN:  direction of track
            int fAtMaxWidth,                    // IN:  formating is at full width of column
            int durAvailable,                   // IN:  width of available space
            int dvrAvailable,                   // IN:  height of available space
            IntPtr pfsFloatContent,             // IN:  floater content (in UIElementParagraph, this is an alias to the paraClient)
            out PTS.FSFMTRBL fsfmtrbl,          // OUT: result of formatting
            out int durFloaterWidth,            // OUT: floater width
            out int dvrFloaterHeight,           // OUT: floater height
            out PTS.FSBBOX fsbbox,              // OUT: floater bbox
            out int cPolygons,                  // OUT: number of polygons
            out int cVertices)                  // OUT: total number of vertices in all polygons
        {
            // This implementation simply calls into format floater content bottomless. As para has no real content, this is ok.
            FormatFloaterContentBottomless(paraClient, fSuppressTopSpace, fswdir, fAtMaxWidth, durAvailable, dvrAvailable, out fsfmtrbl, out pfsFloatContent,
                                           out durFloaterWidth, out dvrFloaterHeight, out fsbbox, out cPolygons, out cVertices);
        }


        //-------------------------------------------------------------------
        // GetMCSClientAfterFloater
        //-------------------------------------------------------------------
        internal override void GetMCSClientAfterFloater(
            uint fswdirTrack,                   // IN:  direction of Track
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            out IntPtr pmcsclientOut)           // OUT: MCSCLIENT that floater will return to track
        {
            MarginCollapsingState mcsNew;
            int margin;
            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
            MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, null, out mcsNew, out margin);
            if (mcsNew != null)
            {
                pmcsclientOut = mcsNew.Handle;
            }
            else
            {
                pmcsclientOut = IntPtr.Zero;
            }
        }

        #endregion PTS callbacks

        //-------------------------------------------------------------------
        //
        // Private methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Format UIElement
        /// </summary>
        private void FormatUIElement(int durAvailable, out PTS.FSBBOX fsbbox)
        {
            MbpInfo mbp = MbpInfo.FromElement(Element, StructuralCache.TextFormatterHost.PixelsPerDip);
            double elementHeight;
            double elementWidth = TextDpi.FromTextDpi(Math.Max(1, durAvailable - (mbp.MBPLeft + mbp.MBPRight)));

            if (SizeToFigureParent)
            {
                // Only child of a figure whose height is set. Size to figure's page height less figure's MBP and BlockUIContainer's MBP
                // NOTE: BlockUIContainer margins are always extracted before formatting, either from Figure height or page height
                if (StructuralCache.CurrentFormatContext.FinitePage)
                {
                    elementHeight = StructuralCache.CurrentFormatContext.PageHeight;
                }
                else
                {
                    Figure figure = (Figure) ((BlockUIContainer)Element).Parent;
                    Invariant.Assert(figure.Height.IsAbsolute);
                    elementHeight = figure.Height.Value;
                }

                elementHeight = Math.Max(TextDpi.FromTextDpi(1), elementHeight - TextDpi.FromTextDpi(mbp.MBPTop + mbp.MBPBottom));
                UIElementIsland.DoLayout(new Size(elementWidth, elementHeight), false, false);
   
                // Create fsbbox. Set width to available width since we want block ui container to occupy the full column
                // and UIElement to be algined within it. Set dv to elementHeight. 
                fsbbox.fsrc = new PTS.FSRECT();
                fsbbox.fsrc.du = durAvailable;
                fsbbox.fsrc.dv = TextDpi.ToTextDpi(elementHeight) + mbp.BPTop + mbp.BPBottom;
                fsbbox.fDefined = PTS.True;
            }
            else
            {
                // Either BlockUIContainer is not the only child of a figure or the figure's height is unspecified.
                // In this case, size to height of strcutural cache's current page less page margins and container MBP.
                // This is consistent with figure's behavior on sizing to content

                // Always measure at infinity for bottomless, consistent constraint.
                if (StructuralCache.CurrentFormatContext.FinitePage)
                {
                    Thickness pageMargin = StructuralCache.CurrentFormatContext.DocumentPageMargin;
                    elementHeight = StructuralCache.CurrentFormatContext.DocumentPageSize.Height - pageMargin.Top - pageMargin.Bottom - TextDpi.FromTextDpi(mbp.MBPTop + mbp.MBPBottom);
                    elementHeight = Math.Max(TextDpi.FromTextDpi(1), elementHeight);
                }
                else
                {
                    elementHeight = Double.PositiveInfinity;
                }

                Size uiIslandSize = UIElementIsland.DoLayout(new Size(elementWidth, elementHeight), false, true);

                // Create fsbbox. Set width to available width since we want block ui container to occupy the full column
                // and UIElement to be algined within it
                fsbbox.fsrc = new PTS.FSRECT();
                fsbbox.fsrc.du = durAvailable;
                fsbbox.fsrc.dv = TextDpi.ToTextDpi(uiIslandSize.Height) + mbp.BPTop + mbp.BPBottom;
                fsbbox.fDefined = PTS.True;
            }
        }

        /// <summary>
        /// Create UIElementIsland representing embedded Element Layout island within content world.
        /// </summary>
        private void EnsureUIElementIsland()
        {
            if (_uiElementIsland == null)
            {
                _uiElementIsland = new UIElementIsland(((BlockUIContainer)Element).Child);
                _uiElementIsland.DesiredSizeChanged += new DesiredSizeChangedEventHandler(OnUIElementDesiredSizeChanged);
            }
        }

        /// <summary>
        /// Clear UIElementIsland, unsubscribing from events, etc.
        /// </summary>
        private void ClearUIElementIsland()
        {
            try
            {
                if (_uiElementIsland != null)
                {
                    _uiElementIsland.DesiredSizeChanged -= new DesiredSizeChangedEventHandler(OnUIElementDesiredSizeChanged);
                    _uiElementIsland.Dispose();
                }
            }
            finally
            {
                _uiElementIsland = null;
            }
        }

        /// <summary>
        /// Handler for DesiredSizeChanged raised by UIElement island.
        /// </summary>
        private void OnUIElementDesiredSizeChanged(object sender, DesiredSizeChangedEventArgs e)
        {
            StructuralCache.FormattingOwner.OnChildDesiredSizeChanged(e.Child);
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// UIElementIsland representing embedded Element Layout island within content world.
        /// </summary>
        internal UIElementIsland UIElementIsland
        {
            get
            {
                return _uiElementIsland;
            }
        }

        /// <summary>
        /// Whether sizing to fit entire figure area.
        /// </summary>
        private bool SizeToFigureParent
        {
            get
            {
                // Check for auto value for figure height.
                // If figure height is auto we do not size to it.
                if (!IsOnlyChildOfFigure)
                {
                    return false;
                }

                Figure figure = (Figure)((BlockUIContainer)Element).Parent;
                if (figure.Height.IsAuto)
                {
                    return false;
                }
                if (!StructuralCache.CurrentFormatContext.FinitePage && !figure.Height.IsAbsolute)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Whether this block is the only child of figure.
        /// </summary>
        private bool IsOnlyChildOfFigure
        {
            get
            {
                DependencyObject parent = ((BlockUIContainer)Element).Parent;
                if (parent is Figure)
                {
                    Figure figure = parent as Figure;
                    if (figure.Blocks.FirstChild == figure.Blocks.LastChild &&
                        figure.Blocks.FirstChild == Element)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// UIElementIsland representing embedded ElementLayout island within content world.
        /// </summary>
        private UIElementIsland _uiElementIsland;

        #endregion Private Fields
    }
}

#pragma warning enable 1634, 1691

