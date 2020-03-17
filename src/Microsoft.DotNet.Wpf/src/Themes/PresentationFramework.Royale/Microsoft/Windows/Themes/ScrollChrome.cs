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

        /// <summary>
        ///     DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty = Control.PaddingProperty.AddOwner(typeof(ScrollChrome));

        /// <summary>
        ///     Insets the border of the element.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness) GetValue(PaddingProperty); }
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

            DrawBorders(drawingContext, ref bounds);
            DrawGlyph(drawingContext, glyph, ref bounds);
        }

        #endregion

        #region Private Methods

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
                for (int i = 0; i < 8; i+=2)
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

        private static LinearGradientBrush CommonVerticalFill
        {
            get
            {
                if (_commonVerticalFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonVerticalFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xD1, 0xE9), 0.8));
                            temp.Freeze();

                            _commonVerticalFill = temp;
                        }
                    }
                }
                return _commonVerticalFill;
            }
        }

        private static LinearGradientBrush CommonHorizontalFill
        {
            get
            {
                if (_commonHorizontalFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHorizontalFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 0);
                            temp.EndPoint = new Point(0.5, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC1, 0xD5, 0xED), 0.7));
                            temp.Freeze();

                            _commonHorizontalFill = temp;
                        }
                    }
                }
                return _commonHorizontalFill;
            }
        }


        private static LinearGradientBrush CommonHoverVerticalFill
        {
            get
            {
                if (_commonHoverVerticalFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverVerticalFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xD1, 0xE9), 1));
                            temp.Freeze();

                            _commonHoverVerticalFill = temp;
                        }
                    }
                }
                return _commonHoverVerticalFill;
            }
        }

        private static LinearGradientBrush CommonHoverHorizontalFill
        {
            get
            {
                if (_commonHoverHorizontalFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverHorizontalFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 0);
                            temp.EndPoint = new Point(0.5, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC1, 0xD5, 0xED), 1));
                            temp.Freeze();

                            _commonHoverHorizontalFill = temp;
                        }
                    }
                }
                return _commonHoverHorizontalFill;
            }
        }

        private static LinearGradientBrush CommonPressedVerticalFill
        {
            get
            {
                if (_commonPressedVerticalFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedVerticalFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(1, 0);
                            temp.EndPoint = new Point(0, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC1, 0xD5, 0xED), 0.7));
                            temp.Freeze();

                            _commonPressedVerticalFill = temp;
                        }
                    }
                }
                return _commonPressedVerticalFill;
            }
        }

        private static LinearGradientBrush CommonPressedHorizontalFill
        {
            get
            {
                if (_commonPressedHorizontalFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedHorizontalFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 1);
                            temp.EndPoint = new Point(0, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC1, 0xD5, 0xED), 0.7));
                            temp.Freeze();

                            _commonPressedHorizontalFill = temp;
                        }
                    }
                }
                return _commonPressedHorizontalFill;
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

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF2, 0xF7, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD5, 0xE7, 0xFF), 1));
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

                if (RenderPressed)
                {
                    if (ScrollGlyph == ScrollGlyph.VerticalGripper)
                        return CommonPressedVerticalFill;
                    else
                        return CommonPressedHorizontalFill;
                }

                if (RenderMouseOver)
                {
                    if (ScrollGlyph == ScrollGlyph.VerticalGripper)
                        return CommonHoverVerticalFill;
                    else
                        return CommonHoverHorizontalFill;
                }

                if (ScrollGlyph == ScrollGlyph.VerticalGripper)
                    return CommonVerticalFill;
                else
                    return CommonHorizontalFill;
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
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xB7, 0xCB, 0xE3));
                            temp.Freeze();

                            _commonDisabledGlyph = temp;
                        }
                    }
                }
                return _commonDisabledGlyph;
            }
        }


        private static SolidColorBrush CommonArrowGlyph
        {
            get
            {
                if (_commonArrowGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonArrowGlyph == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x64, 0x73));
                            temp.Freeze();

                            _commonArrowGlyph = temp;
                        }
                    }
                }
                return _commonArrowGlyph;
            }
        }

        private static SolidColorBrush CommonHoverArrowGlyph
        {
            get
            {
                if (_commonHoverArrowGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverArrowGlyph == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x6B, 0x7B, 0x84));
                            temp.Freeze();

                            _commonHoverArrowGlyph = temp;
                        }
                    }
                }
                return _commonHoverArrowGlyph;
            }
        }

        private static SolidColorBrush CommonGripperGlyph
        {
            get
            {
                if (_commonGripperGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyph == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonGripperGlyph = temp;
                        }
                    }
                }
                return _commonGripperGlyph;
            }
        }

        private Brush Glyph
        {
            get
            {
                if (!IsEnabled)
                    return CommonDisabledGlyph;

                ScrollGlyph scrollGlyph = ScrollGlyph;

                if (scrollGlyph == ScrollGlyph.HorizontalGripper || scrollGlyph == ScrollGlyph.VerticalGripper)
                {
                    return CommonGripperGlyph;
                }
                else
                {
                    if (RenderMouseOver || RenderPressed)
                        return CommonHoverArrowGlyph;
                    else
                        return CommonArrowGlyph;
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

        private static SolidColorBrush CommonGripperGlyphShadow
        {
            get
            {
                if (_commonGripperGlyphShadow == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonGripperGlyphShadow == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x83, 0x97, 0xAF));
                            temp.Freeze();

                            _commonGripperGlyphShadow = temp;
                        }
                    }
                }
                return _commonGripperGlyphShadow;
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

                    return CommonGripperGlyphShadow;
                }
                return null;
            }
        }

        private static Pen CommonDisabledOuterBorderPen
        {
            get
            {
                if (_commonDisabledOuterBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledOuterBorderPen == null)
                        {
                            Pen temp = new Pen();

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE1, 0xEE, 0xFF), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB1, 0xC5, 0xDD), 1));
                           
                            temp.Brush = brush;
                            temp.Freeze();

                            _commonDisabledOuterBorderPen = temp;
                        }
                    }
                }
                return _commonDisabledOuterBorderPen;
            }
        }

        private static Pen CommonOuterBorderPen
        {
            get
            {
                if (_commonOuterBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonOuterBorderPen == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x85, 0x99, 0xB1));
                            temp.Freeze();

                            _commonOuterBorderPen = temp;
                        }
                    }
                }
                return _commonOuterBorderPen;
            }
        }

        private static Pen CommonHoverOuterBorderPen
        {
            get
            {
                if (_commonHoverOuterBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverOuterBorderPen == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0x52, 0x66, 0x7E));
                            temp.Freeze();

                            _commonHoverOuterBorderPen = temp;
                        }
                    }
                }
                return _commonHoverOuterBorderPen;
            }
        }


        private Pen OuterBorder
        {
            get
            {
                if (!IsEnabled)
                    return CommonDisabledOuterBorderPen;
                else if (RenderMouseOver || RenderPressed)
                    return CommonHoverOuterBorderPen;
                else
                    return CommonOuterBorderPen;
            }
        }

        

        private static Pen CommonInnerBorderPen
        {
            get
            {
                if (_commonInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonInnerBorderPen == null)
                        {
                            Pen temp = new Pen();
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                            temp.Freeze();

                            _commonInnerBorderPen = temp;
                        }
                    }
                }
                return _commonInnerBorderPen;
            }
        }

        private Pen InnerBorder
        {
            get
            {
                return CommonInnerBorderPen;
            }
        }

        #endregion

        #region Data

        private static LinearGradientBrush _commonVerticalFill;
        private static LinearGradientBrush _commonHorizontalFill;

        private static LinearGradientBrush _commonHoverVerticalFill;
        private static LinearGradientBrush _commonHoverHorizontalFill;

        private static LinearGradientBrush _commonPressedVerticalFill;
        private static LinearGradientBrush _commonPressedHorizontalFill;

        private static LinearGradientBrush _commonDisabledFill;


        private static SolidColorBrush _commonDisabledGlyph;

        private static SolidColorBrush _commonArrowGlyph;

        private static SolidColorBrush _commonHoverArrowGlyph;
        
        private static SolidColorBrush _commonGripperGlyph;


        private static SolidColorBrush _commonDisabledGripperGlyphShadow;

        private static SolidColorBrush _commonGripperGlyphShadow;


        private static Pen _commonDisabledOuterBorderPen;
        private static Pen _commonOuterBorderPen;

        private static Pen _commonHoverOuterBorderPen;

        private static Pen _commonInnerBorderPen;

        private static object _resourceAccess = new object();

       
        private MatrixTransform _transform;

        #endregion
    }
}

