// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Implements a multiple-input, single-output compositor
    /// for two-dimensional (2-D) transformations in a shared coordinate space.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <strong>ManipulationProcessor2D</strong> object treats a collection of 
    /// manipulators as a composite object. Your application is then freed from
    /// the necessity of tracking and managing individual manipulators. 
    /// </para>
    /// <para>
    /// A manipulation processor by itself does not cause an element to move. Your 
    /// application begins a manipulation as necessary, and then receives information from 
    /// a manipulation processor by listening to the
    /// <strong><see cref="Started"/></strong>,
    /// <strong><see cref="Delta"/></strong>
    /// and
    /// <strong><see cref="Completed"/></strong>
    /// events. The values received via these events enable you to change the location, size or
    /// orientation of an element as needed.
    /// </para>
    /// <para>
    /// You inform a manipulation processor which types of manipulations are allowed 
    /// (translate, scale, rotate) by setting the
    /// <strong><see cref="SupportedManipulations"/></strong> property. You can then
    /// provide non-conditional logic to the transformation of the element
    /// that is being manipulated. For instance, instead of checking if rotation is enabled before
    /// changing the orientation of an element, you can unconditionally apply the rotation factor
    /// received from the manipulation processor; if rotation is not enabled, the manipulation
    /// processor will report that no rotational change has occurred.
    /// </para>
    /// <para>
    /// When an element that is being manipulated is released (all manipulators are removed), you
    /// can use inertia processing to simulate friction and cause the element to gradually slow
    /// its movements before coming to a stop. For more information see the
    /// <strong><see cref="System.Windows.Input.Manipulations.InertiaProcessor2D"/></strong>
    /// class.
    /// </para>
    /// </remarks>
    public class ManipulationProcessor2D : ManipulationSequence.ISettings
    {
        #region Statics

        // a coefficient that defines the curve of the dampening factor
        private const double singleManipulatorTorqueFactor = 4.0;

        /// <summary>
        /// The number of timestamp ticks in one millisecond.
        /// </summary>
        /// <remarks>
        /// Timestamp ticks for the manipulation and inertia processors
        /// are defined as 100 nanoseconds.
        /// </remarks>
        internal const long TimestampTicksPerMillisecond = 10000;

        #endregion


        #region Private Fields

        // minimum distance from origin for manipulator to contribute to calculations
        private float minimumScaleRotateRadius = 20f;
        // the manipulations that are supported
        private Manipulations2D supportedManipulations;
        // pivot information
        private ManipulationPivot2D pivot;

        private ManipulationSequence currentManipulation;

        #endregion


        #region Constructors

        /// <summary>
        /// Creates a new 
        /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D"></see></strong>
        /// object.
        /// </summary>
        /// <param name="supportedManipulations">The initial set of supported manipulations.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <em>supportedManipulations</em> parameter is
        /// not a valid combination of the 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulations2D"></see></strong>
        /// enumeration values.</exception>
        public ManipulationProcessor2D(Manipulations2D supportedManipulations)
            : this(supportedManipulations, null)
        {
        }

        /// <summary>
        /// Creates a new 
        /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D"></see></strong>
        /// object.
        /// </summary>
        /// <param name="supportedManipulations">The initial set of supported manipulations.</param>
        /// <param name="pivot">Pivot information for single-manipulator rotations.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <em>supportedManipulations</em> parameter is
        /// not a valid combination of the 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulations2D"></see></strong>
        /// enumeration values.</exception>
        public ManipulationProcessor2D(
            Manipulations2D supportedManipulations,
            ManipulationPivot2D pivot)
        {
            supportedManipulations.CheckValue("supportedManipulations");
            this.supportedManipulations = supportedManipulations;
            this.pivot = pivot;
        }

        #endregion


        #region Public Properties


        /// <summary>
        /// Gets or sets the minimum radius, in coordinate units, necessary
        /// for a manipulator to participate in scaling and rotation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If a manipulator is extremely close to the center of mass of the
        /// manipulators currently being processed, a very small manipulator
        /// motion can become a very large change to rotation or scaling. To
        /// avoid this problem, set <strong>MinimumScaleRotateRadius</strong> 
        /// to something greater than zero. Any manipulator that is closer than that distance to the
        /// center of mass will not be included in rotation and scaling operations.
        /// </para>
        /// <para>
        /// A typical value to use should be based on the likely magnitude of
        /// "accidental" motions of the manipulators.  For example, if the manipulator
        /// is a human finger touching a screen, a radius corresponding to a centimeter
        /// or so might be appropriate.
        /// </para>
        /// <para>
        /// The value for <strong>MinimumScaleRotateRadius</strong> must be a finite,
        /// non-negative number.
        /// </para>
        /// </remarks>
        public float MinimumScaleRotateRadius
        {
            get { return this.minimumScaleRotateRadius; }
            set
            {
                Validations.CheckFiniteNonNegative(value, "MinimumScaleRotateRadius");
                this.minimumScaleRotateRadius = value;
            }
        }

        /// <summary>
        /// Gets or sets the pivot information for the manipulation processor.
        /// </summary>
        /// <remarks>
        /// The <strong>Pivot</strong> property is used to provide pivot information
        /// for single-manipulator rotations. Setting this property to <strong>null</strong>
        /// disables single-manipulator rotations.
        /// </remarks>
        public ManipulationPivot2D Pivot
        {
            get
            {
                return this.pivot;
            }
            set
            {
                this.pivot = value;
            }
        }

        /// <summary>
        /// Gets or sets the current set of supported manipulations.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The property tries to set a value
        /// that is not a valid combination of the 
        /// <strong><see cref="System.Windows.Input.Manipulations.Manipulations2D"></see></strong>
        /// enumeration values.</exception>
        public Manipulations2D SupportedManipulations
        {
            get
            {
                return this.supportedManipulations;
            }
            set
            {
                value.CheckValue("SupportedManipulations");
                this.supportedManipulations = value;
            }
        }

        #endregion


        #region Public Events

        /// <summary>
        /// Occurs when a new manipulation has started.
        /// </summary>
        /// <example>
        /// <para>
        /// In the following example, an event handler for the <strong>Started</strong>
        /// event checks to see if inertia processing is running and if so, stops it.
        /// </para>
        /// <code lang="cs">
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnManipulationStarted"/>
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="Timestamp"/>
        /// </code>
        /// </example>
        public event EventHandler<Manipulation2DStartedEventArgs> Started;

        /// <summary>
        /// Occurs when the manipulation origin has changed or when translation, scaling, or rotation have occurred.
        /// </summary>
        /// <remarks>
        /// The <strong>ManipulationProcessor2D.Delta</strong> event and the
        /// <strong><see cref="E:System.Windows.Input.Manipulations.InertiaProcessor2D.Delta">InertiaProcessor2D.Delta</see></strong>
        /// event are the same type. Typically, you can use the same event handler
        /// for both events.
        /// </remarks>
        /// <example>
        /// <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnManipulationOrInertiaDelta" lang="cs"/>
        /// </example>
        public event EventHandler<Manipulation2DDeltaEventArgs> Delta;

        /// <summary>
        /// Occurs when a manipulation has competed.
        /// </summary>
        public event EventHandler<Manipulation2DCompletedEventArgs> Completed;

        #endregion


        #region Public Methods

        /// <summary>
        /// Processes the specified manipulators as a single batch action.
        /// </summary>
        /// <param name="timestamp">The timestamp for the batch, in 100-nanosecond ticks.</param>
        /// <param name="manipulators">The set of manipulators that are currently in scope.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The timestamp is less than the previous timestamp for the current manipulation.
        /// </exception>
        /// <remarks>
        /// The parameter <em>manipulators</em> may be an empty list or <strong>null</strong>.
        /// If this results in the number of manipulators reaching zero, the 
        /// <strong><see cref="Completed"/></strong> event is raised.
        /// </remarks>
        /// <example>
        /// <para>
        /// In the following example, the 
        /// <strong><see cref="M:System.Windows.UIElement.OnLostMouseCapture">OnLostMouseCapture</see></strong>
        /// method of a 
        /// <strong><see cref="T:System.Windows.UIElement"/></strong>
        /// object is overridden to call the <strong>ProcessManipulators</strong>
        /// method with the list of 
        /// <strong><see cref="T:System.Windows.Input.Manipulations.Manipulator2D"/></strong>
        /// objects set to <strong>null</strong>.
        /// </para>
        /// <code lang="cs">
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnLostMouseCapture"/>
        ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="Timestamp"/>
        /// </code>
        /// </example>
        public void ProcessManipulators(Int64 timestamp, IEnumerable<Manipulator2D> manipulators)
        {
            ManipulationSequence manipulation = this.currentManipulation;
            if (manipulation == null)
            {
                manipulation = new ManipulationSequence();
                manipulation.Started += OnManipulationStarted;
            }

            manipulation.ProcessManipulators(
                timestamp,
                manipulators,
                this);
        }

        /// <summary>
        /// Forces the current manipulation to complete and raises the 
        /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D.Completed"></see></strong>
        /// event.
        /// </summary>
        /// <param name="timestamp">The timestamp to complete the manipulation, in 100-nanosecond ticks.</param>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp is less than the
        /// previous timestamp for the current manipulation.</exception>
        public void CompleteManipulation(Int64 timestamp)
        {
            if (this.currentManipulation != null)
            {
                this.currentManipulation.CompleteManipulation(timestamp);
                this.currentManipulation = null;
            }
        }

        /// <summary>
        /// Set parameters on the manipulation processor.
        /// </summary>
        /// <param name="parameters">Parameters to set.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification="The parameter gets verified."), 
        EditorBrowsable(EditorBrowsableState.Never)]
        public void SetParameters(ManipulationParameters2D parameters)
        {
            Validations.CheckNotNull(parameters, "parameters");
            parameters.Set(this);
        }

        #endregion Public Methods


        #region Private Methods
        /// <summary>
        /// Here when a manipulation starts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnManipulationStarted(object sender, Manipulation2DStartedEventArgs args)
        {
            // A new manipulation has begun, so make it the current one.
            Debug.Assert(sender != null);
            Debug.Assert(this.currentManipulation == null, "Manipulation was already in progress");
            this.currentManipulation = (ManipulationSequence)sender;

            // We need to register for the Delta and Completed events on the manipulation,
            // since we've deferred doing so until this point.
            this.currentManipulation.Delta += OnManipulationDelta;
            this.currentManipulation.Completed += OnManipulationCompleted;

            // Fire the Started event on the manipulation processor.
            if (Started != null)
            {
                Started(this, args);
            }
        }

        /// <summary>
        /// Here when a manipulation fires a delta event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnManipulationDelta(object sender, Manipulation2DDeltaEventArgs args)
        {
            Debug.Assert(object.ReferenceEquals(sender, this.currentManipulation));
            if (Delta != null)
            {
                Delta(this, args);
            }
        }

        /// <summary>
        /// Here when a manipulation completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnManipulationCompleted(object sender, Manipulation2DCompletedEventArgs args)
        {
            Debug.Assert(object.ReferenceEquals(sender, this.currentManipulation));

            // We're done with the current manipulation.
            this.currentManipulation.Started -= OnManipulationStarted;
            this.currentManipulation.Delta -= OnManipulationDelta;
            this.currentManipulation.Completed -= OnManipulationCompleted;
            this.currentManipulation = null;

            // Fire the Completed event on the manipulation processor.
            if (Completed != null)
            {
                Completed(this, args);
            }
        }
        #endregion Private Methods
    }
}