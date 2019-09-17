// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     TextSelectionHelper is a helper class used by TextSelectionProvcrssor
//     and DynamicSelectionProcessor
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using MS.Internal.Annotations.Component;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using MS.Internal.Documents;
using MS.Utility;


namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     TextSelectionHelper uses TextAnchors to represent portions 
    ///     of text that are anchors.  It produces locator parts that 
    ///     represent these TextAnchors and can generate TextAnchors from
    ///     the locator parts.
    /// </summary>  
    internal class TextSelectionHelper
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// This ctor is added to prevent the compiler from
        /// generating a public default ctor. This class
        /// should not be instantiated
        /// </summary>
        private TextSelectionHelper() { }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Merges the two anchors into one, if possible.
        /// </summary>
        /// <param name="anchor1">anchor to merge </param>
        /// <param name="anchor2">other anchor to merge </param>
        /// <param name="newAnchor">new anchor that contains the data from both 
        /// anchor1 and anchor2</param>
        /// <returns>true if the anchors were merged, false otherwise 
        /// </returns>
        /// <exception cref="ArgumentNullException">anchor1 or anchor2 are null</exception>
        public static bool MergeSelections(Object anchor1, Object anchor2, out Object newAnchor)
        {
            TextAnchor firstAnchor = anchor1 as TextAnchor;
            TextAnchor secondAnchor = anchor2 as TextAnchor;

            if ((anchor1 != null) && (firstAnchor == null))
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "anchor1: type = " + anchor1.GetType().ToString());

            if ((anchor2 != null) && (secondAnchor == null))
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "Anchor2: type = " + anchor2.GetType().ToString());

            if (firstAnchor == null)
            {
                newAnchor = secondAnchor;
                return newAnchor != null;
            }

            if (secondAnchor == null)
            {
                newAnchor = firstAnchor;
                return newAnchor != null;
            }

            newAnchor = TextAnchor.ExclusiveUnion(firstAnchor, secondAnchor);

            return true;
        }


        /// <summary>
        ///     Gets the tree elements spanned by the selection.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>a list of elements spanned by the selection; never returns 
        /// null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public static IList<DependencyObject> GetSelectedNodes(Object selection)
        {
            if (selection == null)
                throw new ArgumentNullException("selection");

            IList<TextSegment> segments;
            ITextPointer start = null;
            ITextPointer end = null;

            CheckSelection(selection, out start, out end, out segments);

            IList<DependencyObject> list = new List<DependencyObject>();

            // If the selection is of length 0, then we simply add the parent of the
            // text container and return.
            if (start.CompareTo(end) == 0)
            {
                list.Add(((TextPointer)start).Parent);

                return list;
            }

            TextPointer current = (TextPointer)start.CreatePointer();
            while (((ITextPointer)current).CompareTo(end) < 0)
            {
                DependencyObject node = current.Parent;

                if (!list.Contains(node))
                {
                    list.Add(node);
                }

                current.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            return list;
        }

        /// <summary>
        ///     Gets the parent element of this selection.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>the parent element of the selection; can be null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public static UIElement GetParent(Object selection)
        {
            if (selection == null)
                throw new ArgumentNullException("selection");

            ITextPointer start = null;
            ITextPointer end = null;
            IList<TextSegment> segments;

            CheckSelection(selection, out start, out end, out segments);

            return GetParent(start);
        }


        /// <summary>
        ///     Gets the parent element of ITextPointer.
        /// </summary>
        /// <param name="pointer">the pointer to examine</param>
        /// <returns>the parent element of this pointer; can be null</returns>
        /// <exception cref="ArgumentNullException">pointer is null</exception>
        public static UIElement GetParent(ITextPointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException("pointer");

            DependencyObject document = pointer.TextContainer.Parent;
            DependencyObject parent = PathNode.GetParent(document);

            FlowDocumentScrollViewer scrollViewer = parent as FlowDocumentScrollViewer;
            if (scrollViewer != null)
            {
                return (UIElement)scrollViewer.ScrollViewer.Content;
            }

            // Special case - for paginated content we want the DocumentPageHost for the
            // specific page instead of the viewer.
            DocumentViewerBase documentViewerBase = parent as DocumentViewerBase;
            if (documentViewerBase != null)
            {
                int pageNumber;

                // We get the content again here GetPointerPage handles
                // special cases like FixedDocumentSequences
                IDocumentPaginatorSource content = GetPointerPage(pointer.CreatePointer(LogicalDirection.Forward), out pageNumber);

                if (pageNumber >= 0)
                {
                    foreach (DocumentPageView dpv in documentViewerBase.PageViews)
                    {
                        if (dpv.PageNumber == pageNumber)
                        {
                            // DPVs always have one child - the DocumentPageHost 
                            int count = VisualTreeHelper.GetChildrenCount(dpv);
                            Invariant.Assert(count == 1);
                            return VisualTreeHelper.GetChild(dpv, 0) as DocumentPageHost;
                        }
                    }
                    // Page must not be visible.
                    return null;
                }
            }

            return parent as UIElement;
        }

        /// <summary>
        ///  Gets the anchor point for the selection
        /// </summary>
        /// <param name="selection">the anchor to examine</param>
        /// <returns>
        /// The anchor point of the selection; 
        /// If there is no valid AnchorPoint returns Point(Double.NaN, Double.NaN). 
        /// </returns>
        /// <exception cref="ArgumentNullException">anchor is null</exception>
        /// <exception cref="ArgumentException">anchor is of wrong type</exception>
        public static Point GetAnchorPoint(Object selection)
        {
            if (selection == null)
                throw new ArgumentNullException("selection");

            TextAnchor anchor = selection as TextAnchor;

            if (anchor == null)
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "selection");

            return GetAnchorPointForPointer(anchor.Start.CreatePointer(LogicalDirection.Forward));
        }

        /// <summary>
        ///  Gets the anchor point for the text pointer
        /// </summary>
        /// <param name="pointer">the pointer to examine</param>
        /// <returns>
        /// The anchor point of the text pointer
        /// </returns>
        /// <exception cref="ArgumentNullException">pointer is null</exception>
        public static Point GetAnchorPointForPointer(ITextPointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException("pointer");

            Rect rect = GetAnchorRectangle(pointer);

            if (rect != Rect.Empty)
            {
                return new Point(rect.Left, rect.Top + rect.Height);
            }

            return new Point(0, 0);
        }


        /// <summary>
        ///  Gets a point for the text pointer that can be turned back into
        ///  the TextPointer at a later time.
        /// </summary>
        /// <param name="pointer">the pointer to examine</param>
        /// <returns>
        /// A point that can be turned back into the TextPointer at a later time
        /// </returns>
        /// <exception cref="ArgumentNullException">pointer is null</exception>
        public static Point GetPointForPointer(ITextPointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException("pointer");

            Rect rect = GetAnchorRectangle(pointer);

            if (rect != Rect.Empty)
            {
                return new Point(rect.Left, rect.Top + rect.Height / 2);
            }

            return new Point(0, 0);
        }

        /// <summary>
        ///  Gets the rectangle for this ITextPointer
        /// </summary>
        /// <param name="pointer">the pointer to examine</param>
        /// <returns>
        /// The anchor point of the selection; 
        /// If there is no valid AnchorPoint returns Point(Double.NaN, Double.NaN). 
        /// </returns>
        /// <exception cref="ArgumentNullException">pointer is null</exception>
        public static Rect GetAnchorRectangle(ITextPointer pointer)
        {
            if (pointer == null)
                throw new ArgumentNullException("pointer");
            bool extension = false;

            ITextView textView = GetDocumentPageTextView(pointer);
            if (pointer.CompareTo(pointer.TextContainer.End) == 0)
            {
                //we can not get rectangle for the end of the TextContainer
                //so get the last symbol
                Point endPoint = new Point(Double.MaxValue, Double.MaxValue);
                pointer = textView.GetTextPositionFromPoint(endPoint, true);
                //we need to move the resulting rectangle at half space because
                //the geometry calculating function does the same
                extension = true;
            }
            if (textView != null && textView.IsValid && TextDocumentView.Contains(pointer, textView.TextSegments))
            {
                Rect rect = textView.GetRectangleFromTextPosition(pointer);               
                if (extension && rect != Rect.Empty)
                {
                    rect.X += rect.Height / 2.0;
                }
                return rect;
            }

            return Rect.Empty;
        }

        /// <summary>
        /// Gets DocumentViewerBase and a page number for specified TextPointer
        /// </summary>
        /// <param name="pointer">a TP from the container</param>
        /// <param name="pageNumber">the page number</param>
        /// <returns>DocumentViewerBase</returns>
        public static IDocumentPaginatorSource GetPointerPage(ITextPointer pointer, out int pageNumber)
        {
            Invariant.Assert(pointer != null, "unknown pointer");

            IDocumentPaginatorSource idp = pointer.TextContainer.Parent as IDocumentPaginatorSource;
            FixedDocument fixedDoc = idp as FixedDocument;
            if (fixedDoc != null)
            {
                FixedDocumentSequence sequence = fixedDoc.Parent as FixedDocumentSequence;
                if (sequence != null)
                    idp = sequence;
            }

            Invariant.Assert(idp != null);
            DynamicDocumentPaginator ddp = idp.DocumentPaginator as DynamicDocumentPaginator;
            pageNumber = ddp != null ? ddp.GetPageNumber((ContentPosition)pointer) : -1;

            return idp;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        /// <summary>
        /// Gets the start, end and text segments of the selection.  Throws an exception if the
        /// selection is of the wrong type.
        /// </summary>
        internal static void CheckSelection(object selection, out ITextPointer start, out ITextPointer end, out IList<TextSegment> segments)
        {
            ITextRange textRange = selection as ITextRange;

            if (textRange != null)
            {
                start = textRange.Start;
                end = textRange.End;
                segments = textRange.TextSegments;
            }
            else
            {
                TextAnchor textAnchor = selection as TextAnchor;
                if (textAnchor == null)
                    throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "selection");

                start = textAnchor.Start;
                end = textAnchor.End;
                segments = textAnchor.TextSegments;
            }
        }

        /// <summary>
        /// Gets the TextView exposed by the page where this pointer lives
        /// </summary>
        /// <param name="pointer">the pointer</param>
        /// <returns>the TextView</returns>
        internal static ITextView GetDocumentPageTextView(ITextPointer pointer)
        {
            int pageNumber;

            DependencyObject content = pointer.TextContainer.Parent as DependencyObject;
            if (content != null)
            {
                FlowDocumentScrollViewer scrollViewer = PathNode.GetParent(content) as FlowDocumentScrollViewer;
                if (scrollViewer != null)
                {
                    IServiceProvider provider = scrollViewer.ScrollViewer.Content as IServiceProvider;
                    Invariant.Assert(provider != null, "FlowDocumentScrollViewer should be an IServiceProvider.");
                    return provider.GetService(typeof(ITextView)) as ITextView;
                }
            }

            IDocumentPaginatorSource idp = GetPointerPage(pointer, out pageNumber);
            if (idp != null && pageNumber >= 0)
            {
                DocumentPage docPage = idp.DocumentPaginator.GetPage(pageNumber);
                IServiceProvider isp = docPage as IServiceProvider;
                if (isp != null)
                    return isp.GetService(typeof(ITextView)) as ITextView;
            }

            return null;
        }

        /// <summary>
        /// Gets a list of ITextViews spanned by this text segment
        /// </summary>
        /// <param name="segment">the text segment</param>
        /// <returns>the TextViews list</returns>
        internal static List<ITextView> GetDocumentPageTextViews(TextSegment segment)
        {
            List<ITextView> res = null;
            int startPageNumber, endPageNumber;

            //revert the logical direction of the pointers
            ITextPointer start = segment.Start.CreatePointer(LogicalDirection.Forward);
            ITextPointer end = segment.End.CreatePointer(LogicalDirection.Backward);

            DependencyObject content = start.TextContainer.Parent as DependencyObject;
            if (content != null)
            {
                FlowDocumentScrollViewer scrollViewer = PathNode.GetParent(content) as FlowDocumentScrollViewer;
                if (scrollViewer != null)
                {
                    IServiceProvider provider = scrollViewer.ScrollViewer.Content as IServiceProvider;
                    Invariant.Assert(provider != null, "FlowDocumentScrollViewer should be an IServiceProvider.");
                    res = new List<ITextView>(1);
                    res.Add(provider.GetService(typeof(ITextView)) as ITextView);
                    return res;
                }
            }

            IDocumentPaginatorSource idp = GetPointerPage(start, out startPageNumber);
            DynamicDocumentPaginator ddp = idp.DocumentPaginator as DynamicDocumentPaginator;
            endPageNumber = ddp != null ? ddp.GetPageNumber((ContentPosition)end) : -1;

            if (startPageNumber == -1 || endPageNumber == -1)
            {
                // If either page couldn't be found, we return an empty list.  This
                // could be caused by a failure in paginating the document.
                res = new List<ITextView>(0);
            }
            else if (startPageNumber == endPageNumber)
            {
                res = ProcessSinglePage(idp, startPageNumber);
            }
            else
            {
                res = ProcessMultiplePages(idp, startPageNumber, endPageNumber);
            }

            return res;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        /// <summary>
        /// Gets a single page TextView throug the idp.GetPage. cALL this API only when
        /// it is sure that the page is loaded 
        /// </summary>
        /// <param name="idp">IDocumentPaginatorSource</param>
        /// <param name="pageNumber">page number</param>
        /// <returns>returns a list of one view</returns>
        private static List<ITextView> ProcessSinglePage(IDocumentPaginatorSource idp, int pageNumber)
        {
            Invariant.Assert(idp != null, "IDocumentPaginatorSource is null");

            DocumentPage docPage = idp.DocumentPaginator.GetPage(pageNumber);
            IServiceProvider isp = docPage as IServiceProvider;
            List<ITextView> res = null;
            if (isp != null)
            {
                res = new List<ITextView>(1);
                ITextView view = isp.GetService(typeof(ITextView)) as ITextView;
                if (view != null)
                    res.Add(view);
            }

            return res;
        }

        /// <summary>
        /// Gets existing views for pages from start to end. Scans only existing view to
        /// avoid loading of unloaded pages.
        /// </summary>
        /// <param name="idp">IDocumentPaginatorSource</param>
        /// <param name="startPageNumber">start page number</param>
        /// <param name="endPageNumber">end page number</param>
        /// <returns>returns a list of text views</returns>
        private static List<ITextView> ProcessMultiplePages(IDocumentPaginatorSource idp, int startPageNumber, int endPageNumber)
        {
            Invariant.Assert(idp != null, "IDocumentPaginatorSource is null");

            //now get available views
            DocumentViewerBase viewer = PathNode.GetParent(idp as DependencyObject) as DocumentViewerBase;
            Invariant.Assert(viewer != null, "DocumentViewer not found");

            // If the pages for the text segment are reversed (possibly a floater where the floater
            // reflow on to a page that comes after its anchor) we just swap them
            if (endPageNumber < startPageNumber)
            {
                int temp = endPageNumber;
                endPageNumber = startPageNumber;
                startPageNumber = temp;
            }

            List<ITextView> res = null;
            if (idp != null && startPageNumber >= 0 && endPageNumber >= startPageNumber)
            {
                res = new List<ITextView>(endPageNumber - startPageNumber + 1);
                for (int pageNb = startPageNumber; pageNb <= endPageNumber; pageNb++)
                {
                    DocumentPageView view = AnnotationHelper.FindView(viewer, pageNb);
                    if (view != null)
                    {
                        IServiceProvider serviceProvider = view.DocumentPage as IServiceProvider;
                        if (serviceProvider != null)
                        {
                            ITextView textView = serviceProvider.GetService(typeof(ITextView)) as ITextView;
                            if (textView != null)
                                res.Add(textView);
                        }
                    }
                }
            }

            return res;
        }

        #endregion Private Methods
    }
}

