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

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// The ButtonChrome element
    /// This element is a theme-specific type that is used as an optimization
    /// for a common complex rendering used in Aero
    ///
    /// </summary>
    public sealed class ButtonChrome : Decorator
    {
        #region Constructors

        static ButtonChrome()
        {
            IsEnabledProperty.OverrideMetadata(typeof(ButtonChrome), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        }

        /// <summary>
        /// Instantiates a new instance of a ButtonChrome with no parent element.
        /// </summary>
        /// <ExternalAPI/>
        public ButtonChrome()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                    Control.BackgroundProperty.AddOwner(
                            typeof(ButtonChrome),
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
                        typeof(ButtonChrome),
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
        /// DependencyProperty for <see cref="RenderDefaulted" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderDefaultedProperty =
                 DependencyProperty.Register("RenderDefaulted",
                         typeof(bool),
                         typeof(ButtonChrome),
                         new FrameworkPropertyMetadata(
                                false,
                                new PropertyChangedCallback(OnRenderDefaultedChanged)));

        /// <summary>
        /// When true the chrome renders with a mouse over look.
        /// </summary>
        public bool RenderDefaulted
        {
            get { return (bool)GetValue(RenderDefaultedProperty); }
            set { SetValue(RenderDefaultedProperty, value); }
        }

        private static void OnRenderDefaultedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ButtonChrome chrome = ((ButtonChrome)o);

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

                    ColorAnimation ca = new ColorAnimation(Color.FromArgb(0xF9, 0x00, 0xCC, 0xFF), duration);
                    GradientStopCollection gsc = ((LinearGradientBrush)chrome.InnerBorderPen.Brush).GradientStops;
                    gsc[0].BeginAnimation(GradientStop.ColorProperty, ca);
                    gsc[1].BeginAnimation(GradientStop.ColorProperty, ca);

                    if (!chrome.RenderPressed)
                    {
                        // Create a repeating animation like:
                        //  __/ \__/ \__/ \__...
                        DoubleAnimationUsingKeyFrames daukf = new DoubleAnimationUsingKeyFrames();
                        daukf.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(0.5)));
                        daukf.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, TimeSpan.FromSeconds(0.75)));
                        daukf.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                        daukf.RepeatBehavior = RepeatBehavior.Forever;
                        DoubleAnimationUsingKeyFrames.SetDesiredFrameRate(daukf, 10);

                        chrome.BackgroundOverlay.BeginAnimation(LinearGradientBrush.OpacityProperty, daukf);
                        chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, daukf);
                    }
                }
                else if (chrome._localResources == null)
                {
                    if (!chrome.RenderPressed)
                    {
                        chrome.InvalidateVisual();
                    }
                }
                else
                {
                    Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

                    if (!chrome.RenderPressed)
                    {
                        DoubleAnimation da = new DoubleAnimation();
                        da.Duration = duration;
                        chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        chrome.BackgroundOverlay.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    }

                    ColorAnimation ca = new ColorAnimation();
                    ca.Duration = duration;
                    GradientStopCollection gsc = ((LinearGradientBrush)chrome.InnerBorderPen.Brush).GradientStops;
                    gsc[0].BeginAnimation(GradientStop.ColorProperty, ca);
                    gsc[1].BeginAnimation(GradientStop.ColorProperty, ca);
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
                         typeof(ButtonChrome),
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
            ButtonChrome chrome = ((ButtonChrome)o);

            if (chrome.Animates)
            {
                if (!chrome.RenderPressed)
                {
                    if (((bool)e.NewValue))
                    {
                        if (chrome._localResources == null)
                        {
                            chrome._localResources = new LocalResources();
                            chrome.InvalidateVisual();
                        }

                        Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

                        DoubleAnimation da = new DoubleAnimation(1, duration);

                        chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        chrome.BackgroundOverlay.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    }
                    else if (chrome._localResources == null)
                    {
                        chrome.InvalidateVisual();
                    }
                    else
                    {
                        if (chrome.RenderDefaulted)
                        {
                            // Since the mouse was over the button the opacity should be 1.0
                            // Create a repeating animation like:
                            //  \__/ \__/ \__/ \_...
                            // But if the user quickly mouses over a button, the opacity may not be 1 yet
                            // so the first keyframe brings the opacity to 1
                            double currentOpacity = chrome.BackgroundOverlay.Opacity;

                            // This is the time needed to complete the animation to 1:
                            double to1 = (1.0 - currentOpacity) * 0.5;

                            DoubleAnimationUsingKeyFrames daukf = new DoubleAnimationUsingKeyFrames();
                            daukf.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(to1)));
                            daukf.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, TimeSpan.FromSeconds(to1 + 0.25)));
                            daukf.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(to1 + 1.5)));
                            daukf.KeyFrames.Add(new LinearDoubleKeyFrame(currentOpacity, TimeSpan.FromSeconds(2)));
                            daukf.RepeatBehavior = RepeatBehavior.Forever;
                            DoubleAnimationUsingKeyFrames.SetDesiredFrameRate(daukf, 10);
                            chrome.BackgroundOverlay.BeginAnimation(LinearGradientBrush.OpacityProperty, daukf);
                            chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, daukf);
                        }
                        else
                        {
                            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

                            DoubleAnimation da = new DoubleAnimation();
                            da.Duration = duration;

                            chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                            chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        }
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
        /// DependencyProperty for <see cref="RenderPressed" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderPressedProperty =
                 DependencyProperty.Register("RenderPressed",
                         typeof(bool),
                         typeof(ButtonChrome),
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
            ButtonChrome chrome = ((ButtonChrome)o);

            if (chrome.Animates)
            {
                if (((bool)e.NewValue))
                {
                    if (chrome._localResources == null)
                    {
                        chrome._localResources = new LocalResources();
                        chrome.InvalidateVisual();
                    }

                    Duration duration = new Duration(TimeSpan.FromSeconds(0.1));

                    DoubleAnimation da = new DoubleAnimation(1, duration);

                    chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                    chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                    chrome.LeftDropShadowBrush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    chrome.TopDropShadowBrush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);

                    da = new DoubleAnimation(0, duration);
                    chrome.InnerBorderPen.Brush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);

                    ColorAnimation ca = new ColorAnimation(Color.FromRgb(0xC2, 0xE4, 0xF6), duration);
                    GradientStopCollection gsc = ((LinearGradientBrush)chrome.BackgroundOverlay).GradientStops;
                    gsc[0].BeginAnimation(GradientStop.ColorProperty, ca);
                    gsc[1].BeginAnimation(GradientStop.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0xAB, 0xDA, 0xF3), duration);
                    gsc[2].BeginAnimation(GradientStop.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0x90, 0xCB, 0xEB), duration);
                    gsc[3].BeginAnimation(GradientStop.ColorProperty, ca);

                    ca = new ColorAnimation(Color.FromRgb(0x2C, 0x62, 0x8B), duration);
                    chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                }
                else if (chrome._localResources == null)
                {
                    chrome.InvalidateVisual();
                }
                else
                {
                    bool renderMouseOver = chrome.RenderMouseOver;
                    Duration duration = new Duration(TimeSpan.FromSeconds(0.1));

                    DoubleAnimation da = new DoubleAnimation();
                    da.Duration = duration;
                    chrome.LeftDropShadowBrush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    chrome.TopDropShadowBrush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);
                    chrome.InnerBorderPen.Brush.BeginAnimation(LinearGradientBrush.OpacityProperty, da);

                    if (!renderMouseOver)
                    {
                        chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                        chrome.BackgroundOverlay.BeginAnimation(SolidColorBrush.OpacityProperty, da);
                    }

                    ColorAnimation ca = new ColorAnimation();
                    ca.Duration = duration;
                    chrome.BorderOverlayPen.Brush.BeginAnimation(SolidColorBrush.ColorProperty, ca);

                    GradientStopCollection gsc = ((LinearGradientBrush)chrome.BackgroundOverlay).GradientStops;
                    gsc[0].BeginAnimation(GradientStop.ColorProperty, ca);
                    gsc[1].BeginAnimation(GradientStop.ColorProperty, ca);
                    gsc[2].BeginAnimation(GradientStop.ColorProperty, ca);
                    gsc[3].BeginAnimation(GradientStop.ColorProperty, ca);
                }
            }
            else
            {
                chrome._localResources = null;
                chrome.InvalidateVisual();
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="RoundCorners" /> property.
        /// </summary>
        public static readonly DependencyProperty RoundCornersProperty =
                 DependencyProperty.Register("RoundCorners",
                         typeof(bool),
                         typeof(ButtonChrome),
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true, the left border will have round corners, otherwise they will be square.
        /// </summary>
        public bool RoundCorners
        {
            get { return (bool)GetValue(RoundCornersProperty); }
            set { SetValue(RoundCornersProperty, value); }
        }

        #endregion Dynamic Properties

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the ButtonChrome.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// ButtonChrome basically inflates the desired size of its one child by 2 on all four sides
        /// </remarks>
        /// <param name="availableSize">Available size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The ButtonChrome's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desired;
            UIElement child = Child;
            if (child != null)
            {
                Size childConstraint = new Size();
                bool isWidthTooSmall = (availableSize.Width < 4.0);
                bool isHeightTooSmall = (availableSize.Height < 4.0);

                if (!isWidthTooSmall)
                {
                    childConstraint.Width = availableSize.Width - 4.0;
                }
                if (!isHeightTooSmall)
                {
                    childConstraint.Height = availableSize.Height - 4.0;
                }

                child.Measure(childConstraint);

                desired = child.DesiredSize;

                if (!isWidthTooSmall)
                {
                    desired.Width += 4.0;
                }
                if (!isHeightTooSmall)
                {
                    desired.Height += 4.0;
                }
            }
            else
            {
                desired = new Size(Math.Min(4.0, availableSize.Width), Math.Min(4.0, availableSize.Height));
            }

            return desired;
        }

        /// <summary>
        /// ButtonChrome computes the position of its single child inside child's Margin and calls Arrange
        /// on the child.
        /// </summary>
        /// <remarks>
        /// ButtonChrome basically inflates the desired size of its one child by 2 on all four sides
        /// </remarks>
        /// <param name="finalSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect childArrangeRect = new Rect();

            childArrangeRect.Width = Math.Max(0d, finalSize.Width - 4.0);
            childArrangeRect.Height = Math.Max(0d, finalSize.Height - 4.0);
            childArrangeRect.X = (finalSize.Width - childArrangeRect.Width) * 0.5;
            childArrangeRect.Y = (finalSize.Height - childArrangeRect.Height) * 0.5;

            UIElement child = Child;
            if (child != null)
            {
                child.Arrange(childArrangeRect);
            }

            return finalSize;
        }


        /// <summary>
        /// Render callback.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);

            // Draw Background
            DrawBackground(drawingContext, ref bounds);

            // Draw Border dropshadows
            DrawDropShadows(drawingContext, ref bounds);

            // Draw outer border
            DrawBorder(drawingContext, ref bounds);

            // Draw innerborder
            DrawInnerBorder(drawingContext, ref bounds);
        }


        private void DrawBackground(DrawingContext dc, ref Rect bounds)
        {
            if (!IsEnabled && !RoundCorners)
                return;

            Brush fill = Background;
            if ((bounds.Width > 4.0) && (bounds.Height > 4.0))
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
                if( fill != null)
                    dc.DrawRectangle(fill, null, backgroundRect);
            }
        }

        // Draw the Pressed dropshadows
        private void DrawDropShadows(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width > 4.0) && (bounds.Height > 4.0))
            {
                Brush leftShadow = LeftDropShadowBrush;

                if (leftShadow != null)
                {
                    dc.DrawRectangle(leftShadow, null, new Rect(1.0, 1.0, 2.0, bounds.Bottom - 2.0));
                }

                Brush topShadow = TopDropShadowBrush;

                if (topShadow != null)
                {
                    dc.DrawRectangle(topShadow, null, new Rect(1.0, 1.0, bounds.Right - 2.0, 2.0));
                }
            }
        }

        // Draw the main border
        private void DrawBorder(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 5.0) && (bounds.Height >= 5.0))
            {
                Brush border = BorderBrush;
                Pen pen = null;

                if (border != null)
                {
                    if (_commonBorderPen == null)   // Common case, if non-null, avoid the lock
                    {
                        lock (_resourceAccess )   // If non-null, lock to create the pen for thread safety
                        {
                            if (_commonBorderPen == null)   // Check again in case _pen was created within the last line
                            {
                                // Assume that the first render of Button uses the most common brush for the app.
                                // This breaks down if (a) the first Button is disabled, (b) the first Button is
                                // customized, or (c) ButtonChrome becomes more broadly used than just on Button.
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


                Pen overlayPen = BorderOverlayPen;

                if (pen != null || overlayPen != null)
                {
                    if (RoundCorners)
                    {
                        Rect rect = new Rect(bounds.Left + 0.5,
                                            bounds.Top + 0.5,
                                            bounds.Width - 1.0,
                                            bounds.Height - 1.0);

                        if (IsEnabled && pen != null)
                            dc.DrawRoundedRectangle(null, pen, rect, 2.75, 2.75);

                        if (overlayPen != null)
                            dc.DrawRoundedRectangle(null, overlayPen, rect, 2.75, 2.75);
                    }
                    else
                    {
                        // Left side is flat, have to generate a geometry because
                        // DrawRoundedRectangle does not let you specify per corner radii
                        PathFigure borderFigure = new PathFigure();

                        borderFigure.StartPoint = new Point(0.5, 0.5);
                        borderFigure.Segments.Add(new LineSegment(new Point(0.5, bounds.Bottom - 0.5), true));
                        borderFigure.Segments.Add(new LineSegment(new Point(bounds.Right - 2.5, bounds.Bottom - 0.5), true));
                        borderFigure.Segments.Add(new ArcSegment(new Point(bounds.Right - 0.5, bounds.Bottom - 2.5), new Size(2.0, 2.0), 0.0, false, SweepDirection.Counterclockwise, true));
                        borderFigure.Segments.Add(new LineSegment(new Point(bounds.Right - 0.5, bounds.Top + 2.5), true));
                        borderFigure.Segments.Add(new ArcSegment(new Point(bounds.Right - 2.5, bounds.Top + 0.5), new Size(2.0, 2.0), 0.0, false, SweepDirection.Counterclockwise, true));
                        borderFigure.IsClosed = true;

                        PathGeometry borderGeometry = new PathGeometry();
                        borderGeometry.Figures.Add(borderFigure);

                        if (IsEnabled && pen != null)
                            dc.DrawGeometry(null, pen, borderGeometry);

                        if (overlayPen != null)
                            dc.DrawGeometry(null, overlayPen, borderGeometry);
                    }
                }
            }
        }


        // Draw the inner border
        private void DrawInnerBorder(DrawingContext dc, ref Rect bounds)
        {
            if (!IsEnabled && !RoundCorners)
                return;

            if ((bounds.Width >= 4.0) && (bounds.Height >= 4.0))
            {
                Pen innerBorder = InnerBorderPen;

                if (innerBorder != null)
                {
                    dc.DrawRoundedRectangle(null, innerBorder, new Rect(bounds.Left + 1.5, bounds.Top + 1.5, bounds.Width - 3.0, bounds.Height - 3.0), 1.75, 1.75);
                }
            }
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
                return SystemParameters.PowerLineStatus == PowerLineStatus.Online &&
                       SystemParameters.ClientAreaAnimation &&
                       RenderCapability.Tier > 0 &&
                       IsEnabled;
            }
        }


        private static LinearGradientBrush CommonHoverBackgroundOverlay
        {
            get
            {
                if (_commonHoverBackgroundOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverBackgroundOverlay == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEA, 0xF6, 0xFD), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD9, 0xF0, 0xFC), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBE, 0xE6, 0xFD), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA7, 0xD9, 0xF5), 1));

                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonHoverBackgroundOverlay = temp;
                        }
                    }
                }
                return _commonHoverBackgroundOverlay;
            }
        }

        private static LinearGradientBrush CommonPressedBackgroundOverlay
        {
            get
            {
                if (_commonPressedBackgroundOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedBackgroundOverlay == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC2, 0xE4, 0xF6), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xAB, 0xDA, 0xF3), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x90, 0xCB, 0xEB), 1));

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

        private Pen BorderOverlayPen
        {
            get
            {
                if (!IsEnabled)
                {
                    if (RoundCorners)
                    {
                        return CommonDisabledBorderOverlay;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (!Animates)
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
                    return null;
                }
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

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0,0);
                            brush.EndPoint = new Point(0,1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFA,0xFF,0xFF,0xFF), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x85,0xFF,0xFF,0xFF), 1));

                            temp.Brush = brush;
                            temp.Freeze();
                            _commonInnerBorderPen = temp;
                        }
                    }
                }
                return _commonInnerBorderPen;
            }
        }


        private static Pen CommonDefaultedInnerBorderPen
        {
            get
            {
                if (_commonDefaultedInnerBorderPen == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDefaultedInnerBorderPen == null)
                        {
                            Pen temp = new Pen();
                            temp.Thickness = 1;
                            temp.Brush = new SolidColorBrush(Color.FromArgb(0xF9, 0x00, 0xCC, 0xFF));
                            temp.Freeze();
                            _commonDefaultedInnerBorderPen = temp;
                        }
                    }
                }
                return _commonDefaultedInnerBorderPen;
            }
        }



        private Pen InnerBorderPen
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonInnerBorderPen;
                }

                if (!Animates)
                {
                    if (RenderPressed)
                    {
                        return null;
                    }
                    else if (RenderDefaulted)
                    {
                        return CommonDefaultedInnerBorderPen;
                    }
                    else
                    {
                        return CommonInnerBorderPen;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.InnerBorderPen == null)
                    {
                        _localResources.InnerBorderPen = CommonInnerBorderPen.Clone();
                    }
                    return _localResources.InnerBorderPen;
                }
                else
                {
                    return CommonInnerBorderPen;
                }
            }
        }

        private static LinearGradientBrush CommonPressedLeftDropShadowBrush
        {
            get
            {
                if (_commonPressedLeftDropShadowBrush == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedLeftDropShadowBrush == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(1, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0x33, 0x33, 0x33), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x33, 0x33, 0x33), 1));

                            temp.Freeze();
                            _commonPressedLeftDropShadowBrush = temp;
                        }
                    }
                }
                return _commonPressedLeftDropShadowBrush;
            }
        }


        private LinearGradientBrush LeftDropShadowBrush
        {
            get
            {
                if (!IsEnabled)
                {
                    return null;
                }

                if (!Animates)
                {
                    if (RenderPressed)
                    {
                        return CommonPressedLeftDropShadowBrush;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.LeftDropShadowBrush == null)
                    {
                        _localResources.LeftDropShadowBrush = CommonPressedLeftDropShadowBrush.Clone();
                        _localResources.LeftDropShadowBrush.Opacity = 0;
                    }
                    return _localResources.LeftDropShadowBrush;
                }
                else
                {
                    return null;
                }
            }
        }
        private static LinearGradientBrush CommonPressedTopDropShadowBrush
        {
            get
            {
                if (_commonPressedTopDropShadowBrush == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedTopDropShadowBrush == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0);
                            temp.EndPoint = new Point(0, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, 0x33, 0x33, 0x33), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x33, 0x33, 0x33), 1));

                            temp.Freeze();
                            _commonPressedTopDropShadowBrush = temp;
                        }
                    }
                }
                return _commonPressedTopDropShadowBrush;
            }
        }

        private LinearGradientBrush TopDropShadowBrush
        {
            get
            {
                if (!IsEnabled)
                {
                    return null;
                }

                if (!Animates)
                {
                    if (RenderPressed)
                    {
                        return CommonPressedTopDropShadowBrush;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (_localResources != null)
                {
                    if (_localResources.TopDropShadowBrush == null)
                    {
                        _localResources.TopDropShadowBrush = CommonPressedTopDropShadowBrush.Clone();
                        _localResources.TopDropShadowBrush.Opacity = 0;
                    }
                    return _localResources.TopDropShadowBrush;
                }
                else
                {
                    return null;
                }
            }
        }

        // Common LocalResources
        private static Pen _commonBorderPen;
        private static Pen _commonInnerBorderPen;

        private static Pen _commonDisabledBorderOverlay;
        private static SolidColorBrush _commonDisabledBackgroundOverlay;

        private static Pen _commonDefaultedInnerBorderPen;

        private static LinearGradientBrush _commonHoverBackgroundOverlay;
        private static Pen _commonHoverBorderOverlay;


        private static LinearGradientBrush _commonPressedBackgroundOverlay;
        private static Pen _commonPressedBorderOverlay;

        private static LinearGradientBrush _commonPressedLeftDropShadowBrush;
        private static LinearGradientBrush _commonPressedTopDropShadowBrush;


        private static object _resourceAccess = new object();

        // Per instance resources

        private LocalResources _localResources;

        private class LocalResources
        {
            public Pen BorderOverlayPen;
            public Pen InnerBorderPen;
            public LinearGradientBrush BackgroundOverlay;
            public LinearGradientBrush LeftDropShadowBrush;
            public LinearGradientBrush TopDropShadowBrush;
        }

        #endregion
    }
}

