// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextLine wrapper used by TextBoxView.
//

using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal;
using MS.Internal.PtsHost;
using MS.Internal.Text;

namespace System.Windows.Controls
{
    /// <summary>
    /// TextLine wrapper used by TextBoxView.
    /// </summary>
    internal class TextBoxLine : TextSource, IDisposable
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
        /// <param name="owner">Owner of the line.</param>
        internal TextBoxLine(TextBoxView owner)
        {
            _owner = owner;
            PixelsPerDip = _owner.GetDpi().PixelsPerDip;
        }

        #endregion Constructors

        // ------------------------------------------------------------------
        //
        //  Public Methods
        //
        // ------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Free all resources associated with the line. Prepare it for reuse.
        /// </summary>
        public void Dispose()
        {
            // Dispose text line
            if (_line != null)
            {
                _line.Dispose();
                _line = null;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get a text run at specified text source position.
        /// </summary>
        public override TextRun GetTextRun(int dcp)
        {
            TextRun run = null;
            StaticTextPointer position = _owner.Host.TextContainer.CreateStaticPointerAtOffset(dcp);

            switch (position.GetPointerContext(LogicalDirection.Forward))
            {
                case TextPointerContext.Text:
                    run = HandleText(position);
                    break;

                case TextPointerContext.None:
                    run = new TextEndOfParagraph(_syntheticCharacterLength);
                    break;

                case TextPointerContext.ElementStart:
                case TextPointerContext.ElementEnd:
                case TextPointerContext.EmbeddedElement:
                default:
                    Invariant.Assert(false, "Unsupported position type.");
                    break;
            }
            Invariant.Assert(run != null, "TextRun has not been created.");
            Invariant.Assert(run.Length > 0, "TextRun has to have positive length.");
            if (run.Properties != null)
            {
                run.Properties.PixelsPerDip = this.PixelsPerDip;
            }

            return run;
        }

        /// <summary>
        /// Get text immediately before specified text source position.
        /// </summary>
        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int dcp)
        {
            CharacterBufferRange precedingText = CharacterBufferRange.Empty;
            CultureInfo culture = null;

            if (dcp > 0)
            {
                // Create TextPointer at dcp 
                ITextPointer position = _owner.Host.TextContainer.CreatePointerAtOffset(dcp, LogicalDirection.Backward);

                // Return text in run. If it is at start of TextContainer this will return an empty string.
                // Typically the caller requires just the preceding character.  Worst case is the entire
                // preceding sentence, which we approximate with a 128 char limit.
                int runLength = Math.Min(128, position.GetTextRunLength(LogicalDirection.Backward));
                char[] text = new char[runLength];
                position.GetTextInRun(LogicalDirection.Backward, text, 0, runLength);

                precedingText = new CharacterBufferRange(text, 0, runLength);
                culture = DynamicPropertyReader.GetCultureInfo((Control)_owner.Host);
            }

            return new TextSpan<CultureSpecificCharacterBufferRange>(
                precedingText.Length, new CultureSpecificCharacterBufferRange(culture, precedingText));
        }

        /// <summary>
        /// TextFormatter to map a text source character index to a text effect character index        
        /// </summary>
        /// <param name="textSourceCharacterIndex"> text source character index </param>
        /// <returns> the text effect index corresponding to the text effect character index </returns>
        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            return textSourceCharacterIndex;
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Create and format text line.
        /// </summary>
        /// <param name="dcp">First character position for the line.</param>
        /// <param name="formatWidth">Width to pass to LS formatter.</param>
        /// <param name="paragraphWidth">Line wrapping width.</param>
        /// <param name="lineProperties">Line's properties.</param>
        /// <param name="textRunCache">Run cache.</param>
        /// <param name="formatter">Text formatter.</param>
        /// <remarks>
        /// formatWidth/paragraphWidth is an attempt to work around bug 114719.
        /// Unfortunately, Line Services cannot guarantee that once a line
        /// has been measured, measuring the same content with the actual line
        /// width will produce the same line.
        /// 
        /// For example, suppose we format dcp 0 with paragraphWidth = 100.
        /// Suppose this results in a line from dcp 0 - 10, with width = 95.
        ///
        /// We would expect that a call to FormatLine with dcp = 0,
        /// paragraphWidth = 95 would result in the same 10 char line.
        /// But in practice it might return a 9 char line.
        /// 
        /// The workaround is to pass in an explicit formatting width across
        /// multiple calls, even if the paragraphWidth changes.
        /// </remarks>
        internal void Format(int dcp, double formatWidth, double paragraphWidth, LineProperties lineProperties, TextRunCache textRunCache, TextFormatter formatter)
        {
            _lineProperties = lineProperties;
            _dcp = dcp;
            _paragraphWidth = paragraphWidth;

            // We must ignore TextAlignment here since formatWidth does not
            // necessarilly equal paragraphWidth.  We'll adjust on later calls.
            lineProperties.IgnoreTextAlignment = (lineProperties.TextAlignment != TextAlignment.Justify);
            try
            {
                _line = formatter.FormatLine(this, dcp, formatWidth, lineProperties, null, textRunCache);
            }
            finally
            {
                lineProperties.IgnoreTextAlignment = false;
            }
        }

        /// <summary>
        /// Create and return visual node for the line. 
        /// </summary>
        internal TextBoxLineDrawingVisual CreateVisual(Geometry selectionGeometry)
        {
            TextBoxLineDrawingVisual visual = new TextBoxLineDrawingVisual();

            // Calculate shift in line offset to render trailing spaces or avoid clipping text.
            double delta = CalculateXOffsetShift();
            DrawingContext ctx = visual.RenderOpen();

            if (selectionGeometry != null)
            {
                var uiScope = _owner?.Host?.TextContainer?.TextSelection?.TextEditor?.UiScope;

                if (uiScope != null)
                {
                    Brush selectionBrush = uiScope.GetValue(TextBoxBase.SelectionBrushProperty) as Brush;

                    if (selectionBrush != null)
                    {
                        double selectionBrushOpacity = (double)uiScope.GetValue(TextBoxBase.SelectionOpacityProperty);

                        ctx.PushOpacity(selectionBrushOpacity);

                        // We use a Pen created from the brush used for the selection with a default thickness.
                        // This fixes issues where the geometries for the independent selection do not overlap
                        // and gaps can be seen to the background of the control between selection geometries.
                        ctx.DrawGeometry(selectionBrush, new Pen() { Brush = selectionBrush }, selectionGeometry);

                        ctx.Pop();
                    }
                }
            }

            _line.Draw(ctx, new Point(delta, 0), ((_lineProperties.FlowDirection == FlowDirection.RightToLeft) ? InvertAxes.Horizontal : InvertAxes.None));
            ctx.Close();

            return visual;
        }

        /// <summary>
        /// Retrieve bounds of an object/character at specified text position.
        /// </summary>
        /// <param name="characterIndex">position of an object/character</param>
        /// <param name="flowDirection">flow direction of object/character</param>
        /// <returns>Bounds of an object/character</returns>
        internal Rect GetBoundsFromTextPosition(int characterIndex, out FlowDirection flowDirection)
        {
            return GetBoundsFromPosition(characterIndex, 1, out flowDirection);
        }

        /// <summary>
        /// Returns a collection of rectangles (Rect) that form the bounds of the region 
        /// specified between the start and end points.
        /// </summary>
        /// <param name="cp">Starting point of the region</param>
        /// <param name="cch">Length in characters of the region</param>
        /// <param name="xOffset">Offset of line in x direction, to be added to line bounds</param>
        /// <param name="yOffset">Offset of line in y direction, to be added to line bounds</param>
        /// <remarks>
        /// This function calls GetTextBounds for the line, and then checks if there are 
        /// text run bounds. If they exist, it uses those as the bounding rectangles. If not, 
        /// it returns the rectangle for the first (and only) element of the text bounds.
        /// </remarks>
        internal List<Rect> GetRangeBounds(int cp, int cch, double xOffset, double yOffset)
        {
            List<Rect> rectangles = new List<Rect>();

            // Adjust x offset for trailing spaces
            double delta = CalculateXOffsetShift();
            double adjustedXOffset = xOffset + delta;

            IList<TextBounds> textBounds = _line.GetTextBounds(cp, cch);
            Invariant.Assert(textBounds.Count > 0);

            for (int boundIndex = 0; boundIndex < textBounds.Count; boundIndex++)
            {
                Rect rect = textBounds[boundIndex].Rectangle;
                rect.X += adjustedXOffset;
                rect.Y += yOffset;
                rectangles.Add(rect);
            }
            return rectangles;
        }

        /// <summary>
        /// Retrieve text position index from the distance.
        /// </summary>
        /// <param name="distance">distance relative to the beginning of the line</param>
        /// <returns>Text position index</returns>
        internal CharacterHit GetTextPositionFromDistance(double distance)
        {
            // Adjust distance to account for a line shift due to rendering of trailing spaces
            double delta = CalculateXOffsetShift();
            return _line.GetCharacterHitFromDistance(distance - delta);
        }

        /// <summary>
        /// Retrieve text position for next caret position.
        /// </summary>
        /// <param name="index">CharacterHit for current position</param>
        /// <returns>Text position index</returns>
        internal CharacterHit GetNextCaretCharacterHit(CharacterHit index)
        {
            return _line.GetNextCaretCharacterHit(index);
        }

        /// <summary>
        /// Retrieve text position for previous caret position.
        /// </summary>
        /// <param name="index">CharacterHit for current position</param>
        /// <returns>Text position index</returns>
        internal CharacterHit GetPreviousCaretCharacterHit(CharacterHit index)
        {
            return _line.GetPreviousCaretCharacterHit(index);
        }

        /// <summary>
        /// Retrieve text position for backspace caret position.
        /// </summary>
        /// <param name="index">CharacterHit for current position</param>
        /// <returns>Text position index</returns>
        internal CharacterHit GetBackspaceCaretCharacterHit(CharacterHit index)
        {
            return _line.GetBackspaceCaretCharacterHit(index);
        }

        /// <summary>
        /// Returns true of char hit is at caret unit boundary.
        /// </summary>
        /// <param name="charHit">
        /// CharacterHit to be tested.
        /// </param>
        internal bool IsAtCaretCharacterHit(CharacterHit charHit)
        {
            return _line.IsAtCaretCharacterHit(charHit, _dcp);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Calculated width of the line.
        /// </summary>
        internal double Width
        {
            get
            {
                if (IsWidthAdjusted)
                {
                    // Trailing spaces add to width
                    return _line.WidthIncludingTrailingWhitespace;
                }
                else
                {
                    return _line.Width;
                }
            }
        }

        /// <summary>
        /// Height of the line; line advance distance.
        /// </summary>
        internal double Height { get { return _line.Height; } }

        /// <summary>
        /// Is this the last line of the paragraph?
        /// </summary>
        internal bool EndOfParagraph
        {
            get
            {
                // If there are no Newline characters, it is not the end of paragraph.
                if (_line.NewlineLength == 0) { return false; }
                // Since there are Newline characters in the line, do more expensive and
                // accurate check.
                IList<TextSpan<TextRun>> runs = _line.GetTextRunSpans();
                return (((TextSpan<TextRun>)runs[runs.Count - 1]).Value is TextEndOfParagraph);
            }
        }

        /// <summary>
        /// Length of the line excluding any synthetic characters.
        /// </summary>
        internal int Length
        {
            get { return _line.Length - (EndOfParagraph ? 1 : 0); }
        }

        /// <summary>
        /// Length of the line excluding any synthetic characters and line breaks.
        /// </summary>
        internal int ContentLength { get { return _line.Length - _line.NewlineLength; } }

        /// <summary>
        /// True if line ends in hard line break.
        /// </summary>
        internal bool HasLineBreak
        {
            get
            {
                return (_line.NewlineLength > 0);
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
        /// Fetch the next run at text position.
        /// </summary>
        private TextRun HandleText(StaticTextPointer position)
        {
            // Calculate the end of the run by finding either:
            //      a) the next intersection of highlight ranges, or
            //      b) the natural end of this textrun
            StaticTextPointer endOfRunPosition = _owner.Host.TextContainer.Highlights.GetNextPropertyChangePosition(position, LogicalDirection.Forward);

            // Clamp the text run at an arbitrary limit, so we don't make
            // an unbounded allocation.
            if (position.GetOffsetToPosition(endOfRunPosition) > 4096)
            {
                endOfRunPosition = position.CreatePointer(4096);
            }

            var highlights = position.TextContainer.Highlights;

            // Factor in any speller error squiggles on the run.
            TextDecorationCollection highlightDecorations = highlights.GetHighlightValue(position, LogicalDirection.Forward, typeof(SpellerHighlightLayer)) as TextDecorationCollection;

            TextRunProperties properties = _lineProperties.DefaultTextRunProperties;

            if (highlightDecorations != null)
            {
                if (_spellerErrorProperties == null)
                {
                    _spellerErrorProperties = new TextProperties((TextProperties)properties, highlightDecorations);
                }
                properties = _spellerErrorProperties;
            }

            var textEditor = position.TextContainer.TextSelection?.TextEditor;

            
            // Apply selection highlighting if needed
            if ((textEditor?.TextView?.RendersOwnSelection == true)
                && highlights.GetHighlightValue(position, LogicalDirection.Forward, typeof(TextSelection)) != DependencyProperty.UnsetValue)
            {
                // We need to create a new TextProperties instance here since we are going to change the Foreground and Background.
                var selectionProps = new TextProperties((TextProperties)properties, highlightDecorations);

                // The UiScope that owns this line should be the source for text/highlight properties
                var uiScope = textEditor?.UiScope;

                if (uiScope != null)
                {
                    // Background should not be drawn since the selection is drawn below us
                    selectionProps.SetBackgroundBrush(null);

                    Brush selectionTextBrush = uiScope.GetValue(TextBoxBase.SelectionTextBrushProperty) as Brush;

                    if (selectionTextBrush != null)
                    {
                        selectionProps.SetForegroundBrush(selectionTextBrush);
                    }
                }

                properties = selectionProps;
            }

            // Get character buffer for the text run.
            char[] textBuffer = new char[position.GetOffsetToPosition(endOfRunPosition)];

            // Copy characters from text run into buffer. Since we are dealing with plain text content, 
            // we expect to get all the characters from position to endOfRunPosition.
            int charactersCopied = position.GetTextInRun(LogicalDirection.Forward, textBuffer, 0, textBuffer.Length);
            Invariant.Assert(charactersCopied == textBuffer.Length);

            // Create text run, using characters copied as length
            return new TextCharacters(textBuffer, 0, charactersCopied, properties);
        }

        /// <summary>
        /// Retrieve bounds of an object/character at specified text index.
        /// </summary>
        /// <param name="cp">character index of an object/character</param>
        /// <param name="cch">number of positions occupied by object/character</param>
        /// <param name="flowDirection">flow direction of object/character</param>
        /// <returns>Bounds of an object/character</returns>
        private Rect GetBoundsFromPosition(int cp, int cch, out FlowDirection flowDirection)
        {
            Rect rect;

            // Adjust x offset for trailing spaces
            double delta = CalculateXOffsetShift();
            IList<TextBounds> textBounds = _line.GetTextBounds(cp, cch);
            Invariant.Assert(textBounds != null && textBounds.Count == 1, "Expecting exactly one TextBounds for a single text position.");

            IList<TextRunBounds> runBounds = textBounds[0].TextRunBounds;
            if (runBounds != null)
            {
                Invariant.Assert(runBounds.Count == 1, "Expecting exactly one TextRunBounds for a single text position.");
                rect = runBounds[0].Rectangle;
            }
            else
            {
                rect = textBounds[0].Rectangle;
            }

            rect.X += delta;
            flowDirection = textBounds[0].FlowDirection;
            return rect;
        }

        /// <summary>
        /// Returns amount of shift for X-offset to render trailing spaces
        /// and TextAlignment offset.
        /// </summary>
        private double CalculateXOffsetShift()
        {
            double xOffset = 0;

            if (_lineProperties.TextAlignmentInternal == TextAlignment.Right)
            {
                xOffset = _paragraphWidth - _line.Width;
            }
            else if (_lineProperties.TextAlignmentInternal == TextAlignment.Center)
            {
                xOffset = (_paragraphWidth - _line.Width) / 2;
            }

            if (IsXOffsetAdjusted)
            {
                if (_lineProperties.TextAlignmentInternal == TextAlignment.Center)
                {
                    // Return trailing spaces length divided by two so line remains centered.
                    xOffset += (_line.Width - _line.WidthIncludingTrailingWhitespace) / 2;
                }
                else
                {
                    xOffset += (_line.Width - _line.WidthIncludingTrailingWhitespace);
                }
            }

            return xOffset;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Properites
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// True if line's X-offset needs adjustment to render trailing spaces.
        /// </summary>
        private bool IsXOffsetAdjusted
        {
            get
            {
                return ((_lineProperties.TextAlignmentInternal == TextAlignment.Right || _lineProperties.TextAlignmentInternal == TextAlignment.Center) && IsWidthAdjusted);
            }
        }

        /// <summary>
        /// True if line's width is adjusted to include trailing spaces. For right and center alignment we need to
        /// adjust line offset as well, but for left alignment we need to only make a width asjustment.
        /// </summary>
        private bool IsWidthAdjusted
        {
            get
            {
                // Trailing spaces rendered only around hard breaks
                return (HasLineBreak || EndOfParagraph);
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Owner of the line.
        /// </summary>
        private readonly TextBoxView _owner;

        /// <summary>
        /// Cached text line.
        /// </summary>
        private TextLine _line;

        /// <summary>
        /// Index of the first character in the line.
        /// </summary>
        private int _dcp;

        /// <summary>
        /// Properties of the line.
        /// </summary>
        private LineProperties _lineProperties;

        /// <summary>
        /// Properties of the line when covered by a spelling error squiggle.
        /// </summary>
        private TextProperties _spellerErrorProperties;

        /// <summary>
        /// Width of the enclosing paragraph, used for TextAlignment calculations.
        /// </summary>
        private double _paragraphWidth;

        /// <summary>
        /// TextEndOfParagraph character count.
        /// </summary>
        private const int _syntheticCharacterLength = 1;

        #endregion Private Fields
    }
}
