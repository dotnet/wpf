// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Implementation of ProgressBar control.
//

using System;
using System.Collections.Specialized;
using System.Threading;

using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MS.Internal;
using MS.Internal.Telemetry.PresentationFramework;


using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    /// <summary>
    /// The ProgressBar class
    /// </summary>
    /// <seealso cref="RangeBase" />
    [TemplatePart(Name = "PART_Track", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_Indicator", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_GlowRect", Type = typeof(FrameworkElement))]
    public class ProgressBar : RangeBase
    {
        #region Constructors

        static ProgressBar()
        {
            FocusableProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(typeof(ProgressBar)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ProgressBar));

            // Set default to 100.0
            RangeBase.MaximumProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(100.0));
            ForegroundProperty.OverrideMetadata(typeof(ProgressBar), new FrameworkPropertyMetadata(OnForegroundChanged));

            ControlsTraceLogger.AddControl(TelemetryControls.ProgressBar);
        }

        /// <summary>
        ///     Instantiates a new instance of Progressbar without Dispatcher. 
        /// </summary>
        public ProgressBar() : base()
        {
            // Hook a change handler for IsVisible so we can start/stop animating.
            // Ideally we would do this by overriding metadata, but it's a read-only
            // property so we can't.
            IsVisibleChanged += (s, e) => { UpdateAnimation(); };
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     The DependencyProperty for the IsIndeterminate property.
        ///     Flags:          none
        ///     DefaultValue:   false
        /// </summary>
        public static readonly DependencyProperty IsIndeterminateProperty = 
                DependencyProperty.Register(
                        "IsIndeterminate", 
                        typeof(bool), 
                        typeof(ProgressBar), 
                        new FrameworkPropertyMetadata(
                                false, 
                                new PropertyChangedCallback(OnIsIndeterminateChanged)));

        /// <summary>
        ///     Determines if ProgressBar shows actual values (false)
        ///     or generic, continuous progress feedback (true).
        /// </summary>
        /// <value></value>
        public bool IsIndeterminate
        {
            get { return (bool) GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }

        /// <summary>
        ///     Called when IsIndeterminateProperty is changed on "d".
        /// </summary>
        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar progressBar = (ProgressBar)d;

            // Invalidate automation peer
            ProgressBarAutomationPeer peer = UIElementAutomationPeer.FromElement(progressBar) as ProgressBarAutomationPeer;
            if (peer != null)
            {
                peer.InvalidatePeer();
            }

            progressBar.SetProgressBarGlowElementBrush();

            progressBar.SetProgressBarIndicatorLength();

            progressBar.UpdateVisualState();
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar progressBar = (ProgressBar)d;
            progressBar.SetProgressBarGlowElementBrush();
        }

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
                DependencyProperty.Register(
                        "Orientation", 
                        typeof(Orientation), 
                        typeof(ProgressBar),
                        new FrameworkPropertyMetadata(
                                Orientation.Horizontal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnOrientationChanged)),                        
                        new ValidateValueCallback(IsValidOrientation));

        /// <summary>
        /// Specifies orientation of the ProgressBar.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        internal static bool IsValidOrientation(object o)
        {
            Orientation value = (Orientation)o;
            return value == Orientation.Horizontal
                || value == Orientation.Vertical;
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar progressBar = (ProgressBar)d;
            progressBar.SetProgressBarIndicatorLength();
        }

        #endregion Properties

        #region Event Handler

        // Set the width/height of the contract parts
        private void SetProgressBarIndicatorLength()
        {
            if (_track != null && _indicator != null)
            {
                double min = Minimum;
                double max = Maximum;
                double val = Value;

                // When indeterminate or maximum == minimum, have the indicator stretch the 
                // whole length of track
                double percent = IsIndeterminate || max <= min ? 1.0 : (val - min) / (max - min);
                _indicator.Width = percent * _track.ActualWidth;
                UpdateAnimation();
            }
        }

        // This is used to set the correct brush/opacity mask on the indicator.
        private void SetProgressBarGlowElementBrush()
        {
            if (_glow == null)
                return;
            
            _glow.InvalidateProperty(UIElement.OpacityMaskProperty);
            _glow.InvalidateProperty(Shape.FillProperty);
            if (this.IsIndeterminate)
            {
                if (this.Foreground is SolidColorBrush)
                {
                    Color color = ((SolidColorBrush)this.Foreground).Color;
                    //Create the gradient
                    LinearGradientBrush b = new LinearGradientBrush();
                    
                    b.StartPoint = new Point(0,0);
                    b.EndPoint = new Point(1,0);
                    
                    b.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
                    b.GradientStops.Add(new GradientStop(color, 0.4));                
                    b.GradientStops.Add(new GradientStop(color, 0.6));        
                    b.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                    _glow.SetCurrentValue(Shape.FillProperty, b);
                }
                else
                {
                    // This is not a solid color brush so we will need an opacity mask.
                    LinearGradientBrush mask= new LinearGradientBrush();
                    mask.StartPoint = new Point(0,0);
                    mask.EndPoint = new Point(1,0);
                    mask.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
                    mask.GradientStops.Add(new GradientStop(Colors.Black, 0.4));                
                    mask.GradientStops.Add(new GradientStop(Colors.Black, 0.6));        
                    mask.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                    _glow.SetCurrentValue(UIElement.OpacityMaskProperty, mask);
                    _glow.SetCurrentValue(Shape.FillProperty, this.Foreground);
                }                
            }
}

        //This creates the repeating animation
        private void UpdateAnimation()
        {
            if (_glow != null) 
            {
                if(IsVisible && (_glow.Width > 0) && (_indicator.Width > 0 ))
                {                
                    //Set up the animation
                    double endPos = _indicator.Width + _glow.Width; 
                    double startPos = -1 * _glow.Width;

                    TimeSpan translateTime = TimeSpan.FromSeconds(((int)(endPos - startPos) / 200.0)); // travel at 200px /second
                    TimeSpan pauseTime = TimeSpan.FromSeconds(1.0);  // pause 1 second between animations
                    TimeSpan startTime;

                    //Is the animation currenly running (with one pixel fudge factor)
                    if (DoubleUtil.GreaterThan( _glow.Margin.Left,startPos) && ( DoubleUtil.LessThan(_glow.Margin.Left, endPos-1)))
                    {
                        // make it appear that the timer already started.
                        // To do this find out how many pixels the glow has moved and divide by the speed to get time.
                        startTime = TimeSpan.FromSeconds(-1*(_glow.Margin.Left-startPos)/200.0);
                    }
                    else
                    {
                        startTime = TimeSpan.Zero;
                    }

                    ThicknessAnimationUsingKeyFrames animation = new ThicknessAnimationUsingKeyFrames();
                    
                    animation.BeginTime = startTime;
                    animation.Duration = new Duration(translateTime + pauseTime);
                    animation.RepeatBehavior = RepeatBehavior.Forever;
                    
                    //Start with the glow hidden on the left.
                    animation.KeyFrames.Add(new LinearThicknessKeyFrame(new Thickness(startPos,0,0,0), TimeSpan.FromSeconds(0)));
                    //Move to the glow hidden on the right.
                    animation.KeyFrames.Add(new LinearThicknessKeyFrame(new Thickness(endPos,0,0,0), translateTime));
                    //There is a pause after the glow is off screen

                    _glow.BeginAnimation(FrameworkElement.MarginProperty, animation); 
                }
                else
                {
                    _glow.BeginAnimation(FrameworkElement.MarginProperty, null);
                }
            }
        }

        #endregion

        #region Method Overrides

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsIndeterminate)
            {
                VisualStateManager.GoToState(this, VisualStates.StateDeterminate, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateIndeterminate, useTransitions);
            }

            // Dont call base.ChangeVisualState because we dont want to pick up those state changes (SL compat).
            ChangeValidationVisualState(useTransitions);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ProgressBarAutomationPeer(this);
        }

        /// <summary>
        ///     This method is invoked when the Minimum property changes.
        /// </summary>
        /// <param name="oldMinimum">The old value of the Minimum property.</param>
        /// <param name="newMinimum">The new value of the Minimum property.</param>
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            base.OnMinimumChanged(oldMinimum, newMinimum);
            SetProgressBarIndicatorLength();
        }

        /// <summary>
        ///     This method is invoked when the Maximum property changes.
        /// </summary>
        /// <param name="oldMaximum">The old value of the Maximum property.</param>
        /// <param name="newMaximum">The new value of the Maximum property.</param>
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            base.OnMaximumChanged(oldMaximum, newMaximum);
            SetProgressBarIndicatorLength();
        }

        /// <summary>
        ///     This method is invoked when the Value property changes.
        ///     ProgressBar updates its style parts when Value changes.
        /// </summary>
        /// <param name="oldValue">The old value of the Value property.</param>
        /// <param name="newValue">The new value of the Value property.</param>
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            SetProgressBarIndicatorLength();
        }

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_track != null)
            {
                _track.SizeChanged -= OnTrackSizeChanged;
            }

            _track = GetTemplateChild(TrackTemplateName) as FrameworkElement;
            _indicator = GetTemplateChild(IndicatorTemplateName) as FrameworkElement;
            _glow = GetTemplateChild(GlowingRectTemplateName) as FrameworkElement;
            
            if (_track != null)
            {
                _track.SizeChanged += OnTrackSizeChanged;
            }

            if (this.IsIndeterminate)
                SetProgressBarGlowElementBrush();
        }

        private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetProgressBarIndicatorLength();
        }

        #endregion

        #region Data

        private const string TrackTemplateName = "PART_Track";
        private const string IndicatorTemplateName = "PART_Indicator";
        private const string GlowingRectTemplateName = "PART_GlowRect";

        private FrameworkElement _track;
        private FrameworkElement _indicator;
        private FrameworkElement _glow;
       
        #endregion Data

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
