// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MS.Internal;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    ///     The ScrollChrome element
    ///     This element is a theme-specific type that is used as an optimization
    ///     for a common complex rendering used in Aero
    /// </summary>
    public sealed class ScrollChrome : FrameworkElement
    {

        #region Constructors

        static ScrollChrome()
        {
            IsEnabledProperty.OverrideMetadata(typeof(ScrollChrome), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnEnabledChanged)));
        }

        /// <summary>
        ///     Instantiates a new instance of a ScrollChrome with no parent element.
        /// </summary>
        public ScrollChrome()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        ///     Attached DependencyProperty to assign the orientation and type of the glyph
        /// </summary>
        public static readonly DependencyProperty ScrollGlyphProperty =
                DependencyProperty.RegisterAttached(
                        "ScrollGlyph", 
                        typeof(ScrollGlyph), 
                        typeof(ScrollChrome),
                        new FrameworkPropertyMetadata(
                                ScrollGlyph.None, 
                                FrameworkPropertyMetadataOptions.AffectsRender),
                        new ValidateValueCallback(IsValidScrollGlyph));


        /// <summary>
        ///     Gets the value of the ScrollGlyph property on the object.
        /// </summary>
        /// <param name="element">The element to which the property is attached.</param>
        /// <returns>The value of the property.</returns>
        public static ScrollGlyph GetScrollGlyph(DependencyObject element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            
            return (ScrollGlyph) element.GetValue(ScrollGlyphProperty);
        }

        /// <summary>
        ///     Attachs the value to the object.
        /// </summary>
        /// <param name="element">The element on which the value will be attached.</param>
        /// <param name="value">The value to attach.</param>
        public static void SetScrollGlyph(DependencyObject element, ScrollGlyph value)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            
            element.SetValue(ScrollGlyphProperty, value);
        }

        private ScrollGlyph ScrollGlyph
        {
            get { return (ScrollGlyph) GetValue(ScrollGlyphProperty); }
        }

        private static bool IsValidScrollGlyph(object o)
        {
            ScrollGlyph glyph = (ScrollGlyph)o;
            return glyph == ScrollGlyph.None ||
                glyph == ScrollGlyph.LeftArrow ||
                glyph == ScrollGlyph.RightArrow ||
                glyph == ScrollGlyph.UpArrow ||
                glyph == ScrollGlyph.DownArrow ||
                glyph == ScrollGlyph.VerticalGripper ||
                glyph == ScrollGlyph.HorizontalGripper;
        }        

        private static void OnEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollChrome chrome = ((ScrollChrome)o);

            if (chrome.Animates)
            {
                if (((bool)e.NewValue))
                {
                    if (chrome._localResources == null)
                    {
                        chrome._localResources = new LocalResources();
                        chrome.InvalidateVisual();
                    }

                    Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

                    if (chrome._scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        chrome._scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        DoubleAnimation da = new DoubleAnimation(1, duration);
                        chrome.Glyph.BeginAnimation(LinearGradientBrush.OpacityProperty, da);

                        da = new DoubleAnimation(0.63, duration);
                        chrome.GlyphShadow.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    }
                    else
                    {
                        DoubleAnimation da = new DoubleAnimation(1, duration);
                        chrome.Fill.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                        chrome.OuterBorder.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);

                        da = new DoubleAnimation(0.63, duration);
                        chrome.InnerBorder.Brush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);

                        da = new DoubleAnimation(0.5, duration);
                        chrome.Shadow.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);

                        ColorAnimation ca = new ColorAnimation(Color.FromRgb(0x21, 0x21, 0x21), duration);
                        chrome.Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        ca = new ColorAnimation(Color.FromRgb(0x57, 0x57, 0x57), duration);
                        chrome.Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                        ca = new ColorAnimation(Color.FromRgb(0xB3, 0xB3, 0xB3), duration);
                        chrome.Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    }
                }
                else if (chrome._localResources == null)
                {
                    chrome.InvalidateVisual();
                }
                else
                {
                    Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

                    if (chrome._scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        chrome._scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        DoubleAnimation da = new DoubleAnimation();
                        da.Duration = duration;
                        chrome.Glyph.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                        chrome.GlyphShadow.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    }
                    else
                    {
                        DoubleAnimation da = new DoubleAnimation();
                        da.Duration = duration;

                        chrome.Fill.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                        chrome.OuterBorder.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        chrome.InnerBorder.Brush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                        chrome.Shadow.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);

                        ColorAnimation ca = new ColorAnimation();
                        ca.Duration = duration;
                        chrome.Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        chrome.Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                        chrome.Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    }
                }
            }
            else
            {
                chrome._localResources = null;
                chrome.InvalidateVisual();
            }
        }


        /// <summary>
        /// DependencyProperty for <see cref="RenderMouseOver" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderMouseOverProperty =
                 DependencyProperty.Register("RenderMouseOver",
                         typeof(bool),
                         typeof(ScrollChrome),
                         new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnRenderMouseOverChanged)));

        /// <summary>
        /// When true the chrome renders with a mouse over look.
        /// </summary>
        public bool RenderMouseOver
        {
            get { return (bool)GetValue(RenderMouseOverProperty); }
            set { SetValue(RenderMouseOverProperty, value); }
        }

        private static void OnRenderMouseOverChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollChrome chrome = ((ScrollChrome)o);

            if (chrome.Animates)
            {
                if (chrome._localResources == null)
                {
                    chrome._localResources = new LocalResources();
                    chrome.InvalidateVisual();
                }

                if (((bool)e.NewValue))
                {
                    chrome.AnimateToHover();
                }
                else
                {
                    Duration duration = new Duration(TimeSpan.FromSeconds(0.2));
                    ColorAnimation ca = new ColorAnimation();
                    ca.Duration = duration;

                    chrome.OuterBorder.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                    chrome.Fill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                    chrome.Fill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                    chrome.Fill.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    chrome.Fill.GradientStops[3].BeginAnimation(GradientStop.ColorProperty, ca);
                    chrome.Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                    chrome.Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                    chrome.Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                }
            }
            else
            {
                chrome._localResources = null;
                chrome.InvalidateVisual();
            }
        }

        private void AnimateToHover()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));
            ColorAnimation ca = new ColorAnimation(Color.FromRgb(0x3C, 0x7F, 0xB1), duration);
            OuterBorder.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0xE3, 0xF4, 0xFC), duration);
            Fill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0xD6, 0xEE, 0xFB), duration);
            Fill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0xA9, 0xDB, 0xF6), duration);
            Fill.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0xA4, 0xD5, 0xEF), duration);
            Fill.GradientStops[3].BeginAnimation(GradientStop.ColorProperty, ca);


            if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                _scrollGlyph == ScrollGlyph.VerticalGripper)
            {
                ca = new ColorAnimation(Color.FromRgb(0x15, 0x30, 0x3E), duration);
                Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x3C, 0x7F, 0xB1), duration);
                Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x9C, 0xCE, 0xE9), duration);
                Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
            }
            else
            {
                ca = new ColorAnimation(Color.FromRgb(0x0D, 0x2A, 0x3A), duration);
                Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x1F, 0x63, 0x8A), duration);
                Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x2E, 0x97, 0xCF), duration);
                Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="RenderPressed" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderPressedProperty =
                 DependencyProperty.Register("RenderPressed",
                         typeof(bool),
                         typeof(ScrollChrome),
                         new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnRenderPressedChanged)));

        /// <summary>
        /// When true the chrome renders with a pressed look.
        /// </summary>
        public bool RenderPressed
        {
            get { return (bool)GetValue(RenderPressedProperty); }
            set { SetValue(RenderPressedProperty, value); }
        }

        private static void OnRenderPressedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScrollChrome chrome = ((ScrollChrome)o);

            if (chrome.Animates)
            {
                if (chrome._localResources == null)
                {
                    chrome._localResources = new LocalResources();
                    chrome.InvalidateVisual();
                }

                if (((bool)e.NewValue))
                {
                    Duration duration = new Duration(TimeSpan.FromSeconds(0.3));
                    ColorAnimation ca = new ColorAnimation(Color.FromRgb(0x15, 0x59, 0x8A), duration);
                    chrome.OuterBorder.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0xCA, 0xEC, 0xF9), duration);
                    chrome.Fill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0xAF, 0xE1, 0xF7), duration);
                    chrome.Fill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0x6F, 0xCA, 0xF0), duration);
                    chrome.Fill.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0x66, 0xBA, 0xDD), duration);
                    chrome.Fill.GradientStops[3].BeginAnimation(GradientStop.ColorProperty, ca);


                    if (chrome._scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        chrome._scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        ca = new ColorAnimation(Color.FromRgb(0x0F, 0x24, 0x30), duration);
                        chrome.Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x2E, 0x73, 0x97), duration);
                        chrome.Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x8F, 0xB8, 0xCE), duration);
                        chrome.Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                    }
                    else
                    {
                        ca = new ColorAnimation(Color.FromRgb(0x0E, 0x22, 0x2D), duration);
                        chrome.Glyph.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x2F, 0x79, 0x9E), duration);
                        chrome.Glyph.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x6B, 0xA0, 0xBC), duration);
                        chrome.Glyph.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    }
                }
                else
                {
                    chrome.AnimateToHover();
                }
            }
            else
            {
                chrome._localResources = null;
                chrome.InvalidateVisual();
            }
        }

        #endregion Dynamic Properties

        #region Protected Methods

        /// <summary>
        ///     Updates DesiredSize of the ScrollChrome.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        ///     ScrollChrome basically constrains the value of its Width and Height properties.
        /// </remarks>
        /// <param name="availableSize">Available size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The ScrollChrome's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            _transform = null;

            return new Size(0,0);
        }

        /// <summary>
        ///     ScrollChrome does no work here and returns arrangeSize.
        /// </summary>
        /// <param name="finalSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _transform = null;
            return finalSize;
        }

        /// <summary>
        /// Render callback.  
        /// Note: Assumes all borders are 1 unit.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            _scrollGlyph = ScrollGlyph;

            if ((bounds.Width >= 1.0) && (bounds.Height >= 1.0))
            {
                bounds.X += 0.5;
                bounds.Y += 0.5;
                bounds.Width -= 1.0;
                bounds.Height -= 1.0;
            }

            switch (_scrollGlyph)
            {
                case ScrollGlyph.LeftArrow:
                case ScrollGlyph.RightArrow:
                case ScrollGlyph.HorizontalGripper:
                    if (bounds.Height >= 1.0)
                    {
                        bounds.Y += 1.0;
                        bounds.Height -= 1.0;
                    }
                    break;

                case ScrollGlyph.UpArrow:
                case ScrollGlyph.DownArrow:
                case ScrollGlyph.VerticalGripper:
                    if (bounds.Width >= 1.0)
                    {
                        bounds.X += 1.0;
                        bounds.Width -= 1.0;
                    }
                    break;
            }

            DrawShadow(drawingContext, ref bounds);
            DrawBorders(drawingContext, ref bounds);
            DrawGlyph(drawingContext, ref bounds);
        }

        #endregion

        #region Private Methods

        private void DrawShadow(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width > 0.0) && (bounds.Height > 2.0))
            {
                Pen pen = Shadow;
                if (pen != null)
                {
                    dc.DrawRoundedRectangle(
                        null,
                        pen,
                        new Rect(bounds.X, bounds.Y + 2.0, bounds.Width, bounds.Height - 2.0),
                        3.0, 3.0);
                }
                bounds.Height -= 1.0;
                bounds.Width = Math.Max(0.0, bounds.Width - 1.0);
            }
        }

        private void DrawBorders(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
            {
                Brush brush = Fill;
                Pen pen = OuterBorder;
                if (pen != null)
                {
                    dc.DrawRoundedRectangle(
                        brush,
                        pen,
                        new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                        1.0, 1.0);
                    brush = null; // Done with the fill
                }
                bounds.Inflate(-1.0, -1.0);

                if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
                {
                    pen = InnerBorder;
                    if ((pen != null) || (brush != null))
                    {
                        dc.DrawRoundedRectangle(
                            brush,
                            pen,
                            new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                            0.5, 0.5);
                    }
                    bounds.Inflate(-1.0, -1.0);
                }
            }
        }

        private void DrawGlyph(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width > 0.0) && (bounds.Height > 0.0))
            {
                Brush brush = Glyph;
                if ((brush != null) && (_scrollGlyph != ScrollGlyph.None))
                {
                    switch (_scrollGlyph)
                    {
                        case ScrollGlyph.HorizontalGripper:
                            DrawHorizontalGripper(dc, brush, bounds);
                            break;

                        case ScrollGlyph.VerticalGripper:
                            DrawVerticalGripper(dc, brush, bounds);
                            break;

                        case ScrollGlyph.LeftArrow:
                        case ScrollGlyph.RightArrow:
                        case ScrollGlyph.UpArrow:
                        case ScrollGlyph.DownArrow:
                            DrawArrow(dc, brush, bounds);
                            break;
                    }
                }
            }
        }

        private void DrawHorizontalGripper(DrawingContext dc, Brush brush, Rect bounds)
        {
            if ((bounds.Width > 15.0) && (bounds.Height > 2.0))
            {
                Brush glyphShadow = GlyphShadow;

                double height = Math.Min(7.0, bounds.Height);
                double shadowHeight = height + 1.0;
                double x = bounds.X + ((bounds.Width * 0.5) - 4.0);
                double y = bounds.Y + ((bounds.Height - height) * 0.5);

                for (int i = 0; i < 9; i += 3)
                {
                    if (glyphShadow != null)
                    {
                        dc.DrawRectangle(glyphShadow, null, new Rect(x + i - 0.5, y - 0.5, 3.0, shadowHeight));
                    }

                    dc.DrawRectangle(brush, null, new Rect(x + i, y, 2.0, height));
                }
            }
        }

        private void DrawVerticalGripper(DrawingContext dc, Brush brush, Rect bounds)
        {
            if ((bounds.Width > 2.0) && (bounds.Height > 15.0))
            {
                Brush glyphShadow = GlyphShadow;

                double width = Math.Min(7.0, bounds.Width);
                double shadowWidth = width + 1.0;
                double x = bounds.X + ((bounds.Width - width) * 0.5);
                double y = bounds.Y + ((bounds.Height * 0.5) - 4.0);

                for (int i = 0; i < 9; i += 3)
                {
                    if (glyphShadow != null)
                    {
                        dc.DrawRectangle(glyphShadow, null, new Rect(x - 0.5, y + i - 0.5, shadowWidth, 3.0));
                    }

                    dc.DrawRectangle(brush, null, new Rect(x, y + i, width, 2.0));
                }
            }
        }

        #region Arrow Geometry Generation

        // Geometries for arrows don't change so use static versions to reduce working set
        private static object _glyphAccess = new object();
        private static Geometry _leftArrowGeometry;
        private static Geometry _rightArrowGeometry;
        private static Geometry _upArrowGeometry;
        private static Geometry _downArrowGeometry;

        private static Geometry LeftArrowGeometry
        {
            get
            {
                if (_leftArrowGeometry == null)
                {
                    lock (_glyphAccess)
                    {
                        if (_leftArrowGeometry == null)
                        {
                            PathFigure figure = new PathFigure();
                            figure.StartPoint = new Point(4.0, 0.0);
                            figure.Segments.Add(new LineSegment(new Point(0, 3.5), true));
                            figure.Segments.Add(new LineSegment(new Point(4.0, 7.0), true));
                            figure.IsClosed = true;
                            figure.Freeze();

                            PathGeometry path = new PathGeometry();
                            path.Figures.Add(figure);
                            path.Freeze();

                            _leftArrowGeometry = path;
                        }
                    }
                }

                return _leftArrowGeometry;
            }
        }

        private static Geometry RightArrowGeometry
        {
            get
            {
                if (_rightArrowGeometry == null)
                {
                    lock (_glyphAccess)
                    {
                        if (_rightArrowGeometry == null)
                        {
                            PathFigure figure = new PathFigure();
                            figure.StartPoint = new Point(0.0, 0.0);
                            figure.Segments.Add(new LineSegment(new Point(4, 3.5), true));
                            figure.Segments.Add(new LineSegment(new Point(0.0, 7.0), true));
                            figure.IsClosed = true;
                            figure.Freeze();

                            PathGeometry path = new PathGeometry();
                            path.Figures.Add(figure);
                            path.Freeze();

                            _rightArrowGeometry = path;
                        }
                    }
                }

                return _rightArrowGeometry;
            }
        }

        private static Geometry UpArrowGeometry
        {
            get
            {
                if (_upArrowGeometry == null)
                {
                    lock (_glyphAccess)
                    {
                        if (_upArrowGeometry == null)
                        {
                            PathFigure figure = new PathFigure();
                            figure.StartPoint = new Point(0.0, 4.0);
                            figure.Segments.Add(new LineSegment(new Point(3.5, 0), true));
                            figure.Segments.Add(new LineSegment(new Point(7.0, 4.0), true));
                            figure.IsClosed = true;
                            figure.Freeze();

                            PathGeometry path = new PathGeometry();
                            path.Figures.Add(figure);
                            path.Freeze();

                            _upArrowGeometry = path;
                        }
                    }
                }

                return _upArrowGeometry;
            }
        }

        private static Geometry DownArrowGeometry
        {
            get
            {
                if (_downArrowGeometry == null)
                {
                    lock (_glyphAccess)
                    {
                        if (_downArrowGeometry == null)
                        {
                            PathFigure figure = new PathFigure();
                            figure.StartPoint = new Point(0.0, 0.0);
                            figure.Segments.Add(new LineSegment(new Point(3.5, 4.0), true));
                            figure.Segments.Add(new LineSegment(new Point(7.0, 0.0), true));
                            figure.IsClosed = true;
                            figure.Freeze();

                            PathGeometry path = new PathGeometry();
                            path.Figures.Add(figure);
                            path.Freeze();

                            _downArrowGeometry = path;
                        }
                    }
                }

                return _downArrowGeometry;
            }
        }

                
        #endregion

        private void DrawArrow(DrawingContext dc, Brush brush, Rect bounds)
        {
            if (_transform == null)
            {
                double glyphWidth = 7.0;
                double glyphHeight = 4.0;
                if (_scrollGlyph == ScrollGlyph.LeftArrow || _scrollGlyph == ScrollGlyph.RightArrow)
                {
                    glyphWidth = 4.0;
                    glyphHeight = 7.0;
                }
                Matrix matrix = new Matrix();

                if ((bounds.Width < glyphWidth) || (bounds.Height < glyphHeight))
                {
                    double widthScale = Math.Min(glyphWidth, bounds.Width) / glyphWidth;
                    double heightScale = Math.Min(glyphHeight, bounds.Height) / glyphHeight;

                    double x = (bounds.X + (bounds.Width * 0.5)) / widthScale - (glyphWidth * 0.5);
                    double y = (bounds.Y + (bounds.Height * 0.5)) / heightScale - (glyphHeight * 0.5);

                    if (double.IsNaN(widthScale) || double.IsInfinity(widthScale) || double.IsNaN(heightScale) || double.IsInfinity(heightScale) ||
                        double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                    {
                        return;
                    }

                    matrix.Translate(x, y);
                    matrix.Scale(widthScale, heightScale);
                }
                else
                {
                    double x = bounds.X + (bounds.Width * 0.5) - (glyphWidth * 0.5);
                    double y = bounds.Y + (bounds.Height * 0.5) - (glyphHeight * 0.5);
                    matrix.Translate(x, y);
                }

                _transform = new MatrixTransform();
                _transform.Matrix = matrix;
            }

            dc.PushTransform(_transform);

            switch (_scrollGlyph)
            {
                case ScrollGlyph.LeftArrow:
                    dc.DrawGeometry(brush, null, LeftArrowGeometry);
                    break;

                case ScrollGlyph.RightArrow:
                    dc.DrawGeometry(brush, null, RightArrowGeometry);
                    break;

                case ScrollGlyph.UpArrow:
                    dc.DrawGeometry(brush, null, UpArrowGeometry);
                    break;

                case ScrollGlyph.DownArrow:
                    dc.DrawGeometry(brush, null, DownArrowGeometry);
                    break;
            }
            
            dc.Pop(); // Center and scaling transform
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 19; }
        }

        #endregion

        #region Data

        private bool Animates
        {
            get
            {
                return SystemParameters.ClientAreaAnimation && 
                       RenderCapability.Tier > 0;
            }
        }

        private static LinearGradientBrush CommonHorizontalThumbFill
        {
            get
            {
                if (_commonHorizontalThumbFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalThumbFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xF3, 0xF3, 0xF3), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xE8, 0xE9), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xD6, 0xD6, 0xD8), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xBC, 0xBD, 0xC0), 1));

                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonHorizontalThumbFill = temp;
                        }
                    }
                }
                return _commonHorizontalThumbFill;
            }
        }

        private static LinearGradientBrush CommonHorizontalThumbHoverFill
        {
            get
            {
                if (_commonHorizontalThumbHoverFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalThumbHoverFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xE3, 0xF4, 0xFC), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xD6, 0xEE, 0xFB), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xA9, 0xDB, 0xF6), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xA4, 0xD5, 0xEF), 1));

                            temp.Freeze();
                            _commonHorizontalThumbHoverFill = temp;
                        }
                    }
                }
                return _commonHorizontalThumbHoverFill;
            }
        }

        private static LinearGradientBrush CommonHorizontalThumbPressedFill
        {
            get
            {
                if (_commonHorizontalThumbPressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalThumbPressedFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCA, 0xEC, 0xF9), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xAF, 0xE1, 0xF7), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x6F, 0xCA, 0xF0), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x66, 0xBA, 0xDD), 1));

                            temp.Freeze();
                            _commonHorizontalThumbPressedFill = temp;
                        }
                    }
                }
                return _commonHorizontalThumbPressedFill;
            }
        }


        private static LinearGradientBrush CommonVerticalThumbFill
        {
            get
            {
                if (_commonVerticalThumbFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalThumbFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xF3, 0xF3, 0xF3), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xE8, 0xE9), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xD6, 0xD6, 0xD8), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xBC, 0xBD, 0xC0), 1));

                            temp.Freeze();
                            _commonVerticalThumbFill = temp;
                        }
                    }
                }
                return _commonVerticalThumbFill;
            }
        }

        private static LinearGradientBrush CommonVerticalThumbHoverFill
        {
            get
            {
                if (_commonVerticalThumbHoverFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalThumbHoverFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xE3, 0xF4, 0xFC), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xD6, 0xEE, 0xFB), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xA9, 0xDB, 0xF6), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xA4, 0xD5, 0xEF), 1));

                            temp.Freeze();
                            _commonVerticalThumbHoverFill = temp;
                        }
                    }
                }
                return _commonVerticalThumbHoverFill;
            }
        }

        private static LinearGradientBrush CommonVerticalThumbPressedFill
        {
            get
            {
                if (_commonVerticalThumbPressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalThumbPressedFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCA, 0xEC, 0xF9), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xAF, 0xE1, 0xF7), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x6F, 0xCA, 0xF0), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x66, 0xBA, 0xDD), 1));

                            temp.Freeze();
                            _commonVerticalThumbPressedFill = temp;
                        }
                    }
                }
                return _commonVerticalThumbPressedFill;
            }
        }


        private LinearGradientBrush Fill
        {
            get
            {
                if (!Animates)
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.LeftArrow ||
                        _scrollGlyph == ScrollGlyph.RightArrow)
                    {
                        if (RenderPressed)
                        {
                            return CommonHorizontalThumbPressedFill;
                        }
                        else if (RenderMouseOver)
                        {
                            return CommonHorizontalThumbHoverFill;
                        }
                        else if (IsEnabled || _scrollGlyph == ScrollGlyph.HorizontalGripper)
                        {
                            return CommonHorizontalThumbFill;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else 
                    {
                        if (RenderPressed)
                        {
                            return CommonVerticalThumbPressedFill;
                        }
                        else if (RenderMouseOver)
                        {
                            return CommonVerticalThumbHoverFill;
                        }
                        else if (IsEnabled || _scrollGlyph == ScrollGlyph.VerticalGripper)
                        {
                            return CommonVerticalThumbFill;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.Fill == null)
                    {
                        if (_scrollGlyph == ScrollGlyph.HorizontalGripper)
                        {
                            _localResources.Fill = CommonHorizontalThumbFill.Clone();
                        }
                        else if (_scrollGlyph == ScrollGlyph.VerticalGripper)
                        {
                            _localResources.Fill = CommonVerticalThumbFill.Clone();
                        }
                        else
                        {
                            if (_scrollGlyph == ScrollGlyph.LeftArrow ||
                                _scrollGlyph == ScrollGlyph.RightArrow)
                            {
                                _localResources.Fill = CommonHorizontalThumbFill.Clone();
                            }
                            else
                            {
                                _localResources.Fill = CommonVerticalThumbFill.Clone();
                            }
                            _localResources.Fill.Opacity = 0;

                        }
                    }
                    return _localResources.Fill;
                }
                else
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper)
                    {
                        return CommonHorizontalThumbFill;
                    }
                    else if (_scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonVerticalThumbFill;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        private static Pen CommonThumbOuterBorder
        {
            get
            {
                if (_commonThumbOuterBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbOuterBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x95, 0x95, 0x95));

                            temp.Freeze();
                            _commonThumbOuterBorder = temp;
                        }
                    }
                }
                return _commonThumbOuterBorder;
            }
        }

        private static Pen CommonThumbHoverOuterBorder
        {
            get
            {
                if (_commonThumbHoverOuterBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbHoverOuterBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x3C, 0x7F, 0xB1));

                            temp.Freeze();
                            _commonThumbHoverOuterBorder = temp;
                        }
                    }
                }
                return _commonThumbHoverOuterBorder;
            }
        }

        private static Pen CommonThumbPressedOuterBorder
        {
            get
            {
                if (_commonThumbPressedOuterBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbPressedOuterBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x15, 0x59, 0x8A));

                            temp.Freeze();
                            _commonThumbPressedOuterBorder = temp;
                        }
                    }
                }
                return _commonThumbPressedOuterBorder;
            }
        }

        private Pen OuterBorder
        {
            get
            {
                if (!Animates)
                {
                    if (RenderPressed)
                    {
                        return CommonThumbPressedOuterBorder;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonThumbHoverOuterBorder;
                    }
                    else if (IsEnabled || 
                             _scrollGlyph == ScrollGlyph.HorizontalGripper ||
                             _scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonThumbOuterBorder;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.OuterBorder == null)
                    {
                        _localResources.OuterBorder = CommonThumbOuterBorder.Clone();

                        if (!(_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                            _scrollGlyph == ScrollGlyph.VerticalGripper))
                        {
                            _localResources.OuterBorder.Brush.Opacity = 0;
                        }
                    }
                    return _localResources.OuterBorder;
                }
                else
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonThumbOuterBorder;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        private static Pen CommonThumbInnerBorder
        {
            get
            {
                if (_commonThumbInnerBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbInnerBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Colors.White);
                            temp.Brush.Opacity = 0.63;

                            temp.Freeze();
                            _commonThumbInnerBorder = temp;
                        }
                    }
                }
                return _commonThumbInnerBorder;
            }
        }

        private Pen InnerBorder
        {
            get
            {
                if (!Animates)
                {
                    if (IsEnabled || 
                        _scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonThumbInnerBorder;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.InnerBorder == null)
                    {
                        _localResources.InnerBorder = CommonThumbInnerBorder.Clone();

                        if (!(_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                            _scrollGlyph == ScrollGlyph.VerticalGripper))
                        {
                            _localResources.InnerBorder.Brush.Opacity = 0;
                        }
                    }
                    return _localResources.InnerBorder;
                }
                else
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonThumbInnerBorder;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        private static Pen CommonThumbShadow
        {
            get
            {
                if (_commonThumbShadow == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbShadow == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0xCF, 0xCF, 0xCF));
                            temp.Brush.Opacity = 0.5;

                            temp.Freeze();
                            _commonThumbShadow = temp;
                        }
                    }
                }
                return _commonThumbShadow;
            }
        }

        private Pen Shadow
        {
            get
            {
                if (!Animates)
                {
                    if (IsEnabled ||
                        _scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonThumbShadow;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.Shadow == null)
                    {
                        _localResources.Shadow = CommonThumbShadow.Clone();

                        if (!(_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                            _scrollGlyph == ScrollGlyph.VerticalGripper))
                        {
                            _localResources.Shadow.Brush.Opacity = 0;
                        }
                    }
                    return _localResources.Shadow;
                }
                else
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonThumbShadow;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        private LinearGradientBrush CommonHorizontalThumbEnabledGlyph
        {
            get
            {
                if (_commonHorizontalThumbEnabledGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalThumbEnabledGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0.05);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x00, 0x00), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x97, 0x97, 0x97), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCA, 0xCA, 0xCA), 1));

                            temp.Freeze();
                            _commonHorizontalThumbEnabledGlyph = temp;
                        }
                    }
                }
                return _commonHorizontalThumbEnabledGlyph;
            }
        }

        private LinearGradientBrush CommonHorizontalThumbHoverGlyph
        {
            get
            {
                if (_commonHorizontalThumbHoverGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalThumbHoverGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0.05);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x15, 0x30, 0x3E), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x3C, 0x7F, 0xB1), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x9C, 0xCE, 0xE9), 1));

                            temp.Freeze();
                            _commonHorizontalThumbHoverGlyph = temp;
                        }
                    }
                }
                return _commonHorizontalThumbHoverGlyph;
            }
        }

        private LinearGradientBrush CommonHorizontalThumbPressedGlyph
        {
            get
            {
                if (_commonHorizontalThumbPressedGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalThumbPressedGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0.05);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x0F, 0x24, 0x30), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x2E, 0x73, 0x97), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x8F, 0xB8, 0xCE), 1));

                            temp.Freeze();
                            _commonHorizontalThumbPressedGlyph = temp;
                        }
                    }
                }
                return _commonHorizontalThumbPressedGlyph;
            }
        }

        private LinearGradientBrush CommonVerticalThumbEnabledGlyph
        {
            get
            {
                if (_commonVerticalThumbEnabledGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalThumbEnabledGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0.05, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x00, 0x00), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x97, 0x97, 0x97), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCA, 0xCA, 0xCA), 1));

                            temp.Freeze();
                            _commonVerticalThumbEnabledGlyph = temp;
                        }
                    }
                }
                return _commonVerticalThumbEnabledGlyph;
            }
        }

        private LinearGradientBrush CommonVerticalThumbHoverGlyph
        {
            get
            {
                if (_commonVerticalThumbHoverGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalThumbHoverGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0.05, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x15, 0x30, 0x3E), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x3C, 0x7F, 0xB1), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x9C, 0xCE, 0xE9), 1));

                            temp.Freeze();
                            _commonVerticalThumbHoverGlyph = temp;
                        }
                    }
                }
                return _commonVerticalThumbHoverGlyph;
            }
        }

        private LinearGradientBrush CommonVerticalThumbPressedGlyph
        {
            get
            {
                if (_commonVerticalThumbPressedGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalThumbPressedGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0.05, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x0F, 0x24, 0x30), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x2E, 0x73, 0x97), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x8F, 0xB8, 0xCE), 1));

                            temp.Freeze();
                            _commonVerticalThumbPressedGlyph = temp;
                        }
                    }
                }
                return _commonVerticalThumbPressedGlyph;
            }
        }


        private LinearGradientBrush CommonButtonGlyph
        {
            get
            {
                if (_commonButtonGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonButtonGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.MappingMode = BrushMappingMode.Absolute;
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(4, 4);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x70, 0x70, 0x70), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x76, 0x76, 0x76), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCB, 0xCB, 0xCB), 1));

                            temp.Freeze();
                            _commonButtonGlyph = temp;
                        }
                    }
                }
                return _commonButtonGlyph;
            }
        }

        private LinearGradientBrush CommonButtonEnabledGlyph
        {
            get
            {
                if (_commonButtonEnabledGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonButtonEnabledGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.MappingMode = BrushMappingMode.Absolute;
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(4, 4);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x21, 0x21, 0x21), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x57, 0x57, 0x57), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xB3, 0xB3, 0xB3), 1));

                            temp.Freeze();
                            _commonButtonEnabledGlyph = temp;
                        }
                    }
                }
                return _commonButtonEnabledGlyph;
            }
        }

        private LinearGradientBrush CommonButtonHoverGlyph
        {
            get
            {
                if (_commonButtonHoverGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonButtonHoverGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.MappingMode = BrushMappingMode.Absolute;
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(4, 4);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x0D, 0x2A, 0x3A), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x1F, 0x63, 0x8A), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x2E, 0x97, 0xCF), 1));

                            temp.Freeze();
                            _commonButtonHoverGlyph = temp;
                        }
                    }
                }
                return _commonButtonHoverGlyph;
            }
        }

        private static LinearGradientBrush CommonButtonPressedGlyph
        {
            get
            {
                if (_commonButtonPressedGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonButtonPressedGlyph == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.MappingMode = BrushMappingMode.Absolute;
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(4, 4);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x0E, 0x22, 0x2D), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x2F, 0x79, 0x9E), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x6B, 0xA0, 0xBC), 1));

                            temp.Freeze();
                            _commonButtonPressedGlyph = temp;
                        }
                    }
                }
                return _commonButtonPressedGlyph;
            }
        }

        private LinearGradientBrush Glyph
        {
            get
            {
                if (!Animates)
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper)
                    {
                        if (RenderPressed)
                        {
                            return CommonHorizontalThumbPressedGlyph;
                        }
                        else if (RenderMouseOver)
                        {
                            return CommonHorizontalThumbHoverGlyph;
                        }
                        else 
                        {
                            return CommonHorizontalThumbEnabledGlyph;
                        }
                    }
                    else if (_scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        if (RenderPressed)
                        {
                            return CommonVerticalThumbPressedGlyph;
                        }
                        else if (RenderMouseOver)
                        {
                            return CommonVerticalThumbHoverGlyph;
                        }
                        else 
                        {
                            return CommonVerticalThumbEnabledGlyph;
                        }
                    }
                    else
                    {
                        if (RenderPressed)
                        {
                            return CommonButtonPressedGlyph;
                        }
                        else if (RenderMouseOver)
                        {
                            return CommonButtonHoverGlyph;
                        }
                        else if (IsEnabled)
                        {
                            return CommonButtonEnabledGlyph;
                        }
                        else
                        {
                            return CommonButtonGlyph;
                        }
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.Glyph == null)
                    {
                        if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                            _scrollGlyph == ScrollGlyph.VerticalGripper)
                        {
                            _localResources.Glyph = new LinearGradientBrush();
                            _localResources.Glyph.StartPoint = new Point(0, 0);
                            if (_scrollGlyph == ScrollGlyph.HorizontalGripper)
                            {
                                _localResources.Glyph.EndPoint = new Point(1, 0.05);
                            }
                            else
                            {
                                _localResources.Glyph.EndPoint = new Point(0.05, 1);
                            }

                            _localResources.Glyph.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x00, 0x00), 0.5));
                            _localResources.Glyph.GradientStops.Add(new GradientStop(Color.FromRgb(0x97, 0x97, 0x97), 0.7));
                            _localResources.Glyph.GradientStops.Add(new GradientStop(Color.FromRgb(0xCA, 0xCA, 0xCA), 1));
                        }
                        else
                        {
                            _localResources.Glyph = CommonButtonGlyph.Clone();
                        }
                    }
                    return _localResources.Glyph;
                }
                else
                {
                    if (_scrollGlyph == ScrollGlyph.HorizontalGripper)
                    {
                        return CommonHorizontalThumbEnabledGlyph;
                    }
                    else if (_scrollGlyph == ScrollGlyph.VerticalGripper)
                    {
                        return CommonVerticalThumbEnabledGlyph;
                    }
                    else
                    {
                        return CommonButtonGlyph;
                    }
                }
            }
        }


        private static SolidColorBrush CommonThumbEnabledGlyphShadow
        {
            get
            {
                if (_commonThumbEnabledGlyphShadow == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbEnabledGlyphShadow == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Colors.White);
                            temp.Opacity = 0.63;

                            temp.Freeze();

                            _commonThumbEnabledGlyphShadow = temp;
                        }
                    }
                }
                return _commonThumbEnabledGlyphShadow;
            }
        }

        private SolidColorBrush GlyphShadow
        {
            get
            {
                if (!Animates)
                {
                    if ((_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                        _scrollGlyph == ScrollGlyph.VerticalGripper))
                    {
                        return CommonThumbEnabledGlyphShadow;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.GlyphShadow == null)
                    {
                        if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                            _scrollGlyph == ScrollGlyph.VerticalGripper)
                        {
                            _localResources.GlyphShadow = new SolidColorBrush(Colors.White);
                        }
                    }
                    return _localResources.GlyphShadow;
                }
                else
                {
                    return null;
                }
            }
        }

        // Common Resources
        private static LinearGradientBrush _commonHorizontalThumbFill;
        private static LinearGradientBrush _commonVerticalThumbFill;
        private static Pen _commonThumbOuterBorder;
        private static Pen _commonThumbInnerBorder;
        private static Pen _commonThumbShadow;
        private static LinearGradientBrush _commonHorizontalThumbEnabledGlyph;
        private static LinearGradientBrush _commonVerticalThumbEnabledGlyph;
        private static SolidColorBrush _commonThumbEnabledGlyphShadow;

        private static LinearGradientBrush _commonHorizontalThumbHoverFill;
        private static LinearGradientBrush _commonVerticalThumbHoverFill;
        private static Pen _commonThumbHoverOuterBorder;
        private static LinearGradientBrush _commonHorizontalThumbHoverGlyph;
        private static LinearGradientBrush _commonVerticalThumbHoverGlyph;

        private static LinearGradientBrush _commonHorizontalThumbPressedFill;
        private static LinearGradientBrush _commonVerticalThumbPressedFill;
        private static Pen _commonThumbPressedOuterBorder;
        private static LinearGradientBrush _commonHorizontalThumbPressedGlyph;
        private static LinearGradientBrush _commonVerticalThumbPressedGlyph;

        private static LinearGradientBrush _commonButtonGlyph;
        private static LinearGradientBrush _commonButtonEnabledGlyph;
        private static LinearGradientBrush _commonButtonHoverGlyph;
        private static LinearGradientBrush _commonButtonPressedGlyph;

        
        private static object _resourceAccess = new object();

        // Per instance data
        private ScrollGlyph _scrollGlyph;
        private MatrixTransform _transform;
        private LocalResources _localResources;

        private class LocalResources
        {
            public LinearGradientBrush Fill;
            public Pen OuterBorder;
            public Pen InnerBorder;
            public Pen Shadow;
            public LinearGradientBrush Glyph;
            public SolidColorBrush GlyphShadow;
        }

        #endregion
    }
}

