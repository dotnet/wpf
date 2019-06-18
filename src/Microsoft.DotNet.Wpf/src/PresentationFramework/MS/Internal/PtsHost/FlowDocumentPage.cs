// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DocumentPage representing bottomless of finite page of
//              a PTS host (FlowDocument).
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Threading;         // Dispatcher
using MS.Internal.Documents;
using MS.Internal.Text;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    //-----------------------------------------------------------------------
    // DocumentPage representing bottomless or finite page of a PTS host.
    //-----------------------------------------------------------------------
    internal sealed class FlowDocumentPage : DocumentPage, IServiceProvider, IDisposable, IContentHost
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        //-------------------------------------------------------------------
        // Constructor.
        //
        //      structuralCache - context representing data
        //-------------------------------------------------------------------
        internal FlowDocumentPage(StructuralCache structuralCache) : base(null)
        {
            _structuralCache = structuralCache;
            _ptsPage = new PtsPage(structuralCache.Section);
        }

        // ------------------------------------------------------------------
        // Finalizer
        // ------------------------------------------------------------------
        ~FlowDocumentPage()
        {
            Dispose(false);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        //-------------------------------------------------------------------
        // Dispose the page.
        //-------------------------------------------------------------------
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            base.Dispose();
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        //-------------------------------------------------------------------
        // Visual node representing content of the page.
        //-------------------------------------------------------------------
        public override Visual Visual
        {
            get
            {
                if (IsDisposed)
                {
                    return null;
                }
                UpdateVisual();
                return base.Visual;
            }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        //-------------------------------------------------------------------
        // Format content into a single bottomless page.
        //
        //      pageSize - size of the page
        //-------------------------------------------------------------------
        internal void FormatBottomless(Size pageSize, Thickness pageMargin)
        {
            Invariant.Assert(!IsDisposed);

            // Every time full format is done reset formatted lines count to 0.
            _formattedLinesCount = 0;

            // Make sure that PTS limitations are not exceeded.
            TextDpi.EnsureValidPageSize(ref pageSize);
            _pageMargin = pageMargin;
            SetSize(pageSize);

            if(!DoubleUtil.AreClose(_lastFormatWidth, pageSize.Width) || !DoubleUtil.AreClose(_pageMargin.Left, pageMargin.Left) ||
               !DoubleUtil.AreClose(_pageMargin.Right, pageMargin.Right))
            {
                // No incremental update if width changes.
                _structuralCache.InvalidateFormatCache(false);
            }

            _lastFormatWidth = pageSize.Width;

            using(_structuralCache.SetDocumentFormatContext(this))
            {
                OnBeforeFormatPage();

                if (_ptsPage.PrepareForBottomlessUpdate())
                {
                    _structuralCache.CurrentFormatContext.PushNewPageData(pageSize, _pageMargin, true, false);
                    _ptsPage.UpdateBottomlessPage();
                }
                else
                {
                    _structuralCache.CurrentFormatContext.PushNewPageData(pageSize, _pageMargin, false, false);
                    _ptsPage.CreateBottomlessPage();
                }

                // In bottomless page scenario, need to update PageSize to reflect
                // calculated size of the page.
                pageSize = _ptsPage.CalculatedSize;
                pageSize.Width += pageMargin.Left + pageMargin.Right;
                pageSize.Height += pageMargin.Top + pageMargin.Bottom;
                SetSize(pageSize);
                SetContentBox(new Rect(pageMargin.Left, pageMargin.Top, _ptsPage.CalculatedSize.Width, _ptsPage.CalculatedSize.Height));
                _structuralCache.CurrentFormatContext.PopPageData();

                OnAfterFormatPage();

                _structuralCache.DetectInvalidOperation();
            }
        }

        //-------------------------------------------------------------------
        // Format content into a single finite page.
        //
        //       pageSize - size of the page
        //       pageMargin - margin of the page
        //       breakRecord - input BreakRecor for the page
        //
        // Returns: Returns output break record.
        //-------------------------------------------------------------------
        internal PageBreakRecord FormatFinite(Size pageSize, Thickness pageMargin, PageBreakRecord breakRecord)
        {
            Invariant.Assert(!IsDisposed);

            // Every time full format is done reset formatted lines count to 0.
            _formattedLinesCount = 0;

            // Make sure that PTS limitations are not exceeded.
            TextDpi.EnsureValidPageSize(ref pageSize);
            TextDpi.EnsureValidPageMargin(ref pageMargin, pageSize);

            double pageMarginAdjustment = PtsHelper.CalculatePageMarginAdjustment(_structuralCache, pageSize.Width - (pageMargin.Left + pageMargin.Right));
            if (!DoubleUtil.IsZero(pageMarginAdjustment))
            {
                // Potentially some FP drift here, as we're anticipating that our column count will now work out exactly. Add a small fraction back to prevent this
                pageMargin.Right += pageMarginAdjustment - (pageMarginAdjustment / 100.0);
            }
            _pageMargin = pageMargin;

            SetSize(pageSize);
            SetContentBox(new Rect(pageMargin.Left, pageMargin.Top,
                pageSize.Width - (pageMargin.Left + pageMargin.Right),
                pageSize.Height - (pageMargin.Top + pageMargin.Bottom)));

            using(_structuralCache.SetDocumentFormatContext(this))
            {
                OnBeforeFormatPage();

                if (_ptsPage.PrepareForFiniteUpdate(breakRecord))
                {
                    _structuralCache.CurrentFormatContext.PushNewPageData(pageSize, _pageMargin, true, true);
                    _ptsPage.UpdateFinitePage(breakRecord);
                }
                else
                {
                    _structuralCache.CurrentFormatContext.PushNewPageData(pageSize, _pageMargin, false, true);
                    _ptsPage.CreateFinitePage(breakRecord);
                }
                _structuralCache.CurrentFormatContext.PopPageData();

                OnAfterFormatPage();
                _structuralCache.DetectInvalidOperation();
            }

            return _ptsPage.BreakRecord;
        }

        //-------------------------------------------------------------------
        // Arrange the page contents.
        //-------------------------------------------------------------------
        internal void Arrange(Size partitionSize)
        {
            Invariant.Assert(!IsDisposed);

            _partitionSize = partitionSize;

            using(_structuralCache.SetDocumentArrangeContext(this))
            {
                _ptsPage.ArrangePage();
                _structuralCache.DetectInvalidOperation();
            }

            ValidateTextView();
        }

        //-------------------------------------------------------------------
        // Page update may be requested more than once before rendering is
        // done. But PTS is not able to merge update info.
        // To protect against loosing incremental changes delta, need
        // to force full formatting for the conent.
        //-------------------------------------------------------------------
        internal void ForceReformat()
        {
            Invariant.Assert(!IsDisposed);
            // Clear update info for PTS page.
            _ptsPage.ClearUpdateInfo();
            // Force reformat
            _structuralCache.ForceReformat = true;
        }

        //-------------------------------------------------------------------
        // Hit tests to the correct ContentElement within the ContentHost
        // that the mouse is over.
        //
        //       point - mouse coordinates relative to the ContentHost
        //
        // Returns: IInputElement from specified position.
        //-------------------------------------------------------------------
        internal IInputElement InputHitTestCore(Point point)
        {
            Invariant.Assert(!IsDisposed);

            // Core services require that the IInputElement returned from hittesting
            // is a UIElement or it has a parent that is a UIElement.
            // When using DocumentPageView.DocumentPaginator directly, we may run
            // into case when FlowDocument does not have a logical parent. In
            // such case it is better to disable all core services.
            DependencyObject frameworkParent = FrameworkElement.GetFrameworkParent(_structuralCache.FormattingOwner);
            if (frameworkParent == null)
            {
                return null;
            }

            IInputElement ie = null;
            if (this.IsLayoutDataValid)
            {
                // Transform point to PtsPage coordinate system.
                // NOTE: TransformToAncestor is safe (will never throw an exception).
                GeneralTransform transform = this.PageVisual.Child.TransformToAncestor(this.PageVisual);
                transform = transform.Inverse;

                // Hittest PtsPage only when transform can be inverted in order to calculate
                // point within PtsPage. If transform cannot be inverted, return the owner of this page.
                if (transform != null)
                {
                    point = transform.Transform(point);
                    ie = _ptsPage.InputHitTest(point);
                }
            }
            return (ie != null) ? ie : _structuralCache.FormattingOwner as IInputElement;
        }

        /// <summary>
        /// Returns rectangles for element. First finds element by navigating in FlowDocumentPage.
        /// If element is not found or if call to get rectangles from FlowDocumentPage returns null
        /// we return an empty collection. If the layout is not valid we return null.
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        /// <param name="isLimitedToTextView">
        /// Indicates whether search should be restricted only to those text segments within the page's text view
        /// </param>
        internal ReadOnlyCollection<Rect> GetRectanglesCore(ContentElement child, bool isLimitedToTextView)
        {
            Invariant.Assert(!IsDisposed);

            List<Rect> rectangles = new List<Rect>();
            Debug.Assert(child != null);
            if (IsLayoutDataValid)
            {
                TextPointer elementStart = FindElementPosition(child, isLimitedToTextView);
                if (elementStart != null)
                {
                    // Element exists within this Page, calculate its length
                    int elementStartOffset = _structuralCache.TextContainer.Start.GetOffsetToPosition(elementStart);
                    int elementLength = 1;
                    if (child is TextElement)
                    {
                        TextPointer elementEnd = new TextPointer(((TextElement)child).ElementEnd);
                        elementLength = elementStart.GetOffsetToPosition(elementEnd);
                    }

                    rectangles = _ptsPage.GetRectangles(child, elementStartOffset, elementLength);
                }
            }

            if(this.PageVisual != null && rectangles.Count > 0)
            {
                List<Rect> transformedRectangles = new List<Rect>(rectangles.Count);
                // NOTE: TransformToAncestor is safe (will never throw an exception).
                GeneralTransform transform = this.PageVisual.Child.TransformToAncestor(this.PageVisual);

                for(int index = 0; index < rectangles.Count; index++)
                {
                    transformedRectangles.Add(transform.TransformBounds(rectangles[index]));
                }

                rectangles = transformedRectangles;
            }

            // We should never return null for rectangles from public API, only empty ArrayList
            Invariant.Assert(rectangles != null);

            return new ReadOnlyCollection<Rect>(rectangles);
        }

        /// <summary>
        /// Returns elements hosted by the content host as an enumerator class
        /// </summary>
        internal IEnumerator<IInputElement> HostedElementsCore
        {
            get
            {
                if (IsLayoutDataValid)
                {
                    // At this point, we should create TextView if it doesn't exist
                    _textView = GetTextView();
                    Invariant.Assert(_textView != null && ((ITextView)_textView).TextSegments.Count > 0);
                    return new HostedElements(((ITextView)_textView).TextSegments);
                }
                else
                {
                    // Return empty collection
                    return new HostedElements(new ReadOnlyCollection<TextSegment>(new List<TextSegment>(0)));
                }
            }
        }

        // Floating element list
        internal ReadOnlyCollection<ParagraphResult> FloatingElementResults
        {
            get
            {
                List<ParagraphResult> floatingElements = new List<ParagraphResult>(0);
                List<BaseParaClient> floatingElementList = _ptsPage.PageContext.FloatingElementList;
                if (floatingElementList != null)
                {
                    for (int i = 0; i < floatingElementList.Count; i++)
                    {
                        ParagraphResult paragraphResult = floatingElementList[i].CreateParagraphResult();
                        floatingElements.Add(paragraphResult);
                    }
                }
                return new ReadOnlyCollection<ParagraphResult>(floatingElements);
            }
        }

        /// <summary>
        /// Called when a UIElement-derived class which is hosted by a IContentHost changes it’s DesiredSize
        /// </summary>
        /// <param name="child">
        /// Child element whose DesiredSize has changed
        /// </param>
        internal void OnChildDesiredSizeChangedCore(UIElement child)
        {
            _structuralCache.FormattingOwner.OnChildDesiredSizeChanged(child);
        }

        //-------------------------------------------------------------------
        // Returns a new collection of ColumnResults for the page. Will always
        // have at least one column.
        //  hasTextContent -  True if any column in the page has text
        //                    content, i.e. does not contain only figures/floaters
        //-------------------------------------------------------------------
        internal ReadOnlyCollection<ColumnResult> GetColumnResults(out bool hasTextContent)
        {
            Invariant.Assert(!IsDisposed);
            List<ColumnResult> columnResults = new List<ColumnResult>(0);

            // hasTextContent is set to true if any of the columns in the page has text content. This is determined by checking the columns'
            // paragraph collections
            hasTextContent = false;

            // There are 3 cases:
            // (1) PTS page is not created - no columns are available.
            // (2) PTS page - use page PTS APIs to get columns.
            if (_ptsPage.PageHandle == IntPtr.Zero)
            {
                // (1) PTS page is not created
            }
            else
            {
               // (2) PTS page - use page PTS APIs to get columns.
                PTS.FSPAGEDETAILS pageDetails;
                PTS.Validate(PTS.FsQueryPageDetails(StructuralCache.PtsContext.Context, _ptsPage.PageHandle, out pageDetails));

                // There are 2 different types of PTS page:
                // (a) simple page (contains only one track) - 1 column.
                // (b) complex page (contains header, page body, footnotes and footer) - get columns
                //     from the page body.
                if (PTS.ToBoolean(pageDetails.fSimple))
                {
                    // (a) simple page (contains only one track) - 1 column.
                    PTS.FSTRACKDETAILS trackDetails;
                    PTS.Validate(PTS.FsQueryTrackDetails(StructuralCache.PtsContext.Context, pageDetails.u.simple.trackdescr.pfstrack, out trackDetails));
                    if (trackDetails.cParas > 0)
                    {
                        columnResults = new List<ColumnResult>(1);
                        ColumnResult columnResult = new ColumnResult(this, ref pageDetails.u.simple.trackdescr, new Vector());
                        columnResults.Add(columnResult);
                        if (columnResult.HasTextContent)
                        {
                            hasTextContent = true;
                        }
                    }
                }
                else if (pageDetails.u.complex.cSections > 0)
                {
                    // (b) complex page (contains header, page body, footnotes and footer) - get columns
                    //     from the page body.
                    Debug.Assert(pageDetails.u.complex.cSections == 1); // Only one section is supported right now.

                    // Retrieve description for each section.
                    PTS.FSSECTIONDESCRIPTION[] arraySectionDesc;
                    PtsHelper.SectionListFromPage(StructuralCache.PtsContext, _ptsPage.PageHandle, ref pageDetails, out arraySectionDesc);

                    // Get section details
                    PTS.FSSECTIONDETAILS sectionDetails;
                    PTS.Validate(PTS.FsQuerySectionDetails(StructuralCache.PtsContext.Context, arraySectionDesc[0].pfssection, out sectionDetails));

                    // There are 2 types of sections:
                    // (1) with page notes - footnotes in section treated as endnotes
                    // (2) with column notes - footnotes in section treated as column notes
                    if (PTS.ToBoolean(sectionDetails.fFootnotesAsPagenotes))
                    {
                        // (1) with page notes - footnotes in section treated as endnotes
                        Debug.Assert(sectionDetails.u.withpagenotes.cEndnoteColumns == 0); // Footnotes are not supported yet.

                        Debug.Assert(sectionDetails.u.withpagenotes.cSegmentDefinedColumnSpanAreas == 0);
                        Debug.Assert(sectionDetails.u.withpagenotes.cHeightDefinedColumnSpanAreas == 0);

                        // cBasicColumns == 0, means that section content is empty.
                        // In such case there is nothing to render.
                        if (sectionDetails.u.withpagenotes.cBasicColumns > 0)
                        {
                            // Retrieve description for each column.
                            PTS.FSTRACKDESCRIPTION[] arrayColumnDesc;
                            PtsHelper.TrackListFromSection(StructuralCache.PtsContext, arraySectionDesc[0].pfssection, ref sectionDetails, out arrayColumnDesc);

                            columnResults = new List<ColumnResult>(sectionDetails.u.withpagenotes.cBasicColumns);
                            for (int i = 0; i < arrayColumnDesc.Length; i++)
                            {
                                PTS.FSTRACKDESCRIPTION columnDesc = arrayColumnDesc[i];

                                // Column may have null track, in which case we should not add it
                                if (columnDesc.pfstrack != IntPtr.Zero)
                                {
                                    PTS.FSTRACKDETAILS trackDetails;
                                    PTS.Validate(PTS.FsQueryTrackDetails(StructuralCache.PtsContext.Context, columnDesc.pfstrack, out trackDetails));
                                    if (trackDetails.cParas > 0)
                                    {
                                        ColumnResult columnResult = new ColumnResult(this, ref columnDesc, new Vector());
                                        columnResults.Add(columnResult);
                                        if (columnResult.HasTextContent)
                                        {
                                            hasTextContent = true;
                                        }
                                    }
                                }
                            }
                        }
                        // else; section empty => no columns
                    }
                    else
                    {
                        // (2) with column notes - footnotes in section treated as column notes
                        Debug.Assert(false); // Complex columns are not supported yet.
                    }
                }
            }

            Invariant.Assert(columnResults != null);
            return new ReadOnlyCollection<ColumnResult>(columnResults);
        }

        //-------------------------------------------------------------------
        // Retrieves text range for contents of the column represented
        // by 'pfstrack'.
        //
        //      pfstrack - pointer to PTS track representing a column
        //
        // Returns: text range for contents of the column represented by 'pfstrack'
        //-------------------------------------------------------------------
        internal TextContentRange GetTextContentRangeFromColumn(IntPtr pfstrack)
        {
            Invariant.Assert(!IsDisposed);
            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(StructuralCache.PtsContext.Context, pfstrack, out trackDetails));

            // Combine ranges from all nested paragraphs.
            TextContentRange textContentRange = new TextContentRange();
            if (trackDetails.cParas != 0)
            {
                PTS.FSPARADESCRIPTION[] arrayParaDesc;
                PtsHelper.ParaListFromTrack(StructuralCache.PtsContext, pfstrack, ref trackDetails, out arrayParaDesc);

                // Merge TextContentRanges for all paragraphs
                BaseParaClient paraClient;
                for (int i = 0; i < arrayParaDesc.Length; i++)
                {
                    paraClient = this.StructuralCache.PtsContext.HandleToObject(arrayParaDesc[i].pfsparaclient) as BaseParaClient;
                    PTS.ValidateHandle(paraClient);
                    textContentRange.Merge(paraClient.GetTextContentRange());
                }
            }
            return textContentRange;
        }

        //-------------------------------------------------------------------
        // Returns a collection of ParagraphResults for the column's paragraphs.
        //
        //      pfstrack - pointer to PTS track representing a column
        //      parentOffset - parent offset from the top of the page
        //      hasTextContent - true if any paragraph in the column has some text content
        //
        // Returns: collection of ParagraphResults for the column's paragraphs
        //-------------------------------------------------------------------
        internal ReadOnlyCollection<ParagraphResult> GetParagraphResultsFromColumn(IntPtr pfstrack, Vector parentOffset, out bool hasTextContent)
        {
            Invariant.Assert(!IsDisposed);
            // Get track details
            PTS.FSTRACKDETAILS trackDetails;
            PTS.Validate(PTS.FsQueryTrackDetails(StructuralCache.PtsContext.Context, pfstrack, out trackDetails));
            hasTextContent = false;

            if (trackDetails.cParas == 0)
            {
                return new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));
            }

            PTS.FSPARADESCRIPTION[] arrayParaDesc;
            PtsHelper.ParaListFromTrack(StructuralCache.PtsContext, pfstrack, ref trackDetails, out arrayParaDesc);

            List<ParagraphResult> paragraphResults = new List<ParagraphResult>(arrayParaDesc.Length);
            for (int i = 0; i < arrayParaDesc.Length; i++)
            {
                BaseParaClient paraClient = StructuralCache.PtsContext.HandleToObject(arrayParaDesc[i].pfsparaclient) as BaseParaClient;
                PTS.ValidateHandle(paraClient);
                ParagraphResult paragraphResult = paraClient.CreateParagraphResult();
                if (paragraphResult.HasTextContent)
                {
                    hasTextContent = true;
                }
                paragraphResults.Add(paragraphResult);
            }
            return new ReadOnlyCollection<ParagraphResult>(paragraphResults);
        }

        //-------------------------------------------------------------------
        // Notification about new line being formatted.
        //-------------------------------------------------------------------
        internal void OnFormatLine()
        {
            Invariant.Assert(!IsDisposed);
            ++_formattedLinesCount;
        }

        //-------------------------------------------------------------------
        // Ensures visual structure for this document page is clean
        //-------------------------------------------------------------------
        internal void EnsureValidVisuals()
        {
            Invariant.Assert(!IsDisposed);
            UpdateVisual();
        }

        //-------------------------------------------------------------------
        // Update the viewport
        //-------------------------------------------------------------------
        internal void UpdateViewport(ref PTS.FSRECT viewport, bool drawBackground)
        {
            Rect contentViewport;

            // Transform point to PtsPage coordinate system.
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            GeneralTransform transform = this.PageVisual.Child.TransformToAncestor(this.PageVisual);
            transform = transform.Inverse;

            contentViewport = viewport.FromTextDpi();
            if (transform != null)
            {
                contentViewport = transform.TransformBounds(contentViewport);
            }

            if(!IsDisposed)
            {
                // Draw background
                if (drawBackground)
                {
                    this.PageVisual.DrawBackground((Brush)_structuralCache.PropertyOwner.GetValue(FlowDocument.BackgroundProperty), contentViewport);
                }

                using (_structuralCache.SetDocumentVisualValidationContext(this))
                {
                    PTS.FSRECT contentViewportTextDpi = new PTS.FSRECT(contentViewport);
                    _ptsPage.UpdateViewport(ref contentViewportTextDpi);

                    _structuralCache.DetectInvalidOperation();
                }

                ValidateTextView();
            }
        }

        #endregion Internal methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        //-------------------------------------------------------------------
        // Is being used in a plain text box?
        //-------------------------------------------------------------------
        internal bool UseSizingWorkaroundForTextBox
        {
            get { return _ptsPage.UseSizingWorkaroundForTextBox; }
            set { _ptsPage.UseSizingWorkaroundForTextBox = value; }
        }


        //-------------------------------------------------------------------
        // Margin of the page.
        //-------------------------------------------------------------------
        internal Thickness Margin { get { return _pageMargin; } }

        //-------------------------------------------------------------------
        // Is this page already disposed?
        //-------------------------------------------------------------------
        internal bool IsDisposed { get { return (_disposed != 0) || _structuralCache.PtsContext.Disposed; } }

        //-------------------------------------------------------------------
        // Size of content on page.
        //-------------------------------------------------------------------
        internal Size ContentSize
        {
            get
            {
                Size size = _ptsPage.ContentSize;
                size.Width += _pageMargin.Left + _pageMargin.Right;
                size.Height += _pageMargin.Top + _pageMargin.Bottom;
                return size;
            }
        }

        //-------------------------------------------------------------------
        // Is it finite page or bottomless?
        //-------------------------------------------------------------------
        internal bool FinitePage { get { return _ptsPage.FinitePage; } }

        //-------------------------------------------------------------------
        // Page context
        //-------------------------------------------------------------------
        internal PageContext PageContext { get { return _ptsPage.PageContext; } }

        //-------------------------------------------------------------------
        // Is during incremental update mode?
        //-------------------------------------------------------------------
        internal bool IncrementalUpdate { get { return _ptsPage.IncrementalUpdate; } }

        //-------------------------------------------------------------------
        // StructuralCache associated with this page.
        //-------------------------------------------------------------------
        internal StructuralCache StructuralCache { get { return _structuralCache; } }

        //-------------------------------------------------------------------
        // Number of lines formatted during page formatting.
        //-------------------------------------------------------------------
        internal int FormattedLinesCount { get { return _formattedLinesCount; } }

        //-------------------------------------------------------------------
        // Is layout data is in a valid state.
        //-------------------------------------------------------------------
        internal bool IsLayoutDataValid
        {
            get
            {
                bool layoutDataValid = false;
                if (!IsDisposed)
                {
                    // In case of any content/properties changes FlowDocument does BreakRecordTable
                    // management and disposes any affected pages. So it is unnecessary to check
                    // for DtrList of ForceReformat here, because _disposed flag reflects this fact
                    // in more granular way.
                    layoutDataValid = _structuralCache.FormattingOwner.IsLayoutDataValid;
                }
                return layoutDataValid;
            }
        }

        //-------------------------------------------------------------------
        // Save the maximum dcpDepend of the page, for invalidations
        // of later pages.
        //
        // DCPDepend - number of characters past end of page that were
        // considered for formatting of this page
        //-------------------------------------------------------------------
        internal TextPointer DependentMax
        {
            get
            {
                return _DependentMax;
            }
            set
            {
                if ((_DependentMax == null) || ((value != null) && (value.CompareTo(_DependentMax) > 0)))
                {
                    _DependentMax = value;
                }
            }
        }

        //-------------------------------------------------------------------
        // Viewport
        //-------------------------------------------------------------------
        internal Rect Viewport
        {
            get
            {
                return new Rect(this.Size);
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Destroy all unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether dispose is caused by explicit call to Dispose.</param>
        /// <remarks>
        /// Finalizer needs to follow rules below:
        ///     a) Your Finalize method must tolerate partially constructed instances.
        ///     b) Your Finalize method must consider the consequence of failure.
        ///     c) Your object is callable after Finalization.
        ///     d) Your object is callable during Finalization.
        ///     e) Your Finalizer could be called multiple times.
        ///     f) Your Finalizer runs in a delicate security context.
        /// See: http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx
        /// </remarks>
        private void Dispose(bool disposing)
        {
            // Do actual dispose only once.
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    // Clear content of the root visual
                    if (this.PageVisual != null)
                    {
                        // Disconnect all embedded visuals (UIElements) to make sure that
                        // they are not part of visual tree when page is destroyed.
                        // This is necessary for building proper event route, because
                        // BuildRoute prefers visual tree.
                        DestroyVisualLinks(this.PageVisual);

                        // Clear its drawing context and children collection.
                        this.PageVisual.Children.Clear();
                        this.PageVisual.ClearDrawingContext();
                    }

                    // Dispose PTS page
                    if (_ptsPage != null)
                    {
                        _ptsPage.Dispose();
                    }
                }
                try
                {
                    if (disposing)
                    {
                        // Notify interested parties about disposal of the page.
                        OnPageDestroyed(EventArgs.Empty);
                    }
}
                finally
                {
                    _ptsPage = null;
                    _structuralCache = null;
                    _textView = null;
                    _DependentMax = null;
                }
            }
        }

        //-------------------------------------------------------------------
        // Update visual representation of the page.
        //-------------------------------------------------------------------
        private void UpdateVisual()
        {
            if (this.PageVisual == null)
            {
                SetVisual(new PageVisual(this));
            }
            if (_visualNeedsUpdate)
            {
                // Draw background
                this.PageVisual.DrawBackground((Brush)_structuralCache.PropertyOwner.GetValue(FlowDocument.BackgroundProperty), new Rect(_partitionSize));

                // Connect visual created by PTS page.
                ContainerVisual pageVisual = null;
                using (_structuralCache.SetDocumentVisualValidationContext(this))
                {
                    pageVisual = _ptsPage.GetPageVisual(); // This method will update the visual tree if necessary.
                    _structuralCache.DetectInvalidOperation();
                }
                this.PageVisual.Child = pageVisual; // No-op if already connected.

                // DocumentPage.Visual for printing scenarions needs to be always returned
                // in LeftToRight FlowDirection. Hence, if the document is RightToLeft,
                // mirroring transform need to be applied to the content of DocumentPage.Visual.
                FlowDirection flowdirection = (FlowDirection)_structuralCache.PropertyOwner.GetValue(FlowDocument.FlowDirectionProperty);
                PtsHelper.UpdateMirroringTransform(FlowDirection.LeftToRight, flowdirection, pageVisual, Size.Width);

                // Clear update info for PTS page.
                using (_structuralCache.SetDocumentVisualValidationContext(this))
                {
                    _ptsPage.ClearUpdateInfo();
                    _structuralCache.DetectInvalidOperation();
                }
                _visualNeedsUpdate = false;
            }
        }

        //-------------------------------------------------------------------
        // Prepares for format page process.
        //-------------------------------------------------------------------
        private void OnBeforeFormatPage()
        {
            if (_visualNeedsUpdate)
            {
                // Clear update info for PTS page.
                _ptsPage.ClearUpdateInfo();
            }
        }

        //-------------------------------------------------------------------
        // Completes format page process.
        //-------------------------------------------------------------------
        private void OnAfterFormatPage()
        {
            if (_textView != null)
            {
                _textView.Invalidate();
            }
            _visualNeedsUpdate = true;
        }

        //-------------------------------------------------------------------
        // IContentHost Helpers
        //-------------------------------------------------------------------

        /// <summary>
        /// Searches for an element in the _structuralCache.TextContainer. If the element is found, returns the
        /// position at which it is found. Otherwise returns null.
        /// </summary>
        /// <param name="e">
        /// Element to be found.
        /// </param>
        /// <param name="isLimitedToTextView">
        /// bool value indicating whether the search should only be limited to the text view of the page,
        /// in which case we search only text segments in the text view
        /// </param>
        private TextPointer FindElementPosition(IInputElement e, bool isLimitedToTextView)
        {
            // Parameter validation
            Debug.Assert(e != null);

            // Validate that this function is only called when a TextContainer exists as complex content
            Debug.Assert(_structuralCache.TextContainer is TextContainer);

            TextPointer elementPosition = null;

            // If e is a TextElement we can optimize by checking its TextContainer
            if (e is TextElement)
            {
                if ((e as TextElement).TextContainer == _structuralCache.TextContainer)
                {
                    // Element found
                    elementPosition = new TextPointer((e as TextElement).ElementStart);
                }
                // else: elementPosition stays null
            }
            else
            {
                // Else: search for e in the complex content
                if (!(_structuralCache.TextContainer.Start is TextPointer) ||
                    !(_structuralCache.TextContainer.End is TextPointer))
                {
                    // Invalid TextContainer, don't search
                    return null;
                }

                TextPointer searchPosition = new TextPointer(_structuralCache.TextContainer.Start as TextPointer);
                while (elementPosition == null && ((ITextPointer)searchPosition).CompareTo(_structuralCache.TextContainer.End) < 0)
                {
                    // Search each position in _structuralCache.TextContainer for the element
                    switch (searchPosition.GetPointerContext(LogicalDirection.Forward))
                    {
                        case TextPointerContext.EmbeddedElement:
                            DependencyObject embeddedObject = searchPosition.GetAdjacentElement(LogicalDirection.Forward);
                            if (embeddedObject is ContentElement || embeddedObject is UIElement)
                            {
                                if (embeddedObject == e as ContentElement || embeddedObject == e as UIElement)
                                {
                                    // Element found. Stop searching
                                    elementPosition = new TextPointer(searchPosition);
                                    break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    searchPosition.MoveToNextContextPosition(LogicalDirection.Forward);
                }
            }

            // If the element was found, check if we are limited to text view
            if (elementPosition != null)
            {
                if (isLimitedToTextView)
                {
                    // At this point, we should create TextView if it doesn't exist
                    _textView = GetTextView();
                    Invariant.Assert(_textView != null);
                    // Check all segements in text view for position
                    for (int segmentIndex = 0; segmentIndex < ((ITextView)_textView).TextSegments.Count; segmentIndex++)
                    {
                        if (((ITextPointer)elementPosition).CompareTo(((ITextView)_textView).TextSegments[segmentIndex].Start) >= 0 &&
                            ((ITextPointer)elementPosition).CompareTo(((ITextView)_textView).TextSegments[segmentIndex].End) < 0)
                        {
                            // Element lies within a segment. Return position
                            return elementPosition;
                        }
                    }
                    // Element not found in all segments of TextView. Set position to null
                    elementPosition = null;
                }
            }
            return elementPosition;
        }

        //-------------------------------------------------------------------
        // Disconnect all embedded visuals (UIElements) to make sure that
        // they are not part of visual tree when page is destroyed.
        // This is necessary for building proper event route, because
        // BuildRoute prefers visual tree.
        //-------------------------------------------------------------------
        private void DestroyVisualLinks(ContainerVisual visual)
        {
            VisualCollection vc = visual.Children;
            if (vc != null)
            {
                for (int index = 0; index < vc.Count; index++)
                {
                    if (vc[index] is UIElementIsland)
                    {
                        vc.RemoveAt(index);
                    }
                    else
                    {
                        Invariant.Assert(vc[index] is ContainerVisual, "The children should always derive from ContainerVisual");
                        DestroyVisualLinks((ContainerVisual)(vc[index]));
                    }
                }
            }
        }

        /// <summary>
        /// Raise TextView.Updated event.
        /// </summary>
        private void ValidateTextView()
        {
            if (_textView != null)
            {
                _textView.OnUpdated();
            }
        }

        /// <summary>
        /// Gets TextView for this page.
        /// </summary>
        private TextDocumentView GetTextView()
        {
            TextDocumentView textView = (TextDocumentView)((IServiceProvider)this).GetService(typeof(ITextView));
            Invariant.Assert(textView != null);
            return textView;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        //-------------------------------------------------------------------
        // Visual representing content of the page.
        //-------------------------------------------------------------------
        private PageVisual PageVisual
        {
            get { return (base.Visual as PageVisual); }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        //-------------------------------------------------------------------
        // Associated PTS page.
        //-------------------------------------------------------------------
        private PtsPage _ptsPage;

        //-------------------------------------------------------------------
        // Structural cache.
        //-------------------------------------------------------------------
        private StructuralCache _structuralCache;

        //-------------------------------------------------------------------
        // Number of lines formatted during page formatting.
        // NOTE: This field is used only internally for layout DRTs.
        //-------------------------------------------------------------------
        private int _formattedLinesCount;

        //-------------------------------------------------------------------
        // TextView associated with the document page.
        //-------------------------------------------------------------------
        private TextDocumentView _textView;

        //-------------------------------------------------------------------
        // Size of partition for the page.
        //-------------------------------------------------------------------
        private Size _partitionSize;

        //-------------------------------------------------------------------
        // Margin of the page.
        //-------------------------------------------------------------------
        private Thickness _pageMargin;

        //-------------------------------------------------------------------
        // Is it already disposed?
        //-------------------------------------------------------------------
        private int _disposed;

        //-------------------------------------------------------------------
        // Max of dcpDepend for page
        //-------------------------------------------------------------------
        private TextPointer _DependentMax;

        //-------------------------------------------------------------------
        // Need to update visual?
        //-------------------------------------------------------------------
        private bool _visualNeedsUpdate;

        //-------------------------------------------------------------------
        // Width of page during last format
        //-------------------------------------------------------------------
        private double _lastFormatWidth;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IServiceProvider Members
        //
        //-------------------------------------------------------------------

        #region IServiceProvider Members

        //-------------------------------------------------------------------
        // Gets the service object of the specified type. FlowDocumentPage
        // currently supports only TextView
        //
        //      serviceType - an object that specifies the type of service
        //                    object to get
        //
        // Returns: A service object of type serviceType. A null reference
        //          if there is no service object of type serviceType.
        //-------------------------------------------------------------------
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (serviceType == typeof(ITextView))
            {
                if (_textView == null)
                {
                    _textView = new TextDocumentView(this, _structuralCache.TextContainer);
                }

                return _textView;
            }

            return null;
        }

        #endregion IServiceProvider Members

        //-------------------------------------------------------------------
        //
        //  IContentHost Members
        //
        //-------------------------------------------------------------------

        #region IContentHost Members

        /// <summary>
        /// Hit tests to the correct ContentElement
        /// within the ContentHost that the mouse
        /// is over
        /// </summary>
        /// <param name="point">
        /// Mouse coordinates relative to
        /// the ContentHost
        /// </param>
        IInputElement IContentHost.InputHitTest(Point point)
        {
            return this.InputHitTestCore(point);
        }

        /// <summary>
        /// Returns rectangles for element. First finds element by navigating in FlowDocumentPage.
        /// If element is not found or if call to get rectangles from FlowDocumentPage returns null
        /// we return an empty collection. If the layout is not valid we return null.
        /// </summary>
        /// <param name="child">
        /// Content element for which rectangles are required
        /// </param>
        ReadOnlyCollection<Rect> IContentHost.GetRectangles(ContentElement child)
        {
            // Restrict search to only the text segments in the page's text view. This is not needed for
            // HitTest because it takes only a point
            return this.GetRectanglesCore(child, true);
        }

        /// <summary>
        /// Returns elements hosted by the content host as an enumerator class
        /// </summary>
        IEnumerator<IInputElement> IContentHost.HostedElements
        {
            get
            {
                return this.HostedElementsCore as IEnumerator<IInputElement>;
            }
        }

        /// <summary>
        /// Called when a UIElement-derived class which is hosted by a IContentHost changes it’s DesiredSize
        /// </summary>
        /// <param name="child">
        /// Child element whose DesiredSize has changed
        /// </param>
        void IContentHost.OnChildDesiredSizeChanged(UIElement child)
        {
            this.OnChildDesiredSizeChangedCore(child);
        }

        #endregion IContentHost Members
    }
}
