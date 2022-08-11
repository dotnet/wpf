// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//      DocumentSequenceTextView implements TextView for DocumentSequence
//      to support text editing (e.g Selection). 
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using MS.Internal;
    using MS.Utility;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;


    /// <summary>
    /// DocumentSequenceTextView implements TextView for DocumentSequence
    /// to support text editing (e.g Selection). 
    /// </summary>
    internal sealed class DocumentSequenceTextView : TextViewBase
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        internal DocumentSequenceTextView(FixedDocumentSequenceDocumentPage docPage)
        {
            _docPage = docPage;
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieves a position matching a point.
        /// </summary>
        /// <param name="point">
        /// Point in pixel coordinates to test.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method must always return a positioned text position 
        /// (the closest position as calculated by the control's heuristics). 
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
            DocumentsTrace.FixedDocumentSequence.TextOM.Trace(string.Format("GetTextPositionFromPoint {0}-{1}", point, snapToText));
            DocumentSequenceTextPointer tp = null;
            LogicalDirection edge = LogicalDirection.Forward;

            if (ChildTextView != null)
            {
                ITextPointer childOTP = ChildTextView.GetTextPositionFromPoint(point, snapToText);
                if (childOTP != null)
                {
                    tp = new DocumentSequenceTextPointer(ChildBlock, childOTP);
                    edge = childOTP.LogicalDirection;
                }
            }

            // When snapToText is true, ChildTextView.GetTextPositionFromPoint will guranttee to
            // return a non-null position. 
            // In current code, ChildTextView can't be null. 
            return tp == null ? null : DocumentSequenceTextPointer.CreatePointer(tp, edge);
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
        /// 
        /// If the document is empty, then this method returns the expected
        /// height of a character, if placed at the specified position.
        /// </remarks>
        internal override Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
            DocumentsTrace.FixedDocumentSequence.TextOM.Trace(string.Format("GetRawRectangleFromTextPosition {0} {1}", position, position.LogicalDirection));
            DocumentSequenceTextPointer tp = null;

            // Initialize transform to identity
            transform = Transform.Identity;

            if (position != null)
            {
                 tp = _docPage.FixedDocumentSequence.TextContainer.VerifyPosition(position);
            }

            if (tp != null)
            {
                if (ChildTextView != null)
                {
                    if (ChildTextView.TextContainer == tp.ChildBlock.ChildContainer)
                    {
                        return ChildTextView.GetRawRectangleFromTextPosition(tp.ChildPointer.CreatePointer(position.LogicalDirection), out transform);
                    }
                }
            }
            return Rect.Empty;
        }

        /// <summary>
        /// <see cref="TextViewBase.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal override Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            if (startPosition != null && endPosition != null && ChildTextView != null)
            {
                DocumentSequenceTextPointer startTp = null;
                DocumentSequenceTextPointer endTp = null;
                
                startTp = _docPage.FixedDocumentSequence.TextContainer.VerifyPosition(startPosition);
                endTp = _docPage.FixedDocumentSequence.TextContainer.VerifyPosition(endPosition);

                if (startTp != null && endTp != null)
                {
                    return ChildTextView.GetTightBoundingGeometryFromTextPositions(startTp.ChildPointer, endTp.ChildPointer);
                }
            }

            return (new PathGeometry());;
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
            linesMoved = count;

            DocumentsTrace.FixedDocumentSequence.TextOM.Trace(string.Format("GetPositionAtNextLine {0} {1} {2} {3} ", position, position.LogicalDirection, suggestedX, count));
            DocumentSequenceTextPointer newTp  = null;
            LogicalDirection newEdge = LogicalDirection.Forward;
            DocumentSequenceTextPointer tp = null;
            if (position != null)
            {
                tp = _docPage.FixedDocumentSequence.TextContainer.VerifyPosition(position);
            }

            // Note we do not handle cross page navigation
            if (tp != null)
            {
                if (ChildTextView != null)
                {
                    if (ChildTextView.TextContainer == tp.ChildBlock.ChildContainer)
                    {
                        ITextPointer childOTP = ChildTextView.GetPositionAtNextLine(tp.ChildPointer.CreatePointer(position.LogicalDirection), suggestedX, count, out newSuggestedX, out linesMoved);
                        if (childOTP != null)
                        {
                            newTp = new DocumentSequenceTextPointer(ChildBlock, childOTP);
                            newEdge = childOTP.LogicalDirection;
                        }
                    }
                }
            }
            return DocumentSequenceTextPointer.CreatePointer(newTp, newEdge);
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
            Invariant.Assert(position != null);            
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            //Verify the position and propagate the call to the child text view
            Invariant.Assert(ChildTextView != null);
            DocumentSequenceTextPointer ftp = this.DocumentSequenceTextContainer.VerifyPosition(position);

            return this.ChildTextView.IsAtCaretUnitBoundary(ftp.ChildPointer);
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
            Invariant.Assert(position != null);            
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            //Verify the position and propagate the call to the child text view
            Invariant.Assert(ChildTextView != null);
            DocumentSequenceTextPointer ftp = this.DocumentSequenceTextContainer.VerifyPosition(position);

            return this.ChildTextView.GetNextCaretUnitPosition(ftp.ChildPointer, direction);
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
            Invariant.Assert(position != null);            
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            //Verify the position and propagate the call to the child text view
            Invariant.Assert(ChildTextView != null);
            DocumentSequenceTextPointer ftp = this.DocumentSequenceTextContainer.VerifyPosition(position);

            return this.ChildTextView.GetBackspaceCaretUnitPosition(ftp.ChildPointer);
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
            DocumentsTrace.FixedDocumentSequence.TextOM.Trace(string.Format("GetLineRange {0} {1}", position, position.LogicalDirection));
            DocumentSequenceTextPointer tpStart = null;
            DocumentSequenceTextPointer tpEnd   = null;
            DocumentSequenceTextPointer tpLine = null;

            if (position != null)
            {
                if (ChildTextView != null)
                {
                    tpLine = _docPage.FixedDocumentSequence.TextContainer.VerifyPosition(position);

                    if (ChildTextView.TextContainer == tpLine.ChildBlock.ChildContainer)
                    {
                        TextSegment childTR = ChildTextView.GetLineRange(tpLine.ChildPointer.CreatePointer(position.LogicalDirection));
                        if (!childTR.IsNull)
                        {
                            tpStart = new DocumentSequenceTextPointer(ChildBlock, childTR.Start);
                            tpEnd = new DocumentSequenceTextPointer(ChildBlock, childTR.End);
                            return new TextSegment(tpStart, tpEnd, true);
                        }
                    }
                }
            }
            return TextSegment.Null;
        }

        /// <summary>
        /// Provides a collection of glyph properties corresponding to runs 
        /// of Unicode code points.
        /// </summary>
        /// <param name="start">
        /// A position preceding the first code point to examine.
        /// </param>
        /// <param name="end">
        /// A position following the last code point to examine.
        /// </param>
        /// <returns>
        /// A collection of glyph property runs.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// If IsValid returns false, Validate method must be called before 
        /// calling this method.
        /// </exception>
        /// <remarks>
        /// A "glyph" in this context is the lowest level rendered representation
        /// of text.  Each entry in the output array describes a constant run of
        /// properties on the glyphs corresponding to a range of Unicode code points.
        /// With this array, it's possible to enumerate the glpyh properties for
        /// each code point in the specified text run.
        /// </remarks>
        internal override  ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end)
        {
            throw new NotImplementedException();
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
            DocumentsTrace.FixedDocumentSequence.TextOM.Trace(string.Format("Contains {0} {1}", position, position.LogicalDirection));
            DocumentSequenceTextPointer tp = null;
            if (position != null)
            {
                tp = _docPage.FixedDocumentSequence.TextContainer.VerifyPosition(position);
            }

            // Note we do not handle cross page navigation
            if (tp != null)
            {
                if (ChildTextView != null)
                {
                    if (ChildTextView.TextContainer == tp.ChildBlock.ChildContainer)
                    {
                        return ChildTextView.Contains(tp.ChildPointer.CreatePointer(position.LogicalDirection));
                    }
                }
            }
            return false;
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
            if (ChildTextView != null)
            {
                ChildTextView.Validate();
            }

            return ((ITextView)this).IsValid;
        }

        /// <see cref="ITextView.Validate(Point)"/>
        internal override bool Validate(Point point)
        {
            if (ChildTextView != null)
            {
                ChildTextView.Validate(point);
            }

            return ((ITextView)this).IsValid;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

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
        internal override ITextContainer TextContainer
        {
            get
            {
                return this._docPage.FixedDocumentSequence.TextContainer;
            }
        }

        /// <summary>
        /// Determines whenever layout is in clean state and it is possible
        /// to retrieve layout related data.
        /// </summary>
        internal override bool IsValid
        {
            get 
            {
                if (ChildTextView != null)
                {
                    return ChildTextView.IsValid;
                }
                return true; 
            }
        }

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
                    ReadOnlyCollection<TextSegment> childSegments = ChildTextView.TextSegments;
                    if (childSegments != null)
                    {
                        List<TextSegment> parentSegments = new List<TextSegment>(childSegments.Count);
                        foreach (TextSegment segment in childSegments)
                        {
                            DocumentSequenceTextPointer ptpStart, ptpEnd;
                            ptpStart = this._docPage.FixedDocumentSequence.TextContainer.MapChildPositionToParent(segment.Start);
                            ptpEnd = this._docPage.FixedDocumentSequence.TextContainer.MapChildPositionToParent(segment.End);
                            parentSegments.Add(new TextSegment(ptpStart, ptpEnd,true));
                        }
                        _textSegments = new ReadOnlyCollection<TextSegment>(parentSegments);
                    }
                }
                return _textSegments;
            }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties
        private ITextView ChildTextView
        {
            get
            {
                if (_childTextView == null)
                {
                    IServiceProvider isp = _docPage.ChildDocumentPage as IServiceProvider;
                    if (isp != null)
                    {
                        _childTextView = (ITextView)isp.GetService(typeof(ITextView));
                    }
                }
                return _childTextView;
            }
        }


        private ChildDocumentBlock ChildBlock
        {
            get
            {
                if (_childBlock == null)
                {
                    _childBlock = _docPage.FixedDocumentSequence.TextContainer.FindChildBlock(_docPage.ChildDocumentReference);
                }
                return _childBlock;
            }
        }

        private DocumentSequenceTextContainer DocumentSequenceTextContainer
        {
            get
            {
                return _docPage.FixedDocumentSequence.TextContainer;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // The FixedDocumentSequenceDocumentPage for this TextView
        private readonly FixedDocumentSequenceDocumentPage _docPage;
        private ITextView _childTextView;
        private ReadOnlyCollection<TextSegment> _textSegments;
        private ChildDocumentBlock _childBlock;
    }
}


