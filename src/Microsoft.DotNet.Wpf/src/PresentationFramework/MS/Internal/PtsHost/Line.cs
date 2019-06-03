// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text line formatter.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown
                                    // message numbers and unknown pragmas for PRESharp contol

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security;                  // SecurityCritical
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal.Text;
using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Text line formatter.
    /// </summary>
    /// <remarks>
    /// NOTE: All DCPs used during line formatting are related to cpPara.
    /// To get abosolute CP, add cpPara to a dcp value.
    /// </remarks>
    internal sealed class Line : LineBase
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
        /// <param name="host">
        /// TextFormatter host
        /// </param>
        /// <param name="paraClient">
        /// Owner of the line
        /// </param>
        /// <param name="cpPara">
        /// CP of the beginning of the text paragraph
        /// </param>
        internal Line(TextFormatterHost host, TextParaClient paraClient, int cpPara) : base(paraClient)
        {
            _host = host;
            _cpPara = cpPara;
            _textAlignment = (TextAlignment)TextParagraph.Element.GetValue(Block.TextAlignmentProperty);
            _indent = 0.0;
        }

        /// <summary>
        /// Free all resources associated with the line. Prepare it for reuse.
        /// </summary>
        public override void Dispose()
        {
            Debug.Assert(_line != null, "Line has been already disposed.");
            try
            {
                if (_line != null)
                {
                    _line.Dispose();
                }
            }
            finally
            {
                _line = null;
                _runs = null;
                _hasFigures = false;
                _hasFloaters = false;
                base.Dispose();
            }
        }

        #endregion Constructors

        // ------------------------------------------------------------------
        //
        //  PTS Callbacks
        //
        // ------------------------------------------------------------------

        #region PTS Callbacks

        /// <summary>
        /// GetDvrSuppressibleBottomSpace
        /// </summary>
        /// <param name="dvrSuppressible">
        /// OUT: empty space suppressible at the bottom
        /// </param>
        internal void GetDvrSuppressibleBottomSpace(
            out int dvrSuppressible)            
        {
            dvrSuppressible = Math.Max(0, TextDpi.ToTextDpi(_line.OverhangAfter));
        }

        /// <summary>
        /// GetDurFigureAnchor
        /// </summary>
        /// <param name="paraFigure">
        /// IN: FigureParagraph for which we require anchor dur
        /// </param>
        /// <param name="fswdir">
        /// IN: current direction
        /// </param>
        /// <param name="dur">
        /// OUT: distance from the beginning of the line to the anchor
        /// </param>
        internal void GetDurFigureAnchor(
            FigureParagraph paraFigure,         
            uint fswdir,                        
            out int dur)                        
        {
            int cpFigure = TextContainerHelper.GetCPFromElement(_paraClient.Paragraph.StructuralCache.TextContainer, paraFigure.Element, ElementEdge.BeforeStart);
            int dcpFigure = cpFigure - _cpPara;
            double distance = _line.GetDistanceFromCharacterHit(new CharacterHit(dcpFigure, 0));
            dur = TextDpi.ToTextDpi(distance);
        }

        #endregion PTS Callbacks

        // ------------------------------------------------------------------
        //
        //  TextSource Implementation
        //
        // ------------------------------------------------------------------

        #region TextSource Implementation

        /// <summary>
        /// Get a text run at specified text source position and return it.
        /// </summary>
        /// <param name="dcp">
        /// dcp of position relative to start of line
        /// </param>
        internal override TextRun GetTextRun(int dcp)
        {
            TextRun run = null;
            ITextContainer textContainer = _paraClient.Paragraph.StructuralCache.TextContainer;
            StaticTextPointer position = textContainer.CreateStaticPointerAtOffset(_cpPara + dcp);

            switch (position.GetPointerContext(LogicalDirection.Forward))
            {
                case TextPointerContext.Text:
                    run = HandleText(position);
                    break;

                case TextPointerContext.ElementStart:
                    run = HandleElementStartEdge(position);
                    break;

                case TextPointerContext.ElementEnd:
                    run = HandleElementEndEdge(position);
                    break;

                case TextPointerContext.EmbeddedElement:
                    run = HandleEmbeddedObject(dcp, position);
                    break;

                case TextPointerContext.None:
                    run = new ParagraphBreakRun(_syntheticCharacterLength, PTS.FSFLRES.fsflrEndOfParagraph);
                    break;
            }
            Invariant.Assert(run != null, "TextRun has not been created.");
            Invariant.Assert(run.Length > 0, "TextRun has to have positive length.");
            return run;
        }

        /// <summary>
        /// Get text immediately before specified text source position. Return CharacterBufferRange
        /// containing this text.
        /// </summary>
        /// <param name="dcp">
        /// dcp of position relative to start of line
        /// </param>
        internal override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int dcp)
        {
            // Parameter validation
            Invariant.Assert(dcp >= 0);

            int nonTextLength = 0;
            CharacterBufferRange precedingText = CharacterBufferRange.Empty;
            CultureInfo culture = null;
            
            if (dcp > 0)
            {
                // Create TextPointer at dcp, and pointer at paragraph start to compare
                ITextPointer startPosition = TextContainerHelper.GetTextPointerFromCP(_paraClient.Paragraph.StructuralCache.TextContainer, _cpPara, LogicalDirection.Forward);
                ITextPointer position = TextContainerHelper.GetTextPointerFromCP(_paraClient.Paragraph.StructuralCache.TextContainer, _cpPara + dcp, LogicalDirection.Forward);

                // Move backward until we find a position at the end of a text run, or reach start of TextContainer
                while (position.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.Text &&
                       position.CompareTo(startPosition) != 0)
                {
                    position.MoveByOffset(-1);
                    nonTextLength++;
                }


                // Return text in run. If it is at start of TextContainer this will return an empty string
                string precedingTextString = position.GetTextInRun(LogicalDirection.Backward);
                precedingText = new CharacterBufferRange(precedingTextString, 0, precedingTextString.Length);                

                StaticTextPointer pointer = position.CreateStaticPointer();
                DependencyObject element = (pointer.Parent != null) ? pointer.Parent : _paraClient.Paragraph.Element;
                culture = DynamicPropertyReader.GetCultureInfo(element);
            }

            return new TextSpan<CultureSpecificCharacterBufferRange>(
                nonTextLength + precedingText.Length, 
                new CultureSpecificCharacterBufferRange(culture, precedingText)
                );
        }

        /// <summary>
        /// Get Text effect index from text source character index. Return int value of Text effect index.
        /// </summary>
        /// <param name="dcp">
        /// dcp of CharacterHit relative to start of line
        /// </param>
        internal override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int dcp)
        {
            return _cpPara + dcp;
        }

        #endregion TextSource Implementation

        // ------------------------------------------------------------------
        //
        //  Internal Methods
        //
        // ------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Create and format text line. 
        /// </summary>
        /// <param name="ctx">
        /// Line formatting context.
        /// </param>
        /// <param name="dcp">
        /// Character position where the line starts.
        /// </param>
        /// <param name="width">
        /// Requested width of the line.
        /// </param>
        /// <param name="trackWidth">
        /// Requested width of track.
        /// </param>
        /// <param name="lineProps">
        /// Line properties.
        /// </param>
        /// <param name="textLineBreak">
        /// Line break object.
        /// </param>
        internal void Format(FormattingContext ctx, int dcp, int width, int trackWidth, TextParagraphProperties lineProps, TextLineBreak textLineBreak)
        {
            // Set formatting context
            _formattingContext = ctx;
            _dcp = dcp;
            _host.Context = this;
            _wrappingWidth = TextDpi.FromTextDpi(width);
            _trackWidth = TextDpi.FromTextDpi(trackWidth);
            _mirror = (lineProps.FlowDirection == FlowDirection.RightToLeft);
            _indent = lineProps.Indent;

            try
            {
                // Create line object
                if(ctx.LineFormatLengthTarget == -1)
                {
                    _line = _host.TextFormatter.FormatLine(_host, dcp, _wrappingWidth, lineProps, textLineBreak, ctx.TextRunCache);
                }
                else
                {
                    _line = _host.TextFormatter.RecreateLine(_host, dcp, ctx.LineFormatLengthTarget, _wrappingWidth, lineProps, textLineBreak, ctx.TextRunCache);
                }
                _runs = _line.GetTextRunSpans();
                Invariant.Assert(_runs != null, "Cannot retrieve runs collection.");

                // Submit inline objects (only in measure mode)
                if (_formattingContext.MeasureMode)
                {
                    List<InlineObject> inlineObjects = new List<InlineObject>(1);
                    int dcpRun = _dcp;
                    // Enumerate through all runs in the current line and retrieve 
                    // all inline objects.
                    // If there are any figures / floaters, store this information for later use.
                    foreach (TextSpan<TextRun> textSpan in _runs)
                    {
                        TextRun run = (TextRun)textSpan.Value;
                        if (run is InlineObjectRun)
                        {
                            inlineObjects.Add(new InlineObject(dcpRun, ((InlineObjectRun)run).UIElementIsland, (TextParagraph)_paraClient.Paragraph));
                        }
                        else if (run is FloatingRun)
                        {
                            if (((FloatingRun)run).Figure)
                            {
                                _hasFigures = true;
                            }
                            else
                            {
                                _hasFloaters = true;
                            }
                        }

                        // Do not use TextRun.Length, because it gives total length of the run.
                        // So, if the run is broken between lines, it gives incorrect value.
                        // Use length of the TextSpan instead, which gives the correct length here.
                        dcpRun += textSpan.Length;
                    }

                    // Submit inline objects to the paragraph cache
                    if (inlineObjects.Count == 0)
                    {
                        inlineObjects = null;
                    }
                    TextParagraph.SubmitInlineObjects(dcp, dcp + ActualLength, inlineObjects);
                }
            }
            finally
            {
                // Clear formatting context
                _host.Context = null;
            }
        }

        /// <summary>
        /// Measure child UIElement. 
        /// </summary>
        /// <param name="inlineObject">
        /// Element whose size we are measuring
        /// </param>
        /// <returns>
        /// Size of the child UIElement
        /// </returns>
        internal Size MeasureChild(InlineObjectRun inlineObject)
        {
            // Measure inline object only during measure pass. Otherwise
            // use cached data.
            Size desiredSize;
            if (_formattingContext.MeasureMode)
            {
                Debug.Assert(!DoubleUtil.IsNaN(_trackWidth), "Track width must be set for measure pass.");

                // Always measure at infinity for bottomless, consistent constraint.
                double pageHeight = _paraClient.Paragraph.StructuralCache.CurrentFormatContext.DocumentPageSize.Height;
                if (!_paraClient.Paragraph.StructuralCache.CurrentFormatContext.FinitePage)
                {
                    pageHeight = Double.PositiveInfinity;
                }

                desiredSize = inlineObject.UIElementIsland.DoLayout(new Size(_trackWidth, pageHeight), true, true);
            }
            else
            {
                desiredSize = inlineObject.UIElementIsland.Root.DesiredSize;
            }
            return desiredSize;
        }

        /// <summary>
        /// Create and return visual node for the line. 
        /// </summary>
        internal ContainerVisual CreateVisual()
        {
            LineVisual visual = new LineVisual();

            // Set up the text source for rendering callback
            _host.Context = this;

            try
            {
                // Handle text trimming.
                IList<TextSpan<TextRun>> runs = _runs;
                System.Windows.Media.TextFormatting.TextLine line = _line;
                if (_line.HasOverflowed && TextParagraph.Properties.TextTrimming != TextTrimming.None)
                {
                    line = _line.Collapse(GetCollapsingProps(_wrappingWidth, TextParagraph.Properties));
                    Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                    runs = line.GetTextRunSpans();
                }

                // Add visuals for all embedded elements.
                if (HasInlineObjects())
                {
                    VisualCollection visualChildren = visual.Children;

                    // Get flow direction of the paragraph element.
                    DependencyObject paragraphElement = _paraClient.Paragraph.Element;
                    FlowDirection paragraphFlowDirection = (FlowDirection)paragraphElement.GetValue(FrameworkElement.FlowDirectionProperty);

                    // Before text rendering, add all visuals for inline objects.
                    int dcpRun = _dcp;
                    // Enumerate through all runs in the current line and connect visuals for all inline objects. 
                    foreach (TextSpan<TextRun> textSpan in runs)
                    {
                        TextRun run = (TextRun)textSpan.Value;
                        if (run is InlineObjectRun)
                        {
                            InlineObjectRun inlineObject = (InlineObjectRun)run;
                            FlowDirection flowDirection;
                            Rect rect = GetBoundsFromPosition(dcpRun, run.Length, out flowDirection);
                            Debug.Assert(DoubleUtil.GreaterThanOrClose(rect.Width, 0), "Negative inline object's width.");

                            // Disconnect visual from its old parent, if necessary.
                            Visual currentParent = VisualTreeHelper.GetParent(inlineObject.UIElementIsland) as Visual;
                            if (currentParent != null)
                            {
                                ContainerVisual parent = currentParent as ContainerVisual;
                                Invariant.Assert(parent != null, "Parent should always derives from ContainerVisual.");
                                parent.Children.Remove(inlineObject.UIElementIsland);
                            }                                                        

                            if (!line.HasCollapsed || ((rect.Left + inlineObject.UIElementIsland.Root.DesiredSize.Width) < line.Width))
                            {
                                // Check parent's FlowDirection to determine if mirroring is needed
                                if (inlineObject.UIElementIsland.Root is FrameworkElement)
                                {
                                    DependencyObject parent = ((FrameworkElement)inlineObject.UIElementIsland.Root).Parent;
                                    FlowDirection parentFlowDirection = (FlowDirection)parent.GetValue(FrameworkElement.FlowDirectionProperty);
                                    PtsHelper.UpdateMirroringTransform(paragraphFlowDirection, parentFlowDirection, inlineObject.UIElementIsland, rect.Width);
                                }

                                visualChildren.Add(inlineObject.UIElementIsland);
                                inlineObject.UIElementIsland.Offset = new Vector(rect.Left, rect.Top);
                            }
                        }

                        // Do not use TextRun.Length, because it gives total length of the run.
                        // So, if the run is broken between lines, it gives incorrect value.
                        // Use length of the TextSpan instead, which gives the correct length here.
                        dcpRun += textSpan.Length;
                    }
                }

                // Calculate shift in line offset to render trailing spaces or avoid clipping text
                double delta = TextDpi.FromTextDpi(CalculateUOffsetShift());
                DrawingContext ctx = visual.Open();
                line.Draw(ctx, new Point(delta, 0), (_mirror ? InvertAxes.Horizontal : InvertAxes.None));
                ctx.Close();
                
                visual.WidthIncludingTrailingWhitespace = line.WidthIncludingTrailingWhitespace - _indent;
            }
            finally
            {
                _host.Context = null; // clear the context
            }
            
            return visual;
        }

        /// <summary>
        /// Return bounds of an object/character at specified text position. 
        /// </summary>
        /// <param name="textPosition">
        /// Position of the object/character
        /// </param>
        /// <param name="flowDirection">
        /// Flow direction of the object/character
        /// </param>
        internal Rect GetBoundsFromTextPosition(int textPosition, out FlowDirection flowDirection)
        {
            return GetBoundsFromPosition(textPosition, 1, out flowDirection);            
        }

        /// <summary>
        /// Returns an ArrayList of rectangles (Rect) that form the bounds of the region specified between
        /// the start and end points
        /// </summary>
        /// <param name="cp"></param>
        /// int offset indicating the starting point of the region for which bounds are required
        /// <param name="cch">
        /// Length in characters of the region for which bounds are required
        /// </param>
        /// <param name="xOffset">
        /// Offset of line in x direction, to be added to line bounds to get actual rectangle for line
        /// </param>
        /// <param name="yOffset">
        /// Offset of line in y direction, to be added to line bounds to get actual rectangle for line
        /// </param>
        /// <remarks>
        /// This function calls GetTextBounds for the line, and then checks if there are text run bounds. If they exist,
        /// it uses those as the bounding rectangles. If not, it returns the rectangle for the first (and only) element
        /// of the text bounds.
        /// </remarks>
        internal List<Rect> GetRangeBounds(int cp, int cch, double xOffset, double yOffset)
        {
            List<Rect> rectangles = new List<Rect>();
    
            // Calculate shift in line offset to render trailing spaces or avoid clipping text
            double delta = TextDpi.FromTextDpi(CalculateUOffsetShift());
            double newUOffset = xOffset + delta;
            IList<TextBounds> textBounds;
            if (_line.HasOverflowed && TextParagraph.Properties.TextTrimming != TextTrimming.None)
            {
                // Verify that offset shift is 0 for this case. We should never shift offsets when ellipses are
                // rendered.
                Invariant.Assert(DoubleUtil.AreClose(delta, 0));
                System.Windows.Media.TextFormatting.TextLine line = _line.Collapse(GetCollapsingProps(_wrappingWidth, TextParagraph.Properties));
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                textBounds = line.GetTextBounds(cp, cch);
            }
            else
            {
                textBounds = _line.GetTextBounds(cp, cch);
            }
            Invariant.Assert(textBounds.Count > 0);

            for (int boundIndex = 0; boundIndex < textBounds.Count; boundIndex++)
            {
                Rect rect = textBounds[boundIndex].Rectangle;
                rect.X += newUOffset;
                rect.Y += yOffset;
                rectangles.Add(rect);
            }
            return rectangles;
        }

        /// <summary>
        /// Passes line break object out from underlying line object
        /// </summary>
        internal TextLineBreak GetTextLineBreak()
        {
            if(_line == null)
            {
                return null;
            }

            return _line.GetTextLineBreak();
        }


        /// <summary>
        /// Return text position index from the given distance.
        /// </summary>
        /// <param name="urDistance">
        /// Distance relative to the beginning of the line.
        /// </param>
        internal CharacterHit GetTextPositionFromDistance(int urDistance)
        {
            // Calculate shift in line offset to render trailing spaces or avoid clipping text
            int delta = CalculateUOffsetShift();
            if (_line.HasOverflowed && TextParagraph.Properties.TextTrimming != TextTrimming.None)
            {
                System.Windows.Media.TextFormatting.TextLine line = _line.Collapse(GetCollapsingProps(_wrappingWidth, TextParagraph.Properties));
                Invariant.Assert(delta == 0);
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                return line.GetCharacterHitFromDistance(TextDpi.FromTextDpi(urDistance));
            }
            return _line.GetCharacterHitFromDistance(TextDpi.FromTextDpi(urDistance - delta));
        }

        /// <summary>
        /// Hit tests to the correct ContentElement within the line.
        /// </summary>
        /// <param name="urOffset">
        /// Offset within the line.
        /// </param>
        /// <returns>
        /// ContentElement which has been hit.
        /// </returns>
        internal IInputElement InputHitTest(int urOffset)
        {
            DependencyObject element = null;
            TextPointer position;
            TextPointerContext type = TextPointerContext.None;
            CharacterHit charIndex;
            int cp, delta;

            // Calculate shift in line offset to render trailing spaces or avoid clipping text
            delta = CalculateUOffsetShift();
            if (_line.HasOverflowed && TextParagraph.Properties.TextTrimming != TextTrimming.None)
            {
                // We should not shift offset in this case
                Invariant.Assert(delta == 0);
                System.Windows.Media.TextFormatting.TextLine line = _line.Collapse(GetCollapsingProps(_wrappingWidth, TextParagraph.Properties));
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");

                // Get TextPointer from specified distance.
                charIndex = line.GetCharacterHitFromDistance(TextDpi.FromTextDpi(urOffset));
            }
            else
            {
                // Get TextPointer from specified distance.
                charIndex = _line.GetCharacterHitFromDistance(TextDpi.FromTextDpi(urOffset - delta));
            }

            cp = _paraClient.Paragraph.ParagraphStartCharacterPosition + charIndex.FirstCharacterIndex + charIndex.TrailingLength;
            position = TextContainerHelper.GetTextPointerFromCP(_paraClient.Paragraph.StructuralCache.TextContainer, cp, LogicalDirection.Forward) as TextPointer;

            if (position != null)
            {
                // If start of character, look forward. Otherwise, look backward.
                type = position.GetPointerContext((charIndex.TrailingLength == 0) ? LogicalDirection.Forward : LogicalDirection.Backward);

                // Get element only for Text & Start/End element, for all other positions
                // return null (it means that the line owner has been hit).
                if (type == TextPointerContext.Text || type == TextPointerContext.ElementEnd)
                {
                    element = position.Parent;
                }
                else if (type == TextPointerContext.ElementStart)
                {
                    element = position.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward);
                }
            }
            return element as IInputElement;
        }

        /// <summary>
        /// Get length of content hidden by ellipses. Return integer length of this content. 
        /// </summary>
        internal int GetEllipsesLength()
        {
            // There are no ellipses, if:
            // * there is no overflow in the line
            // * text trimming is turned off
            if (!_line.HasOverflowed) 
            { 
                return 0; 
            }
            if (TextParagraph.Properties.TextTrimming == TextTrimming.None) 
            { 
                return 0; 
            }

            // Create collapsed text line to get length of collapsed content.
            System.Windows.Media.TextFormatting.TextLine collapsedLine = _line.Collapse(GetCollapsingProps(_wrappingWidth, TextParagraph.Properties));
            Invariant.Assert(collapsedLine.HasCollapsed, "Line has not been collapsed");
            IList<TextCollapsedRange> collapsedRanges = collapsedLine.GetTextCollapsedRanges();
            if (collapsedRanges != null)
            {
                Invariant.Assert(collapsedRanges.Count == 1, "Multiple collapsed ranges are not supported.");
                TextCollapsedRange collapsedRange = collapsedRanges[0];
                return collapsedRange.Length;
            }
            return 0;
        }

        /// <summary>
        /// Retrieves collection of GlyphRuns from a range of text.
        /// </summary>
        /// <param name="glyphRuns">
        /// Glyph runs.
        /// </param>
        /// <param name="dcpStart">
        /// Start dcp of range
        /// </param>
        /// <param name="dcpEnd">
        /// End dcp of range.
        /// </param>
        internal void GetGlyphRuns(System.Collections.Generic.List<GlyphRun> glyphRuns, int dcpStart, int dcpEnd)
        {
            // NOTE: Following logic is only temporary workaround for lack
            //       of appropriate API that should be exposed by TextLine.

            int dcp = dcpStart - _dcp;
            int cch = dcpEnd - dcpStart;
            Debug.Assert(dcp >= 0 && (dcp + cch <= _line.Length));

            IList<TextSpan<TextRun>> spans = _line.GetTextRunSpans();

            DrawingGroup drawing = new DrawingGroup();
            DrawingContext ctx = drawing.Open();

            // Calculate shift in line offset to render trailing spaces or avoid clipping text
            double delta = TextDpi.FromTextDpi(CalculateUOffsetShift());
 
            _line.Draw(ctx, new Point(delta, 0), InvertAxes.None);
            ctx.Close();

            // Copy glyph runs into separate array (for backward navigation).
            // And count number of chracters in the glyph runs collection.
            int cchGlyphRuns = 0;
            ArrayList glyphRunsCollection = new ArrayList(4);

            AddGlyphRunRecursive(drawing, glyphRunsCollection, ref cchGlyphRuns);

            Debug.Assert(cchGlyphRuns > 0 && glyphRunsCollection.Count > 0);
            // Count number of characters in text runs.
            int cchTextSpans = 0;
            foreach (TextSpan<TextRun> textSpan in spans)
            {
                if (textSpan.Value is TextCharacters)
                {
                    cchTextSpans += textSpan.Length;
                }
            }
            // If number of characters in glyph runs is greater than number of characters
            // in text runs, it means that there is bullet at the beginning of the line
            // or hyphen at the end of the line.
            // For now hyphen case is ignored.
            // Remove those glyph runs from our colleciton.
            while (cchGlyphRuns > cchTextSpans)
            {
                GlyphRun glyphRun = (GlyphRun)glyphRunsCollection[0];
                cchGlyphRuns -= (glyphRun.Characters == null ? 0 : glyphRun.Characters.Count);
                glyphRunsCollection.RemoveAt(0);
            }

            int curDcp = 0;
            int runIndex = 0;
            foreach (TextSpan<TextRun> span in spans)
            {
                if (span.Value is TextCharacters)
                {
                    int cchRunsInSpan = 0;
                    while (cchRunsInSpan < span.Length)
                    {
                        Invariant.Assert(runIndex < glyphRunsCollection.Count);
                        GlyphRun run = (GlyphRun)glyphRunsCollection[runIndex];
                        int characterCount = (run.Characters == null ? 0 : run.Characters.Count);
                        if ((dcp < curDcp + characterCount) && (dcp + cch > curDcp))
                        {
                            glyphRuns.Add(run);
                        }
                        cchRunsInSpan += characterCount;
                        ++runIndex;
                    }
                    Invariant.Assert(cchRunsInSpan == span.Length);

                    // No need to continue, if dcpEnd has been reached.
                    if (dcp + cch <= curDcp + span.Length)
                        break;
                }
                curDcp += span.Length;
            }
        }

        /// <summary>
        /// Return text position for next caret position 
        /// </summary>
        /// <param name="index">
        /// CharacterHit for current position
        /// </param>
        internal CharacterHit GetNextCaretCharacterHit(CharacterHit index)
        {
            return _line.GetNextCaretCharacterHit(index);
        }

        /// <summary>
        /// Return text position for previous caret position 
        /// </summary>
        /// <param name="index">
        /// CharacterHit for current position
        /// </param>
        internal CharacterHit GetPreviousCaretCharacterHit(CharacterHit index)
        {
            return _line.GetPreviousCaretCharacterHit(index);
        }

        /// <summary>
        /// Return text position for backspace caret position 
        /// </summary>
        /// <param name="index">
        /// CharacterHit for current position
        /// </param>
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
        /// Distance from the beginning of paragraph edge to the line edge. 
        /// </summary>
        internal int Start 
        { 
            get 
            { 
                return TextDpi.ToTextDpi(_line.Start) + TextDpi.ToTextDpi(_indent) + CalculateUOffsetShift(); 
            } 
        }

        /// <summary>
        /// Calculated width of the line.
        /// </summary>
        internal int Width 
        { 
            get 
            {
                int width;
                if (IsWidthAdjusted)
                {
                    width = TextDpi.ToTextDpi(_line.WidthIncludingTrailingWhitespace) - TextDpi.ToTextDpi(_indent);
                }
                else
                {
                    width = TextDpi.ToTextDpi(_line.Width) - TextDpi.ToTextDpi(_indent);
                }
                Invariant.Assert(width >= 0, "Line width cannot be negative");
                return width;
            } 
        }

        /// <summary>
        /// Height of the line; line advance distance. 
        /// </summary>
        internal int Height 
        { 
            get 
            { 
                return TextDpi.ToTextDpi(_line.Height); 
            } 
        }

        /// <summary>
        /// Baseline offset from the top of the line.
        /// </summary>
        internal int Baseline 
        { 
            get 
            { 
                return TextDpi.ToTextDpi(_line.Baseline); 
            } 
        }

        /// <summary>
        /// True if last line of paragraph
        /// </summary>
        internal bool EndOfParagraph
        {
            get
            {
                // If there are no Newline characters, it is not the end of paragraph.
                if (_line.NewlineLength == 0) 
                { 
                    return false; 
                }
                // Since there are Newline characters in the line, do more expensive and
                // accurate check.
                return (((TextSpan<TextRun>)_runs[_runs.Count-1]).Value is ParagraphBreakRun);
            }
        }

        /// <summary>
        /// Length of the line including any synthetic characters.
        /// This length is PTS frendly. PTS does not like 0 length lines.
        /// </summary>
        internal int SafeLength 
        { 
            get 
            { 
                return _line.Length; 
            } 
        }

        /// <summary>
        /// Length of the line excluding any synthetic characters. 
        /// </summary>
        internal int ActualLength 
        { 
            get 
            { 
                return _line.Length - (EndOfParagraph ? _syntheticCharacterLength : 0); 
            } 
        }

        /// <summary>
        /// Length of the line excluding any synthetic characters and line breaks. 
        /// </summary>
        internal int ContentLength 
        { 
            get 
            { 
                return _line.Length - _line.NewlineLength; 
            } 
        }

        /// <summary>
        /// Number of characters after the end of the line which may affect
        /// line wrapping. 
        /// </summary>
        internal int DependantLength 
        { 
            get 
            { 
                return _line.DependentLength; 
            } 
        }

        /// <summary>
        /// Was line truncated (forced broken)?
        /// </summary>
        internal bool IsTruncated
        { 
            get 
            { 
                return _line.IsTruncated; 
            } 
        }

        /// <summary>
        /// Formatting result of the line. 
        /// </summary>
        internal PTS.FSFLRES FormattingResult
        {
            get
            {
                PTS.FSFLRES formatResult = PTS.FSFLRES.fsflrOutOfSpace;
                // If there are no Newline characters, we run out of space.
                if (_line.NewlineLength == 0) 
                { 
                    return formatResult; 
                }
                // Since there are Newline characters in the line, do more expensive and
                // accurate check.
                TextRun run = ((TextSpan<TextRun>)_runs[_runs.Count - 1]).Value as TextRun;
                if (run is ParagraphBreakRun)
                {
                    formatResult = ((ParagraphBreakRun)run).BreakReason;
                }
                else if (run is LineBreakRun)
                {
                    formatResult = ((LineBreakRun)run).BreakReason;
                }
                return formatResult;
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
        /// Returns true if there are any inline objects, false otherwise. 
        /// </summary>
        private bool HasInlineObjects()
        {
            bool hasInlineObjects = false;

            foreach (TextSpan<TextRun> textSpan in _runs)
            {
                if (textSpan.Value is InlineObjectRun)
                {
                    hasInlineObjects = true;
                    break;
                }
            }
            return hasInlineObjects;
        }

        /// <summary>
        /// Returns bounds of an object/character at specified text index.
        /// </summary>
        /// <param name="cp">
        /// Character index of an object/character
        /// </param>
        /// <param name="cch">
        /// Number of positions occupied by object/character
        /// </param>
        /// <param name="flowDirection">
        /// Flow direction of object/character
        /// </param>
        /// <returns></returns>
        private Rect GetBoundsFromPosition(int cp, int cch, out FlowDirection flowDirection)
        {
            Rect rect;
            // Calculate shift in line offset to render trailing spaces or avoid clipping text
            double delta = TextDpi.FromTextDpi(CalculateUOffsetShift());
            IList<TextBounds> textBounds;

            if (_line.HasOverflowed && TextParagraph.Properties.TextTrimming != TextTrimming.None)
            {
                // We should not shift offset in this case
                Invariant.Assert(DoubleUtil.AreClose(delta, 0));
                System.Windows.Media.TextFormatting.TextLine line = _line.Collapse(GetCollapsingProps(_wrappingWidth, TextParagraph.Properties));
                Invariant.Assert(line.HasCollapsed, "Line has not been collapsed");
                textBounds = line.GetTextBounds(cp, cch);
            }
            else
            {
                textBounds = _line.GetTextBounds(cp, cch);
            }
            Invariant.Assert(textBounds != null && textBounds.Count == 1, "Expecting exactly one TextBounds for a single text position.");
            IList<TextRunBounds> runBounds = textBounds[0].TextRunBounds;
            if (runBounds != null)
            {
                Debug.Assert(runBounds.Count == 1, "Expecting exactly one TextRunBounds for a single text position.");
                rect = runBounds[0].Rectangle;
            }
            else
            {
                rect = textBounds[0].Rectangle;
            }

            flowDirection = textBounds[0].FlowDirection;           
            rect.X = rect.X + delta;
            return rect;
        }

        /// <summary>
        /// Returns Line collapsing properties
        /// </summary>
        /// <param name="wrappingWidth">
        /// Wrapping width for collapsed line.
        /// </param>
        /// <param name="paraProperties">
        /// Paragraph properties
        /// </param>
        private TextCollapsingProperties GetCollapsingProps(double wrappingWidth, LineProperties paraProperties)
        {
            Invariant.Assert(paraProperties.TextTrimming != TextTrimming.None, "Text trimming must be enabled.");
            TextCollapsingProperties collapsingProps;
            if (paraProperties.TextTrimming == TextTrimming.CharacterEllipsis)
            {
                collapsingProps = new TextTrailingCharacterEllipsis(wrappingWidth, paraProperties.DefaultTextRunProperties);
            }
            else
            {
                collapsingProps = new TextTrailingWordEllipsis(wrappingWidth, paraProperties.DefaultTextRunProperties);
            }

            return collapsingProps;
        }

        /// <summary>
        /// Perform depth-first search on a drawing tree to add all the glyph
        /// runs to the collection
        /// </summary>
        /// <param name="drawing">
        /// Drawing on which we perform DFS
        /// </param>
        /// <param name="glyphRunsCollection">
        /// Glyph run collection.
        /// </param>
        /// <param name="cchGlyphRuns">
        /// Character length of glyph run collection
        /// </param>
        private void AddGlyphRunRecursive(
            Drawing drawing,
            IList   glyphRunsCollection,
            ref int cchGlyphRuns)
        {
            DrawingGroup group = drawing as DrawingGroup;
            if (group != null)
            {
                foreach (Drawing child in group.Children)
                {
                    AddGlyphRunRecursive(child, glyphRunsCollection, ref cchGlyphRuns);
                }
            }
            else
            {
                GlyphRunDrawing glyphRunDrawing = drawing as GlyphRunDrawing;
                if (glyphRunDrawing != null)
                {
                    // Add a glyph run
                    GlyphRun glyphRun = glyphRunDrawing.GlyphRun;
                    if (glyphRun != null)
                    {
                        cchGlyphRuns += (glyphRun.Characters == null ? 0 : glyphRun.Characters.Count);
                        glyphRunsCollection.Add(glyphRun);
                    }
                }
            }
        }

        /// <summary>
        /// Returns amount of shift for X-offset to render trailing spaces
        /// </summary>
        internal int CalculateUOffsetShift()
        {
            int width;
            int trailingSpacesDelta = 0;

            // Calculate amount by which to to move line back if trailing spaces are rendered
            if (IsUOffsetAdjusted)
            {
                width = TextDpi.ToTextDpi(_line.WidthIncludingTrailingWhitespace);
                trailingSpacesDelta =  TextDpi.ToTextDpi(_line.Width) - width;
                Invariant.Assert(trailingSpacesDelta <= 0);
            }
            else
            {
                width = TextDpi.ToTextDpi(_line.Width);
                trailingSpacesDelta = 0;
            }

            // Calculate amount to shift line forward in case we are clipping the front of the line. 
            // If line is showing ellipsis do not perform this check since we should not be clipping the front
            // of the line anyway
            int widthDelta = 0;
            if ((_textAlignment == TextAlignment.Center || _textAlignment == TextAlignment.Right) && !ShowEllipses)
            {
                if (width > TextDpi.ToTextDpi(_wrappingWidth))
                {
                    widthDelta = width - TextDpi.ToTextDpi(_wrappingWidth);
                }
                else
                {
                    widthDelta = 0;
                }
            }
            int totalShift;
            if (_textAlignment == TextAlignment.Center)
            {
                // Divide shift by two to center line
                totalShift = (int)((widthDelta + trailingSpacesDelta) / 2);
            }
            else
            {
                totalShift = widthDelta + trailingSpacesDelta;
            }
            return totalShift;
        }

        #endregion Private methods

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// True if line ends in hard break
        /// </summary>
        private bool HasLineBreak
        {
            get
            {
                return (_line.NewlineLength > 0);
            }
        }

        /// <summary>
        /// True if line's X-offset needs adjustment to render trailing spaces
        /// </summary>
        private bool IsUOffsetAdjusted
        {
            get
            {
                return ((_textAlignment == TextAlignment.Right || _textAlignment == TextAlignment.Center) && IsWidthAdjusted);
            }
        }

        /// <summary>
        /// True if line's width is adjusted to include trailing spaces. For right and center alignment we need to
        /// adjust line offset as well, but for left alignment we need to only make a width asjustment
        /// </summary>
        private bool IsWidthAdjusted
        {
            get
            {
                bool adjusted = false;

                // Trailing spaces rendered only around hard breaks
                if (HasLineBreak || EndOfParagraph)
                {
                    // Lines with ellipsis are not shifted because ellipsis would not appear after trailing spaces
                    if (!ShowEllipses)
                    {
                        adjusted = true;
                    }
                }
                return adjusted;
            }
        }

        /// <summary>
        /// True if eliipsis is displayed in the line
        /// </summary>
        private bool ShowEllipses
        {
            get
            {
                if (TextParagraph.Properties.TextTrimming == TextTrimming.None)
                {
                    return false;
                }
                if (_line.HasOverflowed)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Text Paragraph this line is formatted for
        /// </summary>
        private TextParagraph TextParagraph
        {
            get
            {
                return _paraClient.Paragraph as TextParagraph;
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
        /// TextFormatter host
        /// </summary>
        private readonly TextFormatterHost _host;

        /// <summary>
        /// Character position at the beginning of text paragraph. All DCPs
        /// of the line are relative to this value.
        /// </summary>
        private readonly int _cpPara;

        /// <summary>
        /// Line formatting context. Valid only during formatting. 
        /// </summary>
        private FormattingContext _formattingContext;

        /// <summary>
        /// Text line objects 
        /// </summary>
        private System.Windows.Media.TextFormatting.TextLine _line;

        /// <summary>
        /// Cached run list. This list needs to be in sync with _line object.
        /// Every time the line is recreated, this list needs to be updated. 
        /// </summary>
        private IList<TextSpan<TextRun>> _runs;

        /// <summary>
        /// Character position at the beginning of the line. 
        /// </summary>
        private int _dcp;

        /// <summary>
        /// Line wrapping width
        /// </summary>
        private double _wrappingWidth;

        /// <summary>
        /// Track width (line width ignoring floats) 
        /// </summary>
        private double _trackWidth = Double.NaN;

        /// <summary>
        /// Is text mirrored? 
        /// </summary>
        private bool _mirror;

        /// <summary>
        /// Text indent. 0 for all lines except the first line, which maybe have non-zero indent.
        /// </summary>
        private double _indent;

        /// <summary>
        /// TextAlignment of owner
        /// </summary>
        private TextAlignment _textAlignment;

        #endregion Private Fields

        // ------------------------------------------------------------------
        //
        // FormattingContext Class
        //
        // ------------------------------------------------------------------

        #region FormattingContext Class

        /// <summary>
        /// Text line formatting context 
        /// </summary>
        internal class FormattingContext
        {
            internal FormattingContext(bool measureMode, bool clearOnLeft, bool clearOnRight, TextRunCache textRunCache)
            {
                MeasureMode = measureMode;
                ClearOnLeft = clearOnLeft;
                ClearOnRight = clearOnRight;
                TextRunCache = textRunCache;
                LineFormatLengthTarget = -1;
            }

            internal TextRunCache TextRunCache;
            internal bool MeasureMode;
            internal bool ClearOnLeft;
            internal bool ClearOnRight;
            internal int LineFormatLengthTarget;
        }

        #endregion FormattingContext Class
    }
}

#pragma warning enable 1634, 1691

