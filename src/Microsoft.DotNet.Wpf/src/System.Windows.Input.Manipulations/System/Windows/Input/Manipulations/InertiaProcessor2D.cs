// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;
using System.Text;
using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Implements the extrapolation of a manipulation's position, orientation, and average radius.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <strong>InertiaProcessor2D</strong> object enables your application to extrapolate an 
    /// element's location, orientation, and other properties by simulating real-world behavior. 
    /// </para>
    /// <para>
    /// For instance, when a user moves an element and then releases it, 
    /// the element can continue moving, decelerate, and then slowly stop.
    /// An inertia processor implements this behavior by causing the affine 2-D values 
    /// (origin, scale, translation, and rotation) to change over a specified time at a 
    /// specified deceleration rate.
    /// </para>
    /// <para>
    /// An inertia processor by itself does not cause an element to move and decelerate. Your 
    /// application receives information from an inertia processor and applies the values
    /// as needed to an application-specific element. Typically, an application uses the 
    /// information received from an inertia processor to change the location, size or
    /// orientation of an element.
    /// </para>
    /// <para>
    /// Inertia processing is typically used in conjunction with manipulation processing.
    /// For more information, see the
    /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D"/></strong>
    /// class.
    /// </para>
    /// </remarks>
    public class InertiaProcessor2D
    {
        private const double timestampTicksPerMillisecond = ManipulationProcessor2D.TimestampTicksPerMillisecond;
        private const double millisecondsPerTimestampTick = 1.0 / timestampTicksPerMillisecond;
        private const double millisecondsPerTimestampTickSquared = millisecondsPerTimestampTick * millisecondsPerTimestampTick;

        private const string initialOriginXName = "InitialOriginX";
        private const string initialOriginYName = "InitialOriginY";

        #region Private Fields

#if DEBUG
        // for debugging only to log all manipulation activity
        private StringBuilder log = new StringBuilder();
#endif

        // inital timestamp
        private Int64 initialTimestamp;

        // previous timestamp
        private Int64 previousTimestamp = -1L; // should be any value different than initialTimestamp

        // behaviors
        private InertiaTranslationBehavior2D translationBehavior;
        private InertiaRotationBehavior2D rotationBehavior;
        private InertiaExpansionBehavior2D expansionBehavior;

        // initial scale
        private double initialScale = 1.0;
        private double desiredDisplacement = double.NaN;
        private double desiredDeceleration = double.NaN;

        // initial states
        private InitialState initialTranslationX = new InitialState();
        private InitialState initialTranslationY = new InitialState();
        private InitialState initialOrientation = new InitialState();
        private InitialState initialExpansion = new InitialState();

        // exrapolation states
        private ExtrapolationState translationX;
        private ExtrapolationState translationY;
        private ExtrapolationState orientation;
        private ExtrapolationState expansion;

        // current state of the processors
        private ProcessorState processorState = ProcessorState.NotInitialized;

        #endregion Private Fields


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InertiaProcessor2D"/> class.
        /// </summary>
        public InertiaProcessor2D()
        {
            // do not allow expansion be less than 1
            this.initialExpansion.MinBound = 1;
            this.initialExpansion.Value = 1;

            // use property accessors rather than direct member access to
            // ensure that all the proper plumbing is hooked up
            TranslationBehavior = new InertiaTranslationBehavior2D();
            RotationBehavior = new InertiaRotationBehavior2D();
            ExpansionBehavior = new InertiaExpansionBehavior2D();
        }

        #endregion Constructors


        #region Public Properties
        /// <summary>
        /// Gets or sets the x-coordinate for the initial origin, in coordinate units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The origin point represented by the <strong>InitialOriginX</strong> and
        /// <strong><see cref="InitialOriginY"/></strong>
        /// properties is the average position of all manipulators associated with an element. 
        /// </para>
        /// <para>
        ///  A valid value for <strong>InitialOriginX</strong> is any finite number. 
        ///  The default value is 0.0.
        /// </para>
        /// </remarks>
        public float InitialOriginX
        {
            get
            {
                return (float)this.initialTranslationX.Value;
            }
            set
            {
                CheckNotRunning(initialOriginXName);
                CheckOriginalValue(value, initialOriginXName);
                Reset();
                this.initialTranslationX.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the y-coordinate for the initial origin, in coordinate units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The origin point represented by the <strong><see cref="InitialOriginX"/></strong> and
        /// <strong>InitialOriginY</strong>
        /// properties is the average position of all manipulators associated with an element. 
        /// </para>
        /// <para>
        ///  A valid value for <strong>InitialOriginY</strong> is any finite number. 
        ///  The default value is 0.0.
        /// </para>
        /// </remarks>
        public float InitialOriginY
        {
            get
            {
                return (float)this.initialTranslationY.Value;
            }
            set
            {
                CheckNotRunning(initialOriginYName);
                CheckOriginalValue(value, initialOriginYName);
                Reset();
                this.initialTranslationY.Value = value;
            }
        }

        /// <summary>
        /// Gets whether inertia is currently in progress.
        /// </summary>
        /// <example>
        /// <para>
        /// In the following example, an event handler for the 
        /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D.Started">ManipulationProcessor2D.Started</see></strong>
        /// event checks to see if inertia processing is running and, if so, stops it by calling the
        /// <strong><see cref="Complete">InertiaProcessor2D.Complete</see></strong> method.
        /// </para>
        /// <code lang="cs">
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnManipulationStarted"/>
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="Timestamp"/>
        /// </code>
        /// </example>
        public bool IsRunning
        {
            get { return this.processorState == ProcessorState.Running; }
        }

        /// <summary>
        /// Gets or sets the translation behavior of the inertia processor.
        /// </summary>
        public InertiaTranslationBehavior2D TranslationBehavior
        {
            get { return this.translationBehavior; }
            set
            {
                SetBehavior<InertiaTranslationBehavior2D>(
                    ref this.translationBehavior,
                    value,
                    OnTranslationBehaviorChanged,
                    "TranslationBehavior");
            }
        }

        /// <summary>
        /// Gets or sets the rotation behavior of the inertia processor.
        /// </summary>
        /// <example>
        /// In the following example, the 
        /// <strong><see cref="P:System.Windows.Input.Manipulations.InertiaRotationBehavior2D.DesiredRotation"/></strong>
        /// property is set to enable inertia processing to rotate an object 
        /// three-and-one-half times from its starting orientation.
        /// <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="SetDesiredRotation"/>
        /// </example>
        public InertiaRotationBehavior2D RotationBehavior
        {
            get { return this.rotationBehavior; }
            set
            {
                SetBehavior<InertiaRotationBehavior2D>(
                    ref this.rotationBehavior,
                    value,
                    OnRotationBehaviorChanged,
                    "RotationBehavior");
            }
        }

        /// <summary>
        /// Gets or sets the expansion behavior of the inertia processor.
        /// </summary>
        public InertiaExpansionBehavior2D ExpansionBehavior
        {
            get { return this.expansionBehavior; }
            set
            {
                SetBehavior<InertiaExpansionBehavior2D>(
                    ref this.expansionBehavior,
                    value,
                    OnExpansionBehaviorChanged,
                    "ExpansionBehavior");
            }
        }

        #endregion Public Properties


        #region Public Events

        /// <summary>
        /// Occurs when the extrapolation origin has changed or when translation, scaling, or rotation have occurred.
        /// </summary>
        /// <remarks>
        /// The <strong>InertiaProcessor2D.Delta</strong> event and the
        /// <strong><see cref="E:System.Windows.Input.Manipulations.ManipulationProcessor2D.Delta">ManipulationProcessor2D.Delta</see></strong>
        /// event are the same type. Typically, you can use the same event handler
        /// for both events.
        /// </remarks>
        /// <example>
        /// <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnManipulationOrInertiaDelta" lang="cs"/>
        /// </example>
        public event EventHandler<Manipulation2DDeltaEventArgs> Delta;

        /// <summary>
        /// Occurs when extrapolation has completed.
        /// </summary>
        public event EventHandler<Manipulation2DCompletedEventArgs> Completed;

        #endregion Public Events


        #region Public Methods

        /// <summary>
        /// Extrapolates the manipulation's position, orientation, and average radius at the specified
        /// time.
        /// </summary>
        /// <param name="timestamp">The timestamp to perform extrapolation, in 100-nanosecond ticks.</param>
        /// <returns><strong>true</strong> if extrapolation is in progress; otherwise, <strong>false</strong>
        /// if extrapolation has completed.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">The timestamp parameter
        /// is less than the initial or previous timestamp.
        /// </exception>
        /// <remarks>
        /// Timestamps are in 100-nanosecond units.
        /// </remarks>
        public bool Process(Int64 timestamp)
        {
            return Process(timestamp, false/*forceCompleted*/);
        }

        /// <summary>
        /// Completes final extrapolation by using the specified timestamp and raises the
        /// <strong><see cref="Completed"/></strong> event.
        /// </summary>
        /// <param name="timestamp">The timestamp to complete extrapolation, in 100-nanosecond ticks.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">The timestamp parameter
        /// is less than the initial or previous timestamp.
        /// </exception>
        /// <remarks>
        /// Timestamps are in 100-nanosecond units.
        /// </remarks>
        /// <example>
        /// <para>
        /// In the following example, an event handler for the 
        /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D.Started">ManipulationProcessor2D.Started</see></strong>
        /// event checks to see if inertia processing is running and if so, stops it by calling the
        /// <strong>Complete</strong> method.
        /// </para>
        /// <code lang="cs">
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnManipulationStarted"/>
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="Timestamp"/>
        /// </code>
        /// </example>
        public void Complete(Int64 timestamp)
        {
            bool result = Process(timestamp, true/*forceCompleted*/);
            Debug.Assert(!result, "Complete method is supposed to raise Completed event.");
        }

        /// <summary>
        /// Set parameters on the inertia processor.
        /// </summary>
        /// <param name="parameters">Parameters to set.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetParameters(InertiaParameters2D parameters)
        {
            Validations.CheckNotNull(parameters, "parameters");

            InertiaTranslationBehavior2D translationParameters = parameters as InertiaTranslationBehavior2D;
            if (translationParameters != null)
            {
                TranslationBehavior = translationParameters;
                return;
            }

            InertiaRotationBehavior2D rotationParameters = parameters as InertiaRotationBehavior2D;
            if (rotationParameters != null)
            {
                RotationBehavior = rotationParameters;
                return;
            }

            InertiaExpansionBehavior2D expansionParameters = parameters as InertiaExpansionBehavior2D;
            if (expansionParameters != null)
            {
                ExpansionBehavior = expansionParameters;
                return;
            }

            Debug.Fail("Unsupported parameters");
        }
        #endregion Public Methods


        #region Private Methods & Properties

        /// <summary>
        /// Sets the initial timestamp.
        /// </summary>
        private void SetInitialTimestamp(Int64 timestamp)
        {
            Debug.Assert(!IsRunning);
            if (timestamp != this.initialTimestamp)
            {
                Reset();
                this.initialTimestamp = timestamp;
                this.previousTimestamp = timestamp;
            }
        }

        /// <summary>
        /// Resets the processor to the initial state.
        /// </summary>
        private void Reset()
        {
            if (this.processorState != ProcessorState.NotInitialized)
            {
                this.processorState = ProcessorState.NotInitialized;

                // set previousTimestamp to a value different than initialTimestamp,
                // this indicates that initialTimestamp is not initialized, basically
                // IsInitialTimestampInitialized is defined as "previousTimestamp==initialTimestamp"
                this.previousTimestamp = unchecked(this.initialTimestamp - 1);
            }
        }

        /// <summary>
        /// Called when a parameter is changed that is not allowed to change
        /// while inertia is running.
        /// </summary>
        /// <param name="paramName"></param>
        private void CheckNotRunning(string paramName)
        {
            if (IsRunning)
            {
                throw Exceptions.CannotChangeParameterDuringInertia(paramName);
            }
        }

        /// <summary>
        /// Sets acceleration behavior.
        /// </summary>
        /// <typeparam name="TBehavior"></typeparam>
        /// <param name="currentBehavior"></param>
        /// <param name="newBehavior"></param>
        /// <param name="handler"></param>
        /// <param name="propertyName"></param>
        private void SetBehavior<TBehavior>(
            ref TBehavior currentBehavior,
            TBehavior newBehavior,
            Action<InertiaParameters2D, string> handler,
            string propertyName)
            where TBehavior : InertiaParameters2D
        {
            Debug.Assert(handler != null);

            if (!object.ReferenceEquals(newBehavior, currentBehavior))
            {
                if (currentBehavior != null)
                {
                    currentBehavior.Changed -= handler;
                    currentBehavior = null;
                }
                Reset();
                if (newBehavior != null)
                {
                    currentBehavior = newBehavior;
                    currentBehavior.Changed += handler;
                }
                handler(currentBehavior, propertyName);
            }
        }

        /// <summary>
        /// Here when the translation behavior changes.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="paramName"></param>
        private void OnTranslationBehaviorChanged(
            InertiaParameters2D parameters,
            string paramName)
        {
            CheckNotRunning(paramName);
            Reset();

            InertiaTranslationBehavior2D behavior = GetBehavior<InertiaTranslationBehavior2D>(parameters);

            this.desiredDeceleration = behavior.DesiredDeceleration;
            this.desiredDisplacement = behavior.DesiredDisplacement;
            this.initialTranslationX.Velocity = behavior.InitialVelocityX;
            this.initialTranslationY.Velocity = behavior.InitialVelocityY;
        }

        /// <summary>
        /// Here when the rotation behavior changes.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="paramName"></param>
        private void OnRotationBehaviorChanged(
            InertiaParameters2D parameters,
            string paramName)
        {
            CheckNotRunning(paramName);
            Reset();

            InertiaRotationBehavior2D behavior = GetBehavior<InertiaRotationBehavior2D>(parameters);

            this.initialOrientation.AbsoluteDeceleration = behavior.DesiredDeceleration;
            this.initialOrientation.AbsoluteOffset = behavior.DesiredRotation;
            this.initialOrientation.Velocity = behavior.InitialVelocity;
        }

        /// <summary>
        /// Here when the expansion behavior changes.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="paramName"></param>
        private void OnExpansionBehaviorChanged(
            InertiaParameters2D parameters,
            string paramName)
        {
            CheckNotRunning(paramName);
            Reset();

            InertiaExpansionBehavior2D behavior = GetBehavior<InertiaExpansionBehavior2D>(parameters);

            this.initialExpansion.Value = behavior.InitialRadius;
            this.initialExpansion.AbsoluteDeceleration = behavior.DesiredDeceleration;

            // Just use the X values and ignore the Y. Y will either be the same
            // (in which case ignoring it doesn't matter), or else an exception
            // will get thrown when they try to start the processor (in which case
            // it doesn't matter, either).
            this.initialExpansion.AbsoluteOffset = (behavior == null) ? float.NaN : behavior.DesiredExpansionX;
            this.initialExpansion.Velocity = (behavior == null) ? float.NaN : behavior.InitialVelocityX;
        }

        /// <summary>
        /// Given some inertia parameters, get an appropriate behavior object.
        /// If the parameters are null, gets a default.
        /// </summary>
        /// <typeparam name="TBehavior"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static TBehavior GetBehavior<TBehavior>(
            InertiaParameters2D parameters) where TBehavior : InertiaParameters2D, new()
        {
            if (parameters == null)
            {
                return new TBehavior();
            }
            else
            {
                return (TBehavior)parameters;
            }
        }

        /// <summary>
        /// Gets the velocities for the processor.
        /// </summary>
        /// <returns></returns>
        private ManipulationVelocities2D GetVelocities()
        {
            switch (this.processorState)
            {
                case ProcessorState.Running:
                case ProcessorState.Completing:
                    break;

                default:
                    // If we're not running or completing, velocity is zero.
                    return ManipulationVelocities2D.Zero;
            }

            long timeDelta = unchecked(this.previousTimestamp - this.initialTimestamp);
            if (timeDelta < 0)
            {
                // timestamps haven't been initialized yet
                return ManipulationVelocities2D.Zero;
            }

            return new ManipulationVelocities2D(
                this.translationX.GetVelocity(timeDelta),
                this.translationY.GetVelocity(timeDelta),
                this.orientation.GetVelocity(timeDelta),
                this.expansion.GetVelocity(timeDelta));
        }

        /// <summary>
        /// Gets incremental delta since last event.
        /// </summary>
        /// <param name="translationX"></param>
        /// <param name="translationY"></param>
        /// <param name="orientation"></param>
        /// <param name="expansion"></param>
        /// <param name="scaleDelta"></param>
        /// <returns></returns>
        private static ManipulationDelta2D GetIncrementalDelta(
            ExtrapolatedValue translationX,
            ExtrapolatedValue translationY,
            ExtrapolatedValue orientation,
            ExtrapolatedValue expansion,
            double scaleDelta)
        {
            return new ManipulationDelta2D(
                (float)translationX.Delta,
                (float)translationY.Delta,
                (float)orientation.Delta,
                (float)scaleDelta,
                (float)scaleDelta,
                (float)expansion.Delta,
                (float)expansion.Delta);
        }

        /// <summary>
        /// Gets cumulative delta since inertia began.
        /// </summary>
        /// <param name="translationX"></param>
        /// <param name="translationY"></param>
        /// <param name="orientation"></param>
        /// <param name="expansion"></param>
        /// <param name="totalScale"></param>
        /// <returns></returns>
        private static ManipulationDelta2D GetCumulativeDelta(
            ExtrapolatedValue translationX,
            ExtrapolatedValue translationY,
            ExtrapolatedValue orientation,
            ExtrapolatedValue expansion,
            double totalScale)
        {
            return new ManipulationDelta2D(
                (float)translationX.Total,
                (float)translationY.Total,
                (float)orientation.Total,
                (float)totalScale,
                (float)totalScale,
                (float)expansion.Total,
                (float)expansion.Total);
        }

        /// <summary>
        /// Prepares extrapolation.
        /// </summary>
        private void Prepare()
        {
#if DEBUG
            // clear the log
            log.Length = 0;
#endif
            // calculate initial DesiredTranslationXY and initial DesiredDecelerationXY
            if (!double.IsNaN(this.desiredDisplacement))
            {
                Debug.Assert(double.IsNaN(this.desiredDeceleration), "desiredDisplacement and desiredDeceleration are mutually exclusive.");
                VectorD desiredTranslationXY = GetAbsoluteVector(this.desiredDisplacement,
                    new VectorD(this.initialTranslationX.Velocity, this.initialTranslationY.Velocity));
                initialTranslationX.AbsoluteOffset = Math.Abs(desiredTranslationXY.X);
                initialTranslationY.AbsoluteOffset = Math.Abs(desiredTranslationXY.Y);
                initialTranslationX.AbsoluteDeceleration = double.NaN;
                initialTranslationY.AbsoluteDeceleration = double.NaN;
            }

            else if (!double.IsNaN(this.desiredDeceleration))
            {
                Debug.Assert(double.IsNaN(this.desiredDisplacement), "desiredDisplacement and desiredDeceleration are mutually exclusive.");
                VectorD desiredDecelerationXY = GetAbsoluteVector(this.desiredDeceleration,
                    new VectorD(this.initialTranslationX.Velocity, this.initialTranslationY.Velocity));
                initialTranslationX.AbsoluteDeceleration = Math.Abs(desiredDecelerationXY.X);
                initialTranslationY.AbsoluteDeceleration = Math.Abs(desiredDecelerationXY.Y);
                initialTranslationX.AbsoluteOffset = double.NaN;
                
                initialTranslationY.AbsoluteOffset = double.NaN;
            }

            this.translationX = Prepare(this.initialTranslationX, "translationX");
            this.translationY = Prepare(this.initialTranslationY, "translationY");
            this.orientation = Prepare(this.initialOrientation, "orientation");
            this.expansion = Prepare(this.initialExpansion, "expansion");

            // check that at least one value needs to be extrapolated
            if (this.translationX.ExtrapolationResult == ExtrapolationResult.Skip &&
                this.translationY.ExtrapolationResult == ExtrapolationResult.Skip &&
                this.orientation.ExtrapolationResult == ExtrapolationResult.Skip &&
                this.expansion.ExtrapolationResult == ExtrapolationResult.Skip)
            {
                throw Exceptions.NoInertiaVelocitiesSpecified(
                    "TranslationBehavior.InitialVelocityX",
                    "TranslationBehavior.InitialVelocityY",
                    "RotationBehavior.InitialVelocity",
                    "ExpansionBehavior.InitialVelocityX",
                    "ExpansionBehavior.InitialVelocityY");
            }
        }


        /// <summary>
        /// Prepares extrapolation state for the specified dimension.
        /// </summary>
        private ExtrapolationState Prepare(
            InitialState initialState,
            string dimension)
        {
#if DEBUG
            LogLine("PREPARE: " + dimension);
#endif
            ExtrapolationState state = new ExtrapolationState(initialState);

            // check if initial velocity is set, do nothing if not
            if (state.ExtrapolationResult != ExtrapolationResult.Skip)
            {
                Debug.Assert(
                    !double.IsNaN(state.Offset) || !double.IsNaN(state.AbsoluteDeceleration),
                    "Either offset or deceleration should have been set by now");

                // if velocity is 0, no need to extrapolate, the object gets stopped already
                if (DoubleUtil.IsZero(state.InitialVelocity))
                {
                    state.InitialVelocity = 0; // set to explicit 0 to clear out the fractional part
                    state.Duration = 0;
                    state.Offset = 0;
                    state.AbsoluteDeceleration = 0;
                }

                else
                {
                    Debug.Assert(!DoubleUtil.IsZero(state.InitialVelocity));

                    // calculate duration based on offset
                    if (!double.IsNaN(state.Offset))
                    {
                        if (DoubleUtil.IsZero(state.Offset))
                        {
                            state.Offset = 0; // set to explicit 0 to clear out the fractional part
                        }

                        state.Duration = 2 * Math.Abs(state.Offset / state.InitialVelocity);

                        if (DoubleUtil.IsZero(state.Duration))
                        {
                            state.Duration = 0; // set to explicit 0 to clear out the fractional part
                            state.AbsoluteDeceleration = double.PositiveInfinity;
                        }
                        else
                        {
                            state.AbsoluteDeceleration = Math.Abs(state.InitialVelocity) / state.Duration;
                        }
                    }

                    // calculate duration based on desiredDeceleration
                    // Note: the block above may get almost 0 AbsoluteDeceleration, check it here
                    if (DoubleUtil.IsZero(state.AbsoluteDeceleration))
                    {
                        // 0 desiredDeceleration => infinite duration and offset
                        state.AbsoluteDeceleration = 0; // set to explicit 0 to clear out the fractional part
                        state.Duration = double.PositiveInfinity;
                        state.Offset =
                            (state.InitialVelocity > 0 ? double.PositiveInfinity : double.NegativeInfinity);
                    }
                    else if (double.IsNaN(state.Offset))
                    {
                        // not 0-desiredDeceleration, calculate duration and offset
                        state.Duration = Math.Abs(state.InitialVelocity) / state.AbsoluteDeceleration;
                        state.Offset = state.InitialVelocity * state.Duration * 0.5;
                    }

                    Debug.Assert(state.Duration >= 0);
                    Debug.Assert(!double.IsNaN(state.Deceleration));
                    Debug.Assert(!double.IsNaN(state.Offset));
                }
            }

#if DEBUG
            LogLine(state.ToString());
            LogLine("");
            state.AssertValid();
#endif
            return state;
        }

        /// <summary>
        /// Extrapolates and raises an event.
        /// </summary>
        /// <returns>returns false if extrapolation is completed.</returns>
        private bool ExtrapolateAndRaiseEvents(Int64 timestamp, bool forceCompleted)
        {
            Debug.Assert(this.processorState == ProcessorState.Running);

            // use 'unchecked' to avoid overflow exceptions
            Int64 timeDelta = unchecked(timestamp - this.initialTimestamp);
            if (timeDelta < 0)
            {
                // timeDelta can be less than 0 if extrapolation runs too long,
                // just stop it here,
                // Note: we are using 'double' to calculate extrapolation,
                // so we could continue execution but unfortunately
                // inertia processor takes Int64 for the timestamp.
                // For simplicity stop here otherwise we would need to do some tricks
                // to bypass Int64.MaxValue barrier.
                timeDelta = Int64.MaxValue;
                forceCompleted = true;
                Debug.WriteLine("Too long extrapolation, stop it.");
            }

            // extrapolate values
            ExtrapolatedValue extrapolatedTranslationX = GetExtrapolatedValueAndUpdateState(this.translationX, timeDelta);
            ExtrapolatedValue extrapolatedTranslationY = GetExtrapolatedValueAndUpdateState(this.translationY, timeDelta);
            ExtrapolatedValue extrapolatedOrientation = GetExtrapolatedValueAndUpdateState(this.orientation, timeDelta);
            ExtrapolatedValue extrapolatedExpansion = GetExtrapolatedValueAndUpdateState(this.expansion, timeDelta);

            // check if all extrapolations are completed or not
            bool completed = forceCompleted ||
                (extrapolatedTranslationX.ExtrapolationResult != ExtrapolationResult.Continue &&
                extrapolatedTranslationY.ExtrapolationResult != ExtrapolationResult.Continue &&
                extrapolatedOrientation.ExtrapolationResult != ExtrapolationResult.Continue &&
                extrapolatedExpansion.ExtrapolationResult != ExtrapolationResult.Continue);

            if (completed)
            {
                // Need to set state to "completing" before raising the event (otherwise,
                // if a completed handler checked the "is running" property, it would
                // return true, which would be odd), 
                // we set to "completing" and not "completed" to let GetVelocity return
                // the last known velocity value and not 0.
                this.processorState = ProcessorState.Completing;

                // raise Completed event
                EventHandler<Manipulation2DCompletedEventArgs> eventHandler = Completed;
                if (eventHandler != null)
                {
                    // calculate scale
                    double totalScale = this.initialScale;
                    if (this.expansion.ExtrapolationResult != ExtrapolationResult.Skip)
                    {
                        Debug.Assert(this.expansion.InitialValue > 0 &&
                            !double.IsInfinity(this.expansion.InitialValue) &&
                            !double.IsNaN(this.expansion.InitialValue), "Invalid initial expansion value.");
                        totalScale *= extrapolatedExpansion.Value / this.expansion.InitialValue;
                    }

                    ManipulationDelta2D total = GetCumulativeDelta(
                        extrapolatedTranslationX,
                        extrapolatedTranslationY,
                        extrapolatedOrientation,
                        extrapolatedExpansion,
                        totalScale);

                    Manipulation2DCompletedEventArgs args = new Manipulation2DCompletedEventArgs(
                        (float)extrapolatedTranslationX.Value,
                        (float)extrapolatedTranslationY.Value,
                        GetVelocities(),
                        total);
                    eventHandler(this, args);

#if DEBUG
                    LogLine(
                        "Completed event:" +
                        " timeDelta=" + timeDelta +
                        " OriginX=" + args.OriginX +
                        " OriginY=" + args.OriginY +
                        " TotalTranslationX=" + args.Total.TranslationX +
                        " TotalTranslationY=" + args.Total.TranslationY +
                        " TotalRotation=" + args.Total.Rotation +
                        " TotalScale=" + args.Total.ScaleX);
#endif
                }
            }

            else
            {
                // raise Delta event
                EventHandler<Manipulation2DDeltaEventArgs> eventHandler = Delta;
                if (eventHandler != null)
                {
                    // calculate scale
                    double totalScale = this.initialScale;
                    double scaleDelta = 1;
                    if (this.expansion.ExtrapolationResult != ExtrapolationResult.Skip)
                    {
                        Debug.Assert(this.expansion.InitialValue > 0 &&
                            !double.IsInfinity(this.expansion.InitialValue) &&
                            !double.IsNaN(this.expansion.InitialValue), "Invalid initial expansion value.");
                        totalScale *= extrapolatedExpansion.Value / this.expansion.InitialValue;

                        if (!DoubleUtil.IsZero(extrapolatedExpansion.Delta))
                        {
                            double previousExpansionValue = extrapolatedExpansion.Value - extrapolatedExpansion.Delta;
                            Debug.Assert(!DoubleUtil.IsZero(previousExpansionValue));
                            scaleDelta = extrapolatedExpansion.Value / previousExpansionValue;
                        }
                    }

                    ManipulationDelta2D delta = GetIncrementalDelta(
                        extrapolatedTranslationX,
                        extrapolatedTranslationY,
                        extrapolatedOrientation,
                        extrapolatedExpansion,
                        scaleDelta);
                    ManipulationDelta2D total = GetCumulativeDelta(
                        extrapolatedTranslationX,
                        extrapolatedTranslationY,
                        extrapolatedOrientation,
                        extrapolatedExpansion,
                        totalScale);

                    Manipulation2DDeltaEventArgs args = new Manipulation2DDeltaEventArgs(
                        (float)extrapolatedTranslationX.Value,
                        (float)extrapolatedTranslationY.Value,
                        GetVelocities(),
                        delta,
                        total);

                    eventHandler(this, args);

#if DEBUG
                    LogLine(
                        "Delta event:" +
                        " timeDelta=" + timeDelta +
                        " OriginX=" + args.OriginX +
                        " OriginY=" + args.OriginY +
                        " DeltaX=" + args.Delta.TranslationX +
                        " DeltaY=" + args.Delta.TranslationY +
                        " RotationDelta=" + args.Delta.Rotation +
                        " ScaleDelta=" + args.Delta.ScaleX +
                        " CumulativeTranslationX=" + args.Cumulative.TranslationX +
                        " CumulativeTranslationY=" + args.Cumulative.TranslationY +
                        " CumulativeRotation=" + args.Cumulative.Rotation +
                        " CumulativeScale=" + args.Cumulative.ScaleX);
#endif

                }
            }

            return !completed;
        }

        /// <summary>
        /// Gets extrapolated value.
        /// </summary>
        /// <param name="initialValue"></param>
        /// <param name="initialVelocity"></param>
        /// <param name="deceleration"></param>
        /// <param name="timeDelta"></param>
        /// <returns></returns>
        private static double GetExtrapolatedValue(double initialValue, double initialVelocity, double deceleration, double timeDelta)
        {
            Debug.Assert(!double.IsNaN(initialVelocity) && !double.IsInfinity(initialValue));
            Debug.Assert(!double.IsNaN(initialVelocity) && !double.IsInfinity(initialVelocity));
            Debug.Assert(!double.IsNaN(deceleration) && !double.IsInfinity(deceleration));
            Debug.Assert(!double.IsNaN(timeDelta) && !double.IsInfinity(timeDelta) && timeDelta >= 0);

            double result = initialValue + (initialVelocity - deceleration * timeDelta * 0.5) * timeDelta;
            return result;
        }

        /// <summary>
        /// Gets extrapolated value.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timeDelta">in timestamp units (100-nanoseconds)</param>
        /// <returns></returns>
        private static ExtrapolatedValue GetExtrapolatedValueAndUpdateState(ExtrapolationState state, double timeDelta)
        {
            Debug.Assert(!double.IsNaN(timeDelta) && !double.IsInfinity(timeDelta) && timeDelta >= 0);

            if (state.ExtrapolationResult == ExtrapolationResult.Skip)
            {
                return new ExtrapolatedValue(state.InitialValue, 0, 0, ExtrapolationResult.Skip);
            }

            if (state.ExtrapolationResult == ExtrapolationResult.Stop)
            {
                return new ExtrapolatedValue(state.PreviousValue, 0, state.PreviousValue - state.InitialValue, ExtrapolationResult.Stop);
            }

            ExtrapolationResult resultAction = ExtrapolationResult.Continue;
            double resultValue = double.NaN;
            if (timeDelta >= state.Duration)
            {
                resultValue = state.FinalValue;
                timeDelta = state.Duration;
                resultAction = ExtrapolationResult.Stop;
            }

            if (double.IsNaN(resultValue))
            {
                // extrapolate position:
                resultValue = GetExtrapolatedValue(state.InitialValue, state.InitialVelocity, state.Deceleration, timeDelta);

                // make sure that the value is within allowed limits
                double limitedResultValue = state.LimitValue(resultValue);
                if (limitedResultValue != resultValue)
                {
                    resultValue = limitedResultValue;
                    resultAction = ExtrapolationResult.Stop;
                }
            }
            Debug.Assert(!double.IsNaN(resultValue) && !double.IsInfinity(resultValue), "Calculation error, value should be a finite number.");

            ExtrapolatedValue value = new ExtrapolatedValue(resultValue,
                resultValue - state.PreviousValue,
                resultValue - state.InitialValue,
                resultAction);

            // update the state
            state.PreviousValue = resultValue;
            state.ExtrapolationResult = value.ExtrapolationResult;

#if DEBUG
            state.AssertValid();
#endif
            return value;
        }

        /// <summary>
        /// Performs extrapolation.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="forceCompleted"></param>
        /// <returns></returns>
        private bool Process(Int64 timestamp, bool forceCompleted)
        {
            switch (this.processorState)
            {
                case ProcessorState.NotInitialized:
                    // Check our various inertia behaviors to make sure they're in valid states
                    if (this.translationBehavior != null)
                    {
                        this.translationBehavior.CheckValid();
                    }
                    if (this.expansionBehavior != null)
                    {
                        this.expansionBehavior.CheckValid();
                    }
                    if (this.rotationBehavior != null)
                    {
                        this.rotationBehavior.CheckValid();
                    }

                    // verify if initialTimestamp is initialized and set it to the current timestamp if not
                    if (this.previousTimestamp != this.initialTimestamp)
                    {
                        SetInitialTimestamp(timestamp);
                    }

                    // verify parameters and perform initial calculations
                    Prepare();
                    this.processorState = ProcessorState.Running;
                    break;

                case ProcessorState.Running:
                    break;

                case ProcessorState.Completing:
                case ProcessorState.Completed:
                    // do nothing, extrapolation finished
                    return false;

                default:
                    Debug.Assert(false);
                    break;
            }

            // To handle potential wrapping, make sure that 'unchecked' delta between the new value and the previous one
            // is greater than or equal to 0, otherwise there is a chance that user specified a "smaller"
            // timestamp which is wrong.
            if (unchecked(timestamp - this.previousTimestamp) < 0)
            {
                // throw an exception,
                // make sure that the outer method has parameter named "timestamp"
                throw Exceptions.InvalidTimestamp("timestamp", timestamp);
            }

            bool result = ExtrapolateAndRaiseEvents(timestamp, forceCompleted);
            this.previousTimestamp = timestamp;

            if (!result)
            {
                // exrapolation finished, update the state
                this.processorState = ProcessorState.Completed;
            }

            return result;
        }

        /// <summary>
        /// Returns NaN if 'value' is NaN, or 0 - if 'value' is 0 or 'scale' - otherwise.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private static double ScaleValue(double value, double scale)
        {
            Debug.Assert(!double.IsInfinity(value));
            if (double.IsNaN(value))
            {
                return double.NaN;
            }
            if (DoubleUtil.IsZero(value))
            {
                return 0;
            }
            return scale;
        }

        /// <summary>
        /// Returns an absolute vector with a given length along the base vector.
        /// The result of this method is used by InitialState.AbsolureOffset and InitialState.AbsolureDeceleration.
        /// </summary>
        private static VectorD GetAbsoluteVector(double length, VectorD baseVector)
        {
            Debug.Assert(!double.IsNaN(length) && length >= 0 && !double.IsInfinity(length));
            Debug.Assert(!double.IsInfinity(baseVector.X));
            Debug.Assert(!double.IsInfinity(baseVector.Y));

            VectorD result;
            if (!double.IsNaN(baseVector.X) && !double.IsNaN(baseVector.Y) &&
                !DoubleUtil.IsZero(baseVector.X) && !DoubleUtil.IsZero(baseVector.Y))
            {
                double scale = length / baseVector.Length;
                result = new VectorD(Math.Abs(baseVector.X * scale),
                    Math.Abs(baseVector.Y * scale));
            }
            else
            {
                result = new VectorD(ScaleValue(baseVector.X, length), ScaleValue(baseVector.Y, length));
            }

            return result;
        }

        /// <summary>
        /// Checks if the given value is valid.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        private static void CheckOriginalValue(float value, string paramName)
        {
            Validations.CheckFinite(value, paramName);
        }

#if DEBUG
        /// <summary>
        /// Logs diagnostic trace.
        /// </summary>
        /// <param name="msg"></param>
        private void LogLine(string msg)
        {
            if (log.Length < 1000000)
            {
                log.AppendLine(msg);
            }
        }
#endif

        #endregion Private Methods


        #region Private Classes

        /// <summary>
        /// Initial state for a single dimension.
        /// </summary>
        private class InitialState
        {
            // initial velocity, in coordinate units per millisecond
            public double Velocity = double.NaN;

            // initial value
            public double Value;

            // absolute offset
            public double AbsoluteOffset = double.NaN;

            // boundary value (for collision detection)
            public double MinBound = double.NegativeInfinity;
            public double MaxBound = double.PositiveInfinity;

            // absolute desiredDeceleration, in coordinate units per squared millisecond
            public double AbsoluteDeceleration = double.NaN;

            // clones the object
            public InitialState Clone()
            {
                return (InitialState)this.MemberwiseClone();
            }

#if DEBUG
            // dumps the object properties
            public override string ToString()
            {
                string result =
                    "Velocity=" + this.Velocity +
                    ",\nValue=" + this.Value +
                    ",\nAbsoluteOffset=" + this.AbsoluteOffset +
                    ",\nAbsoluteDeceleration=" + this.AbsoluteDeceleration +
                    ",\nMinBound=" + this.MinBound +
                    ",\nMaxBound=" + this.MaxBound;
                return result;
            }
#endif

        }

        /// <summary>
        /// Extrapolation state for a single dimension.
        ///
        /// Input:
        /// - InitialVelocity
        /// - InitialValue
        /// - Offset or Deceleration
        /// Optional:
        /// - Bounds
        ///
        /// The inertia processor will use liner extrapolation and calculate duration of the whole extrapolation,
        /// detect collision with the boundary and calculate CollisionTime and Velocity/Deceleration after collision.
        ///
        /// </summary>
        private class ExtrapolationState
        {
            // initial state of the extrapolation
            private readonly InitialState initialState = new InitialState();

            // initial velocity, in coordinate units per timestamp units (100-nanoseconds)
            public double InitialVelocity = double.NaN;

            // initial value
            public readonly double InitialValue;

            // offset from the initial value
            public double Offset;

            // final value
            public double FinalValue
            {
                get
                {
                    // make sure that the final value is within the bounds
                    return LimitValue(InitialValue + Offset);
                }
            }


            /// <summary>
            /// Limits the given value to allowed bounds.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public double LimitValue(double value)
            {
                return DoubleUtil.Limit(
                    value,
                    this.initialState.MinBound,
                    this.initialState.MaxBound);
            }

            // absolute desiredDeceleration in coordinate units per timestamp squared (100-nanoseconds squared)
            public double AbsoluteDeceleration = double.NaN;

            // desiredDeceleration in coordinate units per timestamp squared (100-nanoseconds squared)
            public double Deceleration
            {
                get
                {
                    return InitialVelocity < 0 ? -AbsoluteDeceleration : AbsoluteDeceleration;
                }
            }

            // total duration in timestamp units (100-nanoseconds)
            public double Duration;

            // indicates whether the extrapolation is completed or not
            public ExtrapolationResult ExtrapolationResult = ExtrapolationResult.Skip;

            // previous value
            public double PreviousValue;

            // constructs extrapolation state
            public ExtrapolationState(InitialState initialState)
            {
                Debug.Assert(initialState != null);
                Debug.Assert(initialState.AbsoluteDeceleration >= 0 || double.IsNaN(initialState.AbsoluteDeceleration));
                Debug.Assert(initialState.AbsoluteOffset >= 0 || double.IsNaN(initialState.AbsoluteOffset));

                // clone the initial state to make sure that if a user modifies it
                // (in InertiaProcessor's event handler), it doesn't affect the extrapolation.
                this.initialState = initialState.Clone();
                this.InitialVelocity = initialState.Velocity * InertiaProcessor2D.millisecondsPerTimestampTick; // convert to timestamp units
                this.InitialValue = initialState.Value;
                this.Offset = initialState.Velocity < 0 ? -initialState.AbsoluteOffset : initialState.AbsoluteOffset;
                this.AbsoluteDeceleration = initialState.AbsoluteDeceleration * InertiaProcessor2D.millisecondsPerTimestampTickSquared; // convert to timestamp units
                this.PreviousValue = this.InitialValue;
                this.ExtrapolationResult = double.IsNaN(this.InitialVelocity) ? ExtrapolationResult.Skip : ExtrapolationResult.Continue;
            }

            /// <summary>
            /// Gets the velocity at the specified point in time.
            /// </summary>
            /// <param name="elapsedTimeSinceInitialTimestamp">Timestamp ticks that have elapsed since the initial timestamp.</param>
            /// <returns></returns>
            public float GetVelocity(long elapsedTimeSinceInitialTimestamp)
            {
                Debug.Assert(elapsedTimeSinceInitialTimestamp >= 0);

                if (ExtrapolationResult != ExtrapolationResult.Continue)
                {
                    // extrapolation for this dimension isn't set
                    return 0;
                }

                double result = InitialVelocity - Deceleration * elapsedTimeSinceInitialTimestamp;

                // convert to milliseconds
                result = result * timestampTicksPerMillisecond;
                Debug.Assert(Validations.IsFinite((float)result));
                return (float)result;
            }

#if DEBUG
            // dumps the object properties
            public override string ToString()
            {
                string result =
                    "initialState:\n{" + this.initialState.ToString() + "}\n" +
                    "\nExtrapolationResult=" + this.ExtrapolationResult +
                    ",\nInitialVelocity=" + this.InitialVelocity +
                    ",\nInitialValue=" + this.InitialValue +
                    ",\nFinalValue=" + this.FinalValue +
                    ",\nDeceleration=" + this.Deceleration +
                    ",\nOffset=" + this.Offset +
                    ",\nDuration=" + this.Duration;
                return result;
            }

            // integrity check
            public void AssertValid()
            {
                if (double.IsNaN(this.InitialVelocity))
                {
                    // ignore this dimension
                    Debug.Assert(this.ExtrapolationResult == ExtrapolationResult.Skip);
                    return;
                }

                Debug.Assert(!double.IsNaN(this.InitialValue) && !double.IsInfinity(this.InitialValue));
                Debug.Assert(!double.IsNaN(this.InitialVelocity) && !double.IsInfinity(this.InitialVelocity));
                Debug.Assert(!double.IsNaN(this.Offset)); // can be infinity
                Debug.Assert(!double.IsNaN(this.AbsoluteDeceleration) && this.AbsoluteDeceleration >= 0 && !double.IsInfinity(this.InitialVelocity));
                Debug.Assert(!double.IsNaN(this.Duration) && this.Duration >= 0); // can be infinity
                Debug.Assert(!double.IsNaN(this.FinalValue)); // can be infinity
            }
#endif
        }

        /// <summary>
        /// Extrapolation result for a single dimension.
        /// </summary>
        private enum ExtrapolationResult
        {
            // ignore value
            Skip,

            // continue extrapolation
            Continue,

            // final value
            Stop
        }

        /// <summary>
        /// Extrapolated value for a single dimension.
        /// </summary>
        private struct ExtrapolatedValue
        {
            // the current value
            public readonly double Value;

            // delta from the previous one
            public readonly double Delta;

            // total delta from the moment when extrapolation started
            public readonly double Total;

            // a flag indicating how to handle this value
            public readonly ExtrapolationResult ExtrapolationResult;

            public ExtrapolatedValue(double value, double delta, double total, ExtrapolationResult result)
            {
                Debug.Assert(!double.IsNaN(value) && !double.IsInfinity(value));
                Debug.Assert(!double.IsNaN(delta) && !double.IsInfinity(delta));
                Debug.Assert(!double.IsNaN(total) && !double.IsInfinity(total));
                this.Value = value;
                this.Delta = delta;
                this.Total = total;
                this.ExtrapolationResult = result;
            }
        }

        /// <summary>
        /// Processor state.
        /// </summary>
        private enum ProcessorState
        {
            /// <summary>
            /// instance created by Initialize() method is not called yet,
            /// attempt to call Process() or Complete() will result in an Exception.
            /// </summary>
            NotInitialized,

            /// <summary>
            /// Initialize() method has been called, user can call Process() or Complete().
            /// </summary>
            Running,

            /// <summary>
            /// Extrapolation is about to complete, this is intermediate state, it's set before raising Completed event,
            /// and will be transitioning to Completed by the end of the Process/Complete method call.
            /// This state is introduced in order to make IsRunning property return 'false' but 
            /// GetVelocity method return non 0 value when Completed event handler gets raised.
            /// </summary>
            Completing,

            /// <summary>
            /// Extrapolation is completed, subsequent calls Process() or Complete() will result in Completed event.
            /// To reset the processor, the user needs to call Initialize().
            /// </summary>
            Completed,
        }

        #endregion Private Classes
    }
}