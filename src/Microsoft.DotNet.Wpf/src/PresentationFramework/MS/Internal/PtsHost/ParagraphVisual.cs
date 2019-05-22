// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Visual representing a paragraph. 
//

using System;
using System.Windows;
using System.Windows.Media;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Visual representing a paragraph.
    /// </summary>
    internal class ParagraphVisual : DrawingVisual
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal ParagraphVisual()
        {
            _renderBounds = Rect.Empty;
        }

        /// <summary>
        /// Draw background and border.
        /// </summary>
        /// <param name="backgroundBrush">The brush used for background.</param>
        /// <param name="borderBrush">The brush used for border.</param>
        /// <param name="borderThickness">Border thickness.</param>
        /// <param name="renderBounds">Render bounds of the visual.</param>
        /// <param name="isFirstChunk">Whether this is paragraph's first chunk.</param>
        /// <param name="isLastChunk">Whether this is paragraph's last chunk.</param>
        internal void DrawBackgroundAndBorder(Brush backgroundBrush, Brush borderBrush, Thickness borderThickness, Rect renderBounds, bool isFirstChunk, bool isLastChunk)
        {
            if (_backgroundBrush != backgroundBrush || _renderBounds != renderBounds ||
                _borderBrush != borderBrush || !Thickness.AreClose(_borderThickness, borderThickness))
            {
                // Open DrawingContext and draw background.
                using (DrawingContext dc = RenderOpen())
                {
                    DrawBackgroundAndBorderIntoContext(dc, backgroundBrush, borderBrush, borderThickness, renderBounds, isFirstChunk, isLastChunk);
                }
            }
        }


        /// <summary>
        /// Draw background and border.
        /// </summary>
        /// <param name="dc">Drawing context.</param>
        /// <param name="backgroundBrush">The brush used for background.</param>
        /// <param name="borderBrush">The brush used for border.</param>
        /// <param name="borderThickness">Border thickness.</param>
        /// <param name="renderBounds">Render bounds of the visual.</param>
        /// <param name="isFirstChunk">Whether this is paragraph's first chunk.</param>
        /// <param name="isLastChunk">Whether this is paragraph's last chunk.</param>
        internal void DrawBackgroundAndBorderIntoContext(DrawingContext dc, Brush backgroundBrush, Brush borderBrush, Thickness borderThickness, Rect renderBounds, bool isFirstChunk, bool isLastChunk)
        {
            // We do not want to cause the user's Brushes to become frozen when we
            // freeze the Pen in OnRender, therefore we make our own copy of the
            // Brushes if they are not already frozen.
            _backgroundBrush = (Brush)FreezableOperations.GetAsFrozenIfPossible(backgroundBrush);
            _renderBounds = renderBounds;
            _borderBrush = (Brush)FreezableOperations.GetAsFrozenIfPossible(borderBrush);
            _borderThickness = borderThickness;

            // Exclude top/bottom border if this is not the first/last chunk of the paragraph.
            if (!isFirstChunk)
            {
                _borderThickness.Top = 0.0;
            }
            if (!isLastChunk)
            {
                _borderThickness.Bottom = 0.0;
            }

            // If we have a brush with which to draw the border, do so.
            // NB: We double draw corners right now.  Corner handling is tricky (bevelling, &c...) and
            //     we need a firm spec before doing "the right thing."  (greglett, ffortes)
            if (_borderBrush != null)
            {
                // Initialize the first pen.  Note that each pen is created via new()
                // and frozen if possible.  Doing this avoids the overhead of
                // maintaining changed handlers.
                Pen pen = new Pen();
                pen.Brush = _borderBrush;
                pen.Thickness = _borderThickness.Left;
                if (pen.CanFreeze) { pen.Freeze(); }

                if (_borderThickness.IsUniform)
                {
                    // Uniform border; stroke a rectangle. 
                    dc.DrawRectangle(null, pen, new Rect(
                        new Point(_renderBounds.Left + pen.Thickness * 0.5, _renderBounds.Bottom - pen.Thickness * 0.5),
                        new Point(_renderBounds.Right - pen.Thickness * 0.5, _renderBounds.Top + pen.Thickness * 0.5)));
                }
                else
                {
                    if (DoubleUtil.GreaterThan(_borderThickness.Left, 0))
                    {
                        dc.DrawLine(pen,
                            new Point(_renderBounds.Left + pen.Thickness / 2, _renderBounds.Top),
                            new Point(_renderBounds.Left + pen.Thickness / 2, _renderBounds.Bottom));
                    }
                    if (DoubleUtil.GreaterThan(_borderThickness.Right, 0))
                    {
                        pen = new Pen();
                        pen.Brush = _borderBrush;
                        pen.Thickness = _borderThickness.Right;
                        if (pen.CanFreeze) { pen.Freeze(); }

                        dc.DrawLine(pen,
                            new Point(_renderBounds.Right - pen.Thickness / 2, _renderBounds.Top),
                            new Point(_renderBounds.Right - pen.Thickness / 2, _renderBounds.Bottom));
                    }
                    if (DoubleUtil.GreaterThan(_borderThickness.Top, 0))
                    {
                        pen = new Pen();
                        pen.Brush = _borderBrush;
                        pen.Thickness = _borderThickness.Top;
                        if (pen.CanFreeze) { pen.Freeze(); }

                        dc.DrawLine(pen,
                            new Point(_renderBounds.Left, _renderBounds.Top + pen.Thickness / 2),
                            new Point(_renderBounds.Right, _renderBounds.Top + pen.Thickness / 2));
                    }
                    if (DoubleUtil.GreaterThan(_borderThickness.Bottom, 0))
                    {
                        pen = new Pen();
                        pen.Brush = _borderBrush;
                        pen.Thickness = _borderThickness.Bottom;
                        if (pen.CanFreeze) { pen.Freeze(); }

                        dc.DrawLine(pen,
                            new Point(_renderBounds.Left, _renderBounds.Bottom - pen.Thickness / 2),
                            new Point(_renderBounds.Right, _renderBounds.Bottom - pen.Thickness / 2));
                    }
                }
            }


            // Draw background in rectangle inside border.
            if (_backgroundBrush != null)
            {
                dc.DrawRectangle(_backgroundBrush, null,
                    new Rect(
                        new Point(_renderBounds.Left + _borderThickness.Left, _renderBounds.Top + _borderThickness.Top),
                        new Point(_renderBounds.Right - _borderThickness.Right, _renderBounds.Bottom - _borderThickness.Bottom)));
            }
        }

        private Brush _backgroundBrush;         // Background brush
        private Brush _borderBrush;             // Border brush
        private Thickness _borderThickness;     // Border thickness
        private Rect _renderBounds;             // Render bounds of the visual
    }
}
