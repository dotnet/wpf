// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading;

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MS.Internal;

using System;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// The BulletChrome element
    /// This element is a theme-specific type that is used as an optimization
    /// for a common complex rendering used in Aero
    ///   
    /// </summary>
    public sealed class BulletChrome : FrameworkElement
    {
        #region Constructors

        static BulletChrome()
        {
            IsEnabledProperty.OverrideMetadata(typeof(BulletChrome), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OnIsEnabledChanged)));
        }

        private static void OnIsEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            OnIsCheckedChanged(o, e);
        }
        
        /// <summary>
        /// Instantiates a new instance of a BulletChrome with no parent element.
        /// </summary>
        /// <ExternalAPI/>
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
                Border.BorderBrushProperty.AddOwner(
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
        /// DependencyProperty for <see cref="RenderMouseOver" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderMouseOverProperty =
                 DependencyProperty.Register("RenderMouseOver",
                         typeof(bool),
                         typeof(BulletChrome),
                         new FrameworkPropertyMetadata(
                                false,
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
            BulletChrome chrome = ((BulletChrome)o);

            if (chrome.Animates)
            {
                if (((bool)e.NewValue))
                {
                    AnimateToHover(chrome);
                }
                else if (chrome._localResources == null)
                {
                    chrome.InvalidateVisual();
                }
                else
                {
                    Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

                    DoubleAnimation da = new DoubleAnimation();
                    da.Duration = duration;

                    chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                    chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                    GradientStopCollection stops;
                    ColorAnimation ca = new ColorAnimation();

                    if (chrome.IsChecked == null)
                    {
                        AnimateToIndeterminate(chrome);
                    }
                    else
                    {
                        ca.Duration = duration;

                        stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                        chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                    }

                    if (chrome.IsRound)
                    {
                        stops = ((GradientBrush)chrome.GlyphFill).GradientStops;
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    }
                }
            }
            else
            {
                chrome._localResources = null;
                chrome.InvalidateVisual();
            }
        }

        private static void AnimateToHover(BulletChrome chrome)
        {
            if (chrome._localResources == null)
            {
                chrome._localResources = new LocalResources();
                chrome.InvalidateVisual();
            }

            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

            DoubleAnimation da = new DoubleAnimation(1, duration);

            // Border and Background Overlay Opacity
            chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
            chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.OpacityProperty, da);

            // Background and Border Overlay Colors
            ColorAnimation ca = new ColorAnimation();
            ca.Duration = duration;
            chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.ColorProperty, ca);
            GradientStopCollection stops;


            if (chrome.IsChecked == null)
            {
                // InnerBorder 
                stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;

                ca = new ColorAnimation(Color.FromRgb(0x29, 0x62, 0x8D), duration);
                stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x24, 0x54, 0x79), duration);
                stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x19, 0x3B, 0x55), duration);
                stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                // InnerFill
                ca = new ColorAnimation(Color.FromRgb(0x33, 0xD7, 0xED), duration);
                chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x20, 0x94, 0xCE), duration);
                chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                // Highlight Brush
                stops = ((GradientBrush)chrome.HighlightStroke.Brush).GradientStops;


                ca = new ColorAnimation(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF), duration);
                stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromArgb(0x00, 0x33, 0x33, 0xA0), duration);
                stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromArgb(0x80, 0x33, 0x33, 0xA0), duration);
                stops[3].BeginAnimation(GradientStop.ColorProperty, ca);
            }
            else
            {
                // Inner Border Gradient Stops
                stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;

                ca = new ColorAnimation(Color.FromRgb(0x79, 0xC6, 0xF9), duration);
                stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x79, 0xC6, 0xF9), duration);
                stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0xD2, 0xED, 0xFD), duration);
                stops[2].BeginAnimation(GradientStop.ColorProperty, ca);


                // Inner Fill
                ca = new ColorAnimation(Color.FromRgb(0xB1, 0xDF, 0xFD), duration);
                chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0xE9, 0xF7, 0xFE), duration);
                chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
            }


            // Glyph Fill
            if (chrome.IsRound && chrome.IsChecked == true)
            {
                stops = ((GradientBrush)chrome.GlyphFill).GradientStops;

                ca = new ColorAnimation(Color.FromRgb(0xFF, 0xFF, 0xFF), duration);
                stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x74, 0xFF, 0xFF), duration);
                stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                ca = new ColorAnimation(Color.FromRgb(0x0D, 0xA0, 0xF3), duration);
                stops[2].BeginAnimation(GradientStop.ColorProperty, ca);
            }
        }

        private static void AnimateToIndeterminate(BulletChrome chrome)
        {
            DoubleAnimation da = new DoubleAnimation();
            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));
            da.Duration = duration;

            chrome.GlyphStroke.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
            chrome.GlyphFill.BeginAnimation(SolidColorBrush.OpacityProperty, da);

            da = new DoubleAnimation(1.0, duration);
            chrome.HighlightStroke.Brush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);

            ColorAnimation ca = new ColorAnimation(Color.FromRgb(0x2A, 0x62, 0x8D), duration);

            // Inner Border Pen
            GradientStopCollection stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;

            stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0x24, 0x54, 0x79), duration);
            stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0x19, 0x3B, 0x55), duration);
            stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

            // InnerFill
            ca = new ColorAnimation(Color.FromRgb(0x2F, 0xA8, 0xD5), duration);
            chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromRgb(0x25, 0x59, 0x8C), duration);
            chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

            // Highlight Brush
            stops = ((GradientBrush)chrome.HighlightStroke.Brush).GradientStops;

            ca = new ColorAnimation(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF), duration);
            stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromArgb(0x00, 0x33, 0x33, 0xA0), duration);
            stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

            ca = new ColorAnimation(Color.FromArgb(0x00, 0x33, 0x33, 0xA0), duration);
            stops[3].BeginAnimation(GradientStop.ColorProperty, ca);
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
            BulletChrome chrome = ((BulletChrome)o);

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

                    ColorAnimation ca = new ColorAnimation(Color.FromRgb(0x2C, 0x62, 0x8B), duration);
                    chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0xC2, 0xE4, 0xF6), duration);
                    chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.ColorProperty, ca);

                    if (chrome.IsChecked == null)
                    {
                        // InnerBorderPen
                        GradientStopCollection stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;

                        ca = new ColorAnimation(Color.FromRgb(0x19, 0x3B, 0x55), duration);
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x24, 0x54, 0x79), duration);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x29, 0x62, 0x8D), duration);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                        //InnerFill 
                        ca = new ColorAnimation(Color.FromRgb(0x17, 0x44, 0x7A), duration);
                        chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x21, 0x8B, 0xC3), duration);
                        chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                        // HighlightStroke
                        stops = ((GradientBrush)chrome.HighlightStroke.Brush).GradientStops;

                        ca = new ColorAnimation(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), duration);
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), duration);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), duration);
                        stops[3].BeginAnimation(GradientStop.ColorProperty, ca);
                    }
                    else
                    {
                        // Inner Border Pen
                        GradientStopCollection stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;

                        ca = new ColorAnimation(Color.FromRgb(0x54, 0xA6, 0xD5), duration);
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x5E, 0xB5, 0xE4), duration);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0xC4, 0xE5, 0xF6), duration);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);

                        // Inner Fill
                        ca = new ColorAnimation(Color.FromRgb(0x7F, 0xBA, 0xDC), duration);
                        chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0xD6, 0xED, 0xF9), duration);
                        chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                    }

                    if (chrome.IsChecked != null)
                    {
                        DoubleAnimation da = new DoubleAnimation(1.0, duration);

                        chrome.GlyphFill.BeginAnimation(Brush.OpacityProperty, da);
                        chrome.GlyphStroke.Brush.BeginAnimation(Brush.OpacityProperty, da);
                    }

                    if (chrome.IsRound)
                    {
                        GradientStopCollection stops = ((GradientBrush)chrome.GlyphFill).GradientStops;

                        ca = new ColorAnimation(Color.FromRgb(0x95, 0xD9, 0xFC), duration);
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x3A, 0x84, 0xAA), duration);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);

                        ca = new ColorAnimation(Color.FromRgb(0x07, 0x54, 0x83), duration);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    }
                }
                else if (chrome._localResources == null)
                {
                    chrome.InvalidateVisual();
                }
                else
                {
                    AnimateToHover(chrome);

                    Duration duration = new Duration(TimeSpan.FromSeconds(0.3));
                    if (chrome.IsChecked != true)
                    {
                        DoubleAnimation da = new DoubleAnimation();
                        da.Duration = duration;
                        chrome.GlyphFill.BeginAnimation(Brush.OpacityProperty, da);
                        chrome.GlyphStroke.Brush.BeginAnimation(Brush.OpacityProperty, da);
                    }

                    if (chrome.IsRound)
                    {
                        ColorAnimation ca = new ColorAnimation();
                        ca.Duration = duration;

                        GradientStopCollection stops = ((GradientBrush)chrome.GlyphFill).GradientStops;
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);
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
        /// DependencyProperty for <see cref="IsChecked" /> property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
                 DependencyProperty.Register("IsChecked",
                         typeof(bool?),
                         typeof(BulletChrome),
                         new FrameworkPropertyMetadata(
                                ((bool?)false),
                                new PropertyChangedCallback(OnIsCheckedChanged)));

        /// <summary>
        /// When true, the left border will have round corners, otherwise they will be square.
        /// </summary>
        public bool? IsChecked
        {
            get { return (bool?)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Also called when IsRound and IsEnabled changes to set up Glyph animations
        private static void OnIsCheckedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BulletChrome chrome = ((BulletChrome)o);

            if (chrome.Animates)
            {
                if (chrome._localResources == null)
                {
                    chrome._localResources = new LocalResources();
                    chrome.InvalidateVisual();
                }

                Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

                if (chrome.IsChecked == null)
                {
                    AnimateToIndeterminate(chrome);
                }
                else
                {
                    if (chrome.IsChecked == true)
                    {
                        DoubleAnimation da = new DoubleAnimation(1, duration);
                    
                        chrome.GlyphStroke.Brush.BeginAnimation(Brush.OpacityProperty, da);
                        chrome.GlyphFill.BeginAnimation(Brush.OpacityProperty, da);
                    
                        da = new DoubleAnimation();
                        da.Duration = duration;
                        chrome.HighlightStroke.Brush.BeginAnimation(Brush.OpacityProperty, da);
                    }
                    else
                    {
                        DoubleAnimation da = new DoubleAnimation();
                        da.Duration = duration;
                    
                        chrome.GlyphStroke.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        chrome.GlyphFill.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        chrome.HighlightStroke.Brush.BeginAnimation(Brush.OpacityProperty, da);
                    }

                    if (chrome.RenderMouseOver)
                    {
                        AnimateToHover(chrome);
                    }
                    else
                    {
                        ColorAnimation ca = new ColorAnimation();
                        ca.Duration = duration;
                        GradientStopCollection stops = ((GradientBrush)chrome.InnerBorderPen.Brush).GradientStops;
                        stops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                        stops[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    
                        chrome.InnerFill.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, ca);
                        chrome.InnerFill.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, ca);
                    
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
        /// DependencyProperty for <see cref="IsRound" /> property.
        /// </summary>
        public static readonly DependencyProperty IsRoundProperty =
                 DependencyProperty.Register("IsRound",
                         typeof(bool),
                         typeof(BulletChrome),
                         new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(OnIsRoundChanged)));

        private static void OnIsRoundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((BulletChrome)o)._localResources = null;

            // Force update of glyph colors
            OnIsCheckedChanged(o, e);
        }

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
            if (IsRound)
                return new Size(12.0, 12.0);
            else
                return new Size(13.0, 13.0);
        }

        
        /// <summary>
        /// Render callback.  
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);

            // Draw Background
            DrawBackground(drawingContext, ref bounds);
            
            // Draw innerborder and fill with inner fill
            DrawInnerBorder(drawingContext, ref bounds);

            DrawGlyph(drawingContext, ref bounds);
            
            // Draw outer border
            DrawBorder(drawingContext, ref bounds);
        }
       

        private void DrawBackground(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width > 4.0) && (bounds.Height > 4.0))
            {
                Brush fill = Background;


                if (!IsRound)
                {
                    Rect backgroundRect = new Rect(bounds.Left + 1.0,
                                                   bounds.Top + 1.0,
                                                   bounds.Width - 2.0,
                                                   bounds.Height - 2.0);
                    // Draw Background
                    if (fill != null)
                        dc.DrawRectangle(fill, null, backgroundRect);

                    // Draw BackgroundOverlay
                    fill = BackgroundOverlay;
                    if (fill != null)
                        dc.DrawRectangle(fill, null, backgroundRect);
                }
                else
                {
                    double centerX = bounds.Width * 0.5;
                    double centerY = bounds.Height * 0.5;

                    // Draw Background
                    if (fill != null)
                        dc.DrawEllipse(fill, null, new Point(centerX, centerY), centerX - 1, centerY - 1);

                    // Draw BackgroundOverlay
                    fill = BackgroundOverlay;
                    if (fill != null)
                        dc.DrawEllipse(fill, null, new Point(centerX, centerY), centerX - 1, centerY - 1);
                }
            }
        }
       
        
        // Draw the inner border
        private void DrawInnerBorder(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 6.0) && (bounds.Height >= 6.0))
            {
                Brush innerFill = InnerFill;

                if (!IsRound)
                {
                    if (innerFill != null)
                    {
                        dc.DrawRectangle(innerFill, null, new Rect(bounds.Left + 3.0, bounds.Top + 3.0, bounds.Width - 6.0, bounds.Height - 6.0));
                    }

                    Pen innerBorder = InnerBorderPen;

                    if (innerBorder != null)
                    {
                        dc.DrawRectangle(null, innerBorder, new Rect(bounds.Left + 2.5, bounds.Top + 2.5, bounds.Width - 5.0, bounds.Height - 5.0));
                    }
                }
                else
                {
                    double centerX = bounds.Width * 0.5;
                    double centerY = bounds.Height * 0.5;

                    if (innerFill != null)
                    {
                        dc.DrawEllipse(innerFill, null, new Point(centerX, centerY), centerX - 3.0, centerY - 3.0);
                    }

                    Pen innerBorder = InnerBorderPen;

                    if (innerBorder != null)
                    {
                        dc.DrawEllipse(null, innerBorder, new Point(centerX, centerY), centerX - 2.5, centerY - 2.5);
                    }
                }
            }
        }

        // Draw the CheckMark or Dot
        private void DrawGlyph(DrawingContext dc, ref Rect bounds)
        {
            if (!IsRound)
            {
            
                // Need to reverse Checkbox in RTL so it draws to screen the same as LTR 
                if (FlowDirection == FlowDirection.RightToLeft)
                {
                    dc.PushTransform(new ScaleTransform(-1.0, 1.0, 6.5, 0));
                }

                dc.DrawGeometry(null, GlyphStroke, CheckMarkGeometry);
                dc.DrawGeometry(GlyphFill, null, CheckMarkGeometry);

                if (FlowDirection == FlowDirection.RightToLeft)
                {
                    dc.Pop();
                }
            
                dc.DrawRectangle(null, HighlightStroke, new Rect(3.5, 3.5, 6, 6));
            }
            else
            {
                double centerX = bounds.Width * 0.5;
                double centerY = bounds.Height * 0.5;
                dc.DrawEllipse(GlyphFill, GlyphStroke, new Point(centerX, centerY), centerX - 3, centerY - 3);
            }
        }

        // Draw the main border
        private void DrawBorder(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 5.0) && (bounds.Height >= 5.0))
            {

                Pen pen = GetBorderPen(BorderBrush);
                Pen overlayPen = BorderOverlayPen;

                if (pen != null || overlayPen != null)
                {
                    if (!IsRound)
                    {
                        Rect rect = new Rect(bounds.Left + 0.5,
                                            bounds.Top + 0.5,
                                            bounds.Width - 1.0,
                                            bounds.Height - 1.0);

                        if (pen != null)
                            dc.DrawRectangle(null, pen, rect);

                        if (overlayPen != null)
                            dc.DrawRectangle(null, overlayPen, rect);
                    }
                    else
                    {
                        double centerX = bounds.Width * 0.5;
                        double centerY = bounds.Height * 0.5;

                        if (pen != null)
                            dc.DrawEllipse(null, pen, new Point(centerX, centerY), centerX - 0.5, centerY - 0.5);

                        if (overlayPen != null)
                            dc.DrawEllipse(null, overlayPen, new Point(centerX, centerY), centerX - 0.5, centerY - 0.5);
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

        private bool Animates
        {
            get
            {
                return SystemParameters.ClientAreaAnimation && 
                       RenderCapability.Tier > 0 &&
                       IsEnabled;
                    
            }
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
                            PathFigure figure = new PathFigure();
                            figure.StartPoint = new Point(9.0, 1.833);
                            figure.Segments.Add(new LineSegment(new Point(10.667, 3.167), true));
                            figure.Segments.Add(new LineSegment(new Point(7, 10.667), true));
                            figure.Segments.Add(new LineSegment(new Point(5.333, 10.667), true));
                            figure.Segments.Add(new LineSegment(new Point(3.333, 8.167), true));
                            figure.Segments.Add(new LineSegment(new Point(3.333, 6.833), true));
                            figure.Segments.Add(new LineSegment(new Point(4.833, 6.5), true));
                            figure.Segments.Add(new LineSegment(new Point(6, 8), true));
                            figure.IsClosed = true;
                            figure.Freeze();

                            PathGeometry path = new PathGeometry();
                            path.Figures.Add(figure);
                            path.Freeze();

                            _checkMarkGeometry = path;
                        }
                    }
                }

                return _checkMarkGeometry;
            }
        }

        private static Pen CommonCheckMarkStroke
        {
            get
            {
                if (_commonCheckMarkStroke == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckMarkStroke == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1.5;
                            temp.Brush = new SolidColorBrush(Colors.White);
                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonCheckMarkStroke = temp;
                        }
                    }
                }
                return _commonCheckMarkStroke;
            }
        }

        private static Pen CommonCheckMarkPressedStroke
        {
            get
            {
                if (_commonCheckMarkPressedStroke == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckMarkPressedStroke == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1.5;
                            temp.Brush = new SolidColorBrush(Colors.White);
                            temp.Brush.Opacity = 0.7;
                            temp.Freeze();
                            _commonCheckMarkPressedStroke = temp;
                        }
                    }
                }
                return _commonCheckMarkPressedStroke;
            }
        }

        private static Pen CommonRadioButtonDisabledGlyphStroke
        {
            get
            {
                if (_commonRadioButtonDisabledGlyphStroke == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonDisabledGlyphStroke == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = new SolidColorBrush(Color.FromRgb(0xA2, 0xAE, 0xB9));
                            temp.Freeze();
                            _commonRadioButtonDisabledGlyphStroke = temp;
                        }
                    }
                }
                return _commonRadioButtonDisabledGlyphStroke;
            }
        }


        private static Pen CommonRadioButtonGlyphStroke
        {
            get
            {
                if (_commonRadioButtonGlyphStroke == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonGlyphStroke == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x19, 0x3B, 0x55));
                            temp.Freeze();
                            _commonRadioButtonGlyphStroke = temp;
                        }
                    }
                }
                return _commonRadioButtonGlyphStroke;
            }
        }

        private static Pen CommonIndeterminateHighlight
        {
            get
            {
                if (_commonIndeterminateHighlight == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateHighlight == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x33, 0x33, 0xA0), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x33, 0x33, 0xA0), 1));

                            temp.Brush = brush;

                            temp.Freeze();

                            _commonIndeterminateHighlight = temp;
                        }
                    }
                }
                return _commonIndeterminateHighlight;
            }
        }

        private static Pen CommonIndeterminateHoverHighlight
        {
            get
            {
                if (_commonIndeterminateHoverHighlight == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateHoverHighlight == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x33, 0x33, 0xA0), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0x33, 0x33, 0xA0), 1));

                            temp.Brush = brush;

                            temp.Freeze();
                            _commonIndeterminateHoverHighlight = temp;
                        }
                    }
                }
                return _commonIndeterminateHoverHighlight;
            }
        }

        private static Pen CommonIndeterminatePressedHighlight
        {
            get
            {
                if (_commonIndeterminatePressedHighlight == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminatePressedHighlight == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 1));

                            temp.Brush = brush;

                            temp.Freeze();
                            _commonIndeterminatePressedHighlight = temp;
                        }
                    }
                }
                return _commonIndeterminatePressedHighlight;
            }
        }

        private Pen HighlightStroke
        {
            get
            {
                if (_localResources != null)
                {
                    if (_localResources.HighlightStroke == null)
                    {
                        _localResources.HighlightStroke = CommonIndeterminateHighlight.Clone();
                        _localResources.HighlightStroke.Brush.Opacity = 0;
                    }
                    return _localResources.HighlightStroke;
                }

                if (!IsRound && IsChecked == null)
                {
                    if (RenderPressed)
                    {
                        return CommonIndeterminatePressedHighlight;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonIndeterminateHoverHighlight;
                    }
                    else
                    {
                        return CommonIndeterminateHighlight;
                    }
                }

                return null;
            }
        }

        private static SolidColorBrush CommonCheckMarkDisabledFill
        {
            get
            {
                if (_commonCheckMarkDisabledFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckMarkDisabledFill == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0xAE, 0xB7, 0xCF));
                            temp.Freeze();
                            _commonCheckMarkDisabledFill = temp;
                        }
                    }
                }
                return _commonCheckMarkDisabledFill;
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
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0x31, 0x34, 0x7C));
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
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0x31, 0x34, 0x7C));
                            temp.Opacity = 0.7;
                            temp.Freeze();
                            _commonCheckMarkPressedFill = temp;
                        }
                    }
                }
                return _commonCheckMarkPressedFill;
            }
        }

        private static RadialGradientBrush CommonRadioButtonGlyphDisabledFill
        {
            get
            {
                if (_commonRadioButtonGlyphDisabledFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonGlyphDisabledFill == null)
                        {
                            RadialGradientBrush temp = new RadialGradientBrush();

                            temp.Center = new Point(0.25, 0.25);
                            temp.GradientOrigin = new Point(0.25, 0.25);
                            temp.RadiusX = 0.75;
                            temp.RadiusY = 0.75;
                            temp.Opacity = 0.7;

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xC9, 0xD5, 0xDE), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xC0, 0xE3, 0xE8), 0.35));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xB0, 0xD4, 0xE9), 1));

                            temp.Freeze();
                            _commonRadioButtonGlyphDisabledFill = temp;
                        }
                    }
                }
                return _commonRadioButtonGlyphDisabledFill;
            }
        }

        private static RadialGradientBrush CommonRadioButtonGlyphFill
        {
            get
            {
                if (_commonRadioButtonGlyphFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonGlyphFill == null)
                        {
                            RadialGradientBrush temp = new RadialGradientBrush();

                            temp.Center = new Point(0.25, 0.25);
                            temp.GradientOrigin = new Point(0.25, 0.25);
                            temp.RadiusX = 0.75;
                            temp.RadiusY = 0.75;

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xE5, 0xE5, 0xE5), 0.1));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x5D, 0xCE, 0xDD), 0.35));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x0B, 0x82, 0xC7), 1));

                            temp.Freeze();
                            _commonRadioButtonGlyphFill = temp;
                        }
                    }
                }
                return _commonRadioButtonGlyphFill;
            }
        }

        private static RadialGradientBrush CommonRadioButtonGlyphHoverFill
        {
            get
            {
                if (_commonRadioButtonGlyphHoverFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonGlyphHoverFill == null)
                        {
                            RadialGradientBrush temp = new RadialGradientBrush();

                            temp.Center = new Point(0.25, 0.25);
                            temp.GradientOrigin = new Point(0.25, 0.25);
                            temp.RadiusX = 0.75;
                            temp.RadiusY = 0.75;

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0.1));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x74, 0xFF, 0xFF), 0.35));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x0D, 0xA0, 0xF3), 1));

                            temp.Freeze();
                            _commonRadioButtonGlyphHoverFill = temp;
                        }
                    }
                }
                return _commonRadioButtonGlyphHoverFill;
            }
        }



        private static RadialGradientBrush CommonRadioButtonGlyphPressedFill
        {
            get
            {
                if (_commonRadioButtonGlyphPressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonGlyphPressedFill == null)
                        {
                            RadialGradientBrush temp = new RadialGradientBrush();

                            temp.Center = new Point(0.25, 0.25);
                            temp.GradientOrigin = new Point(0.25, 0.25);
                            temp.RadiusX = 0.75;
                            temp.RadiusY = 0.75;

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x95, 0xD9, 0xFC), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x3A, 0x84, 0xAA), 0.35));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x07, 0x54, 0x83), 1));

                            temp.Freeze();
                            _commonRadioButtonGlyphPressedFill = temp;
                        }
                    }
                }
                return _commonRadioButtonGlyphPressedFill;
            }
        }

        private static SolidColorBrush CommonHoverBackgroundOverlay
        {
            get
            {
                if (_commonHoverBackgroundOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverBackgroundOverlay == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0xDE, 0xF9, 0xFA));
                            temp.Freeze();
                            _commonHoverBackgroundOverlay = temp;
                        }
                    }
                }
                return _commonHoverBackgroundOverlay;
            }
        }

        private static SolidColorBrush CommonPressedBackgroundOverlay
        {
            get
            {
                if (_commonPressedBackgroundOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedBackgroundOverlay == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0xC2, 0xE4, 0xF6));
                            temp.Freeze();
                            _commonPressedBackgroundOverlay = temp;
                        }
                    }
                }
                return _commonPressedBackgroundOverlay;
            }
        }

        private static SolidColorBrush CommonDisabledBackgroundOverlay
        {
            get
            {
                if (_commonDisabledBackgroundOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledBackgroundOverlay == null)
                        {
                            SolidColorBrush temp = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4));
                            temp.Freeze();
                            _commonDisabledBackgroundOverlay = temp;
                        }
                    }
                }
                return _commonDisabledBackgroundOverlay;
            }
        }

       private static Pen CommonHoverBorderOverlay
        {
            get
            {
                if (_commonHoverBorderOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverBorderOverlay == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x3C, 0x7F, 0xB1));
                            temp.Freeze();
                            _commonHoverBorderOverlay = temp;
                        }
                    }
                }
                return _commonHoverBorderOverlay;
            }
        }

        private static Pen CommonPressedBorderOverlay
        {
            get
            {
                if (_commonPressedBorderOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedBorderOverlay == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = new SolidColorBrush(Color.FromRgb(0x2C, 0x62, 0x8B));
                            temp.Freeze();
                            _commonPressedBorderOverlay = temp;
                        }
                    }
                }
                return _commonPressedBorderOverlay;
            }
        }

        private static Pen CommonDisabledBorderOverlay
        {
            get
            {
                if (_commonDisabledBorderOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDisabledBorderOverlay == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = new SolidColorBrush(Color.FromRgb(0xAD, 0xB2, 0xB5));
                            temp.Freeze();
                            _commonDisabledBorderOverlay = temp;
                        }
                    }
                }
                return _commonDisabledBorderOverlay;
            }
        }

        private static Pen CommonCheckBoxDisabledInnerBorderPen
        {
            get
            {
                if (_commonCheckBoxDisabledInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxDisabledInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0,0);
                            brush.EndPoint = new Point(1,1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xE1, 0xE3, 0xE5), 0.25));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xE9, 0xEA), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF3, 0xF3, 0xF3), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonCheckBoxDisabledInnerBorderPen = temp;
                        }
                    }
                }
                return _commonCheckBoxDisabledInnerBorderPen;
            }
        }

        private static Pen CommonCheckBoxInnerBorderPen
        {
            get
            {
                if (_commonCheckBoxInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0,0);
                            brush.EndPoint = new Point(1,1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xAE, 0xB3, 0xB9), 0.25));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xC2, 0xC4, 0xC6), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xEA, 0xEB, 0xEB), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonCheckBoxInnerBorderPen = temp;
                        }
                    }
                }
                return _commonCheckBoxInnerBorderPen;
            }
        }

        private static Pen CommonCheckBoxHoverInnerBorderPen
        {
            get
            {
                if (_commonCheckBoxHoverInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxHoverInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x79, 0xC6, 0xF9), 0.3));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x79, 0xC6, 0xF9), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xD2, 0xED, 0xFD), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonCheckBoxHoverInnerBorderPen = temp;
                        }
                    }
                }
                return _commonCheckBoxHoverInnerBorderPen;
            }
        }

        private static Pen CommonCheckBoxPressedInnerBorderPen
        {
            get
            {
                if (_commonCheckBoxPressedInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxPressedInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x54, 0xA6, 0xD5), 0.3));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x5E, 0xB5, 0xE4), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xC4, 0xE5, 0xF6), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonCheckBoxPressedInnerBorderPen = temp;
                        }
                    }
                }
                return _commonCheckBoxPressedInnerBorderPen;
            }
        }

        private static Pen CommonIndeterminateDisabledInnerBorderPen
        {
            get
            {
                if (_commonIndeterminateDisabledInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateDisabledInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xBF, 0xD0, 0xDD), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xBD, 0xCB, 0xD7), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xBA, 0xC4, 0xCC), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonIndeterminateDisabledInnerBorderPen = temp;
                        }
                    }
                }
                return _commonIndeterminateDisabledInnerBorderPen;
            }
        }

        private static Pen CommonIndeterminateInnerBorderPen
        {
            get
            {
                if (_commonIndeterminateInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x2A, 0x62, 0x8D), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x24, 0x54, 0x79), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x19, 0x3B, 0x55), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonIndeterminateInnerBorderPen = temp;
                        }
                    }
                }
                return _commonIndeterminateInnerBorderPen;
            }
        }

        private static Pen CommonIndeterminateHoverInnerBorderPen
        {
            get
            {
                if (_commonIndeterminateHoverInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateHoverInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x29, 0x62, 0x8D), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x24, 0x54, 0x79), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x19, 0x3B, 0x55), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonIndeterminateHoverInnerBorderPen = temp;
                        }
                    }
                }
                return _commonIndeterminateHoverInnerBorderPen;
            }
        }

        private static Pen CommonIndeterminatePressedInnerBorderPen
        {
            get
            {
                if (_commonIndeterminatePressedInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminatePressedInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x19, 0x3B, 0x55), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x24, 0x54, 0x79), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x29, 0x62, 0x8D), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonIndeterminatePressedInnerBorderPen = temp;
                        }
                    }
                }
                return _commonIndeterminatePressedInnerBorderPen;
            }
        }


        private static Pen CommonRadioButtonInnerBorderPen
        {
            get
            {
                if (_commonRadioButtonInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);
                            
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB3, 0xB8, 0xBD), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xEB, 0xEB, 0xEB), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonRadioButtonInnerBorderPen = temp;
                        }
                    }
                }
                return _commonRadioButtonInnerBorderPen;
            }
        }

        private static Pen CommonRadioButtonHoverInnerBorderPen
        {
            get
            {
                if (_commonRadioButtonHoverInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonHoverInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x80, 0xCA, 0xF9), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xD2, 0xEE, 0xFD), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonRadioButtonHoverInnerBorderPen = temp;
                        }
                    }
                }
                return _commonRadioButtonHoverInnerBorderPen;
            }
        }

        private static Pen CommonRadioButtonPressedInnerBorderPen
        {
            get
            {
                if (_commonRadioButtonPressedInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRadioButtonPressedInnerBorderPen == null)
                        {
                            Pen temp = new Pen();

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(1, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x5C, 0xAA, 0xD7), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xC3, 0xE4, 0xF6), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonRadioButtonPressedInnerBorderPen = temp;
                        }
                    }
                }
                return _commonRadioButtonPressedInnerBorderPen;
            }
        }

        private Brush BackgroundOverlay
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonDisabledBackgroundOverlay;
                }

                if (!Animates)
                {
                    if (RenderPressed)
                    {
                        return CommonPressedBackgroundOverlay;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonHoverBackgroundOverlay;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.BackgroundOverlay == null)
                    {
                        _localResources.BackgroundOverlay = CommonHoverBackgroundOverlay.Clone();
                        _localResources.BackgroundOverlay.Opacity = 0;
                    }
                    return _localResources.BackgroundOverlay;
                }
                else
                {
                    return null;
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

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xCB, 0xCF, 0xD5), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xF7, 0xF7, 0xF7), 0.8));

                            temp.Freeze();
                            _commonCheckBoxInnerFill = temp;
                        }
                    }
                }
                return _commonCheckBoxInnerFill;
            }
        }

        private static LinearGradientBrush CommonCheckBoxHoverInnerFill
        {
            get
            {
                if (_commonCheckBoxHoverInnerFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxHoverInnerFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xB1, 0xDF, 0xFD), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xE9, 0xF7, 0xFE), 0.8));

                            temp.Freeze();
                            _commonCheckBoxHoverInnerFill = temp;
                        }
                    }
                }
                return _commonCheckBoxHoverInnerFill;
            }
        }

        private static LinearGradientBrush CommonCheckBoxPressedInnerFill
        {
            get
            {
                if (_commonCheckBoxPressedInnerFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonCheckBoxPressedInnerFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);


                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x7F, 0xBA, 0xDC), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xD6, 0xED, 0xF9), 0.8));

                            temp.Freeze();
                            _commonCheckBoxPressedInnerFill = temp;
                        }
                    }
                }
                return _commonCheckBoxPressedInnerFill;
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

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xC0, 0xE5, 0xF3), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0xBD, 0xCD, 0xDC), 0.8));

                            temp.Freeze();
                            _commonIndeterminateDisabledFill = temp;
                        }
                    }
                }
                return _commonIndeterminateDisabledFill;
            }
        }


        private static LinearGradientBrush CommonIndeterminateFill
        {
            get
            {
                if (_commonIndeterminateFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x2F, 0xA8, 0xD5), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x25, 0x59, 0x8C), 0.8));

                            temp.Freeze();
                            _commonIndeterminateFill = temp;
                        }
                    }
                }
                return _commonIndeterminateFill;
            }
        }

        private static LinearGradientBrush CommonIndeterminateHoverFill
        {
            get
            {
                if (_commonIndeterminateHoverFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonIndeterminateHoverFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x33, 0xD7, 0xED), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x20, 0x94, 0xCE), 0.8));

                            temp.Freeze();
                            _commonIndeterminateHoverFill = temp;
                        }
                    }
                }
                return _commonIndeterminateHoverFill;
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

                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x17, 0x44, 0x7A), 0.2));
                            temp.GradientStops.Add(new GradientStop(Color.FromRgb(0x21, 0x8B, 0xC3), 0.8));

                            temp.Freeze();
                            _commonIndeterminatePressedFill = temp;
                        }
                    }
                }
                return _commonIndeterminatePressedFill;
            }
        }

        private GradientBrush InnerFill
        {
            get
            {
                if (!IsEnabled)
                {
                    if (IsChecked == null)
                    {
                        return CommonIndeterminateDisabledFill;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.InnerFill == null)
                    {
                        _localResources.InnerFill = CommonCheckBoxInnerFill.Clone();
                    }
                    return _localResources.InnerFill;
                }

                if (IsChecked == null)
                {
                    if (RenderPressed)
                    {
                        return CommonIndeterminatePressedFill;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonIndeterminateHoverFill;
                    }
                    else
                    {
                        return CommonIndeterminateFill;
                    }
                }

                if (RenderPressed)
                {
                    return CommonCheckBoxPressedInnerFill;
                }
                else if (RenderMouseOver)
                {
                    return CommonCheckBoxHoverInnerFill;

                }
                else
                {
                    return CommonCheckBoxInnerFill;
                }
            }
        }

        private Pen GlyphStroke
        {
            get
            {
                if (!IsEnabled)
                {
                    if (IsRound && IsChecked == true)
                    {
                        return CommonRadioButtonDisabledGlyphStroke;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.GlyphStroke == null)
                    {
                        if (!IsRound)
                        {
                            _localResources.GlyphStroke = CommonCheckMarkStroke.Clone();
                            _localResources.GlyphStroke.Brush.Opacity = 0;
                        }
                        else
                        {
                            _localResources.GlyphStroke = CommonRadioButtonGlyphStroke.Clone();
                            _localResources.GlyphStroke.Brush.Opacity = 0;
                        }
                    }
                    return _localResources.GlyphStroke;
                }

                if (!IsRound)
                {
                    if (IsChecked == true)
                    {
                        if (RenderPressed)
                        {
                            return CommonCheckMarkPressedStroke;
                        }
                        else
                        {
                            return CommonCheckMarkStroke;
                        }
                    }
                    else if (IsChecked == false && RenderPressed)
                    {
                        return CommonCheckMarkPressedStroke;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (IsChecked == true || RenderPressed)
                    {
                        return CommonRadioButtonGlyphStroke;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

              

        private Brush GlyphFill
        {
            get
            {
                if (!IsEnabled)
                {
                    if (IsChecked == true)
                    {
                        if (!IsRound)
                        {
                            return CommonCheckMarkDisabledFill;
                        }
                        else
                        {
                            return CommonRadioButtonGlyphDisabledFill;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.GlyphFill == null)
                    {
                        if (!IsRound)
                        {
                            _localResources.GlyphFill = CommonCheckMarkFill.Clone();
                            _localResources.GlyphFill.Opacity = 0;
                        }
                        else
                        {
                            _localResources.GlyphFill = CommonRadioButtonGlyphFill.Clone();
                            _localResources.GlyphFill.Opacity = 0;
                        }
                    }
                    return _localResources.GlyphFill;
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
                    else if (IsChecked == false && RenderPressed)
                    {
                        return CommonCheckMarkPressedFill;
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
                        if (RenderPressed)
                        {
                            return CommonRadioButtonGlyphPressedFill;
                        }
                        else if (RenderMouseOver)
                        {
                            return CommonRadioButtonGlyphHoverFill;
                        }
                        else
                        {
                            return CommonRadioButtonGlyphFill;
                        }
                    }
                    else if (RenderPressed)
                    {
                        return CommonRadioButtonGlyphPressedFill;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        private Pen BorderOverlayPen
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonDisabledBorderOverlay;
                }

                if (_localResources != null)
                {
                    if (_localResources.BorderOverlayPen == null)
                    {
                        _localResources.BorderOverlayPen = CommonHoverBorderOverlay.Clone();
                        _localResources.BorderOverlayPen.Brush.Opacity = 0;
                    }
                    return _localResources.BorderOverlayPen;
                }
                else
                {
                    if (RenderPressed)
                    {
                        return CommonPressedBorderOverlay;
                    }
                    else if (RenderMouseOver)
                    {
                        return CommonHoverBorderOverlay;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        private Pen InnerBorderPen
        {
            get
            {
                if (!IsEnabled)
                {
                    if (IsChecked == null)
                    {
                        return CommonIndeterminateDisabledInnerBorderPen;
                    }
                    else
                    {
                        return CommonCheckBoxDisabledInnerBorderPen;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.InnerBorderPen == null)
                    {
                        _localResources.InnerBorderPen = CommonCheckBoxInnerBorderPen.Clone();
                    }
                    return _localResources.InnerBorderPen;
                }

                if (RenderPressed)
                {
                    if (!IsRound)
                    {
                        if (IsChecked == null)
                        {
                            return CommonIndeterminatePressedInnerBorderPen;
                        }
                        else
                        {
                            return CommonCheckBoxPressedInnerBorderPen;
                        }
                    }
                    else
                    {
                        return CommonRadioButtonPressedInnerBorderPen;
                    }
                }
                else if (RenderMouseOver)
                {
                    if (!IsRound)
                    {
                        if (IsChecked == null)
                        {
                            return CommonIndeterminateHoverInnerBorderPen;
                        }
                        else
                        {
                            return CommonCheckBoxHoverInnerBorderPen;
                        }
                    }
                    else
                    {
                        return CommonRadioButtonHoverInnerBorderPen;
                    }
                }
                else
                {
                    if (!IsRound)
                    {
                        if (IsChecked == null)
                        {
                            return CommonIndeterminateInnerBorderPen;
                        }
                        else
                        {
                            return CommonCheckBoxInnerBorderPen;
                        }
                    }
                    else
                    {
                        return CommonRadioButtonInnerBorderPen;
                    }
                }
            }
        }



        // Common LocalResources
        private static Geometry _checkMarkGeometry;

        private static Pen _commonCheckMarkStroke;
        private static Pen _commonCheckMarkPressedStroke;
        private static SolidColorBrush _commonCheckMarkDisabledFill;
        private static SolidColorBrush _commonCheckMarkFill;
        private static SolidColorBrush _commonCheckMarkPressedFill;
        private static Pen _commonRadioButtonDisabledGlyphStroke;
        private static Pen _commonRadioButtonGlyphStroke;
        private static RadialGradientBrush _commonRadioButtonGlyphFill;
        private static RadialGradientBrush _commonRadioButtonGlyphHoverFill;
        private static RadialGradientBrush _commonRadioButtonGlyphPressedFill;
        private static RadialGradientBrush _commonRadioButtonGlyphDisabledFill;

        private static LinearGradientBrush _commonIndeterminateFill;
        private static LinearGradientBrush _commonIndeterminateHoverFill;
        private static LinearGradientBrush _commonIndeterminatePressedFill;
        private static LinearGradientBrush _commonIndeterminateDisabledFill;

        private static Pen _commonIndeterminateInnerBorderPen;
        private static Pen _commonIndeterminateHoverInnerBorderPen;
        private static Pen _commonIndeterminatePressedInnerBorderPen;
        private static Pen _commonIndeterminateDisabledInnerBorderPen;

        private static Pen _commonIndeterminateHighlight;
        private static Pen _commonIndeterminateHoverHighlight;
        private static Pen _commonIndeterminatePressedHighlight;

        private static Pen _commonBorderPen;
        private static Pen _commonCheckBoxInnerBorderPen;
        private static Pen _commonRadioButtonInnerBorderPen;
        private static LinearGradientBrush _commonCheckBoxInnerFill;

        private static Pen _commonDisabledBorderOverlay;
        private static SolidColorBrush _commonDisabledBackgroundOverlay;
        private static Pen _commonCheckBoxDisabledInnerBorderPen;

        private static SolidColorBrush _commonHoverBackgroundOverlay;
        private static Pen _commonHoverBorderOverlay;
        private static Pen _commonCheckBoxHoverInnerBorderPen;
        private static Pen _commonRadioButtonHoverInnerBorderPen;
        private static LinearGradientBrush _commonCheckBoxHoverInnerFill;
        
        private static SolidColorBrush _commonPressedBackgroundOverlay;
        private static Pen _commonPressedBorderOverlay;
        private static Pen _commonCheckBoxPressedInnerBorderPen;
        private static Pen _commonRadioButtonPressedInnerBorderPen;
        private static LinearGradientBrush _commonCheckBoxPressedInnerFill;
        
        private static object _resourceAccess = new object();        
        
        // Per instance resources
    
        private LocalResources _localResources;

        private class LocalResources
        {
            public Pen BorderOverlayPen;
            public Pen InnerBorderPen;
            public SolidColorBrush BackgroundOverlay;
            public GradientBrush InnerFill;
            public Pen HighlightStroke;
            public Pen GlyphStroke;
            public Brush GlyphFill;
        }        

        #endregion
    }
}

