// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security;
using System.Windows.Input.StylusWisp;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The Stylus class represents all stylus devices
    /// </summary>
    public static class Stylus
    {
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        ///     PreviewStylusDown
        /// </summary>
        public static readonly RoutedEvent PreviewStylusDownEvent = EventManager.RegisterRoutedEvent("PreviewStylusDown", RoutingStrategy.Tunnel, typeof(StylusDownEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusDownHandler(DependencyObject element, StylusDownEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusDownHandler(DependencyObject element, StylusDownEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusDownEvent, handler);
        }

        /// <summary>
        ///     StylusDown
        /// </summary>
        public static readonly RoutedEvent StylusDownEvent = EventManager.RegisterRoutedEvent("StylusDown", RoutingStrategy.Bubble, typeof(StylusDownEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusDownHandler(DependencyObject element, StylusDownEventHandler handler)
        {
            UIElement.AddHandler(element, StylusDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusDownHandler(DependencyObject element, StylusDownEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusDownEvent, handler);
        }

        /// <summary>
        ///     PreviewStylusUp
        /// </summary>
        public static readonly RoutedEvent PreviewStylusUpEvent = EventManager.RegisterRoutedEvent("PreviewStylusUp", RoutingStrategy.Tunnel, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusUpHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusUpHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusUpEvent, handler);
        }

        /// <summary>
        ///     StylusUp
        /// </summary>
        public static readonly RoutedEvent StylusUpEvent = EventManager.RegisterRoutedEvent("StylusUp", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusUpHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusUpHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusUpEvent, handler);
        }

        /// <summary>
        ///     PreviewStylusMove
        /// </summary>
        public static readonly RoutedEvent PreviewStylusMoveEvent = EventManager.RegisterRoutedEvent("PreviewStylusMove", RoutingStrategy.Tunnel, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusMoveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusMoveEvent, handler);
        }

        /// <summary>
        ///     StylusMove
        /// </summary>
        public static readonly RoutedEvent StylusMoveEvent = EventManager.RegisterRoutedEvent("StylusMove", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusMoveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusMoveEvent, handler);
        }

        /// <summary>
        ///     PreviewStylusInAirMove
        /// </summary>
        public static readonly RoutedEvent PreviewStylusInAirMoveEvent = EventManager.RegisterRoutedEvent("PreviewStylusInAirMove", RoutingStrategy.Tunnel, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusInAirMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusInAirMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusInAirMoveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusInAirMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusInAirMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusInAirMoveEvent, handler);
        }

        /// <summary>
        ///     StylusInAirMove
        /// </summary>
        public static readonly RoutedEvent StylusInAirMoveEvent = EventManager.RegisterRoutedEvent("StylusInAirMove", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusInAirMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusInAirMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusInAirMoveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusInAirMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusInAirMoveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusInAirMoveEvent, handler);
        }

        /// <summary>
        ///     StylusEnter
        /// </summary>
        public static readonly RoutedEvent StylusEnterEvent = EventManager.RegisterRoutedEvent("StylusEnter", RoutingStrategy.Direct, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusEnterHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusEnterEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusEnterHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusEnterEvent, handler);
        }

        /// <summary>
        ///     StylusLeave
        /// </summary>
        public static readonly RoutedEvent StylusLeaveEvent = EventManager.RegisterRoutedEvent("StylusLeave", RoutingStrategy.Direct, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusLeaveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusLeaveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusLeaveHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusLeaveEvent, handler);
        }

        /// <summary>
        ///     PreviewStylusInRange
        /// </summary>
        public static readonly RoutedEvent PreviewStylusInRangeEvent = EventManager.RegisterRoutedEvent("PreviewStylusInRange", RoutingStrategy.Tunnel, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusInRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusInRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusInRangeEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusInRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusInRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusInRangeEvent, handler);
        }

        /// <summary>
        ///     StylusInRange
        /// </summary>
        public static readonly RoutedEvent StylusInRangeEvent = EventManager.RegisterRoutedEvent("StylusInRange", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusInRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusInRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusInRangeEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusInRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusInRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusInRangeEvent, handler);
        }

        /// <summary>
        ///     PreviewStylusOutOfRange
        /// </summary>
        public static readonly RoutedEvent PreviewStylusOutOfRangeEvent = EventManager.RegisterRoutedEvent("PreviewStylusOutOfRange", RoutingStrategy.Tunnel, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusOutOfRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusOutOfRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusOutOfRangeEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusOutOfRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusOutOfRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusOutOfRangeEvent, handler);
        }

        /// <summary>
        ///     StylusOutOfRange
        /// </summary>
        public static readonly RoutedEvent StylusOutOfRangeEvent = EventManager.RegisterRoutedEvent("StylusOutOfRange", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusOutOfRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusOutOfRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, StylusOutOfRangeEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusOutOfRange attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusOutOfRangeHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusOutOfRangeEvent, handler);
        }

        /// <summary>
        ///     PreviewStylusSystemGesture
        /// </summary>
        public static readonly RoutedEvent PreviewStylusSystemGestureEvent = EventManager.RegisterRoutedEvent("PreviewStylusSystemGesture", RoutingStrategy.Tunnel, typeof(StylusSystemGestureEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusSystemGesture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusSystemGestureHandler(DependencyObject element, StylusSystemGestureEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusSystemGestureEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusSystemGesture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusSystemGestureHandler(DependencyObject element, StylusSystemGestureEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusSystemGestureEvent, handler);
        }

        /// <summary>
        ///     StylusSystemGesture
        /// </summary>
        public static readonly RoutedEvent StylusSystemGestureEvent = EventManager.RegisterRoutedEvent("StylusSystemGesture", RoutingStrategy.Bubble, typeof(StylusSystemGestureEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusSystemGesture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusSystemGestureHandler(DependencyObject element, StylusSystemGestureEventHandler handler)
        {
            UIElement.AddHandler(element, StylusSystemGestureEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusSystemGesture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusSystemGestureHandler(DependencyObject element, StylusSystemGestureEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusSystemGestureEvent, handler);
        }

        /// <summary>
        ///     GotStylusCapture
        /// </summary>
        public static readonly RoutedEvent GotStylusCaptureEvent = EventManager.RegisterRoutedEvent("GotStylusCapture", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the GotStylusCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddGotStylusCaptureHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, GotStylusCaptureEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the GotStylusCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveGotStylusCaptureHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, GotStylusCaptureEvent, handler);
        }

        /// <summary>
        ///     LostStylusCapture
        /// </summary>
        public static readonly RoutedEvent LostStylusCaptureEvent = EventManager.RegisterRoutedEvent("LostStylusCapture", RoutingStrategy.Bubble, typeof(StylusEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the LostStylusCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddLostStylusCaptureHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.AddHandler(element, LostStylusCaptureEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the LostStylusCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveLostStylusCaptureHandler(DependencyObject element, StylusEventHandler handler)
        {
            UIElement.RemoveHandler(element, LostStylusCaptureEvent, handler);
        }

        /// <summary>
        /// StylusButtonDown
        /// </summary>
        public static readonly RoutedEvent StylusButtonDownEvent = EventManager.RegisterRoutedEvent("StylusButtonDown", RoutingStrategy.Bubble, typeof(StylusButtonEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusButtonDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusButtonDownHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.AddHandler(element, StylusButtonDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusButtonDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusButtonDownHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusButtonDownEvent, handler);
        }


        /// <summary>
        ///     StylusButtonUp
        /// </summary>
        public static readonly RoutedEvent StylusButtonUpEvent = EventManager.RegisterRoutedEvent("StylusButtonUp", RoutingStrategy.Bubble, typeof(StylusButtonEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the StylusButtonUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddStylusButtonUpHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.AddHandler(element, StylusButtonUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the StylusButtonUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveStylusButtonUpHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, StylusButtonUpEvent, handler);
        }


        /// <summary>
        ///     PreviewStylusButtonDown
        /// </summary>
        public static readonly RoutedEvent PreviewStylusButtonDownEvent = EventManager.RegisterRoutedEvent("PreviewStylusButtonDown", RoutingStrategy.Tunnel, typeof(StylusButtonEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusButtonDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusButtonDownHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusButtonDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusButtonDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusButtonDownHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusButtonDownEvent, handler);
        }


        /// <summary>
        ///     PreviewStylusButtonUp
        /// </summary>
        public static readonly RoutedEvent PreviewStylusButtonUpEvent = EventManager.RegisterRoutedEvent("PreviewStylusButtonUp", RoutingStrategy.Tunnel, typeof(StylusButtonEventHandler), typeof(Stylus));

        /// <summary>
        ///     Adds a handler for the PreviewStylusButtonUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewStylusButtonUpHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewStylusButtonUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewStylusButtonUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewStylusButtonUpHandler(DependencyObject element, StylusButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewStylusButtonUpEvent, handler);
        }


        /// <summary>
        /// Reads the attached property IsPressAndHoldEnabled from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Stylus.IsPressAndHoldEnabledProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsPressAndHoldEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            object boolValue = element.GetValue(IsPressAndHoldEnabledProperty);

            if (boolValue == null)
                return false;  // If we don't find property then return false.
            else
                return (bool)boolValue;
        }

        /// <summary>
        /// Writes the attached property IsPressAndHoldEnabled to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="enabled">The property value to set</param>
        /// <seealso cref="Stylus.IsPressAndHoldEnabledProperty" />
        public static void SetIsPressAndHoldEnabled(DependencyObject element, bool enabled)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsPressAndHoldEnabledProperty, enabled);
        }

        /// <summary>
        /// DependencyProperty for IsPressAndHoldEnabled property.
        /// </summary>
        /// <seealso cref="Stylus.GetIsPressAndHoldEnabled" />
        /// <seealso cref="Stylus.SetIsPressAndHoldEnabled" />
        public static readonly DependencyProperty IsPressAndHoldEnabledProperty
            = DependencyProperty.RegisterAttached("IsPressAndHoldEnabled", typeof(bool), typeof(Stylus), new PropertyMetadata(true));  // note we can't specify inherits in frameworkcore.  new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits)


        /// <summary>
        /// Reads the attached property IsFlicksEnabled from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Stylus.IsFlicksEnabledProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsFlicksEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            object boolValue = element.GetValue(IsFlicksEnabledProperty);

            if (boolValue == null)
                return false;  // If we don't find property then return false.
            else
                return (bool)boolValue;
        }

        /// <summary>
        /// Writes the attached property IsFlicksEnabled to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="enabled">The property value to set</param>
        /// <seealso cref="Stylus.IsFlicksEnabledProperty" />
        public static void SetIsFlicksEnabled(DependencyObject element, bool enabled)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsFlicksEnabledProperty, enabled);
        }

        /// <summary>
        /// DependencyProperty for IsFlicksEnabled property.
        /// </summary>
        /// <seealso cref="Stylus.GetIsFlicksEnabled" />
        /// <seealso cref="Stylus.SetIsFlicksEnabled" />
        public static readonly DependencyProperty IsFlicksEnabledProperty
            = DependencyProperty.RegisterAttached("IsFlicksEnabled", typeof(bool), typeof(Stylus), new PropertyMetadata(true));  // note we can't specify inherits in frameworkcore.  new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits)


        /// <summary>
        /// Reads the attached property IsTapFeedbackEnabled from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Stylus.IsTapFeedbackEnabledProperty" />
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static bool GetIsTapFeedbackEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            object boolValue = element.GetValue(IsTapFeedbackEnabledProperty);

            if (boolValue == null)
                return false;  // If we don't find property then return false.
            else
                return (bool)boolValue;
        }

        /// <summary>
        /// Writes the attached property IsTapFeedbackEnabled to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="enabled">The property value to set</param>
        /// <seealso cref="Stylus.IsTapFeedbackEnabledProperty" />
        public static void SetIsTapFeedbackEnabled(DependencyObject element, bool enabled)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsTapFeedbackEnabledProperty, enabled);
        }

        /// <summary>
        /// DependencyProperty for IsTapFeedbackEnabled property.
        /// </summary>
        /// <seealso cref="Stylus.GetIsTapFeedbackEnabled" />
        /// <seealso cref="Stylus.SetIsTapFeedbackEnabled" />
        public static readonly DependencyProperty IsTapFeedbackEnabledProperty
            = DependencyProperty.RegisterAttached("IsTapFeedbackEnabled", typeof(bool), typeof(Stylus), new PropertyMetadata(true));  // note we can't specify inherits in frameworkcore.  new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits)


        /// <summary>
        /// Reads the attached property IsTouchFeedbackEnabled from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="Stylus.IsTouchFeedbackEnabledProperty" />
        public static bool GetIsTouchFeedbackEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            object boolValue = element.GetValue(IsTouchFeedbackEnabledProperty);

            if (boolValue == null)
                return false;  // If we don't find property then return false.
            else
                return (bool)boolValue;
        }

        /// <summary>
        /// Writes the attached property IsTouchFeedbackEnabled to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="enabled">The property value to set</param>
        /// <seealso cref="Stylus.IsTouchFeedbackEnabledProperty" />
        public static void SetIsTouchFeedbackEnabled(DependencyObject element, bool enabled)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsTouchFeedbackEnabledProperty, enabled);
        }

        /// <summary>
        /// DependencyProperty for IsTouchFeedbackEnabled property.
        /// </summary>
        /// <seealso cref="Stylus.GetIsTouchFeedbackEnabled" />
        /// <seealso cref="Stylus.SetIsTouchFeedbackEnabled" />
        public static readonly DependencyProperty IsTouchFeedbackEnabledProperty
            = DependencyProperty.RegisterAttached("IsTouchFeedbackEnabled", typeof(bool), typeof(Stylus), new PropertyMetadata(true));  // note we can't specify inherits in frameworkcore.  new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits)


        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns the element that the stylus is over.
        /// </summary>
        public static IInputElement DirectlyOver
        {
            get
            {
               return Stylus.CurrentStylusDevice?.DirectlyOver;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Returns the element that has captured the stylus.
        /// </summary>
        public static IInputElement Captured
        {
            get
            {
                return Stylus.CurrentStylusDevice?.Captured ?? Mouse.Captured;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        /// <param name="element">
        ///     The element to capture the stylus to.
        /// </param>
        public static bool Capture(IInputElement element)
        {
            return Capture(element, CaptureMode.Element);
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        /// <param name="element">
        ///     The element to capture the stylus to.
        /// </param>
        /// <param name="captureMode">
        ///     The kind of capture to acquire.
        /// </param>
        public static bool Capture(IInputElement element, CaptureMode captureMode)
        {
            // The stylus code watches mouse capture changes and it will trigger us to
            // sync up all the stylusdevice capture settings to be the same as the mouse.
            // So we just call Mouse.Capture() here to trigger this all to happen.
            return Mouse.Capture(element, captureMode);
        }


        /// <summary>
        ///     Forces the sytlus to resynchronize.
        /// </summary>
        public static void Synchronize()
        {
            CurrentStylusDevice?.Synchronize();
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     [TBS]
        /// </summary>
        public static StylusDevice CurrentStylusDevice
        {
            get
            {
                return StylusLogic.CurrentStylusLogic?.CurrentStylusDevice?.StylusDevice;
            }
        }
    }
}
