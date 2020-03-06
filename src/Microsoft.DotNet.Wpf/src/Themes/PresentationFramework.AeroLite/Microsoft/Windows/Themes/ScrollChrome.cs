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
    ///     for a common complex rendering used in AeroLite
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
                glyph == ScrollGlyph.VerticalGripper ||
                glyph == ScrollGlyph.HorizontalGripper;
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
        public static readonly DependencyProperty RenderPressedProperty =
                 DependencyProperty.Register("RenderPressed",
                         typeof(bool),
                         typeof(ScrollChrome),
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
            return new Size(0,0);
        }

        /// <summary>
        ///     ScrollChrome does no work here and returns arrangeSize.
        /// </summary>
        /// <param name="finalSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
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
                case ScrollGlyph.HorizontalGripper:
                    if (bounds.Height >= 1.0)
                    {
                        bounds.Y += 1.0;
                        bounds.Height -= 1.0;
                    }
                    break;

                case ScrollGlyph.VerticalGripper:
                    if (bounds.Width >= 1.0)
                    {
                        bounds.X += 1.0;
                        bounds.Width -= 1.0;
                    }
                    break;
            }

            DrawBorders(drawingContext, ref bounds);
            DrawGlyph(drawingContext, ref bounds);
        }

        #endregion

        #region Private Methods

        private void DrawBorders(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
            {
                Brush brush = Fill;
                Pen pen = Border;
                if (pen != null)
                {
                    dc.DrawRectangle(
                        brush,
                        pen,
                        new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
                    brush = null; // Done with the fill
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
                    }
                }
            }
        }

        private void DrawHorizontalGripper(DrawingContext dc, Brush brush, Rect bounds)
        {
            if ((bounds.Width > 15.0) && (bounds.Height > 2.0))
            {
                double height = Math.Min(7.0, bounds.Height);
                double x = bounds.X + ((bounds.Width * 0.5) - 4.0);
                double y = bounds.Y + ((bounds.Height - height) * 0.5);

                for (int i = 0; i < 9; i += 3)
                {
                    dc.DrawRectangle(brush, null, new Rect(x + i, y, 2.0, height));
                }
            }
        }

        private void DrawVerticalGripper(DrawingContext dc, Brush brush, Rect bounds)
        {
            if ((bounds.Width > 2.0) && (bounds.Height > 15.0))
            {
                double width = Math.Min(7.0, bounds.Width);
                double x = bounds.X + ((bounds.Width - width) * 0.5);
                double y = bounds.Y + ((bounds.Height * 0.5) - 4.0);

                for (int i = 0; i < 9; i += 3)
                {
                    dc.DrawRectangle(brush, null, new Rect(x, y + i, width, 2.0));
                }
            }
        }
        
        #endregion

        #region Data

        private static SolidColorBrush CommonThumbFill
        {
            get
            {
                if (_commonThumbFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
                            temp.Freeze();
                            _commonThumbFill = temp;
                        }
                    }
                }
                return _commonThumbFill;
            }
        }

        private static SolidColorBrush CommonThumbHoverFill
        {
            get
            {
                if (_commonThumbHoverFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbHoverFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xBD, 0xE6, 0xFD));
                            temp.Freeze();
                            _commonThumbHoverFill = temp;
                        }
                    }
                }
                return _commonThumbHoverFill;
            }
        }

        private static SolidColorBrush CommonThumbPressedFill
        {
            get
            {
                if (_commonThumbPressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbPressedFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0xBD, 0xE6, 0xFD));
                            temp.Freeze();
                            _commonThumbPressedFill = temp;
                        }
                    }
                }
                return _commonThumbPressedFill;
            }
        }

        private SolidColorBrush Fill
        {
            get
            {
                if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                    _scrollGlyph == ScrollGlyph.VerticalGripper)
                {
                    if (RenderPressed)
                    {
                        return CommonThumbPressedFill;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonThumbHoverFill;
                    }
                    else
                    {
                        return CommonThumbFill;
                    }
                }

                return null;
            }
        }

        private static Pen CommonThumbBorder
        {
            get
            {
                if (_commonThumbBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0xA3, 0xA3, 0xA3));

                            temp.Freeze();
                            _commonThumbBorder = temp;
                        }
                    }
                }
                return _commonThumbBorder;
            }
        }

        private static Pen CommonThumbHoverBorder
        {
            get
            {
                if (_commonThumbHoverBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbHoverBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x21, 0xA1, 0xC4));

                            temp.Freeze();
                            _commonThumbHoverBorder = temp;
                        }
                    }
                }
                return _commonThumbHoverBorder;
            }
        }

        private static Pen CommonThumbPressedBorder
        {
            get
            {
                if (_commonThumbPressedBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbPressedBorder == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;

                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x00, 0x73, 0x94));

                            temp.Freeze();
                            _commonThumbPressedBorder = temp;
                        }
                    }
                }
                return _commonThumbPressedBorder;
            }
        }

        private Pen Border
        {
            get
            {
                if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                    _scrollGlyph == ScrollGlyph.VerticalGripper)
                {
                    if (RenderPressed)
                    {
                        return CommonThumbPressedBorder;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonThumbHoverBorder;
                    }
                    else
                    {
                        return CommonThumbBorder;
                    }
                }

                return null;
            }
        }

        private SolidColorBrush CommonThumbEnabledGlyph
        {
            get
            {
                if (_commonThumbEnabledGlyph == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonThumbEnabledGlyph == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                            temp.Freeze();
                            _commonThumbEnabledGlyph = temp;
                        }
                    }
                }
                return _commonThumbEnabledGlyph;
            }
        }

        private SolidColorBrush Glyph
        {
            get
            {
                if (_scrollGlyph == ScrollGlyph.HorizontalGripper ||
                    _scrollGlyph == ScrollGlyph.VerticalGripper)
                {
                    if (IsEnabled)
                    {
                        return CommonThumbEnabledGlyph;
                    }
                    else
                    {
                        return SystemColors.GrayTextBrush;
                    }
                }

                return null;
            }
        }

        // Common Resources
        private static SolidColorBrush _commonThumbFill;
        private static SolidColorBrush _commonThumbHoverFill;
        private static SolidColorBrush _commonThumbPressedFill;

        private static Pen _commonThumbBorder;
        private static Pen _commonThumbHoverBorder;
        private static Pen _commonThumbPressedBorder;
        
        private static SolidColorBrush _commonThumbEnabledGlyph;
        
        private static object _resourceAccess = new object();

        // Per instance data
        private ScrollGlyph _scrollGlyph;
        
        #endregion
    }
}

