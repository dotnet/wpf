// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using MS.Internal;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Data;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MS.Internal.KnownBoxes;

using MS.Win32;


// For typeconverter
using System.ComponentModel.Design.Serialization;
using System.Reflection;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Enum which describes how to position the TickBar.
    /// </summary>
    public enum TickBarPlacement
    {
        /// <summary>
        /// Position this tick at the left of target element.
        /// </summary>
        Left,

        /// <summary>
        /// Position this tick at the top of target element.
        /// </summary>
        Top,

        /// <summary>
        /// Position this tick at the right of target element.
        /// </summary>
        Right,

        /// <summary>
        /// Position this tick at the bottom of target element.
        /// </summary>
        Bottom,

        // NOTE: if you add or remove any values in this enum, be sure to update TickBar.IsValidTickBarPlacement()
    };

    /// <summary>
    /// TickBar is an element that use for drawing Slider's Ticks.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string    
    public class TickBar : FrameworkElement
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------
        #region Constructors

        static TickBar()
        {
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(TickBar), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        ///     Default constructor for TickBar class
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public TickBar() : base()
        {
        }

        #endregion

        /// <summary>
        /// Fill property
        /// </summary>
        public static readonly DependencyProperty FillProperty
            = DependencyProperty.Register(
                "Fill",
                typeof(Brush),
                typeof(TickBar),
                new FrameworkPropertyMetadata(
                    (Brush)null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,
                    null)
                );

        /// <summary>
        /// Fill property
        /// </summary>
        public Brush Fill
        {
            get
            {
                return (Brush)GetValue(FillProperty);
            }
            set
            {
                SetValue(FillProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
                RangeBase.MinimumProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                0.0,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Logical position where the Minimum Tick will be drawn
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double Minimum
        {
            get { return (double) GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="Maximum"/>  property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
                RangeBase.MaximumProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                100.0,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Logical position where the Maximum Tick will be drawn
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double Maximum
        {
            get { return (double) GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="SelectionStart"/>  property.
        /// </summary>
        public static readonly DependencyProperty SelectionStartProperty =
                Slider.SelectionStartProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                -1.0d,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Logical position where the SelectionStart Tick will be drawn
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double SelectionStart
        {
            get { return (double) GetValue(SelectionStartProperty); }
            set { SetValue(SelectionStartProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="SelectionEnd"/>  property.
        /// </summary>
        public static readonly DependencyProperty SelectionEndProperty =
                Slider.SelectionEndProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                -1.0d,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Logical position where the SelectionEnd Tick will be drawn
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double SelectionEnd
        {
            get { return (double) GetValue(SelectionEndProperty); }
            set { SetValue(SelectionEndProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="IsSelectionRangeEnabled"/>  property.
        /// </summary>
        public static readonly DependencyProperty IsSelectionRangeEnabledProperty =
                Slider.IsSelectionRangeEnabledProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     IsSelectionRangeEnabled specifies whether to draw SelectionStart Tick and SelectionEnd Tick or not.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsSelectionRangeEnabled
        {
            get { return (bool) GetValue(IsSelectionRangeEnabledProperty); }
            set { SetValue(IsSelectionRangeEnabledProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="TickFrequency" /> property.
        /// </summary>
        public static readonly DependencyProperty TickFrequencyProperty =
                Slider.TickFrequencyProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                1.0,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// TickFrequency property defines how the tick will be drawn.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double TickFrequency
        {
            get { return (double) GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Ticks" /> property.
        /// </summary>
        public static readonly DependencyProperty TicksProperty =
                Slider.TicksProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                new FreezableDefaultValueFactory(DoubleCollection.Empty),
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Ticks property contains collection of value of type Double which
        /// are the logical positions use to draw the ticks.
        /// The property value is a <see cref="DoubleCollection" />.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public DoubleCollection Ticks
        {
            get { return (DoubleCollection) GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for IsDirectionReversed property.
        /// </summary>
        public static readonly DependencyProperty IsDirectionReversedProperty =
                Slider.IsDirectionReversedProperty.AddOwner(
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The IsDirectionReversed property defines the direction of value incrementation.
        /// By default, if Tick's orientation is Horizontal, ticks will be drawn from left to right.
        /// (And, bottom to top for Vertical orientation).
        /// If IsDirectionReversed is 'true' the direction of the drawing will be in opposite direction.
        /// Ticks property contains collection of value of type Double which
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsDirectionReversed
        {
            get { return (bool) GetValue(IsDirectionReversedProperty); }
            set { SetValue(IsDirectionReversedProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Placement" /> property.
        /// </summary>
        public static readonly DependencyProperty PlacementProperty =
                DependencyProperty.Register(
                        "Placement",
                        typeof(TickBarPlacement),
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                TickBarPlacement.Top,
                                FrameworkPropertyMetadataOptions.AffectsRender),
                        new ValidateValueCallback(IsValidTickBarPlacement));

        /// <summary>
        /// Placement property specified how the Tick will be placed.
        /// This property affects the way ticks are drawn.
        /// This property has type of <see cref="TickBarPlacement" />.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public TickBarPlacement Placement
        {
            get { return (TickBarPlacement) GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        private static bool IsValidTickBarPlacement(object o)
        {
            TickBarPlacement placement = (TickBarPlacement)o;
            return placement == TickBarPlacement.Left ||
                   placement == TickBarPlacement.Top ||
                   placement == TickBarPlacement.Right ||
                   placement == TickBarPlacement.Bottom;
        }

        /// <summary>
        /// DependencyProperty for ReservedSpace property.
        /// </summary>
        public static readonly DependencyProperty ReservedSpaceProperty =
                DependencyProperty.Register(
                        "ReservedSpace",
                        typeof(double),
                        typeof(TickBar),
                        new FrameworkPropertyMetadata(
                                0d,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// TickBar will use ReservedSpaceProperty for left and right spacing (for horizontal orientation) or
        /// tob and bottom spacing (for vertical orienation).
        /// The space on both sides of TickBar is half of specified ReservedSpace.
        /// This property has type of <see cref="double" />.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double ReservedSpace
        {
            get { return (double) GetValue(ReservedSpaceProperty); }
            set { SetValue(ReservedSpaceProperty, value); }
        }

        /// <summary>
        /// Draw ticks.
        /// Ticks can be draw in 8 diffrent ways depends on Placment property and IsDirectionReversed property.
        ///
        /// This function also draw selection-tick(s) if IsSelectionRangeEnabled is 'true' and
        /// SelectionStart and SelectionEnd are valid.
        ///
        /// The primary ticks (for Mininum and Maximum value) height will be 100% of TickBar's render size (use Width or Height
        /// depends on Placement property).
        ///
        /// The secondary ticks (all other ticks, including selection-tics) height will be 75% of TickBar's render size.
        ///
        /// Brush that use to fill ticks is specified by Shape.Fill property.
        ///
        /// Pen that use to draw ticks is specified by Shape.Pen property.
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            Size size = new Size(ActualWidth,ActualHeight);
            double range = Maximum - Minimum;
            double tickLen = 0.0d;  // Height for Primary Tick (for Mininum and Maximum value)
            double tickLen2;        // Height for Secondary Tick
            double logicalToPhysical = 1.0;
            double progression = 1.0d;
            Point startPoint = new Point(0d,0d);
            Point endPoint = new Point(0d, 0d);

            // Take Thumb size in to account
            double halfReservedSpace = ReservedSpace * 0.5;

            switch(Placement)
            {
                case TickBarPlacement.Top:
                    if (DoubleUtil.GreaterThanOrClose(ReservedSpace, size.Width))
                    {
                        return;
                    }
                    size.Width -= ReservedSpace;
                    tickLen = - size.Height;
                    startPoint = new Point(halfReservedSpace, size.Height);
                    endPoint = new Point(halfReservedSpace + size.Width, size.Height);
                    logicalToPhysical = size.Width / range;
                    progression = 1;
                    break;

                case TickBarPlacement.Bottom:
                    if (DoubleUtil.GreaterThanOrClose(ReservedSpace, size.Width))
                    {
                        return;
                    }
                    size.Width -= ReservedSpace;
                    tickLen = size.Height;
                    startPoint = new Point(halfReservedSpace, 0d);
                    endPoint = new Point(halfReservedSpace + size.Width, 0d);
                    logicalToPhysical = size.Width / range;
                    progression = 1;
                    break;

                case TickBarPlacement.Left:
                    if (DoubleUtil.GreaterThanOrClose(ReservedSpace, size.Height))
                    {
                        return;
                    }
                    size.Height -= ReservedSpace;
                    tickLen = -size.Width;
                    startPoint = new Point(size.Width, size.Height + halfReservedSpace);
                    endPoint = new Point(size.Width, halfReservedSpace);
                    logicalToPhysical = size.Height / range * -1;
                    progression = -1;
                    break;

                case TickBarPlacement.Right:
                    if (DoubleUtil.GreaterThanOrClose(ReservedSpace, size.Height))
                    {
                        return;
                    }
                    size.Height -= ReservedSpace;
                    tickLen = size.Width;
                    startPoint = new Point(0d, size.Height + halfReservedSpace);
                    endPoint = new Point(0d, halfReservedSpace);
                    logicalToPhysical = size.Height / range * -1;
                    progression = -1;
                    break;
            };

            tickLen2 = tickLen * 0.75;

            // Invert direciton of the ticks
            if (IsDirectionReversed)
            {
                progression = -progression;
                logicalToPhysical *= -1;

                // swap startPoint & endPoint
                Point pt = startPoint;
                startPoint = endPoint;
                endPoint = pt;
            }

            Pen pen = new Pen(Fill, 1.0d);

            bool snapsToDevicePixels = SnapsToDevicePixels;
            DoubleCollection xLines = snapsToDevicePixels ? new DoubleCollection() : null;
            DoubleCollection yLines = snapsToDevicePixels ? new DoubleCollection() : null;

            // Is it Vertical?
            if ((Placement == TickBarPlacement.Left) || (Placement == TickBarPlacement.Right))
            {
                // Reduce tick interval if it is more than would be visible on the screen
                double interval = TickFrequency;
                if (interval > 0.0)
                {
                    double minInterval = (Maximum - Minimum) / size.Height;
                    if (interval < minInterval)
                    {
                        interval = minInterval;
                    }
                }

                // Draw Min & Max tick
                dc.DrawLine(pen, startPoint, new Point(startPoint.X + tickLen, startPoint.Y));
                dc.DrawLine(pen, new Point(startPoint.X, endPoint.Y),
                                 new Point(startPoint.X + tickLen, endPoint.Y));

                if (snapsToDevicePixels)
                {
                    xLines.Add(startPoint.X);
                    yLines.Add(startPoint.Y - 0.5);
                    xLines.Add(startPoint.X + tickLen);
                    yLines.Add(endPoint.Y - 0.5);
                    xLines.Add(startPoint.X + tickLen2);
                }

                // This property is rarely set so let's try to avoid the GetValue
                // caching of the mutable default value
                DoubleCollection ticks = null;
                bool hasModifiers;
                if (GetValueSource(TicksProperty, null, out hasModifiers)
                    != BaseValueSourceInternal.Default || hasModifiers)
                {
                    ticks = Ticks;
                }

                // Draw ticks using specified Ticks collection
                if ((ticks != null) && (ticks.Count > 0))
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        if (DoubleUtil.LessThanOrClose(ticks[i],Minimum) || DoubleUtil.GreaterThanOrClose(ticks[i],Maximum))
                        {
                            continue;
                        }

                        double adjustedTick = ticks[i] - Minimum;

                        double y = adjustedTick * logicalToPhysical + startPoint.Y;
                        dc.DrawLine(pen,
                            new Point(startPoint.X, y),
                            new Point(startPoint.X + tickLen2, y));

                        if (snapsToDevicePixels)
                        {
                            yLines.Add(y - 0.5);
                        }
                    }
                }
                // Draw ticks using specified TickFrequency
                else if (interval > 0.0)
                {
                    for (double i = interval; i < range; i += interval)
                    {
                        double y = i * logicalToPhysical + startPoint.Y;

                        dc.DrawLine(pen,
                            new Point(startPoint.X, y),
                            new Point(startPoint.X + tickLen2, y));

                        if (snapsToDevicePixels)
                        {
                            yLines.Add(y - 0.5);
                        }
                    }
                }

                // Draw Selection Ticks
                if (IsSelectionRangeEnabled)
                {
                    double y0 = (SelectionStart - Minimum) * logicalToPhysical + startPoint.Y;
                    Point pt0 = new Point(startPoint.X, y0);
                    Point pt1 = new Point(startPoint.X + tickLen2, y0);
                    Point pt2 = new Point(startPoint.X + tickLen2, y0 + Math.Abs(tickLen2) * progression);

                    PathSegment[] segments = new PathSegment[] {
                        new LineSegment(pt2, true),
                        new LineSegment(pt0, true),
                    };
                    PathGeometry geo = new PathGeometry(new PathFigure[] { new PathFigure(pt1, segments, true) });

                    dc.DrawGeometry(Fill, pen, geo);

                    y0 = (SelectionEnd - Minimum) * logicalToPhysical + startPoint.Y;
                    pt0 = new Point(startPoint.X, y0);
                    pt1 = new Point(startPoint.X + tickLen2, y0);
                    pt2 = new Point(startPoint.X + tickLen2, y0 - Math.Abs(tickLen2) * progression);

                    segments = new PathSegment[] {
                        new LineSegment(pt2, true),
                        new LineSegment(pt0, true),
                    };
                    geo = new PathGeometry(new PathFigure[] { new PathFigure(pt1, segments, true) });
                    dc.DrawGeometry(Fill, pen, geo);
                }
            }
            else  // Placement == Top || Placement == Bottom
            {
                // Reduce tick interval if it is more than would be visible on the screen
                double interval = TickFrequency;
                if (interval > 0.0)
                {
                    double minInterval = (Maximum - Minimum) / size.Width;
                    if (interval < minInterval)
                    {
                        interval = minInterval;
                    }
                }

                // Draw Min & Max tick
                dc.DrawLine(pen, startPoint, new Point(startPoint.X, startPoint.Y + tickLen));
                dc.DrawLine(pen, new Point(endPoint.X, startPoint.Y),
                                 new Point(endPoint.X, startPoint.Y + tickLen));

                if (snapsToDevicePixels)
                {
                    xLines.Add(startPoint.X - 0.5);
                    yLines.Add(startPoint.Y);
                    xLines.Add(startPoint.X - 0.5);
                    yLines.Add(endPoint.Y + tickLen);
                    yLines.Add(endPoint.Y + tickLen2);
                }

                // This property is rarely set so let's try to avoid the GetValue
                // caching of the mutable default value
                DoubleCollection ticks = null;
                bool hasModifiers;
                if (GetValueSource(TicksProperty, null, out hasModifiers)
                    != BaseValueSourceInternal.Default || hasModifiers)
                {
                    ticks = Ticks;
                }

                // Draw ticks using specified Ticks collection
                if ((ticks != null) && (ticks.Count > 0))
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        if (DoubleUtil.LessThanOrClose(ticks[i],Minimum) || DoubleUtil.GreaterThanOrClose(ticks[i],Maximum))
                        {
                            continue;
                        }
                        double adjustedTick = ticks[i] - Minimum;

                        double x = adjustedTick * logicalToPhysical + startPoint.X;
                        dc.DrawLine(pen,
                            new Point(x, startPoint.Y),
                            new Point(x, startPoint.Y + tickLen2));

                        if (snapsToDevicePixels)
                        {
                            xLines.Add(x - 0.5);
                        }
                    }
                }
                // Draw ticks using specified TickFrequency
                else if (interval > 0.0)
                {
                    for (double i = interval; i < range; i += interval)
                    {
                        double x = i * logicalToPhysical + startPoint.X;
                        dc.DrawLine(pen,
                            new Point(x, startPoint.Y),
                            new Point(x, startPoint.Y + tickLen2));

                        if (snapsToDevicePixels)
                        {
                            xLines.Add(x - 0.5);
                        }
                    }
                }

                // Draw Selection Ticks
                if (IsSelectionRangeEnabled)
                {
                    double x0 = (SelectionStart - Minimum) * logicalToPhysical + startPoint.X;
                    Point pt0 = new Point(x0, startPoint.Y);
                    Point pt1 = new Point(x0, startPoint.Y + tickLen2);
                    Point pt2 = new Point(x0 + Math.Abs(tickLen2) * progression, startPoint.Y + tickLen2);

                    PathSegment[] segments = new PathSegment[] {
                        new LineSegment(pt2, true),
                        new LineSegment(pt0, true),
                    };
                    PathGeometry geo = new PathGeometry(new PathFigure[] { new PathFigure(pt1, segments, true) });

                    dc.DrawGeometry(Fill, pen, geo);

                    x0 = (SelectionEnd - Minimum) * logicalToPhysical + startPoint.X;
                    pt0 = new Point(x0, startPoint.Y);
                    pt1 = new Point(x0, startPoint.Y + tickLen2);
                    pt2 = new Point(x0 - Math.Abs(tickLen2) * progression, startPoint.Y + tickLen2);

                    segments = new PathSegment[] {
                        new LineSegment(pt2, true),
                        new LineSegment(pt0, true),
                    };
                    geo = new PathGeometry(new PathFigure[] { new PathFigure(pt1, segments, true) });
                    dc.DrawGeometry(Fill, pen, geo);
                }
            }

            if (snapsToDevicePixels)
            {
                xLines.Add(ActualWidth);
                yLines.Add(ActualHeight);
                VisualXSnappingGuidelines = xLines;
                VisualYSnappingGuidelines = yLines;
            }
            return;
        }

        private void BindToTemplatedParent(DependencyProperty target, DependencyProperty source)
        {
            if (!HasNonDefaultValue(target))
            {
                Binding binding = new Binding();
                binding.RelativeSource = RelativeSource.TemplatedParent;
                binding.Path = new PropertyPath(source);
                SetBinding(target, binding);
            }
        }

        /// <summary>
        /// TickBar sets bindings on its properties to its TemplatedParent if the
        /// properties are not already set.
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            Slider parent = TemplatedParent as Slider;
            if (parent != null)
            {
                BindToTemplatedParent(TicksProperty, Slider.TicksProperty);
                BindToTemplatedParent(TickFrequencyProperty, Slider.TickFrequencyProperty);
                BindToTemplatedParent(IsSelectionRangeEnabledProperty, Slider.IsSelectionRangeEnabledProperty);
                BindToTemplatedParent(SelectionStartProperty, Slider.SelectionStartProperty);
                BindToTemplatedParent(SelectionEndProperty, Slider.SelectionEndProperty);
                BindToTemplatedParent(MinimumProperty, Slider.MinimumProperty);
                BindToTemplatedParent(MaximumProperty, Slider.MaximumProperty);
                BindToTemplatedParent(IsDirectionReversedProperty, Slider.IsDirectionReversedProperty);

                if (!HasNonDefaultValue(ReservedSpaceProperty) && parent.Track != null)
                {
                    Binding binding = new Binding();
                    binding.Source = parent.Track.Thumb;

                    if (parent.Orientation == Orientation.Horizontal)
                    {
                        binding.Path = new PropertyPath(Thumb.ActualWidthProperty);
                    }
                    else
                    {
                        binding.Path = new PropertyPath(Thumb.ActualHeightProperty);
                    }

                    SetBinding(ReservedSpaceProperty, binding);
                }
            }
        }
    }
}
