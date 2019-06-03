// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines the IDocumentScrollInfo interface.
//              Spec:  03.01.01 API Design.PageGrid+.doc
//

using MS.Internal;
using MS.Utility;
using System.Collections.ObjectModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// An IDocumentScrollInfo serves as the main scrolling region inside a <see cref="System.Windows.Controls.DocumentViewer" />
    /// or derived class.  It exposes properties and methods for navigating through and manipulating the view of an
    /// IDocumentPaginatorSource-based Document.
    /// </summary>
    internal interface IDocumentScrollInfo : IScrollInfo
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        /// <summary>
        /// Scrolls the requested page into view.  Requests for invalid pages
        /// will be clipped into range.
        /// </summary>
        /// <param name="pageNumber">The page to make visible.</param>
        void MakePageVisible(int pageNumber);

        /// <summary>
        /// Scrolls the current selection into view.  Requests for empty or 
        /// invalid selections will do nothing.
        /// </summary>
        void MakeSelectionVisible();

        /// <summary>
        /// Overload for IScrollInfo.MakeVisible, which allows the caller to
        /// specify a page number for the specified object to be made visible.
        /// </summary>
        Rect MakeVisible(object o, Rect r, int pageNumber);

        /// <summary>
        /// Scrolls the next row of pages into view.  This differs from 
        /// IScrollInfo’s “PageDown” in that PageDown pages by Viewports 
        /// which may not coincide with page dimensions, whereas 
        /// ScrollToNextRow takes these dimensions into account so that 
        /// precisely the next row of pages is displayed.
        /// </summary>
        void ScrollToNextRow();

        /// Scrolls the previous row of pages into view.  This differs from 
        /// IScrollInfo’s “PageUp” in that PageUp pages by Viewports 
        /// which may not coincide with page dimensions, whereas 
        /// ScrollToPreviousRow takes these dimensions into account so that 
        /// precisely the previous row of pages is displayed.
        void ScrollToPreviousRow();

        /// <summary>
        /// Scrolls to the top of the document.
        /// </summary>
        void ScrollToHome();

        /// <summary>
        /// Scrolls to the bottom of the document.
        /// </summary>
        void ScrollToEnd();

        /// <summary>
        /// Sets the scale factor applied to pages in the document.
        /// Scale values less than or equal to zero will throw.
        /// </summary>
        /// <param name="scale"></param>
        void SetScale(double scale);

        /// <summary>
        /// Changes the layout of the document to have the specified
        /// number of columns without affecting the scale.
        /// Column specifications less than 1 will throw.
        /// </summary>
        /// <param name="columns"></param>
        void SetColumns(int columns);

        /// <summary>
        /// Changes the view to the specified number of columns and
        /// scales the view to fit.
        /// Column specifications less than 1 will throw.
        /// </summary>
        /// <param name="columns">The number of columns to fit.</param>
        void FitColumns(int columns);

        /// <summary>
        /// Changes the view to a single page, scaled such that it is as 
        /// wide as the Viewport. 
        /// </summary>
        void FitToPageWidth();

        /// <summary>
        /// Changes the view to a single page, scaled such that it is as 
        /// tall as the Viewport.
        /// </summary>
        void FitToPageHeight();

        /// <summary>
        /// Changes the view to “thumbnail view” which will scale the document 
        /// such that as many pages are visible at once as is possible.
        /// </summary>
        void ViewThumbnails();

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        /// <summary>
        /// Provides the IDocumentScrollInfo implementer with a content 
        /// tree to be paginated.  Developers are free to modify this 
        /// Content at any time (remove, add, modify pages, etc...) 
        /// and the IDocumentScrollInfo implementer is responsible for 
        /// noting the changes and updating as necessary. 
        /// </summary>
        /// <value>The DocumentPaginator to be assigned as the content</value>
        DynamicDocumentPaginator Content { get; set; }

        /// <summary>
        /// Indicates the number of pages currently in the document. 
        /// </summary>
        /// <value></value>
        int PageCount { get; }

        /// <summary>
        /// When queried, FirstVisiblePage returns the first page visible onscreen.
        /// </summary>
        /// <value></value>
        int FirstVisiblePageNumber { get; }

        /// <summary>
        /// Returns the current Scale factor applied to the pages given the current settings.
        /// </summary>
        /// <value></value>
        double Scale { get; }

        /// <summary>
        /// Returns the current number of Columns of pages displayed given the current settings.
        /// </summary>
        /// <value></value>
        int MaxPagesAcross { get; }

        /// <summary>
        /// Specifies the vertical gap between Pages when laid out, in pixel (1/96”) units.  
        /// </summary>
        /// <value></value>
        double VerticalPageSpacing { get; set; }

        /// <summary>
        /// Specifies the horizontal gap between Pages when laid out, in pixel (1/96”) units.  
        /// </summary>
        /// <value></value>
        double HorizontalPageSpacing { get; set; }

        /// <summary>
        /// Specifies whether each displayed page should be adorned with a “Drop Shadow” border or not.
        /// </summary>
        /// <value></value>
        bool ShowPageBorders { get; set; }

        /// <summary>
        /// LockViewModes changes behavior of layout such that the last-set page-fit mode will be
        /// re-applied when layout size changes.  This will allow "Full Page" to always show a full
        /// page even if the user resizes the window, for example.
        /// </summary>

        bool LockViewModes { get; set; }
        /// <summary>
        /// Returns the current TextView
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Abstract text container needed for a text editor.
        /// </summary>
        ITextContainer TextContainer { get; }

        /// <summary>
        /// The collection of currently-visible DocumentPageViews.
        /// </summary>
        ReadOnlyCollection<DocumentPageView> PageViews { get; }

        /// <summary>
        /// DocumentViewerOwner is the DocumentViewer Control and UI that hosts the IDocumentScrollInfo object.  
        /// This control is dependent on this IDSI’s properties, so implementers of IDSI should call 
        /// InvalidateDocumentScrollInfo() on this object when related properties change so that 
        /// DocumentViewer’s UI will be kept in sync.  This property is analogous to IScrollInfo’s ScrollOwner 
        /// property.
        /// </summary>
        /// <value></value>
        DocumentViewer DocumentViewerOwner { get; set; }
    }
}
