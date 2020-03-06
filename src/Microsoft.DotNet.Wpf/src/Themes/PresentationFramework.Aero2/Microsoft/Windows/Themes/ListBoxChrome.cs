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
    /// The ListBoxChrome element
    /// This element is a theme-specific type that is used as an optimization
    /// for a common complex rendering used in Aero
    ///   
    /// </summary>
    public sealed class ListBoxChrome : Decorator
    {
        #region Constructors

        static ListBoxChrome()
        {
            IsEnabledProperty.OverrideMetadata(typeof(ListBoxChrome), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        }

        /// <summary>
        /// Instantiates a new instance of a ListBoxChrome with no parent element.
        /// </summary>
        /// <ExternalAPI/>
        public ListBoxChrome()
        {
        }

        #endregion Constructors

        #region Dynamic Properties

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty = 
                    Control.BackgroundProperty.AddOwner(
                            typeof(ListBoxChrome),
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
                        typeof(ListBoxChrome),
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
                Border.BorderThicknessProperty.AddOwner(
                        typeof(ListBoxChrome),
                        new FrameworkPropertyMetadata(
                                new Thickness(1),
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The BorderThickness property defines the thickness of the border.
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
                         typeof(ListBoxChrome),
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
            ListBoxChrome chrome = ((ListBoxChrome)o);

            if (chrome.RenderFocused)
            {
                return;
            }

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

                    DoubleAnimation da = new DoubleAnimation(1, duration);

                    chrome.BorderOverlayPen.Brush.BeginAnimation(Brush.OpacityProperty, da);
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

                    chrome.BorderOverlayPen.Brush.BeginAnimation(Brush.OpacityProperty, da);
                }
            }
            else
            {
                chrome._localResources = null;
                chrome.InvalidateVisual();
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="RenderFocused" /> property.
        /// </summary>
        public static readonly DependencyProperty RenderFocusedProperty =
                 DependencyProperty.Register("RenderFocused",
                         typeof(bool),
                         typeof(ListBoxChrome),
                         new FrameworkPropertyMetadata(
                                false,
                                new PropertyChangedCallback(OnRenderFocusedChanged)));

        /// <summary>
        /// When true the chrome renders with a Focused look.
        /// </summary>
        public bool RenderFocused
        {
            get { return (bool)GetValue(RenderFocusedProperty); }
            set { SetValue(RenderFocusedProperty, value); }
        }

        private static void OnRenderFocusedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ListBoxChrome chrome = ((ListBoxChrome)o);

            chrome._localResources = null;
            chrome.InvalidateVisual();
        }

        #endregion Dynamic Properties

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the ListBoxChrome.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <param name="availableSize">Available size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The ListBoxChrome's desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desired;

            Thickness border = BorderThickness;
            double borderX = border.Left + border.Right;
            double borderY = border.Top + border.Bottom;

            // When BorderThickness is 0, draw no border - otherwise add 1 on each side 
            // for inner border
            if (borderX > 0 || borderY > 0)
            {
                borderX += 2.0;
                borderY += 2.0;
            }

            UIElement child = Child;
            if (child != null)
            {
                Size childConstraint = new Size();
                bool isWidthTooSmall = (availableSize.Width < borderX);
                bool isHeightTooSmall = (availableSize.Height < borderY);

                if (!isWidthTooSmall)
                {
                    childConstraint.Width = availableSize.Width - borderX;
                }
                if (!isHeightTooSmall)
                {
                    childConstraint.Height = availableSize.Height - borderY;
                }

                child.Measure(childConstraint);

                desired = child.DesiredSize;

                if (!isWidthTooSmall)
                {
                    desired.Width += borderX;
                }
                if (!isHeightTooSmall)
                {
                    desired.Height += borderY;
                }
            }
            else
            {
                desired = new Size(Math.Min(borderX, availableSize.Width), Math.Min(borderY, availableSize.Height));
            }

            return desired;
        }

        /// <summary>
        /// ListBoxChrome computes the position of its single child inside child's Margin and calls Arrange
        /// on the child.
        /// </summary>
        /// <remarks>
        /// ListBoxChrome basically inflates the desired size of its one child by 2 on all four sides
        /// </remarks>
        /// <param name="finalSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect childArrangeRect = new Rect();

            Thickness border = BorderThickness;
            double borderX = border.Left + border.Right;
            double borderY = border.Top + border.Bottom;

            // When BorderThickness is 0, draw no border - otherwise add 1 on each side 
            // for inner border
            if (borderX > 0 || borderY > 0)
            {
                border.Left += 1.0;
                border.Top += 1.0;
                borderX += 2.0;
                borderY += 2.0;
            }

            if ((finalSize.Width > borderX) && (finalSize.Height > borderY))
            {
                childArrangeRect.X = border.Left;
                childArrangeRect.Y = border.Top;
                childArrangeRect.Width = finalSize.Width - borderX;
                childArrangeRect.Height = finalSize.Height - borderY;
            }

            if (Child != null)
            {
                Child.Arrange(childArrangeRect);
            }

            return finalSize;
        }

        
        /// <summary>
        /// Render callback.  
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);

            Thickness border = BorderThickness;
            double borderX = border.Left + border.Right;
            double borderY = border.Top + border.Bottom;

            // ListBoxChrome is rendered with a border of BorderThickness
            // Inside this is the Background.  Inside the background
            // is the Background overlay for disabled controls which
            // is offset by 1 px.

            bool isSimpleBorder = border.Left == 1.0 && border.Right == 1.0 &&
                                  border.Top == 1.0 && border.Bottom == 1.0;

            double innerBorderThickness = (borderX == 0.0 && borderY == 0.0) ? 0.0 : 1.0;
            double innerBorderThickness2 = 2 * innerBorderThickness;

            if ((bounds.Width > borderX) && 
                (bounds.Height > borderY))
            {
                Rect backgroundRect = new Rect(bounds.Left + border.Left,
                                               bounds.Top + border.Top,
                                               bounds.Width - borderX,
                                               bounds.Height - borderY);

                Brush fill = Background;

                // Draw Background
                if (fill != null)
                    dc.DrawRectangle(fill, null, backgroundRect);

                // Draw Background Overlay inset by 1px from edge of main border
                if ((bounds.Width > borderX + innerBorderThickness2) &&
                    (bounds.Height > borderY + innerBorderThickness2))
                {
                    backgroundRect = new Rect(bounds.Left + border.Left + innerBorderThickness,
                                              bounds.Top + border.Top + innerBorderThickness,
                                              bounds.Width - borderX - innerBorderThickness2,
                                              bounds.Height - borderY - innerBorderThickness2);

                    // Draw BackgroundOverlay
                    fill = BackgroundOverlay;
                    if (fill != null)
                        dc.DrawRoundedRectangle(fill, null, backgroundRect, 1, 1);
                }
            }

            // Draw borders
            // innerBorderThickness is 0 when the listbox border is 0
            if (innerBorderThickness > 0)
            {
                // Draw Main border
                if ((bounds.Width >= borderX) &&
                    (bounds.Height >= borderY))
                {
                    if (isSimpleBorder)
                    {
                        Rect rect = new Rect(bounds.Left + 0.5,
                                            bounds.Top + 0.5,
                                            bounds.Width - 1.0,
                                            bounds.Height - 1.0);

                        Pen pen = GetBorderPen(BorderBrush);
                        Pen overlayPen = BorderOverlayPen;

                        if (pen != null)
                            dc.DrawRoundedRectangle(null, pen, rect, 1.0, 1.0);

                        if (overlayPen != null)
                            dc.DrawRoundedRectangle(null, overlayPen, rect, 1.0, 1.0);
                    }
                    else
                    {
                        Geometry geometry = GetBorderGeometry(border, bounds);

                        if (BorderBrush != null)
                            dc.DrawGeometry(BorderBrush, null, geometry);
                    }
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
                return SystemParameters.ClientAreaAnimation && 
                       RenderCapability.Tier > 0 &&
                       IsEnabled;
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
                            // customized, or (c) ListBoxChrome becomes more broadly used than just on Button.
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

        private static Geometry GetBorderGeometry(Thickness thickness, Rect bounds)
        {
            PathFigure borderFigure = new PathFigure();

            borderFigure.StartPoint = new Point(bounds.Left, bounds.Top);
            borderFigure.Segments.Add(new LineSegment(new Point(bounds.Left, bounds.Bottom), false));
            borderFigure.Segments.Add(new LineSegment(new Point(bounds.Right, bounds.Bottom), false));
            borderFigure.Segments.Add(new LineSegment(new Point(bounds.Right, bounds.Top), false));
            borderFigure.IsClosed = true;

            PathGeometry borderGeometry = new PathGeometry();
            borderGeometry.Figures.Add(borderFigure);

            borderFigure = new PathFigure();

            borderFigure.StartPoint = new Point(bounds.Left + thickness.Left, bounds.Top + thickness.Top);
            borderFigure.Segments.Add(new LineSegment(new Point(bounds.Left + thickness.Left, bounds.Bottom - thickness.Bottom), false));
            borderFigure.Segments.Add(new LineSegment(new Point(bounds.Right - thickness.Right, bounds.Bottom - thickness.Bottom), false));
            borderFigure.Segments.Add(new LineSegment(new Point(bounds.Right - thickness.Right, bounds.Top + thickness.Top), false));
            borderFigure.IsClosed = true;

            borderGeometry.Figures.Add(borderFigure);

            return borderGeometry;
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

                            // Static field must not be set until the local has been frozen
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

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(0, 20);
                            brush.MappingMode = BrushMappingMode.Absolute;

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x57, 0x94, 0xBF), 0.05));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB7, 0xD5, 0xEA), 0.07));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC7, 0xE2, 0xF1), 1));

                            temp.Brush = brush;
                            temp.Freeze();

                            _commonHoverBorderOverlay = temp;
                        }
                    }
                }
                return _commonHoverBorderOverlay;
            }
        }

        private static Pen CommonFocusedBorderOverlay
        {
            get
            {
                if (_commonFocusedBorderOverlay == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonFocusedBorderOverlay == null)
                        {
                            Pen temp = new Pen();
                            

                            temp.Thickness = 1;

                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0, 0);
                            brush.EndPoint = new Point(0, 20);
                            brush.MappingMode = BrushMappingMode.Absolute;

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x3D, 0x7B, 0xAD), 0.05));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA4, 0xC9, 0xE3), 0.07));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB7, 0xD9, 0xED), 1));

                            temp.Brush = brush;
                            temp.Freeze();

                            _commonFocusedBorderOverlay = temp;
                        }
                    }
                }
                return _commonFocusedBorderOverlay;
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

        


        private Brush BackgroundOverlay
        {
            get
            {
                if (!IsEnabled)
                {
                    return CommonDisabledBackgroundOverlay;
                }
                else
                {
                    return null;
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

                if (RenderFocused)
                {
                    return CommonFocusedBorderOverlay;
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

        private static SolidColorBrush _commonDisabledBackgroundOverlay;

        private static Pen _commonBorderPen; 
        
        private static Pen _commonDisabledBorderOverlay; 
        private static Pen _commonHoverBorderOverlay;
        private static Pen _commonFocusedBorderOverlay;

        private static object _resourceAccess = new object();        
        
        // Per instance resources
    
        private LocalResources _localResources;

        private class LocalResources
        {
            public Pen BorderOverlayPen;
        }        

        #endregion
    }
}

