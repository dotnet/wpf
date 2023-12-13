// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading;

using System.Windows;
using System.Windows.Media;
using MS.Internal;

using System;

namespace Microsoft.Windows.Themes
{
    public sealed class SystemDropShadowChrome : Decorator
    {
        #region Constructors

        /// <summary>
        /// Instantiates a new instance of a SystemDropShadowChrome
        /// </summary>
        public SystemDropShadowChrome()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="Color" /> property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
                DependencyProperty.Register(
                        "Color",
                        typeof(Color),
                        typeof(SystemDropShadowChrome),
                        new FrameworkPropertyMetadata(
                                Color.FromArgb(0x71, 0x00, 0x00, 0x00),
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(ClearBrushes)));

        /// <summary>
        /// The Color property defines the Color used to fill the shadow region.
        /// </summary>
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="CornerRadius" /> property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = 
                DependencyProperty.Register(
                        "CornerRadius",
                        typeof(CornerRadius),
                        typeof(SystemDropShadowChrome),
                        new FrameworkPropertyMetadata(
                                new CornerRadius(),
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(ClearBrushes)),
                        new ValidateValueCallback(IsCornerRadiusValid));

        private static bool IsCornerRadiusValid(object value)
        {
            CornerRadius cr = (CornerRadius)value;
            return !(cr.TopLeft < 0.0 || cr.TopRight < 0.0 || cr.BottomLeft < 0.0 || cr.BottomRight < 0.0 ||
                     double.IsNaN(cr.TopLeft) || double.IsNaN(cr.TopRight) || double.IsNaN(cr.BottomLeft) || double.IsNaN(cr.BottomRight) ||
                     double.IsInfinity(cr.TopLeft) || double.IsInfinity(cr.TopRight) || double.IsInfinity(cr.BottomLeft) || double.IsInfinity(cr.BottomRight));
        }

        /// <summary>
        /// The CornerRadius property defines the CornerRadius of the object casting the shadow.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        private static void ClearBrushes(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SystemDropShadowChrome)o)._brushes = null;
        }

        #endregion Dynamic Properties

        #region Protected Methods

        private const double ShadowDepth = 5;

        /// <summary>
        /// Render callback.  
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            CornerRadius cornerRadius = CornerRadius;

            Rect shadowBounds = new Rect(new Point(ShadowDepth, ShadowDepth),
                             new Size(RenderSize.Width, RenderSize.Height));
            Color color = Color;

            if (shadowBounds.Width > 0 && shadowBounds.Height > 0 && color.A > 0)
            {
                // The shadow is drawn with a dark center the size of the shadow bounds
                // deflated by shadow depth on each side.
                double centerWidth = shadowBounds.Right - shadowBounds.Left - 2 * ShadowDepth;
                double centerHeight = shadowBounds.Bottom - shadowBounds.Top - 2 * ShadowDepth;

                // Clamp corner radii to be less than 1/2 the side of the inner shadow bounds 
                double maxRadius = Math.Min(centerWidth * 0.5, centerHeight * 0.5);
                cornerRadius.TopLeft = Math.Min(cornerRadius.TopLeft, maxRadius);
                cornerRadius.TopRight = Math.Min(cornerRadius.TopRight, maxRadius);
                cornerRadius.BottomLeft = Math.Min(cornerRadius.BottomLeft, maxRadius);
                cornerRadius.BottomRight = Math.Min(cornerRadius.BottomRight, maxRadius);

                // Get the brushes for the 9 regions
                Brush[] brushes = GetBrushes(color, cornerRadius);

                // Snap grid to device pixels
                double centerTop = shadowBounds.Top + ShadowDepth;
                double centerLeft = shadowBounds.Left + ShadowDepth;
                double centerRight = shadowBounds.Right - ShadowDepth;
                double centerBottom = shadowBounds.Bottom - ShadowDepth;

                // Because of different corner radii there are 6 potential x (or y) lines to snap to
                double[] guidelineSetX = new double[] { centerLeft,
                                                        centerLeft + cornerRadius.TopLeft,
                                                        centerRight - cornerRadius.TopRight,
                                                        centerLeft + cornerRadius.BottomLeft,
                                                        centerRight - cornerRadius.BottomRight,
                                                        centerRight};

                double[] guidelineSetY = new double[] { centerTop,
                                                        centerTop + cornerRadius.TopLeft,
                                                        centerTop + cornerRadius.TopRight,
                                                        centerBottom - cornerRadius.BottomLeft,
                                                        centerBottom - cornerRadius.BottomRight,
                                                        centerBottom};
                
                drawingContext.PushGuidelineSet(new GuidelineSet(guidelineSetX, guidelineSetY));

                // The corner rectangles are drawn drawn ShadowDepth pixels bigger to 
                // account for the blur
                cornerRadius.TopLeft = cornerRadius.TopLeft + ShadowDepth;
                cornerRadius.TopRight = cornerRadius.TopRight + ShadowDepth;
                cornerRadius.BottomLeft = cornerRadius.BottomLeft + ShadowDepth;
                cornerRadius.BottomRight = cornerRadius.BottomRight + ShadowDepth;


                // Draw Top row
                Rect topLeft = new Rect(shadowBounds.Left, shadowBounds.Top, cornerRadius.TopLeft, cornerRadius.TopLeft);
                drawingContext.DrawRectangle(brushes[TopLeft], null, topLeft);

                double topWidth = guidelineSetX[2] - guidelineSetX[1];
                if (topWidth > 0)
                {
                    Rect top = new Rect(guidelineSetX[1], shadowBounds.Top, topWidth, ShadowDepth);
                    drawingContext.DrawRectangle(brushes[Top], null, top);
                }

                Rect topRight = new Rect(guidelineSetX[2], shadowBounds.Top, cornerRadius.TopRight, cornerRadius.TopRight);
                drawingContext.DrawRectangle(brushes[TopRight], null, topRight);

                // Middle row
                double leftHeight = guidelineSetY[3] - guidelineSetY[1];
                if (leftHeight > 0)
                {
                    Rect left = new Rect(shadowBounds.Left, guidelineSetY[1], ShadowDepth, leftHeight);
                    drawingContext.DrawRectangle(brushes[Left], null, left);
                }

                double rightHeight = guidelineSetY[4] - guidelineSetY[2];
                if (rightHeight > 0)
                {
                    Rect right = new Rect(guidelineSetX[5], guidelineSetY[2], ShadowDepth, rightHeight);
                    drawingContext.DrawRectangle(brushes[Right], null, right);
                }

                // Bottom row
                Rect bottomLeft = new Rect(shadowBounds.Left, guidelineSetY[3], cornerRadius.BottomLeft, cornerRadius.BottomLeft);
                drawingContext.DrawRectangle(brushes[BottomLeft], null, bottomLeft);

                double bottomWidth = guidelineSetX[4] - guidelineSetX[3];
                if (bottomWidth > 0)
                {
                    Rect bottom = new Rect(guidelineSetX[3], guidelineSetY[5], bottomWidth, ShadowDepth);
                    drawingContext.DrawRectangle(brushes[Bottom], null, bottom);
                }

                Rect bottomRight = new Rect(guidelineSetX[4], guidelineSetY[4], cornerRadius.BottomRight, cornerRadius.BottomRight);
                drawingContext.DrawRectangle(brushes[BottomRight], null, bottomRight);


                // Fill Center

                // Because the heights of the top/bottom rects and widths of the left/right rects are fixed
                // and the corner rects are drawn with the size of the corner, the center 
                // may not be a square.  In this case, create a path to fill the area

                // When the target object's corner radius is 0, only need to draw one rect
                if (cornerRadius.TopLeft == ShadowDepth &&
                    cornerRadius.TopLeft == cornerRadius.TopRight &&
                    cornerRadius.TopLeft == cornerRadius.BottomLeft &&
                    cornerRadius.TopLeft == cornerRadius.BottomRight)
                {
                    // All corners of target are 0, render one large rectangle
                    Rect center = new Rect(guidelineSetX[0], guidelineSetY[0], centerWidth, centerHeight);
                    drawingContext.DrawRectangle(brushes[Center], null, center);
                }
                else
                {
                    // If the corner radius is TL=2, TR=1, BL=0, BR=2 the following shows the shape that needs to be created.
                    //             _________________
                    //            |                 |_
                    //         _ _|                   |
                    //        |                       |
                    //        |                    _ _|
                    //        |                   |   
                    //        |___________________| 
                    // The missing corners of the shape are filled with the radial gradients drawn above

                    // Define shape counter clockwise
                    PathFigure figure = new PathFigure();

                    if (cornerRadius.TopLeft > ShadowDepth)
                    {
                        figure.StartPoint = new Point(guidelineSetX[1], guidelineSetY[0]);
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[1], guidelineSetY[1]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[0], guidelineSetY[1]), true));
                    }
                    else
                    {
                        figure.StartPoint = new Point(guidelineSetX[0], guidelineSetY[0]);
                    }

                    if (cornerRadius.BottomLeft > ShadowDepth)
                    {
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[0], guidelineSetY[3]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[3], guidelineSetY[3]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[3], guidelineSetY[5]), true));
                    }
                    else
                    {
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[0], guidelineSetY[5]), true));
                    }

                    if (cornerRadius.BottomRight > ShadowDepth)
                    {
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[4], guidelineSetY[5]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[4], guidelineSetY[4]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[5], guidelineSetY[4]), true));
                    }
                    else
                    {
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[5], guidelineSetY[5]), true));
                    }


                    if (cornerRadius.TopRight > ShadowDepth)
                    {
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[5], guidelineSetY[2]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[2], guidelineSetY[2]), true));
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[2], guidelineSetY[0]), true));
                    }
                    else
                    {
                        figure.Segments.Add(new LineSegment(new Point(guidelineSetX[5], guidelineSetY[0]), true));
                    }
                    
                    figure.IsClosed = true;
                    figure.Freeze();

                    PathGeometry geometry = new PathGeometry();
                    geometry.Figures.Add(figure);
                    geometry.Freeze();

                    drawingContext.DrawGeometry(brushes[Center], null, geometry);
                }

                drawingContext.Pop();
            }
        }

        #endregion

        #region Private Properties

        // Create common gradient stop collection for gradient brushes
        private static GradientStopCollection CreateStops(Color c, double cornerRadius)
        {
            // Scale stops to lie within 0 and 1
            double gradientScale = 1 / (cornerRadius + ShadowDepth);

            GradientStopCollection gsc = new GradientStopCollection();
            gsc.Add(new GradientStop(c, (0.5 + cornerRadius) * gradientScale));

            // Create gradient stops based on the Win32 dropshadow fall off
            Color stopColor = c;
            stopColor.A = (byte)(.74336 * c.A);
            gsc.Add(new GradientStop(stopColor, (1.5 + cornerRadius) * gradientScale));

            stopColor.A = (byte)(.38053 * c.A);
            gsc.Add(new GradientStop(stopColor, (2.5 + cornerRadius)* gradientScale));

            stopColor.A = (byte)(.12389 * c.A);
            gsc.Add(new GradientStop(stopColor, (3.5 + cornerRadius) * gradientScale));

            stopColor.A = (byte)(.02654 * c.A);
            gsc.Add(new GradientStop(stopColor, (4.5 + cornerRadius) * gradientScale));

            stopColor.A = 0;
            gsc.Add(new GradientStop(stopColor, (5 + cornerRadius) * gradientScale));

            gsc.Freeze();

            return gsc;
        }

        // Creates an array of brushes needed to render this 
        private static Brush[] CreateBrushes(Color c, CornerRadius cornerRadius)
        {
            Brush[] brushes = new Brush[9];

            // Create center brush
            brushes[Center] = new SolidColorBrush(c);
            brushes[Center].Freeze();



            // Sides
            GradientStopCollection sideStops = CreateStops(c, 0);
            LinearGradientBrush top = new LinearGradientBrush(sideStops, new Point(0, 1), new Point(0, 0));
            top.Freeze();
            brushes[Top] = top;

            LinearGradientBrush left = new LinearGradientBrush(sideStops, new Point(1, 0), new Point(0, 0));
            left.Freeze();
            brushes[Left] = left;

            LinearGradientBrush right = new LinearGradientBrush(sideStops, new Point(0, 0), new Point(1, 0));
            right.Freeze();
            brushes[Right] = right;

            LinearGradientBrush bottom = new LinearGradientBrush(sideStops, new Point(0, 0), new Point(0, 1));
            bottom.Freeze();
            brushes[Bottom] = bottom;

            // Corners

            // Use side stops if the corner radius is 0
            GradientStopCollection topLeftStops;
            if (cornerRadius.TopLeft == 0)
                topLeftStops = sideStops;
            else
                topLeftStops = CreateStops(c, cornerRadius.TopLeft);

            RadialGradientBrush topLeft = new RadialGradientBrush(topLeftStops);
            topLeft.RadiusX = 1;
            topLeft.RadiusY = 1;
            topLeft.Center = new Point(1, 1);
            topLeft.GradientOrigin = new Point(1, 1);
            topLeft.Freeze();
            brushes[TopLeft] = topLeft;

            // Reuse previous stops if corner radius is the same as side or top left
            GradientStopCollection topRightStops;
            if (cornerRadius.TopRight == 0)
                topRightStops = sideStops;
            else if (cornerRadius.TopRight == cornerRadius.TopLeft)
                topRightStops = topLeftStops;
            else
                topRightStops = CreateStops(c, cornerRadius.TopRight);

            RadialGradientBrush topRight = new RadialGradientBrush(topRightStops);
            topRight.RadiusX = 1;
            topRight.RadiusY = 1;
            topRight.Center = new Point(0, 1);
            topRight.GradientOrigin = new Point(0, 1);
            topRight.Freeze();
            brushes[TopRight] = topRight;

            // Reuse previous stops if corner radius is the same as any of the previous radii
            GradientStopCollection bottomLeftStops;
            if (cornerRadius.BottomLeft == 0)
                bottomLeftStops = sideStops;
            else if (cornerRadius.BottomLeft == cornerRadius.TopLeft)
                bottomLeftStops = topLeftStops;
            else if (cornerRadius.BottomLeft == cornerRadius.TopRight)
                bottomLeftStops = topRightStops;
            else
                bottomLeftStops = CreateStops(c, cornerRadius.BottomLeft);

            RadialGradientBrush bottomLeft = new RadialGradientBrush(bottomLeftStops);
            bottomLeft.RadiusX = 1;
            bottomLeft.RadiusY = 1;
            bottomLeft.Center = new Point(1, 0);
            bottomLeft.GradientOrigin = new Point(1, 0);
            bottomLeft.Freeze();
            brushes[BottomLeft] = bottomLeft;

            // Reuse previous stops if corner radius is the same as any of the previous radii
            GradientStopCollection bottomRightStops;
            if (cornerRadius.BottomRight == 0)
                bottomRightStops = sideStops;
            else if (cornerRadius.BottomRight == cornerRadius.TopLeft)
                bottomRightStops = topLeftStops;
            else if (cornerRadius.BottomRight == cornerRadius.TopRight)
                bottomRightStops = topRightStops;
            else if (cornerRadius.BottomRight == cornerRadius.BottomLeft)
                bottomRightStops = bottomLeftStops;
            else
                bottomRightStops = CreateStops(c, cornerRadius.BottomRight);

            RadialGradientBrush bottomRight = new RadialGradientBrush(bottomRightStops);
            bottomRight.RadiusX = 1;
            bottomRight.RadiusY = 1;
            bottomRight.Center = new Point(0, 0);
            bottomRight.GradientOrigin = new Point(0, 0);
            bottomRight.Freeze();
            brushes[BottomRight] = bottomRight;

            return brushes;
        }

        private Brush[] GetBrushes(Color c, CornerRadius cornerRadius)
        {
            if (_commonBrushes == null)
            {
                lock (_resourceAccess)
                {
                    if (_commonBrushes == null)
                    {
                        // Assume that the first render of DropShadow uses the most common color for the app.
                        // This breaks down if (a) the first Shadow is customized, or 
                        // (b) ButtonChrome becomes more broadly used than just on system controls.
                        _commonBrushes = CreateBrushes(c, cornerRadius);
                        _commonCornerRadius = cornerRadius;
                    }
                }
            }
            
            if (c == ((SolidColorBrush)_commonBrushes[Center]).Color &&
                cornerRadius == _commonCornerRadius)
            {
                _brushes = null;  // clear local brushes - use common
                return _commonBrushes;
            }
            else if (_brushes == null)
            {
                // need to create local brushes
                _brushes = CreateBrushes(c, cornerRadius); 
            }
            return _brushes;
        }


        private const int TopLeft = 0;
        private const int Top = 1;
        private const int TopRight = 2;
        private const int Left = 3;
        private const int Center = 4;
        private const int Right = 5;
        private const int BottomLeft = 6;
        private const int Bottom = 7;
        private const int BottomRight = 8;

        // 9 brushes:
        //  0 TopLeft       1 Top       2 TopRight  
        //  3 Left          4 Center    5 Right
        //  6 BottomLeft    7 Bottom    8 BottomRight
        private static Brush[] _commonBrushes;
        private static CornerRadius _commonCornerRadius;
        private static object _resourceAccess = new object();

        // Local brushes if our color is not the common color
        private Brush[] _brushes;

        #endregion
    }
}
