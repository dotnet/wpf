// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using System.Windows.Input;
using System.Windows.Media;

using MS.Win32;
using MS.Internal;
using MS.Internal.Commands;
using MS.Internal.Telemetry.PresentationFramework;


// For typeconverter
using System.ComponentModel.Design.Serialization;
using System.Reflection;


namespace System.Windows.Controls
{
    /// <summary>
    /// Slider control lets the user select from a range of values by moving a slider.
    /// Slider is used to enable to user to gradually modify a value (range selection).
    /// Slider is an easy and natural interface for users, because it provides good visual feedback.
    /// </summary>
    /// <seealso cref="RangeBase" />
    [Localizability(LocalizationCategory.Ignore)]
    [DefaultEvent("ValueChanged"), DefaultProperty("Value")]
    [TemplatePart(Name = "PART_Track", Type = typeof(Track))]
    [TemplatePart(Name = "PART_SelectionRange", Type = typeof(FrameworkElement))]
    public class Slider : RangeBase
    {
        #region Constructors

        /// <summary>
        /// Instantiates a new instance of a Slider with out Dispatcher.
        /// </summary>
        /// <ExternalAPI/>
        public Slider() : base()
        {
        }

        /// <summary>
        /// This is the static constructor for the Slider class.  It
        /// simply registers the appropriate class handlers for the input
        /// devices, and defines a default style sheet.
        /// </summary>
        static Slider()
        {
            // Initialize CommandCollection & CommandLink(s)
            InitializeCommands();

            // Register all PropertyTypeMetadata
            MinimumProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(0.0d, FrameworkPropertyMetadataOptions.AffectsMeasure));
            MaximumProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(10.0d, FrameworkPropertyMetadataOptions.AffectsMeasure));
            ValueProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));

            // Register Event Handler for the Thumb
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragStartedEvent, new DragStartedEventHandler(Slider.OnThumbDragStarted));
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragDeltaEvent, new DragDeltaEventHandler(Slider.OnThumbDragDelta));
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragCompletedEvent, new DragCompletedEventHandler(Slider.OnThumbDragCompleted));

            // Listen to MouseLeftButtonDown event to determine if slide should move focus to itself
            EventManager.RegisterClassHandler(typeof(Slider), Mouse.MouseDownEvent, new MouseButtonEventHandler(Slider._OnMouseLeftButtonDown),true);

            DefaultStyleKeyProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(typeof(Slider)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Slider));

            ControlsTraceLogger.AddControl(TelemetryControls.Slider);
        }

        #endregion Constructors

        #region Commands

        private static RoutedCommand _increaseLargeCommand = null;
        private static RoutedCommand _increaseSmallCommand = null;
        private static RoutedCommand _decreaseLargeCommand = null;
        private static RoutedCommand _decreaseSmallCommand = null;
        private static RoutedCommand _minimizeValueCommand = null;
        private static RoutedCommand _maximizeValueCommand = null;

        /// <summary>
        /// Increase Slider value
        /// </summary>
        public static RoutedCommand IncreaseLarge
        {
            get { return _increaseLargeCommand; }
        }
        /// <summary>
        /// Decrease Slider value
        /// </summary>
        public static RoutedCommand DecreaseLarge
        {
            get { return _decreaseLargeCommand; }
        }
        /// <summary>
        /// Increase Slider value
        /// </summary>
        public static RoutedCommand IncreaseSmall
        {
            get { return _increaseSmallCommand; }
        }
        /// <summary>
        /// Decrease Slider value
        /// </summary>
        public static RoutedCommand DecreaseSmall
        {
            get { return _decreaseSmallCommand; }
        }
        /// <summary>
        /// Set Slider value to mininum
        /// </summary>
        public static RoutedCommand MinimizeValue
        {
            get { return _minimizeValueCommand; }
        }
        /// <summary>
        /// Set Slider value to maximum
        /// </summary>
        public static RoutedCommand MaximizeValue
        {
            get { return _maximizeValueCommand; }
        }

        static void InitializeCommands()
        {
            _increaseLargeCommand = new RoutedCommand("IncreaseLarge", typeof(Slider));
            _decreaseLargeCommand = new RoutedCommand("DecreaseLarge", typeof(Slider));
            _increaseSmallCommand = new RoutedCommand("IncreaseSmall", typeof(Slider));
            _decreaseSmallCommand = new RoutedCommand("DecreaseSmall", typeof(Slider));
            _minimizeValueCommand = new RoutedCommand("MinimizeValue", typeof(Slider));
            _maximizeValueCommand = new RoutedCommand("MaximizeValue", typeof(Slider));

            CommandHelpers.RegisterCommandHandler(typeof(Slider), _increaseLargeCommand, new ExecutedRoutedEventHandler(OnIncreaseLargeCommand),
                                                  new SliderGesture(Key.PageUp, Key.PageDown, false));

            CommandHelpers.RegisterCommandHandler(typeof(Slider), _decreaseLargeCommand, new ExecutedRoutedEventHandler(OnDecreaseLargeCommand),
                                                  new SliderGesture(Key.PageDown, Key.PageUp, false));

            CommandHelpers.RegisterCommandHandler(typeof(Slider), _increaseSmallCommand, new ExecutedRoutedEventHandler(OnIncreaseSmallCommand),
                                                  new SliderGesture(Key.Up, Key.Down, false),
                                                  new SliderGesture(Key.Right, Key.Left, true));

            CommandHelpers.RegisterCommandHandler(typeof(Slider), _decreaseSmallCommand, new ExecutedRoutedEventHandler(OnDecreaseSmallCommand),
                                                  new SliderGesture(Key.Down, Key.Up, false),
                                                  new SliderGesture(Key.Left, Key.Right, true));

            CommandHelpers.RegisterCommandHandler(typeof(Slider), _minimizeValueCommand, new ExecutedRoutedEventHandler(OnMinimizeValueCommand),
                                                  Key.Home);

            CommandHelpers.RegisterCommandHandler(typeof(Slider), _maximizeValueCommand, new ExecutedRoutedEventHandler(OnMaximizeValueCommand),
                                                  Key.End);
        }

        private class SliderGesture : InputGesture
        {
            public SliderGesture(Key normal, Key inverted, bool forHorizontal)
            {
                _normal = normal;
                _inverted = inverted;
                _forHorizontal = forHorizontal;
            }

            /// <summary>
            /// Sees if the InputGesture matches the input associated with the inputEventArgs
            /// </summary>
            public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
            {
                KeyEventArgs keyEventArgs = inputEventArgs as KeyEventArgs;
                Slider slider = targetElement as Slider;
                if (keyEventArgs != null && slider != null && Keyboard.Modifiers == ModifierKeys.None)
                {
                    if((int)_normal == (int)keyEventArgs.RealKey)
                    {
                        return !IsInverted(slider);
                    }
                    if ((int)_inverted == (int)keyEventArgs.RealKey)
                    {
                        return IsInverted(slider);
                    }
                }
                return false;
            }

            private bool IsInverted(Slider slider)
            {
                if (_forHorizontal)
                {
                    return slider.IsDirectionReversed != (slider.FlowDirection == FlowDirection.RightToLeft);
                }
                else
                {
                    return slider.IsDirectionReversed;
                }
            }

            private Key _normal, _inverted;
            private bool _forHorizontal;
        }



        private static void OnIncreaseSmallCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.OnIncreaseSmall();
            }
        }

        private static void OnDecreaseSmallCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.OnDecreaseSmall();
            }
        }

        private static void OnMaximizeValueCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.OnMaximizeValue();
            }
        }

        private static void OnMinimizeValueCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.OnMinimizeValue();
            }
        }

        private static void OnIncreaseLargeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.OnIncreaseLarge();
            }
        }

        private static void OnDecreaseLargeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                slider.OnDecreaseLarge();
            }
        }

        #endregion Commands

        #region Properties

        #region Orientation Property

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
                DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Slider),
                                          new FrameworkPropertyMetadata(Orientation.Horizontal),
                                          new ValidateValueCallback(ScrollBar.IsValidOrientation));

        /// <summary>
        /// Get/Set Orientation property
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        #endregion

        #region IsDirectionReversed Property
        /// <summary>
        /// Slider ThumbProportion property
        /// </summary>
        public static readonly DependencyProperty IsDirectionReversedProperty
            = DependencyProperty.Register("IsDirectionReversed", typeof(bool), typeof(Slider),
                                          new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Get/Set IsDirectionReversed property
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsDirectionReversed
        {
            get
            {
                return (bool)GetValue(IsDirectionReversedProperty);
            }
            set
            {
                SetValue(IsDirectionReversedProperty, value);
            }
        }
        #endregion

        #region Delay Property
        /// <summary>
        ///     The Property for the Delay property.
        /// </summary>
        public static readonly DependencyProperty DelayProperty = RepeatButton.DelayProperty.AddOwner(typeof(Slider), new FrameworkPropertyMetadata(RepeatButton.GetKeyboardDelay()));

        /// <summary>
        ///     Specifies the amount of time, in milliseconds, to wait before repeating begins.
        /// Must be non-negative.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public int Delay
        {
            get
            {
                return (int)GetValue(DelayProperty);
            }
            set
            {
                SetValue(DelayProperty, value);
            }
        }

        #endregion Delay Property

        #region Interval Property
        /// <summary>
        ///     The Property for the Interval property.
        /// </summary>
        public static readonly DependencyProperty IntervalProperty = RepeatButton.IntervalProperty.AddOwner(typeof(Slider), new FrameworkPropertyMetadata(RepeatButton.GetKeyboardSpeed()));

        /// <summary>
        ///     Specifies the amount of time, in milliseconds, between repeats once repeating starts.
        /// Must be non-negative
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public int Interval
        {
            get
            {
                return (int)GetValue(IntervalProperty);
            }
            set
            {
                SetValue(IntervalProperty, value);
            }
        }

        #endregion Interval Property

        #region AutoToolTipPlacement Property
        /// <summary>
        ///     The DependencyProperty for the AutoToolTipPlacement property.
        /// </summary>
        public static readonly DependencyProperty AutoToolTipPlacementProperty
            = DependencyProperty.Register("AutoToolTipPlacement", typeof(AutoToolTipPlacement), typeof(Slider),
                                          new FrameworkPropertyMetadata(Primitives.AutoToolTipPlacement.None),
                                          new ValidateValueCallback(IsValidAutoToolTipPlacement));

        /// <summary>
        ///     AutoToolTipPlacement property specifies the placement of the AutoToolTip
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public Primitives.AutoToolTipPlacement AutoToolTipPlacement
        {
            get
            {
                return (Primitives.AutoToolTipPlacement)GetValue(AutoToolTipPlacementProperty);
            }
            set
            {
                SetValue(AutoToolTipPlacementProperty, value);
            }
        }

        private static bool IsValidAutoToolTipPlacement(object o)
        {
            AutoToolTipPlacement placement = (AutoToolTipPlacement)o;
            return placement == AutoToolTipPlacement.None ||
                   placement == AutoToolTipPlacement.TopLeft ||
                   placement == AutoToolTipPlacement.BottomRight;
        }

        #endregion

        #region AutoToolTipPrecision Property
        /// <summary>
        ///     The DependencyProperty for the AutoToolTipPrecision property.
        ///     Flags:              None
        ///     Default Value:      0
        /// </summary>
        public static readonly DependencyProperty AutoToolTipPrecisionProperty
            = DependencyProperty.Register("AutoToolTipPrecision", typeof(int), typeof(Slider),
            new FrameworkPropertyMetadata(0), new ValidateValueCallback(IsValidAutoToolTipPrecision));

        /// <summary>
        ///     Get or set number of decimal digits of Slider's Value shown in AutoToolTip
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public int AutoToolTipPrecision
        {
            get
            {
                return (int)GetValue(AutoToolTipPrecisionProperty);
            }
            set
            {
                SetValue(AutoToolTipPrecisionProperty, value);
            }
        }

        /// <summary>
        /// Validates AutoToolTipPrecision value
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static bool IsValidAutoToolTipPrecision(object o)
        {
            return (((int)o) >= 0);
        }

        #endregion


        /*
         * TickMark support
         *
         *   - double           TickFrequency
         *   - bool             IsSnapToTickEnabled
         *   - Enum             TickPlacement
         *   - DoubleCollection Ticks
         */
        #region TickMark support
        /// <summary>
        ///     The DependencyProperty for the IsSnapToTickEnabled property.
        /// </summary>
        public static readonly DependencyProperty IsSnapToTickEnabledProperty
            = DependencyProperty.Register("IsSnapToTickEnabled", typeof(bool), typeof(Slider),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     When 'true', Slider will automatically move the Thumb (and/or change current value) to the closest TickMark.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool IsSnapToTickEnabled
        {
            get
            {
                return (bool)GetValue(IsSnapToTickEnabledProperty);
            }
            set
            {
                SetValue(IsSnapToTickEnabledProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the TickPlacement property.
        /// </summary>
        public static readonly DependencyProperty TickPlacementProperty
            = DependencyProperty.Register("TickPlacement", typeof(Primitives.TickPlacement), typeof(Slider),
                                          new FrameworkPropertyMetadata(Primitives.TickPlacement.None),
                                          new ValidateValueCallback(IsValidTickPlacement));

        /// <summary>
        ///     Slider uses this value to determine where to show the Ticks.
        /// When Ticks is not 'null', Slider will ignore 'TickFrequency', and draw only TickMarks
        /// that specified in Ticks collection.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Primitives.TickPlacement TickPlacement
        {
            get
            {
                return (Primitives.TickPlacement)GetValue(TickPlacementProperty);
            }
            set
            {
                SetValue(TickPlacementProperty, value);
            }
        }

        private static bool IsValidTickPlacement(object o)
        {
            TickPlacement value = (TickPlacement)o;
            return value == TickPlacement.None ||
                   value == TickPlacement.TopLeft ||
                   value == TickPlacement.BottomRight ||
                   value == TickPlacement.Both;
        }

        /// <summary>
        ///     The DependencyProperty for the TickFrequency property.
        ///     Default Value is 1.0
        /// </summary>
        public static readonly DependencyProperty TickFrequencyProperty
            = DependencyProperty.Register("TickFrequency", typeof(double), typeof(Slider),
            new FrameworkPropertyMetadata(1.0),
            new ValidateValueCallback(IsValidDoubleValue));

        /// <summary>
        ///     Slider uses this value to determine where to show the Ticks.
        /// When Ticks is not 'null', Slider will ignore 'TickFrequency', and draw only TickMarks
        /// that specified in Ticks collection.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double TickFrequency
        {
            get
            {
                return (double)GetValue(TickFrequencyProperty);
            }
            set
            {
                SetValue(TickFrequencyProperty, value);
            }
        }

        // Consider using List<double> instead of DoubleCollection, if we can get a better perf.

        /// <summary>
        ///     The DependencyProperty for the Ticks property.
        /// </summary>
        public static readonly DependencyProperty TicksProperty
            = DependencyProperty.Register("Ticks", typeof(DoubleCollection), typeof(Slider),
            new FrameworkPropertyMetadata(new FreezableDefaultValueFactory(DoubleCollection.Empty)));

        /// <summary>
        ///     Slider uses this value to determine where to show the Ticks.
        /// When Ticks is not 'null', Slider will ignore 'TickFrequency', and draw only TickMarks
        /// that specified in Ticks collection.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public DoubleCollection Ticks
        {
            get
            {
                return (DoubleCollection)GetValue(TicksProperty);
            }
            set
            {
                SetValue(TicksProperty, value);
            }
        }
        #endregion TickMark support

        /*
         * Selection support
         *
         *   - bool   IsSelectionRangeEnabled
         *   - double SelectionStart
         *   - double SelectionEnd
         */

        #region Selection supports

        /// <summary>
        ///     The DependencyProperty for the IsSelectionRangeEnabled property.
        /// </summary>
        public static readonly DependencyProperty IsSelectionRangeEnabledProperty
            = DependencyProperty.Register("IsSelectionRangeEnabled", typeof(bool), typeof(Slider),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Enable or disable selection support on Slider
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsSelectionRangeEnabled
        {
            get
            {
                return (bool)GetValue(IsSelectionRangeEnabledProperty);
            }
            set
            {
                SetValue(IsSelectionRangeEnabledProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the SelectionStart property.
        /// </summary>
        public static readonly DependencyProperty SelectionStartProperty
            = DependencyProperty.Register("SelectionStart", typeof(double), typeof(Slider),
                    new FrameworkPropertyMetadata(0.0d,
                        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                        new PropertyChangedCallback(OnSelectionStartChanged),
                        new CoerceValueCallback(CoerceSelectionStart)),
                    new ValidateValueCallback(IsValidDoubleValue));

        /// <summary>
        ///     Get or set starting value of selection.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double SelectionStart
        {
            get { return (double) GetValue(SelectionStartProperty); }
            set { SetValue(SelectionStartProperty, value); }
        }

        private static void OnSelectionStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Slider ctrl = (Slider)d;
            double oldValue = (double)e.OldValue;
            double newValue = (double)e.NewValue;

            ctrl.CoerceValue(SelectionEndProperty);
            ctrl.UpdateSelectionRangeElementPositionAndSize();
        }

        private static object CoerceSelectionStart(DependencyObject d, object value)
        {
            Slider slider = (Slider)d;
            double selection = (double)value;

            double min = slider.Minimum;
            double max = slider.Maximum;

            if (selection < min)
            {
                return min;
            }
            if (selection > max)
            {
                return max;
            }
            return value;
        }

        /// <summary>
        ///     The DependencyProperty for the SelectionEnd property.
        /// </summary>
        public static readonly DependencyProperty SelectionEndProperty
            = DependencyProperty.Register("SelectionEnd", typeof(double), typeof(Slider),
                    new FrameworkPropertyMetadata(0.0d,
                        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                        new PropertyChangedCallback(OnSelectionEndChanged),
                        new CoerceValueCallback(CoerceSelectionEnd)),
                    new ValidateValueCallback(IsValidDoubleValue));

        /// <summary>
        ///     Get or set starting value of selection.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public double SelectionEnd
        {
            get { return (double) GetValue(SelectionEndProperty); }
            set { SetValue(SelectionEndProperty, value); }
        }

        private static void OnSelectionEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Slider ctrl = (Slider)d;
            ctrl.UpdateSelectionRangeElementPositionAndSize();
        }

        private static object CoerceSelectionEnd(DependencyObject d, object value)
        {
            Slider slider = (Slider)d;
            double selection = (double)value;

            double min = slider.SelectionStart;
            double max = slider.Maximum;

            if (selection < min)
            {
                return min;
            }
            if (selection > max)
            {
                return max;
            }
            return value;
        }

        /// <summary>
        ///     Called when the value of SelectionEnd is required by the property system.
        /// </summary>
        /// <param name="d">The object on which the property was queried.</param>
        /// <returns>The value of the SelectionEnd property on "d."</returns>
        private static object OnGetSelectionEnd(DependencyObject d)
        {
            return ((Slider)d).SelectionEnd;
        }

        /// <summary>
        ///     This method is invoked when the Minimum property changes.
        /// </summary>
        /// <param name="oldMinimum">The old value of the Minimum property.</param>
        /// <param name="newMinimum">The new value of the Minimum property.</param>
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            CoerceValue(SelectionStartProperty);
        }

        /// <summary>
        ///     This method is invoked when the Maximum property changes.
        /// </summary>
        /// <param name="oldMaximum">The old value of the Maximum property.</param>
        /// <param name="newMaximum">The new value of the Maximum property.</param>
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            CoerceValue(SelectionStartProperty);
            CoerceValue(SelectionEndProperty);
        }

        #endregion Selection supports

        /*
         * Move-To-Point support
         *
         * Property
         *   - bool   IsMoveToPointEnabled
         *
         * Event Handlers
         *   - OnPreviewMouseLeftButtonDown
         *   - double SelectionEnd
         */
        #region Move-To-Point support

        /// <summary>
        ///     The DependencyProperty for the IsMoveToPointEnabled property.
        /// </summary>
        public static readonly DependencyProperty IsMoveToPointEnabledProperty
            = DependencyProperty.Register("IsMoveToPointEnabled", typeof(bool), typeof(Slider),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Enable or disable Move-To-Point support on Slider.
        ///     Move-To-Point feature, enables Slider to immediately move the Thumb directly to the location where user
        /// clicked the Mouse.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool IsMoveToPointEnabled
        {
            get
            {
                return (bool)GetValue(IsMoveToPointEnabledProperty);
            }
            set
            {
                SetValue(IsMoveToPointEnabledProperty, value);
            }
        }

        /// <summary>
        /// When IsMoveToPointEneabled is 'true', Slider needs to preview MouseLeftButtonDown event, in order prevent its RepeatButtons
        /// from handle Left-Click.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsMoveToPointEnabled && Track != null && Track.Thumb != null && !Track.Thumb.IsMouseOver)
            {
                // Move Thumb to the Mouse location

                Point pt = e.MouseDevice.GetPosition(Track);

                double newValue = Track.ValueFromPoint(pt);
                if (System.Windows.Shapes.Shape.IsDoubleFinite(newValue))
                {
                    UpdateValue(newValue);
                }
                e.Handled = true;
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        #endregion Move-To-Point support

        #endregion // Properties


        #region Event Handlers
        /// <summary>
        /// Listen to Thumb DragStarted event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            Slider slider = sender as Slider;
            slider.OnThumbDragStarted(e);
        }

        /// <summary>
        /// Listen to Thumb DragDelta event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            Slider slider = sender as Slider;

            slider.OnThumbDragDelta(e);
        }

        /// <summary>
        /// Listen to Thumb DragCompleted event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Slider slider = sender as Slider;
            slider.OnThumbDragCompleted(e);
        }

        /// <summary>
        /// Called when user start dragging the Thumb.
        /// This function can be override to customize the way Slider handles Thumb movement.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragStarted(DragStartedEventArgs e)
        {
            // Show AutoToolTip if needed.
            Thumb thumb = e.OriginalSource as Thumb;

            if ((thumb == null) || (this.AutoToolTipPlacement == Primitives.AutoToolTipPlacement.None))
            {
                return;
            }

            // Save original tooltip
            _thumbOriginalToolTip = thumb.ToolTip;

            if (_autoToolTip == null)
            {
                _autoToolTip = new ToolTip();
                _autoToolTip.Placement = PlacementMode.Custom;
                _autoToolTip.PlacementTarget = thumb;
                _autoToolTip.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(this.AutoToolTipCustomPlacementCallback);
            }

            thumb.ToolTip = _autoToolTip;
            _autoToolTip.Content = GetAutoToolTipNumber();
            _autoToolTip.IsOpen = true;
            ((Popup)_autoToolTip.Parent).Reposition();
        }

        /// <summary>
        /// Called when user dragging the Thumb.
        /// This function can be override to customize the way Slider handles Thumb movement.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragDelta(DragDeltaEventArgs e)
        {
            Thumb thumb = e.OriginalSource as Thumb;
            // Convert to Track's co-ordinate
            if (Track != null && thumb == Track.Thumb)
            {
                double newValue = Value + Track.ValueFromDistance(e.HorizontalChange, e.VerticalChange);
                if (System.Windows.Shapes.Shape.IsDoubleFinite(newValue))
                {
                    UpdateValue(newValue);
                }

                // Show AutoToolTip if needed
                if (this.AutoToolTipPlacement != Primitives.AutoToolTipPlacement.None)
                {
                    if (_autoToolTip == null)
                    {
                        _autoToolTip = new ToolTip();
                    }

                    _autoToolTip.Content = GetAutoToolTipNumber();

                    if (thumb.ToolTip != _autoToolTip)
                    {
                        thumb.ToolTip = _autoToolTip;
                    }

                    if (!_autoToolTip.IsOpen)
                    {
                        _autoToolTip.IsOpen = true;
                    }
                    ((Popup)_autoToolTip.Parent).Reposition();
                }
            }
        }

        private string GetAutoToolTipNumber()
        {
            NumberFormatInfo format = (NumberFormatInfo)(NumberFormatInfo.CurrentInfo.Clone());
            format.NumberDecimalDigits = this.AutoToolTipPrecision;
            return this.Value.ToString("N", format);
        }

        /// <summary>
        /// Called when user stop dragging the Thumb.
        /// This function can be override to customize the way Slider handles Thumb movement.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            // Show AutoToolTip if needed.
            Thumb thumb = e.OriginalSource as Thumb;

            if ((thumb == null) || (this.AutoToolTipPlacement == Primitives.AutoToolTipPlacement.None))
            {
                return;
            }

            if (_autoToolTip != null)
            {
                _autoToolTip.IsOpen = false;
            }

            thumb.ToolTip = _thumbOriginalToolTip;
        }


        private CustomPopupPlacement[] AutoToolTipCustomPlacementCallback(Size popupSize, Size targetSize, Point offset)
        {
            switch (this.AutoToolTipPlacement)
            {
                case Primitives.AutoToolTipPlacement.TopLeft:
                    if (Orientation == Orientation.Horizontal)
                    {
                        // Place popup at top of thumb
                        return new CustomPopupPlacement[]{new CustomPopupPlacement(
                            new Point((targetSize.Width - popupSize.Width) * 0.5, -popupSize.Height),
                            PopupPrimaryAxis.Horizontal)
                        };
                    }
                    else
                    {
                        // Place popup at left of thumb
                        return new CustomPopupPlacement[] {
                            new CustomPopupPlacement(
                            new Point(-popupSize.Width, (targetSize.Height - popupSize.Height) * 0.5),
                            PopupPrimaryAxis.Vertical)
                        };
                    }

                case Primitives.AutoToolTipPlacement.BottomRight:
                    if (Orientation == Orientation.Horizontal)
                    {
                        // Place popup at bottom of thumb
                        return new CustomPopupPlacement[] {
                            new CustomPopupPlacement(
                            new Point((targetSize.Width - popupSize.Width) * 0.5, targetSize.Height) ,
                            PopupPrimaryAxis.Horizontal)
                        };
                    }
                    else
                    {
                        // Place popup at right of thumb
                        return new CustomPopupPlacement[] {
                            new CustomPopupPlacement(
                            new Point(targetSize.Width, (targetSize.Height - popupSize.Height) * 0.5),
                            PopupPrimaryAxis.Vertical)
                        };
                    }

                default:
                    return new CustomPopupPlacement[]{};
            }
        }


        /// <summary>
        /// Resize and resposition the SelectionRangeElement.
        /// </summary>
        private void UpdateSelectionRangeElementPositionAndSize()
        {
            Size trackSize = new Size(0d, 0d);
            Size thumbSize = new Size(0d, 0d);

            if (Track == null || DoubleUtil.LessThan(SelectionEnd,SelectionStart))
            {
                return;
            }

            trackSize = Track.RenderSize;
            thumbSize = (Track.Thumb != null) ? Track.Thumb.RenderSize : new Size(0d, 0d);

            double range = Maximum - Minimum;
            double valueToSize;

            FrameworkElement rangeElement = this.SelectionRangeElement as FrameworkElement;

            if (rangeElement == null)
            {
                return;
            }

            if (Orientation == Orientation.Horizontal)
            {
                // Calculate part size for HorizontalSlider
                if (DoubleUtil.AreClose(range, 0d) || (DoubleUtil.AreClose(trackSize.Width, thumbSize.Width)))
                {
                    valueToSize = 0d;
                }
                else
                {
                    valueToSize = Math.Max(0.0, (trackSize.Width - thumbSize.Width) / range);
                }

                rangeElement.Width = ((SelectionEnd - SelectionStart) * valueToSize);
                if (IsDirectionReversed)
                {
                    Canvas.SetLeft(rangeElement, (thumbSize.Width * 0.5) + Math.Max(Maximum - SelectionEnd, 0) * valueToSize);
                }
                else
                {
                    Canvas.SetLeft(rangeElement, (thumbSize.Width * 0.5) + Math.Max(SelectionStart - Minimum, 0) * valueToSize);
                }
            }
            else
            {
                // Calculate part size for VerticalSlider
                if (DoubleUtil.AreClose(range, 0d) || (DoubleUtil.AreClose(trackSize.Height, thumbSize.Height)))
                {
                    valueToSize = 0d;
                }
                else
                {
                    valueToSize = Math.Max(0.0, (trackSize.Height - thumbSize.Height) / range);
                }

                rangeElement.Height = ((SelectionEnd - SelectionStart) * valueToSize);
                if (IsDirectionReversed)
                {
                    Canvas.SetTop(rangeElement, (thumbSize.Height * 0.5) + Math.Max(SelectionStart - Minimum, 0) * valueToSize);
                }
                else
                {
                    Canvas.SetTop(rangeElement, (thumbSize.Height * 0.5) + Math.Max(Maximum - SelectionEnd,0) * valueToSize);
                }
            }
        }


        /// <summary>
        /// Gets or sets reference to Slider's Track element.
        /// </summary>
        internal Track Track
        {
            get
            {
                return _track;
            }
            set
            {
                _track = value;
            }
        }



        /// <summary>
        /// Gets or sets reference to Slider's SelectionRange element.
        /// </summary>
        internal FrameworkElement SelectionRangeElement
        {
            get
            {
                return _selectionRangeElement;
            }
            set
            {
                _selectionRangeElement = value;
            }
        }

        /// <summary>
        /// Snap the input 'value' to the closest tick.
        /// If input value is exactly in the middle of 2 surrounding ticks, it will be snapped to the tick that has greater value.
        /// </summary>
        /// <param name="value">Value that want to snap to closest Tick.</param>
        /// <returns>Snapped value if IsSnapToTickEnabled is 'true'. Otherwise, returns un-snaped value.</returns>
        private double SnapToTick(double value)
        {
            if (IsSnapToTickEnabled)
            {
                double previous = Minimum;
                double next = Maximum;

                // This property is rarely set so let's try to avoid the GetValue
                // caching of the mutable default value
                DoubleCollection ticks = null;
                bool hasModifiers;
                if (GetValueSource(TicksProperty, null, out hasModifiers)
                    != BaseValueSourceInternal.Default || hasModifiers)
                {
                    ticks = Ticks;
                }

                // If ticks collection is available, use it.
                // Note that ticks may be unsorted.
                if ((ticks != null) && (ticks.Count > 0))
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        double tick = ticks[i];
                        if (DoubleUtil.AreClose(tick, value))
                        {
                            return value;
                        }

                        if (DoubleUtil.LessThan(tick, value) && DoubleUtil.GreaterThan(tick, previous))
                        {
                            previous = tick;
                        }
                        else if (DoubleUtil.GreaterThan(tick ,value) && DoubleUtil.LessThan(tick, next))
                        {
                            next = tick;
                        }
                    }
                }
                else if (DoubleUtil.GreaterThan(TickFrequency, 0.0))
                {
                    previous = Minimum + (Math.Round(((value - Minimum) / TickFrequency)) * TickFrequency);
                    next = Math.Min(Maximum, previous + TickFrequency);
                }

                // Choose the closest value between previous and next. If tie, snap to 'next'.
                value = DoubleUtil.GreaterThanOrClose(value, (previous + next) * 0.5) ? next : previous;
            }

            return value;
        }

        // Sets Value = SnapToTick(value+direction), unless the result of SnapToTick is Value,
        // then it searches for the next tick greater(if direction is positive) than value
        // and sets Value to that tick
        private void MoveToNextTick(double direction)
        {
            if (direction != 0.0)
            {
                double value = this.Value;

                // Find the next value by snapping
                double next = SnapToTick(Math.Max(this.Minimum, Math.Min(this.Maximum, value + direction)));

                bool greaterThan = direction > 0; //search for the next tick greater than value?

                // If the snapping brought us back to value, find the next tick point
                if (next == value
                    && !( greaterThan && value == Maximum)  // Stop if searching up if already at Max
                    && !(!greaterThan && value == Minimum)) // Stop if searching down if already at Min
                {
                    // This property is rarely set so let's try to avoid the GetValue
                    // caching of the mutable default value
                    DoubleCollection ticks = null;
                    bool hasModifiers;
                    if (GetValueSource(TicksProperty, null, out hasModifiers)
                        != BaseValueSourceInternal.Default || hasModifiers)
                    {
                        ticks = Ticks;
                    }

                    // If ticks collection is available, use it.
                    // Note that ticks may be unsorted.
                    if ((ticks != null) && (ticks.Count > 0))
                    {
                        for (int i = 0; i < ticks.Count; i++)
                        {
                            double tick = ticks[i];

                            // Find the smallest tick greater than value or the largest tick less than value
                            if ((greaterThan && DoubleUtil.GreaterThan(tick, value) && (DoubleUtil.LessThan(tick, next) || next == value))
                             ||(!greaterThan && DoubleUtil.LessThan(tick, value) && (DoubleUtil.GreaterThan(tick, next) || next == value)))
                            {
                                next = tick;
                            }
                        }
                    }
                    else if (DoubleUtil.GreaterThan(TickFrequency, 0.0))
                    {
                        // Find the current tick we are at
                        double tickNumber = Math.Round((value - Minimum) / TickFrequency);

                        if (greaterThan)
                            tickNumber += 1.0;
                        else
                            tickNumber -= 1.0;

                        next = Minimum + tickNumber * TickFrequency;
                    }
                }


                // Update if we've found a better value
                if (next != value)
                {
                    this.SetCurrentValueInternal(ValueProperty, next);
                }
            }
        }
        #endregion Event Handlers

        #region Override Functions

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SliderAutomationPeer(this);
        }

        /// <summary>
        /// This is a class handler for MouseLeftButtonDown event.
        /// The purpose of this handle is to move input focus to Slider when user pressed
        /// mouse left button on any part of slider that is not focusable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton != MouseButton.Left) return;

            Slider slider = (Slider)sender;

            // When someone click on the Slider's part, and it's not focusable
            // Slider need to take the focus in order to process keyboard correctly
            if (!slider.IsKeyboardFocusWithin)
            {
                e.Handled = slider.Focus() || e.Handled;
            }
        }

        /// <summary>
        /// Perform arrangement of slider's children
        /// </summary>
        /// <param name="finalSize"></param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);

            UpdateSelectionRangeElementPositionAndSize();

            return size;
        }

        /// <summary>
        /// Update SelectionRange Length.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            UpdateSelectionRangeElementPositionAndSize();
        }

        /// <summary>
        /// Slider locates the SelectionRangeElement when its visual tree is created
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            SelectionRangeElement = GetTemplateChild(SelectionRangeElementName) as FrameworkElement;
            Track = GetTemplateChild(TrackName) as Track;

            if (_autoToolTip != null)
            {
                _autoToolTip.PlacementTarget = Track != null ? Track.Thumb : null;
            }
        }

        #endregion Override Functions

        #region Virtual Functions

        /// <summary>
        /// Call when Slider.IncreaseLarge command is invoked.
        /// </summary>
        protected virtual void OnIncreaseLarge()
        {
            MoveToNextTick(this.LargeChange);
        }

        /// <summary>
        /// Call when Slider.DecreaseLarge command is invoked.
        /// </summary>
        protected virtual void OnDecreaseLarge()
        {
            MoveToNextTick(-this.LargeChange);
        }

        /// <summary>
        /// Call when Slider.IncreaseSmall command is invoked.
        /// </summary>
        protected virtual void OnIncreaseSmall()
        {
            MoveToNextTick(this.SmallChange);
        }

        /// <summary>
        /// Call when Slider.DecreaseSmall command is invoked.
        /// </summary>
        protected virtual void OnDecreaseSmall()
        {
            MoveToNextTick(-this.SmallChange);
        }

        /// <summary>
        /// Call when Slider.MaximizeValue command is invoked.
        /// </summary>
        protected virtual void OnMaximizeValue()
        {
            this.SetCurrentValueInternal(ValueProperty, this.Maximum);
        }

        /// <summary>
        /// Call when Slider.MinimizeValue command is invoked.
        /// </summary>
        protected virtual void OnMinimizeValue()
        {
            this.SetCurrentValueInternal(ValueProperty, this.Minimum);
        }

        #endregion Virtual Functions

        #region Helper Functions
        /// <summary>
        /// Helper function for value update.
        /// This function will also snap the value to tick, if IsSnapToTickEnabled is true.
        /// </summary>
        /// <param name="value"></param>
        private void UpdateValue(double value)
        {
            Double snappedValue = SnapToTick(value);

            if (snappedValue != Value)
            {
                this.SetCurrentValueInternal(ValueProperty, Math.Max(this.Minimum, Math.Min(this.Maximum, snappedValue)));
            }
        }

        /// <summary>
        /// Validate input value in Slider (LargeChange, SmallChange, SelectionStart, SelectionEnd, and TickFrequency).
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns False if value is NaN or NegativeInfinity or PositiveInfinity. Otherwise, returns True.</returns>
        private static bool IsValidDoubleValue(object value)
        {
            double d = (double)value;

            return !(DoubleUtil.IsNaN(d) || double.IsInfinity(d));
        }


        #endregion Helper Functions


        #region Private Fields

        private const string TrackName = "PART_Track";
        private const string SelectionRangeElementName = "PART_SelectionRange";

        // Slider required parts
        private FrameworkElement _selectionRangeElement;
        private Track _track;
        private ToolTip _autoToolTip = null;
        private object _thumbOriginalToolTip = null;

        #endregion Private Fields

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}

