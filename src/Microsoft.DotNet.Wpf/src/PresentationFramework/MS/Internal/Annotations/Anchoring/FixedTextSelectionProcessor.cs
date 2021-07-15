// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     FixedTextSelectionProcessor uses TextAnchors to represent portions 
//     of text that are anchors. It produces FixedTextRange locator parts that are designed 
//     specifically for use inside the DocumentViewer control. This locator part contains the
//     page number and the start and end points of the text selection. 
//     FixedTextSelectionProcessor converts the text selection to FixedTextRange
//    
//     Spec: Anchoring to text in paginated docs.doc
//

using System;
using System.IO;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using MS.Utility;
using MS.Internal.Documents;
using MS.Internal.PtsHost;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     FixedTextSelectionProcessor uses TextAnchors to represent portions 
    ///     of text that are anchors.  It produces locator parts that 
    ///     represent these TextRanges by beginning and end position
    /// </summary>  
    internal class FixedTextSelectionProcessor : SelectionProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of FixedTextSelectionProcessor.
        /// </summary>
        public FixedTextSelectionProcessor()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Merges the two anchors into one, if possible. It does not require
        /// the anchors to be connected. All this method does is to create a 
        /// TextAnchor that spans the two anchors.
        /// </summary>
        /// <param name="anchor1">anchor to merge. Must be a TextAnchor. </param>
        /// <param name="anchor2">other anchor to merge. Must be a TextAnchor. </param>
        /// <param name="newAnchor">new anchor that contains the data from both 
        /// anchor1 and anchor2</param>
        /// <returns>true if the anchors were merged, false otherwise 
        /// </returns>
        public override bool MergeSelections(Object anchor1, Object anchor2, out Object newAnchor)
        {
            return TextSelectionHelper.MergeSelections(anchor1, anchor2, out newAnchor);
        }


        /// <summary>
        ///  Generates FixedPageProxy objects for each page, spaned by the selection
        /// </summary>
        /// <param name="selection">the selection to examine. Must implement ITextRange</param>
        /// <returns>a list of FixedPageProxy objects, corresponding to each page spanned by the selection; never returns 
        /// null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        /// <exception cref="ArgumentException">selection start or end point can not be resolved to a page</exception>
        public override IList<DependencyObject> GetSelectedNodes(Object selection)
        {
            IList<TextSegment> textSegments = CheckSelection(selection);

            IList<DependencyObject> pageEl = new List<DependencyObject>();

            Point start;
            Point end;
            foreach (TextSegment segment in textSegments)
            {
                int startPage = int.MinValue;
                ITextPointer startPointer = segment.Start.CreatePointer(LogicalDirection.Forward);
                TextSelectionHelper.GetPointerPage(startPointer, out startPage);
                start = TextSelectionHelper.GetPointForPointer(startPointer);
                if (startPage == int.MinValue)
                    throw new ArgumentException(SR.Get(SRID.SelectionDoesNotResolveToAPage, "start"), "selection");

                int endPage = int.MinValue;
                ITextPointer endPointer = segment.End.CreatePointer(LogicalDirection.Backward);
                TextSelectionHelper.GetPointerPage(endPointer, out endPage);
                end = TextSelectionHelper.GetPointForPointer(endPointer);
                if (endPage == int.MinValue)
                    throw new ArgumentException(SR.Get(SRID.SelectionDoesNotResolveToAPage, "end"), "selection");

                int firstPage = pageEl.Count;
                int numOfPages = endPage - startPage;

                Debug.Assert(numOfPages >= 0, "start page number is bigger than the end page number");

                // If the first page of this segment already has an FPP, then use that one for an additional segment
                int i = 0;

                if (pageEl.Count > 0 && ((FixedPageProxy)pageEl[pageEl.Count - 1]).Page == startPage)
                {
                    firstPage--;  // use the existing one from the list as the first
                    i++;  // make 1 fewer FPPs
                }

                for (; i <= numOfPages; i++)
                {
                    pageEl.Add(new FixedPageProxy(segment.Start.TextContainer.Parent, startPage + i));
                }

                // If entire segment is on one page set both start/end on that page
                if (numOfPages == 0)
                {
                    ((FixedPageProxy)pageEl[firstPage]).Segments.Add(new PointSegment(start, end));
                }
                else
                {
                    // otherwise set start on the first page and end on the last page
                    ((FixedPageProxy)pageEl[firstPage]).Segments.Add(new PointSegment(start, PointSegment.NotAPoint));
                    ((FixedPageProxy)pageEl[firstPage + numOfPages]).Segments.Add(new PointSegment(PointSegment.NotAPoint, end));
                }
            }

            return pageEl;
        }

        /// <summary>
        /// Gets the parent element of this selection. The parent element is the 
        /// FixedPage that contains selection.Start TextPointer. 
        /// </summary>
        /// <param name="selection">the selection to examine. Must implement ITextRange</param>
        /// <returns>the parent element of the selection; can be null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override UIElement GetParent(Object selection)
        {
            CheckAnchor(selection);
            return TextSelectionHelper.GetParent(selection);
        }


        /// <summary>
        /// Gets the anchor point for the selection. This is the Point that corresponds
        /// to the start position of the selection
        /// </summary>
        /// <param name="selection">the selection to examine. Must implement ITextRange</param>
        /// <returns>the anchor point of the selection; can be (double.NaN, double.NaN) if the 
        /// selection start point is not contained in a document viewer</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override Point GetAnchorPoint(Object selection)
        {
            CheckAnchor(selection);
            return TextSelectionHelper.GetAnchorPoint(selection);
        }

        /// <summary>
        ///     Creates one locator part representing part of the selection
        /// that lies within start node
        /// </summary>
        /// <param name="selection">the selection that is being processed. Must implement ITextRange</param>
        /// <param name="startNode">The FixedPageProxy object, representing one page of the document</param>
        /// <returns>A list containing one FixedTextRange locator part</returns>
        /// <exception cref="ArgumentNullException">startNode or selection is null</exception>
        /// <exception cref="ArgumentException">selection is of the wrong type</exception>
        public override IList<ContentLocatorPart>
            GenerateLocatorParts(Object selection, DependencyObject startNode)
        {
            if (startNode == null)
                throw new ArgumentNullException("startNode");

            if (selection == null)
                throw new ArgumentNullException("selection");

            CheckSelection(selection);

            FixedPageProxy fp = startNode as FixedPageProxy;

            if (fp == null)
                throw new ArgumentException(SR.Get(SRID.StartNodeMustBeFixedPageProxy), "startNode");

            ContentLocatorPart part = new ContentLocatorPart(FixedTextElementName);
            if (fp.Segments.Count == 0)
            {
                part.NameValuePairs.Add(TextSelectionProcessor.CountAttribute, 1.ToString(NumberFormatInfo.InvariantInfo));
                part.NameValuePairs.Add(TextSelectionProcessor.SegmentAttribute + 0.ToString(NumberFormatInfo.InvariantInfo), ",,,");
            }
            else
            {
                part.NameValuePairs.Add(TextSelectionProcessor.CountAttribute, fp.Segments.Count.ToString(NumberFormatInfo.InvariantInfo));

                for (int i = 0; i < fp.Segments.Count; i++)
                {
                    string value = "";
                    if (!double.IsNaN(fp.Segments[i].Start.X))
                    {
                        value += fp.Segments[i].Start.X.ToString(NumberFormatInfo.InvariantInfo) + TextSelectionProcessor.Separator[0] + fp.Segments[i].Start.Y.ToString(NumberFormatInfo.InvariantInfo);
                    }
                    else
                    {
                        value += TextSelectionProcessor.Separator[0];
                    }
                    value += TextSelectionProcessor.Separator[0];
                    if (!double.IsNaN(fp.Segments[i].End.X))
                    {
                        value += fp.Segments[i].End.X.ToString(NumberFormatInfo.InvariantInfo) + TextSelectionProcessor.Separator[0] + fp.Segments[i].End.Y.ToString(NumberFormatInfo.InvariantInfo);
                    }
                    else
                    {
                        value += TextSelectionProcessor.Separator[0];
                    }

                    part.NameValuePairs.Add(TextSelectionProcessor.SegmentAttribute + i.ToString(NumberFormatInfo.InvariantInfo), value);
                }
            }
            List<ContentLocatorPart> res = new List<ContentLocatorPart>(1);
            res.Add(part);
            return res;
        }

        /// <summary>
        ///     Creates a TextRange object spanning the portion of 'startNode' 
        ///     specified by 'locatorPart'.
        /// </summary>
        /// <param name="locatorPart">FixedTextRange locator part specifying start and end point of 
        /// the TextRange</param>
        /// <param name="startNode">the FixedPage containing this locator part</param>
        /// <param name="attachmentLevel">set to AttachmentLevel.Full if the FixedPage for the locator 
        /// part was found, AttachmentLevel.Unresolved otherwise</param>
        /// <returns>a TextRange spanning the text between start end end point in the FixedTextRange
        /// locator part
        /// , null if selection described by locator part could not be
        /// recreated</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are 
        /// null</exception>
        /// <exception cref="ArgumentException">locatorPart is of the incorrect type</exception>
        /// <exception cref="ArgumentException">startNode is not a FixedPage</exception>
        /// <exception cref="ArgumentException">startNode does not belong to the DocumentViewer</exception>
        public override Object ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out AttachmentLevel attachmentLevel)
        {
            if (startNode == null)
                throw new ArgumentNullException("startNode");

            DocumentPage docPage = null;
            FixedPage page = startNode as FixedPage;

            if (page != null)
            {
                docPage = GetDocumentPage(page);
            }
            else
            {
                // If we were passed a DPV because we are walking the visual tree,
                // extract the DocumentPage from it;  its TextView will be used to
                // turn coordinates into text positions
                DocumentPageView dpv = startNode as DocumentPageView;
                if (dpv != null)
                {
                    docPage = dpv.DocumentPage as FixedDocumentPage;
                    if (docPage == null)
                    {
                        docPage = dpv.DocumentPage as FixedDocumentSequenceDocumentPage;
                    }
                }
            }

            if (docPage == null)
            {
                throw new ArgumentException(SR.Get(SRID.StartNodeMustBeDocumentPageViewOrFixedPage), "startNode");
            }

            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            attachmentLevel = AttachmentLevel.Unresolved;

            ITextView tv = (ITextView)((IServiceProvider)docPage).GetService(typeof(ITextView));
            Debug.Assert(tv != null);

            ReadOnlyCollection<TextSegment> ts = tv.TextSegments;

            //check first if a TextRange can be generated
            if (ts == null || ts.Count <= 0)
                return null;

            TextAnchor resolvedAnchor = new TextAnchor();

            if (docPage != null)
            {
                string stringCount = locatorPart.NameValuePairs["Count"];
                if (stringCount == null)
                    throw new ArgumentException(SR.Get(SRID.InvalidLocatorPart, TextSelectionProcessor.CountAttribute));
                int count = Int32.Parse(stringCount, NumberFormatInfo.InvariantInfo);

                for (int i = 0; i < count; i++)
                {
                    // First we extract the start and end Point from the locator part.
                    Point start;
                    Point end;
                    GetLocatorPartSegmentValues(locatorPart, i, out start, out end);

                    //calulate start ITextPointer
                    ITextPointer segStart;
                    if (double.IsNaN(start.X) || double.IsNaN(start.Y))
                    {
                        //get start of the page
                        segStart = FindStartVisibleTextPointer(docPage);
                    }
                    else
                    {
                        //convert Point to TextPointer
                        segStart = tv.GetTextPositionFromPoint(start, true);
                    }

                    if (segStart == null)
                    {
                        //selStart can be null if there are no insertion points on this page 
                        continue;
                    }

                    //calulate end ITextPointer
                    ITextPointer segEnd;
                    if (double.IsNaN(end.X) || double.IsNaN(end.Y))
                    {
                        segEnd = FindEndVisibleTextPointer(docPage);
                    }
                    else
                    {
                        //convert Point to TextPointer
                        segEnd = tv.GetTextPositionFromPoint(end, true);
                    }

                    //end TP can not be null when start is not
                    Invariant.Assert(segEnd != null, "end TP is null when start TP is not");

                    attachmentLevel = AttachmentLevel.Full;  // Not always true right?
                    resolvedAnchor.AddTextSegment(segStart, segEnd);
                }
            }

            if (resolvedAnchor.TextSegments.Count > 0)
                return resolvedAnchor;
            else
                return null;
        }

        /// <summary>
        ///     Returns a list of XmlQualifiedNames representing the
        ///     the locator parts this processor can resolve/generate.
        /// </summary>
        public override XmlQualifiedName[] GetLocatorPartTypes()
        {
            return (XmlQualifiedName[])LocatorPartTypeNames.Clone();
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------        
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------        

        #region Private Methods

        private DocumentPage GetDocumentPage(FixedPage page)
        {
            Invariant.Assert(page != null);

            DocumentPage docPage = null;
            PageContent content = page.Parent as PageContent;
            if (content != null)
            {
                FixedDocument document = content.Parent as FixedDocument;

                // If the document is part of a FixedDocumentSequence then we want to get the 
                // FixedDocumentSequenceDocumentPage for the FixedPage (cause its TextView is 
                // the one we want to use).
                FixedDocumentSequence sequence = document.Parent as FixedDocumentSequence;
                if (sequence != null)
                {
                    docPage = sequence.GetPage(document, document.GetIndexOfPage(page));
                }
                else
                {
                    docPage = document.GetPage(document.GetIndexOfPage(page));
                }
            }
            return docPage;
        }

        /// <summary>
        /// Checks if the selection object satisfies the requirements 
        /// for this processor
        /// </summary>
        /// <param name="selection">selection</param>
        /// <returns>ITextRange interface, implemented by the object</returns>
        private IList<TextSegment> CheckSelection(object selection)
        {
            if (selection == null)
                throw new ArgumentNullException("selection");

            IList<TextSegment> textSegments = null;
            ITextPointer start = null;
            ITextRange textRange = selection as ITextRange;

            if (textRange != null)
            {
                start = textRange.Start;
                textSegments = textRange.TextSegments;
            }
            else
            {
                TextAnchor anchor = selection as TextAnchor;
                if (anchor != null)
                {
                    start = anchor.Start;
                    textSegments = anchor.TextSegments;
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "selection: type=" + selection.GetType().ToString());
                }
            }

            if (!(start.TextContainer is FixedTextContainer ||
                start.TextContainer is DocumentSequenceTextContainer))
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "selection: type=" + selection.GetType().ToString());

            return textSegments;
        }

        /// <summary>
        /// Checks if the selection object satisfies the requirements 
        /// for this processor
        /// </summary>
        /// <param name="selection">selection</param>
        /// <returns>ITextRange interface, implemented by the object</returns>
        private TextAnchor CheckAnchor(object selection)
        {
            if (selection == null)
                throw new ArgumentNullException("selection");

            TextAnchor anchor = selection as TextAnchor;

            if (anchor == null || !(anchor.Start.TextContainer is FixedTextContainer ||
                    anchor.Start.TextContainer is DocumentSequenceTextContainer))
            {
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "selection: type=" + selection.GetType().ToString());
            }

            return anchor;
        }

        /// <summary>
        ///     Extracts the values of attributes from a locator part.
        /// </summary>
        /// <param name="locatorPart">the locator part to extract values from</param>
        /// <param name="segmentNumber">number of segment value to retrieve</param>
        /// <param name="start">the start point value based on StartXAttribute and StartYAttribute values</param>
        /// <param name="end">the end point value based on EndXAttribyte and EndYattribute values</param>
        private void GetLocatorPartSegmentValues(ContentLocatorPart locatorPart, int segmentNumber, out Point start, out Point end)
        {
            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            if (FixedTextElementName != locatorPart.PartType)
                throw new ArgumentException(SR.Get(SRID.IncorrectLocatorPartType, locatorPart.PartType.Namespace + ":" + locatorPart.PartType.Name), "locatorPart");

            string segmentValue = locatorPart.NameValuePairs[TextSelectionProcessor.SegmentAttribute + segmentNumber.ToString(NumberFormatInfo.InvariantInfo)];
            if (segmentValue == null)
                throw new ArgumentException(SR.Get(SRID.InvalidLocatorPart, TextSelectionProcessor.SegmentAttribute + segmentNumber.ToString(NumberFormatInfo.InvariantInfo)));

            string[] values = segmentValue.Split(TextSelectionProcessor.Separator);
            if (values.Length != 4)
                throw new ArgumentException(SR.Get(SRID.InvalidLocatorPart, TextSelectionProcessor.SegmentAttribute + segmentNumber.ToString(NumberFormatInfo.InvariantInfo)));
            start = GetPoint(values[0], values[1]);
            end = GetPoint(values[2], values[3]);
        }

        /// <summary>
        /// Calculate Point out of string X and Y values
        /// </summary>
        /// <param name="xstr">x string value</param>
        /// <param name="ystr">y string value</param>
        /// <returns></returns>
        private Point GetPoint(string xstr, string ystr)
        {
            Point point;
            if (xstr != null && !String.IsNullOrEmpty(xstr.Trim()) && ystr != null && !String.IsNullOrEmpty(ystr.Trim()))
            {
                double x = Double.Parse(xstr, NumberFormatInfo.InvariantInfo);
                double y = Double.Parse(ystr, NumberFormatInfo.InvariantInfo);
                point = new Point(x, y);
            }
            else
            {
                point = new Point(double.NaN, double.NaN);
            }

            return point;
        }

        /// <summary>
        /// Gets the first visible TP on a DocumentPage
        /// </summary>
        /// <param name="documentPage">document page</param>
        /// <returns>The first visible TP or null if no visible TP on this page</returns>
        private static ITextPointer FindStartVisibleTextPointer(DocumentPage documentPage)
        {
            ITextPointer start, end;
            if (!GetTextViewRange(documentPage, out start, out end))
                return null;

            if (!start.IsAtInsertionPosition && !start.MoveToNextInsertionPosition(LogicalDirection.Forward))
            {
                //there is no insertion point in this direction
                return null;
            }

            //check if it is outside of the page
            if (start.CompareTo(end) > 0)
                return null;

            return start;
        }

        /// <summary>
        /// Gets the last visible TP on a DocumentPage
        /// </summary>
        /// <param name="documentPage">document page</param>
        /// <returns>The last visible TP or null if no visible TP on the page</returns>
        private static ITextPointer FindEndVisibleTextPointer(DocumentPage documentPage)
        {
            ITextPointer start, end;
            if (!GetTextViewRange(documentPage, out start, out end))
                return null;

            if (!end.IsAtInsertionPosition && !end.MoveToNextInsertionPosition(LogicalDirection.Backward))
            {
                //there is no insertion point in this direction
                return null;
            }
            //check if it is outside of the page
            if (start.CompareTo(end) > 0)
                return null;

            return end;
        }

        /// <summary>
        /// Gets first and last TP on a documentPage. 
        /// </summary>
        /// <param name="documentPage">the document page</param>
        /// <param name="start">start TP</param>
        /// <param name="end">end TP</param>
        /// <returns>true if there aretext segments on this page, otherwise false</returns>
        private static bool GetTextViewRange(DocumentPage documentPage, out ITextPointer start, out ITextPointer end)
        {
            ITextView textView;
            start = end = null;

            // Missing pages can't produce TextPointers
            Invariant.Assert(documentPage != DocumentPage.Missing);

            textView = ((IServiceProvider)documentPage).GetService(typeof(ITextView)) as ITextView;
            Invariant.Assert(textView != null, "DocumentPage didn't provide a TextView.");

            //check if there is any content
            if ((textView.TextSegments == null) || (textView.TextSegments.Count == 0))
                return false;

            start = textView.TextSegments[0].Start.CreatePointer(LogicalDirection.Forward);
            end = textView.TextSegments[textView.TextSegments.Count - 1].End.CreatePointer(LogicalDirection.Backward);
            Debug.Assert((start != null) && (end != null), "null start/end TextPointer on a non empty page");
            return true;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Name of locator part element
        private static readonly XmlQualifiedName FixedTextElementName = new XmlQualifiedName("FixedTextRange", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

        // ContentLocatorPart types understood by this processor
        private static readonly XmlQualifiedName[] LocatorPartTypeNames =
                new XmlQualifiedName[]
                {
                    FixedTextElementName
                };

        #endregion Private Fields

        #region Internal Classes
        /// <summary>
        /// Returned by GetSelectedNodes - one object per page spanned by the selection
        /// </summary>
        internal sealed class FixedPageProxy : DependencyObject
        {
            public FixedPageProxy(DependencyObject parent, int page)
            {
                SetValue(PathNode.HiddenParentProperty, parent);
                _page = page;
            }

            public int Page
            {
                get
                {
                    return _page;
                }
            }

            public IList<PointSegment> Segments
            {
                get
                {
                    return _segments;
                }
            }


            int _page;
            IList<PointSegment> _segments = new List<PointSegment>(1);
        }

        /// <summary>
        /// PointSegment represents a segment in fixed content with start and end points.
        /// </summary>
        internal sealed class PointSegment
        {
            /// <summary>
            /// Creates a PointSegment with the given points.
            /// </summary>
            internal PointSegment(Point start, Point end)
            {
                _start = start;
                _end = end;
            }

            /// <summary>
            /// The start point of the segment
            /// </summary>
            public Point Start
            {
                get
                {
                    return _start;
                }
            }

            /// <summary>
            /// The end point of the segment
            /// </summary>
            public Point End
            {
                get
                {
                    return _end;
                }
            }

            /// <summary>
            /// Used to represent a non-existent point - for instance for a segment
            /// which spans an entire page there is no starting point.
            /// </summary>
            public static readonly Point NotAPoint = new Point(double.NaN, double.NaN);

            private Point _start;
            private Point _end;
        }

        #endregion Internal Classes
    }
}
