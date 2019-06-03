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

namespace MS.Internal.PtsHost
{
    internal class ListMarkerLine : LineBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">
        /// TextFormatter host
        /// </param>
        /// <param name="paraClient">
        /// Owner of the ListMarker
        /// </param>
        internal ListMarkerLine(TextFormatterHost host, ListParaClient paraClient) : base(paraClient)
        {
            _host = host;
        }

        // ------------------------------------------------------------------
        //
        //  TextSource Implementation
        //
        // ------------------------------------------------------------------

        #region TextSource Implementation

        /// <summary>
        /// Return the text run at specified text source position.
        /// </summary>
        /// <param name="dcp">
        /// Offset of specified position
        /// </param>
        internal override TextRun GetTextRun(int dcp)
        {
            return new ParagraphBreakRun(1, PTS.FSFLRES.fsflrEndOfParagraph);
        }

        /// <summary>
        /// Return the text, as CharacterBufferRange, immediately before specified text source position.
        /// </summary>
        /// <param name="dcp">
        /// Offset of specified position
        /// </param>
        internal override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int dcp)
        {
            return new TextSpan<CultureSpecificCharacterBufferRange>(
                0,
                new CultureSpecificCharacterBufferRange(null, CharacterBufferRange.Empty)
                );
        }

        /// <summary>
        /// Get Text effect index from specified position 
        /// </summary>
        /// <param name="dcp">
        /// Offset of specified position
        /// </param>
        /// <returns></returns>
        internal override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int dcp)
        {
            return dcp;
        }

        #endregion TextSource Implementation

        /// <summary>
        /// Create and format text line. 
        /// </summary>
        /// <param name="ctx">
        /// DrawingContext for text line.
        /// </param>
        /// <param name="lineProps">
        /// LineProperties of text line
        /// </param>
        /// <param name="ur">
        /// Horizontal draw location
        /// </param>
        /// <param name="vrBaseline">
        /// Vertical baseline draw location
        /// </param>
        internal void FormatAndDrawVisual(DrawingContext ctx, LineProperties lineProps, int ur, int vrBaseline)
        {
            System.Windows.Media.TextFormatting.TextLine line;
            bool mirror = (lineProps.FlowDirection == FlowDirection.RightToLeft);

            _host.Context = this;

            try
            {
                // Create line object
                line = _host.TextFormatter.FormatLine(_host, 0, 0, lineProps.FirstLineProps, null, new TextRunCache());

                Point drawLocation = new Point(TextDpi.FromTextDpi(ur), TextDpi.FromTextDpi(vrBaseline) - line.Baseline);

                line.Draw(ctx, drawLocation, (mirror ? InvertAxes.Horizontal : InvertAxes.None));
                line.Dispose();
            }
            finally
            {
                // clear the context
                _host.Context = null; 
            }
        }

        /// <summary>
        /// Text formatter host
        /// </summary>
        private readonly TextFormatterHost _host;
    }
}
