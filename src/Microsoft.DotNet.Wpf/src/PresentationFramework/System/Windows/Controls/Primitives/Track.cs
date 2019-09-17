// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the Track class.
//

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationFramework;
using MS.Utility;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Input;
using System;
using System.Diagnostics;


namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Track handles layout of the parts of a ScrollBar and Slider.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string    
    public class Track : FrameworkElement
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static Track()
        {
            IsEnabledProperty.OverrideMetadata(typeof(Track), new UIPropertyMetadata(new PropertyChangedCallback(OnIsEnabledChanged)));
        }

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        public Track() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Calculate the value from given Point. The input point is relative to TopLeft conner of Track.
        /// </summary>
        /// <param name="pt">Point (in Track's co-ordinate).</param>        
        public virtual double ValueFromPoint(Point pt)
        {
            double val;
            // Find distance from center of thumb to given point.
            if (Orientation == Orientation.Horizontal)
            {
                val = Value + ValueFromDistance(pt.X - ThumbCenterOffset, pt.Y - (RenderSize.Height * 0.5));
            }
            else
            {
                val = Value + ValueFromDistance(pt.X - (RenderSize.Width * 0.5), pt.Y - ThumbCenterOffset);
            }
            return Math.Max(Minimum, Math.Min(Maximum, val));
        }

        /// <summary>
        /// This function returns the delta in value that would be caused by moving the thumb the given pixel distances.
        /// The returned delta value is not guaranteed to be inside the valid Value range.
        /// </summary>
        /// <param name="horizontal">Total horizontal distance that the Thumb has moved.</param>
        /// <param name="vertical">Total vertical distance that the Thumb has moved.</param>        
        public virtual double ValueFromDistance(double horizontal, double vertical)
        {
            double scale = IsDirectionReversed ? -1 : 1;
            //
            // Note: To implement 'Snap-Back' feature, we could check whether the point is far away from center of the track.
            // If so, just return current value (this should move the Thumb back to its original localtion).
            //
            if (Orientation == Orientation.Horizontal)
            {
                return scale * horizontal * Density;
            }
            else
            {
                // Increases in y cause decreases in Sliders value
                return -1 * scale * vertical * Density;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        private void UpdateComponent(Control oldValue, Control newValue)
        {
            if (oldValue != newValue)
            {
                if (_visualChildren == null)
                {
                    _visualChildren = new Visual[3];
                }

                if (oldValue != null)
                {
                    // notify the visual layer that the old component has been removed.
                    RemoveVisualChild(oldValue);
                }

                // Remove the old value from our z index list and add new value to end
                int i = 0;
                while (i < 3) 
                {
                    // Array isn't full, break
                    if (_visualChildren[i] == null)
                        break;

                    // found the old value
                    if (_visualChildren[i] == oldValue)
                    {
                        // Move values down until end of array or a null element
                        while (i < 2 && _visualChildren[i + 1] != null)
                        {
                            _visualChildren[i] = _visualChildren[i + 1];
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
                // Add newValue at end of z-order
                _visualChildren[i] = newValue;

                AddVisualChild(newValue);

                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        /// <summary>
        /// The RepeatButton used to decrease the Value
        /// </summary>
        public RepeatButton DecreaseRepeatButton
        {
            get
            {
                return _decreaseButton;
            }
            set
            {
                if (_increaseButton == value)
                {
                    throw new NotSupportedException(SR.Get(SRID.Track_SameButtons));
                }
                UpdateComponent(_decreaseButton, value);
                _decreaseButton = value;

                if (_decreaseButton != null)
                {
                    CommandManager.InvalidateRequerySuggested(); // Should post an idle queue item to update IsEnabled on button
                }
            }
        }

        /// <summary>
        /// The Thumb in the Track
        /// </summary>
        public Thumb Thumb
        {
            get
            {
                return _thumb;
            }
            set
            {
                UpdateComponent(_thumb, value);
                _thumb = value;
            }
        }

        /// <summary>
        /// The RepeatButton used to increase the Value
        /// </summary>
        public RepeatButton IncreaseRepeatButton
        {
            get
            {
                return _increaseButton;
            }
            set
            {
                if (_decreaseButton == value)
                {
                    throw new NotSupportedException(SR.Get(SRID.Track_SameButtons));
                }
                UpdateComponent(_increaseButton, value);
                _increaseButton = value;

                if (_increaseButton != null)
                {
                    CommandManager.InvalidateRequerySuggested(); // Should post an idle queue item to update IsEnabled on button
                }
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty OrientationProperty =
                DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Track),
                                          new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure),
                                          new ValidateValueCallback(ScrollBar.IsValidOrientation));

        /// <summary>
        /// This property represents the Track layout orientation: Vertical or Horizontal.
        /// On vertical ScrollBars, the thumb moves up and down.  On horizontal bars, the thumb moves left to right.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="Minimum" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty MinimumProperty =
                RangeBase.MinimumProperty.AddOwner(typeof(Track),
                        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// The Minimum value of the Slider or ScrollBar
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="Maximum" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty MaximumProperty =
                RangeBase.MaximumProperty.AddOwner(typeof(Track),
                        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// The Maximum value of the Slider or ScrollBar
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="Value" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ValueProperty =
                RangeBase.ValueProperty.AddOwner(typeof(Track),
                        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// The current value of the Slider or ScrollBar
        /// </summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="ViewportSize" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty ViewportSizeProperty =
                DependencyProperty.Register("ViewportSize",
                        typeof(double),
                        typeof(Track),
                        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsArrange),
                        new ValidateValueCallback(IsValidViewport));

        /// <summary>
        /// ViewportSize is the amount of the scrolled extent currently visible.  For most scrolled content, this value
        /// will be bound to one of <see cref="ScrollViewer" />'s ViewportSize properties.
        /// This property is in logical scrolling units.
        /// 
        /// Setting this value to NaN will turn off automatic sizing of the thumb
        /// </summary>
        public double ViewportSize
        {
            get { return (double)GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        private static bool IsValidViewport(object o)
        {
            double d = (double)o;
            return d >= 0.0 || double.IsNaN(d);
        }


        /// <summary>
        /// DependencyProperty for <see cref="IsDirectionReversed" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty IsDirectionReversedProperty =
                DependencyProperty.Register("IsDirectionReversed",
                                            typeof(bool),
                                            typeof(Track),
                                            new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Indicates if the location of the DecreaseRepeatButton and IncreaseRepeatButton 
        /// should be swapped.
        /// </summary>
        public bool IsDirectionReversed
        {
            get { return (bool)GetValue(IsDirectionReversedProperty); }
            set { SetValue(IsDirectionReversedProperty, value); }
        }
     
        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods
        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: 
        ///       During this virtual call it is not valid to modify the Visual tree. 
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_visualChildren == null || _visualChildren[index] == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return _visualChildren[index];
        }
        
        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate 
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>        
        protected override int VisualChildrenCount
        {
            get 
            {
                if (_visualChildren == null || _visualChildren[0] == null)
                {
                    Debug.Assert(_visualChildren == null || _visualChildren[1] == null, "Child[1] should be null if Child[0] == null)");
                    Debug.Assert(_visualChildren == null || _visualChildren[2] == null, "Child[2] should be null if Child[0] == null)");
                    return 0;
                }
                else if (_visualChildren[1] == null)
                {
                    Debug.Assert(_visualChildren[2] == null, "Child[2] should be null if Child[1] == null)");
                    return 1;
                }
                else
                {
                    return _visualChildren[2] == null ? 2 : 3;
                }
            }
        }
        
       /// <summary>
        /// The desired size of a Track is the width (if vertically oriented) or height (if horizontally
        /// oriented) of the Thumb.  
        ///
        /// When ViewportSize is NaN:
        ///    The thumb is measured to find the other dimension.  
        /// Otherwise:
        ///    Zero size is returned; Track can scale to any size along its children.
        ///    This means that it will occupy no space (and not display) unless made larger by a parent or specified size.
        /// <seealso cref="FrameworkElement.MeasureOverride" />
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size(0.0, 0.0);

            // Only measure thumb
            // Repeat buttons will be sized based on thumb
            if (Thumb != null)
            {
                Thumb.Measure(availableSize);
                desiredSize = Thumb.DesiredSize;
            }

            if (!double.IsNaN(ViewportSize))
            {
                // ScrollBar can shrink to 0 in the direction of scrolling
                if (Orientation == Orientation.Vertical)
                    desiredSize.Height = 0.0;
                else
                    desiredSize.Width = 0.0;
            }

            return desiredSize;
        }

        // Force length of one of track's pieces to be > 0 and less than tracklength
        private static void CoerceLength(ref double componentLength, double trackLength)
        {
            if (componentLength < 0)
            {
                componentLength = 0.0;
            }
            else if (componentLength > trackLength || double.IsNaN(componentLength))
            {
                componentLength = trackLength;
            }
        }

        /// <summary>
        /// Children will be stretched to fit horizontally (if vertically oriented) or vertically (if horizontally 
        /// oriented).
        /// 
        /// There are essentially three possible layout states:
        /// 1. The track is enabled and the thumb is proportionally sizing.
        /// 2. The track is enabled and the thumb has reached its minimum size. 
        /// 3. The track is disabled or there is not enough room for the thumb. 
        ///    Track elements are not displayed, and will not be arranged.
        /// <seealso cref="FrameworkElement.ArrangeOverride" />
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double decreaseButtonLength, thumbLength, increaseButtonLength;

            bool isVertical = (Orientation == Orientation.Vertical);


            double viewportSize = Math.Max(0.0, ViewportSize);

            // If viewport is NaN, compute thumb's size based on its desired size,
            // otherwise compute the thumb base on the viewport and extent properties
            if (double.IsNaN(ViewportSize))
            {
                ComputeSliderLengths(arrangeSize, isVertical, out decreaseButtonLength, out thumbLength, out increaseButtonLength);
            }
            else
            {
                // Don't arrange if there's not enough content or the track is too small
                if (!ComputeScrollBarLengths(arrangeSize, viewportSize, isVertical, out decreaseButtonLength, out thumbLength, out increaseButtonLength))
                {
                    return arrangeSize;
                }
            }

            // Layout the pieces of track

            Point offset = new Point();
            Size pieceSize = arrangeSize;
            bool isDirectionReversed = IsDirectionReversed;

            if (isVertical)
            {
                // Vertical Normal   :    |Inc Button |
                //                        |Thumb      |
                //                        |Dec Button | 
                // Vertical Reversed :    |Dec Button |
                //                        |Thumb      |
                //                        |Inc Button | 

                CoerceLength(ref decreaseButtonLength, arrangeSize.Height);
                CoerceLength(ref increaseButtonLength, arrangeSize.Height);
                CoerceLength(ref thumbLength, arrangeSize.Height);

                offset.Y = isDirectionReversed ? decreaseButtonLength + thumbLength : 0.0;
                pieceSize.Height = increaseButtonLength;
                
                if (IncreaseRepeatButton != null)
                    IncreaseRepeatButton.Arrange(new Rect(offset, pieceSize));


                offset.Y = isDirectionReversed ? 0.0 : increaseButtonLength + thumbLength;
                pieceSize.Height = decreaseButtonLength;

                if (DecreaseRepeatButton != null)
                    DecreaseRepeatButton.Arrange(new Rect(offset, pieceSize));


                offset.Y = isDirectionReversed ? decreaseButtonLength : increaseButtonLength;
                pieceSize.Height = thumbLength;

                if (Thumb != null)
                    Thumb.Arrange(new Rect(offset, pieceSize));

                ThumbCenterOffset = offset.Y + (thumbLength * 0.5);
            }
            else
            {   
                // Horizontal Normal   :    |Dec Button |Thumb| Inc Button| 
                // Horizontal Reversed :    |Inc Button |Thumb| Dec Button|

                CoerceLength(ref decreaseButtonLength, arrangeSize.Width);
                CoerceLength(ref increaseButtonLength, arrangeSize.Width);
                CoerceLength(ref thumbLength, arrangeSize.Width);

                offset.X = isDirectionReversed ? increaseButtonLength + thumbLength : 0.0;
                pieceSize.Width = decreaseButtonLength;

                if (DecreaseRepeatButton != null)
                    DecreaseRepeatButton.Arrange(new Rect(offset, pieceSize));


                offset.X = isDirectionReversed ? 0.0 : decreaseButtonLength + thumbLength;
                pieceSize.Width = increaseButtonLength;

                if (IncreaseRepeatButton != null)
                    IncreaseRepeatButton.Arrange(new Rect(offset, pieceSize));


                offset.X = isDirectionReversed ? increaseButtonLength : decreaseButtonLength;
                pieceSize.Width = thumbLength;
                
                if (Thumb != null)
                    Thumb.Arrange(new Rect(offset, pieceSize));

                ThumbCenterOffset = offset.X + (thumbLength * 0.5);
            }

            return arrangeSize;
        }


        // Computes the length of the decrease button, thumb and increase button
        // Thumb's size is based on it's desired size
        private void ComputeSliderLengths(Size arrangeSize, bool isVertical, out double decreaseButtonLength, out double thumbLength, out double increaseButtonLength)
        {
            double min = Minimum;
            double range = Math.Max(0.0, Maximum - min);
            double offset = Math.Min(range, Value - min);

            double trackLength;

            // Compute thumb size
            if (isVertical)
            {
                trackLength = arrangeSize.Height;
                thumbLength = Thumb == null ? 0 : Thumb.DesiredSize.Height;
            }
            else
            {
                trackLength = arrangeSize.Width;
                thumbLength = Thumb == null ? 0 : Thumb.DesiredSize.Width;
            }

            CoerceLength(ref thumbLength, trackLength);

            double remainingTrackLength = trackLength - thumbLength;

            decreaseButtonLength = remainingTrackLength * offset / range;
            CoerceLength(ref decreaseButtonLength, remainingTrackLength);
           
            increaseButtonLength = remainingTrackLength - decreaseButtonLength;
            CoerceLength(ref increaseButtonLength, remainingTrackLength);

            Debug.Assert(decreaseButtonLength >= 0.0 && decreaseButtonLength <= remainingTrackLength, "decreaseButtonLength is outside bounds");
            Debug.Assert(increaseButtonLength >= 0.0 && increaseButtonLength <= remainingTrackLength, "increaseButtonLength is outside bounds");
            
            Density = range / remainingTrackLength;
        }

        // Computes the length of the decrease button, thumb and increase button
        // Thumb's size is based on viewport and extent
        // returns false if the track should be hidden
        private bool ComputeScrollBarLengths(Size arrangeSize, double viewportSize, bool isVertical, out double decreaseButtonLength, out double thumbLength, out double increaseButtonLength)
        {
            double min = Minimum;
            double range = Math.Max(0.0, Maximum - min);
            double offset = Math.Min(range, Value - min);

            Debug.Assert(DoubleUtil.GreaterThanOrClose(offset, 0.0), "Invalid offest (negative value).");

            double extent = Math.Max(0.0, range) + viewportSize;

            double trackLength;

            // Compute thumb size
            double thumbMinLength;
            if (isVertical)
            {
                trackLength = arrangeSize.Height;
                // Try to use the apps resource if it exists, fall back to SystemParameters if it doesn't
                object buttonHeightResource = TryFindResource(SystemParameters.VerticalScrollBarButtonHeightKey);
                double buttonHeight = buttonHeightResource is double ? (double)buttonHeightResource : SystemParameters.VerticalScrollBarButtonHeight;
                thumbMinLength = Math.Floor(buttonHeight * 0.5); 
            }
            else
            {
                trackLength = arrangeSize.Width;
                // Try to use the apps resource if it exists, fall back to SystemParameters if it doesn't
                object buttonWidthResource = TryFindResource(SystemParameters.HorizontalScrollBarButtonWidthKey);
                double buttonWidth = buttonWidthResource is double ? (double)buttonWidthResource : SystemParameters.HorizontalScrollBarButtonWidth;
                thumbMinLength = Math.Floor(buttonWidth * 0.5);
            }

            thumbLength =  trackLength * viewportSize / extent;
            CoerceLength(ref thumbLength, trackLength);
         
            thumbLength = Math.Max(thumbMinLength, thumbLength);


            // If we don't have enough content to scroll, disable the track.
            bool notEnoughContentToScroll = DoubleUtil.LessThanOrClose(range, 0.0);
            bool thumbLongerThanTrack = thumbLength > trackLength;

            // if there's not enough content or the thumb is longer than the track, 
            // hide the track and don't arrange the pieces
            if (notEnoughContentToScroll || thumbLongerThanTrack)
            {
                if (Visibility != Visibility.Hidden)
                {
                    Visibility = Visibility.Hidden;
                }

                ThumbCenterOffset = Double.NaN;
                Density = Double.NaN;
                decreaseButtonLength = 0.0;
                increaseButtonLength = 0.0;
                return false; // don't arrange
            }
            else if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible;
            }

            // Compute lengths of increase and decrease button
            double remainingTrackLength = trackLength - thumbLength;
            decreaseButtonLength = remainingTrackLength * offset / range;
            CoerceLength(ref decreaseButtonLength, remainingTrackLength);

            increaseButtonLength = remainingTrackLength - decreaseButtonLength;
            CoerceLength(ref increaseButtonLength, remainingTrackLength);

            Density = range / remainingTrackLength;

            return true;
        }


        // Bind track to templated parent
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

        // Bind thumb or repeat button to templated parent
        private void BindChildToTemplatedParent(FrameworkElement element, DependencyProperty target, DependencyProperty source)
        {
            if (element != null && !element.HasNonDefaultValue(target))
            {
                Binding binding = new Binding();
                binding.Source = this.TemplatedParent;
                binding.Path = new PropertyPath(source);
                element.SetBinding(target, binding);
            }
        }

        /// <summary>
        /// Track automatically sets bindings to its templated parent 
        /// to aid styling
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            RangeBase rangeBase = TemplatedParent as RangeBase;

            if (rangeBase != null)
            {
                BindToTemplatedParent(MinimumProperty, RangeBase.MinimumProperty);
                BindToTemplatedParent(MaximumProperty, RangeBase.MaximumProperty);
                BindToTemplatedParent(ValueProperty, RangeBase.ValueProperty);

                // Setup ScrollBar specific bindings
                ScrollBar scrollBar = rangeBase as ScrollBar;

                if (scrollBar != null)
                {
                    BindToTemplatedParent(ViewportSizeProperty, ScrollBar.ViewportSizeProperty);
                    BindToTemplatedParent(OrientationProperty, ScrollBar.OrientationProperty);
                }
                else
                {
                    // Setup Slider specific bindings
                    Slider slider = rangeBase as Slider;

                    if (slider != null)
                    {
                        BindToTemplatedParent(OrientationProperty, Slider.OrientationProperty);
                        BindToTemplatedParent(IsDirectionReversedProperty, Slider.IsDirectionReversedProperty);

                        BindChildToTemplatedParent(DecreaseRepeatButton, RepeatButton.DelayProperty, Slider.DelayProperty);
                        BindChildToTemplatedParent(DecreaseRepeatButton, RepeatButton.IntervalProperty, Slider.IntervalProperty);
                        BindChildToTemplatedParent(IncreaseRepeatButton, RepeatButton.DelayProperty, Slider.DelayProperty);
                        BindChildToTemplatedParent(IncreaseRepeatButton, RepeatButton.IntervalProperty, Slider.IntervalProperty);
                    }
                }
            }
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // When IsEnabled of UIElement changes the InputManager.HitTestInvalidatedAsync is
            // queued to be executed at input priority. This execution will eventually call Mouse.Synchronize
            // which may result in addition of new elements to the route of routed events from that moment.
            // Tracks are usually associated with triggers which enables them when IsMouseOver is true.
            // A combination of all these works good for Mouse and pen based stylus, because MouseMoves are generated
            // beforehand independent of respective 'Down' events due to firsthand Mouse moves/ InRange pen moves, 
            // and hence HitTestInvalidatedAsync gets executed much before the 'Down' event appears. This is not true for
            // Touch because there is no equivalent of InRange pen moves in touch. 
            // 
            //  Pen based event flow
            //      StylusInRange 
            //          |
            //          V
            //      StylusMove --> Generates a Mouse Move --> enqueues HitTestInvalidatedAsync
            //          |
            //          V
            //      HitTestInvalidatedAsync (an input priority dispactcher operation)
            //          |
            //          V
            //      StylusDown --> Generates MouseDown (at this moment Track is already in the route)
            //
            //
            //  Finger based event flow
            //      StylusDown --> Generates a Mouse Move (to sync the cursor) --> enqueues HitTestInvalidatedAsync --> Followed by TouchDown --> Followed by MouseDown
            //          |
            //          V
            //      HitTestInvalidatedAsync (an input priority dispactcher operation)
            //
            //
            // Note that in pen based stylus, the HitTestInvalidatedAsync gets executed before the MouseDown, and hence the track is
            // included into its route and things work fine. Where as in finger based stylus, since the MouseMove is generated due to
            // StylusDown, HitTestInvalidateAsync (which is the next operation in the queue) doesnt get executed till after MouseDown is routed
            // (which happens in the same dispatcher operation) and hence the Track doesn't get included into the route of MouseDown and things dont work.
            // The fix here is to do the Mouse.Synchronize ourselves synchrounously when IsEnabled of Track changes, instead of waiting
            // for the next input dispatcher operation to happen.
            if ((bool)e.NewValue)
            {
                Mouse.Synchronize();
            }
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        private double ThumbCenterOffset
        {
            get { return _thumbCenterOffset; }
            set { _thumbCenterOffset = value; }
        }
        private double Density
        {
            get { return _density; }
            set { _density = value; }
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 28; }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private RepeatButton _increaseButton;
        private RepeatButton _decreaseButton;
        private Thumb _thumb;
        private Visual[] _visualChildren;

        // Density of scrolling units present in 1/96" of track (not thumb).  Computed during ArrangeOverride.
        // Note that density default really *is* NaN.  This corresponds to no track having been computed/displayed.
        private double _density = Double.NaN;
        private double _thumbCenterOffset = Double.NaN;

        #endregion
    }
}


