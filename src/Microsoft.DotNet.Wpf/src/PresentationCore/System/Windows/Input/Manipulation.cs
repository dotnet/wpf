// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input.Manipulations;
using MS.Internal.PresentationCore;

namespace System.Windows.Input
{
    /// <summary>
    ///     Provides access to various features and settings of manipulation.
    /// </summary>
    public static class Manipulation
    {
        internal static readonly RoutedEvent ManipulationStartingEvent = EventManager.RegisterRoutedEvent("ManipulationStarting", RoutingStrategy.Bubble, typeof(EventHandler<ManipulationStartingEventArgs>), typeof(ManipulationDevice));
        internal static readonly RoutedEvent ManipulationStartedEvent = EventManager.RegisterRoutedEvent("ManipulationStarted", RoutingStrategy.Bubble, typeof(EventHandler<ManipulationStartedEventArgs>), typeof(ManipulationDevice));
        internal static readonly RoutedEvent ManipulationDeltaEvent = EventManager.RegisterRoutedEvent("ManipulationDelta", RoutingStrategy.Bubble, typeof(EventHandler<ManipulationDeltaEventArgs>), typeof(ManipulationDevice));
        internal static readonly RoutedEvent ManipulationInertiaStartingEvent = EventManager.RegisterRoutedEvent("ManipulationInertiaStarting", RoutingStrategy.Bubble, typeof(EventHandler<ManipulationInertiaStartingEventArgs>), typeof(ManipulationDevice));
        internal static readonly RoutedEvent ManipulationBoundaryFeedbackEvent = EventManager.RegisterRoutedEvent("ManipulationBoundaryFeedback", RoutingStrategy.Bubble, typeof(EventHandler<ManipulationBoundaryFeedbackEventArgs>), typeof(ManipulationDevice));
        internal static readonly RoutedEvent ManipulationCompletedEvent = EventManager.RegisterRoutedEvent("ManipulationCompleted", RoutingStrategy.Bubble, typeof(EventHandler<ManipulationCompletedEventArgs>), typeof(ManipulationDevice));

        /// <summary>
        ///     Determines if a manipulation is currently active on an element.
        /// </summary>
        /// <param name="element">Specifies the element that may or may not have a manipulation.</param>
        /// <returns>True if there is an active manipulation, false otherwise.</returns>
        public static bool IsManipulationActive(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return GetActiveManipulationDevice(element) != null;
        }

        private static ManipulationDevice GetActiveManipulationDevice(UIElement element)
        {
            Debug.Assert(element != null, "element should be non-null.");

            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if ((device != null) && device.IsManipulationActive)
            {
                return device;
            }

            return null;
        }

        /// <summary>
        ///     If a manipulation is active, forces the manipulation to proceed to the inertia phase.
        ///     If inertia is already occurring, it will restart inertia.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        public static void StartInertia(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if (device != null)
            {
                device.CompleteManipulation(/* withInertia = */ true);
            }
        }

        /// <summary>
        ///     If a manipulation is active, forces the manipulation to complete.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        public static void CompleteManipulation(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (!TryCompleteManipulation(element))
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotActive));
            }
        }

        internal static bool TryCompleteManipulation(UIElement element)
        {
            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if (device != null)
            {
                device.CompleteManipulation(/* withInertia = */ false);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Changes the manipulation mode of an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <param name="mode">The new manipulation mode.</param>
        public static void SetManipulationMode(UIElement element, ManipulationModes mode)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = GetActiveManipulationDevice(element);
            if (device != null)
            {
                device.ManipulationMode = mode;
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotActive));
            }
        }

        /// <summary>
        ///     Retrieves the current manipulation mode of an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <returns>The current manipulation mode.</returns>
        public static ManipulationModes GetManipulationMode(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if (device != null)
            {
                return device.ManipulationMode;
            }
            else
            {
                return ManipulationModes.None;
            }
        }

        /// <summary>
        ///     Changes the container that defines the coordinate space of event data
        ///     for an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <param name="container">The container that defines the coordinate space.</param>
        public static void SetManipulationContainer(UIElement element, IInputElement container)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = GetActiveManipulationDevice(element);
            if (device != null)
            {
                device.ManipulationContainer = container;
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotActive));
            }
        }

        /// <summary>
        ///     Retrieves the container that defines the coordinate space of event data
        ///     for an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <returns>The container that defines the coordinate space.</returns>
        public static IInputElement GetManipulationContainer(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if (device != null)
            {
                return device.ManipulationContainer;
            }

            return null;
        }

        /// <summary>
        ///     Changes the pivot for single-finger manipulation on an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <param name="pivot">The new pivot for single-finger manipulation.</param>
        public static void SetManipulationPivot(UIElement element, ManipulationPivot pivot)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = GetActiveManipulationDevice(element);
            if (device != null)
            {
                device.ManipulationPivot = pivot;
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotActive));
            }
        }

        /// <summary>
        ///     Retrieves the pivot for single-finger manipulation on an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <returns>The pivot for single-finger manipulation.</returns>
        public static ManipulationPivot GetManipulationPivot(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if (device != null)
            {
                return device.ManipulationPivot;
            }

            return null;
        }

        /// <summary>
        ///     Associates a manipulator with a UIElement. This will either will start an 
        ///     active manipulation or add to an existing one.
        /// </summary>
        /// <param name="element">The element with which to associate the manipulator.</param>
        /// <param name="manipulator">The manipulator, such as a TouchDevice.</param>
        public static void AddManipulator(UIElement element, IManipulator manipulator)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (manipulator == null)
            {
                throw new ArgumentNullException("manipulator");
            }
            if (!element.IsManipulationEnabled)
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotEnabled));
            }

            ManipulationDevice device = ManipulationDevice.AddManipulationDevice(element);
            device.AddManipulator(manipulator);
        }

        /// <summary>
        ///     Disassociates a manipulator with a UIElement. This will remove it from
        ///     an active manipulation, possibly completing it.
        /// </summary>
        /// <param name="element">The element that the manipulator used to be associated with.</param>
        /// <param name="manipulator">The manipulator, such as a TouchDevice.</param>
        public static void RemoveManipulator(UIElement element, IManipulator manipulator)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (manipulator == null)
            {
                throw new ArgumentNullException("manipulator");
            }

            if (!TryRemoveManipulator(element, manipulator))
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotActive));
            }
        }

        internal static bool TryRemoveManipulator(UIElement element, IManipulator manipulator)
        {
            ManipulationDevice device = ManipulationDevice.GetManipulationDevice(element);
            if (device != null)
            {
                device.RemoveManipulator(manipulator);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Adds a ManipulationParameters2D object to an active manipulation.
        /// </summary>
        /// <param name="element">The element on which there is an active manipulation.</param>
        /// <param name="parameter">The parameter.</param>
        [Browsable(false)]
        public static void SetManipulationParameter(UIElement element, ManipulationParameters2D parameter)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            ManipulationDevice device = GetActiveManipulationDevice(element);
            if (device != null)
            {
                device.SetManipulationParameters(parameter);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.Manipulation_ManipulationNotActive));
            }
        }

        /// <summary>
        ///     Searches from the specified visual (inclusively) up the visual tree
        ///     for a UIElement that has a ManipulationMode that desires manipulation events.
        /// </summary>
        /// <param name="visual">
        ///     The starting element. Usually, this is an element that an input device has hit-tested to.
        /// </param>
        /// <returns>
        ///     A UIElement that has ManipulationMode set to Translate, Scale, Rotate, or some combination of the three.
        ///     If no element is found, then null is returned.
        /// </returns>
        /// <remarks>
        ///     This function reads data that is thread-bound and should be 
        ///     called on the same thread that 'visual' is bound to.
        /// </remarks>
        internal static UIElement FindManipulationParent(Visual visual)
        {
            while (visual != null)
            {
                UIElement element = visual as UIElement;
                if ((element != null) && element.IsManipulationEnabled)
                {
                    return element;
                }
                else
                {
                    visual = VisualTreeHelper.GetParent(visual) as Visual;
                }
            }

            return null;
        }
    }
}
