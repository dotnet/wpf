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
using System.Windows.Media;

namespace System.Windows.Input
{
    /// <summary>
    ///     Provides an update on an ocurring manipulation.
    /// </summary>
    public sealed class ManipulationDeltaEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal ManipulationDeltaEventArgs(
            ManipulationDevice manipulationDevice,
            int timestamp,
            IInputElement manipulationContainer,
            Point origin,
            ManipulationDelta delta,
            ManipulationDelta cumulative,
            ManipulationVelocities velocities,
            bool isInertial)
            : base(manipulationDevice, timestamp)
        {
            if (delta == null)
            {
                throw new ArgumentNullException("delta");
            }

            if (cumulative == null)
            {
                throw new ArgumentNullException("cumulative");
            }

            if (velocities == null)
            {
                throw new ArgumentNullException("velocities");
            }

            RoutedEvent = Manipulation.ManipulationDeltaEvent;

            ManipulationContainer = manipulationContainer;
            ManipulationOrigin = origin;
            DeltaManipulation = delta;
            CumulativeManipulation = cumulative;
            Velocities = velocities;
            IsInertial = isInertial;
        }

        /// <summary>
        ///     Invokes a handler of this event.
        /// </summary>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler == null)
            {
                throw new ArgumentNullException("genericHandler");
            }

            if (genericTarget == null)
            {
                throw new ArgumentNullException("genericTarget");
            }

            if (RoutedEvent == Manipulation.ManipulationDeltaEvent)
            {
                ((EventHandler<ManipulationDeltaEventArgs>)genericHandler)(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }

        /// <summary>
        ///     Whether the event was generated due to inertia.
        /// </summary>
        public bool IsInertial
        {
            get;
            private set;
        }

        /// <summary>
        ///     Defines the coordinate space of the other properties.
        /// </summary>
        public IInputElement ManipulationContainer
        {
            get;
            private set;
        }

        /// <summary>
        ///     Returns the value of the origin.
        /// </summary>
        public Point ManipulationOrigin
        {
            get;
            private set;
        }

        /// <summary>
        ///     Returns the cumulative transformation associated with the manipulation.
        /// </summary>
        public ManipulationDelta CumulativeManipulation
        {
            get;
            private set;
        }

        /// <summary>
        ///     Returns the delta transformation associated with the manipulation.
        /// </summary>
        public ManipulationDelta DeltaManipulation
        {
            get;
            private set;
        }

        /// <summary>
        ///     Returns the current velocities associated with a manipulation.
        /// </summary>
        public ManipulationVelocities Velocities
        {
            get;
            private set;
        }

        /// <summary>
        ///     Allows a handler to specify that the manipulation has gone beyond certain boundaries.
        ///     By default, this value will then be used to provide panning feedback on the window, but
        ///     it can be change by handling the ManipulationBoundaryFeedback event.
        /// </summary>
        public void ReportBoundaryFeedback(ManipulationDelta unusedManipulation)
        {
            if (unusedManipulation == null)
            {
                throw new ArgumentNullException("unusedManipulation");
            }

            UnusedManipulation = unusedManipulation;
        }

        /// <summary>
        ///     The value of the unused manipulation information in global coordinate space.
        /// </summary>
        internal ManipulationDelta UnusedManipulation
        {
            get;
            private set;
        }

        /// <summary>
        ///     Preempts further processing and completes the manipulation without any inertia.
        /// </summary>
        public void Complete()
        {
            RequestedComplete = true;
            RequestedInertia = false;
            RequestedCancel = false;
        }

        /// <summary>
        ///     Preempts further processing and completes the manipulation, allowing inertia to continue.
        /// </summary>
        public void StartInertia()
        {
            RequestedComplete = true;
            RequestedInertia = true;
            RequestedCancel = false;
        }

        /// <summary>
        ///     Method to cancel the Manipulation
        /// </summary>
        /// <returns>A bool indicating the success of Cancel</returns>
        public bool Cancel()
        {
            if (!IsInertial)
            {
                RequestedCancel = true;
                RequestedComplete = false;
                RequestedInertia = false;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     A handler requested that the manipulation complete.
        /// </summary>
        internal bool RequestedComplete
        {
            get;
            private set;
        }

        /// <summary>
        ///     A handler requested that the manipulation complete with inertia.
        /// </summary>
        internal bool RequestedInertia
        {
            get;
            private set;
        }

        /// <summary>
        ///     A handler Requested to cancel the Manipulation
        /// </summary>
        internal bool RequestedCancel
        {
            get;
            private set;
        }

        /// <summary>
        ///     The Manipulators for this manipulation.
        /// </summary>
        public IEnumerable<IManipulator> Manipulators
        {
            get
            {
                if (_manipulators == null)
                {
                    _manipulators = ((ManipulationDevice)Device).GetManipulatorsReadOnly();
                }
                return _manipulators;
            }
        }

        private IEnumerable<IManipulator> _manipulators;
    }
}
