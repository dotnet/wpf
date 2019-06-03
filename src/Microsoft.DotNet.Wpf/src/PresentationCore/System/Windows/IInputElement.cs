// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;

namespace System.Windows
{
    /// <summary>
    /// </summary>
    public interface IInputElement 
    {
        #region Events
    
        /// <summary>
        ///     Raise routed event with the given args
        /// </summary>
        /// <param name="e">
        ///     <see cref="RoutedEventArgs"/> for the event to be raised.
        /// </param>
        void RaiseEvent(RoutedEventArgs e);

        /// <summary>
        ///     Add an instance handler for the given RoutedEvent
        /// </summary>
        /// <param name="routedEvent"/>
        /// <param name="handler"/>
        void AddHandler(RoutedEvent routedEvent, Delegate handler);
        
        /// <summary>
        ///     Remove all instances of the given 
        ///     handler for the given RoutedEvent
        /// </summary>
        /// <param name="routedEvent"/>
        /// <param name="handler"/>
        void RemoveHandler(RoutedEvent routedEvent, Delegate handler);

        #endregion Events    

        #region Input

        // Mouse          

        /// <summary>
        ///     A property indicating if the mouse is over this element or its descendents.
        /// </summary>
        bool IsMouseOver { get; }

        /// <summary>
        ///     An event reporting the left mouse button was pressed.
        /// </summary>
        event MouseButtonEventHandler PreviewMouseLeftButtonDown;
    
        /// <summary>
        ///     An event reporting the left mouse button was pressed.
        /// </summary>
        event MouseButtonEventHandler MouseLeftButtonDown;
    
        /// <summary>
        ///     An event reporting the left mouse button was released.
        /// </summary>
        event MouseButtonEventHandler PreviewMouseLeftButtonUp; 
    
        /// <summary>
        ///     An event reporting the left mouse button was released.
        /// </summary>
        event MouseButtonEventHandler MouseLeftButtonUp; 

        /// <summary>
        ///     An event reporting the right mouse button was pressed.
        /// </summary>
        event MouseButtonEventHandler PreviewMouseRightButtonDown;
    
        /// <summary>
        ///     An event reporting the right mouse button was pressed.
        /// </summary>
        event MouseButtonEventHandler MouseRightButtonDown;
    
        /// <summary>
        ///     An event reporting the right mouse button was released.
        /// </summary>
        event MouseButtonEventHandler PreviewMouseRightButtonUp; 
    
        /// <summary>
        ///     An event reporting the right mouse button was released.
        /// </summary>
        event MouseButtonEventHandler MouseRightButtonUp; 
    
        /// <summary>
        ///     An event reporting a mouse move.
        /// </summary>
        event MouseEventHandler PreviewMouseMove;  
    
        /// <summary>
        ///     An event reporting a mouse move.
        /// </summary>
        event MouseEventHandler MouseMove;  
    
        /// <summary>
        ///     An event reporting a mouse wheel rotation.
        /// </summary>
        event MouseWheelEventHandler PreviewMouseWheel;
    
        /// <summary>
        ///     An event reporting a mouse wheel rotation.
        /// </summary>
        event MouseWheelEventHandler MouseWheel;
             
         /// <summary>
         ///     A property indicating if the mouse is over this element or not.
         /// </summary>
        bool IsMouseDirectlyOver { get; }         

        /// <summary>
        ///     An event reporting the mouse entered this element.
        /// </summary>
        event MouseEventHandler MouseEnter;
    
        /// <summary>
        ///     An event reporting the mouse left this element.
        /// </summary>
        event MouseEventHandler MouseLeave;         


        /// <summary>
        ///     An event reporting that this element got the mouse capture.
        /// </summary>
        event MouseEventHandler GotMouseCapture;
        
        /// <summary>
        ///     An event reporting that this element lost the mouse capture.
        /// </summary>
        event MouseEventHandler LostMouseCapture;        
    
        /// <summary>
        ///     A property indicating if the mouse is captured to this element or not.
        /// </summary>
        bool IsMouseCaptured { get; }
    
        /// <summary>
        ///     Captures the mouse to this element.
        /// </summary>
        bool CaptureMouse();
    
        /// <summary>
        ///     Releases the mouse capture.
        /// </summary>
        void ReleaseMouseCapture();         
    
        // Stylus
        /// <summary>
        ///     A property indicating if a stylus is over this element or its descendents.
        /// </summary>
        bool IsStylusOver { get; }

        /// <summary>
        ///     An event reporting a stylus-down.
        /// </summary>
        event StylusDownEventHandler PreviewStylusDown;
    
        /// <summary>
        ///     An event reporting a stylus-down.
        /// </summary>
        event StylusDownEventHandler StylusDown;  
    
        /// <summary>
        ///     An event reporting a stylus-up.
        /// </summary>
        event StylusEventHandler PreviewStylusUp;
    
        /// <summary>
        ///     An event reporting a stylus-up.
        /// </summary>
        event StylusEventHandler StylusUp;
    
        /// <summary>
        ///     An event reporting a stylus move.
        /// </summary>
        event StylusEventHandler PreviewStylusMove;
    
        /// <summary>
        ///     An event reporting a stylus move.
        /// </summary>
        event StylusEventHandler StylusMove;  
    
        /// <summary>
        ///     An event reporting a stylus in-air-move.
        /// </summary>
        event StylusEventHandler PreviewStylusInAirMove;
        
        /// <summary>
        ///     An event reporting a stylus in-air-move.
        /// </summary>
        event StylusEventHandler StylusInAirMove;  
    
        /// <summary>
        ///     An event reporting the stylus entered this element.
        /// </summary>
        event StylusEventHandler StylusEnter;
    
        /// <summary>
        ///     An event reporting the stylus left this element.
        /// </summary>
        event StylusEventHandler StylusLeave;
    
        /// <summary>
        ///     An event reporting the stylus is now in range of the digitizer.
        /// </summary>
        event StylusEventHandler PreviewStylusInRange;
    
        /// <summary>
        ///     An event reporting the stylus is now in range of the digitizer.
        /// </summary>
        event StylusEventHandler StylusInRange;
    
        /// <summary>
        ///     An event reporting the stylus is now out of range of the digitizer.
        /// </summary>
        event StylusEventHandler PreviewStylusOutOfRange;
    
        /// <summary>
        ///     An event reporting the stylus is now out of range of the digitizer.
        /// </summary>
        event StylusEventHandler StylusOutOfRange;
    
        /// <summary>
        ///     An event reporting a stylus system gesture.
        /// </summary>
        event StylusSystemGestureEventHandler PreviewStylusSystemGesture;
    
        /// <summary>
        ///     An event reporting a stylus system gesture.
        /// </summary>
        event StylusSystemGestureEventHandler StylusSystemGesture;

        /// <summary>
        /// An event reporting stylus button down
        /// </summary>
        event StylusButtonEventHandler StylusButtonDown;

        /// <summary>
        /// An event reporting preview stylus button down
        /// </summary>
        event StylusButtonEventHandler PreviewStylusButtonDown;

        /// <summary>
        /// An event reporting preview stylus button up
        /// </summary>
        event StylusButtonEventHandler PreviewStylusButtonUp;

        /// <summary>
        /// An event reporting stylus button up
        /// </summary>
        event StylusButtonEventHandler StylusButtonUp;

        /// <summary>
        ///     A property indicating if the stylus is over this element or not.
        /// </summary>
        bool IsStylusDirectlyOver { get; }         
    
        /// <summary>
        ///     An event reporting that this element got the stylus capture.
        /// </summary>
        event StylusEventHandler GotStylusCapture;
        
        /// <summary>
        ///     An event reporting that this element lost the stylus capture.
        /// </summary>
        event StylusEventHandler LostStylusCapture;        
    
        /// <summary>
        ///     A property indicating if the stylus is captured to this element or not.
        /// </summary>
        bool IsStylusCaptured { get; }
    
        /// <summary>
        ///     Captures the stylus to this element.
        /// </summary>
        bool CaptureStylus();
    
        /// <summary>
        ///     Releases the stylus capture.
        /// </summary>
        void ReleaseStylusCapture();         

        // Keyboard         
    
        /// <summary>
        ///     An event reporting a key was pressed.
        /// </summary>
        event KeyEventHandler PreviewKeyDown; 
    
        /// <summary>
        ///     An event reporting a key was pressed.
        /// </summary>
        event KeyEventHandler KeyDown; 
    
        /// <summary>
        ///     An event reporting a key was released.
        /// </summary>
        event KeyEventHandler PreviewKeyUp;
    
        /// <summary>
        ///     An event reporting a key was released.
        /// </summary>
        event KeyEventHandler KeyUp;

        /// <summary>
        ///     A property indicating if a Focus is over this element or its descendents.
        /// </summary>
        bool IsKeyboardFocusWithin { get; }

        /// <summary>
        ///     A property indicating if the keyboard is focused on this
        ///     element or not.
        /// </summary>
        bool IsKeyboardFocused { get; }
    
        /// <summary>
        ///     Focuses the keyboard on this element.
        /// </summary>
        bool Focus();                         
    
        /// <summary>
        ///     An event announcing that the keyboard is focused on this element.
        /// </summary>
        event KeyboardFocusChangedEventHandler PreviewGotKeyboardFocus;
    
        /// <summary>
        ///     An event announcing that the keyboard is focused on this element.
        /// </summary>
        event KeyboardFocusChangedEventHandler GotKeyboardFocus;
    
        /// <summary>
        ///     An event announcing that the keyboard is no longer focused
        ///     on this element.
        /// </summary>
        event KeyboardFocusChangedEventHandler PreviewLostKeyboardFocus;
    
        /// <summary>
        ///     An event announcing that the keyboard is no longer focused
        ///     on this element.
        /// </summary>
        event KeyboardFocusChangedEventHandler LostKeyboardFocus;

        /// <summary>
        ///     A property indicating if the element is enabled or not.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        ///     Gettor and Settor for Focusable Property
        /// </summary>
        bool Focusable { get; set; }

        // Text
        
        /// <summary>
        ///     An event announcing some text input.
        /// </summary>
        event TextCompositionEventHandler PreviewTextInput;
        
        /// <summary>
        ///     An event announcing some text input.
        /// </summary>
        event TextCompositionEventHandler TextInput;         

        #endregion Input
    }
}

