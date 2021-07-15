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
    ///     Allows a handler to provide feedback when a manipulation has encountered a boundary.
    /// </summary>
    public sealed class ManipulationBoundaryFeedbackEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal ManipulationBoundaryFeedbackEventArgs(
            ManipulationDevice manipulationDevice,
            int timestamp, 
            IInputElement manipulationContainer, 
            ManipulationDelta boundaryFeedback)
            : base(manipulationDevice, timestamp)
        {
            RoutedEvent = Manipulation.ManipulationBoundaryFeedbackEvent;

            ManipulationContainer = manipulationContainer;
            BoundaryFeedback = boundaryFeedback;
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

            if (RoutedEvent == Manipulation.ManipulationBoundaryFeedbackEvent)
            {
                ((EventHandler<ManipulationBoundaryFeedbackEventArgs>)genericHandler)(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
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
        ///     Returns the excess portion of a direct manipulation.
        /// </summary>
        public ManipulationDelta BoundaryFeedback
        {
            get;
            private set;
        }

        /// <summary>
        ///     Function to compensate the Manipulation positions
        ///     with respect to BoundaryFeedback.
        /// </summary>
        internal Func<Point, Point> CompensateForBoundaryFeedback
        {
            get;
            set;
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
