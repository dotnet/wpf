// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents all states associated with a single "manipulation
    /// sequence," defined as a sequence of events comprising a
    /// manipulation start, deltas, and completed.
    /// </summary>
    /// <remarks>
    /// The ManipulationProcessor2D creates a new instance of this
    /// class each time a manipulation begins, and holds on to
    /// a reference to it for the duration of the manipulation.
    /// Event args emitted from the processor may also hold references
    /// to it as needed.
    /// </remarks>
    internal class ManipulationSequence
    {
        #region Statics

        // a pre-defined zero point
        private static readonly PointF ZeroPoint = new PointF(0, 0);
        // a pre-defined zero vector
        private static readonly VectorF ZeroVector = new VectorF(0, 0);
        // a coefficient that defines the curve of the dampening factor
        private const double singleManipulatorTorqueFactor = 4.0;

        #endregion


        #region Private Fields

#if DEBUG
        // for debugging only to log all manipulation activity
        private StringBuilder log = new StringBuilder();
#endif

        // dictionary to keep track of the manipulators by ID
        private Dictionary<int, ManipulatorState> manipulatorStates;
        // a brief history of manipulators
        private HistoryQueue history = new HistoryQueue();
        private SmoothingQueue smoothing = new SmoothingQueue();

        // the current state of the processor
        private ProcessorState processorState;
        // initial state as the composite of initial manipulators
        private ManipulationState initialManipulationState;

        // current state as the composite of all manipulators
        private ManipulationState currentManipulationState;
        // the total amount of translation since the manipulation began
        private VectorF cumulativeTranslation;
        // the total scale factor since the manipulation began
        private float cumulativeScale;
        // the total expansion since the manipulation began
        private float cumulativeExpansion;
        // the total rotation since the manipulation began
        private float cumulativeRotation;

        // smoothed scale, rotation and expansion
        private float smoothedCumulativeScale;
        private float smoothedCumulativeRotation;
        private float smoothedCumulativeExpansion;

        // average radius for all manipulators excluding ones outside of minimumScaleRotateRadius
        private float averageRadius;

        #endregion


        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public ManipulationSequence()
        {
        }

        #endregion


        #region Public Events

        /// <summary>
        /// Occurs when a new manipulation starts.
        /// </summary>
        public event EventHandler<Manipulation2DStartedEventArgs> Started;

        /// <summary>
        /// Occurs when the manipulation origin changes or when translation, scaling, or rotation occur.
        /// </summary>
        public event EventHandler<Manipulation2DDeltaEventArgs> Delta;

        /// <summary>
        /// Occurs when a manipulation completes.
        /// </summary>
        public event EventHandler<Manipulation2DCompletedEventArgs> Completed;

        #endregion


        #region Public Methods

        /// <summary>
        /// Processes the specified manipulators as a single batch action.
        /// </summary>
        /// <param name="timestamp">The timestamp for the batch, in 100-nanosecond ticks.</param>
        /// <param name="manipulators">The set of manipulators that are currently in scope.</param>
        /// <param name="settings">manipulation settings</param>
        public void ProcessManipulators(
            Int64 timestamp,
            IEnumerable<Manipulator2D> manipulators,
            ISettings settings)
        {
            if (this.processorState != ProcessorState.Waiting)
            {
                // Make sure that the timestamp is advancing during the processing of manipulations.
                if (unchecked(timestamp - this.currentManipulationState.Timestamp) < 0)
                {
                    throw Exceptions.InvalidTimestamp("timestamp", timestamp);
                }
            }
            // NOTE: null and empty are both valid for manipulators.
            OnProcessManipulators(timestamp, manipulators, settings);
        }

        /// <summary>
        /// Forces the current manipulation to complete and raises the Completed event.
        /// </summary>
        /// <param name="timestamp">The timestamp to complete the manipulation, in 100-nanosecond ticks.</param>
        /// <exception cref="ArgumentOutOfRangeException">The timestamp is less than the
        /// previous timestamp for the current manipulation.</exception>
        public void CompleteManipulation(Int64 timestamp)
        {
            if (this.processorState != ProcessorState.Waiting)
            {
                // Make sure that the timestamp is advancing during the processing of manipulations.
                if (unchecked(timestamp - this.currentManipulationState.Timestamp) < 0)
                {
                    throw Exceptions.InvalidTimestamp("timestamp", timestamp);
                }
            }

            OnCompleteManipulation(timestamp);
            if (this.manipulatorStates != null)
            {
                this.manipulatorStates.Clear();
            }
        }

        #endregion Public Methods


        #region Private Methods

        /// <summary>
        /// Indicates whether there is pinned behavior or not, meaning that no translation
        /// is allowed and there is a valid settings.Pivot point.
        /// </summary>
        private static bool IsPinned(ISettings settings)
        {
            return !settings.SupportedManipulations.SupportsAny(Manipulations2D.Translate)
                && (settings.Pivot != null)
                && settings.Pivot.HasPosition;
        }

        /// <summary>
        /// Gets the current rate of translational change along the x coordinate axis per timeslice.
        /// </summary>
        /// <returns>the rate of translational change along the x coordinate axis per timeslice</returns>
        private float GetVelocityX()
        {
            return GetVelocity(this.history, (item) => item.Position.X);
        }

        /// <summary>
        /// Gets the current rate of translational change along the y coordinate axis per timeslice.
        /// </summary>
        /// <returns>the rate of translational change along the y coordinate axis per timeslice</returns>
        private float GetVelocityY()
        {
            return GetVelocity(this.history, (item) => item.Position.Y);
        }

        /// <summary>
        /// Gets the current rate of scale change in coordinate values per timeslice.
        /// </summary>
        /// <returns>the rate of scale change in coordinate values per timeslice</returns>
        private float GetExpansionVelocity()
        {
            return GetVelocity(this.history, (item) => item.Expansion);
        }

        /// <summary>
        /// Gets the current rate of rotational change in radians clockwise per timeslice.
        /// </summary>
        /// <returns>the rate of rotational change in radians clockwise per timeslice</returns>
        private float GetAngularVelocity()
        {
            ManipulationState firstSample = this.history.Count > 0 ? this.history.Peek() : default(ManipulationState);
            return GetVelocity(this.history, (item) => AdjustOrientation(item.Orientation, firstSample.Orientation));
        }

        /// <summary>
        /// Get velocities for the processor.
        /// </summary>
        /// <returns></returns>
        private ManipulationVelocities2D GetVelocities()
        {
            return new ManipulationVelocities2D(
                GetVelocityX,
                GetVelocityY,
                GetAngularVelocity,
                GetExpansionVelocity);
        }

        /// <summary>
        /// Gets the smoothed orientation.
        /// </summary>
        /// <returns></returns>
        private float GetSmoothOrientation()
        {
            if (this.smoothing.Count > 1)
            {
                // no need to adjust orientation here because smoothing queue contains cumulative orientation
                return CalculateMovingAverage(this.smoothing, delegate(ManipulationState item) { return item.Orientation; }, 0.0f);
            }

            // not enough samples to smooth, return cumulativeRotation
            return this.cumulativeRotation;
        }

        /// <summary>
        /// Gets the smoothed orientation.
        /// </summary>
        /// <returns></returns>
        private float GetSmoothExpansion()
        {
            if (this.smoothing.Count > 1)
            {
                return CalculateMovingAverage(this.smoothing, delegate(ManipulationState item) { return item.Expansion; }, 0.0f);
            }

            // not enough samples to smooth, return cumulativeExpansion
            return this.cumulativeExpansion;
        }

        /// <summary>
        /// Calculate smoothed scale.
        /// </summary>
        /// <returns></returns>
        private float GetSmoothScale()
        {
            float result = float.NaN;
            if (this.smoothing.Count > 1)
            {
                // CalculateMovingAverage can generate float.Infinity if we're near the
                // limits, since it adds multiple items together
                result = CalculateMovingAverage(this.smoothing, delegate(ManipulationState item) { return item.Scale; }, 1.0f);
                if (float.IsInfinity(result))
                {
                    result = this.cumulativeScale;
                }
            }
            else
            {
                // not enough samples to smooth, return cumulativeScale
                result = this.cumulativeScale;
            }

            Debug.Assert(!float.IsNaN(result) && !float.IsInfinity(result));
            return result;
        }


        /// <summary>
        /// Creates validated lists of added and removed manipulators and updates exiting
        /// manipulators with changes, as appropriate.
        /// </summary>
        /// <param name="manipulators">the set of manipulators currently in scope (may be empty or null)</param>
        /// <param name="timestamp">the timestamp to be used for the updates</param>
        /// <param name="addedManipulatorList">the resulting list of added manipulators</param>
        /// <param name="removedManipulatorIds">the resulting set of removed manipulator IDs</param>
        /// <param name="currentManipulatorCount">the number of current manipulators</param>
        /// <param name="updatedManipulatorCount">the number of updated manipulators</param>
        private void ExtractAndUpdateManipulators(
            IEnumerable<Manipulator2D> manipulators,
            Int64 timestamp,
            out List<ManipulatorState> addedManipulatorList,
            out HashSet<int> removedManipulatorIds,
            out int currentManipulatorCount,
            out int updatedManipulatorCount)
        {
            updatedManipulatorCount = 0;
            currentManipulatorCount = 0;
            addedManipulatorList = null;
            removedManipulatorIds = ((this.manipulatorStates != null) && (this.manipulatorStates.Count > 0))
                ? new HashSet<int>(this.manipulatorStates.Keys)
                : null;

            if (manipulators != null)
            {
                foreach (Manipulator2D manipulator in manipulators)
                {
                    if (removedManipulatorIds != null)
                    {
                        removedManipulatorIds.Remove(manipulator.Id);
                    }
                    currentManipulatorCount++;

                    ManipulatorState state;
                    if (this.manipulatorStates == null || !this.manipulatorStates.TryGetValue(manipulator.Id, out state) || state == null)
                    {
                        // Not there, so must be added.
                        state = CreateManipulatorState(manipulator);
                        if (addedManipulatorList == null)
                        {
                            addedManipulatorList = new List<ManipulatorState>(20);
                        }
                        addedManipulatorList.Add(state);
                    }
                    else
                    {
                        // Skip the update if position is not changed.
                        if (state.CurrentManipulatorSnapshot.X != manipulator.X ||
                            state.CurrentManipulatorSnapshot.Y != manipulator.Y)
                        {
                            state.CurrentManipulatorSnapshot = manipulator;
#if DEBUG
                            this.log.AppendLine(timestamp.ToString(CultureInfo.InvariantCulture) + "\t" +
                                state.CurrentManipulatorSnapshot.Id + "\tChanged\t" + new PointF(state.CurrentManipulatorSnapshot.X, state.CurrentManipulatorSnapshot.Y));
#endif
                            updatedManipulatorCount++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles processing of manipulators.
        /// </summary>
        /// <param name="timestamp">the timestamp for the calculations</param>
        /// <param name="manipulators">the set of manipulators currently in scope</param>
        /// <param name="settings">manipulation settings</param>
        private void OnProcessManipulators(
            Int64 timestamp,
            IEnumerable<Manipulator2D> manipulators,
            ISettings settings)
        {
#if DEBUG
            // Prevent the log from growing too big if the manipulation continues
            // on and on without completing.
            if (this.log.Length >= 100000)
            {
                this.log.Length = 0;
            }
#endif

            // Within a given batch, there are adds, removes, and updates.
            // All updates are relative to the previous composite position,
            // and therefore must be processed *before* any adds or removes.
            //
            // Approach:
            //     1) Parse out adds, removes, and updates, updating states.
            //     2) Initialize the manipulation if necessary.
            //     3) If there are updated manipulators, process the
            //        composite change.
            //     4) If there are adds or removes, modify the collection
            //        and update the composite position.
            //     5) Raise the appropriate event.

            // Cache the totals for comparison after the work is done.
            VectorF previousTranslation = this.cumulativeTranslation;
            float previousSmoothedScale = this.smoothedCumulativeScale;
            float previousSmoothedExpansion = this.smoothedCumulativeExpansion;
            float previousSmoothedRotation = this.smoothedCumulativeRotation;

            // Step 1: Parse out adds, removes, and updates.
            List<ManipulatorState> addedManipulatorList;
            HashSet<int> removedManipulatorIds;
            int currentManipulatorCount;
            int updatedManipulatorCount;
            ExtractAndUpdateManipulators(
                manipulators,
                timestamp,
                out addedManipulatorList,
                out removedManipulatorIds,
                out currentManipulatorCount,
                out updatedManipulatorCount);

            if (updatedManipulatorCount == 0 &&
                (addedManipulatorList == null || addedManipulatorList.Count == 0) &&
                (removedManipulatorIds == null || removedManipulatorIds.Count == 0))
            {
                if (this.processorState != ProcessorState.Waiting && currentManipulatorCount > 0 && this.history.Count > 0)
                {
                    // update smoothed values and get deltas
                    float smoothedScale;
                    float smoothedExpansion;
                    float smoothedRotation;
                    GetSmoothedDeltas(timestamp, settings, out smoothedScale, out smoothedExpansion, out smoothedRotation);

                    // try to update the history, set deltas to 0 because the manipulators are not changed,
                    // this will be no-op if time delta between the current and previous samples is very small
                    this.history.Enqueue(new ManipulationState(timestamp), true/*stopMark*/);

                    // raise delta event with 0 deltas. This event can be used to read the current velocity which will gradually fade out
                    // to 0 unless real changes happen
                    RaiseEvents(this.cumulativeTranslation, previousSmoothedScale, previousSmoothedExpansion, previousSmoothedRotation);
                }

                // There are no changes to process.
                return;
            }

            // Step 2: Initialize the manipulation if necessary.
            if (addedManipulatorList != null && addedManipulatorList.Count > 0)
            {
                EnsureReadyToProcessManipulators(timestamp);
            }
            else
            {
                Debug.Assert(this.processorState == ProcessorState.Manipulating, "Wrong state.");
            }

            // Step 3: If there are updated manipulators, process the composite change.
            if (updatedManipulatorCount > 0)
            {
                CalculateTransforms(timestamp, settings);
            }

            // Step 4a: If there are adds or removes, modify the collection.
            ProcessAddsAndRemoves(timestamp, addedManipulatorList, removedManipulatorIds);
            // Step 4b: Update the composite position if necessary.
            if ((addedManipulatorList != null && addedManipulatorList.Count > 0) ||
                (removedManipulatorIds != null && removedManipulatorIds.Count > 0))
            {
                if (this.manipulatorStates != null && this.manipulatorStates.Count > 0)
                {
                    // As long as we haven't removed the last manipulator, just update the position.
                    OverwriteManipulationState(
                        GetAveragePoint(),
                        this.currentManipulationState.Scale,
                        this.currentManipulationState.Expansion,
                        this.currentManipulationState.Orientation, timestamp);
                    SetVectorsFromPoint(this.currentManipulationState.Position, settings);
                }
                else
                {
                    // If all manipulators are now gone, leave the final composite position alone.
                    OnCompleteManipulation(timestamp);
                    return; // short-circuit any further events in step 5
                }
            }

            // Step 5: Raise the appropriate event.
            RaiseEvents(previousTranslation, previousSmoothedScale, previousSmoothedExpansion, previousSmoothedRotation);
        }

        /// <summary>
        /// Modifies the manipulator collection and updates the composite position.
        /// </summary>
        /// <param name="timestamp">the timestamp for the changes</param>
        /// <param name="addedManipulatorList">the list of added manipulators</param>
        /// <param name="removedManipulatorIds">the list of removed manipulators</param>
        private void ProcessAddsAndRemoves(Int64 timestamp, List<ManipulatorState> addedManipulatorList, HashSet<int> removedManipulatorIds)
        {
            if (addedManipulatorList != null && addedManipulatorList.Count > 0)
            {
                foreach (ManipulatorState state in addedManipulatorList)
                {
#if DEBUG
                    this.log.AppendLine(timestamp.ToString(CultureInfo.InvariantCulture) + "\t" +
                        state.Id + "\tAdded\t" + new PointF(state.InitialManipulatorSnapshot.X, state.InitialManipulatorSnapshot.Y));
#endif
                    AddManipulator(state);
                }
            }
            if ((removedManipulatorIds != null) && (removedManipulatorIds.Count > 0))
            {
                foreach (int removedId in removedManipulatorIds)
                {
                    RemoveManipulator(removedId);
#if DEBUG
                    this.log.AppendLine(timestamp.ToString(CultureInfo.InvariantCulture) + "\t" +
                        removedId + "\tRemoved");
#endif
                }
            }
        }

        /// <summary>
        /// Raises the appropriate event based on the current state.
        /// </summary>
        /// <param name="previousTranslation">cumulativeTranslation from the previous frame</param>
        /// <param name="previousSmoothedScale">cumulativeScale from the previous frame</param>
        /// <param name="previousSmoothedExpansion">cumulativeExpansion from the previous frame</param>
        /// <param name="previousSmoothedRotation">cumulativeRotation from the previous frame</param>
        private void RaiseEvents(VectorF previousTranslation, float previousSmoothedScale, float previousSmoothedExpansion, float previousSmoothedRotation)
        {
            if (this.processorState == ProcessorState.Waiting)
            {
                // now starting a manipulation
                this.processorState = ProcessorState.Manipulating;
                Debug.Assert(Started != null, "Processor hasn't registered for Started event");
                Started(this, new Manipulation2DStartedEventArgs(
                    this.currentManipulationState.Position.X, this.currentManipulationState.Position.Y));
            }
            else
            {
                 // raise delta event with smoothed values
                Debug.Assert(Delta != null, "Processor hasn't registered for Delta event");
                float scaleDelta = this.smoothedCumulativeScale / previousSmoothedScale;
                float expansionDelta = this.smoothedCumulativeExpansion - previousSmoothedExpansion;

                ManipulationDelta2D delta = new ManipulationDelta2D(
                    this.cumulativeTranslation.X - previousTranslation.X,
                    this.cumulativeTranslation.Y - previousTranslation.Y,
                    this.smoothedCumulativeRotation - previousSmoothedRotation,
                    scaleDelta,
                    scaleDelta,
                    expansionDelta,
                    expansionDelta);

                ManipulationDelta2D cumulative = new ManipulationDelta2D(
                    this.cumulativeTranslation.X,
                    this.cumulativeTranslation.Y,
                    this.smoothedCumulativeRotation,
                    this.smoothedCumulativeScale,
                    this.smoothedCumulativeScale,
                    this.smoothedCumulativeExpansion,
                    this.smoothedCumulativeExpansion);

                Manipulation2DDeltaEventArgs args = new Manipulation2DDeltaEventArgs(
                    this.currentManipulationState.Position.X,
                    this.currentManipulationState.Position.Y,
                    GetVelocities(),
                    delta,
                    cumulative);

                Delta(this, args);
            }
        }


        /// <summary>
        /// Handles completion of manipulations.
        /// </summary>
        private void OnCompleteManipulation(Int64 timestamp)
        {
            if (this.processorState == ProcessorState.Waiting)
            {
                // nothing to complete, manipulation is not started yet
                return;
            }

            // Get the final values.
            PointF lastKnownOrigin = this.currentManipulationState.Position;
            VectorF translate = this.cumulativeTranslation;
            float smoothedScale = this.smoothedCumulativeScale;
            float smoothedExpansion = this.smoothedCumulativeExpansion;
            float smoothedRotate = this.smoothedCumulativeRotation;

            // go to the Waiting state
            this.processorState = ProcessorState.Waiting;

#if DEBUG
            this.log.AppendLine(timestamp.ToString(CultureInfo.InvariantCulture) + "\t" +
                "Manipulation\tCompleted\t" + lastKnownOrigin + "\t" + smoothedScale + "\t" + smoothedExpansion + "\t" + smoothedRotate);
#endif
            // raise the event
            Debug.Assert(Completed != null, "Processor hasn't registered for Completed event");
            ManipulationDelta2D total = new ManipulationDelta2D(
                translate.X,
                translate.Y,
                smoothedRotate,
                smoothedScale,
                smoothedScale,
                smoothedExpansion,
                smoothedExpansion);

            Manipulation2DCompletedEventArgs args = new Manipulation2DCompletedEventArgs(
                lastKnownOrigin.X,
                lastKnownOrigin.Y,
                GetVelocities(),
                total);

            Completed(this, args);
#if DEBUG
            Debug.Assert(this.log.Length > 0); // makes a good breakpoint to read the log
#endif
        }

        /// <summary>
        /// Adds a manipulator to the dictionary if it doesn't exist yet.
        /// </summary>
        /// <param name="initialState">the initial state of the manipulator to add</param>
        private void AddManipulator(ManipulatorState initialState)
        {
            if (this.manipulatorStates == null)
            {
                this.manipulatorStates = new Dictionary<int, ManipulatorState>();
            }

            if (!this.manipulatorStates.ContainsKey(initialState.Id))
            {
                this.manipulatorStates[initialState.Id] = initialState;
            }
        }

        /// <summary>
        /// Removes a manipulator from the dictionary.
        /// </summary>
        /// <param name="manipulatorId">the key of the manipulator to remove</param>
        /// <returns>true if the manipulator was removed, false otherwise</returns>
        private bool RemoveManipulator(int manipulatorId)
        {
            if (this.manipulatorStates != null)
            {
                return this.manipulatorStates.Remove(manipulatorId);
            }
            return false;
        }

        /// <summary>
        /// Calls InitializeManipulationState if the processor is
        /// currently idle.
        /// </summary>
        /// <param name="timestamp">the time of the initialization</param>
        private void EnsureReadyToProcessManipulators(Int64 timestamp)
        {
            // If we were waiting idle, then initialize the state.
            if (this.processorState == ProcessorState.Waiting)
            {
#if DEBUG
                // clear the log
                this.log.Length = 0;
#endif
                // clear the history
                this.history.Clear();
                this.smoothing.Clear();

                // zero-out the tracked states
                InitializeManipulationState(timestamp);
            }
        }

        /// <summary>
        /// Calculates transformations and updates manipulation history,
        /// cumulative transformations, and current manipulation state.
        /// </summary>
        /// <param name="timestamp">the time of the calculation</param>
        /// <param name="settings">manipulation settings</param>
        private void CalculateTransforms(
            Int64 timestamp,
            ISettings settings)
        {
            Debug.Assert(this.processorState == ProcessorState.Manipulating, "Invalid state.");

            // get the average point, it will include all current manipulators
            PointF averagePoint = GetAveragePoint();

            // don't update the vectors from the new average yet, as we need
            // to get deltas from old values

            // calculate translation
            VectorF translation = new VectorF(averagePoint.X - this.currentManipulationState.Position.X, averagePoint.Y - this.currentManipulationState.Position.Y);

            // calculate scale and rotation if necessary
            float rotation = 0;
            float expansion = 0;
            float scaleFactor = 1;

            // clear average radius, if Rotation and/or Scale is enabled and there are manipulators far enough from the manipulation origin
            // then the average radius will be re-calculated
            this.averageRadius = 0;

            if (this.manipulatorStates != null)
            {
                if (this.manipulatorStates.Count > 1 && // rotate and scale require more than one manipulator
                    settings.SupportedManipulations.SupportsAny(Manipulations2D.Rotate | Manipulations2D.Scale))
                {
                    // Rotate and/or scale using more than one manipulator.
                    CalculateMultiManipulatorRotationAndScale(averagePoint, ref rotation, ref scaleFactor, ref expansion, settings);
                }
                else if ((this.manipulatorStates.Count == 1) // try single-manipulator rotation
                    && settings.SupportedManipulations.SupportsAny(Manipulations2D.Rotate)
                    && (settings.Pivot != null)
                    && settings.Pivot.HasPosition)
                {
                    // Rotate around the settings.Pivot point, only dampening if the manipulation is not pinned.
                    rotation = CalculateSingleManipulatorRotation(
                        averagePoint,
                        this.currentManipulationState.Position,
                        settings);
                }
            }

            // remove the translation if not currently supported
            if (!settings.SupportedManipulations.SupportsAny(Manipulations2D.TranslateX))
            {
                translation.X = 0;
            }
            if (!settings.SupportedManipulations.SupportsAny(Manipulations2D.TranslateY))
            {
                translation.Y = 0;
            }

            // update the totals
            this.cumulativeTranslation += translation;
            this.cumulativeScale *= scaleFactor;
            this.cumulativeRotation += rotation;
            this.cumulativeExpansion += expansion;

            // Multiple successive scaling can cause exponential runaway
            this.cumulativeTranslation.X = ForceFinite(this.cumulativeTranslation.X);
            this.cumulativeTranslation.Y = ForceFinite(this.cumulativeTranslation.Y);
            this.cumulativeRotation = ForceFinite(cumulativeRotation);
            this.cumulativeScale = ForceFinite(this.cumulativeScale);
            this.cumulativeScale = ForcePositive(this.cumulativeScale);
            this.cumulativeExpansion = ForceFinite(this.cumulativeExpansion);

            // update the smoothed totals and get smooth deltas
            float smoothedScale;
            float smoothedExpansion;
            float smoothedRotation;
            GetSmoothedDeltas(timestamp, settings, out smoothedScale, out smoothedExpansion, out smoothedRotation);

            // update the history
            if (this.history.Count == 0)
            {
                this.history.Enqueue(new ManipulationState(this.currentManipulationState.Timestamp));
            }

            // enqueue smoothed values
            this.history.Enqueue(new ManipulationState((PointF)translation, smoothedScale, smoothedExpansion, smoothedRotation, timestamp));
                
            // update the current manipulation state
            OverwriteManipulationState(
                averagePoint,
                this.cumulativeScale,
                this.cumulativeExpansion,
                this.cumulativeRotation,
                timestamp);
        }

        private static float ForceFinite(float value)
        {
            Debug.Assert(!double.IsNaN(value));
            if (float.IsInfinity(value))
            {
                return float.IsNegativeInfinity(value) ? float.MinValue : float.MaxValue;
            }
            else
            {
                return value;
            }
        }

        private static float ForcePositive(float value)
        {
            // Const value comes from sdk\inc\crt\float.h 
            const float FLT_EPSILON = 1.192092896e-07F; /* smallest such that 1.0+FLT_EPSILON != 1.0 */

            if (value < FLT_EPSILON)
            {
                return FLT_EPSILON;
            }

            return value;
        }

        /// <summary>
        /// Updates the smoothed totals and get smooth deltas.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="settings"></param>
        /// <param name="smoothedScale"></param>
        /// <param name="smoothedExpansion"></param>
        /// <param name="smoothedRotation"></param>
        private void GetSmoothedDeltas(
            Int64 timestamp,
            ISettings settings,
            out float smoothedScale,
            out float smoothedExpansion,
            out float smoothedRotation)
        {
            // calculate smoothing level, the smaller manipulation radius the more smoothing
            //
            // MaxSmoothingRadius ->        smoothingLevel = 0 (no smoothing)
            // MinimumScaleRotateRadius ->  smoothingLevel = 1 (max smoothing)
            //
            Debug.Assert(this.manipulatorStates != null);

            float minimumScaleRotateRadius = settings.MinimumScaleRotateRadius;
            float maximumSmoothingRadius = 10F * settings.MinimumScaleRotateRadius;

            float smoothingLevel;
            if ((this.manipulatorStates.Count < 2)
                || (this.averageRadius < minimumScaleRotateRadius)
                || (this.averageRadius >= maximumSmoothingRadius))
            {
                // no smoothing if number of manipulators is less than 2 or 
                // manipulation radius is outside of Min and Max limits
                smoothingLevel = 0; 
            }
            else
            {
                smoothingLevel = 1 - (this.averageRadius - minimumScaleRotateRadius) / (maximumSmoothingRadius - minimumScaleRotateRadius);
            }

            // set smoothing level
            this.smoothing.SetSmoothingLevel(smoothingLevel);

            // put the calculated expansion and rotation into smoothing queue
            this.smoothing.Enqueue(new ManipulationState(
                (PointF)this.cumulativeTranslation,
                this.cumulativeScale,
                this.cumulativeExpansion,
                this.cumulativeRotation,
                timestamp));

            // get smoothed rotation and calculate delta
            float previousSmoothedCumulativeRotation = this.smoothedCumulativeRotation;
            this.smoothedCumulativeRotation = GetSmoothOrientation();
            smoothedRotation = this.smoothedCumulativeRotation - previousSmoothedCumulativeRotation;
            Debug.Assert(!float.IsNaN(smoothedRotation) && !float.IsInfinity(smoothedRotation));

            // get smoothed expansion and calculate delta
            float previousSmoothedCumulativeExpansion = this.smoothedCumulativeExpansion;
            this.smoothedCumulativeExpansion = GetSmoothExpansion();
            smoothedExpansion = this.smoothedCumulativeExpansion - previousSmoothedCumulativeExpansion;
            Debug.Assert(!float.IsNaN(smoothedExpansion) && !float.IsInfinity(smoothedExpansion));

            // get smoothed expansion and calculate delta
            float previousSmoothedCumulativeScale = this.smoothedCumulativeScale;
            this.smoothedCumulativeScale = GetSmoothScale();
            smoothedScale = this.smoothedCumulativeScale / previousSmoothedCumulativeScale;
            Debug.Assert(!float.IsNaN(smoothedScale) && !float.IsInfinity(smoothedScale));
        }

        private void CalculateMultiManipulatorRotationAndScale(
            PointF averagePoint,
            ref float rotation,
            ref float scaleFactor,
            ref float expansion,
            ISettings settings)
        {
            double cumulativeAngleDelta = 0;
            int angleDeltaCount = 0;
            double cumulativeOldVectorLength = 0;
            double cumulativeNewVectorLength = 0;
            int expansionCount = 0;
            bool isPinned = IsPinned(settings);
            float minimumScaleRotateRadius = settings.MinimumScaleRotateRadius;
            foreach (KeyValuePair<int, ManipulatorState> pair in this.manipulatorStates)
            {
                VectorF oldVectorFromOrigin = pair.Value.VectorFromManipulationOrigin;
                VectorF newVectorFromOrigin = new PointF(pair.Value.CurrentManipulatorSnapshot.X, pair.Value.CurrentManipulatorSnapshot.Y) - averagePoint;
                VectorF oldVectorFromPivot = pair.Value.VectorFromPivotPoint;
                VectorF newVectorFromPivot = isPinned
                    ? new VectorF(pair.Value.CurrentManipulatorSnapshot.X - settings.Pivot.X, pair.Value.CurrentManipulatorSnapshot.Y - settings.Pivot.Y)
                    : ZeroVector;

                // Don't compute with vectors that are too close to the center,
                // as they subject the calculation to wild fluctuations.
                double oldVectorLength = oldVectorFromOrigin.Length;
                double newVectorLength = newVectorFromOrigin.Length;
                if (oldVectorLength >= minimumScaleRotateRadius &&
                    newVectorLength >= minimumScaleRotateRadius)
                {
                    // always calculate old and new vector length, we need it to update averageRadius
                    cumulativeOldVectorLength += oldVectorLength;
                    cumulativeNewVectorLength += newVectorLength;

                    // Rotation
                    if (settings.SupportedManipulations.SupportsAny(Manipulations2D.Rotate) &&
                        (!isPinned ||
                            (oldVectorFromPivot.Length >= minimumScaleRotateRadius &&
                             newVectorFromPivot.Length >= minimumScaleRotateRadius)))
                    {
                        // For rotation, each of the manipulators will have a change in their angle from
                        // the center point. The average of those changes is the change in rotation.
                        // If the manipulation is pinned, then the center point should be the settings.Pivot point
                        // instead of the manipulation origin.
                        VectorF oldVectorForRotationCalc = isPinned ? oldVectorFromPivot : oldVectorFromOrigin;
                        VectorF newVectorForRotationCalc = isPinned ? newVectorFromPivot : newVectorFromOrigin;

                        // Each of these angles is in radians, counterclockwise from east.
                        // Values range from -Pi to Pi.
                        double oldAngle = Math.Atan2(oldVectorForRotationCalc.Y, oldVectorForRotationCalc.X);
                        double newAngle = Math.Atan2(newVectorForRotationCalc.Y, newVectorForRotationCalc.X);
                        double delta = newAngle - oldAngle;
                        if (delta > Math.PI)
                            delta -= Math.PI * 2.0;
                        if (delta < -Math.PI)
                            delta += Math.PI * 2.0;
                        cumulativeAngleDelta += delta;
                        angleDeltaCount++;
                    }

                    // Scale
                    if (settings.SupportedManipulations.SupportsAny(Manipulations2D.Scale))
                    {
                        expansionCount++;
                    }
                }

                // Save the new vectors into the state for this manipulator.
                pair.Value.VectorFromManipulationOrigin = newVectorFromOrigin;
                pair.Value.VectorFromPivotPoint = newVectorFromPivot;
            }
            // Get average deltas.
            if (angleDeltaCount > 0)
            {
                rotation = (float)(cumulativeAngleDelta / angleDeltaCount);
                this.averageRadius = (float)(cumulativeNewVectorLength / angleDeltaCount);
            }
            if (expansionCount > 0 && cumulativeOldVectorLength > 0)
            {
                scaleFactor = (float)(cumulativeNewVectorLength / cumulativeOldVectorLength);
                expansion = (float)(cumulativeNewVectorLength - cumulativeOldVectorLength) / expansionCount;
                this.averageRadius = (float)(cumulativeNewVectorLength / expansionCount);
            }
        }

        /// <summary>
        /// Calculates the rotation of an implied object by the movement of the
        /// manipulation around a settings.Pivot point.
        /// </summary>
        /// <param name="currentPosition">the new manipulation position</param>
        /// <param name="previousPosition">the old manipulation position</param>
        /// <param name="settings">manipulation settings</param>
        /// <returns>the rotation, in radians</returns>
        private static float CalculateSingleManipulatorRotation(
            PointF currentPosition,
            PointF previousPosition,
            ISettings settings)
        {
            Debug.Assert(settings.Pivot != null, "don't call unless we have a settings.Pivot");
            Debug.Assert(settings.Pivot.HasPosition, "don't call unless there's a settings.Pivot location");
            bool dampen = !IsPinned(settings);
            PointF pivotPoint = new PointF(settings.Pivot.X, settings.Pivot.Y);
            VectorF oldVector = previousPosition - pivotPoint;
            VectorF newVector = currentPosition - pivotPoint;

            float torqueFactor = 1.0f;
            if (dampen && !float.IsNaN(settings.Pivot.Radius))
            {
                torqueFactor = (float)Math.Min(1.0, Math.Pow(oldVector.Length / settings.Pivot.Radius, singleManipulatorTorqueFactor));
            }
            float angle = VectorF.AngleBetween(oldVector, newVector);
            if (float.IsNaN(angle))
            {
                return 0.0f;
            }

            return angle * torqueFactor;
        }

        /// <summary>
        /// Initializes the manipulation state and cumulative values.
        /// </summary>
        /// <param name="timestamp">the time of the initialization</param>
        private void InitializeManipulationState(Int64 timestamp)
        {
            this.initialManipulationState = new ManipulationState(timestamp);
            this.currentManipulationState = this.initialManipulationState;
            this.cumulativeTranslation = new VectorF(0.0f, 0.0f);
            this.cumulativeScale = 1.0f;
            this.cumulativeRotation = 0.0f;
            this.cumulativeExpansion = 0.0f;
            this.smoothedCumulativeRotation = 0.0f;
            this.smoothedCumulativeExpansion = 0.0f;
            this.smoothedCumulativeScale = 1.0f;
            this.averageRadius = 0;

#if DEBUG
            this.log.AppendLine(timestamp.ToString(CultureInfo.InvariantCulture) + "\t" +
                "Manipulation\tInitialized\t" + this.initialManipulationState.Position + "\t" +
                this.initialManipulationState.Expansion + "\t" + this.initialManipulationState.Orientation);
#endif
        }

        /// <summary>
        /// Overwrites the currentManipulationState.
        /// </summary>
        /// <param name="position">the new position</param>
        /// <param name="scale">the new scale</param>
        /// <param name="expansion">the new expansion</param>
        /// <param name="orientation">the new orientation</param>
        /// <param name="timestamp">the time of the new values</param>
        private void OverwriteManipulationState(in PointF position, float scale, float expansion, float orientation, Int64 timestamp)
        {
            this.currentManipulationState = new ManipulationState(position, scale, expansion, orientation, timestamp);
#if DEBUG
            this.log.AppendLine(timestamp.ToString(CultureInfo.InvariantCulture) + "\t" +
                "Manipulation\tUpdated\t" + position + "\t" + scale + "\t" + expansion + "\t" + orientation);
#endif
        }

        /// <summary>
        /// Calculates the average point from all current manipulators.
        /// </summary>
        /// <returns>the average point</returns>
        private PointF GetAveragePoint()
        {
            Debug.Assert(this.manipulatorStates != null && this.manipulatorStates.Count > 0);

            float x = 0;
            float y = 0;
            foreach (KeyValuePair<int, ManipulatorState> pair in this.manipulatorStates)
            {
                x += pair.Value.CurrentManipulatorSnapshot.X;
                y += pair.Value.CurrentManipulatorSnapshot.Y;
            }

            PointF result = new PointF(x / this.manipulatorStates.Count, y / this.manipulatorStates.Count);
            return result;
        }

        /// <summary>
        /// Sets a vector within the state for each manipulator's location from a common origin.
        /// </summary>
        /// <param name="referenceOrigin">the common point of reference</param>
        /// <param name="settings">manipulation settings</param>
        private void SetVectorsFromPoint(in PointF referenceOrigin, ISettings settings)
        {
            Debug.Assert(this.manipulatorStates != null && this.manipulatorStates.Count > 0);

            foreach (KeyValuePair<int, ManipulatorState> pair in this.manipulatorStates)
            {
                pair.Value.VectorFromManipulationOrigin = new PointF(
                    pair.Value.CurrentManipulatorSnapshot.X,
                    pair.Value.CurrentManipulatorSnapshot.Y) - referenceOrigin;
                pair.Value.VectorFromPivotPoint = IsPinned(settings)
                    ? new VectorF(pair.Value.CurrentManipulatorSnapshot.X - settings.Pivot.X, pair.Value.CurrentManipulatorSnapshot.Y - settings.Pivot.Y)
                    : ZeroVector;
            }
        }


        /// <summary>
        /// Calculates the velocity for the specified manipulation. The result is in coordinate units per millisecond. 
        /// </summary>
        /// <param name="queue">collection of samples.</param>
        /// <param name="accessor">delegate to access property value to calculate.</param>
        /// <returns></returns>
        private static float GetVelocity(Queue<ManipulationState> queue, PropertyAccessor accessor)
        {
            float result = CalculateWeightedMovingAverage(queue, accessor);

            // convert to milliseconds
            result = result * ManipulationProcessor2D.TimestampTicksPerMillisecond;
            return result;
        }



        /// <summary>
        /// Calculates the weighted moving average for the specified manipulation.
        /// </summary>
        /// <param name="queue">collection of samples.</param>
        /// <param name="accessor">delegate to access property value to calculate.</param>
        /// <returns>the weighted moving average, could be 0 if the average cannot be determined</returns>
        private static float CalculateWeightedMovingAverage(Queue<ManipulationState> queue, PropertyAccessor accessor)
        {
            Debug.Assert(queue != null);

            // calculate velocity vector through weighted moving average based on translation history
            // WMA = Sum(i * P(i)) / (N * (N+1) / 2), where i=1..N
            //   or the same:
            // WMA = (N * Pn + (N-1) * Pn-1 + ... + 2 * P2 + 1 * P1) / (N * (N+1) / 2)
            int count = queue.Count;
            if (count <= 1)
            {
                // not enough samples
                return 0;
            }

            int i = 0; // ignore the very first sample, we need it only to get delta for the next one
            float wma = 0;
            float tmp = 0;
            Int64 previousTimestamp = 0;
            foreach (ManipulationState item in queue)
            {
                float value = accessor(item);

                if (i > 0)
                {
                    Int64 timeDelta = item.Timestamp - previousTimestamp;
                    Debug.Assert(timeDelta >= ManipulationProcessor2D.TimestampTicksPerMillisecond,
                        "the queue should not contain samples with timeDelta < 1 msec.");

                    // normal velocity vector
                    tmp = (float)i / (float)(timeDelta);
                    wma += tmp * value;
                }

                previousTimestamp = item.Timestamp;
                i++;
            }

            if (i <= 1)
            {
                // not enough non-0 samples
                return 0;
            }

            tmp = 2.0f / (i * (i - 1));
            wma *= tmp;

            return wma;
        }


        /// <summary>
        /// Calculates average.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="accessor"></param>
        /// <param name="defaultValue">Default value when average cannot be calculated. 
        /// E.g. if parameter is Scale, the default value should be 1 and not 0.
        /// Note: CalculateWeightedMovingAverage doesn't have this issue because it's used for velocity calculation. 
        /// Default value for velocity is always 0 even for scale change.
        /// </param>
        /// <returns></returns>
        private static float CalculateMovingAverage(Queue<ManipulationState> queue, PropertyAccessor accessor, float defaultValue)
        {
            Debug.Assert(queue != null);

            // calculate average
            // MA = Sum(P(i)) / N;

            int count = queue.Count;
            if (count < 1)
            {
                // not enough samples
                return defaultValue;
            }

            float sum = 0;
            foreach (ManipulationState item in queue)
            {
                sum += accessor(item);
            }

            return sum / count;
        }


        /// <summary>
        /// Adjusts orientation value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static float AdjustOrientation(float value, float baseValue)
        {
            // adjust value by +-360 to the closest to the given baseValue
            float value2 = value + (float)(2 * Math.PI);
            float value3 = value - (float)(2 * Math.PI);
            float delta = Math.Abs(baseValue - value);
            if (Math.Abs(baseValue - value2) < delta)
            {
                value = value2; // use the adjusted value (=value+360)
            }
            else if (Math.Abs(baseValue - value3) < delta)
            {
                value = value3; // use the adjusted value (=value-360)
            }
            return value;
        }

        /// <summary>
        /// Static helper method to create a ManipulatorState object from a Manipulator2D.
        /// </summary>
        /// <param name="manipulator">the Manipulator2D from which to create the state</param>
        /// <returns>the new ManipulatorState object</returns>
        private static ManipulatorState CreateManipulatorState(Manipulator2D manipulator)
        {
            ManipulatorState state = new ManipulatorState(manipulator.Id);
            state.InitialManipulatorSnapshot = manipulator;
            state.CurrentManipulatorSnapshot = state.InitialManipulatorSnapshot;
            return state;
        }

        #endregion Private Methods


        #region Internal Classes
        /// <summary>
        /// Represents a read-only collection of settings that controls
        /// how manipulations behave.
        /// </summary>
        internal interface ISettings
        {
            /// <summary>
            /// Gets the supported manipulations.
            /// </summary>
            Manipulations2D SupportedManipulations { get; }

            /// <summary>
            /// Gets the pivot (may be null).
            /// </summary>
            ManipulationPivot2D Pivot { get; }

            /// <summary>
            /// Gets the minimum scale/rotate radius.
            /// </summary>
            float MinimumScaleRotateRadius { get; }
        }
        #endregion Internal Classes


        #region Private Classes

        /// <summary>
        /// Internal queue to keep history of transformations to predict the next position.
        /// This is used to calculate velocities.
        /// Note: In comparison with the original Queue, this class pushes out the last item when
        /// the queue reaches the Capacity limit.
        /// </summary>
        private class HistoryQueue : Queue<ManipulationState>
        {
            // length of the history queue
            private const int MaxHistoryLength = 5;
            private const int MaxHistoryDuration = 200;// in msecs
            private const long MaxTimestampDelta = MaxHistoryDuration * ManipulationProcessor2D.TimestampTicksPerMillisecond;
            private long? previousTimestamp;

            // stop mark counter.
            // when this counter reaches MaxHistoryLength, the queue stops accepting records and clears itself.
            // this is used when all manipulators stays in the same place. In that case, the processor still sends Delta events with all deltas set to 0 
            // but non-0 velocity which gets "animated" to 0.
            private int stopMarkCount;

            public HistoryQueue()
                : base(MaxHistoryLength)
            {
            }

            /// <summary>
            /// Enqueues a record.
            /// </summary>
            /// <param name="item"></param>
            public new void Enqueue(ManipulationState item)
            {
                Enqueue(item, false/*stopMark*/);
            }

            /// <summary>
            /// Enqueues a record.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="stopMark"></param>
            public void Enqueue(ManipulationState item, bool stopMark)
            {
                if (this.previousTimestamp != null && item.Timestamp - this.previousTimestamp.Value < ManipulationProcessor2D.TimestampTicksPerMillisecond)
                {
                    // do nothing, the new sample is too close to the previous one
                    return;
                }

                if (stopMark)
                {
                    // increase the stopMarkCounter
                    this.stopMarkCount++;
                    if (this.stopMarkCount > MaxHistoryLength)
                    {
                        // the counter reached the limit,
                        // stop enqueueing records and clear the history
                        Clear();
                        return;
                    }
                }
                else
                {
                    // clear stop mark counter
                    this.stopMarkCount = 0;
                }

                if (Count >= MaxHistoryLength)
                {
                    base.Dequeue();
                }

                // dequeue very old items
                while (Count > 0)
                {
                    ManipulationState oldItem = base.Peek();
                    Int64 timestampDelta = item.Timestamp - oldItem.Timestamp;
                    if (timestampDelta <= MaxTimestampDelta)
                    {
                        break;
                    }
                    base.Dequeue();
                }

                base.Enqueue(item);
                previousTimestamp = item.Timestamp;
            }

            /// <summary>
            /// Clears the queue.
            /// </summary>
            public new void Clear()
            {
                base.Clear();
                previousTimestamp = null;
            }
        }


        /// <summary>
        /// Queue to smooth transformations.
        /// </summary>
        private class SmoothingQueue : Queue<ManipulationState>
        {
            // length of the  queue
            private const int MaxHistoryLength = 9; // the longer the smoother but less responsive
            private const int MaxHistoryDuration = 200;// in msecs

            private int historyLength;
            private Int64 maxTimestampDelta;
            private ManipulationState lastItem;

            /// <summary>
            /// Smoothing level between 0 and 1.
            /// 0 - no smoothing, 1 - max smoothing.
            /// </summary>
            public void SetSmoothingLevel(double smoothingLevel)
            {
                // adjust level to 0..1 interval
                smoothingLevel = Math.Max(0, Math.Min(1, smoothingLevel));

                // calculate history length and duration
                int newHistoryLength = (int)Math.Round(smoothingLevel * MaxHistoryLength);
                if (newHistoryLength != this.historyLength)
                {
                    this.historyLength = newHistoryLength;
                    this.maxTimestampDelta = (Int64)(smoothingLevel * MaxHistoryDuration * ManipulationProcessor2D.TimestampTicksPerMillisecond);

                    if (this.historyLength <= 1)
                    {
                        // if requested history is too short, do not do any smoothing and clear the queue
                        Clear();
                    }
                    else
                    {
                        // trim the queue
                        while (Count > this.historyLength)
                        {
                            Dequeue();
                        }
                    }
                }
            }

            /// <summary>
            /// Fills the whole queue with the given record.
            /// </summary>
            /// <param name="item"></param>
            /// <param name="timestamp"></param>
            private void Fill(ManipulationState item, Int64 timestamp)
            {
                // rough frame duration 15 msecs, convert it to timestamp units,
                // use it to create artifical samples in the past
                Int64 frameTimestampDelta = 15 * ManipulationProcessor2D.TimestampTicksPerMillisecond;

                for (int i = -this.historyLength + 1; i < 0; i++)
                {
                    // add items with some timestamp 
                    item.Timestamp = (timestamp + frameTimestampDelta * i);
                    base.Enqueue(item);
                }
            }

            /// <summary>
            /// Enqueues an item and prepend extra samples to make value approximation smoother.
            /// </summary>
            public new void Enqueue(ManipulationState item)
            {
                // check history length, do nothing if history length is too short
                if (this.historyLength > 1)
                {
                    Int64 timestamp = item.Timestamp;

                    if (Count == 0)
                    {
                        // queue is empty,
                        // fill the queue with zeros, it will make value approximation smoother
                        Fill(this.lastItem, timestamp);
                    }
                    else if (timestamp - this.lastItem.Timestamp > this.maxTimestampDelta)
                    {
                        // the last value is very old,
                        // clear the whole queue and fill it with the last value
                        base.Clear();
                        Fill(this.lastItem, timestamp);
                    }
                    else
                    {
                        // make sure that there is a place in the queue
                        if (Count >= this.historyLength)
                        {
                            // dequeue the last item
                            base.Dequeue();
                        }

                        // dequeue very old items
                        while (Count > 0)
                        {
                            ManipulationState old = base.Peek();
                            Int64 timestampDelta = timestamp - old.Timestamp;
                            if (timestampDelta <= this.maxTimestampDelta)
                            {
                                break;
                            }

                            base.Dequeue();
                        }
                    }

                    // add a new item
                    base.Enqueue(item);
                }

                // remember the new last item
                this.lastItem = item;
            }

            public new void Clear()
            {
                base.Clear();
                this.lastItem = new ManipulationState(0L);
            }
        }

        /// <summary>
        /// State of a manipulator.
        /// </summary>
        private class ManipulatorState
        {
            private readonly int manipulatorId;
            private Manipulator2D initialManipulatorSnapshot;
            private Manipulator2D currentManipulatorSnapshot;
            private VectorF vectorFromManipulationOrigin;
            private VectorF vectorFromPivotPoint;

            /// <summary>
            /// Construct ManipulatorState.
            /// </summary>
            /// <param name="manipulatorId"></param>
            public ManipulatorState(int manipulatorId)
            {
                this.manipulatorId = manipulatorId;
            }

            /// <summary>
            /// Gets Id.
            /// </summary>
            public int Id
            {
                get
                {
                    return this.manipulatorId;
                }
            }

            /// <summary>
            /// Gets or sets initial Manipulator2D.
            /// </summary>
            public Manipulator2D InitialManipulatorSnapshot
            {
                get
                {
                    return this.initialManipulatorSnapshot;
                }
                set
                {
                    this.initialManipulatorSnapshot = value;
                }
            }

            /// <summary>
            /// Gets or sets the current manipulator.
            /// </summary>
            public Manipulator2D CurrentManipulatorSnapshot
            {
                get
                {
                    return this.currentManipulatorSnapshot;
                }
                set
                {
                    this.currentManipulatorSnapshot = value;
                }
            }

            /// <summary>
            /// Gets or sets a vector from the manipulation origin.
            /// </summary>
            public VectorF VectorFromManipulationOrigin
            {
                get
                {
                    return this.vectorFromManipulationOrigin;
                }
                set
                {
                    this.vectorFromManipulationOrigin = value;
                }
            }

            /// <summary>
            /// Gets or sets a vector from the settings.Pivot point.
            /// </summary>
            public VectorF VectorFromPivotPoint
            {
                get
                {
                    return this.vectorFromPivotPoint;
                }
                set
                {
                    this.vectorFromPivotPoint = value;
                }
            }
        }

        /// <summary>
        /// Internal struct used to track the state of the composite manipulation.
        /// </summary>
        private struct ManipulationState
        {
            public readonly PointF Position;
            public readonly float Scale;
            public readonly float Expansion;
            public readonly float Orientation;
            public Int64 Timestamp;

            public ManipulationState(in PointF position, float scale, float expansion, float orientation, Int64 timestamp)
            {
                Debug.Assert(!float.IsNaN(position.X) && !float.IsNaN(position.Y));
                Debug.Assert(!float.IsInfinity(position.Y) && !float.IsInfinity(position.Y));
                Debug.Assert(!float.IsNaN(scale) && !float.IsInfinity(scale) && scale > 0);
                Debug.Assert(!float.IsNaN(expansion) && !float.IsInfinity(expansion));
                Debug.Assert(!float.IsNaN(orientation) && !float.IsInfinity(orientation));

                Position = position;
                Scale = scale;
                Expansion = expansion;
                Orientation = orientation;
                Timestamp = timestamp;
            }

            public ManipulationState(Int64 timestamp) :
                this(ZeroPoint, 1.0f, 0.0f, 0.0f, timestamp)
            {
            }
        }

        /// <summary>
        /// State of the processor.
        /// </summary>
        private enum ProcessorState
        {
            Waiting,                // waiting for an input
            Manipulating,           // manipulation is in progress
        }

        /// <summary>
        /// Delegate with a ManipulationState parameter. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private delegate float PropertyAccessor(ManipulationState item);

        #endregion Private Classes

    }
}
