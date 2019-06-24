// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Input
{
    /// <summary>
    ///     Handles detection of manipulations.
    /// </summary>
    internal sealed class ManipulationLogic
    {
        /// <summary>
        ///     Instantiates an instance of this class.
        /// </summary>
        internal ManipulationLogic(ManipulationDevice manipulationDevice)
        {
            _manipulationDevice = manipulationDevice;
        }

        /// <summary>
        ///     Hooked up to the manipulation processor and inertia processor's started event.
        /// </summary>
        private void OnManipulationStarted(object sender, Manipulation2DStartedEventArgs e)
        {
            PushEvent(new ManipulationStartedEventArgs(
                _manipulationDevice, 
                LastTimestamp, 
                _currentContainer, 
                new Point(e.OriginX, e.OriginY)));
        }

        /// <summary>
        ///     Hooked up to the manipulation processor and inertia processor's delta event.
        /// </summary>
        private void OnManipulationDelta(object sender, Manipulation2DDeltaEventArgs e)
        {
            var deltaArguments = new ManipulationDeltaEventArgs(
                _manipulationDevice,
                LastTimestamp,
                _currentContainer,
                new Point(e.OriginX, e.OriginY),
                ConvertDelta(e.Delta, null),
                ConvertDelta(e.Cumulative, _lastManipulationBeforeInertia),
                ConvertVelocities(e.Velocities),
                IsInertiaActive);

            PushEvent(deltaArguments);
        }

        /// <summary>
        ///     Hooked up to the manipulation processor's completed event.
        /// </summary>
        private void OnManipulationCompleted(object sender, Manipulation2DCompletedEventArgs e)
        {
            // Manipulation portion completed.

            if (_manualComplete && !_manualCompleteWithInertia)
            {
                // This is the last event in the sequence.

                ManipulationCompletedEventArgs completedArguments = ConvertCompletedArguments(e);
                RaiseManipulationCompleted(completedArguments);
            }
            else
            {
                // This event will configure inertia, which will start after this event.

                _lastManipulationBeforeInertia = ConvertDelta(e.Total, null);

                ManipulationInertiaStartingEventArgs inertiaArguments = new ManipulationInertiaStartingEventArgs(
                    _manipulationDevice,
                    LastTimestamp,
                    _currentContainer,
                    new Point(e.OriginX, e.OriginY),
                    ConvertVelocities(e.Velocities),
                    false);

                PushEvent(inertiaArguments);
            }

            _manipulationProcessor = null;
        }

        /// <summary>
        ///     Hooked up to the inertia processor's completed event.
        /// </summary>
        private void OnInertiaCompleted(object sender, Manipulation2DCompletedEventArgs e)
        {
            // Inertia portion completed.

            ClearTimer();

            if (_manualComplete && _manualCompleteWithInertia)
            {
                // Another inertia portion was requested

                _lastManipulationBeforeInertia = ConvertDelta(e.Total, _lastManipulationBeforeInertia);

                ManipulationInertiaStartingEventArgs inertiaArguments = new ManipulationInertiaStartingEventArgs(
                    _manipulationDevice,
                    LastTimestamp,
                    _currentContainer,
                    new Point(e.OriginX, e.OriginY),
                    ConvertVelocities(e.Velocities),
                    true);

                PushEvent(inertiaArguments);
            }
            else
            {
                // This is the last event in the sequence.

                ManipulationCompletedEventArgs completedArguments = ConvertCompletedArguments(e);

                RaiseManipulationCompleted(completedArguments);
            }

            _inertiaProcessor = null;
        }

        private void RaiseManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            PushEvent(e);
        }

        /// <summary>
        ///     Called after a Completed event has been processed.
        /// </summary>
        internal void OnCompleted()
        {
            _lastManipulationBeforeInertia = null;
            SetContainer(null);
        }

        /// <summary>
        ///     Converts an Affine2DOperationCompletedEventArgs object into a ManipulationCompletedEventArgs object.
        /// </summary>
        private ManipulationCompletedEventArgs ConvertCompletedArguments(Manipulation2DCompletedEventArgs e)
        {
            return new ManipulationCompletedEventArgs(
                _manipulationDevice,
                LastTimestamp,
                _currentContainer,
                new Point(e.OriginX, e.OriginY),
                ConvertDelta(e.Total, _lastManipulationBeforeInertia),
                ConvertVelocities(e.Velocities),
                IsInertiaActive);
        }

        private static ManipulationDelta ConvertDelta(ManipulationDelta2D delta, ManipulationDelta add)
        {
            if (add != null)
            {
                return new ManipulationDelta(
                    new Vector(delta.TranslationX + add.Translation.X, delta.TranslationY + add.Translation.Y),
                    AngleUtil.RadiansToDegrees(delta.Rotation) + add.Rotation,
                    new Vector(delta.ScaleX * add.Scale.X, delta.ScaleY * add.Scale.Y),
                    new Vector(delta.ExpansionX + add.Expansion.X, delta.ExpansionY + add.Expansion.Y));
            }
            else
            {
                return new ManipulationDelta(
                    new Vector(delta.TranslationX, delta.TranslationY),
                    AngleUtil.RadiansToDegrees(delta.Rotation),
                    new Vector(delta.ScaleX, delta.ScaleY),
                    new Vector(delta.ExpansionX, delta.ExpansionY));
            }
        }

        private static ManipulationVelocities ConvertVelocities(ManipulationVelocities2D velocities)
        {
            return new ManipulationVelocities(
                new Vector(velocities.LinearVelocityX, velocities.LinearVelocityY),
                AngleUtil.RadiansToDegrees(velocities.AngularVelocity),
                new Vector(velocities.ExpansionVelocityX, velocities.ExpansionVelocityY));
        }

        /// <summary>
        ///     Completes any pending manipulation or inerita processing.
        /// </summary>
        /// <param name="withInertia">
        ///     If a manipulation is active, specifies whether to continue
        ///     to an inertia phase (true) or simply end the sequence (true).
        /// </param>
        internal void Complete(bool withInertia)
        {
            try
            {
                _manualComplete = true;
                _manualCompleteWithInertia = withInertia;

                if (IsManipulationActive)
                {
                    _manipulationProcessor.CompleteManipulation(GetCurrentTimestamp());
                }
                else if (IsInertiaActive)
                {
                    _inertiaProcessor.Complete(GetCurrentTimestamp());
                }
            }
            finally
            {
                _manualComplete = false;
                _manualCompleteWithInertia = false;
            }
        }

        /// <summary>
        ///     Gets ManipulationCompletedEventArgs object out of ManipulationInertiaStartingEventArgs
        /// </summary>
        private ManipulationCompletedEventArgs GetManipulationCompletedArguments(ManipulationInertiaStartingEventArgs e)
        {
            Debug.Assert(_lastManipulationBeforeInertia != null);
            return new ManipulationCompletedEventArgs(
                _manipulationDevice,
                LastTimestamp,
                _currentContainer,
                new Point(e.ManipulationOrigin.X, e.ManipulationOrigin.Y),
                _lastManipulationBeforeInertia,
                e.InitialVelocities,
                IsInertiaActive);
        }

        /// <summary>
        ///     Starts the inertia phase based on the results of a ManipulationInertiaStarting event.
        /// </summary>
        internal void BeginInertia(ManipulationInertiaStartingEventArgs e)
        {
            if (e.CanBeginInertia())
            {
                _inertiaProcessor = new InertiaProcessor2D();
                _inertiaProcessor.Delta += OnManipulationDelta;
                _inertiaProcessor.Completed += OnInertiaCompleted;

                e.ApplyParameters(_inertiaProcessor);

                // Setup a timer to tick the inertia to completion
                _inertiaTimer = new DispatcherTimer();
                _inertiaTimer.Interval = TimeSpan.FromMilliseconds(15);
                _inertiaTimer.Tick += new EventHandler(OnInertiaTick);
                _inertiaTimer.Start();
            }
            else
            {
                // This is the last event in the sequence.
                ManipulationCompletedEventArgs completedArguments = GetManipulationCompletedArguments(e);
                RaiseManipulationCompleted(completedArguments);
                PushEventsToDevice();
            }
        }

        internal static Int64 GetCurrentTimestamp()
        {
            // Does QueryPerformanceCounter to get the current time in 100ns units
            return MediaContext.CurrentTicks;
        }

        private void OnInertiaTick(object sender, EventArgs e)
        {
            // Tick the inertia
            if (IsInertiaActive)
            {
                if (!_inertiaProcessor.Process(GetCurrentTimestamp()))
                {
                    ClearTimer();
                }

                PushEventsToDevice();
            }
            else
            {
                ClearTimer();
            }
        }

        private void ClearTimer()
        {
            if (_inertiaTimer != null)
            {
                _inertiaTimer.Stop();
                _inertiaTimer = null;
            }
        }

        /// <summary>
        ///     Prepares and raises a manipulation event.
        /// </summary>
        private void PushEvent(InputEventArgs e)
        {
            // We only expect to generate one event at a time and should never need a queue.
            Debug.Assert(_generatedEvent == null, "There is already a generated event waiting to be pushed.");
            _generatedEvent = e;
        }

        /// <summary>
        ///     Pushes generated events to the inertia input provider.
        /// </summary>
        internal void PushEventsToDevice()
        {
            if (_generatedEvent != null)
            {
                InputEventArgs generatedEvent = _generatedEvent;
                _generatedEvent = null;
                _manipulationDevice.ProcessManipulationInput(generatedEvent);
            }
        }

        /// <summary>
        ///     Raises ManipulationBoundaryFeedback to allow handlers to provide feedback that manipulation has hit an edge.
        /// </summary>
        /// <param name="unusedManipulation">The total unused manipulation.</param>
        internal void RaiseBoundaryFeedback(ManipulationDelta unusedManipulation, bool requestedComplete)
        {
            bool hasUnusedManipulation = (unusedManipulation != null);
            if ((!hasUnusedManipulation || requestedComplete) && HasPendingBoundaryFeedback)
            {
                // Create a "zero" message to end currently pending feedback
                unusedManipulation = new ManipulationDelta(new Vector(), 0.0, new Vector(1.0, 1.0), new Vector());
                HasPendingBoundaryFeedback = false;
            }
            else if (hasUnusedManipulation)
            {
                HasPendingBoundaryFeedback = true;
            }

            if (unusedManipulation != null)
            {
                PushEvent(new ManipulationBoundaryFeedbackEventArgs(_manipulationDevice, LastTimestamp, _currentContainer, unusedManipulation));
            }
        }

        private bool HasPendingBoundaryFeedback
        {
            get;
            set;
        }

        private int LastTimestamp
        {
            get;
            set;
        }

        internal void ReportFrame(ICollection<IManipulator> manipulators)
        {
            Int64 timestamp = GetCurrentTimestamp();

            // InputEventArgs timestamps are Int32 while the processors take Int64
            // GetMessageTime() is used for all other InputEventArgs, such as mouse and keyboard input.
            // And it does not match QueryPerformanceCounter(), my experiments show GetMessageTime() is ~ 120ms ahead.
            LastTimestamp = SafeNativeMethods.GetMessageTime(); 

            int numManipulators = manipulators.Count;
            if (IsInertiaActive && (numManipulators > 0))
            {
                // Inertia is active but now there are fingers, stop inertia
                _inertiaProcessor.Complete(timestamp);
                PushEventsToDevice();
            }

            if (!IsManipulationActive && (numManipulators > 0))
            {
                // Time to start a new manipulation

                ManipulationStartingEventArgs startingArgs = RaiseStarting();
                if (!startingArgs.RequestedCancel && (startingArgs.Mode != ManipulationModes.None))
                {
                    // Determine if we allow single-finger manipulation
                    if (startingArgs.IsSingleTouchEnabled || (numManipulators >= 2))
                    {
                        SetContainer(startingArgs.ManipulationContainer);
                        _mode = startingArgs.Mode;
                        _pivot = startingArgs.Pivot;
                        IList<ManipulationParameters2D> parameters = startingArgs.Parameters;

                        _manipulationProcessor = new ManipulationProcessor2D(ConvertMode(_mode), ConvertPivot(_pivot));

                        if (parameters != null)
                        {
                            int count = parameters.Count;
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                _manipulationProcessor.SetParameters(parameters[i]);
                            }
                        }

                        _manipulationProcessor.Started += OnManipulationStarted;
                        _manipulationProcessor.Delta += OnManipulationDelta;
                        _manipulationProcessor.Completed += OnManipulationCompleted;

                        _currentManipulators.Clear();
                    }
                }
            }

            if (IsManipulationActive)
            {
                // A manipulation process is available to process this frame of manipulators
                UpdateManipulators(manipulators);
                _manipulationProcessor.ProcessManipulators(timestamp, CurrentManipulators);
                PushEventsToDevice();
            }
        }

        private ManipulationStartingEventArgs RaiseStarting()
        {
            ManipulationStartingEventArgs starting = new ManipulationStartingEventArgs(_manipulationDevice, Environment.TickCount);
            starting.ManipulationContainer = _manipulationDevice.Target;

            _manipulationDevice.ProcessManipulationInput(starting);

            return starting;
        }

        internal IInputElement ManipulationContainer
        {
            get { return _currentContainer; }
            set
            {
                // If a manipulation is in progress, we should consider creating some form 
                // of transition between the old coordinate space and the 
                // new one. At the very least, the old processor needs to 
                // stop and a new one needs to start.
                SetContainer(value);
            }
        }

        internal ManipulationModes ManipulationMode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                if (_manipulationProcessor != null)
                {
                    _manipulationProcessor.SupportedManipulations = ConvertMode(_mode);
                }
            }
        }

        private static Manipulations2D ConvertMode(ManipulationModes mode)
        {
            Manipulations2D manipulations = Manipulations2D.None;

            if ((mode & ManipulationModes.TranslateX) != 0)
            {
                manipulations |= Manipulations2D.TranslateX;
            }

            if ((mode & ManipulationModes.TranslateY) != 0)
            {
                manipulations |= Manipulations2D.TranslateY;
            }

            if ((mode & ManipulationModes.Scale) != 0)
            {
                manipulations |= Manipulations2D.Scale;
            }

            if ((mode & ManipulationModes.Rotate) != 0)
            {
                manipulations |= Manipulations2D.Rotate;
            }

            return manipulations;
        }

        internal ManipulationPivot ManipulationPivot
        {
            get { return _pivot; }
            set
            {
                _pivot = value;
                if (_manipulationProcessor != null)
                {
                    _manipulationProcessor.Pivot = ConvertPivot(value);
                }
            }
        }

        private static ManipulationPivot2D ConvertPivot(ManipulationPivot pivot)
        {
            if (pivot != null)
            {
                Point center = pivot.Center;
                return new ManipulationPivot2D()
                {
                    X = (float)center.X,
                    Y = (float)center.Y,
                    Radius = (float)Math.Max(1.0, pivot.Radius)
                };
            }

            return null;
        }

        internal void SetManipulationParameters(ManipulationParameters2D parameter)
        {
            if (_manipulationProcessor != null)
            {
                _manipulationProcessor.SetParameters(parameter);
            }
        }

        private void UpdateManipulators(ICollection<IManipulator> updatedManipulators)
        {
            // Clear out the old removed collection and use it to store
            // the new current collection. The old current collection
            // will be used to generate the new removed collection.
            _removedManipulators.Clear();
            var temp = _removedManipulators;
            _removedManipulators = _currentManipulators;
            _currentManipulators = temp;

            // End the manipulation if the element is not
            // visible anymore
            UIElement uie = _currentContainer as UIElement;
            if (uie != null)
            {
                if (!uie.IsVisible)
                {
                    return;
                }
            }
            else
            {
                UIElement3D uie3D = _currentContainer as UIElement3D;
                if (uie3D != null &&
                    !uie3D.IsVisible)
                {
                    return;
                }
            }

            // For each updated manipulator, convert it to the correct format in the
            // current collection and remove it from the removed collection. What is left
            // in the removed collection will be the manipulators that were removed.
            foreach (IManipulator updatedManipulator in updatedManipulators)
            {
                // consider making these Ids unique across devices
                int id = updatedManipulator.Id;
                _removedManipulators.Remove(id); // This manipulator was not removed
                Point position = updatedManipulator.GetPosition(_currentContainer);
                position = _manipulationDevice.GetTransformedManipulatorPosition(position);
                _currentManipulators[id] = new Manipulator2D(id, (float)position.X, (float)position.Y);
            }
        }

        private void SetContainer(IInputElement newContainer)
        {
            // unsubscribe from LayoutUpdated
            UnsubscribeFromLayoutUpdated();

            // clear cached values
            _containerPivotPoint = new Point();
            _containerSize = new Size();
            _root = null;

            // remember the new container
            _currentContainer = newContainer;

            if (newContainer != null)
            {
                // get the new root
                PresentationSource presentationSource = PresentationSource.CriticalFromVisual((Visual)newContainer);
                if (presentationSource != null)
                {
                    _root = presentationSource.RootVisual as UIElement;
                }

                // subscribe to LayoutUpdated
                if (_containerLayoutUpdated != null)
                {
                    SubscribeToLayoutUpdated();
                }
            }
        }

        internal event EventHandler<EventArgs> ContainerLayoutUpdated
        {
            add
            {
                bool wasNull = _containerLayoutUpdated == null;
                _containerLayoutUpdated += value;

                // if this is the first handler, try to subscribe to LayoutUpdated event
                if (wasNull && _containerLayoutUpdated != null)
                {
                    SubscribeToLayoutUpdated();
                }
            }
            remove
            {
                bool wasNull = _containerLayoutUpdated == null;
                _containerLayoutUpdated -= value;

                // if this is the last handler, unsubscribe from LayoutUpdated event
                if (!wasNull && _containerLayoutUpdated == null)
                {
                    UnsubscribeFromLayoutUpdated();
                }
            }
        }

        private void SubscribeToLayoutUpdated()
        {
            UIElement container = _currentContainer as UIElement;
            if (container != null)
            {
                container.LayoutUpdated += OnLayoutUpdated;
            }
        }

        private void UnsubscribeFromLayoutUpdated()
        {
            UIElement container = _currentContainer as UIElement;
            if (container != null)
            {
                container.LayoutUpdated -= OnLayoutUpdated;
            }
        }

        /// <summary>
        /// OnLayoutUpdated handler, raises ContainerLayoutUpdated event if container's position or size have been changed 
        /// since the last LayoutUpdate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            Debug.Assert(_containerLayoutUpdated != null);

            //check position and size and update the cached values
            if (UpdateCachedPositionAndSize())
            {
                _containerLayoutUpdated(this, EventArgs.Empty);
            }
        }

        private bool UpdateCachedPositionAndSize()
        {
            // Determine if the manipulation needs to be updated because of position or size change.
            // * Size change is detected by comparing RenderSize
            // * Position change is detected by translating PivotPoint to the element coordinate, in general
            // this is not accurate because rotation over PivotPoint won't be detected but the PivotPoint is selected far outside
            // of the Window bounds, so practically that should be a very rare case.
            // The more accurate solution would require 2 or 3 points which is more expensive.
            if (_root == null)
            {
                return false;
            }

            UIElement container = _currentContainer as UIElement;
            if (container == null)
            {
                return false;
            }

            Size renderSize = container.RenderSize;
            Point translatedPivotPoint = _root.TranslatePoint(LayoutUpdateDetectionPivotPoint, container);

            bool changed = (!DoubleUtil.AreClose(renderSize, _containerSize) ||
                            !DoubleUtil.AreClose(translatedPivotPoint, _containerPivotPoint));
            if (changed)
            {
                // update cached values
                _containerSize = renderSize;
                _containerPivotPoint = translatedPivotPoint;
            }

            return changed;
        }

        private IEnumerable<Manipulator2D> CurrentManipulators
        {
            get { return (_currentManipulators.Count > 0) ? _currentManipulators.Values : null; }
        }

        internal bool IsManipulationActive
        {
            get { return _manipulationProcessor != null; }
        }

        private bool IsInertiaActive
        {
            get { return _inertiaProcessor != null; }
        }

        private ManipulationDevice _manipulationDevice;

        private IInputElement _currentContainer;
        private ManipulationPivot _pivot;
        private ManipulationModes _mode;

        private ManipulationProcessor2D _manipulationProcessor;
        private InertiaProcessor2D _inertiaProcessor;

        // A list of manipulators that are currently active (i.e. fingers touching the screen)
        private Dictionary<int, Manipulator2D> _currentManipulators = new Dictionary<int, Manipulator2D>(2);

        // A list of manipulators that have been removed (stored to avoid allocating each frame)
        private Dictionary<int, Manipulator2D> _removedManipulators = new Dictionary<int, Manipulator2D>(2);

        // When inertia starts, its values are relative to the end point specified in
        // this event. WPF's API wants to expose inertia deltas relative to the first
        // Started event. This Completed event provides enough information to convert
        // the delta values so that they are relative to the Started event.
        private ManipulationDelta _lastManipulationBeforeInertia;

        private InputEventArgs _generatedEvent;

        private DispatcherTimer _inertiaTimer;

        private bool _manualComplete;
        private bool _manualCompleteWithInertia;

        private EventHandler<EventArgs> _containerLayoutUpdated;

        // pivot point to detect position and size change, see UpdateCachedPositionAndSize for more details
        // The odd magic number is to make it more rare.
        private static readonly Point LayoutUpdateDetectionPivotPoint = new Point(-10234.1234, -10234.1234);

        // cached values to detect position and size change
        private Point _containerPivotPoint;
        private Size _containerSize;
        private UIElement _root;
    }
}
