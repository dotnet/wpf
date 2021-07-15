// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      A class which is used as the feedback adorner of the InkCanvas selection
//

using MS.Internal;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MS.Internal.Controls
{
    /// <summary>
    /// InkCanvasFeedbackAdorner
    /// </summary>
    internal class InkCanvasFeedbackAdorner : Adorner
    {
        // No default constructor
        private InkCanvasFeedbackAdorner() : base(null) { }

        /// <summary>
        /// InkCanvasFeedbackAdorner Constructor
        /// </summary>
        /// <param name="inkCanvas">The adorned InkCanvas</param>
        internal InkCanvasFeedbackAdorner(InkCanvas inkCanvas)
            : base((inkCanvas != null ? inkCanvas.InnerCanvas : null))
        {
            if (inkCanvas == null)
                throw new ArgumentNullException("inkCanvas");

            // Initialize the internal data
            _inkCanvas = inkCanvas;

            _adornerBorderPen = new Pen(Brushes.Black, 1.0);
            DoubleCollection dashes = new DoubleCollection();
            dashes.Add(4.5);
            dashes.Add(4.5);
            _adornerBorderPen.DashStyle = new DashStyle(dashes, 2.25);
            _adornerBorderPen.DashCap = PenLineCap.Flat;
        }

        /// <summary>
        /// The overridden GetDesiredTransform method
        /// </summary>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            if (transform == null)
            {
                throw new ArgumentNullException("transform");
            }

            VerifyAccess();
            GeneralTransformGroup desiredTransform = new GeneralTransformGroup();
            desiredTransform.Children.Add(transform);

            // Check if we need translate the adorner.
            if (!DoubleUtil.AreClose(_offsetX, 0) || !DoubleUtil.AreClose(_offsetY, 0))
            {
                desiredTransform.Children.Add(new TranslateTransform(_offsetX, _offsetY));
            }

            return desiredTransform;
        }

        /// <summary>
        /// The OnBoundsUpdated method
        /// </summary>
        /// <param name="rect"></param>
        private void OnBoundsUpdated(Rect rect)
        {
            VerifyAccess();

            // Check if the rectangle has been changed.
            if (rect != _previousRect)
            {
                Size newSize;
                double offsetX;
                double offsetY;
                bool invalidArrange = false;

                if (!rect.IsEmpty)
                {
                    double offset = BorderMargin + CornerResizeHandleSize / 2;
                    Rect adornerRect = Rect.Inflate(rect, offset, offset);

                    newSize = new Size(adornerRect.Width, adornerRect.Height);
                    offsetX = adornerRect.Left;
                    offsetY = adornerRect.Top;
                }
                else
                {
                    newSize = new Size(0, 0);
                    offsetX = 0;
                    offsetY = 0;
                }

                // Check if the size has been changed
                if (_frameSize != newSize)
                {
                    _frameSize = newSize;
                    invalidArrange = true;
                }

                if (!DoubleUtil.AreClose(_offsetX, offsetX) || !DoubleUtil.AreClose(_offsetY, offsetY))
                {
                    _offsetX = offsetX;
                    _offsetY = offsetY;
                    invalidArrange = true;
                }

                if (invalidArrange)
                {
                    InvalidateMeasure();
                    InvalidateVisual(); //ensure re-rendering
                    UIElement parent = ((UIElement)VisualTreeHelper.GetParent(this)) as UIElement;

                    if (parent != null)
                    {
                        ((UIElement)VisualTreeHelper.GetParent(this)).InvalidateArrange();
                    }
                }

                _previousRect = rect;
            }
        }

        /// <summary>
        /// The overridden MeasureOverride method
        /// </summary>
        /// <param name="constraint"></param>
        protected override Size MeasureOverride(Size constraint)
        {
            VerifyAccess();

            // return the frame size.
            return _frameSize;
        }

        /// <summary>
        /// The overridden OnRender method
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // No need to invoke VerifyAccess since this method calls DrawingContext.DrawRectangle.

            Debug.Assert(_frameSize != new Size(0, 0));
            // Draw the wire frame.
            drawingContext.DrawRectangle(null, _adornerBorderPen,
                new Rect(CornerResizeHandleSize / 2, CornerResizeHandleSize / 2,
                    _frameSize.Width - CornerResizeHandleSize, _frameSize.Height - CornerResizeHandleSize));
        }

        /// <summary>
        /// The method is called by InkCanvasSelection.UpdateFeedbackRect
        /// </summary>
        /// <param name="rect"></param>
        internal void UpdateBounds(Rect rect)
        {
            // Invoke OnBoundsUpdated.
            OnBoundsUpdated(rect);
        }

        private InkCanvas _inkCanvas;
        private Size _frameSize = new Size(0, 0);
        private Rect _previousRect = Rect.Empty;
        private double _offsetX = 0;
        private double _offsetY = 0;

        private Pen _adornerBorderPen;

        private const int CornerResizeHandleSize = 8;
        private const double BorderMargin = 8f;
    }
}
