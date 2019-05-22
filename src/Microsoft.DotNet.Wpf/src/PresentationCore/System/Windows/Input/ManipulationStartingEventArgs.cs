// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using MS.Internal.PresentationCore;

namespace System.Windows.Input
{
    public sealed class ManipulationStartingEventArgs : InputEventArgs
    {
        internal ManipulationStartingEventArgs(
            ManipulationDevice manipulationDevice, 
            int timestamp)
            : base(manipulationDevice, timestamp)
        {
            RoutedEvent = Manipulation.ManipulationStartingEvent;
            Mode = ManipulationModes.All;
            IsSingleTouchEnabled = true;
        }

        public ManipulationModes Mode
        {
            get { return _mode; }
            set
            {
                if ((value & ~ManipulationModes.All) != 0)
                {
                    throw new ArgumentException(SR.Get(SRID.Manipulation_InvalidManipulationMode), "value");
                }

                _mode = value;
            }
        }

        /// <summary>
        ///     The ManipulationContainer defines the coordinate space of all parameters
        ///     and values for this manipulation.
        /// </summary>
        public IInputElement ManipulationContainer
        {
            get;
            set;
        }

        /// <summary>
        ///     For single-finger rotation, the pivot is used to determine how to rotate.
        /// </summary>
        /// <remarks>
        ///     The values of the the pivot properties should be in the coordinate space of the ManipulationContainer.
        /// </remarks>
        public ManipulationPivot Pivot
        {
            get;
            set;
        }

        /// <summary>
        ///     Whether one finger can start manipulation or if two or more fingers are required.
        /// </summary>
        public bool IsSingleTouchEnabled
        {
            get;
            set;
        }

        /// <summary>
        ///     Method to cancel the Manipulation
        /// </summary>
        /// <returns>A bool indicating the success of Cancel</returns>
        public bool Cancel()
        {
            RequestedCancel = true;
            return true;
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

            if (RoutedEvent == Manipulation.ManipulationStartingEvent)
            {
                ((EventHandler<ManipulationStartingEventArgs>)genericHandler)(genericTarget, this);
            }
            else
            {
                base.InvokeEventHandler(genericHandler, genericTarget);
            }
        }

        [Browsable(false)]
        public void SetManipulationParameter(ManipulationParameters2D parameter)
        {
            if (_parameters == null)
            {
                _parameters = new List<ManipulationParameters2D>(1);
            }

            _parameters.Add(parameter);
        }

        internal IList<ManipulationParameters2D> Parameters
        {
            get { return _parameters; }
        }

        private List<ManipulationParameters2D> _parameters;
        private ManipulationModes _mode;
        private IEnumerable<IManipulator> _manipulators;
    }
}
