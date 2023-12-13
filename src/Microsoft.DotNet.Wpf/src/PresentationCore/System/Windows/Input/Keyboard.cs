// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Windows;
using MS.Win32;
using System.Security;

namespace System.Windows.Input
{
    /// <summary>
    ///     The Keyboard class represents the mouse device to the
    ///     members of a context.
    /// </summary>
    /// <remarks>
    ///     The static members of this class simply delegate to the primary
    ///     keyboard device of the calling thread's input manager.
    /// </remarks>
    public static class Keyboard
    {
        /// <summary>
        ///     PreviewKeyDown
        /// </summary>
        public static readonly RoutedEvent PreviewKeyDownEvent = EventManager.RegisterRoutedEvent("PreviewKeyDown", RoutingStrategy.Tunnel, typeof(KeyEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the PreviewKeyDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewKeyDownHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewKeyDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewKeyDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewKeyDownHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewKeyDownEvent, handler);
        }

        /// <summary>
        ///     KeyDown
        /// </summary>
        public static readonly RoutedEvent KeyDownEvent = EventManager.RegisterRoutedEvent("KeyDown", RoutingStrategy.Bubble, typeof(KeyEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the KeyDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddKeyDownHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.AddHandler(element, KeyDownEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the KeyDown attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveKeyDownHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.RemoveHandler(element, KeyDownEvent, handler);
        }

        /// <summary>
        ///     PreviewKeyUp
        /// </summary>
        public static readonly RoutedEvent PreviewKeyUpEvent = EventManager.RegisterRoutedEvent("PreviewKeyUp", RoutingStrategy.Tunnel, typeof(KeyEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the PreviewKeyUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewKeyUpHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewKeyUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewKeyUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewKeyUpHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewKeyUpEvent, handler);
        }

        /// <summary>
        ///     KeyUp
        /// </summary>
        public static readonly RoutedEvent KeyUpEvent = EventManager.RegisterRoutedEvent("KeyUp", RoutingStrategy.Bubble, typeof(KeyEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the KeyUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddKeyUpHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.AddHandler(element, KeyUpEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the KeyUp attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void RemoveKeyUpHandler(DependencyObject element, KeyEventHandler handler)
        {
            UIElement.RemoveHandler(element, KeyUpEvent, handler);
        }

        /// <summary>
        ///     PreviewGotKeyboardFocus
        /// </summary>
        public static readonly RoutedEvent PreviewGotKeyboardFocusEvent = EventManager.RegisterRoutedEvent("PreviewGotKeyboardFocus", RoutingStrategy.Tunnel, typeof(KeyboardFocusChangedEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the PreviewGotKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewGotKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewGotKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewGotKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void RemovePreviewGotKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewGotKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     PreviewKeyboardInputProviderAcquireFocus
        /// </summary>
        public static readonly RoutedEvent PreviewKeyboardInputProviderAcquireFocusEvent = EventManager.RegisterRoutedEvent("PreviewKeyboardInputProviderAcquireFocus", RoutingStrategy.Tunnel, typeof(KeyboardInputProviderAcquireFocusEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the PreviewKeyboardInputProviderAcquireFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewKeyboardInputProviderAcquireFocusHandler(DependencyObject element, KeyboardInputProviderAcquireFocusEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewKeyboardInputProviderAcquireFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewKeyboardInputProviderAcquireFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewKeyboardInputProviderAcquireFocusHandler(DependencyObject element, KeyboardInputProviderAcquireFocusEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewKeyboardInputProviderAcquireFocusEvent, handler);
        }

        /// <summary>
        ///     KeyboardInputProviderAcquireFocus
        /// </summary>
        public static readonly RoutedEvent KeyboardInputProviderAcquireFocusEvent = EventManager.RegisterRoutedEvent("KeyboardInputProviderAcquireFocus", RoutingStrategy.Bubble, typeof(KeyboardInputProviderAcquireFocusEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the KeyboardInputProviderAcquireFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddKeyboardInputProviderAcquireFocusHandler(DependencyObject element, KeyboardInputProviderAcquireFocusEventHandler handler)
        {
            UIElement.AddHandler(element, KeyboardInputProviderAcquireFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the KeyboardInputProviderAcquireFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveKeyboardInputProviderAcquireFocusHandler(DependencyObject element, KeyboardInputProviderAcquireFocusEventHandler handler)
        {
            UIElement.RemoveHandler(element, KeyboardInputProviderAcquireFocusEvent, handler);
        }

        /// <summary>
        ///     GotKeyboardFocus
        /// </summary>
        public static readonly RoutedEvent GotKeyboardFocusEvent = EventManager.RegisterRoutedEvent("GotKeyboardFocus", RoutingStrategy.Bubble, typeof(KeyboardFocusChangedEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the GotKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddGotKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.AddHandler(element, GotKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the GotKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveGotKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.RemoveHandler(element, GotKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     PreviewLostKeyboardFocus
        /// </summary>
        public static readonly RoutedEvent PreviewLostKeyboardFocusEvent = EventManager.RegisterRoutedEvent("PreviewLostKeyboardFocus", RoutingStrategy.Tunnel, typeof(KeyboardFocusChangedEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the PreviewLostKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddPreviewLostKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.AddHandler(element, PreviewLostKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the PreviewLostKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemovePreviewLostKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.RemoveHandler(element, PreviewLostKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     LostKeyboardFocus
        /// </summary>
        public static readonly RoutedEvent LostKeyboardFocusEvent = EventManager.RegisterRoutedEvent("LostKeyboardFocus", RoutingStrategy.Bubble, typeof(KeyboardFocusChangedEventHandler), typeof(Keyboard));

        /// <summary>
        ///     Adds a handler for the LostKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddLostKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.AddHandler(element, LostKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the LostKeyboardFocus attached event
        /// </summary>
        /// <param name="element">UIElement or ContentElement that removedto this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveLostKeyboardFocusHandler(DependencyObject element, KeyboardFocusChangedEventHandler handler)
        {
            UIElement.RemoveHandler(element, LostKeyboardFocusEvent, handler);
        }

        /// <summary>
        ///     Returns the element that the keyboard is focused on.
        /// </summary>
        public static IInputElement FocusedElement
        {
            get
            {
                return Keyboard.PrimaryDevice.FocusedElement;
            }
}

        /// <summary>
        ///     Clears focus.
        /// </summary>
        public static void ClearFocus()
        {
            Keyboard.PrimaryDevice.ClearFocus();
        }

        /// <summary>
        ///     Focuses the keyboard on a particular element.
        /// </summary>
        /// <param name="element">
        ///     The element to focus the keyboard on.
        /// </param>
        public static IInputElement Focus(IInputElement element)
        {
            return Keyboard.PrimaryDevice.Focus(element);
        }

        /// <summary>
        ///     The default mode for restoring focus.
        /// <summary>
        public static RestoreFocusMode DefaultRestoreFocusMode
        {
            get
            {
                return Keyboard.PrimaryDevice.DefaultRestoreFocusMode;
            }
            
            set
            {
                Keyboard.PrimaryDevice.DefaultRestoreFocusMode = value;
            }
        }

        /// <summary>
        ///     The set of modifier keys currently pressed.
        /// </summary>
        public static ModifierKeys Modifiers
        {
            get
            {
                return Keyboard.PrimaryDevice.Modifiers;
            }
        }

        /// <summary>
        ///     Returns whether or not the specified key is down.
        /// </summary>
        public static bool IsKeyDown(Key key)
        {
            return Keyboard.PrimaryDevice.IsKeyDown(key);
        }

        /// <summary>
        ///     Returns whether or not the specified key is up.
        /// </summary>
        public static bool IsKeyUp(Key key)
        {
            return Keyboard.PrimaryDevice.IsKeyUp(key);
        }

        /// <summary>
        ///     Returns whether or not the specified key is toggled.
        /// </summary>
        public static bool IsKeyToggled(Key key)
        {
            return Keyboard.PrimaryDevice.IsKeyToggled(key);
        }

        /// <summary>
        ///     Returns the state of the specified key.
        /// </summary>
        public static KeyStates GetKeyStates(Key key)
        {
            return Keyboard.PrimaryDevice.GetKeyStates(key);
        }

        /// <summary>
        ///     The primary keyboard device.
        /// </summary>
        public static KeyboardDevice PrimaryDevice
        {
            get
            {
                KeyboardDevice keyboardDevice = InputManager.UnsecureCurrent.PrimaryKeyboardDevice;
                return keyboardDevice;
            }
        }

        // Check for Valid enum, as any int can be casted to the enum.
        internal static bool IsValidKey(Key key)
        {
            return ((int)key >= (int)Key.None && (int)key <= (int)Key.OemClear);
        }

        internal static bool IsFocusable(DependencyObject element)
        {
            // This should really be its own property, but it is hard to do efficiently.
            if (element == null)
            {
                return false;
            }

            UIElement uie = element as UIElement;
            if(uie != null)
            {
                if(uie.IsVisible == false)
                {
                    return false;
                }
            }

            if((bool)element.GetValue(UIElement.IsEnabledProperty) == false)
            {
                return false;
            }

            // There are too many conflicting desires for whether or not
            // an element is focusable.  We need to differentiate between
            // a false default value, and the user specifying false
            // explicitly.
            //
            bool hasModifiers = false;
            BaseValueSourceInternal valueSource = element.GetValueSource(UIElement.FocusableProperty, null, out hasModifiers);
            bool focusable = (bool) element.GetValue(UIElement.FocusableProperty);

            if(!focusable && valueSource == BaseValueSourceInternal.Default && !hasModifiers)
            {
                // The Focusable property was not explicitly set to anything.
                // The default value is generally false, but true in a few cases.

                if(FocusManager.GetIsFocusScope(element))
                {
                    // Focus scopes are considered focusable, even if
                    // the Focusable property is false.
                    return true;
                }
                else if(uie != null && uie.InternalVisualParent == null)
                {
                    PresentationSource presentationSource = PresentationSource.CriticalFromVisual(uie);
                    if(presentationSource != null)
                    {
                        // A UIElements that is the root of a PresentationSource is considered focusable.
                        return true;
                    }
                }
            }

            return focusable;
        }
    }
}

