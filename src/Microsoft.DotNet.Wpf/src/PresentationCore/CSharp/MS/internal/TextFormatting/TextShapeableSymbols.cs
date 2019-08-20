// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Definition of text shapeable symbols
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal.Shaping;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Provide definition for a group of characters in which measuring, hittesting 
    /// and drawing of character is done as a group of glyphs.
    /// </summary>
    internal abstract class TextShapeableSymbols : TextRun
    {
        /// <summary>
        /// Compute a shaped glyph run object from specified glyph-based info
        /// </summary>
        /// <param name="origin">location relative to drawing context reference location where the glyph run is drawn</param>
        /// <param name="characterString">character string</param>
        /// <param name="clusterMap">character to glyph cluster mapping</param>
        /// <param name="glyphIndices">array of glyph indices</param>
        /// <param name="glyphAdvances">glyph advance width array</param>
        /// <param name="glyphOffsets">glyph offset array</param>
        /// <param name="rightToLeft">flag indicating whether run is drawn from right to left</param>
        /// <param name="sideways">flag indicating whether run is drawn with its side parallel to baseline</param>
        /// <returns>shaped glyph run object</returns>
        internal abstract GlyphRun ComputeShapedGlyphRun(
            Point                   origin,
            char[]                   characterString,
            ushort[]                 clusterMap,
            ushort[]                 glyphIndices,
            IList<double>            glyphAdvances,
            IList<Point>             glyphOffsets,
            bool                     rightToLeft,
            bool                     sideways
            );


        /// <summary>
        /// Return value indicates whether two runs can shape together
        /// </summary>
        /// <param name="shapeable">another run</param>
        internal abstract bool CanShapeTogether(
            TextShapeableSymbols    shapeable
            );


        /// <summary>
        /// A Boolean value indicates whether run cannot be treated as simple characters because shaping is required
        /// </summary>
        internal abstract bool IsShapingRequired
        { get; }

        /// <summary>
        /// This is needed to decide whether we should pass a max cluster size to LS.
        /// We pass a max cluster size to LS to perform line breaking correctly.
        /// Passing a max cluster size larger than necessary will have impact on perf.
        /// </summary>
        internal abstract bool NeedsMaxClusterSize
        { get; }

        /// <summary>
        /// Get the maximum cluster size that may be associated with the text. 
        /// </summary>
        internal abstract ushort MaxClusterSize
        { get; }

        /// <summary>
        /// A Boolean value indicates whether additional info is required for caret positioning
        /// </summary>
        internal abstract bool NeedsCaretInfo
        { get; }

        /// <summary>
        /// A Boolean value indicates whether run has extended character
        /// </summary>
        internal abstract bool HasExtendedCharacter
        { get; }

        internal abstract GlyphTypeface GlyphTypeFace
        { get; }

        internal abstract double EmSize
        { get; }

        internal abstract MS.Internal.Text.TextInterface.ItemProps ItemProps
        { get; }

        /// <summary>
        /// Get advance widths of unshaped characters
        /// </summary>
        /// <param name="characterString">character string</param>
        /// <param name="characterLength">character length</param>
        /// <param name="scalingFactor">scaling factor</param>
        /// <param name="advanceWidthsUnshaped">unshaped glyph advance widths</param>
        /// <remarks>The method gets glyph advances and glyph offsets in ideal values </remarks>        
        internal abstract unsafe void GetAdvanceWidthsUnshaped(
            char*         characterString,
            int           characterLength,
            double        scalingFactor,
            int*          advanceWidthsUnshaped
            );


        /// <summary>
        /// Compute unshaped glyph run object from the specified character-based info
        /// </summary>
        /// <remarks>
        /// The result of this call is guaranteed to produce a correct display result 
        /// only when IsShapingRequired property of this run is set to true.
        /// </remarks>
        /// <param name="origin">location relative to drawing context reference location where the glyph run is drawn</param>
        /// <param name="characterString">character string</param>
        /// <param name="characterAdvances">character advance values</param>
        /// <returns>display bounding box</returns> 
        /// <remarks>The method constructs glyph run with real values</remarks>
        internal abstract GlyphRun ComputeUnshapedGlyphRun(
            Point         origin,       
            char[]        characterString,
            IList<double> characterAdvances
            );


        /// <summary>
        /// Draw glyph run to the drawing surface
        /// </summary>
        /// <param name="drawingContext">drawing surface</param>
        /// <param name="foregroundBrush">
        /// Foreground brush of the glyphrun. Passing in null brush will mean the GlyphRun 
        /// is to be drawn with the Foreground of the TextRun.
        /// </param>
        /// <param name="glyphRun">glyph run object to be drawn</param>
        internal abstract void Draw(
            DrawingContext      drawingContext,
            Brush               foregroundBrush,
            GlyphRun            glyphRun
            );


        /// <summary>
        /// Run height
        /// </summary>
        internal abstract double Height
        { get; }


        /// <summary>
        /// Distance from top to baseline
        /// </summary>
        internal abstract double Baseline
        { get; }


        /// <summary>
        /// Distance from baseline to underline position relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal abstract double UnderlinePosition
        { get; }


        /// <summary>
        /// Underline thickness relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal abstract double UnderlineThickness
        { get; }


        /// <summary>
        /// Distance from baseline to strike-through position relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal abstract double StrikethroughPosition
        { get; }


        /// <summary>
        /// strike-through thickness relative to TextRunProperties.FontRenderingEmSize
        /// </summary>
        internal abstract double StrikethroughThickness
        { get; }
    }
}

