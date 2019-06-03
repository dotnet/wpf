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
    internal sealed class OptimalTextSource : LineBase
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
        /// <param name="cpPara">
        /// Character position of paragraph
        /// </param>
        /// <param name="durTrack">
        /// Track width
        /// </param>
        /// <param name="paraClient">
        /// Owner of the line
        /// </param>
        /// <param name="runCache">
        /// Text run cache
        /// </param>
        internal OptimalTextSource(TextFormatterHost host, int cpPara, int durTrack, TextParaClient paraClient, TextRunCache runCache) : base(paraClient)
        {
            _host = host;
            _durTrack = durTrack;
            _runCache = runCache;
            _cpPara = cpPara;
        }


        /// <summary>
        /// Free all resources associated with the line. Prepare it for reuse.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion Constructors


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
            // Text effect index is an index relative to the start of the Text Container.
            // To convert a text source index to text effect index, we first convert the dcp into text pointer
            // and call GetDistance() from the start of the text container.
            ITextPointer position = TextContainerHelper.GetTextPointerFromCP(_paraClient.Paragraph.StructuralCache.TextContainer, _cpPara + dcp, LogicalDirection.Forward);
            return position.TextContainer.Start.GetOffsetToPosition(position);
        }

        #endregion TextSource Implementation


        internal PTS.FSFLRES GetFormatResultForBreakpoint(int dcp, TextBreakpoint textBreakpoint)
        {
            int dcpRun = 0;
            PTS.FSFLRES formatResult = PTS.FSFLRES.fsflrOutOfSpace;

            foreach (TextSpan<TextRun> textSpan in _runCache.GetTextRunSpans())
            {
                TextRun run = textSpan.Value;

                
                if (run != null && ((dcpRun + run.Length) >= (dcp + textBreakpoint.Length)))
                {
                    if (run is ParagraphBreakRun)
                    {
                        formatResult = ((ParagraphBreakRun)run).BreakReason;
                    }
                    else if (run is LineBreakRun)
                    {
                        formatResult = ((LineBreakRun)run).BreakReason;
                    }
                    break;
                }

                dcpRun += textSpan.Length;
            }

            return formatResult;
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
            // Always measure at infinity for bottomless, consistent constraint.
            double pageHeight = _paraClient.Paragraph.StructuralCache.CurrentFormatContext.DocumentPageSize.Height;
            if (!_paraClient.Paragraph.StructuralCache.CurrentFormatContext.FinitePage)
            {
                pageHeight = Double.PositiveInfinity;
            }

            return inlineObject.UIElementIsland.DoLayout(new Size(TextDpi.FromTextDpi(_durTrack), pageHeight), true, true);
        }


        /// <summary>
        /// TextFormatter host
        /// </summary>
        private readonly TextFormatterHost _host;

        private TextRunCache _runCache;

        private int _durTrack;
        private int _cpPara;
    }
}

#pragma warning enable 1634, 1691

