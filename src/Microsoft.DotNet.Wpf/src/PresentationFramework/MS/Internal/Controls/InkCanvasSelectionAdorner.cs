// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      A class which is used as the selection adorner of the InkCanvas selection
//

using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Ink;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MS.Internal.Controls
{
    /// <summary>
    /// InkCanvasSelectionAdorner
    /// </summary>
    internal class InkCanvasSelectionAdorner : Adorner
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="adornedElement">The adorned InkCanvas</param>
        internal InkCanvasSelectionAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            Debug.Assert(adornedElement is InkCanvasInnerCanvas,
                "InkCanvasSelectionAdorner only should be used by InkCanvas internally");

            // Initialize the internal data.
            _adornerBorderPen = new Pen(Brushes.Black, 1.0);
            DoubleCollection dashes = new DoubleCollection();
            dashes.Add(4.5);
            dashes.Add(4.5);
            _adornerBorderPen.DashStyle = new DashStyle(dashes, 2.25);
            _adornerBorderPen.DashCap = PenLineCap.Flat;
            _adornerBorderPen.Freeze();

            _adornerPenBrush = new Pen(new SolidColorBrush(Color.FromRgb(132, 146, 222)), 1);
            _adornerPenBrush.Freeze();

            _adornerFillBrush = new LinearGradientBrush(Color.FromRgb(240, 242, 255), //start color
                                            Color.FromRgb(180, 207, 248),               //end color
                                            45f                                         //angle
                                            );
            _adornerFillBrush.Freeze();

            // Create a hatch pen
            DrawingGroup hatchDG = new DrawingGroup();
            DrawingContext dc = null;

            try
            {
                dc = hatchDG.Open();

                dc.DrawRectangle(
                    Brushes.Transparent,
                    null,
                    new Rect(0.0, 0.0, 1f, 1f));

                Pen squareCapPen = new Pen(Brushes.Black, LineThickness);
                squareCapPen.StartLineCap = PenLineCap.Square;
                squareCapPen.EndLineCap = PenLineCap.Square;

                dc.DrawLine(squareCapPen,
                    new Point(1f, 0f), new Point(0f, 1f));
            }
            finally
            {
                if (dc != null)
                {
                    dc.Close();
                }
            }
            hatchDG.Freeze();

            DrawingBrush tileBrush = new DrawingBrush(hatchDG);
            tileBrush.TileMode = TileMode.Tile;
            tileBrush.Viewport = new Rect(0, 0, HatchBorderMargin, HatchBorderMargin);
            tileBrush.ViewportUnits = BrushMappingMode.Absolute;
            tileBrush.Freeze();

            _hatchPen = new Pen(tileBrush, HatchBorderMargin);
            _hatchPen.Freeze();

            _elementsBounds = new List<Rect>();
            _strokesBounds = Rect.Empty;
        }

        /// <summary>
        /// SelectionHandleHitTest
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        internal InkCanvasSelectionHitResult SelectionHandleHitTest(Point point)
        {
            InkCanvasSelectionHitResult result = InkCanvasSelectionHitResult.None;
            Rect rectWireFrame = GetWireFrameRect();

            if (!rectWireFrame.IsEmpty)
            {
                // Hit test on the grab handles first
                for (InkCanvasSelectionHitResult hitResult = InkCanvasSelectionHitResult.TopLeft;
                        hitResult <= InkCanvasSelectionHitResult.Left; hitResult++)
                {
                    Rect toleranceRect;
                    Rect visibleRect;
                    GetHandleRect(hitResult, rectWireFrame, out visibleRect, out toleranceRect);

                    if (toleranceRect.Contains(point))
                    {
                        result = hitResult;
                        break;
                    }
                }

                // Now, check if we hit on the frame
                if (result == InkCanvasSelectionHitResult.None)
                {
                    Rect outterRect = Rect.Inflate(rectWireFrame, CornerResizeHandleSize / 2, CornerResizeHandleSize / 2);
                    if (outterRect.Contains(point))
                    {
                        result = InkCanvasSelectionHitResult.Selection;

                        //We need to add Hittest on the selected element
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Update the selection wire frame.
        /// Called by
        ///         InkCanvasSelection.UpdateSelectionAdorner
        /// </summary>
        /// <param name="strokesBounds"></param>
        /// <param name="hatchBounds"></param>
        internal void UpdateSelectionWireFrame(Rect strokesBounds, List<Rect> hatchBounds)
        {
            bool isStrokeBoundsDifferent = false;
            bool isElementsBoundsDifferent = false;

            // Check if the strokes' bounds are changed.
            if (_strokesBounds != strokesBounds)
            {
                _strokesBounds = strokesBounds;
                isStrokeBoundsDifferent = true;
            }

            // Check if the elements' bounds are changed.
            int count = hatchBounds.Count;
            if (count != _elementsBounds.Count)
            {
                isElementsBoundsDifferent = true;
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (_elementsBounds[i] != hatchBounds[i])
                    {
                        isElementsBoundsDifferent = true;
                        break;
                    }
                }
            }


            if (isStrokeBoundsDifferent || isElementsBoundsDifferent)
            {
                if (isElementsBoundsDifferent)
                {
                    _elementsBounds = hatchBounds;
                }

                // Invalidate our visual since the selection is changed.
                InvalidateVisual();
            }
        }

        /// <summary>
        /// OnRender
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // Draw the background and hatch border around the elements
            DrawBackgound(drawingContext);

            // Draw the selection frame.
            Rect rectWireFrame = GetWireFrameRect();
            if (!rectWireFrame.IsEmpty)
            {
                // Draw the wire frame.
                drawingContext.DrawRectangle(null,
                    _adornerBorderPen,
                    rectWireFrame);

                // Draw grab handles
                DrawHandles(drawingContext, rectWireFrame);
            }
        }


        /// <summary>
        /// Draw Handles
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="rectWireFrame"></param>
        private void DrawHandles(DrawingContext drawingContext, Rect rectWireFrame)
        {
            for (InkCanvasSelectionHitResult hitResult = InkCanvasSelectionHitResult.TopLeft;
                    hitResult <= InkCanvasSelectionHitResult.Left; hitResult++)
            {
                // Draw the handle
                Rect toleranceRect;
                Rect visibleRect;
                GetHandleRect(hitResult, rectWireFrame, out visibleRect, out toleranceRect);

                drawingContext.DrawRectangle(_adornerFillBrush, _adornerPenBrush, visibleRect);
            }
        }

        /// <summary>
        /// Draw the hatches and the transparent area where isn't covering the elements.
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawBackgound(DrawingContext drawingContext)
        {
            PathGeometry hatchGeometry = null;
            Geometry rectGeometry = null;

            int count = _elementsBounds.Count;
            if (count != 0)
            {
                // Create a union collection of the element regions.
                for (int i = 0; i < count; i++)
                {
                    Rect hatchRect = _elementsBounds[i];

                    if (hatchRect.IsEmpty)
                    {
                        continue;
                    }

                    hatchRect.Inflate(HatchBorderMargin / 2, HatchBorderMargin / 2);

                    if (hatchGeometry == null)
                    {
                        PathFigure path = new PathFigure();
                        path.StartPoint = new Point(hatchRect.Left, hatchRect.Top);

                        PathSegmentCollection segments = new PathSegmentCollection();

                        PathSegment line = new LineSegment(new Point(hatchRect.Right, hatchRect.Top), true);
                        line.Freeze();
                        segments.Add(line);

                        line = new LineSegment(new Point(hatchRect.Right, hatchRect.Bottom), true);
                        line.Freeze();
                        segments.Add(line);

                        line = new LineSegment(new Point(hatchRect.Left, hatchRect.Bottom), true);
                        line.Freeze();
                        segments.Add(line);

                        line = new LineSegment(new Point(hatchRect.Left, hatchRect.Top), true);
                        line.Freeze();
                        segments.Add(line);

                        segments.Freeze();
                        path.Segments = segments;

                        path.IsClosed = true;
                        path.Freeze();

                        hatchGeometry = new PathGeometry();
                        hatchGeometry.Figures.Add(path);
                    }
                    else
                    {
                        rectGeometry = new RectangleGeometry(hatchRect);
                        rectGeometry.Freeze();

                        hatchGeometry = Geometry.Combine(hatchGeometry, rectGeometry, GeometryCombineMode.Union, null);
                    }
                }
            }

            // Then, create a region which equals to "SelectionFrame - element1 bounds - element2 bounds - ..."
            GeometryGroup backgroundGeometry = new GeometryGroup();
            GeometryCollection geometryCollection = new GeometryCollection();

            // Add the entile rectanlge to the group.
            rectGeometry = new RectangleGeometry(new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            rectGeometry.Freeze();
            geometryCollection.Add(rectGeometry);

            // Add the union of the element rectangles. Then the group will do oddeven operation.
            Geometry outlineGeometry = null;

            if (hatchGeometry != null)
            {
                hatchGeometry.Freeze();

                outlineGeometry = hatchGeometry.GetOutlinedPathGeometry();
                outlineGeometry.Freeze();
                if (count == 1 && ((InkCanvasInnerCanvas)AdornedElement).InkCanvas.GetSelectedStrokes().Count == 0)
                {
                    geometryCollection.Add(outlineGeometry);
                }
            }

            geometryCollection.Freeze();
            backgroundGeometry.Children = geometryCollection;
            backgroundGeometry.Freeze();

            // Then, draw the region which may contain holes so that the elements cannot be covered.
            // After that, the underneath elements can receive the messages.
#if DEBUG_OUTPUT
            // Draw the debug feedback
            drawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(128, 255, 255, 0)), null, backgroundGeometry);
#else
            drawingContext.DrawGeometry(Brushes.Transparent, null, backgroundGeometry);
#endif

            // At last, draw the hatch borders
            if (outlineGeometry != null)
            {
                drawingContext.DrawGeometry(null, _hatchPen, outlineGeometry);
            }
        }

        /// <summary>
        /// Returns the handle rect (both visibile and the tolerance one)
        /// </summary>
        private void GetHandleRect(InkCanvasSelectionHitResult hitResult, Rect rectWireFrame, out Rect visibleRect, out Rect toleranceRect)
        {
            Point center = new Point();
            double size = 0;
            double tolerance = ResizeHandleTolerance;

            switch (hitResult)
            {
                case InkCanvasSelectionHitResult.TopLeft:
                    {
                        size = CornerResizeHandleSize;
                        center = new Point(rectWireFrame.Left, rectWireFrame.Top);
                        break;
                    }
                case InkCanvasSelectionHitResult.Top:
                    {
                        size = MiddleResizeHandleSize;
                        center = new Point(rectWireFrame.Left + rectWireFrame.Width / 2, rectWireFrame.Top);
                        tolerance = (CornerResizeHandleSize - MiddleResizeHandleSize) + ResizeHandleTolerance;
                        break;
                    }
                case InkCanvasSelectionHitResult.TopRight:
                    {
                        size = CornerResizeHandleSize;
                        center = new Point(rectWireFrame.Right, rectWireFrame.Top);
                        break;
                    }
                case InkCanvasSelectionHitResult.Left:
                    {
                        size = MiddleResizeHandleSize;
                        center = new Point(rectWireFrame.Left, rectWireFrame.Top + rectWireFrame.Height / 2);
                        tolerance = (CornerResizeHandleSize - MiddleResizeHandleSize) + ResizeHandleTolerance;
                        break;
                    }
                case InkCanvasSelectionHitResult.Right:
                    {
                        size = MiddleResizeHandleSize;
                        center = new Point(rectWireFrame.Right, rectWireFrame.Top + rectWireFrame.Height / 2);
                        tolerance = (CornerResizeHandleSize - MiddleResizeHandleSize) + ResizeHandleTolerance;
                        break;
                    }
                case InkCanvasSelectionHitResult.BottomLeft:
                    {
                        size = CornerResizeHandleSize;
                        center = new Point(rectWireFrame.Left, rectWireFrame.Bottom);
                        break;
                    }
                case InkCanvasSelectionHitResult.Bottom:
                    {
                        size = MiddleResizeHandleSize;
                        center = new Point(rectWireFrame.Left + rectWireFrame.Width / 2, rectWireFrame.Bottom);
                        tolerance = (CornerResizeHandleSize - MiddleResizeHandleSize) + ResizeHandleTolerance;
                        break;
                    }
                case InkCanvasSelectionHitResult.BottomRight:
                    {
                        size = CornerResizeHandleSize;
                        center = new Point(rectWireFrame.Right, rectWireFrame.Bottom);
                        break;
                    }
            }

            visibleRect = new Rect(center.X - size / 2, center.Y - size / 2, size, size);
            toleranceRect = visibleRect;
            toleranceRect.Inflate(tolerance, tolerance);
        }

        /// <summary>
        /// Returns the wire frame bounds which crosses the center of the selection handles
        /// </summary>
        /// <returns></returns>
        private Rect GetWireFrameRect()
        {
            Rect frameRect = Rect.Empty;
            Rect selectionRect = ((InkCanvasInnerCanvas)AdornedElement).InkCanvas.GetSelectionBounds();

            if (!selectionRect.IsEmpty)
            {
                frameRect = Rect.Inflate(selectionRect, BorderMargin, BorderMargin);
            }

            return frameRect;
        }

        private Pen _adornerBorderPen;
        private Pen _adornerPenBrush;
        private Brush _adornerFillBrush;
        private Pen _hatchPen;
        private Rect _strokesBounds;
        private List<Rect> _elementsBounds;

        // The buffer around the outside of this element
        private const double BorderMargin = HatchBorderMargin + 2f;
        private const double HatchBorderMargin = 6f;

        // Constants for Resize handles.
        private const int CornerResizeHandleSize = 8;
        private const int MiddleResizeHandleSize = 6;
        private const double ResizeHandleTolerance = 3d;
        private const double LineThickness = 0.16;
    }
}

