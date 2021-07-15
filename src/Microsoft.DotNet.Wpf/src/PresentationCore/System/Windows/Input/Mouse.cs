// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Windows;
using System.Security;
using MS.Win32;
using MS.Internal;

namespace System.Windows.Input
{
    /// <summary>
    ///     The Mouse class represents the mouse device to the
    ///     members of a context.
    /// </summary>
    /// <remarks>
    ///     The static members of this class simply delegate to the primary
    ///     mouse device of the calling thread's input manager.
    /// </remarks>
    public static class Mouse
    {
        /// <summary>
        ///     PreviewMouseMove
        /// </summary>
        public static readonly RoutedEvent PreviewMouseMoveEvent = EventManager.RegisterRoutedEvent("PreviewMouseMove", RoutingStrategy.Tunnel, typeof(MouseEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the PreviewMouseMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewMouseMoveHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewMouseMoveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewMouseMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewMouseMoveHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewMouseMoveEvent, handler);
        }

        /// <summary>
        ///     MouseMove
        /// </summary>
        public static readonly RoutedEvent MouseMoveEvent = EventManager.RegisterRoutedEvent("MouseMove", RoutingStrategy.Bubble, typeof(MouseEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddMouseMoveHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.AddHandler(element, MouseMoveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseMove attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that removedto this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveMouseMoveHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.RemoveHandler(element, MouseMoveEvent, handler);
        }

        /// <summary>
        ///     MouseDownOutsideCapturedElement
        /// </summary>
        public static readonly RoutedEvent PreviewMouseDownOutsideCapturedElementEvent = EventManager.RegisterRoutedEvent("PreviewMouseDownOutsideCapturedElement", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the PreviewMouseDownOutsideCapturedElement attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewMouseDownOutsideCapturedElementHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewMouseDownOutsideCapturedElementEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseDownOutsideCapturedElement attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewMouseDownOutsideCapturedElementHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewMouseDownOutsideCapturedElementEvent, handler);
        }

        /// <summary>
        ///     MouseUpOutsideCapturedElement
        /// </summary>
        public static readonly RoutedEvent PreviewMouseUpOutsideCapturedElementEvent = EventManager.RegisterRoutedEvent("PreviewMouseUpOutsideCapturedElement", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseUpOutsideCapturedElement attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewMouseUpOutsideCapturedElementHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewMouseUpOutsideCapturedElementEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseUpOutsideCapturedElement attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewMouseUpOutsideCapturedElementHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewMouseUpOutsideCapturedElementEvent, handler);
        }

        /// <summary>
        ///     PreviewMouseDown
        /// </summary>
        public static readonly RoutedEvent PreviewMouseDownEvent = EventManager.RegisterRoutedEvent("PreviewMouseDown", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the PreviewMouseDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewMouseDownHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewMouseDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewMouseDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewMouseDownHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewMouseDownEvent, handler);
        }

        /// <summary>
        ///     MouseDown
        /// </summary>
        public static readonly RoutedEvent MouseDownEvent = EventManager.RegisterRoutedEvent("MouseDown", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddMouseDownHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.AddHandler(element, MouseDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveMouseDownHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, MouseDownEvent, handler);
        }

        /// <summary>
        ///     PreviewMouseUp
        /// </summary>
        public static readonly RoutedEvent PreviewMouseUpEvent = EventManager.RegisterRoutedEvent("PreviewMouseUp", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the PreviewMouseUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewMouseUpHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewMouseUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewMouseUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewMouseUpHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewMouseUpEvent, handler);
        }

        /// <summary>
        ///     MouseUp
        /// </summary>
        public static readonly RoutedEvent MouseUpEvent = EventManager.RegisterRoutedEvent("MouseUp", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddMouseUpHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.AddHandler(element, MouseUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveMouseUpHandler(DependencyObject element, MouseButtonEventHandler handler)
        {
            UIElement.RemoveHandler(element, MouseUpEvent, handler);
        }

        /// <summary>
        ///     PreviewMouseWheel
        /// </summary>
        public static readonly RoutedEvent PreviewMouseWheelEvent = EventManager.RegisterRoutedEvent("PreviewMouseWheel", RoutingStrategy.Tunnel, typeof(MouseWheelEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the PreviewMouseWheel attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewMouseWheelHandler(DependencyObject element, MouseWheelEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewMouseWheelEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewMouseWheel attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewMouseWheelHandler(DependencyObject element, MouseWheelEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewMouseWheelEvent, handler);
        }

        /// <summary>
        ///     MouseWheel
        /// </summary>
        public static readonly RoutedEvent MouseWheelEvent = EventManager.RegisterRoutedEvent("MouseWheel", RoutingStrategy.Bubble, typeof(MouseWheelEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseWheel attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddMouseWheelHandler(DependencyObject element, MouseWheelEventHandler handler)
        {
            UIElement.AddHandler(element, MouseWheelEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseWheel attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveMouseWheelHandler(DependencyObject element, MouseWheelEventHandler handler)
        {
            UIElement.RemoveHandler(element, MouseWheelEvent, handler);
        }

        /// <summary>
        ///     MouseEnter
        /// </summary>
        public static readonly RoutedEvent MouseEnterEvent = EventManager.RegisterRoutedEvent("MouseEnter", RoutingStrategy.Direct, typeof(MouseEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddMouseEnterHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.AddHandler(element, MouseEnterEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseEnter attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveMouseEnterHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.RemoveHandler(element, MouseEnterEvent, handler);
        }

        /// <summary>
        ///     MouseLeave
        /// </summary>
        public static readonly RoutedEvent MouseLeaveEvent = EventManager.RegisterRoutedEvent("MouseLeave", RoutingStrategy.Direct, typeof(MouseEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the MouseLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddMouseLeaveHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.AddHandler(element, MouseLeaveEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the MouseLeave attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveMouseLeaveHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.RemoveHandler(element, MouseLeaveEvent, handler);
        }

        /// <summary>
        ///     GotMouseCapture
        /// </summary>
        public static readonly RoutedEvent GotMouseCaptureEvent = EventManager.RegisterRoutedEvent("GotMouseCapture", RoutingStrategy.Bubble, typeof(MouseEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the GotMouseCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddGotMouseCaptureHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.AddHandler(element, GotMouseCaptureEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the GotMouseCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveGotMouseCaptureHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.RemoveHandler(element, GotMouseCaptureEvent, handler);
        }

        /// <summary>
        ///     LostMouseCapture
        /// </summary>
        public static readonly RoutedEvent LostMouseCaptureEvent = EventManager.RegisterRoutedEvent("LostMouseCapture", RoutingStrategy.Bubble, typeof(MouseEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the LostMouseCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddLostMouseCaptureHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.AddHandler(element, LostMouseCaptureEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the LostMouseCapture attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveLostMouseCaptureHandler(DependencyObject element, MouseEventHandler handler)
        {
            UIElement.RemoveHandler(element, LostMouseCaptureEvent, handler);
        }

        /// <summary>
        ///     QueryCursor
        /// </summary>
        public static readonly RoutedEvent QueryCursorEvent = EventManager.RegisterRoutedEvent("QueryCursor", RoutingStrategy.Bubble, typeof(QueryCursorEventHandler), typeof(Mouse));

        /// <summary>
        ///     Adds a handler for the QueryCursor attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddQueryCursorHandler(DependencyObject element, QueryCursorEventHandler handler)
        {
            UIElement.AddHandler(element, QueryCursorEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the QueryCursor attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveQueryCursorHandler(DependencyObject element, QueryCursorEventHandler handler)
        {
            UIElement.RemoveHandler(element, QueryCursorEvent, handler);
        }

        /// <summary>
        ///     Returns the element that the mouse is over.
        /// </summary>
        /// <remarks>
        ///     This will be true if the element has captured the mouse.
        /// </remarks>
        public static IInputElement DirectlyOver
        {
            get
            {
                return Mouse.PrimaryDevice.DirectlyOver;
}
        }

        /// <summary>
        ///     Returns the element that has captured the mouse.
        /// </summary>
        public static IInputElement Captured
        { 
            get
            {
                return Mouse.PrimaryDevice.Captured;
            }
        }

        /// <summary>
        ///     Returns the element that has captured the mouse.
        /// </summary>
        internal static CaptureMode CapturedMode
        { 
            get
            {
                return Mouse.PrimaryDevice.CapturedMode;
            }
        }

        /// <summary>
        ///     Captures the mouse to a particular element.
        /// </summary>
        /// <param name="element">
        ///     The element to capture the mouse to.
        /// </param>
        public static bool Capture(IInputElement element)
        {
            return Mouse.PrimaryDevice.Capture(element);
        }

        /// <summary>
        ///     Captures the mouse to a particular element.
        /// </summary>
        /// <param name="element">
        ///     The element to capture the mouse to.
        /// </param>
        /// <param name="captureMode">
        ///     The kind of capture to acquire.
        /// </param>
        public static bool Capture(IInputElement element, CaptureMode captureMode)
        {
            return Mouse.PrimaryDevice.Capture(element, captureMode);
        }

        /// <summary>
        ///     Retrieves the history of intermediate Points up to 64 previous coordinates of the mouse or pen.
        /// </summary>
        /// <param name="relativeTo">
        ///     The element relative which the points need to be returned.
        /// </param>
        /// <param name="points">
        ///     Points relative to the first parameter are returned.
        /// </param>
        public static int GetIntermediatePoints(IInputElement relativeTo, Point[] points)
        {
            // Security Mitigation: do not give out input state if the device is not active.
            if(Mouse.PrimaryDevice.IsActive)
            {
                if (relativeTo != null)
                {
                    PresentationSource inputSource = PresentationSource.FromDependencyObject(InputElement.GetContainingVisual(relativeTo as DependencyObject));
                    if (inputSource != null)
                    {
                        IMouseInputProvider mouseInputProvider = inputSource.GetInputProvider(typeof(MouseDevice)) as IMouseInputProvider;
                        if (null != mouseInputProvider)
                        {
                            return mouseInputProvider.GetIntermediatePoints(relativeTo, points);
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// The override cursor
        /// </summary>
        public static Cursor OverrideCursor
        {
            get
            {
                return Mouse.PrimaryDevice.OverrideCursor;
            }

            set
            {
                // forwarding to the MouseDevice, will be validated there.	
                Mouse.PrimaryDevice.OverrideCursor = value;
            }
        }

        /// <summary>
        ///     Sets the mouse cursor
        /// </summary>
        /// <param name="cursor">The cursor to be set</param>
        /// <returns>True on success (always the case for Win32)</returns>
        public static bool SetCursor(Cursor cursor)
        {
            return Mouse.PrimaryDevice.SetCursor(cursor);
        }

        /// <summary>
        ///     The state of the left button.
        /// </summary>
        public static MouseButtonState LeftButton
        { 
            get
            {
                return Mouse.PrimaryDevice.LeftButton;
            }
        }

        /// <summary>
        ///     The state of the right button.
        /// </summary>
        public static MouseButtonState RightButton
        { 
            get
            {
                return Mouse.PrimaryDevice.RightButton;
            }
        }

        /// <summary>
        ///     The state of the middle button.
        /// </summary>
        public static MouseButtonState MiddleButton
        { 
            get
            {
                return Mouse.PrimaryDevice.MiddleButton;
            }
        }

        /// <summary>
        ///     The state of the first extended button.
        /// </summary>
        public static MouseButtonState XButton1
        { 
            get
            {
                return Mouse.PrimaryDevice.XButton1;
            }
        }

        /// <summary>
        ///     The state of the second extended button.
        /// </summary>
        public static MouseButtonState XButton2
        { 
            get
            {
                return Mouse.PrimaryDevice.XButton2;
            }
        }

        /// <summary>
        ///     Calculates the position of the mouse relative to
        ///     a particular element.
        /// </summary>
        public static Point GetPosition(IInputElement relativeTo)
        { 
            return Mouse.PrimaryDevice.GetPosition(relativeTo);
        }

        /// <summary>
        ///     Forces the mouse to resynchronize.
        /// </summary>
        public static void Synchronize()
        { 
            Mouse.PrimaryDevice.Synchronize();
        }

        /// <summary>
        ///     Forces the mouse cursor to be updated.
        /// </summary>
        public static void UpdateCursor()
        {
            Mouse.PrimaryDevice.UpdateCursor();
        }

        /// <summary>
        ///     The number of units the mouse wheel should be rotated to scroll one line.
        /// </summary>
        /// <remarks>
        ///     The delta was set to 120 to allow Microsoft or other vendors to
        ///     build finer-resolution wheels in the future, including perhaps
        ///     a freely-rotating wheel with no notches. The expectation is
        ///     that such a device would send more messages per rotation, but
        ///     with a smaller value in each message. To support this
        ///     possibility, you should either add the incoming delta values
        ///     until MouseWheelDeltaForOneLine amount is reached (so for a
        ///     delta-rotation you get the same response), or scroll partial
        ///     lines in response to the more frequent messages. You could also
        ///     choose your scroll granularity and accumulate deltas until it
        ///     is reached.
        /// </remarks>
        public const int MouseWheelDeltaForOneLine = 120;
        
        /// <summary>
        ///     The primary mouse device.
        /// </summary>
        public static MouseDevice PrimaryDevice
        {
            get
            {
                MouseDevice mouseDevice;
                //there is a link demand on the Current property
                mouseDevice =  InputManager.UnsecureCurrent.PrimaryMouseDevice;
                return mouseDevice;
            }
        }
    }
}
