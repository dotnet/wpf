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
    ///     Provides information regarding the beginning of a manipulation.
    /// </summary>
    public sealed class ManipulationStartedEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal ManipulationStartedEventArgs(
            ManipulationDevice manipulationDevice, 
            int timestamp, 
            IInputElement manipulationContainer, 
            Point origin)
            : base(manipulationDevice, timestamp)
        {
            RoutedEvent = Manipulation.ManipulationStartedEvent;

            ManipulationContainer = manipulationContainer;
            ManipulationOrigin = origin;
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

            if (RoutedEvent == Manipulation.ManipulationStartedEvent)
            {
                ((EventHandler<ManipulationStartedEventArgs>)genericHandler)(genericTarget, this);
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
        ///     Returns the value of the origin.
        /// </summary>
        public Point ManipulationOrigin
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
            RequestedCancel = false;
        }

        /// <summary>
        ///     Method to cancel the Manipulation
        /// </summary>
        /// <returns>A bool indicating the success of Cancel</returns>
        public bool Cancel()
        {
            RequestedCancel = true;
            RequestedComplete = false;
            return true;
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
