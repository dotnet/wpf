// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Threading;
using System.Security;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Utility;


namespace System.Windows.Input
{
    /// <summary>
    ///     Analyzes input events to be processed into manipulation and inertia events.
    /// </summary>
    internal sealed class ManipulationDevice : InputDevice
    {
        /// <remarks>
        ///     Created in AddManipulationDevice.
        /// </remarks>
        private ManipulationDevice(UIElement element) : base()
        {
            _target = element;
            _activeSource = PresentationSource.CriticalFromVisual(element);

            _inputManager = InputManager.UnsecureCurrent;
            _inputManager.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);

            _manipulationLogic = new ManipulationLogic(this);
        }

        private void DetachManipulationDevice()
        {
            _inputManager.PostProcessInput -= new ProcessInputEventHandler(PostProcessInput);
        }

        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        public override IInputElement Target
        {
            get { return _target; }
        }

        /// <summary>
        ///     Returns the PresentationSource of Target.
        /// </summary>
        public override PresentationSource ActiveSource
        {
            get
            {
                return _activeSource;
            }
        }

        /// <summary>
        ///     Returns a ManipulationDevice associated with the given UIElement.
        /// </summary>
        /// <param name="element">The target of the ManipulationDevice.</param>
        /// <returns>
        ///     A ManipulationDevice associated with the element.
        ///     If a device already exists for the element, a reference to that instance
        ///     will be returned, otherwise a new instance will be created.
        /// </returns>
        /// <remarks>
        ///     This function is thread-safe but should be called only on the 
        ///     same thread that 'element' is bound to, due to possibly calling
        ///     the ManipulationDevice constructor.
        /// </remarks>
        internal static ManipulationDevice AddManipulationDevice(UIElement element)
        {
            Debug.Assert(element != null, "element should be non-null.");

            element.VerifyAccess();

            ManipulationDevice device = GetManipulationDevice(element);
            if (device == null)
            {
                if (_manipulationDevices == null)
                {
                    _manipulationDevices = new Dictionary<UIElement, ManipulationDevice>(2);
                }

                device = new ManipulationDevice(element);
                _manipulationDevices[element] = device;
            }

            return device;
        }

        /// <summary>
        ///     Returns a ManipulationDevice associated with the given UIElement.
        /// </summary>
        /// <param name="element">The target of the ManipulationDevice.</param>
        /// <returns>
        ///     A ManipulationDevice associated with the element.
        ///     If a device does not already exists for the element, null is returned.
        /// </returns>
        internal static ManipulationDevice GetManipulationDevice(UIElement element)
        {
            Debug.Assert(element != null, "element should be non-null.");

            if (_manipulationDevices != null)
            {
                ManipulationDevice device;
                _manipulationDevices.TryGetValue(element, out device);
                return device;
            }

            return null;
        }

        /// <summary>
        ///     When a ManipulationDevice is no longer needed, remove it
        ///     from the global list of devices.
        /// </summary>
        private void RemoveManipulationDevice()
        {
            _wasTicking = false;
            StopTicking();
            DetachManipulationDevice();
            _compensateForBoundaryFeedback = null;

            RemoveAllManipulators();

            if (_manipulationDevices != null)
            {
                _manipulationDevices.Remove(_target);
            }
        }

        private void RemoveAllManipulators()
        {
            if (_manipulators != null)
            {
                for (int i = _manipulators.Count - 1; i >= 0; i--)
                {
                    _manipulators[i].Updated -= OnManipulatorUpdated;
                }
                _manipulators.Clear();
            }
        }

        internal void AddManipulator(IManipulator manipulator)
        {
            Debug.Assert(manipulator != null);
            VerifyAccess();
            _manipulationEnded = false;

            if (_manipulators == null)
            {
                _manipulators = new List<IManipulator>(2);
            }

            _manipulators.Add(manipulator);
            manipulator.Updated += OnManipulatorUpdated;

            // Adding a manipulator counts as an update
            OnManipulatorUpdated(manipulator, EventArgs.Empty);
        }

        internal void RemoveManipulator(IManipulator manipulator)
        {
            Debug.Assert(manipulator != null);
            VerifyAccess();

            manipulator.Updated -= OnManipulatorUpdated;
            if (_manipulators != null)
            {
                _manipulators.Remove(manipulator);
            }

            // Removing a manipulator counts as an update
            OnManipulatorUpdated(manipulator, EventArgs.Empty);
            if (!_manipulationEnded)
            {
                if (_manipulators == null || _manipulators.Count == 0)
                {
                    // cache the last removed manipulator
                    _removedManipulator = manipulator;
                }
                // Call ReportFrame so that ManipulationInertiaStarting / ManipulationCompleted 
                // gets called synchronously if needed
                ReportFrame();
                _removedManipulator = null;
            }
        }

        internal ManipulationModes ManipulationMode
        {
            get { return _manipulationLogic.ManipulationMode; }
            set { _manipulationLogic.ManipulationMode = value; }
        }

        internal ManipulationPivot ManipulationPivot
        {
            get { return _manipulationLogic.ManipulationPivot; }
            set { _manipulationLogic.ManipulationPivot = value; }
        }

        internal IInputElement ManipulationContainer
        {
            get { return _manipulationLogic.ManipulationContainer; }
            set { _manipulationLogic.ManipulationContainer = value; }
        }

        internal IEnumerable<IManipulator> GetManipulatorsReadOnly()
        {
            if (_manipulators != null)
            {
                return new ReadOnlyCollection<IManipulator>(_manipulators);
            }
            else
            {
                return new ReadOnlyCollection<IManipulator>(new List<IManipulator>(2));
            }
        }

        internal void OnManipulatorUpdated(object sender, EventArgs e)
        {
            // After a period of inactivity, the ManipulationDevice will stop polling at the screen framerate
            // to stop wasting CPU usage. This notification will tell the device that activity is happening
            // so that it can know to poll.

            LastUpdatedTimestamp = ManipulationLogic.GetCurrentTimestamp();
            ResumeAllTicking(); // Resumes the ticking of all the suspended devices on the thread
            StartTicking(); // Ensures that we continue ticking or restart ticking for this device
        }

        internal Point GetTransformedManipulatorPosition(Point point)
        {
            if (_compensateForBoundaryFeedback != null)
            {
                return _compensateForBoundaryFeedback(point);
            }
            return point;
        }

        /// <summary>
        ///     Starts the ticking for all the ManipulationDevices
        ///     on the thread only if they were ticking earlier.
        /// </summary>
        private static void ResumeAllTicking()
        {
            if (_manipulationDevices != null)
            {
                foreach (UIElement element in _manipulationDevices.Keys)
                {
                    ManipulationDevice device = _manipulationDevices[element];
                    if (device != null && device._wasTicking)
                    {
                        device.StartTicking();
                        device._wasTicking = false;
                    }
                }
            }
        }

        private void StartTicking()
        {
            if (!_ticking)
            {
                _ticking = true;
                CompositionTarget.Rendering += new EventHandler(OnRendering);
                SubscribeToLayoutUpdate();
            }
        }

        private void StopTicking()
        {
            if (_ticking)
            {
                CompositionTarget.Rendering -= new EventHandler(OnRendering);
                _ticking = false;
                UnsubscribeFromLayoutUpdate();
            }
        }

        private void SubscribeToLayoutUpdate()
        {
            _manipulationLogic.ContainerLayoutUpdated += OnContainerLayoutUpdated;
        }

        private void UnsubscribeFromLayoutUpdate()
        {
            _manipulationLogic.ContainerLayoutUpdated -= OnContainerLayoutUpdated;
        }

        private void OnContainerLayoutUpdated(object sender, EventArgs e)
        {
            ReportFrame();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            ReportFrame();

            // If Manipulation didn't activate or becomes disabled, then stop ticking.
            // If we've exceeded the timeout without any manipulators updating, then stop ticking
            // to save energy. If a manipulator updates, we'll start ticking again.
            if (!IsManipulationActive || 
                (ManipulationLogic.GetCurrentTimestamp() - LastUpdatedTimestamp) > ThrottleTimeout)
            {
                _wasTicking = _ticking; // ReportFrame could have stopped the ticking, hence take the latest value.
                StopTicking();
            }
        }

        private void ReportFrame()
        {
            if (!_manipulationEnded)
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.ManipulationReportFrame, 0);

                _manipulationLogic.ReportFrame(_manipulators);
            }
        }

        internal bool IsManipulationActive
        {
            get
            {
                return _manipulationLogic.IsManipulationActive;
            }
        }

        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            InputEventArgs inputEventArgs = e.StagingItem.Input;
            if (inputEventArgs.Device == this)
            {
                RoutedEvent routedEvent = inputEventArgs.RoutedEvent;
                if (routedEvent == Manipulation.ManipulationDeltaEvent)
                {
                    ManipulationDeltaEventArgs deltaEventArgs = inputEventArgs as ManipulationDeltaEventArgs;
                    if (deltaEventArgs != null)
                    {
                        // During deltas, see if panning feedback is needed on the window
                        ManipulationDelta unusedManipulation = deltaEventArgs.UnusedManipulation;
                        _manipulationLogic.RaiseBoundaryFeedback(unusedManipulation, deltaEventArgs.RequestedComplete);
                        _manipulationLogic.PushEventsToDevice();

                        // If a Complete is requested, then pass it along to the manipulation processor
                        if (deltaEventArgs.RequestedComplete)
                        {
                            _manipulationLogic.Complete(/* withInertia = */ deltaEventArgs.RequestedInertia);
                            _manipulationLogic.PushEventsToDevice();
                        }
                        else if (deltaEventArgs.RequestedCancel)
                        {
                            Debug.Assert(!deltaEventArgs.IsInertial);
                            OnManipulationCancel();
                        }
                    }
                }
                else if (routedEvent == Manipulation.ManipulationStartingEvent)
                {
                    ManipulationStartingEventArgs startingEventArgs = inputEventArgs as ManipulationStartingEventArgs;
                    if (startingEventArgs != null && startingEventArgs.RequestedCancel)
                    {
                        OnManipulationCancel();
                    }
                }
                else if (routedEvent == Manipulation.ManipulationStartedEvent)
                {
                    ManipulationStartedEventArgs startedEventArgs = inputEventArgs as ManipulationStartedEventArgs;
                    if (startedEventArgs != null)
                    {
                        if (startedEventArgs.RequestedComplete)
                        {
                            // If a Complete is requested, pass it along to the manipulation processor
                            _manipulationLogic.Complete(/* withInertia = */ false);
                            _manipulationLogic.PushEventsToDevice();
                        }
                        else if (startedEventArgs.RequestedCancel)
                        {
                            OnManipulationCancel();
                        }
                        else
                        {
                            // Start ticking to produce delta events
                            ResumeAllTicking(); // Resumes the ticking of all the suspended devices on the thread
                            StartTicking(); // Ensures that we continue ticking or restart ticking for this device
                        }
                    }
                }
                else if (routedEvent == Manipulation.ManipulationInertiaStartingEvent)
                {
                    // Switching from using rendering for ticking to a timer at lower priority (handled by ManipulationLogic)
                    StopTicking();

                    // Remove all the manipulators so that we dont re-start manipulations accidentally
                    RemoveAllManipulators();

                    // Initialize inertia
                    ManipulationInertiaStartingEventArgs inertiaEventArgs = inputEventArgs as ManipulationInertiaStartingEventArgs;
                    if (inertiaEventArgs != null)
                    {
                        if (inertiaEventArgs.RequestedCancel)
                        {
                            OnManipulationCancel();
                        }
                        else
                        {
                            _manipulationLogic.BeginInertia(inertiaEventArgs);
                        }
                    }
                }
                else if (routedEvent == Manipulation.ManipulationCompletedEvent)
                {
                    _manipulationLogic.OnCompleted();
                    ManipulationCompletedEventArgs completedEventArgs = inputEventArgs as ManipulationCompletedEventArgs;
                    if (completedEventArgs != null)
                    {
                        if (completedEventArgs.RequestedCancel)
                        {
                            Debug.Assert(!completedEventArgs.IsInertial);
                            OnManipulationCancel();
                        }
                        else if (!(completedEventArgs.IsInertial && _ticking))
                        {
                            // Remove the manipulation device only if
                            // another manipulation didnot start
                            OnManipulationComplete();
                        }
                    }
                }
                else if (routedEvent == Manipulation.ManipulationBoundaryFeedbackEvent)
                {
                    ManipulationBoundaryFeedbackEventArgs boundaryEventArgs = inputEventArgs as ManipulationBoundaryFeedbackEventArgs;
                    if (boundaryEventArgs != null)
                    {
                        _compensateForBoundaryFeedback = boundaryEventArgs.CompensateForBoundaryFeedback;
                    }
                }
            }
        }

        private void OnManipulationCancel()
        {
            _manipulationEnded = true;
            if (_manipulators != null)
            {
                if (_removedManipulator != null)
                {
                    Debug.Assert(_manipulators == null || _manipulators.Count == 0);
                    // Report Manipulation Cancel to last removed manipulator
                    _removedManipulator.ManipulationEnded(true);
                }
                else
                {
                    // Report Manipulation Cancel to all the remaining manipulators
                    List<IManipulator> manipulators = new List<IManipulator>(_manipulators);
                    foreach (IManipulator manipulator in manipulators)
                    {
                        manipulator.ManipulationEnded(true);
                    }
                }
            }
            RemoveManipulationDevice();
        }

        private void OnManipulationComplete()
        {
            _manipulationEnded = true;
            if (_manipulators != null)
            {
                // Report Manipulation Complete to all the remaining manipulators
                List<IManipulator> manipulators = new List<IManipulator>(_manipulators);
                foreach (IManipulator manipulator in manipulators)
                {
                    manipulator.ManipulationEnded(false);
                }
            }
            RemoveManipulationDevice();
        }

        internal void SetManipulationParameters(ManipulationParameters2D parameter)
        {
            _manipulationLogic.SetManipulationParameters(parameter);
        }

        /// <summary>
        ///     Completes the pending manipulation or inertia.
        /// </summary>
        internal void CompleteManipulation(bool withInertia)
        {
            if (_manipulationLogic != null)
            {
                _manipulationLogic.Complete(withInertia);
                _manipulationLogic.PushEventsToDevice();
            }
        }

        internal void ProcessManipulationInput(InputEventArgs e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.ManipulationEventRaised, 0);

            _inputManager.ProcessInput(e);
        }

        private InputManager _inputManager;

        private ManipulationLogic _manipulationLogic;

        private PresentationSource _activeSource;

        private UIElement _target;
        private List<IManipulator> _manipulators;
        private bool _ticking;
        private bool _wasTicking; // boolean used to track suspended manipulation devices
        private Func<Point, Point> _compensateForBoundaryFeedback;
        private bool _manipulationEnded = false;
        IManipulator _removedManipulator = null;

        [ThreadStatic]
        private static Int64 LastUpdatedTimestamp;
        private const Int64 ThrottleTimeout = TimeSpan.TicksPerSecond * 5; // 5 seconds (in 100ns units) of no activity will throttle down

        [ThreadStatic]
        private static Dictionary<UIElement, ManipulationDevice> _manipulationDevices;
    }
}
