// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input.Manipulations;
using System.Windows.Media;

using MS.Internal;

namespace System.Windows.Input
{
    /// <summary>
    ///     Provides information about the start of the inertia phase of the manipulation.
    /// </summary>
    public sealed class ManipulationInertiaStartingEventArgs : InputEventArgs
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal ManipulationInertiaStartingEventArgs(
            ManipulationDevice manipulationDevice, 
            int timestamp,
            IInputElement manipulationContainer,
            Point origin,
            ManipulationVelocities initialVelocities,
            bool isInInertia)
            : base(manipulationDevice, timestamp)
        {
            if (initialVelocities == null)
            {
                throw new ArgumentNullException("initialVelocities");
            }

            RoutedEvent = Manipulation.ManipulationInertiaStartingEvent;

            ManipulationContainer = manipulationContainer;
            ManipulationOrigin = origin;
            InitialVelocities = initialVelocities;
            _isInInertia = isInInertia;
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

            if (RoutedEvent == Manipulation.ManipulationInertiaStartingEvent)
            {
                ((EventHandler<ManipulationInertiaStartingEventArgs>)genericHandler)(genericTarget, this);
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
            set;
        }

        /// <summary>
        ///     Returns the default, calculated velocities of the current manipulation.
        /// </summary>
        /// <remarks>
        ///     These values can be used to populate the initial velocity properties in
        ///     the various behaviors that can be set.
        /// </remarks>
        public ManipulationVelocities InitialVelocities
        {
            get;
            private set;
        }

        /// <summary>
        ///     The desired behavior for how position changes due to inertia.
        /// </summary>
        public InertiaTranslationBehavior TranslationBehavior
        {
            get
            {
                if (!IsBehaviorSet(Behaviors.Translation))
                {
                    _translationBehavior = new InertiaTranslationBehavior(InitialVelocities.LinearVelocity);
                    SetBehavior(Behaviors.Translation);
                }
                return _translationBehavior;
            }
            set
            {
                _translationBehavior = value;
            }
        }

        /// <summary>
        ///     The desired behavior for how the angle changes due to inertia.
        /// </summary>
        public InertiaRotationBehavior RotationBehavior
        {
            get
            {
                if (!IsBehaviorSet(Behaviors.Rotation))
                {
                    _rotationBehavior = new InertiaRotationBehavior(InitialVelocities.AngularVelocity);
                    SetBehavior(Behaviors.Rotation);
                }
                return _rotationBehavior;
            }
            set
            {
                _rotationBehavior = value;
            }
        }

        /// <summary>
        ///     The desired behavior for how the size changes due to inertia.
        /// </summary>
        public InertiaExpansionBehavior ExpansionBehavior
        {
            get
            {
                if (!IsBehaviorSet(Behaviors.Expansion))
                {
                    _expansionBehavior = new InertiaExpansionBehavior(InitialVelocities.ExpansionVelocity);
                    SetBehavior(Behaviors.Expansion);
                }
                return _expansionBehavior;
            }
            set
            {
                _expansionBehavior = value;
            }
        }

        /// <summary>
        ///     Method to cancel the Manipulation
        /// </summary>
        /// <returns>A bool indicating the success of Cancel</returns>
        public bool Cancel()
        {
            if (!_isInInertia)
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

        /// <summary>
        ///     Supplies additional parameters for calculating the inertia.
        /// </summary>
        /// <param name="parameter">The new parameter.</param>
        [Browsable(false)]
        public void SetInertiaParameter(InertiaParameters2D parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            if (_inertiaParameters == null)
            {
                _inertiaParameters = new List<InertiaParameters2D>();
            }

            _inertiaParameters.Add(parameter);
        }

        internal bool CanBeginInertia()
        {
            if (_inertiaParameters != null && _inertiaParameters.Count > 0)
            {
                return true;
            }

            if (_translationBehavior != null && _translationBehavior.CanUseForInertia())
            {
                return true;
            }

            if (_rotationBehavior != null && _rotationBehavior.CanUseForInertia())
            {
                return true;
            }

            if (_expansionBehavior != null && _expansionBehavior.CanUseForInertia())
            {
                return true;
            }

            return false;
        }

        internal void ApplyParameters(InertiaProcessor2D processor)
        {
            processor.InitialOriginX = (float)ManipulationOrigin.X;
            processor.InitialOriginY = (float)ManipulationOrigin.Y;

            ManipulationVelocities velocities = InitialVelocities;
            
            InertiaTranslationBehavior.ApplyParameters(_translationBehavior, processor, velocities.LinearVelocity);
            InertiaRotationBehavior.ApplyParameters(_rotationBehavior, processor, velocities.AngularVelocity);
            InertiaExpansionBehavior.ApplyParameters(_expansionBehavior, processor, velocities.ExpansionVelocity);

            if (_inertiaParameters != null)
            {
                for (int i = 0, count = _inertiaParameters.Count; i < count; i++)
                {
                    processor.SetParameters(_inertiaParameters[i]);
                }
            }
        }

        private bool IsBehaviorSet(Behaviors behavior)
        {
            return ((_behaviors & behavior) == behavior);
        }

        private void SetBehavior(Behaviors behavior)
        {
            _behaviors |= behavior;
        }

        private List<InertiaParameters2D> _inertiaParameters;
        private InertiaTranslationBehavior _translationBehavior;
        private InertiaRotationBehavior _rotationBehavior;
        private InertiaExpansionBehavior _expansionBehavior;
        private Behaviors _behaviors = Behaviors.None;
        private bool _isInInertia = false; // This is true when it is a second level inertia (inertia due to inertia).
        private IEnumerable<IManipulator> _manipulators;

        [Flags]
        private enum Behaviors
        {
            None         = 0,
            Translation  = 0x00000001,
            Rotation     = 0x00000002,
            Expansion    = 0x00000004
        }
    }
}
