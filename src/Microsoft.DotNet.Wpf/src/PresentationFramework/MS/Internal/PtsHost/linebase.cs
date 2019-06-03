// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text line formatter.
//


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal.Text;
using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown
                                    // message numbers and unknown pragmas for PRESharp contol

namespace MS.Internal.PtsHost
{
    internal abstract class LineBase : UnmanagedHandle
    {
        internal LineBase(BaseParaClient paraClient) : base(paraClient.PtsContext)
        {
            _paraClient = paraClient;
        }
        
        // ------------------------------------------------------------------
        //
        //  TextSource Implementation
        //
        // ------------------------------------------------------------------

        #region TextSource Implementation

        /// <summary>
        /// Get a text run at specified text source position. 
        /// </summary>
        /// <param name="dcp">
        /// dcp of specified position relative to start of line
        /// </param>
        internal abstract TextRun GetTextRun(int dcp);

        /// <summary>
        /// Get text immediately before specified text source position.
        /// </summary>
        /// <param name="dcp">
        /// dcp of specified position relative to start of line
        /// </param>         
        internal abstract TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int dcp);

        /// <summary>
        /// Get Text effect index from text source character index
        /// </summary>
        /// <param name="dcp">
        /// dcp of specified position relative to start of line
        /// </param>                 
        internal abstract int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int dcp);

        #endregion TextSource Implementation


        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Fetch the next run at text position. 
        /// </summary>
        /// <param name="position">
        /// Current position in text array
        /// </param>
        /// <returns></returns>
        protected TextRun HandleText(StaticTextPointer position)
        {
            DependencyObject element;
            StaticTextPointer endOfRunPosition;

            Invariant.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text, "TextPointer does not point to characters.");

            if (position.Parent != null)
            {
                element = position.Parent;
            }
            else
            {
                element = _paraClient.Paragraph.Element;
            }

            // Extract the aggregated properties into something that the textrun can use.
            //      For properties that can be applied to Highlight services, need to use 'textHighlights'.
            //      Right now only background is properly retrieved.
            TextProperties textProps = new TextProperties(element, position, false /* inline objects */, true /* get background */,
                _paraClient.Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            // Calculate the end of the run by finding either:
            //      a) the next intersection of highlight ranges, or
            //      b) the natural end of this textrun
            endOfRunPosition = position.TextContainer.Highlights.GetNextPropertyChangePosition(position, LogicalDirection.Forward);

            // Clamp the text run at an arbitrary limit, so we don't make
            // an unbounded allocation.
            if (position.GetOffsetToPosition(endOfRunPosition) > 4096)
            {
                endOfRunPosition = position.CreatePointer(4096);
            }

            // Get character buffer for the text run.
            char[] textBuffer = new char[position.GetOffsetToPosition(endOfRunPosition)];

            // Copy characters from text run into buffer. Note the actual number of characters copied,
            // which may be different than the buffer's length. Buffer length only specifies the maximum
            // number of characters
            int charactersCopied = position.GetTextInRun(LogicalDirection.Forward, textBuffer, 0, textBuffer.Length);

            // Create text run using the actual number of characters copied
            return new TextCharacters(textBuffer, 0, charactersCopied, textProps);
        }

        /// <summary>
        /// Return next TextRun at element edge start position
        /// </summary>
        /// <param name="position">
        /// Current position in text array
        /// </param>
        protected TextRun HandleElementStartEdge(StaticTextPointer position)
        {
            Invariant.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart, "TextPointer does not point to element start edge.");

            //      In the future, handle visibility collapsed.
            TextRun run = null;
            TextElement element = (TextElement)position.GetAdjacentElement(LogicalDirection.Forward);
            Debug.Assert(element != null, "Cannot use ITextContainer that does not provide TextElement instances.");

            Invariant.Assert(!(element is Block), "We do not expect any Blocks inside Paragraphs");

            // Treat figure and floaters as special hidden runs.
            if (element is Figure || element is Floater)
            {
                // Get the length of the element
                int cch = TextContainerHelper.GetElementLength(_paraClient.Paragraph.StructuralCache.TextContainer, element);
                // Create special hidden run.
                run = new FloatingRun(cch, element is Figure);
                if (element is Figure)
                {
                    _hasFigures = true;
                }
                else
                {
                    _hasFloaters = true;
                }
            }
            else if (element is LineBreak)
            {
                int cch = TextContainerHelper.GetElementLength(_paraClient.Paragraph.StructuralCache.TextContainer, element);
                run = new LineBreakRun(cch, PTS.FSFLRES.fsflrSoftBreak);
            }
            else if (element.IsEmpty)
            {
                // Empty TextElement should affect line metrics.
                // TextFormatter does not support this feature right now, so as workaround
                // TextRun with ZERO WIDTH SPACE is used.
                TextProperties textProps = new TextProperties(element, position, false /* inline objects */, true /* get background */,
                    _paraClient.Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

                char[] textBuffer = new char[_elementEdgeCharacterLength * 2];
                
                // Assert that _elementEdgeCharacterLength is 1 before we use hard-coded indices
                Invariant.Assert(_elementEdgeCharacterLength == 1, "Expected value of _elementEdgeCharacterLength is 1");

                textBuffer[0] = (char)0x200B;
                textBuffer[1] = (char)0x200B;

                run = new TextCharacters(textBuffer, 0, textBuffer.Length, textProps);
            }
            else
            {
                Inline inline = (Inline) element;
                DependencyObject parent = inline.Parent;

                FlowDirection inlineFlowDirection = inline.FlowDirection;
                FlowDirection parentFlowDirection = inlineFlowDirection;

                TextDecorationCollection inlineTextDecorations = DynamicPropertyReader.GetTextDecorations(inline);

                if(parent != null)
                {
                    parentFlowDirection = (FlowDirection)parent.GetValue(FrameworkElement.FlowDirectionProperty);
                }

                if (inlineFlowDirection != parentFlowDirection)
                {
                    // Inline's flow direction is different from its parent. Need to create new TextSpanModifier with flow direction
                    if (inlineTextDecorations == null || inlineTextDecorations.Count == 0)
                    {
                        run = new TextSpanModifier(
                            _elementEdgeCharacterLength,
                            null,
                            null,
                            inlineFlowDirection
                            );
                    }
                    else
                    {
                        run = new TextSpanModifier(
                            _elementEdgeCharacterLength,
                            inlineTextDecorations,
                            inline.Foreground,
                            inlineFlowDirection
                            );
                    }
                }
                else
                {
                    if (inlineTextDecorations == null || inlineTextDecorations.Count == 0)
                    {
                        run = new TextHidden(_elementEdgeCharacterLength);
                    }
                    else
                    {
                        run = new TextSpanModifier(
                            _elementEdgeCharacterLength,
                            inlineTextDecorations,
                            inline.Foreground
                            );
                    }
                }
            }
            return run;
        }

        /// <summary>
        /// Fetch the next run at element end edge position.
        /// ElementEndEdge; we can have 2 possibilities:
        /// (1) Close edge of element associated with the text paragraph,
        ///     create synthetic LineBreak run to end the current line.
        /// (2) End of inline element, hide CloseEdge character and continue
        /// </summary>
        /// <param name="position"></param>
        /// Position in current text array
        protected TextRun HandleElementEndEdge(StaticTextPointer position)
        {
            Invariant.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd, "TextPointer does not point to element end edge.");

            TextRun run;
            if (position.Parent == _paraClient.Paragraph.Element)
            {
                // (1) Close edge of element associated with the text paragraph,
                //     create synthetic LineBreak run to end the current line.
                run = new ParagraphBreakRun(_syntheticCharacterLength, PTS.FSFLRES.fsflrEndOfParagraph);
            }
            else
            {
                TextElement element = (TextElement)position.GetAdjacentElement(LogicalDirection.Forward);
                Debug.Assert(element != null, "Element should be here.");
                Inline inline = (Inline) element;
                DependencyObject parent = inline.Parent;
                FlowDirection parentFlowDirection = inline.FlowDirection;

                if(parent != null)
                {
                    parentFlowDirection = (FlowDirection)parent.GetValue(FrameworkElement.FlowDirectionProperty);
                }

                if (inline.FlowDirection != parentFlowDirection)
                {
                    run = new TextEndOfSegment(_elementEdgeCharacterLength);
                }
                else
                {
                    TextDecorationCollection textDecorations = DynamicPropertyReader.GetTextDecorations(inline);
                    if (textDecorations == null || textDecorations.Count == 0)
                    {
                        // (2) End of inline element, hide CloseEdge character and continue
                        run = new TextHidden(_elementEdgeCharacterLength);
                    }
                    else
                    {
                        run = new TextEndOfSegment(_elementEdgeCharacterLength);
                    }
                }
            }
            return run;
        }

        /// <summary>
        /// Fetch the next run at embedded object position. 
        /// </summary>
        /// <param name="dcp">
        /// Character offset of this run.
        /// </param>
        /// <param name="position">
        /// Current position in the text array.
        /// </param>
        protected TextRun HandleEmbeddedObject(int dcp, StaticTextPointer position)
        {
            Invariant.Assert(position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement, "TextPointer does not point to embedded object.");

            TextRun run = null;
            DependencyObject embeddedObject = position.GetAdjacentElement(LogicalDirection.Forward) as DependencyObject;
            if (embeddedObject is UIElement)
            {
                // Extract the aggregated properties into something that the textrun can use.
                TextRunProperties textProps = new TextProperties(embeddedObject, position, true /* inline objects */, true /* get background */,
                    _paraClient.Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

                // Create inline object run.
                run = new InlineObjectRun(TextContainerHelper.EmbeddedObjectLength, (UIElement)embeddedObject, textProps, _paraClient.Paragraph as TextParagraph);
            }
            else
            {
                // If the embedded object is of an unknown type, treat it as hidden content.
                run = new TextHidden(TextContainerHelper.EmbeddedObjectLength);
            }
            return run;
        }

        #endregion Protected Methods


        #region Internal Methods

        /// <summary>
        /// Synthetic character length. 
        /// </summary>
        internal static int SyntheticCharacterLength 
        { 
            get 
            { 
                return _syntheticCharacterLength; 
            } 
        }

        /// <summary>
        /// Returns true if any figure runs have been handled by this text source - Only valid after line is formatted.
        /// </summary>
        internal bool HasFigures
        {
            get { return _hasFigures; }
        }

        /// <summary>
        /// Returns true if any floater runs have been handled by this text source - Only valid after line is formatted.
        /// </summary>
        internal bool HasFloaters
        {
            get { return _hasFloaters; }
        }

        #endregion Internal Methods


        #region Protected Fields

        /// <summary>
        /// Owner of the line
        /// </summary>
        protected readonly BaseParaClient _paraClient;

        /// <summary>
        /// Has any figures?
        /// </summary>
        protected bool _hasFigures;

        /// <summary>
        /// Has any floaters?
        /// </summary>
        protected bool _hasFloaters;


        protected static int _syntheticCharacterLength = 1;

        /// <summary>
        /// Element edge character length.
        /// Text and TextFlow assume that ElementEdge position has
        /// always length = 1. 
        /// </summary>
        protected static int _elementEdgeCharacterLength = 1;

        #endregion Protected Fields
    }
}

#pragma warning enable 1634, 1691

