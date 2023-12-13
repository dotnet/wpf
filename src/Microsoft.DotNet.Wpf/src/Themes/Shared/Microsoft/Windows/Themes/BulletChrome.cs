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
    public sealed class BulletChrome : FrameworkElement
    {

        #region Constructors

        static BulletChrome()
        {
            IsEnabledProperty.OverrideMetadata(typeof(BulletChrome), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        }

        /// <summary>
        /// Instantiates a new instance of a BulletChrome with no parent element.
        /// </summary>
        public BulletChrome()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty = 
                    Control.BackgroundProperty.AddOwner(
                            typeof(BulletChrome),
                            new FrameworkPropertyMetadata(
                                    null,
                                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Background property defines the brush used to fill the background of the button.
        /// </summary>
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="BorderBrush" /> property.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty = 
                System.Windows.Controls.Border.BorderBrushProperty.AddOwner(
                        typeof(BulletChrome),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The BorderBrush property defines the brush used to draw the outer border.
        /// </summary>
        public Brush BorderBrush
        {
            get { return (Brush) GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="BorderThickness" /> property.
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty =
                System.Windows.Controls.Border.BorderThicknessProperty.AddOwner(typeof(BulletChrome));

        /// <summary>
        /// The BorderThickness property defines the Thickness used to draw the outer border.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="RenderMouseOver" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderMouseOverProperty =
                 DependencyProperty.Register("RenderMouseOver",
                         typeof(bool),
                         typeof(BulletChrome),
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
                         typeof(BulletChrome),
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

        /// <summary>
        /// DependencyProperty for <see cref="IsChecked" /> property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
                 DependencyProperty.Register("IsChecked",
                         typeof(bool?),
                         typeof(BulletChrome),
                         new FrameworkPropertyMetadata(
                                ((bool?)false),
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true, the left border will have round corners, otherwise they will be square.
        /// </summary>
        public bool? IsChecked
        {
            get { return (bool?)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="IsRound" /> property.
        /// </summary>
        public static readonly DependencyProperty IsRoundProperty =
                 DependencyProperty.Register("IsRound",
                         typeof(bool),
                         typeof(BulletChrome),
                         new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true, the left border will have round corners, otherwise they will be square.
        /// </summary>
        public bool IsRound
        {
            get { return (bool)GetValue(IsRoundProperty); }
            set { SetValue(IsRoundProperty, value); }
        }
       
        #endregion Dynamic Properties

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the BulletChrome.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <param name="availableSize">Available size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The BulletChrome's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Thickness thickness = BorderThickness;

            bool isRound = IsRound;

            double borderX = isRound ? 2.0 : thickness.Left + thickness.Right;
            double borderY = isRound ? 2.0 : thickness.Top + thickness.Bottom;

            Size desired;
            desired = new Size(Math.Min(11.0 + borderX, availableSize.Width), Math.Min(11.0 + borderY, availableSize.Height));

            return desired;
        }

        /// <summary>
        /// Render callback.  
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Thickness thickness = BorderThickness;
            bool isUnitThickness = thickness.Left == 1.0 && thickness.Right == 1.0 &&
                                   thickness.Top == 1.0 && thickness.Bottom == 1.0;

            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);

            Rect innerBounds = bounds;
            
            // Apply BorderThickness to Checkbox only
            if (!IsRound)
            {
                innerBounds = new Rect(thickness.Left,
                                       thickness.Top,
                                       Math.Max(0, bounds.Width - thickness.Left - thickness.Right),
                                       Math.Max(0, bounds.Height - thickness.Top - thickness.Bottom));
            }

            // Draw Background
            DrawBackground(drawingContext, ref innerBounds);
            
            // Draw innerborder and fill with inner fill
            DrawHighlight(drawingContext, ref innerBounds);

            DrawGlyph(drawingContext, ref innerBounds, isUnitThickness);
            
            // Draw outer border
            DrawBorder(drawingContext, ref bounds, thickness, isUnitThickness);
        }
       

        private void DrawBackground(DrawingContext dc, ref Rect bounds)
        {
            Brush fill = BackgroundBrush;
            if (fill != null && (bounds.Width > 2.0) && (bounds.Height > 2.0))
            {
                if (!IsRound)
                {
                    // Draw Background
                    dc.DrawRectangle(fill, null, bounds);
                }
                else
                {
                    double centerX = bounds.Width * 0.5;
                    double centerY = bounds.Height * 0.5;

                    // Draw Background
                    dc.DrawEllipse(fill, null, new Point(centerX, centerY), centerX - 1, centerY - 1);
                }
            }
        }
       
        
        // Draw the inner border
        private void DrawHighlight(DrawingContext dc, ref Rect bounds)
        {
            Pen highlightPen = HighlightPen;

            if (highlightPen != null && (bounds.Width >= 4.0) && (bounds.Height >= 4.0))
            {
                if (!IsRound)
                {
                    dc.DrawRectangle(null, highlightPen, new Rect(bounds.Left + 1.0, bounds.Top + 1.0, bounds.Width - 2.0, bounds.Height - 2.0));
                }
                else 
                {
                    double centerX = bounds.Width * 0.5;
                    double centerY = bounds.Height * 0.5;

                    dc.DrawEllipse(null, highlightPen, new Point(centerX, centerY), centerX - 2, centerY - 2);
                }
            }
        }

        // Draw the CheckMark or Dot
        private void DrawGlyph(DrawingContext dc, ref Rect bounds, bool isUnitThickness)
        {
            Brush glyphFill = GlyphFill;
            if (glyphFill != null && (bounds.Width > 4.0) && (bounds.Height > 4.0))
            {
                if (!IsRound)
                {
                    if (IsChecked == true)
                    {
                        // Translate Glyph if not 1px borders
                        if (!isUnitThickness)
                        {
                            dc.PushTransform(new TranslateTransform(bounds.Left - 1.0, bounds.Top - 1.0));
                        }

                        // Need to reverse Checkbox in RTL so it draws to screen the same as LTR 
                        if (FlowDirection == FlowDirection.RightToLeft)
                        {
                            dc.PushTransform(new ScaleTransform(-1.0, 1.0, 6.5, 0));
                        }

                        dc.DrawGeometry(glyphFill, null, CheckMarkGeometry);

                        if (FlowDirection == FlowDirection.RightToLeft)
                        {
                            dc.Pop();
                        }

                        if (!isUnitThickness)
                        {
                            dc.Pop();
                        }
                    }
                    else if (IsChecked == null)
                    {
                        dc.DrawRectangle(glyphFill, null, new Rect(bounds.Left + 2, bounds.Top + 2, bounds.Width - 4.0, bounds.Height - 4.0));
                    }
                }
                else 
                {
                    if ((bounds.Width > 8.0) && (bounds.Height > 8.0))
                    {
                        double centerX = bounds.Width * 0.5;
                        double centerY = bounds.Height * 0.5;
                        dc.DrawEllipse(glyphFill, null, new Point(centerX, centerY), centerX - 4, centerY - 4);
                    }
                }
            }
        }

        // Draw the main border
        private void DrawBorder(DrawingContext dc, ref Rect bounds, Thickness thickness, bool isUnitThickness)
        {
            if ((bounds.Width >= 5.0) && (bounds.Height >= 5.0))
            {
                if (!IsRound)
                {
                    if (isUnitThickness)
                    {
                        Pen borderPen = BorderPen;

                        if (borderPen != null)
                        {
                            Rect rect = new Rect(bounds.Left + 0.5,
                                                bounds.Top + 0.5,
                                                bounds.Width - 1.0,
                                                bounds.Height - 1.0);

                            dc.DrawRectangle(null, borderPen, rect);
                        }
                    }
                    else
                    {
                        Brush borderBrush = Border;

                        if (borderBrush != null)
                        {
                            dc.DrawGeometry(borderBrush, null, GenerateBorderGeometry(bounds, thickness));
                        }
                    }
                }
                else // IsRound == true
                {
                     Pen borderPen = BorderPen;

                     if (borderPen != null)
                     {
                         double centerX = bounds.Width * 0.5;
                         double centerY = bounds.Height * 0.5;

                         dc.DrawEllipse(null, borderPen, new Point(centerX, centerY), centerX - 0.5, centerY - 0.5);
                     }
                }
            }
        }

        private static Pen GetBorderPen(Brush border)
        {
            Pen pen = null;

            if (border != null)
            {
                if (_commonBorderPen == null)   // Common case, if non-null, avoid the lock
                {
                    lock (_resourceAccess)   // If non-null, lock to create the pen for thread safety
                    {
                        if (_commonBorderPen == null)   // Check again in case _pen was created within the last line
                        {
                            // Assume that the first render of Button uses the most common brush for the app.
                            // This breaks down if (a) the first Button is disabled, (b) the first Button is
                            // customized, or (c) BulletChrome becomes more broadly used than just on Button.
                            //
                            // If these cons sufficiently weaken the effectiveness of this cache, then we need
                            // to build a larger brush-to-pen mapping cache.


                            // If the brush is not already frozen, we need to create our own
                            // copy.  Otherwise we will inadvertently freeze the user's
                            // BorderBrush when we freeze the pen below.
                            if (!border.IsFrozen && border.CanFreeze)
                            {
                                border = border.Clone();
                                border.Freeze();
                            }

                            Pen commonPen = new Pen(border, 1);
                            if (commonPen.CanFreeze)
                            {
                                // Only save frozen pens, some brushes such as VisualBrush
                                // can not be frozen
                                commonPen.Freeze();
                                _commonBorderPen = commonPen;
                            }
                        }
                    }
                }

                if (_commonBorderPen != null && border == _commonBorderPen.Brush)
                {

                    pen = _commonBorderPen;
                }
                else
                {
                    if (!border.IsFrozen && border.CanFreeze)
                    {
                        border = border.Clone();
                        border.Freeze();
                    }

                    pen = new Pen(border, 1);
                    if (pen.CanFreeze)
                    {
                        pen.Freeze();
                    }
                }
            }

            return pen;
        }

    
        #endregion

        #region Private Properties

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        /// Helper to deflate rectangle by thickness
        private static Rect HelperDeflateRect(Rect rt, Thickness thick)
        {
            return new Rect(rt.Left + thick.Left,
                            rt.Top + thick.Top,
                            Math.Max(0.0, rt.Width - thick.Left - thick.Right),
                            Math.Max(0.0, rt.Height - thick.Top - thick.Bottom));
        }

        // Creates a rectangle figure
        private static PathFigure GenerateRectFigure(Rect rect)
        {
            PathFigure figure = new PathFigure();
            figure.StartPoint = rect.TopLeft;
            figure.Segments.Add(new LineSegment(rect.TopRight, true));
            figure.Segments.Add(new LineSegment(rect.BottomRight, true));
            figure.Segments.Add(new LineSegment(rect.BottomLeft, true));
            figure.IsClosed = true;
            figure.Freeze();

            return figure;
        }

        // Creates a border geometry used to render complex border brushes
        private static Geometry GenerateBorderGeometry(Rect rect, Thickness borderThickness)
        {
            PathGeometry geometry = new PathGeometry();

            // Add outer rectangle figure
            geometry.Figures.Add(GenerateRectFigure(rect));

            // Subtract inner rectangle figure 
            geometry.Figures.Add(GenerateRectFigure(HelperDeflateRect(rect, borderThickness)));

            geometry.Freeze();

            return geometry;
        }

        private static Geometry CheckMarkGeometry
        {
            get
            {
                if (_checkMarkGeometry == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_checkMarkGeometry == null)
                        {
                            StreamGeometry geometry = new StreamGeometry();

                            StreamGeometryContext sgc = geometry.Open();

                            sgc.BeginFigure(new Point(3, 5.0), true, true);
                            sgc.LineTo(new Point(3, 7.8), false, false);
                            sgc.LineTo(new Point(5.5, 10.4), false, false);
                            sgc.LineTo(new Point(10.1, 5.8), false, false);
                            sgc.LineTo(new Point(10.1, 3), false, false);
                            sgc.LineTo(new Point(5.5, 7.6), false, false);

                            sgc.Close();

                            geometry.Freeze();

                            _checkMarkGeometry = geometry;
                        }
                    }
                }

                return _checkMarkGeometry;
            }
        }

        private static SolidColorBrush CommonDisabledGlyphFill
        {
            get
            {
                return CommonDisabledBorder;
            }
        }

      
        private static SolidColorBrush CommonCheckMarkFill
        {
            get
            {
                if (_commonCheckMarkFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckMarkFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0x21, 0xA1, 0x21));
                            temp.Freeze();
                            _commonCheckMarkFill = temp;
                        }
                    }
                }
                return _commonCheckMarkFill;
            }
        }

        private static SolidColorBrush CommonCheckMarkPressedFill
        {
            get
            {
                if (_commonCheckMarkPressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckMarkPressedFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0x1A, 0x7E, 0x18));
                            temp.Freeze();
                            _commonCheckMarkPressedFill = temp;
                        }
                    }
                }
                return _commonCheckMarkPressedFill;
            }
        }

        private static LinearGradientBrush CommonRadioButtonGlyphFill
        {
            get
            {
                if (_commonRadioButtonGlyphFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonGlyphFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();

                            temp.StartPoint = new Point();
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x60, 0xCF, 0x5D), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xAC, 0xEF, 0xAA), 0.302469134));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x13, 0x92, 0x10), 1));

                            temp.Freeze();
                            _commonRadioButtonGlyphFill = temp;
                        }
                    }
                }
                return _commonRadioButtonGlyphFill;
            }
        }

        private static LinearGradientBrush CommonPressedBackground
        {
            get
            {
                if (_commonPressedBackground == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedBackground == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xB2, 0xB2, 0xA9), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xEB, 0xEA, 0xDA), 1));

                            temp.Freeze();
                            _commonPressedBackground = temp;
                        }
                    }
                }
                return _commonPressedBackground;
            }
        }

        private static SolidColorBrush CommonDisabledBackground
        {
            get
            {
                return Brushes.White;
            }
        }

        private static SolidColorBrush CommonDisabledBorder
        {
            get
            {
                if (_commonDisabledBorder == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledBorder == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0xCA, 0xC8, 0xBB));
                            temp.Freeze();
                            _commonDisabledBorder = temp;
                        }
                    }
                }
                return _commonDisabledBorder;
            }
        }

        private Brush Border
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonDisabledBorder;
                }


                return BorderBrush;
            }
        }

        private static Pen CommonDisabledBorderPen
        {
            get
            {
                if (_commonDisabledBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledBorderPen == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = CommonDisabledBorder;
                            temp.Freeze();
                            _commonDisabledBorderPen = temp;
                        }
                    }
                }
                return _commonDisabledBorderPen;
            }
        }

        private static Pen CommonCheckBoxHoverHighlightPen
        {
            get
            {
                if (_commonCheckBoxHoverHighlightPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxHoverHighlightPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 2;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xF0, 0xCF), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF8, 0xB3, 0x30), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonCheckBoxHoverHighlightPen = temp;
                        }
                    }
                }
                return _commonCheckBoxHoverHighlightPen;
            }
        }

        private static Pen CommonRadioButtonHoverHighlightPen
        {
            get
            {
                if (_commonRadioButtonHoverHighlightPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonHoverHighlightPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 2;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFE, 0xDF, 0x9C), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF9, 0xBB, 0x43), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonRadioButtonHoverHighlightPen = temp;
                        }
                    }
                }
                return _commonRadioButtonHoverHighlightPen;
            }
        }


        private Brush BackgroundBrush
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonDisabledBackground;
                }

            
                if (RenderPressed)
                {
                    return CommonPressedBackground;
                }
                else
                {
                    return Background;
                }
            }
        }

        private static LinearGradientBrush CommonCheckBoxInnerFill
        {
            get
            {
                if (_commonCheckBoxInnerFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxInnerFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCB, 0xCF, 0xD5), 0.3));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xF6, 0xF6, 0xF6), 1));

                            temp.Freeze();
                            _commonCheckBoxInnerFill = temp;
                        }
                    }
                }
                return _commonCheckBoxInnerFill;
            }
        }

       

        private static LinearGradientBrush CommonIndeterminateDisabledFill
        {
            get
            {
                if (_commonIndeterminateDisabledFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateDisabledFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x2F, 0xA8, 0xD5), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x25, 0x59, 0x8C), 1));

                            temp.Freeze();
                            _commonIndeterminateDisabledFill = temp;
                        }
                    }
                }
                return _commonIndeterminateDisabledFill;
            }
        }


        private static SolidColorBrush CommonIndeterminateFill
        {
            get
            {
                if (_commonIndeterminateFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0x73, 0xC2, 0x73));

                            temp.Freeze();
                            _commonIndeterminateFill = temp;
                        }
                    }
                }
                return _commonIndeterminateFill;
            }
        }


        private static LinearGradientBrush CommonIndeterminatePressedFill
        {
            get
            {
                if (_commonIndeterminatePressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminatePressedFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x17, 0x74, 0x7A), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x21, 0x8B, 0xC3), 1));

                            temp.Freeze();
                            _commonIndeterminatePressedFill = temp;
                        }
                    }
                }
                return _commonIndeterminatePressedFill;
            }
        }

        private Brush GlyphFill
        {
            get
            {
                if (!IsEnabled)
                {
                    if (IsChecked != false)
                    {
                        return CommonDisabledGlyphFill;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (!IsRound)
                {
                    if (IsChecked == true)
                    {
                        if (RenderPressed)
                        {
                            return CommonCheckMarkPressedFill;
                        }
                        else
                        {
                            return CommonCheckMarkFill;
                        }
                    }
                    else if (IsChecked == null)
                    {
                        if (RenderPressed)
                        {
                            return CommonCheckMarkPressedFill;
                        }
                        else
                        {
                            return CommonIndeterminateFill;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (IsChecked == true)
                    {
                        return CommonRadioButtonGlyphFill;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        private Pen BorderPen
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonDisabledBorderPen;
                }

                return GetBorderPen(BorderBrush);
            }
        }


        private Pen HighlightPen
        {
            get
            {
                if (!RenderMouseOver || RenderPressed || !IsEnabled)
                {
                    return null;
                }

                if (!IsRound)
                {
                    return CommonCheckBoxHoverHighlightPen;
                   
                }
                else
                {
                    return CommonRadioButtonHoverHighlightPen;
                }

            }
        }

        // Common Resources
        private static Geometry _checkMarkGeometry;

        private static SolidColorBrush _commonCheckMarkFill;
        private static SolidColorBrush _commonCheckMarkPressedFill;

        private static LinearGradientBrush _commonRadioButtonGlyphFill;

        private static SolidColorBrush _commonIndeterminateFill;
        private static LinearGradientBrush _commonIndeterminatePressedFill;
        private static LinearGradientBrush _commonIndeterminateDisabledFill;

        private static Pen _commonBorderPen;
        private static LinearGradientBrush _commonCheckBoxInnerFill;

        private static SolidColorBrush _commonDisabledBorder;
        private static Pen _commonDisabledBorderPen;

        private static Pen _commonCheckBoxHoverHighlightPen;
        private static Pen _commonRadioButtonHoverHighlightPen;

        private static LinearGradientBrush _commonPressedBackground;
                
        private static object _resourceAccess = new object();        
        
        #endregion
    }
}
