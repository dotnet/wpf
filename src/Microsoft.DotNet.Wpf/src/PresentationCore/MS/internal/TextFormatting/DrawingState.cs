// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Drawing state of full text
//
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Formatting state of full text
    /// </summary>
    internal sealed class DrawingState : IDisposable
    {
        private TextMetrics.FullTextLine    _currentLine;               // full text line currently formatted
        private DrawingContext              _drawingContext;            // current drawing context
        private Point                       _lineOrigin;                // line origin XY relative to drawing context reference location
        private Point                       _vectorToLineOrigin;        // vector to line origin in UV relative to paragraph start
        private MatrixTransform             _antiInversion;             // anti-inversion transform applied on drawing surface
        private bool                        _overrideBaseGuidelineY;    // a flag indicating whether a new guideline overrides the line's Y guideline
        private double                      _baseGuidelineY;            // the Y guideline of the text line.

        /// <summary>
        /// Construct drawing state for full text
        /// </summary>
        internal DrawingState(
            DrawingContext                  drawingContext,
            Point                           lineOrigin,
            MatrixTransform                 antiInversion,
            TextMetrics.FullTextLine        currentLine
            )
        {
            _drawingContext = drawingContext;
            _antiInversion = antiInversion;
            _currentLine = currentLine;            

            if (antiInversion == null)
            {
                _lineOrigin = lineOrigin;
            }
            else
            {
                _vectorToLineOrigin = lineOrigin;
            }

            if (_drawingContext != null)
            {
                // LineServices draws GlyphRun and TextDecorations in multiple 
                // callbacks and GlyphRuns may have different baselines. Pushing guideline
                // for each DrawGlyphRun are too costly. We optimize for the common case where
                // GlyphRuns and TextDecorations in the TextLine share the same baseline. 
                _baseGuidelineY = lineOrigin.Y + currentLine.Baseline;

                _drawingContext.PushGuidelineY1(_baseGuidelineY);
            }
        }

        /// <summary>
        /// Set guideline Y for a drawing operation if necessary. It is a no-op if the Y value is the same
        /// as the guideline Y of the line. Otherwise, it will push the Y to override the guideline of the line.
        /// A SetGuidelineY() must be paired with an UnsetGuidelineY() to ensure balanced push and pop. 
        /// </summary>
        internal void SetGuidelineY(double runGuidelineY)
        {
            if (_drawingContext == null)
                return;

            Invariant.Assert(!_overrideBaseGuidelineY);

            if (runGuidelineY != _baseGuidelineY)
            {
                // Push a new guideline to override the line's guideline
                _drawingContext.PushGuidelineY1(runGuidelineY);
                    
                _overrideBaseGuidelineY = true; // the new Guideline Y overrides the line's guideline until next unset.
            }
        }  

        /// <summary>
        /// Unset guideline Y for a drawing operation if necessary. It is a no-op if the Y value is the same
        /// as the guideline Y of the line. Otherwise, it will push the Y to override the guideline of the line.
        /// A SetGuidelineY() must be paired with an UnsetGuidelineY() to ensure balanced push and pop.  
        /// </summary>
        internal void UnsetGuidelineY()
        {
            if (_overrideBaseGuidelineY)
            {
                _drawingContext.Pop();
                _overrideBaseGuidelineY = false;                    
            }
        }

        /// <summary>
        /// Clean up internal state.
        /// </summary>
        public void Dispose()
        {
            // clear the guideline at line's baseline
            if (_drawingContext != null)
            {
                _drawingContext.Pop();
            }
        }             


        /// <summary>
        /// Current drawing context
        /// </summary>
        internal DrawingContext DrawingContext
        {
            get { return _drawingContext; }
        }


        /// <summary>
        /// Anti-inversion transform applied on drawing surface
        /// </summary>
        internal MatrixTransform AntiInversion
        {
            get { return _antiInversion; }
        }


        /// <summary>
        /// Origin XY of the current line relative to the drawing context reference location
        /// </summary>
        internal Point LineOrigin
        {
            get { return _lineOrigin; }
        }


        /// <summary>
        /// Vector to line origin in UV relative to paragraph start
        /// </summary>
        internal Point VectorToLineOrigin
        {
            get { return _vectorToLineOrigin; }
        }


        /// <summary>
        /// Line currently being drawn
        /// </summary>
        internal TextMetrics.FullTextLine CurrentLine
        {
            get { return _currentLine; }
        }
    }
}
