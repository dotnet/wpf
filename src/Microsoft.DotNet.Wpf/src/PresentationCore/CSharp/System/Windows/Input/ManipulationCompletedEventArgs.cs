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
    ///     Provides information about the end of a manipulation.
    /// </summary>
    public sealed class ManipulationCompletedEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal ManipulationCompletedEventArgs(
            ManipulationDevice manipulationDevice,
            int timestamp, 
            IInputElement manipulationContainer,
            Point origin, 
            ManipulationDelta total,
            ManipulationVelocities velocities,
            bool isInertial)
            : base(manipulationDevice, timestamp)
        {
            if (total == null)
            {
                throw new ArgumentNullException("total");
            }

            if (velocities == null)
            {
                throw new ArgumentNullException("velocities");
            }

            RoutedEvent = Manipulation.ManipulationCompletedEvent;

            ManipulationContainer = manipulationContainer;
            ManipulationOrigin = origin;
            TotalManipulation = total;
            FinalVelocities = velocities;
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

            if (RoutedEvent == Manipulation.ManipulationCompletedEvent)
            {
                ((EventHandler<ManipulationCompletedEventArgs>)genericHandler)(genericTarget, this);
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
        public ManipulationDelta TotalManipulation
        {
            get;
            private set;
        }

        /// <summary>
        ///     Returns the current velocities associated with a manipulation.
        /// </summary>
        public ManipulationVelocities FinalVelocities
        {
            get;
            private set;
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
                return true;
            }
            return false;
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
