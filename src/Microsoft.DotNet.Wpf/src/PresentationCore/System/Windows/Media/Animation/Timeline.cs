// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

#if DEBUG
#define TRACE
#endif // DEBUG

using MS.Internal;
using MS.Utility;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Markup;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Describes run-time timing behavior for timed objects.
    /// </summary>
    /// <remarks>
    /// A Timeline object defines the run-time behavior of a Clock
    /// object. Clock objects are arranged in trees. Correspondingly,
    /// Timeline objects are also arranged in trees. When a tree of clocks is
    /// created, its structure follows that of the tree of Timeline objects.
    /// </remarks>
    [RuntimeNameProperty("Name")]
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)] // cannnot be read & localized as string    
    public abstract partial class Timeline : Animatable
    {
        #region External interface

        #region Construction

        /// <summary>
        /// Creates a Timeline with default properties.
        /// </summary>
        protected Timeline()
        {
#if DEBUG
            lock (_debugLockObject)
            {
                _debugIdentity = ++_nextIdentity;
                WeakReference weakRef = new WeakReference(this);
                _objectTable[_debugIdentity] = weakRef;
            }
#endif // DEBUG

        }

        /// <summary>
        /// Creates a Timeline with the specified BeginTime.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this timeline.
        /// </param>
        protected Timeline(Nullable<TimeSpan> beginTime)
            : this()
        {
            BeginTime = beginTime;
        }

        /// <summary>
        /// Creates a Timeline with the specified begin time and duration.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this timeline.
        /// </param>
        /// <param name="duration">
        /// The simple Duration of this timeline.
        /// </param>
        protected Timeline(Nullable<TimeSpan> beginTime, Duration duration)
            : this()
        {
            BeginTime = beginTime;
            Duration = duration;
        }

        /// <summary>
        /// Creates a Timeline with the specified BeginTime, Duration and RepeatBehavior.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this Timeline.
        /// </param>
        /// <param name="duration">
        /// The simple Duration of this Timeline.
        /// </param>
        /// <param name="repeatBehavior">
        /// The RepeatBehavior for this Timeline.
        /// </param>
        protected Timeline(Nullable<TimeSpan> beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : this()
        {
            BeginTime = beginTime;
            Duration = duration;
            RepeatBehavior = repeatBehavior;
        }

        #endregion // Construction

        #region Freezable

        /// <summary>
        /// Override of FreezeCore.  We need to validate the timeline
        /// before Freezing it. 
        /// </summary>
        /// <param name="isChecking"></param>
        protected override bool FreezeCore(bool isChecking)
        {
            ValidateTimeline();
            return base.FreezeCore(isChecking);
        }

        //
        // Overrides for GetAsFrozenCore and GetCurrentValueAsFrozenCore
        // Timeline does not need to overide CloneCore and CloneCurrentValueCore
        // See the comment in CopyCommon
        //
        
        /// <summary>
        /// Creates a frozen base value clone of another Timeline.
        /// </summary>
        /// <param name="sourceFreezable">
        /// The timeline to copy properties from. If this parameter is null,
        /// this timeline is constructed with default property values.
        /// </param>
        /// <remarks>
        /// This method should be used by implementations of Freezable.CloneCommonCore.
        /// </remarks>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            Timeline sourceTimeline = (Timeline)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);

            CopyCommon(sourceTimeline);
        }


        /// <summary>
        /// Creates a frozen current value clone of another Timeline.
        /// </summary>
        /// <param name="sourceFreezable">
        /// The timeline to copy properties from. If this parameter is null,
        /// this timeline is constructed with default property values.
        /// </param>
        /// <remarks>
        /// This method should be used by implementations of CopyCommonCore.
        /// </remarks>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            Timeline sourceTimeline = (Timeline)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);

            CopyCommon(sourceTimeline);
        }

        #endregion

        #region Properties

        private static void Timeline_PropertyChangedFunction(DependencyObject d, 
                                                              DependencyPropertyChangedEventArgs e)
        {
            ((Timeline)d).PropertyChanged(e.Property);
        }

        #region AccelerationRatio Property


        /// <summary>
        /// AccelerationRatio Property
        /// </summary>
        public static readonly DependencyProperty AccelerationRatioProperty =
            DependencyProperty.Register(
                "AccelerationRatio",
                typeof(double),
                typeof(Timeline),
                new PropertyMetadata(
                    (double)0.0,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                    new ValidateValueCallback(ValidateAccelerationDecelerationRatio));

        /// <summary>
        /// Gets or sets a value indicating the percentage of the duration of
        /// an active period spent accelerating the passage of time from zero
        /// to its maximum rate.
        /// </summary>
        /// <value>
        /// The percentage of the duration of an active period spent
        /// accelerating the passage of time from zero to its maximum rate.
        /// </value>
        /// <remarks>
        /// This property must be set to a value between 0 and 1, inclusive,
        /// or it raises an InvalidArgumentException exception. This property
        /// has a default value of zero.
        /// </remarks>
        public double AccelerationRatio
        {
            get
            {
                return (double)GetValue(AccelerationRatioProperty);
            }
            set
            {
                SetValue(AccelerationRatioProperty, value);
            }
        }

        private static bool ValidateAccelerationDecelerationRatio(object value)
        {
            double newValue = (double)value;

            if (newValue < 0 || newValue > 1 || double.IsNaN(newValue))
            {
                throw new ArgumentException(SR.Get(SRID.Timing_InvalidArgAccelAndDecel), "value");
            }

            return true;
        }

        #endregion // AccelerationRatio Property


        #region AutoReverse Property

        /// <summary>
        /// AutoReverseProperty
        /// </summary>
        public static readonly DependencyProperty AutoReverseProperty =
            DependencyProperty.Register(
                "AutoReverse",
                typeof(bool),
                typeof(Timeline),
                new PropertyMetadata(
                    false,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));

        /// <summary>
        /// Gets or sets a value indicating whether a normal
        /// forward-progressing activation period should be succeeded by a
        /// backward-progressing activation period.
        /// </summary>
        /// <value>
        /// true if a normal forward-progressing activation period should be
        /// succeeded by a backward-progressing activation period; otherwise,
        /// false.
        /// </value>
        /// <remarks>
        /// This property has a default value of false.
        /// </remarks>
        [DefaultValue(false)]
        public bool AutoReverse
        {
            get
            {
                return (bool)GetValue(AutoReverseProperty);
            }
            set
            {
                SetValue(AutoReverseProperty, value);
            }
        }

        #endregion


        #region BeginTime Property

        /// <summary>
        /// BeginTimeProperty
        /// </summary>
        public static readonly DependencyProperty BeginTimeProperty =
            DependencyProperty.Register(
                "BeginTime",
                typeof(TimeSpan?),
                typeof(Timeline),
                new PropertyMetadata(
                    (TimeSpan?)TimeSpan.Zero,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));

        /// <summary>
        /// Gets or sets the scheduled time at which this Timeline should
        /// begin, relative to its parents begin time, in local coordinates.
        /// </summary>
        /// <value>
        /// The scheduled time at which this Timeline should begin, relative
        /// to its parents begin time, in local coordinates.
        /// </value>
        /// <remarks>
        /// This property has a default value of zero.
        /// </remarks>
        public TimeSpan? BeginTime
        {
            get
            {
                return (TimeSpan?)GetValue(BeginTimeProperty);
            }
            set
            {
                SetValue(BeginTimeProperty, value);
            }
        }

        #endregion // BeginTime Property

        #region DecelerationRatio Property 

        /// <summary>
        /// DecelerationRatioProperty
        /// </summary>
        public static readonly DependencyProperty DecelerationRatioProperty =
            DependencyProperty.Register(
                "DecelerationRatio",
                typeof(double),
                typeof(Timeline),
                new PropertyMetadata(
                    (double)0.0,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                    new ValidateValueCallback(ValidateAccelerationDecelerationRatio));

        /// <summary>
        /// Gets or sets a value indicating the percentage of the duration
        /// of an active period spent decelerating the passage of time its
        /// maximum rate to from zero.
        /// </summary>
        /// <value>
        /// The percentage of the duration of an active period spent
        /// decelerating the passage of time its maximum rate to from zero.
        /// </value>
        /// <remarks>
        /// This property must be set to a value between 0 and 1, inclusive,
        /// or it raises an InvalidArgumentException exception. This
        /// property has a default value of zero.
        /// </remarks>
        public double DecelerationRatio
        {
            get
            {
                return (double)GetValue(DecelerationRatioProperty);
            }
            set
            {
                SetValue(DecelerationRatioProperty, value);
            }
        }

        #endregion // DecelerationRatio Property

        #region DesiredFrameRate Property

        /// <summary>
        /// DesiredFrameRateProperty
        /// </summary>
        public static readonly DependencyProperty DesiredFrameRateProperty =
            DependencyProperty.RegisterAttached(
                "DesiredFrameRate",
                typeof(Int32?),
                typeof(Timeline),
                new PropertyMetadata(
                    (Int32?)null,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                    new ValidateValueCallback(ValidateDesiredFrameRate));

        private static bool ValidateDesiredFrameRate(object value)
        {
            Int32? desiredFrameRate = (Int32?)value;

            return  (!desiredFrameRate.HasValue || desiredFrameRate.Value > 0);
        }

        /// <summary>
        /// Reads the attached property DesiredFrameRate from the given Timeline.
        /// </summary>
        /// <param name="timeline">Timeline from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Timeline.DesiredFrameRateProperty" />
        public static Int32? GetDesiredFrameRate(Timeline timeline)
        {
            if (timeline == null) { throw new ArgumentNullException("timeline"); }

            return (Int32?)timeline.GetValue(DesiredFrameRateProperty);
        }

        /// <summary>
        /// Writes the attached property DesiredFrameRate to the given Timeline.
        /// </summary>
        /// <param name="timeline">Timeline to which to write the attached property.</param>
        /// <param name="desiredFrameRate">The property value to set</param>
        /// <seealso cref="Timeline.DesiredFrameRateProperty" />
        public static void SetDesiredFrameRate(Timeline timeline, Int32? desiredFrameRate)
        {
            if (timeline == null) { throw new ArgumentNullException("timeline"); }

            timeline.SetValue(DesiredFrameRateProperty, desiredFrameRate);
        }

        #endregion // DesiredFrameRate Property

        #region Duration Property
        /// <summary>
        /// DurationProperty
        /// </summary>
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                "Duration",
                typeof(Duration),
                typeof(Timeline),
                new PropertyMetadata(
                    Duration.Automatic,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));


        /// <summary>
        /// Gets or sets a value indicating the natural length of an
        /// activation period, in local coordinates.
        /// </summary>
        /// <value>
        /// The natural length of an activation period, in local coordinates.
        /// </value>
        /// <remarks>
        /// This length represents a single forward or backward section of a
        /// single repeat iteration. This property has a default value of
        /// Duration.Automatic.
        /// </remarks>
        public Duration Duration
        {
            get
            {
               return (Duration)GetValue(DurationProperty);
            }
            set
            {
                SetValue(DurationProperty, value);
            }
        }

        #endregion // Duration Property

        #region FillBehavior Property

        /// <summary>
        /// FillBehavior Property
        /// </summary>
        public static readonly DependencyProperty FillBehaviorProperty =
            DependencyProperty.Register(
                "FillBehavior",
                typeof(FillBehavior),
                typeof(Timeline),
                new PropertyMetadata(
                    FillBehavior.HoldEnd,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateFillBehavior));
        
       
        private static bool ValidateFillBehavior(object value)
        {
            return TimeEnumHelper.IsValidFillBehavior((FillBehavior)value);
        }

        /// <summary>
        /// This property indicates how a Timeline will behave when it is outside
        /// of its active period but its parent is in its active or hold period.
        /// </summary>
        /// <value></value>
        public FillBehavior FillBehavior
        {
            get
            {
                return (FillBehavior)GetValue(FillBehaviorProperty);
            }
            set
            {
                SetValue(FillBehaviorProperty, value);
            }
        }

        #endregion // FillBehavior Property

        #region Name Property

        /// <summary>
        /// Name Property
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                "Name",
                typeof(string),
                typeof(Timeline),
                new PropertyMetadata(
                    (string)null,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(System.Windows.Markup.NameValidationHelper.NameValidationCallback));

        /// <summary>
        /// Gets or sets the Name of this Timeline.
        /// </summary>
        /// <value>
        /// The Name of this Timeline.
        /// </value>
        /// <remarks>
        /// This property can be used to set up sync-arcs between sibling
        /// Timeline objects. A sync-arc is established when the
        /// <see ref="TimeSyncValue.SyncTimeline"/> property of one Timeline
        /// corresponds to the Name of another Timeline.
        /// </remarks>
        [DefaultValue((string)null)]
        [MergableProperty(false)]
        public string Name
        {
            get
            {
                return (string)GetValue(NameProperty);
            }
            set
            {
                SetValue(NameProperty, value);
            }
        }

        #endregion // Name Property

        #region RepeatBehavior Property
        
        /// <summary>
        /// RepeatBehaviorProperty
        /// </summary>
        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.Register(
                "RepeatBehavior",
                typeof(RepeatBehavior),
                typeof(Timeline),
                new PropertyMetadata(
                    new RepeatBehavior(1.0),
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)));
         

        /// <summary>
        /// Gets or sets the a RepeatBehavior structure which specifies the way this Timeline will
        /// repeat its simple duration.
        /// </summary>
        /// <value>A RepeatBehavior structure which specifies the way this Timeline will repeat its
        /// simple duration.</value>
        public RepeatBehavior RepeatBehavior
        {
            get
            {
                return (RepeatBehavior)GetValue(RepeatBehaviorProperty);
            }
            set
            {
                SetValue(RepeatBehaviorProperty, value);
            }
        }

        #endregion // RepeatBehavior Property


        #region SpeedRatio Property
        /// <summary>
        /// SpeedRatioProperty
        /// </summary>
        public static readonly DependencyProperty SpeedRatioProperty =
            DependencyProperty.Register(
                "SpeedRatio",
                typeof(double),
                typeof(Timeline),
                new PropertyMetadata(
                    (double)1.0,
                    new PropertyChangedCallback(Timeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateSpeedRatio));

        /// <summary>
        /// Gets or sets the ratio at which time progresses on this Timeline,
        /// relative to its parent.
        /// </summary>
        /// <value>
        /// The ratio at which time progresses on this Timeline, relative to
        /// its parent.
        /// </value>
        /// <remarks>
        /// If Acceleration or Deceleration are specified, this ratio is the
        /// average ratio over the natural length of the Timeline. This
        /// property has a default value of 1.0.
        /// </remarks>
        [DefaultValue((double)1.0)]
        public double SpeedRatio
        {
            get
            {
                return (double)GetValue(SpeedRatioProperty);
            }
            set
            {
                SetValue(SpeedRatioProperty, value);
            }
        }

        private static bool ValidateSpeedRatio(object value)
        {
            double newValue = (double)value;

            if (newValue <= 0 || newValue > double.MaxValue || double.IsNaN(newValue))
            {
                throw new ArgumentException(SR.Get(SRID.Timing_InvalidArgFinitePositive), "value");
            }

            return true;
        }

        #endregion // SpeedRatio Property

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Called by the <see ref="Clock.FromTimeline" /> method to
        /// create a type-specific clock for this Timeline.
        /// </summary>
        /// <returns>
        /// A clock for this Timeline.
        /// </returns>
        /// <remarks>
        /// If a derived class overrides this method, it should only create
        /// and return an object of a class inheriting from Clock.
        /// </remarks>
        protected internal virtual Clock AllocateClock()
        {
            return new Clock(this);
        }

        /// <summary>
        /// Creates a new Clock using this Timeline as the root. If this
        /// Timeline has children, a tree of clocks will be created. 
        /// </summary>
        /// <remarks>
        /// Although this Timeline may be included as a child of one or more
        /// TimelineGroups, this information will be ignored. For the purposes
        /// of this method this Timeline will be treated as a root Timeline.
        /// </remarks>
        /// <returns>
        /// A new Clock or tree of Clocks depending on whether
        /// or not this Timeline is a TimelineGroup that contains children.
        /// </returns>
        public Clock CreateClock()
        {
            return CreateClock(true);
        }

        /// <summary>
        /// Creates a new Clock using this Timeline as the root. If this
        /// Timeline has children, a tree of clocks will be created. 
        /// </summary>
        /// <param name="hasControllableRoot">True if the root Clock returned should
        /// return a ClockController from its Controller property so that
        /// the Clock tree can be interactively controlled.</param>
        /// <remarks>
        /// Although this Timeline may be included as a child of one or more
        /// TimelineGroups, this information will be ignored. For the purposes
        /// of this method this Timeline will be treated as a root Timeline.
        /// </remarks>
        /// <returns>
        /// A new Clock or tree of Clocks depending on whether
        /// or not this Timeline is a TimelineGroup that contains children.
        /// </returns>
        public Clock CreateClock(bool hasControllableRoot)
        {
            // Create the tree of clocks from this timeline
            return Clock.BuildClockTreeFromTimeline(this, hasControllableRoot);
        }

        /// <summary>
        /// Returns the period of a single iteration.  This will only be called when
        /// the Duration property is set to Automatic.  If Duration is Automatic,
        /// the natural duration is determined by the nature of the specific timeline class,
        /// as determined by its author.  If GetNaturalDuration returns Automatic, it means
        /// that the natural duration is unknown, which temporarily implies Forever.
        /// Streaming media would fit this case.
        /// </summary>
        /// <param name="clock">
        /// The Clock whose natural duration is desired.
        /// </param>
        /// <returns>
        /// A Duration quantity representing the natural duration.
        /// </returns>
        internal protected Duration GetNaturalDuration(Clock clock)
        {
            return GetNaturalDurationCore(clock);
        }

        /// <summary>
        /// Implemented by the class author to provide a custom natural Duration
        /// in the case that the Duration property is set to Automatic.  If the author
        /// cannot determine the Duration, this method should return Automatic.
        /// </summary>
        /// <param name="clock">
        /// The Clock whose natural duration is desired.
        /// </param>
        /// <returns>
        /// A Duration quantity representing the natural duration.
        /// </returns>
        protected virtual Duration GetNaturalDurationCore(Clock clock)
        {
            return Duration.Automatic;
        }

        /// <summary>
        /// This method will throw an exception if a timeline has been incorrectly
        /// constructed.  Currently we validate all possible values when they are 
        /// set, but it is not possible to do this for Acceleration/DecelerationRatio.
        /// 
        /// The reason is when a timeline is instantiated in xaml the properties are
        /// set with direct calls to DependencyObject.SetValue.  This means our only
        /// chance to validate the value is in the ValidateValue callback, which does
        /// not allow querying of other DPs.  Acceleration/DecelerationRatio are invalid
        /// if their sum is > 1.  This can't be verified in the ValidateValue callback,
        /// so is done here.
        /// </summary>
        private void ValidateTimeline()
        {
            if (AccelerationRatio + DecelerationRatio > 1)
            {
                throw new InvalidOperationException(SR.Get(SRID.Timing_AccelAndDecelGreaterThanOne));
            }
        }

        #endregion // Methods

        #region Events

        /// <summary>
        /// Raised whenever the value of the CurrentStateInvalidated property changes.
        /// </summary>
        public event EventHandler CurrentStateInvalidated
        {
            add
            {
                AddEventHandler(CurrentStateInvalidatedKey, value);
            }
            remove
            {
                RemoveEventHandler(CurrentStateInvalidatedKey, value);
            }
        }

        /// <summary>
        /// Raised whenever the value of the CurrentTimeInvalidated property changes.
        /// </summary>
        public event EventHandler CurrentTimeInvalidated
        {
            add
            {
                AddEventHandler(CurrentTimeInvalidatedKey, value);
            }
            remove
            {
                RemoveEventHandler(CurrentTimeInvalidatedKey, value);
            }
        }

        /// <summary>
        /// Raised whenever the value of the CurrentGlobalSpeed property changes.
        /// </summary>
        public event EventHandler CurrentGlobalSpeedInvalidated
        {
            add
            {
                AddEventHandler(CurrentGlobalSpeedInvalidatedKey, value);
            }
            remove
            {
                RemoveEventHandler(CurrentGlobalSpeedInvalidatedKey, value);
            }
        }

        /// <summary>
        /// Raised whenever the value of the Completed property changes.
        /// </summary>
        public event EventHandler Completed
        {
            add
            {
                AddEventHandler(CompletedKey, value);
            }
            remove
            {
                RemoveEventHandler(CompletedKey, value);
            }
        }

        /// <summary>
        /// Raised whenever the value of the RemoveRequested property changes.
        /// </summary>
        public event EventHandler RemoveRequested
        {
            add
            {
                AddEventHandler(RemoveRequestedKey, value);
            }
            remove
            {
                RemoveEventHandler(RemoveRequestedKey, value);
            }
        }

        #endregion // Events

        #endregion // External interface

        #region Internal implementation

        #region Properties

        /// <summary>
        /// Read-only access to the EventHandlerStore.  Used by
        /// Clock to copy the Timeline's events
        /// </summary>
        internal EventHandlersStore InternalEventHandlersStore
        {
            get
            {
                return EventHandlersStoreField.GetValue(this);
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Exposes the OnFreezablePropertyChanged protected method for use
        /// by the TimelineCollection class.
        /// </summary>
        /// <param name="originalTimeline">
        /// The previous value of the timeline.
        /// </param>
        /// <param name="newTimeline">
        /// The new value of the timeline.
        /// </param>
        internal void InternalOnFreezablePropertyChanged(Timeline originalTimeline, Timeline newTimeline)
        {
            OnFreezablePropertyChanged(originalTimeline, newTimeline);
        }

        /// <summary>
        /// Called by the TimelineCollection class to propagate the
        /// Freezable.FreezeCore call to this object.
        /// </summary>
        internal bool InternalFreeze(bool isChecking)
        {
            return Freeze(this, isChecking);
        }

        /// <summary>        
        /// Asks this timeline to perform read verification.
        /// </summary>
        internal void InternalReadPreamble()
        {
            ReadPreamble();
        }

        /// <summary>
        /// Notifies this timeline that a change has been made.
        /// </summary>
        internal void InternalWritePostscript()
        {
            WritePostscript();
        }


        /// <summary>
        /// Adds a delegate to the list of event handlers on this object.
        /// </summary>
        /// <param name="key">
        /// A unique identifier for the event handler.
        /// </param>
        /// <param name="handler">The delegate to add.</param>
        private void AddEventHandler(EventPrivateKey key, Delegate handler)
        {
            WritePreamble();

            EventHandlersStore store = EventHandlersStoreField.GetValue(this);

            if (store == null)
            {
                store = new EventHandlersStore();
                EventHandlersStoreField.SetValue(this, store);
            }

            store.Add(key, handler);
            WritePostscript();
        }

        /// <summary>
        /// Implements copy functionalty for GetAsFrozenCore and GetCurrentValueAsFrozenCore
        /// Timeline does not need to override CloneCore and CloneCurrentValueCore.
        /// </summary>
        /// <param name="sourceTimeline"></param>
        private void CopyCommon(Timeline sourceTimeline)
        {
            // When creating a frozen copy of a Timeline we want to copy the
            // event handlers. This is for two reasons
            // 
            //   1.) Internally when creating a clock tree we use
            //       a frozen copy of the timing tree.  If that frozen
            //       copy does not preserve the event handlers then
            //       any callbacks registered on the Timelines will be lost.
            // 
            //   2.) GetAsFrozen and GetCurrentValueAsFrozen don't always clone.  
            //       If any object in the tree is frozen it'll simply return it. 
            //       If we did not copy the event handlers GetAsFrozen
            //       would return different results depending on whether a
            //       Timeline was frozen before the call.
            //       
            //
            // The other two clone methods make unfrozen clones, so it's consisent
            // to not copy the event handlers for those methods.  Cloning an object
            // is basically the only way to get a 'fresh' copy without event handlers
            // attached.  If someone wants a frozen clone they probably want an exact
            // copy of the original.

            EventHandlersStore sourceStore = EventHandlersStoreField.GetValue(sourceTimeline);
            if (sourceStore != null)
            {
                Debug.Assert(sourceStore.Count > 0);
                EventHandlersStoreField.SetValue(this, new EventHandlersStore(sourceStore));
            }
        }

        /// <summary>
        /// Removes a delegate from the list of event handlers on this object.
        /// </summary>
        /// <param name="key">
        /// A unique identifier for the event handler.
        /// </param>
        /// <param name="handler">The delegate to remove.</param>
        private void RemoveEventHandler(EventPrivateKey key, Delegate handler)
        {
            WritePreamble();

            EventHandlersStore store = EventHandlersStoreField.GetValue(this);
            if (store != null)
            {
                store.Remove(key, handler);
                if (store.Count == 0)
                {
                    // last event handler was removed -- throw away underlying EventHandlersStore
                    EventHandlersStoreField.ClearValue(this);
                }

                WritePostscript();
            }
        }

        #endregion // Methods


        #region Debugging instrumentation

#if DEBUG

        /// <summary>
        /// Dumps the description of the subtree rooted at this timeline.
        /// </summary>
        internal void Dump()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Capacity = 1024;
            builder.Append("========================================\n");
            builder.Append("Timelines rooted at Timeline ");
            builder.Append(_debugIdentity);
            builder.Append('\n');
            builder.Append("----------------------------------------\n");
            BuildInfoRecursive(builder, 0);
            builder.Append("----------------------------------------\n");
            Trace.Write(builder.ToString());
        }

        /// <summary>
        /// Dumps the description of all timelines in the known timeline table.
        /// </summary>
        internal static void DumpAll()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int timelineCount = 0;
            builder.Capacity = 1024;
            builder.Append("========================================\n");
            builder.Append("Timelines in the GC heap\n");
            builder.Append("----------------------------------------\n");

            lock (_debugLockObject)
            {
                if (_objectTable.Count > 0)
                {
                    // Output the timelines sorted by Name
                    int[] idTable = new int[_objectTable.Count];
                    _objectTable.Keys.CopyTo(idTable, 0);
                    Array.Sort(idTable);

                    for (int index = 0; index < idTable.Length; index++)
                    {
                        WeakReference weakRef = (WeakReference)_objectTable[idTable[index]];
                        Timeline timeline = (Timeline)weakRef.Target;
                        if (timeline != null)
                        {
                            timeline.BuildInfo(builder, 0, true);
                            timelineCount++;
                        }
                    }
                }
            }

            if (timelineCount == 0)
            {
                builder.Append("There are no Timelines in the GC heap.\n");
            }

            builder.Append("----------------------------------------\n");
            Trace.Write(builder.ToString());
        }

        /// <summary>
        /// Dumps the description of the subtree rooted at this timeline.
        /// </summary>
        /// <param name="builder">
        /// A StringBuilder that accumulates the description text.
        /// </param>
        /// <param name="depth">
        /// The depth of recursion for this timeline.
        /// </param>
        internal void BuildInfoRecursive(System.Text.StringBuilder builder, int depth)
        {
            // Add the info for this timeline
            BuildInfo(builder, depth, true);

            // Recurse into the children
            depth++;
            TimelineGroup timelineGroup = this as TimelineGroup;

            if (timelineGroup != null)
            {
                TimelineCollection children = timelineGroup.Children;
                if (children != null)
                {
                    for (int childIndex = 0; childIndex < children.Count; childIndex++)
                    {
                        children.Internal_GetItem(childIndex).BuildInfoRecursive(builder, depth);
                    }
                }
            }
        }

        /// <summary>
        /// Dumps the description of this timeline.
        /// </summary>
        /// <param name="builder">
        /// A StringBuilder that accumulates the description text.
        /// </param>
        /// <param name="depth">
        /// The depth of recursion for this timeline.
        /// </param>
        /// <param name="includeDebugID">
        /// Whether or not to include the debug ID in the description.
        /// </param>
        internal void BuildInfo(System.Text.StringBuilder builder, int depth, bool includeDebugID)
        {
            // Start with the name of the timeline
            if (includeDebugID)
            {
                builder.Append(' ', depth);
                builder.Append("Timeline ");
                builder.Append(_debugIdentity);
            }
            builder.Append(" (");
            builder.Append(GetType().Name);

            // Build attributes
            if (Name != null)
            {
                builder.Append(", Name=\"");
                builder.Append(Name);
                builder.Append("\"");
            }
            if (AccelerationRatio != 0.0f)
            {
                builder.Append(", AccelerationRatio = ");
                builder.Append(AccelerationRatio.ToString());
            }
            if (AutoReverse != false)
            {
                builder.Append(", AutoReverse = ");
                builder.Append(AutoReverse.ToString());
            }
            if (DecelerationRatio != 0.0f)
            {
                builder.Append(", DecelerationRatio = ");
                builder.Append(DecelerationRatio.ToString());
            }
            if (Duration != Duration.Automatic)
            {
                builder.Append(", Duration = ");
                builder.Append(Duration.ToString());
            }
            if (FillBehavior != FillBehavior.HoldEnd)
            {
                builder.Append(", FillBehavior = ");
                builder.Append(FillBehavior.ToString());
            }
            if (SpeedRatio != 1.0f)
            {
                builder.Append(", Speed = ");
                builder.Append(SpeedRatio);
            }
            builder.Append(")\n");
        
        }

        /// <summary>
        /// Finds a previously registered object.
        /// </summary>
        /// <param name="id">
        /// The Name of the object to look for
        /// </param>
        /// <returns>
        /// The object if found, null otherwise.
        /// </returns>
        internal static Timeline Find(int id)
        {
            Timeline timeline = null;

            lock (_debugLockObject)
            {
                object handleReference = _objectTable[id];
                if (handleReference != null)
                {
                    WeakReference weakRef = (WeakReference)handleReference;
                    timeline = (Timeline)weakRef.Target;
                    if (timeline == null)
                    {
                        // Object has been destroyed, so remove the weakRef.
                        _objectTable.Remove(id);
                    }
                }
            }

            return timeline;
        }

        /// <summary>
        /// Cleans up the known timelines table by removing dead weak
        /// references.
        /// </summary>
        internal static void CleanKnownTimelinesTable()
        {
            lock (_debugLockObject)
            {
                Hashtable removeTable = new Hashtable();

                // Identify dead references
                foreach (DictionaryEntry e in _objectTable)
                {
                    WeakReference weakRef = (WeakReference) e.Value;
                    if (weakRef.Target == null)
                    {
                        removeTable[e.Key] = weakRef;
                    }
                }

                // Remove dead references
                foreach (DictionaryEntry e in removeTable)
                {
                    _objectTable.Remove(e.Key);
                }
            }
        }

#endif // DEBUG

        #endregion // Debugging instrumentation

        #region Data

        #region Event Handler Storage

        internal static readonly UncommonField<EventHandlersStore> EventHandlersStoreField = new UncommonField<EventHandlersStore>();
        
        // Unique identifiers for each of the events defined on Timeline.  
        // This is used as a key in the EventHandlerStore
        internal static readonly EventPrivateKey CurrentGlobalSpeedInvalidatedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey CurrentStateInvalidatedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey CurrentTimeInvalidatedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey CompletedKey = new EventPrivateKey();
        internal static readonly EventPrivateKey RemoveRequestedKey = new EventPrivateKey();

        #endregion // Event Handler Storage

        #region Debug data

#if DEBUG

        internal int                _debugIdentity;

        internal static int         _nextIdentity;
        internal static Hashtable   _objectTable = new Hashtable();
        internal static object      _debugLockObject = new object();

#endif // DEBUG

        #endregion // Debug data

        #endregion // Data

        #endregion // Internal implementation
    }
}
