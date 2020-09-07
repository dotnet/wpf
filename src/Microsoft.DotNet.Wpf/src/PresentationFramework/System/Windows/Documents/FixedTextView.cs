// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: TextView implementation for FixedDocument. 
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using MS.Internal.Documents;
    using MS.Internal.Media;
    using MS.Utility;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows.Documents;
    using System.Windows.Controls;
    using System.Windows.Shapes;
    using System.Windows.Media;
    using System.Windows.Media.TextFormatting;  // CharacterHit
    using System;


    /// <summary>
    /// TextView for each individual FixedDocumentPage
    /// </summary>
    internal sealed class FixedTextView : TextViewBase
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors
        internal FixedTextView(FixedDocumentPage  docPage)
        {
            _docPage = docPage;
        }
        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieves a position matching a point.
        /// </summary>
        /// <param name="point">
        /// Point in pixel coordinates to test.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method must always return a positioned text position 
        /// (the closest position as calculated by the control's heuristics)
        /// unless the point is outside the boundaries of the page.
        /// If false, this method should return null position, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <returns>
        /// A text position and its orientation matching or closest to the point.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        internal override ITextPointer GetTextPositionFromPoint(Point point, bool snapToText)
        {
            //Return ITextPointer to the end of this view in this special case
            if (point.Y == Double.MaxValue && point.X == Double.MaxValue)
            {
                FixedPosition fixedp;
                ITextPointer textPos = this.End;
                if (_GetFixedPosition(this.End, out fixedp))
                {
                    textPos = _CreateTextPointer(fixedp, LogicalDirection.Backward);
                    if (textPos == null)
                    {
                        textPos = this.End;
                    }
                }
                return textPos;
            }
        
            ITextPointer pos = null;
            
            UIElement e;
            bool isHit = _HitTest(point, out e);
            if (isHit)
            {
                Glyphs g = e as Glyphs;
                if (g != null)
                {
                    pos = _CreateTextPointerFromGlyphs(g, point);
                }
                else if (e is Image)
                {
                    Image im = (Image)e;
                    FixedPosition fixedp = new FixedPosition(this.FixedPage.CreateFixedNode(this.PageIndex, im), 0);
                    pos = _CreateTextPointer(fixedp, LogicalDirection.Forward);
                }
                else if (e is Path)
                {
                    Path p = (Path)e;
                    if (p.Fill is ImageBrush)
                    {
                        FixedPosition fixedp = new FixedPosition(this.FixedPage.CreateFixedNode(this.PageIndex, p), 0);
                        pos = _CreateTextPointer(fixedp, LogicalDirection.Forward);
                    }
                }
            }

            if (snapToText && pos == null)
            {
                pos = _SnapToText(point);
                Debug.Assert(pos != null);
            }


            DocumentsTrace.FixedTextOM.TextView.Trace(string.Format("GetTextPositionFromPoint P{0}, STT={1}, CP={2}", point, snapToText, pos == null ? "null" : ((FixedTextPointer)pos).ToString()));
            return pos;
        }

        /// <summary>
        /// Retrieves the height and offset, in pixels, of the edge of 
        /// the object/character represented by position.
        /// </summary>
        /// <param name="position">
        /// Position of an object/character.
        /// </param>
        /// <param name="transform">
        /// Transform to be applied to returned rect
        /// </param>
        /// <returns>
        /// The height, in pixels, of the edge of the object/character 
        /// represented by position.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        /// <remarks>
        /// Rect.Width is always 0.
        /// Output parameter Transform is always Identity. It is not expected that editing scenarios 
        /// will require speparate transform with raw rectangle for this case.
        /// If the document is empty, then this method returns the expected
        /// height of a character, if placed at the specified position.
        /// </remarks>
        internal override Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
#if DEBUG
            DocumentsTrace.FixedTextOM.TextView.Trace(string.Format("GetRectFromTextPosition {0}, {1}", (FixedTextPointer)position, position.LogicalDirection));
#endif

            FixedTextPointer ftp = Container.VerifyPosition(position);
            FixedPosition fixedp;

            // need a default caret size, otherwise infinite corners cause text editor and MultiPageTextView problems.
            // Initialize transform to Identity. This function always returns Identity transform.
            Rect designRect = new Rect(0, 0, 0, 10);
            transform = Transform.Identity;

            Debug.Assert(ftp != null);
            if (ftp.FlowPosition.IsBoundary)
            {
                if  (!_GetFirstFixedPosition(ftp, out fixedp))
                {
                    return designRect;
                }
            }

            else if (!_GetFixedPosition(ftp, out fixedp))
            {
                //
                // This is the start/end element, we need to find out the next element and return the next element
                // start offset/height.
                //
                if (position.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.None)
                {
                    ITextPointer psNext = position.CreatePointer(1);
                    FixedTextPointer ftpNext = Container.VerifyPosition(psNext);
                    if (!_GetFixedPosition(ftpNext, out fixedp))
                    {
                        return designRect;
                    }
                }
                else
                {
                    return designRect;
                }
            }

            if (fixedp.Page != this.PageIndex)
            {
                return designRect;
            }

            DependencyObject element = this.FixedPage.GetElement(fixedp.Node);
            if (element is Glyphs)
            {
                Glyphs g = (Glyphs)element;
                designRect = _GetGlyphRunDesignRect(g, fixedp.Offset, fixedp.Offset);
                // need to do transform
                GeneralTransform tran = g.TransformToAncestor(this.FixedPage);
                designRect = _GetTransformedCaretRect(tran, designRect.TopLeft, designRect.Height);
            }
            else if (element is Image)
            {
                Image image = (Image)element;
                GeneralTransform tran = image.TransformToAncestor(this.FixedPage);
                Point offset = new Point(0, 0);
                if (fixedp.Offset > 0)
                {
                    offset.X += image.ActualWidth;
                }
                designRect = _GetTransformedCaretRect(tran, offset, image.ActualHeight);
            }
            else if (element is Path)
            {
                Path path = (Path)element;
                GeneralTransform tran = path.TransformToAncestor(this.FixedPage);
                Rect bounds = path.Data.Bounds;
                Point offset = bounds.TopLeft;
                if (fixedp.Offset > 0)
                {
                    offset.X += bounds.Width;
                }
                designRect = _GetTransformedCaretRect(tran, offset, bounds.Height);
            }

            return designRect;
        }

        /// <summary>
        /// <see cref="TextViewBase.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal override Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            PathGeometry boundingGeometry = new PathGeometry();
            Debug.Assert(startPosition != null && this.Contains(startPosition));
            Debug.Assert(endPosition != null && this.Contains(endPosition));
            Dictionary<FixedPage, ArrayList> highlights = new Dictionary<FixedPage,ArrayList>();
            FixedTextPointer startftp = this.Container.VerifyPosition(startPosition);
            FixedTextPointer endftp = this.Container.VerifyPosition(endPosition);
            
            this.Container.GetMultiHighlights(startftp, endftp, highlights, FixedHighlightType.TextSelection, null, null);

            ArrayList highlightList;

            highlights.TryGetValue(this.FixedPage, out highlightList);

            if (highlightList != null)
            {
                foreach (FixedHighlight fh in highlightList)
                {
                    if (fh.HighlightType == FixedHighlightType.None)
                    {
                        continue;
                    }

                    Rect backgroundRect = fh.ComputeDesignRect();

                    if (backgroundRect == Rect.Empty)
                    {
                        continue;
                    }

                    GeneralTransform transform = fh.Element.TransformToAncestor(this.FixedPage);

                    Transform t = transform.AffineTransform;
                    if (t == null)
                    {
                        t = Transform.Identity;
                    }

                    Glyphs g = fh.Glyphs;

                    if (fh.Element.Clip != null)
                    {
                        Rect clipRect = fh.Element.Clip.Bounds;
                        backgroundRect.Intersect(clipRect);
                    }
                    
                    Geometry thisGeometry = new RectangleGeometry(backgroundRect);
                    thisGeometry.Transform = t;

                    backgroundRect = transform.TransformBounds(backgroundRect);

                    boundingGeometry.AddGeometry(thisGeometry);
                }
            }

            return boundingGeometry;
        }

        /// <summary>
        /// Retrieves an oriented text position matching position advanced by 
        /// a number of lines from its initial position.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="suggestedX">
        /// The suggested X offset, in pixels, of text position on the destination 
        /// line. If suggestedX is set to Double.NaN it will be ignored, otherwise 
        /// the method will try to find a position on the destination line closest 
        /// to suggestedX.
        /// </param>
        /// <param name="count">
        /// Number of lines to advance. Negative means move backwards.
        /// </param>
        /// <param name="newSuggestedX">
        /// newSuggestedX is the offset at the position moved (useful when moving 
        /// between columns or pages).
        /// </param>
        /// <param name="linesMoved">
        /// linesMoved indicates the number of lines moved, which may be less 
        /// than count if there is no more content.
        /// </param>
        /// <returns>
        /// A TextPointer and its orientation matching suggestedX on the 
        /// destination line.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        internal override ITextPointer GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved)
        {
            newSuggestedX = suggestedX;
            linesMoved = 0;
#if DEBUG
            DocumentsTrace.FixedTextOM.TextView.Trace(string.Format("FixedTextView.MoveToLine {0}, {1}, {2}, {3}", (FixedTextPointer)position, position.LogicalDirection, suggestedX, count));
#endif

            FixedPosition fixedp;
            LogicalDirection edge = position.LogicalDirection;
            LogicalDirection scanDir = LogicalDirection.Forward;
            ITextPointer pos = position;

            FixedTextPointer ftp = Container.VerifyPosition(position);
            FixedTextPointer nav = new FixedTextPointer(true, edge, (FlowPosition)ftp.FlowPosition.Clone());

            //Skip any formatting tags
            _SkipFormattingTags(nav);
            bool gotFixedPosition = false;
            if (   count == 0
                || ((gotFixedPosition = _GetFixedPosition(nav, out fixedp))
                && fixedp.Page != this.PageIndex )
                )
            {
                //Invalid text position, so do nothing. PS #963924
                return position;
            }

            if (count < 0)
            {
                count   = -count;
                scanDir = LogicalDirection.Backward;
            }

            if (!gotFixedPosition)
            {
                // move to next insertion position in direction, provided we're in this view
                if (this.Contains(position))
                {
                    nav = new FixedTextPointer(true, scanDir, (FlowPosition)ftp.FlowPosition.Clone());
                    ((ITextPointer)nav).MoveToInsertionPosition(scanDir);
                    ((ITextPointer)nav).MoveToNextInsertionPosition(scanDir);
                    if (this.Contains(nav))
                    {
                        // make sure we haven't changed pages
                        linesMoved = (scanDir == LogicalDirection.Forward) ? 1 : -1;
                        return nav;
                    }
                }
                return position;
            }

            if (DoubleUtil.IsNaN(suggestedX))
            {
                suggestedX = 0;
            }

            while (count > linesMoved)
            {
                if (!_GetNextLineGlyphs(ref fixedp, ref edge, suggestedX, scanDir))
                    break;
                linesMoved++;
            }

            if (linesMoved == 0)
            {
                pos = position.CreatePointer();
                return pos;
            }

            if (scanDir == LogicalDirection.Backward)
            {
                linesMoved = -linesMoved;
            }
            
            pos = _CreateTextPointer(fixedp, edge);
            Debug.Assert(pos != null);

            if (pos.CompareTo(position) == 0)
            {
                linesMoved = 0;
            }
            
            return pos;
        }

        /// <summary>
        /// Determines if a position is located between two caret units.
        /// </summary>
        /// <param name="position">
        /// Position to test.
        /// </param>
        /// <returns>
        /// Returns true if the specified position precedes or follows 
        /// the first or last code point of a caret unit, respectively.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        /// <remarks>
        /// In the context of this method, "caret unit" refers to a group
        /// of one or more Unicode code points that map to a single rendered
        /// glyph.
        /// </remarks>
        internal override bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            FixedTextPointer ftp = Container.VerifyPosition(position);
            FixedPosition fixedp;

            if (_GetFixedPosition(ftp, out fixedp))
            {
                DependencyObject element = this.FixedPage.GetElement(fixedp.Node);
                if (element is Glyphs)
                {
                    Glyphs g = (Glyphs)element;
                    int characterCount = (g.UnicodeString == null ? 0 : g.UnicodeString.Length);
                    if (fixedp.Offset == characterCount)
                    {   //end of line -- allow caret
                        return true;
                    }
                    else
                    {
                        GlyphRun run = g.MeasurementGlyphRun;
                        return run.CaretStops == null || run.CaretStops[fixedp.Offset];
                    }
                }
                else if (element is Image || element is Path)
                {   //support caret before and after image
                    return true;
                }
                else
                {
                    // No text position should be on any other type of element
                    Debug.Assert(false);
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the next position at the edge of a caret unit in 
        /// specified direction.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="direction">
        /// If Forward, this method returns the "caret unit" position following 
        /// the initial position.
        /// If Backward, this method returns the caret unit" position preceding 
        /// the initial position.
        /// </param>
        /// <returns>
        /// The next caret unit break position in specified direction.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        /// <remarks>
        /// In the context of this method, "caret unit" refers to a group of one 
        /// or more Unicode code points that map to a single rendered glyph.
        /// 
        /// If position is located between two caret units, this method returns 
        /// a new position located at the opposite edge of the caret unit in 
        /// the indicated direction.
        /// If position is located within a group of Unicode code points that map 
        /// to a single caret unit, this method returns a new position at 
        /// the indicated edge of the containing caret unit.
        /// If position is located at the beginning of end of content -- there is 
        /// no content in the indicated direction -- then this method returns 
        /// a position located at the same location as initial position.
        /// </remarks>
        internal override ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            FixedTextPointer ftp = Container.VerifyPosition(position);
            FixedPosition fixedp;

            if (_GetFixedPosition(ftp, out fixedp))
            {
                DependencyObject element = this.FixedPage.GetElement(fixedp.Node);
                if (element is Glyphs)
                {
                    Glyphs g = (Glyphs)element;
                    GlyphRun run = g.ToGlyphRun();

                    int characterCount = (run.Characters == null) ? 0 : run.Characters.Count;
                    CharacterHit start = (fixedp.Offset == characterCount) ?
                        new CharacterHit(fixedp.Offset - 1, 1) :
                        new CharacterHit(fixedp.Offset, 0);
                    CharacterHit next = (direction == LogicalDirection.Forward) ?
                        run.GetNextCaretCharacterHit(start) :
                        run.GetPreviousCaretCharacterHit(start);

                    if (!start.Equals(next))
                    {
                        LogicalDirection edge = LogicalDirection.Forward;
                        if (next.TrailingLength > 0)
                        {
                            edge = LogicalDirection.Backward;
                        }

                        int index = next.FirstCharacterIndex + next.TrailingLength;

                        return _CreateTextPointer(new FixedPosition(fixedp.Node, index), edge);
                    }
                }
            }
            //default behavior is to simply move textpointer
            ITextPointer pointer = position.CreatePointer();
            pointer.MoveToNextInsertionPosition(direction);
            return pointer;
        }

        /// <summary>
        /// Finds the previous position at the edge of a caret after backspacing.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <returns>
        /// The previous caret unit break position after backspacing.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        internal override ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a TextSegment that spans the line on which position is located.
        /// </summary>
        /// <param name="position">
        /// Any oriented text position on the line.
        /// </param>
        /// <returns>
        /// TextSegment that spans the line on which position is located.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        internal override TextSegment GetLineRange(ITextPointer position)
        {
#if DEBUG
            DocumentsTrace.FixedTextOM.TextView.Trace(string.Format("GetLineRange {0}, {1}", (FixedTextPointer)position, position.LogicalDirection));
#endif
            FixedTextPointer ftp = Container.VerifyPosition(position);
            FixedPosition fixedp;
            if (!_GetFixedPosition(ftp, out fixedp))
            {
				return new TextSegment(position, position, true);
            }

            int count = 0;
            FixedNode[] fixedNodes = Container.FixedTextBuilder.GetNextLine(fixedp.Node, true, ref count);

            if (fixedNodes == null)
            {
                // This will happen in the case of images
                fixedNodes = new FixedNode[] { fixedp.Node };
            }

            FixedNode lastNode = fixedNodes[fixedNodes.Length - 1];
            DependencyObject element = FixedPage.GetElement(lastNode);

            int lastIndex = 1;
            if (element is Glyphs)
            {
                lastIndex = ((Glyphs)element).UnicodeString.Length;
            }

            ITextPointer begin = _CreateTextPointer(new FixedPosition(fixedNodes[0], 0), LogicalDirection.Forward);
            ITextPointer end = _CreateTextPointer(new FixedPosition(lastNode, lastIndex), LogicalDirection.Backward);

            if (begin.CompareTo(end) > 0)
            {
                ITextPointer temp = begin;
                begin = end;
                end = temp;
            }

            return new TextSegment(begin, end, true);
        }

        /// <summary>
        /// Determines whenever TextView contains specified position.
        /// </summary>
        /// <param name="position">
        /// A position to test.
        /// </param>
        /// <returns>
        /// True if TextView contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        internal override bool Contains(ITextPointer position)
        {
            FixedTextPointer tp = Container.VerifyPosition(position);

            return ((tp.CompareTo(this.Start) > 0 && tp.CompareTo(this.End) < 0) ||
                    (tp.CompareTo(this.Start) == 0 && (tp.LogicalDirection == LogicalDirection.Forward || this.IsContainerStart)) ||
                    (tp.CompareTo(this.End) == 0 && (tp.LogicalDirection == LogicalDirection.Backward || this.IsContainerEnd))
                    );
        }

        /// <summary>
        /// Makes sure that TextView is in a clean layout state and it is 
        /// possible to retrieve layout related data.
        /// </summary>
        /// <remarks>
        /// If IsValid returns false, it is required to call this method 
        /// before calling any other method on TextView.
        /// Validate method might be very expensive, because it may lead 
        /// to full layout update.
        /// </remarks>
        internal override bool Validate()
        {
            // Always valid. Do nothing. 
            return true;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// </summary>
        internal override UIElement RenderScope 
        { 
            get 
            {
                Visual visual = _docPage.Visual;

                while (visual != null && !(visual is UIElement))
                {
                    visual = VisualTreeHelper.GetParent(visual) as Visual;
                }

                return visual as UIElement;
            }
        }

        /// <summary>
        /// TextContainer that stores content.
        /// </summary>
        internal override ITextContainer TextContainer { get { return this.Container; } }

        /// <summary>
        /// Determines whenever layout is in clean state and it is possible
        /// to retrieve layout related data.
        /// </summary>
        internal override bool IsValid { get { return true; } }


        /// <summary>
        /// <see cref="ITextView.RendersOwnSelection"/>
        /// </summary>
        internal override bool RendersOwnSelection
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Collection of TextSegments representing content of the TextView.
        /// </summary>
        internal override ReadOnlyCollection<TextSegment> TextSegments
        { 
            get 
            {
                if (_textSegments == null)
                {
                    List<TextSegment> list = new List<TextSegment>(1);
                    list.Add(new TextSegment(this.Start, this.End, true));
                    _textSegments = new ReadOnlyCollection<TextSegment>(list);
                }
                return _textSegments;
            }
        }

        internal FixedTextPointer Start
        {
            get
            {
                if (_start == null)
                {
                    FlowPosition flowStart = Container.FixedTextBuilder.GetPageStartFlowPosition(this.PageIndex);
                    _start = new FixedTextPointer(false, LogicalDirection.Forward, flowStart);
                }
                return _start;
            }
        }

        internal FixedTextPointer End
        {
            get
            {
                if (_end == null)
                {
                    FlowPosition flowEnd = Container.FixedTextBuilder.GetPageEndFlowPosition(this.PageIndex);
                    _end = new FixedTextPointer(false, LogicalDirection.Backward, flowEnd);
                }
                return _end;
            }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // hit testing to find the inner most UIElement that was hit
        // as well as the containing fixed page. 
        private bool _HitTest(Point pt, out UIElement e)
        {
            e = null;

            HitTestResult result = VisualTreeHelper.HitTest(this.FixedPage, pt);
            DependencyObject v = (result != null) ? result.VisualHit : null;

            while (v != null)
            {
                DependencyObjectType t = v.DependencyObjectType;
                if (t == UIElementType || t.IsSubclassOf(UIElementType))
                {
                    e = (UIElement) v;
                    
                    return true;
                }
                v = VisualTreeHelper.GetParent(v);
            }

            return false;
        }

        private void _GlyphRunHitTest(Glyphs g, double xoffset, out int charIndex, out LogicalDirection edge)
        {
            charIndex = 0;
            edge = LogicalDirection.Forward;

            GlyphRun run = g.ToGlyphRun();
            bool isInside;

            double distance;
            if ((run.BidiLevel & 1) != 0)
            {
                distance = run.BaselineOrigin.X - xoffset;
            }
            else
            {
                distance = xoffset - run.BaselineOrigin.X;
            }

            CharacterHit hit = run.GetCaretCharacterHitFromDistance(distance, out isInside);

            charIndex = hit.FirstCharacterIndex + hit.TrailingLength;
            edge = (hit.TrailingLength > 0) ? LogicalDirection.Backward : LogicalDirection.Forward;
        }


        private ITextPointer _SnapToText(Point point)
        {
            ITextPointer itp = null;
            FixedNode[] fixedNodes = Container.FixedTextBuilder.GetLine(this.PageIndex, point);
            if (fixedNodes != null && fixedNodes.Length > 0)
            {
                double closestDistance = Double.MaxValue;
                double xoffset = 0;
                Glyphs closestGlyphs = null;
                FixedNode closestNode = fixedNodes[0];

                foreach (FixedNode node in fixedNodes)
                {
                    Glyphs startGlyphs = this.FixedPage.GetGlyphsElement(node);
                    GeneralTransform tranToGlyphs = this.FixedPage.TransformToDescendant(startGlyphs);
                    Point transformedPt = point;
                    if (tranToGlyphs != null)
                    {                        
                        tranToGlyphs.TryTransform(transformedPt, out transformedPt);
                    }

                    GlyphRun run = startGlyphs.ToGlyphRun();
                    Rect alignmentRect = run.ComputeAlignmentBox();
                    alignmentRect.Offset(startGlyphs.OriginX, startGlyphs.OriginY);

                    double horizontalDistance = Math.Max(0, (transformedPt.X > alignmentRect.X) ? (transformedPt.X - alignmentRect.Right) : (alignmentRect.X - transformedPt.X));
                    double verticalDistance = Math.Max(0, (transformedPt.Y > alignmentRect.Y) ? (transformedPt.Y - alignmentRect.Bottom) : (alignmentRect.Y - transformedPt.Y));
                    double manhattanDistance = horizontalDistance + verticalDistance;

                    if (closestGlyphs == null || manhattanDistance < closestDistance)
                    {
                        closestDistance = manhattanDistance;
                        closestGlyphs = startGlyphs;
                        closestNode = node;
                        xoffset = transformedPt.X;
                    }
                }

                int index;
                LogicalDirection dir;
                _GlyphRunHitTest(closestGlyphs, xoffset, out index, out dir);

                FixedPosition fixedp = new FixedPosition(closestNode, index);
                itp = _CreateTextPointer(fixedp, dir);
				Debug.Assert(itp != null);
            }
            else
            {
                //
                // That condition is only possible when there is no line in the page
                //
                if (point.Y < this.FixedPage.Height / 2)
                {
                    itp = ((ITextPointer)this.Start).CreatePointer(LogicalDirection.Forward);
                    itp.MoveToInsertionPosition(LogicalDirection.Forward);
                }
                else
                {
                    itp = ((ITextPointer)this.End).CreatePointer(LogicalDirection.Backward);
                    itp.MoveToInsertionPosition(LogicalDirection.Backward);
                }
            }
            return itp;
        }


        // If return false, nothing has been modified, which implies out of page boundary
        // The input of suggestedX is in the VisualRoot's cooridnate system
        private bool _GetNextLineGlyphs(ref FixedPosition fixedp, ref LogicalDirection edge, double suggestedX, LogicalDirection scanDir)
        {
            int count = 1;
            int pageIndex = fixedp.Page;
            bool moved = false;
            FixedNode[] fixedNodes = Container.FixedTextBuilder.GetNextLine(fixedp.Node, (scanDir == LogicalDirection.Forward), ref count);

            if (fixedNodes != null && fixedNodes.Length > 0)
            {
                FixedPage page = Container.FixedDocument.SyncGetPage(pageIndex, false);
                // This line contains multiple Glyhs. Scan backward
                // util we hit the first one whose OriginX is smaller 
                // then suggestedX;
                if (Double.IsInfinity(suggestedX))
                {
                    suggestedX = 0;
                }
                Point topOfPage = new Point(suggestedX, 0);
                Point secondPoint = new Point(suggestedX, 1000);

                FixedNode hitNode = fixedNodes[0];
                Glyphs hitGlyphs = null;
                double closestDistance = Double.MaxValue;
                double xoffset = 0;

                for (int i = fixedNodes.Length - 1; i >= 0; i--)
                {
                    FixedNode node = fixedNodes[i];
                    Glyphs g = page.GetGlyphsElement(node);
                    if (g != null)
                    {
                        GeneralTransform transform = page.TransformToDescendant(g);
                        Point pt1 = topOfPage;
                        Point pt2 = secondPoint;
                        if (transform != null)
                        {                            
                            transform.TryTransform(pt1, out pt1);
                            transform.TryTransform(pt2, out pt2);
                        }
                        double invSlope = (pt2.X - pt1.X) / (pt2.Y - pt1.Y);
                        double xoff, distance;

                        GlyphRun run = g.ToGlyphRun();
                        Rect box = run.ComputeAlignmentBox();
                        box.Offset(g.OriginX, g.OriginY);

                        if (invSlope > 1000 || invSlope < -1000)
                        {
                            // special case for vertical text
                            xoff = 0;
                            distance = (pt1.Y > box.Y) ? (pt1.Y - box.Bottom) : (box.Y - pt1.Y);
                        }
                        else
                        {
                            double centerY = (box.Top + box.Bottom) / 2;
                            xoff = pt1.X + invSlope * (centerY - pt1.Y);
                            distance = (xoff > box.X) ? (xoff - box.Right) : (box.X - xoff);
                        }

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            xoffset = xoff;
                            hitNode = node;
                            hitGlyphs = g;

                            if (distance <= 0)
                            {
                                break;
                            }
                        }
                    }
                }

                Debug.Assert(hitGlyphs != null);

                int charIdx;
                _GlyphRunHitTest(hitGlyphs, xoffset, out charIdx, out edge);

                fixedp = new FixedPosition(hitNode, charIdx);
                moved = true;
            }

            return moved;
        }

        private static double _GetDistanceToCharacter(GlyphRun run, int charOffset)
        {
            int firstChar = charOffset, trailingLength = 0;

            int characterCount = (run.Characters == null) ? 0 : run.Characters.Count;
            if (firstChar == characterCount)
            {
                // place carat at end of previous character to make sure it works at end of line
                firstChar--;
                trailingLength = 1;
            }

            return run.GetDistanceFromCaretCharacterHit(new CharacterHit(firstChar, trailingLength));
        }

        // char index == -1 implies end of run. 
        internal static Rect _GetGlyphRunDesignRect(Glyphs g, int charStart, int charEnd)
        {
            GlyphRun run = g.ToGlyphRun();
            if (run == null)
            {
                return Rect.Empty;
            }

            Rect designRect = run.ComputeAlignmentBox();
            designRect.Offset(run.BaselineOrigin.X, run.BaselineOrigin.Y);

            int charCount = 0;
            if (run.Characters != null)
            {
                charCount = run.Characters.Count;
            }
            else if (g.UnicodeString != null)
            {
                charCount = g.UnicodeString.Length;
            }

            if (charStart > charCount)
            {
                //Extra space was added at the end of the run for contiguity
                Debug.Assert(charStart - charCount == 1);
                charStart = charCount;
            }
            else if (charStart < 0)
            {
                //This is a reversed run
                charStart = 0;
            }
            if (charEnd > charCount)
            {
                //Extra space was added at the end of the run for contiguity
                Debug.Assert(charEnd - charCount == 1);
                charEnd = charCount;
            }
            else if (charEnd < 0)
            {
                //This is a reversed run
                charEnd = 0;
            }


            double x1 = _GetDistanceToCharacter(run, charStart);
            double x2 = _GetDistanceToCharacter(run, charEnd);
            double width = x2 - x1;

            if ((run.BidiLevel & 1) != 0)
            {
                // right to left
                designRect.X = run.BaselineOrigin.X - x2;
            }
            else
            {
                designRect.X = run.BaselineOrigin.X + x1;
            }

            designRect.Width = width;

            return designRect;
        }

        // Creates an axis-aligned caret for possibly rotated glyphs
        private Rect _GetTransformedCaretRect(GeneralTransform transform, Point origin, double height)
        {
            Point bottom = origin;
            bottom.Y += height;
            transform.TryTransform(origin, out origin);
            transform.TryTransform(bottom, out bottom);
            Rect caretRect = new Rect(origin, bottom);
            if (caretRect.Width > 0)
            {
                // only vertical carets are supported by TextEditor
                // What to do if height == 0?
                caretRect.X += caretRect.Width / 2;
                caretRect.Width = 0;
            }

            if (caretRect.Height < 1)
            {
                caretRect.Height = 1;
            }

            return caretRect;
        }

        //--------------------------------------------------------------------
        // FlowPosition --> FixedPosition
        //---------------------------------------------------------------------

        // Helper function to overcome the limitation in FixedTextBuilder.GetFixedPosition 
        // Making sure we are asking a position that is either a Run or an EmbeddedElement. 
        private bool _GetFixedPosition(FixedTextPointer ftp, out FixedPosition fixedp)
        {
            LogicalDirection textdir = ftp.LogicalDirection;
            TextPointerContext symbolType = ((ITextPointer)ftp).GetPointerContext(textdir);

            if (ftp.FlowPosition.IsBoundary || symbolType == TextPointerContext.None)
            {
                return _GetFirstFixedPosition(ftp, out fixedp);
            }

            if (symbolType == TextPointerContext.ElementStart || symbolType == TextPointerContext.ElementEnd)
            {
                //Try to find the first valid insertion position if exists
                switch (symbolType)
                {
                case TextPointerContext.ElementStart:
                    textdir = LogicalDirection.Forward;
                    break;
                case TextPointerContext.ElementEnd:
                    textdir = LogicalDirection.Backward;
                    break;
                }

                FixedTextPointer nav = new FixedTextPointer(true, textdir, (FlowPosition)ftp.FlowPosition.Clone());

                _SkipFormattingTags(nav);

                symbolType = ((ITextPointer)nav).GetPointerContext(textdir);
                if (symbolType != TextPointerContext.Text && symbolType != TextPointerContext.EmbeddedElement)
                {
                    //Try moving to the next insertion position
                    if (((ITextPointer)nav).MoveToNextInsertionPosition(textdir) &&
                        this.Container.GetPageNumber(nav) == this.PageIndex)
                    {
                        return Container.FixedTextBuilder.GetFixedPosition(nav.FlowPosition, textdir, out fixedp);
                    }
                    else
                    {
                        fixedp = new FixedPosition(this.Container.FixedTextBuilder.FixedFlowMap.FixedStartEdge, 0);
                        return false;
                    }
                }
                else
                {
                    ftp = nav;
                }
            }

            Debug.Assert(symbolType == TextPointerContext.Text || symbolType == TextPointerContext.EmbeddedElement);
            return Container.FixedTextBuilder.GetFixedPosition(ftp.FlowPosition, textdir, out fixedp);
        }


        //Find the first valid insertion position after or before the boundary node
        private bool _GetFirstFixedPosition(FixedTextPointer ftp, out FixedPosition fixedP)
        {
            LogicalDirection dir = LogicalDirection.Forward;
            if (ftp.FlowPosition.FlowNode.Fp != 0)
            {
                //End boundary
                dir = LogicalDirection.Backward;
            }
            FlowPosition flowP = (FlowPosition) ftp.FlowPosition.Clone();
            //Get the first node that comes before or after the boundary node(probably a start or end node)
            flowP.Move(dir);

            FixedTextPointer nav = new FixedTextPointer(true, dir, flowP);
            if (flowP.IsStart || flowP.IsEnd)
            {
                ((ITextPointer)nav).MoveToNextInsertionPosition(dir);
            }
            if (this.Container.GetPageNumber(nav) == this.PageIndex)
            {
                return Container.FixedTextBuilder.GetFixedPosition(nav.FlowPosition, dir, out fixedP);
            }
            else
            {
                //This position is on another page.
                fixedP = new FixedPosition(this.Container.FixedTextBuilder.FixedFlowMap.FixedStartEdge, 0);
                return false;
            }
        }

        //--------------------------------------------------------------------
        // FixedPosition --> ITextPointer
        //---------------------------------------------------------------------

        // Create a text position from description of a fixed position. 
        // This is primarily called from HitTesting code
        private ITextPointer _CreateTextPointer(FixedPosition fixedPosition, LogicalDirection edge)
        {
            // Create a FlowPosition to represent this fixed position
            FlowPosition flowHit = Container.FixedTextBuilder.CreateFlowPosition(fixedPosition);
            if (flowHit != null)
            {
                DocumentsTrace.FixedTextOM.TextView.Trace(string.Format("_CreatetTextPointer {0}:{1}", fixedPosition.ToString(), flowHit.ToString()));

                // Create a TextPointer from the flow position
                return new FixedTextPointer(true, edge, flowHit);
            }
            return null;
        }

        // Create a text position from description of a fixed position. 
        // This is primarily called from HitTesting code
        private ITextPointer _CreateTextPointerFromGlyphs(Glyphs g, Point point)
        {
            GeneralTransform transform = this.VisualRoot.TransformToDescendant(g);
            if (transform != null)
            {
                transform.TryTransform(point, out point);
            }

            int charIndex;
            LogicalDirection edge;
            _GlyphRunHitTest(g, point.X, out charIndex, out edge);
            FixedPosition fixedp = new FixedPosition(this.FixedPage.CreateFixedNode(this.PageIndex, g), charIndex);
            return _CreateTextPointer(fixedp, edge);
        }

        private void _SkipFormattingTags(ITextPointer textPointer)
        {
            Debug.Assert(!textPointer.IsFrozen, "Can't reposition a frozen pointer!");
            
            LogicalDirection dir = textPointer.LogicalDirection;
            int increment = (dir == LogicalDirection.Forward ? +1 : -1);
            while (TextSchema.IsFormattingType( textPointer.GetElementType(dir)) )
            {
                textPointer.MoveByOffset(increment);
            }
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties
        private FixedTextContainer Container
        {
            get
            {
                return (FixedTextContainer)_docPage.TextContainer;
            }
        }


        // The visual node that is root of this TextView's visual tree
        private Visual VisualRoot
        {
            get
            {
                return this._docPage.Visual;
            }
        }

        // The FixedPage that provides content for this view
        private FixedPage FixedPage
        {
            get
            {
                return this._docPage.FixedPage;
            }
        }

        // 
        private int PageIndex
        {
            get
            {
                return this._docPage.PageIndex;
            }
        }

        private bool IsContainerStart
        {
            get
            {
                return (this.Start.CompareTo(this.TextContainer.Start) == 0);
            }
        }

        private bool IsContainerEnd
        {
            get
            {
                return (this.End.CompareTo(this.TextContainer.End) == 0);
            }
        }
        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields
        private readonly FixedDocumentPage _docPage;

        // Cache for start/end
        private FixedTextPointer _start;
        private FixedTextPointer _end;
        private ReadOnlyCollection<TextSegment> _textSegments;

        /// <summary>
        /// Caches the UIElement's DependencyObjectType.
        /// </summary>
        private static DependencyObjectType UIElementType = DependencyObjectType.FromSystemTypeInternal(typeof(UIElement));
        #endregion Private Fields
    }
}


