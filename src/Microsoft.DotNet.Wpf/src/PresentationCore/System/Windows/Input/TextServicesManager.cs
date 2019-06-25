// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Provides input to ImeProcessed promotion -- feeds keystrokes
//              to IMEs.
//
//

using System.Windows.Threading;

using MS.Internal;
using MS.Win32;

using System;
using System.Security;

namespace System.Windows.Input
{
    internal class TextServicesManager : DispatcherObject
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        internal TextServicesManager(InputManager inputManager)
        {
            _inputManager = inputManager;

            _inputManager.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
            _inputManager.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // Track the focus of KeyboardDevice. KeyboardDevice.ChangeFocus() this.
        internal void Focus(DependencyObject focus)
        {
            if (focus == null)
            {
                // Don't grab keyboard events from Text Services Framework without keyboard focus.
                this.Dispatcher.IsTSFMessagePumpEnabled = false;

                return;
            }

            // Grab keyboard events from Text Services Framework with keyboard focus.
            this.Dispatcher.IsTSFMessagePumpEnabled = true;

            if ((bool)focus.GetValue(InputMethod.IsInputMethodSuspendedProperty))
            {
                // The focus is on the element that suspending IME's input (such as menu).
                // The document focus should remain.
                return;
            }

            InputMethod.Current.EnableOrDisableInputMethod((bool)focus.GetValue(InputMethod.IsInputMethodEnabledProperty));
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
 
        // Marks interesting KeyDown events as ImeInput.
        private void PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            TextServicesContext context;
            KeyEventArgs keyArgs;

            if (!TextServicesLoader.ServicesInstalled)
                return;

            if(e.StagingItem.Input.RoutedEvent != Keyboard.PreviewKeyDownEvent &&
                e.StagingItem.Input.RoutedEvent != Keyboard.PreviewKeyUpEvent)
            {
                return;
            }

            // filter SysKey
            if (IsSysKeyDown())
                return;

            // IMM32-IME handles the key event and we don't do anything.
            if (InputMethod.IsImm32ImeCurrent())
                return;

            DependencyObject element = Keyboard.FocusedElement as DependencyObject;
            if ((element == null) || (bool)element.GetValue(InputMethod.IsInputMethodSuspendedProperty))
            {
                // The focus is on the element that suspending IME's input (such as menu).
                // we don't do anything.
                return;
            }

            keyArgs = (KeyEventArgs)e.StagingItem.Input;
            
            if(!keyArgs.Handled)
            {
                context = TextServicesContext.DispatcherCurrent;

                if (context != null)
                {
                    if (TextServicesKeystroke(context, keyArgs, true /* test */))
                    {
                        keyArgs.MarkImeProcessed();
                    }
                }
            }
        }

        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            TextServicesContext context;
            KeyEventArgs keyArgs;

            if (!TextServicesLoader.ServicesInstalled)
                return;

            // IMM32-IME handles the key event and we don't do anything.
            if (InputMethod.IsImm32ImeCurrent())
                return;

            DependencyObject element = Keyboard.FocusedElement as DependencyObject;
            if ((element == null) || (bool)element.GetValue(InputMethod.IsInputMethodSuspendedProperty))
            {
                // The focus is on the element that suspending IME's input (such as menu).
                // we don't do anything.
                return;
            }

            if(e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyDownEvent ||
               e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyUpEvent)
            {
                // filter SysKey
                if (IsSysKeyDown())
                    return;

                keyArgs = (KeyEventArgs)e.StagingItem.Input;
            
                if(!keyArgs.Handled && keyArgs.Key == Key.ImeProcessed)
                {
                    context = TextServicesContext.DispatcherCurrent;

                    if (context != null)
                    {
                        if (TextServicesKeystroke(context, keyArgs, false /* test */))
                        {
                            keyArgs.Handled = true;
                        }
                    }
                }
            }
            else if(e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent ||
                    e.StagingItem.Input.RoutedEvent == Keyboard.KeyUpEvent)
            {
                keyArgs = (KeyEventArgs)e.StagingItem.Input;
                if(!keyArgs.Handled && keyArgs.Key == Key.ImeProcessed)
                {
                    keyArgs.Handled = true;
                }
            }
        }

        private bool TextServicesKeystroke(TextServicesContext context, KeyEventArgs keyArgs, bool test)
        {
            TextServicesContext.KeyOp keyop;
            int wParam;
            int lParam;
            int scancode;

            // Cicero's Keystroke Manager and TIP does not recognize VK_RSHIFT or VK_LSHIFT.
            // We need to pass VK_SHIFT and the proper scancode.
            // 
            switch (keyArgs.RealKey)
            {
                case Key.RightShift:
                    wParam = NativeMethods.VK_SHIFT;
                    scancode = 0x36;
                    break;
                case Key.LeftShift:
                    wParam = NativeMethods.VK_SHIFT;
                    scancode = 0x2A;
                    break;
                default:
                    wParam = KeyInterop.VirtualKeyFromKey(keyArgs.RealKey);
                    scancode = 0; 
                    break;
            }

            lParam = (int)(((uint)scancode << 16) | 1);

            if (keyArgs.RoutedEvent == Keyboard.PreviewKeyDownEvent/*keyArgs.IsDown*/)
            {
                keyop = test ? TextServicesContext.KeyOp.TestDown : TextServicesContext.KeyOp.Down;
            }
            else
            {
                // Previous key state and transition state always 1 for WM_KEYUP.
                lParam |= (1 << 31) | (1 << 30);

                keyop = test ? TextServicesContext.KeyOp.TestUp : TextServicesContext.KeyOp.Up;
            }

            return context.Keystroke(wParam, lParam, keyop);
        }

        private bool IsSysKeyDown()
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || 
                Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.F10))
                return true;

            return false;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly InputManager _inputManager;

        #endregion Private Fields
    }
}

