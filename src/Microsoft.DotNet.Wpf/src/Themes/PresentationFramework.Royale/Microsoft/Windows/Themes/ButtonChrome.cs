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
    /// <summary>
    /// The ButtonChrome element
    /// This element is a theme-specific type that is used as an optimization
    /// for a common complex rendering used in Royale
    ///   
    /// </summary>
    /// <ExternalAPI/>
    
    // This is functionally equivalent to the following visual tree:
    //
    //   <Grid>
    //       <RowDefinition Height="*"/>
    //       <ColumnDefinition Width="*"/>
    //
    //       <Rectangle
    //           StrokeThickness="1pt" 
    //           RadiusX="3"
    //           RadiusY="3"
    //           Stroke = [OuterHighlightProperty.Value]
    //           Fill="Transparent" />
    //
    //       <!-- Actual Background -->
    //       <Rectangle
    //           Grid.Left = "0.75"
    //           Grid.Top = "0.75"
    //           Grid.Right="0.75"
    //           Grid.Bottom="0.75"
    //           Fill = [FillProperty.Value] 
    //           RadiusX="4" 
    //           RadiusY="4" 
    //           StrokeThickness="0" />
    //       
    //       <!-- Top Shade -->
    //       <Rectangle
    //           Height="6px"
    //           Grid.Left = "0.75"
    //           Grid.Top = "0.75"
    //           Grid.Right="0.75"
    //           Grid.Bottom = "Auto"
    //           Fill = [TopShadeProperty.Value]
    //           Margin="0.3" 
    //           RadiusX="4" 
    //           RadiusY="4"
    //           StrokeThickness="0" />
    //       
    //       <!-- Bottom Shade -->
    //       <Rectangle 
    //           Height="6px"
    //           Grid.Bottom = "0.75"
    //           Grid.Left = "0.75"
    //           Grid.Right = "0.75"
    //           Grid.Top = "Auto"
    //           Fill = [BottomShadeProperty.Value]
    //           Margin="0.3"
    //           RadiusX="4"
    //           RadiusY="4"
    //           StrokeThickness="0" />
    //
    //       <!-- Left Shade -->
    //       <Rectangle
    //           Width="6px" 
    //           Grid.Bottom = "0.75"
    //           Grid.Left = "0.75"
    //           Grid.Top = "0.75"
    //           Grid.Right = "Auto"
    //           Fill = [LeftShadeProperty.Value]
    //           Margin="0.3"
    //           RadiusX="4"
    //           RadiusY="4"
    //           StrokeThickness="0" />
    //           
    //       <!-- Right Shade -->
    //       <Rectangle
    //           Width="6px"
    //           Grid.Bottom = "0.75"
    //           Grid.Left = "Auto"
    //           Grid.Right = "0.75"
    //           Grid.Top = "0.75"
    //           Fill = [RightShadeProperty.Value]
    //           Margin="0.3"
    //           RadiusX="4"
    //           RadiusY="4"
    //           StrokeThickness="0" />
    //
    //       <!-- Inner Highlight (for MouseOver and Focused State) -->
    //       <Rectangle
    //           Grid.Left = "0.75"
    //           Grid.Right = "0.75"
    //           Grid.Top = "0.75"
    //           Grid.Bottom ="0.75"
    //           StrokeThickness="2pt"  
    //           RadiusX="3" 
    //           RadiusY="3" 
    //           Stroke = [InnerHighlightProperty.Value] 
    //           Fill="Transparent" />
    //       
    //       <!-- Border -->
    //       <Rectangle 
    //           Grid.Left = "0.75"
    //           Grid.Right = "0.75"
    //           Grid.Top = "0.75"
    //           Grid.Bottom ="0.75"
    //           StrokeThickness="0.75pt"  
    //           RadiusX="3" 
    //           RadiusY="3" 
    //           Stroke = [BorderBrushProperty.Value] 
    //           Fill="Transparent" />
    //           
    //       <!-- Button Content -->
    //       <Border
    //           Grid.Left = "0.75"
    //           Grid.Right = "0.75"
    //           Grid.Top = "0.75"
    //           Grid.Bottom ="0.75"
    //       >
    //   </Grid>
    
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
        /// DependencyProperty for <see cref="Fill" /> property.
        /// </summary>
        public static readonly DependencyProperty FillProperty = 
                Shape.FillProperty.AddOwner(
                        typeof(ButtonChrome),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Fill property defines the brush used to fill the border region.
        /// </summary>
        public Brush Fill
        {
            get { return (Brush) GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
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
        /// The BorderBrush property defines the brush used to fill the border region.
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
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true the chrome renders with a defaulted look.
        /// </summary>
        public bool RenderDefaulted
        {
            get { return (bool) GetValue(RenderDefaultedProperty); }
            set { SetValue(RenderDefaultedProperty, value); }
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
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true the chrome renders with a mouse over look.
        /// </summary>
        public bool RenderMouseOver
        {
            get { return (bool) GetValue(RenderMouseOverProperty); }
            set { SetValue(RenderMouseOverProperty, value); }
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
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true the chrome renders with a pressed look.
        /// </summary>
        public bool RenderPressed
        {
            get { return (bool) GetValue(RenderPressedProperty); }
            set { SetValue(RenderPressedProperty, value); }
        }

        #endregion Dynamic Properties

        #region Protected Methods

        private const double sideThickness = 4.0;
        private const double sideThickness2 = 2 * sideThickness;

        /// <summary>
        /// Updates DesiredSize of the ButtonChrome.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// ButtonChrome basically inflates the desired size of its one child by 4 on all four sides
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
                bool isWidthTooSmall = (availableSize.Width < sideThickness2);
                bool isHeightTooSmall = (availableSize.Height < sideThickness2);

                if (!isWidthTooSmall)
                {
                    childConstraint.Width = availableSize.Width - sideThickness2;
                }
                if (!isHeightTooSmall)
                {
                    childConstraint.Height = availableSize.Height - sideThickness2;
                }

                child.Measure(childConstraint);

                desired = child.DesiredSize;

                if (!isWidthTooSmall)
                {
                    desired.Width += sideThickness2;
                }
                if (!isHeightTooSmall)
                {
                    desired.Height += sideThickness2;
                }
            }
            else
            {
                desired = new Size(Math.Min(sideThickness2, availableSize.Width), Math.Min(sideThickness2, availableSize.Height));
            }

            return desired;
        }

        /// <summary>
        /// ButtonChrome computes the position of its single child inside child's Margin and calls Arrange
        /// on the child.
        /// </summary>
        /// <remarks>
        /// ButtonChrome basically inflates the desired size of its one child by 4 on all four sides
        /// </remarks>
        /// <param name="finalSize">Size the ContentPresenter will assume.</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect childArrangeRect = new Rect();

            childArrangeRect.Width = Math.Max(0d, finalSize.Width - sideThickness2);
            childArrangeRect.Height = Math.Max(0d, finalSize.Height - sideThickness2);
            childArrangeRect.X = (finalSize.Width - childArrangeRect.Width) * 0.5;
            childArrangeRect.Y = (finalSize.Height - childArrangeRect.Height) * 0.5;

            UIElement child = Child;
            if (child != null)
            {
                child.Arrange(childArrangeRect);
            }

            return finalSize;
        }

        private bool DrawBackground(DrawingContext dc, ref Rect bounds)
        {
            // draw actual background
            Brush brush = Background;
            if (brush != null)
            {
                dc.DrawRoundedRectangle(brush, null, bounds, 3.0, 3.0);
            }

            if ((bounds.Width < 0.6) || (bounds.Height < 0.6))
            {
                // out of space; we're done
                return true;
            }

            return false;
        }

        private void DrawShades(DrawingContext dc, ref Rect bounds)
        {
            // shades are inset an additional 0.3
            bounds.Inflate(-0.3, -0.3);

            // draw top shade
            Brush brush = TopShade;
            if (brush != null)
            {
                dc.DrawRoundedRectangle(brush, null, new Rect(bounds.Left, bounds.Top, bounds.Width, 6.0), 3.0, 3.0);
            }

            // draw bottom shade
            brush = BottomShade;
            if (brush != null)
            {
                dc.DrawRoundedRectangle(brush, null, new Rect(bounds.Left, bounds.Bottom - 6.0, bounds.Width, 6.0), 3.0, 3.0);
            }

            // draw left shade
            brush = LeftShade;
            if (brush != null)
            {
                dc.DrawRoundedRectangle(brush, null, new Rect(bounds.Left, bounds.Top, 6.0, bounds.Height), 3.0, 3.0);
            }

            // draw right shade
            brush = RightShade;
            if (brush != null)
            {
                dc.DrawRoundedRectangle(brush, null, new Rect(bounds.Right - 6.0, bounds.Top, 6.0, bounds.Height), 3.0, 3.0);
            }

            // dones with shades; outset bounds
            bounds.Inflate(0.3, 0.3);
        }

        private void DrawInnerHighlight(DrawingContext dc, ref Rect bounds)
        {
            // draw inner highlight
            Pen pen = InnerHighlight;
            if (pen != null && (bounds.Width >= (8.0 / 3.0)) && (bounds.Height >= (8.0 / 3.0)))
            {
                dc.DrawRoundedRectangle(
                        null,
                        pen,
                        new Rect(
                                bounds.Left + 4.0 / 3.0,
                                bounds.Top + 4.0 / 3.0,
                                bounds.Width - 8.0 / 3.0,
                                bounds.Height - 8.0 / 3.0),
                        2.0,
                        2.0);
            }
        }

        private void DrawBorder(DrawingContext dc, ref Rect bounds)
        {
            // draw border
            Pen borderPen = BorderPen;
            if ((borderPen != null) && (bounds.Width >= 1.0) && (bounds.Height >= 1.0))
            {
                dc.DrawRoundedRectangle(
                        null,
                        borderPen,
                        new Rect(
                                bounds.Left + 0.5,
                                bounds.Top + 0.5,
                                bounds.Width - 1.0,
                                bounds.Height - 1.0),
                        2.0,
                        2.0);
            }
        }

        /// <summary>
        /// Render callback.  
        /// Note: Assumes OuterHighlight.Thickness = 1pt and InnerHighlight.Thickness = 2pt
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect bounds = new Rect(0, 0, ActualWidth, ActualHeight);

            if (DrawBackground(drawingContext, ref bounds))
            {
                // Out of space, stop
                return;
            }

            DrawShades(drawingContext, ref bounds);
            DrawInnerHighlight(drawingContext, ref bounds);
            DrawBorder(drawingContext, ref bounds);
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
            get { return 12; }
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
                            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(0xFF,0xC6,0xC5,0xC9));

                            Pen temp = new Pen(brush, 1);
                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonDisabledBorderPen = temp;
                        }
                    }
                }
                return _commonDisabledBorderPen;
            }
        }

        private Pen BorderPen
        {
            get
            {
                if (!IsEnabled)
                    return CommonDisabledBorderPen;

                Pen pen = null;

                Brush brush = BorderBrush;
                if (brush != null)
                {
                    if (_commonBorderPen == null)   // Common case, if non-null, avoid the lock
                    {
                        lock (_resourceAccess)   // If non-null, lock to create the pen for thread safety
                        {
                            if (_commonBorderPen == null)   // Check again in case _commonBorderPen was created within the last line
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
                                if (!brush.IsFrozen && brush.CanFreeze)
                                {
                                    brush = brush.Clone();
                                    brush.Freeze();
                                }

                                Pen commonPen = new Pen(brush, 1);
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

                    
                    if (_commonBorderPen != null && brush == _commonBorderPen.Brush)
                    {
                        pen = _commonBorderPen;
                    }
                    else
                    {
                        if (!brush.IsFrozen && brush.CanFreeze)
                        {
                            brush = brush.Clone();
                            brush.Freeze();
                        }

                        pen = new Pen(brush, 1);
                        if (pen.CanFreeze)
                        {
                            pen.Freeze();
                        }
                    }
                }
                return pen;
            }
        }

        private static Pen CommonOuterHighlight
        {
            get
            {
                if (_commonOuterHighlight == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonOuterHighlight == null)
                        {
                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0,0);
                            brush.EndPoint = new Point(0.4,1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x20,0x00,0x00,0x00), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00,0xFF,0xFF,0xFF), 0.5));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x80,0xFF,0xFF,0xFF), 1));

                            Pen temp = new Pen(brush, 1.3333333333);
                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonOuterHighlight = temp;
                        }
                    }
                }
                return _commonOuterHighlight;
            }
        }

        private Pen OuterHighlight
        {
            get
            {
                if (!IsEnabled)
                    return null;
                return CommonOuterHighlight;
            }
        }

        private static Pen CommonDefaultedInnerHighlight
        {
            get
            {
                if (_commonDefaultedInnerHighlight == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonDefaultedInnerHighlight == null)
                        {
                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0.5, 0);
                            brush.EndPoint = new Point(0.5, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCE, 0xE7, 0xFF), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBC, 0xD4, 0xF6), 0.3));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x89, 0xAD, 0xE4), 0.97));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x69, 0x82, 0xEE), 1));

                            Pen temp = new Pen(brush, 2.6666666667);
                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonDefaultedInnerHighlight = temp;
                        }
                    }
                }
                return _commonDefaultedInnerHighlight;
            }
        }

      
        private static Pen CommonHoverInnerHighlight
        {
            get
            {
                if (_commonHoverInnerHighlight == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonHoverInnerHighlight == null)
                        {
                            LinearGradientBrush brush = new LinearGradientBrush();
                            brush.StartPoint = new Point(0.5, 0);
                            brush.EndPoint = new Point(0.5, 1);

                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xF0, 0xCF), 0));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFC, 0xD2, 0x79), 0.03));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF8, 0xB7, 0x3B), 0.75));
                            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE5, 0x97, 0x00), 1));

                            Pen temp = new Pen(brush, 2.6666666667);
                            temp.Freeze();

                            // Static field must not be set until the local has been frozen
                            _commonHoverInnerHighlight = temp;
                        }
                    }
                }
                return _commonHoverInnerHighlight;
            }
        }


        private Pen InnerHighlight
        {
            get
            {
                if (!IsEnabled || RenderPressed)
                {
                    return null;
                }
                if (RenderMouseOver)
                {
                    return CommonHoverInnerHighlight;
                }
                if (RenderDefaulted)
                {
                    return CommonDefaultedInnerHighlight;
                }
                return null;
            }
        }


        private static LinearGradientBrush CommonBottomShade
        {
            get
            {
                if (_commonBottomShade == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonBottomShade == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5,0);
                            temp.EndPoint = new Point(0.5,1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00,0xFF,0xFF,0xFF), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x35,0x59,0x2F,0x00), 1));
                            temp.Freeze();

                            _commonBottomShade = temp;
                        }
                    }
                }
                return _commonBottomShade;
            }
        }

        private static LinearGradientBrush CommonPressedBottomShade
        {
            get
            {
                if (_commonPressedBottomShade == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedBottomShade == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 0);
                            temp.EndPoint = new Point(0.5, 1);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.6));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 1));
                            temp.Freeze();

                            _commonPressedBottomShade = temp;
                        }
                    }
                }
                return _commonPressedBottomShade;
            }
        }

        private LinearGradientBrush BottomShade
        {
            get
            {
                if (!IsEnabled)
                    return null;
                if (RenderPressed)
                    return CommonPressedBottomShade;

                return CommonBottomShade;
            }
        }

        private static LinearGradientBrush CommonRightShade
        {
            get
            {
                if (_commonRightShade == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonRightShade == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0, 0.5);
                            temp.EndPoint = new Point(1, 0.5);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00,0xFF,0xFF,0xFF), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF,0xFF,0xFF,0xFF), 1));
                            temp.Freeze();

                            _commonRightShade = temp;
                        }
                    }
                }
                return _commonRightShade;
            }
        }

        private LinearGradientBrush RightShade
        {
            get
            {
                if (!IsEnabled || RenderPressed)
                    return null;

                return CommonRightShade;
                
            }
        }

        private static LinearGradientBrush CommonPressedTopShade
        {
            get
            {
                if (_commonPressedTopShade == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedTopShade == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 1);
                            temp.EndPoint = new Point(0.5, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF,0x97,0x8B,0x72), 1));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00,0xFF,0xFF,0xFF), 0.6));
                            temp.Freeze();

                            _commonPressedTopShade = temp;
                        }
                    }
                }
                return _commonPressedTopShade;
            }
        }

        private LinearGradientBrush TopShade
        {
            get
            {
                if (!IsEnabled)
                    return null;
                if (RenderPressed)
                    return CommonPressedTopShade;
                return null;
            }
        }

        private static LinearGradientBrush CommonLeftShade
        {
            get
            {
                if (_commonLeftShade == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonLeftShade == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(1, 0.5);
                            temp.EndPoint = new Point(0, 0.5);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF,0xFF,0xFF,0xFF), 1));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00,0xFF,0xFF,0xFF), 0.5));
                            temp.Freeze();

                            _commonLeftShade = temp;
                        }
                    }
                }
                return _commonLeftShade;
            }
        }

        private static LinearGradientBrush CommonPressedLeftShade
        {
            get
            {
                if (_commonPressedLeftShade == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedLeftShade == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(1, 0.5);
                            temp.EndPoint = new Point(0, 0.5);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF,0xAA,0x9D,0x87), 1));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0x00,0xFF,0xFF,0xFF), 0.6));
                            temp.Freeze();

                            _commonPressedLeftShade = temp;
                        }
                    }
                }
                return _commonPressedLeftShade;
            }
        }


        private LinearGradientBrush LeftShade
        {
            get
            {
                if (!IsEnabled)
                    return null;
                if (RenderPressed)
                    return CommonPressedLeftShade;
                return CommonLeftShade;
            }
        }

        private static SolidColorBrush CommonDisabledFill
        {
            get
            {
                return Brushes.White;
            }
        }



        private static LinearGradientBrush CommonPressedFill
        {
            get
            {
                if (_commonPressedFill == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_commonPressedFill == null)
                        {
                            LinearGradientBrush temp = new LinearGradientBrush();
                            temp.StartPoint = new Point(0.5, 1);
                            temp.EndPoint = new Point(0.5, 0);

                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE3, 0xEB, 0xF3), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD0, 0xDC, 0xEB), 0.5));
                            temp.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA6, 0xB8, 0xCF), 1));
                            temp.Freeze();

                            _commonPressedFill = temp;
                        }
                    }
                }
                return _commonPressedFill;
            }
        }


        private Brush Background
        {
            get
            {
                if (!IsEnabled)
                    return CommonDisabledFill;
                if (RenderPressed)
                    return CommonPressedFill;
                return Fill;
            }
        }
        
        private static Pen _commonBorderPen;
        private static Pen _commonDisabledBorderPen;

        private static Pen _commonOuterHighlight;

        private static LinearGradientBrush _commonBottomShade;

        private static LinearGradientBrush _commonRightShade;
        private static LinearGradientBrush _commonLeftShade;
        
        
        private static Pen _commonDefaultedInnerHighlight;
        
        private static Pen _commonHoverInnerHighlight;
        
        private static LinearGradientBrush _commonPressedBottomShade;
        private static LinearGradientBrush _commonPressedTopShade;
        private static LinearGradientBrush _commonPressedLeftShade;
        
        private static LinearGradientBrush _commonPressedFill;

        private static object _resourceAccess = new object();

      
        #endregion
    }
}

