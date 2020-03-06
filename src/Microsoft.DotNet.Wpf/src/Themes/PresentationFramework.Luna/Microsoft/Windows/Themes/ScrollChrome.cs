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
using MS.Internal;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    ///     The ScrollChrome element
    ///     This element is a theme-specific type that is used as an optimization
    ///     for a common complex rendering used in Luna
    /// </summary>
    public sealed class ScrollChrome : FrameworkElement
    {

        #region Constructors

        static ScrollChrome()
        {
            IsEnabledProperty.OverrideMetadata(typeof(ScrollChrome), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
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
        public static readonly DependencyProperty ThemeColorProperty
            = ButtonChrome.ThemeColorProperty.AddOwner(typeof(ScrollChrome),
                                new FrameworkPropertyMetadata(
                                ThemeColor.NormalColor,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The color variation of the button.
        /// </summary>
        public ThemeColor ThemeColor
        {
            get { return (ThemeColor)GetValue(ThemeColorProperty); }
            set { SetValue(ThemeColorProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="HasOuterBorder" /> property.
        /// </summary>
        public static readonly DependencyProperty HasOuterBorderProperty =
                 DependencyProperty.Register("HasOuterBorder",
                         typeof(bool),
                         typeof(ScrollChrome),
                         new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true the chrome renders with a white outer border.
        /// </summary>
        public bool HasOuterBorder
        {
            get { return (bool)GetValue(HasOuterBorderProperty); }
            set { SetValue(HasOuterBorderProperty, value); }
        }


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
        /// <param name="element">The object to which the property is attached.</param>
        /// <returns>The value of the property.</returns>
        public static ScrollGlyph GetScrollGlyph(DependencyObject element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }

            return (ScrollGlyph)element.GetValue(ScrollGlyphProperty);
        }

        /// <summary>
        ///     Attachs the value to the object.
        /// </summary>
        /// <param name="element">The object on which the value will be attached.</param>
        /// <param name="value">The value to attach.</param>
        public static void SetScrollGlyph(DependencyObject element, ScrollGlyph value)
        {
            if (element == null) { throw new ArgumentNullException("element"); }

            element.SetValue(ScrollGlyphProperty, value);
        }

        private ScrollGlyph ScrollGlyph
        {
            get { return (ScrollGlyph)GetValue(ScrollGlyphProperty); }
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

        /// <summary>
        ///     DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty = Control.PaddingProperty.AddOwner(typeof(ScrollChrome));

        /// <summary>
        ///     Insets the border of the element.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="RenderMouseOver" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderMouseOverProperty
            = ButtonChrome.RenderMouseOverProperty.AddOwner(typeof(ScrollChrome),
                                new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true the chrome renders with a mouse over look.
        /// </summary>
        public bool RenderMouseOver
        {
            get { return (bool)GetValue(RenderMouseOverProperty); }
            set { SetValue(RenderMouseOverProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="RenderPressed" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderPressedProperty
            = ButtonChrome.RenderPressedProperty.AddOwner(typeof(ScrollChrome),
                                new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true the chrome renders with a pressed look.
        /// </summary>
        public bool RenderPressed
        {
            get { return (bool)GetValue(RenderPressedProperty); }
            set { SetValue(RenderPressedProperty, value); }
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

            return new Size(0, 0);
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
            ScrollGlyph glyph = ScrollGlyph;
            bool hasModifiers;

            // Padding will be used to inset the border - used by ComboBox buttons
            if (GetValueSource(PaddingProperty, null, out hasModifiers) != BaseValueSourceInternal.Default || hasModifiers)
            {
                Thickness padding = Padding;

                double totalPadding = padding.Left + padding.Right;
                if (totalPadding >= bounds.Width)
                {
                    bounds.Width = 0.0;
                }
                else
                {
                    bounds.X += padding.Left;
                    bounds.Width -= totalPadding;
                }

                totalPadding = padding.Top + padding.Bottom;
                if (totalPadding >= bounds.Height)
                {
                    bounds.Height = 0.0;
                }
                else
                {
                    bounds.Y += padding.Top;
                    bounds.Height -= totalPadding;
                }
            }

            if ((bounds.Width >= 1.0) && (bounds.Height >= 1.0))
            {
                bounds.X += 0.5;
                bounds.Y += 0.5;
                bounds.Width -= 1.0;
                bounds.Height -= 1.0;
            }

            switch (glyph)
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
            DrawGlyph(drawingContext, glyph, ref bounds);
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

                    bounds.Height -= 1.0;
                    bounds.Width = Math.Max(0.0, bounds.Width - 1.0);
                }
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
                        2.0, 2.0);
                    brush = null; // Done with the fill
                    bounds.Inflate(-1.0, -1.0);
                }

                if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
                {
                    pen = InnerBorder;
                    if ((pen != null) || (brush != null))
                    {
                        dc.DrawRoundedRectangle(
                            brush,
                            pen,
                            new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                            1.5, 1.5);
                        bounds.Inflate(-1.0, -1.0);
                    }
                }
            }
        }

        private void DrawGlyph(DrawingContext dc, ScrollGlyph glyph, ref Rect bounds)
        {

            if ((bounds.Width > 0.0) && (bounds.Height > 0.0))
            {
                Brush brush = Glyph;

                if ((brush != null) && (glyph != ScrollGlyph.None))
                {
                    switch (glyph)
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
                            DrawArrow(dc, brush, bounds, glyph);
                            break;
                    }
                }
            }
        }

        private void DrawHorizontalGripper(DrawingContext dc, Brush brush, Rect bounds)
        {
            if ((bounds.Width > 8.0) && (bounds.Height > 2.0))
            {
                Brush glyphShadow = GlyphShadow;

                double height = Math.Min(6.0, bounds.Height);
                double x = bounds.X + ((bounds.Width * 0.5) - 4.0);
                double y = bounds.Y + ((bounds.Height - height) * 0.5);

                height -= 1.0;
                for (int i = 0; i < 8; i += 2)
                {
                    dc.DrawRectangle(brush, null, new Rect(x + i, y, 1.0, height));
                    if (glyphShadow != null)
                    {
                        dc.DrawRectangle(glyphShadow, null, new Rect(x + i + 1, y + 1, 1.0, height));
                    }
                }
            }
        }

        private void DrawVerticalGripper(DrawingContext dc, Brush brush, Rect bounds)
        {
            if ((bounds.Width > 2.0) && (bounds.Height > 8.0))
            {
                Brush glyphShadow = GlyphShadow;

                double width = Math.Min(6.0, bounds.Width);
                double x = bounds.X + ((bounds.Width - width) * 0.5);
                double y = bounds.Y + ((bounds.Height * 0.5) - 4.0);

                width -= 1.0;
                for (int i = 0; i < 8; i += 2)
                {
                    dc.DrawRectangle(brush, null, new Rect(x, y + i, width, 1.0));
                    if (glyphShadow != null)
                    {
                        dc.DrawRectangle(glyphShadow, null, new Rect(x + 1, y + i + 1, width, 1.0));
                    }
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
                            figure.StartPoint = new Point(4.5, 0.0);
                            figure.Segments.Add(new LineSegment(new Point(0.0, 4.5), true));
                            figure.Segments.Add(new LineSegment(new Point(4.5, 9.0), true));
                            figure.Segments.Add(new LineSegment(new Point(6.0, 7.5), true));
                            figure.Segments.Add(new LineSegment(new Point(3.0, 4.5), true));
                            figure.Segments.Add(new LineSegment(new Point(6.0, 1.5), true));
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
                            figure.StartPoint = new Point(3.5, 0.0);
                            figure.Segments.Add(new LineSegment(new Point(8.0, 4.5), true));
                            figure.Segments.Add(new LineSegment(new Point(3.5, 9.0), true));
                            figure.Segments.Add(new LineSegment(new Point(2.0, 7.5), true));
                            figure.Segments.Add(new LineSegment(new Point(5.0, 4.5), true));
                            figure.Segments.Add(new LineSegment(new Point(2.0, 1.5), true));
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
                            figure.StartPoint = new Point(0.0, 4.5);
                            figure.Segments.Add(new LineSegment(new Point(4.5, 0.0), true));
                            figure.Segments.Add(new LineSegment(new Point(9.0, 4.5), true));
                            figure.Segments.Add(new LineSegment(new Point(7.5, 6.0), true));
                            figure.Segments.Add(new LineSegment(new Point(4.5, 3.0), true));
                            figure.Segments.Add(new LineSegment(new Point(1.5, 6.0), true));
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
                            figure.StartPoint = new Point(0.0, 3.5);
                            figure.Segments.Add(new LineSegment(new Point(4.5, 8.0), true));
                            figure.Segments.Add(new LineSegment(new Point(9.0, 3.5), true));
                            figure.Segments.Add(new LineSegment(new Point(7.5, 2.0), true));
                            figure.Segments.Add(new LineSegment(new Point(4.5, 5.0), true));
                            figure.Segments.Add(new LineSegment(new Point(1.5, 2.0), true));
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

        private void DrawArrow(DrawingContext dc, Brush brush, Rect bounds, ScrollGlyph glyph)
        {
            if (_transform == null)
            {
                double glyphWidth = 9.0;
                double glyphHeight = 9.0;
                switch (glyph)
                {
                    case ScrollGlyph.LeftArrow:
                    case ScrollGlyph.RightArrow:
                        glyphWidth = 8.0;
                        break;

                    case ScrollGlyph.UpArrow:
                    case ScrollGlyph.DownArrow:
                        glyphHeight = 8.0;
                        break;
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

            switch (glyph)
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
            get { return 9; }
        }



        private static LinearGradientBrush CommonLineButtonFillNC
        {
            get
            {
                if (_commonLineButtonFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonLineButtonFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE1, 0xEA, 0xFE), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xD3, 0xFD), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xD3, 0xFD), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBB, 0xCD, 0xF9), 1));
                            temp.Freeze();

                            _commonLineButtonFillNC = temp;
                        }
                    }
                }
                return _commonLineButtonFillNC;
            }
        }

        private static LinearGradientBrush CommonVerticalFillNC
        {
            get
            {
                if (_commonVerticalFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC9, 0xD8, 0xFC), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC2, 0xD3, 0xFC), 0.65));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB6, 0xCD, 0xFB), 1));
                            temp.Freeze();

                            _commonVerticalFillNC = temp;
                        }
                    }
                }
                return _commonVerticalFillNC;
            }
        }

        private static LinearGradientBrush CommonHorizontalFillNC
        {
            get
            {
                if (_commonHorizontalFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC9, 0xD8, 0xFC), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC2, 0xD3, 0xFC), 0.65));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB6, 0xCD, 0xFB), 1));
                            temp.Freeze();

                            _commonHorizontalFillNC = temp;
                        }
                    }
                }
                return _commonHorizontalFillNC;
            }
        }

        private static LinearGradientBrush CommonHoverLineButtonFillNC
        {
            get
            {
                if (_commonHoverLineButtonFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverLineButtonFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFD, 0xFF, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE2, 0xF3, 0xFD), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB9, 0xDA, 0xFB), 1));
                            temp.Freeze();

                            _commonHoverLineButtonFillNC = temp;
                        }
                    }
                }
                return _commonHoverLineButtonFillNC;
            }
        }

        private static LinearGradientBrush CommonHoverVerticalFillNC
        {
            get
            {
                if (_commonHoverVerticalFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverVerticalFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDA, 0xE9, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD4, 0xE6, 0xFF), 0.65));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCA, 0xE0, 0xFF), 1));
                            temp.Freeze();

                            _commonHoverVerticalFillNC = temp;
                        }
                    }
                }
                return _commonHoverVerticalFillNC;
            }
        }

        private static LinearGradientBrush CommonHoverHorizontalFillNC
        {
            get
            {
                if (_commonHoverHorizontalFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverHorizontalFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);


                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDA, 0xE9, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD4, 0xE6, 0xFF), 0.65));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCA, 0xE0, 0xFF), 1));
                            temp.Freeze();

                            _commonHoverHorizontalFillNC = temp;
                        }
                    }
                }
                return _commonHoverHorizontalFillNC;
            }
        }

        private static LinearGradientBrush CommonPressedLineButtonFillNC
        {
            get
            {
                if (_commonPressedLineButtonFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedLineButtonFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x6E, 0x8E, 0xF1), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x80, 0x9D, 0xF1), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xAF, 0xBF, 0xED), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD2, 0xDE, 0xEB), 1));
                            temp.Freeze();

                            _commonPressedLineButtonFillNC = temp;
                        }
                    }
                }
                return _commonPressedLineButtonFillNC;
            }
        }

        private static LinearGradientBrush CommonPressedVerticalFillNC
        {
            get
            {
                if (_commonPressedVerticalFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedVerticalFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA8, 0xBE, 0xF5), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA1, 0xBD, 0xFA), 0.65));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x98, 0xB0, 0xEE), 1));
                            temp.Freeze();

                            _commonPressedVerticalFillNC = temp;
                        }
                    }
                }
                return _commonPressedVerticalFillNC;
            }
        }

        private static LinearGradientBrush CommonPressedHorizontalFillNC
        {
            get
            {
                if (_commonPressedHorizontalFillNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedHorizontalFillNC == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA8, 0xBE, 0xF5), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA1, 0xBD, 0xFA), 0.65));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x98, 0xB0, 0xEE), 1));
                            temp.Freeze();

                            _commonPressedHorizontalFillNC = temp;
                        }
                    }
                }
                return _commonPressedHorizontalFillNC;
            }
        }

        private static LinearGradientBrush CommonVerticalFillHS
        {
            get
            {
                if (_commonVerticalFillHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalFillHS == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA2, 0xB3, 0x8D), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA5, 0xB7, 0x8E), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA5, 0xB7, 0x8E), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x95, 0xA7, 0x75), .82));
                            temp.Freeze();

                            _commonVerticalFillHS = temp;
                        }
                    }
                }
                return _commonVerticalFillHS;
            }
        }

        private static LinearGradientBrush CommonHorizontalFillHS
        {
            get
            {
                if (_commonHorizontalFillHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalFillHS == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA2, 0xB3, 0x8D), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA5, 0xB7, 0x8E), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA5, 0xB7, 0x8E), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x95, 0xA7, 0x75), .82));
                            temp.Freeze();

                            _commonHorizontalFillHS = temp;
                        }
                    }
                }
                return _commonHorizontalFillHS;
            }
        }

        private static LinearGradientBrush CommonHoverVerticalFillHS
        {
            get
            {
                if (_commonHoverVerticalFillHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverVerticalFillHS == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCA, 0xD7, 0xA7), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCB, 0xD9, 0xA9), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCB, 0xD9, 0xA9), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xD0, 0x96), .82));
                            temp.Freeze();

                            _commonHoverVerticalFillHS = temp;
                        }
                    }
                }
                return _commonHoverVerticalFillHS;
            }
        }


        private static LinearGradientBrush CommonHoverHorizontalFillHS
        {
            get
            {
                if (_commonHoverHorizontalFillHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverHorizontalFillHS == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCA, 0xD7, 0xA7), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCB, 0xD9, 0xA9), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCB, 0xD9, 0xA9), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xD0, 0x96), .82)); 
                            temp.Freeze();

                            _commonHoverHorizontalFillHS = temp;
                        }
                    }
                }
                return _commonHoverHorizontalFillHS;
            }
        }

        private static LinearGradientBrush CommonPressedVerticalFillHS
        {
            get
            {
                if (_commonPressedVerticalFillHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedVerticalFillHS == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x92, 0xA4, 0x7A), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x9A, 0xAD, 0x80), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x9A, 0xAD, 0x80), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x95, 0xAA, 0x72), 0.8));
                            temp.Freeze();

                            _commonPressedVerticalFillHS = temp;
                        }
                    }
                }
                return _commonPressedVerticalFillHS;
            }
        }

        private static LinearGradientBrush CommonPressedHorizontalFillHS
        {
            get
            {
                if (_commonPressedHorizontalFillHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedHorizontalFillHS == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x92, 0xA4, 0x7A), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x9A, 0xAD, 0x80), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x9A, 0xAD, 0x80), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x95, 0xAA, 0x72), 0.8));
                            temp.Freeze();

                            _commonPressedHorizontalFillHS = temp;
                        }
                    }
                }
                return _commonPressedHorizontalFillHS;
            }
        }

        private static LinearGradientBrush CommonVerticalFillM
        {
            get
            {
                if (_commonVerticalFillM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalFillM == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE9, 0xE9, 0xEE), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE9, 0xE9, 0xEE), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDB, 0xDB, 0xE6), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC7, 0xC8, 0xD6), 0.9));
                            temp.Freeze();

                            _commonVerticalFillM = temp;
                        }
                    }
                }
                return _commonVerticalFillM;
            }
        }

        private static LinearGradientBrush CommonHorizontalFillM
        {
            get
            {
                if (_commonHorizontalFillM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalFillM == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 0);
                            temp.EndPoint = new Point(0.5, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE9, 0xE9, 0xEE), 0.25));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE9, 0xE9, 0xEE), 0.4));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDB, 0xDB, 0xE6), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC7, 0xC8, 0xD6), 0.9));
                            temp.Freeze();

                            _commonHorizontalFillM = temp;
                        }
                    }
                }
                return _commonHorizontalFillM;
            }
        }

        

        private static LinearGradientBrush CommonHoverVerticalFillM
        {
            get
            {
                if (_commonHoverVerticalFillM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverVerticalFillM == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.18));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.45));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDC, 0xDD, 0xE8), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xC5, 0xD6), 1)); 
                            temp.Freeze();

                            _commonHoverVerticalFillM = temp;
                        }
                    }
                }
                return _commonHoverVerticalFillM;
            }
        }

        private static LinearGradientBrush CommonHoverHorizontalFillM
        {
            get
            {
                if (_commonHoverHorizontalFillM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverHorizontalFillM == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 0);
                            temp.EndPoint = new Point(0.5, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.18));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.45));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDC, 0xDD, 0xE8), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xC5, 0xD6), 1)); 
                            temp.Freeze();

                            _commonHoverHorizontalFillM = temp;
                        }
                    }
                }
                return _commonHoverHorizontalFillM;
            }
        }


        private static LinearGradientBrush CommonPressedVerticalFillM
        {
            get
            {
                if (_commonPressedVerticalFillM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedVerticalFillM == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.12)); 
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xC5, 0xD6), 0.12));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDC, 0xDD, 0xE8), 0.48));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.58));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.9));
                            temp.Freeze();

                            _commonPressedVerticalFillM = temp;
                        }
                    }
                }
                return _commonPressedVerticalFillM;
            }
        }

        private static LinearGradientBrush CommonPressedHorizontalFillM
        {
            get
            {
                if (_commonPressedHorizontalFillM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedHorizontalFillM == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 0);
                            temp.EndPoint = new Point(0.5, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.12));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xC5, 0xD6), 0.12));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDC, 0xDD, 0xE8), 0.48));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.58));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xED, 0xF4), 0.7));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.9));
                            temp.Freeze();

                            _commonPressedHorizontalFillM = temp;
                        }
                    }
                }
                return _commonPressedHorizontalFillM;
            }
        }

        private static LinearGradientBrush CommonDisabledFill
        {
            get
            {
                if (_commonDisabledFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF7, 0xF7, 0xF7), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEC), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE3, 0xE3, 0xE3), 1));
                            temp.Freeze();

                            _commonDisabledFill = temp;
                        }
                    }
                }
                return _commonDisabledFill;
            }
        }


        private Brush Fill
        {
            get
            {
                if (!IsEnabled)
                    return CommonDisabledFill;

                ScrollGlyph glyph = ScrollGlyph;
                ThemeColor themeColor = ThemeColor;

                if (glyph == ScrollGlyph.VerticalGripper)
                {
                    if (RenderPressed)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonPressedVerticalFillNC;
                        else if (themeColor == ThemeColor.Homestead)
                            return CommonPressedVerticalFillHS;
                        else
                            return CommonPressedVerticalFillM;
                    }

                    if (RenderMouseOver)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonHoverVerticalFillNC;
                        else if (themeColor == ThemeColor.Homestead)
                            return CommonHoverVerticalFillHS;
                        else
                            return CommonHoverVerticalFillM;
                    }

                    if (themeColor == ThemeColor.NormalColor)
                        return CommonVerticalFillNC;
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonVerticalFillHS;
                    else
                        return CommonVerticalFillM;
                }
                else if (glyph == ScrollGlyph.HorizontalGripper)
                {
                    if (RenderPressed)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonPressedHorizontalFillNC;
                        else if (themeColor == ThemeColor.Homestead)
                            return CommonPressedHorizontalFillHS;
                        else
                            return CommonPressedHorizontalFillM;
                    }

                    if (RenderMouseOver)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonHoverHorizontalFillNC;
                        else if (themeColor == ThemeColor.Homestead)
                            return CommonHoverHorizontalFillHS;
                        else
                            return CommonHoverHorizontalFillM;
                    }

                    if (themeColor == ThemeColor.NormalColor)
                        return CommonHorizontalFillNC;
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonHorizontalFillHS;
                    else
                        return CommonHorizontalFillM;
                }
                else
                {
                    if (RenderPressed)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonPressedLineButtonFillNC;
                        else if (themeColor == ThemeColor.Homestead)
                            return CommonPressedHorizontalFillHS;
                        else
                            return CommonPressedHorizontalFillM;
                    }

                    if (RenderMouseOver)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonHoverLineButtonFillNC;
                        else if (themeColor == ThemeColor.Homestead)
                            return CommonHoverHorizontalFillHS;
                        else
                            return CommonHoverHorizontalFillM;
                    }

                    if (themeColor == ThemeColor.NormalColor)
                        return CommonLineButtonFillNC;
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonHorizontalFillHS;
                    else
                        return CommonHorizontalFillM;
                }
            }
        }


        private static SolidColorBrush CommonDisabledGlyph
        {
            get
            {
                if (_commonDisabledGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledGlyph == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xC9, 0xC9, 0xC2));
                            temp.Freeze();

                            _commonDisabledGlyph = temp;
                        }
                    }
                }
                return _commonDisabledGlyph;
            }
        }

        private static SolidColorBrush CommonArrowGlyphNC
        {
            get
            {
                if (_commonArrowGlyphNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonArrowGlyphNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x4D, 0x61, 0x85));
                            temp.Freeze();

                            _commonArrowGlyphNC = temp;
                        }
                    }
                }
                return _commonArrowGlyphNC;
            }
        }

        private static SolidColorBrush CommonArrowGlyphHS
        {
            get
            {
                return Brushes.White;
            }
        }

        private static SolidColorBrush CommonArrowGlyphM
        {
            get
            {
                if (_commonArrowGlyphM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonArrowGlyphM == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x3F, 0x3D, 0x3D));
                            temp.Freeze();

                            _commonArrowGlyphM = temp;
                        }
                    }
                }
                return _commonArrowGlyphM;
            }
        }

        private static SolidColorBrush CommonHoverArrowGlyphM
        {
            get
            {
                if (_commonHoverArrowGlyphM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverArrowGlyphM == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20));
                            temp.Freeze();

                            _commonHoverArrowGlyphM = temp;
                        }
                    }
                }
                return _commonHoverArrowGlyphM;
            }
        }

        private static SolidColorBrush CommonGripperGlyphNC
        {
            get
            {
                if (_commonGripperGlyphNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xEE, 0xF4, 0xFE));
                            temp.Freeze();

                            _commonGripperGlyphNC = temp;
                        }
                    }
                }
                return _commonGripperGlyphNC;
            }
        }

        private static SolidColorBrush CommonHoverGripperGlyphNC
        {
            get
            {
                if (_commonHoverGripperGlyphNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverGripperGlyphNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xFD, 0xFF));
                            temp.Freeze();

                            _commonHoverGripperGlyphNC = temp;
                        }
                    }
                }
                return _commonHoverGripperGlyphNC;
            }
        }

        private static SolidColorBrush CommonPressedGripperGlyphNC
        {
            get
            {
                if (_commonPressedGripperGlyphNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedGripperGlyphNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xCF, 0xDD, 0xFD));
                            temp.Freeze();

                            _commonPressedGripperGlyphNC = temp;
                        }
                    }
                }
                return _commonPressedGripperGlyphNC;
            }
        }

        private static SolidColorBrush CommonGripperGlyphHS
        {
            get
            {
                if (_commonGripperGlyphHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphHS == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xD0, 0xDF, 0xAC));
                            temp.Freeze();

                            _commonGripperGlyphHS = temp;
                        }
                    }
                }
                return _commonGripperGlyphHS;
            }
        }

        private static SolidColorBrush CommonHoverGripperGlyphHS
        {
            get
            {
                if (_commonHoverGripperGlyphHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverGripperGlyphHS == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xEB, 0xF5, 0xD4));
                            temp.Freeze();

                            _commonHoverGripperGlyphHS = temp;
                        }
                    }
                }
                return _commonHoverGripperGlyphHS;
            }
        }

        private static SolidColorBrush CommonPressedGripperGlyphHS
        {
            get
            {
                if (_commonPressedGripperGlyphHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedGripperGlyphHS == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xB9, 0xD0, 0x97));
                            temp.Freeze();

                            _commonPressedGripperGlyphHS = temp;
                        }
                    }
                }
                return _commonPressedGripperGlyphHS;
            }
        }

        private static SolidColorBrush CommonGripperGlyphM
        {
            get
            {
                if (_commonGripperGlyphM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphM == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonGripperGlyphM = temp;
                        }
                    }
                }
                return _commonGripperGlyphM;
            }
        }

        private Brush Glyph
        {
            get
            {
                if (!IsEnabled)
                    return CommonDisabledGlyph;

                ThemeColor themeColor = ThemeColor;
                ScrollGlyph scrollGlyph = ScrollGlyph;

                if (scrollGlyph == ScrollGlyph.HorizontalGripper || scrollGlyph == ScrollGlyph.VerticalGripper)
                {
                    if (themeColor == ThemeColor.NormalColor)
                    {
                        if (RenderPressed)
                            return CommonPressedGripperGlyphNC;
                        else if (RenderMouseOver)
                            return CommonHoverGripperGlyphNC;
                        else
                            return CommonGripperGlyphNC;
                    }
                    else if (themeColor == ThemeColor.Homestead)
                    {
                        if (RenderPressed)
                            return CommonPressedGripperGlyphHS;
                        else if (RenderMouseOver)
                            return CommonHoverGripperGlyphHS;
                        else
                            return CommonGripperGlyphHS;
                    }
                    else
                    {
                        return CommonGripperGlyphM;
                    }
                }
                else
                {
                    if (themeColor == ThemeColor.NormalColor)
                        return CommonArrowGlyphNC;
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonArrowGlyphHS;
                    else
                    {
                        if (RenderMouseOver || RenderPressed)
                            return CommonHoverArrowGlyphM;
                        else
                            return CommonArrowGlyphM;
                    }
                }
            }
        }




        private static SolidColorBrush CommonDisabledGripperGlyphShadow
        {
            get
            {
                if (_commonDisabledGripperGlyphShadow == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledGripperGlyphShadow == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xB9, 0xB9, 0xB2));
                            temp.Freeze();

                            _commonDisabledGripperGlyphShadow = temp;
                        }
                    }
                }
                return _commonDisabledGripperGlyphShadow;
            }
        }

        private static SolidColorBrush CommonGripperGlyphShadowNC
        {
            get
            {
                if (_commonGripperGlyphShadowNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphShadowNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x8C, 0xB0, 0xF8));
                            temp.Freeze();

                            _commonGripperGlyphShadowNC = temp;
                        }
                    }
                }
                return _commonGripperGlyphShadowNC;
            }
        }

        private static SolidColorBrush CommonHoverGripperGlyphShadowNC
        {
            get
            {
                if (_commonHoverGripperGlyphShadowNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverGripperGlyphShadowNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x9C, 0xC5, 0xFF));
                            temp.Freeze();

                            _commonHoverGripperGlyphShadowNC = temp;
                        }
                    }
                }
                return _commonHoverGripperGlyphShadowNC;
            }
        }

        private static SolidColorBrush CommonPressedGripperGlyphShadowNC
        {
            get
            {
                if (_commonPressedGripperGlyphShadowNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedGripperGlyphShadowNC == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x83, 0x9E, 0xD8));
                            temp.Freeze();

                            _commonPressedGripperGlyphShadowNC = temp;
                        }
                    }
                }
                return _commonPressedGripperGlyphShadowNC;
            }
        }

        private static SolidColorBrush CommonGripperGlyphShadowHS
        {
            get
            {
                if (_commonGripperGlyphShadowHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphShadowHS == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x8C, 0x9D, 0x73));
                            temp.Freeze();

                            _commonGripperGlyphShadowHS = temp;
                        }
                    }
                }
                return _commonGripperGlyphShadowHS;
            }
        }

        private static SolidColorBrush CommonHoverGripperGlyphShadowHS
        {
            get
            {
                if (_commonHoverGripperGlyphShadowHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverGripperGlyphShadowHS == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xB6, 0xC6, 0x8E));
                            temp.Freeze();

                            _commonHoverGripperGlyphShadowHS = temp;
                        }
                    }
                }
                return _commonHoverGripperGlyphShadowHS;
            }
        }

        private static SolidColorBrush CommonPressedGripperGlyphShadowHS
        {
            get
            {
                if (_commonPressedGripperGlyphShadowHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedGripperGlyphShadowHS == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x7A, 0x8B, 0x63));
                            temp.Freeze();

                            _commonPressedGripperGlyphShadowHS = temp;
                        }
                    }
                }
                return _commonPressedGripperGlyphShadowHS;
            }
        }

        private static SolidColorBrush CommonGripperGlyphShadowM
        {
            get
            {
                if (_commonGripperGlyphShadowM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphShadowM == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x8E, 0x95, 0xA2));
                            temp.Freeze();

                            _commonGripperGlyphShadowM = temp;
                        }
                    }
                }
                return _commonGripperGlyphShadowM;
            }
        }


        private Brush GlyphShadow
        {
            get
            {
                ScrollGlyph scrollGlyph = ScrollGlyph;

                if (scrollGlyph == ScrollGlyph.HorizontalGripper || scrollGlyph == ScrollGlyph.VerticalGripper)
                {
                    if (!IsEnabled)
                        return CommonDisabledGripperGlyphShadow;

                    ThemeColor themeColor = ThemeColor;

                    if (themeColor == ThemeColor.NormalColor)
                    {
                        if (RenderPressed)
                            return CommonPressedGripperGlyphShadowNC;
                        else if (RenderMouseOver)
                            return CommonHoverGripperGlyphShadowNC;
                        else
                            return CommonGripperGlyphShadowNC;
                    }
                    else if (themeColor == ThemeColor.Homestead)
                    {
                        if (RenderPressed)
                            return CommonPressedGripperGlyphShadowHS;
                        else if (RenderMouseOver)
                            return CommonHoverGripperGlyphShadowHS;
                        else
                            return CommonGripperGlyphShadowHS;
                    }
                    else
                    {
                        return CommonGripperGlyphShadowM;
                    }
                }
                return null;
            }
        }

        private static Pen CommonOuterBorderPenNC
        {
            get
            {
                if (_commonOuterBorderPenNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonOuterBorderPenNC == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonOuterBorderPenNC = temp;
                        }
                    }
                }
                return _commonOuterBorderPenNC;
            }
        }

        private static Pen CommonOuterBorderPenHS
        {
            get
            {
                if (_commonOuterBorderPenHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonOuterBorderPenHS == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonOuterBorderPenHS = temp;
                        }
                    }
                }
                return _commonOuterBorderPenHS;
            }
        }

        private static Pen CommonOuterBorderPenM
        {
            get
            {
                if (_commonOuterBorderPenM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonOuterBorderPenM == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x94, 0x95, 0xA2));
                            temp.Freeze();

                            _commonOuterBorderPenM = temp;
                        }
                    }
                }
                return _commonOuterBorderPenM;
            }
        }

        private static Pen CommonHoverOuterBorderPenM
        {
            get
            {
                if (_commonHoverOuterBorderPenM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverOuterBorderPenM == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x66, 0x65));
                            temp.Freeze();

                            _commonHoverOuterBorderPenM = temp;
                        }
                    }
                }
                return _commonHoverOuterBorderPenM;
            }
        }

        private static Pen CommonPressedOuterBorderPenM
        {
            get
            {
                if (_commonPressedOuterBorderPenM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedOuterBorderPenM == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x43, 0x48, 0x48));
                            temp.Freeze();

                            _commonPressedOuterBorderPenM = temp;
                        }
                    }
                }
                return _commonPressedOuterBorderPenM;
            }
        }

        
        private Pen OuterBorder
        {
            get
            {
                ThemeColor themeColor = ThemeColor;

                if (themeColor == ThemeColor.Metallic)
                {
                    ScrollGlyph glyph = ScrollGlyph;
                    if (!IsEnabled)
                        return CommonOuterBorderPenNC;
                    else if (RenderPressed && (glyph == ScrollGlyph.HorizontalGripper || glyph == ScrollGlyph.VerticalGripper) )
                        return CommonPressedOuterBorderPenM;
                    else if (RenderPressed || RenderMouseOver)
                        return CommonHoverOuterBorderPenM;
                    else
                        return CommonOuterBorderPenM;
                }
                else
                {
                    if (HasOuterBorder)
                    {
                        if (themeColor == ThemeColor.NormalColor)
                            return CommonOuterBorderPenNC;
                        else // themeColor == ThemeColor.Homestead
                            return CommonOuterBorderPenHS;
                    }
                    else
                        return null;
                }
            }
        }


        private static Pen CommonDisabledInnerBorderPen
        {
            get
            {
                if (_commonDisabledInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledInnerBorderPen == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xE8, 0xDF));
                            temp.Freeze();

                            _commonDisabledInnerBorderPen = temp;
                        }
                    }
                }
                return _commonDisabledInnerBorderPen;
            }
        }

        private static Pen CommonInnerBorderPenNC
        {
            get
            {
                if (_commonInnerBorderPenNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonInnerBorderPenNC == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xB4, 0xC8, 0xF6));
                            temp.Freeze();

                            _commonInnerBorderPenNC = temp;
                        }
                    }
                }
                return _commonInnerBorderPenNC;
            }
        }



        private static Pen CommonHoverInnerBorderPenNC
        {
            get
            {
                if (_commonHoverInnerBorderPenNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverInnerBorderPenNC == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x98, 0xB1, 0xE4));
                            temp.Freeze();

                            _commonHoverInnerBorderPenNC = temp;
                        }
                    }
                }
                return _commonHoverInnerBorderPenNC;
            }
        }

        private static Pen CommonHoverThumbInnerBorderPenNC
        {
            get
            {
                if (_commonHoverThumbInnerBorderPenNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverThumbInnerBorderPenNC == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xAC, 0xCE, 0xFF));
                            temp.Freeze();

                            _commonHoverThumbInnerBorderPenNC = temp;
                        }
                    }
                }
                return _commonHoverThumbInnerBorderPenNC;
            }
        }

        private static Pen CommonPressedInnerBorderPenNC
        {
            get
            {
                if (_commonPressedInnerBorderPenNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedInnerBorderPenNC == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x83, 0x8F, 0xDA));
                            temp.Freeze();

                            _commonPressedInnerBorderPenNC = temp;
                        }
                    }
                }
                return _commonPressedInnerBorderPenNC;
            }
        }

        private static Pen CommonInnerBorderPenHS
        {
            get
            {
                if (_commonInnerBorderPenHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonInnerBorderPenHS == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x8E, 0x99, 0x7D));
                            temp.Freeze();

                            _commonInnerBorderPenHS = temp;
                        }
                    }
                }
                return _commonInnerBorderPenHS;
            }
        }

        private static Pen CommonHoverInnerBorderPenHS
        {
            get
            {
                if (_commonHoverInnerBorderPenHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverInnerBorderPenHS == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xBD, 0xCB, 0x96));
                            temp.Freeze();

                            _commonHoverInnerBorderPenHS = temp;
                        }
                    }
                }
                return _commonHoverInnerBorderPenHS;
            }
        }


        private static Pen CommonPressedInnerBorderPenHS
        {
            get
            {
                if (_commonPressedInnerBorderPenHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedInnerBorderPenHS == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x7A, 0x8B, 0x63));
                            temp.Freeze();

                            _commonPressedInnerBorderPenHS = temp;
                        }
                    }
                }
                return _commonPressedInnerBorderPenHS;
            }
        }

        private static Pen CommonInnerBorderPenM
        {
            get
            {
                if (_commonInnerBorderPenM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonInnerBorderPenM == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonInnerBorderPenM = temp;
                        }
                    }
                }
                return _commonInnerBorderPenM;
            }
        }

        private static Pen CommonHoverInnerBorderPenM
        {
            get
            {
                if (_commonHoverInnerBorderPenM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverInnerBorderPenM == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonHoverInnerBorderPenM = temp;
                        }
                    }
                }
                return _commonHoverInnerBorderPenM;
            }
        }

       
        private static Pen CommonPressedInnerBorderPenM
        {
            get
            {
                if (_commonPressedInnerBorderPenM == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedInnerBorderPenM == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonPressedInnerBorderPenM = temp;
                        }
                    }
                }
                return _commonPressedInnerBorderPenM;
            }
        }



        private Pen InnerBorder
        {
            get
            {
                ScrollGlyph scrollGlyph = ScrollGlyph;


                if (!IsEnabled)
                    return CommonDisabledInnerBorderPen;

                ThemeColor themeColor = ThemeColor;

                if (RenderPressed)
                {
                    if (themeColor == ThemeColor.NormalColor)
                        return CommonPressedInnerBorderPenNC;
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonPressedInnerBorderPenHS;
                    else
                        return CommonPressedInnerBorderPenM;
                }

                if (RenderMouseOver)
                {
                    if (themeColor == ThemeColor.NormalColor)
                    {
                        if (scrollGlyph == ScrollGlyph.HorizontalGripper || scrollGlyph == ScrollGlyph.VerticalGripper)
                            return CommonHoverThumbInnerBorderPenNC;
                        else
                            return CommonHoverInnerBorderPenNC;
                    }
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonHoverInnerBorderPenHS;
                    else
                        return CommonHoverInnerBorderPenM;
                }

                if (themeColor == ThemeColor.NormalColor)
                    return CommonInnerBorderPenNC;
                else if (themeColor == ThemeColor.Homestead)
                    return CommonInnerBorderPenHS;
                else
                    return CommonInnerBorderPenM;
            }
        }

        private static Pen CommonDisabledShadowPen
        {
            get
            {
                if (_commonDisabledShadowPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledShadowPen == null)
                        {
                            Pen temp = new Pen();

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(0, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xCC, 0xCC, 0xBA), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCC, 0xCC, 0xBA), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC4, 0xC4, 0xAF), 1));
                            brush.Freeze();

                            temp.Brush = brush;
                            temp.Freeze();

                            _commonDisabledShadowPen = temp;
                        }
                    }
                }
                return _commonDisabledShadowPen;
            }
        }

        private static Pen CommonShadowPenNC
        {
            get
            {
                if (_commonShadowPenNC == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonShadowPenNC == null)
                        {
                            Pen temp = new Pen();

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(0, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xA0, 0xB5, 0xD3), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA0, 0xB5, 0xD3), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x7C, 0x9F, 0xD3), 1));
                            brush.Freeze();

                            temp.Brush = brush;
                            temp.Freeze();

                            _commonShadowPenNC = temp;
                        }
                    }
                }
                return _commonShadowPenNC;
            }
        }

        private static Pen CommonShadowPenHS
        {
            get
            {
                if (_commonShadowPenHS == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonShadowPenHS == null)
                        {
                            Pen temp = new Pen();

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(0, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xB6, 0xC1, 0xA6), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x9B, 0xB1, 0x81), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x8B, 0x93, 0x77), 1));
                            brush.Freeze();

                            temp.Brush = brush;
                            temp.Freeze();

                            _commonShadowPenHS = temp;
                        }
                    }
                }
                return _commonShadowPenHS;
            }
        }

        private Pen Shadow
        {
            get
            {
                if (HasOuterBorder)
                {
                    if (!IsEnabled)
                        return CommonDisabledShadowPen;

                    ThemeColor themeColor = ThemeColor;

                    if (themeColor == ThemeColor.NormalColor)
                        return CommonShadowPenNC;
                    else if (themeColor == ThemeColor.Homestead)
                        return CommonShadowPenHS;
                    else
                        return null;
                }
                return null;
            }
        }

        #endregion

        #region Data

        private static LinearGradientBrush _commonLineButtonFillNC;
        private static LinearGradientBrush _commonVerticalFillNC;
        private static LinearGradientBrush _commonHorizontalFillNC;

        private static LinearGradientBrush _commonVerticalFillHS;
        private static LinearGradientBrush _commonHorizontalFillHS;
        
        private static LinearGradientBrush _commonVerticalFillM;
        private static LinearGradientBrush _commonHorizontalFillM;

        
        private static LinearGradientBrush _commonHoverLineButtonFillNC;
        private static LinearGradientBrush _commonHoverVerticalFillNC;
        private static LinearGradientBrush _commonHoverHorizontalFillNC;
        
        private static LinearGradientBrush _commonHoverVerticalFillHS;
        private static LinearGradientBrush _commonHoverHorizontalFillHS;
        
        private static LinearGradientBrush _commonHoverVerticalFillM;
        private static LinearGradientBrush _commonHoverHorizontalFillM;

        
        private static LinearGradientBrush _commonPressedLineButtonFillNC;
        private static LinearGradientBrush _commonPressedVerticalFillNC;
        private static LinearGradientBrush _commonPressedHorizontalFillNC;

        private static LinearGradientBrush _commonPressedVerticalFillHS;
        private static LinearGradientBrush _commonPressedHorizontalFillHS;
                
        private static LinearGradientBrush _commonPressedVerticalFillM;
        private static LinearGradientBrush _commonPressedHorizontalFillM;

        private static LinearGradientBrush _commonDisabledFill;


        private static SolidColorBrush _commonDisabledGlyph;

        private static SolidColorBrush _commonArrowGlyphNC;
        private static SolidColorBrush _commonArrowGlyphM;

        private static SolidColorBrush _commonHoverArrowGlyphM;

        private static SolidColorBrush _commonGripperGlyphNC;
        private static SolidColorBrush _commonHoverGripperGlyphNC;
        private static SolidColorBrush _commonPressedGripperGlyphNC;

        private static SolidColorBrush _commonGripperGlyphHS;
        private static SolidColorBrush _commonHoverGripperGlyphHS;
        private static SolidColorBrush _commonPressedGripperGlyphHS;

        private static SolidColorBrush _commonGripperGlyphM;

        private static SolidColorBrush _commonDisabledGripperGlyphShadow;

        private static SolidColorBrush _commonGripperGlyphShadowNC;
        private static SolidColorBrush _commonHoverGripperGlyphShadowNC;
        private static SolidColorBrush _commonPressedGripperGlyphShadowNC;

        private static SolidColorBrush _commonGripperGlyphShadowHS;
        private static SolidColorBrush _commonHoverGripperGlyphShadowHS;
        private static SolidColorBrush _commonPressedGripperGlyphShadowHS;

        private static SolidColorBrush _commonGripperGlyphShadowM;

        private static Pen _commonOuterBorderPenNC;

        private static Pen _commonOuterBorderPenHS;

        private static Pen _commonOuterBorderPenM;
        private static Pen _commonHoverOuterBorderPenM;
        private static Pen _commonPressedOuterBorderPenM;
        

        private static Pen _commonDisabledInnerBorderPen;

        private static Pen _commonInnerBorderPenNC;
        private static Pen _commonInnerBorderPenHS;
        private static Pen _commonInnerBorderPenM;

        private static Pen _commonHoverInnerBorderPenNC;
        private static Pen _commonHoverThumbInnerBorderPenNC;

        private static Pen _commonHoverInnerBorderPenHS;
        private static Pen _commonHoverInnerBorderPenM;

        private static Pen _commonPressedInnerBorderPenNC;
        private static Pen _commonPressedInnerBorderPenHS;
        private static Pen _commonPressedInnerBorderPenM;

        private static Pen _commonDisabledShadowPen;

        private static Pen _commonShadowPenNC;
        private static Pen _commonShadowPenHS;


        private static object _resourceAccess = new object();


        private MatrixTransform _transform;

        #endregion
    }
}

